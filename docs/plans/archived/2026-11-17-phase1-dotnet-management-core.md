# KẾ HOẠCH TRIỂN KHAI (IMPLEMENTATION PLAN): GIAI ĐOẠN 1 (BẢN CẬP NHẬT ĐIỀU CHỈNH)
## XÂY DỰNG NỀN TẢNG CƠ SỞ DỮ LIỆU & .NET MANAGEMENT SERVICE CORE

---

## 1. Metadata
* **Mã Kế Hoạch:** `PLAN-2026-11-17-PHASE1`
* **File Lưu Trữ:** `docs/plans/active/2026-11-17-phase1-dotnet-management-core.md`
* **Trạng Thái Hiện Tại:** `COMPLETED` *(Toàn bộ 9 projects & 2 test projects hoàn thành, 23/23 unit tests đạt PASS 100%)*
* **Target Framework:** `.NET 10.0` (`net10.0`)
* **Thư Mục Mã Nguồn .NET:** `src/management/`
* **File Solution Chính:** `src/management/QuickSign.sln` & `src/management/QuickSign.slnx`
* **Hạ Tầng:** Sử dụng cụm dịch vụ bên ngoài có sẵn (PostgreSQL 16, Redis 7, HashiCorp Vault). **Không tạo docker-compose.**
* **Các Tầng Chịu Ảnh Hưởng:** Toàn bộ 9 projects trong `src/management/` theo chuẩn ABP Layered Architecture & Tactical DDD.

---

## 2. Hiện Trạng Dự Án (Current Project Baseline)

1. **Hạ Tầng Cơ Sở:**
   * PostgreSQL, Redis và HashiCorp Vault đã cấu hình sẵn trên server riêng.
2. **Cấu Trúc Mã Nguồn .NET 10:**
   * Đã có sẵn 9 projects chuẩn ABP trong `src/management/` với đầy đủ NuGet packages và Project References.
   * File Solution `QuickSign.sln` và `QuickSign.slnx` đã được đặt trực tiếp tại `src/management/`.

---

## 3. Kiến Trúc Chi Tiết & Các Thành Phần Triển Khai

### 3.1. Phân Tách Interface & Implementation Theo Chuẩn ABP Layered

| Tên Thành Phần Nghiệp Vụ | Tên Interface | Project Đặt Interface | Tên Implementation | Project Đặt Implementation |
| :--- | :--- | :--- | :--- | :--- |
| **Domain Services** | `ICertificateManager` | `QuickSign.Domain` (`/Certificates`) | `CertificateManager` | `QuickSign.Domain` (`/Certificates`) |
| **Data Seeders** | `IDataSeedContributor` | `QuickSign.Domain` (`/Data`) | `QuickSignDbMigrationService` | `QuickSign.Domain` (`/Data`) |
| **Schema Migrator** | `IQuickSignDbSchemaMigrator` | `QuickSign.Domain` (`/Data`) | `EfCoreQuickSignDbSchemaMigrator` | `QuickSign.EntityFrameworkCore` (`/Migrations`) |
| **Repositories & UoW** | `IUnitOfWork`, `ICertificateRepository`, `IApiKeyRepository`, `ISignedDocumentRepository` | `QuickSign.Domain` (`/Repositories`) | `EfCoreUnitOfWork`, `EfCoreCertificateRepository`, `EfCoreApiKeyRepository`, `EfCoreSignedDocumentRepository` | `QuickSign.EntityFrameworkCore` (`/Repositories`) |
| **Application Services** | `IAuthAppService`, `ICertificateAppService`, `IApiKeyAppService`, `IDocumentAppService`, `IStatisticsAppService`, `IAdminAppService` | `QuickSign.Application.Contracts` (`/Services`) | `AuthAppService`, `CertificateAppService`, `ApiKeyAppService`, `DocumentAppService`, `StatisticsAppService`, `AdminAppService` | `QuickSign.Application` (`/Services`) |
| **QuickBite Identity Auth** | `IQuickBiteIdentityService` | `QuickSign.Application.Contracts` (`/Infrastructure`) | `QuickBiteIdentityService` | `QuickSign.Infrastructure` (`/Identity`) |
| **User Context** | `ICurrentUserService` | `QuickSign.Application.Contracts` (`/Infrastructure`) | `CurrentUserService` | `QuickSign.Infrastructure` (`/Identity`) |
| **Go Crypto Gateway** | `ICryptoEngineGrpcService` | `QuickSign.Application.Contracts` (`/Infrastructure`) | `CryptoEngineGrpcService` | `QuickSign.Infrastructure` (`/Cryptography`) |
| **Distributed Cache** | `ICacheService` | `QuickSign.Application.Contracts` (`/Infrastructure`) | `RedisCacheService` | `QuickSign.Infrastructure` (`/Caching`) |
| **API Key Security** | `IApiKeyCryptoService` | `QuickSign.Application.Contracts` (`/Infrastructure`) | `ApiKeyCryptoService` | `QuickSign.Infrastructure` (`/Security`) |

