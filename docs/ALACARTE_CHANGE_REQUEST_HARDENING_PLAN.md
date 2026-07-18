# ALACARTE_CHANGE_REQUEST_HARDENING_PLAN.md

## P0A Hardening — Đưa ChangeRequest lên Production-Ready

Ngày: 24/06/2026

---

# NGUYÊN TẮC CỐT LÕI

**Quote là nguồn sự thật (source of truth). Order là hiện thân (implementation).**

Khi Order thay đổi (rebuild), mọi dữ liệu tài chính và nghiệp vụ **thuộc về Quote**, không thuộc về Order. Order chỉ là "phiên bản hiện tại" của Quote.

```
Quote (bất biến về mặt nghiệp vụ)
  ├── Deposit (thuộc Quote — đặt cọc cho báo giá)
  ├── CashFundReceipt (thuộc Deposit — chứng từ kế toán)
  ├── Reservation (thuộc Quote — đặt phòng cho báo giá)
  │
  └── Order (có thể thay thế — hiện thân hiện tại)
        ├── OrderLine (sinh từ QuoteLine)
        ├── OrderTable (sinh từ TablePlan)
        ├── PreparationTicket (thuộc Order — lịch sử bếp)
        ├── StockIssue (thuộc PreparationTicket — xuất kho)
        └── ServingGroup (thuộc Order — nhóm phục vụ)
```

### Quy tắc Re-link vs Clone vs Giữ nguyên

| Entity | Hành động | Lý do |
|---|---|---|
| **RestaurantDeposit** | **Re-link** `AlacarteOrderId → newOrder.Id` | Deposit thuộc Quote, không thuộc Order. Chỉ cập nhật reference |
| **CashFundReceipt** | **Giữ nguyên + Note** | Chứng từ kế toán — không được sửa. Thêm note "Đơn đã được thay thế bởi [OrderNo mới]" vào Description |
| **StockIssue** (Case A) | **Không tồn tại** — chưa gửi bếp | Không cần xử lý |
| **StockIssue** (Case B) | **Giữ nguyên** | Đã xuất kho — không thể đảo ngược. Tạo stock issue mới cho phần chênh lệch |
| **TableReservation** | **Re-link** `ReservationId → newOrder.Id` | Reservation thuộc Quote, không thuộc Order |
| **PreparationTicket** | **Giữ nguyên** trên order cũ | Lịch sử bếp — không thay đổi. Tạo ticket mới cho order mới khi gửi bếp |
| **ServingGroup** | **Clone** sang order mới (nếu cần) | Nhóm phục vụ cần được tái tạo cho order mới |

---

# HARDENING 1: TRANSACTION BOUNDARY

## Vấn đề hiện tại

```csharp
// ApplyChangeAsync — 2 lần SaveChanges riêng biệt
await _dbContext.SaveChangesAsync(cancellationToken);  // Save 1: order + quote
// ... set cr.Status = "Applied" ...
await _dbContext.SaveChangesAsync(cancellationToken);  // Save 2: ChangeRequest + AuditLog
```

Nếu Save 2 fail → order đã rebuild, quote đã cập nhật, nhưng ChangeRequest không được đánh dấu "Applied".

## Hậu quả

- Dữ liệu nghiệp vụ đã thay đổi nhưng không có audit trail
- ChangeRequest vẫn ở trạng thái "Approved" → có thể bị apply lại → tạo order thứ 3
- Không thể biết ai đã apply, lúc nào

## Giải pháp

**Wrap toàn bộ `ApplyChangeAsync` trong EF Core ExecutionStrategy + TransactionScope:**

```
using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
try {
    // 1. Validate (cr, quote, order)
    // 2. Apply changes (FullRebuild hoặc AdjustmentPlan)
    // 3. Update ChangeRequest (Status, AppliedBy, AppliedAt)
    // 4. Write AuditLogs
    // 5. SaveChanges ONCE
    await _dbContext.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);
} catch {
    await tx.RollbackAsync(ct);
    throw;
}
```

