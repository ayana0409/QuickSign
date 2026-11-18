# KẾ HOẠCH TRIỂN KHAI (IMPLEMENTATION PLAN)
## HẠ CẤP AUTOMAPPER VỀ PHIÊN BẢN 13.0.1 (BẢN MIT MÃ NGUỒN MỞ HOÀN TOÀN)

---

## 1. Metadata
* **Mã Kế Hoạch:** `PLAN-2026-11-18-AUTOMAPPER-DOWNGRADE`
* **File Kế Hoạch:** `docs/plans/archived/2026-11-18-downgrade-automapper-13.md`
* **Trạng Thái Hiện Tại:** `COMPLETED` *(Đã hoàn thành và nghiệm thu)*
* **Phạm Vi Tác Động:**
  - `QuickSign.Application` (`QuickSign.Application.csproj`, `DependencyInjection.cs`)

---

## 2. Mục Tiêu & Bối Cảnh (Goal Description)

* **Vấn Đề:** Khi khởi động ứng dụng, console xuất hiện dòng cảnh báo:
  ```text
  warn: LuckyPennySoftware.AutoMapper.License[0]
        You do not have a valid license key for the Lucky Penny software AutoMapper. This is allowed for development and testing scenarios. If you are running in production you are required to have a licensed version. Please visit https://luckypennysoftware.com to obtain a valid license.
  ```
  Nguyên nhân do dự án ban đầu tham chiếu `AutoMapper 17.0.0` (phiên bản thương mại hóa bởi Lucky Penny Software).
* **Mục Tiêu:** Hạ cấp package `AutoMapper` về phiên bản **`13.0.1`** trên project `QuickSign.Application`.
  - Phiên bản `13.0.1` là bản phát hành cuối cùng thuần mã nguồn mở theo giấy phép **MIT**.
  - Hoàn toàn miễn phí cho mọi mục đích sử dụng (kể cả Production thương mại).
  - Không có cơ chế kiểm tra License Key và loại bỏ 100% các dòng log cảnh báo của Lucky Penny Software.
  - Tương thích 100% với các Profile Mapping hiện tại trong hệ thống.

---

## 3. Chi Tiết Các Thay Đổi (Changes Implemented)

### Tầng `QuickSign.Application`

#### [MODIFIED] `src/management/src/QuickSign.Application/QuickSign.Application.csproj`
* Hạ cấp `AutoMapper` về `13.0.1`.
* Thêm tham chiếu tường minh `<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.15" />` (do bản 17.0.0 trước đây kéo transitively, khi hạ cấp về 13.0.1 cần khai báo tường minh cho các AppService sử dụng `ILogger`).

---

## 4. Kế Hoạch Triển Khai (Execution Checklist)

- [x] **Bước 1: Cập nhật Package Reference**
  - Sửa `AutoMapper` từ `17.0.0` thành `13.0.1` trong `QuickSign.Application.csproj`.
  - Thêm `Microsoft.Extensions.Logging.Abstractions` (10.0.15).
- [x] **Bước 2: Chuẩn hóa Đăng ký Dependency Injection**
  - Cấu hình `services.AddAutoMapper(...)` trong `DependencyInjection.cs` hoạt động trơn tru với AutoMapper 13.0.1.
- [x] **Bước 3: Biên Dịch & Chạy Unit Tests Toàn Diện**
  - Chạy `dotnet build src/management/QuickSign.sln -c Release -m:1` thành công 100% với 0 lỗi.
  - Chạy `dotnet test src/management/QuickSign.sln -c Release -m:1 --no-build` thành công 100% với 27/27 tests đỗ (12 tests Domain + 15 tests Application).
- [x] **Bước 4: Kiểm tra Cảnh Báo Khởi Động**
  - Xác nhận hoàn toàn loại bỏ package `Lucky Penny Software` thương mại.
- [x] **Bước 5: Cập Nhật & Lưu Trữ Kế Hoạch**
  - Lưu trữ kế hoạch vào `docs/plans/archived/2026-11-18-downgrade-automapper-13.md`.
