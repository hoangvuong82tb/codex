# ALACARTE_CHANGE_REQUEST_REVIEW.md

## Đánh giá thiết kế & triển khai Change Request — Góc nhìn Nghiệp vụ

Ngày: 24/06/2026

Người đánh giá: Restaurant Operations Consultant / ERP Architect / Senior BA

---

# TÓM TẮT

Thiết kế Change Request P0-A có **nền tảng đúng**, nhưng **chưa xử lý nhiều tình huống nghiệp vụ phức tạp**. Có 5 lỗ hổng CRITICAL cần sửa trước khi đưa vào production.

---

# PHẦN 1: PHÂN TÍCH THEO TÌNH HUỐNG

---

## 1. TĂNG KHÁCH: 40 → 47

### Case A (chưa gửi bếp): HOẠT ĐỘNG ĐÚNG

- Full rebuild: order cũ → "Replaced", order mới tạo
- Bàn: 4 → 5 (thêm 1 bàn 7 khách)
- Món PerGuest: tăng số lượng (40 → 47)
- Món PerTable: tăng (4 bàn → 5 bàn)
- Tiền tính lại đúng

### Case B (đã gửi bếp): CÓ VẤN ĐỀ

- Bàn mới được thêm (`RestaurantOrderTable`) **không có DiningTableId** — bàn logic không gắn với bàn vật lý nào trong phòng
- Bếp không biết "Bàn 05" nằm ở đâu
- **Rủi ro**: Phục vụ bê món ra không biết để vào đâu

---

## 2. GIẢM KHÁCH: 47 → 38

### Case A (chưa gửi bếp): HOẠT ĐỘNG ĐÚNG

### Case B (đã gửi bếp): CÓ VẤN ĐỀ

- Bàn bị xóa: `Status = "Removed"` nhưng món đã gửi bếp trên bàn đó chỉ được `LineStatus = "Cancelled"`
- **KHÔNG tạo Cancel Ticket cho bếp** — bếp đã nấu món nhưng không được báo hủy
- Stock issue đã tạo từ lần gửi bếp trước **không được đảo ngược** → tồn kho vẫn bị trừ dù món đã hủy
- **Rủi ro**: Bếp nấu thừa, tồn kho sai

---

## 3. ĐỔI PHÒNG: VIP1 → VIP2

### KHÔNG ĐƯỢC HỖ TRỢ

`QuoteChangeService` không có method nào xử lý đổi phòng. Toàn bộ logic chỉ xoay quanh `GuestCount`.

**Những gì cần xảy ra khi đổi phòng:**

| Hành động | Hiện trạng |
|---|---|
| Kiểm tra phòng mới có trống không | Không có |
| Kiểm tra sức chứa phòng mới | Không có |
| Cập nhật `DiningRoomId` trên Quote | Không làm |
| Cập nhật `DiningRoomId` trên Order | Không làm |
| Cập nhật trạng thái phòng cũ → Trống | Không làm |
| Cập nhật trạng thái phòng mới → Occupied | Không làm |
| Cập nhật Reservation (nếu có) | Không làm |

**Kết luận**: P2 — cần thiết kế riêng.

---

## 4. ĐỔI SUẤT: VIP B → VIP C

### KHÔNG ĐƯỢC HỖ TRỢ

`QuoteChangeService` chỉ thay đổi số lượng món hiện có, không thay đổi DANH SÁCH món.

**Những gì cần xảy ra khi đổi suất:**

| Hành động | Hiện trạng |
|---|---|
| Xóa món của suất cũ khỏi Quote | Không làm |
| Thêm món của suất mới vào Quote | Không làm |
| Tính lại giá (giá suất mới khác) | Không làm |
| Cập nhật Order lines tương ứng | Không làm |

**Kết luận**: P2 — cần thiết kế riêng. Đổi suất KHÁC với tăng/giảm khách vì thay đổi cả cấu trúc món.

---

## 5. ĐÃ GỬI BẾP — PHÂN TÍCH SÂU

### Vấn đề 1: KHÔNG có Cancel Ticket khi giảm khách (Case B)

