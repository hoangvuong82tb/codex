# ALACARTE_CHANGE_REQUEST_V1.md

## Thiết kế nghiệp vụ: Tăng/Giảm Khách Sau Khi Đã Duyệt Báo Giá

Ngày: 24/06/2026

---

# PHẦN 1: TỔNG QUAN

## 1.1. Vấn đề

Trong nhà hàng VIP, số khách thường xuyên thay đổi SAU KHI báo giá đã được duyệt và đơn mở đã được tạo. Ví dụ:

- Đặt 40 khách → khách báo lên 47 người
- Đặt 40 khách → 5 người hủy, còn 35

Hiện tại hệ thống **không cho phép** thay đổi sau khi duyệt (`SaveQuoteAsync` chặn nếu `ApprovedOrderId.HasValue`).

## 1.2. Mục tiêu

Thiết kế workflow **Change Request** cho phép:

1. Tăng/giảm số khách sau khi báo giá đã duyệt
2. Tự động tính lại: bàn, món PerGuest, món PerTable, tổng tiền
3. Xử lý đúng với trạng thái bếp (chưa gửi / đã gửi một phần / đã phục vụ)
4. Audit trail đầy đủ
5. Không làm mất dữ liệu, không sai tiền

---

# PHẦN 2: STATE MACHINE HIỆN TẠI

## 2.1. Quote

```
[Nháp] → [Đã gửi] → [Đã duyệt] → (order Serving)
                          ↓
                      [Lưu trữ]
```

- `SaveQuoteAsync` chặn sửa nếu `ApprovedOrderId.HasValue` (line 1300-1303)
- `ApproveQuoteAsync` idempotent — gọi 2 lần không tạo trùng (line 1452)

## 2.2. Order

```
[Draft] → [Serving] → [SentKitchen] → [Paid] (terminal)
                       ↓                      ↓
                   [Cancelled] (terminal)  [Cancelled] (terminal)
```

- `EnsureOrderEditable` (line 2635): chặn khi `Status is "Paid" or "Cancelled"`
- `SendToPreparationAsync`: gửi tất cả line có `KitchenStatus != "Sent" && LineStatus != "Cancelled"`
- `PayOrderAsync`: yêu cầu `paidAmount >= RemainingAmount` → set `Paid`

## 2.3. OrderLine

```
KitchenStatus: New → Sent → (adjustment creates shadow "Adjustment" line)
LineStatus:    Ordered → Cancelled
LineType:      Normal → Supplemental (post-send add)
```

---

# PHẦN 3: CHANGE REQUEST WORKFLOW

## 3.1. State Machine mới cho Quote

Thêm trạng thái **"Đang sửa" (Revising)**:

```
                        ┌──────────────────────────┐
                        │      ĐÃ DUYỆT             │
                        │  (order = Serving/        │
                        │   SentKitchen)            │
                        └─────────┬────────────────┘
                                  │ Khách yêu cầu
                                  │ tăng/giảm người
                        ┌─────────▼────────────────┐
                        │   YÊU CẦU SỬA             │
                        │   (Change Requested)      │
                        └─────────┬────────────────┘
                                  │ Quản lý duyệt
                        ┌─────────▼────────────────┐
                        │   ĐANG SỬA                │
                        │   (Revising)              │
                        │   Order bị khóa tạm thời  │
                        └─────────┬────────────────┘
                                  │ Áp dụng thay đổi
                        ┌─────────▼────────────────┐
                        │   ĐÃ DUYỆT                │
                        │   (order được rebuild/    │
                        │    adjusted)              │
                        └──────────────────────────┘
```

## 3.2. Các trạng thái mới

| Trạng thái | Mô tả | Hành động được phép |
|---|---|---|
| **Đã duyệt** | Quote đã duyệt, order đang Serving/SentKitchen | Yêu cầu sửa (Request Change) |
| **Yêu cầu sửa** | Khách yêu cầu thay đổi, chờ quản lý duyệt | Duyệt / Từ chối / Sửa |
| **Đang sửa** | Đang áp dụng thay đổi, order bị khóa | Sửa GuestCount, xem Adjustment Plan |
| **Đã duyệt** (sau sửa) | Thay đổi đã áp dụng, order mới hoặc đã điều chỉnh | Tiếp tục phục vụ |

