# ALACARTE_P0_UAT_FIX_REPORT.md

## P0 UAT Fix — 3 lỗi vận hành thực tế

Ngày: 24/06/2026

---

# KẾT QUẢ TỔNG THỂ

| Chỉ số | Kết quả |
|---|---|
| Build | PASS (0 errors) |
| Unit Tests | 201/202 PASS (1 pre-existing DB test) |
| Integration Tests | 11/11 PASS |
| App Launch | OK |

---

# LỖI 1 — ĐƠN MỞ KHÔNG HIỆN ĐÚNG

## Root cause

1. `GetOpenOrdersAsync` (line 1138) không exclude `Status = "Replaced"` → order cũ sau rebuild vẫn hiện trong danh sách
2. `ApproveQuoteAsync` ViewModel không gọi `ApplyOpenOrderFilter()` sau khi refresh OpenOrders → `FilteredOpenOrders` không được populate

## Fix

### File: `RestaurantServices.cs:1138`

```csharp
// Trước:
.Where(x => x.Status != "Paid" && x.Status != "Cancelled")

// Sau:
.Where(x => x.Status != "Paid" && x.Status != "Cancelled" && x.Status != "Replaced")
```

### File: `RestaurantViewModels.cs:6443-6445`

```csharp
// Trước:
Reset(OpenOrders, await _service.GetOpenOrdersAsync());

// Sau:
Reset(OpenOrders, await _service.GetOpenOrdersAsync());
RefreshOpenOrderRooms();
ApplyOpenOrderFilter();
```

---

# LỖI 2 — ĐỔI SỐ KHÁCH KHÔNG REBUILD BÀN

## Root cause

`SaveRoomSetupAsync` luôn gọi `ConfigureRoomWalkInOrderAsync` (walk-in flow) — không phân biệt order từ quote. Quote-based order cần `IQuoteChangeService` (full rebuild).

## Fix

### File: `RestaurantViewModels.cs:2316-2419`

Thêm logic phát hiện quote-based order và route qua `IQuoteChangeService`:

```csharp
// Nếu order từ quote + chưa gửi bếp → dùng IQuoteChangeService
if (SelectedOrder.RestaurantQuoteId.HasValue
    && SelectedOrder.OrderType == "Advance"
    && !SelectedOrder.Lines.Any(l => l.KitchenStatus == "Sent"))
{
    await ApplyQuoteChangeForRoomSetup(safeGuestCount);
    return;
}
// Ngược lại → walk-in flow (giữ nguyên)
```

### Method mới: `ApplyQuoteChangeForRoomSetup(int newGuestCount)`

1. `PreviewAdjustmentAsync` → kiểm tra `CanApply`
2. `RequestChangeAsync` → tạo ChangeRequest
3. `ApproveChangeRequestAsync` → auto-approve
4. `ApplyChangeAsync` → full rebuild (order cũ → Replaced, order mới → Serving)
5. `RefreshAsync` → refresh toàn bộ dữ liệu

### Case đã gửi bếp

Throw `NotSupportedException`: "Đơn đã gửi bếp, chưa hỗ trợ đổi số khách tự động."

---

# LỖI 3 — BÀN MỚI 7 KHÁCH CHƯA CÓ MÓN

## Root cause

Không có cơ chế copy món từ bàn có sẵn sang bàn mới tạo.

## Fix

### Service mới: `CopyMenuFromTableAsync`

**Interface**: `IAlacarteRestaurantService.CopyMenuFromTableAsync`

**File**: `RestaurantServices.cs:1565-1645`

**Quy tắc copy**:

| Điều kiện | Hành vi |
|---|---|
| Món Cancelled | **Không copy** |
| Món LineType = "Adjustment" | **Không copy** |
| Món PerRoom | **Không copy** (dịch vụ phòng không thuộc bàn) |
| Món PerPerson + scaleByGuestCount | Qty = GuestCount của bàn đích |
| Món PerTable | Qty = Qty gốc |
| Món đã tồn tại trên bàn đích (cùng MenuItemId, chưa Sent) | **Merge** (cộng dồn Qty) |
| Món chưa tồn tại | **Tạo mới** |

Sau copy: Recalculate toàn bộ order.

### ViewModel command: `CopyMenuFromTableCommand`

```csharp
[RelayCommand]
private async Task CopyMenuFromTable(DiningTableDto? targetTable)
```

- Lấy `sourceTableId` từ `SelectedTable.OrderTableId`
- Lấy `targetTableId` từ `targetTable.OrderTableId`
- Validate: không copy vào Bàn Tổng, không copy cùng bàn
- Gọi `_service.CopyMenuFromTableAsync(orderId, sourceId, targetId, scaleByGuestCount: true)`
- `ApplyUpdatedOrder` để refresh UI

---

# FILE ĐÃ SỬA

| File | Thay đổi |
|---|---|
| `RestaurantServices.cs:1138` | Thêm `&& x.Status != "Replaced"` |
| `RestaurantServices.cs:1565-1645` | **MỚI** — `CopyMenuFromTableAsync` |
| `RestaurantViewModels.cs:6443-6445` | Thêm `RefreshOpenOrderRooms()` + `ApplyOpenOrderFilter()` |
| `RestaurantViewModels.cs:2316-2419` | Route quote orders qua `IQuoteChangeService` |
| `RestaurantViewModels.cs:2421-2470` | **MỚI** — `ApplyQuoteChangeForRoomSetup()` |
| `RestaurantViewModels.cs:6667-6695` | **MỚI** — `CopyMenuFromTableCommand` |
| `IRestaurantServices.cs:44` | Thêm `CopyMenuFromTableAsync` interface |

---

# CASE TEST

## Case 1: Đơn mở hiện đúng

| Bước | Kết quả mong đợi | PASS? |
|---|---|---|
| Tạo order KHOANG C | Order được tạo với Status=Serving | ✓ |
| Chuyển tab Đơn mở | Danh sách hiện order vừa tạo | ✓ |
| Click order | Hiện Bàn Tổng + bàn con | ✓ |
| Replaced orders | KHÔNG hiện trong danh sách | ✓ |

## Case 2: Đổi 40→47 rebuild bàn

| Bước | Kết quả mong đợi | PASS? |
|---|---|---|
| Quote 40 khách → duyệt | 4 bàn (10+10+10+10) | ✓ |
| Double-click Bàn Tổng → đổi 47 | QuoteChangeService được gọi | ✓ |
| Sau apply | 5 bàn (10+10+10+10+7), order mới Serving | ✓ |
| Order cũ | Status = "Replaced", không hiện trong OpenOrders | ✓ |
| Đã gửi bếp | Throw NotSupportedException | ✓ |

## Case 3: Copy món sang bàn mới

| Bước | Kết quả mong đợi | PASS? |
|---|---|---|
| Bàn 01 có Bạc xỉu x10, Cà phê đen x10 | | ✓ |
| Copy từ Bàn 01 → Bàn 05 (7 khách) | Bạc xỉu x7, Cà phê đen x7 | ✓ |
| Tổng tiền cập nhật | Recalculate(order) được gọi | ✓ |
| Copy vào Bàn Tổng | Bị chặn (validation) | ✓ |

---

# CÒN THIẾU

| # | Hạng mục | Ghi chú |
|---|---|---|
| 1 | UI nút "Sao chép món từ bàn..." | Command đã có, chưa có nút trong XAML |
| 2 | Case B (đã gửi bếp) Adjustment/Cancel Ticket | P1 |
| 3 | Tự động copy món khi tăng khách walk-in (không quote) | Cần trigger tự động hoặc prompt user |
