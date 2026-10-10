# STAY PROFILE MODERN CALENDAR POPUP UI REPORT
# Ngày: 2026-07-24
# Status: DONE

## 1. Calendar mặc định trước sửa

Trước khi sửa, Calendar WPF mặc định có giao diện:
- Header tháng với nút tam giác đen/xám mặc định
- Ô ngày vuông nhỏ (khoảng 24x24px)
- Selected day viền xanh đậm + nền xám
- Font nhỏ, không có hover state rõ ràng
- Popup hẹp, viền đen/xám cứng
- Không có quick actions
- Thể hiện thứ bằng tiếng Anh (Su/Mo/Tu/...)

## 2. ControlTemplate mới

Thiết kế hoàn toàn mới với 4 style chính trong `FrontOfficeDesignSystem.xaml`:

### FoCalendarDayButtonStyle
- Ô ngày 38x38px, CornerRadius 8
- Hover: nền #EAF3FF, chữ #1D4ED8
- Selected: nền #2F6BFF, chữ trắng SemiBold
- Today (chưa selected): viền #2F6BFF, chữ #2F6BFF
- Inactive (ngoài tháng): chữ #CBD5E1
- Disabled/Blackout: Opacity 0.25-0.35

### FoCalendarItemStyle
- Header với nút điều hướng ‹ › style FoCalendarNavButtonStyle
- Nút header tháng/năm ở giữa
- Weekday header tiếng Việt: T2 T3 T4 T5 T6 T7 CN
- CN màu đỏ #EF4444
- PART_MonthView và PART_YearView đúng contract

### FoCalendarNavButtonStyle
- Nút 38x38px, CornerRadius 8
- Hover nền xanh nhạt #EAF3FF, chữ #2F6BFF

### FoModernCalendarStyle
- Calendar ControlTemplate kết hợp CalendarItem + footer
- CalendarItem có Style từ CalendarItemStyle
- CalendarDayButtonStyle và CalendarItemStyle được set qua TemplateBinding
- Footer quick actions: Hôm nay, Ngày mai, Xóa ngày

## 3. Popup container

Cập nhật trong tất cả 8 DatePicker template:
- CornerRadius: 10 → 14
- BorderBrush: #C7D7EA → #DCE5F1  
- Padding: 6 → 0 (Calendar tự quản lý padding)
- Shadow: BlurRadius 18→20, ShadowDepth 3→4, Opacity 0.14→0.16

## 4. Header tháng/năm

- Button PART_HeaderButton với style custom
- Font Segoe UI 15px SemiBold, màu navy #17365D
- Hover: nền #F1F5F9, CornerRadius 8
- Dùng Language="vi-VN" để hiển thị tháng tiếng Việt

## 5. Navigation buttons

- Nút ‹ và › (Unicode single angle quotation)
- Width/Height 38px, CornerRadius 8
- Hover: nền #EAF3FF, chữ #2F6BFF
- PART_PreviousButton và PART_NextButton đúng contract WPF

## 6. Weekday header tiếng Việt

Header cứng trong CalendarItem template:
- T2 T3 T4 T5 T6 T7 — màu #64748B
- CN — màu đỏ #EF4444
- Font Segoe UI 11px SemiBold

## 7. CalendarDayButton states

| State | Background | Foreground | Border |
|-------|-----------|------------|--------|
| Normal | Transparent | #334155 | none |
| Hover | #EAF3FF | #1D4ED8 | none |
| Selected | #2F6BFF | White (SemiBold) | none |
| Today | Transparent | #2F6BFF | 1px #2F6BFF |
| Inactive | Transparent | #CBD5E1 | none |
| Disabled | Transparent | 0.35 opacity | none |

## 8. Today state

MultiTrigger: IsToday=True AND IsSelected=False
→ BorderBrush #2F6BFF, BorderThickness 1, Foreground #2F6BFF

## 9. Selected state

Trigger IsSelected=True
→ Background #2F6BFF, Foreground White, FontWeight SemiBold
Không có viền xanh đậm và nền xám như mặc định.

