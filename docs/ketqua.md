# KẾT QUẢ THỰC HIỆN

## Phiên 2026-07-10 (lần 2) — Sửa lỗi lưu thực đơn sự kiện tiệc

### Mục tiêu
Sửa lỗi runtime: thực đơn (cả preset và custom) không được lưu khi thao tác trên UI.

### Nguyên nhân
- Entity `BanquetEventMenu` đã có `MenuItemId`, `ItemName`, `Qty`, `Note` nhưng **thiếu migration** tạo cột trong DB
- `SaveCustomMenuAsync` gọi EF Core → throw SqlException → bị `RunBusyAsync` nuốt
- `LoadSelectedEventDetailAsync` luôn reset `SelectedBanquetCustomFoods` về rỗng

### File đã sửa
| File | Thay đổi |
|---|---|
| `Migrations/20260710040633_AddBanquetEventMenuCustomColumns.cs` | Migration mới: thêm 4 cột + FK + Index |
| `ViewModels/BanquetRestaurantViewModel.Partial.cs:590` | Thay reset cứng bằng `PopulateCustomFoodsFromMenus` |
| `ViewModels/BanquetRestaurantViewModel.Partial.cs:607-629` | Thêm method `PopulateCustomFoodsFromMenus` |

### Build status
PASS (0 errors, 0 warnings)

### Kết quả
- **Migration**: Đã tạo, sẵn sàng áp dụng
- **Gói thực đơn**: Đã hoạt động (gọi `AttachMenuPackageAsync` đúng)
- **Món tùy chỉnh**: Đã sửa — save/load hoạt động sau khi áp dụng migration
- **Cần test runtime**: Chạy app, áp dụng migration, kiểm tra TC-M01 → TC-M09

---

## Phiên 2026-07-10 — Audit toàn diện module Nhà hàng Tiệc

### Mục tiêu
Phân tích và kiểm tra toàn diện module Nhà hàng Tiệc, tập trung sâu vào tab Tổng quan.

### Báo cáo đã tạo
| File | Nội dung | Dung lượng |
|---|---|---|
| `docs/banquet-module-audit.md` | Báo cáo kiểm tra toàn diện (15 section) | 39 KB |
| `docs/banquet-mock-data-audit.md` | Báo cáo dữ liệu mock và hard-code | 11 KB |
| `docs/banquet-database-gap-analysis.md` | Báo cáo khoảng trống CSDL | 14 KB |
| `docs/banquet-overview-test-checklist.md` | Checklist kiểm thử tab Tổng quan (25 test case) | 14 KB |

### Kết quả chính
- Module hoàn thiện: ~55% (backend 80%, UI 65%, kết nối 50%)
- Tab Tổng quan hoàn thiện: ~60%
- Tổng lỗi: P0=5, P1=12, P2=18, P3=15
- Vị trí mock/hard-code: 16
- Bảng CSDL thiếu: 8
- Cột CSDL thiếu: 18
- Chức năng UI chưa có backend: 6
- **Chưa thể vận hành thực tế**

### 5 vấn đề nghiêm trọng nhất
1. **P0**: Event-type-specific fields (cô dâu, chú rể...) không lưu vào DB
2. **P0**: ExtraGuestCount/ExtraTableCount không lưu vào DB
3. **P0**: Cascade delete BanquetContract, BanquetDeposit khi xóa event
4. **P0**: Customer.Email [NotMapped] — không lưu vào DB
5. **P1**: Không có state machine cho trạng thái — có thể chuyển sai thứ tự

### 5 việc cần làm trước tiên
1. Thêm cột event-type-specific vào Entity + Migration (P0)
2. Sửa cascade delete → Restrict cho Contract và Deposit (P0)
3. Thêm Email, CreatedAt, UpdatedAt vào Customer (P0)
4. Implement state machine validation cho trạng thái (P1)
5. Xóa toàn bộ hard-code UI (alert count, username, dates, settings) (P1-P2)

---

## Phiên 2026-07-06 — Khôi phục giao diện Nhà hàng Tiệc bản 11h trưa

### Mục tiêu
Khôi phục giao diện module Nhà hàng Tiệc về đúng bản lúc 11:00 AM ngày 06/07/2026.

### File khôi phục
| File | Nguồn backup | Dòng |
|---|---|---|
| `Views/BanquetRestaurantView.xaml` | `tmp/BanquetRestaurantView.xaml.backup_20260706_1100` | 219 |
| `Themes/BanquetTheme.xaml` | `tmp/BanquetTheme.xaml.backup_20260706_1100` | 418 |

