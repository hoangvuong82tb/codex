# ALACARTE_PRICING_RULE_ARCHITECTURE.md

## Kiến trúc PricingRule — Thiết kế chuẩn cho À La Carte VIP

Ngày: 24/06/2026

---

# 1. HIỆN TRẠNG: PricingRule TRONG TOÀN BỘ STACK

## Sơ đồ dòng chảy PricingRule

```
┌─────────────────────────────────────────────────────────────────┐
│ LỚP ENTITY                                                      │
│                                                                 │
│ MenuItem              ← KHÔNG có PricingRule (đúng)             │
│ BanquetMenuPackage    ← PricingMode = "PerTable" (line 439)    │
│ BanquetMenuPkgLine    ← PricingRule = null (line 455)          │
│ RestaurantQuoteLine   ← PricingRule = "Manual" (line 270)      │
│ AlacarteOrderLine     ← PricingRule = "Manual" (line 128)      │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│ LỚP VIEWMODEL                                                   │
│                                                                 │
│ QuoteLineItem         ← PricingRule = "Manual" (line 7054)     │
│ AddMenuItemToQuote    ← hardcode "PerTable" (line 1135) ← BUG  │
│ AddMenuPackageToQuote ← dùng line.PricingRule ?? package.Mode  │
│ SaveQuoteAsync        ← default "PerTable" nếu empty (5788)    │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│ LỚP SERVICE                                                     │
│                                                                 │
│ NormalizePricingRule  ← "PerPerson"/"PerTable"/"PerRoom"/Manual│
│ MenuCalculationEngine ← tính SL dựa trên rule                  │
│ CreateOrderForQuote   ← phân phối line theo rule               │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Kết luận hiện trạng

| Câu hỏi | Trả lời |
|---|---|
| MenuItem có PricingRule không? | **KHÔNG** — và đây là ĐÚNG. PricingRule thuộc về Quote, không phải MenuItem |
| BanquetMenuPackageLine có PricingRule? | **CÓ** — `string?` (nullable), fallback về `PricingMode` của Package |
| AddMenuItemToQuote hardcode? | **CÓ** — line 1135 hardcode `"PerTable"` |
| SaveQuoteAsync default? | **CÓ** — line 5788 default `"PerTable"` nếu PricingRule rỗng |

---

# 2. VÌ SAO Hardcode "PerTable" LÀ SAI

## Code hiện tại

### File: `RestaurantViewModels.cs:1135`
```csharp
// AddMenuItemToQuote — khi user click "+" để thêm món vào quote
QuoteLines.Add(new QuoteLineItem { 
    PricingRule = "PerTable"   // ← TẠI SAO?
});
```

### File: `RestaurantViewModels.cs:5788`
```csharp
// SaveQuoteAsync — khi lưu quote
PricingRule = string.IsNullOrWhiteSpace(line.PricingRule) 
    ? (IsQuoteServiceLine(line.Type) ? "PerRoom" : "PerTable")  // ← TẠI SAO?
    : line.PricingRule,
