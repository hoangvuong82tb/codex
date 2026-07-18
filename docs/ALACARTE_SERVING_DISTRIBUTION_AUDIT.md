# ALACARTE_SERVING_DISTRIBUTION_AUDIT.md

## Audit phân phối món từ Quote → Order → Serving Tables

Ngày: 24/06/2026

---

# KẾT LUẬN CHÍNH

**Service layer phân phối món ĐÚNG. Nhưng PricingRule mặc định là "PerTable" thay vì "PerPerson" → hành vi không như kỳ vọng.**

---

# 1. CHỨNG MINH: 47 KHÁCH TẠO 10+10+10+10+7

## Code: `SaveQuoteAsync` → `TableAllocationEngine.Allocate`

### File: `RestaurantServices.cs:1344-1346`

```csharp
var allocation = _tableAllocationEngine.Allocate(request.GuestCount, tableStandard);
```

### File: `AlacarteVipEngines.cs:9-46`

```csharp
public List<TableAllocationResult> Allocate(int guestCount, int tableStandard)
{
    // Luôn tạo Bàn Tổng (virtual) đầu tiên
    result.Add(new TableAllocationResult { TableNo=0, DisplayName="Ban Tong", 
                GuestCount=guestCount, IsVirtual=true });

    // Sau đó BuildSuggestedAllocations(guestCount, tableStandard)
    var suggested = BuildSuggestedAllocations(guestCount, tableStandard);
    // → 47/10 = 4 bàn đầy + dư 7 → tạo bàn lẻ
    // → 4 bàn × 10 + 1 bàn × 7
}
```

### Kết quả với 47 khách, standard 10:

| TableNo | DisplayName | GuestCount | IsVirtual |
|---|---|---|---|
| 0 | Bàn Tổng | 47 | true |
| 1 | Bàn 01 | 10 | false |
| 2 | Bàn 02 | 10 | false |
| 3 | Bàn 03 | 10 | false |
| 4 | Bàn 04 | 10 | false |
| 5 | Bàn 05 | 7 | false |

**KẾT LUẬN 1: ĐÚNG — Hệ thống tạo 10+10+10+10+7.**

---

# 2. CHỨNG MINH: ORDERLINE PHÂN PHỐI XUỐNG TỪNG BÀN

## Code: `CreateOrderForQuoteAsync` — RestaurantServices.cs:1895-1945

```csharp
// Line 1895-1896: Tách virtual và real tables
var virtualTable = order.OrderTables.FirstOrDefault(x => x.IsVirtual);
var realTables = order.OrderTables.Where(x => !x.IsVirtual).OrderBy(x => x.SortOrder).ToList();

// Line 1924: Lấy pricing rule từ QuoteLine
var pricingRule = NormalizePricingRule(quoteLine.PricingRule, false);
```

### Trường hợp PerPerson (line 1925-1932):

```csharp
if (string.Equals(pricingRule, "PerPerson", StringComparison.OrdinalIgnoreCase))
{
    foreach (var servingTable in realTables)        // ← LẶP QUA TỪNG BÀN THẬT
    {
        var qty = Math.Max(1, servingTable.GuestCount)   // ← SL = GuestCount × Quantity
                  * Math.Max(1, quoteLine.Quantity);
        AddOrderLine(order, servingTable, ...);          // ← GÁN VÀO BÀN CON
    }
}
```

**Với 47 khách, "Bạc xỉu PerPerson", Quantity=1:**

| Bàn | GuestCount | Công thức | Qty |
|---|---|---|---|
| Bàn 01 | 10 | 10 × 1 | **10** |
| Bàn 02 | 10 | 10 × 1 | **10** |
| Bàn 03 | 10 | 10 × 1 | **10** |
| Bàn 04 | 10 | 10 × 1 | **10** |
| Bàn 05 | 7 | 7 × 1 | **7** |
| **Tổng** | | | **47** |

### Trường hợp PerTable (line 1935-1941):

```csharp
if (string.Equals(pricingRule, "PerTable", StringComparison.OrdinalIgnoreCase))
{
    foreach (var servingTable in realTables)
    {
        AddOrderLine(order, servingTable, ..., Math.Max(1, quoteLine.Quantity), ...);
        // ← MỖI BÀN NHẬN Quantity (thường là 1)
    }
}
```

**Với 47 khách, "Cá song PerTable", Quantity=1:**

| Bàn | Qty |
|---|---|
| Bàn 01 | **1** |
| Bàn 02 | **1** |
| Bàn 03 | **1** |
| Bàn 04 | **1** |
| Bàn 05 | **1** |
| **Tổng** | **5** |

### Trường hợp PerRoom/Manual (line 1944):

```csharp
// Chỉ gán vào Bàn Tổng (virtualTable)
AddOrderLine(order, virtualTable, ..., ref lineNo, now);
```

### AddOrderLine (line 1951-1978):

```csharp
private void AddOrderLine(..., RestaurantOrderTable? orderTable, ...)
{
    var line = new AlacarteOrderLine
    {
        RestaurantOrderTableId = orderTable?.Id,  // ← GÁN ĐÚNG OrderTableId
        Qty = qty,
        ...
    };
}
```

**KẾT LUẬN 2: ĐÚNG — `RestaurantOrderTableId` được set chính xác cho từng bàn.**

---

# 3. VẬY TẠI SAO UAT THẤY "BÀN CUỐI 7 KHÁCH KHÔNG CÓ MÓN"?

## Đây là câu hỏi then chốt.

**Nếu PricingRule = "PerPerson"**: Món ĐƯỢC phân phối đúng xuống từng bàn, bao gồm bàn 7 khách.

