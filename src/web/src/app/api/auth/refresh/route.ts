import { cookies } from "next/headers";
import { NextRequest, NextResponse } from "next/server";

const REFRESH_COOKIE_NAME = "qs_refresh_token";
const SESSION_COOKIE_NAME = "qs_access_session";
const DOTNET_API_URL =
  process.env.DOTNET_API_URL ??
  process.env.NEXT_PUBLIC_DOTNET_API_URL ??
  "http://localhost:5001/api/v1";

interface DotNetAuthResponse {
  access_token: string;
  token_type?: string;
  expires_in?: number;
  refresh_token?: string | null;
}

function isTokenStillValid(token: string): boolean {
  try {
    const parts = token.split(".");
    if (parts.length !== 3) return false;
    const payloadJson = Buffer.from(parts[1], "base64url").toString("utf-8");
    const claims = JSON.parse(payloadJson) as { exp?: number };
    if (!claims.exp) return true;
    return claims.exp > Math.floor(Date.now() / 1000) + 10;
  } catch {
    return false;
  }
}

export async function POST(request: NextRequest) {
  const cookieStore = await cookies();
  const refreshToken = cookieStore.get(REFRESH_COOKIE_NAME)?.value;
  const sessionToken = cookieStore.get(SESSION_COOKIE_NAME)?.value;
  const isSecure = request.nextUrl.protocol === "https:";

  if (refreshToken) {
    try {
      // .NET AuthController.RefreshTokenAsync expects [FromBody] string refreshToken
      const upstream = await fetch(`${DOTNET_API_URL}/auth/refresh`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(refreshToken),
        cache: "no-store",
      });

      const text = await upstream.text();
      const data = text ? (JSON.parse(text) as Record<string, unknown>) : {};

      if (upstream.ok) {
        const authData = data as unknown as DotNetAuthResponse;
        const expiresIn = authData.expires_in ?? 3600;
        const response = NextResponse.json(
          {
            access_token: authData.access_token,
            token_type: authData.token_type ?? "Bearer",
            expires_in: expiresIn,
          },
          { status: 200 }
        );

        if (authData.refresh_token) {
          response.cookies.set(REFRESH_COOKIE_NAME, authData.refresh_token, {
            httpOnly: true,
            secure: isSecure,
            sameSite: "lax",
            path: "/api/auth",
            maxAge: 30 * 24 * 60 * 60,
          });
        }

        if (authData.access_token) {
          response.cookies.set(SESSION_COOKIE_NAME, authData.access_token, {
            httpOnly: true,
            secure: isSecure,
            sameSite: "lax",
            path: "/api/auth",
            maxAge: expiresIn,
          });
        }

        return response;
      }

      if (upstream.status !== 401 && sessionToken && isTokenStillValid(sessionToken)) {
        return NextResponse.json(
          { access_token: sessionToken, token_type: "Bearer" },
          { status: 200 }
        );
      }
    } catch {
      if (sessionToken && isTokenStillValid(sessionToken)) {
        return NextResponse.json(
          { access_token: sessionToken, token_type: "Bearer" },
          { status: 200 }
        );
      }
      return NextResponse.json(
        {
          errorCode: "SERVICE_UNAVAILABLE",
          httpStatus: 503,
          timestamp: new Date().toISOString(),
        },
        { status: 503 }
      );
    }
  }

  // Fallback to HttpOnly session cookie if SSO did not issue a refresh_token
  if (sessionToken && isTokenStillValid(sessionToken)) {
    return NextResponse.json(
      { access_token: sessionToken, token_type: "Bearer" },
      { status: 200 }
    );
  }

  const missingResponse = NextResponse.json(
    {
      errorCode: "REFRESH_TOKEN_MISSING",
      httpStatus: 401,
      timestamp: new Date().toISOString(),
    },
    { status: 401 }
  );
  missingResponse.cookies.delete({ name: REFRESH_COOKIE_NAME, path: "/api/auth" });
  missingResponse.cookies.delete({ name: SESSION_COOKIE_NAME, path: "/api/auth" });
  return missingResponse;
}
