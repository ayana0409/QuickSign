# ADR 0001: Standardization, Cryptography & Distributed Integrity

**Date:** 2026-11-17
**Status:** Accepted
**Context:** 
Hệ thống QuickSign đối mặt với các xung đột về chuẩn mã lỗi giữa ABP và quy trình nội bộ, rủi ro hỏng cấu trúc PDF khi nhúng metadata, thất thoát RAM khi xử lý Private Key trong Go, lỗi mồ côi dữ liệu phân tán (Vault vs DB) và ảo giác bảo mật khi tra cứu QR Code.

## Quyết định Kỹ thuật (Decisions)

### 1. Chuẩn hóa Mã Lỗi (Error Codes)
- **Quyết định:** Bãi bỏ định dạng Namespace của ABP (VD: `QuickSign:Auth:001`). Toàn bộ hệ thống (.NET, Go) bắt buộc sử dụng chuẩn `SCREAMING_SNAKE_CASE` (VD: `TAMPERED_CONTENT`). 
- **Lý do:** Tách bạch UI/UX. Frontend chỉ cần map một chuỗi duy nhất với từ điển đa ngôn ngữ (i18n).

### 2. Ký số PAdES & Metadata (Golang)
- **Quyết định:** `document_code` bắt buộc được nhúng vào **Signed Attributes (PKCS#9)** bên trong khối CMS/PKCS#7. Tuyệt đối không chèn trực tiếp vào PDF Dictionary.
- **Lý do:** Đảm bảo Incremental Update hợp chuẩn ISO 32000-1. Metadata được băm cùng tài liệu, không thể bị làm giả và không làm hỏng file khi mở bằng Adobe Acrobat.

### 3. Giải phóng Private Key trên RAM (Memory Zeroing)
- **Quyết định:** Sau khi sử dụng xong `*rsa.PrivateKey` / `*ecdsa.PrivateKey`, không chỉ xóa byte raw (PEM/DER) mà phải gọi utility custom (dùng `reflect` hoặc truy cập trực tiếp) để ghi đè `0` (zeroing) lên toàn bộ các lát cắt `big.Int.Bytes()` (`D, P, Q, Dp, Dq`) trước khi thu hồi.
- **Lý do:** Chống tấn công trích xuất RAM (Memory Dump/Cold Boot Attack) đối với các số nguyên lớn nằm trên Heap của Go runtime.

### 4. Giao dịch bù trừ (Compensating Transactions)
- **Quyết định:** Khi .NET gọi gRPC sinh khóa trên Vault thành công nhưng insert PostgreSQL thất bại, khối `catch` bắt buộc phải gọi lại gRPC `DeleteKey(vaultPath)`.
- **Lý do:** Triệt tiêu hoàn toàn rác và "khóa mồ côi" (Orphan Keys) kẹt lại vĩnh viễn trong HashiCorp Vault.

### 5. Xử lý Race Condition & JWT Parsing
- **Quyết định:** Go Worker cập nhật bộ đếm phải dùng SQL Atomic (`UPDATE table SET count = count + 1 WHERE id = $1`). Dùng `FlexibleRoleClaim` (Custom JSON Unmarshaler) để xử lý claim `role` linh hoạt (string/array) từ QuickBite Identity.

### 6. Cảnh báo Ảo giác Bảo mật (Security Illusion)
- **Quyết định:** API quét mã QR `GET /api/v1/verify/code/{code}` phải trả về status `CODE_EXISTS_UNVERIFIED_CONTENT`. UI Next.js bắt buộc hiện màn hình cảnh báo (màu vàng), yêu cầu upload file bản mềm để đối soát mật mã thay vì tin tưởng mù quáng vào bản in.