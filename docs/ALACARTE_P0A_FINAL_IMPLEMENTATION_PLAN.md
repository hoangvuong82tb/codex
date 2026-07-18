# ALACARTE_P0A_FINAL_IMPLEMENTATION_PLAN.md

## Tài liệu cuối cùng — Giao cho Codex triển khai

Ngày: 24/06/2026

Phiên bản: FINAL v1

---

# I. PHẠM VI

## ĐƯỢC PHÉP TRIỂN KHAI (P0A Hardening)

| # | Hạng mục | Mức độ | Mô tả ngắn |
|---|---|---|---|
| 1 | **Transaction Boundary** | CRITICAL | Wrap ApplyChangeAsync trong 1 transaction duy nhất |
| 2 | **RowVersion** | CRITICAL | Thêm RowVersion vào QuoteChangeRequest |
| 3 | **Deposit Re-link** | CRITICAL | Cập nhật AlacarteOrderId trên deposit khi full rebuild |
| 4 | **Reservation Cleanup** | CRITICAL | Clear ReservationId trên order cũ khi full rebuild |
| 5 | **Replaced Protection** | CRITICAL | Chặn mọi thao tác trên order có Status="Replaced" |
| 6 | **OrderLineDomainService** | FOUNDATION | Shared stateless service — adjust/cancel/allocate lines |
| 7 | **KitchenTicketService** | FOUNDATION | Shared service — tạo PreparationTicket (tách từ AlacarteService) |
| 8 | **Case A Full Rebuild Audit** | IMPORTANT | Audit log chi tiết: deposit re-link, reservation, delta |

## HOÃN LẠI (P0B)

| # | Hạng mục | Lý do hoãn |
|---|---|---|
| 1 | Case B Adjustment Ticket (tăng món đã gửi bếp) | Cần OrderLineDomainService làm nền — sẽ làm P0B |
| 2 | Case B Cancel Ticket (giảm món đã gửi bếp) | Cần OrderLineDomainService làm nền — sẽ làm P0B |
| 3 | Case B DiningTableId cho bàn mới | Phức tạp, cần TableSlot entity |
| 4 | Audit log chi tiết từng dòng thay đổi | P1 |
| 5 | Hoàn cọc / thu thêm cọc khi tổng tiền đổi | P2 |
| 6 | Đổi phòng | P2 |
| 7 | Đổi suất | P2 |

---

# II. FILE ĐƯỢC PHÉP SỬA

| # | File | Hành động | Dòng ước lượng |
|---|---|---|---|
| 1 | `Core/Entities/RestaurantEntities.cs` | Thêm `RowVersion` vào `QuoteChangeRequest` | +1 |
| 2 | `Infrastructure/Persistence/AppDbContext.cs` | Thêm `IsRowVersion()` config | +1 |
| 3 | `Infrastructure/Services/QuoteChangeService.cs` | Transaction + deposit re-link + reservation cleanup + audit | ~60 dòng sửa |
| 4 | `Infrastructure/Services/RestaurantServices.cs` | Thêm "Replaced" vào `EnsureOrderEditable` | +1 dòng |
| 5 | `Infrastructure/Services/OrderLineDomainService.cs` | **TẠO MỚI** | ~80 dòng |
| 6 | `Infrastructure/Services/KitchenTicketService.cs` | **TẠO MỚI** | ~60 dòng |
| 7 | `Infrastructure/DependencyInjection.cs` | Đăng ký 2 service mới | +2 dòng |
| 8 | `Tests.Unit/RestaurantModuleTests.cs` | Unit test cho hardening | +10 test |

## FILE TUYỆT ĐỐI KHÔNG SỬA

| File | Lý do |
|---|---|
| `AlacarteRestaurantService.cs` (TRỪ EnsureOrderEditable) | God service — giữ nguyên tránh regression |
| `AlacarteRestaurantView.xaml` | Không UI |
| `AlacarteRestaurantView.xaml.cs` | Không UI |
| `RestaurantViewModels.cs` | Không UI |
| `IRestaurantServices.cs` (TRỪ thêm interface mới) | Interface hiện tại giữ nguyên |
| `IAlacarteVipEngines.cs` | Không thay đổi engine |
| `AlacarteVipEngines.cs` | Không thay đổi engine |
| Bất kỳ file nào ngoài danh sách "ĐƯỢC PHÉP SỬA" | |

---

# III. FINAL DEPENDENCY GRAPH

