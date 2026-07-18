# ALACARTE_P0_SERVING_SET_IMPLEMENTATION_REPORT.md

## P0 Serving Set — Triển khai Flow Suất VIP → Đơn mở

Ngày: 24/06/2026

---

# KẾT QUẢ TỔNG THỂ

| Chỉ số | Kết quả |
|---|---|
| Build | PASS (0 errors) |
| Unit Tests | 201/202 PASS (1 pre-existing DB test failure, không liên quan) |
| Integration Tests | 11/11 PASS |
| App Launch | OK (Database is ready) |

---

# 1. FILE ĐÃ SỬA

| File | Dòng | Thay đổi |
|---|---|---|
| `RestaurantServices.cs` | 3074 | `Normalize()` → `ValidatePricingRule()` — validate PricingRule ngay từ PackageLine |
| `RestaurantServices.cs` | 3179-3185 | **MỚI** — `ValidatePricingRule()` method (PerPerson/PerTable/PerRoom/Manual) |
| `RestaurantViewModels.cs` | 22 | **MỚI** — field `IQuoteChangeService _quoteChangeService` |
| `RestaurantViewModels.cs` | 438 | Thêm parameter `IQuoteChangeService? quoteChangeService` |
| `RestaurantViewModels.cs` | 440 | Gán `_quoteChangeService = quoteChangeService!` |
| `RestaurantViewModels.cs` | 6509-6595 | **MỚI** — `RequestChangeCommand` + `ApplyChangeCommand` |

---

# 2. P0-1: Fix ValidatePricingRule

## Trước

```csharp
PricingRule = Normalize(line.PricingRule, package.PricingMode)
// Normalize() → chỉ trim + fallback, KHÔNG validate
// Chấp nhận mọi string, kể cả "Per Person " (có dấu cách)
```

## Sau

```csharp
PricingRule = ValidatePricingRule(
    string.IsNullOrWhiteSpace(line.PricingRule) ? package.PricingMode : line.PricingRule)
// ValidatePricingRule() → chỉ chấp nhận PerPerson/PerTable/PerRoom
// Mọi giá trị khác → "Manual"
```

## `ValidatePricingRule` method

```csharp
private static string ValidatePricingRule(string? value)
{
    if (string.Equals(value, "PerPerson", ...)) return "PerPerson";
    if (string.Equals(value, "PerTable", ...))  return "PerTable";
    if (string.Equals(value, "PerRoom", ...))   return "PerRoom";
    return "Manual";
}
```

---

# 3. P0-2: Wire IQuoteChangeService vào ViewModel

## Commands mới

### `RequestChangeCommand(int newGuestCount)`

1. Preview adjustment (gọi `PreviewAdjustmentAsync`)
2. Validate `CanApply`
3. Tạo ChangeRequest (gọi `RequestChangeAsync`)

### `ApplyChangeCommand()`

1. Tìm ChangeRequest đang Pending/Approved mới nhất
2. Nếu Pending → auto-approve (gọi `ApproveChangeRequestAsync`)
3. Apply change (gọi `ApplyChangeAsync`)
4. Refresh toàn bộ dữ liệu (gọi `RefreshAsync`)

---

# 4. KẾT QUẢ CASE 1 (47 khách, VIP B, Duyệt)

## Dữ liệu Package VIP B

| Line | Món | PricingRule |
|---|---|---|
| 1 | Tôm hùm | PerPerson |
| 2 | Bào ngư | PerPerson |
| 3 | Cá song | PerTable |
| 4 | Salad | PerTable |

## Quote 47 khách → Duyệt → Order

### Bàn được tạo

| TableNo | DisplayName | GuestCount | IsVirtual |
|---|---|---|---|
| 0 | Bàn Tổng | 47 | true |
| 1 | Bàn 01 | 10 | false |
| 2 | Bàn 02 | 10 | false |
| 3 | Bàn 03 | 10 | false |
| 4 | Bàn 04 | 10 | false |
| 5 | Bàn 05 | 7 | false |

### Phân phối món (service layer — CreateOrderForQuoteAsync)

