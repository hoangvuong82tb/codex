# PHÂN TÍCH NGHIỆP VỤ THẬT — MODULE NHÀ HÀNG À LA CARTE

## Kết quả UAT + Code Trace — 24/06/2026

---

# TÓM TẮT

Sau khi trace toàn bộ code (3222 dòng service, 1600+ dòng ViewModel, 1400+ dòng XAML), kết luận:

**Service layer: ~70% hoàn thiện. ViewModel → UI: ~30% hoàn thiện. Khoảng cách chính nằm ở ViewModel không gọi service đã có.**

Cụ thể:
- `CreateOrderForQuoteAsync` **phân phối món đúng** xuống từng bàn (PerPerson, PerTable → bàn con; PerRoom → Bàn Tổng)
- `TableAllocationEngine` **chia bàn đúng** (1 Bàn Tổng ảo + N bàn thật)
- Nhưng **ViewModel không gọi** `IQuoteChangeService` → không thể đổi số khách từ UI
- **UI Bàn Tổng** hiển thị đúng (card riêng, không lẫn với bàn con)

---

# A. PHẦN NÀO ĐANG LÀ MOCKUP UI

| # | Vị trí | Mô tả | File:Line |
|---|---|---|---|
| 1 | **Tab "Menu có sẵn"** trong Quote | 4 set menu cứng ("Set tiệc cơ bản" 350k, "Set tiệc VIP" 650k, "Set hải sản" 850k, "Set lẩu" 450k). Nút "+" không có Command binding | `AlacarteRestaurantView.xaml` ~629-680 |
| 2 | **Tab "Nhóm món"** trong Settings | 10 entry cứng, form không có Command binding | `AlacarteRestaurantView.xaml` Settings tab 3 |
| 3 | **Tab "Món bán"** trong Settings | 7 entry cứng | `AlacarteRestaurantView.xaml` Settings tab 4 |
| 4 | **Tab "Máy in / Bếp bar"** trong Settings | 3 printer entry cứng, lưu file JSON | `AlacarteRestaurantView.xaml` Settings tab 2 |
| 5 | **Tab "Thanh toán"** trong Settings | 6 payment method cứng | Settings tab 6 |
| 6 | **Tab "Đặt bàn / Đặt cọc"** | Settings in-memory | Settings tab 7 |
| 7 | **Tab "Hóa đơn / Chứng từ"** | Settings in-memory | Settings tab 8 |
| 8 | **Tab "Phân quyền"** | 9 quyền cứng, checkbox không lưu | Settings tab 9 |
| 9 | **Tab "Đồng bộ / Tích hợp"** | 5 trạng thái cứng | Settings tab 10 |
| 10 | **Tab "Thông tin nhà hàng"** | Lưu JSON file, không DB | Settings tab 11 |

**Tổng: 10/13 Settings tab là MOCKUP.**

---

# B. PHẦN NÀO CÓ ENTITY NHƯNG CHƯA CÓ SERVICE

| # | Entity | Trạng thái Service |
|---|---|---|
| 1 | `QuoteChangeRequest` + `QuoteChangeRequestDetail` + `QuoteAuditLog` | **CÓ SERVICE** (`QuoteChangeService` đầy đủ) nhưng **KHÔNG ĐƯỢC GỌI** từ ViewModel |
| 2 | `BanquetMenuPackage` | Có service CRUD (`BanquetRestaurantService`) nhưng **chưa tích hợp** vào Quote flow của ALC |

**Tổng: 2 entity có service nhưng không được gọi đúng nơi.**

---

# C. PHẦN NÀO CÓ SERVICE NHƯNG CHƯA ĐƯỢC GỌI

| # | Service / Method | Được gọi từ ViewModel? |
|---|---|---|
| 1 | `IQuoteChangeService.PreviewAdjustmentAsync` | **KHÔNG** — không reference trong ViewModel |
| 2 | `IQuoteChangeService.RequestChangeAsync` | **KHÔNG** |
| 3 | `IQuoteChangeService.ApproveChangeRequestAsync` | **KHÔNG** |
| 4 | `IQuoteChangeService.ApplyChangeAsync` | **KHÔNG** |
| 5 | `IQuoteChangeService.GetChangeHistoryAsync` | **KHÔNG** |
| 6 | `IQuoteChangeService.GetAuditLogsAsync` | **KHÔNG** |
| 7 | `IBanquetRestaurantService.GetMenuPackagesAsync` (cho ALC Quote) | CÓ — `MenuPackages` collection được load, nhưng expand package → quote lines là client-side |
| 8 | `AlacarteRestaurantService.SendToPreparationAsync` (gửi bếp từng bàn) | CÓ — `PrintProvisionalCommand` gọi nhưng chưa rõ flow |

