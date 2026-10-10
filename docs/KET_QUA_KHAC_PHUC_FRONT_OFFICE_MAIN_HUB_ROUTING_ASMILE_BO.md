# KẾT QUẢ KHẮC PHỤC FRONT OFFICE MAIN HUB ROUTING — ASMILE-BO

## 1. Executive Summary

Đã sửa nguyên nhân khiến Desktop mở trực tiếp Reservation Hub. Entry Desktop hiện mở `Front Office`; trong cùng cửa sổ đó, card `Đặt phòng` hiển thị lại Reservation Hub hiện có. Back từ Reservation đóng modal đang hoạt động (nếu có) và quay lại Main Hub trong cùng cửa sổ.

Trạng thái cuối: **CHƯA HOÀN THÀNH** — build, test và EF đều pass; Debug runtime đã mở tới màn hình đăng nhập nhưng smoke sau đăng nhập chưa thể thực hiện vì không tự động hóa bước xác thực.

## 2. Current Incorrect Runtime Flow

Trước khi sửa, catalog có hai entry Desktop:

`front-office` → FrontOfficeHubViewModel

`front-office-reservations` → ReservationHubViewModel

Entry `front-office-reservations` vẫn có `ShowOnDesktop = true`, vì vậy người dùng vẫn bấm shortcut Đặt phòng cũ và vào thẳng Reservation Hub.

## 3. Exact Route Root Cause

Main Hub đã tồn tại và đã đăng ký đúng DI/DataTemplate/route. Sai lệch nằm ở catalog Desktop và cách card Đặt phòng gọi `INavigationService.OpenModuleAsync("front-office-reservations")`, vốn tạo một module window Reservation độc lập.

## 4. Desktop Route Before

`Đặt phòng` (Desktop) → `front-office-reservations` → ReservationHubViewModel.

## 5. Desktop Route After

`Front Office` (Desktop) → `front-office` → FrontOfficeHubViewModel.

`front-office-reservations` vẫn được giữ để định danh Reservation Hub, nhưng bị ẩn khỏi Desktop. Workspace đã lưu route Reservation cũ được chuyển thành `front-office` khi restore.

## 6. Front Office Main Hub

Main Hub là parent layer duy nhất cho Front Office. Header hiển thị:

- `Quản lý khách sạn`
- `Chọn phân hệ để bắt đầu làm việc.`

Khi vào Reservation, Reservation Hub hiện có được đặt làm content bên trong cùng cửa sổ Front Office; không tạo một Reservation Hub hay Window mới.

## 7. Eleven Top-level Cards

1. Đặt phòng
2. Lễ tân
3. Thu ngân
4. Đoàn
5. Nhà buồng
6. Tổng đài
7. Kế toán phải thu
8. Truy vấn thông tin
9. Kinh doanh tiếp thị
10. Báo cáo
11. Night Audit

Chỉ Đặt phòng hoạt động. Mười card còn lại chỉ mở overlay in-app với thông báo: `Chức năng sẽ được triển khai ở sprint tiếp theo.` Không gọi backend, không mở workspace rỗng và không dùng MessageBox.

## 8. Reservation Card Mapping

Card Đặt phòng nhận ReservationHubViewModel từ Front Office scope, gán vào `ActiveWorkspace`, và giữ nguyên Property Context hiện hữu.

## 9. Reservation Hub Reuse

Reservation Hub, card, command, modal workflow và backend Reservation không bị redesign hoặc thay đổi. Chỉ bổ sung callback parent để Back quay về Front Office Main Hub.

## 10. Back Navigation

Luồng đã cài đặt:

`Front Office Main Hub` → `Đặt phòng` → `Reservation Hub` → `Back` → `Front Office Main Hub`

Back gọi `ModalService.Close()` trước, sau đó trả content về Main Hub. Window Front Office không bị đóng và không tạo navigation history/window lặp.

## 11. Coming Soon Behavior

Coming Soon dùng overlay trong FrontOfficeHubView, không gọi Navigation hoặc backend.

## 12. Window Title Behavior

Window Front Office có title `Front Office` ở Main Hub. Khi Reservation được mở trong content hiện hữu, title chuyển thành `Đặt phòng`; Back trả title về `Front Office`.

## 13. Property Context Preservation

Front Office Main Hub và Reservation Hub cùng dùng IFrontOfficePropertyContextHost đang sống trong shell. Không hard-code PropertyId, không clear/chọn lại Property Context khi chuyển vào hoặc quay ra Reservation.

## 14. Tests

Focused tests pass: **27/27**.

Bổ sung/điều chỉnh coverage:

- FO-MAIN-U-001/004: route Desktop resolves FrontOfficeHubViewModel, Reservation route không hiện Desktop.
- FO-MAIN-U-003/005/006/009/010/011/012: Reservation cùng window, Back quay Main Hub, window không đóng, context giữ nguyên, không duplicate window, title đổi đúng.
- FO-MAIN-U-007/008: Coming Soon không navigation/backend và mở safe overlay.

Full unit tests: **287/287 pass**.

Full integration tests: **20/20 pass**.

## 15. Debug Runtime Screenshot/Result

Debug executable đã được khởi động:

`D:\ASMILE-BO\ASMILE.BO.App\bin\Debug\net8.0-windows\ASMILE.BO.App.exe`

Runtime dừng ở LoginPanel trước khi có một phiên đăng nhập. Không tự động hóa thao tác xác thực; vì vậy chưa thể ghi nhận kết quả live sau đăng nhập.

## 16. Release Runtime Result

Release build pass. Release smoke sau đăng nhập chưa chạy vì cùng điều kiện xác thực runtime.

## 17. Files Created

- `KET_QUA_KHAC_PHUC_FRONT_OFFICE_MAIN_HUB_ROUTING_ASMILE_BO.md`

## 18. Files Modified

- `ASMILE.BO.App/Models/AppModuleInfo.cs`
- `ASMILE.BO.App/Services/NavigationService.cs`
- `ASMILE.BO.App/ViewModels/FrontOfficeHubViewModel.cs`
- `ASMILE.BO.App/ViewModels/ReservationHubViewModel.cs`
- `ASMILE.BO.App/Views/FrontOfficeHubView.xaml`
- `ASMILE.BO.Tests.Unit/FrontOfficeHubViewModelTests.cs`
- `ASMILE.BO.Tests.Unit/NavigationScopeRegressionTests.cs`

## 19. Remaining Issues

Không có lỗi build/test/EF liên quan đến thay đổi này. Còn thiếu đúng hai smoke test có xác thực: Debug và Release sau khi đăng nhập.

## 20. Final Status

**CHƯA HOÀN THÀNH**

Điều kiện còn thiếu để đạt HOÀN THÀNH: runtime trực tiếp phải chứng minh `Desktop → Front Office Main Hub → Đặt phòng → Reservation Hub → Back → Front Office Main Hub` ở cả Debug và Release.