## Entity ảnh hưởng

Không — chỉ thay đổi code flow.

## Service ảnh hưởng

`QuoteChangeService.ApplyChangeAsync`

## Migration cần thiết

Không.

## Rủi ro

- EF Core ExecutionStrategy có retry mechanism — cần đảm bảo idempotent (kiểm tra `cr.Status != "Applied"` trước khi apply)
- Transaction timeout nếu order có quá nhiều lines (500+ lines × nhiều tables)

---

# HARDENING 2: DEPOSIT MIGRATION

## Vấn đề hiện tại

Khi Full Rebuild:
- Tạo order mới (Order B)
- Order cũ (Order A) → Status = "Replaced"
- `RestaurantDeposit.AlacarteOrderId` vẫn trỏ đến Order A
- `quote.DepositId` vẫn trỏ đến deposit cũ (đúng — không cần đổi)
- Nhưng deposit reference đến Order A → **mồ côi**

## Hậu quả

- Báo cáo "Doanh thu đặt cọc theo đơn" sẽ không thấy deposit của Order B
- Nếu cần hoàn cọc, không biết deposit thuộc order nào
- `newOrder.PaidAmount` đã được copy từ oldOrder (đúng), nhưng deposit entity không khớp

## Phân tích các phương án

### Phương án A: Clone Deposit

Tạo deposit MỚI cho Order B, giữ deposit cũ cho Order A.

**Ưu**: Order A và Order B đều có deposit riêng.
**NHƯỢC**: **Sai nghiệp vụ** — deposit là 1 giao dịch tài chính, không thể nhân bản. Sẽ có 2 deposit cho cùng 1 khoản tiền.

### Phương án B: Re-link Deposit

Cập nhật `deposit.AlacarteOrderId → newOrder.Id`. Thêm note "Đơn gốc [OrderNo cũ] → thay thế bởi [OrderNo mới]".

**ƯU**: Đúng nghiệp vụ — deposit vẫn là 1 giao dịch, chỉ đổi reference.
**NHƯỢC**: Mất link đến order cũ (cần audit trail bù lại).

### Phương án C: Giữ nguyên

Không đụng vào deposit. Order A vẫn có deposit, Order B không có.

**ƯU**: Đơn giản.
**NHƯỢC**: Order B không có deposit → sai báo cáo.

## Giải pháp: **PHƯƠNG ÁN B — Re-link**

```
Trong ApplyFullRebuildAsync, sau khi tạo newOrder:

// Re-link deposits từ order cũ sang order mới
var deposits = await _dbContext.RestaurantDeposits
    .Where(x => x.AlacarteOrderId == oldOrder.Id)
    .ToListAsync(ct);

foreach (var deposit in deposits)
{
    deposit.AlacarteOrderId = newOrder.Id;
    deposit.Note = (deposit.Note ?? "") + $" | Đơn gốc {oldOrder.OrderNo} → thay thế bởi {newOrder.OrderNo}";
}

// KHÔNG đụng vào:
// - deposit.Id (giữ nguyên)
// - deposit.DepositNo (giữ nguyên — mã chứng từ)
// - deposit.Amount (giữ nguyên — số tiền)
// - deposit.CashFundId (giữ nguyên — quỹ)
// - deposit.Status (giữ nguyên — Confirmed)
// - quote.DepositId (giữ nguyên — vẫn trỏ đến deposit này)
```

## Entity ảnh hưởng

`RestaurantDeposit` — cập nhật `AlacarteOrderId`, `Note`

## Service ảnh hưởng

`QuoteChangeService.ApplyFullRebuildAsync`

## Migration cần thiết

Không.

## Rủi ro

- **Thấp** — deposit là entity riêng, không có FK cascade
- Cần audit log ghi rõ: "Deposit [DepositNo] re-linked from Order [A] to Order [B]"

---

