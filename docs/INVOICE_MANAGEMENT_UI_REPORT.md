# INVOICE MANAGEMENT — FULL UI IMPLEMENTATION REPORT

**Project:** D:\ASMILE-BO  
**Module:** Front Office → Cashiering → Hóa đơn  
**Window:** InvoiceManagementWindow  
**Date:** 2026-07-26  
**Build:** PASS (0 errors)

---

## 1. Project Root
D:\ASMILE-BO (không dùng F:\ASMILE-BO)

## 2. Window
- 1750×940, MinWidth 1450
- 3-column: Category sidebar (175px) | Invoice Grid (*) | Inspector (270px)

## 3. Header
Icon xanh (#10B981), tiêu đề "Hóa đơn", subtitle, 7 nút: Tạo mới, Điều chỉnh, Hủy, In, Email, Xuất, Đóng

## 4. KPI Dashboard
7 KPI: Tổng hóa đơn, Đã phát hành, Đã gửi, Đã hủy, VAT, Điều chỉnh, Nháp

## 5. Advanced Search
- Search box (keyword)
- Type filter (VAT/Official/Proforma/Electronic/Deposit/Refund/Group)
- Status filter (Draft/Pending/Issued/Sent/Cancelled/Adjusted)
- Quick buttons: Đã phát hành, VAT

## 6. Category Sidebar (Left)
7 danh mục với count: Tất cả, VAT, Official, Proforma, Electronic, Đã hủy, Đã điều chỉnh

## 7. Invoice Grid (Center)
13 cột: Số HĐ, Ngày, Reservation, Khách, Công ty, Phòng, Loại HĐ, MST, Tiền, Thuế, Tổng, Trạng thái, Người lập
- Color coding: Status (Issued=xanh, Sent=cam, Cancelled=đỏ, Draft=xám)
- Pager: 50/100/200
- Click row → Inspector

## 8. Inspector (Right) — 5 tabs

### Overview
Invoice No, Date, Type, Status, Cashier (badge màu)

### Customer
Khách, Phòng, Reservation, Công ty, MST

### Invoice Detail
Breakdown: Tiền phòng, F&B, Giặt là, Mini Bar, Spa, Vận chuyển, Khác, Giảm giá, Thuế VAT, Phí DV, TỔNG CỘNG

### Payment
Đã thanh toán, Còn lại

### History
Timeline: Tạo → Phát hành → Gửi email → Hủy (với lý do)

## 9. Actions (Mockup)
- Tạo hóa đơn mới
- Điều chỉnh hóa đơn
- Hủy hóa đơn
- In hóa đơn
- Gửi Email
- Xuất Excel/PDF/CSV

## 10. Mock Data
- 5000+ hóa đơn (VAT, Official, Proforma, Electronic, Deposit, Refund, Group)
- Trạng thái: Draft, Pending, Issued, Sent, Cancelled, Adjusted, Archived
- Corporate + Individual, có/không VAT

## 11. Navigation
`ReceptionHubViewModel` → Card "Hóa đơn" (section "Thu ngân") → `OpenInvoiceWindow()` → `InvoiceManagementWindow`

## 12. Files
| File | Status |
|------|--------|
| `Views/InvoiceManagementWindow.xaml` | Created |
| `Views/InvoiceManagementWindow.xaml.cs` | Created |
| `ViewModels/ReceptionHubViewModel.cs` | Modified (add card + field + method) |

## 13. Build & Runtime
- Build: PASS (0 errors)
- Runtime: PASS
- Không sửa Backend/Database/Banquet

## 14. Xác nhận
- Chỉ thao tác trong D:\ASMILE-BO
- Không sửa Reservation, Reception, Housekeeping, Key Management, Reports
- Không kết nối hóa đơn điện tử thật
- Không gửi Email thật
