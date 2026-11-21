# KẾ HOẠCH TRIỂN KHAI (IMPLEMENTATION PLAN)
## PHASE 3 — BƯỚC 3.1 / 4: KHỞI TẠO NEXT.JS WEB PORTAL & PHÂN HỆ XÁC THỰC `(auth)`

---

## 1. Metadata
* **Mã Kế Hoạch (Related Issue ID):** `PLAN-2026-11-21-WEB-PHASE3-STEP1-AUTH`
* **File Kế Hoạch:** `docs/plans/active/2026-11-21-web-phase3-step1-auth.md`
* **Trạng Thái (Status):** `COMPLETED` *(Đã hoàn thành & nghiệm thu)*
* **Phạm Vi Tác Động (Affected Layers):**
  - `src/web/` (Khởi tạo mới toàn bộ ứng dụng Next.js App Router và phân hệ `(auth)`)
  - `docs/plans/` (Lưu trữ và theo dõi tiến độ theo quy trình `AGENT.md`)
* **Không tác động tới:** `src/management/`, `src/crypto-engine/`, PostgreSQL Schema Migrations.

---

## 2. Mục Tiêu & Lộ Trình 4 Bước Của Phase 3

Theo mục 4 tài liệu kiến trúc `docs/quicksign_software_architecture.md`, Frontend **QuickSign Web Portal (`src/web`)** được chia thành **4 bước triển khai tuần tự** tương ứng 4 Route Groups:

| Bước | Phân Hệ (Route Group) | Phạm Vi Chính | Trạng Thái |
| :---: | :--- | :--- | :---: |
| **Bước 3.1** | **`(auth)` — Nền Tảng & Xác Thực** | Khởi tạo Next.js (`src/web`), Hệ thống Tailwind Styles tập trung, Đa ngôn ngữ (`vi`/`en`), BFF HttpOnly Cookie Auth Proxy, In-Memory Access Token State, `/login`, `/register` | 🎯 **Đang triển khai** |
| **Bước 3.2** | **`(public)` — Cổng Xác Thực Công Khai** | `goCryptoHttpClient.ts`, `crypto.service.ts`, `/verify`, `/verify/[documentCode]` (cảnh báo Security Illusion ADR 0001 §6) | ⏳ Kế tiếp |
| **Bước 3.3** | **`(dashboard)` — Bảng Điều Khiển User** | `/overview`, `/sign` (PDF PAdES & JSON JWS), `/certificates`, `/api-keys`, `/documents`, `/documents/[id]/verifications` | ⏳ Sau 3.2 |
| **Bước 3.4** | **`(admin)` — Quản Trị Hệ Thống** | Guard Role `Admin`, `/admin/statistics`, `/admin/certificates` | ⏳ Sau 3.3 |

### Nguyên Tắc Cốt Lõi Của Bước 3.1
1. **Bảo mật Token tối đa (Zero-Storage XSS Protection)**:
   * `access_token` chỉ lưu trong **In-Memory State (RAM)** (`src/lib/token-memory.ts` + `AuthContext`), tuyệt đối không ghi vào `localStorage` hay `sessionStorage`.
   * `refresh_token` lưu trong **`HttpOnly` Cookie** (`HttpOnly; SameSite=Strict; Secure` trên production; `Path=/api/auth`) thông qua Next.js Server Route Handlers (`/api/auth/login`, `/api/auth/refresh`, `/api/auth/logout`).
   * Tự động khôi phục phiên bằng **Silent Refresh** khi người dùng F5 / mở lại tab.
2. **Tách biệt hoàn toàn Giao diện chung (Tailwind Theme & Style Presets)**:
   * Không hardcode chuỗi class lặp lại trong từng component.
   * Gom toàn bộ biến thiết kế vào `src/app/globals.css` và bộ class Tailwind dùng chung vào `src/styles/ui.styles.ts`.
   * Thiết kế Mobile-First ($320\text{px}$ – $430\text{px}$), mọi nút bấm/input đảm bảo kích thước vùng chạm tối thiểu $44 \times 44\text{px}$ (`AGENT.md` §4.3).
