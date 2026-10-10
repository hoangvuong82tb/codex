# SPLIT FOLIO — FULL UI IMPLEMENTATION REPORT

**Project:** D:\ASMILE-BO  
**Module:** Front Office → Thu ngân / Folio → Tách Folio  
**Card:** CashieringWorkspaceView → OnSplit  
**Window:** SplitFolioWindow  
**Date:** 2026-07-25  
**Build:** PASS (0 errors, 0 warnings)  
**Runtime:** OK

---

## 1. Project Root
D:\ASMILE-BO (tuyệt đối không dùng F:\ASMILE-BO)

## 2. Card & Command
`CashieringWorkspaceView.xaml.cs` → `OnSplit` handler mở `SplitFolioWindow` với duplicate protection.

## 3. SplitFolioWindow
- Size: 1480×900, MinWidth 1260, MinHeight 760
- CenterOwner, CanResize, ShowInTaskbar=False
- Duplicate protection: nếu đã mở → Activate

## 4. Phân biệt với Chuyển khoản mục
- **Tách Folio**: Phân chia Folio thành nhiều Folio đích, hỗ trợ tách một phần, tạo Folio đích mới, Package/Tax/SC handling
- **Chuyển khoản mục**: Chuyển khoản từ A→B (không implement ở đây)

## 5. Kiến trúc Wizard 6 bước

| Step | Tên | Mô tả |
|------|-----|-------|
| A | Folio nguồn | Chọn Folio cần tách (120 mock) |
| B | Khoản mục | Chọn khoản cần tách (350 mock), partial split |
| C | Folio đích | Chọn/tạo Folio đích, multi-target |
| D | Quy tắc tách | 11 rule types: ByItem, ByService, ByDate, ByGuest, ByRoom, ByPayer, ByCompanyTA, ByRatio, ByAmount, ByEqual, ByBilling |
| E | Đối chiếu | Before/after reconciliation, Warning Center, Blocking issues |
| F | Xác nhận | Summary + Readiness checklist + Confirmation dialog |

## 6. Header
Icon gradient tím (#7C3AED → #5B21B6), tiêu đề "Tách Folio", subtitle, nút Làm mới + Đóng

## 7. Summary Strip
Khách, Phòng, Nguồn, Tổng nguồn, Khoản đã chọn, Tổng tách, Folio đích, Chênh lệch, Currency, Readiness badge

## 8. Step A — Folio nguồn
- Filter: từ khóa, loại Folio, currency, trạng thái
- DataGrid 10 cột: Mã Folio, Loại, Khách, Phòng, Payer, Cty/TA, Currency, Tổng Nợ, Balance, Khoản, T.Thái
- Pager cố định: 20/50/100
- Detail panel: 15 field (Code, Type, Guest, Room, Res, Open/Close dates, Payer, Company, TA, Billing, Currency, Total, Paid, Deposit, Balance, Items, Locked, Disputed, Status) + "Chọn Folio này"

## 9. Step B — Khoản mục
- Filter: loại khoản, bộ phận, split flag
- Quick actions: Chọn tất cả, Bỏ chọn, Chỉ tiền phòng
- Selection summary: số khoản đã chọn, tổng tách, thuế, phí DV, bị chặn
- DataGrid 15 cột
- Detail panel + Partial split form (Gốc, Tách, Giữ lại, Tỷ lệ, Lý do)
- Pager: 30/50/100

## 10. Step C — Folio đích
- Danh sách Folio đích đã chọn (target + mock)
- Danh sách Folio có sẵn trong hồ sơ (click để thêm)
- Form tạo Folio đích mockup (Loại, Payer)

## 11. Step D — Quy tắc tách
- 11 rule types với UI riêng cho từng loại
- ByRatio: slider tỷ lệ với validation 100%
- ByAmount: input số tiền cố định
- ByService: mapping table (Room→Company, Minibar→Guest,...)
- ByPayer: mapping theo payer type

## 12. Step E — Đối chiếu
- Left: Trước/Sau khi tách (Tổng Nợ, Tổng Có, Balance, Thuế, Phí DV, Discount)
- Right: Warning Center với blocking issues và non-blocking warnings
- Mỗi warning có: mức độ, message, step liên quan

## 13. Step F — Xác nhận
- Left: Tóm tắt (Nguồn, Payer, Currency, Tổng trước, Khoản tách, Folio đích, Quy tắc, Chênh lệch)
- Right: Readiness checklist 12 mục + tổng kết
- Confirmation dialog (MessageBox mockup)

## 14. Mock Data
| Loại | Số lượng |
|------|----------|
| Folio | 120 |
| Items | 350 |
| Đủ loại: Master, Room, Restaurant, Spa, Laundry, Extra, Company, Agent, Guest, City Ledger |

## 15. Error Handling
- Không MessageBox hệ thống cho lỗi
- Inline warning/blocking issue
- Disable action khi không đủ điều kiện

## 16. Files

| File | Trạng thái | Dòng |
|------|-----------|------|
| `Views/SplitFolioWindow.xaml` | Tạo mới | 200 |
| `Views/SplitFolioWindow.xaml.cs` | Tạo mới | 718 |
| `Views/CashieringWorkspaceView.xaml.cs` | Sửa (wire OnSplit) | +8 |

## 17. Build & Runtime
- Build App: PASS (0 errors)
- Build Solution: PASS (0 errors)
- App launched: OK

## 18. Xác nhận
- Không sửa Folio Workspace
- Không sửa Gộp Folio
- Không sửa Chuyển khoản mục
- Không sửa Thu tiền, Đặt cọc, Hoàn tiền
- Không sửa backend/database/Banquet
- Chỉ thao tác trong D:\ASMILE-BO