---

### 3.2. Cấu Trúc Thư Mục Chi Tiết Sẽ Tạo Trong `src/management/`

```
src/management/
├── QuickSign.sln
├── QuickSign.slnx
│
├── QuickSign.Domain.Shared/
│   ├── Certificates/
│   │   ├── CertificateConsts.cs                           # MaxActiveCertificatesPerUser = 5, độ dài chuỗi
│   │   ├── CertificateStatusType.cs                       # Active = 1, Expired = 2, Revoked = 3
│   │   └── KeyAlgorithmType.cs                            # Rsa2048 = 1, EcdsaP256 = 2
│   ├── ApiKeys/
│   │   └── ApiKeyConsts.cs                                # PrefixLength = 8, KeyBytes = 32
│   ├── Documents/
│   │   ├── DocumentType.cs                                # Pdf = 1, Json = 2
│   │   ├── VerificationMethodType.cs                      # WebUpload = 1, WebCode = 2, Api = 3
│   │   └── ValidationStatusType.cs                        # Valid, TamperedContent, CertRevoked, CertExpiredAtSigning, UnknownSignature, CodeExistsUnverifiedContent
│   └── QuickSignDomainErrorCodes.cs                       # SCREAMING_SNAKE_CASE (khớp 100% docs/error_codes.md)
│
├── QuickSign.Domain/
│   ├── SeedWork/
│   │   ├── Entity.cs, AggregateRoot.cs, ValueObject.cs
│   ├── Certificates/
│   │   ├── Certificate.cs                                 # Aggregate Root
│   │   ├── RevocationInfo.cs                              # Value Object
│   │   ├── ICertificateManager.cs                         # Domain Service Interface
│   │   └── CertificateManager.cs                          # Domain Service Implementation (kiểm tra giới hạn 5 certs active)
│   ├── ApiKeys/
│   │   ├── ApiKey.cs                                      # Aggregate Root
│   │   └── ApiKeySecretHash.cs                            # Value Object
│   ├── Documents/
│   │   ├── SignedDocument.cs                              # Aggregate Root
│   │   ├── VerificationLog.cs                             # Child Entity
│   │   ├── DocumentCode.cs                                # Value Object
│   │   └── Sha256Hash.cs                                  # Value Object
│   ├── Repositories/
│   │   ├── IUnitOfWork.cs
│   │   ├── ICertificateRepository.cs
│   │   ├── IApiKeyRepository.cs
│   │   └── ISignedDocumentRepository.cs
│   ├── Data/
│   │   ├── IQuickSignDbSchemaMigrator.cs
│   │   ├── IDataSeedContributor.cs
│   │   └── QuickSignDbMigrationService.cs
│   ├── Exceptions/
│   │   └── BusinessException.cs                           # Exception nghiệp vụ chứa ErrorCode & details
│   └── DependencyInjection.cs                             # AddDomainServices()
│
├── QuickSign.Application.Contracts/
│   ├── Permissions/
│   │   └── QuickSignPermissions.cs
│   ├── Services/
│   │   ├── IAuthAppService.cs
│   │   ├── ICertificateAppService.cs
│   │   ├── IApiKeyAppService.cs
│   │   ├── IDocumentAppService.cs
│   │   ├── IStatisticsAppService.cs
│   │   └── IAdminAppService.cs
│   ├── Infrastructure/
│   │   ├── IQuickBiteIdentityService.cs
│   │   ├── ICurrentUserService.cs
│   │   ├── ICryptoEngineGrpcService.cs
│   │   ├── ICacheService.cs
│   │   └── IApiKeyCryptoService.cs
│   └── DTOs/
│       ├── Auth/ (LoginRequestDto, RegisterRequestDto, AuthTokenResponseDto)
│       ├── Certificates/ (IssueCertificateRequestDto, RevokeCertificateRequestDto, CertificateResponseDto, GeneratedKeyMaterialDto)
│       ├── ApiKeys/ (CreateApiKeyRequestDto, ApiKeyCreatedResponseDto, ApiKeyItemDto)
│       └── Documents/ (SignedDocumentDto, VerificationLogDto, DashboardOverviewStatsDto)
│
├── QuickSign.Application/
│   ├── Services/
│   │   ├── AuthAppService.cs
│   │   ├── CertificateAppService.cs                       # Kèm Compensating Transaction gọi Go DeleteKey khi DB lỗi
│   │   ├── ApiKeyAppService.cs                            # Đồng bộ cache Redis khi tạo/xóa key
│   │   ├── DocumentAppService.cs                          # .AsNoTracking() + AutoMapper
│   │   ├── StatisticsAppService.cs
│   │   └── AdminAppService.cs
│   ├── Mappings/
│   │   └── QuickSignApplicationAutoMapperProfile.cs
│   ├── Validators/
│   │   ├── IssueCertificateRequestValidator.cs
│   │   ├── RevokeCertificateRequestValidator.cs
│   │   └── CreateApiKeyRequestValidator.cs
│   └── DependencyInjection.cs                             # AddApplicationServices()
│
├── QuickSign.EntityFrameworkCore/
│   ├── EntityFrameworkCore/
│   │   ├── QuickSignDbContext.cs                          # 4 DbSets
│   │   ├── QuickSignDbContextFactory.cs                   # Cho EF CLI Migration
│   │   └── EfCoreQuickSignDbSchemaMigrator.cs
│   ├── Configurations/
│   │   ├── CertificateConfiguration.cs                    # Fluent API (Indexes: user_id, unique serial_number)
│   │   ├── ApiKeyConfiguration.cs                         # Fluent API (Indexes: user_id, unique key_hash)
│   │   ├── SignedDocumentConfiguration.cs                 # Fluent API (Indexes: user_id, original_hash, signed_hash, unique document_code)
│   │   └── VerificationLogConfiguration.cs                # Fluent API (Indexes: signed_document_id, verified_at)
│   ├── Repositories/
│   │   ├── EfCoreUnitOfWork.cs
│   │   ├── EfCoreCertificateRepository.cs
│   │   ├── EfCoreApiKeyRepository.cs
│   │   └── EfCoreSignedDocumentRepository.cs
│   ├── Migrations/                                        # Migrations sinh bởi EF Core
│   └── DependencyInjection.cs                             # AddEntityFrameworkCoreServices()
│
├── QuickSign.Infrastructure/
│   ├── Identity/
│   │   ├── QuickBiteIdentityOptions.cs
│   │   ├── QuickBiteIdentityService.cs                    # BFF REST client gọi QuickBite Identity (password grant, refresh, register)
│   │   └── CurrentUserService.cs                          # Đọc claims sub/roles từ HttpContext
│   ├── Cryptography/
│   │   ├── Protos/crypto_engine.proto                     # GenerateCertificateKeyPair + DeleteCertificateKeyPair (ADR 0001)
│   │   └── CryptoEngineGrpcService.cs                     # gRPC Client + Polly Retry
│   ├── Caching/
│   │   └── RedisCacheService.cs                           # StackExchange.Redis wrapper
│   ├── Security/
│   │   └── ApiKeyCryptoService.cs                         # Cryptographic random generator, prefix 8, SHA-256 hash
│   └── DependencyInjection.cs                             # AddInfrastructureServices()
│
├── QuickSign.HttpApi/
│   ├── Controllers/
│   │   ├── QuickSignControllerBase.cs
│   │   ├── AuthController.cs                              # /api/v1/auth/login, register, refresh
│   │   ├── CertificatesController.cs                      # /api/v1/certificates
│   │   ├── ApiKeysController.cs                           # /api/v1/api-keys
│   │   ├── DocumentsController.cs                         # /api/v1/documents
│   │   ├── StatisticsController.cs                        # /api/v1/stats
│   │   └── AdminController.cs                             # /api/v1/admin
│   └── DependencyInjection.cs                             # AddHttpApiControllers()
│
├── QuickSign.DbMigrator/
│   ├── DbMigratorHostedService.cs
│   ├── appsettings.json
│   └── Program.cs
│
└── QuickSign.HttpApi.Host/
    ├── Middlewares/
    │   └── BusinessExceptionHandlingMiddleware.cs         # JSON format: { errorCode, httpStatus, timestamp, traceId, details }
    ├── appsettings.json
    ├── appsettings.Development.json                       # Kết nối server Postgres/Redis nội bộ của bạn
    └── Program.cs                                         # DI composition root & middleware pipeline
```