3. **Tách biệt 100% Nội dung chữ Đa ngôn ngữ (`vi` & `en`)**:
   * Tuân thủ `AGENT.md` §3.3 & §4.4: Ánh xạ toàn bộ mã lỗi `SCREAMING_SNAKE_CASE` thông qua `src/locales/vi/errors.json` và `src/locales/en/errors.json` bằng ngôn ngữ tự nhiên, lịch sự; không hiển thị raw stack trace hay raw error code.
   * Tách nội dung giao diện ra `src/locales/vi/common.json`, `src/locales/vi/auth.json` và `src/locales/en/common.json`, `src/locales/en/auth.json` với kiểu TypeScript đồng bộ 100%, kèm nút chuyển ngôn ngữ `VI / EN` tức thì.

---

## 3. Architecture Impact (Chi Tiết Thành Phần Tạo Mới / Thay Đổi)

### 3.1. Cấu Hình Dự Án & Bảo Mật (`src/web/`)
* `[NEW]` `package.json`: Next.js (App Router), React, TypeScript, Tailwind CSS, `@phosphor-icons/react`, `clsx`, `tailwind-merge`, kèm scripts `dev`, `build`, `start`, `lint`, `typecheck`, `test` (Vitest).
* `[NEW]` `tsconfig.json`, `postcss.config.mjs`, `vitest.config.ts`.
* `[NEW]` `next.config.ts`: Cấu hình Security Headers (`X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy`).
* `[NEW]` `.env.example`: Khai báo biến môi trường mẫu (`DOTNET_API_URL`, `NEXT_PUBLIC_DOTNET_API_URL`, `NEXT_PUBLIC_GO_CRYPTO_API_URL`, `NEXT_PUBLIC_APP_URL` — sử dụng placeholder, không chứa bí mật).

### 3.2. Tầng Giao Diện Chung (`src/web/src/styles/` & `src/web/src/app/globals.css`)
* `[NEW]` `src/app/globals.css`: Tailwind v4 `@theme`, font `Geist` & `Geist Mono`, màu ngữ nghĩa (Emerald/Amber/Crimson), đường cong `cubic-bezier(0.32, 0.72, 0, 1)`.
* `[NEW]` `src/styles/ui.styles.ts`: Khai báo tập trung toàn bộ class Tailwind cho `layout`, `card` (Double-Bezel), `typography`, `input`, `button` (Island Button, minimum $44\text{px}$ touch target), `alert`, `badge`.

### 3.3. Tầng Đa Ngôn Ngữ (`src/web/src/locales/` & `src/web/src/i18n/`)
* `[NEW]` `src/locales/vi/errors.json` & `src/locales/en/errors.json`: Bảng dịch đồng bộ 100% với `docs/error_codes.md`.
* `[NEW]` `src/locales/vi/common.json` & `src/locales/en/common.json`: Chuỗi văn bản dùng chung.
* `[NEW]` `src/locales/vi/auth.json` & `src/locales/en/auth.json`: Chuỗi văn bản cho trang Đăng nhập, Đăng ký, Kiểm tra mật khẩu, và Shell xác nhận phiên.
* `[NEW]` `src/i18n/types.ts` & `src/i18n/language-context.tsx`: Provider đa ngôn ngữ + Hook `useTranslation()` + hàm nội suy tham số `tError(errorCode, details)`.

### 3.4. Tầng Bảo Mật Token, BFF Route Handlers & HTTP Client
* `[NEW]` `src/lib/token-memory.ts`: Lưu `access_token` thuần trong RAM closure + hàm `decodeJwtUser()` xử lý linh hoạt cả `role` dạng chuỗi đơn lẫn mảng `string[]` (tương thích QuickBite Identity JWT).
* `[NEW]` `src/app/api/auth/login/route.ts`: BFF gọi `.NET POST /api/v1/auth/login`, tách `refresh_token` lưu vào `HttpOnly` Cookie (`qs_refresh_token`, `SameSite=Strict`, `Path=/api/auth`), chỉ trả `{ access_token, expires_in, token_type }` về client.
* `[NEW]` `src/app/api/auth/refresh/route.ts`: BFF đọc `HttpOnly` Cookie `qs_refresh_token`, gọi `.NET POST /api/v1/auth/refresh`, xoay vòng cookie mới và trả `{ access_token, expires_in }` về RAM state.
* `[NEW]` `src/app/api/auth/logout/route.ts`: BFF xóa sạch cookie `qs_refresh_token` (`Max-Age=0`).
* `[NEW]` `src/services/http/dotnetHttpClient.ts`: Fetch client gắn Bearer từ `token-memory.ts` + cơ chế **Single-Flight Refresh Lock** khi gặp `401`.
* `[NEW]` `src/services/auth.service.ts`: Đóng gói `login()`, `register()`, `refreshSession()`, `logout()`.