Khi bàn bị xóa và món bị hủy:
```csharp
line.LineStatus = "Cancelled";  // Chỉ set status, không tạo ticket
```

So với code gốc trong `RemoveLineAsync`:
```csharp
if (createCancelTicket) {
    // Tạo PreparationTicket với TicketType = "Cancel"
}
```

**Hậu quả**: Bếp không biết món bị hủy → nấu thừa → lãng phí.

### Vấn đề 2: KHÔNG có Adjustment Ticket khi tăng khách (Case B)

Khi tăng món đã gửi bếp:
```csharp
existingLine.SupplementQuantity += supplementQty;  // Chỉ tăng SupplementQuantity
```

So với code gốc trong `ApplySentLineAdjustmentAsync`:
```csharp
// Tạo PreparationTicket với TicketType = "Send" cho phần chênh lệch
// Tạo shadow line "Adjustment" cho consumption tracking
```

**Hậu quả**: Bếp không biết cần nấu thêm → thiếu món.

### Vấn đề 3: Stock Issue không được cập nhật

`CreateConsumptionIssuesAsync` được gọi TRONG `SendToPreparationAsync`. Khi rebuild order, stock issues của order cũ:
- Vẫn trỏ đến order cũ (đã "Replaced")
- Order mới chưa có stock issue nào
- Nếu hủy món đã gửi bếp → stock issue cũ vẫn tồn tại → **tồn kho bị trừ kép** (một lần từ order cũ, một lần từ order mới khi gửi bếp lại)

---

## 6. ĐÃ PHỤC VỤ MỘT PHẦN

### Tình huống: Order đã SentKitchen, đã có 1 phần thanh toán

- `ApplyChangeAsync` kiểm tra `order.Status == "Paid"` hoặc `"Cancelled"` → từ chối
- Nhưng **KHÔNG kiểm tra `PaymentStatus == "Deposit"` hoặc `PaidAmount > 0`**

Nếu order đã nhận đặt cọc (PaymentStatus = "Deposit", PaidAmount = 20 triệu):
- Full rebuild: copy `PaidAmount` sang order mới (dòng 415-418)
- **Nhưng Deposit entity vẫn trỏ đến order cũ** → mất liên kết
- **CashFundReceipt không được cập nhật** → sổ quỹ sai

---

## 7. ĐÃ THANH TOÁN ĐẶT CỌC

### Vấn đề: Deposit & CashFund mồ côi

Khi `ApproveQuoteAsync` chạy lần đầu:
1. Tạo `RestaurantDeposit` với `AlacarteOrderId = oldOrder.Id`
2. Tạo `CashFundReceipt` qua `_cashFundService.CreateReceiptAsync()`
3. Set `quote.DepositId = deposit.Id`

Khi `ApplyFullRebuildAsync` chạy:
1. Tạo order MỚI → `newOrder.PaidAmount = oldOrder.PaidAmount` ✓
2. **Deposit vẫn trỏ đến oldOrder** ✗ — deposit.AlacarteOrderId = oldOrder.Id
3. **CashFundReceipt không được cập nhật** ✗
4. **quote.DepositId vẫn trỏ đến deposit cũ** ✗

**Hậu quả**:
- Báo cáo deposit không khớp với order mới
- Sổ quỹ có receipt cho order đã "Replaced"
- Không thể hoàn cọc nếu cần
- Audit trail đứt đoạn

---

## 8. ĐÃ PHÁT SINH CÔNG NỢ

### Tình huống: Order đã có customer debt

Hiện tại `PayOrderAsync` không hỗ trợ công nợ (chỉ thanh toán toàn bộ). Nhưng nếu trong tương lai có:

- Công nợ gắn với `AlacarteOrderId` → sau rebuild, công nợ mồ côi
- Cần migrate công nợ sang order mới hoặc chặn rebuild nếu có công nợ

**Hiện trạng**: Chưa có vấn đề vì chưa có tính năng công nợ. Nhưng là **rủi ro tương lai**.

---

## 9. ĐÃ TẠO StockIssue

### Vấn đề: Stock issue mồ côi

`CreateConsumptionIssuesAsync` tạo `StockIssue` với `Description = $"Xuất tiêu hao A La Carte {order.OrderNo}"`. Các stock issue này **không được migrate** sang order mới.

