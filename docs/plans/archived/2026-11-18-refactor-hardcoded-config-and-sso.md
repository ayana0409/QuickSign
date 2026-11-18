# KẾ HOẠCH TRIỂN KHAI (IMPLEMENTATION PLAN)
## RÀ SOÁT & XÓA BỎ HARDCODE CẤU HÌNH, CHUYỂN SANG NÉM LỖI FAIL-FAST VÀ CHUẨN HÓA ĐĂNG NHẬP SSO

---

## 1. Metadata
* **Mã Kế Hoạch:** `PLAN-2026-11-18-CONFIG-CLEANUP`
* **File Kế Hoạch:** `docs/plans/archived/2026-11-18-refactor-hardcoded-config-and-sso.md`
* **Trạng Thái Hiện Tại:** `COMPLETED`
* **Phạm Vi Ảnh Hưởng:**
  - `QuickSign.HttpApi.Host` (`Program.cs`, `appsettings.Development.json`)
  - `QuickSign.Infrastructure` (`SsoIdentityOptions`, `SsoIdentityService`, `DependencyInjection.cs`, `CryptoEngineGrpcService.cs`, `CurrentUserService.cs`)
  - `QuickSign.Application.Contracts` (`ISsoIdentityService.cs`)
  - `QuickSign.Application` (`AuthAppService.cs`)
  - `QuickSign.HttpApi` (`AdminController.cs`)
  - Tài liệu đặc tả: `docs/quicksign_api_specification.md`, `docs/error_codes.md`

---

## 2. Mục Tiêu & Yêu Cầu Cốt Lõi

1. **Nguyên Tắc Fail-Fast (Cấu Hình Rỗng Phải Ném Lỗi Ngay):**
   - Loại bỏ 100% các giá trị fallback âm thầm dạng `configuration[...] ?? "default_value"`.
   - Nếu bất kỳ cấu hình bắt buộc nào bị thiếu hoặc rỗng (`SsoIdentity:Authority`, `Cors:AllowedOrigins`, `Redis:Configuration`, `CryptoEngine:GrpcEndpoint`, `ConnectionStrings:Default`), ứng dụng phải lập tức ném ngoại lệ `InvalidOperationException` kèm thông điệp rõ ràng và dừng khởi động.
2. **Loại Bỏ Hoàn Toàn Hardcode Link Identity:**
   - Không được hardcode bất kỳ URL cụ thể nào của nhà cung cấp danh tính (ví dụ: tuyệt đối không hardcode url).
   - Mọi URL (`Authority`, `TokenEndpoint`, `RegisterEndpoint`) phải được nạp động từ file cấu hình / biến môi trường.
3. **Trung Tính Hóa Thuật Ngữ (Chuẩn Hóa Đăng Nhập SSO):**
   - Gọi chung là "Hệ thống Đăng nhập SSO (Identity Server)" / `SsoIdentity`, tuyệt đối không đưa tên riêng "QuickBite" vào code, tên interface, tên class hay tài liệu API.
   - Đổi `IQuickBiteIdentityService` $\rightarrow$ `ISsoIdentityService`.
   - Đổi `QuickBiteIdentityOptions` $\rightarrow$ `SsoIdentityOptions`.
   - Đổi Section cấu hình từ `QuickBiteIdentity` $\rightarrow$ `SsoIdentity`.
   - Đổi role `quickbite-admin` $\rightarrow$ `sso-admin` trong `AdminController`.

---

## 3. Danh Sách Các Vị Trí Cần Thay Đổi Chi Tiết

### 3.1. Tầng `QuickSign.HttpApi.Host`
* **`Program.cs`:**
  - Thay thế fallback `builder.Configuration["QuickBiteIdentity:Authority"] ?? "https://..."` bằng kiểm tra:
    ```csharp
    var ssoAuthority = builder.Configuration["SsoIdentity:Authority"];
    if (string.IsNullOrWhiteSpace(ssoAuthority))
    {
        throw new InvalidOperationException("Configuration 'SsoIdentity:Authority' is required and cannot be empty.");
    }
    ```
  - Thay thế fallback `builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" }` bằng:
    ```csharp
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    if (allowedOrigins == null || allowedOrigins.Length == 0)
    {
        throw new InvalidOperationException("Configuration 'Cors:AllowedOrigins' is required and cannot be empty.");
    }
    ```
* **`appsettings.Development.json`:**
  - Đổi tên Section `QuickBiteIdentity` thành `SsoIdentity`.

### 3.2. Tầng `QuickSign.Infrastructure`
* **[RENAME] `Identity/QuickBiteIdentityOptions.cs` $\rightarrow$ `Identity/SsoIdentityOptions.cs`:**
  - Đổi `SectionName` thành `"SsoIdentity"`.
  - Xóa bỏ toàn bộ URL hardcode mặc định (`Authority = default!`, `TokenEndpoint = default!`, `RegisterEndpoint = default!`, `ClientId = default!`).
* **[RENAME] `Identity/QuickBiteIdentityService.cs` $\rightarrow$ `Identity/SsoIdentityService.cs`:**
  - Implement `ISsoIdentityService`.
  - Trong constructor, kiểm tra `_options`: nếu `Authority`, `TokenEndpoint`, `RegisterEndpoint`, hoặc `ClientId` rỗng thì ném `InvalidOperationException`.
  - Chuẩn hóa log: `"SSO login failed..."`, `"SSO registration failed..."`.
* **`Identity/CurrentUserService.cs`:**
  - Chuẩn hóa XML comment: Thay "QuickBite SSO" thành "SSO Identity Server".