## 3.3. Chi tiết workflow

```
Bước 1: KHÁCH YÊU CẦU THAY ĐỔI
  → Nhân viên mở Quote đã duyệt
  → Ấn "Yêu cầu sửa" (Request Change)
  → Nhập: số khách MỚI, lý do
  → Hệ thống tạo ChangeRequest (trạng thái: Pending)
  → Quote status → "Yêu cầu sửa"

Bước 2: QUẢN LÝ DUYỆT
  → Quản lý xem ChangeRequest
  → Xem trước Adjustment Plan (Preview):
      - Bàn mới: thêm/bớt bao nhiêu
      - Món PerGuest: tăng/giảm số lượng
      - Món PerTable: tăng/giảm số lượng (nếu số bàn thay đổi)
      - Dịch vụ: không đổi (PerRoom)
      - Chênh lệch tiền: +X / -Y
  → Duyệt hoặc Từ chối

Bước 3: ÁP DỤNG THAY ĐỔI
  → Quote status → "Đang sửa"
  → Order bị khóa (không thể sửa, không thể gửi bếp)
  → Tùy theo trạng thái bếp:

    CASE A: CHƯA GỬI BẾP (KitchenStatus = "New" cho tất cả line)
      → Xóa order cũ (soft delete: Status = "Replaced")
      → Cập nhật Quote: GuestCount mới, tính lại bàn, tính lại món
      → Tạo order MỚI từ Quote (gọi CreateOrderForQuoteAsync)
      → Giữ nguyên QuoteId, tạo OrderNo mới
      → Copy payment history sang order mới

    CASE B: ĐÃ GỬI BẾP MỘT PHẦN
      → KHÔNG xóa order
      → Tạo Adjustment Plan:
          - Tăng khách → thêm bàn mới (nếu cần), thêm món vào bàn mới
          - Giảm khách → giảm món trên bàn lẻ, hủy bàn thừa
          - Món đã Sent → KHÔNG sửa số lượng, chỉ đánh dấu
          - Món chưa Sent → cập nhật số lượng
      → Tạo ticket điều chỉnh (Adjustment Ticket) cho bếp
      → Cập nhật order line quantities

    CASE C: ĐÃ PHỤC VỤ / THANH TOÁN MỘT PHẦN
      → KHÔNG sửa số lượng đã phục vụ
      → Chỉ thêm món mới (tăng khách) hoặc giảm món chưa làm (giảm khách)
      → Cảnh báo nếu có món đã Sent không thể giảm

Bước 4: HOÀN TẤT
  → Quote status → "Đã duyệt"
  → Mở khóa order
  → Lưu Audit Trail
```

---

# PHẦN 4: ENTITY CẦN THÊM

## 4.1. QuoteChangeRequest (Yêu cầu sửa báo giá)

```
QuoteChangeRequest
├── Id: Guid
├── QuoteId: Guid (FK → RestaurantQuote)
├── RequestNumber: string ("CR-20260624-001")
├── PreviousGuestCount: int (40)
├── NewGuestCount: int (47)
├── GuestCountDelta: int (+7)
├── Reason: string ("Khách báo thêm 7 người")
├── Status: enum (Pending / Approved / Rejected / Applied)
├── RequestedBy: string (người yêu cầu)
├── RequestedAt: datetime
├── ReviewedBy: string (người duyệt, nullable)
├── ReviewedAt: datetime (nullable)
├── AppliedBy: string (người áp dụng, nullable)
├── AppliedAt: datetime (nullable)
├── PreviousTotalAmount: decimal
├── NewTotalAmount: decimal
├── AmountDelta: decimal
├── AdjustmentPlanJson: string (JSON — kế hoạch điều chỉnh chi tiết)
├── PreviousOrderId: Guid (order cũ)
├── NewOrderId: Guid (order mới, nullable — chỉ có trong Case A)
└── Note: string
```

## 4.2. QuoteChangeRequestDetail (Chi tiết thay đổi từng dòng)

