# ASMILE-BO Front Office — Tổng kết hệ thống Wizard

**Project Root:** `D:\ASMILE-BO`  
**Ngày:** 2026-07-25  
**Pattern:** Header → Step Bar → Summary Strip → Body `*` → Footer cố định  
**Build:** 0 errors

---

## 1. Điều chỉnh lưu trú (4 window)

| STT | Card | Window | Bước | File XAML | File CS |
|-----|------|--------|------|-----------|---------|
| 1 | Đổi phòng | `RoomMoveWindow` | A→E (5) | `Views/RoomMoveWindow.xaml` | `.cs` |
| 2 | Gia hạn lưu trú | `ExtendStayWindow` | A→D (4) | `Views/ExtendStayWindow.xaml` | `.cs` |
| 3 | Đổi phòng + Gia hạn | `RoomMoveExtendWindow` | A→F (6) | `Views/RoomMoveExtendWindow.xaml` | `.cs` |
| 4 | Điều chỉnh thông tin | `StayInformationAdjustmentWindow` | A→F (6) | `Views/StayInformationAdjustmentWindow.xaml` | `.cs` |

### Chi tiết từng window

#### 1. RoomMoveWindow (Đổi phòng)
- **Steps:** A. Chọn khách → B. Chọn phòng → C. Điều chỉnh → D. Bàn giao → E. Xác nhận
- **Features:** 120 guests, 12 rooms, comparison table, readiness, warnings, confirmation popup
- **State:** `_guest`, `_room`, `_reason`, `_pricePolicy`, `_handoverChecks`

#### 2. ExtendStayWindow (Gia hạn lưu trú)
- **Steps:** A. Chọn khách → B. Điều chỉnh thời gian → C. Kiểm tra giá → D. Xác nhận
- **Features:** 120 guests, DatePicker, availability check, price calculation, timeline
- **State:** `_guest`, `_newDeparture`, `_reason`, `_ratePlan`

#### 3. RoomMoveExtendWindow (Đổi phòng + Gia hạn)
- **Steps:** A. Chọn khách → B. Chọn thời gian → C. Chọn phòng → D. Giá & ĐC → E. Bàn giao → F. Xác nhận
- **Features:** 120 guests, 100 rooms, 2-phase pricing, cross-period availability, 18-item checklist
- **State:** `_guest`, `_room`, `_newDeparturePicker`, `_handover`
- **Card:** `StayAdjustmentWorkspaceView.xaml` → `OnMoveAndExtend`

#### 4. StayInformationAdjustmentWindow (Điều chỉnh thông tin)
- **Steps:** A. Chọn hồ sơ → B. Khách lưu trú → C. Thời gian → D. Phòng & giá → E. Yêu cầu & nguồn → F. Xác nhận
- **Features:** 120 guests, room readonly, change tracking, 14 special request chips, 2-column guest form
- **State:** `_guest`, `_changeLog`, `_specialRequests`

---

## 2. Check-out (5 window)

| STT | Card | Window | Bước | File XAML | File CS |
|-----|------|--------|------|-----------|---------|
| 1 | Chuẩn bị Check-out | `CheckoutPreparationWindow` | A→F (6) | `Views/CheckoutPreparationWindow.xaml` | `.cs` |
| 2 | Check-out nhanh | `QuickCheckoutWindow` | A→D (4) | `Views/QuickCheckoutWindow.xaml` | `.cs` |
| 3 | Check-out đoàn | `GroupCheckoutWindow` | A→F (6) | `Views/GroupCheckoutWindow.xaml` | `.cs` |
| 4 | Express Check-out | `ExpressCheckoutWindow` | A→F (6) | `Views/ExpressCheckoutWindow.xaml` | `.cs` |
| 5 | Kiểm tra Folio | `FolioReviewWindow` | A→F (6) | `Views/FolioReviewWindow.xaml` | `.cs` |

### Chi tiết từng window

#### 1. CheckoutPreparationWindow (Chuẩn bị Check-out)
- **Steps:** A. Chọn khách → B. Folio → C. Dịch vụ → D. Bàn giao → E. HĐ & TT → F. Xác nhận
- **Features:** 120 guests, 20-item checklist, service DataGrid, invoice/payment plan
- **State:** `_guest`, `_folioBalance`, `_unpostedServices`, `_handover`

#### 2. QuickCheckoutWindow (Check-out nhanh)
- **Steps:** A. Chọn hồ sơ → B. Kiểm tra nhanh → C. TT & HĐ → D. Xác nhận
- **Features:** 120 guests with status filter, "Chọn hồ sơ này" button, blocking on balance > 0
- **State:** `_guest`, `_selected`, `_folioBalance`, `_paymentMethod`

#### 3. GroupCheckoutWindow (Check-out đoàn)
- **Steps:** A. Chọn đoàn → B. Phòng & KH → C. Folio → D. DV & BG → E. HĐ & TT → F. Xác nhận
- **Features:** 80 groups, 500 rooms, multi-select rooms, Master/Room Folio, payer mapping, dual pagers
- **State:** `_group`, `_groupSelected`, `_selectedRooms`, `_blockedRooms`, `_stayingRooms`

