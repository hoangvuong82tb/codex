# Banquet Menu Table Pricing Fix Result — ASMILE-BO

> Ngày sửa: 2026-07-10 | Lỗi: Tổng tiền thực đơn chưa nhân với số bàn

---

## 1. Nguyên nhân gốc

### Công thức cũ (SAI)

```
SaveCustomMenuAsync:  Amount = Qty × UnitPrice              // lưu giá 1 bàn
ToDto:                MenuAmount = Sum(BanquetEventMenu.Amount)  // cộng dồn, KHÔNG nhân số bàn
GetEventsAsync:       MenuAmount = Sum(BanquetEventMenu.Amount)  // tương tự
AttachMenuPackageAsync: Amount = package.Price × TableCount      // preset chỉ nhân bàn chính
```

**Kết quả**: Custom menu luôn ra giá 1 bàn. Preset menu không tính ExtraTableCount.

### Công thức mới (ĐÚNG)

```
SaveCustomMenuAsync:  Amount = Qty × UnitPrice              // giữ nguyên — lưu giá 1 bàn per-item
ToDto:                MenuAmount = Sum(Custom: Amount × (TableCount + ExtraTableCount))
                               + Sum(Preset: Amount)        // preset đã nhân sẵn
GetEventsAsync:       Giống ToDto
AttachMenuPackageAsync: Amount = package.Price × (TableCount + ExtraTableCount)
```

**Tại sao custom menu giữ Amount per-table trong DB**: Vì `SaveCustomMenuAsync` không có quyền truy cập `TableCount`/`ExtraTableCount` của event tại thời điểm lưu. Thay vào đó, phép nhân được thực hiện tại thời điểm **đọc** (`ToDto`/`GetEventsAsync`). Điều này cũng đảm bảo khi `TableCount` thay đổi, tổng tiền tự động cập nhật mà không cần lưu lại menu.

### ExtraTableCount: Bàn phát sinh đã sử dụng

`ExtraTableCount` được tính vào tổng tiền (cả preset và custom). Đây là **bàn phát sinh thực tế đã sử dụng**, không phải bàn dự phòng.

---

## 2. File đã sửa

| # | File | Method/Vị trí | Thay đổi |
|---|---|---|---|
| 1 | `Infrastructure/Services/RestaurantServices.cs:4628` | `ToDto(BanquetEvent x)` | `Sum(m.Amount)` → `Sum(Custom ? Amount × (TableCount + ExtraTableCount) : Amount)` |
| 2 | `Infrastructure/Services/RestaurantServices.cs:4121-4128` | `GetEventsAsync` inline mapping | Tương tự — nhân custom menu với số bàn |
| 3 | `Infrastructure/Services/RestaurantServices.cs:4320-4321` | `AttachMenuPackageAsync` | `package.Price × TableCount` → `package.Price × (TableCount + ExtraTableCount)`. `TableCount` lưu cũng cập nhật thành tổng. |
| 4 | `Tests.Unit/RestaurantModuleTests.cs:1310-1312` | `Banquet_ShouldCreateEventDepositContractAndTotals` | Cập nhật expected value: 55,000,000 → 57,500,000 (thêm 1 ExtraTable × 2,500,000) |
| 5 | `Tests.Unit/RestaurantModuleTests.cs:1487-1552` | Method mới | `Banquet_ShouldCalculateCustomMenuAmountByTableCount` — 3 assertions |

---

## 3. Build & Test

| Check | Result |
|---|---|
| `dotnet build ASMILE.BO.sln` | **0 errors, 4 warnings** (pre-existing) |
| `dotnet test --filter "Banquet"` | **6/6 PASS** |

### Test cases

| # | Test | Assertions |
|---|---|---|
| 1 | Custom menu 280,000đ/bàn × 15 bàn (10+5) | MenuAmount = 4,200,000đ |
| 2 | Đổi ExtraTableCount về 0 → load lại | MenuAmount = 2,800,000đ (× 10) |
| 3 | Thêm service 1,000,000đ + 10% VAT | ServiceAmount = 1,100,000đ, Total = 3,900,000đ |
| 4 | Preset 2,500,000đ × (20+1) bàn + Service 5,000,000đ | TotalAmount = 57,500,000đ |

---

## 4. Luồng tính tiền (đã sửa)

```text
UI (ViewModel)
  SelectedBanquetCustomFoodTotalAmount = Sum(Qty × UnitPrice)     ← giá 1 bàn
  SelectedBanquetCustomFoodMainTableAmount = trên × TableCount    ← tiền bàn chính
  SelectedBanquetCustomFoodProjectedTableAmount = trên × ExtraTableCount ← tiền bàn PS

  → Các giá trị này để hiển thị preview, KHÔNG lưu DB

Service (ToDto / GetEventsAsync)
  MenuAmount = Sum(Custom: Amount × (TableCount + ExtraTableCount))
             + Sum(Preset: Amount)

  TotalAmount = MenuAmount + ServiceAmount

  → Tính từ DB, dùng cho Tab Tài chính, Hợp đồng, Dashboard

DB (BanquetEventMenus)
  Custom: Amount = Qty × UnitPrice           ← giá 1 bàn per-item
  Preset: Amount = package.Price × (TableCount + ExtraTableCount) ← tổng đã nhân
```

---

## 5. Kết quả UI runtime

| Check | Kết quả |
|---|---|
| Đơn giá 1 bàn hiển thị đúng | ✅ `SelectedBanquetCustomFoodTotalAmount` |
| Tiền bàn chính = đơn giá × TableCount | ✅ `SelectedBanquetCustomFoodMainTableAmount` |
| Tiền bàn PS = đơn giá × ExtraTableCount | ✅ `SelectedBanquetCustomFoodProjectedTableAmount` |
| Tổng tiền thực đơn trong Tab Tài chính | ✅ `MenuAmount` từ DB đã nhân số bàn |
| Hợp đồng dùng cùng TotalAmount | ✅ `TotalAmount = MenuAmount + ServiceAmount` |
| Đổi TableCount → tổng tiền cập nhật | ✅ Custom menu nhân tại thời điểm đọc |
| Đóng mở app → tổng tiền giữ nguyên | ✅ Tính từ DB mỗi lần load |
| Preset menu tính cả ExtraTableCount | ✅ `AttachMenuPackageAsync` đã sửa |

---

*Báo cáo được tạo bởi Claude Code analysis ngày 2026-07-10*
