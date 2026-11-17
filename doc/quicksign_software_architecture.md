# TÀI LIỆU KIẾN TRÚC PHẦN MỀM: HỆ THỐNG CHỮ KÝ SỐ (QUICKSIGN)

## 1. Tổng quan Kiến trúc Hệ thống

Hệ thống **QuickSign** gồm 3 thành phần ứng dụng cốt lõi hoạt động độc lập:

1. **QuickSign Web (`Next.js`):** Cung cấp giao diện người dùng (UI) cho các tác vụ đăng nhập/đăng ký, quản lý chứng thư số, quản lý API Key, ký số trực tiếp, tra cứu xác thực tài liệu công khai và xem thống kê lịch sử xác nhận.
2. **QuickSign Management Service (`.NET`):** Thiết kế theo cấu trúc phân tầng đa dự án của **ABP Layered Architecture** kết hợp **Tactical DDD**. Đảm nhận toàn bộ nghiệp vụ quản trị, tích hợp xác thực với QuickBite Identity (tại tầng `Infrastructure`), phân quyền (`User`/`Admin`), quản lý vòng đời Chứng thư số, cấp phát API Key, thực thi Migration độc lập (`DbMigrator`) và cung cấp dữ liệu thống kê qua AutoMapper.
3. **QuickSign Crypto Engine (`Golang`):** Thiết kế theo **Hexagonal Architecture (Ports & Adapters)**. Đảm nhận 100% các thuật toán mật mã học bao gồm khởi tạo cặp khóa ($\text{RSA-2048}$, $\text{ECDSA-P256}$), giao tiếp với HashiCorp Vault, nhúng chữ ký số vào PDF (chuẩn PAdES) / JSON (chuẩn JWS) và xác thực tính toàn vẹn của tài liệu.

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                              FRONTEND: NEXT.JS APPLICATION                             │
│  - Giao diện Đăng nhập/Đăng ký, Quản lý Chứng thư số, API Key, Dashboard Thống kê      │
│  - Giao diện Ký số thủ công (PDF/JSON) & Trang Xác thực Tài liệu công khai             │
└──────────────────┬──────────────────────────────────────────────────┬──────────────────┘
                   │                                                  │
   1. REST API     │ (Auth, Certificates, API Keys,                   │ 2. REST API (File Stream)
   (JSON Payload)  │  Documents Metadata, Analytics)                  │    (Ký PDF/JSON & Xác thực)
                   ▼                                                  ▼
┌──────────────────────────────────────────┐       ┌─────────────────────────────────────┐
│          .NET MANAGEMENT SERVICE         │       │          GO CRYPTO ENGINE           │
│   (ABP Layered Structure + Strict DDD)   │       │       (Hexagonal Architecture)      │
│                                          │       │                                     │
│  ┌────────────────────────────────────┐  │       │  ┌───────────────────────────────┐  │
│  │ QuickSign.HttpApi.Host             │  │       │  │ Inbound Adapters (HTTP, gRPC) │  │
│  │ └── QuickSign.HttpApi (Controllers)│  │       │  └───────────────┬───────────────┘  │
│  └─────────────────┬──────────────────┘  │       │                  ▼                  │
│                    ▼                     │       │  ┌───────────────────────────────┐  │
│  ┌────────────────────────────────────┐  │       │  │ Core Domain & Crypto Services │  │
│  │ QuickSign.Application.Contracts    │  │       │  │ (Sign, Verify, KeyGen, Ports) │  │
│  │ ▲               ▲                  │  │       │  └───────┬───────────────┬───────┘  │
│  │ │               └───┐              │  │       │          ▼               ▼          │
│  │ │QuickSign.         │QuickSign.    │  │ gRPC  │  ┌───────────────┐ ┌─────────────┐  │
│  │ │Application        │Infrastructure├──┼───────┼─►│ Outbound      │ │ Vault &     │  │
│  └─┼───────────┬───────┴──────────────┘  │       │  │ Adapters      │ │ Local File  │  │
│    ▼           ▼                         │       │  │ (pgx, Redis,  │ │ Adapters    │  │
│  ┌─────────────────┐ ┌────────────────┐  │       │  │  PAdES, JWS)  │ └──────┬──────┘  │
│  │QuickSign.Domain │◄┤QuickSign.      │  │       │  └───────┬───────┘        │         │
│  │└── Domain.Shared│ │EntityFramework │  │       │          │                │         │
│  └─────────────────┘ └───────┬────────┘  │       │          │                │         │
│  ┌───────────────────────────┤           │       │          │                │         │
│  │QuickSign.DbMigrator       │           │       │          │                │         │
└──┴───────────────────────────┼───────────┘       └──────────┼────────────────┼─────────┘
                               │                              │                │
                               ▼                              ▼                ▼
                        ┌────────────────────────────────────────────┐  ┌─────────────┐
                        │      PostgreSQL Database & Redis Cache     │  │  HashiCorp  │
                        └────────────────────────────────────────────┘  │    Vault    │
                                                                        └─────────────┘
