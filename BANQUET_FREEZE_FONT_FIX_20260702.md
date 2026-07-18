# Banquet Freeze And Font Fix 2026-07-02

## Mục tiêu
- Sửa treo app khi chọn sự kiện tiệc, mở tab hợp đồng và các tab chi tiết trong module Nhà hàng tiệc.
- Dọn lỗi hiển thị tiếng Việt bị mã hóa sai trong giao diện `BanquetRestaurantView`.

## Nguyên nhân chính
- `BanquetRestaurantViewModel` tự gán lại `SelectedEvent` trong lúc tải chi tiết sự kiện.
- Việc này kích hoạt `OnSelectedEventChanged` nhiều lần và tạo vòng lặp nạp chi tiết.
- File `BanquetRestaurantView.xaml` chứa nhiều chuỗi tiếng Việt bị hỏng encoding nên giao diện hiển thị sai.

## File đã chỉnh
- `D:\ASMILE-BO\ASMILE.BO.App\ViewModels\RestaurantViewModels.cs`
- `D:\ASMILE-BO\ASMILE.BO.App\ViewModels\BanquetRestaurantViewModel.Partial.cs`
- `D:\ASMILE-BO\ASMILE.BO.App\Views\BanquetRestaurantView.xaml`

## Nội dung sửa
- Thêm cờ chặn vòng lặp `SelectedEvent` khi đồng bộ dữ liệu chi tiết tiệc.
- Tránh nạp chi tiết lặp trong `RefreshPreservingSelectionAsync`.
- Chuẩn hóa lại text giao diện Banquet bằng entity Unicode/XML để không phụ thuộc encoding terminal.
- Giữ nguyên layout/sidebar đã làm trước đó, chỉ sửa logic ổn định và text hiển thị.

## Kiểm tra
- `dotnet build D:\ASMILE-BO\ASMILE.BO.sln -c Release`
- Kết quả: build thành công, 0 warning, 0 error.

## Ghi chú
- Đã mở lại app sau khi build để test trực tiếp.