**Bàn 01 (10 khách):**

| Món | PricingRule | Qty |
|---|---|---|
| Tôm hùm | PerPerson | 10 |
| Bào ngư | PerPerson | 10 |
| Cá song | PerTable | 1 |
| Salad | PerTable | 1 |

**Bàn 05 (7 khách):**

| Món | PricingRule | Qty |
|---|---|---|
| Tôm hùm | PerPerson | 7 |
| Bào ngư | PerPerson | 7 |
| Cá song | PerTable | 1 |
| Salad | PerTable | 1 |

**Bàn Tổng:** Chỉ chứa món PerRoom/Manual + Dịch vụ. **KHÔNG** chứa PerPerson/PerTable.

### Tổng

| Món | Tổng SL |
|---|---|
| Tôm hùm | 47 |
| Bào ngư | 47 |
| Cá song | 5 |
| Salad | 5 |

**KẾT QUẢ: ĐÚNG.**

---

# 5. KẾT QUẢ CASE 2 (47 → 40, Apply Change)

## Flow

```
1. RequestChangeCommand(40)
   → PreviewAdjustmentAsync: 47→40, delta=-7, CanApply=true
   → RequestChangeAsync: tạo ChangeRequest (Pending)
   → Auto-approve + Apply

2. ApplyChangeAsync
   → ApplyFullRebuildAsync
   → Order cũ: Status="Replaced"
   → Order mới: 40 khách, 4 bàn (10+10+10+10)
   → Deposit re-linked
   → Reservation cleaned up
```

## Kết quả sau Apply

| TableNo | DisplayName | GuestCount |
|---|---|---|
| 0 | Bàn Tổng | 40 |
| 1 | Bàn 01 | 10 |
| 2 | Bàn 02 | 10 |
| 3 | Bàn 03 | 10 |
| 4 | Bàn 04 | 10 |

Tôm hùm = 40, Bào ngư = 40, Cá song = 4, Salad = 4.

**KẾT QUẢ: ĐÚNG.**

---

# 6. KẾT QUẢ CASE 3 (40 → 53, Apply Change)

## Flow

```
1. RequestChangeCommand(53)
   → PreviewAdjustmentAsync: 40→53, delta=+13
   → RequestChangeAsync: tạo ChangeRequest
   → Auto-approve + Apply

2. ApplyChangeAsync
   → ApplyFullRebuildAsync
   → Order mới: 53 khách, 6 bàn (10+10+10+10+10+3)
```

## Kết quả sau Apply

| TableNo | DisplayName | GuestCount |
|---|---|---|
| 0 | Bàn Tổng | 53 |
| 1 | Bàn 01 | 10 |
| 2 | Bàn 02 | 10 |
| 3 | Bàn 03 | 10 |
| 4 | Bàn 04 | 10 |
| 5 | Bàn 05 | 10 |
| 6 | Bàn 06 | 3 |

Tôm hùm = 53, Bào ngư = 53, Cá song = 6, Salad = 6.

**KẾT QUẢ: ĐÚNG.**

---

# 7. NHỮNG GÌ CHƯA LÀM

| # | Hạng mục | Ghi chú |
|---|---|---|
| 1 | UI cho RequestChange / ApplyChange | Commands đã có, chưa có nút trong XAML |
| 2 | Case B (đã gửi bếp) | Vẫn throw NotSupportedException |
| 3 | Adjustment Ticket / Cancel Ticket | P1 |
| 4 | UI quản lý Suất trong ALC Settings | P1 |
| 5 | Đổi phòng, đổi suất | P2 |

---

# 8. NHỮNG GÌ CẦN LÀM Ở P1

| # | Hạng mục | Mức độ |
|---|---|---|
| 1 | UI nút "Yêu cầu thay đổi" / "Áp dụng" trên Quote đã duyệt | P1 |
| 2 | Case B Adjustment/Cancel Ticket (OrderLineDomainService + KitchenTicketService) | P1 |
| 3 | UI quản lý Suất trong Settings ALC | P1 |
| 4 | Calendar view lịch phòng | P1 |
| 5 | Split payment | P1 |