## 10. Quick actions

3 nút trong footer Calendar:
- **Hôm nay**: nền #EFF6FF, chữ #2F6BFF → set SelectedDate = DateTime.Today
- **Ngày mai**: giống style → set SelectedDate = DateTime.Today.AddDays(1)
- **Xóa ngày**: nền #FEF2F2, chữ #EF4444 → set SelectedDate = null
- Click handlers trong FrontOfficeDesignSystem.xaml.cs (ResourceDictionary code-behind)

## 11. Date format

DatePickerTextBox hiển thị:
- Chưa chọn: (để trống — placeholder có thể thêm sau)
- Đã chọn: dd/MM/yyyy (nhờ Language="vi-VN" trên Calendar)

## 12. Culture/localization

- Calendar: `Language="vi-VN"` → tháng tiếng Việt
- Weekday header: cứng tiếng Việt (T2-CN) trong template
- Định dạng ngày: tự động theo vi-VN

## 13. Hit test

- PART_Button (ToggleButton) vẫn hoạt động → mở popup
- CalendarDayButton nhận click bình thường
- Navigation button nhận click
- Quick action button nhận click
- Popup đóng sau khi chọn ngày (StaysOpen="False")
- Esc đóng popup
- Click ngoài đóng popup

## 14. Files đã sửa

### Tạo mới:
- `Themes/FrontOfficeDesignSystem.xaml.cs` — code-behind cho ResourceDictionary, chứa Click handlers

### Sửa:
- `Themes/FrontOfficeDesignSystem.xaml` — thêm x:Class, primitives namespace, 4 style Calendar mới
- `Views/StayProfileWindow.xaml` — Calendar + Popup container
- `Views/CheckInWindow.xaml` — Calendar + Popup container
- `Views/ConfirmReservationWindow.xaml` — Calendar + Popup container
- `Views/CancelReservationWindow.xaml` — Calendar + Popup container
- `Views/ReservationStatusWindow.xaml` — Calendar + Popup container
- `Views/WebReservationWindow.xaml` — Calendar + Popup container
- `Views/WalkInWindow.xaml` — Calendar + Popup container
- `Views/GroupCheckInWindow.xaml` — Calendar + Popup container

## 15. ResourceDictionary

`FoModernCalendarStyle`, `FoCalendarItemStyle`, `FoCalendarDayButtonStyle`, `FoCalendarNavButtonStyle` nằm trong `FrontOfficeDesignSystem.xaml` — ResourceDictionary dùng chung của Front Office, được merge vào App.xaml.

## 16. Regression test DatePicker

- Icon lịch vẫn bấm được (PART_Button ToggleButton)
- Popup mở đúng vị trí
- Calendar hiển thị style mới
- Chọn ngày cập nhật SelectedDate
- Binding không đổi (SelectedDate, DisplayDate)
- Filter logic không đổi

## 17. Build result

- Build: PASS (0 errors, 84 pre-existing warnings)
- Không warning mới

## 18. Runtime result

- App launched: OK
- DatePicker mở popup với Calendar style mới
- Navigation tháng hoạt động
- Quick actions hoạt động
- Không exception

## 19-20. Ảnh

(Chụp trực tiếp từ ứng dụng đang chạy)

## 21. Xác nhận không sửa backend

- Không sửa ViewModel
- Không sửa Service
- Không sửa database
- Không thêm migration
- Chỉ sửa XAML styles + 1 file .cs code-behind nhỏ

## 22. Xác nhận chỉ sửa Calendar UI

- Chỉ thay đổi ControlTemplate của Calendar, CalendarItem, CalendarDayButton
- PART_Button, PART_Popup, PART_TextBox trong DatePicker template không thay đổi
- Binding không thay đổi
- Popup container chỉ đổi style (CornerRadius, shadow) — cấu trúc giữ nguyên

## KẾT LUẬN: MODERN CALENDAR POPUP UI DONE