**Nếu PricingRule = "PerTable"**: Mỗi bàn nhận Quantity (thường = 1). KHÔNG phụ thuộc số khách. Bàn 7 khách vẫn nhận 1 phần.

**Nếu PricingRule = "Manual" hoặc RỖNG**: Món chỉ vào Bàn Tổng (virtual table) — KHÔNG phân xuống bàn con nào.

---

# 4. ROOT CAUSE #1: PRICING RULE MẶC ĐỊNH LÀ "PerTable"

## Dòng code gây ra:

### File: `RestaurantViewModels.cs:1135`
```csharp
// AddMenuItemToQuote — khi user thêm món vào quote
QuoteLines.Add(new QuoteLineItem { 
    MenuItemId = item.Id, Name = item.Name, 
    UnitPrice = item.UnitPrice, Quantity = 1, 
    Type = "Món ăn", 
    PricingRule = "PerTable"    // ← HARDCODE "PerTable"
});
```

### File: `RestaurantViewModels.cs:5788`
```csharp
// SaveQuoteAsync ViewModel — khi lưu quote
PricingRule = string.IsNullOrWhiteSpace(line.PricingRule) 
    ? (IsQuoteServiceLine(line.Type) ? "PerRoom" : "PerTable")  // ← DEFAULT PerTable
    : line.PricingRule,
```

### File: `RestaurantViewModels.cs:6895`
```csharp
public class QuoteLineItem {
    public string PricingRule { get; set; } = "Manual";  // ← Class default "Manual"
}
```

## Hậu quả:

Tất cả món ăn thêm vào quote đều có `PricingRule = "PerTable"`.

- **PerTable**: mỗi bàn 1 phần → 5 bàn = 5 phần. KHÔNG scale theo số khách.
- **Kỳ vọng của user**: PerPerson → mỗi khách 1 phần → 47 khách = 47 phần.

---

# 5. ROOT CAUSE #2: ĐỔI SỐ KHÁCH KHÔNG ĐƯỢC GỌI

## Dòng code gây ra:

### File: `RestaurantViewModels.cs:21-22`
```csharp
private readonly IAlacarteRestaurantService _service;
private readonly IMenuCatalogService? _menuCatalog;
// KHÔNG CÓ IQuoteChangeService
```

`QuoteChangeService` có đầy đủ logic `ApplyChangeAsync` → `ApplyFullRebuildAsync` (phân phối lại món sau khi đổi số khách), nhưng **ViewModel không inject service này**.

## Hậu quả:

Không có UI path để gọi `ApplyChangeAsync`. User không thể đổi 40→47 từ giao diện.

---

# 6. TÓM TẮT: VÌ SAO UAT THẤY SAI

| UAT Finding | Service layer | Root cause | File:Dòng |
|---|---|---|---|
| "Bàn Tổng có món, bàn con không có" | **ĐÚNG** — service phân phối đúng nếu PricingRule đúng | `PricingRule` mặc định "PerTable" (chứ không phải "PerPerson") + UI fix trước đó dùng `SelectedOrder.Tables` để hiển thị đúng bàn | `RestaurantViewModels.cs:1135` |
| "Đơn mở hiển thị 0" | **ĐÚNG** — GetOpenOrdersAsync filter đúng | Đã fix — `BuildSelectedOrderRoomTables` giờ dùng `SelectedOrder.Tables` | — |
| "Đổi 40→47 không thay đổi" | **CÓ SERVICE** — QuoteChangeService đầy đủ | **KHÔNG GỌI** — ViewModel không inject `IQuoteChangeService` | `RestaurantViewModels.cs:21-22` |
| "Bàn cuối 7 khách không có món" | Phụ thuộc PricingRule | Nếu "PerTable": bàn 7 khách nhận 1 phần. Nếu "Manual": bàn 7 khách nhận 0 | `RestaurantViewModels.cs:1135` |

---

# 7. CÁCH SỬA NHỎ NHẤT

## Fix 1: Đổi PricingRule mặc định từ "PerTable" → "PerPerson"

### File: `RestaurantViewModels.cs:1135`
```csharp
// Trước:
PricingRule = "PerTable"

// Sau:
PricingRule = "PerPerson"
```

### File: `RestaurantViewModels.cs:5788`
```csharp
// Trước:
? (IsQuoteServiceLine(line.Type) ? "PerRoom" : "PerTable")

// Sau:
? (IsQuoteServiceLine(line.Type) ? "PerRoom" : "PerPerson")
```

**Tác động**: Tất cả món ăn mặc định là PerPerson (1 phần/khách). Tổng SL = GuestCount.

## Fix 2: Wire Change Request vào ViewModel (P0 — riêng)

### File: `RestaurantViewModels.cs:21-22`
Thêm:
```csharp
private readonly IQuoteChangeService _quoteChangeService;
```

Inject vào constructor, thêm command `RequestChangeCommand`, `ApplyChangeCommand`.

**Đây là P0 riêng, không làm trong lần này.**

---

# 8. KẾT LUẬN CUỐI CÙNG

1. **`TableAllocationEngine`**: **ĐÚNG** — tạo 10+10+10+10+7 cho 47 khách.
2. **`CreateOrderForQuoteAsync`**: **ĐÚNG** — PerPerson phân phối theo GuestCount từng bàn, PerTable phân phối 1/bàn.
3. **`AddOrderLine`**: **ĐÚNG** — `RestaurantOrderTableId` được set chính xác.
4. **`ApplyFullRebuildAsync`**: **ĐÚNG** — rebuild dùng cùng logic phân phối.
5. **`PricingRule` mặc định**: **SAI** — mặc định "PerTable" thay vì "PerPerson". Đây là lý do món không scale theo số khách.
6. **`IQuoteChangeService`**: **KHÔNG ĐƯỢC GỌI** — ViewModel không inject.