```

## Hậu quả

| PricingRule | Hành vi | Phù hợp cho |
|---|---|---|
| `"PerPerson"` | 1 phần/khách. 47 khách = 47 phần | Món chính (tôm hùm, bào ngư, steak) |
| `"PerTable"` | 1 phần/bàn. 5 bàn = 5 phần | Món chia sẻ (salad, canh, lẩu) |
| `"PerRoom"` | 1 phần/phòng. 1 phòng = 1 phần | Dịch vụ (MC, âm thanh) |

Hardcode `"PerTable"` cho mọi món → **sai với món PerPerson**.

Hardcode `"PerPerson"` cho mọi món → **sai với món PerTable**.

**Không có default nào đúng cho tất cả.** Default phải đến từ ngữ cảnh.

---

# 3. THIẾT KẾ ĐÚNG: PricingRule THEO TỪNG NGỮ CẢNH

## Nguyên tắc

> **PricingRule là thuộc tính của QuoteLine, không phải MenuItem.**
> 
> Mỗi lần một món được thêm vào quote, PricingRule phải được XÁC ĐỊNH từ ngữ cảnh, không được hardcode.

## 3 nguồn xác định PricingRule

```
┌──────────────────────────────────────────────────────────────────┐
│ NGUỒN 1: BANQUET MENU PACKAGE (Suất có sẵn)                     │
│                                                                  │
│ BanquetMenuPackageLine.PricingRule                               │
│   → nếu null → BanquetMenuPackage.PricingMode                   │
│   → nếu vẫn null → hiển thị dropdown cho user chọn              │
│                                                                  │
│ ĐÃ ĐÚNG trong AddMenuPackageToQuote (line 1173):                │
│   var pricingRule = string.IsNullOrWhiteSpace(line.PricingRule)  │
│       ? package.PricingMode                                      │
│       : line.PricingRule;                                        │
│                                                                  │
├──────────────────────────────────────────────────────────────────┤
│ NGUỒN 2: USER CHỌN THỦ CÔNG (Thêm món lẻ)                       │
│                                                                  │
│ User đang có dropdown QuotePricingRules (line 37):               │
│   ["PerPerson", "PerTable", "PerRoom", "Manual"]                │
│                                                                  │
│ Nhưng AddMenuItemToQuote (line 1135) KHÔNG dùng dropdown này.   │
│ Nó hardcode "PerTable".                                          │
│                                                                  │
│ SỬA: AddMenuItemToQuote phải dùng giá trị từ dropdown,           │
│ hoặc mặc định "PerPerson" (phổ biến nhất cho À La Carte VIP).   │
│                                                                  │
├──────────────────────────────────────────────────────────────────┤
│ NGUỒN 3: DỊCH VỤ PHÒNG (Luôn PerRoom)                           │
│                                                                  │
│ AddServiceToQuote (line 1146):                                   │
│   PricingRule = "PerRoom"                                        │
│                                                                  │
│ ĐÃ ĐÚNG — dịch vụ luôn PerRoom.                                 │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

# 4. MÔ HÌNH DỮ LIỆU CHUẨN

## PricingRule nên lưu ở đâu?

| Vị trí | Có nên lưu? | Lý do |
|---|---|---|
| **MenuItem** | **KHÔNG** | Cùng 1 món có thể dùng PerPerson cho tiệc này, PerTable cho tiệc khác. PricingRule là QUYẾT ĐỊNH CỦA QUOTE, không phải thuộc tính của món |
| **BanquetMenuPackage** | **CÓ** — `PricingMode` | Fallback cho toàn bộ package. "Gói VIP B mặc định tính theo PerPerson" |
| **BanquetMenuPackageLine** | **CÓ** — `PricingRule` (nullable) | Override cho từng món trong package. "Trong gói VIP B, tôm hùm tính PerPerson, salad tính PerTable" |
| **RestaurantQuoteLine** | **CÓ** — `PricingRule` | **ĐÂY LÀ NƠI LƯU CHÍNH THỨC**. Mọi quyết định pricing được lưu ở quote line |
| **AlacarteOrderLine** | **CÓ** — `PricingRule` | Copy từ QuoteLine khi duyệt. Dùng để trace ngược |
| **QuoteLineItem (ViewModel)** | **CÓ** — `PricingRule` | ViewModel state trước khi lưu |

## Sơ đồ quan hệ

```
MenuItem (không có PricingRule)
  │
  │ được thêm vào
  ▼
QuoteLineItem (ViewModel)
  │  PricingRule = từ 1 trong 3 nguồn:
  │    1. Package → PackageLine.PricingRule ?? Package.PricingMode
  │    2. User chọn từ dropdown [PerPerson, PerTable, PerRoom, Manual]
  │    3. Dịch vụ → luôn PerRoom
  │
  │ lưu xuống
  ▼
RestaurantQuoteLine (Entity)
  │  PricingRule = normalized value
  │
  │ duyệt quote
  ▼
AlacarteOrderLine (Entity)
  │  PricingRule = copy từ QuoteLine
  │  RestaurantOrderTableId = phân phối theo rule
  │    PerPerson → mỗi bàn × GuestCount
  │    PerTable  → mỗi bàn × 1
  │    PerRoom   → Bàn Tổng
```