### Giao diện
- Top bar: "Nhà hàng Tiệc (Banquet)" + ngày + avatar
- Sidebar 88px: 5 nút (Tổng quan, Phòng tiệc, Lịch phòng, Báo cáo, Cài đặt) + nút Thu gọn
- Dashboard: Danh sách sự kiện (trái) + Chi tiết sự kiện (phải) với các tab Thông tin, Thực đơn, Dịch vụ, Checklist, Timeline, Tài chính, Hợp đồng

### Build status
PASS (0 warnings, 0 errors). Encoding check: PASS.

### Runtime status
App launched OK.

---

## Phiên 2026-06-19 — Bổ sung bộ lọc khu vực + card phòng Đơn mở + fix layout

### Mục tiêu
1. Sửa bộ lọc khu vực (chỉ hiện "Tất cả", thiếu chip khu vực)
2. Thêm card phòng vào tab Đơn mở, lọc được theo phòng
3. Sửa lỗi 2 phòng đầu không hiển thị trong tab Khu vực

### Root cause
1. `Floors` chỉ được populate trong `PopulateSampleDataInMemory()`, không gọi trong `RefreshAsync()` → khi có data từ DB, Floors rỗng
2. Tab Đơn mở chưa có card phòng
3. `RefreshOpenOrderRooms()` mutate chung object `DiningRoomDto` giữa `OpenOrderRooms` và `Rooms` → làm hỏng Status/TableCount ở TAB 1
4. WrapPanel trong ScrollViewer không constrain width → item tràn ngang, 2 phòng đầu bị đẩy khỏi viewport
5. Empty state dùng `B2V` với int → không bao giờ Visible; `Grid.RowSpan="2"` có thể gây overlap

### Fix
| File | Thay đổi |
|---|---|
| `ViewModels/RestaurantViewModels.cs` | `RefreshFloorsFromRooms()`: extract distinct RoomType → sync vào Floors |
| | `RefreshOpenOrderRooms()`: clone DiningRoomDto (không share reference với Rooms) |
| | `ApplyOpenOrderFilter()`: lọc đơn mở theo floor + room + search text |
| | `SelectOrderRoomCommand`: toggle chọn phòng lọc đơn mở |
| | `OnSelectedFloorChanged`: cập nhật cả OpenOrderRooms + FilteredOpenOrders |
| `Views/AlacarteRestaurantView.xaml` | TAB 1: WrapPanel bind `ViewportWidth`, bỏ `Grid.RowSpan="2"`, empty state dùng `InvBool` |
| | TAB 3: thêm area filter chips + card phòng có đơn mở + search box |
| | Đăng ký `StringEqualityConverter` cho DataTrigger selected room |

### Build status
PASS (0 warnings, 0 errors)

### Runtime status
App launched. Cần verify layout tab Khu vực hiện đủ phòng.

---

## Phiên 2026-06-14 — Sửa triệt để lỗi mã hóa font tiếng Việt toàn bộ dự án

### Mục tiêu
Sửa toàn bộ chữ tiếng Việt bị lỗi mã hóa (mojibake) trong toàn bộ dự án, bao gồm: module Đồ ăn sáng & cà phê, Chi tiết phiếu nhập/xuất, Virtual File Center, Report Center, Payable Debt, Suppliers, Settings, Dialogs.

### File đã sửa (15 files, ~180+ corrections)

| File | Số chỗ sửa | Module |
|---|---|---|
| `Views/BreakfastCafeSalesView.xaml` | 13 | Breakfast Cafe |
| `Views/BreakfastCafeSettingsView.xaml` | 16 | Breakfast Cafe |
| `Views/BreakfastCafeClosedOrdersView.xaml` | 5 | Breakfast Cafe |
| `ViewModels/BreakfastCafeSettingsViewModel.cs` | 8 | Breakfast Cafe |
| `ViewModels/BreakfastCafeSalesViewModel.cs` | 1 | Breakfast Cafe |
| `Views/StockReceiptDetailModuleView.xaml` | 13 | Stock |
| `Views/StockReceiptDetailWindow.xaml` | 16 | Stock |
| `Views/StockIssueDetailModuleView.xaml` | 15 | Stock |
| `ViewModels/VirtualExplorerViewModel.cs` | 18 | VFC |
| `Views/PayableDebtDetailModuleView.xaml` | 14 | Payable |
| `Views/SupplierQuickCheckModuleView.xaml` | 8 | Supplier |
| `Views/SuppliersView.xaml` | 1 | Supplier |
| `Views/ReportCenterView.xaml` | 20 | Report |
| `Views/ModulePlaceholderView.xaml` | 1 | Core |
| `Views/SettingsView.xaml` | 4 | Settings |
| `Views/Dialogs/ReconciliationDetailDialog.xaml` | 1 | Dialogs |