```

---

## 2. Kiến trúc Chi tiết Dịch vụ .NET (`QuickSign.Management`)

Dịch vụ **.NET** được chia tách thành **9 Projects** riêng biệt dựa trên chuẩn **ABP Layered Solution Structure**. Việc chia nhỏ này đảm bảo `HttpApi` hoàn toàn không biết đến `Application` hay `Domain`, và `Application` hoàn toàn độc lập với `EntityFrameworkCore` cũng như `Infrastructure`.

### 2.1. Sơ đồ Phụ thuộc giữa các Project trong Solution (.NET)

| Tên Project (Layer) | Các Project được phép tham chiếu (`ProjectReference`) | Vai trò cốt lõi trong kiến trúc |
| :--- | :--- | :--- |
| **`QuickSign.Domain.Shared`** | *Không phụ thuộc bất kỳ project nào* | Chứa Enums, Constants, mã lỗi (`DomainErrorCodes`) dùng chung cho toàn bộ Solution (cả `Domain` và `Application.Contracts`). |
| **`QuickSign.Domain`** | `QuickSign.Domain.Shared` | Chứa Aggregate Roots, Entities, Value Objects, Domain Services (Interface + Impl), Repository Interfaces và Data Seeder Interfaces. |
| **`QuickSign.Application.Contracts`** | `QuickSign.Domain.Shared` | Chứa DTOs, Interfaces của Application Services, Interfaces của Infrastructure Services (QuickBite Auth, gRPC, Cache) và định nghĩa Permissions. |
| **`QuickSign.Application`** | `QuickSign.Domain`, `QuickSign.Application.Contracts` | Chứa Implementations của Application Services, cấu hình AutoMapper Profiles và FluentValidation. |
| **`QuickSign.EntityFrameworkCore`** | `QuickSign.Domain` | Chứa `QuickSignDbContext`, Fluent API Configurations, EF Core Migrations và Implementations của Repositories / UnitOfWork. |
| **`QuickSign.Infrastructure`** | `QuickSign.Domain`, `QuickSign.Application.Contracts` | Chứa Implementations của các dịch vụ tích hợp bên ngoài: Đăng nhập/Đăng ký QuickBite Identity, gRPC Client gọi Golang, Redis Cache, băm API Key. |
| **`QuickSign.HttpApi`** | `QuickSign.Application.Contracts` | Chứa toàn bộ `ApiController`. Chỉ gọi vào các Interface trong `Application.Contracts`, không phụ thuộc vào `Application` hay `Domain`. |
| **`QuickSign.DbMigrator`** | `QuickSign.Domain`, `QuickSign.EntityFrameworkCore` | Ứng dụng Console độc lập chuyên thực thi EF Core Database Migrations và Seed dữ liệu ban đầu (như cấu hình Root CA). |
| **`QuickSign.HttpApi.Host`** | `Application`, `EntityFrameworkCore`, `Infrastructure`, `HttpApi` | Project thực thi chính (Web Host): Cấu hình Dependency Injection (kết nối mọi Interface với Implementation), JWT Middleware và khởi chạy Server. |

### 2.2. Quy tắc Đặt Vị trí `Interface` và `Implementation` theo Chuẩn ABP Layered

| Phân loại Thành phần | Tên Interface | Project đặt Interface | Tên Implementation | Project đặt Implementation |
| :--- | :--- | :--- | :--- | :--- |
| **Domain Services (Managers)** | `ICertificateManager` | `QuickSign.Domain` (`/Certificates`) | `CertificateManager` | `QuickSign.Domain` (`/Certificates`) |
| **Domain Data Seeders** | `IDataSeedContributor` | `QuickSign.Domain` (`/Data`) | `QuickSignDbMigrationService` | `QuickSign.Domain` (`/Data`) |
| **Database Schema Migrator** | `IQuickSignDbSchemaMigrator` | `QuickSign.Domain` (`/Data`) | `EfCoreQuickSignDbSchemaMigrator` | `QuickSign.EntityFrameworkCore` (`/Migrations`) |
| **Repositories & UnitOfWork** | `ICertificateRepository`, `IApiKeyRepository`, `ISignedDocumentRepository`, `IUnitOfWork` | `QuickSign.Domain` (`/Repositories`) | `EfCoreCertificateRepository`, `EfCoreApiKeyRepository`, `EfCoreSignedDocumentRepository`, `EfCoreUnitOfWork` | `QuickSign.EntityFrameworkCore` (`/Repositories`) |
| **Application Services** | `IAuthAppService`, `ICertificateAppService`, `IApiKeyAppService`, `IDocumentAppService`, `IStatisticsAppService`, `IAdminAppService` | `QuickSign.Application.Contracts` (`/Services`) | `AuthAppService`, `CertificateAppService`, `ApiKeyAppService`, `DocumentAppService`, `StatisticsAppService`, `AdminAppService` | `QuickSign.Application` (`/Services`) |
| **QuickBite Identity Auth** | `IQuickBiteIdentityService` | `QuickSign.Application.Contracts` (`/Infrastructure`) | `QuickBiteIdentityService` | `QuickSign.Infrastructure` (`/Identity`) |
| **Current User Context** | `ICurrentUserService` | `QuickSign.Application.Contracts` (`/Infrastructure`) | `CurrentUserService` | `QuickSign.Infrastructure` (`/Identity`) |
| **Golang gRPC Gateway** | `ICryptoEngineGrpcService` | `QuickSign.Application.Contracts` (`/Infrastructure`) | `CryptoEngineGrpcService` | `QuickSign.Infrastructure` (`/Cryptography`) |
| **Distributed Cache** | `ICacheService` | `QuickSign.Application.Contracts` (`/Infrastructure`) | `RedisCacheService` | `QuickSign.Infrastructure` (`/Caching`) |
| **API Key Security** | `IApiKeyCryptoService` | `QuickSign.Application.Contracts` (`/Infrastructure`) | `ApiKeyCryptoService` | `QuickSign.Infrastructure` (`/Security`) |

### 2.3. Cấu trúc Thư mục Solution (.NET)

```
src/dotnet-management/
├── QuickSign.sln
└── src/
    ├── QuickSign.Domain.Shared/                            # 1. DOMAIN.SHARED (Hằng số, Enums, Mã lỗi dùng chung)
    │   ├── Certificates/
    │   │   ├── CertificateConsts.cs
    │   │   ├── CertificateStatusType.cs                    # Enum: Active, Expired, Revoked
    │   │   └── KeyAlgorithmType.cs                         # Enum: Rsa2048, EcdsaP256
    │   ├── ApiKeys/
    │   │   └── ApiKeyConsts.cs
    │   ├── Documents/
    │   │   ├── DocumentType.cs                             # Enum: Pdf, Json
    │   │   ├── VerificationMethodType.cs                   # Enum: WebUpload, WebCode, Api
    │   │   └── ValidationStatusType.cs                     # Enum: Valid, TamperedContent, CertRevoked...
    │   └── QuickSignDomainErrorCodes.cs
    │
    ├── QuickSign.Domain/                                   # 2. DOMAIN (Aggregates, Value Objects, Domain Services, Repo Interfaces)
    │   ├── SeedWork/
    │   │   ├── Entity.cs
    │   │   ├── AggregateRoot.cs
    │   │   └── ValueObject.cs
    │   ├── Certificates/
    │   │   ├── Certificate.cs                              # Aggregate Root
    │   │   ├── RevocationInfo.cs                           # Value Object
    │   │   ├── ICertificateManager.cs                      # Domain Service Interface
    │   │   └── CertificateManager.cs                       # Domain Service Implementation
    │   ├── ApiKeys/
    │   │   ├── ApiKey.cs                                   # Aggregate Root
    │   │   └── ApiKeySecretHash.cs                         # Value Object
    │   ├── Documents/
    │   │   ├── SignedDocument.cs                           # Aggregate Root
    │   │   ├── VerificationLog.cs                          # Child Entity
    │   │   ├── DocumentCode.cs                             # Value Object
    │   │   └── Sha256Hash.cs                               # Value Object
    │   ├── Repositories/                                   # REPOSITORY & UOW INTERFACES
    │   │   ├── IUnitOfWork.cs
    │   │   ├── ICertificateRepository.cs
    │   │   ├── IApiKeyRepository.cs
    │   │   └── ISignedDocumentRepository.cs
    │   ├── Data/                                           # INTERFACES & LOGIC CHO DBMIGRATOR
    │   │   ├── IQuickSignDbSchemaMigrator.cs
    │   │   ├── IDataSeedContributor.cs
    │   │   └── QuickSignDbMigrationService.cs
    │   ├── Exceptions/
    │   │   └── BusinessException.cs
    │   └── DependencyInjection.cs
    │
    ├── QuickSign.Application.Contracts/                    # 3. APPLICATION.CONTRACTS (DTOs & Service Interfaces)
    │   ├── Permissions/
    │   │   └── QuickSignPermissions.cs
    │   ├── Services/                                       # APPLICATION SERVICE INTERFACES
    │   │   ├── IAuthAppService.cs
    │   │   ├── ICertificateAppService.cs
    │   │   ├── IApiKeyAppService.cs
    │   │   ├── IDocumentAppService.cs
    │   │   ├── IStatisticsAppService.cs
    │   │   └── IAdminAppService.cs
    │   ├── Infrastructure/                                 # INFRASTRUCTURE SERVICE INTERFACES
    │   │   ├── IQuickBiteIdentityService.cs
    │   │   ├── ICurrentUserService.cs
    │   │   ├── ICryptoEngineGrpcService.cs
    │   │   ├── ICacheService.cs
    │   │   └── IApiKeyCryptoService.cs
    │   └── DTOs/
    │       ├── Auth/
    │       │   ├── LoginRequestDto.cs
    │       │   ├── RegisterRequestDto.cs
    │       │   └── AuthTokenResponseDto.cs
    │       ├── Certificates/
    │       │   ├── IssueCertificateRequestDto.cs
    │       │   ├── RevokeCertificateRequestDto.cs
    │       │   ├── GeneratedKeyMaterialDto.cs
    │       │   └── CertificateResponseDto.cs
    │       ├── ApiKeys/
    │       │   ├── CreateApiKeyRequestDto.cs
    │       │   ├── ApiKeyCreatedResponseDto.cs
    │       │   └── ApiKeyItemDto.cs
    │       └── Documents/
    │           ├── SignedDocumentDto.cs
    │           ├── VerificationLogDto.cs
    │           └── DashboardOverviewStatsDto.cs
    │
    ├── QuickSign.Application/                              # 4. APPLICATION (Application Service Implementations & AutoMapper)
    │   ├── Services/                                       # APPLICATION SERVICE IMPLEMENTATIONS
    │   │   ├── AuthAppService.cs
    │   │   ├── CertificateAppService.cs
    │   │   ├── ApiKeyAppService.cs
    │   │   ├── DocumentAppService.cs
    │   │   ├── StatisticsAppService.cs
    │   │   └── AdminAppService.cs
    │   ├── Mappings/                                       # AUTOMAPPER PROFILES
    │   │   └── QuickSignApplicationAutoMapperProfile.cs
    │   ├── Validators/                                     # FluentValidation
    │   └── DependencyInjection.cs
    │
    ├── QuickSign.EntityFrameworkCore/                      # 5. ENTITYFRAMEWORKCORE (DbContext, Configurations, Repo Impls)
    │   ├── EntityFrameworkCore/
    │   │   ├── QuickSignDbContext.cs
    │   │   ├── QuickSignDbContextFactory.cs                # Hỗ trợ CLI tạo Migration
    │   │   └── EfCoreQuickSignDbSchemaMigrator.cs          # Thực thi IQuickSignDbSchemaMigrator
    │   ├── Configurations/                                 # IEntityTypeConfiguration (Fluent API)
    │   │   ├── CertificateConfiguration.cs
    │   │   ├── ApiKeyConfiguration.cs
    │   │   ├── SignedDocumentConfiguration.cs
    │   │   └── VerificationLogConfiguration.cs
    │   ├── Repositories/                                   # REPOSITORY & UOW IMPLEMENTATIONS
    │   │   ├── EfCoreUnitOfWork.cs
    │   │   ├── EfCoreCertificateRepository.cs
    │   │   ├── EfCoreApiKeyRepository.cs
    │   │   └── EfCoreSignedDocumentRepository.cs
    │   ├── Migrations/
    │   └── DependencyInjection.cs
    │
    ├── QuickSign.Infrastructure/                           # 6. INFRASTRUCTURE (QuickBite Auth, gRPC, Redis, Security Impls)
    │   ├── Identity/
    │   │   ├── QuickBiteIdentityOptions.cs
    │   │   ├── QuickBiteIdentityService.cs                 # Thực thi IQuickBiteIdentityService (Đăng nhập/Đăng ký)
    │   │   └── CurrentUserService.cs                       # Thực thi ICurrentUserService
    │   ├── Cryptography/
    │   │   ├── Protos/crypto_engine.proto
    │   │   └── CryptoEngineGrpcService.cs                  # Thực thi ICryptoEngineGrpcService
    │   ├── Caching/
    │   │   └── RedisCacheService.cs                        # Thực thi ICacheService
    │   ├── Security/
    │   │   └── ApiKeyCryptoService.cs                      # Thực thi IApiKeyCryptoService
    │   └── DependencyInjection.cs
    │
    ├── QuickSign.HttpApi/                                  # 7. HTTPAPI (Controllers - Chỉ phụ thuộc Application.Contracts)
    │   ├── Controllers/
    │   │   ├── QuickSignControllerBase.cs
    │   │   ├── AuthController.cs
    │   │   ├── CertificatesController.cs
    │   │   ├── ApiKeysController.cs
    │   │   ├── DocumentsController.cs
    │   │   ├── StatisticsController.cs
    │   │   └── AdminController.cs
    │   └── DependencyInjection.cs
    │
    ├── QuickSign.DbMigrator/                               # 8. DBMIGRATOR (Console App chạy Migration & Seeding độc lập)
    │   ├── DbMigratorHostedService.cs
    │   ├── appsettings.json
    │   └── Program.cs
    │
    └── QuickSign.HttpApi.Host/                             # 9. HTTPAPI.HOST (Startup Project, DI Wiring, Middlewares)
        ├── Middlewares/
        │   └── BusinessExceptionHandlingMiddleware.cs
        ├── appsettings.json
        └── Program.cs
