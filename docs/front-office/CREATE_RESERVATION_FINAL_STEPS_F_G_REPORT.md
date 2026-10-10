# CREATE RESERVATION FINAL STEPS F & G REPORT

> **Ngày**: 2026-07-22
> **Kết luận**: STEPS F & G COMPLETE — 2-card modern layout, realtime summaries, 5-group confirmation, validation checklist

---

## 1. FILES ĐÃ SỬA

| File | Thay đổi |
|------|----------|
| `ASMILE.BO.App/Views/ReservationCreateWindow.xaml` | Rewrite Step F (958-983 → new), Rewrite Step G (985-1040 → new), Update footer button |
| `ASMILE.BO.App/Views/ReservationCreateWindow.xaml.cs` | Add Step F properties (8 fields + 8 display), Add Step G logic (validation, status, navigation), Add helper classes |

---

## 2. STEP F — GHI CHÚ

### Left Card: Input (5* width)

| # | Field | Type | Binding | TabIndex |
|---|-------|------|---------|----------|
| 1 | Ghi chú đặt phòng | TextBox 120px, multiline | `BookingNotes` | 73 |
| 2 | Ghi chú nội bộ | TextBox 100px, multiline | `Modal.Workspace.InternalNotes` | 74 |
| 3 | Yêu cầu đặc biệt | TextBox 90px, multiline | `SpecialRequests` | 75 |
| 4 | Đón | ToggleSwitch | `NeedPickup` (bool) | 76 |
| 5 | Tiễn | ToggleSwitch | `NeedDropoff` (bool) | 77 |
| 6 | CB đến | TextBox | `FlightArrival` | 78 |
| 7 | Giờ đến | TextBox | `FlightArrivalTime` | 79 |
| 8 | CB đi | TextBox | `FlightDeparture` | 80 |
| 9 | Giờ đi | TextBox | `FlightDepartureTime` | 81 |

### Right Card: Realtime Summary (3* width)

7 preview blocks:
- **Ghi chú đặt phòng** — truncated to 80 chars, ellipsis
- **Ghi chú nội bộ** — truncated to 80 chars
- **Yêu cầu đặc biệt** — truncated to 80 chars
- **Vận chuyển** — "Có" / "—" for Đón/Tiễn
- **Chuyến bay đến** — flight number + time
- **Chuyến bay đi** — flight number + time
- **Status pill** — "Đã có ghi chú" (green) or "Không có ghi chú" (gray)

### Code-behind Properties

```csharp
// Data
string BookingNotes, SpecialRequests, FlightArrival, FlightDeparture, FlightArrivalTime, FlightDepartureTime
bool NeedPickup, NeedDropoff

// Display
string BookingNotesPreview, InternalNotesPreview, SpecialRequestsPreview
string PickupDisplay, DropoffDisplay, FlightArrivalDisplay, FlightDepartureDisplay
bool HasNotes

// Update
void UpdateNotesSummary() — fires PropertyChanged for all display props
```

---

## 3. STEP G — XÁC NHẬN

### Left Card: 5 Summary Groups (5.5* width)

| # | Group | Has "Chỉnh sửa" button | Navigates to |
|---|-------|----------------------|--------------|
| 1 | Lưu trú | Yes | Step A (`GoToStepA`) |
| 2 | Khách | Yes | Step C (`GoToStepC`) |
| 3 | Đặt phòng | Yes | Step D (`GoToStepD`) |
| 4 | Tài chính | Yes | Step E (`GoToStepE`) |
| 5 | Ghi chú | Yes | Step F (`GoToStepF`) |

Each group shows relevant summary data from previous steps. Bottom: balance bar "Còn phải thu".

### Right Card: Step Status + Validation (2.5* width)

**Step Status** — 6 items A-F using `StepGStatusItem`:
- Green `✓` = Hoàn tất
- Gray `—` = Có thể bỏ qua (Steps B, D, E, F)
- Red `✗` = Thiếu dữ liệu (when not optional)

**Validation Issues** — red section "Cần bổ sung":
- Chưa có tên khách
- Ngày lưu trú không hợp lệ
- Số người lớn phải >= 1
- Chưa chọn hạng phòng
- Chưa có nguồn giá hợp lệ

Hidden when `CanCreateReservation = true`.

**All Good** — green pill "✓ Sẵn sàng tạo đặt phòng" visible when `CanCreateReservation = true`.

### Validation Blocking

`CanCreateReservation` = `HasGuestName && HasValidDates && HasValidAdults`

- `HasGuestName` → `!string.IsNullOrWhiteSpace(Modal.Workspace.GuestName)`
- `HasValidDates` → Arrival/Departure not null, Departure > Arrival
- `HasValidAdults` → Adults >= 1
- `HasValidPriceSource` → Rate Code filled (if using rate code) or Room Rate > 0 (if negotiated)

### Footer Button

"Tạo đặt phòng" button respects `CanCreateReservation`:
- `IsEnabled="{Binding CanCreateReservation}"` — disabled when validation fails
- Uses existing `Modal.SaveNewCommand`

### Tab Order (Step G)

| # | Control | TabIndex |
|---|---------|----------|
| 1 | Chỉnh sửa Lưu trú | 82 |
| 2 | Chỉnh sửa Khách | 83 |
| 3 | Chỉnh sửa Đặt phòng | 84 |
| 4 | Chỉnh sửa Tài chính | 85 |
| 5 | Chỉnh sửa Ghi chú | 86 |

---

## 4. CODE-BEHIND CLASSES ADDED

```csharp
class StepGValidationItem  // Label, TargetStep
class StepGStatusItem      // Label, IsComplete, IsOptional, Icon, IconColor, FgColor, StatusText
```

---

## 5. KẾT QUẢ RUNTIME

| Test | Step | Result |
|------|------|--------|
| Left card input fields functional | F | PASS |
| Right card updates realtime | F | PASS |
| Pickup/Dropoff toggles work | F | PASS |
| Flight fields bind correctly | F | PASS |
| "Không có ghi chú" / "Đã có ghi chú" toggle | F | PASS |
| Navigate back to E, forward to F preserves data | F | PASS |
| 5 summary groups displayed | G | PASS |
| "Chỉnh sửa" buttons navigate to correct steps | G | PASS |
| Step status A-F with correct icons/colors | G | PASS |
| Validation issues shown when data missing | G | PASS |
| "Tạo đặt phòng" disabled when invalid | G | PASS |
| "Tạo đặt phòng" enabled when valid | G | PASS |
| Balance bar shown | G | PASS |
| Build | ALL | PASS (0 errors, 0 warnings) |
| App running | ALL | 286MB, no new exceptions |

---

## 6. XÁC NHẬN

- [x] Không sửa Step A–E
- [x] Không sửa wizard header
- [x] Không sửa popup window
- [x] Không sửa backend
- [x] Không thêm migration
- [x] Không tạo DataGrid ở Step G
- [x] Không tạo thêm bước H
- [x] Không dùng MessageBox cho validation
- [x] 2-card modern layout matching Steps A-E design language
- [x] Step F: left input + right realtime summary
- [x] Step G: left 5-group summary + right step status checklist
- [x] Footer "Tạo đặt phòng" enabled only when validation passes
