import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import { LanguageProvider } from "@/i18n/language-context";
import { AuthProvider } from "@/features/auth/context/auth-context";
import { uiStyles } from "@/styles/ui.styles";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin", "latin-ext"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "QuickSign — Digital Signature & Integrity Platform",
  description:
    "Cryptographic signing (PAdES-BES & JWS) and byte-level document verification platform.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi" className={`${geistSans.variable} ${geistMono.variable}`}>
      <body className={uiStyles.layout.pageCanvas}>
        <div className="bg-noise-overlay pointer-events-none fixed inset-0 z-0" aria-hidden="true" />
        <LanguageProvider>
          <AuthProvider>
            <div className="relative z-10">{children}</div>
          </AuthProvider>
        </LanguageProvider>
      </body>
    </html>
  );
}
