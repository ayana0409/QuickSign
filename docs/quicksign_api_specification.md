# TÀI LIỆU ĐẶC TẢ CHI TIẾT TOÀN BỘ API HỆ THỐNG QUICKSIGN
## (QUICKSIGN PLATFORM - API SPECIFICATION & REFERENCE MANUAL)

---

## 1. Thông Tin Chung & Quy Chuẩn Kiến Trúc

Tài liệu này là bản đặc tả kỹ thuật chi tiết toàn bộ các giao diện lập trình ứng dụng (API Endpoints) của nền tảng **QuickSign Digital Signature Platform**. Tài liệu phục vụ cho việc tích hợp của Frontend Web Portal (Next.js), các đối tác tích hợp bên thứ ba (thông qua API Key) và chuẩn hóa giao tiếp liên dịch vụ giữa **.NET Management Service** và **Go Crypto Engine**.

### 1.1. Môi Trường & Base URLs
* **Dịch vụ Quản Trị (.NET Management Service):**
  - Môi trường phát triển cục bộ: `http://localhost:5000` (hoặc cổng cấu hình trong `launchSettings.json`).
  - Đường dẫn API gốc: `/api/v1`
* **Dịch vụ Ký & Xác Thực (Go Crypto Engine):**
  - Môi trường phát triển cục bộ: `http://localhost:5001`
  - Đường dẫn API gốc: `/api/v1`
* **Dịch vụ Định Danh SSO (SSO Identity Provider):**
  - Địa chỉ máy chủ SSO được cấu hình linh hoạt qua biến môi trường hoặc file cấu hình `{SSO_AUTHORITY_URL}` (không hardcode URL cụ thể).

---

### 1.2. Cơ Chế Xác Thực & Phân Quyền (Authentication & Headers)

Toàn bộ hệ thống QuickSign áp dụng 2 cơ chế xác thực tiêu chuẩn:

1. **Xác thực Người Dùng Giao Diện (JWT Bearer Token):**
   - Sử dụng cho người dùng đăng nhập trên Web Portal.
   - Header bắt buộc: `Authorization: Bearer <access_token>`
   - Phân quyền (Roles):
     - `User`: Người dùng thông thường (quản lý chứng thư số, tạo API key, xem tài liệu của chính mình).
     - `Admin` (hoặc `admin`, `sso-admin`): Quản trị viên hệ thống (xem thống kê toàn sàn, thu hồi khẩn cấp chứng thư số của bất kỳ ai).
2. **Xác thực Tích Hợp Hệ Thống Bên Thứ Ba (API Key):**
   - Sử dụng cho các ứng dụng backend (như hệ thống nghiệp vụ, phần mềm hóa đơn số, kế toán, ERP).
   - Header bắt buộc: `X-API-Key: qs_live_<hex_32_bytes>`
   - Phạm vi truy cập (Scopes): `sign` (quyền ký số), `verify` (quyền xác thực tài liệu), `read` (đọc metadata).

---

### 1.3. Định Dạng Phản Hồi Lỗi Thống Nhất (Unified Error JSON Response)

Khi xảy ra lỗi (HTTP Status $4\text{xx}$ hoặc $5\text{xx}$), tất cả các endpoint đều trả về cấu trúc JSON đồng nhất:

```json
{
  "errorCode": "MAX_ACTIVE_CERTS_REACHED",
  "httpStatus": 400,
  "timestamp": "2026-11-18T12:00:00.0000000Z",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "details": {
    "limit": 5,
    "activeCount": 5
  }
}
```

* **`errorCode`:** Chuỗi định danh lỗi duy nhất viết theo quy tắc `SCREAMING_SNAKE_CASE` (tra cứu tại `docs/error_codes.md`).
* **`httpStatus`:** Mã trạng thái HTTP tương ứng.
* **`timestamp`:** Thời điểm phát sinh lỗi theo chuẩn ISO 8601 UTC.
* **`traceId`:** Mã vết phân tán (W3C Trace Context) phục vụ điều tra lỗi.
* **`details`:** *(Tùy chọn)* Đối tượng chứa các tham số nội suy động hoặc chi tiết vi phạm validation.

---

## Bảng Mục Lục API

| STT | Nhóm Chức Năng | Endpoint | Method | Trạng Thái | Mô Tả Tóm Tắt |
| :---: | :--- | :--- | :---: | :---: | :--- |
| **I** | **Xác Thực (Auth BFF)** | `/api/v1/auth/login` | `POST` | Đã có (.NET) | Đăng nhập lấy JWT Bearer & Refresh token |
| | | `/api/v1/auth/register` | `POST` | Đã có (.NET) | Đăng ký tài khoản người dùng mới |
| | | `/api/v1/auth/refresh` | `POST` | Đã có (.NET) | Cấp mới Access token bằng Refresh token |
| **II** | **Chứng Thư Số** | `/api/v1/certificates` | `GET` | Đã có (.NET) | Danh sách chứng thư số của tôi |
| | | `/api/v1/certificates/{id}` | `GET` | Đã có (.NET) | Chi tiết một chứng thư số theo GUID ID |
| | | `/api/v1/certificates` | `POST` | Đã có (.NET) | Cấp mới chứng thư số (RSA/ECDSA) |
| | | `/api/v1/certificates/{id}/revoke` | `PUT` | Đã có (.NET) | Thu hồi chứng thư số |
| **III** | **API Keys** | `/api/v1/api-keys` | `GET` | Đã có (.NET) | Danh sách API Key của tôi |
| | | `/api/v1/api-keys` | `POST` | Đã có (.NET) | Tạo mới API Key tích hợp |
| | | `/api/v1/api-keys/{id}` | `DELETE` | Đã có (.NET) | Vô hiệu hóa / Xóa API Key |
| **IV** | **Hồ Sơ Tài Liệu** | `/api/v1/documents` | `GET` | Đã có (.NET) | Danh sách tài liệu đã ký của tôi (phân trang) |
| | | `/api/v1/documents/{id}` | `GET` | Đã có (.NET) | Xem thông tin tài liệu đã ký theo ID |
| | | `/api/v1/documents/code/{documentCode}` | `GET` | Đã có (.NET) | Tra cứu công khai tài liệu qua mã QR |
| | | `/api/v1/documents/{id}/verifications` | `GET` | Đã có (.NET) | Xem lịch sử các lượt xác thực tài liệu |
| **V** | **Thống Kê** | `/api/v1/stats/overview` | `GET` | Đã có (.NET) | Thống kê tổng quan Dashboard người dùng |
| **VI** | **Quản Trị Admin** | `/api/v1/admin/stats/overview` | `GET` | Đã có (.NET) | Thống kê số liệu toàn bộ nền tảng |
| | | `/api/v1/admin/certificates/{id}/revoke` | `PUT` | Đã có (.NET) | Quản trị viên thu hồi khẩn cấp chứng thư |
| **VII** | **Ký & Xác Thực (Go)** | `/api/v1/sign/pdf` | `POST` | Đặc tả (Go) | Ký file PDF nhúng mã tra cứu chuẩn PAdES |
| | | `/api/v1/sign/json` | `POST` | Đặc tả (Go) | Ký dữ liệu JSON chuẩn JWS |
| | | `/api/v1/verify/file` | `POST` | Đặc tả (Go) | Xác thực tính toàn vẹn file PDF hoặc JWS |
| | | `/api/v1/verify/code/{documentCode}` | `GET` | Đặc tả (Go) | Tra cứu mã và cảnh báo Security Illusion |