```
QuoteChangeRequestDetail
├── Id: Guid
├── ChangeRequestId: Guid (FK → QuoteChangeRequest)
├── QuoteLineId: Guid? (dòng báo giá gốc)
├── OrderLineId: Guid? (dòng đơn mở bị ảnh hưởng)
├── ChangeType: enum (Added / Removed / QuantityChanged / TableChanged / Unchanged)
├── ItemName: string
├── PreviousQuantity: decimal
├── NewQuantity: decimal
├── PreviousAmount: decimal
├── NewAmount: decimal
├── OrderTableId: Guid? (bàn bị ảnh hưởng)
├── IsAlreadySent: bool (đã gửi bếp chưa?)
└── Note: string
```

## 4.3. QuoteAuditLog (Nhật ký thay đổi — dùng chung cho mọi thay đổi Quote)

```
QuoteAuditLog
├── Id: Guid
├── QuoteId: Guid (FK → RestaurantQuote)
├── Timestamp: datetime
├── Action: enum (Created / Sent / Approved / ChangeRequested / ChangeApproved / ChangeRejected / ChangeApplied / GuestCountChanged / LineChanged / Cancelled)
├── ChangedBy: string
├── FieldName: string (nullable — "GuestCount", "Line.TomHum.Quantity")
├── OldValue: string (nullable)
├── NewValue: string (nullable)
├── ChangeRequestId: Guid? (FK → QuoteChangeRequest)
└── Note: string
```

---

# PHẦN 5: DTO CẦN THÊM

## 5.1. ChangeRequestDto

```
ChangeRequestDto
├── Id, QuoteId, RequestNumber
├── PreviousGuestCount, NewGuestCount, GuestCountDelta
├── Reason, Status
├── RequestedBy, RequestedAt
├── ReviewedBy, ReviewedAt
├── PreviousTotalAmount, NewTotalAmount, AmountDelta
├── Details: List<ChangeRequestDetailDto>
├── CanApply: bool (có thể áp dụng không?)
├── OrderStatus: string (trạng thái order hiện tại: Serving / SentKitchen / Paid)
├── HasSentLines: bool (đã có line nào gửi bếp chưa?)
└── Note
```

## 5.2. ChangeRequestDetailDto

```
ChangeRequestDetailDto
├── Id
├── ChangeType (Added / Removed / QuantityChanged / TableChanged / Unchanged)
├── ItemName
├── PreviousQuantity, NewQuantity, QuantityDelta
├── PreviousAmount, NewAmount, AmountDelta
├── TableDisplayName (tên bàn bị ảnh hưởng)
├── IsAlreadySent
└── Warning (cảnh báo nếu không thể thay đổi)
```

## 5.3. PreviewAdjustmentRequest / Response

```
PreviewAdjustmentRequest
├── QuoteId
├── NewGuestCount

PreviewAdjustmentResponse
├── ChangeRequestDto (preview)
├── TableChanges: List<TableChange>
│     ├── Action (Add / Remove / Keep)
│     ├── TableDisplayName
│     ├── GuestCount
│     └── IsVirtual
├── LineChanges: List<ChangeRequestDetailDto>
├── TotalDelta: decimal
├── Warnings: List<string>
└── RequiresFullRebuild: bool (true nếu chưa gửi bếp → Case A)
```

## 5.4. ApplyChangeRequest

```
ApplyChangeRequest
├── ChangeRequestId
├── ConfirmedBy: string
```

---

# PHẦN 6: SERVICE CẦN THÊM

## 6.1. IQuoteChangeService (interface mới)

```
IQuoteChangeService
├── PreviewAdjustmentAsync(quoteId, newGuestCount)
│     → Trả về PreviewAdjustmentResponse (tính toán trước, không lưu gì)
│
├── RequestChangeAsync(quoteId, newGuestCount, reason, requestedBy)
│     → Tạo QuoteChangeRequest (Pending)
│     → Quote status → "Yêu cầu sửa"
│     → Ghi audit log
│
├── ApproveChangeRequestAsync(changeRequestId, reviewedBy)
│     → ChangeRequest status → Approved
│     → Ghi audit log
│
├── RejectChangeRequestAsync(changeRequestId, reviewedBy)
│     → ChangeRequest status → Rejected
│     → Quote status → "Đã duyệt" (quay lại)
│     → Ghi audit log
│
├── ApplyChangeRequestAsync(changeRequestId, appliedBy)
│     → ChangeRequest status → Applied
│     → TH Case A: RebuildOrderFromQuote(quoteId, changeRequestId)
│     → TH Case B/C: ApplyAdjustmentPlan(orderId, adjustmentPlanJson)
│     → Quote status → "Đã duyệt"
│     → Cập nhật GuestCount, TotalAmount trên Quote
│     → Ghi audit log
│
├── GetChangeHistoryAsync(quoteId)
│     → Trả về List<QuoteAuditLog> + List<QuoteChangeRequest>
```

