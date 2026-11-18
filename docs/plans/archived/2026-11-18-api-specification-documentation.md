# KẾ HOẠCH TRIỂN KHAI (IMPLEMENTATION PLAN): TỔNG HỢP TÀI LIỆU ĐẶC TẢ TOÀN BỘ API QUICKSIGN

---

## 1. Metadata
* **Mã Kế Hoạch:** `PLAN-2026-11-18-API-SPEC`
* **File Kế Hoạch:** `docs/plans/active/2026-11-18-api-specification-documentation.md`
* **Trạng Thái Hiện Tại:** `COMPLETED` *(Tài liệu docs/quicksign_api_specification.md đã được khởi tạo hoàn chỉnh 100%)*
* **File Tài Liệu Đích:** `docs/quicksign_api_specification.md`
* **Các Tầng/Thành Phần Liên Quan:**
  - `QuickSign Management Service` (.NET ASP.NET Core Controllers & DTOs)
  - `QuickSign Crypto Engine` (Go Engine API Specification)
  - `QuickBite Identity Provider` (SSO Auth BFF Gateway)
  - `QuickSign Centralized Error Code Registry` (`docs/error_codes.md`)

---

## 2. Mục Tiêu & Phạm Vi Công Việc

### 2.1. Mục Tiêu
Tổng hợp và chuẩn hóa toàn bộ các giao diện lập trình ứng dụng (API Endpoints) của nền tảng **QuickSign Digital Signature Platform** thành một file Markdown duy nhất tại `docs/quicksign_api_specification.md`. 

### 2.2. Yêu Cầu Cốt Lõi Về Định Dạng Cho Từng Endpoint
Mỗi API Endpoint trong tài liệu bắt buộc phải thể hiện đầy đủ và chi tiết:
1. **Method & Route Path** (ví dụ: `POST /api/v1/certificates`).
2. **Yêu Cầu Xác Thực & Phân Quyền (Auth & Permissions)**:
   - `AllowAnonymous`, `Authorize` (User), hoặc `Authorize(Roles = "Admin")`.
   - Loại Token: `Authorization: Bearer <JWT>` hoặc `X-API-Key: qs_live_...`.
3. **Tham Số Đường Dẫn (Route / Path Parameters)**: Tên, Kiểu dữ liệu, Required/Optional, Mô tả.
4. **Tham Số Truy Vấn (Query Parameters)**: Tên, Kiểu dữ liệu, Required/Optional, Giá trị mặc định (Default), Mô tả.
5. **Request Headers**: Tên header, Required/Optional, Ví dụ.
6. **Request Body**:
   - `Content-Type` (`application/json` hoặc `multipart/form-data`).
   - Bảng chi tiết từng field: **Field Name**, **Data Type**, **Required / Optional**, **Ràng Buộc & Validation Rules (Độ dài, Regex, Phạm vi)**, **Mô tả chức năng**.
   - Ví dụ JSON Request payload hoàn chỉnh.
7. **Response Body**:
   - Mã HTTP thành công (`200 OK`, `201 Created`, `204 NoContent`).
   - Bảng chi tiết từng field trả về (Tên trường, Kiểu dữ liệu, Mô tả).
   - Ví dụ JSON Response payload thành công.
8. **Mã Lỗi Tiềm Ẩn (Error Responses)**:
   - Bảng các mã lỗi `SCREAMING_SNAKE_CASE` có thể trả về, mã HTTP tương ứng và nguyên nhân kích hoạt (đồng bộ 100% với `docs/error_codes.md`).

---

## 3. Danh Mục Các Endpoint Được Tổng Hợp

Tài liệu sẽ phân nhóm toàn bộ các API theo cấu trúc logic:

### Nhóm 1: Xác Thực & Danh Tính Người Dùng (Auth BFF - .NET Proxying QuickBite)
1. `POST /api/v1/auth/login`: Đăng nhập với Username/Email và Password, nhận JWT Bearer.
2. `POST /api/v1/auth/register`: Đăng ký tài khoản người dùng mới trên hệ thống QuickBite SSO.
3. `POST /api/v1/auth/refresh`: Cấp mới Access Token bằng Refresh Token.

### Nhóm 2: Quản Lý Chứng Thư Số & Khóa Mật Mã (Certificates - .NET Management)
4. `GET /api/v1/certificates`: Lấy danh sách toàn bộ chứng thư số của người dùng hiện tại.
5. `GET /api/v1/certificates/{id}`: Xem thông tin chi tiết một chứng thư số theo ID.
6. `POST /api/v1/certificates`: Khởi tạo chứng thư số mới (gọi Go Engine tạo khóa & lưu Vault).
7. `PUT /api/v1/certificates/{id}/revoke`: Thu hồi và vô hiệu hóa chứng thư số.

### Nhóm 3: Quản Lý Khóa Tích Hợp API Key (ApiKeys - .NET Management)
8. `GET /api/v1/api-keys`: Lấy danh sách API Key của người dùng hiện tại (chỉ hiển thị tiền tố mask).
9. `POST /api/v1/api-keys`: Tạo mới API Key (trả về Plaintext key duy nhất một lần).
10. `DELETE /api/v1/api-keys/{id}`: Thu hồi và xóa vĩnh viễn quyền truy cập của API Key.