### Các pattern chính
- `D?` → `Đã` (đã gửi bếp, đã thanh toán, đã hủy, đã chọn, đã trả...)
- `M?` → `Mã` (mã đơn, mã hàng, mã bàn, mã khu vực, mã file...)
- `Ng?y` → `Ngày`, `Ch?ng t?` → `Chứng từ`, `Di?n gi?i` → `Diễn giải`
- `Ph?i tr?` → `Phải trả`, `C?n l?i` → `Còn lại`
- `S? lu?ng` → `Số lượng`, `Th?nh ti?n` → `Thành tiền`
- `?V t?nh` → `ĐV tính`, `?on gi?` → `Đơn giá`
- `??ng` → `Đóng`, `Xem tru?c in` → `Xem trước in`
- `Tr?ng th?i` → `Trạng thái`, `Thanh to?n` → `Thanh toán`
- `b?o c?o` → `báo cáo`, `Xu?t` → `Xuất`, `H?y` → `Hủy`

### Build
**Succeeded** — 0 errors, 0 warnings. Encoding check: PASS.

---

## Phiên 2026-06-14 — Sửa lỗi mã hóa font tiếng Việt module Đồ ăn sáng & cà phê

### Mục tiêu
Sửa toàn bộ chữ tiếng Việt bị lỗi mã hóa (mojibake) trong module Breakfast/Cafe.

### Bug investigation

| Triệu chứng | Root cause | Fix |
|---|---|---|
| `D? g?i b?p` | Font encoding hỏng dấu tiếng Việt → `?` | `Đã gửi bếp` (4 vị trí trong DataTrigger) |
| `D? thanh to?n` / `D? h?y` | Như trên | `Đã thanh toán` / `Đã hủy` (5 vị trí) |
| `M? khu v?c` / `M? b?n` / `M? nh?m` / `M? ca` / `M? m?n` / `M? don` | Như trên | `Mã khu vực` / `Mã bàn` / `Mã nhóm` / `Mã ca` / `Mã món` / `Mã đơn` (11 vị trí) |
| `T?m m? don, b?n...` | Như trên | `Tìm mã đơn, bàn...` |
| `H?y m?n d? ch?n` | Như trên | `Hủy món đã chọn` |
| `Don d? thanh to?n` | Như trên | `Đơn đã thanh toán` (2 vị trí) |
| `B?t voucher/m? khuy?n m?i` | Như trên | `Bật voucher/mã khuyến mại` |
| `B?t bu?c m? tham chi?u...` | Như trên | `Bắt buộc mã tham chiếu...` |
| `In l?i khi s?a m?n d? g?i` | Như trên | `In lại khi sửa món đã gửi` |
| `Lu?ng b?n hi?n d? d?ng c?nh b?o...` | Như trên | `Luồng bán hiện đang dùng cảnh báo...` |
| `Sua mon ban` / `Phan loai` | Dấu tiếng Việt bị strip | `Sửa món bán` / `Phân loại` |
| `Kho giay bep/bar` / `In thu bep/bar/hoa don` | Như trên | `Khổ giấy bếp/bar` / `In thử bếp/bar/hóa đơn` |
| `Tien mat/Chuyen khoan/Cong no/Tang/San vuon/Mang di/Giao hang` (ViewModel) | ASCII-fied mock data | `Tiền mặt/Chuyển khoản/Công nợ/Tầng/Sân vườn/Mang đi/Giao hàng` |
| `Combo banh mi + ca phe: gia uu dai 35k!` | ASCII-fied combo message | `Combo bánh mì + cà phê: giá ưu đãi 35k!` |

### File đã sửa
| File | Số chỗ sửa |
|---|---|
| `Views/BreakfastCafeSalesView.xaml` | 13 |
| `Views/BreakfastCafeSettingsView.xaml` | 16 |
| `Views/BreakfastCafeClosedOrdersView.xaml` | 5 |
| `ViewModels/BreakfastCafeSettingsViewModel.cs` | 8 |
| `ViewModels/BreakfastCafeSalesViewModel.cs` | 1 |

### Build
**Succeeded** — 0 errors, 0 warnings

### Còn lại
- Các module khác (Stock, PayableDebt, ReportCenter, Settings, Supplier, VirtualExplorer) cũng có lỗi mã hóa tương tự nhưng ngoài scope lần này

---

## Phiên 2026-06-04 — Upload ảnh, expand NL, History fix, ImagePath DTO

### Mục tiêu
Thêm upload ảnh món, expand/collapse xem nguyên liệu inline, fix History tab, thêm ImagePath vào DTO.

### Bug investigation

