# FOLIO WORKSPACE — IMPLEMENTATION REPORT

**Project:** D:\ASMILE-BO  
**Module:** Front Office → Thu ngân / Folio → Folio  
**Date:** 2026-07-25  
**Build:** PASS (0 errors, 0 warnings)

---

## 1. Kiến trúc Workspace

FolioWorkspaceWindow là Financial Workspace, không phải Wizard:
- **Header** cố định: Icon, tiêu đề, subtitle, 4 nút (Làm mới, In, Xuất, Đóng)
- **Toolbar** cố định: Search + 4 filter ComboBox + Tìm/Xóa
- **Top Summary Strip**: Khách, Phòng, Reservation, Stay, Arrival, Departure, Company, TA, Master Folio, Balance, Deposit, Credit Limit, Currency, Status badge
- **Body 3 cột**: Left (Folio list) | Center (Transaction Workspace) | Right (Inspector)
- **Bottom Summary Strip**: Realtime Charge, Payment, Deposit, Refund, Tax, Discount, Service Charge, Balance
- **Footer** cố định: Transaction count + Balance + Close

---

## 2. Layout

```
┌─ HEADER ───────────────────────────────────────────────────────┐
│ [Icon] Folio                                                    │
│ Quản lý toàn bộ giao dịch tài chính của hồ sơ lưu trú.        │
│                                      [Làm mới] [In] [Xuất] [Đóng] │
├─ TOOLBAR ───────────────────────────────────────────────────────┤
│ [Search...] [Type▼] [Curr▼] [Dept▼] [Status▼] [Tìm] [Xóa]      │
├─ SUMMARY STRIP ─────────────────────────────────────────────────┤
│ Khách: ... │ Phòng: ... │ Res: ... │ Stay: ... │ Balance: ...  │
├─ BODY (3 cột) ──────────────────────────────────────────────────┤
│ LEFT (210px)    │ CENTER (*)          │ RIGHT (290px)           │
│ ┌Side Tabs────┐ │ ┌Ribbon───────────┐ │ ┌Inspector Tabs──────┐ │
│ │Tổng quan    │ │ │ALL Charge Pay...│ │ │General Financial...│ │
│ │Giao dịch ◄──┤ │ ├─────────────────┤ │ ├────────────────────┤ │
│ │Thanh toán   │ │ │                 │ │ │                    │ │
│ │Đặt cọc      │ │ │  DataGrid       │ │ │  Inspector         │ │
│ │Hoàn tiền    │ │ │  18 cột         │ │ │  Content           │ │
│ │Routing      │ │ │  320 giao dịch  │ │ │                    │ │
│ │Lịch sử      │ │ │                 │ │ │                    │ │
│ │Tài liệu     │ │ ├─────────────────┤ │ │                    │ │
│ └─────────────┘ │ │Pager: ← 1 2 →   │ │ │                    │ │
│ ┌Folio List───┐ │ └─────────────────┘ │ └────────────────────┘ │
│ │Master     ◄──┤ │                     │                        │
│ │Room          │ │                     │                        │
│ │Restaurant    │ │                     │                        │
│ │Spa           │ │                     │                        │
│ │Laundry       │ │                     │                        │
│ │Extra         │ │                     │                        │
│ │Company       │ │                     │                        │
│ │Voucher       │ │                     │                        │
│ └─────────────┘ │                     │                        │
├─ BOTTOM SUMMARY ────────────────────────────────────────────────┤
│ Charge: ... · Payment: ... · Deposit: ... · Balance: ...       │
├─ FOOTER ────────────────────────────────────────────────────────┤
│ Master · FOL-26001 · Balance: 0 VND              [Đóng]        │
└─────────────────────────────────────────────────────────────────┘
```

---

## 3. Danh sách Folio (Left Panel)

- **Side Tabs**: 8 tab tùy chỉnh (không dùng TabControl mặc định) — Tổng quan, Giao dịch, Thanh toán, Đặt cọc, Hoàn tiền, Routing, Lịch sử, Tài liệu
- **Folio List**: 13 folio cho 2 khách
  - Khách 1 (Nguyễn Văn An): 8 folio — Master, Room, Restaurant, Spa, Laundry, Extra, Company, Voucher
  - Khách 2 (Trần Thị Bình): 5 folio — Master, Room, Restaurant, Spa, City Ledger
- Mỗi folio hiển thị: Type, Balance, Currency, Payer, Status badge (Open/Settled/Pending/Transferred)
- Selected state: nền xanh nhạt #EFF6FF, border #BFDBFE
- Badge màu: Open + nợ = cam #F59E0B, Đã cân = xanh #16A34A, Settled = xanh, Pending = tím #8B5CF6, Transferred = cyan #06B6D4

---

## 4. Transaction Workspace (Center Panel)

### Ribbon Filter
10 nút lọc: Tất cả, Charge (đỏ), Payment (xanh), Deposit (cam), Refund (tím), Điều chỉnh (cyan), Chuyển (hồng), Thuế (indigo), Discount (cam đậm), Phí DV (teal)
- Active button: màu đặc trưng với chữ trắng
- Inactive: nền xám #F1F5F9, chữ #475569