# HARDENING 3: CASHFUND RECEIPT MIGRATION

## Vấn đề hiện tại

Khi `ApproveQuoteAsync` tạo deposit CÓ tích hợp CashFund:
```csharp
var receipt = await _cashFundService.CreateReceiptAsync(new CashTransactionRequest {
    Amount = quote.DepositAmount,
    ...
});
quote.DepositCashTransactionId = receipt.Id;
quote.DepositCashTransactionNo = receipt.TransactionNo;
```

Sau Full Rebuild: CashFundReceipt vẫn tồn tại, trỏ đến deposit cũ (vẫn đúng), nhưng reference đến Order A bị mất ngữ cảnh.

## Hậu quả

- Sổ quỹ có receipt cho "đơn cũ" — nhưng deposit đã được re-link sang order mới
- Nếu audit kiểm tra receipt → thấy reference đến order đã "Replaced"

## Phân tích

**CashFundReceipt là chứng từ kế toán — KHÔNG ĐƯỢC SỬA.**

Đây là nguyên tắc kế toán cơ bản. Chứng từ đã tạo không được thay đổi.

## Giải pháp: **GIỮ NGUYÊN + NOTE**

```
// Sau khi re-link deposit, cập nhật note cho receipt
if (quote.DepositCashTransactionId.HasValue)
{
    // Thêm audit log ghi rõ mapping
    _dbContext.QuoteAuditLogs.Add(new QuoteAuditLog
    {
        QuoteId = quote.Id,
        Action = "DepositReLinked",
        ChangedBy = appliedBy,
        FieldName = "CashFundReceipt",
        OldValue = $"Order {oldOrder.OrderNo}",
        NewValue = $"Order {newOrder.OrderNo}",
        Note = $"CashFundReceipt {quote.DepositCashTransactionNo} vẫn giữ nguyên. Deposit đã được re-link sang đơn mới.",
    });
}
```

**Không gọi API sửa CashFundReceipt.** Chỉ ghi audit log.

## Entity ảnh hưởng

Không — `CashFundReceipt` không bị sửa.

## Service ảnh hưởng

Không — không gọi `ICashFundService`.

## Migration cần thiết

Không.

## Rủi ro

- **Thấp** — receipt không bị đụng vào
- Audit log đủ để giải trình nếu kiểm toán hỏi

---

# HARDENING 4: ADJUSTMENT TICKET (TĂNG MÓN ĐÃ GỬI BẾP)

## Vấn đề hiện tại

```csharp
// ApplyAdjustmentPlan — khi tăng món đã Sent
if (existingLine.KitchenStatus == "Sent")
{
    if (newQty > existingLine.Qty)
    {
        existingLine.SupplementQuantity += supplementQty;  // ← CHỈ tăng SupplementQuantity
    }
}
```

Không tạo `RestaurantPreparationTicket`, không tạo shadow line, không gọi `CreateConsumptionIssuesAsync`.

## Hậu quả

- Bếp không nhận được ticket cho phần tăng thêm → thiếu món
- Stock issue không được tạo cho phần chênh lệch → tồn kho không bị trừ
- SupplementQuantity tăng nhưng không có audit trail

## Giải pháp: **GỌI LẠI LOGIC ApplySentLineAdjustmentAsync**

Code gốc trong `AlacarteRestaurantService.ApplySentLineAdjustmentAsync` (line 1644) đã xử lý chuẩn:

1. Tính `currentEffectiveQty = Qty + (SupplementQuantity ?? 0)`
2. Nếu tăng → tạo PreparationTicket (TicketType = "Send") cho phần chênh lệch
3. Tạo shadow `AlacarteOrderLine` với `LineType = "Adjustment"` để track consumption
4. Gọi `CreateConsumptionIssuesAsync` cho shadow line
5. Cập nhật `SupplementQuantity`, `SupplementNote`

**Không duplicate code. Inject `IAlacarteRestaurantService` và gọi method có sẵn.**

