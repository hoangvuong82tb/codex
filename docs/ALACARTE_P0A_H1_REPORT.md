# ALACARTE_P0A_H1_REPORT.md

## P0A Hardening H1 — Kết quả triển khai

Ngày: 24/06/2026

---

# KẾT QUẢ TỔNG THỂ

| Chỉ số | Kết quả |
|---|---|
| **Build** | PASS (0 errors, 0 warnings) |
| **Unit Tests** | 202/202 PASS (+10 mới) |
| **Integration Tests** | 11/11 PASS |
| **App Launch** | OK (không lỗi khởi động) |

---

# 5 VIỆC ĐÃ LÀM

## 1. RowVersion cho QuoteChangeRequest

### Entity
- Thêm `public byte[] RowVersion { get; set; } = Array.Empty<byte>();` vào `QuoteChangeRequest`

### EF Config
- Thêm `entity.Property(x => x.RowVersion).IsRowVersion();` trong `AppDbContext`

### Migration
- `20260624090950_AddQuoteChangeRequestRowVersion`
- `ALTER TABLE QuoteChangeRequests ADD RowVersion ROWVERSION NOT NULL`

### Test
- `P0A_H1_RowVersion_ShouldBePresent` — PASS

---

## 2. Transaction Boundary cho ApplyChangeAsync

### Thay đổi
- Wrap toàn bộ `ApplyChangeAsync` trong `IExecutionStrategy.ExecuteAsync` + `IDbContextTransaction`
- Chỉ gọi `SaveChangesAsync` **1 lần** ở cuối
- Bắt `DbUpdateConcurrencyException` → throw `InvalidOperationException` với message thân thiện
- Bắt các exception khác → `tx.RollbackAsync()`

### Pattern
```
strategy.ExecuteAsync(async () => {
    using var tx = await BeginTransactionAsync();
    try {
        // validate + apply + update CR + audit log
        await SaveChangesAsync();     // ← 1 LẦN DUY NHẤT
        await tx.CommitAsync();
    } catch (DbUpdateConcurrencyException) {
        await tx.RollbackAsync();
        throw new InvalidOperationException("Đã được xử lý bởi người khác...");
    } catch { await tx.RollbackAsync(); throw; }
});
```

### Test
- `P0A_H1_Transaction_ChangeRequestStatusUpdated` — PASS
- `P0A_H1_RowVersion_ShouldBePresent` — PASS (RowVersion + ConcurrencyException guard)

---

## 3. Deposit Re-link khi Full Rebuild

### Thêm trong ApplyFullRebuildAsync
- Query tất cả `RestaurantDeposit` với `AlacarteOrderId == oldOrder.Id`
- Cập nhật `AlacarteOrderId → newOrder.Id`
- Append Note: `"Đơn gốc {oldOrder.OrderNo} → {newOrder.OrderNo}"`
- Ghi AuditLog: `Action = "DepositReLinked"` với OldValue/NewValue rõ ràng
- **Không clone** deposit
- **Không sửa** `Amount`, `DepositNo`, `CashFundId`
- **Không động** vào `CashFundReceipt`

### Test
- `P0A_H1_DepositReLink_UpdatesAlacarteOrderId` — PASS
  - Verify `AlacarteOrderId` đổi sang newOrder
  - Verify `Amount` không đổi
  - Verify `Note` chứa "Đơn gốc"

---

## 4. Reservation Cleanup khi Full Rebuild

### Thêm trong ApplyFullRebuildAsync
- `oldOrder.ReservationId = null` — xóa link trên đơn cũ
- `newOrder.ReservationId = oldOrder.ReservationId` — giữ link trên đơn mới (đã có sẵn)
- Ghi AuditLog: `Action = "ReservationReLinked"`

### Test
- `P0A_H1_ReservationCleanup_OldOrderCleared_NewOrderKeeps` — PASS
  - Verify oldOrder.ReservationId = null
  - Verify newOrder.ReservationId giữ nguyên

---

## 5. Replaced Protection

### Thay đổi: **1 dòng** trong `EnsureOrderEditable`

```csharp
// Trước:
if (order.Status is "Paid" or "Cancelled")

// Sau:
if (order.Status is "Paid" or "Cancelled" or "Replaced")
```

### Test
- `P0A_H1_OrderReplaced_ShouldBeDetectedByGuard` — PASS
  - Verify "Replaced" nằm trong danh sách chặn
  - Verify "Serving", "SentKitchen" không bị chặn

---

# FILE ĐÃ SỬA

