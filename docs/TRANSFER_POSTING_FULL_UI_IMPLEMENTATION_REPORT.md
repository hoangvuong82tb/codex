# TRANSFER POSTING — FULL UI IMPLEMENTATION REPORT

**Project:** D:\ASMILE-BO  
**Module:** Front Office → Thu ngân / Folio → Chuyển khoản mục  
**Window:** TransferPostingWindow  
**Date:** 2026-07-25  
**Build:** PASS (0 errors)

---

## 1. Card & Command
`CashieringWorkspaceView.xaml.cs` → `OnTransfer` handler mở `TransferPostingWindow` với duplicate protection.

## 2. TransferPostingWindow
- 1480×900, MinWidth 1260
- Wizard 6 bước: A. Chọn Posting → B. Folio đích → C. Quy tắc chuyển → D. Preview → E. Kiểm tra → F. Xác nhận

## 3. Phân biệt với Split/Merge
- **Chuyển khoản mục**: Di chuyển posting giữa 2 Folio, giữ nguyên Transaction ID, audit, reference
- **Tách Folio**: Chia Folio thành nhiều Folio
- **Gộp Folio**: Hợp nhất nhiều Folio → 1

## 4. Step A — Chọn Posting
- Filter: từ khóa, loại, bộ phận, eligibility
- Quick actions: Tiền phòng, Có thể chuyển, Bỏ chọn
- DataGrid 17 cột: Mã GD, Ngày, Loại, Diễn giải, BP, Outlet, Khách, Phòng, Folio, SL, ĐG, Thuế, Phí, Net, Currency, Payer, T.Thái
- Pager: 30/50/100
- Detail panel: 16 field + eligibility flag
- 320 mock postings

## 5. Step B — Folio đích
- 10 target folios: Master, Room, Restaurant, Spa, Company, Agent, Incidentals
- Preview: Balance trước/sau, compatibility check
- Tạo Folio đích mockup

## 6. Step C — Quy tắc chuyển
- 4 transfer modes: Toàn bộ, Tỷ lệ %, Số tiền, Số lượng
- Tax rule: Chuyển cùng / Giữ lại / Tách riêng
- Package rule: Chuyển cùng / Giữ cha-con / Cảnh báo
- Payer rule: Giữ nguyên / Đổi về đích / Map Company / Map Guest / Map TA

## 7. Step D — Preview
- Left: posting nguồn + tổng chuyển
- Right: Folio đích + balance trước/sau

## 8. Step E — Validation
- Checklist 8 mục: Posting, Target, Currency, Payer, Tax, Package, Duplicate, Conflict
- Warning Center: posting locked/settled/invoice/disputed, currency mismatch

## 9. Step F — Confirmation
- Summary + Readiness 6 mục
- Confirmation dialog (MessageBox mockup)

## 10. Mock Data
- 320 postings: Room Charge, Restaurant, Spa, Laundry, Minibar, Package, Tax, Discount, Adjustment
- 10 target folios
- Đủ eligibility flags: Transferable, Locked, Settled, InvoiceIssued, Disputed, PackageParent/Child

## 11. Files
| File | Status | Lines |
|------|--------|-------|
| `Views/TransferPostingWindow.xaml` | Created | 150 |
| `Views/TransferPostingWindow.xaml.cs` | Created | 420 |
| `Views/CashieringWorkspaceView.xaml.cs` | Modified (wire OnTransfer) | +8 |

## 12. Build & Runtime
- Build: PASS (0 errors)
- App launched: OK
- No backend/database/Banquet modifications
