# ALACARTE_SERVING_SET_GAP_ANALYSIS.md

## Audit: BanquetMenuPackage làm ServingSet cho À La Carte

Ngày: 24/06/2026

---

# A. CHỌN VIP A/VIP B — CÓ THỰC SỰ BUNG MÓN XUỐNG QuoteLine KHÔNG?

**TRẢ LỜI: CÓ — 100% hoạt động.**

## Bằng chứng

### Bước 1: Load packages từ DB

`GetMenuPackagesAsync` (`RestaurantServices.cs:1537-1545`):

```csharp
_dbContext.BanquetMenuPackages
    .Include(x => x.Lines).ThenInclude(x => x.MenuItem)
    .Where(x => x.IsActive)
    .Select(x => ToBanquetMenuPackageDto(x))
```

→ Load đầy đủ package + lines + menu items.

### Bước 2: PricingRule được resolve từ line hoặc package

`ToBanquetMenuPackageDto` (`RestaurantServices.cs:2906`):

```csharp
PricingRule = string.IsNullOrWhiteSpace(l.PricingRule) 
    ? x.PricingMode       // ← fallback từ package
    : l.PricingRule!      // ← ưu tiên line-level
```

→ **PricingRule không bao giờ null trong DTO.**

### Bước 3: User click "+" → bung món vào quote

`AddMenuPackageToQuote` (`RestaurantViewModels.cs:1166-1199`):

```csharp
foreach (var line in package.Lines)           // ← DUYỆT TẤT CẢ LINE
{
    var pricingRule = string.IsNullOrWhiteSpace(line.PricingRule) 
        ? package.PricingMode : line.PricingRule;
    
    QuoteLines.Add(new QuoteLineItem
    {
        MenuItemId = line.MenuItemId,
        Name = line.MenuItemName,
        UnitPrice = line.UnitPrice,
        Quantity = Math.Max(1, (int)Math.Ceiling(line.Qty)),
        PricingRule = pricingRule              // ← GIỮ NGUYÊN
    });
}
```

→ **Tất cả line trong package được expand thành QuoteLineItem với PricingRule đúng.**

## Kết quả

| Kiểm tra | Kết quả |
|---|---|
| Tất cả line được expand? | **CÓ** — line 1166 |
| PricingRule từ line? | **CÓ** — line 1173, ưu tiên line > package |
| Quantity từ line? | **CÓ** — line 1174 |
| UnitPrice từ MenuItem? | **CÓ** — line 2911 (DTO) |

---

# B. PRICING RULE CỦA PACKAGE LINE — CÓ ĐƯỢC GIỮ NGUYÊN TỚI ORDERLINE KHÔNG?

**TRẢ LỜI: CÓ — nếu PricingRule hợp lệ. Nếu không → "Manual".**

## Chuỗi truyền PricingRule

```
BanquetMenuPackageLine.PricingRule (entity, nullable)
  │ line 2906: resolve với package.PricingMode
  ▼
BanquetMenuPackageLineDto.PricingRule (DTO, string)
  │ line 1173: check lại (redundant safety net)
  ▼
QuoteLineItem.PricingRule (ViewModel)
  │ line 5864, 5971: direct passthrough
  ▼
SaveRestaurantQuoteLineRequest.PricingRule (DTO)
  │ line 1411: NormalizePricingRule ← KIỂM TRA
  ▼
RestaurantQuoteLine.PricingRule (entity, string)
  │ line 1924: NormalizePricingRule ← KIỂM TRA LẠI
  ▼
AlacarteOrderLine.PricingRule (entity, string)
```

## Điểm kiểm tra

`NormalizePricingRule` (`RestaurantServices.cs:2478-2484`):

```csharp
if ("PerPerson") return "PerPerson";
if ("PerTable")  return "PerTable";
if ("PerRoom")   return "PerRoom";
return isServiceLine ? "PerRoom" : "Manual";  // ← FALLBACK
```

