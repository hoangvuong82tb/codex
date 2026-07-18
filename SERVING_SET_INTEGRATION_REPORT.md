# ServingSet Integration Report

## Phạm vi đã làm

Mục tiêu của đợt này là thay tab `Menu có sẵn` trong module À La Carte từ mockup sang dữ liệu thật, nhưng vẫn giữ nguyên core flow:

- `Quote -> Approve -> Order`
- thêm món lẻ
- thêm dịch vụ phòng
- không động vào POS `Bữa sáng` và `Cà phê`

Tôi giữ đúng nguyên tắc đó.

## Quyết định tái sử dụng

Không tạo entity `ServingSet` mới.

Thay vào đó, tái sử dụng cụm entity đã có sẵn:

- `BanquetMenuPackage`
- `BanquetMenuPackageLine`

Lý do:

- repo đã có service CRUD cho package
- đã có quan hệ package -> nhiều món
- phù hợp để dùng như "suất / menu có sẵn" cho báo giá À La Carte
- tránh sinh thêm một mô hình trùng nghĩa với `ServingSet`

## Những gì đã thay đổi

### 1. A La Carte đọc package thật từ backend

Đã bổ sung `GetMenuPackagesAsync()` vào `IAlacarteRestaurantService` và triển khai ở `AlacarteRestaurantService`.

Tab `Menu có sẵn` trong:

- `D:\ASMILE-BO\ASMILE.BO.App\Views\AlacarteRestaurantView.xaml`

đã bỏ mockup cứng và chuyển sang bind `MenuPackages` thật.

### 2. Package line có dữ liệu chi tiết thật

Đã mở rộng DTO:

- `BanquetMenuPackageDto`
- thêm `BanquetMenuPackageLineDto`

để trả về:

- `MenuItemId`
- `MenuItemCode`
- `MenuItemName`
- `UnitName`
- `UnitPrice`
- `Qty`
- `CourseType`
- `PricingRule`
- `LineNo`
- `Note`

Nhờ vậy tab `Menu có sẵn` không còn là card tĩnh nữa mà dùng được dữ liệu package thật.

### 3. Mỗi món trong package giữ `PricingRule`

Đã bổ sung `PricingRule` cho `BanquetMenuPackageLine`.

Migration mới:

- `20260624021045_AddBanquetMenuPackageLinePricingRule`

Backfill:

- các package line cũ sẽ tự nhận `PricingRule = PricingMode` của package nếu trước đó chưa có giá trị

Các rule đang dùng thống nhất với quote line hiện có:

- `PerGuest`
- `PerTable`
- `PerRoom`
- `Manual`

### 4. Chọn package sẽ tự bung vào `QuoteLine`

Trong `AlacarteRestaurantViewModel`, command mới:

- `AddMenuPackageToQuote`

logic:

- lấy toàn bộ line thật từ `BanquetMenuPackage`
- bung mỗi món thành `QuoteLine`
- giữ `PricingRule` của từng món
- giữ `UnitPrice` theo `MenuItem`
- merge vào dòng đang có nếu trùng:
  - cùng `MenuItem`
  - cùng loại `Món ăn`
  - cùng `PricingRule`

### 5. Flow quote hiện hữu vẫn giữ nguyên

Tôi không sửa core chuyển đổi báo giá sang đơn mở.

Flow hiện tại vẫn dùng lại đúng line đã có:

- `SaveQuoteAsync()` vẫn lưu `QuoteLine`
- `ApproveQuoteAsync()` vẫn convert từ `RestaurantQuoteLine` sang `AlacarteOrderLine`
- `PricingRule` từng dòng tiếp tục được service tính và chia món như trước

Nói ngắn gọn:

- package chỉ là nguồn sinh `QuoteLine`
- còn quote/order pipeline phía sau không bị viết lại

## File chính đã sửa

