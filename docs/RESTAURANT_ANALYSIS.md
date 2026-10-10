# RESTAURANT_ANALYSIS

## Phạm vi phân tích

Phân tích module `Bán đồ ăn sáng & cà phê` hiện tại trong ASMILE-BO để dùng làm chuẩn cho hai module mới:

- `Nhà hàng À La Carte`
- `Nhà hàng Tiệc / Banquet`

Các nhóm file đã đọc:

- Entity: `ASMILE.BO.Core/Entities/BreakfastCafeSalesEntities.cs`
- DTO: `ASMILE.BO.Application/DTOs/BreakfastCafeSalesDtos.cs`
- Service contract: `ASMILE.BO.Application/Interfaces/IBreakfastCafeSalesService.cs`
- Service: `ASMILE.BO.Infrastructure/Services/BreakfastCafeSalesService.cs`
- Costing service: `ASMILE.BO.Infrastructure/Services/BreakfastCafeCostingService.cs`
- ViewModel: `ASMILE.BO.App/ViewModels/BreakfastCafeSalesViewModel.cs`
- XAML: `ASMILE.BO.App/Views/BreakfastCafeSalesView.xaml`, `BreakfastCafeSettingsView.xaml`, `BreakfastCafeClosedOrdersView.xaml`
- Navigation: `ASMILE.BO.App/Services/NavigationService.cs`
- DI: `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- DbContext: `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- Migration: `20260528095952_AddBreakfastCafeSalesModule`, `20260607022536_AddBreakfastCafePosTablesAndKitchenTickets`

## Kiến trúc hiện tại

Module Breakfast Cafe đang đi theo kiến trúc 4 lớp:

- `Core`: entity nghiệp vụ, có company/branch scope.
- `Application`: DTO và service interface.
- `Infrastructure`: EF Core service, truy vấn, tính tiền, thanh toán, trừ định lượng, tích hợp kế toán/quỹ/ngân hàng.
- `App`: WPF ViewModel + XAML view, dùng CommunityToolkit MVVM, command binding và Win11/Fluent style.

Luồng chính:

1. UI mở module qua route `breakfast-cafe-sales`.
2. `BreakfastCafeSalesViewModel` load bàn, menu, đơn mở, doanh thu ngày.
3. ViewModel gọi `IBreakfastCafeSalesService`.
4. Service thao tác EF Core trên `BreakfastCafeOrder`, `BreakfastCafeOrderLine`, `PosTable`, `BreakfastCafePayment`, `BreakfastCafeKitchenTicket`.
5. Khi thanh toán đủ, service gọi costing để trừ nguyên vật liệu theo định lượng.
6. Thanh toán tiền mặt/chuyển khoản/card/công nợ đi qua các điểm tích hợp Cash, Bank, AR và Journal.

## Entity hiện có

Nhóm bán hàng POS:

- `BreakfastCafeOrder`
  - Order number, date, status, payment status, order type, service status.
  - Tham chiếu bàn `PosTableId`, khách, phòng.
  - Tổng tiền: tạm tính, giảm giá, VAT, phí phục vụ, đã trả, còn lại.
  - `KitchenSentAt` để nhận biết đã gửi bếp/bar.
  - Navigation: `Lines`, `Payments`, `KitchenTickets`.

- `BreakfastCafeOrderLine`
  - Món, số lượng, giá bán, VAT, giảm giá dòng, tổng dòng.
  - `KitchenStatus` để phân biệt món mới và món đã gửi.
  - `Note` đang dùng cả ghi chú bếp và dấu hiệu "Món thêm".

- `PosTable`
  - Khu vực, mã bàn, tên bàn, loại order, trạng thái, số ghế.
  - Hiện là mô hình bàn chung cho breakfast/cafe, chưa có phòng ăn tách riêng.

- `BreakfastCafeKitchenTicket`
  - Phiếu gửi bếp/bar/hủy món.
  - `Station`: Kitchen, Bar, KitchenBar.
  - `Status`: Sent hoặc Cancelled.

