# Banquet Mock UI Report

## Mục tiêu

Chuyển module `Nhà hàng Tiệc` sang chế độ mock-first để chốt đúng giao diện trước, chưa yêu cầu dữ liệu thật hay luồng nghiệp vụ đầy đủ.

## Thay đổi chính

- Thay toàn bộ file `D:\ASMILE-BO\ASMILE.BO.App\Views\BanquetRestaurantView.xaml`.
- Dựng mock layout tĩnh theo phong cách ERP:
  - sidebar xanh đậm,
  - topbar có tìm kiếm và user info,
  - step workflow ngang,
  - cột trái danh sách sự kiện,
  - cột giữa chi tiết sự kiện và tabs nghiệp vụ,
  - cột phải lịch phòng và cảnh báo,
  - footer checklist và action buttons.

## Chủ ý

- Không nối dữ liệu thật ở bước này.
- Ưu tiên mock đúng bố cục và mật độ thông tin theo yêu cầu.
- Giữ app chạy ổn để tiếp tục tinh chỉnh giao diện ở vòng sau.

## Kiểm tra

- `dotnet build D:\ASMILE-BO\ASMILE.BO.sln -c Debug` thành công.
- Đã mở app từ:
  - `D:\ASMILE-BO\ASMILE.BO.App\bin\Debug\net8.0-windows\ASMILE.BO.App.exe`
