# PROFORMA FOLIO — FULL UI IMPLEMENTATION REPORT

**Project:** D:\ASMILE-BO  
**Module:** Front Office → Thu ngân / Folio → In tạm tính  
**Window:** ProformaFolioWindow  
**Date:** 2026-07-25  
**Build:** PASS (0 errors)

---

## 1. Card & Command
`CashieringWorkspaceView.xaml.cs` → `OnPrint` handler mở `ProformaFolioWindow` với duplicate protection.

## 2. ProformaFolioWindow
- 1520×920, MinWidth 1300
- Là Bill Review Workspace, không phải Print Dialog
- 3-column: Folio Browser (200px) | Bill Preview (*) | Inspector (270px)

## 3. Header
- Icon máy in + Folio (xanh indigo #6366F1 → #4F46E5)
- Tiêu đề "In tạm tính", subtitle, 6 nút: Làm mới, Preview, In, Xuất, Email, Đóng

## 4. Summary Strip
Guest, Room, Reservation, Arrival, Departure, Company, TA, Folio, Currency, Balance, Readiness badge

## 5. Folio Browser (Left)
- 8 folio: Master, Room, Restaurant, Spa, Company, Master (USD), Room (USD), Incidentals
- Mỗi folio: Type, Balance, Currency, Status, Posting Count
- Click để chọn, active state nền xanh nhạt

## 6. Toolbar (Template + Language + Currency)
- 6 templates: Standard, Compact, Detailed, Corporate, TA, Group
- 3 ngôn ngữ: Tiếng Việt, English, Song ngữ
- 3 currency: Gốc, VND, USD

## 7. Bill Preview (Center)
- Hotel header (tên, địa chỉ, MST)
- "IN TẠM TÍNH / PROFORMA FOLIO" title
- Guest info grid 2 cột: Khách, Phòng, Reservation, Stay, Đến, Đi, Công ty, Đại lý, Folio, Currency, Template, Ngôn ngữ
- Posting table 6 cột: Ngày, Diễn giải, SL, Đơn giá, Thuế/Phí, Thành tiền
- Totals: Charge, Thuế, Phí DV, Giảm giá, Thanh toán, Đặt cọc, Hoàn tiền, CÒN LẠI

## 8. Inspector (Right)
8 tabs: Tổng quan, Posting, TT toán, Đặt cọc, Hoàn tiền, Routing, Tài liệu, Lịch sử

## 9. Side Tabs (Center)
8 tabs: Tổng quan (KPI cards), Posting (DataGrid), Thanh toán (DataGrid), Đặt cọc (DataGrid), Hoàn tiền (DataGrid), Routing, Tài liệu, Lịch sử

## 10. Actions
- Preview: Tạo preview + lưu history
- Print: Mockup in
- Export: Mockup PDF/Excel/CSV
- Email: Mockup gửi email

## 11. Bottom Summary + Footer
Realtime: Charge, Payment, Deposit, Refund, Balance

## 12. Mock Data
- 8 folios (VND + USD)
- 320 postings, 80 payments, 30 deposits, 20 refunds
- Company + TA data

## 13. Files
| File | Status | Lines |
|------|--------|-------|
| `Views/ProformaFolioWindow.xaml` | Created | 95 |
| `Views/ProformaFolioWindow.xaml.cs` | Created | 430 |
| `Views/CashieringWorkspaceView.xaml.cs` | Modified (wire OnPrint) | +8 |

## 14. Build & Runtime
- Build: PASS (0 errors)
- App launched: OK
- No backend/database/Banquet modifications