```
// Trong ApplyAdjustmentPlan, thay vì tự set SupplementQuantity:

// Tạo request giống như AddOrUpdateLineAsync đã làm
foreach (var table in tablesWithChanges)
{
    var request = new SaveAlacarteOrderLineRequest
    {
        OrderId = order.Id,
        OrderLineId = existingLine.Id,  // line đã tồn tại
        MenuItemId = existingLine.MenuItemId,
        Qty = newQty,                   // số lượng MỚI
        OrderTableId = table.Id,
    };
    await _alacarteService.AddOrUpdateLineAsync(request, ct);
    // Method này tự động detect line đã Sent → gọi ApplySentLineAdjustmentAsync
}
```

**Ưu điểm**: Tận dụng code có sẵn, không duplicate, đảm bảo consistency.

## Entity ảnh hưởng

- `RestaurantPreparationTicket` (tạo mới — ticket tăng thêm)
- `RestaurantPreparationTicketLine`
- `AlacarteOrderLine` (tạo shadow "Adjustment")
- `StockIssue` (tạo mới cho phần chênh lệch)

## Service ảnh hưởng

- `QuoteChangeService.ApplyAdjustmentPlan` — thay đổi logic
- `IAlacarteRestaurantService.AddOrUpdateLineAsync` — gọi lại

## Migration cần thiết

Không.

## Rủi ro

- `AddOrUpdateLineAsync` có thể merge line nếu trùng `MenuItemId + OrderTableId` — cần kiểm tra kỹ
- Shadow line sẽ có `LineType = "Adjustment"` → cần đảm bảo Recalculate xử lý đúng

---

# HARDENING 5: CANCEL TICKET (GIẢM MÓN ĐÃ GỬI BẾP)

## Vấn đề hiện tại

```csharp
// ApplyAdjustmentPlan — khi xóa bàn, hủy món
if (line.KitchenStatus == "Sent")
{
    line.LineStatus = "Cancelled";  // ← CHỈ set status
}
```

Không tạo Cancel PreparationTicket.

## Hậu quả

- Bếp không biết món bị hủy → nấu thừa
- Không có ticket cancel → không có cơ sở hoàn kho
- Stock issue cũ vẫn tồn tại → tồn kho đã bị trừ cho món không dùng

## Giải pháp: **GỌI RemoveLineAsync VỚI createCancelTicket = true**

```
// Trong ApplyAdjustmentPlan, thay vì tự set LineStatus:

foreach (var lineToCancel in linesOnRemovedTables)
{
    if (lineToCancel.KitchenStatus == "Sent")
    {
        // Gọi RemoveLineAsync với createCancelTicket = true
        await _alacarteService.RemoveLineAsync(
            order.Id,
            lineToCancel.Id,
            createCancelTicket: true,
            cancelledBy: appliedBy,
            ct);
        // Method này tự động:
        // 1. Set LineStatus = "Cancelled"
        // 2. Tạo Cancel PreparationTicket
        // 3. Tạo AdjustmentTicketLine (Cancel type)
    }
    else
    {
        // Món chưa gửi bếp → xóa cứng
        order.Lines.Remove(lineToCancel);
    }
}
```

## Entity ảnh hưởng

- `RestaurantPreparationTicket` (tạo mới — ticket hủy)
- `RestaurantPreparationTicketLine`
- `AlacarteOrderLine` (LineStatus → "Cancelled")

## Service ảnh hưởng

- `QuoteChangeService.ApplyAdjustmentPlan` — thay đổi logic
- `IAlacarteRestaurantService.RemoveLineAsync` — gọi lại

## Migration cần thiết

Không.

## Rủi ro

- `RemoveLineAsync` có thể có side effect (cập nhật serving group) — cần kiểm tra
- Cancel ticket cần có lý do rõ ràng: "Hủy do giảm khách từ XX → YY"

---

# HARDENING 6: ROW VERSION (OPTIMISTIC CONCURRENCY)

