# Banquet Sidebar Update 2026-07-01

## Mục tiêu

Bổ sung danh mục `sidebar` cho module `Nhà hàng Tiệc` để đồng bộ trải nghiệm với `Nhà hàng À La Carte` và `Bán đồ ăn sáng & cà phê`.

## Thay đổi đã làm

- Thêm thanh điều hướng bên trái trong `ASMILE.BO.App/Views/BanquetRestaurantView.xaml`.
- Sidebar bind trực tiếp với `SelectedTabIndex` và `NavigateTabCommand`.
- Giữ nguyên logic service và dữ liệu hiện có, chỉ tổ chức lại giao diện điều hướng.
- Tinh chỉnh giao diện sidebar theo ảnh mẫu:
  - card điều hướng bản rộng hơn
  - bo góc lớn hơn
  - trạng thái chọn dùng nền xanh nhạt, viền xanh rất nhẹ
  - phần chân sidebar có caption ngắn để tạo cảm giác app shell Win11 hơn
- Tách riêng `sidebar cấp module` và `tab ngang chi tiết sự kiện`:
  - sidebar không còn điều khiển trực tiếp tab ngang
  - thêm `SelectedDetailTabIndex` cho phần chi tiết sự kiện
  - `Phòng tiệc` đã có workspace riêng: danh sách phòng bên trái, form thêm/sửa phòng bên phải
  - `Báo cáo` và `Cài đặt` đã được tách vùng hiển thị riêng để không chen vào luồng sự kiện
- Các mục đã bổ sung:
  - `Thông tin`
  - `Hợp đồng`
  - `Đặt cọc`
  - `Checklist`
  - `Timeline`
  - `Dịch vụ`
  - `Thực đơn`

## Kiểm tra

- Cần đóng app `ASMILE.BO.App` trước khi build lại để tránh lỗi khóa file `ASMILE.BO.App.exe`.