---

## 4. Danh Sách Công Việc Chi Tiết Cần Thực Thi (Execution Checklist)

### Bước 1: Cấu Hình Kết Nối Hạ Tầng Sẵn Có
- [x] Cấu hình Connection Strings cho PostgreSQL và Redis trong `QuickSign.HttpApi.Host/appsettings.Development.json` và `QuickSign.DbMigrator/appsettings.json`.
- [x] Cấu hình Endpoint và Token của Vault trong cấu hình môi trường phát triển.
- [x] *(Đã bỏ qua hoàn toàn bước tạo `docker-compose.yml` theo yêu cầu)*.

### Bước 2: Tầng Domain.Shared & Domain
- [x] Triển khai `QuickSign.Domain.Shared`:
  - [x] Enums: `CertificateStatusType`, `KeyAlgorithmType`, `DocumentType`, `VerificationMethodType`, `ValidationStatusType`.
  - [x] Hằng số: `CertificateConsts`, `ApiKeyConsts`.
  - [x] Hằng số mã lỗi: `QuickSignDomainErrorCodes.cs` (toàn bộ mã lỗi `SCREAMING_SNAKE_CASE` theo SSOT `docs/error_codes.md`).
- [x] Triển khai `QuickSign.Domain`:
  - [x] SeedWork: `Entity<T>`, `AggregateRoot<T>`, `ValueObject`.
  - [x] Aggregates & Value Objects: `Certificate`, `RevocationInfo`, `ApiKey`, `ApiKeySecretHash`, `SignedDocument`, `VerificationLog`, `DocumentCode`, `Sha256Hash`.
  - [x] Domain Service: `ICertificateManager` & `CertificateManager` (ràng buộc tối đa 5 certs active).
  - [x] Repositories & UoW: `IUnitOfWork`, `ICertificateRepository`, `IApiKeyRepository`, `ISignedDocumentRepository`.
  - [x] Data Seeder / Migrator Interfaces: `IQuickSignDbSchemaMigrator`, `IDataSeedContributor`, `QuickSignDbMigrationService`.
  - [x] `BusinessException.cs` và `DependencyInjection.cs`.