## Vấn đề hiện tại

`QuoteChangeRequest` không có cơ chế chống concurrent modification. Nếu 2 user cùng apply 1 ChangeRequest:

```
User A đọc CR (Status = "Approved")
User B đọc CR (Status = "Approved")
User A apply → CR.Status = "Applied", SaveChanges
User B apply → CR.Status VẪN LÀ "Approved" trong memory của User B
```

## Hậu quả

- User B apply thành công (vì `cr.Status` trong memory vẫn là "Approved") → tạo order thứ 3
- 2 order "Serving" cho cùng 1 quote

## Giải pháp: **THÊM ROWVERSION**

### Entity

```csharp
public sealed class QuoteChangeRequest
{
    // ... existing fields ...
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
```

### EF Configuration

```csharp
modelBuilder.Entity<QuoteChangeRequest>(entity =>
{
    entity.Property(x => x.RowVersion).IsRowVersion();
});
```

### Service

```csharp
public async Task<QuoteChangeRequestDto> ApplyChangeAsync(ApplyChangeRequest req, CancellationToken ct)
{
    var cr = await _dbContext.QuoteChangeRequests
        .FirstOrDefaultAsync(x => x.Id == req.ChangeRequestId, ct);

    // Validate
    if (cr.Status != "Approved") throw ...;

    // EF Core tự động check RowVersion khi SaveChanges
    // Nếu row bị thay đổi bởi người khác → DbUpdateConcurrencyException

    try
    {
        await _dbContext.SaveChangesAsync(ct);
    }
    catch (DbUpdateConcurrencyException)
    {
        throw new InvalidOperationException(
            "Yêu cầu sửa đã được xử lý bởi người khác. Vui lòng tải lại trang.");
    }
}
```

## Entity ảnh hưởng

`QuoteChangeRequest` — thêm `RowVersion`

## Service ảnh hưởng

`QuoteChangeService.ApplyChangeAsync` — thêm try-catch `DbUpdateConcurrencyException`

## Migration cần thiết

**Có** — migration mới: `AddQuoteChangeRequestRowVersion`

```sql
ALTER TABLE QuoteChangeRequests ADD RowVersion ROWVERSION NOT NULL
```

## Rủi ro

- **Thấp** — RowVersion là tính năng chuẩn của EF Core / SQL Server
- Cần test: 2 user apply cùng lúc → 1 user báo lỗi concurrency

---

# HARDENING 7: REPLACED ORDER PROTECTION

## Vấn đề hiện tại

```csharp
private static void EnsureOrderEditable(AlacarteOrder order)
{
    if (order.Status is "Paid" or "Cancelled")
        throw ...;
    // "Replaced" không bị chặn
}
```

## Hậu quả

Order đã "Replaced" vẫn có thể bị:
- Thêm món (`AddOrUpdateLineAsync`)
- Hủy món (`RemoveLineAsync`)
- Gửi bếp (`SendToPreparationAsync`)
- Thanh toán (`PayOrderAsync`)
- Hủy (`CancelOrderAsync`)

## Giải pháp

### A. Thêm "Replaced" vào EnsureOrderEditable

```csharp
if (order.Status is "Paid" or "Cancelled" or "Replaced")
    throw new InvalidOperationException(
        $"Không thể thao tác trên đơn '{order.Status}'. " +
        $"Đơn đã được thay thế bởi đơn mới.");
```

**Đây là sửa 1 dòng trong `AlacarteRestaurantService`.**

### B. Không cần thêm EF configuration hay migration

## Entity ảnh hưởng

Không.

## Service ảnh hưởng

`AlacarteRestaurantService.EnsureOrderEditable` — thêm 1 điều kiện

## Migration cần thiết

Không.

## Rủi ro

- **Thấp** — thay đổi 1 dòng, impact rõ ràng
- Cần đảm bảo không có flow hợp lệ nào cần sửa order "Replaced"

---

# HARDENING 8: RESERVATION CLEANUP

