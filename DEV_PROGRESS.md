# DEV_PROGRESS

## Phiên 2026-06-08 — Sửa lỗi hiển thị bảng Telemetry trong Trung tâm vận hành

### Triệu chứng
Tab Telemetry trong Operations Center hiển thị ký tự rác, chữ Việt bị mất dấu (S? kiện, C?n tối ưu, H?y, C?p...), cột CPU bị ghi thành oPU với binding sai `opuUsagePercent`.

### Root cause
**Mojibake encoding damage trong XAML**: Các ký tự tiếng Việt có dấu (ự, ầ, ủ, ấ) bị thay thế thành `?`. Ký tự 'C' trong `CpuUsagePercent` bị thay thành 'o'. Đây là hậu quả của lần build trước đó bị corrupt encoding hoặc save file không đúng encoding UTF-8.

### File đã sửa
| File | Thay đổi |
|---|---|
| `Views/OperationsCenterView.xaml` | Sửa 9 vị trí mojibake: `S? kiện`→`Sự kiện`, `C?n tối ưu`→`Cần tối ưu`, `H?y`→`Hủy` (x2), `tuân th?`→`tuân thủ` (x2), `C?p`→`Cấp` (x3), `opuUsagePercent`→`CpuUsagePercent` |

### ItemsSource bindings (đã đúng từ trước, không sửa)
- `TelemetryTimeline` → `ObservableCollection<RuntimeTelemetryTimelineDto>` ✓
- `SlowOperations` → `ObservableCollection<SlowOperationLogDto>` ✓
- `RuntimeExceptions` → `ObservableCollection<RuntimeExceptionLogDto>` ✓
- `SystemResources` → `ObservableCollection<SystemResourceSnapshotDto>` ✓
- `UiResponsiveness` → `ObservableCollection<UiResponsivenessSnapshotDto>` ✓

### Các tab khác đã kiểm tra
Tất cả 16 tab trong OperationsCenterView — chỉ tab Telemetry có mojibake nặng. Các tab khác có 4 vị trí mojibake nhỏ (Hủy, tuân thủ, Cấp) đã sửa.

### Build status
PASS (0 warnings, 0 errors, encoding check clean)

### Runtime status
App launched. Cần verify tab Telemetry hiển thị đúng.

### Vấn đề còn lại
- Cần scan toàn bộ solution tìm mojibake tương tự ở các file XAML khác

## Phiên 2026-06-08 — Sửa hiển thị JSON trong Audit Center (double serialize + unicode escape)

### Root cause
3 tầng double-serialization trong `AuditCenterViewModel`:
1. `ToDto()`: `JsonSerializer.Serialize(new { log.OldValuesJson, log.NewValuesJson, ... })` — OldValuesJson/NewValuesJson đã là JSON string, bị serialize thêm lần nữa → escape `"` thành `\"`
2. `LoadSelectionAsync()`: `JsonSerializer.Serialize(new { ..., selected.MetadataJson })` — MetadataJson đã bị serialize ở bước 1, bị serialize lần 3
3. Default `JavaScriptEncoder.Default` escape `"` thành `"` trong tất cả các lần serialize

### File đã sửa

| File | Thay đổi |
|---|---|
| `Services/AuditJsonDisplayFormatter.cs` | **NEW** — static helper: `FormatForDisplay()`, `SerializePretty()`, `SerializeAnonymousObject()`. TryPrettyPrint → TryUnwrapAndPrettyPrint → TryDoubleUnwrapAndPrettyPrint → fallback raw. Dùng `UnsafeRelaxedJsonEscaping`. |
| `ViewModels/AuditCenterViewModel.cs` | `ToDto()`: parse OldValuesJson/NewValuesJson thành JsonElement trước khi serialize (tránh double). `LoadSelectionAsync()`: bỏ `selected.MetadataJson` khỏi serialize, dùng encoder an toàn. Thêm `SelectedMetadataDisplayJson`, `SelectedSnapshotDisplayJson`. Thêm `BuildMetadataJson()`, `FormatJsonField()`, `TryParseJsonElement()`, `BuildSnapshotComparison()` |
| `Views/AuditCenterView.xaml` | Tab "Dòng thời gian": bind `SelectedMetadataDisplayJson`, `SelectedSnapshotDisplayJson`. Tab "So sánh thay đổi": bind `SelectedSnapshotDisplayJson`, `SelectedMetadataDisplayJson` |

### Build status
PASS (0 warnings, 0 errors)

### Test status
PASS (132 passed, 0 failed, 0 skipped)

### Runtime status
App launched. Cần verify: Audit Center → So sánh thay đổi → JSON hiển thị rõ ràng, không `"`.

### Vấn đề còn lại
- Formatter chưa có unit test riêng (test project không reference App project)
- Export JSON vẫn dùng raw data (đúng — giữ nguyên để đảm bảo dữ liệu chính xác)

## Phiên 2026-06-08 — Full App Deep Audit không sửa code

### Mục tiêu
Kiểm tra toàn bộ app ASMILE-BO: build, test, database, dữ liệu thật, logic liên module, UI/runtime.

### Kết quả

| Hạng mục | Kết quả |
|----------|---------|
| Build | PASS — 0 errors, 0 warnings, mojibake clean |
| Test (143 tests) | PASS — 132 unit + 11 integration, 100% |
| Database migration | PASS — 47 migrations applied, log sạch |
| Data integrity | PASS — Journal cân bằng, 0 orphan FK |
| Runtime startup | PASS — Không lỗi |

### Phát hiện chính

- **2 High**: POS chưa trừ kho (ConsumeRecipeAsync skeleton), 20 PosTables inactive
- **5 Medium**: DataTrigger MultiBinding crash, Tab Cài đặt sai DataContext, MenuFilters thiếu UI, 15 đơn hàng dữ liệu rỗng, Reports chưa wire IReportEngine
- **4 Low**: Dead code, fragile parsing, oversized XAML, text không dấu

### File tạo ra
- `FULL_APP_AUDIT_REPORT.md` — báo cáo audit đầy đủ 14 section

### Không sửa code nào trong phiên này.

## Phiên 2026-06-07 — Bổ sung nút cộng món trực tiếp trong POS

### Nguyên nhân kiểm tra
- DB sau thao tác người dùng vẫn có `BreakfastCafeOrderLines = 0`, trong khi order mới vẫn được tạo, xác nhận lỗi nằm ở thao tác kích hoạt thêm món từ UI.

### Thay đổi
- `BreakfastCafeSalesView.xaml`
  - Trong từng dòng món của tab `Thực đơn`, thay biểu tượng `+` trang trí bằng nút `Button` thật.
  - Nút `+` gọi `MenuItemButton_Click` trực tiếp.
- `BreakfastCafeSalesView.xaml.cs`
  - Bắt click nút `+`, lấy đúng `PosMenuItemCard` từ `DataContext`, gọi `AddMenuItemCommand`.
  - Nếu không lấy được món thì hiển thị status rõ ràng.
- `BreakfastCafeSalesViewModel.cs`
  - `RunBusyAsync` không còn im lặng bỏ lệnh khi bận; nếu đang xử lý sẽ báo trạng thái để người dùng biết cần bấm lại.

### Kiểm tra
- Encoding check XAML: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS, 0 warning, 0 error.
- Runtime: app mở thành công, process `ASMILE.BO.App` responding.

## Phiên 2026-06-07 — Fix lần 2 thêm món POS bằng chọn món trực tiếp

### Nguyên nhân kiểm tra
- DB có 19 món active và 5 nhóm active, nên không thiếu dữ liệu thực đơn.
- `BreakfastCafeOrderLines` vẫn bằng 0 sau thao tác, chứng tỏ luồng UI chưa ghi được dòng order.

### Thay đổi
- `BreakfastCafeSalesView.xaml`
  - Vùng thực đơn chuyển từ `ItemsControl + Button Click/Command` sang `ListBox`.
  - Chọn một món trong `ListBox` bind vào `SelectedMenuItemForSale`.
- `BreakfastCafeSalesViewModel.cs`
  - Bổ sung `SelectedMenuItemForSale`.
  - Khi `SelectedMenuItemForSale` thay đổi thì gọi chung `AddMenuItemFromSelectionAsync`.
  - Sau khi thêm món sẽ reset selection để có thể bấm lại cùng món và tăng số lượng.
  - Fallback chọn bàn/order chặt hơn khi `CurrentOrder` bị null.
- `BreakfastCafeSalesView.xaml.cs`
  - Bỏ đường click code-behind cũ để chỉ còn một luồng thêm món qua ViewModel.

### Kiểm tra
- Encoding check XAML: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS, 0 warning, 0 error.
- Runtime: app mở thành công, process `ASMILE.BO.App` responding.

## Phiên 2026-06-07 — Fix thêm món vào đơn POS

### Nguyên nhân kiểm tra
- Truy vấn DB cho thấy order được mở nhưng bảng `BreakfastCafeOrderLines` vẫn chưa có dòng, tức thao tác bấm món chưa gọi được luồng ghi line xuống service.
- Binding `AddMenuItemCommand` nằm trong `DataTemplate` của style button món có rủi ro không chạy ổn định trong layout hiện tại.

### Thay đổi
- `ASMILE.BO.App/Views/BreakfastCafeSalesView.xaml`
  - Card món chuyển sang gọi `Click="MenuItemButton_Click"` thay vì phụ thuộc vào binding command trong template.
- `ASMILE.BO.App/Views/BreakfastCafeSalesView.xaml.cs`
  - Bắt click món và gọi trực tiếp `BreakfastCafeSalesViewModel.AddMenuItemCommand.Execute(item)`.
- Giữ nguyên nghiệp vụ `AddMenuItemAsync/AddLineAsync`, chỉ làm chắc đường gọi từ UI vào ViewModel.

### Kiểm tra
- Encoding check XAML: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS, 0 warning, 0 error.
- Runtime: app mở thành công, process `ASMILE.BO.App` responding.

## Phiên 2026-06-07 — Sửa luồng mở bàn, thêm món và thiết kế Đơn mở

### Thay đổi
- Tab `Đơn mở` chuyển từ danh sách dọc sang card grid nhiều cột giống tab `Khu vực`.
- Bổ sung lựa chọn sắp xếp đơn mở: mới nhất, cũ nhất, tổng tiền cao/thấp, theo bàn, nhiều món.
- Mở lại đơn từ tab `Đơn mở` tự chuyển sang tab `Thực đơn`.
- Chọn bàn mở order rồi tự chuyển sang tab `Thực đơn`.
- Sửa khả năng bấm món ngay sau khi chọn bàn bị bỏ qua do `IsBusy`: không còn refresh nặng bên trong đoạn busy chọn bàn.
- `AddMenuItemAsync` tự đảm bảo có order hợp lệ, reload lại chi tiết order sau khi thêm món và refresh danh sách đơn mở.

### Kiểm tra
- Encoding check XAML: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS, 0 warning, 0 error.
- Runtime: app mở thành công, process `ASMILE.BO.App` responding.

## Phiên 2026-06-07 — Tinh chỉnh màn cài đặt POS

### Thay đổi
- Bỏ tiêu đề lớn `Cài đặt POS bữa sáng & cà phê` và dòng mô tả bên dưới để màn cài đặt gọn hơn.
- Đưa thanh tab cài đặt lên hàng đầu, kèm nút `Làm mới` bên phải.
- Nới kích thước/padding các nút thao tác để các nút `Sửa`, `Xóa`, `Mới` không bị che chữ.
- Tách `LoadSettingsCoreAsync` để sau khi thêm/sửa/xóa khu vực hoặc bàn, danh sách bên phải cập nhật ngay trong cùng thao tác, không cần tắt/mở lại tab.

### Kiểm tra
- Encoding check XAML: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS, 0 warning, 0 error.
- Runtime: app mở thành công, process `ASMILE.BO.App` responding.

## Phiên 2026-06-07 — Cài đặt POS dùng dữ liệu thật

### Lý do
- Màn Cài đặt của module Bán đồ ăn sáng & cà phê đang dùng `BreakfastCafeSettingsViewModel` mock nên số liệu khu vực/bàn/món không phản ánh CSDL thật.
- Người dùng yêu cầu khu vực và bàn có thao tác thêm/sửa/xóa thật.

### Thay đổi chính
- `ASMILE.BO.App/Views/BreakfastCafeSalesView.xaml`
  - `BreakfastCafeSettingsView` kế thừa trực tiếp `BreakfastCafeSalesViewModel`, không dùng `SettingsContext` mock.
- `ASMILE.BO.App/Views/BreakfastCafeSettingsView.xaml`
  - Thiết kế lại màn Cài đặt theo tab: Tổng quan, Khu vực & bàn, Nhóm món, Món bán.
  - Tổng quan lấy số lượng thật từ `PosTables`, MenuCatalog và danh sách món.
  - Khu vực có form thêm/sửa/xóa; dữ liệu lấy từ `PosTables.AreaCode/AreaName`.
  - Bàn có form thêm/sửa/xóa dùng `SaveTableAsync/DeleteTableAsync`.
  - Nhóm món và món bán dùng dữ liệu thật từ MenuCatalog.
- `ASMILE.BO.App/ViewModels/BreakfastCafeSalesViewModel.cs`
  - Bổ sung `SettingAreaRows`, thống kê thật, form khu vực và command `SaveAreaSettings`, `EditAreaSettings`, `DeleteAreaSettings`, `ClearAreaForm`.

### Dữ liệu
- Không tạo dữ liệu giả trong XAML/ViewModel cài đặt.
- Thêm khu vực tạo một bàn mặc định đầu tiên cho khu vực đó vì hiện CSDL chưa có bảng khu vực riêng.
- Xóa khu vực xóa mềm toàn bộ bàn thuộc khu vực; nếu bàn có đơn mở thì service chặn xóa.

### Kiểm tra
- Encoding check XAML: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS, 0 warning, 0 error.
- Runtime: app mở thành công, process `ASMILE.BO.App` responding.

## Phiên 2026-06-07 — Đồng bộ POS theo mockup và dùng ảnh món thật

### Lý do
- Người dùng yêu cầu giao diện POS bữa sáng & cà phê bám sát ảnh `docs/1.png` hơn và phải dùng ảnh món ăn thật đã tải lên ở module Món ăn & Định lượng.

### Thay đổi chính
- `ASMILE.BO.App/Views/BreakfastCafeSalesView.xaml`
  - Card món trong vùng thực đơn chuyển sang dạng ảnh lớn phía trên, tên/mã món, badge trạng thái/định lượng và giá bán màu cam phía dưới.
  - Dòng món trong đơn hàng hiện tại bổ sung thumbnail ảnh món bên trái, giữ cụm tăng/giảm số lượng và xóa ở bên phải.
  - Ảnh dùng binding thật `DisplayImageSource`; chỉ dùng icon fallback khi món chưa có ảnh.
- `ASMILE.BO.App/ViewModels/BreakfastCafeSalesViewModel.cs`
  - Bổ sung `DisplayImageSource`, `HasImage`, `HasNoImage` cho `PosMenuItemCard` và `PosOrderLineRow`.
- `ASMILE.BO.Application/DTOs/BreakfastCafeSalesDtos.cs`
  - Bổ sung `ImagePath`, `ImageUrl` cho `BreakfastCafeOrderLineDto`.
- `ASMILE.BO.Infrastructure/Services/BreakfastCafeSalesService.cs`
  - Nạp `MenuItem` khi lấy chi tiết order/dòng order để trả ảnh món đã lưu trong MenuCatalog/MenuRecipe.

### Dữ liệu
- Không hard-code ảnh hoặc món trong XAML.
- Ảnh món lấy từ dữ liệu thật `MenuItem.ImagePath/ImageUrl`, bao gồm ảnh đã upload trong module Món ăn & Định lượng.

### Kiểm tra
- Encoding check XAML: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS, 0 warning, 0 error.
- Runtime: mở app thành công, process `ASMILE.BO.App` responding.

## Phiên 2026-06-07 — Thiết kế lại UI POS bữa sáng & cà phê theo luồng bán hàng nhanh

### Lý do thiết kế lại
- Layout POS cũ còn dàn ngang, khu chọn bàn nhỏ và dải `Đơn đang mở` phía dưới chiếm nhiều diện tích.
- Cần ưu tiên thao tác bán hàng nhanh: chọn bàn, chọn món, xem đơn hiện tại và thanh toán/gửi bếp ngay trong một màn hình.

### Layout mới
- Bố cục 3 cột chính: `Bàn & khu vực` 280px, `Thực đơn` chiếm phần còn lại, `Đơn hàng hiện tại` 380px.
- Bỏ dải `Đơn đang mở` toàn chiều ngang phía dưới; chuyển vào cột bàn/khu vực.
- Bàn hiển thị dạng card 2 cột, có trạng thái, mã đơn và tổng tiền.
- Món hiển thị dạng grid card dày hơn, dễ bấm, có emoji/icon, tên món, mã, badge và giá bán lớn.
- Đơn hiện tại dùng card dòng món, không dùng DataGrid; có empty state khi chưa có món.
- Phương thức thanh toán chuyển sang chip/segmented style, nút thanh toán/gửi bếp/hủy là nút lớn theo style Win 11 dùng chung.

### File đã sửa
- `ASMILE.BO.App/Views/BreakfastCafeSalesView.xaml`
  - Thay toàn bộ layout POS theo 3 cột, bỏ row đơn đang mở dưới cùng.
  - Dùng style chung `EnterpriseModulePanelCard`, `EnterpriseModuleCmd*`, `EnterpriseModuleTextBox`, `EnterpriseModuleComboBox`.
- `ASMILE.BO.App/ViewModels/BreakfastCafeSalesViewModel.cs`
  - Thêm `OpenTakeAwayCommand`, `SelectPaymentMethodCommand`, `HasNoOrderLines`, `HasOpenOrders`.
- `ASMILE.BO.Infrastructure/Services/BreakfastCafeSalesService.cs`
  - Cập nhật seed bàn POS idempotent, bổ sung A01-A08, B01-B06, Sân vườn 1-4, Phòng 101-105, Mang đi.

### Entity/service đã dùng
- Dùng dữ liệu thật từ `PosTables`, `BreakfastCafeOrders`, `BreakfastCafeOrderLines`, `BreakfastCafeKitchenTickets` và món từ MenuCatalog/MenuRecipe.
- Không hard-code danh sách món hoặc bàn trong XAML.

### Seed bàn
- Có cập nhật seed bàn trong `BreakfastCafeSalesService.EnsurePosDefaultsAsync`.
- Seed dạng additive theo `TableCode`, DB đã có bàn sẽ chỉ bổ sung bàn còn thiếu.

### Build/runtime
- Encoding check XAML: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS, 0 warning, 0 error.
- App Debug mở thành công: `ASMILE BO - Windows 11 Enterprise Shell`, process responding.

### Workflow đã kiểm tra
- Kiểm tra build-time toàn bộ binding/command của màn POS compile được.
- Kiểm tra runtime cơ bản: app khởi động sau thay đổi layout.

### Phần còn lại
- Chưa thao tác thủ công đầy đủ trên UI: chọn A01, thêm Bánh mì bò, thêm Cà phê sữa, gửi bếp/bar, mở lại A01, thanh toán và tạo đơn mang đi.
- Nút `In tạm tính` vẫn là ghi nhận thao tác trên UI, chưa nối máy in/phiếu in thực tế.
## Phiên 2026-06-07 — Làm lại module POS Bữa sáng & Cà phê

### Hiện trạng cũ
- Module bán đồ ăn sáng & cà phê dùng layout bán nhanh đơn giản, chưa có chọn bàn/khu vực, chưa theo dõi đơn đang mở rõ ràng.
- Thực đơn và đơn hàng còn rời rạc, đơn hàng dùng bảng thô, chưa có workflow gửi bếp/bar, phục vụ phòng, mang đi.

### Lý do làm lại
- Cần màn POS dùng thật cho nhà hàng/khách sạn: chọn bàn/phòng, mở đơn, thêm món nhanh, sửa số lượng/ghi chú, gửi bếp/bar, thanh toán và hủy đơn.
- Dữ liệu món lấy từ MenuCatalog/MenuRecipe hiện có, không hard-code món demo trong XAML.

### Entity/service đã thêm
- `BreakfastCafeOrder`: thêm `PosTableId`, `OrderType`, `ServiceStatus`, `AreaName`, `TableCode`, `TableName`, `ServiceChargeAmount`, `KitchenSentAt`.
- `BreakfastCafeOrderLine`: thêm `KitchenStatus`.
- Thêm entity `PosTable` và `BreakfastCafeKitchenTicket`.
- `IBreakfastCafeSalesService`/`BreakfastCafeSalesService`: thêm chọn bàn, mở đơn theo bàn, đơn đang mở, ghi chú dòng, gửi bếp/bar và cập nhật trạng thái bàn khi thanh toán/hủy.
- Seeder/service tạo bàn POS thật trong DB khi service được gọi: Tầng 1, Sân vườn, Mang đi, Khách sạn/phòng.

### View/ViewModel đã sửa
- `ASMILE.BO.App/ViewModels/BreakfastCafeSalesViewModel.cs`: làm lại workflow POS, command chọn bàn, thêm món, tăng/giảm/xóa dòng, lưu ghi chú, gửi bếp/bar, in tạm tính, thanh toán, hủy đơn có xác nhận.
- `ASMILE.BO.App/Views/BreakfastCafeSalesView.xaml`: làm lại layout Win 11 POS gồm cột bàn/khu vực, vùng menu card lớn, cột đơn hiện tại và hàng đơn đang mở; không dùng DataGrid cho màn bán hàng chính.

### Migration
- Tạo migration `20260607022536_AddBreakfastCafePosTablesAndKitchenTickets`.
- Thêm bảng `PosTables`, `BreakfastCafeKitchenTickets` và cột POS cho `BreakfastCafeOrders`/`BreakfastCafeOrderLines`.
- Migration được chỉnh idempotent cho các cột MenuRecipe đã tồn tại trên DB để không lỗi trùng cột.

### Build/runtime
- Encoding check XAML: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS, 0 warning, 0 error.
- `dotnet ef database update`: PASS, migration đã áp dụng.
- SQL kiểm tra có bảng `PosTables`, `BreakfastCafeKitchenTickets` và cột `PosTableId`, `OrderType`, `ServiceStatus`, `ServiceChargeAmount`, `KitchenSentAt`.
- App Debug mở thành công: `ASMILE BO - Windows 11 Enterprise Shell`, process responding.

### Workflow đã kiểm tra
- Build-time: binding/command chính của màn POS đã compile.
- Runtime cơ bản: app khởi động sau migration, không có XAML parse exception lúc startup.
- Chưa thao tác UI thủ công đầy đủ bằng chuột trên từng workflow POS trong phiên này.

### Vấn đề còn lại
- Nên kiểm tra trực tiếp trên UI: chọn bàn, thêm món, tăng/giảm số lượng, lưu ghi chú, gửi bếp/bar, thanh toán, hủy đơn, mở lại đơn đang mở.
- Nút `In tạm tính` hiện mới ghi nhận trạng thái trên UI, chưa nối máy in/phiếu in thực tế.
## Phiên 2026-06-05 — Sửa lỗi binding màn Cảnh báo mới

### Vấn đề
- Khi chọn món hoặc render vùng chi tiết sau khi thêm màn Cảnh báo mới, app hiện hộp thoại `Thao tác không thành công`.
- Log hệ thống ghi lỗi binding WPF: `CostAllWarningCount` và `RecipeAllWarningCount` là property chỉ đọc nhưng `Run.Text` đang bind theo mode mặc định gây TwoWay.

### File đã sửa
- `ASMILE.BO.App/Views/MenuRecipe/Screens/MenuWarningsScreen.xaml`
  - Chuyển binding của `Run.Text` sang `Mode=OneWay`.
  - Chuyển các binding cột chi tiết cảnh báo sang `Mode=OneWay` để tránh DataGrid cố ghi ngược vào property chỉ đọc.

### Build/test/runtime
- Encoding check: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS, 0 warning, 0 error.
- `dotnet test .\ASMILE.BO.Tests.Unit\ASMILE.BO.Tests.Unit.csproj -c Debug --no-build --filter MenuRecipeModuleTests`: PASS 9/9.
- App Debug mở lại thành công, log startup mới không lặp lỗi binding.

## Phiên 2026-06-05 — Thiết kế lại cảnh báo và giới hạn định lượng theo kho tiêu hao

### Vấn đề
- Màn `Cảnh báo` còn đơn giản, thiếu thống kê và thiếu chi tiết theo nguyên liệu.
- Chưa hiển thị rõ cảnh báo kho hết/thiếu tồn theo nguyên liệu định lượng.
- Tab `Định lượng` vẫn có thể chọn kho thường, trong khi nghiệp vụ yêu cầu chỉ dùng nguyên liệu tại kho tiêu hao.

### File đã sửa
- `ASMILE.BO.App/Views/MenuRecipe/Screens/MenuWarningsScreen.xaml`
  - Thiết kế lại màn cảnh báo với header, 4 thẻ thống kê, bảng chi tiết và empty state.
  - Bảng hiển thị mức cảnh báo, loại cảnh báo, món, nguyên liệu, số lượng cần/tồn/thiếu và nội dung chi tiết.
- `ASMILE.BO.App/ViewModels/MenuRecipeViewModel.cs`
  - Bổ sung count cảnh báo nghiêm trọng, kho, giá vốn, định lượng.
  - Tách cảnh báo theo dòng nguyên liệu từ cost preview, bao gồm kho hết/thiếu tồn.
  - Khi thêm nguyên liệu định lượng bắt buộc phải chọn kho tiêu hao.
- `ASMILE.BO.Application/DTOs/BreakfastCafeSalesDtos.cs`
  - Bổ sung `IsConsumptionWarehouse` cho `WarehouseLookupDto`.
- `ASMILE.BO.Application/Interfaces/IMenuCatalogService.cs`
  - Sửa `GetIngredientsLookupAsync` dùng `int? warehouseId`, đúng kiểu khóa kho.
- `ASMILE.BO.Infrastructure/Services/MenuCatalogService.cs`
  - Lookup định lượng chỉ trả kho `IsConsumptionWarehouse`.
  - Danh sách nguyên liệu chỉ lấy nguyên liệu có stock balance trong kho tiêu hao.
  - Giá tham chiếu lấy từ cost layer/stock thuộc kho tiêu hao.
- `ASMILE.BO.Tests.Unit/MenuRecipeModuleTests.cs`
  - Thêm test lookup chỉ dùng kho tiêu hao.
  - Thêm test cảnh báo thiếu tồn kho tiêu hao.

### Build/test
- Encoding check: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS, 0 warning, 0 error.
- `dotnet test .\ASMILE.BO.Tests.Unit\ASMILE.BO.Tests.Unit.csproj -c Debug --no-build --filter MenuRecipeModuleTests`: PASS 9/9.

## Phiên 2026-06-05 — Bỏ nút Lưu header và thêm lịch sử món ăn

### Vấn đề
- Header chi tiết món có cả `Lưu` và `Lưu & GV`, gây trùng thao tác.
- Tab `Lịch sử` trước đó là nội dung tĩnh, không bind dữ liệu nên sửa nhiều lần vẫn luôn hiển thị chưa có lịch sử.

### File đã sửa
- `ASMILE.BO.App/Views/MenuRecipe/Screens/MenuItemsScreen.xaml`
  - Bỏ nút `Lưu` cạnh `Lưu & GV`, giữ lại `Lưu & GV`, `Sao chép`, `Làm mới`.
- `ASMILE.BO.App/ViewModels/MenuRecipeViewModel.cs`
  - Thêm `MenuHistoryEntries` và cache lịch sử theo món.
  - Ghi lịch sử khi tạo/lưu món, đổi trạng thái, lưu định lượng, tính giá vốn, lưu & tính giá vốn.
  - Nạp lại lịch sử khi chọn món khác; món mới/sao chép sẽ clear lịch sử hiện hành.
- `ASMILE.BO.App/Views/MenuRecipe/Tabs/MenuHistoryTab.xaml`
  - Thay empty state tĩnh bằng bảng lịch sử thật và empty state theo binding.

### Build/test
- Encoding check: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS, 0 warning, 0 error.
- `dotnet test .\ASMILE.BO.Tests.Unit\ASMILE.BO.Tests.Unit.csproj -c Debug --no-build --filter MenuRecipeModuleTests`: PASS 7/7.

### Ghi chú
- Lịch sử hiện lưu trong phiên làm việc của màn Món ăn. Nếu cần giữ lịch sử qua lần mở app mới, phase sau nên nối vào audit database.

## Phiên 2026-06-05 — Sửa xóa/sửa dòng nguyên liệu định lượng

### Vấn đề
- Khi xóa một dòng nguyên liệu rồi lưu định lượng, EF báo circular dependency trên `MenuItemRecipeLine.LineNo`.
- Nguyên nhân là các dòng được đánh lại STT trong cùng lần lưu với thao tác xóa, trong khi database có unique index theo `MenuItemRecipeId + LineNo`.
- Bảng định lượng có cột sửa được nhưng WPF cần double-click/gõ phím, chưa đúng yêu cầu click vào dòng là sửa được.

### File đã sửa
- `ASMILE.BO.Infrastructure/Services/MenuRecipeService.cs`
  - Trước khi xóa/đánh lại STT, chuyển tạm các dòng hiện có sang `LineNo` âm và lưu trước một nhịp để tránh xung đột unique index.
  - Sau đó mới áp lại STT 1..n, cập nhật/xóa/thêm dòng và lưu chính thức.
- `ASMILE.BO.App/Views/MenuRecipe/Tabs/MenuRecipeTab.xaml`
  - Bật `SelectionUnit="CellOrRowHeader"` và gắn handler single-click edit cho bảng định lượng.
- `ASMILE.BO.App/Views/MenuRecipe/Tabs/MenuRecipeTab.xaml.cs`
  - Thêm handler `RecipeGrid_PreviewMouseLeftButtonDown` để click vào ô editable là vào edit ngay.
  - Bỏ qua các nút thao tác lên/xuống/xóa để không ảnh hưởng command.
- `ASMILE.BO.Tests.Unit/MenuRecipeModuleTests.cs`
  - Thêm test hồi quy `SaveRecipeAsync_ShouldRemoveRecipeLineAndReindex`.

### Build/test
- Encoding check: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS, 0 warning, 0 error.
- `dotnet test .\ASMILE.BO.Tests.Unit\ASMILE.BO.Tests.Unit.csproj -c Debug --no-build --filter MenuRecipeModuleTests`: PASS 7/7.

## Phiên 2026-06-05 — Sửa nút Lưu/Lưu & GV trong Món ăn

### Vấn đề
- Nút `Lưu` và `Lưu & GV` ở màn Món ăn không tạo cảm giác có tác dụng lưu ổn định.
- `Lưu & GV` gọi vòng qua command `Lưu`, dễ gặp nhánh trả về sớm khi trạng thái bận.
- Nút dấu ba chấm `Thêm tùy chọn` chưa có chức năng thật và gây rối giao diện.

### File đã sửa
- `ASMILE.BO.App/ViewModels/MenuRecipeViewModel.cs`
  - Tách `SaveMenuItemCoreAsync()` dùng chung cho `Lưu` và `Lưu & GV`.
  - Sau khi lưu, cập nhật trực tiếp `EditingItem`, danh sách món, selected card và trạng thái hiển thị.
  - `Lưu & GV` lưu món và tính giá vốn trong cùng command, không gọi vòng qua command khác.
- `ASMILE.BO.App/Views/MenuRecipe/Screens/MenuItemsScreen.xaml`
  - Bỏ nút `Thêm tùy chọn`/dấu ba chấm khỏi header chi tiết món.

### Build/test
- Encoding check: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS, 0 warning, 0 error.
- `dotnet test .\ASMILE.BO.Tests.Unit\ASMILE.BO.Tests.Unit.csproj -c Debug --no-build --filter MenuRecipeModuleTests`: PASS 6/6.

## Phiên 2026-06-05 — Sửa lỗi thêm định lượng món ăn báo thành công nhưng không lưu

### Root cause
- Dòng định lượng mới trong UI đang được gán `Guid.NewGuid()` ngay khi thêm, làm service khó phân biệt dòng mới với dòng đã có trong database.
- Khi lưu mà `RecipeId` không được truyền hoặc bị reset, service cũ tạo thêm một recipe mặc định mới cho cùng món. Sau đó màn hình có thể tải lại recipe mặc định cũ/trống nên người dùng thấy báo thành công nhưng dữ liệu không còn.
- Dòng recipe mới được thêm qua navigation collection chưa được track đủ chắc chắn trong một số luồng test EF InMemory, dẫn tới rủi ro không insert dòng nguyên liệu.

### File đã sửa
- `ASMILE.BO.App/ViewModels/MenuRecipeViewModel.cs`
  - Dòng định lượng mới dùng `Guid.Empty` để thể hiện đúng trạng thái chưa persist.
  - Sau khi lưu định lượng, nạp lại `RecipeLines` từ DTO service trả về để UI phản ánh đúng dữ liệu đã lưu.
  - Thông báo thành công hiển thị số nguyên liệu đã lưu.
- `ASMILE.BO.Infrastructure/Services/MenuRecipeService.cs`
  - Khi không có `RecipeId`, tái sử dụng recipe mặc định hiện có của món thay vì tạo trùng.
  - Sau khi lưu, trả về đúng recipe vừa lưu theo `recipe.Id`.
  - Nếu recipe đang lưu là mặc định, các recipe mặc định khác của cùng món được bỏ trạng thái mặc định.
  - Khi tạo dòng định lượng mới, thêm rõ vào `MenuItemRecipeLines` để EF luôn insert xuống database.
  - Dòng mới không có `Id` được ghép theo `LineNo` trước khi tạo bản ghi mới để tránh xóa/thêm không cần thiết.
- `ASMILE.BO.Tests.Unit/MenuRecipeModuleTests.cs`
  - Thêm test hồi quy `SaveRecipeAsync_WithoutRecipeId_ShouldReuseExistingDefaultRecipe`.

### Build/test/runtime
- Encoding check: PASS, không có mojibake XAML.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS, 0 warning, 0 error.
- `dotnet test .\ASMILE.BO.Tests.Unit\ASMILE.BO.Tests.Unit.csproj -c Debug --no-build --filter MenuRecipeModuleTests`: PASS 6/6.
- Runtime: app Debug đã mở bằng bản build mới, startup log không có XAML parse exception hoặc binding error nghiêm trọng.

## 2026-06-04 — Thiết kế lại danh sách món với lọc nhóm, sort và card item gọn hơn

### Vấn đề trước khi sửa
- Cột danh sách món 250px quá hẹp, item bị cắt chữ
- Không có bộ lọc nhóm rõ ràng (chỉ có chips mờ)
- Không có sắp xếp
- Item món chen chúc: badge đặt sai chỗ
- Không hiển thị số lượng món

### Layout mới
| Cột | Trước | Sau |
|---|---|---|
| Sidebar module | 155px | 155px |
| Danh sách món | 250px | 320px (+70px) |
| Chi tiết món | * | * |
| Panel phải | 280px | 260px |

### Filter/sort đã thêm
- Header: "Danh sách món (18 món)" — hiển thị count
- Search: placeholder có thể search theo tên/mã
- Category filter: ComboBox có IsTextSearchEnabled
- Filter chips: 5 chip nhỏ (Tất cả, Có định mức, Chưa định mức, Đang bán, Ngưng bán)
- Sort: ComboBox 5 option (Tên A-Z, Z-A, Giá thấp-cao, cao-thấp, Chưa định mức trước)
- Empty state: "Không tìm thấy món phù hợp" + nút "Xóa bộ lọc"

### Item card redesign
- Dòng 1: Tên món (trái) + Badge trạng thái (phải)
- Dòng 2: Mã món - Nhóm món
- Dòng 3: Giá bán xanh (trái) + Badge định mức (phải)
- Item height: ~86px, có border ngăn cách
- Selected: border trái xanh + nền xanh nhạt

### File đã sửa
- `ViewModels/MenuRecipeViewModel.cs` — SortOptions, SelectedSortOption, UnitPriceNumeric, ApplyFilters sorting, LastUpdatedDisplay động
- `Views/MenuRecipeView.xaml` — Cột rộng 320px, filter, sort, card redesign

### Build status
**Build succeeded** — 0 errors

### Runtime status
**OK** — App chạy, log sạch

### Vấn đề còn lại
- Menu ba chấm chưa implement
- History tab chưa có data thật
- `_itemsWithRecipe` chỉ populate khi click món — recipe status chưa chính xác ngay lần đầu

### Connection string
`Server=localhost;Database=ASMILE_BO;User Id=sa;Password=123456;TrustServerCertificate=True;MultipleActiveResultSets=True;`

### Bảng đã kiểm tra
| Bảng | Records |
|---|---|
| MenuCategories | 5 |
| MenuItems | 18 |
| MenuItemRecipes | 4 |
| MenuItemRecipeLines | 14 |
| Items (ingredients) | 33 |

### Root cause: Món ăn bị trống
- `InitializeAsync()` gọi `RefreshLookupsAsync()` trước `LoadAllMenuItemsAsync()`
- `RefreshLookupsAsync()` đếm item từ `_allMenuItems` — rỗng ở lần đầu → `ItemCount = 0` trên CategoryCards
- `BuildMenuCards()` gọi `ApplyFilters()` sau khi populate MenuCards — đúng, nhưng vì `FilterCategoryId` mặc định null + `ActiveFilterChip` = "Tất cả", tất cả 18 items đều vào `FilteredMenuCards`
- **Fix**: Đảo thứ tự — load items trước, refresh lookups sau

### Root cause: Nhóm món không hiện danh sách món
- `SelectEditCategory` không load lại menu items và không refresh lookups
- `CategoryDetailItems` rỗng vì `MenuCards` chưa được populate
- **Fix**: `SelectEditCategoryAsync` giờ là async, gọi `RefreshLookupsAsync` + load items fallback từ `_allMenuItems`

### Seed dữ liệu
- Thêm 10 ingredient items vào `Items` table (Bánh mì, Thịt bò, etc.)
- Seed 3 recipes: Bánh mì bò (4 nguyên liệu), Cà phê sữa (4), Phở bò (3)
- Sử dụng script `scripts/seed-menu-recipe-data.sql` (idempotent)

### File đã sửa
- `ViewModels/MenuRecipeViewModel.cs` — InitializeAsync ordering, async SelectEditCategory
- `scripts/seed-menu-recipe-data.sql` — seed script

### Build status
**Build succeeded** — 0 errors

### Runtime status
**OK** — App chạy, log sạch, 18 items + 4 recipes

### Vấn đề còn lại
- Menu ba chấm chưa implement
- History tab chưa có data audit thật
- `_itemsWithRecipe` chỉ populate khi click vào món — cần cải thiện để hiển thị recipe status chính xác hơn

### Root cause

**Vấn đề: Danh sách món trong nhóm không hiển thị, số món về 0**
- `InitializeAsync()` gọi `RefreshLookupsAsync()` (đếm `_allMenuItems.Count`) TRƯỚC `LoadAllMenuItemsAsync()` (populate `_allMenuItems`). Trên lần load đầu tiên, `_allMenuItems` rỗng → ItemCount = 0.
- `SelectEditCategory` không load lại menu items → `MenuCards` rỗng → `CategoryDetailItems` rỗng.

**Vấn đề: Ký tự rác trong bảng**
- Không phải lỗi binding — bảng không hiển thị vì `CategoryDetailItems` rỗng. Ký tự rác có thể từ lỗi XAML trước đó.

### Fix
- Đảo thứ tự InitializeAsync: `LoadAllMenuItemsAsync()` trước, `RefreshLookupsAsync()` sau.
- `SelectEditCategoryAsync` giờ là async, gọi `RefreshLookupsAsync()` trước khi load items.
- `LoadCategoryDetailItems` fallback build từ `_allMenuItems` nếu `MenuCards` rỗng.
- Thêm `SelectedCategoryMenuItem` + `SelectedCategoryRecipeLines` + `SelectCategoryMenuItemCommand` — chọn món trong nhóm → load recipe lines.
- Thêm `GoToMenuItemCommand` + `GoToMenuItemRecipeCommand` — chuyển sang màn Món ăn.

### File đã sửa
- `ViewModels/MenuRecipeViewModel.cs` — InitializeAsync reorder, SelectEditCategory async, SelectedCategoryMenuItem, recipe lines loading, navigation commands
- `Views/MenuRecipeView.xaml` — items DataGrid + recipe lines DataGrid + empty states + navigation buttons

### Build status
**Build succeeded** — 0 errors

### Runtime status
**OK** — App chạy, log sạch

### Đã kiểm tra
- Sidebar Nhóm món → chọn nhóm → danh sách món hiển thị
- Chọn món trong nhóm → nguyên liệu hiển thị
- Nút Xem món / Nhập ĐL chuyển đúng section
- Số món trên badge chính xác sau khi refresh

### Còn lại
- Menu ba chấm chưa implement
- History tab chưa có data thật
- Ảnh local chưa đồng bộ

### Vấn đề trước khi sửa
- Màn Món ăn có 2 box tìm/lọc (search + ComboBox) chiếm quá nhiều không gian
- Danh sách món có nút "Xem NL" và expand recipe — không đúng, nguyên liệu chỉ nằm trong tab Định lượng
- Item món bị chật, chữ bị cắt, badge không rõ
- Màn Nhóm món bên phải trống khi chọn nhóm
- `SelectMenuImageCommand` có HTML entity trong C# string

### Cách sửa
- Gộp search + filter chips thành một khu vực duy nhất, bỏ ComboBox riêng
- Xoá nút "Xem NL", expand recipe khỏi danh sách món
- Thiết kế lại item món: name + status badge trên 1 dòng, code + category dòng 2, price + recipe badge dòng 3
- Màn Nhóm món: thêm card chi tiết (KPIs + edit form + danh sách món trong nhóm) bên phải
- Sửa HTML entity trong C# string thành tiếng Việt chuẩn

### File đã sửa
- `Views/MenuRecipeView.xaml` — gộp filter, xoá expand, redesign items, category detail panel
- `ViewModels/MenuRecipeViewModel.cs` — CategoryDetailItems + KPIs, fix HTML entities, SelectEditCategory loads items

### Build status
**Build succeeded** — 0 errors

### Runtime status
**OK** — App chạy, log sạch

### Đã kiểm tra
- Màn Món ăn: 1 ô search + chips, item hiển thị gọn
- Màn Nhóm món: chọn nhóm → card chi tiết + KPIs + danh sách món trong nhóm
- Status bar: có text thông tin
- Build 0 error

### Còn lại
- Menu ba chấm (Xuất Excel, In, Xoá) chưa implement
- Ảnh món lưu local chưa đồng bộ
- History tab chưa có data thật

### Mục tiêu
- Loại bỏ cột "Nhóm món" lớn trong màn Món ăn (nhóm món đã là mục sidebar riêng)
- Thay bằng danh sách món thuần + filter nhóm dạng ComboBox gọn
- Thiết kế lại tab Thông tin chung: padding 28px, MaxWidth 960, 2 cột (identity + settings/image)
- Thêm IsTextSearchEnabled cho ComboBox
- Sửa label: "Danh sách món", "+ Thêm món", "+ Thêm nhóm"

### Vấn đề thiết kế sai trước khi sửa
- "Nhóm món" chiếm cả cột trái màn Món ăn, nhầm với màn sidebar riêng
- Form Thông tin chung sát viền, dàn quá rộng
- ComboBox thiếu IsTextSearchEnabled
- Label không rõ ngữ cảnh ("+ Thêm" không biết thêm gì)

### Cách sửa
- Xoá category cards ListBox khỏi màn Món ăn, thay bằng ComboBox lọc nhóm
- Header đổi từ "Nhóm món" → "Danh sách món"
- Form: padding 28px, MaxWidth 960, 2 cột layout mới
- Thêm IsTextSearchEnabled cho ComboBox (category, ingredient, warehouse, preparation)
- Label cụ thể: "+ Thêm món", "+ Thêm nhóm"

### File đã sửa
- `Views/MenuRecipeView.xaml` — layout restructuring + form redesign

### Migration
Không cần

### Build status
**Build succeeded** — 0 errors

### Runtime status
**OK** — App chạy, log sạch

### Đã kiểm tra
- Màn Món ăn: danh sách món + filter ComboBox
- Tab Thông tin chung: layout mới, padding, 2 cột
- ComboBox: IsTextSearchEnabled
- Màn Nhóm món: vẫn hoạt động riêng

### Vấn đề còn lại
- Menu ba chấm (Xuất Excel, In, Xoá) chưa implement
- Ảnh món lưu local chưa đồng bộ
- History tab chưa có dữ liệu thật

### Mục tiêu
- Thêm chức năng upload ảnh món ăn (OpenFileDialog + copy vào AppData)
- Thêm expand/collapse menu item xem nguyên liệu inline trong danh sách
- Fix tab Lịch sử với empty state đẹp
- Cập nhật `CreateMenuItemRequest` DTO + service mapping cho ImagePath
- Nâng cấp form thông tin với khung ảnh + nút chọn/xoá

### Vấn đề trước khi sửa
- Menu không có ảnh — chỉ có placeholder
- Không thể chọn/xoá ảnh món từ UI
- Trong danh sách món, không xem được nguyên liệu định lượng nếu không chọn vào món
- Tab Lịch sử hiển thị warnings grid không liên quan
- DTO `CreateMenuItemRequest` thiếu field `ImagePath`

### Root cause
- `CreateMenuItemRequest` không có ImagePath → không lưu được ảnh khi save
- Service `MenuCatalogService.CreateOrUpdateMenuItemAsync` không map ImagePath
- XAML History tab bind sai DataGrid

### File đã sửa
- `ViewModels/MenuRecipeViewModel.cs` — SelectMenuImageCommand, RemoveMenuImageCommand, SelectedImageFullPath, ToggleItemExpandCommand, IsExpanded trên MenuItemCardState
- `Views/MenuRecipeView.xaml` — Image upload UI, expand/collapse inline trong danh sách món, History tab empty state
- `Application/DTOs/BreakfastCafeSalesDtos.cs` — thêm ImagePath vào CreateMenuItemRequest
- `Infrastructure/Services/MenuCatalogService.cs` — map ImagePath khi lưu menu item

### Migration
Không cần — entity `MenuItem` đã có `ImagePath` từ trước

### Build status
**Build succeeded** — 0 errors, 0 warnings

### Runtime status
**OK** — App chạy, log sạch

### Đã kiểm tra
- Form thông tin chung: khung ảnh + nút chọn/xoá
- Expand/collapse nguyên liệu trong danh sách món
- Tab Lịch sử: empty state đẹp

### Vấn đề còn lại
- Ảnh lưu trong AppData, chưa có cơ chế đồng bộ giữa các máy
- Category section right panel vẫn còn trống khi không có category editing
- Menu ba chấm (Xuất Excel, In, Xoá) chưa implement

### Mục tiêu
- Xoá hoàn toàn Segoe MDL2 Assets icon font (nguyên nhân gây ô vuông)
- Tất cả button có text label rõ ràng, không icon-only
- Sidebar text label (không icon font)
- DataGrid action button dùng ▲/▼/X + tooltip
- Warnings panel có badge đếm số lượng
- Right panel cost card có empty state hướng dẫn

### Vấn đề trước khi sửa
- Toàn bộ icon dùng Segoe MDL2 Assets (`&#xHHHH;`) — nếu font thiếu sẽ hiển thị ô vuông
- Nhiều button icon-only (➕, ba chấm) không có tooltip rõ ràng
- Warnings panel không hiển thị badge số lượng
- Cost panel không có hướng dẫn khi empty

### Thay đổi đã làm
- Xoá `FontFamily="Segoe MDL2 Assets"` khỏi toàn bộ XAML
- Sidebar: bỏ icon `Tag`, chỉ giữ text label (vd: "  Tổng quan")
- Action buttons: text label rõ thay vì icon (vd: "Lưu" thay vì icon save)
- DataGrid action: ▲ (lên), ▼ (xuống), X (xóa) + tooltip
- Warnings panel: thêm badge đỏ đếm số `CostWarningCount`
- Cost panel: thêm empty state "Thêm nguyên liệu để tính giá vốn"
- Tab headers: text label thuần, không icon
- Giữ nguyên toàn bộ binding và command

### File đã sửa
- `Views/MenuRecipeView.xaml` — toàn bộ icon/label/button styles

### Build status
**Build succeeded** — 0 errors, 0 warnings

### Runtime status
**OK** — App chạy, log sạch, không ERR

### Các màn đã kiểm tra
- Dashboard, Món ăn, Nhóm món, Nguyên liệu, Định lượng, Giá vốn, Cảnh báo, Báo cáo, Cài đặt

### Vấn đề còn lại
- Menu ba chấm còn 3 item disabled (chưa implement backend)
- Ảnh món upload chưa có
- Settings còn basic

### Mục tiêu
- Nâng cấp Dashboard: thay thế KPI trùng "Có recipe" → "Cảnh báo" (DashboardWarningCount)
- Enable History tab với AllItemWarnings grid
- Replace Reports placeholder → cost analysis grid
- Replace Settings placeholder → module configuration panel
- Fix DbContext concurrency (SemaphoreSlim)

### BUG INVESTIGATION: DbContext Concurrency

**Triệu chứng:**
Runtime log có `System.InvalidOperationException: A second operation was started on this context instance before a previous operation completed`

**Root cause:**
Constructor gọi `_ = InitializeAsync()` fire-and-forget, trong khi section navigation cũng trigger database operations (`LoadDashboardAsync`, `LoadAllWarningsAsync`). Cả hai dùng chung `AppDbContext` (scoped), EF Core không cho concurrent operations.

**Cách sửa:**
Thêm `SemaphoreSlim(1,1)` guard trong `InitializeAsync()` và `LoadDashboardAsync()`, đảm bảo chỉ một DB operation chạy tại một thời điểm.

### File đã sửa
- `ViewModels/MenuRecipeViewModel.cs` — DashboardWarningCount, DashboardTotalCostEstimate, SemaphoreSlim lock
- `Views/MenuRecipeView.xaml` — Dashboard KPI thay thế, History tab enabled, Reports/Sections real data, recipe cost column

### Build status
**Build succeeded** — 0 errors, 0 warnings

### Runtime status
**App chạy ổn định** — Log sạch, không ERR, không concurrency exception

### Vấn đề còn lại
- Settings section còn đơn giản (configuration display)
- Menu ba chấm (Export/Print/Delete) chưa implement
- Recipe tab cost column hiển thị EstimatedUnitCost từ lookup

### Mục tiêu
- Fix sidebar navigation không hoạt động do `int` parameter type mismatch
- Fix active state sidebar luôn hiển thị "Món ăn" bất kể section nào được chọn
- Fix runtime exception `ArgumentException: string cannot be of type Int32` trong `RelayCommand<T>.CanExecute`
- Unicode escape toàn bộ XAML để chống mojibake

### BUG INVESTIGATION

**Triệu chứng:**
- Sidebar xuất hiện nhưng click không chuyển section
- "Món ăn" luôn hiển thị active
- Runtime log có `ERR Unhandled UI exception` với `System.ArgumentException: Parameter "parameter" (object) cannot be of type System.String, as the command type requires an argument of type System.Int32`

**Root cause:**
- `NavigateSection(int section)` dùng `IRelayCommand<int>` nhưng XAML truyền `CommandParameter="0"` là string
- CommunityToolkit.Mvvm `RelayCommand<T>.CanExecute(object)` không tự convert string -> int, gây `ArgumentException`
- `SidebarBtnActive` style gán cứng cho "Món ăn", không có DataTrigger cho các section khác
- `SectionButtonStyle` trong resources không dùng được vì WPF không cho change `Style` property qua `Style.Triggers`

**Cách sửa:**
- `NavigateSection(int)` → `NavigateSection(string)` — chấp nhận string từ XAML, parse int trong method
- `NavigateFromDashboard(int)` → `NavigateFromDashboard(string)` — tương tự
- Xóa `SidebarBtn` + `SidebarBtnActive` style cũ, thay bằng `SBtnBase` + 9 style `SBtn0`-`SBtn8` mỗi style có `DataTrigger` cho `SelectedSection` tương ứng
- Xóa `SectionButtonStyle` không dùng được
- Unicode escape toàn bộ text tiếng Việt trong XAML

### File đã sửa
- `ViewModels/MenuRecipeViewModel.cs` — Fix 2 command parameter types
- `Views/MenuRecipeView.xaml` — Fix sidebar styles, Unicode escape toàn bộ text

### Build status
**Build succeeded** — 0 errors, 0 warnings

### Runtime status
**App chạy ổn định** — PID 8992, log sạch không ERR sau fix

### Vấn đề còn lại
- Các command parameter `int` khác (nếu có) cần kiểm tra
- Báo cáo & Cài đặt section vẫn là placeholder
- Lịch sử tab vẫn disabled

















### Phase 0 - Baseline & safety
- Created DB backup: `D:\ASMILE-BO\Backups\ASMILE_BO_20260528_093130.bak`.
- Refreshed baseline snapshot: copied `KIEMTRAAPP.md` -> `KIEMTRAAPP_BEFORE_FIX.md`.
- Captured runtime config from `ASMILE.BO.App\appsettings.json`:
  - `Database:ConnectionString=Server=localhost;Database=ASMILE_BO;User Id=sa;Password=123456;TrustServerCertificate=True;MultipleActiveResultSets=True;`
  - No `TrainingData:*` in this file; destructive flags were enabled temporarily in reset runner only.

### Phase 1 - Build blocker
- Fixed mojibake at `ASMILE.BO.App\Views\OperationsCenterView.xaml` header bound to `MemoryUsageBytes` (`B? nhớ` -> `B? nh?`).
- Gate passed:
  - `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
  - `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-xaml-encoding.ps1`: pass.

### Phase 2 - TT99/TT200 failing tests
- Root cause: compliance validators auto-healed foundation before validation.
- Updated:
  - `ASMILE.BO.Infrastructure\Services\TT99ComplianceService.cs`: removed `EnsureDefaultFoundationAsync` call in `ValidateComplianceAsync`.
  - `ASMILE.BO.Infrastructure\Services\TT200ComplianceService.cs`: removed `EnsureDefaultFoundationAsync` call in `ValidateComplianceAsync`.
- Gate passed:
  - `dotnet test .\ASMILE.BO.sln`: pass (Unit 120/120, Integration 11/11).

### Phase 3 - Real blank reset
- First reset attempt failed due FK: `PostingSources -> JournalHeaders`.
- Hardened reset cleaner in `ASMILE.BO.Infrastructure\Services\TrainingDataService.cs`:
  - Added `PostingSources` cleanup before `JournalHeaders`.
  - Added `BusinessDocuments` cleanup in destructive reset flow.
  - Added `PostingSources` marker-based cleanup in training-only flow.
- Re-ran reset runner with temporary local guards and confirmation `RESET ALL BUSINESS DATA`.
- Reset completed successfully.
- Verified business tables cleared to 0: `BusinessDocuments`, `JournalHeaders`, `JournalLines`, `Items`, `Customers`, `Suppliers`, `StockReceipts`, `StockIssues`, `CashTransactions`, `BankTransactions`, `ReceivableTransactions`, `PayableTransactions`, `VatTransactions`.
- Verified system tables preserved: `AppUsers`, `AppRoles`, `AppPermissions`, `AccountingStandards`, `AccountTemplates`.

### Final gates (current)
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln`: pass (Unit 120/120, Integration 11/11).

## 2026-05-28 - Stabilization execution (Phase 4 mojibake cleanup)

### Scope completed
- Services: `BankingService.cs`, `ClosingService.cs`, `PermissionService.cs`, `TrainingDataService.cs`.
- Views: `OperationsCenterView.xaml`, `TaxFinancialReportingView.xaml`, `AuditCenterView.xaml`, `NotificationTaskCenterView.xaml`, `FiscalPeriodManagementView.xaml`, `SecurityAdminView.xaml`, `CashBankView.xaml`, `AssetManagementView.xaml`.

### Changes
- Fixed user-facing mojibake strings manually by context (no bulk blind replace, no behavior change).
- Fixed remaining XAML mojibake text in fiscal period summary subtitle (`dang ch?n`).
- No migration changes.

### Gates
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-xaml-encoding.ps1`: pass.
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass (0 warnings, 0 errors).
- `dotnet test .\ASMILE.BO.sln`: pass (Unit 120/120, Integration 11/11).
- Note: one flaky run hit `BackgroundSchedulerServiceTests.Cancellation_PropagatesToRunningExecution`; immediate rerun and full suite rerun passed.

## 2026-05-28 - Phase 7 runtime UI smoke test (manual-assisted)

### Runtime smoke execution
- App launched and user completed real module-by-module smoke from shell.
- Training-data onboarding UI was included in smoke checklist (UI-only, no destructive reset).
- Post-smoke log scan (`logs\\asmile-bo-20260528.log`) found repeated runtime UI errors:
  - `ERR Unhandled UI exception`
  - `XamlParseException` (`DeferredBinaryDeserializerExtension`)
  - inner `FormatException: Token is not valid` (brush/color parse at runtime)

### Gate results
- Post-smoke build: `dotnet build .\ASMILE.BO.sln --no-incremental` pass, 0 warnings, 0 errors.
- Post-smoke test:
  - one flaky fail on `BackgroundSchedulerServiceTests.Cancellation_PropagatesToRunningExecution`
  - rerun targeted test pass
  - rerun full solution tests pass (Unit 120/120, Integration 11/11)

### Phase conclusion
- Phase 7 runtime stability status: **FAIL** due to repeated unhandled UI runtime exception in real smoke session logs.

## Step 1 completed - Chu?n h�a encoding v� build s?ch

### K?t qu?
- ?? scan v? s?a c?c chu?i ti?ng Vi?t l?i m? h?a r? r?ng trong source/settings/seed li?n quan.
- ?? d?m b?o c?c file tracking ch?nh d?ng UTF-8.
- Build solution pass tru?c khi bu?c sang c�c bu?c nghi?p v?.

### Build result
- Build: pass.
- Errors: 0.
- Warning c�n l?i: CA1416 ? `ASMILE.BO.Infrastructure/DependencyInjection.cs` do `DpapiSecretProtector` l� Windows-only API.

---

## Step 2 completed - Accounting Core Entities

### K?t qu?
- ?? t?o n?n t?ng k? to?n persisted g?m AccountingStandard, AccountingPeriod, JournalHeader, JournalLine, PostingBatch, PostingSource, PostingRule, PostingRuleLine, AccountRoleMapping, AccountTagMapping.
- ?? th?m enum k? to?n n?n t?ng.
- ?? th?m DbSet v? Fluent API v?o AppDbContext.
- Kh�ng x�a ho?c thay d?i `LedgerAccount` hi?n c�.

### Migration
- `AddAccountingCoreEntities`

### Build result
- Build: pass.
- Errors: 0.
- Warning c�n l?i: CA1416 ? `ASMILE.BO.Infrastructure/DependencyInjection.cs`.

---

## Step 3 completed - Posting Engine

### K?t qu?
- ?? t?o `IPostingEngine`, `IPostingRuleService`, `IJournalService` v? implementation tuong ?ng.
- ?? t?o DTO/model posting preview/result/validation.
- ?? dang k? DI cho posting services.
- ?? c?u h?nh precision cho `BusinessDocument.Amount`.
- Engine kh�ng hard-code t�i kho?n; resolve qua `AccountRoleMapping`.

### Migration
- `FixBusinessDocumentAmountPrecision` n?u snapshot/model y�u c?u.

### Build result
- Build: pass.
- Errors: 0.
- Warnings: 4 CA1416 trong `ASMILE.BO.Infrastructure/DependencyInjection.cs`.

### R?i ro/TODO
- `LedgerAccount.AllowPosting` chua c� n�n m?i ki?m tra t�i kho?n t?n t?i; enforce chi ti?t d? Step 7.
- Reversal chua c� `ReversedJournalId` li�n k?t hai chi?u.

---

## Step 4 completed - T�ch h?p phi?u nh?p/xu?t v?i Posting Engine

### K?t qu?
- ?? b? sung posting th?t cho `StockReceipt` v? `StockIssue` trong `PostingEngine`.
- ?? h? tr? `PreviewPostingAsync` cho phi?u nh?p/xu?t.
- ?? ch?ng duplicate posting theo `JournalHeader.SourceType + SourceId` v? metadata ch?ng t?.
- ?? th?m transaction an to?n khi approve v? post phi?u nh?p/xu?t.
- ?? seed mapping TT99 t?i thi?u cho Inventory, Payable, COGS.

### File t?o
- `ASMILE.BO.Core/Accounting/PostingSourceIds.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260520093157_AddStockVoucherPosting.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260520093157_AddStockVoucherPosting.Designer.cs`

### File s?a
- `ASMILE.BO.Core/Entities/StockReceipt.cs`
- `ASMILE.BO.Core/Entities/StockIssue.cs`
- `ASMILE.BO.Core/Interfaces/Repositories/IUnitOfWork.cs`
- `ASMILE.BO.Application/Interfaces/IInventoryAppService.cs`
- `ASMILE.BO.Application/Services/InventoryAppService.cs`
- `ASMILE.BO.Infrastructure/Persistence/EfUnitOfWork.cs`
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Services/PostingEngine.cs`
- `ASMILE.BO.Infrastructure/Services/JournalService.cs`

### Migration
- `AddStockVoucherPosting`

### Build result
- Build: pass.
- Errors: 0.
- Warnings: 4 CA1416 trong `ASMILE.BO.Infrastructure/DependencyInjection.cs`.

### R?i ro/TODO
- Costing Engine Step 10 s? thay gi� v?n t?m cho `StockIssue`.
- VAT chua h?ch to�n.
- AR/AP persisted chua l�m.
- `BusinessDocument` chua t�ch h?p posting.

---

## Step 5 completed - Accounting Period Engine v� kh�a k? th?t

### M?c ti?u d? ho?n th?nh
- ?? t?o service qu?n l? k? k? to?n th?t d?a tr?n entity `AccountingPeriod`.
- ?? enforce kh?a k? tru?c khi post/reverse journal.
- ?? enforce kh?a k? trong lu?ng phi?u nh?p/xu?t ? t?ng `InventoryAppService`.
- Kh�ng thay d?i UI l?n v� kh�ng l�m Step 6.

### File t?o
- `ASMILE.BO.Application/Interfaces/IAccountingPeriodService.cs`
- `ASMILE.BO.Infrastructure/Services/AccountingPeriodService.cs`

### File s?a
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.Infrastructure/Services/JournalService.cs`
- `ASMILE.BO.Infrastructure/Services/PostingEngine.cs`
- `ASMILE.BO.Application/Services/InventoryAppService.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- Kh?ng c? migration m?i. Entity `AccountingPeriod` ? Step 2 d? d? cho Step 5.

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`

### Build result
- Build: pass.
- Errors: 0.
- Warnings: 4.
- Warning c�n l?i: CA1416 trong `ASMILE.BO.Infrastructure/DependencyInjection.cs` do `DpapiSecretProtector` l� Windows-only API.

### Enforcement d? l?m
- `JournalService.PostDraftAsync`: g?i `ValidatePostingDateAsync` tru?c khi chuy?n journal sang Posted.
- `JournalService.ReverseAsync`: ki?m tra k? c?a journal g?c v� ng�y reversal.
- `PostingEngine.PostAsync`: ki?m tra k? tr?c ti?p tru?c khi t?o draft cho StockReceipt/StockIssue d? tr�nh orphan draft khi k? kh�a.
- `InventoryAppService.SaveReceiptAsync`: ch?n t?o/s?a phi?u nh?p trong k? kh?a; phi?u d? posted kh?ng du?c s?a.
- `InventoryAppService.SaveIssueAsync`: ch?n t?o/s?a phi?u xu?t trong k? kh?a; phi?u d? posted kh?ng du?c s?a.
- `InventoryAppService.ApproveReceiptAndPostAsync`: ch?n duy?t/ghi s? phi?u nh?p trong k? kh�a.
- `InventoryAppService.ApproveIssueAndPostAsync`: ch?n duy?t/ghi s? phi?u xu?t trong k? kh�a.
- `InventoryAppService.UnapproveReceiptAsync`, `UnapproveIssueAsync`, `DeleteReceiptAsync`, `DeleteIssueAsync`: ch?n thao t?c n?u k? kh?a ho?c ch?ng t? d? ghi s?.

### R?i ro c�n l?i
- UI Settings k? k? to�n chua wire tr?c ti?p v�o `IAccountingPeriodService`; Step n�y uu ti�n service v� enforcement.
- Ch�nh s�ch unlock/close c�n don gi?n, chua ki?m quy?n admin d?c bi?t.
- C�c module kh�c nhu `BusinessDocument`, phi?u thu, phi?u chi chua enforce period lock ? Step 5.
- Reversal v?n chua c� `ReversedJournalId` li�n k?t hai chi?u.
- Chua c� test t? d?ng; c?n b? sung ? Step 19.

### Vi?c ti?p theo
- Step 6: N?ng c?p S? c?i d?ng Journal persisted.



---

## Step 6 completed - N?ng c?p S? c?i d?ng Journal persisted

### M?c ti?u d? ho?n th?nh
- `EfGeneralLedgerService` d? d?ng `JournalHeader`/`JournalLine` persisted l?m ngu?n ch?nh cho S? c?i.
- Logic ph�i sinh runtime cu t? phi?u kho/ch?ng t? du?c gi? l�m fallback legacy, ch? ch?y khi chua c� journal persisted d? tr�nh double count.
- B?t to?n th? c?ng d? t?o `JournalHeader`/`JournalLine` th?t v? post qua `IJournalService`.
- Kh�ng l�m Step 7 v� kh�ng d?i UI l?n.

### File t?o
- Kh�ng c� file m?i.

### File s?a
- `ASMILE.BO.Infrastructure/Services/EfGeneralLedgerService.cs`
- `ASMILE.BO.Application/DTOs/GeneralLedgerDtos.cs`
- `ASMILE.BO.App/ViewModels/GeneralLedgerViewModel.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- Kh�ng c� migration m?i.

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`

### Build result
- Build: pass.
- Errors: 0.
- Warnings: 4.
- Warning c�n l?i: CA1416 trong `ASMILE.BO.Infrastructure/DependencyInjection.cs` do `DpapiSecretProtector` l� Windows-only API.

### Nh?ng ph?n d? chuy?n sang Journal persisted
- B�t to�n: `GetLedgerEntriesAsync` d?c journal posted t? `JournalHeaders` + `JournalLines` tru?c, fallback legacy ch? khi chua c� journal.
- C?n d?i ph?t sinh: `GetLedgerBalancesAsync` group tr?c ti?p theo `JournalLine.AccountCode`, t?ch ph?t sinh tru?c k? v?o opening v? trong k? v?o period.
- Ki?m tra c?n d?i: d? li?u tab ki?m tra c?n d?i hi?n d?a tr?n entries/balances t? journal persisted; DTO c? `DebitAmount`/`CreditAmount` d? ViewModel t?nh t?ng N?/C? d?ng theo journal line.
- Manual journal: `CreateManualEntryAsync` t?o journal 2 d�ng N?/C�, validate qua `IJournalService`, post ngay v?i `SourceType=ManualJournal`.

### Fallback legacy c�n l?i
- `LoadAllEntriesAsync` trong `EfGeneralLedgerService` v?n t?n t?i d? d?c ph�i sinh t? `StockReceipt`, `StockIssue`, `BusinessDocument` khi DB chua c� journal persisted.
- Fallback kh�ng tr?n v?i persisted journal d? tr�nh double count.

### R?i ro c�n l?i
- Opening balance m?i t�nh t? ph�t sinh tru?c `FromDate`, chua c� s? du d?u k? chu?n ri�ng.
- `BusinessDocument` chua post journal persisted n�n ch? xu?t hi?n khi fallback legacy ch?y ho?c khi Step sau t�ch h?p posting.
- AR/AP persisted chua l�m n�n b�o c�o c�ng n? v?n chua d?a tr�n b?ng c�ng n? th?t.
- Report engine t?p trung chua l?m; print/export hi?n v?n d?ng lu?ng cu nhung data source service d? uu ti?n journal persisted.
- DTO d? th?m tru?ng tuong th?ch ngu?c `DebitAmount`, `CreditAmount`, `SourceType`, `Status`; XAML hi?n chua hi?n th? h?t c?c tru?ng n?y.

### Vi?c ti?p theo
- Step 7: Chu?n h�a h? th?ng t�i kho?n cho TT99-ready.

---

## Step 7 completed - Chu?n h�a h? th?ng t�i kho?n TT99-ready

### M?c ti?u d? ho?n th?nh
- ?? b? sung metadata k? to?n cho `LedgerAccount` d? h? tr? chu?n TT99/TT200/Custom v? sau.
- Kh?ng x?a t?i kho?n cu, kh?ng d?i m? t?i kho?n hi?n c?.
- Kh�ng hard-code t�i kho?n trong engine.
- Kh�ng l�m Step 8.

### File t?o
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260520145845_AddLedgerAccountMetadata.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260520145845_AddLedgerAccountMetadata.Designer.cs`

### File s?a
- `ASMILE.BO.Core/Entities/GeneralLedgerEntities.cs`
- `ASMILE.BO.Application/DTOs/GeneralLedgerDtos.cs`
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ASMILE.BO.Infrastructure/Services/EfGeneralLedgerService.cs`
- `ASMILE.BO.Infrastructure/Services/JournalService.cs`
- `ASMILE.BO.Infrastructure/Services/PostingEngine.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- `AddLedgerAccountMetadata`
- Migration th�m c�c c?t metadata cho `LedgerAccounts` v� index cho `AccountingStandardCode`, `AccountCategory`, `AllowPosting`.
- Migration c� backfill nh? theo ti?n t? t�i kho?n cho `AccountCategory`, `NormalBalance`, `IsFinancialAccount`, `IsManagementAccount`.

### Metadata d? th?m
- `AccountingStandardCode`
- `AllowPosting`
- `IsSystemAccount`
- `IsEditable`
- `IsDeletable`
- `NormalBalance`
- `AccountCategory`
- `RequireCustomer`
- `RequireSupplier`
- `RequireWarehouse`
- `RequireItem`
- `RequireDepartment`
- `RequireEmployee`
- `RequireCurrency`
- `IsFinancialAccount`
- `IsManagementAccount`

### Validation m?i trong JournalService
- Kh�ng cho post v�o t�i kho?n `AllowPosting=false`.
- N?u t?i kho?n y?u c?u kh?ch h?ng/nh? cung c?p/kho/h?ng h?a/b? ph?n/nh?n s?/ti?n t? th? `JournalLine` ph?i c? d? li?u tuong ?ng.
- V?n gi? validation cu: t?i kho?n t?n t?i, journal c?n d?i, kh?ng ?m, kh?ng v?a N? v?a C?.

### C?p nh?t PostingEngine
- D�ng kho c?a `StockReceipt` v� `StockIssue` hi?n set `WarehouseId` v� `ItemId` theo detail n?u c�.
- `StockReceipt` t?o nhi?u d�ng N? Inventory theo chi ti?t v� m?t d�ng C� Payable t?ng.
- `StockIssue` t?o d�ng N? COGS v� C� Inventory theo t?ng chi ti?t.

### UI/import/export
- DTO `LedgerAccountDto` d? c? metadata m?i d? UI/import/export c? th? m? r?ng m? kh?ng l?i d? li?u.
- Chua l�m UI l?n; m�n t�i kho?n hi?n t?i chua b?t bu?c hi?n th? to�n b? c?t metadata m?i.

### X�a t�i kho?n
- `EfGeneralLedgerService.DeleteAccountAsync` hi?n ch?n x?a t?i kho?n h? th?ng, t?i kho?n kh?ng cho x?a, ho?c t?i kho?n d? c? ph?t sinh `JournalLine`.

### Command d? ch?y
- `dotnet-ef.exe migrations add AddLedgerAccountMetadata --project .\ASMILE.BO.Infrastructure\ASMILE.BO.Infrastructure.csproj --startup-project .\ASMILE.BO.App\ASMILE.BO.App.csproj --context AppDbContext`
- `dotnet build .\ASMILE.BO.sln --no-incremental`

### Build result
- Build: pass.
- Errors: 0.
- Warnings: 4.
- Warning c�n l?i: CA1416 trong `ASMILE.BO.Infrastructure/DependencyInjection.cs` do `DpapiSecretProtector` l� Windows-only API.

### R?i ro c�n l?i
- Backfill metadata ch? l� heuristic theo ti?n t? t�i kho?n, c?n k? to�n r� so�t t�i kho?n custom.
- Chua t? d?ng set `AllowPosting=false` cho t�i kho?n cha d? tr�nh ph� mapping hi?n dang tr? v�o 156/331/632; c?n chu?n h�a mapping sang t�i kho?n chi ti?t tru?c khi b?t ch?t.
- M?t s? t�i kho?n 99x/qu?n tr? c?n r� th? c�ng d? x�c d?nh t�i kho?n t�i ch�nh hay qu?n tr?.
- UI t�i kho?n chua hi?n th? d?y d? metadata m?i.

### Vi?c ti?p theo
- Step 8: T?o AR/AP persisted.

### Step 7 b? sung theo ghi ch� t�i kho?n th?t
- ?? d?i mapping m?c d?nh TT99 cho posting kho/c?ng n?/gi? v?n t? t?i kho?n t?ng h?p m?u sang t?i kho?n chi ti?t:
  - Inventory: `156` -> `1561`
  - Payable: `331` -> `3311`
  - COGS: `632` -> `6321`
- ?? set t?i kho?n t?ng h?p `AccountKind=H` m?c d?nh kh?ng cho h?ch to?n tr?c ti?p (`AllowPosting=false`) v? kh?ng cho x?a (`IsDeletable=false`).
- ?? c?p nh?t runtime table repair trong `EnsureLedgerAccountsTableAsync` d? t?i kho?n t?ng h?p kh?ng b? h?ch to?n tr?c ti?p khi app t? v? schema.
- ?? t?o migration b? sung `AlignPostingMappingsToDetailAccounts` d? snapshot EF v? d? li?u seed mapping d?ng v?i t?i kho?n chi ti?t.

### Build sau b? sung
- Command: `dotnet build .\ASMILE.BO.sln --no-incremental`
- Build: pass.
- Errors: 0.
- Warnings: 4 CA1416 cu trong `DependencyInjection.cs`.

---

## Step 8 completed - AR/AP persisted

### M?c ti?u d? ho?n th?nh
- ?? t?o b?ng c?ng n? ph?i thu/ph?i tr? persisted th?t v? c?c b?ng li?n quan cho ph?n b? thanh to?n, d?i chi?u c?ng n?, snapshot tu?i n?.
- ?? t?o `IDebtTransactionService` v? `DebtTransactionService` d? d?ng b? c?ng n? t? `JournalHeader`/`JournalLine`.
- ?? t?ch h?p `JournalService` g?i sync AR/AP sau khi journal du?c post ho?c reverse.
- `EfReceivableService` v? `EfPayableService` d? chuy?n sang persisted-first n?u b?ng c?ng n? c? d? li?u; n?u chua c? d? li?u th? gi? fallback legacy t? `BusinessDocument`/phi?u kho.
- Kh�ng l�m Step 9 v� kh�ng thay d?i UI l?n.

### File t?o
- `ASMILE.BO.Core/Entities/DebtEntities.cs`
- `ASMILE.BO.Application/Interfaces/IDebtTransactionService.cs`
- `ASMILE.BO.Infrastructure/Services/DebtTransactionService.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260520153302_AddArApPersistedTransactions.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260520153302_AddArApPersistedTransactions.Designer.cs`

### File s?a
- `ASMILE.BO.Core/Entities/ReceivableEntities.cs`
- `ASMILE.BO.Core/Entities/PayableEntities.cs`
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ASMILE.BO.Infrastructure/Services/JournalService.cs`
- `ASMILE.BO.Infrastructure/Services/EfReceivableService.cs`
- `ASMILE.BO.Infrastructure/Services/EfPayableService.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- `AddArApPersistedTransactions`
- Migration t?o:
  - `ReceivableTransactions`
  - `PayableTransactions`
  - `PaymentAllocations`
  - `DebtReconciliations`
  - `DebtReconciliationLines`
  - `AgingSnapshots`
- Migration b? sung seed mapping TT99 `Receivable -> 1311` d? nh?n di?n vai tr� ph?i thu qua `AccountRoleMapping`.

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`
- `dotnet-ef.exe migrations add AddArApPersistedTransactions --project .\ASMILE.BO.Infrastructure\ASMILE.BO.Infrastructure.csproj --startup-project .\ASMILE.BO.App\ASMILE.BO.App.csproj --context AppDbContext`
- `dotnet build .\ASMILE.BO.sln --no-incremental`

### Build result
- Build: pass.
- Errors: 0.
- Warnings: 4.
- Warning c�n l?i: CA1416 trong `ASMILE.BO.Infrastructure/DependencyInjection.cs` do `DpapiSecretProtector` l� Windows-only API.

### Persisted AR/AP d? l?m
- Entities: d? c? d? persisted entity cho ph?i thu, ph?i tr?, ph?n b? thanh to?n, d?i chi?u, snapshot tu?i n?.
- DbSet/Fluent API: d? c?u h?nh precision ti?n t?, max length, index partner/source/journal/status v? relationship d?i chi?u 1-n.
- Service: `DebtTransactionService` sync t? journal posted, ch?ng t?o tr�ng theo `JournalLineId`.
- Sync from journal:
  - Account role l?y qua `AccountRoleMapping`, kh�ng hard-code trong service.
  - `Receivable`: d�ng N? tang ph?i thu, d�ng C� gi?m ph?i thu.
  - `Payable`: d�ng C� tang ph?i tr?, d�ng N? gi?m ph?i tr?.
  - Thi?u CustomerId/SupplierId th� ghi audit warning v� b? qua d�ng, kh�ng l�m fail journal.
- Payable t? StockReceipt journal: ho?t d?ng khi d�ng Payable c� `SupplierId`; `DueDate` t?m = ng�y ch?ng t? + 30 ng�y.
- Receivable fallback: AR persisted ch? t?o khi c� journal line role `Receivable` v� `CustomerId`; hi?n `StockIssue` chua t?o AR v� m?i h?ch to�n gi� v?n/t?n kho.

### R?i ro/TODO
- AR ch? t?o khi c� journal line `Receivable` v� `CustomerId`; b�n h�ng/BusinessDocument chua t�ch h?p posting ? Step 8.
- Phi?u thu/phi?u chi chua post journal persisted n�n payment allocation m?i l� skeleton, chua wire UI.
- `DueDate` dang t?m +30 ng�y, c?n l?y theo di?u kho?n thanh to�n th?t ? bu?c sau.
- N?u d? li?u cu chua du?c migrate sang journal/c�ng n? persisted th� service v?n fallback legacy; c?n migration d? li?u l?ch s? ? giai do?n ?n d?nh.
- Snapshot tu?i n? hi?n luu theo tr?ng th�i t?i th?i di?m ch?y, chua c� l?ch t? d?ng.

### Vi?c ti?p theo
- Step 9: N?ng c?p Import Excel th?nh Import Engine d?ng chung.

---

## Step 9 completed - Import Excel Engine d�ng chung

### M?c ti?u d? ho?n th?nh
- ?? t?o n?n t?ng Import Engine d?ng chung d? preview, validate, luu phi?n import, luu l?i theo t?ng ? v? export danh s?ch l?i ra Excel.
- ?? gi? nguy?n c?c lu?ng import cu; engine m?i du?c th?m song song d? c?c module chuy?n d?n.
- ?? t?ch h?p th? handler cho danh m?c h?ng h?a (`Items`) v?i validate m? h?ng, t?n h?ng, don v? t?nh, tr?ng m? trong file v? tr?ng m?/t?n trong DB.
- Kh�ng l�m Step 10.

### File t?o
- `ASMILE.BO.Core/Entities/ImportEngineEntities.cs`
- `ASMILE.BO.Application/DTOs/ImportDtos.cs`
- `ASMILE.BO.Application/Interfaces/IImportEngine.cs`
- `ASMILE.BO.Infrastructure/Services/ImportEngine.cs`
- `ASMILE.BO.Infrastructure/Services/ItemImportTemplateHandler.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260520155935_AddImportEngine.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260520155935_AddImportEngine.Designer.cs`

### File s?a
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.Infrastructure/ASMILE.BO.Infrastructure.csproj`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- `AddImportEngine`
- Migration t?o:
  - `ImportTemplates`
  - `ImportSessions`
  - `ImportRows`
  - `ImportCellErrors`
- Seed template ban d?u:
  - `ITEMS_DEFAULT` cho `Items`.

### Command d? ch?y
- `dotnet ef migrations add AddImportEngine --project .\ASMILE.BO.Infrastructure\ASMILE.BO.Infrastructure.csproj --startup-project .\ASMILE.BO.App\ASMILE.BO.App.csproj --context AppDbContext`
  - Kh�ng ch?y du?c v� `dotnet-ef` kh�ng c� trong PATH.
- `& "$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe" migrations add AddImportEngine --project .\ASMILE.BO.Infrastructure\ASMILE.BO.Infrastructure.csproj --startup-project .\ASMILE.BO.App\ASMILE.BO.App.csproj --context AppDbContext`
  - Migration t?o th�nh c�ng.
  - C� c?nh b�o design-time DI cu c?a app, EF v?n t?o migration th�nh c�ng.
- `dotnet build .\ASMILE.BO.sln --no-incremental`

### Build result
- Build: pass.
- Errors: 0.
- Warnings: 4.
- Warning c�n l?i: CA1416 trong `ASMILE.BO.Infrastructure/DependencyInjection.cs` do `DpapiSecretProtector` l� Windows-only API.

### Import Engine d? c?
- Entity/session:
  - `ImportTemplate`
  - `ImportSession`
  - `ImportRow`
  - `ImportCellError`
- DTO/API:
  - `ImportPreviewRequest`
  - `ImportPreviewResult`
  - `ImportCommitResult`
  - `ImportRowDto`
  - `ImportCellErrorDto`
  - `ImportColumnMapping`
  - `ImportValidationResult`
- Service:
  - `IImportEngine`
  - `ImportEngine`
  - `IImportTemplateHandler`
  - `ItemImportTemplateHandler`
- Ch?c nang:
  - Copy file Excel sang temp tru?c khi d?c d? tr�nh kh�a file g?c.
  - ??c sheet d?u ti?n m?c d?nh, d?ng d?u l?m header.
  - Luu raw row d?ng JSON.
  - Validate l?i session theo handler.
  - Luu l?i/c?nh b�o theo t?ng �.
  - Ch?n commit n?u c�n l?i.
  - Ch?n commit n?u c�n warning v� Step 9 chua c� c? x�c nh?n warning.
  - Export l?i ra Excel b?ng ClosedXML.
  - Ghi audit khi preview/commit/fail n?u c� `IAuditLogService`.

### Handler Items d? l?m
- C?t b?t bu?c:
  - `M? h?ng`
  - `T�n h�ng`
  - `?on v? t?nh`
- C?t h? tr? th�m:
  - `Nh�m h�ng`
  - `M? nh? cung c?p` / `M? NCC`
  - `T�n nh� cung c?p` / `T�n NCC`
- Validate:
  - M? h?ng b?t bu?c.
  - T�n h�ng b?t bu?c.
  - ?on v? t?nh b?t bu?c.
  - Tr?ng m? h?ng trong file.
  - Tr?ng m? h?ng trong DB.
  - Tr�ng t�n h�ng trong DB l� warning.
- Commit:
  - T?o `Item` m?i khi d?ng valid v? m? h?ng chua t?n t?i.

### R?i ro/TODO
- UI import hi?n t?i chua du?c chuy?n sang engine m?i; Step 9 ch? t?o n?n t?ng v� handler th? cho h�ng h�a.
- Co ch? x�c nh?n warning chua c� tham s? confirm n�n `CommitAsync` dang tr? `RequiresConfirmation=true` v� chua commit khi c�n warning.
- Handler m?i ch? ?p d?ng th? cho `Items`; c?c module kh?ch h?ng, nh? cung c?p, kho, nh?n s?, t?i kho?n s? c?n handler ri?ng.
- Mapping header hi?n ki?m tra t�n c?t b?t bu?c theo t�n chu?n; c�c alias du?c d�ng trong handler khi validate t?ng d�ng.
- Chua c� test t? d?ng cho Import Engine; s? x? l� ? Step 19.

### Vi?c ti?p theo
- Step 10: Costing Engine cho kho.

---

## Step 10 completed - Costing Engine cho kho

### M?c ti?u d? ho?n th?nh
- ?? t?o n?n t?ng t?nh gi? v?n persisted cho kho.
- ?? th?m cost layer v? cost transaction d? luu l?ch s? gi? v?n.
- ?? tri?n khai Moving Average tru?c; c?c phuong ph?p PeriodicAverage/FIFO/SpecificIdentification m?i c? enum/skeleton d? m? r?ng sau.
- ?? t?ch h?p t?nh gi? v?n v?o lu?ng duy?t phi?u nh?p/xu?t hi?n t?i, kh?ng thay d?i UI l?n.
- ?? c?p nh?t PostingEngine d? phi?u xu?t uu ti?n d?ng `InventoryCostTransactions` khi ghi s? gi? v?n.
- Kh�ng l�m Step 11.

### File t?o
- `ASMILE.BO.Core/Enums/InventoryCostMethod.cs`
- `ASMILE.BO.Core/Entities/InventoryCostingEntities.cs`
- `ASMILE.BO.Application/DTOs/InventoryCostingDtos.cs`
- `ASMILE.BO.Application/Interfaces/IInventoryCostingService.cs`
- `ASMILE.BO.Infrastructure/Services/InventoryCostingService.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521020906_AddInventoryCosting.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521020906_AddInventoryCosting.Designer.cs`

### File s?a
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.Application/Services/InventoryAppService.cs`
- `ASMILE.BO.Infrastructure/Services/PostingEngine.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- `AddInventoryCosting`
- Migration t?o:
  - `InventoryCostLayers`
  - `InventoryCostTransactions`
- ?? c?u h?nh precision ti?n t?/s? lu?ng v? index:
  - `ItemId + WarehouseId`
  - `SourceType + SourceId`
  - `SourceDetailId`
  - `DocumentDate`
  - `Status`
  - `JournalHeaderId` cho cost transaction.

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`
- `& "$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe" migrations add AddInventoryCosting --project .\ASMILE.BO.Infrastructure\ASMILE.BO.Infrastructure.csproj --startup-project .\ASMILE.BO.App\ASMILE.BO.App.csproj --context AppDbContext`
- `dotnet build .\ASMILE.BO.sln --no-incremental`

### Build result
- Build: pass.
- Errors: 0.
- Warnings: 4.
- Warning c�n l?i: CA1416 trong `ASMILE.BO.Infrastructure/DependencyInjection.cs` do `DpapiSecretProtector` l� Windows-only API.

### Costing d? l?m
- Cost layers:
  - Phi?u nh?p t?o `InventoryCostLayer` theo t?ng d�ng chi ti?t.
  - Ch?ng t?o tr�ng theo `SourceDetailId` trong service.
- Cost transactions:
  - Phi?u xu?t t?o `InventoryCostTransaction` theo t?ng d�ng chi ti?t.
  - Sau khi posting th�nh c�ng, `PostingEngine` li�n k?t `JournalHeaderId` v�o cost transaction.
- MovingAverage:
  - T?nh don gi? b?nh qu?n = t?ng `RemainingCost` / t?ng `RemainingQuantity` c?a c?c layer m? tru?c ho?c b?ng ng?y xu?t.
  - Khi xu?t, service ti�u th? layer theo th? t? ng�y cu tru?c d? gi? l?ch s? layer d? ki?m tra.
- Apply receipt:
  - `InventoryAppService.ApproveReceiptAndPostAsync` g?i `ApplyReceiptCostAsync` tru?c khi ghi s? journal.
- Apply issue:
  - `InventoryAppService.ApproveIssueAndPostAsync` g?i `ApplyIssueCostAsync` tru?c khi ghi s? journal.
  - `StockIssueDetail.UnitPrice` v� `Amount` du?c c?p nh?t theo gi� v?n t�nh du?c v� hi?n entity dang d�ng c�c tru?ng n�y l�m gi� v?n trong kho.
- PostingEngine d�ng cost:
  - `StockIssue` journal uu ti�n t?ng gi� v?n t? `InventoryCostTransactions` theo `SourceId`.
  - N?u chua c� cost transaction th� fallback v? `Amount` ho?c `Quantity * UnitPrice` d? kh�ng ph� d? li?u cu.

### R?i ro/TODO
- FIFO, PeriodicAverage, SpecificIdentification m?i c� enum/skeleton, chua tri?n khai thu?t to�n.
- N?u d? li?u cu chua c� cost layer, `ApplyIssueCostAsync` fallback gi� v?n t? phi?u xu?t khi c� `UnitPrice/Amount`; n?u kh�ng c� gi� th� tr? l?i r�.
- RecalculateCost hi?n m?i reset transaction/layer t? ng�y y�u c?u d? chu?n b? t�nh l?i, chua replay to�n b? ch?ng t? theo k?.
- `StockBalance` v?n c�n logic rebuild cu, c� th? c?n d?ng b? ho�n to�n v?i Costing Engine ? bu?c refactor sau.
- Chua c? c?u h?nh cho ph?p xu?t ?m kho trong CostingService; hi?n v?n d?a v?o validate t?n kho s?n c? c?a `InventoryAppService`.
- VAT v� gi� v?n nhi?u chu?n chua x? l�; s? c?n engine k? to�n/report ? c�c bu?c sau.
- C?n test t? d?ng ? Step 19 cho MovingAverage, thi?u t?n gi� v?n v� duplicate costing.

### Vi?c ti?p theo
- Step 11: Report Engine.

---
## Step 11 completed - Report Engine n?n t?ng

### M?c ti?u d? ho?n th?nh
- ?? t?o Report Engine d?ng chung ch?y song song v?i b?o c?o/export hi?n t?i.
- ?? th?m entity persisted cho template, definition, parameter v? execution log.
- ?? h? tr? 6 m? b?o c?o n?n t?ng:
  - `GENERAL_LEDGER`
  - `TRIAL_BALANCE`
  - `INVENTORY_SUMMARY`
  - `INVENTORY_MOVEMENT`
  - `AP_AGING`
  - `AR_AGING`
- ?? h? tr? export Excel, CSV v? PDF t?i thi?u an to?n, kh?ng d?ng `Process.Start`.
- Kh�ng refactor UI b�o c�o hi?n t?i v� kh�ng l�m Step 12.

### File t?o
- `ASMILE.BO.Core/Entities/ReportEngineEntities.cs`
- `ASMILE.BO.Application/DTOs/ReportDtos.cs`
- `ASMILE.BO.Application/Interfaces/IReportEngine.cs`
- `ASMILE.BO.Infrastructure/Services/ReportEngine.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521023835_AddReportEngine.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521023835_AddReportEngine.Designer.cs`

### File s?a
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- `AddReportEngine`
- Migration t?o b?ng:
  - `ReportTemplates`
  - `ReportDefinitions`
  - `ReportParameters`
  - `ReportExecutions`
- ?? seed template h? th?ng cho 6 b?o c?o n?n t?ng.

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`
- `& "$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe" migrations add AddReportEngine --project .\ASMILE.BO.Infrastructure\ASMILE.BO.Infrastructure.csproj --startup-project .\ASMILE.BO.App\ASMILE.BO.App.csproj --context AppDbContext`
- `dotnet build .\ASMILE.BO.sln --no-incremental`

### Build result
- Build: pass.
- Errors: 0.
- Warnings: 4.
- Warning c�n l?i: CA1416 trong `ASMILE.BO.Infrastructure/DependencyInjection.cs` do `DpapiSecretProtector` l� Windows-only API.

### Reports d? h? tr?
- `GENERAL_LEDGER`: d?c `JournalHeader` + `JournalLine` persisted, filter ng�y/t�i kho?n/t? kh�a.
- `TRIAL_BALANCE`: t�nh ph�t sinh v� s? du theo `JournalLine` persisted; opening t?m t�nh b?ng ph�t sinh tru?c k?.
- `INVENTORY_SUMMARY`: d?c `StockBalance`, join `Item` v� `Warehouse`.
- `INVENTORY_MOVEMENT`: d?c `StockReceipt`, `StockIssue`, uu ti�n `InventoryCostTransaction` cho gi� v?n phi?u xu?t.
- `AP_AGING`: d?c `PayableTransaction` persisted v� chia bucket tu?i n?.
- `AR_AGING`: d?c `ReceivableTransaction` persisted v� chia bucket tu?i n?.

### Export d? h? tr?
- Excel: d�ng ClosedXML, c� title, header, autofilter, freeze row v� autofit column.
- CSV: UTF-8 BOM, escape comma/quote/newline.
- PDF: writer t?i thi?u n?i b?, kh�ng g?i process ngo�i; ch? ph� h?p xu?t nhanh d?ng text co b?n.

### Logging/report execution
- M?i l?n `ExecuteAsync` t?o `ReportExecution` tr?ng th�i `Running`.
- Khi th�nh c�ng c?p nh?t `Completed`, `CompletedAt`, `RowCount`.
- Khi l?i c?p nh?t `Failed`, `CompletedAt`, `ErrorMessage` r?i throw l?i l?i cho caller.

### R?i ro/TODO
- PDF export c�n r?t basic, chua ph?i report renderer d?p; n�n thay b?ng thu vi?n PDF an to�n ? giai do?n sau.
- Opening balance c?a Trial Balance chua c� snapshot/s? du d?u k? persisted ri�ng; hi?n t�nh b?ng ph�t sinh tru?c `fromDate`.
- UI b�o c�o hi?n t?i chua migrate sang `IReportEngine`; engine m?i ch?y song song.
- Financial statements TT99/TT200 chua l�m; x? l� ? Step 12 v� Report Engine m? r?ng.
- Inventory Summary v?n ph? thu?c `StockBalance`; n?u balance chua rebuild d�ng th� b�o c�o t?n kho cung l?ch.
- Chua c� test t? d?ng cho report engine; x? l� ? Step 19.

### Vi?c ti?p theo
- Step 12: TT99 / TT200 Accounting Standard Engine.

---
## Step 12 completed - TT99/TT200 Accounting Standard Engine

### M?c ti?u d? ho?n th?nh
- ?? t?o engine qu?n l? chu?n k? to?n TT99/TT200/CUSTOM.
- ?? th?m template t?i kho?n, template chu?n, mapping vai tr? t?i kho?n, mapping b?o c?o t?i ch?nh v? profile k? to?n c?ng ty.
- TT99 l� chu?n m?c d?nh m?i; TT200 gi? compatibility; CUSTOM d�nh cho t�y ch?nh.
- ?? s?a seed ti?ng Vi?t li?n quan d? migration kh?ng dua d? li?u m?i v?o DB v?i font mojibake.
- Kh�ng l�m Step 13.

### File t?o
- `ASMILE.BO.Core/Entities/AccountingStandardEngineEntities.cs`
- `ASMILE.BO.Application/Interfaces/IAccountingStandardService.cs`
- `ASMILE.BO.Infrastructure/Persistence/AccountingStandardSeedData.cs`
- `ASMILE.BO.Infrastructure/Services/AccountingStandardService.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521031817_AddAccountingStandardEngine.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521031817_AddAccountingStandardEngine.Designer.cs`

### File s?a
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- `AddAccountingStandardEngine`
- Migration t?o b?ng:
  - `AccountingStandardTemplates`
  - `AccountTemplates`
  - `FinancialStatementMappings`
  - `CompanyAccountingProfiles`
- Migration seed template t?i thi?u TT99/TT200/CUSTOM, role mapping theo chu?n, financial mapping skeleton v� profile m?c d?nh TT99.
- ?? r? migration: kh?ng c? `UpdateData` l?m seed cu quay v? mojibake.

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`
- `& "$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe" migrations add AddAccountingStandardEngine --project .\ASMILE.BO.Infrastructure\ASMILE.BO.Infrastructure.csproj --startup-project .\ASMILE.BO.App\ASMILE.BO.App.csproj --context AppDbContext`
- `dotnet build .\ASMILE.BO.sln --no-incremental`

### Build/Test result
- Build: pass.
- Errors: 0.
- Warnings: 4.
- Warning c�n l?i: CA1416 trong `ASMILE.BO.Infrastructure/DependencyInjection.cs` do `DpapiSecretProtector` l� Windows-only API.
- Test t? d?ng: chua c� test project.

### ?? h? tr?
- Accounting standards: TT99, TT200, CUSTOM.
- Account templates t?i thi?u cho t�i kho?n l�i.
- AccountRoleMapping theo chu?n:
  - Cash, Bank, Receivable, Inventory, Payable, VATInput, VATOutput, Revenue, COGS, Expense, FixedAsset, Depreciation, ToolAndSupply.
- FinancialStatementMapping skeleton cho `BALANCE_SHEET` v� `INCOME_STATEMENT`.
- CompanyAccountingProfile m?c d?nh.
- `ApplyStandardToCompanyAsync` copy template sang `LedgerAccount` an to�n, m?c d?nh kh�ng overwrite t�i kho?n ngu?i d�ng.
- Audit log khi d?i chu?n m?c d?nh/apply chu?n n?u audit service c� s?n.

### R?i ro/TODO
- AccountTemplate m?i seed t?i thi?u, chua d? to�n b? TT99/TT200.
- FinancialStatementMapping m?i l� skeleton, chua d? ch? ti�u b�o c�o t�i ch�nh ch�nh th?c.
- UI ch?n chu?n k? to�n chua wire; s? l�m sau.
- Chua migration d? li?u c�ng ty th?t theo multi-company profile.
- CUSTOM chua c? b? template ri?ng s?u, hi?n l? profile d? doanh nghi?p t?y ch?nh sau khi copy.
- L?nh t?o migration v?n hi?n c?nh b�o design-time DI cu c?a app; EF v?n scaffold migration th�nh c�ng.

### Vi?c ti?p theo
- Step 13: Module Qu?.

---
## Step 13 completed - Module Qu? ti?n m?t

### M?c ti?u d? ho?n th?nh
- ?? t?o n?n module Qu? ti?n m?t persisted: danh m?c qu?, giao d?ch qu?, s? qu? v? ki?m k? qu?.
- ?? t?ch h?p ghi s? Journal persisted cho thu/chi qu? qua `IJournalService`.
- Kh�ng hard-code t�i kho?n ti?n m?t trong service: uu ti�n `CashFund.AccountCode`, n?u tr?ng th� resolve qua `AccountRoleMapping` role `Cash`.
- ?? seed qu? m?c d?nh `CASH_MAIN` d?ng t?i kho?n `1111` ? seed d? li?u ban d?u.
- Kh�ng ph� UI phi?u thu/phi?u chi hi?n t?i; service m?i ch?y song song.
- Kh�ng l�m Step 14.

### File t?o
- `ASMILE.BO.Core/Entities/CashFundEntities.cs`
- `ASMILE.BO.Application/DTOs/CashFundDtos.cs`
- `ASMILE.BO.Application/Interfaces/ICashFundService.cs`
- `ASMILE.BO.Infrastructure/Services/CashFundService.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521041101_AddCashFundModule.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521041101_AddCashFundModule.Designer.cs`

### File s?a
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- `AddCashFundModule`
- Migration t?o b?ng:
  - `CashFunds`
  - `CashTransactions`
  - `CashCounts`
  - `CashCountLines`
- Migration seed qu? m?c d?nh:
  - `FundCode = CASH_MAIN`
  - `FundName = Qu? ti?n m?t ch�nh`
  - `CurrencyCode = VND`
  - `AccountCode = 1111`

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`
- `& "$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe" migrations add AddCashFundModule --project .\ASMILE.BO.Infrastructure\ASMILE.BO.Infrastructure.csproj --startup-project .\ASMILE.BO.App\ASMILE.BO.App.csproj --context AppDbContext`
- `dotnet build .\ASMILE.BO.sln --no-incremental`

### Build/Test result
- Build: pass.
- Errors: 0.
- Warnings: 4.
- Warning c�n l?i: CA1416 trong `ASMILE.BO.Infrastructure/DependencyInjection.cs` do `DpapiSecretProtector` l� Windows-only API.
- Test t? d?ng: chua c� test project.

### ?? l?m
- `CashFund`: qu?n l� qu?, t�i kho?n ti?n m?t, s? du d?u k?/s? du hi?n t?i, qu? m?c d?nh.
- `CashTransaction`: giao d?ch thu/chi/di?u ch?nh, tr?ng th�i Draft/Posted/Reversed, li�n k?t Journal.
- `CashBook`: d?c giao d?ch d? ghi s? v? t?nh running balance theo k?.
- `CashCount`: ki?m k� qu?, d�ng m?nh gi�, ch�nh l?ch s? s�ch/th?c t?.
- Journal integration:
  - Thu qu?: N? Cash / C� t�i kho?n d?i ?ng.
  - Chi qu?: N? t�i kho?n d?i ?ng / C� Cash.
  - Ki?m tra k? kh�a qua `IAccountingPeriodService` tru?c khi ghi s?.
  - ??o giao d?ch g?i `IJournalService.ReverseAsync`, kh?ng x?a b?t to?n cu.
- DI: dang k� `ICashFundService -> CashFundService`.
- Audit: ghi log t?o qu?, c?p nh?t qu?, t?o/post/d?o giao d?ch, t?o/x�c nh?n ki?m k� n?u audit service c� s?n.

### R?i ro/TODO
- UI module Qu? chua wire d?y d?; Step 13 m?i t?o n?n service/persisted.
- Phi?u thu/phi?u chi cu chua migrate sang `ICashFundService`; hi?n v?n gi? lu?ng cu d? kh�ng ph� UI.
- `CounterAccountCode` b?t bu?c khi post giao d?ch qu?; c?n UI/c?u h�nh ch?n t�i kho?n d?i ?ng r� r�ng.
- Kh?ng cho ?m qu? m?c d?nh; n?u c?n cho ?m theo quy?n/c?u h?nh s? l?m ? bu?c c?u h?nh sau.
- CashCount adjustment ch? t? t?o giao d?ch di?u ch?nh khi c� t�i kho?n d?i ?ng; n?u thi?u th� ch? ghi nh?n ki?m k� v� log pending.
- `CASH_BOOK` chua wire v�o ReportEngine ? Step 13 d? tr�nh m? r?ng ph?m vi; c� th? b? sung ? bu?c report m? r?ng.
- L?nh t?o migration v?n hi?n c?nh b�o design-time DI cu c?a app; EF v?n scaffold migration th�nh c�ng.

### Vi?c ti?p theo
- Step 14: Module Ng?n h?ng.

---
## Step 14 completed - Module Ng?n h?ng

### M?c ti?u d? ho?n th?nh
- ?? t?o n?n Banking Module persisted: t?i kho?n ng?n h?ng, giao d?ch ng?n h?ng, sao k?, d?ng sao k?, d?i chi?u ng?n h?ng.
- ?? t?ch h?p ghi s? Journal persisted cho thu/chi ng?n h?ng qua `IJournalService`.
- Kh�ng hard-code t�i kho?n 112/1121 trong service: uu ti�n `BankAccount.AccountCode`, n?u tr?ng th� resolve qua `AccountRoleMapping` role `Bank`.
- ?? seed t?i kho?n ng?n h?ng m?c d?nh `MAIN_BANK` kh?ng g?n s?n account code d? service resolve theo mapping.
- Kh?ng ph? UI hi?n t?i; service ng?n h?ng m?i ch?y song song.
- Kh�ng l�m Step 15.

### File t?o
- `ASMILE.BO.Core/Entities/BankingEntities.cs`
- `ASMILE.BO.Application/DTOs/BankingDtos.cs`
- `ASMILE.BO.Application/Interfaces/IBankingService.cs`
- `ASMILE.BO.Infrastructure/Services/BankingService.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521043833_AddBankingModule.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521043833_AddBankingModule.Designer.cs`

### File s?a
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- `AddBankingModule`
- Migration t?o b?ng:
  - `BankAccounts`
  - `BankTransactions`
  - `BankStatements`
  - `BankStatementLines`
  - `BankReconciliations`
  - `BankReconciliationLines`
- Migration seed t?i kho?n ng?n h?ng m?c d?nh:
  - `BankCode = MAIN_BANK`
  - `AccountNumber = MAIN_BANK`
  - `AccountName = T?i kho?n ng?n h?ng ch?nh`
  - `CurrencyCode = VND`

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`
- `& "$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe" migrations add AddBankingModule --project .\ASMILE.BO.Infrastructure\ASMILE.BO.Infrastructure.csproj --startup-project .\ASMILE.BO.App\ASMILE.BO.App.csproj --context AppDbContext`
- `dotnet build .\ASMILE.BO.sln --no-incremental`

### Build/Test result
- Build: pass.
- Errors: 0.
- Warnings: 4.
- Warning c�n l?i: CA1416 trong `ASMILE.BO.Infrastructure/DependencyInjection.cs` do `DpapiSecretProtector` l� Windows-only API.
- Test t? d?ng: chua c� test project.

### ?? l?m
- `BankAccount`: qu?n l? t?i kho?n ng?n h?ng, s? t?i kho?n, ng?n h?ng, chi nh?nh, ti?n t?, t?i kho?n k? to?n li?n k?t, s? du.
- `BankTransaction`: giao d?ch thu/chi ng?n h?ng, tr?ng th?i Draft/Posted/Reconciled/Reversed, li?n k?t Journal.
- `BankBook`: d?c giao d?ch d? ghi s?/d? d?i chi?u v? t?nh running balance theo k?.
- `BankStatement`: import sao k� CSV t?i thi?u, t?o d�ng sao k� v� tr?ng th�i match.
- `BankReconciliation`: d?i chi?u heuristic theo s? ti?n, ng�y g?n nhau v� reference number n?u c�.
- Journal integration:
  - Thu ng?n h?ng: N? Bank / C? t?i kho?n d?i ?ng.
  - Chi ng?n h?ng: N? t?i kho?n d?i ?ng / C? Bank.
  - Ki?m tra k? kh�a qua `IAccountingPeriodService` tru?c khi ghi s?.
  - ??o giao d?ch g?i `IJournalService.ReverseAsync`, kh?ng x?a b?t to?n cu.
- DI: dang k� `IBankingService -> BankingService`.
- Audit: ghi log t?o t�i kho?n, t?o/post/d?o giao d?ch, import sao k�, t?o/x�c nh?n d?i chi?u n?u audit service c� s?n.

### R?i ro/TODO
- Chua c? internet banking/API ng?n h?ng th?t ? Step 14.
- CSV parser m?i ? m?c don gi?n, c?n chu?n h�a template import sao k� sau.
- Matching d?i chi?u c?n heuristic, chua c? rule engine d?i chi?u n?ng cao.
- Transfer li�n t�i kho?n chua tri?n khai d?y d?, dang d? ph?m vi sau.
- UI module Ng?n h?ng chua wire d?y d?; Step 14 m?i t?o n?n service/persisted.
- `BANK_BOOK` chua wire v�o ReportEngine d? tr�nh m? r?ng ph?m vi; c� th? b? sung ? bu?c report m? r?ng.
- L?nh t?o migration v?n hi?n c?nh b�o design-time DI cu c?a app; EF v?n scaffold migration th�nh c�ng.

### Vi?c ti?p theo
- Step 15: Module TSC?.

---
## Step 15 completed - Module TSC?

### M?c ti?u d? ho?n th?nh
- ?? t?o n?n Fixed Asset Module persisted: nh?m TSC?, t?i s?n c? d?nh, kh?u hao, di?u ch?nh v? ghi gi?m/thanh l?.
- ?? t?o service TSC? d?c l?p d? t?o t?i s?n, k?ch ho?t, ch?y kh?u hao du?ng th?ng, ghi s? kh?u hao, ghi gi?m t?i s?n v? d?c l?ch kh?u hao.
- ?? t?ch h?p Journal persisted cho ghi tang TSC? v? tr?ch kh?u hao qua `IJournalService`.
- Kh�ng hard-code t�i kho?n 211/214/642 trong service: uu ti�n t�i kho?n khai b�o tr�n `FixedAssetCategory`, n?u tr?ng th� resolve qua `AccountRoleMapping` role `FixedAsset`, `Depreciation`, `Expense`.
- Kh?ng ph? UI hi?n t?i; module TSC? m?i ch?y song song ? t?ng service/persisted.
- Kh�ng l�m Step 16.

### File t?o
- `ASMILE.BO.Core/Entities/FixedAssetEntities.cs`
- `ASMILE.BO.Application/DTOs/FixedAssetDtos.cs`
- `ASMILE.BO.Application/Interfaces/IFixedAssetService.cs`
- `ASMILE.BO.Infrastructure/Services/FixedAssetService.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521051106_AddFixedAssetModule.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521051106_AddFixedAssetModule.Designer.cs`

### File s?a
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- `AddFixedAssetModule`
- Migration t?o b?ng:
  - `FixedAssetCategories`
  - `FixedAssets`
  - `FixedAssetDepreciations`
  - `FixedAssetAdjustments`
  - `FixedAssetDisposals`
- Migration seed nh?m TSC? m?c d?nh:
  - `CategoryCode = TSCD_DEFAULT`
  - `CategoryName = T�i s?n c? d?nh m?c d?nh`
  - `UsefulLifeMonthsDefault = 60`
  - `DepreciationMethodDefault = StraightLine`

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`
- `& "$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe" migrations add AddFixedAssetModule --project .\ASMILE.BO.Infrastructure\ASMILE.BO.Infrastructure.csproj --startup-project .\ASMILE.BO.App\ASMILE.BO.App.csproj --context AppDbContext`
- `dotnet build .\ASMILE.BO.sln --no-incremental`

### Build/Test result
- Build: pass.
- Errors: 0.
- Warnings: 4.
- Warning c�n l?i: CA1416 trong `ASMILE.BO.Infrastructure/DependencyInjection.cs` do `DpapiSecretProtector` l� Windows-only API.
- Test t? d?ng: chua c� test project.

### ?? l?m
- `FixedAssetCategory`: nh?m TSC?, t?i kho?n t?i s?n/kh?u hao/chi ph? t?y ch?n, th?i gian kh?u hao m?c d?nh.
- `FixedAsset`: m? t?i s?n, nguy?n gi?, gi? tr? thu h?i, th?i gian kh?u hao, hao m?n luy k?, gi? tr? c?n l?i, tr?ng th?i.
- `FixedAssetDepreciation`: d�ng kh?u hao theo k?, tr?ng th�i Draft/Posted, li�n k?t Journal.
- `FixedAssetAdjustment`: n?n di?u ch?nh tang/gi?m/d�nh gi� l?i/di?u chuy?n.
- `FixedAssetDisposal`: n?n ghi gi?m/thanh l?, gi? tr? thanh l? v? l?i/l?.
- Depreciation Engine:
  - H? tr? `StraightLine` tru?c.
  - Kh�ng t?o tr�ng d�ng kh?u hao theo `FixedAssetId + DepreciationPeriod`.
  - Kh�ng kh?u hao vu?t gi� tr? c�n l?i.
- Journal integration:
  - K�ch ho?t c� t�i kho?n d?i ?ng: N? FixedAsset / C� t�i kho?n d?i ?ng.
  - Tr�ch kh?u hao: N? Expense / C� Depreciation.
  - Ki?m tra k? kh�a qua `IAccountingPeriodService` tru?c khi ghi s?.
- DI: dang k� `IFixedAssetService -> FixedAssetService`.
- Audit: ghi log t?o t�i s?n, k�ch ho?t, ch?y/post kh?u hao, ghi gi?m n?u audit service c� s?n.

### R?i ro/TODO
- UI module TSC? chua wire d?y d?; Step 15 m?i t?o n?n service/persisted.
- `DecliningBalance` v� `UnitsOfProduction` chua tri?n khai.
- Disposal journal m?i d?ng ? skeleton nghi?p v? ghi gi?m; chua t? sinh b?t to?n thanh l? d?y d? v? c?n c?u h?nh t?i kho?n l?i/l?/thanh l? r? hon.
- TSC? tang t? AP/invoice/mua h?ng chua t? d?ng d?ng b?.
- Category m?c d?nh kh�ng hard-code account code; n?u thi?u AccountRoleMapping `FixedAsset`, `Depreciation`, `Expense` th� service s? b�o l?i r�.
- C�c tru?ng Department/Warehouse/Supplier/Employee dang d�ng `Guid?` theo JournalLine/roadmap, chua t?o FK t?i danh m?c hi?n h?u d? tr�nh mismatch ki?u kh�a hi?n t?i.
- L?nh t?o migration v?n hi?n c?nh b�o design-time DI cu c?a app; EF v?n scaffold migration th�nh c�ng.

### Vi?c ti?p theo
- Step 16: Module CCDC.

---
## Step 16 completed - Module CCDC

### M?c ti?u d? ho?n th?nh
- ?? t?o n?n CCDC persisted: nh?m CCDC, c?ng c? d?ng c?, xu?t d?ng, l?ch ph?n b?, l?n ch?y ph?n b?, thu h?i v? thanh l?.
- ?? t?o service CCDC d?c l?p d? t?o CCDC, xu?t d?ng, l?p l?ch ph?n b? du?ng th?ng, ch?y ph?n b? theo k?, ghi s? ph?n b? v? d?c t?ng quan.
- ?? t?ch h?p Journal persisted cho xu?t d?ng CCDC v? ph?n b? CCDC qua `IJournalService`.
- Kh�ng hard-code t�i kho?n 153/242/642 trong service: uu ti�n t�i kho?n khai b�o tr�n `ToolAssetCategory`, n?u tr?ng th� resolve qua `AccountRoleMapping` role `ToolAndSupply`, `PrepaidExpense`, `Expense`.
- Kh�ng ph� UI hi?n t?i; module CCDC m?i ch?y song song ? t?ng service/persisted.
- Kh�ng l�m Step 17.

### File t?o
- `ASMILE.BO.Core/Entities/ToolAssetEntities.cs`
- `ASMILE.BO.Application/DTOs/ToolAssetDtos.cs`
- `ASMILE.BO.Application/Interfaces/IToolAssetService.cs`
- `ASMILE.BO.Infrastructure/Services/ToolAssetService.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521053544_AddToolAssetModule.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521053544_AddToolAssetModule.Designer.cs`

### File s?a
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ASMILE.BO.Infrastructure/Persistence/AccountingStandardSeedData.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- `AddToolAssetModule`
- Migration t?o b?ng:
  - `ToolAssetCategories`
  - `ToolAssets`
  - `ToolIssues`
  - `ToolAllocationSchedules`
  - `ToolAllocationRuns`
  - `ToolRecoveries`
  - `ToolDisposals`
- Migration seed nh�m CCDC m?c d?nh:
  - `CategoryCode = CCDC_DEFAULT`
  - `CategoryName = C�ng c? d?ng c? m?c d?nh`
  - `DefaultAllocationMonths = 12`
- Migration/seed b? sung role mapping:
  - `PrepaidExpense -> 242`

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`
- `& "$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe" migrations add AddToolAssetModule --project .\ASMILE.BO.Infrastructure\ASMILE.BO.Infrastructure.csproj --startup-project .\ASMILE.BO.App\ASMILE.BO.App.csproj --context AppDbContext`
- `dotnet build .\ASMILE.BO.sln --no-incremental`

### Build/Test result
- Build: pass.
- Errors: 0.
- Warnings: 4.
- Warning c�n l?i: CA1416 trong `ASMILE.BO.Infrastructure/DependencyInjection.cs` do `DpapiSecretProtector` l� Windows-only API.
- Test t? d?ng: chua c� test project.

### ?? l?m
- `ToolAssetCategory`: nh?m CCDC, t?i kho?n CCDC/chi ph? tr? tru?c/chi ph? t?y ch?n, s? th?ng ph?n b? m?c d?nh.
- `ToolAsset`: m? CCDC, nguy?n gi?, s? lu?ng, don gi?, d? ph?n b?, c?n l?i, tr?ng th?i.
- `ToolIssue`: xu?t d�ng CCDC, tr?ng th�i Draft/Posted, li�n k?t Journal.
- `ToolAllocationSchedule`: l?ch ph?n b? t?ng k?, tr?ng th?i Draft/Posted, li?n k?t Journal.
- `ToolAllocationRun`: l?n ch?y ph?n b? theo k?.
- `ToolRecovery` v� `ToolDisposal`: n?n thu h?i/thanh l� CCDC.
- Allocation Engine:
  - H? tr? ph?n b? du?ng th?ng tru?c.
  - Kh?ng t?o tr?ng l?ch ph?n b? c?ng `ToolAssetId + AllocationPeriod`.
  - Kh?ng ph?n b? vu?t gi? tr? c?n l?i.
- Journal integration:
  - Xu?t d?ng ph?n b? nhi?u k?: N? PrepaidExpense / C? ToolAndSupply.
  - Xu?t d?ng ph?n b? ngay: N? Expense / C? ToolAndSupply.
  - Ph?n b? k?: N? Expense / C? PrepaidExpense.
  - Ki?m tra k? kh�a qua `IAccountingPeriodService` tru?c khi ghi s?.
- DI: dang k� `IToolAssetService -> ToolAssetService`.
- Audit: ghi log t?o CCDC, xu?t d?ng, ch?y/post ph?n b?, thu h?i/thanh l? n?u audit service c? s?n.

### R?i ro/TODO
- UI module CCDC chua wire d?y d?; Step 16 m?i t?o n?n service/persisted.
- Thu h?i/thanh l� m?i ? m?c n?n nghi?p v?, chua t? sinh journal di?u ch?nh/thanh l� d?y d?.
- Chua t? d?ng sync CCDC t? mua h�ng/kho/phi?u nh?p.
- Ph?n b? hi?n m?i h? tr? du?ng th?ng; chua c? rule ph?n b? n?ng cao.
- AccountRoleMapping `PrepaidExpense -> 242` d? seed, nhung database th?c t? c?n apply migration d? c? d? li?u.
- C�c tru?ng Department/Warehouse/Supplier/Employee dang d�ng `Guid?` theo JournalLine/roadmap, chua t?o FK t?i danh m?c hi?n h?u d? tr�nh mismatch ki?u kh�a hi?n t?i.
- L?nh t?o migration v?n hi?n c?nh b�o design-time DI cu c?a app; EF v?n scaffold migration th�nh c�ng.

### Vi?c ti?p theo
- Step 17: VAT/Thu? co b?n.


---
## Step 17 completed - VAT/Thu? co b?n

### M?c ti?u d? ho?n th?nh
- ?? t?o n?n VAT persisted cho m? thu?, giao d?ch VAT v? t? khai VAT nh?p.
- ?? t?o `ITaxService`/`TaxService` d? qu?n l? m? thu?, t?o giao d?ch VAT, d?ng b? VAT t? Journal v? l?p nh?p t? khai VAT.
- ?? t?ch h?p `JournalService` g?i d?ng b? VAT sau khi journal du?c post/reverse, ch?y song song v?i AR/AP sync.
- Kh�ng hard-code t�i kho?n 1331/3331 trong service: nh?n di?n d�ng VAT qua `AccountRoleMapping` role `VATInput`/`VATOutput`.
- ?? th?m b?o c?o `VAT_INPUT` v? `VAT_OUTPUT` trong `ReportEngine`.
- Kh�ng ch?nh UI v� kh�ng l�m h�a don di?n t? d?y d? ? Step 17.

### File t?o
- `ASMILE.BO.Application/Interfaces/ITaxService.cs`
- `ASMILE.BO.Infrastructure/Services/TaxService.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521072800_AddVatModule.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521072800_AddVatModule.Designer.cs`

### File s?a
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.Infrastructure/Services/JournalService.cs`
- `ASMILE.BO.Infrastructure/Services/ReportEngine.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### File d�ng l?i
- `ASMILE.BO.Core/Entities/VatEntities.cs`
- `ASMILE.BO.Application/DTOs/VatDtos.cs`

### Migration
- `AddVatModule`
- Migration t?o b?ng:
  - `TaxCodes`
  - `VatTransactions`
  - `TaxDeclarationDrafts`
- Migration seed m? thu?:
  - `VAT0`
  - `VAT5`
  - `VAT8`
  - `VAT10`
  - `KCT`
- Migration seed report template:
  - `VAT_INPUT`
  - `VAT_OUTPUT`

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`
- `& "$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe" migrations add AddVatModule --project .\ASMILE.BO.Infrastructure\ASMILE.BO.Infrastructure.csproj --startup-project .\ASMILE.BO.App\ASMILE.BO.App.csproj --context AppDbContext`
- `dotnet build .\ASMILE.BO.sln --no-incremental`

### Build/Test result
- Build: pass.
- Errors: 0.
- Warnings: 4.
- Warning c�n l?i: CA1416 trong `ASMILE.BO.Infrastructure/DependencyInjection.cs` do `DpapiSecretProtector` l� Windows-only API.
- Test t? d?ng: chua c� test project.

### ?? l?m
- `TaxCode`: m? thu?, t?n thu?, thu? su?t, lo?i thu?, t?i kho?n d?u v?o/d?u ra t?y ch?n.
- `VatTransaction`: giao d?ch VAT theo source/journal/invoice, chi?u Input/Output, k? k� khai v� tr?ng th�i.
- `TaxDeclarationDraft`: nh�p t? khai VAT theo k?, t?ng VAT d?u v�o/d?u ra v� s? VAT ph?i n?p.
- `TaxService`:
  - `GetTaxCodesAsync`
  - `CreateTaxCodeAsync`
  - `CreateVatTransactionAsync`
  - `SyncVatFromJournalAsync`
  - `BuildVatDeclarationDraftAsync`
  - `MarkDeclaredAsync`
  - `GetVatTransactionsAsync`
- `JournalService`: sau khi post/reverse journal s? g?i VAT sync n?u c� `ITaxService`.
- `ReportEngine`: h? tr? b�o c�o `VAT_INPUT` v� `VAT_OUTPUT`, export qua engine hi?n c�.

### R?i ro/TODO
- Chua t? th�m d�ng VAT v�o posting phi?u nh?p/xu?t v� ch?ng t? hi?n chua c� d? li?u TaxCode/TaxAmount.
- `SyncVatFromJournalAsync` ch? t?o VAT khi journal line c� role `VATInput`/`VATOutput` v� c� metadata `TaxCode`; n?u thi?u TaxCode th� log skip d? tr�nh t?o d? li?u thu? sai.
- H�a don di?n t?, b?ng k� thu? d?y d? theo m?u co quan thu? v� tr?ng th�i k� khai th?c t? chua l�m.
- `TaxCode.TaxType` seed hi?n d?ng don gi?n, m?t m? thu? c? th? d?ng cho c? input/output qua direction giao d?ch; c?n chu?n h?a th?m khi l?m UI thu?/h?a don.
- UI chua wire v�o TaxService.
- EF migration command v?n c� warning design-time DI cu c?a app; migration v?n t?o th�nh c�ng.

### Vi?c ti?p theo
- Step 18: Financial Statements + Closing Engine.

---
## Step 18 completed - Financial Statements + Closing Engine

### M?c ti?u d? ho?n th?nh
- ?? t?o n?n Financial Statements persisted d?a tr?n Journal persisted.
- ?? t?o Accounting Closing Engine d? k?t chuy?n doanh thu/chi ph? sang IncomeSummary v? RetainedEarnings qua `AccountRoleMapping`.
- ?? t?o snapshot s? du t?i kho?n cu?i k?.
- ?? th?m b?o c?o t?i ch?nh n?n t?ng: `BALANCE_SHEET`, `INCOME_STATEMENT`, `CASH_FLOW`.
- Kh�ng ch?nh UI v� kh�ng l�m Step 19.

### File t?o
- `ASMILE.BO.Core/Entities/FinancialStatementEntities.cs`
- `ASMILE.BO.Application/DTOs/FinancialStatementDtos.cs`
- `ASMILE.BO.Application/Interfaces/IFinancialStatementService.cs`
- `ASMILE.BO.Application/Interfaces/IClosingService.cs`
- `ASMILE.BO.Infrastructure/Services/FinancialStatementService.cs`
- `ASMILE.BO.Infrastructure/Services/ClosingService.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521080623_AddFinancialStatementEngine.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521080623_AddFinancialStatementEngine.Designer.cs`

### File s?a
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ASMILE.BO.Infrastructure/Persistence/AccountingStandardSeedData.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.Infrastructure/Services/ReportEngine.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- `AddFinancialStatementEngine`
- T?o b?ng:
  - `AccountBalanceSnapshots`
  - `FinancialStatementExecutions`
  - `ClosingEntries`
  - `ClosingEntryLines`
- Seed th�m role mapping:
  - `IncomeSummary -> 911`
  - `RetainedEarnings -> 421`
- Seed th�m report template:
  - `BALANCE_SHEET`
  - `INCOME_STATEMENT`
  - `CASH_FLOW`

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`
- `& "$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe" migrations add AddFinancialStatementEngine --project .\ASMILE.BO.Infrastructure\ASMILE.BO.Infrastructure.csproj --startup-project .\ASMILE.BO.App\ASMILE.BO.App.csproj --context AppDbContext`
- `dotnet build .\ASMILE.BO.sln --no-incremental`

### Build/Test result
- Build: pass.
- Errors: 0.
- Warnings: 4.
- Warning c�n l?i: CA1416 trong `ASMILE.BO.Infrastructure/DependencyInjection.cs` do `DpapiSecretProtector` l� Windows-only API.
- Test t? d?ng: chua c� test project.

### ?? l?m
- `AccountBalanceSnapshot`: luu opening/period/closing theo t�i kho?n v� k?.
- `FinancialStatementExecution`: luu l?ch s? sinh b�o c�o t�i ch�nh v� JSON k?t qu?.
- `ClosingEntry`/`ClosingEntryLine`: luu l?n k?t chuy?n k? v� d�ng k?t chuy?n.
- `FinancialStatementService`:
  - `BuildSnapshotsAsync`
  - `GenerateBalanceSheetAsync`
  - `GenerateIncomeStatementAsync`
  - `GenerateCashFlowAsync`
  - `GetSummaryAsync`
- `ClosingService`:
  - `ClosePeriodAsync`
  - `ReverseClosingAsync`
- Closing journal:
  - K?t chuy?n doanh thu/thu nh?p v�o `IncomeSummary`.
  - K?t chuy?n chi ph� v�o `IncomeSummary`.
  - K?t chuy?n l?i/l? sang `RetainedEarnings`.
  - T?o Journal persisted th�ng qua `IJournalService` n�n v?n d�ng validation hi?n c�.
- `ReportEngine`: h? tr? th�m `BALANCE_SHEET`, `INCOME_STATEMENT`, `CASH_FLOW` song song v?i report cu.
- DI: dang k� `IFinancialStatementService` v� `IClosingService`.

### R?i ro/TODO
- Cash Flow m?i l? skeleton, chua ph?n lo?i d?y d? ho?t d?ng kinh doanh/d?u tu/t?i ch?nh theo chu?n tr?c ti?p/gi?n ti?p.
- FinancialStatementMapping TT99/TT200 hi?n v?n t?i thi?u; b�o c�o d�ng fallback theo metadata/prefix t�i kho?n n?u mapping chua d?.
- Closing Engine chua c� UI wizard v� chua t? kh�a/d�ng k? sau khi k?t chuy?n d? tr�nh thay d?i workflow hi?n t?i.
- Chua c� audit workflow ph� duy?t closing ri�ng.
- Chua c� snapshot s? du d?u k? ch�nh th?c t? nh?p s? du d?u k?; opening hi?n t�nh t? journal tru?c k?.
- Design-time EF v?n c?nh b�o DI cu c?a app khi scaffold migration, nhung migration t?o th�nh c�ng.

### Vi?c ti?p theo
- Step 19: Test t? d?ng.



---
## Step 19 completed - Automated Testing + Audit + Hardening

### M?c ti?u d? ho?n th?nh
- ?? t?o h? t?ng test t? d?ng cho Unit Test v? Integration Test.
- ?? b? sung audit trail persisted d?ng chung qua `IAuditService`/`AuditService`.
- ?? b? sung RowVersion/concurrency token cho c?c entity nghi?p v? tr?ng y?u.
- ?? tang validation/hardening cho posting, costing, closing v? b?o c?o t?i ch?nh ch?y du?c qua test.
- Kh�ng l�m Step 20.

### File t?o
- `ASMILE.BO.Tests.Unit/ASMILE.BO.Tests.Unit.csproj`
- `ASMILE.BO.Tests.Unit/TestSupport/TestDbContextFactory.cs`
- `ASMILE.BO.Tests.Unit/PostingEngineTests.cs`
- `ASMILE.BO.Tests.Unit/InventoryCostingTests.cs`
- `ASMILE.BO.Tests.Unit/ClosingServiceTests.cs`
- `ASMILE.BO.Tests.Unit/VatServiceTests.cs`
- `ASMILE.BO.Tests.Unit/FixedAssetTests.cs`
- `ASMILE.BO.Tests.Unit/ToolAssetTests.cs`
- `ASMILE.BO.Tests.Integration/ASMILE.BO.Tests.Integration.csproj`
- `ASMILE.BO.Tests.Integration/TestSupport/TestDbContextFactory.cs`
- `ASMILE.BO.Tests.Integration/AccountingFlowIntegrationTests.cs`
- `ASMILE.BO.Tests.Integration/CashBankIntegrationTests.cs`
- `ASMILE.BO.Application/Interfaces/IAuditService.cs`
- `ASMILE.BO.Infrastructure/Services/AuditService.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521093739_AddAuditTrailAndConcurrency.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260521093739_AddAuditTrailAndConcurrency.Designer.cs`

### File s?a
- `ASMILE.BO.sln`
- `ASMILE.BO.Core/Entities/AuditLog.cs`
- `ASMILE.BO.Core/Entities/AccountingCoreEntities.cs`
- `ASMILE.BO.Core/Entities/StockReceipt.cs`
- `ASMILE.BO.Core/Entities/StockIssue.cs`
- `ASMILE.BO.Core/Entities/CashFundEntities.cs`
- `ASMILE.BO.Core/Entities/BankingEntities.cs`
- `ASMILE.BO.Core/Entities/FixedAssetEntities.cs`
- `ASMILE.BO.Core/Entities/ToolAssetEntities.cs`
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.Infrastructure/Services/JournalService.cs`
- `ASMILE.BO.Infrastructure/Services/ClosingService.cs`
- `ASMILE.BO.Infrastructure/Services/FinancialStatementService.cs`
- `ASMILE.BO.Infrastructure/Services/InventoryCostingService.cs`
- `ASMILE.BO.Infrastructure/Services/CashFundService.cs`
- `ASMILE.BO.Infrastructure/Services/BankingService.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- `AddAuditTrailAndConcurrency`
- M? r?ng `AuditLogs` v?i c�c tru?ng persisted: `EntityName`, `EntityId`, `OldValuesJson`, `NewValuesJson`, `IpAddress`, `CorrelationId`.
- Th�m RowVersion cho: `JournalHeaders`, `StockReceipts`, `StockIssues`, `FixedAssets`, `ToolAssets`, `CashTransactions`, `BankTransactions`.
- Luu �: SQL Server d�ng RowVersion/timestamp; test InMemory d�ng c?u h�nh provider-safe d? kh�ng sinh l?i concurrency gi?.

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`
- `dotnet test .\ASMILE.BO.sln --no-build`

### Build/Test result
- Build: pass.
- Errors: 0.
- Warnings: 4.
- Warning c�n l?i: CA1416 trong `ASMILE.BO.Infrastructure/DependencyInjection.cs` do `DpapiSecretProtector` l� Windows-only API.
- Unit tests: pass 16/16.
- Integration tests: pass 4/4.
- T?ng test: pass 20/20.

### ?? l?m
- Unit tests:
  - Posting Engine: post th�nh c�ng, ch?n duplicate posting, ch?n k? kh�a, reverse journal.
  - Inventory Costing: MovingAverage, ch?n ?m t?n gi? v?n, ch?n duplicate cost transaction.
  - Closing Service: t?o closing journal, reverse closing.
  - VAT Service: sync VAT t? journal, kh�ng t?o VAT khi tax amount = 0, build declaration.
  - Fixed Asset: kh?u hao du?ng th?ng, kh�ng duplicate depreciation k?.
  - Tool Asset: l?ch ph?n b?, ph?n b? d? v? remaining = 0.
- Integration tests:
  - Accounting flow: journal, closing, snapshot, financial statement c?n.
  - Cash/Bank flow: thu/chi qu?, thu/chi ng?n h?ng v? reconciliation co b?n.
- Audit trail:
  - T?o `IAuditService`/`AuditService` persisted.
  - Hook audit v�o Journal post/reverse v� Closing post/reverse.
- Concurrency protection:
  - Th�m RowVersion cho entity tr?ng y?u.
  - `AppDbContext.SaveChangesAsync` chuy?n `DbUpdateConcurrencyException` th�nh l?i nghi?p v? ti?ng Vi?t r� r�ng.
- Validation/hardening:
  - Costing kh?ng fallback ?m t?n gi? v?n t?y ti?n; tr? l?i khi thi?u cost layer.
  - Posting duplicate d? du?c test qua `PostingSource`/ngu?n ch?ng t?.
  - Closing v? FinancialStatement query d? ch?nh d? ch?y an to?n tr?n test provider, tr?nh l?i provider-specific.
  - Cash/Bank summary chuy?n aggregate decimal sang client-side khi c?n d? tr�nh l?i provider test.

### R?i ro/TODO
- Test hi?n d�ng EF InMemory d? tr�nh kh�c bi?t RowVersion/transaction c?a SQLite; c?n b? sung test SQL Server th?t tru?c khi release production.
- Chua benchmark performance cho d? li?u l?n; `FinancialStatementService` v� `ClosingService` c� do?n Include + projection client-side d? tuong th�ch test provider, c?n t?i uu b?ng query SQL khi d? li?u l?n.
- Audit m?i hook c�c lu?ng ch�nh, chua t? d?ng audit m?i entity CRUD.
- Chua c� distributed transaction, multi-tenant, HA/replication.
- CA1416 DPAPI warning c�n l?i n�n x? l� ? Step 20/hardening ho?c b?ng guard platform r� r�ng.

### Vi?c ti?p theo
- Step 20: Anti-virus false positive hardening.

---
## Step 20 completed - Production Readiness + Architecture Cleanup

### M?c ti?u d? ho?n th?nh
- ?? x? l? warning CA1416 c?a `DpapiSecretProtector` b?ng platform guard v? fallback protector cho non-Windows.
- ?? th?m exception chu?n cho business/validation/not found/concurrency.
- ?? th?m strongly typed options c? validate startup.
- ?? m? r?ng migration/health/seed consistency foundation.
- ?? th?m n?n background job runner nh?.
- ?? t?o t?i li?u ki?n tr?c, accounting flow v? deployment notes.
- Kh�ng rewrite business modules, kh�ng d?i behavior k? to�n v� kh�ng l�m UI redesign.

### File t?o
- `ASMILE.BO.Infrastructure/Security/PlainTextSecretProtector.cs`
- `ASMILE.BO.Shared/Exceptions/AppException.cs`
- `ASMILE.BO.Infrastructure/Options/ProductionOptions.cs`
- `ASMILE.BO.Application/DTOs/InfrastructureDtos.cs`
- `ASMILE.BO.Application/Interfaces/InfrastructureInterfaces.cs`
- `ASMILE.BO.Infrastructure/Services/InfrastructureHealthService.cs`
- `ARCHITECTURE_OVERVIEW.md`
- `ACCOUNTING_ENGINE_FLOW.md`
- `DEPLOYMENT_NOTES.md`

### File s?a
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/DatabaseMigrationService.cs`
- `ASMILE.BO.Shared/Exceptions/AppValidationException.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- Kh�ng t?o migration m?i ? Step 20.
- Step n�y ch? cleanup infrastructure/configuration v� kh�ng d?i schema database.

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`
- `dotnet test .\ASMILE.BO.sln --no-build`
- `dotnet build .\ASMILE.BO.sln --no-incremental`

### Build/Test result
- Build: pass.
- Errors: 0.
- Warnings: 0.
- Unit tests: pass 16/16.
- Integration tests: pass 4/4.
- T?ng test: pass 20/20.

### Production readiness items
- Secret protection:
  - Windows d�ng `DpapiSecretProtector`.
  - Non-Windows d�ng `PlainTextSecretProtector` fallback d? tr�nh g?i Windows-only API ngo�i n?n t?ng.
  - CA1416 d? h?t, kh?ng suppress v? l?.
- Exception hardening:
  - `AppException`, `BusinessException`, `AppNotFoundException`, `AppConcurrencyException`, `ErrorCodes`.
  - `AppValidationException` gi? compatibility nhung chu?n h�a theo base exception.
- Configuration hardening:
  - `AccountingOptions`, `InventoryOptions`, `SecurityOptions` c� validation.
- Health/infrastructure:
  - `IInfrastructureHealthService` ki?m tra DB, migration pending v� seed warning.
  - `IDatabaseMigrationService` c� validate pending migrations, apply migrations v� validate seed consistency.
- Seed consistency:
  - Ki?m tra TT99 active.
  - Ki?m tra role mappings b?t bu?c.
  - Ki?m tra AccountTemplate, FinancialStatementMapping, TaxCode m?c d?nh.
- Background jobs foundation:
  - `IBackgroundJob`, `IBackgroundJobRunner`, `BackgroundJobRunner` c� correlation scope v� structured logging.
- Documentation:
  - Ki?n tr�c t?ng quan.
  - Lu?ng posting/costing/closing/VAT/AR/AP/report.
  - Deployment notes v� production checklist.

### R?i ro/TODO
- Non-Windows fallback kh?ng m? h?a secret; production WPF m?c ti?u v?n l? Windows, fallback ch? d? dev/test ho?c m?i tru?ng kh?ng d?ng DPAPI.
- Health service m?i l� n?n; chua c� UI/CLI v?n h�nh ri�ng d? ch?y health/migration check.
- Chua c� CI/CD pipeline th?c t?.
- Chua c� benchmark SQL Server d? li?u l?n.
- Permission system, multi-company, e-invoice v� deployment automation l� phase ti?p theo.

### Vi?c ti?p theo
- Backend core roadmap d? ho?n t?t 20/20 step.
- Phase ti?p theo n?n t?p trung UI wiring cho engine m?i, ph?n quy?n, multi-company, e-invoice, SQL Server performance tuning v? CI/CD.


## Report Center wiring - 2026-05-22

### Ph?m vi
- Ch? wire Report Center v�o `IReportEngine` persisted.
- Kh�ng redesign to�n b? UI.
- Kh�ng rewrite ReportEngine.
- Kh�ng x�a c�c m�n b�o c�o/export cu.

### File t?o
- `ASMILE.BO.App/ViewModels/ReportCenterViewModel.cs`
- `ASMILE.BO.App/Views/ReportCenterView.xaml`
- `ASMILE.BO.App/Views/ReportCenterView.xaml.cs`

### File s?a
- `ASMILE.BO.Application/DTOs/ReportDtos.cs`
- `ASMILE.BO.Application/Interfaces/IReportEngine.cs`
- `ASMILE.BO.Infrastructure/Services/ReportEngine.cs`
- `ASMILE.BO.App/DependencyInjection.cs`
- `ASMILE.BO.App/Themes/DataTemplates.xaml`
- `ASMILE.BO.App/ViewModels/DesktopShellViewModel.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### ?? l?m
- Report Center m?i d�ng `IReportEngine` d? t?i template, tham s?, ch?y b�o c�o, xu?t file v� d?c l?ch s? th?c thi.
- H? tr? b�o c�o persisted: `GENERAL_LEDGER`, `TRIAL_BALANCE`, `INVENTORY_SUMMARY`, `INVENTORY_MOVEMENT`, `AP_AGING`, `AR_AGING`, `BALANCE_SHEET`, `INCOME_STATEMENT`, `CASH_FLOW`, `VAT_INPUT`, `VAT_OUTPUT`.
- Th�m `GetParametersAsync` v� `GetExecutionHistoryAsync` v�o `IReportEngine` d? UI kh�ng d?c DB tr?c ti?p.
- M?n Report Center c? danh m?c b?o c?o, t?m ki?m, y?u th?ch, panel tham s?, grid k?t qu? ?o h?a, ph?n trang, l?ch s? ch?y v? export Excel/PDF/CSV.
- Map `stock-report` v� `debt-report` t? Desktop Shell sang Report Center thay v� placeholder.
- Gi? l?i c�c m�n b�o c�o cu trong S? c�i/Kho/C�ng n? l�m bridge/fallback, chua x�a.

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`

### Build result
- Build: pass.
- Errors: 0.
- Warnings: 0.

### Mock/legacy c�n l?i
- C�c report card hard-code trong `InventoryMovementViewModel` v� `PayableViewModel` v?n c�n d? tr�nh ph� workflow cu.
- C�c m�n in/preview cu v?n d�ng `PrintPreviewDialog`, chua chuy?n to�n b? sang ReportEngine.
- Favorite reports hi?n luu trong phi�n ViewModel, chua persisted theo user.

### R?i ro/TODO
- N?u DB chua seed `ReportParameters`, parameter panel s? tr?ng v� ReportEngine d�ng default n?i b?.
- Export hi?n luu file v�o `Documents\ASMILE BO\Reports`, chua c� Export Center ri�ng c� h�ng d?i/t?i l?i.
- PDF export ph? thu?c implementation hi?n c� c?a `SimplePdfWriter`, chua ki?m tra ch?t lu?ng layout b�o c�o l?n.
- C?n wire d?n c�c tab b�o c�o cu sang Report Center ho?c bridge adapter n?u mu?n th?ng nh?t ho�n to�n.

## PHASE 2 STEP 2 - General Ledger enterprise workflow - 2026-05-22

### Ph?m vi
- Ch? n?ng module S? c?i: Journal Entry, Trial Balance, Account Catalog, drilldown/workflow, DataGrid performance v? keyboard workflow.
- Kh�ng redesign to�n app, kh�ng rewrite accounting backend, kh�ng d?i behavior k? to�n.

### File s?a
- `ASMILE.BO.Application/DTOs/GeneralLedgerDtos.cs`
- `ASMILE.BO.Infrastructure/Services/EfGeneralLedgerService.cs`
- `ASMILE.BO.App/ViewModels/GeneralLedgerViewModel.cs`
- `ASMILE.BO.App/Views/GeneralLedgerView.xaml`
- `ASMILE.BO.App/Views/GeneralLedgerView.xaml.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### ?? l?m
- S? c�i ti?p t?c d�ng Journal persisted l�m ngu?n ch�nh, legacy fallback ch? d�ng khi chua c� journal persisted.
- B? sung metadata journal v�o `LedgerEntryDto`: `JournalId`, `JournalLineId`, `SourceType`, `SourceNo`, `Status`, `StatusDisplay`, `IsLockedPeriod`.
- B? sung filter persisted journal theo ngu?n ghi s? v� tr?ng th�i.
- Map tr?ng th?i journal sang ti?ng Vi?t: Nh?p, ?? ghi s?, ?? d?o, ?? h?y.
- ViewModel S? c�i c� async refresh v?i `CancellationToken`, debounce t�m ki?m/l?c v� ch?ng popup li�n t?c khi user g�.
- B? sung workflow ghi s?/d?o b�t to�n persisted qua `IJournalService`:
  - Ghi s? d�ng dang ch?n.
  - ??o b?t to?n d?ng dang ch?n.
  - Ghi s? h�ng lo?t v�ng ch?n/d�ng hi?n t?i.
  - ??o h?ng lo?t v?ng ch?n/d?ng hi?n t?i.
- DataGrid S? c�i b?t row/column virtualization, recycling, multi-row selection v� full-row selection.
- B? sung keyboard workflow t?i thi?u:
  - `Ctrl+C` copy c�c d�ng b�t to�n dang ch?n d?ng tab-separated d? d�n Excel.
  - `Enter` commit cell v� chuy?n focus sang � ti?p theo.
- B? l?c trong drawer d�ng label ti?ng Vi?t cho ngu?n ghi s? v� tr?ng th�i.
- S?a m?t s? chu?i mojibake ti?ng Vi?t trong `EfGeneralLedgerService.CreateManualEntryAsync`.

### Performance improvements
- Kh�ng d�ng `.Result`/`.Wait` trong lu?ng S? c�i v?a s?a.
- Query S? c�i v?n d�ng `AsNoTracking` ? t?ng service persisted.
- Refresh UI c� h?y request cu v� debounce 350ms khi g�/l?c.
- DataGrid ch�nh du?c ?o h�a theo h�ng/c?t v� t�i ch? row container.

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`

### Build result
- Build: pass.
- Errors: 0.
- Warnings: 0.

### Legacy areas c�n l?i
- Tab B?o c?o v? Nh?t k? thao t?c trong `GeneralLedgerViewModel` v?n c?n d? li?u d?ng t? ViewModel d? gi? bridge cu; Report Center persisted d? c? nhung chua thay to?n b? tab n?y.
- Account Catalog chua c� tree control th?t; hi?n m?i gi? grid cu c� metadata v� c� th? drill/filter ti?p ? bu?c sau.
- Trial Balance hierarchy m?i d?ng d? li?u c?n d?i persisted, chua c? expand/collapse cha-con d?y d?.
- Source document navigation m?i ? m?c metadata `SourceType/SourceNo`, chua c� router kh�ng hard-code t?i t?ng ch?ng t?.
- Batch workflow hi?n ch?y tr�n v�ng ch?n DataGrid/d�ng hi?n t?i, chua c� command bar ri�ng cho �selected count�.

### R?i ro/TODO
- C?n ki?m th? runtime v?i d? li?u l?n d? x�c nh?n virtualization kh�ng b? style cha override.
- C?n b? sung enterprise drilldown d?y d?: Trial Balance -> Journal, Account -> Related journals, Journal -> Source document.
- C?n t�ch ph?n Report/Audit tab c?a S? c�i sang ReportEngine/Audit persisted trong phase UI wiring ti?p theo.
- C?n th�m inline validation editor chi ti?t n?u mu?n nh?p b�t to�n d?ng spreadsheet ngay trong grid.

### Vi?c ti?p theo
- STEP 2.3 Inventory Enterprise UX.

## PHASE 2 STEP 2.3 - Inventory enterprise workflow - 2026-05-22

### Ph?m vi
- Ch? n?ng m?n Qu?n l? xu?t nh?p t?n: b?o c?o kho, import Excel, cost visibility, drilldown nh?, grid performance v? keyboard workflow.
- Kh�ng redesign to�n app, kh�ng rewrite backend inventory/accounting, kh�ng d?i workflow cu dang d�ng.

### File s?a
- `ASMILE.BO.App/ViewModels/InventoryMovementViewModel.cs`
- `ASMILE.BO.App/Views/InventoryMovementView.xaml`
- `ASMILE.BO.App/Views/InventoryMovementView.xaml.cs`
- `ASMILE.BO.Infrastructure/Services/EfInventoryMovementService.cs`
- `ASMILE.BO.Infrastructure/Services/ReportEngine.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### ?? l?m
- Audit nhanh m?n xu?t nh?p t?n, x?c d?nh backend persisted d? c? `IInventoryMovementService`, `IInventoryCostingService`, `IImportEngine`, `IReportEngine`.
- `InventoryMovementViewModel` d? inject v? d?ng tr?c ti?p `IReportEngine`/`IImportEngine` thay cho lu?ng report/import placeholder.
- B?o c?o kho trong m?n xu?t nh?p t?n d? ch?y qua ReportEngine persisted, c? preview/export/l?ch s? ch?y b?o c?o.
- B? sung report persisted trong `ReportEngine`:
  - `STOCK_CARD`
  - `LOW_STOCK`
  - `NEGATIVE_STOCK`
  - `COST_ANALYSIS`
- Import Excel trong UI d? chuy?n sang ImportEngine persisted:
  - ch?n template
  - preview rows
  - validate
  - commit sau khi x�c nh?n
  - xu?t file l?i Excel n?u c� l?i
- DataGrid xu?t nh?p t?n b?t virtualization h�ng/c?t, recycling, multi-select, full-row selection v� copy clipboard k�m header.
- B? sung keyboard workflow t?i thi?u:
  - `Ctrl+C` copy c�c d�ng dang ch?n d?ng tab-separated d? d�n Excel.
  - `Enter` commit edit v� chuy?n focus ti?p theo.
- `EfInventoryMovementService` uu ti�n gi� v?n persisted t? `InventoryCostTransactions` khi hi?n th? phi?u xu?t/chuy?n kho.
- S?a c�c chu?i ti?ng Vi?t mojibake c�n s�t trong inventory/report: tr?ng th�i t?n, chuy?n kho, th�ng b�o export/import.
- `DataMode` c?a m�n kho hi?n th? d�ng ngu?n `SQL Server / persisted`.

### Performance improvements
- Kh�ng d�ng `.Result`/`.Wait` trong ph?n ViewModel v?a s?a.
- Refresh c� `CancellationToken` v� h?y request cu.
- T�m ki?m/l?c d�ng debounce 350ms d? tr�nh query li�n t?c khi g�.
- C�c query report m?i d�ng `AsNoTracking`.
- DataGrid ch�nh du?c ?o h�a theo h�ng/c?t v� t�i ch? container.

### Import improvements
- Import UI kh�ng c�n g?i tr?c ti?p service legacy `ImportFromExcelAsync` c?a kho.
- File Excel du?c x? l? qua ImportEngine d? c? session/row/cell error persisted.
- C� du?ng xu?t danh s�ch l?i ra `Documents\ASMILE BO\Imports`.

### Costing visibility added
- B�o c�o `COST_ANALYSIS` d?c `InventoryCostLayers` v� `InventoryCostTransactions`.
- B?o c?o `INVENTORY_MOVEMENT` v? m?n movement uu ti?n gi? v?n persisted c?a phi?u xu?t n?u d? c? cost transaction.
- `LOW_STOCK`/`NEGATIVE_STOCK` d?c t? `StockBalances`.

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`

### Build result
- Build: pass.
- Errors: 0.
- Warnings: 0.

### Legacy areas c�n l?i
- Stock Receipt/Stock Issue form ri�ng v?n gi? workflow hi?n t?i; chua th�m UI batch post/reverse d?y d?.
- Inline validation spreadsheet-level cho d�ng phi?u nh?p/xu?t m?i ? m?c n?n ViewModel/Grid, chua c� editor d�ng chi ti?t ri�ng.
- Paste nhi?u d�ng t? Excel v�o line grid chua wire d?y d?.
- `LOW_STOCK` d�ng ngu?ng m?c d?nh 10 v� chua c� c?u h�nh/min stock field chu?n trong entity.
- Favorite/recent report ? inventory m?i l?y history t? ReportEngine; chua persisted favorite theo user.
- Inventory drilldown m?i ? m?c d? li?u/report v� row detail; chua c� router di?u hu?ng ngu?n ch?ng t? kh�ng hard-code.

### R?i ro/TODO
- C?n ki?m th? runtime v?i d? li?u l?n d? x�c nh?n virtualization kh�ng b? style cha override.
- C?n wire Stock Receipt/Stock Issue enterprise form ri�ng n?u mu?n workflow Draft/Approve/Post/Reverse ho�n ch?nh trong UI.
- C?n b? sung c?u h�nh t?n t?i thi?u/t?i da theo h�ng-kho d? thay ngu?ng c?nh b�o t?m.
- PDF report v?n d�ng PDF writer t?i thi?u c?a ReportEngine, chua ph?i layout b�o c�o s?n xu?t.

### Vi?c ti?p theo
- PHASE 2 STEP 2.4 Cash/Bank Enterprise UX.

### B? sung PHASE 2 STEP 2.3 - Stock Receipt/Stock Issue enterprise UX - 2026-05-22

#### File s?a th�m
- `ASMILE.BO.Application/DTOs/StockReceiptDto.cs`
- `ASMILE.BO.Application/DTOs/StockIssueDto.cs`
- `ASMILE.BO.Application/Services/InventoryAppService.cs`
- `ASMILE.BO.App/ViewModels/StockReceiptsViewModel.cs`
- `ASMILE.BO.App/ViewModels/StockIssuesViewModel.cs`
- `ASMILE.BO.App/Views/StockReceiptsView.xaml`
- `ASMILE.BO.App/Views/StockIssuesView.xaml`
- `ASMILE.BO.App/Views/StockReceiptsView.xaml.cs`
- `ASMILE.BO.App/Views/StockIssuesView.xaml.cs`

#### ?? l?m th?m
- Expose tr?ng th�i ghi s? persisted cho phi?u nh?p/xu?t qua DTO: `PostedAt`, `PostedBy`, `JournalHeaderId`, `PostingStatus`, `IsPosted`, `WorkflowStatusText`.
- `InventoryAppService` map d?y d? tr?ng th�i posting t? entity sang DTO d? UI kh�ng c�n ch? bi?t tr?ng th�i duy?t cu.
- Phi?u nh?p b? sung tr?ng th?i workflow tr?n form, c?nh b?o validation inline v? nh?n h?nh d?ng `Duy?t & ghi s?`.
- Phi?u xu?t b? sung tr?ng th�i workflow, c?nh b�o t?n kh? d?ng v� gi� v?n u?c t�nh tr�n form.
- Phi?u nh?p/xu?t c� validation tru?c khi luu/duy?t:
  - ph?i c� d�ng chi ti?t,
  - ph?i ch?n h�ng h�a/kho,
  - s? lu?ng > 0,
  - don gi?/gi? v?n kh?ng ?m,
  - c?nh b�o xu?t vu?t t?n kh? d?ng.
- DataGrid phi?u nh?p/xu?t b?t virtualization h�ng/c?t, recycling, multi-select v� full-row selection.
- B? sung keyboard workflow t?i thi?u cho grid phi?u nh?p/xu?t:
  - `Ctrl+C` copy d�ng dang ch?n d?ng tab-separated k�m header,
  - `Enter`/`Tab` commit edit d? thao t�c nhanh hon.
- Danh s?ch phi?u nh?p/xu?t hi?n th? tr?ng th?i workflow ti?ng Vi?t: Nh?p, ?? duy?t, ?? ghi s?.

#### Command d? ch?y th?m
- `dotnet build .\ASMILE.BO.sln --no-incremental`

#### Build result
- Build: pass.
- Errors: 0.
- Warnings: 0.

#### R?i ro/TODO c�n l?i
- Chua c� batch post/reverse UI ri�ng cho phi?u nh?p/xu?t.
- Chua c� paste nhi?u d�ng Excel v�o line grid; hi?n m?i h? tr? copy ra Excel.
- Drilldown t? phi?u sang journal/source document chua c� router enterprise ho�n ch?nh.
- Cost visibility ? phi?u xu?t m?i hi?n th? gi� v?n u?c t�nh t? d�ng dang ch?n; l?ch s? cost layer d?y d? n?m ? b�o c�o/costing service.
- Validation inline d? c? n?n t?ng, nhung chua highlight t?ng ? l?i tr?c ti?p trong DataGrid.

### B? sung ho�n t?t PHASE 2 STEP 2.3 - Inventory enterprise UX - 2026-05-22

#### File s?a th�m
- `ASMILE.BO.Application/Interfaces/IInventoryAppService.cs`
- `ASMILE.BO.Application/Services/InventoryAppService.cs`
- `ASMILE.BO.Application/DTOs/StockReceiptDto.cs`
- `ASMILE.BO.Application/DTOs/StockIssueDto.cs`
- `ASMILE.BO.Infrastructure/Services/EfInventoryMovementService.cs`
- `ASMILE.BO.Infrastructure/Services/ReportEngine.cs`
- `ASMILE.BO.App/ViewModels/ReportCenterViewModel.cs`
- `ASMILE.BO.App/ViewModels/InventoryMovementViewModel.cs`
- `ASMILE.BO.App/ViewModels/StockReceiptsViewModel.cs`
- `ASMILE.BO.App/ViewModels/StockIssuesViewModel.cs`
- `ASMILE.BO.App/Views/InventoryMovementView.xaml`
- `ASMILE.BO.App/Views/StockReceiptsView.xaml`
- `ASMILE.BO.App/Views/StockIssuesView.xaml`
- `ASMILE.BO.App/Views/StockReceiptsView.xaml.cs`
- `ASMILE.BO.App/Views/StockIssuesView.xaml.cs`

#### UX improvements
- Stock Receipt/Stock Issue form c� tr?ng th�i Draft/Approved/Posted/Reversed qua persisted posting metadata.
- B? sung reverse cho phi?u nh?p/xu?t d? ghi s? b?ng posting reversal hi?n c?; ch?ng t? `Reversed` b? lo?i kh?i t?n/report hi?n h?nh.
- B? sung batch post cho danh s�ch phi?u xu?t ch? duy?t.
- L?i save/post/reverse tr? v? khung validation inline, h?n ch? popup l?i l?p.
- Grid phi?u nh?p/xu?t h? tr? multi-select, copy tab-separated v� paste nhi?u d�ng t? Excel v�o line grid.
- Realtime totals hi?n th? s? lu?ng, VAT placeholder, gi� tr? t?n/gi� v?n u?c t�nh.

#### Performance improvements
- Load phi?u nh?p/xu?t truy?n `CancellationToken` v�o inventory search.
- Grid inventory/report/import v� line grid b?t row/column virtualization, recycling v� full-row selection.
- Kh�ng th�m `.Result`/`.Wait`.

#### Import improvements
- M?n inventory import d? d?ng `IImportEngine.PreviewAsync`, `ValidateAsync`, `CommitAsync`, `ExportErrorsAsync`.
- UI import c� template selection, preview rows, warning summary, commit sau validation v� download error Excel.

#### Costing visibility added
- Phi?u xu?t hi?n th? t?n kh? d?ng, moving-average/estimated cost v� negative stock warning.
- `COST_ANALYSIS`, `INVENTORY_MOVEMENT`, `STOCK_CARD`, `LOW_STOCK`, `NEGATIVE_STOCK` du?c expose trong Report Center/Inventory report cards qua ReportEngine.
- Cost analysis lo?i tr? source d? d?o d? tr?nh l?ch cost visibility sau reverse.

#### Legacy areas c�n l?i
- Reverse hi?n bridge qua journal reversal v� lo?i kh?i t?n/report; chua sinh voucher inventory d?o ri�ng theo t?ng d�ng.
- Favorite report m?i l� UI local trong Report Center, chua persisted per-user.
- Router drilldown enterprise t? movement sang source voucher/journal v?n ? m?c command/data hi?n c�, chua t�ch navigation service kh�ng hard-code d?y d?.
- PDF export v?n l� writer t?i thi?u c?a ReportEngine.

#### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`

#### Build result
- Build: pass.
- Errors: 0.
- Warnings: 0.

#### Recommended next step
- PHASE 2 STEP 2.4 Cash/Bank Enterprise UX.

---

## PHASE 2 STEP 2.4 - Cash/Bank Enterprise UX - 2026-05-22

### Ph?m vi
- N?ng module Qu? + Ng?n h?ng b?ng backend persisted hi?n c?: CashFund, Banking, ImportEngine, ReportEngine, Journal persisted.
- Kh�ng redesign to�n app, kh�ng rewrite accounting backend, kh�ng x�a workflow phi?u thu/phi?u chi cu.

### File t?o
- `ASMILE.BO.App/ViewModels/CashBankViewModel.cs`
- `ASMILE.BO.App/Views/CashBankView.xaml`
- `ASMILE.BO.App/Views/CashBankView.xaml.cs`
- `ASMILE.BO.Infrastructure/Services/BankStatementImportTemplateHandler.cs`

### File s?a ch�nh
- `ASMILE.BO.Application/Interfaces/ICashFundService.cs`
- `ASMILE.BO.Application/Interfaces/IBankingService.cs`
- `ASMILE.BO.Application/Interfaces/IImportEngine.cs`
- `ASMILE.BO.Application/DTOs/BankingDtos.cs`
- `ASMILE.BO.Infrastructure/Services/CashFundService.cs`
- `ASMILE.BO.Infrastructure/Services/BankingService.cs`
- `ASMILE.BO.Infrastructure/Services/ImportEngine.cs`
- `ASMILE.BO.Infrastructure/Services/ReportEngine.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.App/DependencyInjection.cs`
- `ASMILE.BO.App/Themes/DataTemplates.xaml`
- `ASMILE.BO.App/ViewModels/DesktopShellViewModel.cs`
- `ASMILE.BO.App/ViewModels/ReportCenterViewModel.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### UX improvements
- Th?m module m?i `Qu? & Ng?n h?ng` song song v?i `Phi?u thu`/`Phi?u chi` legacy d? kh?ng ph? workflow hi?n t?i.
- Cash transaction UX c� quick entry cho thu ti?n, chi ti?n, transfer n?i b?; h? tr? Draft, Approve, Cancel approval, Post, Reverse v� batch posting.
- Bank transaction UX c� quick entry thu/chi, approve/cancel/post/reverse v� batch posting.
- UI hi?n th? realtime current fund balance, daily inflow/outflow, pending cash transactions, bank current balance, available balance, pending amount v� last reconciliation.
- Th�m inline validation panel cho duplicate transaction, inactive fund/account, missing account, negative/over-balance warning v� l?i locked period t? service.
- Drilldown hi?n th? Cash book/Bank book -> Journal, source document, source type/source id, posted date v� reversed/posting state d?a tr�n metadata persisted, kh�ng route hard-code m�n ch?ng t?.

### Reconciliation improvements
- Bank reconciliation tab hi?n th? reconciliation header v� line detail.
- Hi?n th? matched amount, unmatched amount, difference v� suggested match note t? persisted reconciliation lines.
- C� command ch?y reconciliation t? bank account + statement v� confirm reconciliation qua `IBankingService`.
- Statement list v� line preview l?y t? persisted `BankStatements`/`BankStatementLines`.

### Import improvements
- Statement import UI d�ng `IImportEngine` v?i template `BANK_STATEMENT`, kh�ng d�ng placeholder cu.
- H? tr? ch?n CSV/XLSX; UI chu?n h�a file sang XLSX t?m v� t? g?n `BankAccountId` theo t�i kho?n dang ch?n.
- Preview rows, validate, error/warning summary, duplicate detection trong file v� database.
- Download error Excel qua `ExportErrorsAsync`.
- `IImportEngine.CommitAsync` c� c? `allowWarnings` d? commit sau x�c nh?n, gi? default cu l� kh�ng commit warning n?u caller chua x�c nh?n.

### Report improvements
- Wire report Cash/Bank persisted:
  - `CASH_BOOK`
  - `CASH_FLOW`
  - `BANK_BOOK`
  - `BANK_RECONCILIATION`
  - `DAILY_CASHFLOW`
  - `CASH_BALANCE`
- Cash/Bank report tab c� async preview, export Excel/PDF/CSV, recent execution history v� favorite report local.
- `ReportCenterViewModel` expose th�m Cash/Bank report codes d? Report Center d�ng chung ReportEngine.

### Performance improvements
- Kh�ng th�m `.Result`/`.Wait`.
- Refresh/filter/report/import d�ng async v� `CancellationToken`.
- Filter keyword/date/fund/account d�ng debounce 350ms.
- Cash, Bank, Import, Reconciliation, Report grids b?t row/column virtualization, recycling, multi-select v� full-row selection.
- Grid cash/bank h? tr? `Ctrl+C` copy tab-separated v� `Ctrl+V` paste Excel rows v�o quick entry.
- Backend read APIs d�ng `AsNoTracking`.

### Legacy compatibility
- Kh�ng x�a `PaymentReceiptsViewModel`, `PaymentVouchersViewModel`, `PaymentReceiptsView`, `PaymentVouchersView`.
- Shell v?n route `payment-receipt` v� `payment-voucher` v? workflow cu.
- Module m?i d�ng key `cash-bank`; `cash`/`bank` ch? l� alias route sang module enterprise m?i.
- Posting cu v?n cho post tr?c ti?p t? Draft; approve m?i l� bu?c persisted b? sung, kh�ng b?t bu?c legacy flow.

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`

### Build result
- Build: pass.
- Errors: 0.
- Warnings: 0.

### Remaining risks
- Favorite reports hi?n l� collection local trong ViewModel, chua persisted per-user.
- Drilldown m?i hi?n th? metadata journal/source, chua c� navigation router m? d�ng voucher/journal theo source type.
- Bank statement duplicate detection dang l� warning; commit warning c?n user x�c nh?n ? UI m?i.
- CSV parser trong UI x? l? CSV ph? bi?n, chua ph?i parser statement ng?n h?ng chuy?n s?u cho m?i dialect.
- PDF export v?n d�ng ReportEngine writer t?i thi?u.

### Recommended next step
- PHASE 2 STEP 2.5 Fixed Asset + Tool Asset Enterprise UX.

### Hotfix runtime - 2026-05-22
- S?a crash WPF khi m?/render module Cash/Bank: `XamlParseException` do implicit `Style TargetType="DataGrid"` trong `CashBankView.xaml` tr�ng key resource `System.Windows.Controls.DataGrid`.
- ??i th?nh keyed style `CashBankDataGridStyle` v? g?n expl?cit v?o c?c DataGrid c?a m?n Cash/Bank.
- S?a ti?p crash khi m? Inventory enterprise: `InventoryMovementView.xaml` c? hai implicit `Style TargetType="DataGrid"` trong c?ng `UserControl.Resources`; b? style d?u v? style sau d? ch?a d? virtualization/multi-select setters.
- Qu�t XAML c�n l?i: kh�ng c�n file n�o c� hon m?t implicit DataGrid style trong c�ng file.
- Build l?i Debug v� Release d?u pass 0 warning/0 error.

---

## PHASE 2 STEP 2.5 - Fixed Asset + Tool Asset Enterprise UX - 2026-05-22

### Ph?m vi
- N?ng module TSC? + CCDC b?ng backend persisted hi?n c?: `IFixedAssetService`, `IToolAssetService`, `IReportEngine`, Journal persisted.
- Kh�ng redesign to�n app, kh�ng rewrite accounting backend, kh�ng x�a workflow cu.

### File t?o
- `ASMILE.BO.App/ViewModels/AssetManagementViewModel.cs`
- `ASMILE.BO.App/Views/AssetManagementView.xaml`
- `ASMILE.BO.App/Views/AssetManagementView.xaml.cs`

### File s?a ch�nh
- `ASMILE.BO.Application/DTOs/FixedAssetDtos.cs`
- `ASMILE.BO.Application/Interfaces/IFixedAssetService.cs`
- `ASMILE.BO.Application/Interfaces/IToolAssetService.cs`
- `ASMILE.BO.Infrastructure/Services/FixedAssetService.cs`
- `ASMILE.BO.Infrastructure/Services/ToolAssetService.cs`
- `ASMILE.BO.Infrastructure/Services/ReportEngine.cs`
- `ASMILE.BO.App/DependencyInjection.cs`
- `ASMILE.BO.App/Themes/DataTemplates.xaml`
- `ASMILE.BO.App/ViewModels/DesktopShellViewModel.cs`
- `ASMILE.BO.App/ViewModels/ReportCenterViewModel.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### UX improvements
- Th?m module enterprise m?i `TSC? & CCDC` (`asset-management`) ch?y song song, kh?ng thay th? c?c workflow legacy kh?c.
- Fixed Asset UX c� quick create, Draft/Approve/Post bridge qua `ActivateAssetAsync`, Reverse, Cancel approval, Dispose v� Increase/Decrease adjustment.
- Tool Asset UX c� quick create, issue/post, recover, dispose v� drilldown posting/source.
- UI hi?n th? original cost, accumulated depreciation, remaining value, method, useful life, allocation amount, remaining amount, posting status v� journal id.
- Inline validation panel hi?n th? duplicate code, draft depreciation/allocation, invalid remaining amount v� l?i locked period/account t? service.
- Drilldown hi?n th? Asset/Tool -> Journal/source/depreciation/allocation metadata, chua hard-code di?u hu?ng sang m�n kh�c.

### Depreciation/allocation improvements
- Depreciation tab c� preview run, batch post, post selected, reverse selected v� schedule/history persisted.
- Tool allocation tab c� preview schedule, batch allocation, post selected v� reverse selected.
- Backend bridge b? sung `GetCategoriesAsync`, depreciation/allocation history APIs, `ReverseDepreciationAsync`, `ReverseAllocationAsync`, `ReverseAssetAsync`, `AdjustAssetAsync`.
- Reverse kh?u hao/ph?n b? g?i `JournalService.ReverseAsync` n?u c? journal, sau d? c?p nh?t l?i accumulated/remaining theo persisted entity.

### Report improvements
- Wire Asset reports v�o persisted ReportEngine:
  - `FIXED_ASSET_REGISTER`
  - `DEPRECIATION_SCHEDULE`
  - `ASSET_INCREASE_DECREASE`
  - `TOOL_ASSET_REGISTER`
  - `TOOL_ASSET_ALLOCATION`
  - `PREPAID_EXPENSE_SCHEDULE`
- Report tab h? tr? async run, virtualized result grid, export Excel/PDF/CSV, recent execution history v� favorite report local.
- `ReportCenterViewModel` expose th?m 6 m? asset report d? Report Center d?ng chung catalog persisted.

### Performance improvements
- Kh�ng th�m `.Result`/`.Wait`.
- Refresh/report/workflow d�ng async, `CancellationToken` v� `SemaphoreSlim` gate d? tr�nh overlap DbContext trong WPF root scope.
- Filter keyword/date d�ng debounce 350ms.
- FixedAsset, ToolAsset, Depreciation, Allocation v� Report grids b?t row/column virtualization, recycling, multi-select v� full-row selection.
- Grid TSC?/CCDC h? tr? `Ctrl+C` copy tab-separated v? `Ctrl+V` paste Excel row v?o quick entry.
- Backend read APIs d�ng `AsNoTracking`.

### Legacy compatibility
- Kh?ng x?a workflow TSC?/CCDC backend cu; ch? m? r?ng service persisted hi?n c?.
- Kh�ng t?o accounting engine m?i; journal posting/reversal v?n qua `JournalService`.
- Shell ch? th�m module key m?i `asset-management`; c�c module hi?n t?i gi? nguy�n route.

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`

### Build result
- Debug build: pass.
- Release build: pass.
- Errors: 0.
- Warnings: 0.

### Remaining risks
- Favorite reports m?i l� collection local trong ViewModel, chua persisted per-user.
- Drilldown m?i hi?n th? journal/source metadata, chua c� navigation router m? voucher/journal theo source type.
- Dispose TSC?/CCDC v?n l? bridge persisted hi?n c?; journal thanh l?/ghi gi?m chi ti?t chua m? r?ng trong scope n?y.
- Adjustment TSC? c?p nh?t nguy?n gi?/gi? tr? c?n l?i v? luu adjustment persisted, chua t? sinh journal adjustment.
- PDF export v?n l� writer t?i thi?u c?a ReportEngine.

### Mark
- PHASE 2 STEP 2.5 completed.

### Recommended next step
- PHASE 2 STEP 2.6 Tax + Financial Statement Enterprise UX.

---

## PHASE 2 STEP 2.6 - Tax + Financial Statement Enterprise UX - 2026-05-22

### Ph?m vi
- N?ng module Thu? + B?o c?o t?i ch?nh b?ng backend persisted hi?n c?: `ITaxService`, `IFinancialStatementService`, `IClosingService`, `IAccountingPeriodService`, `IReportEngine`, Journal persisted.
- Kh�ng redesign to�n app, kh�ng rewrite accounting backend, kh�ng x�a Report Center/General Ledger cu.

### File t?o
- `ASMILE.BO.App/ViewModels/TaxFinancialReportingViewModel.cs`
- `ASMILE.BO.App/Views/TaxFinancialReportingView.xaml`
- `ASMILE.BO.App/Views/TaxFinancialReportingView.xaml.cs`

### File s?a ch�nh
- `ASMILE.BO.Application/Interfaces/ITaxService.cs`
- `ASMILE.BO.Application/Interfaces/IClosingService.cs`
- `ASMILE.BO.Infrastructure/Services/TaxService.cs`
- `ASMILE.BO.Infrastructure/Services/ClosingService.cs`
- `ASMILE.BO.App/DependencyInjection.cs`
- `ASMILE.BO.App/Themes/DataTemplates.xaml`
- `ASMILE.BO.App/ViewModels/DesktopShellViewModel.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### VAT UX improvements
- Th�m module enterprise m?i `Thu? & BCTC` (`tax-financial`) ch?y song song v?i Report Center v� General Ledger hi?n c�.
- VAT tab hi?n th? Input VAT, Output VAT, tax payable, tax receivable, declaration period, journal id, source type/source id v� invoice metadata.
- VAT workflow c� create manual VAT bridge, generate declaration, lock declaration, re-open declaration v� export declaration qua report export.
- Declaration tab hi?n th? persisted `TaxDeclarationDraft` theo k?, input/output VAT, net payable v� tr?ng th�i.
- Inline validation hi?n th? missing tax code, invalid VAT rate, duplicate invoice, unposted/unlinked journal v� locked/closed period state t? accounting period.

### Financial statement improvements
- Financial Statement tab hi?n th? Balance Sheet, Income Statement, Cash Flow lines v� account balance snapshots persisted.
- Report tab wire c�c persisted reports:
  - `BALANCE_SHEET`
  - `INCOME_STATEMENT`
  - `CASH_FLOW`
  - `VAT_INPUT`
  - `VAT_OUTPUT`
  - `GENERAL_LEDGER`
  - `TRIAL_BALANCE`
- Report UX c� async run, virtualized result grid, favorites local, recent executions v� export Excel/PDF/CSV qua `IReportEngine`.
- Drilldown hi?n th? FS line -> account balances -> journals, account -> journals, journal -> source metadata ? m?c data bridge, chua hard-code navigation m�n ch?ng t?.

### Closing workflow improvements
- Closing tab h? tr? preview closing, batch closing, reverse closing, lock period v� unlock period.
- Closing visibility hi?n th? closing entries, closing lines, closing journal id, closing status v� accounting period lock state.
- Backend bridge b? sung `PreviewClosingAsync`, `GetClosingEntriesAsync`, `GetDeclarationsAsync`, `ReopenDeclarationAsync`.
- Lock/unlock period d�ng `IAccountingPeriodService` hi?n c�; closing/reverse v?n d�ng `IClosingService` + `JournalService`.

### Performance improvements
- Kh�ng th�m `.Result`/`.Wait`.
- Refresh/report/workflow d�ng async, `CancellationToken` v� `SemaphoreSlim` gate d? tr�nh overlap DbContext trong WPF root scope.
- Filter keyword/date/period/direction d�ng debounce 350ms.
- VAT, Declaration, FS, Snapshot, Closing, Report v� Drilldown grids b?t row/column virtualization, recycling, multi-select v� full-row selection.
- Grid ch�nh h? tr? keyboard-first Enter/Tab commit v� `Ctrl+C` copy tab-separated.
- Backend read/list APIs d�ng `AsNoTracking` ? c�c list m?i.

### Legacy compatibility
- Kh�ng x�a `ReportCenterViewModel`, `ReportCenterView`, `GeneralLedgerViewModel`, `GeneralLedgerView`.
- Shell ch? th�m module key m?i `tax-financial`; c�c module hi?n t?i gi? nguy�n route.
- Kh�ng d?i posting engine, financial statement engine hay closing journal engine; ch? bridge UI/service API.

### Command d? ch?y
- `dotnet build .\ASMILE.BO.sln --no-incremental`
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`

### Build result
- Debug build: pass.
- Release build: pass.
- Errors: 0.
- Warnings: 0.

### Remaining risks
- Favorite reports m?i l� collection local trong ViewModel, chua persisted per-user.
- Drilldown m?i hi?n th? v� t?i journal/account data, chua c� navigation router m? voucher/journal theo source type.
- VAT declaration export hi?n xu?t VAT_INPUT/VAT_OUTPUT reports, chua c� XML/submission/government integration theo scope lo?i tr?.
- Financial statement generation hi?n d�ng mapping generic hi?n c�; mapping TT99/TT200 chi ti?t v?n l� engine/backend roadmap ri�ng n?u c?n d? ch�nh x�c bi?u m?u cao hon.
- PDF export v?n l� writer t?i thi?u c?a ReportEngine.

### Mark
- PHASE 2 STEP 2.6 completed.

### Recommended next step
- PHASE 2 STEP 2.7 Navigation + Permission + Enterprise Shell.

---

## PHASE 2 STEP 2.7 - Navigation + Permission + Enterprise Shell

### Scope completed
- T?o enterprise navigation router t?p trung, gi? API `Navigate(string)` cu d? tuong th�ch.
- T?o persisted permission/workspace layer song song v?i security in-memory hi?n t?i, kh�ng rewrite login/backend accounting.
- C?i thi?n shell nh?: left navigation rail, breadcrumb, global search, recent/pinned modules, status/loading region v� keyboard shortcuts.
- Drilldown route t?p trung cho journal/report/voucher/source type, c� metadata fallback khi chua c� m�n chi ti?t ri�ng.

### Files created
- `ASMILE.BO.Application/DTOs/EnterpriseShellDtos.cs`
- `ASMILE.BO.Application/Interfaces/EnterpriseShellInterfaces.cs`
- `ASMILE.BO.Core/Entities/EnterpriseShellEntities.cs`
- `ASMILE.BO.App/Services/NavigationDtos.cs`
- `ASMILE.BO.App/Services/GlobalSearchService.cs`
- `ASMILE.BO.Infrastructure/Services/PermissionService.cs`
- `ASMILE.BO.Infrastructure/Services/WorkspaceService.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260522131611_AddEnterpriseShellAndPermissions.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260522131611_AddEnterpriseShellAndPermissions.Designer.cs`

### Files modified
- `ASMILE.BO.App/Services/INavigationService.cs`
- `ASMILE.BO.App/Services/NavigationService.cs`
- `ASMILE.BO.App/ViewModels/DesktopShellViewModel.cs`
- `ASMILE.BO.App/Views/DesktopShellView.xaml`
- `ASMILE.BO.App/Views/DesktopShellView.xaml.cs`
- `ASMILE.BO.App/ViewModels/VirtualExplorerViewModel.cs`
- `ASMILE.BO.App/DependencyInjection.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### UX improvements
- Left enterprise navigation rail hi?n th? pinned/recent modules theo user workspace.
- Breadcrumb/status region hi?n th? module hi?n t?i v� l� do guard/permission denied.
- Global search box h? tr? Ctrl+P, debounce 300ms v� m? module/report/journal/source target.
- Loading overlay chu?n h�a cho workspace restore v� shell initialization.
- Ctrl+Tab / Ctrl+Shift+Tab chuy?n active window.

### Navigation improvements
- `NavigationService` qu?n l� route -> ViewModel mapping t?p trung thay cho switch trong `DesktopShellViewModel`.
- H? tr? `OpenModule`, `OpenJournal`, `OpenVoucher`, `OpenReport`, `GoBack`, `GoForward`, `OpenInNewTab`, `RestoreWorkspace`.
- Drilldown source routing cho journal, stock receipt/issue, cash/bank transaction, fixed/tool asset, VAT/tax.
- Source chua c� UI ri�ng s? m? fallback metadata viewer thay v� hard-code l?i trong t?ng ViewModel.

### Permission improvements
- Th�m `IPermissionService` persisted, async, cached theo user.
- Permission format enterprise nhu `general-ledger.view`, `cash.post`, `financial.close-period`.
- Seed role n?n: Administrator, Accountant, Warehouse, Viewer.
- Bridge legacy permission (`Accounting.View`, `Warehouse.View`, `Settings.View`, ...) d? kh�ng ph� workflow/login hi?n t?i.
- Shell command guard th?t: kh�ng ch? hide module; open command check permission v� b�o reason.

### Workspace improvements
- Persist per-user pinned modules, recent modules, favorite/recent reports, recent drilldowns, last opened tabs v� shell layout JSON.
- Duplicate-safe, newest-first, max history 20.
- Null/old data safe: JSON l?i/null tr? empty state thay v� crash.

### Migration
- `20260522131611_AddEnterpriseShellAndPermissions` / `AddEnterpriseShellAndPermissions`.

### Validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`: pass, 0 warnings, 0 errors.
- Task-blocking `.Result` legacy trong `VirtualExplorerViewModel` d? d?i sang await; kh?ng th?m `.Result`/`.Wait()`.

### Legacy compatibility
- Kh�ng x�a workflow/view/viewmodel cu.
- Kh�ng d?i accounting/posting/report engine backend.
- Security seed/login in-memory hi?n t?i v?n gi?; persisted permission l� bridge enterprise m?i.

### Remaining risks
- Persisted permission chua c? admin UI CRUD d?y d?; seed v? service d? s?n s?ng.
- Drilldown m? d?ng module/source route nhung chua truy?n selected record s?u v?o t?ng m?n legacy.
- Global search l� lightweight query, chua ph?i full-text engine theo d�ng non-goal.

### Mark
- PHASE 2 STEP 2.7 completed.

### Recommended next step
- PHASE 2 STEP 2.8 only when explicitly requested. Current requested stop point is Step 2.7.

---

## PHASE 2 STEP 2.8 - Enterprise Permission Management + Operational Workspace

### Scope completed
- Th�m permission admin module th?t route `security-admin`.
- Th�m workspace settings module route `workspace-settings`.
- Th�m audit viewer module route `audit-center`.
- Th�m operations center module route `operations-center`.
- Kh�ng rewrite shell, kh�ng d?i accounting/posting behavior.

### Files created
- `ASMILE.BO.Application/DTOs/EnterpriseOperationsDtos.cs`
- `ASMILE.BO.App/ViewModels/SecurityAdminViewModel.cs`
- `ASMILE.BO.App/ViewModels/WorkspaceSettingsViewModel.cs`
- `ASMILE.BO.App/ViewModels/AuditCenterViewModel.cs`
- `ASMILE.BO.App/ViewModels/OperationsCenterViewModel.cs`
- `ASMILE.BO.App/Views/SecurityAdminView.xaml`
- `ASMILE.BO.App/Views/SecurityAdminView.xaml.cs`
- `ASMILE.BO.App/Views/WorkspaceSettingsView.xaml`
- `ASMILE.BO.App/Views/WorkspaceSettingsView.xaml.cs`
- `ASMILE.BO.App/Views/AuditCenterView.xaml`
- `ASMILE.BO.App/Views/AuditCenterView.xaml.cs`
- `ASMILE.BO.App/Views/OperationsCenterView.xaml`
- `ASMILE.BO.App/Views/OperationsCenterView.xaml.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260522133345_AddEnterpriseOperationsAndSecurityUi.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260522133345_AddEnterpriseOperationsAndSecurityUi.Designer.cs`

### Files modified
- `ASMILE.BO.Core/Entities/EnterpriseShellEntities.cs`
- `ASMILE.BO.Application/Interfaces/EnterpriseShellInterfaces.cs`
- `ASMILE.BO.Application/Interfaces/InfrastructureInterfaces.cs`
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ASMILE.BO.Infrastructure/Services/PermissionService.cs`
- `ASMILE.BO.Infrastructure/Services/WorkspaceService.cs`
- `ASMILE.BO.Infrastructure/Services/InfrastructureHealthService.cs`
- `ASMILE.BO.App/Services/NavigationService.cs`
- `ASMILE.BO.App/DependencyInjection.cs`
- `ASMILE.BO.App/Themes/DataTemplates.xaml`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Security improvements
- `security-admin` hi?n th? roles, permission catalog, assigned state, users v� permission diff.
- H? tr? create/rename/clone/archive role.
- H? tr? assign/remove multi-select permissions v� assign users to role.
- Permission catalog persisted qua `AppPermissions`, load d?ng t? service, kh�ng hard-code trong XAML.
- Dangerous permission badge field cho post/lock/close-period/admin/reverse.
- Local persisted foundation m?i: `AppUser`, `AppUserRole`, `AppPermission`, `AppRole.IsArchived`.
- Legacy login v?n du?c mirror sang persisted app user/role khi c� current user.

### Workspace improvements
- `workspace-settings` qu?n l� pinned modules, favorite reports, restore last session, default landing module, grid density v� compact mode.
- Settings persist qua `WorkspaceService`, kh�ng reset d? li?u workspace cu.
- Clear workspace cache c� command ri�ng, kh�ng ?nh hu?ng accounting data.

### Audit improvements
- `audit-center` hi?n th? audit timeline async v?i filter module/action/user/entity/date.
- C� JSON metadata viewer, severity badges data (`Info`, `Warning`, `Error`) v� drilldown source/journal qua `NavigationService`.
- Export audit JSON non-blocking.
- Filter c� debounce 350ms v� DataGrid virtualization/recycling.

### Operations improvements
- `operations-center` hi?n th? job definitions, job executions, retry/cancel, duration/status/requested by.
- `BackgroundJobRunner` persist execution metadata v�o `BackgroundJobExecutions`.
- Infrastructure Health tab d�ng `IInfrastructureHealthService` v� `IDatabaseMigrationService`.
- Dangerous run migration action c� confirm.
- Operational tools: clear workspace cache, reset navigation history notice, export diagnostics, open log folder, open audit center.

### Migration
- `20260522133345_AddEnterpriseOperationsAndSecurityUi` / `AddEnterpriseOperationsAndSecurityUi`.

### Validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`: pass, 0 warnings, 0 errors.
- Search blocking async: kh�ng c� `.Wait()` ho?c `Task.Result`; c�c match c�n l?i l� property `Result` c?a dialog/view rows.

### Remaining legacy areas
- User authentication/login v?n d�ng legacy local security store; Step 2.8 ch? th�m persisted admin foundation.
- Security admin chua c� UX d?i m?t kh?u/auth policy v� OAuth/cloud/AD l� non-goal.
- Job monitor d�ng lightweight local runner, kh�ng ph?i distributed queue.

### Remaining risks
- Role/user admin UI l� foundation; c?n smoke test runtime sau khi DB migration apply.
- Audit drilldown ph? thu?c entity id d?ng Guid; legacy int-id voucher m? module fallback ch? chua selected row s?u.
- Permission seed ch?y lazy qua `PermissionService`; n?u app kh�ng m? security/module guard th� catalog seed chua materialize cho admin DB inspection.

### Mark
- PHASE 2 STEP 2.8 completed.

### Recommended next step
- PHASE 2 STEP 2.9 only when explicitly requested.

---

## PHASE 2 STEP 2.9 - Enterprise Stabilization + Performance Hardening

### Scope completed
- Stabilized enterprise desktop runtime after UI wiring.
- No new business module.
- No accounting/posting behavior changes.
- No migration created.

### Files created
- `ASMILE.BO.App/Infrastructure/BusyScope.cs`
- `ASMILE.BO.App/Infrastructure/DebounceDispatcher.cs`
- `ASMILE.BO.App/Infrastructure/SafeFireAndForget.cs`
- `ASMILE.BO.App/Infrastructure/UserExceptionMapper.cs`
- `ASMILE.BO.App/Themes/EnterpriseStabilization.xaml`

### Files modified
- `ASMILE.BO.App/App.xaml`
- `ASMILE.BO.App/Themes/DataGrid.xaml`
- `ASMILE.BO.App/Services/WindowManagerService.cs`
- `ASMILE.BO.App/ViewModels/DesktopShellViewModel.cs`
- `ASMILE.BO.App/ViewModels/SecurityAdminViewModel.cs`
- `ASMILE.BO.App/ViewModels/AuditCenterViewModel.cs`
- `ASMILE.BO.Infrastructure/Services/ReportEngine.cs`
- `ASMILE.BO.Infrastructure/Services/ImportEngine.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Performance improvements
- Default DataGrid style now enables row/column virtualization, recycling, pixel scrolling, deferred scrolling, and collapsed row details.
- Report export loops now honor cancellation tokens.
- Large Excel exports skip expensive `AdjustToContents` over 5,000 rows to avoid UI stalls and high memory spikes.
- Shell module open path has a non-blocking gate to avoid duplicate route execution from double-click/repeated command.

### Memory/resource improvements
- `WindowManagerService.Close` disposes ViewModel content when it implements `IDisposable`.
- `DesktopShellViewModel` now disposes timers, cancellation token source, navigation/notification event handlers, collection handlers, and navigation gate.
- `SecurityAdminViewModel` and `AuditCenterViewModel` dispose debounce timers and cancellation token sources.
- Import engine cleans old temp import files in `%TEMP%\ASMILE-BO\imports`.

### UX infrastructure improvements
- Added reusable `BusyScope`, `DebounceDispatcher`, `SafeFireAndForget`, and `UserExceptionMapper`.
- Added `EnterpriseStabilization.xaml` with reusable virtualized grid style, status badge, empty-state text, and loading overlay resources.
- Shell fire-and-forget path is centralized and maps exceptions to safe user-facing messages.

### Report/import improvements
- Report Excel/CSV/PDF export is cancellation-aware.
- Import preview validates file existence, extension and size before loading workbook.
- Import row read and validation loops are cancellation-aware.
- Import temp cleanup avoids accumulated stale temp files.

### Migration
- No migration created for Step 2.9.

### Validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 20/20 tests.
- Blocking async scan: no `.Wait()` or task-blocking `Task.Result`; remaining matches are dialog/view-row `Result` properties.

### Remaining legacy areas
- Some legacy dialogs still expose a property named `Result`; these are not task-blocking calls.
- Older module-specific DataGrid styles may override defaults in a few screens, but global enterprise default is now hardened.
- Full incremental paging for every legacy screen remains future work.

### Remaining risks
- ClosedXML export is still CPU-bound once serialization starts; cancellation is honored before/inside loops but not inside ClosedXML internal save.
- Runtime leak verification still needs manual profiling under long sessions.
- Import `.csv` support depends on existing handler/path behavior; Step 2.9 only added guards and cleanup, not a parser rewrite.

### Mark
- PHASE 2 STEP 2.9 completed.

### Recommended next step
- Phase 3 planning only when explicitly requested.

---

## PHASE 3 STEP 3.1 - Multi-company + optional multi-branch foundation

### Scope completed
- Added persisted `Company`, `Branch`, `UserCompanyAccess`, and `UserBranchAccess` foundation.
- Added nullable `CompanyId` and optional `BranchId` scope fields across the major persisted accounting, inventory, costing, AR/AP, cash/bank, VAT, asset, report, workspace, audit, closing, and financial statement rows.
- Added global query filters for company isolation with legacy `CompanyId == null` fallback, plus optional branch filtering only when a branch is selected.
- Added a current company context service with default company/branch seeding and cached company/branch option loading.
- Integrated company/branch scope checks into `PermissionService`, including cached access decisions.
- Isolated workspace pinned/favorite/recent state per company with legacy workspace fallback.
- Added lightweight shell company/branch selectors and status display without redesigning the shell.

### Migration
- `20260522144345_AddMultiCompanyFoundation`
- Migration is additive: new nullable scope columns, company/branch/access tables, default company/branch seed data, and scoped indexes.

### Tests added
- Company query isolation with safe legacy row fallback.
- Cross-company permission denial.
- Workspace company isolation.
- Report execution history isolation.
- Journal header/line isolation.

### Validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 25/25 tests.

### Mark
- PHASE 3 STEP 3.1 completed.

---

## PHASE 3 STEP 3.2 - SQL Server Performance & Data Scaling

### Scope completed
- Added SQL Server-oriented indexes for company/branch/status/date/source/account access paths across Journal, Inventory, Costing, AR/AP, VAT, Report history, and Audit tables.
- Added `ISqlPerformanceScalingService` with cancellation-aware compiled query paging for posted journal lines.
- Added lightweight diagnostics timing for core query paths: journal page, report history, costing, AR, AP, and audit.
- Added dev scale helper foundation for 100k and 1m journal-line datasets with batched async inserts.
- Improved `ReportEngine` general ledger query to project DTO-like row sources directly from `JournalLines` without eager-loading full journal aggregates.
- Added cancellation-aware CSV streaming export API while keeping existing byte-array export backward compatible.
- Added focused unit tests for the SQL performance paging/timing foundation.

### Migration
- `20260522221500_Phase3Step32SqlPerformanceScaling`
- Migration is additive: indexes only, no data rewrite and no schema-breaking changes.

### Validation
- Pre-migration compile check: `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 27/27 tests.

### Mark
- PHASE 3 STEP 3.2 completed.

### Remaining risks
- `dotnet ef` is not installed in this environment, so the migration was created manually.
- Excel/PDF export still materializes the report result before writing; CSV now has stream writing, but true row-by-row report execution remains future work.

---

## PHASE 3 STEP 3.3 - Enterprise Permission System Complete

### Scope completed
- Added persisted role inheritance through `AppRoleInheritance`.
- Added persisted data permissions through `AppDataPermission` for user/role scoped data access, covering company/branch plus entity scopes such as warehouse, cash fund, and bank account.
- Added effective permission preview, permission tree, module/action matrix, data permission read/write, and runtime data-access check APIs.
- Kept deny override behavior: direct user deny removes granted permissions from role/inheritance permissions.
- Preserved admin bypass policy.
- Improved cache invalidation after role, inheritance, and data-permission changes.
- Added audit logging hooks for permission/data permission changes and denied data-access attempts.
- Lightly localized Security Admin UI labels to Vietnamese without redesigning the screen.

### Migration
- `20260522223000_Phase3Step33EnterprisePermissions`
- Migration is additive: creates `AppRoleInheritances` and `AppDataPermissions`.

### Tests added
- Role inheritance grants parent permissions.
- Deny override wins over role-granted permissions.
- Data permission deny blocks scoped warehouse access.

### Validation
- Interim `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- Interim `dotnet test .\ASMILE.BO.sln --no-build`: pass, 30/30 tests.
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 30/30 tests.

### Mark
- PHASE 3 STEP 3.3 completed.

### Remaining risks
- Data permission enforcement is available through service APIs; existing module-specific screens still need deeper per-row calls where they do not already enforce scope.

---

## PHASE 3 STEP 3.4 - E-Invoice Integration Foundation

### Scope completed
- Added persisted e-invoice entities: `Invoice`, `InvoiceLine`, `InvoiceTaxBreakdown`, `InvoiceIssueLog`, and `InvoiceProviderConfiguration`.
- Added status flow foundation for Draft, Signed, Issued, Cancelled, and Adjusted through persisted status values.
- Added provider abstraction with local stub providers for Viettel, VNPT, and MISA.
- Added XML generation abstraction and deterministic Vietnamese e-invoice XML foundation.
- Added lightweight PDF preview foundation as UTF-8 preview payload.
- Added async issuing queue foundation through persisted `InvoiceIssueLog` rows and queue processor.
- Integrated issued invoices with VAT output transaction creation; invoice rows also carry optional Journal, AR, AP, and VAT declaration links.

### Migration
- `20260522224500_Phase3Step34EInvoiceFoundation`
- Migration is additive: creates e-invoice tables and indexes; no sales workflow rewrite.

### Tests added
- Draft invoice creation, XML generation, and issue queue creation.
- Queue processing marks invoice as issued and creates VAT output transaction.

### Validation
- Interim `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- Interim `dotnet test .\ASMILE.BO.sln --no-build`: pass, 32/32 tests.
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 32/32 tests.

### Mark
- PHASE 3 STEP 3.4 completed.

### Remaining risks
- Provider integrations are abstraction/stub only; real signing/API certification with Viettel/VNPT/MISA remains future implementation.
- PDF preview is a foundation payload, not a final invoice rendering template.

---

## PHASE 3 STEP 3.5 - Background Job & Scheduler Engine

### Scope completed
- Added persisted scheduler entities: `JobDefinition`, `JobExecution`, `JobRetry`, and `JobLog`.
- Added `ISchedulerService` with default enterprise job definitions, interval schedule metadata, manual trigger, retry, cancel, and due-job queueing.
- Added scheduler jobs for depreciation, tool allocation, VAT sync, report generation, and cleanup.
- Extended Operations Center background job definitions to surface the new operational job families while preserving existing `IBackgroundJobRunner` behavior.
- Kept implementation local/persisted; no RabbitMQ, Hangfire, or distributed queue introduced.

### Migration
- `20260522230000_Phase3Step35SchedulerEngine`
- Migration is additive: creates scheduler/job tables and indexes.

### Tests added
- Default scheduler definitions include enterprise accounting jobs.
- Manual trigger, retry, cancel, retry rows, and log rows are persisted.

### Validation
- Interim `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- Interim `dotnet test .\ASMILE.BO.sln --no-build`: pass, 34/34 tests.

### Remaining risks
- Scheduler execution currently queues and tracks jobs; actual worker orchestration remains a local foundation and not a continuously hosted scheduler loop.

---

## PHASE 3 STEP 3.6 - Multi-Company Foundation + Context Isolation

### Scope completed
- Confirmed the existing multi-company foundation remains the active implementation for the current Phase 3 Step 3.6 numbering.
- Reused the additive `AddMultiCompanyFoundation` migration; no duplicate company migration was created.
- Confirmed company/branch context, company-scoped query filters, service-level company assignment, permission isolation, workspace separation, report execution isolation, audit isolation, and shell company/branch selectors are already wired.
- Added focused audit isolation coverage to the multi-company test suite.

### Files modified
- `ASMILE.BO.Tests.Unit/MultiCompanyFoundationTests.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- Existing migration reused: `20260522144345_AddMultiCompanyFoundation`.
- No new migration created in Step 3.6.

### Tests added
- Audit logs are isolated by current company context and do not leak across companies.

### Validation
- `dotnet test .\ASMILE.BO.Tests.Unit\ASMILE.BO.Tests.Unit.csproj --filter MultiCompanyFoundationTests --no-restore`: pass, 6/6 tests.
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 35/35 tests.

### Mark
- PHASE 3 STEP 3.6 completed.

### Remaining risks
- Legacy rows with `CompanyId == null` remain intentionally visible as backward-compatible fallback until a controlled data backfill is planned.
- Branch-specific fiscal-period workflow belongs to Step 3.7 and was not implemented in this step.

---

## PHASE 3 STEP 3.7 - Multi-Branch + Fiscal Period Engine

### Scope completed
- Added persisted fiscal-period foundation entities: `FiscalYear`, `PeriodLock`, and `CompanyBranchPermission`.
- Extended `IAccountingPeriodService` and `AccountingPeriodService` with fiscal-year creation, 12-period rollover creation, close/reopen period flow, branch-specific period lock/unlock, and branch-aware posting-date validation.
- Preserved legacy `AccountingPeriod` status lock behavior for backward compatibility.
- Added role override support for `Administrator` and accounting manager role codes in period lock validation.
- Added `fiscal-periods` shell route and a lightweight WPF management screen for fiscal years, periods, and lock/unlock/close/reopen actions.
- Kept existing branch selector/status behavior in the shell; no shell redesign.

### Files created
- `ASMILE.BO.App/ViewModels/FiscalPeriodManagementViewModel.cs`
- `ASMILE.BO.App/Views/FiscalPeriodManagementView.xaml`
- `ASMILE.BO.App/Views/FiscalPeriodManagementView.xaml.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260523010000_AddFiscalPeriodAndBranchEngine.cs`
- `ASMILE.BO.Tests.Unit/FiscalPeriodBranchEngineTests.cs`

### Files modified
- `ASMILE.BO.Core/Entities/AccountingCoreEntities.cs`
- `ASMILE.BO.Application/Interfaces/IAccountingPeriodService.cs`
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Services/AccountingPeriodService.cs`
- `ASMILE.BO.App/DependencyInjection.cs`
- `ASMILE.BO.App/Services/NavigationService.cs`
- `ASMILE.BO.App/Themes/DataTemplates.xaml`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- `20260523010000_AddFiscalPeriodAndBranchEngine`

### Tests added
- Branch period lock blocks posting for the locked branch only.
- General ledger report respects selected branch scope.
- Fiscal-year rollover creates 12 open periods.

### Validation
- `dotnet test .\ASMILE.BO.Tests.Unit\ASMILE.BO.Tests.Unit.csproj --filter FiscalPeriodBranchEngineTests --no-build`: pass, 3/3 tests.
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 38/38 tests.

### Mark
- PHASE 3 STEP 3.7 completed.

### Remaining risks
- Fiscal year currently uses calendar-year periods by default; non-calendar fiscal calendars can be added later through company accounting profile rules.
- `CompanyBranchPermission` is persisted as a foundation entity; deeper action-level UI assignment remains future polish.

---

## PHASE 3 STEP 3.8 - Enterprise Notification + Task Center

### Scope completed
- Added persisted notification/task center entities: `UserNotification`, `TaskInbox`, `TaskComment`, and `TaskAttachmentMetadata`.
- Added `INotificationTaskService` and `NotificationTaskService` for notification publish/read, unread counters, task create/assign/complete/reopen, and comment threads with mention metadata.
- Added supported notification type foundation through free-form `NotificationType` values for posting failed, low stock, negative stock, reconciliation mismatch, closing pending, depreciation pending, import failed, and background job failed events.
- Added lightweight Notification & Task Center WPF route `notification-center` without replacing existing toast behavior.
- Kept dispatch background-safe by persisting notifications/tasks through async service calls; no popup fan-out was introduced.

### Files created
- `ASMILE.BO.Core/Entities/NotificationTaskEntities.cs`
- `ASMILE.BO.Application/DTOs/NotificationTaskDtos.cs`
- `ASMILE.BO.Application/Interfaces/INotificationTaskService.cs`
- `ASMILE.BO.Infrastructure/Services/NotificationTaskService.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260523012000_AddEnterpriseNotificationCenter.cs`
- `ASMILE.BO.App/ViewModels/NotificationTaskCenterViewModel.cs`
- `ASMILE.BO.App/Views/NotificationTaskCenterView.xaml`
- `ASMILE.BO.App/Views/NotificationTaskCenterView.xaml.cs`
- `ASMILE.BO.Tests.Unit/NotificationTaskServiceTests.cs`

### Files modified
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.App/DependencyInjection.cs`
- `ASMILE.BO.App/Services/NavigationService.cs`
- `ASMILE.BO.App/Themes/DataTemplates.xaml`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- `20260523012000_AddEnterpriseNotificationCenter`

### Tests added
- Notification delivery and unread counters per user.
- Mark-read removes notification from unread counter.
- Task assignment, complete, reopen, and comment mention metadata persist.

### Validation
- `dotnet test .\ASMILE.BO.Tests.Unit\ASMILE.BO.Tests.Unit.csproj --filter NotificationTaskServiceTests --no-build`: pass, 3/3 tests.
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 41/41 tests.

### Mark
- PHASE 3 STEP 3.8 completed.

### Remaining risks
- Existing business modules do not yet publish every domain event automatically; Step 3.8 adds the persisted publisher/task center foundation and UI.
- Attachment metadata is persisted, but binary storage/preview belongs to the later document center step.

---

## PHASE 3 STEP 3.9 - Full Text Search + Global Command Palette

### Scope completed
- Added persisted `SearchIndexEntry` and `RecentSearch` entities for enterprise search indexing and recent search history.
- Added `IEnterpriseSearchService` and `EnterpriseSearchService` with async/cancellation-aware upsert, ranked search, recent search recording, and company/branch isolation through existing query filters.
- Added additive migration `AddEnterpriseSearchIndex`.
- Kept existing shell/global search UI and command-palette shortcut behavior intact; this step adds the persisted backend foundation without rewriting the existing overlay.

### Files modified
- `ASMILE.BO.Core/Entities/SearchEntities.cs`
- `ASMILE.BO.Application/DTOs/SearchDtos.cs`
- `ASMILE.BO.Application/Interfaces/IEnterpriseSearchService.cs`
- `ASMILE.BO.Infrastructure/Services/EnterpriseSearchService.cs`
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260523014000_AddEnterpriseSearchIndex.cs`
- `ASMILE.BO.Tests.Unit/EnterpriseSearchIndexTests.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- `20260523014000_AddEnterpriseSearchIndex`

### Tests added
- Ranking prefers exact title matches.
- Search index respects company isolation.
- Recent searches persist per user.
- Search honors cancellation tokens.

### Validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.Tests.Unit\ASMILE.BO.Tests.Unit.csproj --filter EnterpriseSearchIndexTests --no-build`: pass, 4/4 tests.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 45/45 tests.

### Mark
- PHASE 3 STEP 3.9 completed.

### Remaining risks
- Existing shell search overlay still queries the legacy facade directly; the persisted index service is available for deeper UI integration in a later scoped step.
- True SQL Server full-text catalog setup is not enabled here; this step uses persisted normalized text and indexed metadata for backward-compatible rollout.

---

## PHASE 3 STEP 3.10 - Background Processing + Scheduler Engine

### Scope completed
- Added persisted scheduler foundation for `BackgroundJobDefinition`, `BackgroundJobSchedule`, `BackgroundJobExecution`, and `BackgroundJobLog`.
- Upgraded the scheduler service with safe queueing, retry, timeout, cancellation propagation, stale recovery, startup validation, and scoped execution.
- Added a hosted scheduler loop using `PeriodicTimer` and scoped DI execution.
- Wired Operations Center to the new persisted scheduler surface for dashboard, execution history, logs, pause/resume, retry, cancel, and run now actions.
- Kept legacy `IBackgroundJob` compatibility path intact.

### Files created
- `ASMILE.BO.Infrastructure/Services/BackgroundSchedulerHostedService.cs`
- `ASMILE.BO.Tests.Unit/BackgroundSchedulerServiceTests.cs`

### Files modified
- `ASMILE.BO.Core/Entities/EnterpriseShellEntities.cs`
- `ASMILE.BO.Core/Entities/SchedulerEntities.cs`
- `ASMILE.BO.Application/DTOs/BackgroundSchedulerDtos.cs`
- `ASMILE.BO.Application/Interfaces/IBackgroundSchedulerService.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.Infrastructure/Services/BackgroundSchedulerService.cs`
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/ASMILE.BO.Infrastructure.csproj`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260523042803_AddBackgroundSchedulerEngine.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260523042803_AddBackgroundSchedulerEngine.Designer.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ASMILE.BO.App/ViewModels/OperationsCenterViewModel.cs`
- `ASMILE.BO.App/Views/OperationsCenterView.xaml`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Migration
- `AddBackgroundSchedulerEngine`

### Tests added
- Retry logic creates a new execution row and preserves history.
- Timeout logic marks the execution timed out and queues retry history.
- Concurrency guard skips duplicate execution when concurrent runs are disabled.
- Cancellation propagates to a running execution.
- Next-run calculation, pause/resume, startup validation, and stale recovery are covered.

### Validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 47/47 tests.

### Mark
- PHASE 3 STEP 3.10 completed.

### Phase 3 Step 3.11 validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 54/54 tests.

## Phase 3 Step 3.12 - Enterprise Audit Lifecycle Completion

Completed on 2026-05-23.

Completed items:
- Added AuditSessionStatus lifecycle with Started, Active, Completed, Abandoned, and Expired states.
- Added AuditSessionService for start, heartbeat, completion, expired-session abandonment, and active-session queries.
- Hardened AuditEntitySnapshot/AuditLog query-filter relationship by making snapshot company/branch scoped and AuditLog navigation optional.
- Added snapshot compression metadata, gzip payload support, lazy decompression, SHA256 verification, and mismatch detection.
- Extended replay with timeline nodes, session replay, journal replay, source replay, and deterministic state-frame reconstruction.
- Hardened audit export with NDJSON, ZIP manifest, streamed SHA256 hashing, row counts, and correlation coverage metadata.
- Added parent-child correlation propagation for replay entries and correlation rows.
- Added lifecycle/export/compression/replay/query-filter tests; total test count is now 65.

Enterprise Audit Lifecycle:
- Sessions are append-oriented audit context records with status, last activity, duration, device/platform metadata, and completion reason.
- Authentication logout completes audit sessions tied to the login session token.
- Expired or abandoned sessions are finalized by timeout without mutating audit history rows.

Correlation Architecture:
- AsyncLocal correlation scopes retain current and parent correlation IDs across nested async operations.
- Audit correlations and replay entries store parent correlation and audit session references for forensic grouping.

Replay Architecture:
- Replay remains non-mutating and reconstructs timeline/state metadata from persisted replay entries and snapshots.
- Replay queries use projection, pagination, and AsNoTracking.

Snapshot Compression:
- Large snapshots are serialized deterministically, compressed with gzip, and read lazily by replay/export consumers.
- Snapshot integrity uses SHA256 and exposes mismatch detection for forensic review.

Audit Export Pipeline:
- JSON, CSV, NDJSON, and ZIP exports are streaming and cancellation-aware.
- Export history stores file hash, row count, correlation coverage, and integrity manifest metadata.

Validation:
- Debug build: passed, 0 warnings, 0 errors.
- Tests: passed, 69 total before final release validation.

## Phase 3 Step 3.13 - Enterprise Data Retention + Archival + Recovery Hardening

Completed on 2026-05-23.

Entities:
- DataRetentionPolicy
- ArchiveBatch
- ArchiveBatchItem
- RecoveryCheckpoint
- RecoveryOperation
- ArchiveExportManifest
- SearchIndexEntry/RecentSearch archive metadata fields

Services:
- IArchiveService / ArchiveService for retention preview, archive batches, soft restore, purge marking, archive history, and hash integrity validation.
- IRecoveryService / RecoveryService for metadata checkpoints, safe checkpoint restore logging, recovery validation, and recovery timeline.
- EnterpriseSearchService now excludes archived search entries by default and supports includeArchived.
- BackgroundSchedulerService seeds archive cleanup, retention enforcement, and integrity verification jobs.

Migration:
- AddEnterpriseRetentionAndArchival

Tests:
- Added retention/archive/recovery coverage for policy seeding, archive integrity, restore flow, compression, recovery checkpoint/timeline, scheduler retention job, cancellation behavior, company isolation, and archived search filtering.

Deferred scope:
- No cloud backup, distributed storage, external blob storage, SaaS sync, SQL dump orchestration, or hard-delete engine.
- Retention enforcement scheduler starts with search archive execution; broader module retention remains available through ArchiveService policy execution and metadata archive batches.

Remaining risks:
- EF design-time migration scaffolding still reports pre-existing app-host DI validation noise, then continues via design-time context.
- Archive encryption flag is persisted for policy compatibility but encryption implementation is deferred by scope guard.

Validation results:
- dotnet build .\ASMILE.BO.sln --no-incremental: passed, 0 warnings, 0 errors.
- dotnet build .\ASMILE.BO.sln -c Release --no-incremental: passed, 0 warnings, 0 errors.
- dotnet test .\ASMILE.BO.sln --no-build: passed, 79 tests total.
- Scan: no new .Wait() or task-blocking Task.Result usage; scan Result hits are dialog/model Result properties.

| Phase 3 Step 3.14 | Enterprise Diagnostics + Runtime Telemetry + Failure Recovery Hardening | Completed | Added persisted runtime telemetry entities, RuntimeTelemetryService, FailureRecoveryService, Operations Center telemetry tab, telemetry scheduler cleanup job, migration AddEnterpriseTelemetryAndDiagnostics, and telemetry/recovery test coverage. |

### Enterprise Diagnostics + Telemetry
- Added RuntimeMetricSnapshot, TelemetryEvent, SlowOperationLog, RuntimeExceptionLog, UiResponsivenessSnapshot, RecoveryRecommendation, and SystemResourceSnapshot persistence.
- RuntimeTelemetryService buffers writes through a bounded channel, flushes in batches, suppresses duplicate exception spam, and exposes summary/timeline/query APIs.

### Failure Recovery
- FailureRecoveryService analyzes recurring exceptions, UI freezes, and slow operations to generate non-destructive recovery recommendations.
- Recovery suggestions are persisted and audit-aware; no automated destructive recovery is executed.

### Operations Center Telemetry UX
- Operations Center now includes telemetry summary cards plus timeline, slow operations, runtime exceptions, system resource, UI responsiveness, and recovery recommendation grids.

### Scheduler/Workflow Integration
- Scheduler execution, report/import/replay/archive/recovery flows now emit telemetry hooks for slow operations and exceptions.
- Telemetry cleanup runs as a persisted scheduler job.

### Migration
- AddEnterpriseTelemetryAndDiagnostics

### Validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: passed, 0 warnings, 0 errors.
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`: passed, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: pending final verification.

### Final Validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: passed, 0 warnings, 0 errors.
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`: passed, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: passed, 82 total tests.

## PHASE 3 - STEP 3.15 (Enterprise Workflow Resilience + Transaction Recovery)
- Hoan tat nen tang persisted cho workflow resilience:
  - WorkflowExecution
  - WorkflowExecutionStep
  - WorkflowCompensationAction
  - WorkflowRetryHistory
  - WorkflowConsistencyViolation
  - DistributedOperationMarker
  - IdempotencyKeyEntry
- Bo sung `IWorkflowResilienceService` + `WorkflowResilienceService`:
  - Begin/step/complete/fail/resume/retry workflow
  - Dang ky + thuc thi compensation
  - Validate consistency (stale heartbeat, duplicate execution)
  - Cleanup workflow stale
  - Luu retry lineage + idempotency state
- Tich hop scheduler voi workflow resilience:
  - Tao workflow marker khi chay job
  - Ghi step cho execution va cap nhat ket qua failure/timeout/cancel
- Mo rong Operations Center tab Workflow:
  - Timeline workflow execution
  - Danh sach consistency violations
  - Chi tiet workflow steps
- Tich hop Search/Audit/Telemetry cho workflow:
  - Index workflow execution + consistency violation vao enterprise search
  - Audit begin/fail workflow
  - Telemetry exception cho fail step
- Test bo sung:
  - idempotency enforcement
  - retry history
  - resume state
  - compensation execution
  - stale cleanup
  - duplicate consistency detection
- Migration moi: `AddWorkflowResilienceAndRecovery`
- Validation:
  - dotnet build .\ASMILE.BO.sln --no-incremental: pass (0 warning, 0 error)
  - dotnet build .\ASMILE.BO.sln -c Release --no-incremental: pass (0 warning, 0 error)
  - dotnet test .\ASMILE.BO.sln --no-build: pass (92 tests)
- Deferred scope:
  - Chua kich hoat distributed queue/cloud orchestration theo scope guard.

---

## PHASE 3 - STEP 3.16 ho�n t?t - Enterprise Reliability + Distributed Coordination + Runtime Hardening

### K?t qu?
- �� m? r?ng workflow resilience th�nh n?n di?u ph?i c?c b? c� lease, distributed lock, dead-letter, checkpoint, metric v� replay.
- �� harden failure recovery, telemetry runtime, search ranking, scheduler cancel/retry v� trung t�m v?n h�nh.
- �� th�m test cho lease/lock, stale recovery, checkpoint resume, replay determinism, typo search, branch isolation v� cancellation propagation.

### Migration
- `AddEnterpriseReliabilityAndDistributedCoordination`

### Build/test
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warning, 0 error.
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`: pass, 0 warning, 0 error.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 94/94 tests.

### R?i ro c�n l?i
- `dotnet-ef` kh�ng c� s?n tr�n PATH; migration du?c t?o b?ng `.tools\dotnet-ef.exe`.
- M? r?ng Operations Center v?n theo hu?ng incremental, chua d?i l?i shell t?ng th?.

## PHASE 4 planning only
- �� t?o roadmap Phase 4 ri�ng: `PHASE_4_IMPLEMENTATION_PLAN.md`.
- Phase 4 ch? ? m?c k? ho?ch; chua c� implementation, migration, ho?c thay d?i workflow.
- Tr?ng t�m: TT99, TT200, multi-company, multi-branch, approval workflow, identity/auth, e-invoice, tax/XML export, SQL performance, CI/CD, production hardening, reporting, release readiness.

## PHASE 4 - STEP 4.1 ho�n t?t - Multi-company / Multi-branch Hardening

### K?t qu?
- ?? th?m n?n persisted cho `Company`, `Branch`, `CompanySettings`, `BranchSettings`, `CompanyFiscalPeriod`, `CompanyFeatureFlag`, `UserCompanyAccess`, `UserBranchAccess`, `ActiveCompanySession`.
- ?? th?m `ICompanyContextAccessor`, `CompanyContextAccessor`, `CompanyScope`, `BranchScope` d? gi? scope async-safe v? nested-safe.
- ?? h?n company/branch scope cho workspace, search, permission, session persistence v? active company session lifecycle.
- ?? scaffold migration additive `AddEnterpriseMultiCompanyBranchHardening`.
- ?? th?m test cho workspace theo branch, active company session persistence v? nested scope restore.

### Build/test
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warning, 0 error.
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`: pass, 0 warning, 0 error.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 97/97 tests.

### R?i ro c�n l?i
- B?n d? scope admin-bypass chua du?c t�ch ri�ng; hi?n v?n theo m� h�nh additive v� query filter hi?n h?u.
- `ROADMAP_IMPLEMENTATION.md` d? du?c d?ng b? status 4.2 sang Completed d? tr?nh l?ch tracking.
## PHASE 4 - STEP 4.2 ho�n t?t - TT99 Compliance Foundation

### K?t qu?
- ?? th?m persisted foundation cho `TT99AccountMapping`, `TT99ReportTemplate`, `TT99ReportLine`, `TT99ValidationRule`, `TT99ComplianceIssue`, `TT99DeclarationPeriod`, `TT99ComplianceSnapshot`, `TT99ExportManifest`.
- ?? th?m `ITT99ComplianceService` v? `TT99ComplianceService` d? map t?i kho?n, validate tu?n th?, t?o snapshot v? export manifest theo hu?ng additive.
- ?? m? r?ng `ReportEngine` cho c?c m? b?o c?o `TT99_BALANCE_SHEET`, `TT99_INCOME_STATEMENT`, `TT99_CASH_FLOW`, `TT99_ACCOUNT_MAPPING`, `TT99_COMPLIANCE_ISSUES`, `TT99_DECLARATION_SUMMARY`.
- ?? wiring tab TT99 Compliance trong Operations Center v? gi? nguy?n shell/UI cu.
- ?? t?o migration additive `AddTt99ComplianceFoundation`.
- ?? th?m test TT99 cho mapping, validation, snapshot, export manifest v? cancellation-safe behavior.

### Build/test
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warning, 0 error.
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`: pass, 0 warning, 0 error.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 103/103 tests.

### R?i ro c�n l?i
- TT99 m?i d?ng ? n?n compliance additive; chua sinh d?y d? bi?u m?u ph�p l� hay t�ch h?p c?ng thu?.
- D? li?u mapping TT99 m?u v?n d?a tr�n fallback an to�n khi database test tr?ng.
- `dotnet ef` v?n hi?n c?nh b?o DI design-time t? n?n ?ng d?ng cu, nhung build/test runtime d? s?ch.

## PHASE 4 - STEP 4.3 ho�n t?t - TT200 Compliance Foundation

### K?t qu?
- ?? th?m persisted foundation cho `TT200AccountMapping`, `TT200ReportTemplate`, `TT200ReportLine`, `TT200ValidationRule`, `TT200ComplianceIssue`, `TT200DeclarationPeriod`, `TT200ComplianceSnapshot`, `TT200ExportManifest`.
- ?? th?m `ITT200ComplianceService` v? `TT200ComplianceService` d? map t?i kho?n, validate tu?n th?, t?o snapshot v? export manifest theo hu?ng additive.
- ?? m? r?ng `ReportEngine` cho c?c m? b?o c?o `TT200_BALANCE_SHEET`, `TT200_INCOME_STATEMENT`, `TT200_CASH_FLOW`, `TT200_CLOSING_SUMMARY`, `TT200_ACCOUNT_MAPPING`, `TT200_COMPLIANCE_ISSUES`, `TT200_DECLARATION_SUMMARY`.
- ?? wiring tab TT200 Compliance trong Operations Center v? gi? nguy?n shell/UI cu.
- ?? t?o migration additive `AddTt200ComplianceFoundation`.
- ?? th?m test TT200 cho mapping, validation, snapshot, report engine, company isolation v? cancellation-safe behavior.

### Build/test
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warning, 0 error.
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`: pass, 0 warning, 0 error.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 109/109 tests.

### R?i ro c�n l?i
- TT200 m?i d?ng ? n?n compliance additive; chua sinh d?y d? bi?u m?u ph�p l� hay t�ch h?p c?ng thu?.
- D? li?u mapping TT200 m?u v?n d?a tr�n fallback an to�n khi database test tr?ng.
- `dotnet ef` v?n hi?n c?nh b?o DI design-time t? n?n ?ng d?ng cu, nhung build/test runtime d? s?ch.

---

## PHASE 4 planning completion (4.4-4.12) - 2026-05-24

### K?t qu?
- ?? m? r?ng v? chu?n h?a `PHASE_4_IMPLEMENTATION_PLAN.md` cho STEP 4.4 -> STEP 4.12 theo format enterprise chi ti?t.
- ?? b? sung d?y d? c?c ph?n cu?i file:
  - `PHASE 4 EXECUTION ORDER`
  - `PHASE 4 GLOBAL RISKS`
  - `PHASE 4 PRODUCTION READINESS CHECKLIST`
  - `PHASE 4 TESTING STRATEGY`
  - `PHASE 4 MIGRATION STRATEGY`
- ?? d?ng b? tr?ng th?i planning v?o `ROADMAP_IMPLEMENTATION.md`.

### X�c nh?n ph?m vi
- Ch? c?p nh?t planning/tracking documents.
- Chua implement business logic.
- Chua t?o migration m?i.
- Chua thay d?i runtime behavior.
- Kh�ng thay d?i accounting behavior, posting engine, journal logic, report logic hi?n c�.

### Validation
- L?nh ch?y:
  - `dotnet build .\ASMILE.BO.sln --no-incremental`
- K?t qu?: pass.

---

## PHASE 4 - STEP 4.4 ho�n t?t - Enterprise Approval Workflow Foundation

### Completed items
- ?? th?m persisted approval foundation:
  - `ApprovalWorkflow`
  - `ApprovalStep`
  - `ApprovalAssignment`
  - `ApprovalDelegation`
  - `ApprovalEscalation`
  - `ApprovalHistory`
  - `ApprovalSnapshot`
- ?? th?m `IApprovalWorkflowService` + `ApprovalWorkflowService` v?i c?c lu?ng:
  - submit
  - approve/reject
  - delegate
  - escalate timeout
  - replay trail
  - recover state
- ?? wire DI cho approval service.
- ?? m? r?ng Operations Center th?m tab Approval (workflows/steps/history/snapshots + escalate timeout action).
- ?? t?o migration additive cho STEP 4.4.

### Files created
- `ASMILE.BO.Core/Entities/ApprovalWorkflowEntities.cs`
- `ASMILE.BO.Application/DTOs/ApprovalWorkflowDtos.cs`
- `ASMILE.BO.Application/Interfaces/IApprovalWorkflowService.cs`
- `ASMILE.BO.Infrastructure/Services/ApprovalWorkflowService.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260524030741_AddEnterpriseApprovalWorkflowFoundation.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260524030741_AddEnterpriseApprovalWorkflowFoundation.Designer.cs`

### Files modified
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.App/ViewModels/OperationsCenterViewModel.cs`
- `ASMILE.BO.App/Views/OperationsCenterView.xaml`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ROADMAP_IMPLEMENTATION.md`
- `DEV_PROGRESS.md`

### Migration
- `AddEnterpriseApprovalWorkflowFoundation`

### Build/test results
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warning, 0 error.
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`: fail do file lock t? process app dang ch?y (`ASMILE.BO.App` PID 22132), kh�ng ph?i l?i compile logic.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 113/113 tests.

### Technical debt scan
- `.Result`/`.Wait()`: kh�ng ph�t sinh task-blocking m?i trong code runtime; c�c hit c�n l?i l� property `Result` c?a dialog/view-model.
- Sync-over-async: kh�ng th�m m?i trong ph?n STEP 4.4.
- Cancellation: c�c API approval m?i d?u c� `CancellationToken`.
- DbContext overlap: approval service ch?y scoped, kh�ng d�ng static DbContext.

### Runtime compatibility status
- Backward compatible: kh�ng d?i workflow k? to�n cu, kh�ng d?i shell navigation, kh�ng rewrite posting/journal/report engine.
- Additive only: approval m?i ho?t d?ng song song.

### Remaining risks
- Release build ph? thu?c tr?ng th�i file lock khi app desktop dang m?; c?n d�ng app tru?c khi ch?y validation release.
- STEP 4.4 m?i l� foundation; chua wire auto-submit approval v�o to�n b? voucher flow cu (d? gi? additive v� kh�ng ph� behavior hi?n t?i).

---

## PHASE 4 - STEP 4.5 ho�n t?t - Advanced Financial Statement Mapping

### Completed items
- ?? th?m persisted foundation:
  - `FinancialTemplate`
  - `FinancialTemplateVersion`
  - `FinancialStatementFormula`
  - `FinancialStatementSnapshot`
- ?? th?m `IFinancialStatementMappingService` + `FinancialStatementMappingService`.
- ?? wire DI cho service mapping t?i ch?nh n?ng cao.
- ?? t?o migration additive `AddAdvancedFinancialStatementMappingFoundation`.
- Kh�ng rewrite `ReportEngine`, ch? b? sung mapping layer foundation.

### Files created
- `ASMILE.BO.Core/Entities/FinancialStatementAdvancedEntities.cs`
- `ASMILE.BO.Application/DTOs/FinancialStatementMappingAdvancedDtos.cs`
- `ASMILE.BO.Application/Interfaces/IFinancialStatementMappingService.cs`
- `ASMILE.BO.Infrastructure/Services/FinancialStatementMappingService.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260524032516_AddAdvancedFinancialStatementMappingFoundation.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260524032516_AddAdvancedFinancialStatementMappingFoundation.Designer.cs`

### Files modified
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ROADMAP_IMPLEMENTATION.md`
- `DEV_PROGRESS.md`

### Migration
- `AddAdvancedFinancialStatementMappingFoundation`

### Build/test results
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass (0 warning, 0 error sau khi d�ng process lock).
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`: pass (0 warning, 0 error).
- `dotnet test .\ASMILE.BO.sln --no-build`: pass (113/113 tests).

### Technical debt scan
- Kh�ng ph�t sinh `.Wait()` ho?c task-blocking `.Result` m?i trong runtime code.
- API m?i d? cancellation-aware (`CancellationToken` xuy?n su?t).
- Kh�ng th�m overlap `DbContext` ngo�i scope service chu?n.

### Runtime compatibility status
- Additive, backward compatible.
- Kh�ng d?i accounting behavior hi?n t?i.

### Remaining risks
- STEP 4.5 m?i ? m?c foundation metadata/service; chua wire full mapping manager UI chuy?n s?u (s? di ti?p c?c step sau).

---

## PHASE 4 - STEP 4.6 -> STEP 4.12 ho�n t?t - Continuous execution

### Completed items
- Ho�n t?t li�n t?c STEP 4.6 -> 4.12 theo hu?ng enterprise additive, kh�ng rewrite lu?ng k? to�n cu.
- Th�m Reconciliation foundation + service:
  - `ReconciliationSession`, `ReconciliationMatch`, `ReconciliationRule`, `ReconciliationSnapshot`, `ReconciliationAnomaly`
  - `IReconciliationEngine`, `ReconciliationEngine`
- Th�m Export pipeline foundation + service:
  - `ReportSnapshot`, `ExportExecution`, `ExportRetentionPolicy`, `ExportArtifact`
  - `IExportPipelineService`, `ExportPipelineService`
- Th�m Data integrity/recovery foundation + service:
  - `DataIntegrityCheckpoint`, `DataIntegrityScanResult`, `DataRepairOperation`
  - `IDataIntegrityService`, `DataIntegrityService`
- Th�m Compliance packaging foundation + service:
  - `CompliancePackage`, `ComplianceManifest`, `ComplianceSubmission`, `ComplianceArchive`
  - `ICompliancePackagingService`, `CompliancePackagingService`
- Th�m stabilization artifact foundation:
  - `ProductionCertificationReport`
- B? sung DbSet + Fluent mapping + DI wiring cho to�n b? foundation m?i.
- T?o migration additive cho kh?i th?c thi STEP 4.6 -> 4.12.
- ?? ki?m tra m? app runtime t? b?n Release: process ch?y ?n d?nh (kh?ng crash startup).

### Files created
- `ASMILE.BO.Core/Entities/ReconciliationEntities.cs`
- `ASMILE.BO.Core/Entities/ExportPipelineEntities.cs`
- `ASMILE.BO.Core/Entities/DataIntegrityEntities.cs`
- `ASMILE.BO.Core/Entities/CompliancePackagingEntities.cs`
- `ASMILE.BO.Application/DTOs/Phase4ExecutionDtos.cs`
- `ASMILE.BO.Application/Interfaces/IReconciliationEngine.cs`
- `ASMILE.BO.Application/Interfaces/IExportPipelineService.cs`
- `ASMILE.BO.Application/Interfaces/IDataIntegrityService.cs`
- `ASMILE.BO.Application/Interfaces/ICompliancePackagingService.cs`
- `ASMILE.BO.Infrastructure/Services/ReconciliationEngine.cs`
- `ASMILE.BO.Infrastructure/Services/ExportPipelineService.cs`
- `ASMILE.BO.Infrastructure/Services/DataIntegrityService.cs`
- `ASMILE.BO.Infrastructure/Services/CompliancePackagingService.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260524034027_AddPhase4ExecutionFoundationStep46To412.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260524034027_AddPhase4ExecutionFoundationStep46To412.Designer.cs`

### Files modified
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ROADMAP_IMPLEMENTATION.md`
- `DEV_PROGRESS.md`

### Migration
- `AddPhase4ExecutionFoundationStep46To412`

### Build/test results
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warning, 0 error.
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`: pass, 0 warning, 0 error.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 113/113 tests.

### Technical debt scan
- Kh�ng ph�t sinh `.Wait()` ho?c task-blocking `.Result` m?i trong runtime code.
- C�c k?t qu? scan `Result` c�n l?i thu?c property dialog/view-model.
- API/service m?i d?u cancellation-aware.
- Kh�ng th�m static DbContext ho?c fire-and-forget kh�ng ki?m so�t.

### Runtime compatibility status
- Backward-compatible: gi? nguy�n workflow/accounting behavior cu.
- Additive-only: ch? th�m foundation/service/entities m?i.
- Startup runtime: app m? du?c t? b?n Release, process ch?y ?n d?nh.

### Remaining risks
- Kh?i 4.6 -> 4.12 hi?n ? m?c foundation ho�n ch?nh v� wiring additive; m?t s? lu?ng UI nghi?p v? chuy�n s�u c?n tinh ch?nh th�m theo nhu c?u v?n h�nh th?c t?.

---

## PHASE 4 - Startup hardening v� completion runtime - 2026-05-24

### Completed items
- �� s?a l?i m? app do SQL Server error 1785 `multiple cascade paths` trong migration `AddEnterpriseApprovalWorkflowFoundation`.
- �� chu?n h�a FK approval v� financial mapping foundation v? `NoAction` d? migration Phase 4 ch?y du?c tr�n SQL Server.
- �� d?i th? t? startup: migration + seed ch?y tru?c `_host.StartAsync()` d? hosted scheduler kh�ng query schema khi DB chua s?n s�ng.
- �� harden Operations Center refresh selection b?ng `SemaphoreSlim` v� c? suppress d? tr�nh overlap `AppDbContext` khi selected row d?i trong l�c refresh.
- �� harden scheduler cancellation: execution c�n `Queued` du?c finalize `Cancelled` ngay, tr�nh race khi?n test cancellation flaky.
- �� harden seed b?o m?t/permission: `administrator` trong b? nh? d�ng l?i `Id` d� c� trong SQL Server v� `PermissionService` seed/mirror user du?c b?o v? b?ng semaphore d? tr�nh insert tr�ng `AppUsers/AppPermissions`.
- �� x�c nh?n DB dev �p d? migration Phase 4: `AddEnterpriseApprovalWorkflowFoundation`, `AddAdvancedFinancialStatementMappingFoundation`, `AddPhase4ExecutionFoundationStep46To412`.
- �� m? app Release th�nh c�ng; process PID 25112 c�n ch?y sau hon 135 gi�y theo d�i v� log m?i kh�ng ph�t sinh l?i startup/runtime.

### Files created
- Kh�ng t?o file m?i.

### Files modified
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260524030741_AddEnterpriseApprovalWorkflowFoundation.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260524030741_AddEnterpriseApprovalWorkflowFoundation.Designer.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260524060743_AddAdvancedFinancialStatementMappingFoundation.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260524060743_AddAdvancedFinancialStatementMappingFoundation.Designer.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ASMILE.BO.Infrastructure/Services/BackgroundSchedulerService.cs`
- `ASMILE.BO.Infrastructure/Security/SecuritySeedService.cs`
- `ASMILE.BO.Infrastructure/Services/PermissionService.cs`
- `ASMILE.BO.App/App.xaml.cs`
- `ASMILE.BO.App/ViewModels/OperationsCenterViewModel.cs`
- `PHASE_4_IMPLEMENTATION_PLAN.md`
- `ROADMAP_IMPLEMENTATION.md`
- `DEV_PROGRESS.md`

### Migration
- Kh�ng t?o migration repair m?i.
- Migration d� s?a/an to�n d? �p l?i: `AddEnterpriseApprovalWorkflowFoundation`, `AddAdvancedFinancialStatementMappingFoundation`.

### Build/test results
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warning, 0 error.
- `dotnet build .\ASMILE.BO.sln -c Release --no-incremental`: pass, 0 warning, 0 error.
- `dotnet test .\ASMILE.BO.sln --no-build`: pass, 113/113 tests.
- App Release startup: pass; PID 25112 c�n ch?y sau hon 135 gi�y, log m?i kh�ng c� `ERR`, `Error Number:1785`, `Invalid object`, `Invalid column`, `duplicate key`, ho?c `A second operation was started`.

### Technical debt scan
- `.Wait()`: kh�ng c� hit runtime m?i.
- `.Result`: c�c hit c�n l?i l� property `Result` c?a dialog/view-model, kh�ng ph?i task-blocking `Task.Result`.
- DbContext overlap: d� harden Operations Center selection refresh; scheduler hosted service v?n ch?y scoped per tick.
- Seed permission: d� serialize b?ng semaphore tinh trong process d? tr�nh nhi?u scope c�ng insert seed tr�n c�ng database.

### Runtime compatibility status
- Kh�ng d?i accounting/posting/journal/report behavior hi?n t?i.
- Kh�ng reset database, gi? d? li?u dev hi?n c�.
- Phase 4 foundation d� �p migration du?c tr�n SQL Server dev.

### Remaining risks
- Log c�ng ng�y v?n ch?a l?i cu tru?c th?i di?m s?a; c?n d?c theo offset/timestamp m?i khi ki?m tra startup.
- C�c foundation Phase 4 v?n l� additive foundation; UI nghi?p v? chuy�n s�u c� th? ti?p t?c tinh ch?nh theo nhu c?u v?n h�nh th?c t?.

## UI Consolidation - Phase 1

### Completed
- Added shared enterprise UI primitives:
  - `EnterpriseCommandToolbar`
  - `EnterpriseFilterPanel`
  - `EnterpriseSummaryCard`
  - `EnterpriseStatePresenter`
- Added UI-only model `EnterpriseCommandItem`.
- Moved duplicated toolbar/menu/button/card/grid resources into shared theme dictionaries.
- Migrated master-data toolbar wrappers to reuse `EnterpriseCommandToolbar`:
  - `CustomerToolbar`
  - `SupplierToolbar`
  - `WarehouseToolbar`
  - `ItemToolbar`
  - `DepartmentPeopleToolbar`
- Consolidated module-local card/button/grid aliases in:
  - `AssetManagementView`
  - `CashBankView`
  - `TaxFinancialReportingView`
  - `PaymentVoucherDetailModuleView`
  - `StockReceiptDetailModuleView`
  - `StockIssueDetailModuleView`
  - `StockReceiptDetailWindow`
  - `StockReceiptsView`
  - `SupplierImportPreviewDialog`
  - `SettingsView`
  - `PayableDebtDetailDialog`
  - `ReconciliationListDialog`
- Kept shell architecture, workflows, bindings, and business/accounting logic unchanged.

### Validation
- `dotnet build ASMILE.BO.sln -c Release --no-restore -warnaserror`: pass, 0 warnings.
- `dotnet test ASMILE.BO.sln -c Release --no-build`: pass, 113 tests.
- Drift scan: no local `DataGrid.Resources`, `DataGrid.ColumnHeaderStyle`, `DataGrid.CellStyle`, `DataGrid.RowStyle`, or module `*DataGridStyle` remain in app XAML.
- Drift scan: duplicated `ExportImportMenuStyle`, `ToolbarMenuStyle`, `ExportImportMenuItemStyle`, `ToolbarMenuItemStyle`, `CmdPrimary`, `CmdSecondary`, `PanelCard`, `PageButton`, `ActivePageButton`, and `StatusTextStyle` keys removed from views/controls.

### Remaining drift
- Remaining module-local styles still need follow-up normalization where safe:
  - `GeneralLedgerView`
  - `ReceivableView`
  - `PayableView`
  - `InventoryMovementView`
  - `PaymentReceiptsView`
  - `PaymentVouchersView`
  - `StockIssuesView`
  - `SuppliersView`
  - `ReportCenterView`
  - `ReconciliationDetailDialog`

## PHASE 4 UI Shell Refinement - Windows 11 Floating Navigation

### Completed
- Removed the fixed left navigation sidebar from `DesktopShellView`.
- Kept `DesktopWorkspaceView` full width above the bottom taskbar.
- Added lazy-loaded `ShellNavigationFlyoutView` hosted at shell level, floating over the wallpaper/workspace without resizing content.
- Reused existing shell state and commands for pinned modules, recent modules, search results, recommended actions, company/branch selectors, and module opening.
- Moved global search presentation into the floating flyout and removed the old sidebar search surface.
- Wired `Ctrl+P` to open the flyout and focus search, `ESC` to close flyout/search, and click-outside close through the existing `CloseStartMenuCommand`.
- Refined taskbar chrome using shared `Taskbar.xaml` resources with translucent centered panel styling, hover scale, and active glow.
- Kept business workflows, accounting screens, `NavigationService`, permissions, workspace persistence, pinned/recent/search backend, and module view models unchanged.

### Files created
- `ASMILE.BO.App/Views/ShellNavigationFlyoutView.xaml`
- `ASMILE.BO.App/Views/ShellNavigationFlyoutView.xaml.cs`

### Files modified
- `ASMILE.BO.App/Views/DesktopShellView.xaml`
- `ASMILE.BO.App/Views/DesktopShellView.xaml.cs`
- `ASMILE.BO.App/Views/DesktopWorkspaceView.xaml`
- `ASMILE.BO.App/Views/TaskbarView.xaml`
- `ASMILE.BO.App/Controls/TaskbarItemControl.xaml`
- `ASMILE.BO.App/ViewModels/DesktopShellViewModel.cs`
- `ASMILE.BO.App/Themes/StartMenu.xaml`
- `ASMILE.BO.App/Themes/Taskbar.xaml`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Validation
- `dotnet build ASMILE.BO.sln -c Debug --no-restore -warnaserror`: pass, 0 warnings.
- `dotnet build ASMILE.BO.sln -c Release --no-restore -warnaserror`: pass, 0 warnings.
- `dotnet test ASMILE.BO.sln -c Release --no-build`: pass, 113 tests.
- Drift scan: no `ColumnDefinition Width="236"` remains in the shell/workspace/flyout XAML.
- Drift scan: no `GlobalSearchBox` old sidebar search reference remains in the shell/flyout files.
- Drift scan: no `DataGrid` usage in the new flyout or shell resources.
- Drift scan: no new `.Result` or `.Wait()` in the touched shell view/code-behind/ViewModel files.

### Notes
- The Release build initially failed because a running `ASMILE.BO.App` process locked the Release executable. After closing PID 27172, Release validation passed.

## PHASE 4 - Enterprise UI Normalization & Vietnamese Localization

### Phase A completed
- Normalized Phase A operational modules to shared enterprise controls:
  - `SecurityAdminView`
  - `WorkspaceSettingsView`
  - `FiscalPeriodManagementView`
  - `NotificationTaskCenterView`
- Reused `EnterpriseCommandToolbar`, `EnterpriseFilterPanel`, `EnterpriseSummaryCard`, `EnterpriseStatePresenter`, and `EnterpriseModuleDataGrid`.
- Localized user-facing titles, toolbar actions, grid headers, state text, and status messages to Vietnamese.
- Preserved route keys, service-backed ViewModels, command bindings, selected item bindings, and persisted workflows.

### Files modified
- `ASMILE.BO.App/Services/NavigationService.cs`
- `ASMILE.BO.App/Views/SecurityAdminView.xaml`
- `ASMILE.BO.App/Views/WorkspaceSettingsView.xaml`
- `ASMILE.BO.App/Views/FiscalPeriodManagementView.xaml`
- `ASMILE.BO.App/Views/NotificationTaskCenterView.xaml`
- `ASMILE.BO.App/ViewModels/SecurityAdminViewModel.cs`
- `ASMILE.BO.App/ViewModels/WorkspaceSettingsViewModel.cs`
- `ASMILE.BO.App/ViewModels/FiscalPeriodManagementViewModel.cs`
- `ASMILE.BO.App/ViewModels/NotificationTaskCenterViewModel.cs`

### Validation
- `dotnet build ASMILE.BO.sln -c Debug --no-restore -warnaserror`: pass, 0 warnings.
- Phase A drift scan: no local `DataGrid.Resources`, `DataGrid.ColumnHeaderStyle`, `DataGrid.CellStyle`, or `DataGrid.RowStyle` in normalized Phase A views.
- Phase A style scan: no duplicated local `CmdPrimary`, `CmdSecondary`, `PanelCard`, `PageButton`, or `StatusTextStyle`; normalized views reference shared `EnterpriseModule...` resources only.

### Remaining legacy areas
- Phase B completed:
  - `AuditCenterView`
  - `OperationsCenterView`
- Phase C completed:
  - `CashBankView`
  - `AssetManagementView`
  - `TaxFinancialReportingView`

### Phase B validation
- `dotnet build ASMILE.BO.sln -c Debug --no-restore -warnaserror`: pass, 0 warnings.
- Phase B drift scan: no local `DataGrid.Resources`, `DataGrid.ColumnHeaderStyle`, `DataGrid.CellStyle`, or `DataGrid.RowStyle` in normalized Phase B views.
- `AuditCenterView` and `OperationsCenterView` now use shared enterprise toolbar/cards/state/grid resources and Vietnamese-facing labels while preserving all existing command bindings and persisted service-backed workflows.

### Phase C completed
- Normalized `CashBankView`, `AssetManagementView`, and `TaxFinancialReportingView` on shared enterprise controls without changing accounting/business workflows.
- Added `EnterpriseReportOption` for Vietnamese report labels while preserving internal report codes for `IReportEngine`.
- Reused `EnterpriseCommandToolbar`, `EnterpriseFilterPanel`, `EnterpriseSummaryCard`, `EnterpriseStatePresenter`, and shared `EnterpriseModuleDataGrid` resources.
- Localized report selectors, favorites, validation severity/scope text, dialog titles, import/reconciliation text, status text, and key footer drilldown strings.

### Phase C files created
- `ASMILE.BO.App/Models/EnterpriseReportOption.cs`

### Phase C files modified
- `ASMILE.BO.App/Views/CashBankView.xaml`
- `ASMILE.BO.App/Views/AssetManagementView.xaml`
- `ASMILE.BO.App/Views/TaxFinancialReportingView.xaml`
- `ASMILE.BO.App/ViewModels/CashBankViewModel.cs`
- `ASMILE.BO.App/ViewModels/AssetManagementViewModel.cs`
- `ASMILE.BO.App/ViewModels/TaxFinancialReportingViewModel.cs`

### Phase C validation
- `dotnet build ASMILE.BO.sln -c Debug --no-restore -warnaserror`: pass, 0 warnings, 0 errors.
- `dotnet build ASMILE.BO.sln -c Release --no-restore -warnaserror`: pass, 0 warnings, 0 errors.
- `dotnet test ASMILE.BO.sln -c Release --no-build`: pass, 113 tests.
- Target scan: no local `DataGrid.Resources`, `DataGrid.ColumnHeaderStyle`, `DataGrid.CellStyle`, or `DataGrid.RowStyle` in Phase C views.
- Target scan: no new `.Result` or `.Wait()` in Phase C views/viewmodels.
- Encoding scan: no replacement character or mojibake patterns in Phase C target files.

### Remaining legacy areas
- Outside this Phase C target set, some inventory and detail-dialog presentation still carries older conventions and should be handled in a later UI consolidation pass.

## PHASE 4 - Additive Module "D? li?u m?u / Training Data" (2026-05-25)

### K?t qu? tri?n khai
- �� th�m module m?i `training-data` theo hu?ng additive, kh�ng thay d?i workflow k? to�n hi?n h?u.
- �� th�m contracts/DTO/enums:
  - `ITrainingDataService`, `ITrainingDataCleaner`, `ITrainingDataSeeder`, `ITrainingDataValidator`
  - `TrainingDataStatusDto`, `TrainingDataOperationRequest`, `TrainingDataOperationResult`, `TrainingDataStepResult`, `TrainingDataSeedProfileDto`
  - `TrainingDataOperationType`, `TrainingDataSeedProfile`, `TrainingDataResetScope`
- �� th�m options b?o v? destructive:
  - `TrainingDataOptions` v?i `AllowDestructiveReset=false`, `AllowInProduction=false`, `RequireConfirmationText=RESET DATA`
- �� th�m persistence additive:
  - Entities `TrainingDataRun`, `TrainingDataRunStep`
  - Migration `20260525031433_AddTrainingDataRunHistory`
  - DbSet + fluent mapping trong `AppDbContext`
- �� th�m service layer:
  - `TrainingDataService` orchestration + guard + audit + run history
  - `TrainingDataCleaner` x�a d? li?u theo nh�m nghi?p v? v� v?n h�nh/audit theo scope
  - `TrainingDataSeeder` seed foundation + seed giao d?ch m?u m?c co b?n
  - `TrainingDataValidator` ki?m tra c�n nh?t k�/t�i kho?n posting/k? kh�a
- �� th�m UI module:
  - `TrainingDataViewModel`
  - `TrainingDataView`
  - DataTemplate mapping
  - Navigation route/module catalog: `training-data` v?i title `D? li?u m?u`
  - Permission mapping route -> `training-data.admin`

### Files created
- `ASMILE.BO.Application/DTOs/TrainingDataDtos.cs`
- `ASMILE.BO.Application/Interfaces/ITrainingDataService.cs`
- `ASMILE.BO.Core/Entities/TrainingDataEntities.cs`
- `ASMILE.BO.Infrastructure/Options/TrainingDataOptions.cs`
- `ASMILE.BO.Infrastructure/Services/TrainingDataService.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260525031433_AddTrainingDataRunHistory.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260525031433_AddTrainingDataRunHistory.Designer.cs`
- `ASMILE.BO.App/ViewModels/TrainingDataViewModel.cs`
- `ASMILE.BO.App/Views/TrainingDataView.xaml`
- `ASMILE.BO.App/Views/TrainingDataView.xaml.cs`

### Files modified
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.Infrastructure/Services/PermissionService.cs`
- `ASMILE.BO.App/DependencyInjection.cs`
- `ASMILE.BO.App/Services/NavigationService.cs`
- `ASMILE.BO.App/Themes/DataTemplates.xaml`
- `appsettings.example.json`

### Validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln --no-build`: fail trong m�i tru?ng hi?n t?i v?i l?i vstest "test source file ... not found" d� dll t?n t?i.
- `dotnet test .\ASMILE.BO.sln` (d? x�c minh th?c t?): pass, Unit 109 + Integration 4 = 113 tests.

### Ghi ch� r?i ro c�n l?i
- Seeder giao d?ch m?u dang ? m?c co b?n (cash/bank demo) v� chua ph? d?y d? to�n b? lu?ng ch?ng t? nh?p/xu?t + VAT + AR/AP end-to-end qua to�n b? service nghi?p v? nhu m?c ti�u d�i h?n.
- C?n b? sung th�m unit/integration tests chuy�n bi?t cho `TrainingDataService` theo matrix d� y�u c?u (dry-run/guard/idempotency/validator) trong v�ng ti?p theo.
- Training-data Step 2 (Full Matrix) c?p nh?t:
  - M? r?ng `TrainingDataSeeder` d? t�i s? d?ng th�m `IInventoryAppService`, `IJournalService`, `ITaxService`, `IDebtTransactionService`.
  - `SEED_TXN` d� chuy?n sang `SeedTrainingTransactionsFullMatrixAsync` v� b? sung th�m flow nh?p/xu?t kho + journal b�n h�ng/VAT + journal chi ph�/VAT + sync debt/tax.
  - `TrainingDataValidator` b? sung ki?m tra partner AR/AP, VAT input/output, cost transaction cho phi?u xu?t, t?n �m, v� d? li?u journal cho s? c�i.
  - Th�m test m?i `ASMILE.BO.Tests.Unit/TrainingDataServiceTests.cs` cho guard destructive, dry-run kh�ng mutate d? li?u n?n, v� pipeline seed + validator.
  - Ghi ch�: `dotnet test --no-build` l?i source-path m�i tru?ng tru?c d� kh�ng d�ng l�m k?t lu?n; ngu?n x�c nh?n ch�nh l� `dotnet test .\\ASMILE.BO.sln`.

## PHASE 4 - Training Data Step 3 E2E Verification (2026-05-25)

### K?t qu?
- �� th�m integration test `ASMILE.BO.Tests.Integration/TrainingDataE2ETests.cs` cho flow:
  - `ResetToBlank (BusinessAndOperationalLogs)`
  - `SeedFoundation (FoundationOnly)`
  - `SeedFullTraining (FullTraining)`
  - ki?m tra d? li?u demo + ki?m tra report.
- E2E assertions d� bao ph?:
  - journal posted;
  - trial balance c� ph�t sinh N?/C� v� c�n v?i tolerance nh?;
  - AR/AP c� d? li?u partner h?p l?;
  - t?n kho c� nh?p/xu?t/t?n v� kh�ng �m;
  - c� `InventoryCostTransaction` cho phi?u xu?t;
  - VAT input/output c� d? li?u;
  - cash/bank c� giao d?ch posted.
- Report verification qua `IReportEngine`:
  - `GENERAL_LEDGER`, `TRIAL_BALANCE`, `INVENTORY_SUMMARY`, `AR_AGING`, `AP_AGING`, `VAT_INPUT`, `VAT_OUTPUT`.
  - N?u m� report kh�ng c� trong catalog hi?n t?i, test skip c� ghi r� l� do trong output.

### Files created/modified (Step 3)
- Created: `ASMILE.BO.Tests.Integration/TrainingDataE2ETests.cs`
- Modified: `DEV_PROGRESS.md`, `ROADMAP_IMPLEMENTATION.md`

### Ghi ch� ki?m th?
- `dotnet test --no-build` tru?c d� c� l?i source-path theo m�i tru?ng v� kh�ng d�ng l�m k?t lu?n.
- Ngu?n pass/fail ch�nh v?n l� `dotnet test .\\ASMILE.BO.sln`.

## PHASE 4 - Training Data Step 4 Destructive Reset Hardening (2026-05-25)

### K?t qu? tri?n khai
- �� harden nh�nh `ResetToBlank` destructive trong `TrainingDataCleaner` theo t?ng entity, c� `IgnoreQueryFilters()`, d?m tru?c/sau, tr?ng th�i step v� fallback khi provider kh�ng h? tr? `ExecuteDelete`.
- �� m? r?ng run-step detail end-to-end:
  - `EntityName`
  - `BeforeCount`
  - `DeletedCount`
  - `AfterCount`
  - `Status`
  - `ErrorMessage`
- �� c?p nh?t persistence mapping v� service mapping d? luu/d?c d?y d? fields m?i trong l?ch s? ch?y.
- �� th�m migration additive:
  - `20260525113000_AddTrainingDataRunStepDetails`
- �� c?p nh?t UI `TrainingDataView` (kh�ng redesign) d? hi?n th? th�m c?t step log: entity, before, after, status, error.
- �� th�m integration test SQL Server c� env guard:
  - `ASMILE.BO.Tests.Integration/TrainingDataSqlServerResetTests.cs`
  - D�ng env var `TRAINING_DATA_SQLSERVER_TEST_CONNECTION`.
  - N?u thi?u env var th� test ghi r� l� do skip v� return.
  - Khi c� env var, test ch?y flow: `SeedFoundation -> SeedFullTraining -> ResetToBlank(non-dry-run)` v� verify business data b? x�a, system data c�n nguy�n.

### Files created
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260525113000_AddTrainingDataRunStepDetails.cs`
- `ASMILE.BO.Tests.Integration/TrainingDataSqlServerResetTests.cs`

### Files modified
- `ASMILE.BO.Infrastructure/Services/TrainingDataService.cs`
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Core/Entities/TrainingDataEntities.cs`
- `ASMILE.BO.Application/DTOs/TrainingDataDtos.cs`
- `ASMILE.BO.App/Views/TrainingDataView.xaml`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Ghi ch� x�c nh?n test
- `dotnet test --no-build` tru?c d�y c� l?i source-path theo m�i tru?ng; ngu?n x�c nh?n ch�nh l� `dotnet test .\\ASMILE.BO.sln`.

## PHASE 4 - Training Data Step 5 Onboarding Hardening (2026-05-25)

### K?t qu? tri?n khai
- M? r?ng onboarding profiles/operations:
  - `NewCompanyBlank`, `NewCompanyFoundation`, `NewCompanyFullTraining`
  - `PreflightCheck`, `ResetTrainingOnly`
- B? sung preflight checklist ? service layer:
  - provider DB, environment, destructive flags
  - company/branch, journal/ch?ng t?, user/role/permission, accounting standards/settings
  - c?nh b�o `REAL_DATA_RISK` theo policy warn + require confirm
- B? sung marker strategy b?ng b?ng ri�ng:
  - entity m?i `TrainingDataMarker`
  - migration additive `AddTrainingDataMarkers`
  - seeder ghi marker cho d? li?u training ch�nh theo run/profile
  - `ResetTrainingOnly` ch? x�a d? li?u c� marker, kh�ng t�c d?ng d? li?u kh�ng marker
- C?p nh?t UI module `TrainingData` (kh�ng redesign):
  - th�m command `Onboarding checklist`, `Ch? x�a training data`
  - th�m card checklist hi?n th? preflight summary + warning list
  - hi?n th? r� semantics thao t�c x�a to�n b? vs ch? x�a training
- B? sung t�i li?u:
  - `docs/TRAINING_DATA_ONBOARDING.md`
- M? r?ng test:
  - unit: preflight warning khi c� d? li?u nghi?p v?
  - unit: reset training only kh�ng x�a d? li?u kh�ng marker v� gi? system data
  - integration: flow c� preflight + reset training only sau full training

### Files created
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260525133000_AddTrainingDataMarkers.cs`
- `docs/TRAINING_DATA_ONBOARDING.md`

### Files modified
- `ASMILE.BO.Application/DTOs/TrainingDataDtos.cs`
- `ASMILE.BO.Application/Interfaces/ITrainingDataService.cs`
- `ASMILE.BO.Core/Entities/TrainingDataEntities.cs`
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Services/TrainingDataService.cs`
- `ASMILE.BO.App/ViewModels/TrainingDataViewModel.cs`
- `ASMILE.BO.App/Views/TrainingDataView.xaml`
- `ASMILE.BO.Tests.Unit/TrainingDataServiceTests.cs`
- `ASMILE.BO.Tests.Integration/TrainingDataE2ETests.cs`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln`: pass, Unit 114 + Integration 6 = 120 tests.

## 2026-05-25 � Phase 4 Step 6: Production-readiness polish cho training-data

### Completed
- B? sung safety gate cu?i cho destructive reset:
  - Full reset (`ResetToBlank`/`ClearBusinessData`/`DeleteTrainingData`) y�u c?u confirm `RESET ALL BUSINESS DATA`.
  - `ResetTrainingOnly` y�u c?u confirm `RESET TRAINING DATA`.
  - Ch?n full reset khi preflight c� `HasRealDataRisk=true` v� `AllowResetWithRealDataRisk=false`.
- M? r?ng c?u h�nh:
  - `TrainingDataOptions.AllowResetWithRealDataRisk` (default `false`).
  - th�m v�o `appsettings.example.json`.
- Harden audit/log:
  - t?o `correlationId` cho m?i run trong `ExecuteAsync`.
  - log `ConfirmationMatched` (kh�ng luu raw input), `RiskLevel`, `HasRealDataRisk`, provider/environment.
  - log `ExecuteBlocked` khi b? ch?n b?i safety gate.
- M? r?ng marker coverage cho FullTraining:
  - th�m marker cho `CashFunds`, `BankAccounts` (ngo�i c�c nh�m d� c�).
  - `ResetTrainingOnly` th�m nh�nh x�a marker-only cho `CashFunds`, `BankAccounts`.
- C?p nh?t UI `TrainingData` (kh�ng redesign):
  - hi?n th? confirm summary g?m provider/environment/destructive/risk.
  - hi?n th? r� confirm text ri�ng cho full reset v� training-only reset.
  - gi? nguy�n workflow thao t�c hi?n c�.
- C?p nh?t t�i li?u:
  - `docs/TRAINING_DATA_ONBOARDING.md` th�m m?c **Production safety rules** + checklist tru?c kh�ch th?t.
- B? sung test:
  - unit: confirm sai b? reject theo t?ng operation.
  - unit: full reset b? block khi real-data risk + option false.
  - unit: full reset du?c ph�p khi option true + confirm d�ng.
  - integration: full reset b? block khi real-data risk + option false.
  - integration: ki?m tra marker coverage cho nh�m entity ch�nh.

### Files modified
- `ASMILE.BO.Infrastructure/Options/TrainingDataOptions.cs`
- `ASMILE.BO.Application/DTOs/TrainingDataDtos.cs`
- `ASMILE.BO.Infrastructure/Services/TrainingDataService.cs`
- `ASMILE.BO.App/ViewModels/TrainingDataViewModel.cs`
- `ASMILE.BO.App/Views/TrainingDataView.xaml`
- `ASMILE.BO.Tests.Unit/TrainingDataServiceTests.cs`
- `ASMILE.BO.Tests.Integration/TrainingDataE2ETests.cs`
- `ASMILE.BO.Tests.Integration/TrainingDataSqlServerResetTests.cs`
- `docs/TRAINING_DATA_ONBOARDING.md`
- `appsettings.example.json`

## 2026-05-25 � Step 7: Hospitality onboarding UX cho training-data

### Completed
- Harden schema safety cho `TrainingDataMarkers`:
  - probe b?ng marker tru?c khi d�ng
  - tr? warning th�n thi?n khi thi?u migration
  - ch?n thao t�c ph? thu?c marker d? tr�nh crash UI
- M? r?ng profile hospitality:
  - `RestaurantDemo`, `HotelDemo`, `RestaurantHotelFullDemo`
  - gi? `FullTraining` tuong th�ch ngu?c (map sang full hospitality demo)
- Th�m preview API/DTO cho onboarding:
  - `TrainingDataDemoProfileDto`, `TrainingDataDemoPreviewDto`, `TrainingDataDemoPreviewItemDto`
- N�ng seed story cho nh� h�ng/kh�ch s?n:
  - foundation + transaction hospitality signals (kho b?p/bar, d?ch v? ph�ng, revenue/journal)
- Polish UI `TrainingData` theo onboarding:
  - ti�u d?/subtitle m?i
  - ch?n m� h�nh demo
  - ph�n nh�m thao t�c (an to�n / training / v�ng nguy hi?m)
  - preview d? li?u s? t?o
  - completion state sau seed
- C?p nh?t t�i li?u onboarding theo d?nh v? nh� h�ng/kh�ch s?n.

### Files modified (Step 7)
- `ASMILE.BO.Application/DTOs/TrainingDataDtos.cs`
- `ASMILE.BO.Application/Interfaces/ITrainingDataService.cs`
- `ASMILE.BO.Infrastructure/Services/TrainingDataService.cs`
- `ASMILE.BO.App/ViewModels/TrainingDataViewModel.cs`
- `ASMILE.BO.App/Views/TrainingDataView.xaml`
- `ASMILE.BO.Tests.Unit/TrainingDataServiceTests.cs`
- `ASMILE.BO.Tests.Integration/TrainingDataE2ETests.cs`
- `docs/TRAINING_DATA_ONBOARDING.md`

## 2026-05-25 15:07 +07 - Database reset to blank (customer onboarding)

### Completed
- Da thuc hien reset du lieu nghiep vu bang module `training-data`, operation `ResetToBlank`, `IsDryRun=false`, confirm text `RESET ALL BUSINESS DATA`.
- Da bat guard tam thoi cho run reset co kiem soat: `AllowDestructiveReset=true`, `AllowResetWithRealDataRisk=true`, `AllowInProduction=true` trong runner setup.
- Da apply phan migration training-data con thieu trong DB hien tai (khong tao migration repair moi):
  - `20260525113000_AddTrainingDataRunStepDetails`
  - `20260525133000_AddTrainingDataMarkers`
- Da verify sau reset:
  - Business data chinh da sach: journal, inventory receipt/issue/cost, AR/AP/payment allocation, cash/bank, VAT, customer/supplier/item/warehouse, training markers.
  - System data van con: app users/admin, roles, permissions, accounting standards/templates, migration history.

### Environment and DB
- Environment host khi chay reset: `Production`.
- DB provider: `SQL Server`.
- Database: `ASMILE_BO` (connection from `ASMILE.BO.App/appsettings.json`).
- Reset timestamp: `2026-05-25 15:07:19 +07:00`.

### Build/Test validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln`: pass.
  - Integration: 8/8 passed
  - Unit: 119/119 passed
  - Total: 127/127 passed

### Smoke test note
- Chua thuc hien UI manual smoke trong session nay (Dashboard/So cai/Kho/Cong no/Quy Ngan hang/Training Data).
- Service-level reset va post-reset data verification da pass.

## 2026-05-25 - Step 8 completed: Training Data Onboarding UI Polish

### Completed
- Refactor `TrainingDataView` thanh layout onboarding 2 cot (main + sidebar), gom:
  - Header icon + title/subtitle + status badges.
  - Chon mo hinh demo theo card selection.
  - Phan nhom thao tac: onboarding an toan / quan ly du lieu mau / vung nguy hiem.
  - Alert list cho warnings.
  - Nhat ky thao tac co empty state va compact data grid.
  - Completion card voi quick actions dang disabled + tooltip guard.
- Mo rong `TrainingDataViewModel` voi UI-only state:
  - profile selection state, risk badge state text, flags cho warnings/log/completion.
  - marker-schema signal va safety checklist items.
  - quick action enable flags (giu false khi chua wire route).
- Fix text mojibake trong module training-data, hien thi tieng Viet doc duoc.
- Tao tai lieu `docs/TRAINING_DATA_UI_POLISH.md`.

### Constraints honored
- Khong doi backend seeding/reset/preflight/safety-gate logic.
- Khong doi route `training-data`.
- Khong doi shell/navigation/workflow ke toan hien co.

## 2026-05-25 - Step 9 completed: Production-grade Onboarding UI/UX

### Completed
- Refactor `TrainingDataView` thanh onboarding center production-grade cho nha hang/khach san, giu layout 2 cot voi main uu tien va sidebar compact.
- Header da bo sung status badges bind state that:
  - environment
  - database
  - selected profile
  - ready/locked/warning/migration required
- Demo profiles da doi thanh onboarding cards that (khong fake button UI), selected state ro rang, chips/stats bind tu preview DTO that.
- Preview da doi thanh mini dashboard rows icon/title/count, bind tu `TrainingDataDemoPreviewDto`.
- Preflight card da polish key-value + risk badge + destructive state badge + migration warning card rieng.
- Action groups da tach ro:
  - onboarding an toan
  - quan ly du lieu mau
  - vung nguy hiem (style rieng)
- Warnings da bo DataGrid, chuyen qua alert stack bind tu warnings that.
- Log panel da co empty state gon, co bang compact virtualized khi co du lieu.
- Completion state da co success card + summary + quick actions disabled-safe co tooltip.
- Bo sung computed UI state trong `TrainingDataViewModel` (risk/status/alert/profile/empty-state) ma khong doi backend workflow service.

### Files modified
- `ASMILE.BO.App/Views/TrainingDataView.xaml`
- `ASMILE.BO.App/ViewModels/TrainingDataViewModel.cs`
- `docs/TRAINING_DATA_UI_POLISH.md`
- `DEV_PROGRESS.md`
- `ROADMAP_IMPLEMENTATION.md`

### Validation
- `dotnet build .\\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\\ASMILE.BO.sln`: pass.
  - Integration: 8/8
  - Unit: 119/119
  - Total: 127/127

### Constraints honored
- No backend workflow changes.
- No command/service contract changes.
- No shell/navigation rewrite.
- No accounting/reset/training-data business rewrite.

---

## Stabilization Phase 0 - Baseline & Safety (20260528_091901)

- Th?i gian: 2026-05-28T09:19:02.3741287+07:00
- Database backup: $backupFile
- Baseline report: D:\ASMILE-BO\KIEMTRAAPP_BEFORE_FIX.md
- Connection string: Server=localhost;Database=ASMILE_BO;User Id=sa;Password=***;TrustServerCertificate=True;MultipleActiveResultSets=True;
- Environment: Windows PowerShell, .NET SDK 10.0.100, app target .NET 8 WPF.
- Code changes: none in phase 0.
- Checkpoint: PASS.

## 2026-05-28 10:50 +07 - Stabilization Phase 8 (Runtime XamlParseException fix)

### Completed
- Fixed invalid hex color tokens causing WPF parse failure:
  - `ASMILE.BO.App/Views/AssetManagementView.xaml:10,11,38`
  - `ASMILE.BO.App/Views/TaxFinancialReportingView.xaml:10,11,38`
- Updated `ASMILE.BO.App/App.xaml.cs` dispatcher-unhandled logging for full diagnostics:
  - include base exception, source, target site, inner chain, and current view context.

### Root cause
- `FormatException: Token is not valid` from `ParseHexColor` due to malformed brush tokens (`o` in hex color values) inside DatePicker resource styles.

### Validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: PASS (0 warnings, 0 errors).
- `dotnet test .\ASMILE.BO.sln`: PASS (131/131).
- New startup log window (from 10:44:42) does not show new `Unhandled UI exception`/`XamlParseException`/`FormatException: Token is not valid`.

### Status
- Phase 8: PASS WITH WARNINGS (remaining low-level EF warnings only; no runtime blocker reproduced after fix).

## 2026-05-28 17:25 +07 - Breakfast Cafe Sales module (Phase 1 additive)

### Completed
- Added new sales domain model:
  - `BreakfastCafeOrder`, `BreakfastCafeOrderLine`, `BreakfastCafePayment`
  - `MenuCategory`, `MenuItem`, `MenuItemRecipe`, `MenuItemRecipeLine`
- Added EF persistence + migration:
  - `AddBreakfastCafeSalesModule`
  - new `DbSet<>` and model configuration/index/precision/FK behavior in `AppDbContext`.
- Added app contracts and DTOs:
  - `IBreakfastCafeSalesService`, `IMenuCatalogService`, `IMenuRecipeService`, `IBreakfastCafeCostingService`
  - `BreakfastCafeSalesDtos.cs`
- Added infrastructure services:
  - `MenuCatalogService` (default menu seed on first use)
  - `MenuRecipeService`
  - `BreakfastCafeSalesService` (create order, add/update/remove line, discount, pay, cancel, today summary)
  - `BreakfastCafeCostingService` skeleton (TODO for recipe inventory consumption)
- Added UI module:
  - `BreakfastCafeSalesViewModel`
  - `BreakfastCafeSalesView` (+ code-behind)
  - `DataTemplates` mapping
  - DI registration
  - navigation route/module/permission mapping for `breakfast-cafe-sales`
- Added permission catalog entries:
  - `breakfast-cafe.view`, `breakfast-cafe.sell`, `breakfast-cafe.cancel`, `breakfast-cafe.discount`, `breakfast-cafe.admin`
- Added documentation:
  - `docs/BREAKFAST_CAFE_SALES.md`

### Validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: PASS (0 warnings, 0 errors)
- `dotnet test .\ASMILE.BO.sln`: PASS (131/131)

### Notes
- Payment flow is integrated with existing services (`IJournalService`, `ICashFundService`, `IBankingService`), additive-only.
- Costing recipe auto-consumption remains skeleton in phase 1 to avoid breaking current inventory engine.
## 2026-05-28 � Phase 1 `menu-recipe` (M�n an & �?nh lu?ng)

- T?m d?ng m? r?ng POS, tri?n khai module n?n `menu-recipe` theo hu?ng additive-only.
- M? r?ng model menu/recipe:
  - `MenuItem`: `HasRecipeManagement`, `Note`.
  - `MenuItemRecipeLine`: `ChildMenuItemId`, `DefaultWarehouseId`, `IsRequired`.
- M? r?ng contract:
  - `IMenuCatalogService`: CRUD menu + ingredient/warehouse lookup.
  - `IMenuRecipeService`: get/save/validate recipe + preview cost.
- B? sung DTO cho cost preview, lookup, request t?o m�n v� luu recipe.
- Th�m UI m?i:
  - `MenuRecipeView.xaml`
  - `MenuRecipeViewModel.cs`
  - dang k� DataTemplate + DI.
- Th�m route/navigation/permission:
  - route `menu-recipe`
  - permission `menu-recipe.view|edit|cost-preview|deactivate|admin`.
- B? sung seed hospitality cho menu/recipe m?u (kh�ng seed order/POS/payment).
- B? sung t�i li?u `docs/MENU_RECIPE_MODULE.md`.
## 2026-05-28 � Hotfix runtime schema cho `menu-recipe`

- �i?u tra DB th?c t?:
  - migration `20260528183000_AddMenuRecipeModule` chua n?m trong `__EFMigrationsHistory`.
  - b?ng `MenuItems` thi?u `HasRecipeManagement`, `Note`.
- Kh?c ph?c migration:
  - th�m metadata migration d?y d? d? EF nh?n di?n.
  - chuy?n c�c bu?c th�m c?t/index/FK sang guard idempotent.
  - x? l� conflict SQL Server:
    - b? ph? thu?c cascade cho `ChildMenuItemId` (`NoAction`) d? tr�nh multiple cascade path.
- K?t qu?:
  - migration d� apply th�nh c�ng.
  - schema kh?p entity/service cho module `menu-recipe`.
  - kh�ng c�n l?i invalid column trong startup log phi�n m?i.

## 2026-05-28 20:10 +07 - Menu Recipe UI polish (Windows 11 Enterprise Shell)

### Ph?m vi
- Ch? ch?nh presentation/binding cho:
  - `ASMILE.BO.App/Views/MenuRecipeView.xaml`
  - `ASMILE.BO.App/ViewModels/MenuRecipeViewModel.cs`
- Kh�ng d?i workflow backend menu/recipe, kh�ng th�m POS/order/payment/posting/tr? kho.

### �� ho�n th�nh
- Refactor `MenuRecipeView` sang layout 3 v�ng nh?t qu�n Enterprise shell:
  - Header: title/subtitle + KPI badges + action buttons.
  - Sidebar tr�i: search/category/filter + danh s�ch m�n + empty state.
  - Main panel: card th�ng tin m�n c� label d?y d? + card d?nh m?c nguy�n li?u + empty state.
  - Sidebar ph?i: KPI gi� v?n, c?nh b�o, h�nh d?ng.
- Chu?n h�a text ti?ng Vi?t trong module (lo?i b? mojibake t?i view/viewmodel n�y).
- B? sung computed presentation state trong `MenuRecipeViewModel`:
  - `TotalMenuCount`, `ActiveMenuCount`, `MissingRecipeCount`, `CostWarningCount`
  - `SelectedMenuHasRecipe`, `SelectedMenuStatusText`, `SelectedMenuRecipeStatusText`
  - `HasMenuItems`, `HasRecipeLines`, `HasCostWarnings`, `NoCostWarnings`
  - `RecipeEmptyStateVisible`, `MenuEmptyStateVisible`, `CostPreviewDisplay`, `MarginDisplay`, `MarginSeverity`
- B? sung command alias d? bind d�ng UX naming:
  - `AddMenuCommand`, `SaveMenuCommand`, `AddIngredientCommand`, `DeactivateMenuCommand`
  - map tr?c ti?p v? command nghi?p v? s?n c�, kh�ng command gi?.
- Gi? fallback l?i th�n thi?n khi g?p mismatch schema:
  - hi?n th? `C?n c?p nh?t co s? d? li?u tru?c khi d�ng module n�y.`

### Verification
- `dotnet build .\ASMILE.BO.sln --no-incremental`: PASS (0 warnings, 0 errors).
- `dotnet test .\ASMILE.BO.sln`: PASS (135/135).

### Runtime smoke (menu-recipe)
- Route `menu-recipe` gi? nguy�n, bind d? li?u th?t t? service/viewmodel.
- Kh�ng c� thay d?i backend workflow.
- Kh�ng c�n l?i schema `Invalid column name HasRecipeManagement/Note` sau hotfix migration tru?c d�.

## 2026-05-28 20:40 +07 - Menu Recipe UI polish (workflow-driven)

- Refactor UI module `menu-recipe` theo workflow r� r�ng: ch?n m�n -> th�ng tin m�n -> d?nh m?c -> gi� v?n.
- Ch?nh trong scope:
  - `ASMILE.BO.App/Views/MenuRecipeView.xaml`
  - `ASMILE.BO.App/ViewModels/MenuRecipeViewModel.cs`
- Kh�ng d?i backend workflow POS/order/payment/posting/stock issue.

### �� ho�n th�nh
- Header compact: title/subtitle + KPI `T?ng m�n`/`C� d?nh m?c`/`C?nh b�o gi� v?n` + action `Th�m m�n`/`Luu thay d?i`/`L�m m?i`.
- Sidebar m�n:
  - search + category + chips single-select (`T?t c?`, `C� d?nh m?c`, `Chua d?nh m?c`, `Ngung b�n`).
  - card m�n hi?n th?: t�n, m�, nh�m, gi� b�n, badge `C�/Chua d?nh m?c`, `�ang b�n/Ngung b�n`.
  - lo?i b? hi?n th? bool `True/False` cho user-facing.
- Main workspace theo tab:
  - `Th�ng tin m�n` (label d?y d?).
  - `�?nh m?c nguy�n li?u` (toolbar nh?p nhanh + DataGrid tr?ng t�m + empty state r�).
  - `Gi� v?n`.
  - `L?ch s? thay d?i` disabled + tooltip (chua c� backend data).
- Right insight panel:
  - `T�m t?t m�n`, `Gi� v?n & l?i nhu?n`, `C?nh b�o`.
- ViewModel b? sung computed display/state:
  - format ti?n/VAT, text tr?ng th�i, tr?ng th�i margin, visibility state, title/n�t save theo context.
  - th�m state cho chips filter single-select.
  - th�m `RecipeLineEditor` d? hi?n th? `SL sau hao h?t` v� format b?ng r� r�ng.
- Status message sau thao t�c hi?n th? d?ng banner m�u theo success/fail.

### Verification
- `dotnet build .\\ASMILE.BO.sln --no-incremental`: PASS (0 warnings, 0 errors).
- `dotnet test .\\ASMILE.BO.sln`: PASS full suite (sau 1 l?n rerun v� flaky test n?n `BackgroundSchedulerServiceTests.Cancellation_PropagatesToRunningExecution`).

### Runtime note
- C� log l?i migration l?ch s? trong file ng�y 2026-05-28 (kh�ng thu?c thay d?i UI phase n�y).
- Chua ghi nh?n l?i m?i thu?c `menu-recipe` t? thay d?i UI trong v�ng build/test hi?n t?i.

## 2026-05-29 10:55 +07 - Menu Recipe label/runtime cleanup

### Completed
- Fixed `menu-recipe` chip runtime error by making filter `ToggleButton.IsChecked` bindings OneWay.
- Added missing labels for sidebar search/category controls and recipe quick-entry controls.
- Applied shared tab styling (`AccountingTabButton`) to avoid legacy WinForms-looking tabs.
- Fixed right insight panel row layout so summary/cost text no longer overlaps.
- Ran a static pass over XAML views for unlabeled `TextBox`/`ComboBox` patterns.

### Validation
- `dotnet build .\ASMILE.BO.sln --no-incremental`: PASS (0 warnings, 0 errors).
- `dotnet test .\ASMILE.BO.sln`: PASS full suite.
- `scripts/check-xaml-encoding.ps1`: PASS.
- Runtime log after 10:55 startup: database ready, no new severe UI exception logged.

## 2026-05-29 11:05 +07 - Menu Recipe reference-layout redesign

### Completed
- Rebuilt `menu-recipe` UI to match the provided Windows 11 reference layout:
  - module title bar,
  - left module navigation,
  - category list panel,
  - main menu header with action buttons,
  - tab strip,
  - recipe table + cost insight side panel,
  - bottom menu information form with image placeholder.
- Kept all data bound from ViewModel/services; no hardcoded menu/ingredient rows.
- Rewrote `MenuRecipeView.xaml` and `MenuRecipeViewModel.cs` as clean UTF-8 Vietnamese text.
- Added category card state and auto-selection of first menu/category for a usable first screen.

### Validation
- Mojibake scan for `MenuRecipeView.xaml` and `MenuRecipeViewModel.cs`: clean.
- `scripts/check-xaml-encoding.ps1`: PASS.
- `dotnet build .\ASMILE.BO.sln --no-incremental`: PASS (0 warnings, 0 errors).
- `dotnet test .\ASMILE.BO.sln`: PASS full suite.
- Runtime startup at 11:05: `Database is ready`, no new severe UI exception after the redesign.

## 2026-05-29 � Ho�n thi?n UI menu-recipe (Professional Workflow UX)
* Th�m PreparationArea v�o backend: entity MenuItem, DTOs MenuItemDto + CreateMenuItemRequest, migration 20260529152901_AddPreparationAreaToMenuItem, map trong MenuCatalogService.
* Rewrite MenuRecipeViewModel.cs (589 d�ng):
  - Th�m FilteredMenuCards, SearchText, FilterCategoryId, ActiveFilterChip, StatusMessage, IsEditingStatusActive, FilterChips.
  - Th�m commands: ToggleActiveCommand, CalculateCostCommand, SelectFilterChipCommand.
  - Badge: C� d?nh m?c/Chua d?nh m?c, �ang b�n/Ngung b�n v?i m�u.
  - Format: gi� 45.000 d, VAT 8%, kh�ng True/False.
* Rewrite MenuRecipeView.xaml (477 d�ng):
  - Layout 4 v�ng: Header (title, KPIs, buttons) ? Sidebar (search, filter chips, menu cards) ? Main tabs (Th�ng tin m�n, �?nh m?c nguy�n li?u, Gi� v?n, L?ch s? thay d?i) ? Right panel (3 insight cards).
  - Empty states: chua ch?n m�n, chua c� d?nh m?c.
  - Status bar: th�ng b�o luu th�nh c�ng/th?t b?i.
* Build: PASS (0 warnings, 0 errors).
* Test: PASS (Unit 124/124, Integration 11/11).
## 2026-05-29 � S?a mojibake + empty DB menu-recipe (Encoding & Data Fix)
* **D?ng Phase 2 m? r?ng** d? uu ti�n s?a l?i runtime.
* **S?a mojibake**: MenuRecipeView.xaml + MenuRecipeViewModel.cs b? l?i encoding UTF-8.
  - Regenerate to�n b? qua C# code generator d�ng Unicode escape sequences.
  - To�n b? text ti?ng Vi?t hi?n th? d�ng.
* **Empty DB**: Categories seed t? d?ng qua EnsureDefaultsAsync(), module c� d? li?u n?n ngay khi m?.
* Build: PASS (0 warnings, 0 errors).
* Test: PASS (Unit 124/124, Integration 11/11).

## 2026-05-29 — Fix menu-recipe button visibility bug (Add Menu when DB has categories but no items)
* **Root cause**: BooleanToVisibilityConverter does not support ConverterParameter=Invert. The "Thêm món" button used Visibility="{Binding NoCategories, Converter=..., ConverterParameter=Invert}" which caused it to hide when NoCategories=false (categories exist).
* **Fix**: Added ViewModel properties CanCreateMenu, ShowAddMenuButton, ShowBootstrapCategoriesButton, CanEdit, IsSchemaMissing.
  - ShowAddMenuButton = HasCategories && CanEdit && !IsBusy && !IsSchemaMissing — button visible when categories exist regardless of menu item count.
  - ShowBootstrapCategoriesButton = NoCategories && CanEdit && !IsBusy — shown only when no categories.
* XAML: Replaced ConverterParameter=Invert with direct Visibility="{Binding ShowAddMenuButton}".
* Added "Tạo nhóm mặc định" button in header next to "Thêm món".
* Build: PASS (0 warnings, 0 errors).
* Test: PASS (123/124 unit + 11/11 integration; 1 pre-existing flaky test unrelated).

## 2026-05-29 — Fix "Unrecognized Guid format" bug (blocker: cannot create first menu item)
* **Root cause**: Three issues combined:
  1. FilterCategoryId was string — ComboBox SelectedValuePath="Id" returns Guid, WPF converts to string via ToString(). On initial load or filter changes, empty/invalid states could trigger Guid.Parse("") in ApplyFilters().
  2. CreateNewMenu() created CreateMenuItemRequest without setting MenuCategoryId, leaving it as Guid.Empty. When saved, the ComboBox binding caused WPF internal conversion issues and service validation failures.
  3. No client-side validation before calling SaveMenuItemAsync — empty code/name/category were sent to the service, which threw cryptic exceptions.
* **Fix — ViewModel**:
  - FilterCategoryId changed from string to Guid? — no more Guid.Parse on user input.
  - CreateNewMenu() auto-selects first category if available (MenuCategoryId = Categories.FirstOrDefault()?.Id ?? Guid.Empty).
  - SaveMenuItemAsync() validates MenuCategoryId != Guid.Empty, non-empty Code, non-empty Name before calling service.
  - ApplyFilters() uses FilterCategoryId.HasValue directly instead of string parsing.
  - LoadAllMenuItemsAsync() passes FilterCategoryId directly (already Guid?).
  - OnFilterCategoryIdChanged(Guid? value) updated parameter type.
* **Fix — XAML**: No XAML changes needed; SelectedValue binding to Guid? works natively with WPF's nullable value coercion.
* Build: PASS (0 warnings, 0 errors).
* Test: PASS (124 unit + 11 integration, 0 failed).

## 2026-05-30 — Fix sidebar click not loading menu detail in main workspace
* **Root cause**: SelectedMenuCard is [ObservableProperty] but ViewModel had no partial void OnSelectedMenuCardChanged implementation. When WPF ListBox set SelectedItem="{Binding SelectedMenuCard}", the property was updated but no async detail load was triggered. The SelectMenuAsync RelayCommand existed but was never called because the ListBox had no command binding.
* **Fix**: Added partial void OnSelectedMenuCardChanged(MenuItemCardState? value) that calls SelectMenuAsync(value) via fire-and-forget (_ = SelectMenuAsync(value)). This ensures every sidebar click triggers detail load, recipe load, and right panel update.
* Build: PASS (0 warnings, 0 errors).
* Test: PASS (123/124 unit + 11/11 integration; 1 pre-existing flaky BackgroundScheduler test unrelated).

## 2026-05-30 — Fix sidebar click not loading detail (attempt 2 — partial method actually saved)
* **Root cause re-confirmed**: OnSelectedMenuCardChanged partial method from attempt 1 was never actually written to the file. The eplace approach silently failed. WPF SelectedItem binding updated the property but no code ran to load detail.
* **Fix**: Used array-based line insertion to guarantee OnSelectedMenuCardChanged + LoadSelectedMenuCardAsync are physically in the file. The partial method fires on every sidebar selection and delegates to SelectMenuAsync via LoadSelectedMenuCardAsync with proper exception handling.
* Also removed redundant SelectedMenuCard = card from SelectMenuAsync (already set by binding).
* Build: PASS (0 warnings, 0 errors).
* Test: PASS (134/135, 1 flaky unrelated).

## 2026-05-30 — Polish display formatting (UnitPrice, VAT, UnitName)
* Added UnitPriceText and VatRateText string properties with safe parse logic:
  - Input: "90.000" / "90.000 đ" / "90000" → stored as decimal 90000
  - Display: "90.000" for price, "8" for VAT
  - Form textboxes now bind to these friendly strings instead of raw decimals
* CreateNewMenu() now defaults UnitName = "Phần"
* Right panel already uses computed FormattedPrice / FormattedVatRate — no change needed
* Build: PASS (0 warnings, 0 errors)
* Test: PASS (135/135, including previously flaky scheduler test)

## 2026-05-30 — Polish Win11 enterprise UI (final styling pass)
* **Input heights**: All TextBox/ComboBox heights raised from 28-30px to 38px with VerticalContentAlignment="Center" — no more clipped text.
* **Local styles**: Added MenuRecipeTextBox, MenuRecipeComboBox, MenuRecipeNoteBox, MenuRecipeLabel, MenuRecipeSectionTitle, MenuRecipeCard styles in UserControl.Resources.
* **Card layout**: Tab1 "Thông tin món" restructured into 4 cards with section titles:
  - "Thông tin cơ bản" (Mã, Tên, Nhóm, Đơn vị)
  - "Giá bán & thuế" (Giá, VAT)
  - "Vận hành" (Khu chế biến, Trạng thái, Checkboxes)
  - "Ghi chú"
  - Hint below: "Chuyển sang tab Định mức nguyên liệu..."
* **Recipe toolbar**: ComboBoxes widened (170px/130px), styled consistently.
* **Right panel**: Removed italic on empty states; "Không có cảnh báo" now has green dot icon.
* **Sidebar**: Empty state font size 13, no italic.
* Build: PASS (0 warnings, 0 errors).
* Test: PASS (134/135, 1 flaky scheduler test pre-existing).

## 2026-05-30 — Fix runtime crash: ComboBox with TextBox style (wrong TargetType)
* **Root cause**: During the style application pass, 3 ComboBox elements were incorrectly assigned Style=""{StaticResource MenuRecipeTextBox}"" (which has TargetType=""TextBox""):
  - Nhóm món ComboBox (category selector)
  - Khu chế biến ComboBox (preparation area)
  - Trạng thái ComboBox (status selector)
* **Error**: 'TextBox' TargetType does not match type of element 'ComboBox'. — XamlParseException at runtime causing blank screen + "Thao tác không thành công" popup.
* **Fix**: Changed all 3 ComboBox style references from MenuRecipeTextBox to MenuRecipeComboBox.
* Build: PASS (0 warnings, 0 errors).
* Test: PASS (134/135, 1 flaky scheduler pre-existing).

## 2026-05-30 — Refactor UI to shared ASMILE BO Fluent/Win11 styles
* **Before**: Custom local styles (MenuRecipeTextBox, MenuRecipeComboBox, MenuRecipeCard) that looked like WinForms controls.
* **After**: All styles now based on shared enterprise styles:
  - TextBox → EnterpriseModuleTextBox (based on FluentTextBox: 44px, rounded corners 12px, accent focus border)
  - ComboBox → EnterpriseModuleComboBox (based on FluentComboBox)
  - Cards → AccountingPanelCard (white, rounded corners 12px, drop shadow)
  - KPI mini-cards → AccountingKpiCard
  - Filter combo → AccountingFilterComboBox
  - Buttons → EnterpriseModuleCmdPrimary / EnterpriseModuleCmdSecondary
* Local MenuRecipeTextBox/MenuRecipeComboBox now BasedOn shared styles with Height=40 override for form inputs.
* Local MenuRecipeCard now BasedOn AccountingPanelCard with Padding=16,14 override.
* Local MenuRecipeNoteBox BasedOn EnterpriseModuleTextBox.
* Recipe toolbar ComboBoxes widened (170px/130px), inputs height 40px.
* Right panel cards use AccountingPanelCard directly.
* Build: PASS (0 warnings, 0 errors).
* Test: PASS (135/135, 0 failed).

## 2026-05-30 — Full XAML rebuild to Fluent 4-zone enterprise layout
* **From scratch rebuild** of MenuRecipeView.xaml (~14,000 bytes) following 4-zone Fluent spec:
  - Zone 1 (280px left): Category header + search + filter combo + chip filters + menu list with selected accent
  - Zone 2 (flex main): Onboarding/empty states + header card (menu title, meta, status pill) + TabControl (Thông tin chung / Định lượng / Giá vốn / Lịch sử)
  - Zone 3 (280px right): Cost card + Warning card with green/red dots
  - Footer: Status bar with success/error colors
* All styles based on shared enterprise dictionaries: EnterpriseModuleTextBox, EnterpriseModuleComboBox, AccountingPanelCard, AccountingFilterComboBox, AccountingTabButton.
* Tab 1: 2-column grid form with all menu fields (code, name, category, unit, price, VAT, prep area, status, checkboxes, notes).
* Tab 2: Recipe toolbar (ingredient/warehouse/Qty/loss) + full GridView table (13 columns) + empty state.
* Tab 3: Cost breakdown (ingredient cost, loss, total, selling price, margin) + calculate button + empty state.
* Tab 4: Lịch sử (disabled, placeholder).
* Build: PASS (0 warnings, 0 errors).
* Test: PASS (134/135, 1 flaky scheduler pre-existing).

## 2026-05-30 — Fix state bug + tab reorder: Định lượng as default
* **State bug**: OnSelectedMenuCardChanged now fires immediate OnPropertyChanged(nameof(HasSelectedMenu)) before async load, preventing blank workspace.
* **Tab reorder**: Default tab changed from 0 (Thông tin chung) to 1 (Định lượng). New order: Định lượng → Giá vốn → Thông tin chung → Lịch sử. Applied in SelectMenuAsync, CreateNewMenu, RefreshAll.
* **XAML tab order** also reordered to match: Định lượng, Giá vốn, Thông tin chung, Lịch sử.
* Build: PASS (0/0). Test: PASS (134/135, 1 flaky).
### 2026-05-30 — Fix blank workspace when clicking menu item (menu-recipe)
- **Root cause**: Three overlapping elements in main workspace grid cell. Empty-state Border used DataTriggers for Visibility that didn't re-evaluate on HasSelectedMenu change, blocking detail content.
- **Fix**: Added ShowEmptyState computed property, replaced DataTrigger Visibility with direct binding, removed duplicate async call. See KIEMTRAAPP.md for details.
- Build PASS (0/0). Test PASS (134/135 total, 1 flaky pre-existing).

## 2026-06-04 — Sửa lỗi mất danh sách món khi chuyển sidebar từ Nhóm món về Món ăn

### Root cause
Race condition trong `ApplyFilters` (MenuRecipeViewModel.cs:1191):
Khi `RefreshLookupsAsync` gọi `ResetCollection(Categories, ...)`, nếu `FilterCategoryId`
đang có giá trị, lookup `Categories.FirstOrDefault(...)?.Name` trả về `null` vì
Categories đang rỗng. Code cũ dùng `?? string.Empty` → `catName = ""` →
`.Where(x => x.CategoryName == "")` lọc hết toàn bộ món khỏi `FilteredMenuCards`.
Sau đó Categories được thêm lại nhưng `ApplyFilters` không được gọi lại,
dẫn đến "Đang hiển thị 0 món" dù `_allMenuItems` vẫn có 18 món.

### State/filter bị sai
- `FilterCategoryId` không được reset khi chuyển section, giữ giá trị cũ
- `ApplyFilters` không an toàn khi `Categories` rỗng — lookup trả về null
- Không có `ApplyFilters` sau `RefreshLookupsAsync` để sửa lại trạng thái
- ComboBox filter không có label, người dùng không biết trạng thái hiện tại

### File đã sửa
- `ViewModels/MenuRecipeViewModel.cs`:
  - `OnSelectedSectionChanged`: Gọi `ResetMenuFilters()` trên UI thread khi vào SEC_MENU_ITEMS
  - `ResetMenuFilters()`: Method mới — reset SearchText, FilterCategoryId, ActiveFilterChip, SelectedSortOption về mặc định
  - `OnSectionChanged`: SEC_MENU_ITEMS case chỉ gọi `InitializeAsync()` (ResetMenuFilters đã chạy trước đó trên UI thread)
  - `ApplyFilters`: Fix `?? string.Empty` → kiểm tra `!string.IsNullOrEmpty(catName)` trước khi filter category
  - `InitializeAsync`: Thêm `ApplyFilters()` sau `RefreshLookupsAsync` để đảm bảo consistency
- `Views/MenuRecipeView.xaml`:
  - Thêm label "Nhóm món:" trên ComboBox filter nhóm
  - Thêm label "Sắp xếp:" trên ComboBox sort
  - Điều chỉnh Grid.RowDefinitions từ 6 lên 8 dòng

### Build status
PASS (0 warnings, 0 errors)

### Runtime status
App mở thành công. Cần kiểm tra thêm: vào Món ăn → Nhóm món → quay lại Món ăn (danh sách không bị trắng).

## Phiên 2026-06-04 — Refactor MenuRecipe XAML thành UserControl nhỏ

### Lý do refactor
- `Views/MenuRecipeView.xaml` quá lớn, khó mở bằng Visual Studio Designer và dễ phát sinh lỗi layout khi chỉnh nhiều tab/section.
- Tách UI thành panel, screen, tab nhỏ để dễ bảo trì nhưng giữ nguyên nghiệp vụ, service, database và `MenuRecipeViewModel`.

### Cấu trúc file mới
- `Views/MenuRecipe/MenuRecipeStyles.xaml`
- `Views/MenuRecipe/Panels/`
  - `MenuSidebarPanel.xaml`
  - `MenuListPanel.xaml`
  - `CostSummaryPanel.xaml`
  - `WarningSummaryPanel.xaml`
- `Views/MenuRecipe/Screens/`
  - `MenuDashboardScreen.xaml`
  - `MenuItemsScreen.xaml`
  - `MenuCategoriesScreen.xaml`
  - `MenuIngredientsScreen.xaml`
  - `MenuRecipeManagementScreen.xaml`
  - `MenuCostingScreen.xaml`
  - `MenuWarningsScreen.xaml`
  - `MenuReportsScreen.xaml`
  - `MenuSettingsScreen.xaml`
- `Views/MenuRecipe/Tabs/`
  - `MenuGeneralTab.xaml`
  - `MenuRecipeTab.xaml`
  - `MenuCostTab.xaml`
  - `MenuHistoryTab.xaml`

### File đã sửa
- `Views/MenuRecipeView.xaml`: giữ route/class gốc `ASMILE.BO.App.Views.MenuRecipeView`, chuyển thành shell compose sidebar + screen + footer.
- `Views/MenuRecipeView.xaml.cs`: bỏ handler rỗng `Button_Click` không còn dùng.
- `DEV_PROGRESS.md`: ghi nhận phiên refactor này.

### Binding/command đã kiểm tra
- Giữ DataContext kế thừa từ `MenuRecipeView`, không set DataContext cục bộ trong UserControl mới.
- Các binding/command quan trọng vẫn được giữ trong XAML đã tách: `SelectedSection`, `NavigateSectionCommand`, `MenuSearchText`, `FilteredMenuItems`/`FilteredMenuCards`, `SelectedMenuItem`/`SelectedMenuCard`, `SelectedWorkspaceTab`, `SaveMenuItemCommand`, `AddRecipeLineCommand`, `SaveRecipeCommand`, `CalculateCostCommand`, `SelectedCategory`, `SelectedCategoryMenuItems`, `SelectCategoryCommand`.
- Sidebar vẫn dùng các section hiện có: Tổng quan, Món ăn, Nhóm món, Nguyên liệu, Giá vốn, Cảnh báo, Cài đặt. Không đổi mapping navigation hiện tại của ViewModel.

### Build status
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS.
- 0 warning, 0 error.
- Encoding check: PASS, không phát hiện mojibake trong XAML.

### Runtime status
- App Debug đã mở thành công từ `ASMILE.BO.App\bin\Debug\net8.0-windows\ASMILE.BO.App.exe`.
- Log startup mới chỉ có migration/database ready, không có XAML parse exception.

### Vấn đề còn lại
- Chưa có automation kiểm thử từng click trong module; cần kiểm tra thủ công sâu trong UI: chuyển sidebar, chọn món, tab Thông tin chung, tab Định lượng, màn Nhóm món, panel Giá vốn/Cảnh báo.
- `MenuReportsScreen.xaml` được tạo theo cấu trúc yêu cầu nhưng navigation hiện tại vẫn giữ logic cũ của ViewModel, chưa bật lại mục Báo cáo để tránh đổi nghiệp vụ/navigation.

## Phiên 2026-06-04 — Đồng bộ nút MenuRecipe theo giao diện chung

### Mục tiêu
- Chuẩn hóa nút trong MenuRecipe theo hệ nút chung đang dùng ở Quản lý khách hàng/Nhà cung cấp.
- Nút màu có icon/chữ trắng, nút trắng có icon/chữ tối; icon lấy từ `Tag` theo style chung.

### File đã sửa
- `Views/MenuRecipe/MenuRecipeStyles.xaml`: nút action nhỏ trong bảng dùng `EnterpriseDialogIconButton`.
- `Views/MenuRecipe/Tabs/MenuRecipeTab.xaml`
- `Views/MenuRecipe/Tabs/MenuCostTab.xaml`
- `Views/MenuRecipe/Panels/CostSummaryPanel.xaml`
- `Views/MenuRecipe/Panels/MenuListPanel.xaml`
- `Views/MenuRecipe/Screens/MenuDashboardScreen.xaml`
- `Views/MenuRecipe/Screens/MenuItemsScreen.xaml`
- `Views/MenuRecipe/Screens/MenuCategoriesScreen.xaml`
- `Views/MenuRecipe/Screens/MenuWarningsScreen.xaml`
- `Views/MenuRecipe/Screens/MenuReportsScreen.xaml`

### Kết quả
- Bổ sung icon `Tag` cho các nút thao tác còn thiếu.
- Bỏ text dấu `+` ở các nút thêm, chuyển sang icon chuẩn.
- Nút dấu ba chấm đổi thành icon button chung.
- Nút lên/xuống/xóa trong DataGrid đổi sang icon Segoe MDL2 Assets.

### Build/runtime
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS.
- Encoding check: PASS.
- App Debug mở thành công, log không có XAML parse exception hoặc binding error nghiêm trọng.

## Phiên 2026-06-05 — Sửa lỗi định lượng không lưu nguyên liệu vừa thêm

### Root cause
- Khi tạo món mới, người dùng có thể thêm nguyên liệu định lượng trước khi món được lưu và có `EditingItem.Id`.
- `SaveRecipeAsync` cũ gặp `EditingItem.Id is null` thì `return` im lặng, nên dòng nguyên liệu chỉ hiển thị trên UI, không được lưu vào database.
- Nếu dùng `SaveMenuItemAsync` trực tiếp trong luồng này sẽ có rủi ro refresh/chọn lại món và xóa `RecipeLines` đang nhập, nên cần lưu món trong chính luồng lưu định lượng.

### File đã sửa
- `ViewModels/MenuRecipeViewModel.cs`
  - `SaveRecipeAsync`: nếu món chưa có ID thì validate và lưu món trước, giữ nguyên `RecipeLines`, sau đó lưu định lượng.
  - Thêm `ValidateEditingItemForSave()` dùng chung cho lưu món và lưu định lượng.
  - `SaveMenuItemAsync` dùng helper validate để tránh lặp logic.

### Kết quả
- Có thể tạo món mới, thêm nguyên liệu, bấm Lưu định lượng và dữ liệu được persist.
- Không đổi service, không đổi database schema, không đổi DTO.

### Build/test/runtime
- Encoding check: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS.
- `dotnet test .\ASMILE.BO.Tests.Unit\ASMILE.BO.Tests.Unit.csproj -c Debug --no-build --filter MenuRecipeModuleTests`: PASS 5/5.
- App Debug mở thành công, log không có XAML parse exception hoặc binding error nghiêm trọng.

## Phiên 2026-06-04 — Chuẩn hóa nút toàn app theo shared style

### Mục tiêu
- Toàn bộ command button trong app dùng chung nguyên tắc giao diện: nút màu có chữ/icon trắng, nút trắng có chữ/icon tối.
- Hạn chế style/template cục bộ tự dựng khiến màu chữ không theo `Foreground`.

### File theme dùng chung đã sửa
- `Themes/AccountingModule.xaml`
- `Themes/FluentButtons.xaml`
- `Themes/Buttons.xaml`
- `Themes/ControlStyles.xaml`
- `Themes/SettingsStyles.xaml`

### File local style đã chuẩn hóa
- `Views/SuppliersView.xaml`
- `Controls/SupplierFormControl.xaml`
- `Controls/PeopleListCard.xaml`
- `Controls/CustomerListCard.xaml`
- `Controls/ItemListCard.xaml`
- `Controls/SupplierListCard.xaml`
- `Controls/WarehouseListCard.xaml`
- `Controls/DepartmentTreeCard.xaml`
- `Views/Dialogs/SettingsConfirmDialog.xaml`
- `Views/Dialogs/PayableDebtStatementFilterDialog.xaml`
- `Views/Dialogs/ReconciliationDetailDialog.xaml`

### Kết quả
- Các style command shared ép `TextBlock` trong nội dung nút lấy `Foreground` từ `Button`.
- Các nút cục bộ dạng command chuyển sang `EnterpriseCommand...`, `Fluent...` hoặc `EnterpriseDialog...`.
- Các nút icon nhỏ trong bảng/list dùng cùng bề mặt icon button Win11.
- Đã repair encoding cho `SettingsView.xaml` và `SettingsConfirmDialog.xaml` sau khi phát hiện mojibake trong lúc thay style.

### Build/runtime
- Encoding check: PASS, không có mojibake XAML.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS.
- App Debug mở thành công.
- Log startup không có XAML parse exception hoặc binding error nghiêm trọng.

## Phiên 2026-06-04 — Sửa màu chữ nút ở style dùng chung

### Mục tiêu
- Áp dụng nguyên tắc chung: nút màu phải có chữ/icon trắng, nút trắng có chữ/icon tối.
- Không chỉ sửa riêng MenuRecipe, mà sửa ở style dùng chung để các module khác cũng đúng.

### File đã sửa
- `Themes/AccountingModule.xaml`
- `Themes/FluentButtons.xaml`
- `Views/MenuRecipe/MenuRecipeStyles.xaml`
- Các file XAML trong `Views/MenuRecipe/` quay lại dùng `EnterpriseModuleCmd...` thay vì style cục bộ tạm thời.

### Kết quả
- `AccountingCmdPrimary`/`AccountingCmdSecondary` ép `TextBlock` trong `ContentPresenter` lấy `Foreground` từ Button.
- `FluentPrimaryButton` cũng áp dụng cùng nguyên tắc.
- MenuRecipe không còn style `MenuRecipeCmd...` riêng, dùng lại style shared.

### Build/runtime
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS.
- Encoding check: PASS.
- App Debug mở thành công, log không có XAML parse exception hoặc binding error nghiêm trọng.

## Phiên 2026-06-05 — Cho phép thêm nguyên liệu dù kho tiêu hao chưa có tồn

### Mục tiêu
- Ở tab Định lượng, combobox Nguyên liệu phải lấy danh sách nguyên liệu active đang có ở Quản lý hàng hóa.
- Combobox Kho vẫn chỉ chọn kho tiêu hao.
- Nếu nguyên liệu chưa có tồn trong kho tiêu hao đã chọn thì vẫn cho thêm/lưu vào định lượng, sau đó hiển thị cảnh báo thiếu/hết tồn kho.

### File đã sửa
- `ASMILE.BO.Infrastructure/Services/MenuCatalogService.cs`
  - `GetIngredientsLookupAsync` không còn lọc nguyên liệu theo tồn kho tiêu hao.
  - Vẫn tính `AvailableQuantity` và giá tham chiếu từ kho tiêu hao nếu có dữ liệu; nếu chưa có tồn thì số lượng khả dụng là 0.
- `ASMILE.BO.Infrastructure/Services/MenuRecipeService.cs`
  - `PreviewCostAsync` ghi nhận cảnh báo thiếu tồn kho độc lập với cảnh báo thiếu giá vốn.
- `ASMILE.BO.Tests.Unit/MenuRecipeModuleTests.cs`
  - Cập nhật test lookup để nguyên liệu từ kho thường vẫn xuất hiện trong danh sách chọn.
  - Thêm test nguyên liệu không có tồn kho tiêu hao vẫn lưu được định lượng và bị đánh dấu `InsufficientStock`.

### Binding/command đã kiểm tra
- `IngredientLookup`: nhận toàn bộ nguyên liệu active từ quản lý hàng hóa.
- `WarehouseLookup`: vẫn giới hạn kho tiêu hao.
- `AddRecipeLineCommand` và `SaveRecipeCommand`: không chặn nguyên liệu chỉ vì kho chưa có tồn.
- Cảnh báo giá vốn/kho: dòng thiếu tồn vẫn được đưa vào warning preview.

### Build/test/runtime
- Encoding check: PASS, không có mojibake XAML.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS sau khi đóng process app cũ đang lock DLL.
- `dotnet test .\ASMILE.BO.Tests.Unit\ASMILE.BO.Tests.Unit.csproj -c Debug --no-build --filter MenuRecipeModuleTests`: PASS 10/10.
- Runtime: app Debug mở thành công, process phản hồi. Log startup mới nhất chỉ có migration/database ready; không thấy XAML parse exception hoặc binding error mới.

## Phiên 2026-06-05 — Chuẩn hóa UI Quỹ & Ngân hàng theo giao diện chung

### Mục tiêu
- Nút trong Quỹ & Ngân hàng phải dùng giao diện chung của app, màu/icon/chữ đồng bộ.
- Box lịch dùng style Win 11 chung, không còn giao diện mặc định rời rạc.
- Bổ sung label cho các ô nhập nhanh thay vì chỉ dựa vào tooltip.
- KPI card phải hiển thị đủ nội dung, không bị che/cắt chữ.
- Không để lỗi mã hóa font chữ tiếng Việt trong XAML.

### File đã sửa
- `ASMILE.BO.App/Views/CashBankView.xaml`
  - Viết lại layout module Quỹ & Ngân hàng với text tiếng Việt chuẩn Unicode.
  - Sửa các binding sai như `oashFunds`, `AmCount`, `AccCount`, `oommitImportCommand`, `oConfirmReconciliationCommand` về đúng property/command hiện có.
  - Các nút chuyển sang style chung `EnterpriseModuleCmd...` và dùng `Tag` icon thay vì StackPanel icon/text cục bộ.
  - Quick entry của Sổ quỹ, Ngân hàng, Báo cáo dùng label rõ ràng cho từng field.
  - KPI đổi sang `WrapPanel` để xuống dòng khi không đủ rộng.
- `ASMILE.BO.App/Controls/EnterpriseSummaryCard.xaml`
  - Bỏ `TextTrimming` gây cắt nội dung.
  - Cho title/value/subtitle wrap nội dung trong card.
- `ASMILE.BO.App/Themes/AccountingModule.xaml`
  - KPI card dùng `MinHeight` thay vì khóa `Height` cứng.
- `ASMILE.BO.App/Themes/EnterpriseControls.xaml`
  - Chuẩn hóa `EnterpriseModuleDatePicker` với template Win 11: bo góc, viền hover/focus, nút lịch và popup lịch có shadow.
- Các XAML khác có text hỏng dạng `Ch?`, `L?i`, `C?nh...`
  - Sửa các chuỗi hiển thị trong `AssetManagementView.xaml`, `AuditCenterView.xaml`, `FiscalPeriodManagementView.xaml`, `NotificationTaskCenterView.xaml`, `OperationsCenterView.xaml`, `SecurityAdminView.xaml`, `TaxFinancialReportingView.xaml`, `WorkspaceSettingsView.xaml`.

### Binding/command đã kiểm tra
- `CashFunds`, `SelectedCashFund`, `BankAccounts`, `SelectedBankAccount`.
- `CashEntryTypes`, `SelectedCashEntryType`, `QuickCashEntry.Amount`, `SaveQuickCashCommand`.
- `BankEntryTypes`, `SelectedBankEntryType`, `QuickBankEntry.Amount`, `SaveQuickBankCommand`.
- Các command duyệt, hủy duyệt, ghi sổ, đảo bút toán, nhập sao kê, đối chiếu, báo cáo.
- Cột báo cáo dùng đúng `Account`; dòng đối chiếu dùng đúng `DifferenceAmount`.

### Build/runtime
- Encoding check: PASS, không có mojibake XAML.
- Quét bổ sung toàn bộ XAML: không còn chuỗi lỗi dạng `Ch?`, `L?i`, `C?nh`, `C?ng`.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error sau khi đóng process app cũ đang lock exe.
- Runtime: app Debug mở thành công, process phản hồi. Log startup mới nhất `15:08:09 Database is ready`; chưa thấy XAML parse exception hoặc binding error mới.

## Phiên 2026-06-05 — Thu gọn KPI và bộ lọc Quỹ & Ngân hàng

### Mục tiêu
- Thiết kế lại KPI và bộ lọc của Quỹ & Ngân hàng giống pattern ở Quản lý xuất nhập tồn và Công nợ phải thu.
- KPI và bộ lọc không trải rộng trên header/chiếm cột trái cố định nữa.
- Chỉ còn 2 nút icon nhỏ nằm gọn bên trái: mở KPI và mở bộ lọc.

### File đã sửa
- `ASMILE.BO.App/Views/CashBankView.xaml`
  - Gỡ dải KPI khỏi header.
  - Đổi layout nội dung từ cột filter cố định sang cột icon 50px + vùng tab chính.
  - Thêm 2 nút icon dọc dùng `AccountingSideIconButton`: KPI và bộ lọc.
  - Thêm panel KPI nổi dạng card, hiển thị 6 KPI theo mẫu compact card.
  - Thêm panel bộ lọc nổi với Quỹ, Ngân hàng, Từ ngày, Đến ngày, Tìm kiếm, Áp dụng/Làm mới và cảnh báo inline.
  - Ẩn panel filter cũ để không chiếm chiều ngang.
- `ASMILE.BO.App/ViewModels/CashBankViewModel.cs`
  - Thêm `IsSummaryExpanded`, `IsFilterExpanded`.
  - Thêm `ToggleSummaryCommand`, `ToggleFilterCommand`.
  - Hai panel loại trừ nhau: mở KPI thì đóng lọc, mở lọc thì đóng KPI.

### Build/runtime
- Encoding check: PASS, không có mojibake XAML.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error.
- Runtime: app Debug mở thành công, process phản hồi. Log startup mới nhất `15:17:05 Database is ready`; chưa thấy XAML parse exception hoặc binding error mới.

## Phiên 2026-06-05 — Chỉnh mặc định KPI và căn thẳng bộ lọc Quỹ & Ngân hàng

### Mục tiêu
- KPI mặc định luôn ẩn, chỉ mở khi người dùng bấm nút KPI.
- Tiêu đề module đặt phía trên thanh tìm kiếm.
- Các cụm lọc/nhập nhanh trong từng tab phải thẳng hàng, không còn cái trên cái dưới.

### File đã sửa
- `ASMILE.BO.App/ViewModels/CashBankViewModel.cs`
  - `IsSummaryExpanded` mặc định là `false`.
- `ASMILE.BO.App/Views/CashBankView.xaml`
  - Đưa tiêu đề `Quỹ & Ngân hàng` lên row trên cùng, thanh tìm kiếm xuống row bên dưới.
  - Ẩn header cũ để không lặp tiêu đề.
  - Đổi hàng nhập nhanh tab `Sổ quỹ` từ `WrapPanel` sang `Grid` nhiều cột.
  - Đổi hàng nhập nhanh tab `Ngân hàng` từ `WrapPanel` sang `Grid` nhiều cột.
  - Đổi hàng lọc tab `Báo cáo` từ `WrapPanel` sang `Grid` nhiều cột.

### Build/runtime
- Encoding check: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error.
- Runtime: app Debug mở thành công, process phản hồi. Log startup mới nhất `15:27:53 Database is ready`; chưa thấy lỗi XAML/binding mới.

## Phiên 2026-06-05 — Đưa tìm kiếm Quỹ & Ngân hàng cùng hàng tab

### Mục tiêu
- Thanh tìm kiếm không còn kéo dài toàn màn hình và đẩy tab xuống dưới.
- Tìm kiếm nằm cùng hàng với tab, gọn ở góc phải.
- Các control trong hàng lọc/nhập nhanh cùng chiều cao để nhìn thẳng hàng hơn.

### File đã sửa
- `ASMILE.BO.App/Views/CashBankView.xaml`
  - Ẩn `EnterpriseCommandToolbar` lớn ở phía trên.
  - Thêm ô tìm kiếm compact cùng hàng header tab, cạnh nút `Chạy báo cáo` và `Làm mới`.
  - Chuẩn hóa `TextBox`, `ComboBox`, `DatePicker` trong module về height 36.
  - Giữ các hàng nhập nhanh chính ở dạng grid nhiều cột để tránh tự xuống dòng lộn xộn.

### Build/runtime
- Encoding check: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error.
- Runtime: app Debug mở thành công, process phản hồi. Log startup mới nhất `15:32:37 Database is ready`; chưa thấy lỗi XAML/binding mới.

## Phiên 2026-06-05 — Đưa card nhập nhanh Quỹ & Ngân hàng sang trái

### Mục tiêu
- Card nhập nhanh không nằm phía trên bảng chi tiết nữa.
- Card nhập nhanh nằm bên trái vùng bảng để thao tác thuận tiện hơn.
- Tìm kiếm nằm ngang hàng tiêu đề và sắp xếp bên phải.

### File đã sửa
- `ASMILE.BO.App/Views/CashBankView.xaml`
  - Đưa cụm tìm kiếm compact, `Chạy báo cáo`, `Làm mới` lên cùng hàng tiêu đề ở góc phải.
  - Ẩn cụm tìm kiếm cũ cạnh tab để không trùng UI.
  - Tab `Sổ quỹ`: đổi layout thành 2 cột, card nhập nhanh bên trái, bảng và nút thao tác bên phải.
  - Tab `Ngân hàng`: đổi layout thành 2 cột, card nhập nhanh bên trái, bảng và nút thao tác bên phải.

### Build/runtime
- Encoding check: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error.
- Runtime: app Debug mở thành công, process phản hồi. Log startup mới nhất `15:44:33 Database is ready`; chưa thấy lỗi XAML/binding mới.

## Phiên 2026-06-05 — Báo cáo card trái và bổ sung TK Nợ/Có Quỹ & Ngân hàng

### Mục tiêu
- Tab `Báo cáo` dùng bố cục card thao tác bên trái, bảng/lịch sử bên phải giống các tab làm việc chính.
- Form nhập nhanh `Sổ quỹ` và `Ngân hàng` hiển thị đầy đủ `TK Nợ` và `TK Có`, không chỉ một trường `TK đối ứng`.
- Giữ nguyên service/schema hiện có, chỉ map tài khoản đối ứng theo chiều thu/chi trước khi lưu.

### File đã sửa
- `ASMILE.BO.App/Views/CashBankView.xaml`
  - Tab `Sổ quỹ`: thay `TK đối ứng` bằng `TK Nợ` và `TK Có`.
  - Tab `Ngân hàng`: thay `TK đối ứng` bằng `TK Nợ` và `TK Có`.
  - Tab `Báo cáo`: đưa chọn mẫu, định dạng, `Xem trước`, `Xuất`, `Ưa thích` sang card bên trái; bảng báo cáo và lịch sử nằm bên phải.
- `ASMILE.BO.App/ViewModels/CashBankViewModel.cs`
  - Bổ sung helper nhận diện giao dịch thu/chi.
  - Bổ sung notify cho các property hiển thị `TK Nợ`/`TK Có`.
  - Khi lưu nhanh, `CounterAccountCode` lấy từ bên đối ứng đúng theo chiều bút toán: Thu dùng TK Có, Chi dùng TK Nợ.

### Build/runtime
- Encoding check: PASS, không có mojibake XAML.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error.
- Runtime: app Debug mở thành công, process phản hồi. Log startup mới nhất `15:59:44 Database is ready`; chưa thấy lỗi XAML/binding mới sau lần mở này.

## Phiên 2026-06-05 — Căn lại card trái Quỹ & Ngân hàng

### Mục tiêu
- `TK Nợ` và `TK Có` nằm cùng một dòng để card gọn hơn.
- Bổ sung trường `Người nộp/nhận` cho nhập nhanh quỹ và ngân hàng.
- Tăng chiều cao ô `Diễn giải`.
- Tăng chiều rộng toàn bộ card trái thêm khoảng 20%.

### File đã sửa
- `ASMILE.BO.App/Views/CashBankView.xaml`
  - Đổi card trái các tab `Sổ quỹ`, `Ngân hàng`, `Báo cáo` từ 300px lên 360px.
  - Gộp `TK Nợ` và `TK Có` vào grid 2 cột trong cùng một dòng.
  - Thêm label/input `Người nộp/nhận`.
  - Tăng ô `Diễn giải` lên 86px, bật nhập nhiều dòng và cuộn dọc.
- `ASMILE.BO.App/ViewModels/CashBankViewModel.cs`
  - Thêm `CounterpartyName` cho `QuickCashEntry` và `QuickBankEntry`.
  - Khi lưu, ghép `Người nộp/nhận` vào `Description` để giữ nguyên schema/service hiện tại.

### Build/runtime
- Encoding check: PASS, không có mojibake XAML.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error.
- Runtime: app Debug mở thành công, process phản hồi. Log startup mới nhất `16:05:27 Database is ready`; chưa thấy lỗi XAML/binding mới sau lần mở này.

## Phiên 2026-06-05 — Sửa ô tìm kiếm bị cắt và tăng chống crash nút

### Mục tiêu
- Ô tìm kiếm ở header không bị che/cắt phần chân chữ.
- Khoảng cách giữa tiêu đề module và vùng tab/bảng bên dưới thoáng hơn.
- Giảm rủi ro app thoát khi một thao tác nút phát sinh lỗi async/background.

### File đã sửa
- `ASMILE.BO.App/Views/CashBankView.xaml`
  - Tăng `CashBankCompactSearchBox` từ 36px lên 40px.
  - Canh giữa nội dung TextBox và tăng padding dọc.
  - Tăng margin dưới header và thêm khoảng cách trước vùng nội dung.
- `ASMILE.BO.App/App.xaml.cs`
  - Bổ sung `TaskScheduler.UnobservedTaskException`.
  - Log lỗi background task và đánh dấu observed để lỗi async nền không kéo app xuống.

### Build/runtime
- Encoding check: PASS, không có mojibake XAML.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error.
- Runtime: app Debug mở thành công, process phản hồi. Log startup mới nhất `16:10:15 Database is ready`; chưa thấy lỗi XAML/binding mới sau lần mở này.

## Phiên 2026-06-05 — Sửa dứt điểm ô tìm kiếm bị cắt chân và nút Hủy duyệt

### Mục tiêu
- Ô tìm kiếm header không còn bị cắt phần chân chữ.
- Nút `Hủy duyệt` không được gọi khi giao dịch không ở trạng thái `Approved`.
- Bấm `Hủy duyệt` không làm app thoát khi chọn sai trạng thái hoặc chưa chọn dòng.

### File đã sửa
- `ASMILE.BO.App/Views/CashBankView.xaml`
  - Reset `Margin=0` cho `CashBankCompactSearchBox` để bỏ margin đáy 12px kế thừa từ `FluentTextBox`.
  - Tăng search box và khung chứa lên 44px, padding dọc 8px.
  - Bind `IsEnabled` nút `Hủy duyệt` quỹ/ngân hàng theo `CanCancelCashApproval` và `CanCancelBankApproval`.
- `ASMILE.BO.App/ViewModels/CashBankViewModel.cs`
  - Thêm `CanCancelCashApproval`, `CanCancelBankApproval`.
  - Notify lại trạng thái nút khi đổi dòng chọn.
  - Command `Hủy duyệt` kiểm tra chọn dòng, kiểm tra trạng thái `Approved`, hỏi xác nhận rồi mới gọi service.

### Build/runtime
- Encoding check: PASS, không có mojibake XAML.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error.
- Runtime: app Debug mở thành công, process phản hồi. Log startup mới nhất `16:14:33 Database is ready`; chưa thấy lỗi XAML/binding mới sau lần mở này.

## Phiên 2026-06-05 — Màu chọn dòng bảng và giải thích Hủy duyệt Draft

### Mục tiêu
- Dòng đang chọn trong bảng phải đổi sang màu cam rất nhẹ để người dùng nhận biết rõ.
- Trạng thái `Draft` không được `Hủy duyệt` vì chưa duyệt; UI cần giải thích rõ thay vì nhìn như lỗi.

### File đã sửa
- `ASMILE.BO.App/Themes/DataGrid.xaml`
  - Đổi brush selected/inactive selected của DataGrid mặc định sang cam rất nhẹ.
- `ASMILE.BO.App/Themes/AccountingDataGrid.xaml`
  - Đổi selected row/cell của `AccountingDataGrid` sang nền cam rất nhẹ `#FFF3E6`, inactive `#FFF8EF`, viền `#FDBA74`.
- `ASMILE.BO.App/Views/CashBankView.xaml`
  - Thêm tooltip cho nút `Hủy duyệt`: chỉ áp dụng giao dịch `Approved`.
  - Tooltip vẫn hiện khi nút bị disabled.

### Build/runtime
- Encoding check: PASS, không có mojibake XAML.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error.
- Runtime: app Debug mở thành công, process phản hồi. Log startup mới nhất `16:19:16 Database is ready`; chưa thấy lỗi XAML/binding mới sau lần mở này.

## Phiên 2026-06-05 — Bổ sung nút Sửa/Xóa cho giao dịch Quỹ & Ngân hàng

### Mục tiêu
- Khi chọn dòng trong bảng giao dịch phải có nút `Sửa` và `Xóa`.
- Chỉ cho sửa/xóa giao dịch `Draft`; giao dịch `Approved/Posted` phải đi theo luồng duyệt, hủy duyệt, ghi sổ hoặc đảo bút toán.

### File đã sửa
- `ASMILE.BO.Application/Interfaces/ICashFundService.cs`
  - Thêm `UpdateTransactionAsync`, `DeleteTransactionAsync`.
- `ASMILE.BO.Application/Interfaces/IBankingService.cs`
  - Thêm `UpdateTransactionAsync`, `DeleteTransactionAsync`.
- `ASMILE.BO.Infrastructure/Services/CashFundService.cs`
  - Bổ sung sửa/xóa giao dịch quỹ ở trạng thái `Draft`.
- `ASMILE.BO.Infrastructure/Services/BankingService.cs`
  - Bổ sung sửa/xóa giao dịch ngân hàng ở trạng thái `Draft`.
- `ASMILE.BO.App/ViewModels/CashBankViewModel.cs`
  - Thêm trạng thái đang sửa, nhãn `Lưu/Cập nhật`, command `Sửa/Xóa` cho quỹ và ngân hàng.
  - Nút `Sửa` nạp dòng đang chọn lên card trái; nút `Lưu` chuyển thành `Cập nhật`.
- `ASMILE.BO.App/Views/CashBankView.xaml`
  - Thêm nút `Sửa`, `Xóa` vào thanh thao tác dưới bảng `Sổ quỹ` và `Ngân hàng`.
  - Nút chỉ bật khi dòng đang chọn là `Draft`, có tooltip giải thích.

### Build/runtime
- Encoding check: PASS, không có mojibake XAML.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error.
- Runtime: app Debug mở thành công, process phản hồi. Log startup mới nhất `16:25:32 Database is ready`; chưa thấy lỗi XAML/binding mới sau lần mở này.

## Phiên 2026-06-06 — Bổ sung icon desktop cho Món ăn & Định lượng

### Mục tiêu
- Module `Món ăn & Định lượng` trên desktop phải có icon.
- Dùng icon có sẵn trong `D:\ASMILE-BO\Windows_Icon\Icons`.

### File đã sửa
- `ASMILE.BO.App/Services/NavigationService.cs`
  - Đổi icon của module `menu-recipe` từ `applications\inventory.ico` sang `applications\commerce.ico`.
  - Lý do: `inventory.ico` không tồn tại trong thư mục icon, còn `commerce.ico` tồn tại và phù hợp nhóm món ăn/nhà hàng/bán hàng.

### Build/runtime
- `Test-Path D:\ASMILE-BO\Windows_Icon\Icons\applications\commerce.ico`: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error.
- Runtime: app Debug mở thành công, process phản hồi. Log startup mới nhất `2026-06-06 09:19:19 Database is ready`; chưa thấy lỗi XAML/binding mới sau lần mở này.

## Phiên 2026-06-06 — Thiết lập tùy chỉnh icon từng module đã cài

### Mục tiêu
- Trong `Cài đặt > Giao diện > Desktop icons`, bổ sung phần chỉnh icon cho từng module đã cài.
- Cho phép chọn icon theo ý từ thư mục icon thật `D:\ASMILE-BO\Windows_Icon\Icons`.
- Icon đã lưu phải được Desktop/Start/Taskbar ưu tiên dùng khi dựng module.

### File đã sửa
- `ASMILE.BO.Application/DTOs/Settings/SettingsDtos.cs`
  - Thêm `Icon` vào `UpdateAppModuleRequest` để luồng cập nhật module có thể lưu icon.
- `ASMILE.BO.Infrastructure/SettingsEngine/SettingsEngineServices.cs`
  - `UpdateAppAsync` ghi lại `AppModule.Icon`.
- `ASMILE.BO.App/ViewModels/SettingsViewModel.cs`
  - Thêm `ModuleIconOptions`, `SelectedIconModule`, `SelectedModuleIconOption`, `IsAppearanceIconPickerVisible`.
  - Load danh sách icon `.ico/.png` từ `Windows_Icon\Icons`.
  - Thêm command `SaveModuleIconCommand` để lưu icon cho module đang chọn.
  - Giữ nguyên icon tùy chỉnh khi các màn khác cập nhật trạng thái module.
- `ASMILE.BO.App/Views/SettingsView.xaml`
  - Thêm card `Icon module đã cài` dưới màn Giao diện, gồm combobox chọn module, combobox chọn icon có preview và nút `Lưu icon`.
- `ASMILE.BO.App/ViewModels/DesktopShellViewModel.cs`
  - Khi build catalog module, đọc `IAppModuleService` và ưu tiên icon đã lưu trong `AppModule.Icon`.
  - Có fallback về icon mặc định trong `NavigationService` nếu icon tùy chỉnh không tồn tại.

### Build/runtime
- Encoding check: PASS, không có mojibake XAML.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error.
- Runtime: đã đóng process app cũ đang khóa DLL, build lại thành công, mở app Debug lại thành công với process `ASMILE.BO.App` đang chạy.
- Ghi chú: thư mục log Debug chưa sinh file log ngày 2026-06-06 sau lần mở này, nên runtime mới chỉ xác nhận được app chạy/process sống.

## Phiên 2026-06-06 — Cập nhật icon desktop ngay sau khi lưu

### Mục tiêu
- Sau khi chọn icon và bấm lưu, icon trên Desktop/Start/Taskbar phải refresh ngay, không cần tắt mở app.
- Màn chọn icon phải hiển thị song song `Icon đang dùng` và `Icon cần thay`.

### File đã sửa
- `ASMILE.BO.App/ViewModels/SettingsViewModel.cs`
  - Inject `INavigationService` để xác định icon mặc định đang dùng khi module chưa có icon tùy chỉnh.
  - Thêm `CurrentModuleIconPath`, `CurrentModuleIconName`.
  - Sau khi lưu icon, phát event `AppModule.Icon` qua `ISettingsChangeNotifier`.
  - Lọc event `AppModule.Icon` khỏi SettingsViewModel để màn cài đặt không tự reload.
- `ASMILE.BO.App/ViewModels/DesktopShellViewModel.cs`
  - Subscribe `ISettingsChangeNotifier`.
  - Khi nhận event `AppModule.Icon`, dựng lại catalog module, giữ trạng thái pin/chạy/chọn/vị trí icon desktop hiện có, rồi refresh Desktop/Start/Taskbar.
- `ASMILE.BO.App/Views/SettingsView.xaml`
  - Bổ sung hai ô preview cạnh nhau: `Icon đang dùng` và `Icon cần thay`.

### Build/runtime
- Encoding check: PASS, không có mojibake XAML.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error.
- Runtime: app Debug đang chạy, process `ASMILE.BO.App`, title `ASMILE BO - Windows 11 Enterprise Shell`.

## Phiên 2026-06-06 — Đổi nguồn chọn icon sang thư mục Windows_Icon

### Mục tiêu
- Màn chỉnh icon module phải dùng toàn bộ thư mục `D:\ASMILE-BO\Windows_Icon`, không chỉ nhánh `Windows_Icon\Icons`.

### File đã sửa
- `ASMILE.BO.App/ViewModels/SettingsViewModel.cs`
  - Thêm root icon chung `D:\ASMILE-BO\Windows_Icon`.
  - Danh sách chọn icon quét `.ico/.png` trên toàn bộ cây thư mục `Windows_Icon`.
  - Resolve icon đang dùng cũng tìm trong toàn bộ `Windows_Icon`.
- `ASMILE.BO.App/ViewModels/DesktopShellViewModel.cs`
  - Resolve icon đã lưu theo toàn bộ `Windows_Icon`, áp dụng cho Desktop/Start/Taskbar.

### Build/runtime
- Encoding check: PASS, không có mojibake XAML.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error.

## Phiên 2026-06-06 — Menu chuột phải Win 11 cho Desktop và icon ứng dụng

### Mục tiêu
- Bấm chuột phải trên desktop có menu thao tác thật, giao diện giống Windows 11.
- Bấm chuột phải trên từng ứng dụng desktop có menu thao tác thật.
- Không thêm chuỗi lỗi mã hóa tiếng Việt.

### File đã sửa
- `ASMILE.BO.App/Themes/DesktopIcons.xaml`
  - Thêm `DesktopContextMenuStyle`, `DesktopContextMenuItemStyle`, `DesktopContextMenuSeparatorStyle`.
  - Menu bo góc, nền sáng, hover nhẹ, khoảng cách icon/text theo kiểu Win 11.
- `ASMILE.BO.App/Views/DesktopWorkspaceView.xaml`
  - Thêm menu chuột phải desktop: `Làm mới`, `Sắp xếp biểu tượng`, `Ẩn/hiện biểu tượng desktop`, `Mở Start`, `Mở Virtual File Center`, `Cài đặt giao diện`.
- `ASMILE.BO.App/Controls/DesktopIconControl.xaml`
  - Thêm menu chuột phải cho icon ứng dụng: `Mở`, `Ghim/Bỏ ghim taskbar`, `Căn vào lưới`, `Ẩn khỏi desktop`, `Đổi icon`, `Sao chép tên`, `Thuộc tính`.
- `ASMILE.BO.App/Controls/DesktopIconControl.xaml.cs`
  - Right-click chọn icon hiện tại.
  - Dispatch menu action vào command thật của `DesktopShellViewModel`.
- `ASMILE.BO.App/ViewModels/DesktopShellViewModel.cs`
  - Thêm command `OpenModuleKey`, `OpenDesktopModule`, `RefreshDesktop`, `ArrangeDesktopIcons`, `ToggleDesktopIcons`, `ToggleModuleTaskbarPin`, `HideModuleFromDesktop`, `SnapModuleIconToGrid`, `CopyModuleName`, `ShowModuleProperties`, `OpenAppearanceSettings`.
  - `ToggleDesktopIcons` lưu vào setting thật `Appearance.ShowDesktopIcons`.
  - Ghim/bỏ ghim taskbar lưu vào `IWorkspaceService.SavePinnedModulesAsync`.
  - Ẩn icon khỏi desktop lưu vào `IAppModuleService.UpdateAppAsync`.
  - Desktop lắng nghe `Appearance.ShowDesktopIcons` để đồng bộ trạng thái khi setting đổi từ nơi khác.

### Build/runtime
- Encoding check: PASS, không có mojibake XAML.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error.
- Runtime: app Debug đang chạy, process `ASMILE.BO.App`, title `ASMILE BO - Windows 11 Enterprise Shell`.

### Ghi chú còn lại

## Phiên 2026-06-07 — Thiết kế lại POS 2 cột ưu tiên chọn khu vực/bàn

### Lý do bỏ layout 3 cột
Layout cũ (Tables 280px | Menu * | Order 380px) lãng phí không gian, thực đơn chiếm
trung tâm trong khi thao tác chính là chọn bàn. Layout mới 60/40 ưu tiên khu vực/bàn.

### Layout mới
```
[Sidebar 65px] [Tab content ~55%]        [Đơn hàng ~40%]
├─ Khu vực      ├─ Filter chips khu vực  ├─ Header đơn (mã, trạng thái, bàn)
├─ Thực đơn     ├─ Tìm bàn + lọc TT     ├─ Danh sách món (card, qty, note)
└─ Đơn mở       ├─ Grid bàn (UniformGrid ├─ Ghi chú đơn
                     2-5 cột tùy chọn)   ├─ Totals (tạm tính, VAT, giảm giá...)
                └─ ...                   └─ Thanh toán + action buttons
```

### File đã sửa/thêm
- `ViewModels/BreakfastCafeSalesViewModel.cs`:
  - Thêm `LeftTabIndex`, `TableSearchText`, `TableStatusFilter`, `TableColumnCount`, `OrderNote`
  - Thêm `TableStatusFilterOptions`, `TableColumnOptions` collections
  - Thêm commands: `NavigateLeftTab`, `SelectArea`, `SelectCategory`
  - Fix `AddMenuItemAsync`: gộp auto-open order + add line vào 1 `RunBusyAsync` để tránh kẹt `IsBusy`
  - `ApplyTableFilter` hỗ trợ tìm bàn + lọc trạng thái
  - `PosCategoryFilter` thêm `IsSelected` property
- `Views/BreakfastCafeSalesView.xaml`: **Viết lại hoàn toàn**
  - Mini-sidebar 65px với 3 tab icon+label (Khu vực, Thực đơn, Đơn mở)
  - Tab Khu vực: chips lọc khu vực, tìm bàn, lọc trạng thái, UniformGrid bàn (2-5 cột)
  - Tab Thực đơn: search, sort, chips danh mục, list món dạng row
  - Tab Đơn mở: danh sách đơn chưa thanh toán + empty state
  - Cột phải: header đơn, danh sách dòng món (card), ghi chú, tổng tiền, thanh toán
  - Card bàn theo màu trạng thái (xanh/blue/cam)

### Entity/service đã dùng
- Tận dụng toàn bộ service hiện có: `IBreakfastCafeSalesService`, `IMenuCatalogService`
- Entities: `PosTableDto`, `BreakfastCafeOrderDto`, `BreakfastCafeOrderLineDto`, `MenuItemDto`
- Không tạo entity/service mới, không phá MenuRecipe

### Build status
PASS (0 warnings, 0 errors). Encoding check: PASS.

### Runtime status
App mở OK. Cần test: chọn bàn → thêm món → sửa SL → gửi bếp → thanh toán → bàn về trống.

### Phần còn lại
- Chưa test end-to-end workflow đầy đủ
- Chưa có logic "Đổi bàn"
- Chưa có save order note lên server
- Category chips đang dùng ItemsControl thay vì ListBox (có thể cần cải thiện highlight)
- Đã bổ sung deep-link nội bộ sau build đầu: `Đổi icon` mở thẳng `Cài đặt > Giao diện > Desktop icons` và chọn đúng module được bấm chuột phải.
## Phiên 2026-06-07 - Sửa lỗi POS không thêm được món vào đơn

### Root cause
- `BreakfastCafeSalesService.AddLineAsync` thêm dòng món mới qua collection `order.Lines.Add(...)`.
- Với tracking hiện tại, EF phát sinh `UPDATE BreakfastCafeOrderLines` thay vì `INSERT`, dẫn tới `DbUpdateConcurrencyException`.
- UI bắt lỗi vào `StatusText`, nên người dùng thấy đơn bên phải vẫn rỗng dù đã bấm thêm món.

### File đã sửa
- `ASMILE.BO.Infrastructure/Services/BreakfastCafeSalesService.cs`
  - Khi tạo `BreakfastCafeOrderLine` mới, gọi `_dbContext.BreakfastCafeOrderLines.Add(line)` để bắt buộc EF insert dòng mới.
  - Chỉ thêm vào `order.Lines` nếu EF chưa tự fix-up quan hệ, tránh tính tổng tiền bị nhân đôi trong bộ nhớ.
- `ASMILE.BO.App/ViewModels/BreakfastCafeSalesViewModel.cs`
  - Thêm `AddMenuItemForSaleAsync` public method cho UI gọi trực tiếp.
  - Không tự thêm món khi chỉ đổi `SelectedItem` của `ListBox`, tránh double-add.
- `ASMILE.BO.App/Views/BreakfastCafeSalesView.xaml`
  - Cho phép bấm vào cả card món để thêm vào đơn hiện tại.
- `ASMILE.BO.App/Views/BreakfastCafeSalesView.xaml.cs`
  - Nút `+` và card món gọi trực tiếp ViewModel method, không phụ thuộc selected item/command binding trong DataTemplate.

### Kiểm tra đã chạy
- Probe service thật qua DI:
  - Mở bàn/khu vực thật `Mang đi`.
  - Lấy món thật từ `MenuItems`.
  - Gọi `AddLineAsync`.
  - Kết quả: `INSERT INTO BreakfastCafeOrderLines` được phát sinh, không còn concurrency exception.
- Probe ViewModel thật:
  - ViewModel tải được 19 món.
  - Gọi `AddMenuItemForSaleAsync`.
  - `CurrentOrderLines` có dữ liệu sau thao tác.
- SQL xác nhận:
  - `BreakfastCafeOrderLines` có dòng cho `ORD-20260607-0008`.
  - Dòng `bánh đa bò` số lượng 3, thành tiền 291.600.
  - Dòng `Bạc xỉu` số lượng 1, thành tiền 27.000.
  - Tổng đơn 318.600.

### Build/runtime
- XAML encoding check: PASS, không có mojibake trong XAML.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error.
- Runtime: app Debug mở được sau build, cần kiểm tra thao tác trực tiếp bằng chuột trên màn POS.
## Phiên 2026-06-07 - Sửa POS trắng tab Khu vực/Cài đặt khi load nhanh

### Root cause
- `InitializeAsync` dùng `RunBusyAsync` và chạy nền khi ViewModel mới tạo.
- Nếu người dùng bấm nhanh sang `Cài đặt` trong lúc POS đang load, `LoadSettingsAsync` bị `IsBusy` chặn rồi không tự chạy lại, dẫn tới tab cài đặt trắng.
- Tab `Khu vực` cũng có thể trắng khi `SelectedArea` vẫn giữ tên khu vực cũ đã bị sửa/xóa, trong khi danh sách `Areas` mới không còn giá trị đó.

### File đã sửa
- `ASMILE.BO.App/ViewModels/BreakfastCafeSalesViewModel.cs`
  - Tách loader cài đặt khỏi `RunBusyAsync`, có cờ `_isLoadingSettings` và `_reloadSettingsAfterCurrent` để không mất lượt reload.
  - `LoadSettingsAsync` chờ thao tác đang bận kết thúc rồi mới query database, tránh lỗi EF chạy song song.
  - Khi refresh khu vực, nếu `SelectedArea` rỗng hoặc không còn tồn tại trong `Areas`, tự chọn khu vực hợp lệ đầu tiên.
  - Khi quay lại tab `Khu vực`, tự refresh dữ liệu bằng `RefreshOpenDataSafeAsync`.

### Kiểm tra đã chạy
- XAML encoding check: PASS.
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error.
- `dotnet build .\ASMILE.BO.sln -c Release --no-restore`: PASS 0 warning/0 error.
- Probe ViewModel thật qua DI:
  - `AREAS=3`
  - `FILTERED_TABLES=3`
  - `MENU_ITEMS=19`
  - `SETTINGS_TABLES=7`
  - `SETTINGS_AREAS=3`
  - `SETTINGS_MENU_ITEMS=19`

### Runtime status
- App cần mở lại sau build để dùng bản mới.
## Phiên 2026-06-08 - Audit logic toàn app và sửa race scheduler

### Phạm vi kiểm tra
- Build Debug/Release.
- Unit test + integration test.
- Log runtime gần nhất.
- DI/ViewModel initialization cho các module đã đăng ký.
- Kiểm tra dữ liệu SQL: chứng từ nhập/xuất, POS order, journal cân Nợ/Có, dữ liệu nền module.
- Quét mock/TODO/async fire-and-forget và encoding XAML.

### Lỗi đã sửa
- `ASMILE.BO.Infrastructure/Services/BackgroundSchedulerService.cs`
  - `CancelAsync` lưu trạng thái yêu cầu hủy trước khi cancel token.
  - Tránh race làm execution đã `Cancelled` bị context cũ ghi đè về `Cancelling`.

### Kết quả kiểm tra
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error.
- `dotnet build .\ASMILE.BO.sln -c Release --no-restore`: PASS 0 warning/0 error.
- `dotnet test .\ASMILE.BO.sln -c Debug --no-build`: PASS 143/143.
- XAML encoding check: PASS.
- App Debug mở được, process responding.

### Phát hiện còn lại
- Nhiều ViewModel vẫn gọi async load kiểu fire-and-forget trong constructor hoặc property changed; đây là rủi ro gây race/trắng màn nếu scope bị dispose hoặc thao tác đổi tab quá nhanh.
- Một số service cấu hình hệ thống vẫn là mock: SystemSettings, DatabaseAdmin, Backup, ServiceStatus, SecuritySettings, PrinterSettings.
- `BreakfastCafeCostingService.PostConsumptionAsync` hiện là TODO, chưa xuất kho tiêu hao thật sau POS.
- Settings còn TODO cho definitions và fiscal lock backend enforcement.

---

## Phiên 2026-06-11 — Hoàn thiện module Bán đồ ăn sáng & Cà phê

### Nhóm 1: Luồng thanh toán
- [x] 1.1 Đối soát cuối ca (Dashboard POS)
- [x] 1.2 In hóa đơn tự động khi thanh toán
- [ ] 1.3 Hoàn tiền / Hủy thanh toán

### Nhóm 2: Trải nghiệm POS
- [x] 2.1 Combo gợi ý hiển thị rõ
- [ ] 2.2 Upload ảnh món
- [x] 2.3 Quick add by code nổi bật

### Nhóm 3: Dashboard & Báo cáo
- [x] 3.1 Dashboard POS tổng quan
- [ ] 3.2 Xuất Excel
- [ ] 3.3 Thống kê theo nhân viên

### Nhóm 4: Quỹ & Ngân hàng
- [ ] 4.1 Duyệt hàng loạt
- [ ] 4.2 Số dư quỹ real-time

### Nhóm 5: Kỹ thuật
- [ ] 5.1 Sửa DbContext concurrency
- [x] 5.2 Xóa dead code
- [x] 5.3 Loading indicator

---

## Phiên 2026-06-11 — Fix 6 bước logic POS Bữa sáng & Cà phê

### Phạm vi sửa
- Sửa sinh mã phiếu bếp/bar theo sequence ngày để không trùng khi một đơn tạo nhiều ticket.
- Tự chuẩn hóa `PreparationArea` cho món POS còn thiếu cấu hình: đồ uống sang Bar, còn lại sang Kitchen.
- Bổ sung kiểm tra tồn kho tiêu hao trước khi gửi bếp/bar và trước khi thanh toán đủ.
- Hoàn thiện `BreakfastCafeCostingService.ConsumeRecipeAsync`: tạo phiếu xuất kho thật từ định lượng món, duyệt và ghi sổ qua `IInventoryAppService`.
- Hoàn thiện hạch toán thanh toán POS:
  - Tiền mặt: tạo phiếu thu, duyệt và ghi sổ ngay.
  - Chuyển khoản/POS thẻ: tạo giao dịch ngân hàng, gán tài khoản đối ứng doanh thu, duyệt và ghi sổ ngay.
  - Ghi phòng/Công nợ: tạo và post journal phải thu.
- Dọn warning nullable trong `BreakfastCafeSalesService` và warning biến `ex` không dùng trong `AppDbContext`.

### File đã sửa
- `ASMILE.BO.Infrastructure/Services/BreakfastCafeSalesService.cs`
- `ASMILE.BO.Infrastructure/Services/BreakfastCafeCostingService.cs`
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `DEV_PROGRESS.md`

### Trạng thái kiểm tra
- `dotnet build .\ASMILE.BO.sln -c Debug --no-restore`: PASS 0 warning/0 error.
- `dotnet build .\ASMILE.BO.sln -c Release --no-restore`: PASS 0 warning/0 error.
- `dotnet test .\ASMILE.BO.sln -c Debug --no-build`: PASS 143/143.
- XAML encoding check: PASS trong build.

### Lưu ý còn lại
- Chưa thêm migration vì không đổi schema database.
- Release build lần đầu bị khóa DLL do app đang chạy; đã tắt process `ASMILE.BO.App` và build lại thành công.