```

---

### 2.4. Thiết kế Chi tiết Từng Project trong .NET

#### 1. Tầng `QuickSign.Domain.Shared`
Chứa các kiểu liệt kê (Enums), hằng số giới hạn độ dài trường dữ liệu và mã lỗi nghiệp vụ để dùng chung cho cả `Domain` và `Application.Contracts` mà không gây tham chiếu vòng.

```csharp
// File: QuickSign.Domain.Shared/Certificates/CertificateConsts.cs
namespace QuickSign.Domain.Shared.Certificates;

public static class CertificateConsts
{
    public const int MaxSubjectNameLength = 255;
    public const int MaxSerialNumberLength = 100;
    public const int MaxVaultPathLength = 255;
    public const int MaxActiveCertificatesPerUser = 5;
}

// File: QuickSign.Domain.Shared/Certificates/CertificateStatusType.cs
namespace QuickSign.Domain.Shared.Certificates;

public enum CertificateStatusType
{
    Active = 1,
    Expired = 2,
    Revoked = 3
}

// File: QuickSign.Domain.Shared/QuickSignDomainErrorCodes.cs
namespace QuickSign.Domain.Shared;

public static class QuickSignDomainErrorCodes
{
    public const string MaxActiveCertificatesReached = "QuickSign:Certificate:0001";
    public const string CertificateAlreadyRevoked = "QuickSign:Certificate:0002";
    public const string InvalidRevocationTimestamp = "QuickSign:Certificate:0003";
    public const string UnauthorizedCertificateAccess = "QuickSign:Certificate:0004";
}
```

#### 2. Tầng `QuickSign.Domain`
Chỉ phụ thuộc vào `QuickSign.Domain.Shared`. Chứa các Aggregate Roots, Value Objects, Domain Services (`ICertificateManager` / `CertificateManager`) và Interfaces của Repositories.

```csharp
// File: QuickSign.Domain/Certificates/Certificate.cs
namespace QuickSign.Domain.Certificates;