- `BreakfastCafePayment`
  - Dòng thanh toán của order.
  - Hỗ trợ tiền mặt, ngân hàng, card, ghi phòng, công nợ.

Nhóm menu và định lượng:

- `MenuCategory`
- `MenuItem`
- `MenuItemRecipe`
- `MenuItemRecipeLine`

Đây là shared layer quan trọng cho cả À La Carte và Banquet.

## Mapping EF Core

Các DbSet liên quan nằm trong `AppDbContext`:

- `BreakfastCafeOrders`
- `BreakfastCafeOrderLines`
- `BreakfastCafePayments`
- `PosTables`
- `BreakfastCafeKitchenTickets`
- `MenuCategories`
- `MenuItems`
- `MenuItemRecipes`
- `MenuItemRecipeLines`

Các quan hệ đang dùng:

- Order -> Lines: cascade.
- Order -> Payments: cascade.
- Order -> KitchenTickets: cascade.
- Order -> PosTable: `SetNull`.
- Order -> Customer: `SetNull`.
- OrderLine -> MenuItem: `Restrict`.
- Payment -> CashFund/BankAccount/Customer: `SetNull`.
- MenuItem -> MenuCategory: `Restrict`.
- Recipe -> Lines: cascade.
- RecipeLine -> Item/Warehouse: `SetNull`.
- RecipeLine -> ChildMenuItem: `NoAction`.

Điểm cần kế thừa cho module mới:

- Tránh cascade từ nghiệp vụ sang danh mục.
- Với dữ liệu phát sinh như deposit/payment/ticket/checklist, có thể cascade trong phạm vi aggregate nếu không gây multiple cascade path.
- Với Customer/MenuItem/Room/Table/Warehouse/CashFund/BankAccount nên dùng `Restrict`, `SetNull` hoặc `NoAction`.

## Service hiện tại

`IBreakfastCafeSalesService` cung cấp đầy đủ các hành vi POS:

- Lấy menu.
- Lấy bàn.
- Mở order theo bàn.
- Tạo order.
- Thêm món.
- Sửa số lượng.
- Sửa ghi chú dòng.
- Xóa món.
- Áp dụng giảm giá.
- Gửi bếp/bar.
- Thanh toán.
- Hủy order.
- Sửa ghi chú order.
- Đổi bàn.
- Gộp đơn.
- Lấy đơn mở, đơn trong ngày, đơn theo khoảng ngày.
- Lấy summary.
- Lưu/xóa bàn.

Các hành vi cần kế thừa:

- Order number tự sinh theo ngày.
- Không cho thanh toán order rỗng.
- Không sửa trực tiếp order đã paid/cancelled.
- Khi món đã gửi bếp mà tăng số lượng, tạo dòng "Món thêm" thay vì sửa dòng đã gửi.
- Gửi bếp/bar chỉ gửi các dòng chưa gửi.
- Tách Kitchen/Bar theo `MenuItem.PreparationArea`.
- Khi xóa món đã từng gửi, tạo ticket hủy món.
- Thanh toán đủ mới trừ định lượng.
- Kiểm tra tồn kho/định lượng trước khi gửi bếp hoặc paid.
- Cập nhật trạng thái bàn theo vòng đời order.

## ViewModel hiện tại

`BreakfastCafeSalesViewModel` là ViewModel lớn, đang gom nhiều vai trò:

- POS runtime: bàn, order, dòng món, thanh toán, bếp/bar.
- Danh sách đơn mở.
- Đơn đóng.
- Báo cáo.
- Cài đặt bán hàng.
- Quản lý bàn/khu vực.
- Chọn nhóm món bán.
- Cài đặt phí phục vụ, giảm giá, thanh toán, bếp/bar, ca bán, máy in, quyền, kho, audit.
- In/xem trước phiếu tạm tính, phiếu bếp/bar, hóa đơn.

