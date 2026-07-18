# ALACARTE_CHANGE_REQUEST_P0A.md

## Triển khai P0-A — Change Request: Entities, Migration, Service, Unit Test

Ngày: 24/06/2026

---

# KẾT QUẢ

**Build: PASS (0 errors, 0 warnings)**
**Unit Tests: 192/192 PASS**
**Integration Tests: 11/11 PASS**

---

# ĐÃ TRIỂN KHAI

## 1. Entities (3 entity mới)

| Entity | File | Dòng |
|---|---|---|
| `QuoteChangeRequest` | `RestaurantEntities.cs` | ~598-632 |
| `QuoteChangeRequestDetail` | `RestaurantEntities.cs` | ~634-651 |
| `QuoteAuditLog` | `RestaurantEntities.cs` | ~653-668 |

### QuoteChangeRequest
- QuoteId, RequestNumber, PreviousGuestCount, NewGuestCount, GuestCountDelta
- Reason, Status (Pending/Approved/Rejected/Applied)
- RequestedBy/At, ReviewedBy/At, AppliedBy/At
- PreviousTotalAmount, NewTotalAmount, AmountDelta
- AdjustmentPlanJson, PreviousOrderId, NewOrderId
- Navigation: Quote, PreviousOrder, NewOrder, Details

### QuoteChangeRequestDetail
- ChangeRequestId, ChangeType (Added/Removed/QuantityChanged/TableChanged/Unchanged)
- ItemName, PreviousQuantity, NewQuantity
- PreviousAmount, NewAmount, OrderTableId, IsAlreadySent

### QuoteAuditLog
- QuoteId, Timestamp, Action (Created/Sent/Approved/ChangeRequested/ChangeApproved/ChangeRejected/ChangeApplied)
- ChangedBy, FieldName, OldValue, NewValue, ChangeRequestId

## 2. DbSet (3 DbSet mới)

| DbSet | File |
|---|---|
| `DbSet<QuoteChangeRequest>` | `AppDbContext.cs` |
| `DbSet<QuoteChangeRequestDetail>` | `AppDbContext.cs` |
| `DbSet<QuoteAuditLog>` | `AppDbContext.cs` |

## 3. EF Configuration

- `QuoteChangeRequest`: Unique index on RequestNumber, FK to Quote (Cascade), FK to PreviousOrder (NoAction), FK to NewOrder (NoAction)
- `QuoteChangeRequestDetail`: Index on ChangeRequestId
- `QuoteAuditLog`: Indexes on QuoteId, ChangeRequestId, Timestamp

## 4. Migration

| Migration | File |
|---|---|
| `20260624082548_AddQuoteChangeRequest` | `Migrations/` |

Tạo 3 bảng: `QuoteChangeRequests`, `QuoteChangeRequestDetails`, `QuoteAuditLogs`

## 5. DTOs (8 DTO mới)

| DTO | File |
|---|---|
| `QuoteChangeRequestDto` | `RestaurantDtos.cs` |
| `QuoteChangeRequestDetailDto` | `RestaurantDtos.cs` |
| `PreviewAdjustmentRequest` | `RestaurantDtos.cs` |
| `PreviewAdjustmentResponse` | `RestaurantDtos.cs` |
| `TableChangeDto` | `RestaurantDtos.cs` |
| `RequestChangeRequest` | `RestaurantDtos.cs` |
| `ReviewChangeRequest` | `RestaurantDtos.cs` |
| `ApplyChangeRequest` | `RestaurantDtos.cs` |
| `QuoteAuditLogDto` | `RestaurantDtos.cs` |

## 6. Service Interface

| Interface | File |
|---|---|
| `IQuoteChangeService` | `IRestaurantServices.cs` |

### Methods:
- `PreviewAdjustmentAsync` — Tính toán trước thay đổi, không lưu gì
- `RequestChangeAsync` — Tạo ChangeRequest (Pending)
- `ApproveChangeRequestAsync` — Duyệt (Pending → Approved)
- `RejectChangeRequestAsync` — Từ chối (Pending → Rejected)
- `ApplyChangeAsync` — Áp dụng (Approved → Applied)
- `GetChangeHistoryAsync` — Lịch sử thay đổi của Quote
- `GetAuditLogsAsync` — Nhật ký audit