## 6.2. Cập nhật IAlacarteRestaurantService

Thêm method:

```
IAlacarteRestaurantService (bổ sung)
├── RebuildOrderFromQuoteAsync(quoteId, changeRequestId)
│     → Xóa order cũ (soft delete: Status = "Replaced")
│     → Cập nhật Quote (GuestCount, tables, lines)
│     → Gọi CreateOrderForQuoteAsync (tạo order mới)
│     → Copy payments sang order mới
│     → Trả về OrderDto mới
│
├── ApplyAdjustmentPlanAsync(orderId, adjustmentPlanJson)
│     → Parse JSON → thêm/xóa/sửa OrderLine, OrderTable
│     → Tạo Adjustment Ticket cho bếp
│     → Recalculate(order)
│     → Trả về OrderDto đã cập nhật
```

---

# PHẦN 7: UI CẦN THÊM

## 7.1. Nút "Yêu cầu sửa" trên Quote đã duyệt

- Vị trí: Tab Báo giá, khi Quote status = "Đã duyệt"
- Click → mở dialog "Yêu cầu thay đổi"
- Dialog hiển thị:
  - Số khách hiện tại
  - Input: số khách mới
  - Input: lý do
  - Nút "Xem trước" (Preview) → gọi PreviewAdjustmentAsync
  - Nút "Gửi yêu cầu" → gọi RequestChangeAsync

## 7.2. Dialog "Xem trước thay đổi" (Preview Adjustment)

Hiển thị kết quả từ `PreviewAdjustmentResponse`:

- **Thay đổi bàn**:
  - "Thêm 1 bàn (Bàn 05, 7 khách)" hoặc
  - "Bỏ bàn lẻ (Bàn 05, 5 khách)" hoặc
  - "Giữ nguyên 4 bàn"

- **Thay đổi món** (bảng):
  | Món | Trước | Sau | Delta | Cảnh báo |
  |-----|-------|-----|-------|----------|
  | Tôm hùm | 40 | 47 | +7 | — |
  | Cá song | 4 bàn | 5 bàn | +1 | — |
  | Bào ngư | 40 | 47 | +7 | — |
  | MC | 1 | 1 | 0 | Không đổi (PerRoom) |

- **Chênh lệch tiền**: +10.500.000 đ

- **Trạng thái bếp**: "Chưa gửi bếp → có thể rebuild toàn bộ" hoặc "Đã gửi bếp 3/5 món → chỉ điều chỉnh được 2 món chưa gửi"

- **Cảnh báo** (nếu có):
  - "Món 'Tôm hùm' đã gửi bếp (40 con) — không thể tăng lên 47. Cần tạo Adjustment Ticket riêng."
  - "Món 'Cá song' đã gửi bếp (4 con) — cần gửi thêm 1 con cho bàn mới."

## 7.3. Dialog "Duyệt yêu cầu thay đổi" (cho Quản lý)

- Danh sách ChangeRequest đang Pending
- Click → xem chi tiết
- Nút "Duyệt" / "Từ chối" / "Sửa lại"

## 7.4. Dialog "Áp dụng thay đổi"

- Xác nhận lần cuối trước khi áp dụng
- Hiển thị: "Order sẽ bị khóa trong quá trình áp dụng (khoảng 5-10 giây)"
- Nếu Case A: "Sẽ tạo đơn mở mới thay thế đơn cũ"
- Nếu Case B: "Sẽ tạo Adjustment Ticket cho bếp"
- Nút "Xác nhận & Áp dụng"

## 7.5. Tab "Lịch sử thay đổi" trong Quote detail

- Timeline view:
  ```
  24/06 14:00 — Ng.Văn A tạo báo giá (40 khách, 67.5tr)
  24/06 14:30 — Trần Thị B duyệt báo giá
  24/06 15:00 — Ng.Văn A yêu cầu sửa: 40→47 khách
  24/06 15:15 — Trần Thị B duyệt yêu cầu sửa
  24/06 15:16 — Hệ thống áp dụng: +7 khách, +10.5tr, thêm 1 bàn
  ```