---

## 2. Chi Tiết Nhóm I: Xác Thực & Danh Tính Người Dùng (Auth BFF)

Các endpoint này đóng vai trò Backend-For-Frontend (BFF), nhận request từ Web Portal, kiểm tra schema và chuyển tiếp ủy quyền sang máy chủ **Hệ Thống Định Danh SSO (SSO Identity Provider)**.

### 2.1. Đăng Nhập Hệ Thống (Login)
* **URL:** `/api/v1/auth/login`
* **Method:** `POST`
* **Yêu Cầu Xác Thực:** Không (`[AllowAnonymous]`)
* **Content-Type:** `application/json`

#### Request Body
| Tên Trường | Kiểu Dữ Liệu | Bắt Buộc? | Ràng Buộc & Validation | Mô Tả |
| :--- | :--- | :---: | :--- | :--- |
| `usernameOrEmail` | `string` | **Required** | Không được để trống | Tên tài khoản hoặc địa chỉ email đăng nhập |
| `password` | `string` | **Required** | Không được để trống | Mật khẩu tài khoản người dùng |

*Ví dụ Request Body:*
```json
{
  "usernameOrEmail": "nguyenvana",
  "password": "Password@123"
}
```

#### Response Thành Công (HTTP 200 OK)
| Tên Trường | Kiểu Dữ Liệu | Mô Tả |
| :--- | :--- | :--- |
| `access_token` | `string` | Chuỗi JWT Access Token dùng cho các request tiếp theo |
| `token_type` | `string` | Loại token (luôn là `"Bearer"`) |
| `expires_in` | `integer` | Thời gian sống của token tính bằng giây (ví dụ: `86400`) |
| `refresh_token` | `string` | Token dùng để làm mới access token khi hết hạn |
| `id_token` | `string?` | Identity Token OpenID Connect (nếu có) |
| `scope` | `string?` | Danh sách phạm vi quyền hạn được cấp |

*Ví dụ Response:*
```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIs...",
  "token_type": "Bearer",
  "expires_in": 86400,
  "refresh_token": "def50200...",
  "id_token": null,
  "scope": "openid profile email"
}
```

#### Mã Lỗi Thường Gặp
* `INVALID_CREDENTIALS` (401): Tên đăng nhập hoặc mật khẩu không chính xác.
* `INVALID_PAYLOAD` (400): Dữ liệu gửi lên thiếu trường bắt buộc hoặc sai định dạng.
* `SERVICE_UNAVAILABLE` (503): Máy chủ SSO Identity Provider tạm thời không phản hồi.

---

### 2.2. Đăng Ký Tài Khoản Mới (Register)
* **URL:** `/api/v1/auth/register`
* **Method:** `POST`
* **Yêu Cầu Xác Thực:** Không (`[AllowAnonymous]`)
* **Content-Type:** `application/json`

#### Request Body
| Tên Trường | Kiểu Dữ Liệu | Bắt Buộc? | Ràng Buộc & Validation | Mô Tả |
| :--- | :--- | :---: | :--- | :--- |
| `userName` | `string` | **Required** | Viết liền không dấu, không để trống | Tên định danh tài khoản người dùng |
| `emailAddress` | `string` | **Required** | Định dạng email hợp lệ | Địa chỉ hòm thư điện tử |
| `password` | `string` | **Required** | Tối thiểu 6 ký tự, gồm chữ hoa, số và ký tự đặc biệt | Mật khẩu bảo vệ tài khoản |
| `appName` | `string?` | *Optional* | Mặc định: `null` | Tên ứng dụng khởi tạo yêu cầu đăng ký |

*Ví dụ Request Body:*
```json
{
  "userName": "tranvanb",
  "emailAddress": "tranvanb@example.com",
  "password": "SecurePassword@2026",
  "appName": "QuickSign"
}
```

#### Response Thành Công (HTTP 200 OK)
Phản hồi body rỗng (Status 200 biểu thị đăng ký thành công).

#### Mã Lỗi Thường Gặp
* `REGISTRATION_FAILED` (400): Tên đăng nhập hoặc email đã tồn tại trên hệ thống SSO.
* `INVALID_PAYLOAD` (400): Mật khẩu quá yếu hoặc email sai cú pháp.
* `SERVICE_UNAVAILABLE` (503): Máy chủ SSO Identity Provider gặp sự cố.

---

### 2.3. Cấp Mới Access Token (Refresh Token)
* **URL:** `/api/v1/auth/refresh`
* **Method:** `POST`
* **Yêu Cầu Xác Thực:** Không (`[AllowAnonymous]`)
* **Content-Type:** `application/json`

#### Request Body
Request body là một chuỗi JSON string chứa `refreshToken`:
```json
"def502008f51a2e9b0..."
```

#### Response Thành Công (HTTP 200 OK)
Cấu trúc tương tự như Response của `POST /api/v1/auth/login`.