### 3.5. UI Primitives, Module `features/auth` & App Router Pages
* `[NEW]` `src/components/ui/double-bezel-card.tsx`, `island-button.tsx`, `form-field.tsx`, `language-switcher.tsx`.
* `[NEW]` `src/features/auth/types/auth.types.ts`: DTOs khớp 100% với `LoginRequestDto`, `RegisterRequestDto`, `AuthTokenResponseDto`.
* `[NEW]` `src/features/auth/context/auth-context.tsx`: Quản lý `user`, `isAuthenticated`, `isBootstrapping`, Silent Refresh khi mount, `login()`, `logout()`.
* `[NEW]` `src/features/auth/components/login-form.tsx`, `register-form.tsx`, `password-strength.tsx`.
* `[NEW]` `src/features/auth/utils/auth-error-mapper.ts`: Ánh xạ `errorCode` sang thông điệp thân thiện từ `locales/{vi,en}/errors.json`.
* `[NEW]` `src/app/layout.tsx`, `src/app/page.tsx`.
* `[NEW]` `src/app/(auth)/layout.tsx`, `src/app/(auth)/login/page.tsx`, `src/app/(auth)/register/page.tsx`.
* `[NEW]` `src/app/(dashboard)/overview/page.tsx`: Shell kiểm thử phiên đăng nhập In-Memory JWT, kiểm tra Silent Refresh và Đăng xuất (sẽ được nâng cấp thành Dashboard đầy đủ ở Bước 3.3).

---

## 4. Error Codes Impact (Tác Động Mã Lỗi)

* **Không bổ sung mã lỗi backend mới**. Toàn bộ mã lỗi xác thực đã có sẵn trong `docs/error_codes.md`:
  - `UNAUTHORIZED` (`401`)
  - `FORBIDDEN_ACCESS` (`403`)
  - `INVALID_CREDENTIALS` (`401`)
  - `SESSION_EXPIRED` (`401`)
  - `REFRESH_TOKEN_EXPIRED` (`401`)
  - `REGISTRATION_FAILED` (`400`)
  - `INVALID_PAYLOAD` (`400`)
  - `RATE_LIMIT_EXCEEDED` (`429`)
  - `INTERNAL_SERVER_ERROR` (`500`)
  - `SERVICE_UNAVAILABLE` (`503`)
* Bổ sung đồng bộ đầy đủ 100% các mã lỗi trong `docs/error_codes.md` vào `src/web/src/locales/vi/errors.json` và `src/web/src/locales/en/errors.json` (kèm fallback `UNKNOWN_ERROR` và `NETWORK_ERROR` cho lỗi phía client).

---

## 5. Performance & Background Offloading (Tối Ưu Hiệu Năng & Bảo Mật)

* **Single-Flight Silent Refresh Lock**: Đảm bảo dù nhiều thành phần gọi API đồng thời khi `access_token` hết hạn, chỉ có đúng **1** request `POST /api/auth/refresh` được phát đi, tránh thắt cổ chai ở Backend SSO.
* **Server Components First**: `app/layout.tsx` và `(auth)/layout.tsx` tối ưu SSR; chỉ các nhánh tương tác (form, switcher, auth context) dùng `"use client"`.
* **Zero Runtime CSS Overhead**: 100% Tailwind CSS tĩnh + animation chỉ dùng `transform` và `opacity` (GPU-safe, không gây reflow).
* **Cookie Scope Minimization**: Cookie `qs_refresh_token` đặt `Path=/api/auth` để trình duyệt không đính kèm cookie thừa vào các request tải tài nguyên tĩnh (ảnh, JS, CSS, font).

---

## 6. Execution Checklist (Danh Sách Công Việc Chi Tiết)