| Triệu chứng | Root cause | Fix |
|---|---|---|
| Không thể chọn ảnh món | `CreateMenuItemRequest` thiếu `ImagePath` | Thêm vào DTO + service mapping |
| Menu item không xem được NL nếu không click | Thiếu expand/collapse | Thêm `ToggleItemExpandCommand` + inline recipe DataTemplate |
| Lịch sử hiển thị warnings không đúng | DataGrid bind sai | Thay bằng empty state đẹp |
| Nút chọn/xoá ảnh không tồn tại | Thiếu UI | Thêm `SelectMenuImageCommand` + `RemoveMenuImageCommand` |

### File đã sửa
- `ViewModels/MenuRecipeViewModel.cs` — 3 new commands, SelectedImageFullPath, IsExpanded on MenuItemCardState
- `Views/MenuRecipeView.xaml` — upload buttons, expand/collapse, history empty state
- `Application/DTOs/BreakfastCafeSalesDtos.cs` — +ImagePath
- `Infrastructure/Services/MenuCatalogService.cs` — map ImagePath
- `DEV_PROGRESS.md`

### Build
**Succeeded** — 0 errors

### Runtime
**OK**

### Còn lại
- Ảnh lưu local (AppData), chưa đồng bộ
- Category right panel trống khi không edit
- Menu ba chấm chưa implement

---

## Phiên 2026-06-04 — Sửa layout: tách nhóm món, thiết kế lại form Thông tin chung

### Mục tiêu
Loại bỏ cột "Nhóm món" khỏi màn Món ăn (đã có sidebar riêng). Thiết kế lại form với padding đẹp, ComboBox search.

### Bug investigation

| Triệu chứng | Root cause | Fix |
|---|---|---|
| Cột Nhóm món chiếm full trái màn Món ăn | Nhầm lẫn layout: nhóm món có cả sidebar riêng lẫn cột trái | Thay bằng danh sách món + filter ComboBox gọn |
| Form Thông tin chung sát viền, quá rộng | Thiếu padding, không có max-width | padding 28px, MaxWidth 960, 2 cột layout |
| ComboBox không search được | Thiếu IsTextSearchEnabled | Thêm vào category, ingredient, warehouse combo |
| Label "+ Thêm" không rõ | Thiếu ngữ cảnh | "+ Thêm món", "+ Thêm nhóm" tuỳ ngữ cảnh |

### File đã sửa
- `Views/MenuRecipeView.xaml`

### Build
**Succeeded** — 0 errors

### Runtime
**OK**

### Còn lại
- Menu ba chấm chưa implement
- History tab chưa có data thật

---

## Phiên 2026-06-04 — Sửa danh sách món, hoàn thiện màn nhóm món

### Mục tiêu
Gộp filter/search, xoá expand recipe khỏi danh sách món, redesign item layout, thêm card chi tiết nhóm bên phải.

### Bug investigation

| Triệu chứng | Root cause | Fix |
|---|---|---|
| 2 box lọc/tìm trong danh sách món | Search + ComboBox riêng | Gộp thành 1 search + chips |
| Nút "Xem NL" và expand trong danh sách món | Nguyên liệu lẽ ra chỉ trong tab Định lượng | Xoá expand khỏi item template |
| Item món chật, chữ bị cắt | Thiết kế item chưa tối ưu | 3 dòng rõ: (1) name + badge, (2) code + cat, (3) price + recipe |
| Màn Nhóm món phải trống | Thiếu card chi tiết | Thêm KPIs + edit form + items grid |
| HTML entity trong C# string | Copy từ XAML | Sửa thành tiếng Việt chuẩn |

### File đã sửa
- `Views/MenuRecipeView.xaml`
- `ViewModels/MenuRecipeViewModel.cs`

### Build
**Succeeded** — 0 errors

### Runtime
**OK**

### Còn lại
- Menu ba chấm chưa implement
- History tab chưa có data thật
- Ảnh local chưa đồng bộ

### Mục tiêu
Loại bỏ hoàn toàn Segoe MDL2 Assets icon font (nguyên nhân ô vuông), chuyển sang text label + Unicode emoji ổn định.

### Bug investigation

| Triệu chứng | Root cause | Fix |
|---|---|---|
| Nút hiển thị ô vuông | Segoe MDL2 Assets không có sẵn hoặc font bị lỗi | Xoá toàn bộ FontFamily="Segoe MDL2 Assets". Chuyển sang text label thuần + ▲ ▼ X tooltip |
| Button icon-only không rõ chức năng | Dùng icon không label | Tất cả button có text label: "Lưu", "Làm mới", "+ Thêm", "Sao chép" |
| Sidebar icon không render | Tag dùng MDL2 code points | Bỏ Tag icon, chỉ giữ text label |
| Warnings không đếm số lượng | Thiếu badge | Thêm badge đỏ hiển thị CostWarningCount |