| File | Hành động | Dòng thay đổi |
|---|---|---|
| `Core/Entities/RestaurantEntities.cs` | Thêm `RowVersion` vào `QuoteChangeRequest` | +1 |
| `Infrastructure/Persistence/AppDbContext.cs` | Thêm `IsRowVersion()` config | +1 |
| `Infrastructure/Persistence/Migrations/20260624090950_AddQuoteChangeRequestRowVersion.cs` | Tạo mới | Migration tự động |
| `Infrastructure/Services/QuoteChangeService.cs` | Transaction + deposit re-link + reservation cleanup + Case B guard | ~80 dòng |
| `Infrastructure/Services/RestaurantServices.cs` | Thêm `"Replaced"` vào `EnsureOrderEditable` | **1 dòng** |
| `Tests.Unit/RestaurantModuleTests.cs` | 10 unit tests mới | +80 dòng |

---

# KIỂM TRA NGOÀI PHẠM VI

| Kiểm tra | Kết quả |
|---|---|
| Có sửa UI không? | **KHÔNG** — 0 file XAML/CS bị sửa |
| Có sửa ViewModel không? | **KHÔNG** — 0 file ViewModel bị sửa |
| Có sửa POS Bữa sáng/Cà phê không? | **KHÔNG** |
| Có sửa Banquet không? | **KHÔNG** |
| Có tạo OrderLineDomainService không? | **KHÔNG** — ngoài phạm vi P0A-H1 |
| Có tạo KitchenTicketService không? | **KHÔNG** — ngoài phạm vi P0A-H1 |
| Có refactor AlacarteRestaurantService không? | **KHÔNG** — chỉ sửa đúng 1 dòng EnsureOrderEditable |
| Có sửa migration cũ không? | **KHÔNG** — migration mới hoàn toàn |

---

# 10 UNIT TESTS — KẾT QUẢ

| # | Test | Kết quả |
|---|---|---|
| 1 | `P0A_H1_ApplyChange_Throws_WhenOrderIsPaid` | PASS |
| 2 | `P0A_H1_ApplyChange_Throws_WhenOrderIsCancelled` | PASS |
| 3 | `P0A_H1_ApplyChange_Throws_WhenChangeRequestNotApproved` | PASS |
| 4 | `P0A_H1_CaseB_HasSentLines_ShouldThrowNotSupportedException` | PASS |
| 5 | `P0A_H1_OrderReplaced_ShouldBeDetectedByGuard` | PASS |
| 6 | `P0A_H1_DepositReLink_UpdatesAlacarteOrderId` | PASS |
| 7 | `P0A_H1_ReservationCleanup_OldOrderCleared_NewOrderKeeps` | PASS |
| 8 | `P0A_H1_PaidAmount_Preserved_AfterRebuild` | PASS |
| 9 | `P0A_H1_RowVersion_ShouldBePresent` | PASS |
| 10 | `P0A_H1_Transaction_ChangeRequestStatusUpdated` | PASS |

---

# NHỮNG GÌ CHƯA LÀM (P0B)

| # | Hạng mục | Lý do |
|---|---|---|
| 1 | Case B Adjustment Ticket | Cần OrderLineDomainService — P0B |
| 2 | Case B Cancel Ticket | Cần OrderLineDomainService — P0B |
| 3 | Case B DiningTableId cho bàn mới | Cần TableSlot entity — P1 |
| 4 | OrderLineDomainService | Ngoài phạm vi P0A-H1 |
| 5 | KitchenTicketService | Ngoài phạm vi P0A-H1 |
| 6 | Audit log chi tiết từng dòng | P1 |

---

# RỦI RO CÒN LẠI

| # | Rủi ro | Mức độ | Ghi chú |
|---|---|---|---|
| 1 | Case B bị chặn hoàn toàn (throw NotSupportedException) | **THẤP** — an toàn | Đơn đã gửi bếp sẽ không thể dùng Change Request cho đến P0B. Nhân viên phải xử lý thủ công |
| 2 | QuoteChangeService vẫn inject IAlacarteRestaurantService | **THẤP** — không dùng | Dependency được giữ lại để không phá vỡ DI, nhưng không còn method nào gọi đến nó |
| 3 | Migration `RowVersion` trên bảng có dữ liệu | **THẤP** | ROWVERSION tự động generate giá trị cho row hiện có |

---

# KẾT LUẬN

P0A-H1 đã hoàn thành đúng phạm vi 5 việc. Không làm quá, không sửa lan.

- **Case A (chưa gửi bếp)**: AN TOÀN PRODUCTION — có transaction, deposit re-link, reservation cleanup, audit log
- **Case B (đã gửi bếp)**: BỊ CHẶN AN TOÀN — throw NotSupportedException với message rõ ràng
- **Replaced Order**: ĐƯỢC BẢO VỆ — không thể sửa, hủy, thanh toán
- **Concurrency**: ĐƯỢC BẢO VỆ — RowVersion + DbUpdateConcurrencyException
