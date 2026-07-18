# ALACARTE_ORDER_TABLE_DISPLAY_FIX_REPORT.md

## Sửa hiển thị OrderTable từ Quote/Order — Không phụ thuộc DiningTableId

Ngày: 24/06/2026

---

# KẾT QUẢ

| Chỉ số | Kết quả |
|---|---|
| Build | PASS (0 errors) |
| Unit Tests | 202/202 PASS |
| Integration Tests | 11/11 PASS |
| App Launch | OK (Database is ready) |

---

# ĐÃ SỬA: 1 PHƯƠNG THỨC

## `BuildSelectedOrderRoomTables()` — RestaurantViewModels.cs:1968

### Trước khi sửa

```csharp
if (false && SelectedOrder?.Tables?.Count > 0)  // ← DEAD CODE
{
    // Code đúng nhưng không bao giờ chạy
    ...
    yield break;
}
// Fallback: GetSelectedOrderServingTables() → lọc DiningTableId.HasValue → EMPTY
```

### Sau khi sửa

```csharp
if (SelectedOrder?.Tables?.Count > 0)  // ← ACTIVATED
{
    foreach (var table in SelectedOrder.Tables.OrderBy(x => x.SortOrder))
    {
        yield return new DiningTableDto
        {
            Id = table.IsVirtual ? Guid.Empty : Guid.NewGuid(),
            OrderTableId = table.Id,                    // ← KEY: link đến RestaurantOrderTable
            Code = table.IsVirtual ? "ORDER-TOTAL" : $"ORDER-T{table.TableNo:00}",
            Name = table.DisplayName,
            SeatCount = table.GuestCount,
            TableAmount = (SelectedOrder.Lines ?? ...)
                .Where(l => l.OrderTableId == table.Id)  // ← Tổng tiền riêng từng bàn
                .Sum(l => l.Amount),
            Status = table.Status,
            SortOrder = table.SortOrder,
            IsActive = true
        };
    }
    yield break;
}
// Fallback cũ giữ nguyên cho walk-in orders (có DiningTableId)
```

---

# CÁCH HOẠT ĐỘNG

## Với Quote Order (DiningTableId = null)

```
SelectedOrder.Tables:
  [0] Id=AAA, DisplayName="Bàn Tổng", IsVirtual=true,  GuestCount=40
  [1] Id=BBB, DisplayName="Bàn 01",   IsVirtual=false, GuestCount=10
  [2] Id=CCC, DisplayName="Bàn 02",   IsVirtual=false, GuestCount=10
  [3] Id=DDD, DisplayName="Bàn 03",   IsVirtual=false, GuestCount=10
  [4] Id=EEE, DisplayName="Bàn 04",   IsVirtual=false, GuestCount=10

→ BuildSelectedOrderRoomTables() yields:

  Bàn Tổng: Id=Guid.Empty,   OrderTableId=AAA, SeatCount=40
  Bàn 01:   Id=<new guid>,   OrderTableId=BBB, SeatCount=10
  Bàn 02:   Id=<new guid>,   OrderTableId=CCC, SeatCount=10
  Bàn 03:   Id=<new guid>,   OrderTableId=DDD, SeatCount=10
  Bàn 04:   Id=<new guid>,   OrderTableId=EEE, SeatCount=10
```

## Khi click Bàn 01

```
SelectedTable = { Id=<guid>, OrderTableId=BBB, Name="Bàn 01" }

BuildSelectedOrderFoodLines():
  → IsSummaryTableSelected() → false (Id != Guid.Empty)
  → Filter: x.OrderTableId == SelectedTable.OrderTableId  // == BBB
  → Chỉ hiện món có OrderTableId = BBB (món của Bàn 01)
```

## Khi click Bàn Tổng

```
SelectedTable = { Id=Guid.Empty, OrderTableId=AAA, Name="Bàn Tổng" }

BuildSelectedOrderFoodLines():
  → IsSummaryTableSelected() → true (Id == Guid.Empty)
  → AggregateOrderLines() → tổng hợp TẤT CẢ món từ mọi bàn
```

---

# DỮ LIỆU MONG ĐỢI SAU FIX

## Quote 40 khách, bàn 10 → Duyệt → Đơn mở

### Bàn Tổng (click vào)
| Món | SL | Đơn giá | Thành tiền |
|---|---|---|---|
| MC | 1 | 1,200,000 | 1,200,000 |
| Tôm hùm | 40 | 350,000 | 14,000,000 |
| Cá song | 4 | 280,000 | 1,120,000 |
| Bào ngư | 40 | 180,000 | 7,200,000 |
| Salad | 4 | 60,000 | 240,000 |

### Bàn 01 (click vào)
| Món | SL | Đơn giá | Thành tiền |
|---|---|---|---|
| Tôm hùm | 10 | 350,000 | 3,500,000 |
| Cá song | 1 | 280,000 | 280,000 |
| Bào ngư | 10 | 180,000 | 1,800,000 |
| Salad | 1 | 60,000 | 60,000 |

### Bàn 02-04: tương tự Bàn 01

---

# FILE ĐÃ SỬA

| File | Dòng | Thay đổi |
|---|---|---|
| `ViewModels/RestaurantViewModels.cs` | 1970 | `if (false &&` → `if (` (kích hoạt code) |
| `ViewModels/RestaurantViewModels.cs` | 1976-1983 | Thêm `OrderTableId`, tính `TableAmount` riêng từng bàn |

**Tổng: 1 file, ~10 dòng thay đổi.**

---

# KHÔNG SỬA

- Không thêm entity mới
- Không thêm migration
- Không sửa service
- Không sửa XAML
- Không sửa Settings
- Không sửa Pricing engine
- Không sửa Change Request UI