public sealed class Certificate : AggregateRoot<Guid>
{
    public string UserId { get; private set; } = default!;
    public string SubjectName { get; private set; } = default!;
    public string SerialNumber { get; private set; } = default!;
    public KeyAlgorithmType Algorithm { get; private set; }
    public string PublicKeyPem { get; private set; } = default!;
    public string VaultKeyPath { get; private set; } = default!;
    public CertificateStatusType Status { get; private set; }
    public RevocationInfo? Revocation { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Certificate() { } // Dành cho EF Core

    internal Certificate(
        Guid id,
        string userId,
        string subjectName,
        string serialNumber,
        KeyAlgorithmType algorithm,
        string publicKeyPem,
        string vaultKeyPath,
        DateTimeOffset expiresAt)
    {
        Id = id;
        UserId = userId;
        SubjectName = subjectName.Trim();
        SerialNumber = serialNumber;
        Algorithm = algorithm;
        PublicKeyPem = publicKeyPem;
        VaultKeyPath = vaultKeyPath;
        Status = CertificateStatusType.Active;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
    }

    public void Revoke(DateTimeOffset invalidatedFrom, string reason)
    {
        if (Status == CertificateStatusType.Revoked)
            throw new BusinessException(QuickSignDomainErrorCodes.CertificateAlreadyRevoked, "Chứng thư số đã bị thu hồi.");

        if (invalidatedFrom < CreatedAt)
            throw new BusinessException(QuickSignDomainErrorCodes.InvalidRevocationTimestamp, "Thời điểm vô hiệu hóa không hợp lệ.");

        Status = CertificateStatusType.Revoked;
        Revocation = new RevocationInfo(DateTimeOffset.UtcNow, invalidatedFrom, reason);
    }
}

// File: QuickSign.Domain/Certificates/ICertificateManager.cs
namespace QuickSign.Domain.Certificates;

public interface ICertificateManager
{
    Task<Certificate> CreateAsync(
        Guid id,
        string userId,
        string subjectName,
        string serialNumber,
        KeyAlgorithmType algorithm,
        string publicKeyPem,
        string vaultKeyPath,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);
}

// File: QuickSign.Domain/Certificates/CertificateManager.cs
namespace QuickSign.Domain.Certificates;

public sealed class CertificateManager : ICertificateManager
{
    private readonly ICertificateRepository _certificateRepository;

    public CertificateManager(ICertificateRepository certificateRepository)
    {
        _certificateRepository = certificateRepository;
    }