## 7. Service Implementation

| File | Dòng |
|---|---|
| `QuoteChangeService.cs` | 1-515 |

### Case A — Full Rebuild (chưa gửi bếp):
1. Order cũ → Status = "Replaced"
2. Quote: cập nhật GuestCount, tính lại bàn (TableAllocationEngine), tính lại món (MenuCalculationEngine)
3. Tạo Order MỚI với tất cả line, table, extra service
4. Copy PaidAmount từ order cũ
5. Cập nhật ApprovedOrderId → order mới

### Case B — Adjustment Plan (đã gửi bếp):
1. Thêm/Xóa bàn (OrderTable)
2. Cập nhật GuestCount bàn hiện có
3. Món chưa Sent → cập nhật Qty trực tiếp
4. Món đã Sent → tăng SupplementQuantity (không sửa Qty gốc)
5. Món trên bàn bị xóa → LineStatus = "Cancelled"
6. Recalculate order

## 8. DI Registration

| File | Dòng |
|---|---|
| `DependencyInjection.cs` | +1 dòng: `services.AddScoped<IQuoteChangeService, QuoteChangeService>()` |

## 9. Unit Tests (11 test mới)

| Test | Kết quả |
|---|---|
| `QuoteChangeRequest_Entity_DefaultsAreCorrect` | PASS |
| `QuoteChangeRequestDetail_DefaultsAreCorrect` | PASS |
| `QuoteAuditLog_DefaultsAreCorrect` | PASS |
| `PreviewAdjustmentRequest_Validation_ShouldRejectZeroGuests` | PASS |
| `PreviewAdjustmentRequest_Validation_ShouldAcceptValidGuestCount` | PASS |
| `QuoteChangeRequestDto_DeltaCalculation_IsCorrect` | PASS |
| `TableChangeDto_DefaultsAreCorrect` | PASS |
| `QuoteChangeRequestDetailDto_DeltaCalculation_IsCorrect` | PASS |
| `PreviewAdjustmentResponse_DefaultsAreCorrect` | PASS |
| `AlacarteVip_TableAllocation_40To47_ShouldAddOneTable` | PASS |
| `AlacarteVip_TableAllocation_47To38_ShouldRemoveOddTable` | PASS |

---

# CHƯA TRIỂN KHAI (theo yêu cầu)

- Không sửa UI (ViewModel, XAML)
- Không sửa các module khác (POS, Banquet)
- Chưa có integration test cho QuoteChangeService (cần DB)

---

# FILES ĐÃ THAY ĐỔI / TẠO MỚI

| File | Hành động |
|---|---|
| `ASMILE.BO.Core\Entities\RestaurantEntities.cs` | Sửa — thêm 3 entity |
| `ASMILE.BO.Infrastructure\Persistence\AppDbContext.cs` | Sửa — thêm 3 DbSet + EF config |
| `ASMILE.BO.Infrastructure\Persistence\Migrations\20260624082548_AddQuoteChangeRequest.cs` | Tạo mới |
| `ASMILE.BO.Infrastructure\Persistence\Migrations\20260624082548_AddQuoteChangeRequest.Designer.cs` | Tạo mới |
| `ASMILE.BO.Application\DTOs\RestaurantDtos.cs` | Sửa — thêm 9 DTO |
| `ASMILE.BO.Application\Interfaces\IRestaurantServices.cs` | Sửa — thêm interface `IQuoteChangeService` |
| `ASMILE.BO.Infrastructure\Services\QuoteChangeService.cs` | Tạo mới |
| `ASMILE.BO.Infrastructure\DependencyInjection.cs` | Sửa — đăng ký service |
| `ASMILE.BO.Tests.Unit\RestaurantModuleTests.cs` | Sửa — thêm 11 test |