Pattern chính:

- `ObservableCollection` cho danh sách UI.
- `[ObservableProperty]` và `[RelayCommand]` từ CommunityToolkit.
- Các property boolean điều khiển tab/empty state.
- `RunBusyAsync` để bọc thao tác async.
- Filter/sort cục bộ cho menu, bàn, đơn mở, báo cáo.
- Cài đặt được lưu JSON tại `Data/Settings/breakfast-cafe-sales-policy.json`.

Điểm nên kế thừa:

- Cách tổ chức KPI, trạng thái, command, filter/sort.
- Preview trước khi in.
- Chọn khổ giấy và máy in trong settings.
- Dùng `PreparationArea` để route Kitchen/Bar.
- Danh sách đơn mở phải hỗ trợ đổi bàn, chia/gộp đơn.

Điểm không nên sao chép y nguyên:

- ViewModel đang quá lớn. Với À La Carte/Banquet nên tách theo sub-viewmodel hoặc service nhỏ hơn nếu phạm vi tăng.
- `Note` đang bị dùng cho cả ghi chú nghiệp vụ và dấu hiệu "Món thêm"; module mới nên có trường riêng như `LineType`, `IsSupplemental`, `SentAt`, `CancelledAt`.
- Kitchen ticket hiện chỉ lưu header, chưa lưu chi tiết dòng ticket. Module mới nên có `KitchenTicketLine`/`PreparationTicketLine` để audit và in lại chính xác.

## UI/UX hiện tại

`BreakfastCafeSalesView.xaml` dùng phong cách POS Win11:

- Header trắng, KPI nhanh.
- Bố cục 2 cột: khu vực thao tác chính và panel order bên phải.
- Mini sidebar trái gồm Khu vực, Thực đơn, Đơn mở, Cài đặt, Đơn đóng, Báo cáo.
- Card bàn.
- Menu dạng danh sách/lưới.
- Badge trạng thái.
- Empty state.
- Search/filter/sort.
- Action buttons cho gửi bếp/bar, in tạm tính, thanh toán, đổi bàn, gộp đơn.
- Settings dùng card, tab ngang, toggle/checkbox/input.

À La Carte nên kế thừa layout này gần như trực tiếp, vì nghiệp vụ tương tự POS nhà hàng.

Banquet không nên dùng DataGrid toàn màn hình. Nên vẫn dùng:

- Header KPI.
- Sidebar trái theo nghiệp vụ: Dashboard, Phòng tiệc, Sự kiện, Thực đơn tiệc, Dịch vụ, Hợp đồng, Cọc, Checklist, Sơ đồ bàn, Timeline, Lịch phòng.
- Trung tâm là card/list/calendar/floor plan.
- Sidebar phải là chi tiết sự kiện/hợp đồng/tổng tiền.

## Navigation hiện tại

Module catalog nằm trong `NavigationService.GetModuleCatalog()`.

Các route liên quan:

- `menu-recipe`
- `breakfast-cafe-sales`
- `breakfast-cafe-settings`

Permission mapping trong `GetPermissionCode()`:

- `menu-recipe` -> `menu-recipe.view`
- `breakfast-cafe-sales` -> `breakfast-cafe.view`
- `breakfast-cafe-settings` -> `breakfast-cafe-settings.view`

ViewModel resolve nằm trong hàm tạo ViewModel theo route:

- `menu-recipe` -> `MenuRecipeViewModel`
- `breakfast-cafe-sales` -> `BreakfastCafeSalesViewModel`
- `breakfast-cafe-settings` -> `BreakfastCafeSettingsViewModel`

Module mới cần thêm route:

- `restaurant-alacarte`
- `restaurant-banquet`

Permission mapping cần thêm:

- `restaurant.alacarte.view`
- `restaurant.alacarte.manage`
- `restaurant.banquet.view`
- `restaurant.banquet.manage`

## Permission hiện tại

