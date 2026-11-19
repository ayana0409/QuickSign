# KẾ HOẠCH TRIỂN KHAI (IMPLEMENTATION PLAN)
## GIAI ĐOẠN 2: ENGINE KÝ SỐ & XÁC THỰC TÀI LIỆU (PHASE 2 - GO CRYPTO ENGINE)

---

## 1. Metadata
* **Mã Kế Hoạch:** `PLAN-2026-11-19-CRYPTO-ENGINE-PHASE2`
* **File Kế Hoạch:** `docs/plans/active/2026-11-19-crypto-engine-phase2-signing-verification.md`
* **Trạng Thái Hiện Tại:** `PENDING_APPROVAL` *(Chờ Người Dùng Phê Duyệt)*
* **Phạm Vi Tác Động:**
  - `src/crypto-engine/` (Mở rộng toàn diện: Domain, Ports, Adapters, Services, HTTP REST API)
  - `docs/plans/` (Theo dõi tiến độ triển khai)

---

## 2. Mục Tiêu & Bối Cảnh (Goal Description)

### Hiện Trạng Sau Giai Đoạn 1 (Phase 1)
* **Go Crypto Engine (`src/crypto-engine`)** đã hoàn thiện thành công:
  - gRPC Server (`port 50051`) thực hiện sinh cặp khóa RSA-2048 / ECDSA-P256 và chứng thư X.509 self-signed.
  - Tích hợp HashiCorp Vault KV v2 lưu trữ và xóa Private Key (Compensating Transaction).
  - Tẩy sạch byte khóa khỏi RAM sau khi xử lý (`pkg/memguard/zero.go` theo ADR 0001 §3).
  - Tự động nạp cấu hình từ file `.env` khi khởi chạy.
  - .NET Management API kết nối gRPC và chạy bộ kiểm thử tự động đạt 100% (31/31 tests).