```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│  QuoteChangeService                                          │
│  ├── AppDbContext (scoped, shared)                           │
│  ├── ITableAllocationEngine (singleton, stateless)           │
│  ├── IMenuCalculationEngine (singleton, stateless)           │
│  ├── OrderLineDomainService (scoped, stateless logic)        │
│  ├── KitchenTicketService (scoped, creates tickets)          │
│  └── IAlacarteRestaurantService ← ĐÃ LOẠI BỎ               │
│                                                              │
│  AlacarteRestaurantService                                   │
│  ├── AppDbContext                                            │
│  ├── IMenuCatalogService                                     │
│  ├── ICashFundService                                        │
│  ├── ITableAllocationEngine                                  │
│  ├── IMenuCalculationEngine                                  │
│  ├── OrderLineDomainService ← (sẽ dùng trong P0B)           │
│  └── KitchenTicketService ← (sẽ dùng trong P0B)             │
│                                                              │
│  OrderLineDomainService ← KHÔNG inject service nào khác     │
│  ├── (pure logic, không DB, không side effect)              │
│  └── Inject bởi: QuoteChangeService, AlacarteRestaurantSvc  │
│                                                              │
│  KitchenTicketService ← Chỉ inject AppDbContext              │
│  ├── AppDbContext (scoped)                                   │
│  └── Inject bởi: QuoteChangeService, AlacarteRestaurantSvc  │
│                                                              │
│  NO CIRCULAR DEPENDENCIES                                    │
│  NO SERVICE-TO-SERVICE DIRECT CALLS                          │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

# IV. FINAL SEQUENCE — ApplyChangeAsync (Case A: Full Rebuild)

```
Client
  │
  ▼
ApplyChangeAsync(req)
  │
  ├── [1] Validate
  │     ├── Load ChangeRequest (include Details)
  │     ├── Check cr.Status == "Approved"
  │     ├── Load Quote (include Lines, QuoteTables)
  │     ├── Load Order (include Lines, OrderTables, ExtraServices, Deposits)
  │     └── Check order.Status != "Paid" && != "Cancelled"
  │
  ├── [2] BEGIN TRANSACTION
  │
  ├── [3] Soft-delete old order
  │     ├── oldOrder.Status = "Replaced"
  │     ├── oldOrder.ReservationId = null           ← CLEANUP
  │     └── oldOrder.UpdatedAt = now
  │
  ├── [4] Rebuild quote
  │     ├── Xóa QuoteTables cũ
  │     ├── TableAllocationEngine.Allocate(newGuestCount, standard)
  │     ├── Tạo QuoteTables mới
  │     ├── MenuCalculationEngine.Calculate cho từng line
  │     └── quote.GuestCount = newGuestCount
  │
  ├── [5] Create new order
  │     ├── AlacarteOrder mới (copy meta từ oldOrder)
  │     ├── OrderTables từ allocation
  │     ├── OrderLines từ quote lines (PerGuest/PerTable/PerRoom)
  │     ├── ExtraServices từ quote service lines
  │     └── PaidAmount = oldOrder.PaidAmount
  │
  ├── [6] Re-link deposits
  │     ├── Tìm tất cả RestaurantDeposit với AlacarteOrderId = oldOrder.Id
  │     ├── Cập nhật AlacarteOrderId → newOrder.Id
  │     ├── Append Note: "Đơn gốc {old} → thay thế bởi {new}"
  │     └── AuditLog: "DepositReLinked"
  │
  ├── [7] Update Quote reference
  │     └── quote.ApprovedOrderId = newOrder.Id
  │
  ├── [8] Recalculate
  │     ├── RecalculateOrder(newOrder)
  │     └── quote.TotalAmount = newOrder.TotalAmount
  │
  ├── [9] Update ChangeRequest
  │     ├── cr.Status = "Applied"
  │     ├── cr.AppliedBy, cr.AppliedAt
  │     ├── cr.NewOrderId = newOrder.Id
  │     ├── cr.NewTotalAmount = newOrder.TotalAmount
  │     └── cr.AmountDelta = newOrder.TotalAmount - cr.PreviousTotalAmount
  │
  ├── [10] Write AuditLogs
  │     ├── "ChangeApplied" (GuestCount delta)
  │     ├── "DepositReLinked" (nếu có deposit)
  │     └── "ReservationReLinked" (nếu có reservation)
  │
  ├── [11] SaveChanges (1 LẦN DUY NHẤT)
  │     └── EF Core RowVersion tự động check → DbUpdateConcurrencyException nếu conflict
  │
  ├── [12] COMMIT TRANSACTION
  │
  └── [13] Return ChangeRequestDto