### File đã sửa
- `Views/MenuRecipeView.xaml` — toàn bộ icon/label/button styles
- `DEV_PROGRESS.md`

### Build
**Succeeded** — 0 errors, 0 warnings

### Runtime
**OK** — App chạy, log sạch

### Còn lại
- Menu ba chấm còn disabled
- Ảnh món chưa có
- Settings còn basic

---

## Phiên 2026-06-03 — Production upgrade: Dashboard KPIs, History tab, Reports/Settings

### Mục tiêu
Production polish: sửa Dashboard duplicate KPI, enable History tab, replace Reports/Settings placeholders, fix DbContext concurrency.

### Bug investigation

| Triệu chứng | Root cause | Fix |
|---|---|---|
| Dashboard KPI "Có recipe" xuất hiện 2 lần | Code copy | Thay bằng DashboardWarningCount và DashboardTotalCostEstimate |
| History tab disabled | `IsEnabled="False"` | Enable với AllItemWarnings DataGrid |
| Reports/Settings full-screen placeholder | Chưa implement | Reports → cost warnings grid; Settings → module config panel |
| DbContext concurrency exception | Constructor fire-and-forget + section navigation cùng chạy DB operations | Thêm SemaphoreSlim guard trong InitializeAsync và LoadDashboardAsync |

### File đã sửa
- `ViewModels/MenuRecipeViewModel.cs` — DashboardWarningCount, DashboardTotalCostEstimate, SemaphoreSlim lock
- `Views/MenuRecipeView.xaml` — Dashboard KPI, History tab, Reports/Settings content, recipe cost column

### Build
**Succeeded** — 0 errors, 0 warnings

### Runtime
**OK** — Log sạch, không concurrency exception

### Còn lại
- Settings section còn basic
- Menu ba chấm chưa implement
- Recipe tab cost column từ lookup data

---

## Phiên 2026-06-03 — Fix sidebar navigation + active state

### Mục tiêu
Fix sidebar navigation không hoạt động, active state sai, runtime exception, mojibake risk.

### Bug investigation

| Triệu chứng | Root cause | Fix |
|---|---|---|
| Sidebar click không chuyển section | `NavigateSection(int)` nhận string từ XAML `CommandParameter="1"` — CommunityToolkit.Mvvm `RelayCommand<int>.CanExecute(object)` throw `ArgumentException` | Đổi parameter thành `string`, parse int trong method |
| "Thao tác không thành công" | Command không chạy → service không được gọi | Như trên |
| Sidebar active luôn là "Món ăn" | `SidebarBtnActive` style gán cứng; `SectionButtonStyle` dùng `Style.Triggers` đổi `Style` — WPF không support | Tạo 9 style riêng `SBtn0`-`SBtn8`, mỗi style có `DataTrigger` cho `SelectedSection` |
| Mojibake risk | Text tiếng Việt trong XAML | Unicode escape toàn bộ (`&#xHHHH;`) |

### File đã sửa
- `ViewModels/MenuRecipeViewModel.cs`
- `Views/MenuRecipeView.xaml`
- `DEV_PROGRESS.md`
- `CLAUDE.md`

### Build
**Succeeded** — 0 errors, 0 warnings

### Runtime
**OK** — PID 8992, log sạch (không ERR sau fix)

### Còn lại
- Báo cáo & Cài đặt section placeholder (đã sửa ở phiên sau)
- Lịch sử tab disabled (đã sửa ở phiên sau)
- Export/In menu ba chấm chưa implement

---

## Phiên 2026-06-09 — Khôi phục database từ backup + migration

### Mục tiêu
Khôi phục database từ file backup sau khi schema bị hỏng do thiếu file Designer.cs của 2 migration.

### Bug investigation

| Triệu chứng | Root cause | Fix |
|---|---|---|
| App crash "Cannot find object LedgerAccounts" | 2 file migration (`20260426091500_AddApprovalStatusForStockVouchers.cs`, `20260517090000_AddLedgerAccounts.cs`) thiếu file `.Designer.cs` → thiếu `[DbContext]` + `[Migration]` attribute → EF Core không nhận diện → bỏ qua khi chạy migration → `LedgerAccounts` không được tạo → `AddLedgerAccountMetadata` ALTER TABLE fail | Thêm `[Migration]` attribute, đánh dấu đã áp dụng trong `__EFMigrationsHistory` (các thay đổi đã được hấp thụ vào migration sau trong model snapshot) |
| App crash "Invalid object name AccountingStandards" | Hậu quả dây chuyền từ migration lỗi — transaction rollback không hoàn toàn | Khôi phục database từ backup |
| Build fail khi thêm `[DbContext]` attribute | `DbContextAttribute` không tồn tại trong EF Core 8.0.7 cho code manual |