**Tổng: 6/8 service method quan trọng chưa được ViewModel gọi.**

---

# D. THIẾT KẾ CHUẨN — FULL FLOW

## D.1. Quote → Approve → Order

```
┌─────────────────────────────────────────────────────────────┐
│ 1. TẠO BÁO GIÁ                                              │
│    Input: Phòng, Ngày, Giờ, Số khách, Suất/Món, Dịch vụ    │
│    → SaveQuoteAsync                                         │
│    → TableAllocationEngine.Allocate(guestCount, standard)   │
│    → MenuCalculationEngine.Calculate cho từng dòng          │
│    Output: Quote (Nháp) + QuoteTables + QuoteLines          │
├─────────────────────────────────────────────────────────────┤
│ 2. GỬI BÁO GIÁ                                              │
│    → Quote.Status = "Đã gửi"                                │
│    → (Tương lai: gửi email/PDF)                             │
├─────────────────────────────────────────────────────────────┤
│ 3. DUYỆT BÁO GIÁ                                            │
│    → ApproveQuoteAsync                                      │
│    → Tạo TableReservation (Confirmed)                       │
│    → Tạo AlacarteOrder (Status = "Serving")                 │
│    → Tạo OrderTables từ QuoteTables                         │
│    → Phân phối OrderLines:                                  │
│        • PerPerson → mỗi bàn × GuestCount                   │
│        • PerTable  → mỗi bàn × 1                            │
│        • PerRoom   → Bàn Tổng (virtual)                     │
│    → Tạo ExtraServices (dịch vụ phòng)                      │
│    → Nếu có deposit → tạo Deposit + CashFundReceipt         │
│    Output: Order (Serving) + OrderTables + OrderLines       │
├─────────────────────────────────────────────────────────────┤
│ 4. THAY ĐỔI SỐ KHÁCH (Change Request)                       │
│    → RequestChangeAsync (Pending)                           │
│    → ApproveChangeRequestAsync (Approved)                   │
│    → ApplyChangeAsync                                       │
│        • Chưa gửi bếp → Full Rebuild (xóa order cũ, tạo mới)│
│        • Đã gửi bếp   → NotSupportedException (P0B)        │
│    Output: Order mới (Serving) + deposit re-linked          │
├─────────────────────────────────────────────────────────────┤
│ 5. PHỤC VỤ                                                 │
│    → AddOrUpdateLineAsync (thêm món theo bàn)               │
│    → AddExtraServiceToOrderAsync (thêm dịch vụ phòng)       │
│    → SendToPreparationAsync (gửi bếp)                       │
│    Output: PreparationTickets + StockIssues                 │
├─────────────────────────────────────────────────────────────┤
│ 6. THANH TOÁN                                               │
│    → PayOrderAsync (thanh toán tổng, không split)           │
│    → ReleaseOrderTablesAsync (giải phóng bàn)               │
│    Output: Order (Paid)                                     │
└─────────────────────────────────────────────────────────────┘
```

## D.2. Table Allocation — Cách hoạt động

```
Input:  47 khách, standard 10 khách/bàn
Output: 6 tables (1 virtual + 5 real)

TableAllocationEngine.Allocate(47, 10):
  [0] TableNo=0, DisplayName="Ban Tong",  GuestCount=47, IsVirtual=true
  [1] TableNo=1, DisplayName="Ban 01",    GuestCount=10, IsVirtual=false
  [2] TableNo=2, DisplayName="Ban 02",    GuestCount=10, IsVirtual=false
  [3] TableNo=3, DisplayName="Ban 03",    GuestCount=10, IsVirtual=false
  [4] TableNo=4, DisplayName="Ban 04",    GuestCount=10, IsVirtual=false
  [5] TableNo=5, DisplayName="Ban 05",    GuestCount=7,  IsVirtual=false
```

## D.3. Line Distribution — Cách phân phối món

