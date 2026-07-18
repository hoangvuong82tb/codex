# BẰNG CHỨNG CODE — TẠI SAO UAT THẤY SAI

## Truy vết chính xác từ Source Code — 24/06/2026

---

# A. DB CÓ GÌ SAU KHI APPROVE QUOTE

## `CreateOrderForQuoteAsync` — RestaurantServices.cs:1833-1949

### 1 AlacarteOrder được tạo (lines 1842-1861)

```
Status = "Serving"                          ← line 1853
PaymentStatus = "Deposit" hoặc "Unpaid"     ← line 1854
ActualGuestCount = quote.GuestCount         ← line 1852
```

### N+1 RestaurantOrderTable được tạo (lines 1875-1893)

Với 40 khách, standard 10:

| TableNo | DisplayName | GuestCount | IsVirtual | DiningTableId |
|---------|-------------|------------|-----------|---------------|
| 0 | Bàn Tổng | 40 | true | **NULL** |
| 1 | Bàn 01 | 10 | false | **NULL** |
| 2 | Bàn 02 | 10 | false | **NULL** |
| 3 | Bàn 03 | 10 | false | **NULL** |
| 4 | Bàn 04 | 10 | false | **NULL** |

**BẰNG CHỨNG**: Lines 1877-1891 — `DiningTableId` **KHÔNG ĐƯỢC SET**. Tất cả OrderTable đều có `DiningTableId = null`.

```csharp
// Line 1877-1891 — NOT A SINGLE LINE SETS DiningTableId
var orderTable = new RestaurantOrderTable
{
    Id = Guid.NewGuid(),
    AlacarteOrderId = order.Id,
    RestaurantQuoteTableId = quoteTable.Id,
    TableNo = quoteTable.TableNo,
    DisplayName = quoteTable.DisplayName,
    GuestCount = quoteTable.GuestCount,
    SortOrder = quoteTable.SortOrder,
    IsVirtual = quoteTable.IsVirtual,
    Status = "Serving",
    Note = quoteTable.Note,
    // DiningTableId ← KHÔNG ĐƯỢC SET, MẶC ĐỊNH LÀ NULL
};
```

### M × N OrderLines được tạo (lines 1895-1945)

Với 2 món PerPerson (Tôm hùm, Bào ngư), 2 món PerTable (Cá song, Salad), 1 dịch vụ:

| OrderLine | MenuItem | Qty | RestaurantOrderTableId | Trên bàn |
|-----------|----------|-----|----------------------|----------|
| 1 | Tôm hùm | 10 | OrderTableId của Bàn 01 | **Bàn 01** |
| 2 | Tôm hùm | 10 | OrderTableId của Bàn 02 | **Bàn 02** |
| 3 | Tôm hùm | 10 | OrderTableId của Bàn 03 | **Bàn 03** |
| 4 | Tôm hùm | 10 | OrderTableId của Bàn 04 | **Bàn 04** |
| 5 | Bào ngư | 10 | OrderTableId của Bàn 01 | **Bàn 01** |
| ... | ... | ... | ... | ... |
| 9 | Cá song | 1 | OrderTableId của Bàn 01 | **Bàn 01** |
| ... | ... | ... | ... | ... |
| 13 | MC | 1 | OrderTableId của **Bàn Tổng** | **Bàn Tổng** |

**BẰNG CHỨNG**:

```csharp
// Lines 1925-1932 — PerPerson: loop qua realTables, gán OrderTableId = servingTable.Id
foreach (var servingTable in realTables)
{
    var qty = Math.Max(1, servingTable.GuestCount) * Math.Max(1, quoteLine.Quantity);
    AddOrderLine(order, servingTable, menuItem, quote, pricingRule, qty, ref lineNo, now);
}

// Lines 1935-1941 — PerTable: loop qua realTables
foreach (var servingTable in realTables)
{
    AddOrderLine(order, servingTable, menuItem, quote, pricingRule, Math.Max(1, quoteLine.Quantity), ref lineNo, now);
}

// Line 1944 — PerRoom/Manual: gán vào virtualTable
AddOrderLine(order, virtualTable, menuItem, quote, pricingRule, ..., ref lineNo, now);
```

**KẾT LUẬN A**: Service layer **ĐÃ ĐÚNG**. Món PerPerson/PerTable được phân phối xuống bàn con. Chỉ PerRoom/Manual mới vào Bàn Tổng.

---

# B. TẠI SAO UI HIỂN THỊ SAI

## ViewModel — `BuildSelectedOrderRoomTables()` line 1968

```csharp
// LINE 1970: DEAD CODE — không bao giờ chạy
if (false && SelectedOrder?.Tables?.Count > 0)  // ← "false &&" vô hiệu hóa
{
    // Code này SẼ hiển thị đúng tất cả OrderTable từ order
    foreach (var table in SelectedOrder.Tables.OrderBy(x => x.SortOrder))
    {
        yield return new DiningTableDto { ... };  // Hiển thị Bàn Tổng + Bàn 01..04
    }
    yield break;
}

// LINE 1989: Active path — gọi GetSelectedOrderServingTables()
var servingTables = GetSelectedOrderServingTables();
```

