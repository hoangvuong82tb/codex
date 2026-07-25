# STAY PROFILE CALENDAR FINAL VISUAL REFINEMENT REPORT
# Ngày: 2026-07-24
# Status: DONE

## 1. Nguyên nhân placeholder bị chồng

Trước sửa: TextBlock "Chọn ngày" nằm PHÍA SAU DatePickerTextBox (Background="Transparent"). WPF DatePickerTextBox có watermark nội bộ "Select a date". Cả hai text đều hiển thị → chồng nhau.

## 2. Cách xử lý watermark

- TextBlock "Chọn ngày" chuyển lên PHÍA TRƯỚC DatePickerTextBox (trong cùng Grid cell)
- TextBlock có Background="White" → che hoàn toàn watermark "Select a date" của WPF
- IsHitTestVisible="False" → click xuyên qua đến DatePickerTextBox
- DataTrigger: khi SelectedDate != null → collapse TextBlock → hiện ngày đã chọn
- Khi SelectedDate == null → TextBlock visible → hiện "Chọn ngày"
- Không còn text chồng, không còn "Select a date"

## 3. Kích thước popup trước/sau

| Element | Trước | Sau |
|---------|-------|-----|
| Day button | 34×34 | 32×32 |
| Cell (grid) | 36px | 34px |
| Nav button | 30×30 | 30×30 |
| Padding | 12px | 12px |
| Popup width ước tính | ~318px | ~286px |
| CornerRadius | 14 | 12 |

## 4. Header trước/sau

| Property | Trước | Sau |
|----------|-------|-----|
| Title font size | 14px | 13px |
| Column layout | Auto/*/Auto | 34/*/34 |
| Arrow font size | 18px | 16px |
| Margin bottom | 8px | 8px |

## 5. Day button trước/sau

| Property | Trước | Sau |
|----------|-------|-----|
| Size | 34×34 | 32×32 |
| CornerRadius | 8 | 7 |
| FontSize | 12px | 12px |
| FocusVisualStyle | (mặc định) | {x:Null} |
| Hover foreground | #1D4ED8 | #2563EB |
| Today foreground | #2F6BFF | #2563EB |
| Inactive foreground | #CBD5E1 | #B8C3D1 + Opacity 0.7 |

## 6. Selected state trước/sau

Trước: chỉ có border chấm (FocusVisualStyle mặc định WPF)
Sau:
- Nền #2F6BFF
- Chữ trắng SemiBold
- Không dotted border (FocusVisualStyle="{x:Null}")
- CornerRadius 7

## 7. Footer trước/sau

| Button | Trước (W×H) | Sau (W×H) |
|--------|------------|-----------|
| Hôm nay | 82×32 | 76×32 |
| Ngày mai | 88×32 | 82×32 |
| Xóa ngày | 84×32 | 76×32 |
| Gap | 8px | 6px |
| CornerRadius | 8 | 7 |

| State | Blue buttons | Red button |
|-------|-------------|------------|
| Background | #EEF4FF | #FFF1F2 |
| Foreground | #234E9A | #D64545 |
| Hover | #DFEAFF | #FFE4E6 |

## 8. Vị trí popup

- Popup mở trực tiếp dưới DatePicker (Placement="Bottom")
- Calendar chiếm khoảng 286px ngang
- Không wrapper Border trong Popup (Calendar style tự cung cấp border + shadow)

## 9. File đã sửa

- `Themes/FrontOfficeDesignSystem.xaml` — CalendarDayButton, CalendarItem, Calendar, footer
- `Views/StayProfileWindow.xaml` — watermark overlay + DataTrigger

## 10. Build result

- Build: PASS (0 errors, 84 pre-existing warnings)

## 11. Runtime result

- App launched: OK
- Placeholder "Chọn ngày" hiển thị đúng, không chồng text
- Chọn ngày → hiển thị dd/MM/yyyy
- Popup compact và cân đối
- Selected state nền xanh + chữ trắng rõ ràng
- Không dotted border
- Footer nhỏ gọn, căn giữa
- Quick actions hoạt động

## 12-13. Ảnh trước/sau

(Chụp trực tiếp từ app đang chạy)

## 14. Xác nhận giữ nguyên logic Codex

- PART_Button: unchanged
- PART_Popup: unchanged
- CalendarStyle wiring: unchanged
- Quick action Click handlers: unchanged
- SelectedDate binding: unchanged
- Filter logic: untouched

## 15. Xác nhận không sửa backend

- ViewModel: untouched
- Service: untouched
- Database: untouched

## 16. Xác nhận không sửa Banquet

- Banquet views: untouched
- Nhà hàng tiệc: untouched

## KẾT LUẬN: FINAL VISUAL REFINEMENT DONE
