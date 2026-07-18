# Banquet Mock Data Audit — ASMILE-BO

> Ngày kiểm tra: 2026-07-10 | Tập trung: Mock data, hard-code, placeholder, fake service

---

## 1. Tổng quan

Tổng số vị trí mock/hard-code: **16** vị trí, chia thành 4 nhóm:

| Nhóm | Số lượng | Mức nguy hiểm |
|---|---|---|
| Hard-code UI (cần bỏ ngay) | 7 | P1-P2 |
| Hard-code danh mục (cần chuyển DB) | 5 | P1-P2 |
| Seed data hợp lệ | 3 | P3 (OK) |
| Default value hợp lý | 1 | P3 (OK) |

---

## 2. Chi tiết từng vị trí mock

### MOCK-01 — Alert badge count hard-code

| Thuộc tính | Giá trị |
|---|---|
| **File** | `ASMILE.BO.App\Views\BanquetRestaurantView.xaml` |
| **Dòng** | 124 |
| **Nội dung** | `Text="3"` (số lượng alert hiển thị trên badge) |
| **Loại** | Hard-code UI |
| **Mức độ** | **P2** — Hiển thị sai số lượng cảnh báo thực tế |
| **Cách tái hiện** | Mở Dashboard, thấy badge luôn hiện "3" |
| **Cách sửa** | Bind vào `ImportantAlertCount` property đã có sẵn trong VM |

```xml
<!-- Hiện tại -->
<Border Width="16" Height="16" CornerRadius="8" Background="{StaticResource DangerBrush}" Margin="-8,-8,0,0">
  <TextBlock Text="3" FontSize="10" Foreground="White" FontWeight="Bold" HorizontalAlignment="Center" VerticalAlignment="Center"/>
</Border>

<!-- Sửa thành -->
<Border Width="16" Height="16" CornerRadius="8" Background="{StaticResource DangerBrush}" Margin="-8,-8,0,0">
  <TextBlock Text="{Binding ImportantAlertCount}" FontSize="10" Foreground="White" FontWeight="Bold" HorizontalAlignment="Center" VerticalAlignment="Center"/>
</Border>
```

---

### MOCK-02 — Username hard-code

| Thuộc tính | Giá trị |
|---|---|
| **File** | `ASMILE.BO.App\Views\BanquetRestaurantView.xaml` |
| **Dòng** | 125 |
| **Nội dung** | `Text="Administrator"` |
| **Loại** | Hard-code UI |
| **Mức độ** | **P2** — Username giả, không phản ánh user thật |
| **Cách sửa** | Bind vào current user từ `ViewModelBase` hoặc auth service |

---

### MOCK-03 — Room equipment status hard-code

| Thuộc tính | Giá trị |
|---|---|
| **File** | `ASMILE.BO.App\Views\BanquetRestaurantView.xaml` |
| **Dòng** | 650-652 |
| **Nội dung** | `"Âm thanh 8/8"`, `"Ánh sáng 8/8"`, `"LED 7/8"`, `"Máy lạnh 8/8"` |
| **Loại** | Hard-code UI |
| **Mức độ** | **P2** — Dữ liệu giả |
| **Cách sửa** | Tạo entity `BanquetRoomEquipment` hoặc bỏ phần này nếu chưa có tính năng |

---

### MOCK-04 — Quick note hard-code

| Thuộc tính | Giá trị |
|---|---|
| **File** | `ASMILE.BO.App\Views\BanquetRestaurantView.xaml` |
| **Dòng** | 652 |
| **Nội dung** | `"Kiểm tra ánh sáng sân khấu Sảnh Diamond trước 15h 15/06"` |
| **Loại** | Hard-code UI |
| **Mức độ** | **P3** — Ghi chú mẫu, không ảnh hưởng nghiệp vụ |
| **Cách sửa** | Bind vào dữ liệu thật hoặc để trống |

---

### MOCK-05 — Calendar dates hard-code

| Thuộc tính | Giá trị |
|---|---|
| **File** | `ASMILE.BO.App\Views\BanquetRestaurantView.xaml` |
| **Dòng** | 663-669 |
| **Nội dung** | `"14/06"`, `"15/06"`, `"16/06"`, `"17/06"`, `"18/06"`, `"19/06"`, `"20/06"` |
| **Loại** | Hard-code UI |
| **Mức độ** | **P1** — Calendar hiển thị ngày cố định, không thay đổi theo thời gian thực |
| **Cách tái hiện** | Mở tab Lịch phòng, thấy luôn là tuần 14-20/06 |
| **Cách sửa** | Bind vào `OverviewCalendarRows` đã có sẵn trong ViewModel (được build từ `BuildOverviewCalendar()`) |

---