- [x] **Bước 1:** Khởi tạo cấu trúc dự án Next.js (`src/web`): `package.json`, `tsconfig.json`, `next.config.ts`, `postcss.config.mjs`, `vitest.config.ts`, `.env.example`, và cài đặt dependencies.
- [x] **Bước 2:** Xây dựng tầng giao diện chung: `src/app/globals.css` (Geist fonts & tokens) và `src/styles/ui.styles.ts`.
- [x] **Bước 3:** Xây dựng tầng đa ngôn ngữ (`vi` & `en`): `src/locales/{vi,en}/{errors,common,auth}.json`, `src/i18n/types.ts`, `src/i18n/language-context.tsx`.
- [x] **Bước 4:** Xây dựng tầng Token In-Memory (`src/lib/token-memory.ts`), BFF HttpOnly Cookie Route Handlers (`/api/auth/login`, `/api/auth/refresh`, `/api/auth/logout`), `dotnetHttpClient.ts` và `auth.service.ts`.
- [x] **Bước 5:** Xây dựng UI Primitives (`double-bezel-card.tsx`, `island-button.tsx`, `form-field.tsx`, `language-switcher.tsx`).
- [x] **Bước 6:** Xây dựng module `features/auth` (`auth-context.tsx`, `auth-error-mapper.ts`, `password-strength.tsx`, `login-form.tsx`, `register-form.tsx`).
- [x] **Bước 7:** Xây dựng các trang App Router: `app/layout.tsx`, `app/page.tsx`, `(auth)/layout.tsx`, `(auth)/login/page.tsx`, `(auth)/register/page.tsx`, và `(dashboard)/overview/page.tsx` (Authenticated Shell).
- [x] **Bước 8:** Viết bộ Unit Tests tự động (Vitest) cho `token-memory.ts` (giải mã JWT single-role & multi-role array, kiểm tra hết hạn), `auth-error-mapper.ts` (ánh xạ `vi`/`en` & fallback), và `dotnetHttpClient.ts` (Single-Flight Refresh & gắn Bearer header).
- [x] **Bước 9:** Chạy nghiệm thu toàn diện (`npm run lint`, `npx tsc --noEmit`, `npm run test`, `npm run build`), cập nhật checklist sang `[x]`, chuyển plan sang `docs/plans/archived/` và cập nhật `walkthrough.md`.

---

## 7. Test Verification Matrix (Ma Trận Kiểm Thử)

| Nhóm Kiểm Thử | Kịch Bản Kiểm Thử (Test Case) | Kết Quả Kỳ Vọng |
| :--- | :--- | :--- |
| **Happy Path** | Giải mã JWT hợp lệ (UTF-8 username, `sub`, `email`, `exp`) | Trích xuất chính xác `DecodedJwtUser`, `isExpired = false` |
| **Happy Path** | Đăng nhập thành công qua `/api/auth/login` | Set `HttpOnly` Cookie `qs_refresh_token`; JSON trả về chỉ có `access_token`, KHÔNG chứa `refresh_token`; lưu RAM thành công |
| **Happy Path** | Chuyển đổi ngôn ngữ `VI` $\leftrightarrow$ `EN` | Toàn bộ text, validation, và lỗi chuyển đổi tức thì, 0 key bị thiếu |
| **Edge / Boundary** | JWT chứa `role` dạng chuỗi đơn (`"Admin"`) vs mảng (`["User", "Admin"]`) | Cả hai trường hợp đều parse thành `roles: string[]` và `isAdmin: true` không gây lỗi |
| **Edge / Boundary** | Nhiều request đồng thời gặp `401` khi `access_token` hết hạn | Single-Flight Lock chỉ gửi duy nhất 1 request `/api/auth/refresh`, các request chờ dùng chung token mới |
| **Edge / Boundary** | Kiểm tra độ mạnh mật khẩu tại `/register` (thiếu chữ hoa / số / ký tự đặc biệt / $< 6$ ký tự) | Hiển thị rõ từng tiêu chí chưa đạt trước khi cho phép gửi request |
| **Security / Failure** | Kiểm tra `localStorage` & `sessionStorage` sau khi đăng nhập | Hoàn toàn không lưu `access_token` hay `refresh_token` trong Web Storage |
| **Security / Failure** | Nhận `errorCode` không xác định hoặc lỗi mạng | Ánh xạ sang thông điệp fallback tự nhiên theo ngôn ngữ `vi`/`en`, không lộ raw `errorCode` hay stack trace |
| **Security / Failure** | `refresh_token` hết hạn hoặc bị xóa trên server | `/api/auth/refresh` xóa cookie, trả `401`, client xóa RAM state và chuyển về `/login` |