```

---

# V. FINAL SEQUENCE — ApplyChangeAsync (Case B: Adjustment Plan)

**GHI CHÚ: Case B chỉ được gọi khi `hasSentLines == true`. Logic đầy đủ sẽ triển khai trong P0B.**

Trong P0A Hardening, Case B sẽ **throw NotSupportedException** với message rõ ràng:

```
"Đơn đã gửi bếp một phần. Tính năng điều chỉnh đơn đã gửi bếp sẽ có trong bản cập nhật sau.
 Vui lòng liên hệ quản lý để xử lý thủ công."
```

Điều này an toàn hơn là để code chạy với logic thiếu (thiếu ticket, thiếu stock issue).

---

# VI. ENTITY THAY ĐỔI

## QuoteChangeRequest — Thêm RowVersion

```csharp
public sealed class QuoteChangeRequest : ICompanyScoped, IBranchScoped
{
    // ... existing fields ...
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();  // ← THÊM MỚI
}
```

## EF Configuration thêm

```csharp
modelBuilder.Entity<QuoteChangeRequest>(entity =>
{
    // ... existing config ...
    entity.Property(x => x.RowVersion).IsRowVersion();  // ← THÊM MỚI
});
```

---

# VII. MIGRATION

## Chỉ 1 migration mới

```
Tên: AddQuoteChangeRequestRowVersion

Nội dung:
  ALTER TABLE QuoteChangeRequests
  ADD RowVersion ROWVERSION NOT NULL
```

**Không cần migration nào khác.** Tất cả thay đổi khác (Deposit re-link, Reservation cleanup, Replaced status) là logic, không phải schema.

---

# VIII. SERVICE MỚI

## 8.1. OrderLineDomainService

```
File: Infrastructure/Services/OrderLineDomainService.cs
Interface: Application/Interfaces/IRestaurantServices.cs (thêm IOrderLineDomainService)
Scope: Scoped (stateless — không field, nhưng đăng ký Scoped để DI nhất quán)

Methods:

  LineAdjustment AdjustSentLine(AlacarteOrderLine line, decimal newQty, string reason)
    → Tính delta = newQty - (Qty + SupplementQuantity)
    → Nếu delta > 0: cập nhật SupplementQuantity += delta
    → Trả về LineAdjustment { UpdatedLine, Delta, NeedsTicket = delta > 0 }

  LineCancellation CancelLine(AlacarteOrderLine line, string reason, bool wasSent)
    → Set LineStatus = "Cancelled"
    → Trả về LineCancellation { CancelledLine, NeedsCancelTicket = wasSent }

  List<OrderLineAllocation> AllocateLines(
      List<RestaurantQuoteLine> quoteLines,
      List<RestaurantOrderTable> servingTables,
      RestaurantOrderTable summaryTable)
    → Với mỗi quote line:
        PerPerson: line × GuestCount cho từng bàn
        PerTable: line × 1 cho từng bàn
        PerRoom/Manual: line → summary table
    → Trả về danh sách AlacarteOrderLine cần tạo
```

## 8.2. KitchenTicketService

```
File: Infrastructure/Services/KitchenTicketService.cs
Interface: Application/Interfaces/IRestaurantServices.cs (thêm IKitchenTicketService)
Scope: Scoped

Methods:

  RestaurantPreparationTicket CreateTicket(
      AlacarteOrder order,
      List<AlacarteOrderLine> lines,
      string ticketType,  // "Send" hoặc "Cancel"
      string createdBy,
      string? note = null)
    → Tạo ticket với TicketNo format "AKT-{date}-{seq}" (Send) hoặc "ACN-{date}-{seq}" (Cancel)
    → Tạo TicketLine cho từng line
    → Trả về ticket (chưa SaveChanges — để caller tự save)