### MOCK-06 — Revenue pie chart % hard-code

| Thuộc tính | Giá trị |
|---|---|
| **File** | `ASMILE.BO.App\Views\BanquetRestaurantView.xaml` |
| **Dòng** | 694 |
| **Nội dung** | `"Tiệc cưới 42%"`, `"Sinh nhật 21%"`, `"Hội nghị 18%"` |
| **Loại** | Hard-code UI |
| **Mức độ** | **P2** — Số liệu giả trên báo cáo |
| **Cách sửa** | Tính từ dữ liệu thật trong `RefreshDashboardCollections()` |

---

### MOCK-07 — Settings form hoàn toàn hard-code

| Thuộc tính | Giá trị |
|---|---|
| **File** | `ASMILE.BO.App\Views\BanquetRestaurantView.xaml` |
| **Dòng** | 699-714 |
| **Nội dung** | Toàn bộ form: `"Nhà hàng Tiệc (Banquet)"`, `"BNQ001"`, `"123 Nguyễn Văn Linh, Q7, TP.HCM"`, `"(028) 1234 5678"`, `"info@banquet.vn"`, `"www.banquet.vn"` |
| **Loại** | Hard-code UI |
| **Mức độ** | **P1** — Settings không lưu được, luôn hiển thị dữ liệu mẫu |
| **Cách sửa** | Tạo entity `BanquetSettings` + migration + service CRUD + bind vào VM properties |

---

### MOCK-08 — PaymentMethodOptions hard-code list

| Thuộc tính | Giá trị |
|---|---|
| **File** | `ASMILE.BO.App\ViewModels\RestaurantViewModels.cs` |
| **Dòng** | 48 |
| **Nội dung** | `["Cash", "Transfer", "Card", "Debt"]` |
| **Loại** | Hard-code danh mục |
| **Mức độ** | **P2** — Nếu muốn thêm phương thức thanh toán mới phải sửa code |
| **Cách sửa** | Load từ bảng danh mục hoặc config |

---

### MOCK-09 — EventTypeOptions hard-code list

| Thuộc tính | Giá trị |
|---|---|
| **File** | `ASMILE.BO.App\ViewModels\RestaurantViewModels.cs` |
| **Dòng** | 44 |
| **Nội dung** | `["Wedding", "Conference", "Birthday", "Meeting", "Party", "Other"]` |
| **Loại** | Hard-code danh mục |
| **Mức độ** | **P2** — Không thể thêm loại sự kiện mới nếu không sửa code |
| **Cách sửa** | Tạo bảng `EventTypes`, load từ DB |

---

### MOCK-10 — EventStatusOptions hard-code list

| Thuộc tính | Giá trị |
|---|---|
| **File** | `ASMILE.BO.App\ViewModels\RestaurantViewModels.cs` |
| **Dòng** | 45 |
| **Nội dung** | `["Draft", "Hold", "Confirmed", "Preparing", "Completed", "Cancelled"]` |
| **Loại** | Hard-code danh mục |
| **Mức độ** | **P1** — Trạng thái cứng, không có state machine, không kiểm tra transition |
| **Cách sửa** | Tạo bảng `EventStatuses` với `FromStatuses` JSON, load từ DB |

---

### MOCK-11 — ServiceTypeOptions hard-code list

| Thuộc tính | Giá trị |
|---|---|
| **File** | `ASMILE.BO.App\ViewModels\RestaurantViewModels.cs` |
| **Dòng** | 46 |
| **Nội dung** | `["MC", "Singer", "LedScreen", "Decoration", "SoundLight", "Gateway", "PhotoBooth", "Transport", "Other"]` |
| **Loại** | Hard-code danh mục |
| **Mức độ** | **P2** — Không thể thêm loại dịch vụ mới |
| **Cách sửa** | Tạo bảng `ServiceTypes`, load từ DB |

---

### MOCK-12 — ServiceTypeNames dictionary hard-code

| Thuộc tính | Giá trị |
|---|---|
| **File** | `ASMILE.BO.App\ViewModels\BanquetRestaurantViewModel.Partial.cs` |
| **Dòng** | 223-234 |
| **Nội dung** | Dictionary map ServiceType → tên tiếng Việt |
| **Loại** | Hard-code map |
| **Mức độ** | **P3** — Display name cứng |
| **Cách sửa** | Load từ bảng `ServiceTypes` nếu có |

---

### MOCK-13 — SeedDefaultExtraServicesAsync (Alacarte service)

