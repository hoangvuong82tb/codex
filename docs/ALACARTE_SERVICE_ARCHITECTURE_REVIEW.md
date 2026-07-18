# ALACARTE_SERVICE_ARCHITECTURE_REVIEW.md

## Đánh giá kiến trúc Service — Module À La Carte Change Request

Ngày: 24/06/2026

---

# PHẦN 1: SƠ ĐỒ PHỤ THUỘC HIỆN TẠI

```
┌─────────────────────────────────────────────────────────────┐
│                      DEPENDENCY GRAPH                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  QuoteChangeService                                         │
│       │                                                     │
│       ├── AppDbContext (scoped)                             │
│       ├── ITableAllocationEngine (singleton, stateless)     │
│       ├── IMenuCalculationEngine (singleton, stateless)     │
│       └── IAlacarteRestaurantService (scoped) ← NEW DEP     │
│                                                             │
│  AlacarteRestaurantService                                  │
│       │                                                     │
│       ├── AppDbContext (scoped)                             │
│       ├── IMenuCatalogService (scoped)                     │
│       ├── ICashFundService (scoped)                        │
│       ├── ITableAllocationEngine (singleton)                │
│       ├── IMenuCalculationEngine (singleton)                │
│       └── IQuoteChangeService ← NOT DEPENDENT ✓            │
│                                                             │
│  QuoteChangeService ──CALLS──▶ AlacarteRestaurantService    │
│         .AddOrUpdateLineAsync()                             │
│         .RemoveLineAsync()                                  │
│                                                             │
│  NO CIRCULAR DEPENDENCY (currently)                         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Kết luận: **Chưa có circular dependency. Nhưng đang có coupling không lành mạnh.**

---

# PHẦN 2: PHÂN TÍCH CHI TIẾT

---

## 2.1. QuoteChangeService gọi AlacarteRestaurantService

### Luồng hiện tại (chưa gọi)

```
QuoteChangeService.ApplyAdjustmentPlan()
  → Tự set SupplementQuantity
  → Tự set LineStatus = "Cancelled"
  → Tự Recalculate
  → SaveChanges
```

### Luồng Hardening đề xuất (gọi sang AlacarteRestaurantService)

```
QuoteChangeService.ApplyAdjustmentPlan()
  → _alacarteService.AddOrUpdateLineAsync(request)   // Cho món tăng
  → _alacarteService.RemoveLineAsync(request)         // Cho món giảm
```

### Vấn đề 1: TRANSACTION BOUNDARY VIOLATION

`AddOrUpdateLineAsync` và `RemoveLineAsync` **tự gọi `SaveChangesAsync`** bên trong:

```csharp
// AddOrUpdateLineAsync (line 792)
await _dbContext.SaveChangesAsync(cancellationToken);
return await LoadOrderDtoAsync(order.Id, cancellationToken);

// RemoveLineAsync (line 848)
await _dbContext.SaveChangesAsync(cancellationToken);
return await LoadOrderDtoAsync(order.Id, cancellationToken);
```

Nếu `QuoteChangeService.ApplyChangeAsync` wrap toàn bộ trong transaction:

```
Transaction BEGIN
  ApplyAdjustmentPlan()
    → AddOrUpdateLineAsync(line 1)
      → SaveChanges  ← COMMIT LINE 1 NGAY LẬP TỨC (thoát transaction)
    → AddOrUpdateLineAsync(line 2)
      → SaveChanges  ← COMMIT LINE 2
    → RemoveLineAsync(line 3)
      → SaveChanges  ← COMMIT LINE 3
  SaveChanges ← Chỉ còn ChangeRequest + AuditLog