Sau full rebuild:
- Stock issue cũ → vẫn tồn tại, trỏ đến order cũ
- Order mới → chưa có stock issue
- Khi gửi bếp cho order mới → `CreateConsumptionIssuesAsync` tạo stock issue MỚI
- **Tồn kho bị trừ 2 lần** cho cùng 1 lượng nguyên liệu

**Mức độ**: CRITICAL nếu rebuild xảy ra SAU KHI đã gửi bếp. Nhưng Case A chỉ rebuild khi CHƯA gửi bếp, nên tạm thời an toàn.

**Rủi ro**: Nếu sau này có edge case mà `hasSentLines = false` nhưng stock issue đã tồn tại (do lỗi hoặc race condition).

---

## 10. ĐÃ TẠO CashFundReceipt

### Vấn đề: CashFund receipt mồ côi

Xem mục 7 (Đã thanh toán đặt cọc). Receipt không được migrate.

---

# PHẦN 2: LỖ HỔNG HỆ THỐNG

---

## Lỗ hổng 1: KHÔNG CÓ TRANSACTION — CRITICAL

### Vị trí

`ApplyChangeAsync` (dòng 286-304):

```csharp
await _dbContext.SaveChangesAsync(cancellationToken);  // Save 1: order + quote changes

cr.Status = "Applied";                                   // In-memory change
cr.AppliedBy = req.AppliedBy;
cr.AppliedAt = DateTime.UtcNow;

_dbContext.QuoteAuditLogs.Add(...);                      // In-memory add

await _dbContext.SaveChangesAsync(cancellationToken);  // Save 2: ChangeRequest + AuditLog
```

### Vấn đề

Hai lần `SaveChangesAsync` **không nằm trong transaction**. Nếu Save 2 fail:

| Trạng thái | Hậu quả |
|---|---|
| Order cũ → "Replaced" | **Đã lưu** — không thể phục vụ |
| Order mới → đã tạo | **Đã lưu** — đang "Serving" |
| Quote → GuestCount mới, ApprovedOrderId mới | **Đã lưu** |
| QuoteLine → CalculatedQuantity mới | **Đã lưu** |
| QuoteTable → bàn mới | **Đã lưu** |
| ChangeRequest → vẫn "Approved" | **Chưa lưu** — không biết đã applied |
| AuditLog "ChangeApplied" | **Chưa lưu** — mất dấu vết |

**Kết quả**: Dữ liệu đã thay đổi nhưng không có audit trail. ChangeRequest vẫn ở trạng thái "Approved" → có thể bị apply lại lần nữa → **tạo order thứ 3**.

### Fix cần thiết

Wrap toàn bộ `ApplyChangeAsync` trong `IExecutionStrategy` hoặc `TransactionScope`.

---

## Lỗ hổng 2: ORDER CŨ KHÔNG ĐƯỢC CLEANUP ĐẦY ĐỦ — CRITICAL

### Dữ liệu bị bỏ lại khi Full Rebuild

| Dữ liệu | Số phận sau rebuild | Hậu quả |
|---|---|---|
| `AlacarteOrder` cũ | Status = "Replaced" | Vẫn trong DB, vẫn có thể bị query nhầm |
| `AlacarteOrderLine` cũ | Giữ nguyên | Món cũ vẫn tồn tại |
| `RestaurantOrderTable` cũ | Giữ nguyên | Bàn cũ vẫn tồn tại |
| `RestaurantOrderExtraService` cũ | Giữ nguyên | Dịch vụ cũ vẫn tồn tại |
| `RestaurantServingGroup` cũ | Giữ nguyên | Nhóm phục vụ cũ |
| `RestaurantPreparationTicket` cũ | Giữ nguyên | Ticket bếp cũ |
| `RestaurantDeposit` cũ | Giữ nguyên, AlacarteOrderId → oldOrder | **Mồ côi** |
| `StockIssue` cũ | Giữ nguyên | **Mồ côi** |
| `CashFundReceipt` cũ | Giữ nguyên | **Mồ côi** |
| `TableReservation` | ReservationId copy sang order mới | Cả 2 order cùng trỏ 1 reservation |