### Bước 3: Tầng EntityFrameworkCore & DbMigrator
- [x] Xây dựng `QuickSignDbContext` kế thừa `DbContext` với 4 `DbSet`.
- [x] Xây dựng 4 Fluent API configurations trong thư mục `Configurations/`:
  - [x] `CertificateConfiguration`: Table `certificates`, index `user_id`, unique `serial_number`.
  - [x] `ApiKeyConfiguration`: Table `api_keys`, index `user_id`, unique `key_hash`.
  - [x] `SignedDocumentConfiguration`: Table `signed_documents`, unique `document_code`, index `user_id`, `original_hash`, `signed_hash`.
  - [x] `VerificationLogConfiguration`: Table `verification_logs`, index `signed_document_id`, index `verified_at`.
- [x] Triển khai `EfCoreCertificateRepository`, `EfCoreApiKeyRepository`, `EfCoreSignedDocumentRepository`, `EfCoreUnitOfWork`.
- [x] Triển khai `EfCoreQuickSignDbSchemaMigrator` và `QuickSignDbContextFactory`.
- [x] Tạo EF Core Migration `InitialCreate`.
- [x] Chạy `QuickSign.DbMigrator` để khởi tạo cấu trúc bảng trên PostgreSQL server có sẵn.

### Bước 4: Tầng Application.Contracts & Infrastructure
- [x] `Application.Contracts`: Khai báo đầy đủ DTOs (Auth, Certificates, ApiKeys, Documents, Stats) và Interfaces (`IAuthAppService`, `ICertificateAppService`, `IApiKeyAppService`, `IDocumentAppService`, `IStatisticsAppService`, `IAdminAppService`).
- [x] `Infrastructure.Identity`:
  - [x] `QuickBiteIdentityService`: Triển khai BFF HTTP Client gọi QuickBite Identity (Login password grant, Register, Refresh Token).
  - [x] `CurrentUserService`: Lấy thông tin user hiện tại (`UserId`, `Roles`) từ `IHttpContextAccessor`.