| Thuộc tính | Giá trị |
|---|---|
| **File** | `ASMILE.BO.Infrastructure\Services\RestaurantServices.cs` |
| **Dòng** | 1387-1400 |
| **Nội dung** | 11 extra services: LED, MC, SINGER, SOUND, LIGHT, DECOR, FLOWER, PROJECTOR, VIPROOM, SERVICECHARGE, PHOTOBOOTH |
| **Loại** | **Seed data hợp lệ** |
| **Mức độ** | P3 — Đây là dữ liệu khởi tạo danh mục, chấp nhận được |
| **Ghi chú** | Chỉ thêm nếu chưa tồn tại (kiểm tra `existingCodes`) |

---

### MOCK-14 — AddDefaultChecklist (9 items)

| Thuộc tính | Giá trị |
|---|---|
| **File** | `ASMILE.BO.Infrastructure\Services\RestaurantServices.cs` |
| **Dòng** | 4494-4495 |
| **Nội dung** | 9 checklist items: "Đã chốt thực đơn", "Đã nhận cọc", "Đã xác nhận khách", "Đã chuẩn bị phòng", "Đã chuẩn bị âm thanh", "Đã chuẩn bị màn LED", "Đã chuẩn bị nhân sự", "Đã chuẩn bị sân khấu", "Đã hoàn tất thanh toán" |
| **Loại** | **Seed data hợp lệ** |
| **Mức độ** | P3 — Template checklist mặc định khi tạo event mới, chấp nhận được |

---

### MOCK-15 — AddDefaultTimeline (5 items)

| Thuộc tính | Giá trị |
|---|---|
| **File** | `ASMILE.BO.Infrastructure\Services\RestaurantServices.cs` |
| **Dòng** | 4498-4523 |
| **Nội dung** | 5 timeline items: "Đón khách", "Khai tiệc", "Phục vụ món khai vị", "Nghi thức chính", "Kết thúc tiệc" |
| **Loại** | **Seed data hợp lệ** |
| **Mức độ** | P3 — Template timeline mặc định, chấp nhận được |

---

### MOCK-16 — Default eventDate = DateTime.Today.AddDays(1)

| Thuộc tính | Giá trị |
|---|---|
| **File** | `ASMILE.BO.App\ViewModels\RestaurantViewModels.cs` |
| **Dòng** | 9577 |
| **Nội dung** | `eventDate = DateTime.Today.AddDays(1)` |
| **Loại** | **Default value hợp lý** |
| **Mức độ** | P3 — Giá trị mặc định cho form tạo mới, hợp lý |

---

## 3. Các vị trí KHÔNG mock (đã kết nối backend thật)

Các phần sau **đã gọi service thật**, không dùng mock:

| Chức năng | Method | Service |
|---|---|---|
| Load danh sách sự kiện | `_service.GetEventsAsync()` | `BanquetRestaurantService` |
| Load dashboard | `_service.GetDashboardAsync()` | `BanquetRestaurantService` |
| Lưu sự kiện | `_service.SaveEventAsync()` | `BanquetRestaurantService` |
| Load chi tiết sự kiện | `_service.GetEventDetailAsync()` | `BanquetRestaurantService` |
| Load danh sách phòng | `_service.GetRoomsAsync()` | `BanquetRestaurantService` |
| Lưu phòng | `_service.SaveRoomAsync()` | `BanquetRestaurantService` |
| Thêm dịch vụ | `_service.SaveServiceLineAsync()` | `BanquetRestaurantService` |
| Xóa dịch vụ | `_service.DeleteServiceLineAsync()` | `BanquetRestaurantService` |
| Gắn thực đơn | `_service.AttachMenuPackageAsync()` | `BanquetRestaurantService` |
| Gỡ thực đơn | `_service.DetachMenuPackageAsync()` | `BanquetRestaurantService` |
| Đặt cọc | `_service.AddDepositAsync()` | `BanquetRestaurantService` |
| Tạo hợp đồng | `_service.CreateContractAsync()` | `BanquetRestaurantService` |
| Toggle checklist | `_service.ToggleChecklistAsync()` | `BanquetRestaurantService` |
| Thêm timeline item | `_service.SaveTimelineItemAsync()` | `BanquetRestaurantService` |
| Load menu packages | `_service.GetMenuPackagesAsync()` | `BanquetRestaurantService` |
| Load thực đơn món | `_menuCatalog.GetMenuItemsAsync()` | `MenuCatalogService` |

---

## 4. Thống kê

| Phân loại | Số lượng |
|---|---|
| Tổng vị trí mock/hard-code | 16 |
| Cần sửa ngay (P0-P1) | 8 |
| Cần sửa trước khi release (P2) | 5 |
| Chấp nhận được (P3) | 3 |
| File bị ảnh hưởng nhiều nhất | `BanquetRestaurantView.xaml` (6 vị trí) |

---

*Báo cáo được tạo bởi Claude Code analysis ngày 2026-07-10*
