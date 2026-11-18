# QUICK置SIGN CENTRALIZED ERROR CODE REGISTRY

This document is the **Single Source of Truth (SSOT)** for all application and domain error codes across the QuickSign platform (.NET Management Service, Golang Crypto Engine, and Next.js Web Portal).

> **Architectural Rules:**
> 1. All error codes MUST strictly use `SCREAMING_SNAKE_CASE` (per `AGENT.md` & `ADR 0001`).
> 2. Backends (.NET & Go) MUST return this code in the unified error JSON payload: `{"errorCode": "...", "httpStatus": ..., "timestamp": "...", "traceId": "..."}`.
> 3. Frontend Next.js MUST map these codes to localized messages via `src/locales/vi/errors.json` and `src/locales/en/errors.json`.

---

## Error Code Registry Table

| Error Code | HTTP Status | Origin Service | Technical Description | Vietnamese UI Message (`vi`) | English UI Message (`en`) |
| :--- | :---: | :---: | :--- | :--- | :--- |
| **AUTHENTICATION & AUTHORIZATION** | | | | | |
| `UNAUTHORIZED` | `401` | `.NET` / `Go` | Missing, malformed, or invalid JWT or API Key | Vui lòng đăng nhập để tiếp tục. | Please sign in to continue. |
| `FORBIDDEN_ACCESS` | `403` | `.NET` / `Go` | User lacks required role or permission for this resource | Bạn không có quyền truy cập vào tài nguyên này. | You do not have permission to access this resource. |
| `INVALID_CREDENTIALS` | `401` | `.NET` | Username or password incorrect during SSO login | Tài khoản hoặc mật khẩu không chính xác. | Invalid username or password. |
| `SESSION_EXPIRED` | `401` | `.NET` / `Go` | JWT token has expired | Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại. | Your session has expired. Please sign in again. |
| `REFRESH_TOKEN_EXPIRED` | `401` | `.NET` | SSO refresh token has expired or is invalid | Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại. | Refresh token has expired. Please sign in again. |
| `REGISTRATION_FAILED` | `400` | `.NET` | Account registration failed at SSO Identity Provider | Đăng ký tài khoản thất bại. Vui lòng kiểm tra lại thông tin. | Account registration failed. Please verify your details. |
| `API_KEY_INVALID` | `401` | `.NET` / `Go` | API Key provided in `X-API-Key` is invalid or not found | Khóa API Key không hợp lệ. | The provided API Key is invalid. |
| `API_KEY_REVOKED` | `401` | `.NET` / `Go` | API Key has been revoked or deactivated | Khóa API Key này đã bị vô hiệu hóa. | This API Key has been deactivated. |
| `API_KEY_EXPIRED` | `401` | `.NET` / `Go` | API Key has passed its expiration timestamp | Khóa API Key này đã hết hạn sử dụng. | This API Key has expired. |
| `API_KEY_SCOPE_INSUFFICIENT` | `403` | `.NET` / `Go` | API Key does not contain the required scope (`sign` or `verify`) | API Key không có quyền thực hiện thao tác này. | This API Key lacks the required scope for this action. |
| `API_KEY_NOT_FOUND` | `404` | `.NET` | Target API Key ID not found for current user | Không tìm thấy API Key yêu cầu. | The requested API Key was not found. |
| **CERTIFICATES & KEY MANAGEMENT** | | | | | |
| `CERT_NOT_FOUND` | `404` | `.NET` / `Go` | Certificate ID does not exist in the database | Không tìm thấy chứng thư số yêu cầu. | The requested digital certificate was not found. |
| `MAX_ACTIVE_CERTS_REACHED` | `400` | `.NET` | User has reached the maximum of 5 active certificates | Bạn đã đạt giới hạn tối đa 5 chữ ký số đang hoạt động. | You have reached the maximum limit of 5 active certificates. |
| `CERT_ALREADY_REVOKED` | `400` | `.NET` | Certificate is already in `REVOKED` status | Chứng thư số này đã bị thu hồi trước đó. | This certificate has already been revoked. |
| `INVALID_REVOCATION_TIMESTAMP` | `400` | `.NET` | Invalidation timestamp is earlier than certificate creation time | Thời điểm vô hiệu hóa không được sớm hơn thời điểm tạo chứng thư. | Invalidation timestamp cannot be earlier than certificate creation time. |
| `UNAUTHORIZED_CERTIFICATE_ACCESS` | `403` | `.NET` / `Go` | User attempts to access or revoke another user's certificate | Bạn không có quyền thao tác trên chứng thư số này. | You are not authorized to manage this certificate. |
| `CERT_KEYGEN_FAILED` | `500` | `.NET` / `Go` | Cryptographic key pair generation or Vault storage failed | Khởi tạo cặp khóa mật mã thất bại. Vui lòng thử lại. | Failed to generate cryptographic key pair. Please try again. |
| `CERT_REVOKED_AT_SIGNING` | `422` | `Go` | Certificate was revoked prior to the signing timestamp | Chứng thư số đã bị thu hồi trước thời điểm ký. | The certificate was revoked prior to signing. |
| `CERT_EXPIRED_AT_SIGNING` | `422` | `Go` | Certificate had expired prior to the signing timestamp | Chứng thư số đã hết hạn trước thời điểm ký. | The certificate had expired prior to signing. |
| `CERT_INACTIVE` | `422` | `Go` | Certificate is not in `ACTIVE` state | Chứng thư số hiện không ở trạng thái hoạt động. | The certificate is not in an active state. |
| **SIGNING & VERIFICATION** | | | | | |
| `INVALID_PDF_FORMAT` | `400` | `Go` | Uploaded file is empty, corrupted, or not a valid PDF | Tệp tải lên không phải là định dạng PDF hợp lệ. | The uploaded file is not a valid PDF document. |
| `INVALID_JSON_PAYLOAD` | `400` | `Go` | Payload is not valid JSON for JWS signing | Dữ liệu gửi lên không đúng định dạng JSON chuẩn. | The payload is not valid JSON data. |
| `SIGNING_FAILED` | `500` | `Go` | PAdES or JWS cryptographic signing pipeline failed | Quá trình ký số tài liệu thất bại. | Document signing process failed. |
| `TAMPERED_CONTENT` | `422` | `Go` | Document hash mismatch after signing (content altered) | Tài liệu này đã bị chỉnh sửa sau khi ký và không còn nguyên vẹn. | This document has been modified after signing and is no longer intact. |
| `UNKNOWN_SIGNATURE` | `404` | `Go` | Document does not contain a QuickSign digital signature | Tài liệu không chứa chữ ký số của nền tảng QuickSign. | Document does not contain a QuickSign signature. |
| `CODE_EXISTS_UNVERIFIED_CONTENT` | `200` | `Go` | QR Code lookup confirmed code exists; file verification required | Mã tài liệu hợp lệ trên hệ thống. Cần tải file lên để đối soát nguyên vẹn. | Document code exists on system. Upload file to verify content integrity. |
| `DOCUMENT_NOT_FOUND` | `404` | `.NET` / `Go` | Document ID or Document Code not found | Không tìm thấy tài liệu yêu cầu. | The requested document was not found. |
| `DOCUMENT_CODE_EXISTS` | `409` | `Go` | Generated document code collided (rare) | Mã tài liệu đã tồn tại trong hệ thống. | Document code already exists. |
| `INVALID_PAYLOAD` | `400` | `.NET` / `Go` | Request body or query parameters failed schema or model validation rules | Dữ liệu gửi lên không hợp lệ. Vui lòng kiểm tra lại. | The submitted data is invalid. Please verify your input. |
| **SYSTEM & INFRASTRUCTURE** | | | | | |
| `RATE_LIMIT_EXCEEDED` | `429` | `.NET` / `Go` | Request rate limit exceeded | Bạn đang gửi yêu cầu quá nhanh. Vui lòng chờ giây lát. | Too many requests. Please wait a moment and try again. |
| `INTERNAL_SERVER_ERROR` | `500` | `.NET` / `Go` | Unexpected unhandled server exception | Hệ thống gặp lỗi nội bộ. Vui lòng thử lại sau. | An internal server error occurred. Please try again later. |
| `SERVICE_UNAVAILABLE` | `503` | `.NET` / `Go` | Downstream service or engine temporarily unavailable | Dịch vụ tạm thời không khả dụng. Vui lòng thử lại sau. | Service temporarily unavailable. Please try again later. |
| `VAULT_COMMUNICATION_ERROR` | `502` | `Go` | Communication failure with HashiCorp Vault | Lỗi kết nối tới hệ thống lưu trữ khóa bảo mật. | Error communicating with key management vault. |
| `REDIS_CACHE_ERROR` | `502` | `.NET` / `Go` | Redis cache connection or serialization error | Lỗi kết nối bộ đệm dữ liệu. | Cache service connection error. |
| `DATABASE_ERROR` | `500` | `.NET` / `Go` | PostgreSQL execution or constraint failure | Lỗi truy vấn cơ sở dữ liệu. | Database operation failed. |
