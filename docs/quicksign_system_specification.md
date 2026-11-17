# TÀI LIỆU ĐẶC TẢ KỸ THUẬT & CHỨC NĂNG: NỀN TẢNG CHỮ KÝ SỐ (QUICKSIGN)

## 1. Tổng quan Hệ thống

**QuickSign** là nền tảng cung cấp dịch vụ ký số (Digital Signature) và xác thực tài liệu (Hóa đơn PDF, Chứng chỉ PDF/JSON) phục vụ cho người dùng cá nhân trên giao diện Web và tích hợp tự động vào các dự án phần mềm khác thông qua API Key.

### Nguyên tắc thiết kế cốt lõi (MVP Phase):

* **Phân tách Backend chuyên biệt (.NET + Golang):**
  * **Go Crypto Engine (Golang):** Đảm nhận 100% các luồng thực thi thuật toán mật mã học bao gồm khởi tạo cặp khóa, nhúng chữ ký số vào cấu trúc PDF/JSON, xác thực chữ ký và giao tiếp trực tiếp với HashiCorp Vault.
  * **.NET Management API (.NET):** Đảm nhận toàn bộ các tác vụ còn lại bao gồm xác thực JWT với QuickBite Identity, phân quyền (`User`/`Admin`), quản lý vòng đời API Key, quản lý hồ sơ tài liệu và truy vấn thống kê lịch sử xác nhận.
* **Tái sử dụng Định danh (Zero-User Table):** Không xây dựng lại hệ thống đăng nhập/đăng ký. Mọi tác vụ xác thực người dùng trên Web đều gọi trực tiếp API của **QuickBite Identity**.
* **Kiến trúc Tinh gọn (Synchronous Core):** Tập trung vào luồng Ký, Xác thực và Thống kê. Chưa sử dụng Message Broker (Kafka/RabbitMQ) hay dịch vụ gửi Email tự động.

---

## 2. Kiến trúc Kỹ thuật & Phân bổ Dịch vụ

| Thành phần | Công nghệ sử dụng | Vai trò chi tiết trong hệ thống |
| :--- | :--- | :--- |
| **Reverse Proxy / Gateway** | Traefik + Cloudflare Tunnel | Điều hướng truy cập theo đường dẫn (Path-based routing) về .NET hoặc Go, bảo mật lớp mạng, giới hạn tần suất (Rate Limiting). |
| **Frontend (Web Portal)** | Next.js | Giao diện người dùng (Ký/Xác thực thủ công, quản lý API Key, xem thống kê) và trang quản trị Admin. |
| **Identity Provider** | QuickBite Identity (Sẵn có) | Cung cấp API Đăng ký, Đăng nhập, cấp phát JWT Access Token và phân quyền Role (`Admin`, `User`). |
| **Management Service** | **.NET (ASP.NET Core)** | Quản lý chứng thư số (metadata), cấp phát/thu hồi API Key, lưu trữ lịch sử, cung cấp API thống kê (Analytics) và API cho Admin. |
| **Crypto & Signing Engine** | **Golang** | Thực thi thuật toán băm ($\text{SHA-256}$), mã hóa bất đối xứng ($\text{RSA-2048}$, $\text{ECDSA}$), đọc/ghi cấu trúc byte của PDF (PAdES) và JSON (JWS), kết nối trực tiếp với Vault và quản lý giải phóng RAM. |
| **Key Management (KMS)** | HashiCorp Vault | Lưu trữ bảo mật Private Key của hệ thống (Root CA) và Private Key của từng người dùng. Chỉ cho phép **Go Engine** truy cập. |
| **Database** | PostgreSQL | Lưu trữ thông tin Public Key, cấu hình API Key, metadata tài liệu đã ký và lịch sử xác thực. |
| **Caching & Auth Sync** | Redis | Đồng bộ trạng thái API Key và Public Key giữa **.NET** và **Golang**, hỗ trợ chống spam bộ đếm xác thực. |
| **Object Storage (Tùy chọn)** | MinIO / Local Storage | Lưu trữ bản sao file PDF đã ký (nếu bật chế độ lưu trữ phục vụ xem trực tiếp qua QR Code). |

### Tiêu chuẩn Mật mã học (Thực thi bởi Golang):

