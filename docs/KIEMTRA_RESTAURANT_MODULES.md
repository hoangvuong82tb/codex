# KIEMTRA_RESTAURANT_MODULES

## Phạm vi đã triển khai

Đã triển khai nền tảng additive cho:

- Nhà hàng À La Carte.
- Nhà hàng Tiệc / Banquet.

Đã tạo tài liệu phân tích và kiến trúc:

- `RESTAURANT_ANALYSIS.md`
- `RESTAURANT_ARCHITECTURE.md`

## File thêm mới

- `ASMILE.BO.Core/Entities/RestaurantEntities.cs`
- `ASMILE.BO.Application/DTOs/RestaurantDtos.cs`
- `ASMILE.BO.Application/Interfaces/IRestaurantServices.cs`
- `ASMILE.BO.Infrastructure/Services/RestaurantServices.cs`
- `ASMILE.BO.App/ViewModels/RestaurantViewModels.cs`
- `ASMILE.BO.App/Views/AlacarteRestaurantView.xaml`
- `ASMILE.BO.App/Views/AlacarteRestaurantView.xaml.cs`
- `ASMILE.BO.App/Views/BanquetRestaurantView.xaml`
- `ASMILE.BO.App/Views/BanquetRestaurantView.xaml.cs`
- `ASMILE.BO.Tests.Unit/RestaurantModuleTests.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260614055304_AddRestaurantAlacarteAndBanquetModules.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260614055304_AddRestaurantAlacarteAndBanquetModules.Designer.cs`

## File sửa

- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
  - Thêm DbSet và mapping cho À La Carte/Banquet.
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
  - Đăng ký `IAlacarteRestaurantService`, `IBanquetRestaurantService`.
- `ASMILE.BO.App/DependencyInjection.cs`
  - Đăng ký `AlacarteRestaurantViewModel`, `BanquetRestaurantViewModel`.
- `ASMILE.BO.App/Themes/DataTemplates.xaml`
  - Thêm DataTemplate cho hai view mới.
- `ASMILE.BO.App/Services/NavigationService.cs`
  - Thêm menu `restaurant-alacarte`, `restaurant-banquet`.
  - Thêm permission route mapping.
  - Thêm resolve ViewModel theo route.
- `ASMILE.BO.Infrastructure/Services/InventoryCostingService.cs`
  - Khôi phục hành vi reject khi xuất vượt số lượng cost layer để test hiện hữu pass.

## Migration

Migration mới:

- `20260614055304_AddRestaurantAlacarteAndBanquetModules`

Không sửa migration cũ.

Migration được tạo bằng:

```powershell
dotnet ef migrations add AddRestaurantAlacarteAndBanquetModules --project ASMILE.BO.Infrastructure --startup-project ASMILE.BO.App --context AppDbContext --output-dir Persistence\Migrations
```

Ghi chú: EF tooling có cảnh báo DI design-time về một số service singleton/scoped hiện hữu, nhưng vẫn build succeeded và tạo migration thành công.

## Permission

Đã thêm route permission:

- `restaurant-alacarte` -> `restaurant.alacarte.view`
- `restaurant-banquet` -> `restaurant.banquet.view`

Theo kiến trúc, permission manage cần bổ sung vào màn quản trị quyền:

- `restaurant.alacarte.manage`
- `restaurant.banquet.manage`

## Chức năng À La Carte

Đã có:

- Quản lý phòng ăn.
- Quản lý bàn ăn.
- Đặt bàn.
- Mở order theo bàn.
- Thêm/sửa dòng món ở service.
- Tính tổng tiền order.
- Gửi bếp/bar theo `MenuItem.PreparationArea`.
- Chỉ gửi món chưa gửi.
- Phiếu hủy món có line detail.
- Đặt cọc.
- Dashboard KPI.
- UI card/sidebar theo phong cách Breakfast Cafe.

## Chức năng Banquet

Đã có:

- Quản lý phòng tiệc.
- Tạo/sửa sự kiện.
- Kiểm tra trùng phòng/trùng giờ.
- Kiểm tra vượt sức chứa/vượt số bàn.
- Thực đơn tiệc theo bàn/theo khách.
- Dòng dịch vụ tiệc.
- Đặt cọc nhiều lần.
- Tạo hợp đồng.
- Checklist vận hành mặc định theo event.
- Dashboard KPI.
- UI card/sidebar theo phong cách Breakfast Cafe.

## Test đã thêm

- Tạo phòng ăn.
- Tạo bàn ăn.
- Đặt bàn.
- Mở order À La Carte.
- Tính tổng tiền order.
- Gửi bếp/bar chỉ gửi món mới.
- Hủy món đã gửi tạo phiếu hủy.
- Tạo phòng tiệc.
- Tạo sự kiện tiệc.
- Đặt cọc.
- Hợp đồng.
- Kiểm tra trùng lịch.
- Kiểm tra vượt sức chứa.
- Tính tổng tiền tiệc.

## Kết quả build

Lệnh:

```powershell
dotnet build ASMILE.BO.sln -c Release --no-restore
```

Kết quả:

- Build succeeded.
- 0 Warning.
- 0 Error.
- XAML encoding check passed: no mojibake patterns found in XAML.

## Kết quả test

Lệnh:

```powershell
dotnet test ASMILE.BO.sln -c Release --no-build
```

Kết quả:

- Integration: Passed 11/11.
- Unit: Passed 137/137.
- Tổng fail: 0.

## Cảnh báo

- UI hiện là nền tảng vận hành dạng card/sidebar, chưa triển khai kéo thả thật cho sơ đồ bàn tiệc.
- Module mới chưa tự tạo bút toán thật, đúng yêu cầu chỉ chuẩn bị điểm tích hợp công nợ/kế toán.
- `NavigationService.cs` trước khi triển khai đã có nhiều text mojibake ở các module cũ; phần route mới được thêm bằng UTF-8 đúng dấu.
- Workspace không có `.git`, nên không xuất được `git status`.

## Hướng dẫn sử dụng nhanh

1. Mở app Release.
2. Vào Start/Menu module:
   - `Nhà hàng À La Carte`
   - `Nhà hàng Tiệc`
3. À La Carte:
   - Tạo phòng ăn.
   - Chọn phòng, tạo bàn.
   - Tạo đặt bàn hoặc mở order từ card bàn.
4. Banquet:
   - Tạo phòng tiệc.
   - Chọn phòng, tạo sự kiện.
   - Ghi cọc, tạo hợp đồng, xử lý checklist.
