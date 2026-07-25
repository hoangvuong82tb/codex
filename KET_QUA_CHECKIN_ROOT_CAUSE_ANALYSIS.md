# Kết quả phân tích lỗi runtime Check-in

## 1. Executive Summary

Đã tái hiện trực tiếp theo luồng **Front Office → Reception → Check-in** trên Debug sau khi đăng nhập. Generic dialog không phải lỗi DI, Property Context hay route. Có **hai exception độc lập** cùng thoát khỏi luồng khởi tạo modal và bị `App.OnDispatcherUnhandledException` đổi thành thông báo chung.

1. XAML của modal tham chiếu resource không tồn tại: `ErrorBrush`.
2. Database runtime `ASMILE_BO` thiếu bảng `fo.LuuTruKhachSan`, trong khi Check-in luôn truy vấn danh sách `PendingArrival` khi mở.

Đã sửa đúng lỗi XAML bằng một resource Front Office có sẵn. Không thay đổi database, không tạo migration và không thay đổi nghiệp vụ. Check-in **chưa thể xác nhận hoạt động live** cho tới khi migration hiện hữu được áp dụng có kiểm soát vào database runtime.

## 2. Runtime Exception

### Exception 1 — XAML

- Type: `System.Windows.Markup.XamlParseException`
- Message: `Provide value on 'System.Windows.StaticResourceExtension' threw an exception.`
- Inner exception: `System.Exception: Cannot find resource named 'ErrorBrush'. Resource names are case sensitive.`
- File/control/property: `ASMILE.BO.App/Views/CheckInStayModalView.xaml`, `TextBlock.Foreground`
- XAML location emitted by WPF: line 1, position 1090 (file is currently minified to one line)
- Source: `PresentationFramework`
- Target site evidenced by the stack: `System.Windows.Markup.XamlReader.RewrapException`

### Exception 2 — SQL

- Type: `Microsoft.Data.SqlClient.SqlException` (`0x80131904`)
- Message: `Invalid object name 'fo.LuuTruKhachSan'.`
- Inner exception: none
- SQL Server error: Number 208, State 1, Class 16
- Failing table: `[fo].[LuuTruKhachSan]`
- Source component: Microsoft SQL Client / EF Core SQL Server provider
- Target site evidenced by the stack: `Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync`

## 3. Stack Trace

### XAML

```text
System.Windows.Markup.XamlParseException: 'Provide value on
'System.Windows.StaticResourceExtension' threw an exception.' Line number '1'
and line position '1090'.
 ---> System.Exception: Cannot find resource named 'ErrorBrush'.
    at System.Windows.StaticResourceExtension.ProvideValue(IServiceProvider)
    at MS.Internal.Xaml.Runtime.ClrObjectRuntime.CallProvideValue(...)
    at System.Windows.Markup.XamlReader.RewrapException(...)
    at System.Windows.FrameworkTemplate.LoadTemplateXaml(...)
```

### SQL

```text
Microsoft.Data.SqlClient.SqlException: Invalid object name 'fo.LuuTruKhachSan'.
  at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(...)
  at ASMILE.BO.Infrastructure.FrontOffice.Stay.EfHotelStayRepository.SearchAsync(...)
      in EfHotelStayRepository.cs:line 38
  at ASMILE.BO.Application.FrontOffice.Stay.SearchStaysQueryHandler.HandleAsync(...)
      in HotelStayCqrsHandlers.cs:line 20
  at ASMILE.BO.App.Services.ReceptionUiService.GetPendingArrivalsAsync(...)
      in ReceptionUiService.cs:line 79
  at ASMILE.BO.App.ViewModels.CheckInStayModalViewModel.LoadAsync(...)
      in CheckInStayModalViewModel.cs:line 25
  at ASMILE.BO.App.ViewModels.CheckInStayModalViewModel.InitializeAsync(...)
      in CheckInStayModalViewModel.cs:line 19
  at ASMILE.BO.App.Services.CheckInStayModalService.OpenAsync(...)
      in CheckInStayModalService.cs:line 4
  at ASMILE.BO.App.ViewModels.ReceptionHubViewModel.<AddCheckIn>b__37_0()
      in ReceptionHubViewModel.cs:line 130
```

## 4. Root Cause

`ReceptionHubViewModel` tạo `CheckInStayModalViewModel` qua `CheckInStayModalService`. `InitializeAsync` gọi `LoadAsync`, rồi gửi `SearchStaysQuery` với trạng thái `PendingArrival`. Handler gọi `EfHotelStayRepository.SearchAsync`, truy vấn mapped table `[fo].[LuuTruKhachSan]`.