### Vấn đề cụ thể

**Reservation**: `newOrder.ReservationId = oldOrder.ReservationId` (dòng 353). Sau rebuild, 2 order cùng trỏ 1 reservation. Nếu reservation được giải phóng khi order mới thanh toán, order cũ vẫn giữ ReservationId → null reference hoặc constraint violation.

---

## Lỗ hổng 3: DEPOSIT & CASHFUND KHÔNG ĐƯỢC MIGRATE — CRITICAL

### Luồng hiện tại

```
Quote duyệt → Tạo order A → Tạo Deposit (AlacarteOrderId = A) → Tạo CashFundReceipt
↓
Rebuild → Order A → "Replaced" → Tạo order B
↓
Deposit vẫn trỏ đến Order A ✗
CashFundReceipt không được cập nhật ✗
```

### Hậu quả tài chính

- **Sổ quỹ**: Receipt cho order A (đã Replaced), không có receipt cho order B
- **Báo cáo deposit**: Deposit 20 triệu gắn với order đã hủy
- **Hoàn cọc**: Không thể hoàn vì deposit trỏ đến order cũ
- **Thanh toán cuối**: `newOrder.RemainingAmount` = TotalAmount - PaidAmount. PaidAmount đã được copy (dòng 417), nhưng deposit entity không được migrate → **sổ sách không khớp**

---

## Lỗ hổng 4: CASE B — ADJUSTMENT PLAN THIẾU TICKET BẾP — CRITICAL

### Thiếu Adjustment Ticket

`ApplyAdjustmentPlan` khi tăng món đã gửi bếp:
```csharp
existingLine.SupplementQuantity += supplementQty;  // Chỉ tăng SupplementQuantity
```

So với `ApplySentLineAdjustmentAsync` trong code gốc:
- Tạo `RestaurantPreparationTicket` với TicketType = "Send" cho phần chênh lệch
- Tạo shadow `AlacarteOrderLine` với LineType = "Adjustment" cho consumption tracking
- Gọi `CreateConsumptionIssuesAsync` cho shadow line

`ApplyAdjustmentPlan` **không làm cả 3 việc trên**.

### Hậu quả

- Bếp không nhận được ticket → không biết cần nấu thêm
- Stock issue không được tạo → tồn kho không bị trừ cho phần chênh lệch
- Shadow line không được tạo → mất tracking

---

## Lỗ hổng 5: CASE B — THIẾU CANCEL TICKET KHI GIẢM — CRITICAL

### Thiếu Cancel Ticket

```csharp
line.LineStatus = "Cancelled";  // Chỉ set status
```

So với `RemoveLineAsync(createCancelTicket: true)`:
- Tạo `RestaurantPreparationTicket` với TicketType = "Cancel"
- Tạo shadow line cho việc hoàn kho

### Hậu quả

- Bếp không biết món bị hủy → nấu thừa
- Không có ticket cancel → không có cơ sở để hoàn kho

---

## Lỗ hổng 6: CONCURRENT MODIFICATION — HIGH

### Vấn đề

Không có optimistic concurrency check khi apply thay đổi. Nếu 2 người cùng duyệt và apply:

1. User A duyệt ChangeRequest → Approved
2. User B cũng duyệt (có thể cùng lúc) → Approved (không lỗi vì chỉ check `Status == "Pending"`)
3. User A apply → order rebuild, CR → Applied
4. User B apply → **CR vẫn "Approved" (đã bị User A đổi)** → lỗi ở bước check `cr.Status != "Approved"`?

Thực tế: nếu User B đọc CR trước khi User A apply, thì User B thấy Status = "Approved". Sau khi User A apply (CR → "Applied"), User B vẫn có thể gọi ApplyChangeAsync. Lúc này:
- `cr.Status == "Applied"` → ném lỗi "Yêu cầu sửa phải được duyệt trước khi áp dụng" → an toàn

Nhưng nếu 2 user cùng apply **cùng lúc** (race condition):
- Cả 2 đọc CR với Status = "Approved"
- Cả 2 gọi `ApplyFullRebuildAsync`
- **2 order mới được tạo** → 2 order "Serving" cho cùng 1 quote