---

# 5. ĐỀ XUẤT SỬA CỤ THỂ (không code, chỉ thiết kế)

## Vấn đề 1: AddMenuItemToQuote hardcode "PerTable"

### Vị trí: `RestaurantViewModels.cs:1135`

### Thiết kế sửa:

Khi user thêm món lẻ vào quote, PricingRule phải được lấy từ:
1. Giá trị user đã chọn trong dropdown `QuotePricingRules` (nếu có UI dropdown đang active)
2. Hoặc mặc định `"PerPerson"` cho À La Carte VIP (vì đây là trường hợp phổ biến nhất)
3. Hoặc hiển thị prompt cho user chọn rule trước khi thêm

**Khuyến nghị**: Thêm `[ObservableProperty] private string selectedQuoteDefaultPricingRule = "PerPerson"` và dùng nó thay cho "PerTable".

## Vấn đề 2: SaveQuoteAsync default "PerTable"

### Vị trí: `RestaurantViewModels.cs:5788`

### Thiết kế sửa:

Không nên có default ở bước save. Nếu `PricingRule` rỗng khi save, nghĩa là có bug ở bước thêm món. Nên throw hoặc log warning thay vì âm thầm gán "PerTable".

Hoặc ít nhất: đổi default từ "PerTable" → "Manual" để không làm sai lệch phân phối.

---

# 6. BẢNG TỔNG HỢP: ĐÚNG/SAI/MISSING

| Thành phần | Trạng thái | Mô tả |
|---|---|---|
| `MenuItem.PricingRule` | **KHÔNG CÓ — ĐÚNG** | PricingRule thuộc Quote, không phải MenuItem |
| `BanquetMenuPackage.PricingMode` | **CÓ — ĐÚNG** | Package-level fallback |
| `BanquetMenuPackageLine.PricingRule` | **CÓ — ĐÚNG** | Line-level override, nullable |
| `RestaurantQuoteLine.PricingRule` | **CÓ — ĐÚNG** | Nơi lưu chính thức |
| `AlacarteOrderLine.PricingRule` | **CÓ — ĐÚNG** | Copy để trace |
| `AddMenuPackageToQuote` | **ĐÚNG** | Ưu tiên line rule → package mode |
| `AddMenuItemToQuote` | **SAI** | Hardcode "PerTable" thay vì cho user chọn |
| `SaveQuoteAsync default` | **SAI** | Default "PerTable" khi PricingRule rỗng |
| `NormalizePricingRule` | **ĐÚNG** | Chuẩn hóa 4 giá trị, fallback Manual |
| `MenuCalculationEngine.Calculate` | **ĐÚNG** | Tính SL đúng theo từng rule |
| `CreateOrderForQuoteAsync` | **ĐÚNG** | Phân phối line đúng theo từng rule |
| `QuoteChangeService.ApplyFullRebuildAsync` | **ĐÚNG** | Dùng lại cùng logic phân phối |

---

# 7. KẾT LUẬN

1. **PricingRule đã được lưu đúng chỗ**: `RestaurantQuoteLine` và `AlacarteOrderLine`. Không cần thêm entity hay field mới.

2. **Vấn đề duy nhất**: 2 chỗ hardcode `"PerTable"` trong ViewModel. Không cần sửa service, không cần migration, không cần entity mới.

3. **Fix tối thiểu**: Cho phép user chọn PricingRule khi thêm món lẻ, thay vì hardcode "PerTable". Dropdown `QuotePricingRules` đã có sẵn nhưng không được dùng.