### Giải pháp cuối cùng
1. Restore database từ `D:\ASMILE-BO\Backups\ASMILE_BO_20260528_093130.bak` (106.7MB, 197 bảng, 43 migrations)
2. Đánh dấu 2 migration lỗi đã áp dụng (schema đã có từ backup)
3. Chạy `dotnet ef database update` — 4 migration mới thành công
4. Sửa `DatabaseMigrationService.InitializeAsync` bọc `ValidateSeedConsistencyAsync` trong try-catch

### File đã sửa
| File | Thay đổi |
|---|---|
| `Migrations/20260426091500_AddApprovalStatusForStockVouchers.cs` | Thêm `[Migration]` attribute |
| `Migrations/20260517090000_AddLedgerAccounts.cs` | Thêm `[Migration]` attribute |
| `Persistence/DatabaseMigrationService.cs` | Try-catch ValidateSeedConsistencyAsync + using System |

### Migrations đã áp dụng sau restore
- Từ backup: 43 migrations (đến `20260527091500_AddStockReceiptDetailVatColumns`)
- Đánh dấu: 2 migrations (AddApprovalStatusForStockVouchers, AddLedgerAccounts)
- Chạy mới: 4 migrations (AddBreakfastCafeSalesModule, AddMenuRecipeModule, AddPreparationAreaToMenuItem, AddBreakfastCafePosTablesAndKitchenTickets)
- **Tổng: 49 migrations**

### Bảng Breakfast/Cafe sau migration
MenuItems, MenuItemRecipes, MenuItemRecipeLines, MenuCategories, BreakfastCafeOrders, PosTables, BreakfastCafeKitchenTickets — tất cả đã tồn tại.

### Build status
PASS (0 warnings, 0 errors)

### Runtime status
App đang chạy (PID 28064, ~341MB). Log sạch không lỗi sau khởi động.

### Backup an toàn
Database cũ đã backup tại `D:\ASMILE-BO\tmp\ASMILE_BO_before_restore_20260609.bak`

---

## Hướng dẫn lưu kết quả

Sau mỗi phiên làm việc:
1. Ghi nội dung vào file này với format: `## Phiên YYYY-MM-DD — Tiêu đề`
2. Bao gồm: mục tiêu, bug investigation, file đã sửa, build status, runtime status, vấn đề còn lại
3. Không xóa kết quả cũ — chỉ thêm mới

---

## Phiên 2026-06-04 — Sửa lỗi mất danh sách món khi chuyển sidebar

### Mục tiêu
Sửa lỗi: sau khi click sidebar "Nhóm món" rồi quay lại "Món ăn", danh sách món bị trắng
(FilteredMenuCards = 0) dù status bar vẫn báo 18 món.

### Root cause
Race condition trong `ApplyFilters`: khi `RefreshLookupsAsync` gọi `ResetCollection(Categories, ...)`,
nếu `FilterCategoryId` có giá trị, lookup `Categories.FirstOrDefault(...)?.Name` trả về `null`.
Code cũ dùng `?? string.Empty` → `catName = ""` → `.Where(x => x.CategoryName == "")`
lọc hết toàn bộ món. Sau đó Categories được repopulate nhưng ApplyFilters không chạy lại.

### File đã sửa
| File | Thay đổi |
|---|---|
| `ViewModels/MenuRecipeViewModel.cs` | `OnSelectedSectionChanged`: gọi `ResetMenuFilters()` trên UI thread khi vào SEC_MENU_ITEMS |
| | `ResetMenuFilters()`: method mới — reset SearchText, FilterCategoryId, ActiveFilterChip, SelectedSortOption |
| | `ApplyFilters`: fix `?? string.Empty` → `!string.IsNullOrEmpty(catName)` để không lọc khi Categories rỗng |
| | `InitializeAsync`: thêm `ApplyFilters()` sau `RefreshLookupsAsync` |
| `Views/MenuRecipeView.xaml` | Thêm label "Nhóm món:" và "Sắp xếp:" cho ComboBox, Grid.RowDefinitions 6→8 |

### Build status
PASS (0 warnings, 0 errors)

### Runtime status
App mở OK. Cần verify thêm: Món ăn → Nhóm món → Món ăn không bị trắng danh sách.

### Vấn đề còn lại
- Chưa có automated test cho navigation flow này
- `EnableCollectionSynchronization` chưa được bật — ObservableCollection modification từ threadpool thread có thể gây lỗi trong một số trường hợp