Transaction COMMIT
```

**Hậu quả**: Mỗi lần gọi `AddOrUpdateLineAsync` là 1 transaction riêng. Nếu line 3 fail → line 1 và 2 đã được commit → **dữ liệu không nhất quán**.

### Vấn đề 2: SHARED DBCONTEXT — CÓ THỂ GIẢI QUYẾT ĐƯỢC

Cả 2 service dùng chung 1 `AppDbContext` instance (scoped). Nếu QuoteChangeService mở transaction TRƯỚC KHI gọi `AddOrUpdateLineAsync`, thì `SaveChangesAsync` bên trong `AddOrUpdateLineAsync` sẽ nằm trong transaction đó.

**NHƯNG**: `AddOrUpdateLineAsync` gọi `SaveChangesAsync` và SAU ĐÓ gọi `LoadOrderDtoAsync` để đọc lại order từ DB. Việc đọc lại này sẽ thấy dữ liệu đã commit trong transaction (dirty read). Điều này OK trong cùng 1 transaction.

**Vấn đề thực sự**: Nếu `AddOrUpdateLineAsync` fail → nó throw → QuoteChangeService catch → rollback transaction. Nhưng `AddOrUpdateLineAsync` đã gọi `SaveChangesAsync` TRƯỚC KHI fail (nếu fail xảy ra sau SaveChanges). Lúc này transaction đã có dữ liệu dirty → rollback sẽ hủy toàn bộ.

**Kết luận**: VỀ MẶT KỸ THUẬT CÓ THỂ HOẠT ĐỘNG nếu QuoteChangeService mở transaction trước. Nhưng **mong manh** — phụ thuộc vào implementation detail của AlacarteRestaurantService.

### Vấn đề 3: SIDE EFFECT KHÔNG KIỂM SOÁT

`AddOrUpdateLineAsync` làm nhiều việc:
1. Validate order (EnsureOrderEditable)
2. Lookup menu item từ DB
3. Merge line nếu trùng
4. Nếu đã Sent → gọi `ApplySentLineAdjustmentAsync` → tạo ticket + stock issue
5. Recalculate
6. SaveChanges

QuoteChangeService **không kiểm soát được** các side effect này. Ví dụ:
- `ApplySentLineAdjustmentAsync` tạo `PreparationTicket` → QuoteChangeService không biết
- `CreateConsumptionIssuesAsync` tạo `StockIssue` → QuoteChangeService không biết
- `EnsureOrderEditable` có thể throw → QuoteChangeService phải handle

### Vấn đề 4: DEPENDENCY KHÔNG RÕ RÀNG

Nhìn từ ngoài, không rõ `QuoteChangeService` phụ thuộc vào `IAlacarteRestaurantService` ở mức độ nào. Interface `IAlacarteRestaurantService` có 40+ methods — rất khó mock trong unit test.

---

## 2.2. AlacarteRestaurantService — GOD SERVICE

`AlacarteRestaurantService` đang là **God Service** với quá nhiều responsibilities:

| Responsibility | Methods |
|---|---|
| Room & Table CRUD | `GetRoomsAsync`, `SaveRoomAsync`, `DeleteRoomAsync`, `GetTablesAsync`, `SaveTableAsync`, `DeleteTableAsync` |
| Reservation | `ValidateReservationAsync`, `SaveReservationAsync`, `GetReservationsAsync` |
| Order Lifecycle | `OpenOrderAsync`, `OpenRoomWalkInOrderAsync`, `ConfigureRoomWalkInOrderAsync` |
| Order Lines | `AddOrUpdateLineAsync`, `RemoveLineAsync`, `AddExtraServiceToOrderAsync` |
| Kitchen | `SendToPreparationAsync`, `CreatePreparationTicketAsync`, `ApplySentLineAdjustmentAsync` |
| Stock | `CreateConsumptionIssuesAsync` |
| Payment | `PayOrderAsync`, `CancelOrderAsync` |
| Deposit | `AddDepositAsync` |
| Quote | `GetQuotesAsync`, `SaveQuoteAsync`, `ApproveQuoteAsync`, `ArchiveQuoteAsync`, `DeleteQuoteAsync` |
| Quote→Order | `CreateOrderForQuoteAsync` |
| Contract | `GetContractsAsync`, `SaveContractAsync`, `CreateContractFromQuoteAsync` |
| Serving Groups | `GetServingGroupsAsync`, `SaveServingGroupAsync`, `DeleteServingGroupAsync` |
| Menu Packages | `GetMenuPackagesAsync` |
| Extra Services | `GetExtraServicesAsync`, `SaveExtraServiceAsync`, `SeedDefaultExtraServicesAsync` |

**~45 methods trong 1 class.**

Khi `QuoteChangeService` gọi vào class này, nó đang couple với **toàn bộ** logic trên, không chỉ phần nó cần.

---

## 2.3. RỦI RO CIRCULAR DEPENDENCY TRONG TƯƠNG LAI

Hiện tại chưa có circular dependency. Nhưng **rất dễ phát sinh**:

```
Tình huống giả định:
  AlacarteRestaurantService.AddOrUpdateLineAsync()
    → Kiểm tra: "Có ChangeRequest đang pending không?"
    → Nếu có → chặn hoặc cảnh báo
    → Cần inject IQuoteChangeService vào AlacarteRestaurantService
    → CIRCULAR DEPENDENCY: A → B → A
