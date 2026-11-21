import { NextRequest, NextResponse } from "next/server";

const REFRESH_COOKIE_NAME = "qs_refresh_token";
const DOTNET_API_URL = process.env.DOTNET_API_URL ?? process.env.NEXT_PUBLIC_DOTNET_API_URL ?? "http://localhost:5001/api/v1";

interface DotNetAuthResponse {
  access_token: string;
  token_type?: string;
  expires_in?: number;
  refresh_token?: string;
}

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();

    const upstream = await fetch(`${DOTNET_API_URL}/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
      cache: "no-store",
    });

    const text = await upstream.text();
    const data = text ? (JSON.parse(text) as Record<string, unknown>) : {};

    if (!upstream.ok) {
      return NextResponse.json(data, { status: upstream.status });
    }

    const authData = data as unknown as DotNetAuthResponse;
    const response = NextResponse.json(
      {
        access_token: authData.access_token,
        token_type: authData.token_type ?? "Bearer",
        expires_in: authData.expires_in ?? 3600,
      },
      { status: 200 }
    );

    if (authData.refresh_token) {
      response.cookies.set(REFRESH_COOKIE_NAME, authData.refresh_token, {
        httpOnly: true,
        secure: process.env.NODE_ENV === "production",
        sameSite: "strict",
        path: "/api/auth",
        maxAge: 30 * 24 * 60 * 60,
      });
    }

    return response;
  } catch {
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