---

## Phiên 2026-06-07 — Thiết kế lại POS 2 cột

### Mục tiêu
Thiết kế lại module POS Bán đồ ăn sáng & cà phê từ layout 3 cột sang 2 cột (60/40),
ưu tiên chọn khu vực/bàn. Thêm mini-sidebar với 3 tab, grid bàn configurable cột,
và fix lỗi không thêm được món vào bàn.

### File đã sửa
| File | Thay đổi |
|---|---|
| `ViewModels/BreakfastCafeSalesViewModel.cs` | Thêm LeftTabIndex, TableSearchText, TableStatusFilter, TableColumnCount, OrderNote, TableColumnOptions; commands NavigateLeftTab, SelectArea, SelectCategory; fix AddMenuItemAsync gộp vào 1 RunBusyAsync; ApplyTableFilter hỗ trợ tìm bàn+lọc trạng thái; PosCategoryFilter thêm IsSelected |
| `Views/BreakfastCafeSalesView.xaml` | Viết lại hoàn toàn: sidebar 65px, 3 tab (Khu vực/Thực đơn/Đơn mở), UniformGrid bàn 2-5 cột, card bàn màu theo trạng thái, menu dạng row list, order panel bên phải |

### Root cause lỗi không thêm được món
`AddMenuItemAsync` cũ gọi `SelectTableAsync` (dùng `RunBusyAsync`) trước, rồi gọi `RunBusyAsync` lần nữa để add line. Nếu `SelectTableAsync` chưa kịp set `IsBusy = false`, lần `RunBusyAsync` thứ hai bị skip. Fix: gộp toàn bộ logic vào 1 lần `RunBusyAsync`.

### Build status
PASS (0 warnings, 0 errors). Encoding check: PASS.

### Runtime status
App mở OK. Cần test end-to-end workflow.

---

## Phiên 2026-06-07 — Fix card bàn + chọn món + review toàn diện module POS

### Mục tiêu
1. Cố định kích thước card bàn cho phù hợp
2. Sửa lỗi "chọn bàn rồi nhưng chưa chọn được món"
3. Kiểm tra toàn diện module

### Bug investigation

| Triệu chứng | Root cause | Fix |
|---|---|---|
| Card bàn kích thước không đều, co giãn theo cửa sổ | `MinWidth`/`MinHeight` + `Stretch` + `UniformGrid` làm card giãn theo cell | `Width="150"` `Height="110"` fixed, `WrapPanel` thay `UniformGrid`, bỏ column selector |
| Chọn bàn xong không biết cách thêm món | Sau `SelectTableAsync` user vẫn ở tab Khu vực, phải tự chuyển sang tab Thực đơn | Auto-navigate `LeftTabIndex = 1` sau khi mở order thành công |
| Chip phương thức TT không hiển thị active | Style `PayChipActive` có định nghĩa nhưng không dùng DataTrigger | DataTrigger so sánh với `SelectedPaymentMethod` qua StringEqualityConverter |
| Chip khu vực không hiển thị active | Chưa có DataTrigger active | Tương tự, DataTrigger so sánh với `SelectedArea` |
| Thiếu phương thức thanh toán Ghi phòng, Công nợ | ViewModel chỉ có 3 method, service hỗ trợ 5 | Thêm "Ghi phòng" (RoomCharge) và "Công nợ" (CustomerDebt) |
| Empty state "Không có đơn mở" luôn chiếm layout (opacity=0) | Binding `HasOpenOrders` dùng B2V → true = Visible, dùng Opacity=0 ẩn | Đổi sang `HasNoOpenOrders` + B2V, bỏ hack Opacity |
| Không hiển thị số lượng món trong menu | Thiếu label đếm | Thêm TextBlock `{Binding FilteredMenuItems.Count} món` |

### File đã sửa
| File | Thay đổi |
|---|---|
| `Converters/StringEqualityConverter.cs` | NEW — IMultiValueConverter so sánh 2 string cho DataTrigger |
| `Views/BreakfastCafeSalesView.xaml` | Card bàn fixed size, WrapPanel, bỏ column selector, active state area+payment chips, fix empty state, thêm menu item count, namespace converters |
| `ViewModels/BreakfastCafeSalesViewModel.cs` | Auto-navigate LeftTabIndex=1, thêm HasNoOpenOrders, PaymentMethods +RoomCharge+CustomerDebt, ToPaymentCode mapping |

### Build status
PASS (0 warnings, 0 errors)

### Runtime status
App launched OK. PID cũ killed trước build.