Hệ thống có nhiều lớp permission:

- `Core/Security/Permission.cs` là enum quyền hệ thống cũ.
- `AppPermission`, `AppUserPermission`, `AppRolePermission`, `AppDataPermission` cho quyền ứng dụng.
- `PermissionService` kiểm tra quyền theo company/branch.
- `NavigationService.GetPermissionCode()` trả permission code theo route.
- Mock current user hiện cho phép toàn bộ trong môi trường dev.

Module mới nên đi theo permission code dạng string, không bắt buộc mở rộng enum nếu hệ thống navigation đang dùng string.

## Migration hiện tại

Breakfast Cafe có hai migration chính:

- `AddBreakfastCafeSalesModule`
  - Tạo bảng orders, payments, menu categories, menu items, order lines, recipes.
  - Có một số SQL idempotent để bù cột tồn tại/thiếu.

- `AddBreakfastCafePosTablesAndKitchenTickets`
  - Bổ sung bàn POS, kitchen ticket, preparation area, service charge, kitchen sent status.
  - Dùng `SetNull`, `Cascade`, `Restrict`, `NoAction` tùy quan hệ.

Khi làm module mới:

- Chỉ tạo migration mới.
- Không sửa migration cũ.
- Không seed dữ liệu demo vào database thật.
- Tránh cascade path nguy hiểm.
- Nếu cần default catalog dùng code/service để tạo khi người dùng thao tác hoặc để trống.

## Component tái sử dụng

Shared nghiệp vụ:

- Customer.
- MenuCategory, MenuItem.
- MenuItemRecipe, MenuItemRecipeLine.
- Inventory item/warehouse/costing.
- VAT/tax.
- Cash fund.
- Bank account.
- AR/AP integration point.
- Journal/posting integration point.

Shared app:

- `ViewModelBase`.
- `RelayCommand`, `ObservableProperty`.
- `INavigationService`.
- `IWindowManagerService`.
- `IDialogService`.
- Command styles trong theme `EnterpriseModuleCmd...`.
- Table/card/list visual patterns từ Breakfast Cafe.
- Print preview flow và thermal receipt/ticket formatting.

## Những gì cần kế thừa

À La Carte:

- Kế thừa phần POS bàn, order, line, kitchen/bar ticket, payment, open order, đổi bàn, chia/gộp đơn.
- Thay `PosTable` bằng mô hình rõ hơn: `DiningRoom` + `DiningTable`.
- Bổ sung reservation, extra service, deposit, contract.
- Vẫn dùng `MenuItem` và `PreparationArea`.

Banquet:

- Kế thừa menu/recipe/inventory/customer/payment/deposit logic ở mức shared.
- Không kế thừa trực tiếp POS table vì banquet cần room/event/calendar/floor plan/timeline/checklist.
- Dùng contract và deposit nhiều lần.
- Lịch phòng phải kiểm tra trùng phòng, trùng giờ, vượt sức chứa.

## Rủi ro cần kiểm soát

- Một số file source đang có dấu hiệu mojibake khi đọc trực tiếp bằng PowerShell, đặc biệt text trong `NavigationService.cs`, `BreakfastCafeSettingsView.xaml`, `BreakfastCafeSalesView.xaml.cs`. Khi triển khai phải dùng UTF-8 chuẩn và hạn chế chỉnh các file này bằng thao tác ghi đè toàn file.
- Không nên copy toàn bộ `BreakfastCafeSalesViewModel` vì file lớn và dễ tạo lỗi dây chuyền.
- Kitchen ticket hiện không có line detail, cần thiết kế mới tốt hơn để in lại/hủy/in bổ sung chính xác.
- Accounting integration phải để dạng điểm nối, không tự tạo bút toán thật cho module mới theo yêu cầu.
- Floor plan kéo thả và calendar là UI phức tạp, nên triển khai theo hướng tối thiểu ổn định trước, sau đó nâng cấp tương tác.