```

---

# IX. SERVICE SỬA

## 9.1. QuoteChangeService.ApplyChangeAsync

**Thay đổi chính:**

1. Wrap toàn bộ trong `IExecutionStrategy.ExecuteAsync` + `IDbContextTransaction`
2. Chỉ gọi SaveChanges 1 LẦN ở cuối
3. Case B: throw NotSupportedException (thay vì gọi ApplyAdjustmentPlan với logic thiếu)

```csharp
public async Task<QuoteChangeRequestDto> ApplyChangeAsync(ApplyChangeRequest req, CancellationToken ct)
{
    var strategy = _dbContext.Database.CreateExecutionStrategy();
    return await strategy.ExecuteAsync(async () =>
    {
        using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            // 1. Load & Validate
            var cr = await LoadAndValidateChangeRequest(req.ChangeRequestId, ct);
            var quote = await LoadQuote(cr.QuoteId, ct);
            var order = await LoadOrder(cr.PreviousOrderId, ct);
            ValidateOrderState(order);

            // 2. Apply changes (Case A only in P0A)
            var hasSentLines = order.Lines.Any(x => x.KitchenStatus == "Sent");
            if (!hasSentLines)
            {
                await ApplyFullRebuildAsync(quote, order, cr, req.AppliedBy, ct);
            }
            else
            {
                throw new NotSupportedException(
                    "Đơn đã gửi bếp một phần. Tính năng điều chỉnh đơn đã gửi bếp sẽ có trong bản cập nhật sau.");
            }

            // 3. Update ChangeRequest
            cr.Status = "Applied";
            cr.AppliedBy = req.AppliedBy;
            cr.AppliedAt = DateTime.UtcNow;

            // 4. Write AuditLogs (đã được thêm trong ApplyFullRebuildAsync)

            // 5. SINGLE SaveChanges
            await _dbContext.SaveChangesAsync(ct);

            // 6. Commit
            await tx.CommitAsync(ct);
            return MapToDto(cr);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            throw new InvalidOperationException(
                "Yêu cầu sửa đã được xử lý bởi người khác. Vui lòng tải lại trang.");
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    });
}
```

## 9.2. QuoteChangeService.ApplyFullRebuildAsync

**Thay đổi chính:**

1. **KHÔNG tự gọi SaveChanges** — để caller (ApplyChangeAsync) gọi 1 lần duy nhất
2. **Thêm deposit re-link**
3. **Thêm reservation cleanup**
4. **Thêm audit logs**

```csharp
private async Task ApplyFullRebuildAsync(
    RestaurantQuote quote, AlacarteOrder oldOrder,
    QuoteChangeRequest cr, string appliedBy, CancellationToken ct)
{
    // 1. Soft-delete old order
    oldOrder.Status = "Replaced";
    oldOrder.ReservationId = null;  // ← CLEANUP
    oldOrder.UpdatedAt = DateTime.UtcNow;

    // 2. Rebuild quote tables & lines
    // (giữ nguyên logic hiện tại: xóa QuoteTables cũ → tạo mới → tính lại CalculatedQuantity)

    // 3. Create new order
    // (giữ nguyên logic hiện tại: new AlacarteOrder → OrderTables → OrderLines → ExtraServices)

    // 4. Copy PaidAmount từ old order
    if (oldOrder.PaidAmount > 0)
    {
        newOrder.PaidAmount = oldOrder.PaidAmount;
        newOrder.RemainingAmount = Math.Max(0, newOrder.TotalAmount - newOrder.PaidAmount);
    }

    // 5. Recalculate
    RecalculateOrder(newOrder);
    quote.TotalAmount = newOrder.TotalAmount;

    // 6. Re-link deposits ← THÊM MỚI
    var deposits = await _dbContext.RestaurantDeposits
        .Where(x => x.AlacarteOrderId == oldOrder.Id)
        .ToListAsync(ct);
    foreach (var d in deposits)
    {
        _dbContext.QuoteAuditLogs.Add(new QuoteAuditLog
        {
            QuoteId = quote.Id,
            Action = "DepositReLinked",
            ChangedBy = appliedBy,
            FieldName = "AlacarteOrderId",
            OldValue = $"{oldOrder.OrderNo} ({oldOrder.Id})",
            NewValue = $"{newOrder.OrderNo} ({newOrder.Id})",
            ChangeRequestId = cr.Id,
            Note = $"Deposit {d.DepositNo} ({d.Amount:N0}đ) chuyển từ đơn cũ sang đơn mới",
        });
        d.AlacarteOrderId = newOrder.Id;
        d.Note = (d.Note ?? "") + $" | Đơn gốc {oldOrder.OrderNo} → {newOrder.OrderNo}";
    }

    // 7. Reservation cleanup audit ← THÊM MỚI
    if (oldOrder.Reservation != null)
    {
        _dbContext.QuoteAuditLogs.Add(new QuoteAuditLog
        {
            QuoteId = quote.Id,
            Action = "ReservationReLinked",
            ChangedBy = appliedBy,
            FieldName = "ReservationId",
            OldValue = $"Order {oldOrder.OrderNo}",
            NewValue = $"Order {newOrder.OrderNo}",
            ChangeRequestId = cr.Id,
            Note = $"Reservation chuyển từ đơn cũ sang đơn mới",
        });
    }

    // 8. Update references
    _dbContext.AlacarteOrders.Add(newOrder);
    cr.NewTotalAmount = quote.TotalAmount;
    cr.AmountDelta = quote.TotalAmount - cr.PreviousTotalAmount;
    cr.NewOrderId = newOrder.Id;
    quote.ApprovedOrderId = newOrder.Id;

    // KHÔNG gọi SaveChanges ở đây — để caller gọi 1 lần
}
```

## 9.3. AlacarteRestaurantService.EnsureOrderEditable

**Thay đổi: 1 dòng**

```csharp
// Trước:
if (order.Status is "Paid" or "Cancelled")