    public async Task<Certificate> CreateAsync(
        Guid id,
        string userId,
        string subjectName,
        string serialNumber,
        KeyAlgorithmType algorithm,
        string publicKeyPem,
        string vaultKeyPath,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        var activeCount = await _certificateRepository.CountActiveByUserIdAsync(userId, cancellationToken);
        if (activeCount >= CertificateConsts.MaxActiveCertificatesPerUser)
        {
            throw new BusinessException(
                QuickSignDomainErrorCodes.MaxActiveCertificatesReached,
                $"Mỗi tài khoản chỉ được phép duy trì tối đa {CertificateConsts.MaxActiveCertificatesPerUser} chứng thư số đang hoạt động.");
        }

        return new Certificate(id, userId, subjectName, serialNumber, algorithm, publicKeyPem, vaultKeyPath, expiresAt);
    }
}
```

#### 3. Tầng `QuickSign.Application.Contracts`
Chỉ phụ thuộc vào `QuickSign.Domain.Shared`. Định nghĩa toàn bộ DTOs, Interfaces của Application Services (để `HttpApi` gọi) và Interfaces của Infrastructure Services (để `Infrastructure` thực thi).

```csharp
// File: QuickSign.Application.Contracts/Infrastructure/IQuickBiteIdentityService.cs
namespace QuickSign.Application.Contracts.Infrastructure;

public interface IQuickBiteIdentityService
{
    Task<AuthTokenResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthTokenResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}

// File: QuickSign.Application.Contracts/Services/IAuthAppService.cs
namespace QuickSign.Application.Contracts.Services;

public interface IAuthAppService
{
    Task<AuthTokenResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthTokenResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}

// File: QuickSign.Application.Contracts/Services/ICertificateAppService.cs
namespace QuickSign.Application.Contracts.Services;

public interface ICertificateAppService
{
    Task<CertificateResponseDto> IssueCertificateAsync(IssueCertificateRequestDto request, CancellationToken cancellationToken = default);
    Task RevokeCertificateAsync(Guid certificateId, RevokeCertificateRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CertificateResponseDto>> GetMyCertificatesAsync(CancellationToken cancellationToken = default);
}
```

#### 4. Tầng `QuickSign.Application`
Phụ thuộc vào `QuickSign.Domain` và `QuickSign.Application.Contracts`. Chứa phần thực thi (`Implementation`) của các Application Services và cấu hình AutoMapper. Hoàn toàn không phụ thuộc vào `EntityFrameworkCore` hay `Infrastructure`.

```csharp
// File: QuickSign.Application/Services/AuthAppService.cs
namespace QuickSign.Application.Services;

public sealed class AuthAppService : IAuthAppService
{
    private readonly IQuickBiteIdentityService _quickBiteIdentityService;

    public AuthAppService(IQuickBiteIdentityService quickBiteIdentityService)
    {
        _quickBiteIdentityService = quickBiteIdentityService;
    }

    public Task<AuthTokenResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
        => _quickBiteIdentityService.LoginAsync(request, cancellationToken);

    public Task RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
        => _quickBiteIdentityService.RegisterAsync(request, cancellationToken);

    public Task<AuthTokenResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        => _quickBiteIdentityService.RefreshTokenAsync(refreshToken, cancellationToken);
}

// File: QuickSign.Application/Services/CertificateAppService.cs
namespace QuickSign.Application.Services;

public sealed class CertificateAppService : ICertificateAppService
{
    private readonly ICertificateManager _certificateManager;
    private readonly ICertificateRepository _certificateRepository;
    private readonly ICryptoEngineGrpcService _cryptoEngineGrpcService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CertificateAppService(
        ICertificateManager certificateManager,
        ICertificateRepository certificateRepository,
        ICryptoEngineGrpcService cryptoEngineGrpcService,
        ICurrentUserService currentUserService,
        ICacheService cacheService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _certificateManager = certificateManager;
        _certificateRepository = certificateRepository;
        _cryptoEngineGrpcService = cryptoEngineGrpcService;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<CertificateResponseDto> IssueCertificateAsync(IssueCertificateRequestDto request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var certId = Guid.NewGuid();
        var validFrom = DateTimeOffset.UtcNow;
        var expiresAt = validFrom.AddDays(request.ValidityDays);

        // 1. Gọi gRPC sang Golang thông qua Interface hạ tầng
        var keyMaterial = await _cryptoEngineGrpcService.GenerateCertificateKeyPairAsync(
            certId, userId, request.SubjectName, request.Algorithm.ToString(), validFrom, expiresAt, cancellationToken);

        // 2. Khởi tạo Aggregate thông qua Domain Service (ICertificateManager)
        var certificate = await _certificateManager.CreateAsync(
            certId,
            userId,
            request.SubjectName,
            keyMaterial.SerialNumber,
            request.Algorithm,
            keyMaterial.PublicKeyPem,
            keyMaterial.VaultKeyPath,
            expiresAt,
            cancellationToken);

        // 3. Lưu trữ thông qua Repository Interface & UnitOfWork
        await _certificateRepository.InsertAsync(certificate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CertificateResponseDto>(certificate);
    }

    public async Task RevokeCertificateAsync(Guid certificateId, RevokeCertificateRequestDto request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var certificate = await _certificateRepository.GetByIdAsync(certificateId, cancellationToken)
            ?? throw new BusinessException("QuickSign:Certificate:NotFound", "Không tìm thấy chứng thư số.");

        if (certificate.UserId != userId && !_currentUserService.IsInRole("Admin"))
            throw new BusinessException(QuickSignDomainErrorCodes.UnauthorizedCertificateAccess, "Không có quyền thu hồi chứng thư này.");

        certificate.Revoke(request.InvalidatedFrom ?? DateTimeOffset.UtcNow, request.Reason);

        _certificateRepository.Update(certificate);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync($"cert:{certificateId}", cancellationToken);
    }

    public async Task<IReadOnlyList<CertificateResponseDto>> GetMyCertificatesAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var certificates = await _certificateRepository.GetListByUserIdAsync(userId, cancellationToken);
        return _mapper.Map<IReadOnlyList<CertificateResponseDto>>(certificates);
    }
}
```

#### 5. Tầng `QuickSign.EntityFrameworkCore`
Chỉ phụ thuộc vào `QuickSign.Domain`. Đóng gói hoàn toàn `QuickSignDbContext`, cấu hình bảng (`IEntityTypeConfiguration`), Migrations và các class thực thi Repository (`EfCoreCertificateRepository`, `EfCoreUnitOfWork`).

```csharp
// File: QuickSign.EntityFrameworkCore/Repositories/EfCoreCertificateRepository.cs
namespace QuickSign.EntityFrameworkCore.Repositories;

public sealed class EfCoreCertificateRepository : ICertificateRepository
{
    private readonly QuickSignDbContext _dbContext;