## Vấn đề hiện tại

```csharp
// ApplyFullRebuildAsync
newOrder.ReservationId = oldOrder.ReservationId;  // Copy ReservationId
```

Sau rebuild:
- Order A (Replaced) → ReservationId = R1
- Order B (Serving) → ReservationId = R1
- **Cả 2 order cùng trỏ 1 reservation**

## Hậu quả

- Khi thanh toán Order B → `PayOrderAsync` cập nhật reservation status → "Completed"
- Order A vẫn giữ ReservationId → nhưng reservation đã "Completed"
- Nếu có logic cleanup reservation khi order bị hủy → Order A (Replaced) có thể trigger cleanup nhầm

## Giải pháp: **CLEAR RESERVATION ID TRÊN ORDER CŨ**

```
// Trong ApplyFullRebuildAsync, SAU KHI set newOrder.ReservationId:

// Clear reservation link trên order cũ để tránh trùng lặp
oldOrder.ReservationId = null;

// Thêm audit log
_dbContext.QuoteAuditLogs.Add(new QuoteAuditLog
{
    QuoteId = quote.Id,
    Action = "ReservationReLinked",
    ChangedBy = appliedBy,
    FieldName = "ReservationId",
    OldValue = $"Order {oldOrder.OrderNo}",
    NewValue = $"Order {newOrder.OrderNo}",
    Note = $"Reservation chuyển từ đơn cũ sang đơn mới",
});
```

## Entity ảnh hưởng

- `AlacarteOrder` — set `ReservationId = null` trên order cũ
- `TableReservation` — không thay đổi

## Service ảnh hưởng

`QuoteChangeService.ApplyFullRebuildAsync`

## Migration cần thiết

Không.

## Rủi ro

- **Thấp** — `ReservationId` là nullable
- Cần đảm bảo reservation không bị xóa khi order cũ bị cleanup (nếu có logic cascade)

---

# PHẦN TỔNG HỢP: ÁP DỤNG THEO CASE

## Case A: Full Rebuild (chưa gửi bếp)

```
Transaction BEGIN
  1. Validate (cr, quote, order)
  2. oldOrder.Status = "Replaced"
  3. oldOrder.ReservationId = null          ← HARDENING 8
  4. oldOrder.UpdatedAt = now
  5. Rebuild quote tables
  6. Recalculate quote lines
  7. Create newOrder (copy oldOrder meta)
  8. Copy lines → newOrder (PerGuest/PerTable/PerRoom)
  9. Copy extra services → newOrder
  10. newOrder.PaidAmount = oldOrder.PaidAmount
  11. RecalculateOrder(newOrder)
  12. Re-link deposits → newOrder            ← HARDENING 2
  13. Audit log: CashFundReceipt             ← HARDENING 3
  14. Audit log: ReservationReLinked         ← HARDENING 8
  15. quote.ApprovedOrderId = newOrder.Id
  16. cr.Status = "Applied"
  17. cr.RowVersion → auto-check             ← HARDENING 6
  18. Write all audit logs
Transaction COMMIT
```

## Case B: Adjustment Plan (đã gửi bếp)

```
Transaction BEGIN
  1. Validate (cr, quote, order)
  2. Add/Remove OrderTables
  3. Update GuestCount on existing tables
  4. Với món CHƯA Sent → cập nhật Qty trực tiếp
  5. Với món ĐÃ Sent, TĂNG → gọi AddOrUpdateLineAsync    ← HARDENING 4
  6. Với món ĐÃ Sent, GIẢM → gọi RemoveLineAsync(cancel)  ← HARDENING 5
  7. Với món THÊM MỚI → AddOrderLine
  8. RecalculateOrder(order)
  9. quote.GuestCount = cr.NewGuestCount
  10. quote.TotalAmount = order.TotalAmount
  11. cr.Status = "Applied"
  12. cr.RowVersion → auto-check                            ← HARDENING 6
  13. Write all audit logs
Transaction COMMIT
```