```

Đây là pattern rất phổ biến: "khi đang có thay đổi pending thì không cho sửa order". Nếu implement theo cách direct call → **sẽ tạo circular dependency**.

---

# PHẦN 3: ĐỀ XUẤT KIẾN TRÚC MỚI

## 3.1. NGUYÊN TẮC

1. **Domain logic tách khỏi Service** — pure functions, testable, không DB
2. **Service chỉ orchestrate** — gọi domain logic + persistence
3. **Cross-cutting concerns dùng Event** — kitchen ticket, stock issue, audit log
4. **Không service nào gọi service khác trực tiếp** — trừ khi cùng layer và không tạo cycle

## 3.2. TÁCH LAYER

```
┌──────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                          │
│                                                              │
│  QuoteChangeService     AlacarteOrderService                 │
│  (quote changes)        (order lines, kitchen)               │
│       │                      │                               │
│       └────── không gọi ─────┘                               │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│                    DOMAIN LAYER                               │
│                                                              │
│  OrderLineDomainService (SHARED — stateless)                 │
│       │                                                      │
│       ├── AdjustSentLine(AlacarteOrderLine, newQty)          │
│       │     → Trả về: LineAdjustment {                       │
│       │         UpdatedLine,                                 │
│       │         AdjustmentTicketRequest?,                    │
│       │         StockIssueRequest?                           │
│       │       }                                              │
│       │                                                      │
│       ├── CancelLine(AlacarteOrderLine, reason)               │
│       │     → Trả về: LineCancellation {                     │
│       │         CancelledLine,                               │
│       │         CancelTicketRequest?,                        │
│       │         StockReversalRequest?                        │
│       │       }                                              │
│       │                                                      │
│       └── AllocateLines(QuoteLines, TableAllocation)         │
│             → Trả về: List<OrderLineAllocation>              │
│                                                              │
│  TableAllocationEngine (đã có)                               │
│  MenuCalculationEngine (đã có)                               │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│                    INFRASTRUCTURE LAYER                       │
│                                                              │
│  KitchenTicketService (mới — tách từ AlacarteService)        │
│       │                                                      │
│       └── CreateTicket(Order, Lines, TicketType)             │
│                                                              │
│  StockConsumptionService (mới — tách từ AlacarteService)     │
│       │                                                      │
│       └── CreateConsumptionIssue(Order, Lines)               │
│                                                              │
│  AppDbContext (đã có)                                        │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│                    DOMAIN EVENTS (tương lai)                  │
│                                                              │
│  OrderRebuiltEvent { OldOrderId, NewOrderId, QuoteId }       │
│       → DepositRelinkHandler                                 │
│       → ReservationCleanupHandler                            │
│       → AuditLogHandler                                      │
│                                                              │
│  KitchenTicketCreatedEvent { TicketId, OrderId }             │
│       → StockConsumptionHandler                              │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

## 3.3. CHI TIẾT OrderLineDomainService (SHARED)

Đây là service **stateless, pure domain logic**, không inject DbContext:

```csharp
public sealed class OrderLineDomainService
{
    // Pure calculation — không DB, không side effect
    public LineAdjustment AdjustSentLine(
        AlacarteOrderLine existingLine,
        decimal newQty,
        string reason)
    {
        var currentQty = existingLine.Qty + (existingLine.SupplementQuantity ?? 0);
        if (newQty <= currentQty)
            return new LineAdjustment { UpdatedLine = existingLine, NoChange = true };

        var delta = newQty - currentQty;
        existingLine.SupplementQuantity = (existingLine.SupplementQuantity ?? 0) + delta;

        return new LineAdjustment
        {
            UpdatedLine = existingLine,
            AdjustmentTicketRequest = new KitchenTicketRequest
            {
                TicketType = "Send",
                Lines = [new TicketLine { MenuItemName = existingLine.MenuItemName, Qty = delta }],
                Note = reason
            }
        };
    }

    public LineCancellation CancelLine(
        AlacarteOrderLine line,
        string reason,
        bool wasSentToKitchen)
    {
        line.LineStatus = "Cancelled";

        return new LineCancellation
        {
            CancelledLine = line,
            CancelTicketRequest = wasSentToKitchen
                ? new KitchenTicketRequest
                {
                    TicketType = "Cancel",
                    Lines = [new TicketLine { MenuItemName = line.MenuItemName, Qty = line.Qty }],
                    Note = reason
                }
                : null
        };
    }
}
```

