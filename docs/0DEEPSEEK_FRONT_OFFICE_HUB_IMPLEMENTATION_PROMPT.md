# ASMILE-BO — DEEPSEEK IMPLEMENTATION PROMPT
## FRONT OFFICE HUB — APPROVED UI

Đọc Project Context hiện tại và các report mới nhất. Sau đó mở file:

`FRONT_OFFICE_HUB_REFERENCE.html`

File HTML là chuẩn trực quan bắt buộc. Không dùng WebView, không tích hợp HTML vào app; chỉ dùng để đối chiếu khi implement WPF.

# PHẠM VI

Chỉ rebuild UI `FrontOfficeHubView`.

Không sửa backend, Core, Domain, Application, Infrastructure, Reservation backend, HotelStay, Posting Engine, Financial Engine, database hoặc migration.

Không mở rộng sang Workspace hay nghiệp vụ mới.

# KIẾN TRÚC BẮT BUỘC

Front Office Hub
→ Reservation Hub / Reception Hub
→ Workspace
→ Modal

Không mở `ReservationWorkspace` trực tiếp khi vào Front Office.

# GIAO DIỆN PHẢI GIỐNG HTML

## Loại bỏ hoàn toàn

- Sidebar
- Logo khách sạn lớn
- Ô tìm kiếm
- Footer
- Badge trạng thái
- Mô tả trong card
- Live metrics
- DataGrid
- Form
- Toolbar thừa

## Header

Căn giữa.

Tiêu đề: `Quản lý khách sạn`

Dòng phụ: `Trung tâm điều hành các nghiệp vụ tiền sảnh và lưu trú`

Không đặt logo lớn phía trên.

## Card grid

Đủ 11 card, thứ tự:

1. Đặt phòng
2. Lễ tân
3. Thu ngân
4. Đoàn
5. Nhà buồng
6. Tổng đài
7. Kế toán phải thu
8. Truy vấn thông tin
9. Marketing
10. Báo cáo
11. Night Audit

Desktop: 4–4–3. Hàng cuối căn giữa.

Toàn bộ grid nằm giữa màn hình, có `MaxWidth`, không kéo kín cửa sổ.

## Kích thước tham chiếu

- Card: 270–300 px rộng, 115–130 px cao
- CornerRadius: 16–18 px
- Padding: 22–28 px
- Gap ngang: 26–30 px
- Gap dọc: 28–34 px
- Icon box: 70–76 px
- Icon box CornerRadius: 16–18 px
- Icon thực: 38–44 px
- Font card: khoảng 20–26 px, Semibold

## Bố cục trong card

Icon và chữ phải cùng một hàng ngang:

`[ICON]   [TÊN CHỨC NĂNG]`

Icon bên trái, chữ bên phải, căn giữa theo trục dọc.

Không đặt icon trên chữ.

Không có badge, mô tả, mũi tên hay trạng thái.

Tên dài phải ưu tiên một dòng:

- Kế toán phải thu
- Truy vấn thông tin

## Màu icon

- Đặt phòng: xanh dương
- Lễ tân: xanh ngọc
- Thu ngân: cam
- Đoàn: tím
- Nhà buồng: xanh lá
- Tổng đài: xanh lam
- Kế toán phải thu: cyan
- Truy vấn thông tin: xanh dương đậm
- Marketing: hồng
- Báo cáo: tím
- Night Audit: navy

Icon trắng trên nền gradient. Ưu tiên vector resource hiện có, không dùng emoji, không thêm thư viện mới khi chưa cần.

## Card style

- Nền trắng/trắng xanh rất nhạt
- Border rất nhẹ
- Shadow mềm
- Glow màu nhẹ phía dưới
- Không viền đen
- Không nền xám

Hover: nâng 3–5 px, shadow/glow tăng nhẹ.

Pressed: hạ nhẹ 1–2 px.

Có keyboard focus và cursor hand.

## Background

Gradient trắng–xanh rất nhạt, có wave mềm ở góc. Dựng bằng XAML Brush/Path/Ellipse nếu cần. Không dùng ảnh khách sạn hoặc ảnh stock.

## Responsive

- Rộng: 4 cột
- Trung bình: 3 cột
- Hẹp: 2 cột
- Rất hẹp: 1 cột

Không co card đến mức cắt chữ. Có ScrollViewer dọc khi cần.

# NAVIGATION

`Đặt phòng` → `ReservationHub`

`Lễ tân` → `ReceptionHub`

Không mở Workspace trực tiếp.

Các card chưa có màn hình không được gọi route lỗi hoặc làm app crash. Dùng command an toàn/notification service hiện có, nhưng không hiển thị badge trên UI.

# MVVM

Không đặt navigation trong code-behind.

Dùng ICommand và NavigationService hiện có.

Không tạo navigation framework mới.

Không hardcode View vào Window.

# FILE CÓ THỂ SỬA

Ưu tiên:

- `FrontOfficeHubView.xaml`
- `FrontOfficeHubViewModel.cs`
- ResourceDictionary UI liên quan
- DataTemplate nếu cần
- Navigation UI tối thiểu nếu card đang lỗi

Có thể tạo `FrontOfficeHubStyles.xaml`.

# KIỂM THỬ

1. Clean bin/obj.
2. Rebuild.
3. Chạy đúng một instance.
4. Mở app và Front Office.
5. So sánh với HTML.
6. Kiểm tra 4–4–3 và hàng cuối căn giữa.
7. Kiểm tra icon/chữ cùng hàng.
8. Kiểm tra hover, pressed, focus.
9. Resize.
10. Bấm Đặt phòng.
11. Bấm Lễ tân.
12. Bấm các card còn lại.
13. Kiểm tra log.

# NGHIỆM THU

- App và Front Office mở bình thường
- Đủ 11 card
- Không sidebar/logo lớn/search/footer/badge/mô tả
- Card nhỏ gọn, icon và chữ cùng hàng
- Bố cục 4–4–3
- Hàng cuối căn giữa
- Không viền đen/nền xám
- Resize không vỡ
- Đặt phòng mở Reservation Hub
- Lễ tân mở Reception Hub
- Card khác không gây exception
- Build 0 error
- Không XAML exception/binding error nghiêm trọng
- Không sửa backend
- Có ảnh runtime đối chiếu

# REPORT

Tạo:

`FRONT_OFFICE_HUB_APPROVED_DESIGN_IMPLEMENTATION_REPORT.md`

Báo cáo gồm root cause/hiện trạng, file sửa/tạo, kích thước thực tế, layout, style, icon, background, animation, responsive, navigation, runtime, build, log, ảnh chụp và xác nhận không sửa backend.

Hoàn thành thì DỪNG. Không tiếp tục Phase khác.
