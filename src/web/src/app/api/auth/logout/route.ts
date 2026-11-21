import { NextResponse } from "next/server";

const REFRESH_COOKIE_NAME = "qs_refresh_token";

export async function POST() {
  const response = NextResponse.json({ success: true }, { status: 200 });
  response.cookies.set(REFRESH_COOKIE_NAME, "", {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "strict",
    path: "/api/auth",
    maxAge: 0,
  });
  return response;
}