**Cả `QuoteChangeService` và `AlacarteRestaurantService` đều dùng chung `OrderLineDomainService` này.**

---

## 3.4. KIẾN TRÚC MỚI CHO ApplyAdjustmentPlan

```
QuoteChangeService.ApplyAdjustmentPlan()
  │
  ├── 1. Tính toán thay đổi bàn (TableAllocationEngine.Allocate)
  ├── 2. Thêm/Xóa OrderTable
  │
  ├── 3. Với mỗi line cần điều chỉnh:
  │       │
  │       ├── Gọi OrderLineDomainService.AdjustSentLine(line, newQty, reason)
  │       │     → Trả về LineAdjustment (UpdatedLine + TicketRequest)
  │       │
  │       ├── Gọi OrderLineDomainService.CancelLine(line, reason, wasSent)
  │       │     → Trả về LineCancellation (CancelledLine + CancelTicketRequest)
  │       │
  │       └── Với line chưa Sent → cập nhật Qty trực tiếp
  │
  ├── 4. Tạo KitchenTicket từ TicketRequest (nếu có)
  │       → KitchenTicketService.CreateTicket(order, ticketRequest)
  │
  ├── 5. Recalculate(order)
  ├── 6. SaveChanges (1 LẦN DUY NHẤT)
  │
  └── 7. AuditLog
```

**QuoteChangeService KHÔNG CÒN gọi `AddOrUpdateLineAsync` hay `RemoveLineAsync` nữa.** Nó dùng chung `OrderLineDomainService` và `KitchenTicketService`.

---

## 3.5. KIẾN TRÚC MỚI CHO ApplyFullRebuild

```
QuoteChangeService.ApplyFullRebuild()
  │
  ├── 1. oldOrder.Status = "Replaced"
  ├── 2. oldOrder.ReservationId = null
  ├── 3. Rebuild quote tables (TableAllocationEngine)
  ├── 4. Recalculate quote lines (MenuCalculationEngine)
  ├── 5. Create newOrder
  │
  ├── 6. Allocate lines vào newOrder:
  │       → OrderLineDomainService.AllocateLines(quoteLines, allocation)
  │
  ├── 7. Re-link deposits:
  │       → deposit.AlacarteOrderId = newOrder.Id (direct update)
  │
  ├── 8. Recalculate(newOrder)
  ├── 9. SaveChanges (1 LẦN)
  └── 10. AuditLog
```

---

# PHẦN 4: DOMAIN EVENT VS DIRECT CALL

## 4.1. So sánh

| Tiêu chí | Direct Call | Domain Event |
|---|---|---|
| **Độ phức tạp** | Thấp | Trung bình-Cao |
| **Tính rõ ràng** | Rõ ràng (gọi method → kết quả ngay) | Khó trace (event → handler → ...) |
| **Coupling** | Tight coupling | Loose coupling |
| **Transaction** | Dễ quản lý (cùng DbContext) | Phức tạp (có thể khác transaction) |
| **Phù hợp khi** | Logic nằm trong cùng aggregate | Logic cross-aggregate hoặc cross-bounded-context |
| **Test** | Dễ (mock interface) | Trung bình (cần event bus) |

## 4.2. Đề xuất cho từng loại tương tác

| Tương tác | Cơ chế | Lý do |
|---|---|---|
| **Tính toán line adjustment** | **Direct Call** — Shared Domain Service | Pure logic, không side effect, cần kết quả ngay |
| **Tạo Kitchen Ticket** | **Direct Call** — KitchenTicketService | Cùng DbContext transaction, cần ticket ID để ref |
| **Tạo Stock Issue** | **Direct Call** — StockConsumptionService | Cùng DbContext transaction |
| **Re-link Deposit** | **Direct Call** — update entity | Đơn giản, 1 dòng SQL |
| **Audit Log** | **Direct Call** — thêm entity | Cùng transaction với thay đổi chính |
| **Thông báo cho module khác** | **Domain Event** (tương lai) | Ví dụ: "Order đã thay đổi" → thông báo cho CashFund, Inventory report |
| **Gửi email cho khách** | **Domain Event** (tương lai) | Không cần đồng bộ, có thể retry |

## 4.3. Kết luận

**Giai đoạn P0A Hardening**: Dùng **Direct Call** cho tất cả. Event-based architecture là over-engineering ở giai đoạn này.

**Giai đoạn P2+**: Cân nhắc Domain Event cho:
- `OrderRebuiltEvent` → thông báo CashFund, Reservation, Report
- `ChangeRequestApprovedEvent` → gửi email cho khách