---

# PHẦN 8: LUỒNG NGHIỆP VỤ CHI TIẾT

## 8.1. Tăng khách (40 → 47)

### Case A: Chưa gửi bếp

```
Input:  40 khách, 4 bàn (10+10+10+10), 5 món
Output: 47 khách, 5 bàn (10+10+10+10+7), 5 món (tăng SL)

Engine tính:
  TableAllocationEngine.Allocate(47, 10)
    → 4 bàn × 10 + 1 bàn × 7

  PerGuest lines:
    Tôm hùm: 40 → 47 (+7)
    Bào ngư: 40 → 47 (+7)
    Tráng miệng: 40 → 47 (+7)

  PerTable lines:
    Cá song: 4 bàn → 5 bàn (+1)
    Salad: 4 bàn → 5 bàn (+1)

  PerRoom:
    MC: 1 → 1 (không đổi)
    Âm thanh: 1 → 1 (không đổi)

  Tiền:
    Trước: 67.500.000
    Sau:   79.200.000
    Delta: +11.700.000

Thực hiện:
  1. Order.Status → "Replaced" (soft delete)
  2. Quote.GuestCount → 47
  3. Quote.TotalAmount → 79.200.000
  4. CreateOrderForQuoteAsync → order mới
  5. Copy payments từ order cũ
  6. Audit log
```

### Case B: Đã gửi bếp 3/5 món

```
Đã gửi: Tôm hùm (40), Cá song (4), Salad (4)
Chưa gửi: Bào ngư (40), Tráng miệng (40)

Adjustment Plan:
  [ADD] Bàn 05 (7 khách)
  [ADD] Tôm hùm cho Bàn 05: +7 (Adjustment Ticket)
  [ADD] Cá song cho Bàn 05: +1 (Adjustment Ticket)
  [ADD] Salad cho Bàn 05: +1 (Adjustment Ticket)
  [UPDATE] Bào ngư: 40 → 47 (chưa gửi, sửa trực tiếp)
  [UPDATE] Tráng miệng: 40 → 47 (chưa gửi, sửa trực tiếp)

Thực hiện:
  1. Tạo OrderTable mới: Bàn 05 (7 khách)
  2. Với món đã Sent → tạo Adjustment Ticket riêng
  3. Với món chưa Sent → cập nhật Quantity trực tiếp
  4. Recalculate(order)
  5. Audit log
```

## 8.2. Giảm khách (47 → 38)

### Case A: Chưa gửi bếp

```
Input:  47 khách, 5 bàn (10+10+10+10+7)
Output: 38 khách, 4 bàn (10+10+10+8)

Engine tính:
  TableAllocationEngine.Allocate(38, 10)
    → 3 bàn × 10 + 1 bàn × 8 = 4 bàn

  PerGuest lines: 47 → 38 (-9 mỗi món)
  PerTable lines: 5 → 4 (-1 mỗi món)
  PerRoom: không đổi

Thực hiện: Rebuild toàn bộ (như Case A tăng)
```

### Case B: Đã gửi bếp

```
Đã gửi: Tôm hùm (47), Cá song (5), Salad (5)

Adjustment Plan:
  [REMOVE] Bàn 05 (7 khách)
  [ADJUST] Bàn 04: 10 → 8 khách
  
  [CANCEL] Tôm hùm cho Bàn 05: -7 (Cancel Ticket)
  [CANCEL] Cá song cho Bàn 05: -1 (Cancel Ticket)
  [CANCEL] Salad cho Bàn 05: -1 (Cancel Ticket)
  
  Món chưa gửi: cập nhật trực tiếp

Thực hiện:
  1. Đánh dấu Bàn 05 → Removed
  2. Cập nhật Bàn 04 GuestCount: 10 → 8
  3. Tạo Cancel Ticket cho món đã Sent trên Bàn 05
  4. Cập nhật Quantity cho món chưa Sent
  5. Recalculate(order)
  6. Audit log
```

---

# PHẦN 9: EDGE CASES