#### Mã Lỗi Thường Gặp
* `REFRESH_TOKEN_EXPIRED` (401): Refresh token đã hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại.
* `SERVICE_UNAVAILABLE` (503): Máy chủ định danh tạm thời không khả dụng.

---

## 3. Chi Tiết Nhóm II: Quản Lý Chứng Thư Số & Khóa Mật Mã (Certificates)

Dịch vụ này quản lý vòng đời chứng thư số cá nhân. Mỗi người dùng được cấp tối đa **5 chứng thư số đang hoạt động (`Active`)**. Khi tạo chứng thư, .NET gọi sang Go Engine để sinh khóa mật mã và lưu Private Key vào HashiCorp Vault.

### 3.1. Lấy Danh Sách Chứng Thư Số Của Tôi
* **URL:** `/api/v1/certificates`
* **Method:** `GET`
* **Xác thực:** Bắt buộc (`Authorization: Bearer <JWT>`)

#### Request Parameters
Không có parameters.

#### Response Thành Công (HTTP 200 OK)
Mảng danh sách các chứng thư số (`Array of CertificateResponseDto`).

*Ví dụ Response:*
```json
[
  {
    "id": "e4b5a3f2-1234-4a6b-9c8d-112233445566",
    "userId": "usr_01HXYZ987654321",
    "subjectName": "CN=Nguyen Van A, O=Doanh Nghiep XYZ, C=VN",
    "serialNumber": "SN-2026-98765432",
    "algorithm": 1,
    "publicKeyPem": "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8A...\n-----END PUBLIC KEY-----",
    "vaultKeyPath": "secret/data/users/usr_01HXYZ987654321/keys/cert-1",
    "status": 1,
    "revokedAt": null,
    "invalidatedFrom": null,
    "revokedReason": null,
    "expiresAt": "2027-11-18T12:00:00Z",
    "createdAt": "2026-11-18T12:00:00Z"
  }
]
```

---

### 3.2. Xem Chi Tiết Chứng Thư Số
* **URL:** `/api/v1/certificates/{id}`
* **Method:** `GET`
* **Xác thực:** Bắt buộc (`Authorization: Bearer <JWT>`)

#### Path Parameters
| Tên Tham Số | Kiểu Dữ Liệu | Bắt Buộc? | Mô Tả |
| :--- | :--- | :---: | :--- |
| `id` | `string` (UUID) | **Required** | GUID định danh của chứng thư số cần xem |

#### Response Thành Công (HTTP 200 OK)
Đối tượng `CertificateResponseDto` (chi tiết như bảng trường tại mục 3.1).

#### Mã Lỗi Thường Gặp
* `CERT_NOT_FOUND` (404): Không tìm thấy chứng thư với ID tương ứng.
* `UNAUTHORIZED_CERTIFICATE_ACCESS` (403): Chứng thư thuộc sở hữu của người dùng khác (không có quyền truy cập).

---

### 3.3. Khởi Tạo Chứng Thư Số Mới (Issue Certificate)
* **URL:** `/api/v1/certificates`
* **Method:** `POST`
* **Xác thực:** Bắt buộc (`Authorization: Bearer <JWT>`)
* **Content-Type:** `application/json`

#### Request Body
| Tên Trường | Kiểu Dữ Liệu | Bắt Buộc? | Ràng Buộc & Validation | Mô Tả |
| :--- | :--- | :---: | :--- | :--- |
| `subjectName` | `string` | **Required** | Tối đa 255 ký tự, không rỗng | Tên chủ thể chứng thư (VD: `CN=Tên Doanh Nghiệp, C=VN`) |
| `algorithm` | `integer` (Enum) | **Required** | `1` = `Rsa2048`, `2` = `EcdsaP256`. Mặc định: `1` | Thuật toán mật mã sử dụng để sinh cặp khóa |
| `validityDays` | `integer` | **Required** | Giá trị từ `1` đến `3650` ngày. Mặc định: `365` | Thời hạn hiệu lực của chứng thư (tối đa 10 năm) |

*Ví dụ Request Body:*
```json
{
  "subjectName": "CN=Cong Ty Co Phan ABC, O=ABC Corp, C=VN",
  "algorithm": 1,
  "validityDays": 365
}
```

#### Response Thành Công (HTTP 200 OK)
Đối tượng `CertificateResponseDto` mới được khởi tạo và lưu vào PostgreSQL.

#### Mã Lỗi Thường Gặp
* `MAX_ACTIVE_CERTS_REACHED` (400): Người dùng đã sở hữu 5 chứng thư đang `Active`.
* `INVALID_PAYLOAD` (400): `subjectName` để trống hoặc `validityDays` nằm ngoài khoảng 1 – 3650.
* `CERT_KEYGEN_FAILED` (500): Go Crypto Engine hoặc HashiCorp Vault gặp lỗi khi sinh khóa.
* `DATABASE_ERROR` (500): Lỗi ghi cơ sở dữ liệu (tự động kích hoạt Giao dịch bù trừ xóa khóa mồ côi trên Vault).

---

### 3.4. Thu Hồi Chứng Thư Số (Revoke Certificate)
* **URL:** `/api/v1/certificates/{id}/revoke`
* **Method:** `PUT`
* **Xác thực:** Bắt buộc (`Authorization: Bearer <JWT>`)
* **Content-Type:** `application/json`

#### Path Parameters
| Tên Tham Số | Kiểu Dữ Liệu | Bắt Buộc? | Mô Tả |
| :--- | :--- | :---: | :--- |
| `id` | `string` (UUID) | **Required** | GUID của chứng thư số cần thu hồi |

#### Request Body
| Tên Trường | Kiểu Dữ Liệu | Bắt Buộc? | Ràng Buộc & Validation | Mô Tả |
| :--- | :--- | :---: | :--- | :--- |
| `invalidatedFrom` | `string?` (ISO8601) | *Optional* | Không được sớm hơn `createdAt`. Mặc định: thời điểm gọi API | Mốc thời gian bắt đầu mất hiệu lực của chữ ký số |
| `reason` | `string?` | *Optional* | Tối đa 255 ký tự | Lý do thu hồi (VD: Lộ khóa riêng, thay đổi thông tin) |

