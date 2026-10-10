# RESERVATION HUB — REMAINING FUNCTIONS UI REPORT

> **Ngày**: 2026-07-22
> **Kết luận**: 6 active modes implemented, 4 "coming soon" placeholders preserved

---

## 1. FILES CHANGED

| File | Change |
|------|--------|
| `ASMILE.BO.App/ViewModels/ReservationModalWorkspaceViewModel.cs` | Added `IsSearch`, `IsConfirm`, `IsCancel`, `IsStatus`, `IsOverlay` properties. Updated `IconGlyph`, `Title`, `Subtitle` to use `Kind` switch with all 7 values. Updated `SetKind` to notify all new properties. |
| `ASMILE.BO.App/Views/ReservationModalWorkspaceView.xaml` | Complete rewrite — 285 lines. Dynamic header, 5 mode-specific body sections, unified footer. |

---

## 2. ARCHITECTURE

```
ReservationHubView (sub-hub cards)
  ├── "Đặt phòng mới" → CreateReservationPopup → ReservationCreateWindow (wizard A-G)
  └── All other cards → ModalService.OpenAsync() → ReservationModalWorkspaceView (overlay)
       ├── IsSearch → Search mode
       ├── IsDetail → Edit mode
       ├── IsAssignRoom → Assign Room mode
       ├── IsConfirm → Confirm mode
       ├── IsCancel → Cancel mode
       └── IsStatus → Status Dashboard mode
```

All modes share:
- **Header**: Dynamic icon, title, subtitle from ViewModel
- **Footer**: Error/info messages + Cancel/Save buttons (context-aware)
- **Overlay**: Dark `#66000000` backdrop + centered 1100×720 white card

---

## 3. SEARCH MODE (`IsSearch`)

**Card**: "Tìm đặt phòng" → `ReservationModalKind.Search`

**UI**:
- Filter bar: Search text, Status combo, Arrival From/To DatePickers, Search + Clear buttons
- DataGrid: 6 columns (Số đặt phòng, Khách, Nhận phòng, Trả phòng, Trạng thái, Nguồn)
- Pagination: Trước/Sau buttons + total count
- Click row selects → enables detail actions

**Design**: Matches wizard style — white card, blue search button, styled DataGrid with alternating rows

---

## 4. EDIT MODE (`IsDetail`)

**Card**: "Sửa đặt phòng" → `ReservationModalKind.Edit`

**UI**: Two-column form matching wizard Step A layout:
- Left: Guest name, Reservation number (readonly), Source, Internal notes
- Right: Arrival/Departure DatePickers, Nights (computed), Adults/Children, Room type combo
- Info box about stay changes affecting pricing

**Permissions**: Fields gated by `CanUpdate`, `CanChangeStayAndGuests`

---

## 5. ASSIGN ROOM MODE (`IsAssignRoom`)

**Card**: "Phân phòng" → `ReservationModalKind.AssignRoom`

**UI**: Two-panel layout:
- **Left panel (3*)**: Reservation info card (guest, dates, room type, nights), current room status, Assign/Unassign buttons
- **Right panel (4*)**: Available rooms DataGrid (Số phòng, Loại phòng, Tầng, Trạng thái)

**Actions**: Assign room (blue), Unassign room (gray) — gated by `CanAssignRoom`/`CanUnassignRoom`

---

## 6. CONFIRM MODE (`IsConfirm`)

**Card**: "Xác nhận đặt phòng" → `ReservationModalKind.Confirm`

**UI**:
- Description text
- DataGrid: 5 columns filtered to Draft/Tentative reservations
- Action buttons: "Xác nhận đã chọn" (green), "Tạm giữ" (gray)

**Initialize**: Sets `SelectedStatus = Draft` and loads matching reservations

---

## 7. CANCEL MODE (`IsCancel`)

**Card**: "Hủy đặt phòng" → `ReservationModalKind.Cancel`

**UI**: Two-panel layout:
- **Left**: DataGrid with 4 columns
- **Right (280px)**: Cancel form — reason TextBox, penalty fee, refund amount, "Hủy đặt phòng" button (red)

**Initialize**: Sets `SelectedStatus = Confirmed` and loads matching reservations

---

## 8. STATUS DASHBOARD MODE (`IsStatus`)

**Card**: "Tình trạng đặt phòng" → `ReservationModalKind.Status`

**UI**:
- Status cards row: Draft (gray), Tentative (orange), Confirmed (blue), Checked In (green), No Show (red), Cancelled (gray)
- DataGrid: 6 columns with all reservations
- Counts shown as placeholder "--" (backend data needed for real counts)

---

## 9. COMING SOON FUNCTIONS (7-10)

Already handled by `ReservationHubViewModel.BuildCards()` — `AddComingSoon()` creates disabled cards with "Sắp có" badge. Clicking them triggers `ShowComingSoon()` dialog.

| # | Card | Id |
|---|------|-----|
| 7 | Đặt phòng Web | web |
| 8 | Theo dõi từ chối | denied |
| 9 | Khả năng phòng tương lai | availability |
| 10 | Báo cáo đặt phòng | reports |

---

## 10. DESIGN SYSTEM COMPLIANCE

All modes use the exact same design tokens as the New Reservation wizard:

| Element | Value |
|---------|-------|
| Card background | White |
| Card border | `#E2E8F0`, 1px |
| Corner radius | 16px |
| Shadow | `DropShadowEffect BlurRadius=40 Opacity=0.18` |
| Overlay | `#66000000` |
| Primary button | `#2563EB`, white text |
| Success button | `#16A34A` |
| Danger button | `#DC2626` |
| Gray button | `#F1F5F9`, `#475569` text |
| Title font | 20px SemiBold `#102E5A` |
| Subtitle font | 13px `#64748B` |
| DataGrid header | 40px height |
| DataGrid row | 42px height |
| DataGrid alternating | `#F8FAFC` |
| Input height | 38px |
| Input border | `#CBD5E1` |

---

## 11. BUILD & RUNTIME

| Check | Result |
|-------|--------|
| Build (clean) | PASS (0 errors, 0 warnings) |
| App launch | 264MB RAM |
| Exception log | No new exceptions |
| New Reservation wizard | Untouched, verified |

---

## 12. CONFIRMATION

- [x] No backend changes
- [x] No migration
- [x] No database changes
- [x] New Reservation wizard (Steps A-G) completely untouched
- [x] All 6 active functions have proper UI
- [x] 4 "coming soon" cards preserved
- [x] Same design system as wizard
- [x] No WinForms-style controls
- [x] No default unstyled DataGrid
