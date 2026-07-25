# EDIT RESERVATION IMPLEMENTATION REPORT

> **Ngày**: 2026-07-22
> **Kết luận**: EDIT RESERVATION FLOW IMPLEMENTED — selection popup + Edit Mode wizard + change tracking

---

## 1. LUỒNG MỞ SỬA ĐẶT PHÒNG

```
Reservation Hub → Card "Sửa đặt phòng"
  → OpenEditFlowAsync()
    → SearchReservationWindow ("Chọn đặt phòng cần sửa")
      → Chọn reservation (PropertyChanged → SelectedReservation)
        → Close search window
        → OpenEditWizardAsync(reservationId, reservationNumber)
          → CreateReservationPopup + ReservationCreateWindow
          → wizard.SetEditMode(id, number)
          → ShowDialog()
```

## 2. POPUP CHỌN RESERVATION

Tái sử dụng `SearchReservationWindow` với title "Chọn đặt phòng cần sửa". Khi chọn dòng → tự động đóng và mở Edit Wizard.

## 3. CÁCH TÁI SỬ DỤNG WIZARD A–G

Dùng chung `CreateReservationPopup` + `ReservationCreateWindow`. KHÔNG tạo window hoặc form mới.

## 4. CREATE MODE VÀ EDIT MODE

| | Create Mode | Edit Mode |
|---|---|---|
| `IsEditMode` | `false` | `true` |
| Header title | "Đặt phòng mới" | "Sửa đặt phòng" |
| Header subtitle | "Tạo hồ sơ..." | "Số đặt phòng: RSV-..." |
| Footer button | "Tạo đặt phòng" | "Lưu thay đổi" |
| Step G | Validation summary | Change summary + validation |

## 5. DỮ LIỆU LOAD Ở TỪNG STEP

Dữ liệu được load thông qua `ReservationWorkspaceViewModel` — các binding tự động hiển thị dữ liệu từ ViewModel. Step A–F dùng chung binding Create Mode.

## 6. ORIGINAL SNAPSHOT + CHANGE TRACKING

`CaptureSnapshot()` — lưu snapshot các field chính vào dictionary.

`DetectChanges()` — so sánh snapshot với giá trị hiện tại. Được gọi khi:
- Đến Step G
- Thay đổi các field step E (UseRateCode)

## 7. TÓM TẮT OLD → NEW (STEP G)

`ChangeSummary` property trả về `List<ChangeSummaryItem>` các field thay đổi. Hiển thị trong card màu cam ở Step G (chỉ Edit Mode).

Nếu không có thay đổi: "Chưa có thay đổi."

## 8. SAVE FLOW

Footer button dùng `Modal.SaveNewCommand` — `ReservationModalWorkspaceViewModel.SaveNewAsync()`:
```csharp
await Workspace.SaveAsync();
```

Workspace gọi `service.UpdateAsync()` khi `_current is not null` (Edit mode).

## 9. FILES ĐÃ SỬA

| File | Change |
|------|--------|
| `Views/ReservationCreateWindow.xaml.cs` | +150 lines: Edit mode props, snapshot, change tracking, ChangeSummary |
| `Views/ReservationCreateWindow.xaml` | Dynamic header title/subtitle, footer button, Step G change summary |
| `Views/CreateReservationPopup.xaml.cs` | Exposed `Wizard` property |
| `ViewModels/ReservationHubViewModel.cs` | `OpenEditFlowAsync` + `OpenEditWizardAsync` + edit routing |

## 10. BUILD & RUNTIME

| Check | Result |
|-------|--------|
| Build | PASS (0 errors) |
| App | 252MB |
| Search popup (Tìm) | Untouched |
| New Reservation | Untouched |
| Backend | Unchanged |

## 11. XÁC NHẬN

- [x] Chỉ làm Sửa đặt phòng
- [x] Không sửa Tìm đặt phòng
- [x] Không tạo form Edit mới
- [x] Tái sử dụng Wizard A–G
- [x] Không sửa backend
- [x] Không migration
- [x] Dynamic header (title/subtitle thay đổi theo mode)
- [x] Change tracking + Step G change summary
- [x] Footer button "Lưu thay đổi" trong Edit mode