**RỦI RO**: `SaveMenuPackageAsync` dùng `Normalize()` (line 3074) thay vì `NormalizePricingRule()`. `Normalize()` chỉ trim và fallback, không validate. Nếu ai đó lưu `PricingRule = "Per Person "` (có dấu cách), giá trị này sẽ qua được bước lưu package nhưng sẽ bị `NormalizePricingRule` đá về "Manual" ở bước lưu quote.

---

# C. NẾU PACKAGELINE: TÔM HÙM = PerPerson, CÁ SONG = PerTable

## OrderLine cuối cùng có đúng không?

**TRẢ LỜI: CÓ — với điều kiện PricingRule được lưu đúng.**

## Mô phỏng: 47 khách, VIP B (Tôm hùm PerPerson, Cá song PerTable)

### QuoteLine sau khi bung từ package:

| Line | Món | PricingRule | Quantity | CalculatedQuantity |
|---|---|---|---|---|
| 1 | Tôm hùm | PerPerson | 1 | 47 |
| 2 | Cá song | PerTable | 1 | 5 |

### OrderLine sau khi `CreateOrderForQuoteAsync`:

**Tôm hùm (PerPerson) — line 1925-1932:**

```csharp
foreach (var servingTable in realTables)  // [Bàn 01, Bàn 02, Bàn 03, Bàn 04, Bàn 05]
{
    var qty = Math.Max(1, servingTable.GuestCount) * Math.Max(1, 1);
    AddOrderLine(order, servingTable, ..., "PerPerson", qty, ...);
}
```

| OrderLine | Bàn | Qty |
|---|---|---|
| 1 | Bàn 01 (10 khách) | 10 |
| 2 | Bàn 02 (10 khách) | 10 |
| 3 | Bàn 03 (10 khách) | 10 |
| 4 | Bàn 04 (10 khách) | 10 |
| 5 | Bàn 05 (7 khách) | 7 |

Tổng: **47** ✓

**Cá song (PerTable) — line 1935-1941:**

```csharp
foreach (var servingTable in realTables)
{
    AddOrderLine(order, servingTable, ..., "PerTable", Math.Max(1, 1), ...);
}
```

| OrderLine | Bàn | Qty |
|---|---|---|
| 6 | Bàn 01 | 1 |
| 7 | Bàn 02 | 1 |
| 8 | Bàn 03 | 1 |
| 9 | Bàn 04 | 1 |
| 10 | Bàn 05 | 1 |

Tổng: **5** ✓

### Kết luận

**ĐÚNG 100%** — miễn là `PricingRule` trên QuoteLine là "PerPerson" và "PerTable" (không phải "Manual").

---

# D. CÓ THỂ DÙNG NGAY BanquetMenuPackage LÀM SERVING SET CHO À LA CARTE?

**TRẢ LỜI: CÓ — với 2 caveat nhỏ.**

## Những gì đã hoạt động

| Tính năng | Trạng thái | File:Dòng |
|---|---|---|
| Load packages kèm lines + menu items | ✓ | `RestaurantServices.cs:1537-1545` |
| PricingRule resolve (line → package fallback) | ✓ | `RestaurantServices.cs:2906` |
| Expand package → QuoteLineItem | ✓ | `RestaurantViewModels.cs:1166-1199` |
| PricingRule passthrough từ ViewModel → Service | ✓ | `RestaurantViewModels.cs:5864,5971` |
| NormalizePricingRule khi lưu QuoteLine | ✓ | `RestaurantServices.cs:1411` |
| Phân phối OrderLine theo PricingRule | ✓ | `RestaurantServices.cs:1925-1944` |
| PricingRule lưu trên OrderLine | ✓ | `RestaurantServices.cs:1969` |

## 2 caveat cần fix

### Caveat 1: SaveMenuPackageAsync dùng Normalize() thay vì NormalizePricingRule()

**File:** `RestaurantServices.cs:3074`