| # | Tình huống | Cách xử lý |
|---|---|---|
| E1 | **Tăng khách vượt sức chứa phòng** | Từ chối, đề xuất phòng khác hoặc tách thành 2 phòng |
| E2 | **Giảm khách xuống dưới minimum của suất** | Cảnh báo: "Suất VIP B yêu cầu tối thiểu 20 khách. Hiện còn 18." Cho phép override với lý do |
| E3 | **Order đã Paid** | Từ chối — không thể sửa đơn đã thanh toán |
| E4 | **Order đã Cancelled** | Từ chối — không thể sửa đơn đã hủy |
| E5 | **Đang có ChangeRequest Pending** | Từ chối — phải giải quyết request hiện tại trước |
| E6 | **Tăng khách → cần thêm bàn nhưng hết TableSlot** | Cảnh báo, đề xuất tăng sức chứa bàn hiện có (10→12) |
| E7 | **Giảm khách → bàn lẻ 0 khách** | Tự động xóa bàn lẻ |
| E8 | **Thay đổi trong lúc đang gửi bếp** | Khóa order, chờ gửi bếp xong rồi mới áp dụng |
| E9 | **Khách đổi cả suất (không chỉ đổi số lượng)** | Tạo Quote mới, không dùng Change Request |
| E10 | **Tăng khách sau khi đã thanh toán 1 phần** | Cho phép, nhưng không sửa món đã thanh toán. Tạo Adjustment riêng |
| E11 | **2 người cùng yêu cầu sửa 1 Quote** | Optimistic concurrency — dùng RowVersion hoặc check `UpdatedAt` |
| E12 | **Rollback nếu Apply thất bại giữa chừng** | Transactional — nếu Case A: rollback toàn bộ. Nếu Case B: rollback từng bước với savepoint |

---

# PHẦN 10: MIGRATION CẦN THIẾT

## 10.1. Bảng mới

```
Migration: AddQuoteChangeRequest

1. QuoteChangeRequests
   - Id (Guid, PK)
   - QuoteId (Guid, FK → RestaurantQuotes.Id)
   - RequestNumber (nvarchar(50))
   - PreviousGuestCount (int)
   - NewGuestCount (int)
   - GuestCountDelta (int, computed)
   - Reason (nvarchar(500))
   - Status (nvarchar(50)) — Pending/Approved/Rejected/Applied
   - RequestedBy (nvarchar(200))
   - RequestedAt (datetime2)
   - ReviewedBy (nvarchar(200), nullable)
   - ReviewedAt (datetime2, nullable)
   - AppliedBy (nvarchar(200), nullable)
   - AppliedAt (datetime2, nullable)
   - PreviousTotalAmount (decimal(18,2))
   - NewTotalAmount (decimal(18,2))
   - AmountDelta (decimal(18,2), computed)
   - AdjustmentPlanJson (nvarchar(max), nullable)
   - PreviousOrderId (Guid, FK → AlacarteOrders.Id)
   - NewOrderId (Guid, nullable, FK → AlacarteOrders.Id)
   - Note (nvarchar(500), nullable)
   - CreatedAt, UpdatedAt

2. QuoteChangeRequestDetails
   - Id (Guid, PK)
   - ChangeRequestId (Guid, FK)
   - QuoteLineId (Guid, nullable)
   - OrderLineId (Guid, nullable)
   - ChangeType (nvarchar(50)) — Added/Removed/QuantityChanged/TableChanged/Unchanged
   - ItemName (nvarchar(300))
   - PreviousQuantity (decimal(18,4))
   - NewQuantity (decimal(18,4))
   - PreviousAmount (decimal(18,2))
   - NewAmount (decimal(18,2))
   - OrderTableId (Guid, nullable)
   - IsAlreadySent (bit)
   - Note (nvarchar(500), nullable)

3. QuoteAuditLogs
   - Id (Guid, PK)
   - QuoteId (Guid, FK)
   - Timestamp (datetime2)
   - Action (nvarchar(50))
   - ChangedBy (nvarchar(200))
   - FieldName (nvarchar(200), nullable)
   - OldValue (nvarchar(500), nullable)
   - NewValue (nvarchar(500), nullable)
   - ChangeRequestId (Guid, nullable, FK)
   - Note (nvarchar(500), nullable)
```

## 10.2. Cập nhật bảng hiện có