## ViewModel — `GetSelectedOrderServingTables()` line 2014

```csharp
// LINE 2053-2056: servingLookup — LỌC THEO DiningTableId.HasValue
var servingLookup = (SelectedOrder.Tables ?? Array.Empty<RestaurantOrderTableDto>())
    .Where(t => !t.IsVirtual && t.DiningTableId.HasValue)  // ← CHỈ LẤY ORDER TABLE CÓ DiningTableId
    .GroupBy(t => t.DiningTableId!.Value)
    .ToDictionary(g => g.Key, g => g.First());

// KẾT QUẢ: servingLookup = EMPTY
// Vì tất cả OrderTable từ quote flow đều có DiningTableId = NULL (xem mục A)
```

**BẰNG CHỨNG**: Line 2054 — `t.DiningTableId.HasValue`. Với quote flow, **tất cả `DiningTableId` đều null**. `servingLookup` rỗng.

```csharp
// LINE 2060-2065: Duyệt physical tables, thử match với servingLookup
foreach (var table in roomTables)
{
    if (servingLookup.TryGetValue(table.Id, out var servingTable))
    {
        // ← KHÔNG BAO GIỜ VÀO ĐÂY vì servingLookup rỗng
        table.Status = NormalizeOrderTableStatus(servingTable.Status);
        table.SeatCount = servingTable.GuestCount;
    }
    else
    {
        table.Status = "Trống";  // ← TẤT CẢ BÀN ĐỀU HIỂN THỊ "Trống"
    }
}
```

**KẾT LUẬN B**: UI hiển thị bàn "Trống" vì `DiningTableId` không được set → `servingLookup` rỗng → không thể match OrderTable với physical table.

---

# C. TẠI SAO ĐỔI SỐ KHÁCH KHÔNG HOẠT ĐỘNG

## ViewModel — không có reference đến IQuoteChangeService

```csharp
// RestaurantViewModels.cs line 21-22
private readonly IAlacarteRestaurantService _service;
private readonly IMenuCatalogService? _menuCatalog;
// ... KHÔNG CÓ IQuoteChangeService
```

**BẰNG CHỨNG**: Grep toàn bộ `D:\ASMILE-BO\ASMILE.BO.App` cho `IQuoteChangeService` → **0 kết quả**.

ViewModel có đầy đủ service (`QuoteChangeService`) nhưng **không inject, không command, không UI**.

---

# D. TẠI SAO ĐƠN MỞ HIỂN THỊ 0

## GetOpenOrdersAsync — RestaurantServices.cs:1130-1141

```csharp
.Where(x => x.Status != "Paid" && x.Status != "Cancelled")  // line 1138
```

Order vừa tạo có `Status = "Serving"` (line 1853) → **SẼ được include**.

## ViewModel — ApproveQuoteAsync line 6437

```csharp
Reset(OpenOrders, await _service.GetOpenOrdersAsync());  // ← GỌI ĐÚNG
var order = OpenOrders.FirstOrDefault(x => x.Id == approved.ApprovedOrderId);
if (order is not null) SelectedOrder = order;  // ← CHỌN ORDER VỪA TẠO
```

**KẾT LUẬN D**: Service và ViewModel **ĐÃ GỌI ĐÚNG**. Nếu user thấy 0 đơn mở, có thể do:
1. Tab đang filter theo `SelectedOrderRoomFilter` → sai phòng
2. `FilteredOpenOrders` chưa được refresh
3. UI đang ở tab khác (Dashboard/Báo giá thay vì Đơn mở)

---

# E. BẢNG TỔNG HỢP BẰNG CHỨNG

| UAT Finding | Service layer | UI layer | Root cause |
|---|---|---|---|
| Bàn Tổng có món | **ĐÚNG** — PerRoom/Manual + ExtraServices | Hiển thị đúng | — |
| Bàn con không có món | **SAI** — PerPerson/PerTable đã phân xuống đúng | **Hiển thị sai** | `DiningTableId` không được set → `servingLookup` rỗng → UI hiển thị "Trống" |
| Đơn mở = 0 | **ĐÚNG** — `GetOpenOrdersAsync` filter đúng | Cần kiểm tra tab/filter | Có thể do filter `SelectedOrderRoomFilter` hoặc chưa refresh |
| Đổi 40→47 không hoạt động | **CÓ SERVICE** — `QuoteChangeService` đầy đủ | **KHÔNG GỌI** | `IQuoteChangeService` không được inject vào ViewModel |

---

# F. FILE VÀ DÒNG CODE GÂY RA SAI LỆCH

| # | File | Dòng | Vấn đề |
|---|---|---|---|
| 1 | `RestaurantServices.cs` | **1877-1891** | `CreateOrderForQuoteAsync` không set `DiningTableId` trên OrderTable |
| 2 | `RestaurantViewModels.cs` | **2054** | `t.DiningTableId.HasValue` → filter ra empty vì `DiningTableId` = null |
| 3 | `RestaurantViewModels.cs` | **1970** | `if (false && ...)` → code hiển thị đúng bị vô hiệu hóa |
| 4 | `RestaurantViewModels.cs` | **21-22** | Không inject `IQuoteChangeService` |