### Mục Tiêu Giai Đoạn 2 (Phase 2)
Triển khai **Engine Ký số và Xác thực tài liệu (Signing & Verification Engine)** trên nền tảng **Go Crypto Engine**, cung cấp giao diện lập trình **HTTP REST API** (mặc định lắng nghe trên cổng `50052`) với 4 endpoint cốt lõi theo đúng đặc tả hệ thống và [ADR 0001](file:///docs/adr/0001-cryptography-and-distributed-integrity.md):

1. **`POST /api/v1/sign/pdf`**:
   - Nhận stream file PDF gốc ($\le 20\text{MB}$), hash SHA-256.
   - Lấy Private Key từ Vault và kiểm tra tính hợp lệ của Certificate người dùng.
   - Ký số theo chuẩn **PAdES-BES (ISO 32000-1)** bằng cơ chế Incremental Update.
   - Nhúng mã tra cứu `document_code` (VD: `DOC-YYYY-XXXXXXXX`) vào **Signed Attributes (PKCS#9)** bên trong khối CMS/PKCS#7 (tuân thủ nghiêm ngặt ADR 0001 §2).
   - Tự động tạo và nhúng hình ảnh con dấu chữ ký điện tử kèm **mã QR** tra cứu trực tiếp.
   - Ghi nhận thông tin tài liệu vào bảng PostgreSQL `signed_documents`.
   - Trả về file PDF đã ký kèm header `X-Document-Code`.

2. **`POST /api/v1/sign/json`**:
   - Nhận đối tượng JSON, ký số theo chuẩn **JWS (RFC 7515 Compact Serialization)**.
   - Nhúng `document_code` vào header `jti` của JWS.
   - Hỗ trợ cả 2 thuật toán: `RS256` (RSA-2048) và `ES256` (ECDSA-P256).
   - Ghi nhận vào cơ sở dữ liệu và trả về chuỗi JWS (`header.payload.signature`).

3. **`POST /api/v1/verify/file`**:
   - Nhận file PDF đã ký hoặc chuỗi JWS.
   - Bóc tách chữ ký, trích xuất chứng thư và kiểm tra tính hợp lệ toán học của chữ ký.
   - Kiểm tra trạng thái chứng thư tại thời điểm ký (`Status == Active`, chưa bị thu hồi trước mốc `signing_time`).
   - Bóc tách mã `document_code` từ Signed Attributes PKCS#9 và đối soát mã băm gốc trong bảng `signed_documents`.
   - Cập nhật số đếm bằng **SQL nguyên tử (Atomic SQL - ADR 0001 §5)**: `total_verify_count`, `unique_verify_count`, `tampered_alert_count`.
   - Ghi log chi tiết vào bảng `verification_logs`.
   - Trả về kết quả: `isValid`, mã trạng thái chi tiết (`VALID`, `TAMPERED_CONTENT`, `CERT_REVOKED`, `CERT_EXPIRED_AT_SIGNING`, `UNKNOWN_SIGNATURE`).

4. **`GET /api/v1/verify/code/{documentCode}`**:
   - Cổng tra cứu công khai cho người dùng quét mã QR trên bản in hoặc chứng chỉ.
   - Tuân thủ **ADR 0001 §6 - Ngăn chặn ảo giác an toàn (Security Illusion)**: Chỉ xác nhận mã tài liệu có tồn tại trong hệ thống, trả về mã trạng thái `CODE_EXISTS_UNVERIFIED_CONTENT` kèm thông điệp hướng dẫn tải file gốc lên để đối soát mật mã.

---

## 3. Các Điểm Cần Người Dùng Lưu Ý (User Review Required)

> [!IMPORTANT]
> **1. Cổng Dịch Vụ Mới (HTTP REST API Port):**
> * gRPC Server tiếp tục lắng nghe trên cổng **`50051`** (để .NET gọi sang).
> * HTTP REST API của Go Crypto Engine sẽ lắng nghe trên cổng **`50052`** (cấu hình qua biến môi trường `HTTP_PORT=50052` trong file `.env`).
> * Reverse Proxy (hoặc Frontend) sẽ route các request `/api/v1/sign/*` và `/api/v1/verify/*` trực tiếp tới cổng `50052` này.

> [!WARNING]
> **2. Cơ Chế Xác Thực Kép (Dual Authentication Middleware):**
> * Các endpoint ký số (`/api/v1/sign/*`) hỗ trợ 2 hình thức:
>   - **Web Portal:** `Authorization: Bearer <JWT>` (xác thực chữ ký JWT từ QuickBite Identity).
>   - **Tích Hợp Bên Thứ Ba:** `X-API-Key: qs_live_...` (đối soát chuỗi hash SHA-256 trong bảng `api_keys` và kiểm tra scope `sign`).
> * Khi gọi qua API Key, hệ thống tự động cache kết quả vào Redis để đạt throughput cao và giảm tải cho PostgreSQL.

> [!NOTE]
> **3. Thư Viện Ký PAdES & QR Code trong Go:**
> * Ký số PAdES chuẩn ISO 32000-1 đòi hỏi xử lý cấu trúc nhị phân của PDF (`/ByteRange`, Incremental Update, CMS signature container). Ta sẽ sử dụng thư viện ký số PDF tiêu chuẩn kết hợp cơ chế nhúng PKCS#9 Signed Attributes của Go chuẩn để đảm bảo tương thích 100% với Adobe Acrobat Reader.
> * Mã QR được sinh bằng thư viện `skip2/go-qrcode` dạng PNG và nhúng vào visual signature field của trang cuối tài liệu.

---

## 4. Kiến Trúc Kỹ Thuật (Architecture & Component Design)

```mermaid
flowchart TD
    subgraph Clients["Tác Nhân Gọi API"]
        Web["Web Portal / Next.js"] -->|JWT Bearer| HTTP["Go HTTP API (:50052)"]
        Partner["Hệ Thống Tích Hợp"] -->|X-API-Key| HTTP
        PublicUser["Người Quét QR"] -->|AllowAnonymous| HTTP
        DotNet[".NET Management"] -->|gRPC (:50051)| GRPC["Go gRPC Server"]
    end

    subgraph GoEngine["Go Crypto Engine"]
        HTTP --> AuthMW["Auth Middleware\n(JWT & API-Key + Redis Cache)"]
        AuthMW --> SignHandler["Sign Handler"]
        AuthMW --> VerifyHandler["Verify Handler"]

        SignHandler --> SignService["SigningService\n(core/services/signing_service.go)"]
        VerifyHandler --> VerifyService["VerificationService\n(core/services/verification_service.go)"]
        GRPC --> KeyGenService["KeyGenService\n(core/services/keygen_service.go)"]

        SignService --> VaultPort["VaultKeyStorePort"]
        SignService --> PadesPort["PdfCryptoPort (PAdES)"]
        SignService --> JwsPort["JwsCryptoPort (JWS)"]
        SignService --> RepoPort["DocumentRepositoryPort"]
        SignService --> MemGuard["pkg/memguard (Zero RAM)"]

        VerifyService --> PadesPort
        VerifyService --> JwsPort
        VerifyService --> RepoPort
    end

    subgraph Infrastructure["Hạ Tầng Dữ Liệu"]
        VaultPort --> Vault[("HashiCorp Vault\n(Private Key Storage)")]
        RepoPort --> Postgres[("PostgreSQL\n(certificates, signed_documents, verification_logs)")]
        AuthMW --> Redis[("Redis\n(API Key Cache & Anti-Spam)")]
    end
```

---

## 5. Danh Sách Các Thay Đổi & File Mới (Proposed Changes)

### 5.1. Cấu Hình & Dependencies (`src/crypto-engine`)
#### [MODIFY] `go.mod`
* Bổ sung các thư viện cần thiết:
  - `github.com/jackc/pgx/v5`: Kết nối và thực thi truy vấn PostgreSQL hiệu năng cao.
  - `github.com/redis/go-redis/v9`: Kết nối Redis cache.
  - `github.com/golang-jwt/jwt/v5`: Xác thực và parse JWT claims.
  - `github.com/skip2/go-qrcode`: Sinh mã QR nhúng vào tài liệu.
  - `github.com/google/uuid`: Tiện ích sinh UUID cho tài liệu và logs.

#### [MODIFY] `internal/config/config.go`
* Bổ sung các trường cấu hình:
  - `HTTPPort`: Cổng HTTP REST (mặc định `50052`).
  - `DatabaseURL`: Chuỗi kết nối PostgreSQL (`DATABASE_URL` hoặc ghép từ `DB_HOST`, `DB_PORT`, `DB_USER`, `DB_PASSWORD`, `DB_NAME`).
  - `RedisAddr`, `RedisPassword`: Cấu hình Redis (`REDIS_ADDR`, `REDIS_PASSWORD`).
  - `SSOAuthority`: URL máy chủ SSO để lấy JWKS xác thực JWT.
  - `AppBaseURL`: URL gốc của hệ thống dùng để tạo link quét QR (`https://.../verify/code/`).

#### [MODIFY] `.env` & `.env.example`
* Cập nhật các biến môi trường mới vào file `.env` cục bộ và `.env.example`.

---

### 5.2. Tầng Core: Domain Models & Ports
#### [NEW] `internal/core/domain/signing.go`
* Định nghĩa các struct domain cho luồng ký số:
  - `SignPdfInput`: `FileContent []byte`, `FileName string`, `UserID string`, `CertificateID string`, `Reason string`, `Location string`.
  - `SignPdfOutput`: `SignedBytes []byte`, `DocumentCode string`, `FileHash string`, `SignatureValue string`.
  - `SignJsonInput`: `Payload map[string]any`, `UserID string`, `CertificateID string`.
  - `SignJsonOutput`: `JwsToken string`, `DocumentCode string`, `PayloadHash string`.
  - `DocumentType`: Enum `PDF`, `JSON`.

#### [NEW] `internal/core/domain/verification.go`
* Định nghĩa các struct domain cho luồng xác thực:
  - `VerifyFileInput`: `FileBytes []byte`, `FileName string`, `VerifierIP string`, `UserAgent string`, `ApiKeyID *string`.
  - `VerifyResult`: `IsValid bool`, `ValidationStatus string`, `DocumentCode string`, `SignerSubject string`, `SigningTime time.Time`, `Sha256Hash string`, `RevocationChecked bool`, `Warning string`.
  - `ValidationStatus` constants:
    - `StatusValid = "VALID"`
    - `StatusTamperedContent = "TAMPERED_CONTENT"`
    - `StatusCertRevoked = "CERT_REVOKED"`
    - `StatusCertExpiredAtSigning = "CERT_EXPIRED_AT_SIGNING"`
    - `StatusUnknownSignature = "UNKNOWN_SIGNATURE"`
    - `StatusCodeExistsUnverified = "CODE_EXISTS_UNVERIFIED_CONTENT"`

#### [MODIFY] `internal/core/ports/inbound.go`
* Bổ sung các use cases:
  - `DocumentSigningUseCase`:
    - `SignPdf(ctx context.Context, input domain.SignPdfInput) (*domain.SignPdfOutput, error)`
    - `SignJson(ctx context.Context, input domain.SignJsonInput) (*domain.SignJsonOutput, error)`
  - `DocumentVerificationUseCase`:
    - `VerifyFile(ctx context.Context, input domain.VerifyFileInput) (*domain.VerifyResult, error)`
    - `VerifyByCode(ctx context.Context, documentCode string) (*domain.VerifyResult, error)`

#### [MODIFY] `internal/core/ports/outbound.go`
* Bổ sung các outbound ports (driven interfaces):
  - `PdfCryptoPort`:
    - `SignPdfPades(pdfBytes []byte, privKey any, certDER []byte, docCode string, reason, location string) ([]byte, string, error)`
    - `VerifyPdfPades(pdfBytes []byte) (*domain.PdfSignatureData, error)`
  - `JwsCryptoPort`:
    - `SignJws(payload []byte, privKey any, algo domain.KeyAlgorithm, docCode string) (string, error)`
    - `VerifyJws(jwsString string) (*domain.JwsSignatureData, error)`
  - `DocumentRepositoryPort`:
    - `GetActiveCertificate(ctx context.Context, userId string, certId *string) (*domain.CertificateEntity, error)`
    - `GetCertificateById(ctx context.Context, certId string) (*domain.CertificateEntity, error)`
    - `SaveSignedDocument(ctx context.Context, doc *domain.SignedDocumentEntity) error`
    - `GetSignedDocumentByCode(ctx context.Context, code string) (*domain.SignedDocumentEntity, error)`
    - `SaveVerificationLog(ctx context.Context, log *domain.VerificationLogEntity) error`
    - `IncrementVerifyCount(ctx context.Context, docId string, isUnique bool, isTampered bool) error`
  - `AuthCachePort`:
    - `GetApiKey(ctx context.Context, keyHash string) (*domain.ApiKeyCacheItem, error)`
    - `SetApiKey(ctx context.Context, keyHash string, item *domain.ApiKeyCacheItem, ttl time.Duration) error`
    - `CheckAndRecordVerifyRateLimit(ctx context.Context, docCode, ip string) (isUnique bool, err error)`

---

### 5.3. Tầng Core: Triển Khai Dịch Vụ Mật Mã Học (Services)
#### [NEW] `internal/core/services/signing_service.go`
* Triển khai `DocumentSigningUseCase`:
  1. Kiểm tra chứng thư của người dùng trong PostgreSQL (nếu không truyền `certificateId`, tự động lấy cert `Active` mới nhất).
  2. Kiểm tra tính hợp lệ của cert (hạn dùng, trạng thái thu hồi).
  3. Lấy Private Key an toàn từ HashiCorp Vault thông qua `VaultKeyStorePort.GetPrivateKey`.
  4. Tạo mã tra cứu duy nhất: `DOC-YYYY-XXXXXXXX` (dùng timestamp + chuỗi ngẫu nhiên cryptographic an toàn).
  5. Gọi `PdfCryptoPort` (ký PAdES, nhúng QR code và `document_code` vào PKCS#9 Signed Attributes) hoặc `JwsCryptoPort` (ký JWS).
  6. **Bảo mật tuyệt đối:** Gọi `memguard.ZeroPrivateKey` để xóa sạch Private Key khỏi RAM.
  7. Lưu bản ghi metadata vào bảng `signed_documents`.

#### [NEW] `internal/core/services/verification_service.go`
* Triển khai `DocumentVerificationUseCase`:
  1. **Luồng Xác Thực File (`VerifyFile`):**
     - Nhận dạng định dạng file (PDF hoặc JWS).
     - Bóc tách chữ ký số, tính mã băm SHA-256 hiện tại của tài liệu.
     - Trích xuất chứng thư và chữ ký số toán học để kiểm tra tính toàn vẹn (Integrity Check).
     - Trích xuất `document_code` từ thuộc tính PKCS#9 / header JWS.
     - Tìm bản ghi gốc trong bảng `signed_documents`:
       - Nếu băm không khớp bản ghi gốc $\rightarrow$ Trả về `isValid: false`, `StatusTamperedContent` và tăng `tampered_alert_count`.
       - Nếu khớp $\rightarrow$ Kiểm tra chứng thư xem có bị thu hồi trước thời điểm ký không.
     - Ghi nhận vào bảng `verification_logs`.
     - Cập nhật bộ đếm nguyên tử (`IncrementVerifyCount`).
  2. **Luồng Tra Cứu Mã QR (`VerifyByCode`):**
     - Tìm kiếm mã tài liệu trong bảng `signed_documents`.
     - Nếu không thấy: Trả về lỗi `DOCUMENT_NOT_FOUND` (404).
     - Nếu thấy: Trả về trạng thái `CODE_EXISTS_UNVERIFIED_CONTENT` kèm cảnh báo Security Illusion (yêu cầu người dùng tải file lên để đối soát).

---

### 5.4. Tầng Adapters: Outbound (Driven Adapters)
#### [NEW] `internal/adapters/outbound/pdf_pades/pades_adapter.go`
* Thực hiện ký số PAdES-BES theo chuẩn ISO 32000-1:
  - Tính SHA-256 hash của tài liệu.
  - Sinh ảnh QR Code chứa link tra cứu trực tiếp tài liệu.
  - Đóng gói cấu trúc PKCS#7 / CMS:
    - ContentType = `data`
    - MessageDigest = SHA-256 hash của PDF
    - SigningTime = thời điểm hiện tại
    - **Custom PKCS#9 Attribute**: OID `1.3.6.1.4.1.99999.1` (QuickSign Document Code) chứa chuỗi `document_code`.
  - Tạo ByteRange Incremental Update ở cuối file PDF.
* Thực hiện bóc tách và xác thực chữ ký PDF:
  - Đọc từ điển `/ByteRange` và `/Contents`.
  - Giải mã khối CMS PKCS#7, trích xuất Public Key, kiểm tra chữ ký và bóc tách `document_code`.

#### [NEW] `internal/adapters/outbound/jws_crypto/jws_adapter.go`
* Thực hiện ký JWS (RFC 7515):
  - Header: `alg` (`RS256` hoặc `ES256`), `typ: "JOSE"`, `jti: docCode`.
  - Ký payload bằng Private Key RSA hoặc ECDSA.
  - Tạo chuỗi định dạng Compact Serialization: `base64url(header).base64url(payload).base64url(sig)`.
* Thực hiện xác thực JWS:
  - Tách 3 phần, giải mã header, verify chữ ký với Public Key.

#### [NEW] `internal/adapters/outbound/postgres/pgx_repository.go`
* Kết nối PostgreSQL bằng pool kết nối `pgxpool.Pool`:
  - `GetActiveCertificate`: Truy vấn từ bảng `certificates`.
  - `SaveSignedDocument`: `INSERT INTO signed_documents ...`.
  - `GetSignedDocumentByCode`: `SELECT ... FROM signed_documents WHERE document_code = $1`.
  - `SaveVerificationLog`: `INSERT INTO verification_logs ...`.
  - `IncrementVerifyCount`: Thực hiện câu lệnh SQL nguyên tử:
    ```sql
    UPDATE signed_documents 
    SET total_verify_count = total_verify_count + 1,
        unique_verify_count = unique_verify_count + CASE WHEN $2 THEN 1 ELSE 0 END,
        tampered_alert_count = tampered_alert_count + CASE WHEN $3 THEN 1 ELSE 0 END,
        last_verified_at = NOW()
    WHERE id = $1;
    ```

#### [NEW] `internal/adapters/outbound/redis/redis_adapter.go`
* Kết nối Redis:
  - Cache thông tin API Key (TTL 15 phút).
  - Khóa chống spam tra cứu (`SET docCode:ip 1 EX 600 NX`) để xác định `isUnique` lượt xem.

---

### 5.5. Tầng Adapters: Inbound HTTP (Driving Adapters)
#### [NEW] `internal/adapters/inbound/http/auth_middleware.go`
* Xử lý xác thực request:
  - Kiểm tra `Authorization: Bearer <token>`: Giải mã JWT, đọc claim `sub` (User ID) và `roles`.
  - HOẶC kiểm tra `X-API-Key: <key>`: Băm SHA-256 chuỗi secret, kiểm tra trong Redis cache $\rightarrow$ nếu miss thì truy vấn PostgreSQL `api_keys`, kiểm tra scope `sign` hoặc `verify`.
  - Chuẩn hóa mã lỗi trả về: `UNAUTHORIZED` (401), `FORBIDDEN_ACCESS` (403), `API_KEY_SCOPE_INSUFFICIENT` (403).

#### [NEW] `internal/adapters/inbound/http/sign_handler.go`
* Xử lý endpoint `POST /api/v1/sign/pdf`:
  - Đọc multipart form (`file`, `certificateId`, `reason`, `location`).
  - Gọi `DocumentSigningUseCase.SignPdf`.
  - Trả về file PDF với `Content-Disposition: attachment; filename="..._signed.pdf"` và header `X-Document-Code`.
* Xử lý endpoint `POST /api/v1/sign/json`:
  - Đọc JSON payload body.
  - Gọi `DocumentSigningUseCase.SignJson`.
  - Trả về JSON chứa chuỗi JWS và `documentCode`.

#### [NEW] `internal/adapters/inbound/http/verify_handler.go`
* Xử lý endpoint `POST /api/v1/verify/file`:
  - Đọc file upload hoặc raw body JWS.
  - Gọi `DocumentVerificationUseCase.VerifyFile`.
  - Trả về JSON kết quả xác thực chuẩn hóa.
* Xử lý endpoint `GET /api/v1/verify/code/{documentCode}`:
  - Gọi `DocumentVerificationUseCase.VerifyByCode`.
  - Trả về JSON cảnh báo `CODE_EXISTS_UNVERIFIED_CONTENT`.

#### [NEW] `internal/adapters/inbound/http/router.go`
* Thiết lập router HTTP chuẩn (sử dụng `http.ServeMux` của Go 1.22+ hỗ trợ routing method và path param):
  - Áp dụng CORS middleware và Global Error Handling middleware theo cấu trúc JSON chuẩn hóa `SCREAMING_SNAKE_CASE`.
  - Gắn các route `/api/v1/sign/*` và `/api/v1/verify/*`.

#### [MODIFY] `cmd/server/main.go`
* Khởi tạo đồng thời:
  1. gRPC Server trên port `50051`.
  2. HTTP REST Server trên port `50052`.
* Lắng nghe tín hiệu dừng (`SIGINT`, `SIGTERM`) để graceful shutdown cả 2 server mà không làm rớt các kết nối đang xử lý.

---

## 6. Kế Hoạch Triển Khai Chi Tiết (Execution Checklist)

- [ ] **Bước 1: Cập nhật Dependencies & Cấu hình môi trường**
  - Cập nhật `go.mod` thêm `pgx/v5`, `go-redis/v9`, `jwt/v5`, `go-qrcode`.
  - Mở rộng `internal/config/config.go` hỗ trợ `HTTP_PORT`, `DATABASE_URL`, `REDIS_ADDR`.
  - Cập nhật `.env` và `.env.example`.
- [ ] **Bước 2: Xây dựng Domain Models & Interfaces (Ports)**
  - Tạo `internal/core/domain/signing.go` và `verification.go`.
  - Mở rộng `internal/core/ports/inbound.go` và `outbound.go`.
- [ ] **Bước 3: Xây dựng Adapters Outbound cho Cơ sở dữ liệu & Cache**
  - Tạo `pgx_repository.go` kết nối PostgreSQL (truy vấn certificates, lưu signed_documents, log verification, update count).
  - Tạo `redis_adapter.go` kết nối Redis (cache API keys, de-duplicate rate limit).
- [ ] **Bước 4: Xây dựng Adapters Mật mã học (PAdES PDF & JWS)**
  - Tạo `pades_adapter.go` thực thi ký số PAdES-BES (ISO 32000-1), nhúng QR Code và PKCS#9 Signed Attributes.
  - Tạo `jws_adapter.go` thực thi ký số và xác thực JWS (RFC 7515).
- [ ] **Bước 5: Triển khai Core Services**
  - Tạo `signing_service.go` điều phối lấy khóa từ Vault, ký PAdES/JWS, xóa RAM (zeroing), lưu DB.
  - Tạo `verification_service.go` bóc tách chữ ký, đối soát hash, kiểm tra cert, cập nhật bộ đếm nguyên tử.
- [ ] **Bước 6: Xây dựng Tầng Inbound HTTP REST API**
  - Tạo `auth_middleware.go` hỗ trợ JWT Bearer và `X-API-Key`.
  - Tạo `sign_handler.go`, `verify_handler.go`, `router.go`.
- [ ] **Bước 7: Cập nhật Entrypoint `cmd/server/main.go`**
  - Khởi tạo DI, chạy song song gRPC (`:50051`) và HTTP (`:50052`) với graceful shutdown.
- [ ] **Bước 8: Viết Unit Tests Toàn Diện (Go)**
  - Viết unit test cho `SigningService` và `VerificationService` với mock adapters.
  - Viết unit test cho `PadesAdapter` và `JwsAdapter`.
  - Chạy `go test ./tests/unit/... -v` đảm bảo 100% đỗ.
- [ ] **Bước 9: Viết Integration Tests (Kiểm thử thực tế với Vault, DB & HTTP)**
  - Viết integration test gọi trực tiếp HTTP endpoint: Ký PDF, ký JSON, verify file hợp lệ, verify file bị sửa nội dung (tampered), tra cứu mã QR.
  - Chạy `go test ./tests/integration/... -v`.
- [ ] **Bước 10: Nghiệm thu toàn diện & Cập nhật Walkthrough**
  - Biên dịch toàn bộ Go Crypto Engine (`go build ./cmd/server/`).
  - Kiểm tra chạy đồng thời cả .NET và Go.
  - Cập nhật `walkthrough.md` và lưu trữ kế hoạch.

---

## 7. Kế Hoạch Kiểm Thử & Nghiệm Thu (Verification Plan)

### 7.1. Kiểm Thử Tự Động (Automated Tests)
1. **Go Unit Tests:**
   ```powershell
   cd src/crypto-engine
   go test ./tests/unit/... -v
   ```
   *Kỳ vọng:* Toàn bộ các unit test của KeyGen, Signing, Verification, PAdES, JWS đều PASS.

2. **Go Integration Tests (với PostgreSQL, Redis, Vault thật):**
   ```powershell
   cd src/crypto-engine
   go test ./tests/integration/... -v
   ```
   *Kỳ vọng:* Thực hiện ký thật ra file PDF, bóc tách chữ ký đối soát toàn vẹn thành công. Khi thay đổi 1 byte trong file PDF, hàm verify phát hiện chính xác lỗi `TAMPERED_CONTENT`.

3. **.NET Solution Tests:**
   ```powershell
   dotnet test src/management/QuickSign.sln -c Release -m:1 --no-build
   ```
   *Kỳ vọng:* 31/31 unit tests hiện tại tiếp tục đỗ 100%.

### 7.2. Kiểm Thử Thủ Công (Manual Verification)
1. **Kiểm thử Ký file PDF qua cURL/Postman:**
   - Gửi yêu cầu ký file PDF mẫu qua `POST http://localhost:50052/api/v1/sign/pdf`.
   - Mở file PDF kết quả bằng **Adobe Acrobat Reader**: Chữ ký hợp lệ, hiển thị thông tin người ký và mã QR tra cứu ở chân trang.
2. **Kiểm thử Xác thực tính toàn vẹn:**
   - Tải file PDF vừa ký lên `POST http://localhost:50052/api/v1/verify/file` $\rightarrow$ Nhận `isValid: true`, `validationStatus: "VALID"`.
   - Dùng Text Editor sửa 1 ký tự bất kỳ trong file PDF đã ký rồi gửi lại $\rightarrow$ Nhận `isValid: false`, `validationStatus: "TAMPERED_CONTENT"`.
3. **Kiểm thử Quét mã QR tra cứu:**
   - Gọi `GET http://localhost:50052/api/v1/verify/code/{documentCode}` $\rightarrow$ Nhận mã trạng thái `CODE_EXISTS_UNVERIFIED_CONTENT` kèm hướng dẫn tải file gốc lên đối soát.