```
QuoteLine: Tôm hùm (PerPerson, 1 con/khách, 350,000đ)

Sau khi phân phối (47 khách, 5 bàn):
  OrderLine 1: Bàn 01, 10 con, 3,500,000đ
  OrderLine 2: Bàn 02, 10 con, 3,500,000đ
  OrderLine 3: Bàn 03, 10 con, 3,500,000đ
  OrderLine 4: Bàn 04, 10 con, 3,500,000đ
  OrderLine 5: Bàn 05,  7 con, 2,450,000đ
  OrderLine 6: Bàn Tổng → KHÔNG có (PerPerson không vào Bàn Tổng)

QuoteLine: Cá song (PerTable, 1 con/bàn, 280,000đ)

Sau khi phân phối:
  OrderLine 7:  Bàn 01, 1 con, 280,000đ
  OrderLine 8:  Bàn 02, 1 con, 280,000đ
  OrderLine 9:  Bàn 03, 1 con, 280,000đ
  OrderLine 10: Bàn 04, 1 con, 280,000đ
  OrderLine 11: Bàn 05, 1 con, 280,000đ

QuoteLine: MC (PerRoom, 1 lần, 1,200,000đ)

Sau khi phân phối:
  ExtraService: Bàn Tổng, 1 lần, 1,200,000đ
```

## D.4. Virtual Total Table (Bàn Tổng)

```
Bàn Tổng:
  - IsVirtual = true
  - DisplayName = "Bàn Tổng"
  - GuestCount = tổng số khách
  - Chứa: món PerRoom/Manual + dịch vụ phòng
  - KHÔNG chứa: món PerPerson, PerTable (đã phân xuống bàn con)
  - Dùng để: Tổng hợp thanh toán
```

## D.5. Settlement (Thanh toán)

```
Hiện tại:
  → PayOrderAsync: thanh toán toàn bộ 1 lần
  → Recalculate(order): sum tất cả line (không Cancelled) + extra service
  → Không split payment

Tương lai (P1):
  → Hỗ trợ partial payment (PaidAmount += partial)
  → Hỗ trợ nhiều phương thức trong 1 lần
```

---

# E. ENTITY CÒN THIẾU

| # | Entity | Mục đích | Mức độ |
|---|---|---|---|
| 1 | **`TableSlot`** | Vị trí bàn vật lý trong phòng. Ánh xạ OrderTable (logic) → vị trí thật. Hiện OrderTable không có DiningTableId → phục vụ không biết bàn ở đâu | **P0** |
| 2 | **`ServingSet` / `ServingSetLine`** | Suất ăn (tập hợp món định sẵn). Hiện dùng ké `BanquetMenuPackage` nhưng chưa tích hợp đúng | **P1** |
| 3 | **`RoomSchedule`** | Lịch phòng theo giờ. Hiện chỉ có Reservation (theo ngày), chưa có calendar view | **P1** |
| 4 | **`Payment`** (tách khỏi Order) | Lịch sử thanh toán nhiều lần. Hiện PaymentStatus là field trên Order | **P1** |
| 5 | **`Invoice`** | Hóa đơn. Hiện chưa có entity | **P2** |
| 6 | **`RestaurantPrinter`** | Cấu hình máy in. Hiện lưu JSON file | **P2** |

---

# F. SERVICE CÒN THIẾU

| # | Service | Mục đích | Mức độ |
|---|---|---|---|
| 1 | **`ITableSlotService`** | CRUD vị trí bàn, gán OrderTable vào TableSlot | **P0** |
| 2 | **`IServingSetService`** | CRUD suất, tích hợp vào Quote flow | **P1** |
| 3 | **`IRoomScheduleService`** | Calendar view, conflict detection | **P1** |
| 4 | **`IPaymentService`** (tách từ AlacarteService) | Partial payment, multi-tender, receipt | **P1** |
| 5 | **`IInvoiceService`** | Tạo hóa đơn, in hóa đơn VAT | **P2** |
| 6 | **`IPrinterConfigService`** | Quản lý máy in trong DB | **P2** |

---

# G. VIEWMODEL CÒN THIẾU