* **Thuật toán Băm (Hashing):** $\text{SHA-256}$.
* **Thuật toán Bất đối xứng:** $\text{RSA-2048}$ hoặc $\text{ECDSA (P-256)}$.
* **Định dạng Chữ ký:**
  * Tài liệu PDF: Chuẩn **PAdES** (PDF Advanced Electronic Signatures - PKCS#7/CMS detached signature nhúng vào cấu trúc `/ByteRange` của PDF).
  * Dữ liệu JSON: Chuẩn **JWS** (JSON Web Signature - RFC 7515).

---

## 3. Cơ chế Giao tiếp Liên dịch vụ (.NET $\leftrightarrow$ Golang)

Để tối ưu hóa băng thông và bộ nhớ RAM (tránh việc truyền tải file PDF nhiều lần giữa các container), hệ thống áp dụng mô hình **Phân luồng Trực tiếp kết hợp Giao thức Nội bộ**:

1. **Đối với luồng Khởi tạo Khóa (Key Generation):**
   * Người dùng gọi vào **.NET API** (`POST /api/v1/certificates`).
   * **.NET** kiểm tra quyền hạn của User, sau đó gọi nội bộ (qua gRPC hoặc Internal HTTP) sang **Go Crypto Engine**.
   * **Go Engine** sinh cặp khóa, cất Private Key vào **HashiCorp Vault**, tạo chứng thư X.509 rồi trả `public_key_pem` cùng `vault_key_path` về cho **.NET** lưu vào PostgreSQL.

2. **Đối với luồng Ký số và Xác thực (File Streaming):**
   * Traefik định tuyến trực tiếp các request `POST /api/v1/sign/*` và `POST /api/v1/verify/*` thẳng vào **Go Crypto Engine** (không đi vòng qua .NET để tránh nhân đôi dung lượng file PDF trong RAM).
   * **Go Engine** tự xác thực JWT (qua JWKS của QuickBite) hoặc xác thực `X-API-Key` bằng cách tra cứu nhanh trên **Redis** (nếu Redis miss mới đọc từ bảng `api_keys` trong PostgreSQL).
   * Sau khi ký hoặc xác thực thành công, **Go Engine** ghi nhận trực tiếp bản ghi vào bảng `signed_documents` / `verification_logs` (hoặc bắn thông tin metadata nhẹ sang .NET).

---

## 4. Cơ chế Xác thực & Phân quyền (Auth & RBAC)

### 4.1. Hai luồng xác thực (Authentication Flows)

1. **Luồng Web UI (Sử dụng QuickBite JWT):**
   * Giao diện Next.js gọi trực tiếp API `/connect/token` (Đăng nhập) và `/api/account/register` (Đăng ký) của **QuickBite Identity**.
   * Frontend đính kèm `Authorization: Bearer <token>` khi gọi xuống Backend (.NET và Go).
   * Backend xác thực chữ ký của JWT thông qua JWKS Endpoint của QuickBite Identity, trích xuất `sub` (User ID) và `role` (`Admin` hoặc `User`).

2. **Luồng Tích hợp Dự án (Sử dụng API Key):**
   * Các dự án bên ngoài gửi kèm Header `X-API-Key: qs_live_<chuỗi_bí_mật>`.
   * Hệ thống băm $\text{SHA-256}$ chuỗi API Key này và đối chiếu với Redis/PostgreSQL để xác định `user_id` sở hữu, trạng thái hoạt động và quyền hạn (`sign`, `verify`).

### 4.2. Ma trận Phân quyền (Authorization Matrix)

| Chức năng / Tác vụ | Service Xử lý | Public (Khách) | User (JWT) | Dự án (API Key) | Admin (JWT) |
| :--- | :---: | :---: | :---: | :---: | :---: |
| Đăng ký / Đăng nhập | **QuickBite** | $\checkmark$ | - | - | - |
| Khởi tạo / Thu hồi Chứng thư số | **.NET + Go** | - | $\checkmark$ | - | $\checkmark$ |
| Tạo / Xóa / Xem danh sách API Key | **.NET** | - | $\checkmark$ | - | $\checkmark$ |
| Ký số tài liệu (PDF / JSON) | **Golang** | - | $\checkmark$ (Web) | $\checkmark$ (API) | $\checkmark$ |
| Xác thực tài liệu (Upload / Tra mã) | **Golang** | $\checkmark$ | $\checkmark$ | $\checkmark$ | $\checkmark$ |
| Xem lịch sử ký & Thống kê xác thực | **.NET** | - | $\checkmark$ (Cá nhân) | - | $\checkmark$ (Toàn cục) |
| Quản lý Root CA (Khóa gốc nền tảng) | **.NET + Go** | - | - | - | $\checkmark$ |
| Khóa (Revoke) Chứng thư / API Key bất kỳ | **.NET** | - | - | - | $\checkmark$ |

---

## 5. Chi tiết Chức năng & Giải quyết các Điểm nghẽn Kỹ thuật

### 5.1. Quản lý Chứng thư số & Bài toán Thu hồi / Hết hạn (Long-Term Validation)
* **Vấn đề thực tế:** Nếu một hóa đơn được ký vào tháng 06/2026, đến tháng 01/2027 chứng thư số hết hạn (hoặc bị người dùng bấm thu hồi do đổi khóa mới), khi đem hóa đơn cũ đi xác thực vào năm 2027 thì hệ thống không được phép báo hóa đơn đó là giả mạo.
* **Cơ chế xử lý:**
  * Khi thu hồi khóa (Revoke), **.NET** yêu cầu người dùng chọn thời điểm bắt đầu vô hiệu hóa (`invalidated_from` - mặc định là thời điểm bấm nút, hoặc lùi về quá khứ nếu nghi ngờ lộ khóa từ trước).
  * Khi xác thực, **Go Engine** so sánh **Thời điểm ký tài liệu (`signed_documents.created_at`)** với vòng đời của Chứng thư số: Chữ ký được tính là hợp lệ nếu tại thời điểm ký, chứng thư số vẫn đang hoạt động (`cert.created_at <= doc.created_at <= cert.expires_at` và `doc.created_at < cert.invalidated_from`).

### 5.2. Dịch vụ Ký số (Thực thi bởi Golang)
* **Nhúng Định danh Tài liệu (`document_code`) vào Chữ ký:**
  * Trước khi ký, **Go Engine** sinh mã `document_code` (ví dụ: `QS-2026-8F9A2B`).
  * Đối với PDF: Go nhúng `document_code` và `certificate_id` vào vùng Custom Metadata / Signature Dictionary của khối PKCS#7 trong file PDF.
  * Đối với JSON (JWS): Go nhúng `document_code` vào phần Protected Header (`jti` - JWT ID) và `certificate_id` vào (`kid` - Key ID).
* **Tác dụng:** Giúp định danh chính xác tài liệu gốc ngay cả khi nội dung văn bản bên trong file bị kẻ gian chỉnh sửa.

### 5.3. Dịch vụ Xác thực & Phát hiện Giả mạo (Thực thi bởi Golang)
* Khi nhận một file PDF hoặc chuỗi JWS cần xác thực, **Go Engine** thực hiện tuần tự:
  1. Băm toàn bộ file nhận được thành `submitted_hash`.
  2. Tách khối chữ ký số để đọc `document_code`, `certificate_id` và chữ ký mật mã.
  3. **Trường hợp 1 (Không có cấu trúc chữ ký của QuickSign):** Ghi log vào `verification_logs` với trạng thái `UNKNOWN_SIGNATURE` (`signed_document_id = NULL`).
  4. **Trường hợp 2 (Có chữ ký của QuickSign):** Truy vấn bảng `signed_documents` theo `document_code`.
     * Dùng Public Key của `certificate_id` kiểm tra chữ ký mật mã trên phần nội dung gốc của file được tải lên.
     * Nếu thuật toán báo sai lệch (do file PDF bị sửa chữ/số tiền sau khi ký): Trả về kết quả `is_valid = FALSE`, mã lỗi `TAMPERED_CONTENT` (Tài liệu thật nhưng đã bị sửa đổi nội dung), đồng thời **vẫn gắn log cảnh báo này vào đúng `signed_document_id` của chủ sở hữu** để họ biết tài liệu nào của mình đang bị làm giả.
     * Nếu thuật toán khớp hoàn toàn và thời điểm ký hợp lệ: Trả về `is_valid = TRUE`, mã `VALID`.

### 5.4. Thống kê Lịch sử Xác nhận & Chống Spam Bộ đếm (Thực thi bởi .NET & Redis)
* **Chống Spam (Debounce Verification Count):**
  * Khi một tài liệu được xác thực, **Go Engine** kiểm tra key Redis `verify_lock:{document_code}:{verifier_ip}` (TTL: 10 phút).
  * Bảng `verification_logs` vẫn ghi nhận mọi lượt gọi, nhưng cột `unique_verify_count` chỉ tăng khi IP đó chưa kiểm tra tài liệu này trong 10 phút qua, giúp số liệu thống kê trên Dashboard của **.NET** phản ánh đúng thực tế.
* **Dashboard Thống kê (.NET cung cấp API):**
  * Biểu đồ số lượng tài liệu đã ký và số lượt được xác thực theo thời gian.
  * Tra cứu lịch sử xác nhận trên từng tài liệu: Xem danh sách từng lần tài liệu được đem đi kiểm tra (Thời gian, IP, Thiết bị, Kết quả: Hợp lệ hay Bị chỉnh sửa).

---

## 6. Chiến lược Lưu trữ (Storage Architecture)

### 6.1. Lưu trữ Khóa mật mã (HashiCorp Vault - Chỉ giao tiếp với Go)
* Chỉ container **Go Crypto Engine** mới được cấp Vault Token để đọc/ghi vào đường dẫn:
  * Khóa gốc: `secret/data/quicksign/root_ca`
  * Khóa người dùng: `secret/data/quicksign/users/{quickbite_user_id}/keys/{certificate_id}`
* Trong Go, mảng byte (`[]byte`) chứa Private Key sau khi lấy từ Vault về RAM để ký phải được ghi đè bằng các byte `0` (`defer clearPrivateKey(keyBytes)`) ngay khi hàm ký kết thúc.

### 6.2. Lưu trữ Tài liệu (Privacy-First Storage)
* **Chế độ Mặc định (Zero-Document Retention):** Không lưu trữ file gốc hay file đã ký trên ổ cứng. Chỉ lưu mã băm $\text{SHA-256}$, chữ ký và metadata vào PostgreSQL.
* **Chế độ Lưu trữ Bản sao (Tùy chọn theo cấu hình khi ký):** Lưu file PDF đã ký vào MinIO/Local Volume theo đường dẫn `/signed-docs/{year}/{month}/{document_code}.pdf` để phục vụ tính năng quét QR Code xem trực tiếp chứng chỉ.

---

## 7. Thiết kế Cơ sở Dữ liệu (PostgreSQL Schema)

Cả **.NET** và **Golang** cùng kết nối tới một cơ sở dữ liệu PostgreSQL (trong đó **.NET** quản lý Migration qua Entity Framework Core / DbUp). Trường `user_id` lưu trực tiếp giá trị `sub` từ JWT của **QuickBite Identity**.

### 7.1. Bảng `certificates` (Quản lý Chứng thư số & Khóa công khai)

| Tên cột | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `id` | `UUID` | PK | Khóa chính. |
| `user_id` | `VARCHAR(100)` | NOT NULL, INDEX | ID người dùng từ QuickBite Identity. |
| `subject_name` | `VARCHAR(255)` | NOT NULL | Tên chủ sở hữu hiển thị trên chữ ký (VD: Dự án QuickTest). |
| `serial_number` | `VARCHAR(100)` | UNIQUE, NOT NULL | Số serial duy nhất của chứng thư số X.509. |
| `algorithm` | `VARCHAR(50)` | NOT NULL | Thuật toán (`RSA-2048` hoặc `ECDSA-P256`). |
| `public_key_pem` | `TEXT` | NOT NULL | Nội dung Public Key / Certificate định dạng PEM. |
| `vault_key_path` | `VARCHAR(255)` | NOT NULL | Đường dẫn trỏ tới Private Key lưu trong HashiCorp Vault. |
| `status` | `VARCHAR(20)` | NOT NULL | Trạng thái: `ACTIVE`, `EXPIRED`, `REVOKED`. |
| `revoked_at` | `TIMESTAMPTZ` | NULL | Thời điểm thực hiện thao tác thu hồi khóa. |
| `invalidated_from` | `TIMESTAMPTZ` | NULL | Mốc thời gian mà các chữ ký tạo ra sau thời điểm này bị coi là vô hiệu. |
| `revoked_reason` | `VARCHAR(255)` | NULL | Lý do thu hồi (nếu bị thu hồi). |
| `expires_at` | `TIMESTAMPTZ` | NOT NULL | Thời điểm hết hạn chứng thư. |
| `created_at` | `TIMESTAMPTZ` | NOT NULL | Thời điểm khởi tạo. |

### 7.2. Bảng `api_keys` (Do .NET quản lý, Go đọc qua Redis/DB)

| Tên cột | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `id` | `UUID` | PK | Khóa chính. |
| `user_id` | `VARCHAR(100)` | NOT NULL, INDEX | ID người dùng sở hữu từ QuickBite Identity. |
| `name` | `VARCHAR(150)` | NOT NULL | Tên gợi nhớ (VD: "Server Hóa đơn", "Web Chứng chỉ"). |
| `key_prefix` | `VARCHAR(20)` | NOT NULL | 8 ký tự đầu để hiển thị trên UI (VD: `qs_live_9a8b`). |
| `key_hash` | `VARCHAR(64)` | UNIQUE, NOT NULL | Mã băm $\text{SHA-256}$ của toàn bộ API Key. |
| `scopes` | `VARCHAR(100)` | NOT NULL | Quyền hạn, cách nhau bởi dấu phẩy (`sign,verify`). |
| `is_active` | `BOOLEAN` | DEFAULT TRUE | Trạng thái kích hoạt. |
| `last_used_at` | `TIMESTAMPTZ` | NULL | Lần cuối cùng Key này được gọi API. |
| `expires_at` | `TIMESTAMPTZ` | NULL | Thời gian hết hạn (NULL nếu không giới hạn). |
| `created_at` | `TIMESTAMPTZ` | NOT NULL | Thời điểm tạo Key. |

### 7.3. Bảng `signed_documents` (Hồ sơ Tài liệu đã ký - Go ghi, .NET truy vấn)

| Tên cột | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `id` | `UUID` | PK | Khóa chính. |
| `document_code` | `VARCHAR(50)` | UNIQUE, NOT NULL | Mã định danh nhúng vào metadata của file (VD: `QS-2026-8F9A2B`). |
| `user_id` | `VARCHAR(100)` | NOT NULL, INDEX | ID người dùng thực hiện ký. |
| `certificate_id` | `UUID` | FK (`certificates.id`) | Chứng thư số đã dùng để ký tài liệu này. |
| `api_key_id` | `UUID` | FK (`api_keys.id`), NULL | NULL nếu ký trực tiếp trên Web UI, có giá trị nếu ký qua API. |
| `doc_type` | `VARCHAR(20)` | NOT NULL | Định dạng tài liệu: `PDF` hoặc `JSON`. |
| `document_name` | `VARCHAR(255)` | NOT NULL | Tên file hóa đơn hoặc tiêu đề chứng chỉ. |
| `original_hash` | `VARCHAR(64)` | NOT NULL, INDEX | Mã băm $\text{SHA-256}$ của vùng dữ liệu gốc trước khi đóng gói chữ ký. |
| `signed_hash` | `VARCHAR(64)` | NOT NULL, INDEX | Mã băm $\text{SHA-256}$ của toàn bộ file/payload hoàn chỉnh sau khi ký. |
| `signature_value` | `TEXT` | NOT NULL | Chuỗi chữ ký số (Base64). |
| `storage_path` | `VARCHAR(500)` | NULL | Đường dẫn file (nếu bật chế độ lưu bản sao). |
| `total_verify_count` | `INT` | DEFAULT 0 | Tổng số lượt gọi kiểm tra tài liệu này. |
| `unique_verify_count`| `INT` | DEFAULT 0 | Số lượt kiểm tra thực tế (đã lọc trùng lặp theo IP trong 10 phút). |
| `tampered_alert_count`| `INT` | DEFAULT 0 | Số lần phát hiện có người tải lên bản tài liệu này nhưng đã bị chỉnh sửa. |
| `last_verified_at` | `TIMESTAMPTZ` | NULL | Thời điểm gần nhất có người xác thực tài liệu này. |
| `created_at` | `TIMESTAMPTZ` | NOT NULL | Thời điểm ký tài liệu. |

### 7.4. Bảng `verification_logs` (Lịch sử Xác thực chi tiết)

| Tên cột | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `id` | `BIGSERIAL` | PK | Khóa chính tự tăng. |
| `signed_document_id` | `UUID` | FK (`signed_documents.id`), NULL, INDEX | Liên kết tới tài liệu gốc (nhờ đọc `document_code` nhúng trong chữ ký). |
| `submitted_hash` | `VARCHAR(64)` | NOT NULL | Mã băm $\text{SHA-256}$ của file/dữ liệu mà người kiểm tra gửi lên. |
| `verification_method` | `VARCHAR(20)` | NOT NULL | Phương thức kiểm tra: `WEB_UPLOAD`, `WEB_CODE`, `API`. |
| `api_key_id` | `UUID` | NULL | ID của API Key (nếu gọi xác thực thông qua API Key). |
| `verifier_ip` | `VARCHAR(45)` | NOT NULL | Địa chỉ IP (IPv4/IPv6) của người thực hiện kiểm tra. |
| `user_agent` | `TEXT` | NULL | Thông tin trình duyệt / thiết bị của người kiểm tra. |
| `is_valid` | `BOOLEAN` | NOT NULL | Kết quả xác thực: `TRUE` (Hợp lệ), `FALSE` (Không hợp lệ). |
| `validation_status` | `VARCHAR(50)` | NOT NULL | Mã chi tiết: `VALID`, `TAMPERED_CONTENT` (bị sửa nội dung), `CERT_REVOKED`, `CERT_EXPIRED_AT_SIGNING`, `UNKNOWN_SIGNATURE`. |
| `verified_at` | `TIMESTAMPTZ` | NOT NULL, INDEX | Thời gian thực hiện xác thực. |

---

## 8. Đặc tả Giao diện Lập trình (API Endpoints & Routing)

Traefik Reverse Proxy sẽ phân luồng request dựa trên URL Path về đúng dịch vụ xử lý:

### 8.1. Nhóm Xác thực Người dùng (Frontend gọi trực tiếp sang QuickBite Identity)
* `POST {QUICKBITE_URL}/api/account/register`: Đăng ký tài khoản mới.
* `POST {QUICKBITE_URL}/connect/token`: Đăng nhập lấy `access_token` và `refresh_token`.

### 8.2. Nhóm Quản trị, Thống kê & Admin (Traefik route về **.NET Management Service**)
*Yêu cầu Header `Authorization: Bearer <QuickBite_JWT>`*
* `GET /api/v1/certificates`: Lấy danh sách chứng thư số của tôi.
* `POST /api/v1/certificates`: Khởi tạo chứng thư số mới (.NET kiểm tra điều kiện rồi gọi gRPC/HTTP nội bộ sang **Go Engine** để sinh khóa).
* `PUT /api/v1/certificates/{id}/revoke`: Thu hồi chứng thư số (và xóa cache trên Redis).
* `GET /api/v1/api-keys`: Lấy danh sách API Key của tôi.
* `POST /api/v1/api-keys`: Tạo API Key mới (sinh key, băm $\text{SHA-256}$, lưu DB).
* `DELETE /api/v1/api-keys/{id}`: Xóa/Vô hiệu hóa API Key (đồng thời xóa cache Redis).
* `GET /api/v1/stats/overview`: Xem thống kê tổng quan lượt ký, lượt xác thực và cảnh báo giả mạo.
* `GET /api/v1/documents`: Danh sách tài liệu đã ký (phân trang, lọc theo dự án/API Key).
* `GET /api/v1/documents/{id}/verifications`: Xem lịch sử chi tiết từng lần tài liệu `{id}` được xác thực.
* `GET /api/v1/admin/*`: Các API quản trị toàn hệ thống dành riêng cho Role `Admin`.

### 8.3. Nhóm Thực thi Ký & Xác thực (Traefik route trực tiếp về **Go Crypto Engine**)
*Hỗ trợ cả `Authorization: Bearer <JWT>` (Web) và `X-API-Key` (Dự án tích hợp)*
* `POST /api/v1/sign/pdf`: Nhận stream file PDF, nhúng `document_code` và chữ ký chuẩn PAdES, ghi nhận vào bảng `signed_documents`, trả về file PDF đã ký.
* `POST /api/v1/sign/json`: Nhận payload JSON, ký chuẩn JWS kèm Header `jti` (`document_code`), trả về chuỗi JWS.
* `POST /api/v1/verify/file`: Nhận file PDF hoặc chuỗi JWS, bóc tách chữ ký, thực thi thuật toán xác thực mật mã học, cập nhật bộ đếm và ghi log vào `verification_logs`.
* `GET /api/v1/verify/code/{document_code}`: Tra cứu nhanh tính hợp lệ thông qua mã tài liệu gắn trong QR Code.