# Banquet HTML UI Refactor Report

## Mục tiêu

Đổi phần shell giao diện của module `Nhà hàng Tiệc` theo bố cục từ file `C:\Users\webvi\Desktop\app_nha_hang_tiec_tabs.html`, nhưng giữ nguyên logic và binding nghiệp vụ hiện có trong ViewModel.

## Phạm vi đã làm

- Refactor `D:\ASMILE-BO\ASMILE.BO.App\Views\BanquetRestaurantView.xaml`.
- Đổi nền tổng thể sang phong cách ERP sáng, card bo góc lớn hơn và có shadow nhẹ.
- Đổi sidebar sang tông xanh đậm, mở rộng chiều ngang, thêm brand block ở đầu.
- Đổi topbar theo layout HTML:
  - khối tiêu đề bên trái,
  - ô tìm kiếm ở giữa,
  - cụm trạng thái/người dùng/nút làm mới bên phải.
- Đổi style tab chi tiết sang dạng pill thay vì gạch chân.

## Chủ ý kỹ thuật

- Không thay đổi command/binding chính của `BanquetRestaurantViewModel`.
- Không tách lại luồng nghiệp vụ hoặc sửa service trong lần này.
- Giữ nguyên các panel/tab nghiệp vụ bên trong để tránh làm vỡ logic đang có.

## Kiểm tra

- `dotnet build D:\ASMILE-BO\ASMILE.BO.sln -c Debug` thành công.
- Đã mở app từ:
  - `D:\ASMILE-BO\ASMILE.BO.App\bin\Debug\net8.0-windows\ASMILE.BO.App.exe`

## Ghi chú

Lần chỉnh này mới đưa phần “vỏ” giao diện sát HTML tham chiếu. Nếu cần làm khớp sâu hơn với HTML ở từng panel nghiệp vụ trung tâm, nên tiếp tục refactor riêng phần `Events detail workspace` ở lần sau để tránh sửa quá nhiều trong một đợt.