- `D:\ASMILE-BO\ASMILE.BO.Application\Interfaces\IRestaurantServices.cs`
- `D:\ASMILE-BO\ASMILE.BO.Application\DTOs\RestaurantDtos.cs`
- `D:\ASMILE-BO\ASMILE.BO.Core\Entities\RestaurantEntities.cs`
- `D:\ASMILE-BO\ASMILE.BO.Infrastructure\Persistence\AppDbContext.cs`
- `D:\ASMILE-BO\ASMILE.BO.Infrastructure\Services\RestaurantServices.cs`
- `D:\ASMILE-BO\ASMILE.BO.App\ViewModels\RestaurantViewModels.cs`
- `D:\ASMILE-BO\ASMILE.BO.App\Views\AlacarteRestaurantView.xaml`
- `D:\ASMILE-BO\ASMILE.BO.Tests.Unit\RestaurantModuleTests.cs`
- `D:\ASMILE-BO\ASMILE.BO.Infrastructure\Persistence\Migrations\20260624021045_AddBanquetMenuPackageLinePricingRule.cs`

## Test đã chạy

### Build

```powershell
dotnet build D:\ASMILE-BO\ASMILE.BO.sln -c Debug /nodeReuse:false
```

Kết quả:

- `0 Warning(s)`
- `0 Error(s)`

### Unit

```powershell
dotnet test D:\ASMILE-BO\ASMILE.BO.Tests.Unit\ASMILE.BO.Tests.Unit.csproj -c Debug --no-build
```

Kết quả:

- `Passed: 181`

### Integration

```powershell
dotnet test D:\ASMILE-BO\ASMILE.BO.Tests.Integration\ASMILE.BO.Tests.Integration.csproj -c Debug --no-build
```

Kết quả:

- `Passed: 11`

### E2E

```powershell
dotnet test D:\ASMILE-BO\ASMILE.BO.Tests.E2E\ASMILE.BO.Tests.E2E.csproj -c Debug --no-build
```

Kết quả:

- `Passed: 4`

### App mở lại

App đã mở lại từ:

- `D:\ASMILE-BO\ASMILE.BO.App\bin\Debug\net8.0-windows\ASMILE.BO.App.exe`

## Test bổ sung mới

Đã thêm test xác nhận A La Carte tái sử dụng được package thật:

- `Alacarte_ShouldReuseBanquetMenuPackagesAsRealServingSets`

Test này kiểm tra:

- package được lưu bằng `BanquetRestaurantService`
- A La Carte đọc lại được bằng `AlacarteRestaurantService`
- line package giữ đúng `PricingRule`
- line package map đúng `UnitPrice` từ `MenuItem`

## Điều đã cố tình KHÔNG làm trong P0 này

- không tạo entity ServingSet mới
- không sửa core `Quote -> Approve -> Order`
- không polish lại UI
- không đụng Breakfast/Cafe POS

## Những gì còn thiếu / bước tiếp theo hợp lý

### 1. UI quản lý package chưa expose `PricingRule` từng dòng

Data layer đã hỗ trợ `PricingRule` trên từng món trong package.

Nhưng màn quản lý `BanquetMenuPackage` hiện tại chưa thấy bind UI để người dùng chọn rule riêng cho từng line package.

Hiện trạng sau P0:

- package line mới lưu qua code/service: giữ được `PricingRule`
- package line cũ: fallback từ `PricingMode` của package

Nếu muốn nghiệp vụ "món A theo khách, món B theo bàn, món C theo phòng" chỉnh trực tiếp trên màn package, cần làm tiếp UI package editor ở phase sau.

### 2. `QuoteLine.Quantity` hiện vẫn là kiểu int

Vì cấu trúc quote line hiện tại dùng `int`, nên khi line package có `Qty` dạng decimal thì lúc bung sang `QuoteLine` đang được làm tròn lên để không mất món.

Đây là giới hạn của schema quote line hiện tại, không phải bug của tích hợp package.

### 3. Chưa có tracking nguồn line từ package nào

Hiện tại sau khi bung vào quote, line được xem là `QuoteLine` bình thường.

Ưu điểm:

- không phá flow cũ

Nhược điểm:

- chưa có metadata kiểu `SourcePackageId` / `SourcePackageLineId`
- chưa có chức năng "re-sync lại quote theo package gốc"

Nếu cần đồng bộ ngược hoặc audit sâu hơn, đây là bước phase sau.

## Kết luận

P0 này đã hoàn thành đúng trọng tâm:

- tab `Menu có sẵn` không còn mockup
- dùng dữ liệu thật từ `BanquetMenuPackage`
- package bung ra `QuoteLine`
- mỗi line giữ `PricingRule`
- flow `Quote -> Approve -> Order` vẫn an toàn
- build/test pass
- app mở được