### DataGrid (18 cột)
| Cột | Width | Mô tả |
|-----|-------|-------|
| Thời gian | 80 | dd/MM HH:mm |
| Mã GD | 80 | TXN-27001... |
| Tham chiếu | 90 | RCP/DEP reference |
| Loại | 70 | Charge/Payment/Deposit/... |
| Diễn giải | 140 | Tiền phòng, Ăn sáng, Spa... |
| BP | 45 | FO, F&B, SPA... |
| Outlet | 90 | Reception, Main Restaurant... |
| SL | 35 | Số lượng |
| ĐG | 70 | Đơn giá |
| Tiền | 85 | Thành tiền |
| Thuế | 60 | VAT |
| Disc | 55 | Discount |
| P.DV | 55 | Phí dịch vụ |
| Tệ | 40 | VND/USD/EUR |
| Người TT | 65 | Guest/Company/TA |
| Operator | 75 | Lễ tân A/B, Thu ngân... |
| T.Thái | 65 | Posted/Pending/Reversed |

### Interaction
- **Click**: Chọn row → Inspector hiển thị chi tiết
- **Double click**: Chọn row + Inspector chuyển sang tab Tổng quan
- **Pager**: ← 1 2 3 ... → + 20/50/100 dòng/trang

---

## 5. Inspector (Right Panel)

7 sub-tab: Tổng quan, Tài chính, Thuế, Audit, Routing, Phê duyệt, Lịch sử

### Tổng quan
Mã GD, Tham chiếu, Thời gian, Khách, Phòng, Reservation, Folio, Bộ phận, Outlet, Operator, Trạng thái

### Tài chính
Tiền gốc, Discount, Thuế, Phí DV, Net, Tệ, Tỷ giá, Số dư sau

### Thuế
VAT 8%, Phí DV 5%, Tổng thuế/phí

### Audit
Tạo lúc, Người tạo, Sửa lần cuối, Duyệt, In, Chuyển, Đảo ngược

### Routing
Hiển thị routing rules liên quan đến folio hiện tại (SourceFolio → TargetFolio, %)

### Phê duyệt
Trạng thái duyệt, Người duyệt

### Lịch sử
Timeline tạo + đảo ngược (nếu có)

---

## 6. Side Tab Content

### Tổng quan (Tab 0)
6 KPI cards: Charge hôm nay, Thanh toán, Còn nợ, Đặt cọc, Hoàn tiền, Hạn mức tín dụng

### Giao dịch (Tab 1 — default)
DataGrid 18 cột + Ribbon filter + Pager

### Thanh toán (Tab 2)
DataGrid 8 cột: Receipt, Reference, Method, Amount, Currency, Approval, Time, Operator — 80 payment records

### Đặt cọc (Tab 3)
DataGrid 7 cột: ID, Amount, Applied, Remaining, Currency, Status, Time — 30 deposit records

### Hoàn tiền (Tab 4)
DataGrid 7 cột: ID, Amount, Reason, Approved, Status, Time, Operator — 25 refund records

### Routing (Tab 5)
8 routing rules: Room→Master, Restaurant→Master, Spa→Master, Laundry→Master, Room→Company, Restaurant→Company, Room→TA, Extra→Master

### Lịch sử (Tab 6)
Timeline tài chính: Check-in → Payment → Charge → Deposit → ...

### Tài liệu (Tab 7)
4 documents: Receipt, Invoice, Voucher, Approval (PDF)

---

## 7. Mock Data

| Loại | Số lượng |
|------|----------|
| Folio | 13 (2 khách, 11 loại folio) |
| Transaction | 320 |
| Payment | 80 |
| Deposit | 30 |
| Refund | 25 |
| Routing Rule | 8 |
| Tổng | **476 records** |

Data đa dạng:
- 2 khách: Nguyễn Văn An (Phòng 101, RES-24001), Trần Thị Bình (Phòng 205, RES-24002)
- 3 currencies: VND (tỷ giá 1), USD (25,450), EUR (27,800)
- 14 transaction types: Charge, Payment, Deposit, Refund, Adjustment, Transfer, Tax, Discount, Service Charge
- 7 departments: FO, F&B, SPA, LAUN, MINI, EVENT, OTHER
- 8 outlets: Reception, Main Restaurant, Lobby Bar, Spa Center, Laundry, Room Service, Banquet, Pool Bar
- 5 payer types: Guest, Company, TA, House
- 6 operators: Lễ tân A/B, Thu ngân A/B, FO Sup, GSM
- 3 statuses: Posted, Pending, Reversed
- Company: Cty TNHH ABC (Guest 1)
- TA: Saigontourist (Guest 1), Viettravel (Guest 2)

---

## 8. Build & Runtime

| Metric | Value |
|--------|-------|
| Build (App) | PASS — 0 errors, 0 warnings |
| Build (Solution) | PASS — 0 errors, 98 pre-existing warnings |
| Runtime | App launched OK |
| Backend | Không đụng |
| Database | Không đụng |
| Banquet | Không đụng |

---

## 9. Files

| File | Trạng thái | Dòng |
|------|-----------|------|
| `Views/FolioWorkspaceWindow.xaml` | Rewrite hoàn toàn | 187 |
| `Views/FolioWorkspaceWindow.xaml.cs` | Rewrite hoàn toàn | 470 |

---

## 10. Kết luận

FOLIO WORKSPACE IMPLEMENTATION COMPLETE.

Financial Workspace đầy đủ: 13 folio, 320 giao dịch, 80 payment, 30 deposit, 25 refund, 8 routing rules.
3-column layout: Folio list → Transaction center → Inspector.
8 side tabs + 7 inspector sub-tabs.
Custom ControlTemplate, không WPF mặc định.
Mock data 476 records, sẵn sàng kết nối backend thật.
