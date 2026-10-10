# MERGE FOLIO — FULL UI IMPLEMENTATION REPORT

**Project:** D:\ASMILE-BO  
**Module:** Front Office → Thu ngân / Folio → Gộp Folio  
**Window:** MergeFolioWindow  
**Date:** 2026-07-25  
**Build:** PASS (0 errors)

---

## 1. Card & Command
`CashieringWorkspaceView.xaml.cs` → `OnMerge` handler mở `MergeFolioWindow` với duplicate protection.

## 2. MergeFolioWindow
- Size: 1480×900, MinWidth 1260, MinHeight 760
- Wizard 6 bước: A. Folio nguồn → B. Tương thích → C. Folio đích → D. Quy tắc gộp → E. Đối chiếu → F. Xác nhận

## 3. Phân biệt với Tách Folio & Chuyển khoản mục
- **Gộp Folio**: Hợp nhất nhiều Folio nguồn → 1 Folio đích, xử lý payer/deposit/payment/refund/currency/document
- **Tách Folio**: 1 Folio nguồn → nhiều Folio đích
- **Chuyển khoản mục**: Chuyển khoản A→B (không implement ở đây)

## 4. Step A — Chọn Folio nguồn
- Filter: từ khóa, loại, currency, trạng thái
- Quick actions: Cùng hồ sơ, Cùng đoàn, Bỏ chọn
- DataGrid 13 cột + Pager 20/50/100
- Detail panel: 16 field, toggle chọn/bỏ chọn
- Selected panel: danh sách Folio đã chọn với tổng Nợ/Có/Balance
- Tối thiểu 2 Folio

## 5. Step B — Compatibility Matrix
- Ma trận 11 tiêu chí: Hồ sơ, Khách, Phòng, Đoàn, Payer, Company, TA, Currency, Tax Profile, Billing, Trạng thái
- Mỗi tiêu chí hiển thị giá trị từng Folio + kết luận (Tương thích / Cần mapping / Cần approval)
- Warning Center: khác currency, khác payer, Folio bị khóa, Chargeback
- Approval requirement

## 6. Step C — Folio đích
- Ứng viên Folio đích (top 5, ưu tiên Master > Room > khác)
- Điểm phù hợp: Rất phù hợp / Phù hợp / Cần điều chỉnh
- Preview: Balance hiện tại, Tổng sau gộp, Số khoản sau
- Tạo Folio đích mockup

## 7. Step D — Quy tắc gộp
8 nhóm quy tắc: Transaction, Payer, Deposit, Payment, Refund, Currency, Tax/Package, Document
- Mỗi nhóm có 3-4 option
- Source Folio post-merge: Đánh dấu đã gộp / Đóng / Giữ tham chiếu / Alias

## 8. Step E — Đối chiếu
- Before summary: từng Folio nguồn + tổng cộng
- After summary: Folio đích + trạng thái nguồn sau gộp
- Duplicate detection: phát hiện trùng hồ sơ
- Warning Center: blocking + non-blocking

## 9. Step F — Xác nhận
- Tóm tắt: Khách, Phòng, Số Folio, Tổng nguồn, Đích, Quy tắc, Chênh lệch
- Readiness checklist 11 mục
- Confirmation dialog (MessageBox mockup)

## 10. Mock Data
- 120 Folio: Room, Guest, Master, Company, Agent, Incidentals, City Ledger
- Đủ loại: cùng/khác hồ sơ, cùng/khác đoàn, multi-currency, multi-payer, deposit/payment/refund, chargeback, locked, disputed

## 11. Files
| File | Trạng thái | Dòng |
|------|-----------|------|
| `Views/MergeFolioWindow.xaml` | Tạo mới | 150 |
| `Views/MergeFolioWindow.xaml.cs` | Tạo mới | 520 |
| `Views/CashieringWorkspaceView.xaml.cs` | Sửa (wire OnMerge) | +8 |

## 12. Build & Runtime
- Build: PASS (0 errors)
- App launched: OK
- Không sửa backend/database/Banquet