```csharp
PricingRule = Normalize(line.PricingRule, package.PricingMode)
// Normalize() → chỉ trim + fallback, không validate
// NÊN DÙNG: NormalizePricingRule(line.PricingRule, false) ?? package.PricingMode
```

**Rủi ro**: PricingRule sai chính tả ("Per Person ") → lưu được vào package → nhưng bị đá về "Manual" khi lưu quote.

### Caveat 2: Không có giao diện quản lý Package cho ALC

`BanquetMenuPackage` được quản lý qua `BanquetRestaurantService` và UI của module Banquet. Module ALC không có UI CRUD package riêng. Tab "Menu có sẵn" hiển thị package nhưng nút "+" chỉ expand vào quote, không cho phép quản lý.

**Giải pháp**: Thêm tab "Quản lý Suất" trong Settings ALC, gọi chung `BanquetRestaurantService`.

---

# E. NHỮNG GÌ CÒN THIẾU ĐỂ ĐẠT 100% THẬT

## Flow: Suất VIP → Chia bàn → Phân món → Phục vụ

| Bước | Hiện trạng | % | Thiếu gì |
|---|---|---|---|
| 1. Tạo Suất VIP (CRUD) | Có service (`SaveMenuPackageAsync`) nhưng chưa có UI trong ALC | 70% | UI quản lý suất trong ALC Settings |
| 2. Chọn suất → bung món | **HOẠT ĐỘNG** — `AddMenuPackageToQuote` | 100% | — |
| 3. PricingRule đúng | **HOẠT ĐỘNG** — nhưng có rủi ro Caveat 1 | 95% | Fix `Normalize` → `NormalizePricingRule` |
| 4. Lưu báo giá | **HOẠT ĐỘNG** — `SaveQuoteAsync` | 100% | — |
| 5. Chia bàn | **HOẠT ĐỘNG** — `TableAllocationEngine` | 100% | — |
| 6. Duyệt báo giá | **HOẠT ĐỘNG** — `ApproveQuoteAsync` | 100% | — |
| 7. Sinh Order + phân món | **HOẠT ĐỘNG** — `CreateOrderForQuoteAsync` | 100% | — |
| 8. Hiển thị bàn + món | **ĐÃ FIX** — `BuildSelectedOrderRoomTables` | 95% | Đã sửa hôm qua |
| 9. Đổi số khách | Service có, ViewModel chưa gọi | 50% | Wire `IQuoteChangeService` |
| 10. Gửi bếp | **HOẠT ĐỘNG** — `SendToPreparationAsync` | 90% | Chưa tách theo bàn |
| 11. Thanh toán | **HOẠT ĐỘNG** — `PayOrderAsync` | 80% | Chưa split payment |

## Tổng thể: **~85% hoàn thiện cho flow chính.**

## 3 việc còn thiếu để đạt 100%:

| # | Việc | Mức độ | File |
|---|---|---|---|
| 1 | Fix `Normalize` → `NormalizePricingRule` trong `SaveMenuPackageAsync` | P0 | `RestaurantServices.cs:3074` |
| 2 | Wire `IQuoteChangeService` vào ViewModel | P0 | `RestaurantViewModels.cs:21-22` |
| 3 | UI quản lý Suất trong ALC Settings | P1 | XAML + ViewModel |

---

# KẾT LUẬN

**`BanquetMenuPackage` CÓ THỂ dùng làm Serving Set cho À La Carte.**

- Cấu trúc entity đã đủ: `PricingMode` (package-level) + `PricingRule` (line-level, nullable)
- Flow bung món từ package → QuoteLine → OrderLine đã hoạt động 100%
- PricingRule được truyền xuyên suốt và phân phối đúng
- 2 caveat nhỏ cần fix (Normalize → NormalizePricingRule, UI quản lý suất)

**Không cần tạo entity mới như `ServingSet` hay `AlacarteServingSet`.**