    public EfCoreCertificateRepository(QuickSignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Certificate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Certificates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Certificate>> GetListByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        => await _dbContext.Certificates.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

    public Task<int> CountActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        => _dbContext.Certificates.CountAsync(x => x.UserId == userId && x.Status == CertificateStatusType.Active, cancellationToken);

    public async Task InsertAsync(Certificate certificate, CancellationToken cancellationToken = default)
        => await _dbContext.Certificates.AddAsync(certificate, cancellationToken);

    public void Update(Certificate certificate)
        => _dbContext.Certificates.Update(certificate);
}

// File: QuickSign.EntityFrameworkCore/EntityFrameworkCore/EfCoreQuickSignDbSchemaMigrator.cs
namespace QuickSign.EntityFrameworkCore;

public sealed class EfCoreQuickSignDbSchemaMigrator : IQuickSignDbSchemaMigrator
{
    private readonly IServiceProvider _serviceProvider;

    public EfCoreQuickSignDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        await _serviceProvider
            .GetRequiredService<QuickSignDbContext>()
            .Database
            .MigrateAsync();
    }
}
```

#### 6. Tầng `QuickSign.Infrastructure`
Phụ thuộc vào `QuickSign.Application.Contracts` và `QuickSign.Domain`. Chứa các class thực thi (`Implementation`) cho việc gọi API Đăng nhập/Đăng ký sang **QuickBite Identity**, gọi gRPC sang **Golang**, kết nối **Redis** và băm $\text{SHA-256}$ cho API Key.

```csharp
// File: QuickSign.Infrastructure/Identity/QuickBiteIdentityService.cs
namespace QuickSign.Infrastructure.Identity;

public sealed class QuickBiteIdentityService : IQuickBiteIdentityService
{
    private readonly HttpClient _httpClient;
    private readonly QuickBiteIdentityOptions _options;

    public QuickBiteIdentityService(HttpClient httpClient, IOptions<QuickBiteIdentityOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<AuthTokenResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["username"] = request.UsernameOrEmail,
            ["password"] = request.Password,
            ["scope"] = _options.Scope
        });

        var response = await _httpClient.PostAsync(_options.TokenEndpoint, formContent, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new BusinessException("QuickSign:Auth:InvalidCredentials", "Tài khoản hoặc mật khẩu không chính xác.");
        }

        return (await response.Content.ReadFromJsonAsync<AuthTokenResponseDto>(cancellationToken: cancellationToken))!;
    }

    public async Task RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(_options.RegisterEndpoint, request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new BusinessException("QuickSign:Auth:RegistrationFailed", $"Đăng ký thất bại: {error}");
        }
    }

    public async Task<AuthTokenResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["refresh_token"] = refreshToken
        });

        var response = await _httpClient.PostAsync(_options.TokenEndpoint, formContent, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new BusinessException("QuickSign:Auth:RefreshTokenExpired", "Phiên đăng nhập đã hết hạn.");
        }

        return (await response.Content.ReadFromJsonAsync<AuthTokenResponseDto>(cancellationToken: cancellationToken))!;
    }
}
```

#### 7. Tầng `QuickSign.HttpApi`
Chỉ phụ thuộc duy nhất vào `QuickSign.Application.Contracts`. Toàn bộ Controllers chỉ giao tiếp thông qua các Interfaces của Application Services.

```csharp
// File: QuickSign.HttpApi/Controllers/AuthController.cs
namespace QuickSign.HttpApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : QuickSignControllerBase
{
    private readonly IAuthAppService _authAppService;

    public AuthController(IAuthAppService authAppService)
    {
        _authAppService = authAppService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public Task<AuthTokenResponseDto> LoginAsync([FromBody] LoginRequestDto input, CancellationToken cancellationToken)
        => _authAppService.LoginAsync(input, cancellationToken);

    [HttpPost("register")]
    [AllowAnonymous]
    public Task RegisterAsync([FromBody] RegisterRequestDto input, CancellationToken cancellationToken)
        => _authAppService.RegisterAsync(input, cancellationToken);

    [HttpPost("refresh")]
    [AllowAnonymous]
    public Task<AuthTokenResponseDto> RefreshTokenAsync([FromBody] string refreshToken, CancellationToken cancellationToken)
        => _authAppService.RefreshTokenAsync(refreshToken, cancellationToken);
}
```

#### 8. Tầng `QuickSign.DbMigrator`
Project Console độc lập giống hệt chuẩn ABP, dùng để cập nhật cấu trúc bảng PostgreSQL (`EfCoreQuickSignDbSchemaMigrator`) và khởi tạo dữ liệu nền tảng (`IDataSeedContributor`) mà không cần chạy Web Server.

```csharp
// File: QuickSign.DbMigrator/DbMigratorHostedService.cs
namespace QuickSign.DbMigrator;

public sealed class DbMigratorHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IServiceProvider _serviceProvider;

    public DbMigratorHostedService(IHostApplicationLifetime hostApplicationLifetime, IServiceProvider serviceProvider)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var migrationService = scope.ServiceProvider.GetRequiredService<QuickSignDbMigrationService>();
        await migrationService.MigrateAsync();

