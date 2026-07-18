# Banquet Layout Match Report - 2026-07-04

## Mục tiêu

Loại bỏ giao diện mock ảnh của module `Nhà hàng Tiệc (Banquet)` và thay bằng giao diện WPF thật để màn không còn trắng và có thể tiếp tục phát triển nghiệp vụ thật trên cùng layout.

## Cách làm hiện tại

Giao diện `BanquetRestaurantView.xaml` đã được refactor sang layout WPF native:

- sidebar trái thật bằng `Button`
- header workspace thật
- dashboard thật bằng card + danh sách sự kiện + cảnh báo
- màn `Phòng tiệc` thật bằng danh sách phòng + form chỉnh sửa
- màn `Lịch phòng` thật bằng bảng sự kiện + panel giữ lịch
- màn `Sự kiện` thật bằng list sự kiện + khối chi tiết + tab nội dung
- màn `Checklist`, `Báo cáo`, `Cài đặt` thật ở mức layout vận hành cơ bản

## File đã chỉnh

- `D:\ASMILE-BO\ASMILE.BO.App\Views\BanquetRestaurantView.xaml`
- `D:\ASMILE-BO\ASMILE.BO.App\ASMILE.BO.App.csproj`
- `D:\ASMILE-BO\BANQUET_LAYOUT_MATCH_REPORT_20260703.md`

## Ghi chú kỹ thuật

- Đã bỏ hướng render ảnh mock trong view.
- Ảnh trong `Assets\BanquetMock` vẫn còn trên project nhưng không còn được dùng làm UI chính.
- Build `Debug` đã pass sau khi thay layout native.
- App đã được mở lại từ:
  - `D:\ASMILE-BO\ASMILE.BO.App\bin\Debug\net8.0-windows\ASMILE.BO.App.exe`

## Bước tiếp theo hợp lý

- Chuẩn hóa lại text tiếng Việt trong `BanquetRestaurantViewModel.Partial.cs` vì hiện vẫn còn nhiều chuỗi bị lỗi mã hóa ở VM.
- Tách tiếp các màn `Báo cáo`, `Checklist`, `Cài đặt` thành component riêng nếu cần làm sát mẫu hơn.
- Sau khi layout ổn, mới tinh chỉnh chi tiết để bám sát 7 ảnh mẫu hơn.