// Sau:
if (order.Status is "Paid" or "Cancelled" or "Replaced")
```

---

# X. FINAL MIGRATION LIST

| # | Tên Migration | Nội dung | File |
|---|---|---|---|
| 1 | `AddQuoteChangeRequestRowVersion` | `ALTER TABLE QuoteChangeRequests ADD RowVersion ROWVERSION NOT NULL` | Tự động từ EF Core |

**Tổng: 1 migration.**

---

# XI. FINAL SERVICE LIST

| # | Service | File | Mới/Sửa | Scope |
|---|---|---|---|---|
| 1 | `QuoteChangeService` | `QuoteChangeService.cs` | **Sửa** | Scoped |
| 2 | `AlacarteRestaurantService` | `RestaurantServices.cs` | **Sửa 1 dòng** | Scoped |
| 3 | `OrderLineDomainService` | `OrderLineDomainService.cs` | **MỚI** | Scoped |
| 4 | `KitchenTicketService` | `KitchenTicketService.cs` | **MỚI** | Scoped |

---

# XII. FINAL INTERFACE LIST

| # | Interface | File | Mới/Sửa |
|---|---|---|---|
| 1 | `IQuoteChangeService` | `IRestaurantServices.cs` | Đã có |
| 2 | `IAlacarteRestaurantService` | `IRestaurantServices.cs` | Không sửa |
| 3 | `IOrderLineDomainService` | `IRestaurantServices.cs` | **MỚI** |
| 4 | `IKitchenTicketService` | `IRestaurantServices.cs` | **MỚI** |

---

# XIII. FINAL UNIT TEST LIST

| # | Test | Mục tiêu |
|---|---|---|
| 1 | `ApplyChange_FullRebuild_TransactionRollback_OnSaveFailure` | Verify transaction rollback |
| 2 | `ApplyChange_FullRebuild_DepositIsReLinked` | Verify deposit.AlacarteOrderId → newOrder |
| 3 | `ApplyChange_FullRebuild_ReservationIsCleared_OnOldOrder` | Verify oldOrder.ReservationId = null |
| 4 | `ApplyChange_FullRebuild_NewOrderHasReservation` | Verify newOrder.ReservationId = old value |
| 5 | `ApplyChange_FullRebuild_PaidAmount_IsPreserved` | Verify newOrder.PaidAmount = oldOrder.PaidAmount |
| 6 | `ApplyChange_FullRebuild_AuditLogs_AreCreated` | Verify ít nhất 3 audit log entries |
| 7 | `ApplyChange_Throws_WhenOrderIsPaid` | Verify chặn order đã Paid |
| 8 | `ApplyChange_Throws_WhenOrderIsCancelled` | Verify chặn order đã Cancelled |
| 9 | `ApplyChange_Throws_WhenChangeRequestNotApproved` | Verify chặn status != Approved |
| 10 | `ApplyChange_CaseB_ThrowsNotSupportedException` | Verify Case B throw rõ ràng |
| 11 | `OrderLineDomain_AdjustSentLine_Increase_CreatesAdjustment` | Verify domain logic |
| 12 | `OrderLineDomain_CancelLine_ReturnsCancelTicketRequest` | Verify domain logic |
| 13 | `OrderLineDomain_AllocateLines_CorrectlyDistributesPerPerson` | Verify PerPerson allocation |
| 14 | `KitchenTicket_CreateSendTicket_HasCorrectTicketNo` | Verify ticket format |
| 15 | `EnsureOrderEditable_Throws_WhenReplaced` | Verify Replaced protection |
| 16 | `RowVersion_PreventsConcurrentApply` | Verify DbUpdateConcurrencyException |

**Tổng: 16 unit tests.**

---

# XIV. FINAL INTEGRATION TEST LIST (nếu có DB)

| # | Test | Mô tả |
|---|---|---|
| 1 | `EndToEnd_FullRebuild_40To47Guests` | Tạo quote 40 khách → duyệt → request change 47 → duyệt → apply → verify order mới có 47 khách, 5 bàn |
| 2 | `EndToEnd_DepositReLink_AfterRebuild` | Tạo quote có deposit → duyệt → rebuild → verify deposit trỏ đến order mới |
| 3 | `EndToEnd_ConcurrentApply_PreventsDuplicate` | 2 thread apply cùng lúc → 1 fail với concurrency exception |

---

# XV. KẾ HOẠCH TRIỂN KHAI (THỨ TỰ)

```
Bước 1: ENTITY + MIGRATION
  ├── Thêm RowVersion vào QuoteChangeRequest
  ├── Thêm IsRowVersion() config
  ├── Tạo migration
  └── Build → PASS