Truy vấn đúng database runtime xác nhận:

```text
Database: ASMILE_BO
fo.LuuTruKhachSan: MISSING
```

History của database chỉ có migration Front Office đến `20260718014449_AddFrontOfficeHotelRoomMasterData`; source hiện có migration `20260718092912_AddFrontOfficeHotelStayFoundation`, là migration tạo nền bảng Hotel Stay. Database startup policy đang là `MigrationMode=Disabled` và `AllowApplyPendingMigrations=false`, do đó ứng dụng không tự cập nhật schema.

Property Context không phải nguyên nhân: phiên live đã có Branch `MAIN - Main Branch`; lỗi xảy ra khi EF chạy truy vấn table, trước khi kết quả lọc theo property có thể được trả về.

DI cũng không phải nguyên nhân: service, ViewModel, mediator, handler và repository đều đã resolve/được gọi thành công; stack trace đi tới SQL Server mới thất bại.

## 5. Vì sao Generic Dialog xuất hiện

`CheckInStayModalService.OpenAsync` chỉ gọi `Close()` rồi `throw`; `CheckInStayModalViewModel.LoadAsync` không bắt `SqlException`. `AsyncRelayCommand` chuyển exception ra Dispatcher.

Handler toàn cục `ASMILE.BO.App/App.xaml.cs` bắt tại `OnDispatcherUnhandledException` (line 95), ghi log và hiển thị generic dialog tại line 138:

```text
Thao tác không thành công. Vui lòng kiểm tra nhật ký hệ thống.
```

## 6. Exact Fix

Đã sửa đúng lỗi XAML:

```xaml
Foreground="{StaticResource ErrorBrush}"
```

thành token đã tồn tại trong `Themes/FrontOfficeDesignSystem.xaml`:

```xaml
Foreground="{StaticResource FO.Status.ErrorBrush}"
```

Khắc phục SQL còn lại không phải sửa code: cần áp dụng **migration hiện hữu** bắt đầu từ `20260718092912_AddFrontOfficeHotelStayFoundation` vào database `ASMILE_BO` theo quy trình được chủ sở hữu database phê duyệt. Không được tạo migration mới, và chưa áp migration trong lần xử lý này.

## 7. Vì sao Unit Test PASS nhưng Live FAIL

Unit test không truy cập SQL Server runtime. Nó kiểm tra domain/application trong môi trường cô lập nên không thể phát hiện schema thật của database đang chạy ứng dụng.

## 8. Vì sao LocalDB PASS nhưng ASMILE_BO FAIL

Integration test Front Office tạo database LocalDB tạm và gọi `Database.MigrateAsync()`. Vì vậy schema test có bảng Hotel Stay. Runtime dùng database `ASMILE_BO` khác, đang tắt migration tự động và thiếu migration Hotel Stay đã có trong source.

## 9. Files Modified

- `ASMILE.BO.App/Views/CheckInStayModalView.xaml` — thay một resource key XAML không tồn tại bằng resource Front Office hiện có.
- `KET_QUA_CHECKIN_ROOT_CAUSE_ANALYSIS.md` — báo cáo này.

Không sửa backend Reservation/Reception, DI, navigation, Property Context, business rules, database hoặc migration.

## 10. Verification

| Kiểm tra | Kết quả |
| --- | --- |
| Tái hiện Debug trước sửa | PASS — thu được XAML exception và SQL exception live |
| Xác minh database runtime read-only | PASS — `[fo].[LuuTruKhachSan]` thiếu |
| Build Debug `dotnet build ASMILE.BO.sln --no-restore` | PASS, 0 warning, 0 error |
| XAML encoding check | PASS |
| Build Release `dotnet build ASMILE.BO.sln -c Release --no-restore` | PASS, 0 warning, 0 error |
| Live Check-in sau sửa | BLOCKED — database chưa có bảng Hotel Stay |

## 11. Remaining Issues

Một dependency schema còn thiếu trên database runtime. Sau khi migration hiện hữu được áp dụng, cần mở lại Debug và Release, đăng nhập, đi tới Reception → Check-in để xác nhận modal hiển thị list hoặc empty state mà không có generic dialog.

## 12. Final Status

**CHƯA HOÀN THÀNH** — root cause đã xác định và lỗi XAML đã sửa, nhưng không thể ghi `HOÀN THÀNH` khi database runtime vẫn thiếu `[fo].[LuuTruKhachSan]` và live Check-in chưa thể load.