### Fix cần thiết

RowVersion / ConcurrencyToken trên `QuoteChangeRequest`.

---

## Lỗ hổng 7: BÀN MỚI TRONG CASE B KHÔNG CÓ DiningTableId — HIGH

### Vấn đề

```csharp
order.OrderTables.Add(new RestaurantOrderTable
{
    // Không set DiningTableId
    TableNo = newAlloc.TableNo,
    ...
});
```

Bàn mới được thêm vào order nhưng không gắn với bàn vật lý nào trong phòng.

### Hậu quả

- Sơ đồ phòng không cập nhật
- Phục vụ không biết bàn mới nằm ở vị trí nào
- Có thể gán vào bàn vật lý đã có người dùng

---

## Lỗ hổng 8: CASE B — LINE MATCHING THEO MenuItemId CÓ THỂ SAI — HIGH

### Vấn đề

```csharp
var existingLines = order.Lines.Where(x => x.MenuItemId == quoteLine.MenuItemId).ToList();
```

Nếu một quote có 2 dòng cùng `MenuItemId` nhưng khác `PricingRule` (không thể xảy ra với logic merge hiện tại, nhưng có thể xảy ra nếu sau này thay đổi), việc match sẽ sai.

Ngoài ra, `MenuItemId` có thể là `null` (với quote line thủ công) → tất cả line không có MenuItemId sẽ match với nhau.

---

## Lỗ hổng 9: AUDIT LOG CHỈ GHI GUESTCOUNT, KHÔNG GHI CHI TIẾT TỪNG DÒNG — MEDIUM

### Vấn đề

Audit log hiện tại ghi:
```
Action: ChangeApplied
FieldName: GuestCount
OldValue: 40
NewValue: 47
```

Nhưng không ghi:
- Món nào tăng bao nhiêu
- Bàn nào thêm, bàn nào bỏ
- Tiền thay đổi bao nhiêu
- Order cũ → Order mới mapping

### Hậu quả

Khi kiểm toán, không thể truy ngược chính xác những gì đã thay đổi.

---

## Lỗ hổng 10: "REPLACED" STATUS KHÔNG ĐƯỢC XỬ LÝ TRONG ENSUREORDEREDITABLE — MEDIUM

### Vấn đề

```csharp
private static void EnsureOrderEditable(AlacarteOrder order)
{
    if (order.Status is "Paid" or "Cancelled")
        throw new InvalidOperationException(...);
}
```

Order với Status = "Replaced" vẫn có thể bị sửa (thêm món, hủy món, thanh toán) vì không nằm trong danh sách chặn.

---

# PHẦN 3: BẢNG TỔNG HỢP LỖ HỔNG

| # | Lỗ hổng | Mức độ | Ảnh hưởng |
|---|---|---|---|
| 1 | Không có Transaction trong ApplyChangeAsync | **CRITICAL** | Dữ liệu không nhất quán nếu Save 2 fail |
| 2 | Order cũ không cleanup — deposit/receipt/stock issue mồ côi | **CRITICAL** | Sai sổ quỹ, sai tồn kho, mất audit |
| 3 | Deposit & CashFundReceipt không migrate | **CRITICAL** | Sổ quỹ sai, không hoàn cọc được |
| 4 | Case B thiếu Adjustment Ticket khi tăng món đã gửi bếp | **CRITICAL** | Bếp không biết nấu thêm, thiếu món |
| 5 | Case B thiếu Cancel Ticket khi giảm món đã gửi bếp | **CRITICAL** | Bếp nấu thừa, tồn kho sai |
| 6 | Concurrent modification — không có RowVersion | **HIGH** | Race condition tạo 2 order |
| 7 | Bàn mới Case B không có DiningTableId | **HIGH** | Không biết bàn ở đâu |
| 8 | Line matching theo MenuItemId có thể sai | **HIGH** | Sai món khi điều chỉnh |
| 9 | Audit log không ghi chi tiết từng dòng | **MEDIUM** | Khó kiểm toán |
| 10 | "Replaced" không bị EnsureOrderEditable chặn | **MEDIUM** | Order cũ vẫn bị sửa được |

