# KẾ HOẠCH TRIỂN KHAI (IMPLEMENTATION PLAN)
## TÁI CẤU TRÚC FILE PROGRAM.CS & TÁCH CẤU HÌNH SANG FILE RIÊNG

---

## 1. Metadata
* **Mã Kế Hoạch:** `PLAN-2026-11-18-PROGRAM-REFACTOR`
* **File Kế Hoạch:** `docs/plans/archived/2026-11-18-refactor-program-and-configuration.md`
* **Trạng Thái Hiện Tại:** `COMPLETED` *(Đã hoàn thành và nghiệm thu)*
* **Phạm Vi Tác Động:**
  - `QuickSign.HttpApi.Host` (`Program.cs`, `Configuration/HostConfigurationExtensions.cs`)

---

## 2. Mục Tiêu & Bối Cảnh (Goal Description)

* **Hiện trạng:**
  - File `Program.cs` (`src/management/src/QuickSign.HttpApi.Host/Program.cs`) ban đầu dài **272 dòng code**, đảm nhận quá nhiều trách nhiệm hỗn hợp.
* **Mục tiêu:**
  - Tách toàn bộ logic đăng ký Services và cấu hình Host sang **1 file riêng biệt**: `src/management/src/QuickSign.HttpApi.Host/Configuration/HostConfigurationExtensions.cs`.
  - Tái cấu trúc `Program.cs` (`src/management/src/QuickSign.HttpApi.Host/Program.cs`) trở nên gọn gàng, sạch sẽ, chỉ còn **13 dòng code**.
  - Giữ nguyên 100% hành vi chức năng hiện tại: Fail-Fast checks, xử lý lỗi xác thực JWT, CORS, Swagger Bearer/ApiKey, và pipeline middleware.
  - Bảo đảm dự án biên dịch thành công 0 lỗi và 100% unit tests (27/27) tiếp tục đỗ.

---

## 3. Chi Tiết Các Thay Đổi (Changes Implemented)

### Tầng `QuickSign.HttpApi.Host`

#### [NEW] `src/management/src/QuickSign.HttpApi.Host/Configuration/HostConfigurationExtensions.cs`
Đã tạo file mở rộng chứa toàn bộ cấu hình, tổ chức thành các extension methods chuyên biệt:
* `AddHostServices(this IServiceCollection services, IConfiguration configuration)`
* `UseHostPipeline(this WebApplication app)`
* `UseSwaggerDocumentation(this WebApplication app)`
* Các helper private:
  - `AddLayeredArchitectureServices(this IServiceCollection services, IConfiguration configuration)`
  - `ConfigureApiBehavior(this IServiceCollection services)`
  - `ConfigureJwtAuthentication(this IServiceCollection services, IConfiguration configuration)`
  - `ConfigureCorsPolicy(this IServiceCollection services, IConfiguration configuration)`
  - `ConfigureSwagger(this IServiceCollection services)`

#### [MODIFIED] `src/management/src/QuickSign.HttpApi.Host/Program.cs`
Rút gọn toàn bộ file xuống 13 dòng:
```csharp
using QuickSign.HttpApi.Host.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Register layered architecture dependencies and host services
builder.Services.AddHostServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHostPipeline();

app.Run();
```

---

## 4. Kế Hoạch Triển Khai (Execution Checklist)

- [x] **Bước 1: Tạo thư mục & file `Configuration/HostConfigurationExtensions.cs`**
  - Chuyển toàn bộ logic cấu hình từ `Program.cs` sang `HostConfigurationExtensions.cs`.
  - Chia nhỏ thành các private methods rõ ràng, có XML/comment tiếng Anh chuẩn mực.
- [x] **Bước 2: Cập nhật `Program.cs`**
  - Thay thế 272 dòng code bằng cấu trúc tinh gọn 13 dòng.
- [x] **Bước 3: Biên dịch Solution & Kiểm tra Lỗi**
  - Thực thi: `dotnet build src/management/QuickSign.sln -c Release -m:1`.
  - Kết quả: Build Succeeded với 0 lỗi biên dịch.
- [x] **Bước 4: Chạy Toàn Bộ Unit Tests**
  - Thực thi: `dotnet test src/management/QuickSign.sln -c Release -m:1 --no-build`.
  - Kết quả: 27/27 tests đỗ (12 Domain + 15 Application).
- [x] **Bước 5: Lưu Trữ Kế Hoạch & Cập Nhật Walkthrough**
  - Di chuyển kế hoạch từ `docs/plans/active/` sang `docs/plans/archived/`.
  - Cập nhật tài liệu walkthrough.