```
Không cần thay đổi schema bảng hiện có.
Quote status "Yêu cầu sửa" / "Đang sửa" chỉ là giá trị string mới của field Status.
Order status "Replaced" chỉ là giá trị string mới của field Status.
```

---

# PHẦN 11: KẾ HOẠCH TRIỂN KHAI P0

## P0 — TỐI THIỂU KHẢ DỤNG

| # | Hạng mục | File | Mô tả |
|---|---|---|---|
| P0.1 | **Entity** | `RestaurantEntities.cs` | Thêm `QuoteChangeRequest`, `QuoteChangeRequestDetail`, `QuoteAuditLog` |
| P0.2 | **DTO** | `RestaurantDtos.cs` | Thêm `ChangeRequestDto`, `ChangeRequestDetailDto`, `PreviewAdjustmentResponse` |
| P0.3 | **Migration** | `Migrations/` | Tạo 3 bảng mới |
| P0.4 | **DbSet** | `AppDbContext.cs` | Đăng ký 3 DbSet mới |
| P0.5 | **Interface** | `IRestaurantServices.cs` | Thêm `IQuoteChangeService` |
| P0.6 | **Preview** | `RestaurantServices.cs` | `PreviewAdjustmentAsync` — tính toán, không lưu |
| P0.7 | **Request** | `RestaurantServices.cs` | `RequestChangeAsync` — tạo ChangeRequest (Pending) |
| P0.8 | **Approve/Reject** | `RestaurantServices.cs` | `ApproveChangeRequestAsync`, `RejectChangeRequestAsync` |
| P0.9 | **Apply — Case A** | `RestaurantServices.cs` | `RebuildOrderFromQuoteAsync` — xóa order cũ, tạo mới (chưa gửi bếp) |
| P0.10 | **Apply — Case B** | `RestaurantServices.cs` | `ApplyAdjustmentPlanAsync` — điều chỉnh order (đã gửi bếp) |
| P0.11 | **Audit Log** | `RestaurantServices.cs` | Ghi log mỗi bước |
| P0.12 | **ViewModel** | `RestaurantViewModels.cs` | `RequestChangeCommand`, `PreviewAdjustmentCommand` |
| P0.13 | **XAML** | `AlacarteRestaurantView.xaml` | Dialog "Yêu cầu sửa", "Xem trước", "Duyệt", "Áp dụng" |
| P0.14 | **XAML** | `AlacarteRestaurantView.xaml` | Tab "Lịch sử thay đổi" trong Quote detail |

## Thứ tự làm

```
P0.1 → P0.2 → P0.3 → P0.4 (Entity + DTO + Migration + DbSet)
  ↓
P0.5 → P0.6 (Interface + Preview — có thể test ngay)
  ↓
P0.7 → P0.8 (Request + Approve/Reject — flow duyệt)
  ↓
P0.9 → P0.10 (Apply Case A + B — core logic)
  ↓
P0.11 (Audit log xuyên suốt)
  ↓
P0.12 → P0.13 → P0.14 (ViewModel + UI)
```

---

# PHẦN 12: KẾT LUẬN

1. **Change Request workflow là CẦN THIẾT** cho nhà hàng VIP. Không có nó, mọi thay đổi sau duyệt đều phải làm thủ công (hủy đơn → tạo quote mới → duyệt lại).

2. **Thiết kế này phân biệt rõ 3 Case**:
   - **Case A** (chưa gửi bếp): Rebuild toàn bộ — sạch, đơn giản
   - **Case B** (đã gửi bếp một phần): Adjustment Plan — phức tạp hơn nhưng an toàn
   - **Case C** (đã phục vụ/thanh toán): Hạn chế — chỉ thêm mới, không sửa cũ

3. **Audit trail là bắt buộc** — mỗi thay đổi phải có: ai, lúc nào, cái gì, trước/sau, chênh lệch tiền.

4. **P0 nên tập trung vào Case A trước** (chưa gửi bếp) vì:
   - Phổ biến nhất (khách thường báo thay đổi TRƯỚC giờ ăn)
   - Đơn giản nhất (rebuild toàn bộ)
   - An toàn nhất (không có dữ liệu đã gửi bếp)

5. **Case B có thể làm P1** vì phức tạp hơn và ít xảy ra hơn (khách ít khi đổi sau khi đã bắt đầu nấu).
