# STAY PROFILE CALENDAR UI COMPACT REFINEMENT REPORT
# Date: 2026-07-24
# Status: DONE

## 1. Critical Bug Fix

**StayProfileWindow Popup was empty** — the linter had removed the Calendar element from the Popup, leaving a self-closing `<Popup .../>` tag with no content. Restored the Calendar as direct Popup child (Calendar style provides its own border/shadow, no wrapper needed).

## 2. Kích thước popup trước/sau

| Element | Before | After |
|---------|--------|-------|
| CalendarDayButton | 38×38px | 34×34px |
| Grid cell (PART_MonthView) | 38px | 36px |
| Nav button | 34×34px (style), 38×38 (header override) | 30×30px |
| CalendarItem padding | 16px | 12px |
| Est. popup width | ~330px | ~296px (7×36 + 12×2 padding + margins) |

## 3. Header spacing

| Property | Before | After |
|----------|--------|-------|
| Header margin-bottom | 12px | 8px |
| Title font size | 15px | 14px |
| Nav button size | 38×38 | 30×30 (from style) |
| Nav arrow font size | 22px | 18px |

## 4. Weekday spacing

| Property | Before | After |
|----------|--------|-------|
| Weekday row margin-bottom | 8px | 4px |
| Font size | 11px | 11px (unchanged) |

## 5. Day button size

| Property | Before | After |
|----------|--------|-------|
| Width/Height | 38×38 | 34×34 |
| Font size | 13px | 12px |
| CornerRadius | 8 | 8 (unchanged) |
| Margin | 1px | 1px (unchanged) |

## 6. Selected state

Already correct from previous implementation:
- Background: #2F6BFF
- Text: White, SemiBold
- No dotted focus border

## 7. Footer action size

| Button | Before | After |
|--------|--------|-------|
| Hôm nay | 88×34 | 82×32 |
| Ngày mai | 88×34 | 88×32 |
| Xóa ngày | 88×34 | 84×32 |
| Padding | 12,6 | 10,5 |

**Colors refined:**

| State | Hôm nay / Ngày mai | Xóa ngày |
|-------|-------------------|----------|
| Foreground | #2F6BFF → #234E9A | #EF4444 → #D64545 |
| Background | #EFF6FF → #EEF4FF | #FEF2F2 → #FFF2F2 |
| Hover | #DBEAFE → #E2EDFF | #FEE2E2 → #FFE5E5 |

## 8. Placeholder

Added watermark TextBlock behind transparent DatePickerTextBox:
- Text: "Chọn ngày"
- Font: Segoe UI 13px, #94A3B8
- IsHitTestVisible="False"
- Shows through when no date selected, hidden behind TextBox text when date is set

## 9. Separator

| Property | Before | After |
|----------|--------|-------|
| Color | #F1F5F9 | #E8EEF6 |
| Margin | 16,0 | 12,0 |

## 10. Files đã sửa

- `Themes/FrontOfficeDesignSystem.xaml` — compacted all Calendar styles (day button, nav, header, grid, footer)
- `Views/StayProfileWindow.xaml` — restored Popup Calendar, added watermark, added CalendarStyle/FirstDayOfWeek setters

## 11. Build result

- Build: PASS (0 errors, 84 pre-existing warnings)

## 12. Runtime result

- App launched: OK
- Popup opens with compact, modern Calendar
- All states render correctly
- Quick actions work
- PART_Button still works

## 13. Xác nhận giữ nguyên logic Codex

- PART_Button (ToggleButton): unchanged
- PART_Popup contract: unchanged
- PART_Calendar wiring: unchanged
- CalendarStyle reference: unchanged (added as Setter on DatePicker style too)
- Quick action Click handlers: unchanged
- SelectedDate binding: unchanged
- Popup open/close mechanism: unchanged

## 14. Xác nhận không sửa backend

- ViewModel: untouched
- Service: untouched
- Database: untouched
- No migrations added

## 15. Xác nhận không sửa Banquet

- Banquet / Nhà hàng tiệc views: untouched
- Only FrontOfficeDesignSystem.xaml + StayProfileWindow.xaml modified

## KẾT LUẬN: COMPACT REFINEMENT DONE
