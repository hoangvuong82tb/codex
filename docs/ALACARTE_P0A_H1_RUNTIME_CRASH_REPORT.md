# ALACARTE_P0A_H1_RUNTIME_CRASH_REPORT.md

## Runtime Crash — P0A-H1

Ngày: 24/06/2026

---

# LỖI THẬT

## Stacktrace

```
[2026-06-24 16:13:53 ERR] Failed executing DbCommand
CREATE TABLE [QuoteAuditLogs] (
    ...
    CONSTRAINT [FK_QuoteAuditLogs_RestaurantQuotes_QuoteId] 
        FOREIGN KEY ([QuoteId]) REFERENCES [RestaurantQuotes] ([Id]) ON DELETE CASCADE,
    ...
);

Microsoft.Data.SqlClient.SqlException (0x80131904): 
  Introducing FOREIGN KEY constraint 'FK_QuoteAuditLogs_RestaurantQuotes_QuoteId' 
  on table 'QuoteAuditLogs' may cause cycles or multiple cascade paths. 
  Specify ON DELETE NO ACTION or ON UPDATE NO ACTION, or modify other FOREIGN KEY constraints.
  Could not create constraint or index. See previous errors.

Error Number: 1785, State: 0, Class: 16
```

## Vị trí gây lỗi

| File | Dòng | Nội dung |
|---|---|---|
| `AppDbContext.cs` | 4123 | `entity.HasOne(x => x.Quote).WithMany().HasForeignKey(x => x.QuoteId).OnDelete(DeleteBehavior.Cascade)` |
| `Migrations/20260624082548_AddQuoteChangeRequest.cs` | 94 | `onDelete: ReferentialAction.Cascade` |

## Nguyên nhân

SQL Server **không cho phép multiple cascade paths** đến cùng một bảng.

Cụ thể, khi `RestaurantQuotes` bị xóa:
- **Đường 1**: `RestaurantQuotes` → `QuoteChangeRequests` (CASCADE)
- **Đường 2**: `RestaurantQuotes` → `QuoteAuditLogs` (CASCADE)
- **Đường 3** (gián tiếp): `RestaurantQuotes` → `QuoteChangeRequests` (CASCADE) → `QuoteAuditLogs` (SET NULL)

SQL Server phát hiện rằng một hàng trong `QuoteAuditLogs` có thể bị ảnh hưởng qua nhiều đường cascade khác nhau → từ chối tạo FK.

Đây là lỗi từ **P0-A ban đầu** (migration `AddQuoteChangeRequest`), không phải từ P0A-H1. Nhưng migration đó chưa bao giờ được áp dụng thành công vào DB. Đến khi P0A-H1 tạo thêm migration `AddQuoteChangeRequestRowVersion`, cả 2 migration cùng chạy và lỗi xuất hiện.

## Fix

| Thay đổi | File |
|---|---|
| FK `QuoteAuditLog → Quote`: `Cascade` → `NoAction` | `AppDbContext.cs` line 4123 |
| Cập nhật migration: `ReferentialAction.Cascade` → `ReferentialAction.NoAction` | `Migrations/20260624082548_AddQuoteChangeRequest.cs` line 94 |
| Remove + recreate RowVersion migration (model snapshot changed) | `Migrations/20260624090950_AddQuoteChangeRequestRowVersion.cs` |

## Kết quả sau fix

| Chỉ số | Kết quả |
|---|---|
| Build | PASS |
| Unit Tests | 202/202 PASS |
| Migration apply | "Database is ready" — thành công |
| App Launch | OK |