#### 4. ExpressCheckoutWindow (Express Check-out)
- **Steps:** A. Chọn hồ sơ → B. Ủy quyền → C. Folio & TT → D. DV & hậu kiểm → E. HĐ & BG → F. Xác nhận
- **Features:** 120 guests, authorization flow, payment authorization, post-departure charges, timeline
- **State:** `_guest`, `_selected`, `_authStatus`, `_folioBalance`, `_warnings`, `_blocking`

#### 5. FolioReviewWindow (Kiểm tra Folio)
- **Steps:** A. Chọn hồ sơ → B. Tổng quan Folio → C. Khoản mục & DV → D. Người TT → E. Đối chiếu & CB → F. Kết luận
- **Features:** 120 guests, multi-Folio listing, payer mapping, deposit allocation, reconciliation, warning center
- **State:** `_guest`, `_selected`, `_folioCount`, `_totalFolio`, `_deposit`, `_paid`, `_due`

---

## 3. Shared Pattern

Tất cả 9 window dùng chung:

```
┌─ HEADER (Auto) ─────────────────────────────┐
│ [icon] Tiêu đề                   [LM] [Đóng] │
├─ STEP BAR (Auto) ───────────────────────────┤
│  A › B › C › D › E › F                       │
├─ SUMMARY STRIP (Auto) ──────────────────────┤
│ Khách │ Phòng │ ... │ Ready                  │
├─ BODY (*) ──────────────────────────────────┤
│ XAML step panels with Visibility toggle      │
├─ FOOTER (Auto) ─────────────────────────────┤
│ Bước X/N │ [Hủy] [← Qlại] [Tiếp tục →]      │
└─────────────────────────────────────────────┘
```

### Shared components
- **Pager**: `← 1 2 3 ... →` + page size (20/50/100), cố định dưới DataGrid
- **DataGrid**: Modern style, alternating rows, no gridlines
- **ComboBox/TextBox/DatePicker**: Custom ControlTemplate, CornerRadius 9
- **Button**: Primary (xanh) + Secondary (xám), Height 36px
- **Step bar**: Clickable pill-style with active/completed states
- **Duplicate protection**: `Application.Current.Windows.OfType<T>().FirstOrDefault(w => w.IsLoaded)`

### Shared cards (4)
| Card | Window |
|------|--------|
| Điều chỉnh lưu trú | `StayAdjustmentWorkspaceView` |
| Check-out | `CheckoutWorkspaceView` |
| Thu ngân / Folio | `CashieringWorkspaceView` |
| Quản lý chìa khóa | `KeyManagementWorkspaceView` |
| Nhật ký FO | `FrontOfficeLogWorkspaceView` |

---

## 4. Navigation Architecture

```
ReceptionHubViewModel
  ├── ActiveWorkspace = StayAdjustmentWorkspaceView
  │     ├── OnRoomMove → RoomMoveWindow
  │     ├── OnExtendStay → ExtendStayWindow
  │     ├── OnMoveAndExtend → RoomMoveExtendWindow
  │     └── OnModifyInfo → StayInformationAdjustmentWindow
  │
  └── ActiveWorkspace = CheckoutWorkspaceView
        ├── OnPrepCheckout → CheckoutPreparationWindow
        ├── OnQuickCheckout → QuickCheckoutWindow
        ├── OnGroupCheckout → GroupCheckoutWindow
        ├── OnExpressCheckout → ExpressCheckoutWindow
        └── OnCheckFolio → FolioReviewWindow
```

---

## 5. Retired Windows (giữ file, không xóa)

| Window | Lý do |
|--------|-------|
| `StayAdjustmentWindow` | Đã thay bằng `StayAdjustmentWorkspaceView` |
| `CheckoutWindow` | Đã thay bằng `CheckoutWorkspaceView` |
| `CashieringWindow` | Đã thay bằng `CashieringWorkspaceView` |
| `KeyManagementWindow` | Đã thay bằng `KeyManagementWorkspaceView` |
| `FrontOfficeLogWindow` | Đã thay bằng `FrontOfficeLogWorkspaceView` |

---

## 6. Extension Method Conflicts

Đã giải quyết xung đột `WithRuns` bằng cách đánh số:
- `TextBlockEx.WithRuns` (RoomMoveWindow)
- `ExtTextBlockEx.WithRuns2` (StayInformationAdjustmentWindow)
- `CoExt.WithRuns3` (CheckoutPreparationWindow)
- `MxExt.WithRuns` (RoomMoveExtendWindow)
- `QcExt.WithRuns4` (QuickCheckoutWindow)
- `GpExt.WithRuns5` (GroupCheckoutWindow)
- `ExExt.WithRuns6` (ExpressCheckoutWindow)
- `FrExt.WithRuns7` (FolioReviewWindow)

---

## 7. Build Stats

| Metric | Value |
|--------|-------|
| Total windows created | 9 (active) + 5 (retired) = 14 |
| Total XAML files | 14 |
| Total CS files | 18+ |
| Total wizard steps | 49 |
| Build errors | 0 |
| Pre-existing warnings | ~84 |
| Backend modified | 0 |
| Database modified | 0 |
| Banquet modified | 0 |
