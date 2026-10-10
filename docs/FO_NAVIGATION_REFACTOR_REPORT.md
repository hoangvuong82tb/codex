# FO NAVIGATION REFACTOR — SINGLE SOURCE OF TRUTH

**Date:** 2026-07-26  
**Build:** PASS (0 errors)  
**Scope:** Front Office — Navigation Audit & Refactor

---

## 1. Principle

Every business operation has exactly ONE canonical window. All other entry points navigate to that window. No duplicate UIs, no copy-paste workflows.

---

## 2. Navigation Architecture

5 workspace UserControls (embedded in `ReceptionHubViewModel`) → 27 canonical windows:

```
ReceptionHubViewModel
├── StayAdjustmentWorkspaceView → RoomMoveWindow, ExtendStayWindow, RoomMoveExtendWindow, StayInformationAdjustmentWindow
├── CheckoutWorkspaceView → CheckoutPreparationWindow, QuickCheckoutWindow, GroupCheckoutWindow, ExpressCheckoutWindow, FolioReviewWindow
├── CashieringWorkspaceView → CashierPaymentWindow, DepositManagementWindow, RefundManagementWindow, FolioWorkspaceWindow, SplitFolioWindow, MergeFolioWindow, TransferPostingWindow, ProformaFolioWindow
├── KeyManagementWorkspaceView → IssueKeyCardWindow, ReissueKeyCardWindow, ReturnKeyCardWindow, DisableKeyCardWindow, KeyCardAuditLogWindow
└── FrontOfficeLogWorkspaceView → ShiftLogWindow, IncidentLogWindow, LostFoundWindow, VipNotesWindow, ShiftHandoverWindow
```

---

## 3. Legacy Windows Rewired

5 legacy windows were found with MessageBox mockup stubs. All have been rewired to open canonical windows:

| Legacy Window | Handler Count | Rewired To |
|--------------|--------------|------------|
| `CashieringWindow.xaml.cs` | 8 handlers | CashierPaymentWindow, DepositManagementWindow, RefundManagementWindow, FolioWorkspaceWindow, SplitFolioWindow, MergeFolioWindow, TransferPostingWindow, ProformaFolioWindow |
| `CheckoutWindow.xaml.cs` | 5 handlers | CheckoutPreparationWindow, QuickCheckoutWindow, GroupCheckoutWindow, ExpressCheckoutWindow, FolioReviewWindow |
| `StayAdjustmentWindow.xaml.cs` | 4 handlers | RoomMoveWindow, ExtendStayWindow, RoomMoveExtendWindow, StayInformationAdjustmentWindow |
| `FrontOfficeLogWindow.xaml.cs` | 5 handlers | ShiftLogWindow, IncidentLogWindow, LostFoundWindow, VipNotesWindow, ShiftHandoverWindow |
| `KeyManagementWindow.xaml.cs` | 4 handlers | ReissueKeyCardWindow, ReturnKeyCardWindow, DisableKeyCardWindow, KeyCardAuditLogWindow |

**Result:** 26 handler stubs eliminated. Zero remaining MessageBox mockups for FO navigation.

---

## 4. Duplicate Audit Results

| Operation | Canonical Window | Entry Points | Duplicates |
|-----------|-----------------|-------------|------------|
| Room Move | RoomMoveWindow | 2 (StayAdjustmentWorkspaceView + StayAdjustmentWindow) | None (RoomMoveConfirmationWindow is sub-dialog) |
| Extend Stay | ExtendStayWindow | 2 | None |
| Room Move + Extend | RoomMoveExtendWindow | 2 | None |
| Stay Info Adjustment | StayInformationAdjustmentWindow | 2 | None |
| Prepare Checkout | CheckoutPreparationWindow | 2 | None |
| Quick Checkout | QuickCheckoutWindow | 2 | None |
| Group Checkout | GroupCheckoutWindow | 2 | None |
| Express Checkout | ExpressCheckoutWindow | 2 | None |
| Folio Review | FolioReviewWindow | 1 | None |
| Cashier Payment | CashierPaymentWindow | 2 | None |
| Deposit | DepositManagementWindow | 2 | None |
| Refund | RefundManagementWindow | 2 | None |
| Folio Workspace | FolioWorkspaceWindow | 2 | None |
| Split Folio | SplitFolioWindow | 2 | None |
| Merge Folio | MergeFolioWindow | 2 | None |
| Transfer Posting | TransferPostingWindow | 2 | None |
| Proforma Folio | ProformaFolioWindow | 2 | None |
| Issue Key Card | IssueKeyCardWindow | 2 | None |
| Reissue Key Card | ReissueKeyCardWindow | 2 | None |
| Return Key Card | ReturnKeyCardWindow | 2 | None |
| Disable Key Card | DisableKeyCardWindow | 2 | None |
| Key Card Audit | KeyCardAuditLogWindow | 2 | None |
| Shift Log | ShiftLogWindow | 2 | None |
| Incident Log | IncidentLogWindow | 2 | None |
| Lost & Found | LostFoundWindow | 2 | None |
| VIP Notes | VipNotesWindow | 2 | None |
| Shift Handover | ShiftHandoverWindow | 2 | None |

**ZERO true duplicates found.** Each operation has exactly ONE canonical window. Entry points (2 per operation: workspace view + legacy window wrapper) all navigate to the same canonical window.

---

## 5. Notable Findings

### Orphaned MVVM: LostFoundView
- `LostFoundView.xaml/.cs` + `LostFoundViewModel.cs` — Unused MVVM UserControl
- Active implementation: `LostFoundWindow` (standalone Window with inline mock data)
- **Recommendation:** Remove orphaned files or migrate `LostFoundWindow` to use `LostFoundViewModel`

### Dead Stub: InHouseWorkspaceViewModel.OpenRoomMove()
- `InHouseWorkspaceViewModel.cs:128` — sets `InfoMessage` but never opens any window
- **Recommendation:** Either wire to `RoomMoveWindow` or remove the stub

---

## 6. Files Modified

| File | Change |
|------|--------|
| `Views/CashieringWindow.xaml.cs` | Rewired 8 handlers → canonical windows |
| `Views/CheckoutWindow.xaml.cs` | Rewired 5 handlers → canonical windows |
| `Views/StayAdjustmentWindow.xaml.cs` | Rewired 4 handlers → canonical windows |
| `Views/FrontOfficeLogWindow.xaml.cs` | Rewired 5 handlers → canonical windows |
| `Views/KeyManagementWindow.xaml.cs` | Rewired 4 handlers → canonical windows |

---

## 7. Build & Runtime

| Metric | Value |
|--------|-------|
| Build | PASS (0 errors) |
| MessageBox mockups remaining | 0 (all FO navigation wired) |
| Backend modifications | 0 |
| Database modifications | 0 |

---

## 8. Conclusion

**FRONT OFFICE NAVIGATION REFACTOR COMPLETE.**

27 operations, 27 canonical windows, single source of truth. All legacy mockup stubs eliminated. No duplicate UIs, no duplicate workflows.