* **`Cryptography/CryptoEngineGrpcService.cs`:**
  - Thay thế fallback `?? "http://localhost:50051"` bằng:
    ```csharp
    var endpoint = configuration["CryptoEngine:GrpcEndpoint"];
    if (string.IsNullOrWhiteSpace(endpoint))
    {
        throw new InvalidOperationException("Configuration 'CryptoEngine:GrpcEndpoint' is required and cannot be empty.");
    }
    ```
* **`DependencyInjection.cs`:**
  - Thay thế fallback Redis `?? "localhost:6379"` bằng kiểm tra rỗng ném lỗi:
    ```csharp
    var redisConfig = configuration["Redis:Configuration"];
    if (string.IsNullOrWhiteSpace(redisConfig))
    {
        throw new InvalidOperationException("Configuration 'Redis:Configuration' is required and cannot be empty.");
    }
    ```
  - Đăng ký `services.Configure<SsoIdentityOptions>(configuration.GetSection(SsoIdentityOptions.SectionName));`
  - Đăng ký `services.AddHttpClient<ISsoIdentityService, SsoIdentityService>();`

### 3.3. Tầng `QuickSign.Application.Contracts` & `QuickSign.Application`
* **[RENAME] `Infrastructure/IQuickBiteIdentityService.cs` $\rightarrow$ `Infrastructure/ISsoIdentityService.cs`:**
  - Đổi tên interface thành `ISsoIdentityService`.
* **`Services/AuthAppService.cs`:**
  - Inject `ISsoIdentityService` thay vì `IQuickBiteIdentityService`.

### 3.4. Tầng `QuickSign.HttpApi`
* **`Controllers/AdminController.cs`:**
  - Đổi `[Authorize(Roles = "Admin,admin,quickbite-admin")]` $\rightarrow$ `[Authorize(Roles = "Admin,admin,sso-admin")]`.

### 3.5. Tài Liệu Hệ Thống
* **`docs/quicksign_api_specification.md`:**
  - Thay toàn bộ "QuickBite Identity" bằng "Hệ thống Đăng nhập SSO (Identity Server)".
  - Thay thế Identity URL bằng tham số cấu hình `{SSO_AUTHORITY_URL}`.
  - Thay các tên ví dụ `QuickBite Order Invoicing Service` thành `Enterprise Invoicing Service`.
* **`docs/error_codes.md`:**
  - Thay đổi mô tả kỹ thuật của `INVALID_CREDENTIALS`, `REFRESH_TOKEN_EXPIRED`, `REGISTRATION_FAILED` từ "QuickBite" thành "SSO Identity Provider".

---

## 4. Kế Hoạch Triển Khai Chi Tiết (Execution Checklist)

- [x] **Bước 1: Tái cấu trúc Tầng Application.Contracts & Application**
  - [x] Đổi tên file & interface `IQuickBiteIdentityService.cs` sang `ISsoIdentityService.cs`.
  - [x] Cập nhật `AuthAppService.cs` sử dụng `ISsoIdentityService`.
- [x] **Bước 2: Tái cấu trúc Tầng Infrastructure**
  - [x] Đổi tên & chuẩn hóa `SsoIdentityOptions.cs` (loại bỏ URL hardcode).
  - [x] Đổi tên & chuẩn hóa `SsoIdentityService.cs` (kiểm tra rỗng ném lỗi).
  - [x] Cập nhật `CryptoEngineGrpcService.cs` (kiểm tra rỗng ném lỗi).
  - [x] Cập nhật `DependencyInjection.cs` (kiểm tra Redis rỗng ném lỗi, đăng ký SsoIdentity).
- [x] **Bước 3: Tái cấu trúc Tầng HttpApi & HttpApi.Host**
  - [x] Cập nhật `AdminController.cs` (bỏ `quickbite-admin`).
  - [x] Cập nhật `Program.cs` (fail-fast kiểm tra `SsoIdentity:Authority` và `Cors:AllowedOrigins`).
  - [x] Cập nhật `appsettings.Development.json` (`SsoIdentity`).
- [x] **Bước 4: Cập nhật Unit Tests & Kiểm Tra Toàn Diện**
  - [x] Chạy `dotnet build src/management/QuickSign.sln --configuration Release`.
  - [x] Chạy `dotnet test src/management/QuickSign.sln --configuration Release` (đạt 100% PASS).
- [x] **Bước 5: Chuẩn hóa Tài Liệu**
  - [x] Cập nhật `docs/quicksign_api_specification.md`.
  - [x] Cập nhật `docs/error_codes.md`.

---

## 5. Ma Trận Kiểm Thử Nghiệm Thu (Verification Matrix)

| Hạng Mục | Điều Kiện Đầu Vào | Kết Quả Mong Đợi |
| :--- | :--- | :--- |
| **Fail-fast SsoIdentity** | `SsoIdentity:Authority` rỗng trong config | Khởi động ném `InvalidOperationException("Configuration 'SsoIdentity:Authority' is required...")` |
| **Fail-fast Redis** | `Redis:Configuration` rỗng trong config | Khởi động ném `InvalidOperationException("Configuration 'Redis:Configuration' is required...")` |
| **Fail-fast CORS** | `Cors:AllowedOrigins` rỗng trong config | Khởi động ném `InvalidOperationException("Configuration 'Cors:AllowedOrigins' is required...")` |
| **Fail-fast CryptoEngine**| `CryptoEngine:GrpcEndpoint` rỗng trong config | Khởi động ném `InvalidOperationException("Configuration 'CryptoEngine:GrpcEndpoint' is required...")` |
| **Biên dịch & Tests** | Chạy `dotnet test src/management/QuickSign.sln` | Toàn bộ 27/27 unit tests đỗ 100% |
| **Sạch sẽ chuỗi QuickBite**| Quét chuỗi `quickbite` trong source code | 0 kết quả tìm thấy |