        _hostApplicationLifetime.StopApplication();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

#### 9. Tầng `QuickSign.HttpApi.Host`
Project khởi chạy Web API, nơi duy nhất tập hợp các hàm mở rộng `AddDomainServices()`, `AddApplicationServices()`, `AddEntityFrameworkCoreServices()`, `AddInfrastructureServices()` và `AddHttpApiControllers()` để nối toàn bộ `Interface` với `Implementation`.

```csharp
// File: QuickSign.HttpApi.Host/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDomainServices()
    .AddApplicationServices()
    .AddEntityFrameworkCoreServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration)
    .AddHttpApiControllers();

var app = builder.Build();
app.UseMiddleware<BusinessExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

---

## 3. Kiến trúc Chi tiết Dịch vụ Golang (`quicksign-crypto-engine`)

Dịch vụ **Golang** chịu trách nhiệm thực thi toàn bộ thuật toán mật mã học, được thiết kế theo **Hexagonal Architecture (Ports & Adapters)**, tách biệt hoàn toàn giữa `Ports` (Interfaces) và `Adapters` / `Services` (Implementations).

### 3.1. Bảng Phân tách Interface (Port) và Implementation trong Golang

| Phân loại | Tên Interface (Port) | Vị trí đặt Interface | Tên Struct (Implementation) | Vị trí đặt Implementation |
| :--- | :--- | :--- | :--- | :--- |
| **Inbound Port (KeyGen)** | `KeyGenerationUseCase` | `internal/core/ports/inbound.go` | `KeyGenService` | `internal/core/services/keygen_service.go` |
| **Inbound Port (Signing)** | `DocumentSigningUseCase` | `internal/core/ports/inbound.go` | `SigningService` | `internal/core/services/signing_service.go` |
| **Inbound Port (Verify)** | `DocumentVerificationUseCase` | `internal/core/ports/inbound.go` | `VerificationService` | `internal/core/services/verification_service.go` |
| **Outbound Port (Vault KMS)** | `VaultKeyStorePort` | `internal/core/ports/outbound.go` | `HashiCorpVaultAdapter` | `internal/adapters/outbound/vault/hashicorp_vault.go` |
| **Outbound Port (PDF PAdES)** | `PdfCryptoPort` | `internal/core/ports/outbound.go` | `PadesPdfAdapter` | `internal/adapters/outbound/pdf_pades/pades_adapter.go` |
| **Outbound Port (JSON JWS)** | `JwsCryptoPort` | `internal/core/ports/outbound.go` | `JwsAdapter` | `internal/adapters/outbound/jws_crypto/jws_adapter.go` |
| **Outbound Port (Database)** | `DocumentRepositoryPort` | `internal/core/ports/outbound.go` | `PgxDocumentRepository` | `internal/adapters/outbound/postgres/pgx_repository.go` |
| **Outbound Port (Cache)** | `AuthCachePort` | `internal/core/ports/outbound.go` | `RedisAuthCacheAdapter` | `internal/adapters/outbound/redis/redis_adapter.go` |
| **Outbound Port (Storage)** | `FileStoragePort` | `internal/core/ports/outbound.go` | `LocalFileStorageAdapter` | `internal/adapters/outbound/local_fs/disk_storage.go` |

### 3.2. Cấu trúc Thư mục Dự án Golang

```
src/go-crypto-engine/
├── cmd/
│   └── server/
│       └── main.go                       # Khởi tạo DI (tiêm Adapters vào Core Services), chạy HTTP & gRPC Server
├── internal/
│   ├── config/
│   │   └── config.go
│   ├── core/                             # TẦNG LÕI MẬT MÃ HỌC
│   │   ├── domain/
│   │   │   ├── keypair.go                # Struct KeyPair, CertificateMetadata
│   │   │   ├── signing.go                # Struct SignPdfInput, SignPdfOutput, SignJsonInput
│   │   │   └── verification.go           # Struct VerifyInput, VerifyOutput, ValidationStatus
│   │   ├── ports/                        # KHAI BÁO TOÀN BỘ INTERFACES
│   │   │   ├── inbound.go                # KeyGenerationUseCase, DocumentSigningUseCase, DocumentVerificationUseCase
│   │   │   └── outbound.go               # VaultKeyStorePort, PdfCryptoPort, JwsCryptoPort, DocumentRepositoryPort, AuthCachePort
│   │   └── services/                     # IMPLEMENTATIONS CỦA INBOUND USE CASES
│   │       ├── keygen_service.go         # Sinh khóa RSA-2048/ECDSA-P256, tạo X.509, đẩy Private Key vào Vault
│   │       ├── signing_service.go        # Băm SHA-256, lấy khóa từ Vault, ký PAdES/JWS, xóa RAM (zeroing)
│   │       └── verification_service.go   # Bóc tách chữ ký, kiểm tra mật mã học, kiểm tra hạn khóa & ghi log
│   └── adapters/                         # TẦNG ADAPTERS (GIAO TIẾP BÊN NGOÀI)
│       ├── inbound/                      # DRIVING ADAPTERS
│       │   ├── http/
│       │   │   ├── router.go             # Định tuyến /api/v1/sign/* và /api/v1/verify/*
│       │   │   ├── auth_middleware.go    # Xác thực JWT hoặc X-API-Key
│       │   │   ├── sign_handler.go       # Gọi DocumentSigningUseCase
│       │   │   └── verify_handler.go     # Gọi DocumentVerificationUseCase
│       │   └── grpc/
│       │       ├── crypto_grpc_server.go # Gọi KeyGenerationUseCase (nhận lệnh từ .NET)
│       │       └── pb/
│       └── outbound/                     # DRIVEN ADAPTERS (IMPLEMENTATIONS CỦA OUTBOUND PORTS)
│           ├── vault/
│           │   └── hashicorp_vault.go
│           ├── pdf_pades/
│           │   └── pades_adapter.go
│           ├── jws_crypto/
│           │   └── jws_adapter.go
│           ├── postgres/
│           │   └── pgx_repository.go
│           ├── redis/
│           │   └── redis_adapter.go
│           └── local_fs/
│               └── disk_storage.go
├── pkg/
│   └── memguard/
│       └── zero.go                       # Xóa mảng byte nhạy cảm trên RAM
├── proto/
│   └── crypto_engine.proto
└── go.mod
```

---

## 4. Kiến trúc Chi tiết Ứng dụng Frontend (`quicksign-web` - Next.js)

Ứng dụng **Next.js (App Router)** được tổ chức theo kiến trúc **Feature-Based Modular**, phân tách rõ tầng giao diện (Components/Pages) và tầng dịch vụ gọi API (`services/`).

```
src/nextjs-web/
├── src/
│   ├── app/                                       # NEXT.JS APP ROUTER
│   │   ├── (auth)/
│   │   │   ├── login/page.tsx                     # Gọi authService.login() -> .NET HttpApi
│   │   │   └── register/page.tsx                  # Gọi authService.register() -> .NET HttpApi
│   │   ├── (public)/
│   │   │   └── verify/
│   │   │       ├── page.tsx                       # Upload PDF/JWS -> Go Crypto API
│   │   │       └── [documentCode]/page.tsx        # Tra cứu theo mã tài liệu -> Go Crypto API
│   │   ├── (dashboard)/
│   │   │   ├── layout.tsx
│   │   │   ├── overview/page.tsx                  # Biểu đồ thống kê -> .NET HttpApi
│   │   │   ├── sign/page.tsx                      # Ký PDF/JSON trực tiếp -> Go Crypto API
│   │   │   ├── certificates/page.tsx              # Quản lý Chứng thư số -> .NET HttpApi
│   │   │   ├── api-keys/page.tsx                  # Quản lý API Key -> .NET HttpApi
│   │   │   └── documents/
│   │   │       ├── page.tsx                       # Danh sách tài liệu đã ký -> .NET HttpApi
│   │   │       └── [id]/verifications/page.tsx    # Lịch sử xác nhận chi tiết -> .NET HttpApi
│   │   └── (admin)/
│   │       └── admin/
│   │           ├── certificates/page.tsx          # Quản trị chứng thư toàn hệ thống -> .NET HttpApi
│   │           └── statistics/page.tsx            # Thống kê toàn hệ thống -> .NET HttpApi
│   ├── features/                                  # MODULES NGHIỆP VỤ (Components, Hooks, Types)
│   │   ├── auth/
│   │   ├── certificates/
│   │   ├── api-keys/
│   │   ├── signing/
│   │   ├── verification/
│   │   └── statistics/
│   ├── services/                                  # TẦNG GIAO TIẾP BACKEND
│   │   ├── http/
│   │   │   ├── dotnetHttpClient.ts                # Cấu hình BaseURL trỏ về .NET HttpApi.Host
│   │   │   └── goCryptoHttpClient.ts              # Cấu hình BaseURL trỏ về Go Crypto Engine
│   │   ├── auth.service.ts
│   │   ├── certificate.service.ts
│   │   ├── api-key.service.ts
│   │   ├── document.service.ts
│   │   ├── statistics.service.ts
│   │   └── crypto.service.ts
│   └── lib/
│       └── token-storage.ts
└── package.json
```

---

## 5. Luồng Thực thi Chuẩn qua các Project (End-to-End Flows)

1. **Luồng Đăng nhập / Đăng ký (`Next.js` $\rightarrow$ `HttpApi` $\rightarrow$ `Application.Contracts` $\rightarrow$ `Application` $\rightarrow$ `Infrastructure` $\rightarrow$ `QuickBite Identity`):**
   * `Next.js` gọi `POST /api/v1/auth/login` vào `AuthController` (thuộc project `QuickSign.HttpApi`).
   * `AuthController` chỉ tham chiếu tới project `QuickSign.Application.Contracts` và gọi `IAuthAppService.LoginAsync`.
   * Lúc chạy thực tế (`QuickSign.HttpApi.Host`), DI Container điều hướng lệnh gọi vào `AuthAppService` (thuộc project `QuickSign.Application`).
   * `AuthAppService` gọi tiếp vào `IQuickBiteIdentityService.LoginAsync` (khai báo ở `QuickSign.Application.Contracts`).
   * Class `QuickBiteIdentityService` (thuộc project `QuickSign.Infrastructure`) thực thi HTTP POST sang QuickBite Identity và trả Token về.

2. **Luồng Khởi tạo Chứng thư số (`Next.js` $\rightarrow$ `HttpApi` $\rightarrow$ `Application` $\rightarrow$ `Infrastructure` (gRPC) $\rightarrow$ `Golang` & `Domain` $\rightarrow$ `EntityFrameworkCore`):**
   * `CertificatesController` (`QuickSign.HttpApi`) gọi `ICertificateAppService.IssueCertificateAsync` (`QuickSign.Application.Contracts`).
   * `CertificateAppService` (`QuickSign.Application`) gọi `ICryptoEngineGrpcService` $\rightarrow$ thực thi bởi `CryptoEngineGrpcService` (`QuickSign.Infrastructure`) gọi gRPC sang `Golang` để sinh khóa và lưu Private Key vào Vault.
   * `CertificateAppService` gọi Domain Service `ICertificateManager.CreateAsync` (`QuickSign.Domain`) để kiểm tra quy tắc tối đa 5 chứng thư hoạt động và khởi tạo Aggregate `Certificate`.
   * `CertificateAppService` gọi `ICertificateRepository.InsertAsync` và `IUnitOfWork.SaveChangesAsync` (`QuickSign.Domain`) $\rightarrow$ thực thi bởi `EfCoreCertificateRepository` và `EfCoreUnitOfWork` (`QuickSign.EntityFrameworkCore`).

3. **Luồng Chạy Migration Độc lập (`QuickSign.DbMigrator`):**
   * Khi triển khai phiên bản mới, chạy project `QuickSign.DbMigrator`.
   * `DbMigratorHostedService` gọi `QuickSignDbMigrationService` (`QuickSign.Domain`) $\rightarrow$ kích hoạt `IQuickSignDbSchemaMigrator` (thực thi bởi `EfCoreQuickSignDbSchemaMigrator` trong `QuickSign.EntityFrameworkCore`) để cập nhật bảng PostgreSQL và chạy các `IDataSeedContributor`.