- [x] `Infrastructure.Cryptography`:
  - [x] Tạo file proto `crypto_engine.proto` gồm 2 RPC: `GenerateCertificateKeyPair` và `DeleteCertificateKeyPair` (Compensating Transaction theo ADR 0001).
  - [x] Triển khai `CryptoEngineGrpcService` gọi sang Go Engine với Polly Retry Policy.
- [x] `Infrastructure.Caching`: Triển khai `RedisCacheService` kết nối `IConnectionMultiplexer`.
- [x] `Infrastructure.Security`: Triển khai `ApiKeyCryptoService` (sinh key ngẫu nhiên `qs_live_...`, tách prefix 8 ký tự, băm SHA-256).

### Bước 5: Tầng Application
- [x] Triển khai `QuickSignApplicationAutoMapperProfile`.
- [x] Triển khai FluentValidation validators cho các Request DTOs.
- [x] Triển khai `AuthAppService`: Điều hướng gọi `IQuickBiteIdentityService`.
- [x] Triển khai `CertificateAppService`:
  - [x] `IssueCertificateAsync`: Gọi gRPC sang Go $\rightarrow$ Tạo aggregate qua `ICertificateManager` $\rightarrow$ Lưu DB. **Nếu lưu DB ném exception $\rightarrow$ gọi compensating transaction `DeleteCertificateKeyPair` sang Go**.
  - [x] `RevokeCertificateAsync`: Kiểm tra quyền sở hữu, cập nhật trạng thái thu hồi, xóa cache Redis.
  - [x] `GetMyCertificatesAsync`: Sử dụng `.AsNoTracking()` và AutoMapper.
- [x] Triển khai `ApiKeyAppService`:
  - [x] Tạo key mới, băm SHA-256, lưu DB, đồng bộ Redis cache `qs_key:{hash}`.
  - [x] Xóa/thu hồi key và xóa cache Redis.
- [x] Triển khai `DocumentAppService` và `StatisticsAppService`: Sử dụng AutoMapper `.ProjectTo<TDto>()` để tối ưu câu lệnh SQL `SELECT`.

### Bước 6: Tầng HttpApi & HttpApi.Host
- [x] `HttpApi`: Xây dựng Controllers kế thừa `QuickSignControllerBase`:
  - [x] `AuthController` (`/api/v1/auth`)
  - [x] `CertificatesController` (`/api/v1/certificates`)
  - [x] `ApiKeysController` (`/api/v1/api-keys`)
  - [x] `DocumentsController` (`/api/v1/documents`)
  - [x] `StatisticsController` (`/api/v1/stats`)
  - [x] `AdminController` (`/api/v1/admin`)