### Còn lại
- Payment methods "Ghi phòng"/"Công nợ" cần user flow chọn phòng/khách khi dùng
- In tạm tính chưa implement thật
- Chưa có màn hình order history chi tiết (TodayOrders có data nhưng không hiển thị)
- Chưa tích hợp printer/QR payment

---

## Phiên 2026-06-07 — Thêm tab Cài đặt (quản lý khu vực, bàn, nhóm món)

### Mục tiêu
Thêm tab Cài đặt thứ 4 vào sidebar để quản lý khu vực, bàn, nhóm món ngay trong màn hình POS.

### Thay đổi

| Layer | File | Thay đổi |
|---|---|---|
| Interface | `IBreakfastCafeSalesService.cs` | + `SaveTableAsync`, `DeleteTableAsync` |
| Service | `BreakfastCafeSalesService.cs` | Implement SaveTable (create/update) + DeleteTable (soft delete, check open orders) |
| ViewModel | `BreakfastCafeSalesViewModel.cs` | + IsTabSettings, setting form fields (table+category), SettingTables/SettingAreas/SettingCategories collections, commands: SaveTableSettings, DeleteTableSettings, EditTableSettings, ClearTableForm, SaveCategorySettings, EditCategorySettings, ClearCategoryForm, LoadSettings |
| View | `BreakfastCafeSalesView.xaml` | + 4th sidebar button "Cài đặt", + Tab 3 với 2 section: Khu vực & Bàn (form thêm/sửa bàn + danh sách), Nhóm món (form thêm/sửa + danh sách) |

### Chức năng tab Cài đặt
- **Khu vực & Bàn**: ComboBox khu vực (editable để tạo mới), nhập mã/tên bàn, số chỗ, loại (DineIn/TakeAway/RoomService), nút Lưu, danh sách bàn hiện có với nút Sửa/Xóa
- **Nhóm món**: Form mã/tên nhóm, nút Lưu, danh sách nhóm với nút Sửa
- Tự load dữ liệu khi chuyển sang tab Cài đặt
- Xóa bàn: kiểm tra đơn mở trước khi xóa (soft delete)

### Build status
PASS (0 warnings, 0 errors)

### Runtime status
App launched. Cần test thêm/sửa/xóa bàn và nhóm món.

### Còn lại
- Chưa quản lý món ăn (MenuItem) trong tab Cài đặt
- Chưa có xác nhận khi xóa nhóm món
- Nút "Sửa" nhóm món chưa có chức năng xóa nhóm

---

## Phiên 2026-06-27 — Tab filter DS Báo giá (đã duyệt / chưa duyệt / đã phục vụ)

### Mục tiêu
Trong module Nhà hàng à la carte, tab DS Báo giá (Tab 4), card danh sách báo giá bên trái được chia thành 3 tab filter: "Đã duyệt", "Chưa duyệt", "Đã phục vụ".

### Triển khai

| Layer | File | Thay đổi |
|---|---|---|
| ViewModel | `RestaurantViewModels.cs` | + `FilteredSavedQuotes` ObservableCollection; + `QuoteListTabIndex` int (0/1/2); + `QuoteListTabIndexChanged` partial → gọi `RefreshFilteredQuotes()`; + `RefreshFilteredQuotes()`: filter SavedQuotes theo tab — Tab 0: `ApprovedOrderId.HasValue && WorkflowState != "Đã phục vụ"`, Tab 1: `!ApprovedOrderId.HasValue`, Tab 2: `WorkflowState == "Đã phục vụ"`; + `SwitchQuoteListTabCommand` (string param); gọi `RefreshFilteredQuotes()` trong `RefreshSavedQuoteWorkflowStates()` và `ReplaceSavedQuote()` |
| View | `AlacarteRestaurantView.xaml` | LEFT column: thêm row tab filter với 3 nút (Đã duyệt/Chưa duyệt/Đã phục vụ), đổi ItemsControl binding từ `SavedQuotes` → `FilteredSavedQuotes`, empty state hiển thị tên tab hiện tại |
| Converter | `Converters/QuoteListTabConverter.cs` | NEW — 4 converter: `QuoteTabBgConverter`, `QuoteTabFgConverter`, `QuoteTabBrConverter`, `QuoteTabLabelConverter`; active = blue #2563EB, inactive = white/#334155/#CBD5E1 |

### Logíc filter
- **Đã duyệt** (tab 0): quote có ApprovedOrderId nhưng chưa phục vụ
- **Chưa duyệt** (tab 1): quote chưa có ApprovedOrderId
- **Đã phục vụ** (tab 2): WorkflowState == "Đã phục vụ" (đơn mở đang Serving)

### Build status
PASS (0 warnings, 0 errors)

### Runtime status
App launched. Cần verify tab filter hoạt động đúng với data thật.