### Nhóm 4: Quản Lý Hồ Sơ & Lịch Sử Tài Liệu Đã Ký (Documents - .NET Management)
11. `GET /api/v1/documents`: Lấy danh sách tài liệu đã ký của tôi (phân trang).
12. `GET /api/v1/documents/{id}`: Tra cứu thông tin tài liệu đã ký theo GUID ID.
13. `GET /api/v1/documents/code/{documentCode}`: Tra cứu công khai thông tin tài liệu qua mã `documentCode` (gắn trong QR Code).
14. `GET /api/v1/documents/{id}/verifications`: Lấy lịch sử chi tiết các lượt kiểm tra tính toàn vẹn của tài liệu.

### Nhóm 5: Báo Cáo & Thống Kê Dashboard (Statistics - .NET Management)
15. `GET /api/v1/stats/overview`: Lấy chỉ số tổng quan (lượt ký, lượt xác thực, cảnh báo giả mạo, cert active, key active).

### Nhóm 6: Quản Trị Hệ Thống Dành Riêng Cho Quản Trị Viên (Admin - .NET Management)
16. `GET /api/v1/admin/stats/overview`: Thống kê tổng quan toàn bộ nền tảng.
17. `PUT /api/v1/admin/certificates/{id}/revoke`: Thu hồi khẩn cấp bất kỳ chứng thư nào của bất kỳ người dùng nào.

### Nhóm 7: Ký Số & Xác Thực Mật Mã Trực Tiếp (Go Crypto Engine Specification - Giai Đoạn 2)
18. `POST /api/v1/sign/pdf`: Nhận file PDF stream, nhúng chữ ký PAdES và mã tra cứu, trả về file PDF đã ký.
19. `POST /api/v1/sign/json`: Nhận JSON payload, ký chuẩn JWS detached/compact, trả về chuỗi JWS token.
20. `POST /api/v1/verify/file`: Tải file PDF hoặc JWS lên để kiểm tra tính toàn vẹn mật mã học và trạng thái thu hồi.
21. `GET /api/v1/verify/code/{documentCode}`: Endpoint tra cứu nhanh từ Go Engine hỗ trợ cảnh báo Security Illusion (`CODE_EXISTS_UNVERIFIED_CONTENT`).

---

## 4. Kế Hoạch Triển Khai Chi Tiết (Execution Checklist)

- [x] **Giai đoạn 1: Chuẩn bị & Trích xuất dữ liệu metadata**
  - [x] Rà soát 100% Request DTOs, Response DTOs và FluentValidation Validators trong tầng `QuickSign.Application`.
  - [x] Rà soát các route attributes, status codes, query parameters trong tầng `QuickSign.HttpApi`.
  - [x] Đối chiếu mã lỗi cho từng API với `docs/error_codes.md`.
- [x] **Giai đoạn 2: Khởi tạo file đặc tả `docs/quicksign_api_specification.md`**
  - [x] Viết phần mở đầu: Tổng quan kiến trúc, Base URL, Authentication schemes, Unified Error Response format.
  - [x] Viết chi tiết Nhóm 1: Xác thực & Danh tính (3 endpoints).
  - [x] Viết chi tiết Nhóm 2: Quản lý chứng thư số (4 endpoints).
  - [x] Viết chi tiết Nhóm 3: Quản lý API Key (3 endpoints).
  - [x] Viết chi tiết Nhóm 4: Quản lý tài liệu đã ký (4 endpoints).
  - [x] Viết chi tiết Nhóm 5: Thống kê Dashboard (1 endpoint).
  - [x] Viết chi tiết Nhóm 6: Quản trị Admin (2 endpoints).
  - [x] Viết chi tiết Nhóm 7: Đặc tả luồng Ký & Xác thực Go Crypto Engine (4 endpoints).
- [x] **Giai đoạn 3: Rà soát & Nghiệm thu**
  - [x] Kiểm tra tính đầy đủ của các trường: Data type, Required/Optional, Min/Max length, Enum values.
  - [x] Kiểm tra tính chính xác của ví dụ JSON Request/Response.
  - [x] Đảm bảo link nội bộ và bảng mục lục (TOC) hoạt động trơn tru.

---

## 5. Kế Hoạch Kiểm Tra & Xác Minh (Verification Matrix)

| Hạng Mục | Tiêu Chí Kiểm Tra | Kết Quả Thực Tế | Đánh Giá |
| :--- | :--- | :--- | :---: |
| **Tính đầy đủ** | Kiểm tra toàn bộ 21 endpoints (.NET hiện có + Go Spec) | Đầy đủ 21/21 endpoints được ghi nhận chi tiết | **ĐẠT** |
| **Chi tiết tham số** | Kiểm tra từng trường trong Body và Param | Đánh dấu rõ ràng `Required` hoặc `Optional`, kèm quy tắc validation | **ĐẠT** |
| **Đồng bộ mã lỗi** | Đối chiếu các mã lỗi được liệt kê với `docs/error_codes.md` | Khớp 100% với từ điển mã lỗi hệ thống (`SCREAMING_SNAKE_CASE`) | **ĐẠT** |
| **Định dạng Markdown** | Bảng Markdown, Code block JSON, GitHub Alerts | Định dạng rõ ràng, chuyên nghiệp, cấu trúc chuẩn Enterprise | **ĐẠT** |
