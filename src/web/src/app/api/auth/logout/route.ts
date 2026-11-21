import { NextRequest, NextResponse } from "next/server";

const REFRESH_COOKIE_NAME = "qs_refresh_token";
const SESSION_COOKIE_NAME = "qs_access_session";

export async function POST(request: NextRequest) {
  const isSecure = request.nextUrl.protocol === "https:";
  const response = NextResponse.json({ success: true }, { status: 200 });

  response.cookies.set(REFRESH_COOKIE_NAME, "", {
    httpOnly: true,
    secure: isSecure,
    sameSite: "lax",
    path: "/api/auth",
    maxAge: 0,
  });

  response.cookies.set(SESSION_COOKIE_NAME, "", {
    httpOnly: true,
    secure: isSecure,
    sameSite: "lax",
    path: "/api/auth",
    maxAge: 0,
  });

  return response;
}