---

# PHẦN 5: SERVICE TÁCH ĐỀ XUẤT

## 5.1. Các service mới nên tách

| Service | Responsibility | Tách từ đâu |
|---|---|---|
| **OrderLineDomainService** | Pure logic: adjust line, cancel line, allocate lines | Logic nghiệp vụ từ `AlacarteRestaurantService` |
| **KitchenTicketService** | Tạo PreparationTicket, quản lý ticket | Tách từ `AlacarteRestaurantService.CreatePreparationTicketAsync` |
| **StockConsumptionService** | Tạo StockIssue cho consumption | Tách từ `AlacarteRestaurantService.CreateConsumptionIssuesAsync` |

## 5.2. Dependency graph sau khi tách

```
QuoteChangeService
  ├── AppDbContext
  ├── ITableAllocationEngine (stateless)
  ├── IMenuCalculationEngine (stateless)
  ├── OrderLineDomainService (stateless)       ← SHARED, mới
  ├── KitchenTicketService (scoped)            ← SHARED, mới
  └── StockConsumptionService (scoped)         ← SHARED, mới

AlacarteRestaurantService
  ├── AppDbContext
  ├── IMenuCatalogService
  ├── ICashFundService
  ├── ITableAllocationEngine
  ├── IMenuCalculationEngine
  ├── OrderLineDomainService (stateless)       ← SHARED
  ├── KitchenTicketService (scoped)            ← SHARED
  └── StockConsumptionService (scoped)         ← SHARED

KHÔNG service nào gọi service nào khác.
Tất cả dùng chung Domain Services và Infrastructure Services.
```

---

# PHẦN 6: P0A HARDENING — KIẾN TRÚC THỰC TẾ

Không cần tách toàn bộ ngay. **Chỉ tách phần cần thiết cho Hardening.**

## Những gì nên làm NGAY (P0A Hardening)

```
1. TÁCH OrderLineDomainService (stateless, pure logic)
   → Dùng trong QuoteChangeService.ApplyAdjustmentPlan
   → Dùng trong AlacarteRestaurantService.ApplySentLineAdjustmentAsync (sau này)
   → KHÔNG phá vỡ code hiện tại (chỉ thêm, không sửa)

2. GIỮ NGUYÊN mọi thứ khác
   → QuoteChangeService.ApplyFullRebuildAsync: tự tạo line (không gọi service khác)
   → QuoteChangeService.ApplyAdjustmentPlan: dùng OrderLineDomainService + tự gọi KitchenTicketService
   → AlacarteRestaurantService: KHÔNG THAY ĐỔI
```

## Dependency graph P0A Hardening

```
QuoteChangeService
  ├── AppDbContext
  ├── ITableAllocationEngine (đã có)
  ├── IMenuCalculationEngine (đã có)
  ├── OrderLineDomainService (MỚI — stateless, shared)
  └── KitchenTicketService (MỚI — tách logic tạo ticket)

AlacarteRestaurantService
  └── KHÔNG THAY ĐỔI (giữ nguyên để tránh regression)
```

## Code cần viết mới

| File | Nội dung |
|---|---|
| `OrderLineDomainService.cs` | ~80 dòng — pure domain logic |
| `KitchenTicketService.cs` | ~60 dòng — tách logic tạo ticket từ AlacarteService |
| `QuoteChangeService.cs` | Sửa `ApplyAdjustmentPlan` — dùng 2 service mới |
| `DependencyInjection.cs` | Đăng ký 2 service mới (Scoped) |

---

# PHẦN 7: KẾT LUẬN

1. **Chưa có circular dependency** — an toàn ở hiện tại.

2. **Đề xuất Hardening trong P0A có rủi ro** nếu để QuoteChangeService gọi trực tiếp `AddOrUpdateLineAsync` / `RemoveLineAsync`:
   - Transaction boundary bị phá vỡ (mỗi method tự SaveChanges)
   - Tight coupling vào God Service (45 methods)
   - Dễ tạo circular dependency trong tương lai

3. **Giải pháp**: Tách `OrderLineDomainService` (stateless shared logic) và `KitchenTicketService` (ticket creation). Cả 2 service cùng dùng, không service nào gọi service nào.

4. **Domain Event để dành cho P2+** — không cần ở P0A.

5. **Chi phí**: 2 file mới (~140 dòng code), sửa 1 method trong QuoteChangeService, không đụng AlacarteRestaurantService.