*Ví dụ Request Body:*
```json
{
  "invalidatedFrom": "2026-11-18T10:00:00Z",
  "reason": "Khoa rieng bi nghi ngo ro ri"
}
```

#### Response Thành Công (HTTP 204 No Content)
Phản hồi không có nội dung body (Status 204). Chứng thư chuyển trạng thái `Revoked` (`status: 3`) và cache trên Redis bị xóa ngay lập tức.

#### Mã Lỗi Thường Gặp
* `CERT_NOT_FOUND` (404): Không tìm thấy chứng thư với ID chỉ định.
* `CERT_ALREADY_REVOKED` (400): Chứng thư này đã bị thu hồi trước đó.
* `INVALID_REVOCATION_TIMESTAMP` (400): Mốc thời gian vô hiệu hóa sớm hơn thời điểm tạo chứng thư.
* `UNAUTHORIZED_CERTIFICATE_ACCESS` (403): Người dùng không sở hữu chứng thư này và không phải là Admin.

---

## 4. Chi Tiết Nhóm III: Quản Lý Khóa Tích Hợp API Key (ApiKeys)

Cung cấp API cho phép lập trình viên tạo, liệt kê và thu hồi các khóa `X-API-Key` tích hợp tự động vào các hệ thống khác.

### 4.1. Lấy Danh Sách API Key Của Tôi
* **URL:** `/api/v1/api-keys`
* **Method:** `GET`
* **Xác thực:** Bắt buộc (`Authorization: Bearer <JWT>`)

#### Response Thành Công (HTTP 200 OK)
Mảng danh sách các API Key (`Array of ApiKeyItemDto`). Khóa bí mật đã được băm SHA-256 bảo vệ, client chỉ nhìn thấy tiền tố hiển thị `keyPrefix`.

*Ví dụ Response:*
```json
[
  {
    "id": "7a8b9c0d-1122-3344-5566-778899aabbcc",
    "name": "Backend Hoa Don Dien Tu",
    "keyPrefix": "qs_live_12345678",
    "scopes": "sign,verify",
    "isActive": true,
    "lastUsedAt": "2026-11-18T14:30:00Z",
    "expiresAt": "2027-11-18T00:00:00Z",
    "createdAt": "2026-11-18T12:00:00Z"
  }
]
```

---

### 4.2. Khởi Tạo API Key Mới (Create API Key)
* **URL:** `/api/v1/api-keys`
* **Method:** `POST`
* **Xác thực:** Bắt buộc (`Authorization: Bearer <JWT>`)
* **Content-Type:** `application/json`

#### Request Body
| Tên Trường | Kiểu Dữ Liệu | Bắt Buộc? | Ràng Buộc & Validation | Mô Tả |
| :--- | :--- | :---: | :--- | :--- |
| `name` | `string` | **Required** | Tối đa 100 ký tự, không rỗng | Tên gợi nhớ của ứng dụng hoặc mục đích sử dụng |
| `scopes` | `string` | **Required** | Tối đa 100 ký tự. Mặc định: `"sign,verify"` | Danh sách các quyền phân tách bằng dấu phẩy |
| `expiresAt` | `string?` (ISO8601) | *Optional* | Thời điểm tương lai hoặc `null` | Ngày hết hạn của Key (để trống nếu không giới hạn) |

*Ví dụ Request Body:*
```json
{
  "name": "Enterprise Invoicing Service",
  "scopes": "sign,verify",
  "expiresAt": "2027-11-18T00:00:00Z"
}
```

#### Response Thành Công (HTTP 200 OK)
> [!WARNING]
> Trường `plaintextApiKey` chỉ được trả về **DUY NHẤT MỘT LẦN** tại thời điểm khởi tạo này. Hệ thống không lưu trữ plaintext key trong cơ sở dữ liệu. Người dùng phải lưu trữ ngay vào nơi an toàn.

| Tên Trường | Kiểu Dữ Liệu | Mô Tả |
| :--- | :--- | :--- |
| `id` | `string` (UUID) | Định danh khóa API |
| `name` | `string` | Tên gợi nhớ của khóa |
| `keyPrefix` | `string` | Tiền tố hiển thị (8 ký tự đầu: `qs_live_...`) |
| `plaintextApiKey` | `string` | **Chuỗi API Key hoàn chỉnh (72 ký tự)** |
| `scopes` | `string` | Danh sách quyền hạn |
| `expiresAt` | `string?` | Thời điểm hết hạn |
| `createdAt` | `string` | Thời điểm tạo khóa |

*Ví dụ Response:*
```json
{
  "id": "7a8b9c0d-1122-3344-5566-778899aabbcc",
  "name": "Enterprise Invoicing Service",
  "keyPrefix": "qs_live_12345678",
  "plaintextApiKey": "qs_live_12345678abcdef0123456789abcdef0123456789abcdef0123456789abcdef01",
  "scopes": "sign,verify",
  "expiresAt": "2027-11-18T00:00:00Z",
  "createdAt": "2026-11-18T12:00:00Z"
}
```

---

### 4.3. Thu Hồi / Xóa Vĩnh Viễn API Key
* **URL:** `/api/v1/api-keys/{id}`
* **Method:** `DELETE`
* **Xác thực:** Bắt buộc (`Authorization: Bearer <JWT>`)

#### Path Parameters
| Tên Tham Số | Kiểu Dữ Liệu | Bắt Buộc? | Mô Tả |
| :--- | :--- | :---: | :--- |
| `id` | `string` (UUID) | **Required** | GUID định danh của API Key cần xóa/thu hồi |

#### Response Thành Công (HTTP 204 No Content)
Phản hồi không có nội dung body (Status 204). Key bị vô hiệu hóa ngay trong PostgreSQL và bản ghi cache `qs_key:{hash}` trên Redis bị xóa tức thì.

#### Mã Lỗi Thường Gặp
* `API_KEY_NOT_FOUND` (404): Không tìm thấy API Key chỉ định.
* `FORBIDDEN_ACCESS` (403): Bạn không có quyền thu hồi API Key của người khác.

---

## 5. Chi Tiết Nhóm IV: Quản Lý Hồ Sơ & Lịch Sử Tài Liệu Đã Ký (Documents)

Quản lý hồ sơ tài liệu (hóa đơn PDF, chứng chỉ JSON) đã ký trên hệ thống, phục vụ tra cứu công khai và theo dõi lịch sử đối soát tính toàn vẹn.