- [x] `HttpApi.Host`:
  - [x] `BusinessExceptionHandlingMiddleware`: Bắt `BusinessException` và trả payload JSON chuẩn:
    `{"errorCode": "...", "httpStatus": ..., "timestamp": "...", "traceId": "..."}`.
  - [x] Cấu hình JWT Bearer Authentication trỏ về JWKS của QuickBite Identity.
  - [x] Cấu hình CORS cho cổng `localhost:3000` (Next.js).
  - [x] Cấu hình Swagger / OpenAPI hỗ trợ Bearer Token.
  - [x] Kết nối toàn bộ DI Container trong `Program.cs`.

### Bước 7: Kiểm Thử Tự Động & Pre-Handover Verification
- [x] Tạo project `test/QuickSign.Domain.Tests` và `test/QuickSign.Application.Tests`.
- [x] Viết Unit Test cho Domain Service `CertificateManager` (quy tắc 5 certs active, ngày thu hồi hợp lệ, chặn thu hồi lặp lại).
- [x] Viết Unit Test cho `ApiKeyCryptoService` (prefix 8 ký tự, SHA-256 hash).
- [x] Viết Unit Test cho `CertificateAppService` (Compensating Transaction kích hoạt khi DB lỗi).
- [x] Chạy kiểm tra biên dịch Release và thực thi test suite:
  ```bash
  dotnet test src/management/QuickSign.sln
  ```
  *(Kết quả: 23/23 tests Passed, 0 Failed, 0 Skipped).*

---

## 5. Ma Trận Kiểm Thử Nghiệm Thu (Test Verification Matrix)

| Test ID | Module | Tình Huống Kiểm Thử | Điều Kiện Đầu Vào | Kết Quả Mong Đợi | Trạng Thái |
| :--- | :--- | :--- | :--- | :--- | :---: |
| `TC-DOM-01` | Domain Service | Giới hạn 5 chứng thư active | User đã có 5 certs `Active`, gọi `CreateAsync` | Ném `BusinessException` mã `MAX_ACTIVE_CERTS_REACHED` | **PASS (100%)** |
| `TC-DOM-02` | Domain Entity | Thu hồi chứng thư hợp lệ | Cert đang active, ngày vô hiệu hóa $\ge$ `CreatedAt` | Trạng thái = `Revoked`, gán đúng `RevocationInfo` | **PASS (100%)** |
| `TC-DOM-03` | Domain Entity | Thu hồi với ngày không hợp lệ | `InvalidatedFrom < CreatedAt` | Ném `BusinessException` mã `INVALID_REVOCATION_TIMESTAMP` | **PASS (100%)** |
| `TC-DOM-04` | Domain Entity | Thu hồi chứng thư đã thu hồi | Cert có trạng thái `Revoked` | Ném `BusinessException` mã `CERT_ALREADY_REVOKED` | **PASS (100%)** |
| `TC-APP-01` | Application Service | Giao dịch bù trừ (Compensating Tx) | Go trả key thành công nhưng DB Save ném ngoại lệ | Bắt lỗi, kích hoạt gọi gRPC `DeleteCertificateKeyPair` sang Go | **PASS (100%)** |
| `TC-APP-02` | Application Service | Thu hồi chứng thư của người khác | User A thu hồi cert của User B (không có role Admin) | Ném `BusinessException` mã `UNAUTHORIZED_CERTIFICATE_ACCESS` | **PASS (100%)** |
| `TC-INF-01` | Infrastructure | Sinh và băm API Key | Gọi `GenerateApiKey()` | Key có prefix `qs_live_`, hash SHA-256 đúng 64 ký tự hex | **PASS (100%)** |
| `TC-MID-01` | Middleware | Chuẩn hóa lỗi JSON | Controller ném `BusinessException` | Response mã HTTP tương ứng và body có `errorCode` đúng chuẩn | **PASS (100%)** |