Bước 2: DOMAIN SERVICES (FOUNDATION)
  ├── Tạo IOrderLineDomainService + OrderLineDomainService
  ├── Tạo IKitchenTicketService + KitchenTicketService
  ├── Đăng ký DI
  └── Build → PASS

Bước 3: QUOTECHANGESERVICE HARDENING
  ├── Wrap ApplyChangeAsync trong transaction
  ├── Thêm deposit re-link vào ApplyFullRebuildAsync
  ├── Thêm reservation cleanup vào ApplyFullRebuildAsync
  ├── Thêm audit logs
  ├── Case B → throw NotSupportedException
  ├── Bỏ IAlacarteRestaurantService dependency (không dùng nữa)
  └── Build → PASS

Bước 4: REPLACED PROTECTION
  ├── Thêm "Replaced" vào EnsureOrderEditable
  └── Build → PASS

Bước 5: UNIT TESTS
  ├── Viết 16 unit tests
  ├── Chạy test → ALL PASS
  └── Chạy toàn bộ test suite (192 test) → ALL PASS

Bước 6: VERIFY
  ├── Build solution → 0 errors
  ├── Unit tests → ALL PASS
  ├── Integration tests → ALL PASS
  └── Launch app → không lỗi khởi động
```

---

# XVI. TÓM TẮT

| Chỉ số | Giá trị |
|---|---|
| File tạo mới | 2 (`OrderLineDomainService.cs`, `KitchenTicketService.cs`) |
| File sửa | 5 (Entity, DbContext, QuoteChangeService, RestaurantServices, DI) |
| File không đụng | Tất cả file khác |
| Dòng code thêm | ~200 dòng |
| Dòng code sửa | ~60 dòng |
| Migration | 1 (`AddQuoteChangeRequestRowVersion`) |
| Unit test mới | 16 |
| Service mới | 2 (`OrderLineDomainService`, `KitchenTicketService`) |
| Interface mới | 2 (`IOrderLineDomainService`, `IKitchenTicketService`) |
| Circular dependency | 0 |
| Regression risk | Thấp (AlacarteRestaurantService chỉ sửa 1 dòng) |

---

# XVII. ĐIỀU KIỆN HOÀN THÀNH

Tất cả các điều kiện sau phải PASS:

- [ ] Build solution: 0 errors, 0 warnings
- [ ] Unit tests: 192 + 16 = 208 tests PASS
- [ ] Integration tests: 11 tests PASS
- [ ] App khởi động không lỗi
- [ ] QuoteChangeService KHÔNG inject IAlacarteRestaurantService
- [ ] QuoteChangeService.ApplyChangeAsync chỉ gọi SaveChanges 1 lần
- [ ] QuoteChangeService.ApplyChangeAsync nằm trong transaction
- [ ] Deposit được re-link khi full rebuild
- [ ] Reservation được cleanup khi full rebuild
- [ ] Order "Replaced" không thể bị sửa
- [ ] Case B throw NotSupportedException (không chạy code thiếu)