---

# MIGRATION CẦN THIẾT

Chỉ **1 migration mới**:

```
Migration: AddQuoteChangeRequestRowVersion

ALTER TABLE QuoteChangeRequests
ADD RowVersion ROWVERSION NOT NULL DEFAULT 0x0000000000000001
```

---

# DANH SÁCH SERVICE THAY ĐỔI

| Service | Method | Thay đổi |
|---|---|---|
| `QuoteChangeService` | `ApplyChangeAsync` | Wrap transaction + RowVersion handling |
| `QuoteChangeService` | `ApplyFullRebuildAsync` | Re-link deposit, clear oldOrder.ReservationId, audit logs |
| `QuoteChangeService` | `ApplyAdjustmentPlan` | Gọi `AddOrUpdateLineAsync` / `RemoveLineAsync` thay vì set thủ công |
| `AlacarteRestaurantService` | `EnsureOrderEditable` | Thêm "Replaced" vào danh sách chặn (1 dòng) |

---

# RỦI RO TỔNG THỂ

| Rủi ro | Mức độ | Giảm thiểu |
|---|---|---|
| Transaction timeout với order lớn (500+ lines) | Thấp | Case A hiếm khi có order lớn chưa gửi bếp. Nếu có → tăng timeout |
| `AddOrUpdateLineAsync` merge line không mong muốn | Trung bình | Test kỹ Case B với nhiều line cùng MenuItemId |
| RowVersion migration trên bảng có dữ liệu | Thấp | ROWVERSION tự động generate, không ảnh hưởng row hiện có |
| Audit log quá nhiều (10+ dòng cho 1 lần apply) | Thấp | Audit log là cần thiết, không phải vấn đề performance |
| Rollback transaction để lại dirty data trong memory | Thấp | EF Core ChangeTracker tự rollback khi transaction rollback |

---

# KẾ HOẠCH TRIỂN KHAI

| Bước | Hành động | File |
|---|---|---|
| 1 | Thêm `RowVersion` vào `QuoteChangeRequest` | `RestaurantEntities.cs` |
| 2 | Thêm EF config `IsRowVersion()` | `AppDbContext.cs` |
| 3 | Tạo migration `AddQuoteChangeRequestRowVersion` | `Migrations/` |
| 4 | Wrap `ApplyChangeAsync` trong transaction | `QuoteChangeService.cs` |
| 5 | Re-link deposit trong `ApplyFullRebuildAsync` | `QuoteChangeService.cs` |
| 6 | Clear `oldOrder.ReservationId` | `QuoteChangeService.cs` |
| 7 | Gọi `AddOrUpdateLineAsync` trong Case B (tăng) | `QuoteChangeService.cs` |
| 8 | Gọi `RemoveLineAsync(createCancelTicket)` trong Case B (giảm) | `QuoteChangeService.cs` |
| 9 | Thêm "Replaced" vào `EnsureOrderEditable` | `RestaurantServices.cs` |
| 10 | Unit test: transaction rollback | `RestaurantModuleTests.cs` |
| 11 | Unit test: deposit re-link | `RestaurantModuleTests.cs` |
| 12 | Unit test: concurrency exception | `RestaurantModuleTests.cs` |
| 13 | Build + Run all tests | — |

---

# KẾT LUẬN

Sau Hardening, ChangeRequest sẽ:

- **Atomic**: Toàn bộ apply nằm trong 1 transaction — không thể dở dang
- **An toàn tài chính**: Deposit được re-link, CashFundReceipt được audit, không mất mát
- **An toàn bếp**: Adjustment Ticket và Cancel Ticket được tạo đúng chuẩn
- **An toàn concurrency**: RowVersion chống apply trùng
- **An toàn dữ liệu**: Order cũ được bảo vệ, reservation được cleanup

**Tổng cộng**: 1 migration, 4 file thay đổi, ~13 bước triển khai.