### 5.1. Danh Sách Tài Liệu Đã Ký Của Tôi
* **URL:** `/api/v1/documents`
* **Method:** `GET`
* **Xác thực:** Bắt buộc (`Authorization: Bearer <JWT>`)

#### Query Parameters
| Tên Tham Số | Kiểu Dữ Liệu | Bắt Buộc? | Mặc Định | Mô Tả |
| :--- | :--- | :---: | :---: | :--- |
| `skipCount` | `integer` | *Optional* | `0` | Số lượng bản ghi bỏ qua (phục vụ phân trang) |
| `maxResultCount` | `integer` | *Optional* | `20` | Số lượng bản ghi tối đa trên một trang (tối đa 100) |

#### Response Thành Công (HTTP 200 OK)
Mảng danh sách các tài liệu đã ký (`Array of SignedDocumentDto`).

*Ví dụ Response:*
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "documentCode": "DOC-2026-A1B2C3D4",
    "userId": "usr_01HXYZ987654321",
    "certificateId": "e4b5a3f2-1234-4a6b-9c8d-112233445566",
    "apiKeyId": null,
    "docType": 1,
    "documentName": "HoaDonDienTu_INV_2026_001.pdf",
    "originalHash": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
    "signedHash": "a1b2c3d4e5f60718293a4b5c6d7e8f90123456789abcdef0123456789abcdef0",
    "storagePath": "documents/2026/11/HoaDon_Signed.pdf",
    "totalVerifyCount": 15,
    "uniqueVerifyCount": 12,
    "tamperedAlertCount": 0,
    "lastVerifiedAt": "2026-11-18T15:20:00Z",
    "createdAt": "2026-11-18T10:00:00Z"
  }
]
```

---

### 5.2. Xem Thông Tin Chi Tiết Tài Liệu Theo ID
* **URL:** `/api/v1/documents/{id}`
* **Method:** `GET`
* **Xác thực:** Bắt buộc (`Authorization: Bearer <JWT>`)

#### Path Parameters
| Tên Tham Số | Kiểu Dữ Liệu | Bắt Buộc? | Mô Tả |
| :--- | :--- | :---: | :--- |
| `id` | `string` (UUID) | **Required** | GUID định danh của tài liệu đã ký |

#### Response Thành Công (HTTP 200 OK)
Đối tượng `SignedDocumentDto` (cấu trúc trường chi tiết như bảng tại mục 5.1).

#### Mã Lỗi Thường Gặp
* `DOCUMENT_NOT_FOUND` (404): Không tìm thấy tài liệu với ID yêu cầu.
* `FORBIDDEN_ACCESS` (403): Bạn không có quyền truy cập hồ sơ tài liệu của người khác.

---

### 5.3. Tra Cứu Công Khai Tài Liệu Qua Mã QR (Document Code Lookup)
* **URL:** `/api/v1/documents/code/{documentCode}`
* **Method:** `GET`
* **Xác thực:** Không (`[AllowAnonymous]`) - Cho phép người ngoài quét QR Code tra cứu công khai.

#### Path Parameters
| Tên Tham Số | Kiểu Dữ Liệu | Bắt Buộc? | Mô Tả |
| :--- | :--- | :---: | :--- |
| `documentCode` | `string` | **Required** | Mã định danh tài liệu in trên QR Code (định dạng `DOC-YYYY-XXXXXXXX`) |

#### Response Thành Công (HTTP 200 OK)
Đối tượng `SignedDocumentDto` chứa thông tin tóm lược của tài liệu.

#### Mã Lỗi Thường Gặp
* `DOCUMENT_NOT_FOUND` (404): Mã tài liệu không tồn tại trên hệ thống.

---

### 5.4. Lấy Lịch Sử Các Lượt Xác Thực Của Tài Liệu
* **URL:** `/api/v1/documents/{id}/verifications`
* **Method:** `GET`
* **Xác thực:** Bắt buộc (`Authorization: Bearer <JWT>`)

#### Path Parameters
| Tên Tham Số | Kiểu Dữ Liệu | Bắt Buộc? | Mô Tả |
| :--- | :--- | :---: | :--- |
| `id` | `string` (UUID) | **Required** | GUID của tài liệu đã ký |

#### Query Parameters
| Tên Tham Số | Kiểu Dữ Liệu | Bắt Buộc? | Mặc Định | Mô Tả |
| :--- | :--- | :---: | :---: | :--- |
| `skipCount` | `integer` | *Optional* | `0` | Vị trí bắt đầu lấy bản ghi log |
| `maxResultCount` | `integer` | *Optional* | `50` | Số lượng log tối đa trên một lần lấy (tối đa 100) |

#### Response Thành Công (HTTP 200 OK)
Mảng danh sách các bản ghi đối soát (`Array of VerificationLogDto`).

*Ví dụ Response:*
```json
[
  {
    "id": 1001,
    "signedDocumentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "submittedHash": "a1b2c3d4e5f60718293a4b5c6d7e8f90123456789abcdef0123456789abcdef0",
    "verificationMethod": 1,
    "apiKeyId": null,
    "verifierIp": "113.190.234.12",
    "userAgent": "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X)...",
    "isValid": true,
    "validationStatus": 1,
    "verifiedAt": "2026-11-18T15:20:00Z"
  }
]
```

* **`verificationMethod`:** `1` (`WebUpload`), `2` (`WebCode`), `3` (`Api`).
* **`validationStatus`:** `1` (`Valid`), `2` (`TamperedContent`), `3` (`CertRevoked`), `4` (`CertExpiredAtSigning`), `5` (`UnknownSignature`), `6` (`CodeExistsUnverifiedContent`).

---

## 6. Chi Tiết Nhóm V: Báo Cáo & Thống Kê Dashboard (Statistics)

Cung cấp số liệu tổng quan về các hoạt động ký số, xác thực và cảnh báo rủi ro cho người dùng hiện tại.

### 6.1. Lấy Thống Kê Tổng Quan Dashboard
* **URL:** `/api/v1/stats/overview`
* **Method:** `GET`
* **Xác thực:** Bắt buộc (`Authorization: Bearer <JWT>`)

#### Response Thành Công (HTTP 200 OK)
Đối tượng `DashboardOverviewStatsDto`.

| Tên Trường | Kiểu Dữ Liệu | Mô Tả |
| :--- | :--- | :--- |
| `totalSignedDocuments` | `integer` | Tổng số lượng tài liệu đã ký thành công của tôi |
| `totalVerifications` | `integer` | Tổng số lượt tài liệu của tôi được người khác hoặc hệ thống kiểm tra |
| `uniqueVerifications` | `integer` | Số lượt kiểm tra từ các địa chỉ IP duy nhất (chống đếm lặp) |
| `tamperedAlerts` | `integer` | **Số lần phát hiện tài liệu bị chỉnh sửa / giả mạo sau khi ký** |
| `activeCertificates` | `integer` | Số chứng thư số đang hoạt động (`Active`, tối đa 5) |
| `activeApiKeys` | `integer` | Số lượng API Key đang có hiệu lực hoạt động |

*Ví dụ Response:*
```json
{
  "totalSignedDocuments": 48,
  "totalVerifications": 320,
  "uniqueVerifications": 185,
  "tamperedAlerts": 1,
  "activeCertificates": 2,
  "activeApiKeys": 3
}
```

---

## 7. Chi Tiết Nhóm VI: Quản Trị Hệ Thống Dành Riêng Cho Quản Trị Viên (Admin)

Toàn bộ các endpoint trong nhóm này bắt buộc người dùng phải có Role `Admin` (hoặc `admin`, `sso-admin`) trong JWT claims.

### 7.1. Xem Thống Kê Số Liệu Toàn Nền Tảng
* **URL:** `/api/v1/admin/stats/overview`
* **Method:** `GET`
* **Xác thực:** Bắt buộc (`Authorization: Bearer <JWT>`, Role: `Admin`)

#### Response Thành Công (HTTP 200 OK)
Đối tượng `DashboardOverviewStatsDto` tổng hợp số liệu trên toàn bộ cơ sở dữ liệu của tất cả người dùng trong hệ thống.

#### Mã Lỗi Thường Gặp
* `FORBIDDEN_ACCESS` (403): Người dùng không có quyền quản trị viên.

---

### 7.2. Thu Hồi Khẩn Cấp Bất Kỳ Chứng Thư Nào (Admin Revoke)
* **URL:** `/api/v1/admin/certificates/{id}/revoke`
* **Method:** `PUT`
* **Xác thực:** Bắt buộc (`Authorization: Bearer <JWT>`, Role: `Admin`)
* **Content-Type:** `application/json`

#### Path Parameters
| Tên Tham Số | Kiểu Dữ Liệu | Bắt Buộc? | Mô Tả |
| :--- | :--- | :---: | :--- |
| `id` | `string` (UUID) | **Required** | GUID của chứng thư số cần thu hồi khẩn cấp |

#### Request Body
| Tên Trường | Kiểu Dữ Liệu | Bắt Buộc? | Ràng Buộc & Validation | Mô Tả |
| :--- | :--- | :---: | :--- | :--- |
| `invalidatedFrom` | `string?` (ISO8601) | *Optional* | Mặc định: thời điểm gọi API | Mốc thời gian vô hiệu hóa |
| `reason` | `string?` | *Optional* | Tối đa 255 ký tự | Lý do quản trị viên thu hồi khẩn cấp |

*Ví dụ Request Body:*
```json
{
  "invalidatedFrom": "2026-11-18T16:00:00Z",
  "reason": "Admin emergency revocation: User security compromise detected"
}
```

#### Response Thành Công (HTTP 204 No Content)
Phản hồi không có nội dung body (Status 204).

---

## 8. Chi Tiết Nhóm VII: Đặc Tả API Ký & Xác Thực Trực Tiếp (Go Crypto Engine Specification)

> [!NOTE]
> Đây là đặc tả kỹ thuật chuẩn bị cho **Giai đoạn 2 (Go Crypto Engine)**. Traefik Reverse Proxy sẽ phân tuyến trực tiếp các request này về Go Engine (không đi vòng qua .NET để tối ưu RAM khi xử lý stream file lớn). Hỗ trợ cả **Bearer JWT** (Web) lẫn **X-API-Key** (Tích hợp).

### 8.1. Ký Số Tài Liệu PDF (PAdES Digital Signature)
* **URL:** `/api/v1/sign/pdf`
* **Method:** `POST`
* **Xác thực:** `Authorization: Bearer <JWT>` HOẶC `X-API-Key: qs_live_...` (Yêu cầu scope `sign`)
* **Content-Type:** `multipart/form-data`

#### Request Form Data
| Tên Trường | Kiểu Dữ Liệu | Bắt Buộc? | Ràng Buộc & Validation | Mô Tả |
| :--- | :--- | :---: | :--- | :--- |
| `file` | `binary` (File) | **Required** | Định dạng PDF chuẩn (%PDF-1.x), dung lượng $\le 20\text{MB}$ | File tài liệu PDF gốc cần ký số |
| `certificateId` | `string` (UUID) | *Optional* | Nếu để trống, Go Engine tự chọn chứng thư `Active` mới nhất của user | ID chứng thư số dùng để ký |
| `reason` | `string` | *Optional* | Tối đa 100 ký tự (Mặc định: `"Digital Signature"`) | Lý do ký số nhúng vào thuộc tính PAdES |
| `location` | `string` | *Optional* | Tối đa 100 ký tự (Mặc định: `"Vietnam"`) | Địa điểm thực hiện ký số |

#### Response Thành Công (HTTP 200 OK)
* **Content-Type:** `application/pdf`
* **Headers:**
  - `Content-Disposition: attachment; filename="<original_name>_signed.pdf"`
  - `X-Document-Code: DOC-YYYY-XXXXXXXX` (Mã định danh duy nhất vừa được sinh)
* **Response Body:** Dữ liệu nhị phân (Binary Stream) của file PDF đã nhúng chữ ký PAdES-BES và mã QR.

#### Mã Lỗi Thường Gặp
* `INVALID_PDF_FORMAT` (400): File rỗng, hỏng hoặc không đúng chuẩn cấu trúc PDF.
* `CERT_NOT_FOUND` (404): Không tìm thấy chứng thư hợp lệ để ký.
* `CERT_REVOKED_AT_SIGNING` (422): Chứng thư đã bị thu hồi trước thời điểm ký.
* `API_KEY_SCOPE_INSUFFICIENT` (403): Khóa API Key thiếu quyền `sign`.

---

### 8.2. Ký Số Dữ Liệu JSON (JSON Web Signature - JWS)
* **URL:** `/api/v1/sign/json`
* **Method:** `POST`
* **Xác thực:** `Authorization: Bearer <JWT>` HOẶC `X-API-Key: qs_live_...` (Yêu cầu scope `sign`)
* **Content-Type:** `application/json`

#### Request Body
| Tên Trường | Kiểu Dữ Liệu | Bắt Buộc? | Ràng Buộc & Validation | Mô Tả |
| :--- | :--- | :---: | :--- | :--- |
| `payload` | `object` | **Required** | Đối tượng JSON hợp lệ | Nội dung dữ liệu cần ký số (chứng chỉ, hóa đơn cấu trúc) |
| `certificateId` | `string?` (UUID) | *Optional* | Tùy chọn chứng thư cụ thể | ID chứng thư số dùng để ký |

*Ví dụ Request Body:*
```json
{
  "payload": {
    "certificateId": "CERT-VN-2026-001",
    "studentName": "Nguyen Van A",
    "degree": "Bachelor of Software Engineering",
    "graduationYear": 2026
  }
}
```

#### Response Thành Công (HTTP 200 OK)
* **Content-Type:** `application/json`

| Tên Trường | Kiểu Dữ Liệu | Mô Tả |
| :--- | :--- | :--- |
| `documentCode` | `string` | Mã định danh tài liệu (`jti` trong JWS Header) |
| `jws` | `string` | Chuỗi ký số JWS chuẩn RFC 7515 (`header.payload.signature`) |
| `signedAt` | `string` (ISO8601) | Thời điểm ký số |

*Ví dụ Response:*
```json
{
  "documentCode": "DOC-2026-JWS00123",
  "jws": "eyJhbGciOiJSUzI1NiIsImp0aSI6IkRPQy0yMDI2LUpXUzAwMTIzIn0.ey...MEYCIQ...",
  "signedAt": "2026-11-18T16:00:00Z"
}
```

---

### 8.3. Xác Thực Toàn Vẹn File Ký Số (File Verification)
* **URL:** `/api/v1/verify/file`
* **Method:** `POST`
* **Xác thực:** Không bắt buộc (Công khai cho mọi người kiểm tra, hoặc truyền Bearer/API Key để ghi nhận danh tính người kiểm tra)
* **Content-Type:** `multipart/form-data`

#### Request Form Data
| Tên Trường | Kiểu Dữ Liệu | Bắt Buộc? | Ràng Buộc & Validation | Mô Tả |
| :--- | :--- | :---: | :--- | :--- |
| `file` | `binary` (File) | **Required** | Dung lượng $\le 20\text{MB}$ | File PDF hoặc file JSON chứa chữ ký cần kiểm tra |

#### Response Thành Công (HTTP 200 OK)
| Tên Trường | Kiểu Dữ Liệu | Mô Tả |
| :--- | :--- | :--- |
| `isValid` | `boolean` | `true` nếu chữ ký toàn vẹn và chứng thư hợp lệ; `false` nếu bị giả mạo |
| `validationStatus` | `string` | Chi tiết mã trạng thái: `VALID`, `TAMPERED_CONTENT`, `CERT_REVOKED`, `CERT_EXPIRED_AT_SIGNING`, `UNKNOWN_SIGNATURE` |
| `documentCode` | `string?` | Mã tài liệu bóc tách được từ chữ ký |
| `signerSubject` | `string?` | Tên người/tổ chức ký số |
| `signingTime` | `string?` | Thời điểm ký số được ghi nhận trên chứng thư |
| `sha256Hash` | `string` | Mã băm SHA-256 hiện tại của nội dung tài liệu |
| `revocationChecked` | `boolean` | Đã kiểm tra trạng thái thu hồi của chứng thư tại thời điểm ký |

*Ví dụ Response (Tài liệu toàn vẹn):*
```json
{
  "isValid": true,
  "validationStatus": "VALID",
  "documentCode": "DOC-2026-A1B2C3D4",
  "signerSubject": "CN=Cong Ty Co Phan ABC, C=VN",
  "signingTime": "2026-11-18T10:00:00Z",
  "sha256Hash": "a1b2c3d4e5f60718293a4b5c6d7e8f90123456789abcdef0123456789abcdef0",
  "revocationChecked": true
}
```

*Ví dụ Response (Phát hiện tài liệu đã bị chỉnh sửa):*
```json
{
  "isValid": false,
  "validationStatus": "TAMPERED_CONTENT",
  "documentCode": "DOC-2026-A1B2C3D4",
  "signerSubject": "CN=Cong Ty Co Phan ABC, C=VN",
  "signingTime": "2026-11-18T10:00:00Z",
  "sha256Hash": "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
  "revocationChecked": true
}
```

#### Mã Lỗi Thường Gặp
* `INVALID_PDF_FORMAT` (400): File rỗng hoặc không phân tích được cấu trúc PDF.
* `UNKNOWN_SIGNATURE` (404): File không chứa bất kỳ chữ ký số nào của nền tảng QuickSign.

---

### 8.4. Tra Cứu Mã QR & Cảnh Báo Ảo Tưởng An Toàn (QR Code Lookup)
* **URL:** `/api/v1/verify/code/{documentCode}`
* **Method:** `GET`
* **Xác thực:** Không (`[AllowAnonymous]`)

#### Path Parameters
| Tên Tham Số | Kiểu Dữ Liệu | Bắt Buộc? | Mô Tả |
| :--- | :--- | :---: | :--- |
| `documentCode` | `string` | **Required** | Mã định danh tài liệu (in trên QR Code) |

#### Response Thành Công (HTTP 200 OK)
> [!CAUTION]
> **Ngăn Chặn Ảo Tưởng An Toàn (Preventing Security Illusion):**
> Khi quét mã QR và gọi API này, hệ thống chỉ chứng minh rằng mã tài liệu có tồn tại trong cơ sở dữ liệu. Nó **KHÔNG THỂ** khẳng định bản in vật lý hay file PDF trên máy người dùng là nguyên vẹn.
> API trả về mã trạng thái đặc biệt: `CODE_EXISTS_UNVERIFIED_CONTENT`. Frontend bắt buộc phải hiển thị Banner cảnh báo màu vàng yêu cầu người dùng tải file lên để đối soát băm mật mã học.

*Ví dụ Response:*
```json
{
  "documentCode": "DOC-2026-A1B2C3D4",
  "documentName": "HoaDonDienTu_INV_2026_001.pdf",
  "signerSubject": "CN=Cong Ty Co Phan ABC, C=VN",
  "signedAt": "2026-11-18T10:00:00Z",
  "validationStatus": "CODE_EXISTS_UNVERIFIED_CONTENT",
  "verificationWarning": "Mã tài liệu hợp lệ trên hệ thống. Vui lòng tải tệp gốc lên để xác minh nội dung chưa bị chỉnh sửa."
}
```

---

## 9. Phụ Lục: Bảng Tổng Hợp Mã Lỗi Toàn Hệ Thống

Toàn bộ các mã lỗi `SCREAMING_SNAKE_CASE` được định nghĩa thống nhất tại [`docs/error_codes.md`](docs/error_codes.md):

| Mã Lỗi (`errorCode`) | HTTP Status | Dịch Vụ Phát Sinh | Ý Nghĩa Kỹ Thuật |
| :--- | :---: | :---: | :--- |
| `UNAUTHORIZED` | `401` | `.NET` / `Go` | Thiếu hoặc sai định dạng JWT Token / API Key |
| `FORBIDDEN_ACCESS` | `403` | `.NET` / `Go` | Người dùng không có quyền hạn đối với tài nguyên |
| `INVALID_CREDENTIALS` | `401` | `.NET` | Tên đăng nhập hoặc mật khẩu không chính xác |
| `SESSION_EXPIRED` | `401` | `.NET` / `Go` | Phiên làm việc JWT đã hết hạn |
| `REFRESH_TOKEN_EXPIRED` | `401` | `.NET` | Refresh token đã hết hạn |
| `REGISTRATION_FAILED` | `400` | `.NET` | Đăng ký tài khoản thất bại (trùng lặp thông tin) |
| `INVALID_PAYLOAD` | `400` | `.NET` / `Go` | Dữ liệu gửi lên sai schema hoặc vi phạm validation |
| `API_KEY_INVALID` | `401` | `.NET` / `Go` | Khóa API Key không hợp lệ |
| `API_KEY_REVOKED` | `401` | `.NET` / `Go` | Khóa API Key đã bị vô hiệu hóa |
| `API_KEY_EXPIRED` | `401` | `.NET` / `Go` | Khóa API Key đã hết thời hạn sử dụng |
| `API_KEY_SCOPE_INSUFFICIENT` | `403` | `.NET` / `Go` | API Key thiếu scope quyền cần thiết |
| `API_KEY_NOT_FOUND` | `404` | `.NET` | Không tìm thấy API Key chỉ định |
| `CERT_NOT_FOUND` | `404` | `.NET` / `Go` | Không tìm thấy chứng thư số yêu cầu |
| `MAX_ACTIVE_CERTS_REACHED` | `400` | `.NET` | Đã đạt giới hạn tối đa 5 chứng thư active |
| `CERT_ALREADY_REVOKED` | `400` | `.NET` | Chứng thư số đã bị thu hồi trước đó |
| `INVALID_REVOCATION_TIMESTAMP` | `400` | `.NET` | Mốc thời gian vô hiệu hóa sớm hơn thời điểm tạo |
| `UNAUTHORIZED_CERTIFICATE_ACCESS` | `403` | `.NET` / `Go` | Thao tác trên chứng thư của người khác |
| `CERT_KEYGEN_FAILED` | `500` | `.NET` / `Go` | Sinh khóa mật mã bất đối xứng thất bại |
| `CERT_REVOKED_AT_SIGNING` | `422` | `Go` | Chứng thư đã bị thu hồi trước lúc ký |
| `CERT_EXPIRED_AT_SIGNING` | `422` | `Go` | Chứng thư đã hết hạn trước lúc ký |
| `CERT_INACTIVE` | `422` | `Go` | Chứng thư không ở trạng thái Active |
| `INVALID_PDF_FORMAT` | `400` | `Go` | Tệp tải lên không phải là PDF hợp lệ |
| `INVALID_JSON_PAYLOAD` | `400` | `Go` | Payload JSON sai định dạng |
| `SIGNING_FAILED` | `500` | `Go` | Luồng ký số PAdES / JWS gặp sự cố nội bộ |
| `TAMPERED_CONTENT` | `422` | `Go` | Nội dung tài liệu bị thay đổi sau khi ký |
| `UNKNOWN_SIGNATURE` | `404` | `Go` | Tài liệu không chứa chữ ký số của QuickSign |
| `CODE_EXISTS_UNVERIFIED_CONTENT` | `200` | `Go` | Mã QR hợp lệ; cần tải file đối soát |
| `DOCUMENT_NOT_FOUND` | `404` | `.NET` / `Go` | Không tìm thấy tài liệu |
| `DOCUMENT_CODE_EXISTS` | `409` | `Go` | Mã tài liệu bị trùng lặp |
| `RATE_LIMIT_EXCEEDED` | `429` | `.NET` / `Go` | Vượt quá tần suất gửi yêu cầu |
| `INTERNAL_SERVER_ERROR` | `500` | `.NET` / `Go` | Lỗi máy chủ nội bộ không mong muốn |
| `SERVICE_UNAVAILABLE` | `503` | `.NET` / `Go` | Dịch vụ phía dưới tạm thời gián đoạn |
| `VAULT_COMMUNICATION_ERROR` | `500` | `.NET` / `Go` | Lỗi giao tiếp với HashiCorp Vault |
| `REDIS_CACHE_ERROR` | `500` | `.NET` / `Go` | Lỗi kết nối bộ đệm Redis |
| `DATABASE_ERROR` | `500` | `.NET` | Lỗi truy vấn cơ sở dữ liệu PostgreSQL |
