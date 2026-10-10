# Dashboard Redesign Report

## Mục tiêu

Thiết kế lại tab `Tổng quan` của module `Nhà hàng À La Carte` theo hướng executive dashboard thật, không mockup, không fake data, không tính số nghiệp vụ trong ViewModel.

## Phần đã thay đổi

### 1. Contract dữ liệu dashboard

- Mở rộng `AlacarteDashboardDto` từ DTO KPI đơn giản thành DTO tổng hợp.
- Bổ sung các nhóm dữ liệu:
  - `Kpis`
  - `RoomStatuses`
  - `ServingBoard`
  - `TodayTimeline`
  - `RevenueChart`
  - `QuickReports`
  - `Alerts`
  - `RecentActivities`

### 2. Tổng hợp dữ liệu thật trong service

- Viết lại `GetDashboardAsync(...)` trong `AlacarteRestaurantService`.
- Dữ liệu đang lấy từ:
  - `DiningRooms`
  - `DiningTables`
  - `TableReservations`
  - `RestaurantQuotes`
  - `AlacarteOrders`
  - `RestaurantOrderTables`
  - `AlacarteOrderLine`
  - `RestaurantOrderExtraService`
  - `RestaurantDeposits`
  - `RestaurantPreparationTickets`
- Loại bỏ hướng vá số cục bộ ở ViewModel.

### 3. ViewModel

- Thêm auto refresh dashboard mỗi `30 giây` bằng `DispatcherTimer`.
- Dùng `DashboardRevenueSeries` để map series chart từ DTO sang control UI.
- Thêm command điều hướng từ dashboard:
  - mở phòng/khu vực
  - mở đơn đang phục vụ
  - mở timeline booking
  - mở cảnh báo
  - mở recent activity
- Ẩn logic dashboard cũ dựa trên `RefreshAlerts()` và `UpdateDashboardFromLocal()`.

### 4. Giao diện WPF

- Thay block tab `Tổng quan` trong `AlacarteRestaurantView.xaml`.
- Bố cục mới gồm:
  - KPI header
  - tình hình nhà hàng theo card/progress
  - bàn đang phục vụ dạng card
  - quick actions
  - panel cảnh báo
  - timeline đặt hôm nay
  - chart doanh thu theo giờ
  - báo cáo nhanh
  - recent activity
- Ẩn nút `Khởi tạo DL mẫu` khỏi phần dashboard.

### 5. Chart nội bộ

- Nâng `MetricBarChart` để support:
  - `InSeriesTitle`
  - `OutSeriesTitle`
  - `HasOutSeries`
- Cho phép dùng chart 1 series cho dashboard doanh thu mà không phải thêm thư viện ngoài.

## Những gì cố ý chưa làm ở v1

- Chưa hiển thị widget `nhân viên nào đang phục vụ`.
- Lý do: schema hiện tại chưa có dữ liệu phân công phục vụ theo bàn/phòng đủ tin cậy; không suy đoán từ `CreatedBy` hoặc `PreparedBy`.

## Kiểm tra đã chạy

- `dotnet build D:\ASMILE-BO\ASMILE.BO.sln`
- Kết quả: `Build succeeded`

## Ghi chú

- Trong lúc build đã phải tắt tiến trình `ASMILE.BO.App` đang mở vì app khóa DLL output.
- Sau khi build xong, app có thể mở lại để test giao diện và luồng click trực tiếp.
## Bo sung 29/06/2026

- Dialog `Mo phong phuc vu` da them `Gio bat dau` va `Gio ket thuc`.
- Luong luu mo phong da truyen xuyen ViewModel -> request -> service cho don walk-in.
- `ReservationDateTime` duoc dong bo theo gio bat dau, gio ket thuc duoc luu metadata noi bo va map lai ra DTO.
- Panel `Canh bao` tren dashboard da doi sang layout mac dinh 2 cot.