---

# PHẦN 4: TÍNH NĂNG CHƯA HỖ TRỢ

| # | Tính năng | Mức độ | Ghi chú |
|---|---|---|---|
| 1 | Đổi phòng (VIP1 → VIP2) | **P1** | Cần thiết kế riêng, phức tạp |
| 2 | Đổi suất (VIP B → VIP C) | **P1** | Cần thiết kế riêng, thay đổi cả danh sách món |
| 3 | Hoàn cọc khi giảm khách | **P2** | Nếu giảm khách → tổng tiền giảm → có cần hoàn 1 phần cọc? |
| 4 | Tăng cọc khi tăng khách | **P2** | Nếu tăng khách → tổng tiền tăng → có cần thu thêm cọc? |
| 5 | Gửi email báo giá mới cho khách | **P3** | Sau khi thay đổi, khách cần báo giá mới |

---

# PHẦN 5: KHUYẾN NGHỊ HÀNH ĐỘNG

## P0 — CRITICAL: Phải sửa trước khi dùng

| # | Hành động |
|---|---|
| P0.1 | **Wrap ApplyChangeAsync trong TransactionScope** — đảm bảo tất cả thay đổi (order + quote + ChangeRequest + audit log) là atomic |
| P0.2 | **Migrate Deposit khi Full Rebuild** — cập nhật `Deposit.AlacarteOrderId → newOrder.Id`, cập nhật `CashFundReceipt` reference nếu có |
| P0.3 | **Tạo Adjustment Ticket trong Case B (tăng)** — gọi logic tương tự `ApplySentLineAdjustmentAsync` cho phần chênh lệch |
| P0.4 | **Tạo Cancel Ticket trong Case B (giảm)** — gọi logic tương tự `RemoveLineAsync(createCancelTicket: true)` cho món trên bàn bị xóa |
| P0.5 | **Thêm optimistic concurrency** — RowVersion trên `QuoteChangeRequest` |

## P1 — IMPORTANT: Sửa trước khi release

| # | Hành động |
|---|---|
| P1.1 | **Gán DiningTableId cho bàn mới trong Case B** — tìm bàn vật lý trống trong phòng hoặc tạo mới |
| P1.2 | **Cleanup order cũ khi Full Rebuild** — ít nhất: hủy reservation nếu không cần, clear ReservationId trên order cũ |
| P1.3 | **Cập nhật audit log chi tiết** — ghi từng dòng thay đổi (món, bàn, tiền) |
| P1.4 | **Thêm "Replaced" vào EnsureOrderEditable** — chặn mọi thao tác trên order đã replaced |
| P1.5 | **Xử lý edge case newGuestCount == oldGuestCount** — từ chối hoặc báo lỗi rõ ràng |

## P2 — NICE TO HAVE: Làm sau

| # | Hành động |
|---|---|
| P2.1 | **Hỗ trợ đổi phòng** — thiết kế riêng |
| P2.2 | **Hỗ trợ đổi suất** — thiết kế riêng |
| P2.3 | **Hoàn cọc / thu thêm cọc** khi tổng tiền thay đổi |
| P2.4 | **Gửi email báo giá cập nhật** cho khách |

---

# PHẦN 6: KẾT LUẬN

Thiết kế Change Request P0-A có **cấu trúc đúng** và **logic cơ bản hoạt động**. Tuy nhiên, 5 lỗ hổng CRITICAL cần được giải quyết trước khi sử dụng:

1. **Không có transaction** → atomicity không được đảm bảo
2. **Deposit/CashFund mồ côi** → sai sổ sách kế toán
3. **Thiếu Adjustment Ticket (tăng)** → bếp không biết nấu thêm
4. **Thiếu Cancel Ticket (giảm)** → bếp nấu thừa, tồn kho sai
5. **Race condition** → có thể tạo 2 order cho cùng 1 lần đổi

Case A (full rebuild, chưa gửi bếp) tương đối an toàn. Case B (adjustment plan, đã gửi bếp) cần sửa nhiều.

**Khuyến nghị**: Sửa P0.1-P0.5 trước. Tạm thời chỉ cho phép Case A trong production. Case B để P1.