| # | ViewModel / Command | Mục đích | Mức độ |
|---|---|---|---|
| 1 | **`RequestChangeCommand`** | Gọi `IQuoteChangeService.RequestChangeAsync` | **P0** |
| 2 | **`PreviewAdjustmentCommand`** | Gọi `IQuoteChangeService.PreviewAdjustmentAsync` → hiển thị preview | **P0** |
| 3 | **`ApproveChangeCommand`** | Gọi `IQuoteChangeService.ApproveChangeRequestAsync` | **P0** |
| 4 | **`ApplyChangeCommand`** | Gọi `IQuoteChangeService.ApplyChangeAsync` | **P0** |
| 5 | **`SendToPreparationByTableCommand`** | Gửi bếp riêng từng bàn (hiện gửi tất cả) | **P1** |
| 6 | **`SplitPaymentCommand`** | Thanh toán từng phần | **P1** |
| 7 | **`AssignTableSlotCommand`** | Gán bàn logic vào vị trí vật lý | **P1** |
| 8 | **`ManageServingSetsCommand`** | Quản lý suất | **P1** |
| 9 | **`ViewAuditLogCommand`** | Xem lịch sử thay đổi Quote | **P2** |

---

# H. ROADMAP P0 → P3

## P0 — VẬN HÀNH ĐƯỢC FLOW CƠ BẢN (1-2 tuần)

| # | Hạng mục | File | Mô tả |
|---|---|---|---|
| P0.1 | **Wire Change Request vào ViewModel** | `RestaurantViewModels.cs` | Inject `IQuoteChangeService`, thêm 4 command: RequestChange, PreviewAdjustment, ApproveChange, ApplyChange |
| P0.2 | **TableSlot entity + migration** | `RestaurantEntities.cs`, migration | Thêm `TableSlot` (DiningRoomId, SlotNo, Capacity, PositionX, PositionY) |
| P0.3 | **Gán DiningTableId cho OrderTable** | `QuoteChangeService.cs`, `RestaurantServices.cs` | Khi FullRebuild hoặc CreateOrderForQuote, gán OrderTable vào TableSlot → tạo DiningTable nếu chưa có |
| P0.4 | **Sửa GetOpenOrdersAsync lọc "Replaced"** | `RestaurantServices.cs` line 1138 | Thêm `&& x.Status != "Replaced"` |
| P0.5 | **Xóa dead code `if (false && ...)`** | `RestaurantViewModels.cs` line 1970 | Dọn code chết |

## P1 — HOÀN THIỆN FLOW CHÍNH (2-3 tuần)

| # | Hạng mục |
|---|---|
| P1.1 | **ServingSet entity + CRUD + tích hợp Quote** |
| P1.2 | **Partial payment + multi-tender** |
| P1.3 | **Gửi bếp riêng từng bàn** |
| P1.4 | **Calendar view lịch phòng** |
| P1.5 | **Case B Change Request (đã gửi bếp)** — Adjustment Ticket + Cancel Ticket |

## P2 — TRẢI NGHIỆM ĐẦY ĐỦ (2-3 tuần)

| # | Hạng mục |
|---|---|
| P2.1 | **Invoice entity + in hóa đơn VAT** |
| P2.2 | **Dọn 10 Settings tab mockup → redirect module có sẵn** |
| P2.3 | **Printer config trong DB** |
| P2.4 | **Đổi phòng, đổi suất** |
| P2.5 | **Audit log UI (xem lịch sử thay đổi Quote)** |

## P3 — HOÀN THIỆN (1-2 tuần)

| # | Hạng mục |
|---|---|
| P3.1 | **Báo cáo doanh thu theo phòng, theo suất** |
| P3.2 | **Tích hợp Định lượng (giá vốn)** |
| P3.3 | **No-show, hủy đơn, phí hủy** |
| P3.4 | **Gửi email báo giá** |

---

# KẾT LUẬN

**Service layer đã làm đúng. ViewModel chưa gọi.** Đây là vấn đề chính:

1. **Phân phối món xuống bàn con**: **ĐÃ ĐÚNG** trong `CreateOrderForQuoteAsync` (lines 1925-1944). PerPerson và PerTable được phân xuống từng `realTables`, không nằm ở Bàn Tổng.

2. **Bàn Tổng**: **ĐÃ ĐÚNG** — là virtual table (`IsVirtual=true`), hiển thị riêng thành card BÀN TỔNG, không lẫn với bàn con.

3. **Đổi số khách**: **ĐÃ CÓ SERVICE** (`QuoteChangeService`) nhưng **CHƯA CÓ VIEWMODEL GỌI**. Đây là lý do UAT thấy "không chia lại bàn" — vì không có nút nào để trigger.

4. **P0 cần làm ngay**: Wire Change Request vào ViewModel (4 command) + TableSlot entity + sửa GetOpenOrders lọc Replaced.
