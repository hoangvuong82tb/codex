# CREATE RESERVATION STEP E — BINDING AND CLIPPING FIX REPORT

> **Ngày**: 2026-07-22
> **Kết luận**: STEP E FIX COMPLETE — 3 lỗi runtime đã sửa

---

## 1. ROOT CAUSE: `ComboBoxItem` xuất hiện trong summary

### Vấn đề
Runtime hiển thị `System.Windows.Controls.ComboBoxItem: VND` thay vì `VND`.

### Root cause
ComboBox dùng `SelectedValue="{Binding CurrencyValue}"` nhưng **không có `SelectedValuePath`**. Items là `ComboBoxItem` objects (không phải string), nên `SelectedValue` trả về đối tượng `ComboBoxItem`. Khi code-behind dùng giá trị này trong string interpolation (`$"{_roomRate} {_currency}"`), `ComboBoxItem.ToString()` trả về `System.Windows.Controls.ComboBoxItem: VND`.

### Fix
Thêm `SelectedValuePath="Content"` vào ComboBox Currency và Payment Method. WPF sẽ extract `Content` property của `ComboBoxItem` được chọn.

```xml
<!-- TRƯỚC -->
<ComboBox SelectedValue="{Binding CurrencyValue, ...}">
  <ComboBoxItem Content="VND"/><ComboBoxItem Content="USD"/>
</ComboBox>

<!-- SAU -->
<ComboBox SelectedValue="{Binding CurrencyValue, ...}" SelectedValuePath="Content">
  <ComboBoxItem Content="VND"/><ComboBoxItem Content="USD"/>
</ComboBox>
```

---

## 2. BINDING TRƯỚC VÀ SAU

### Currency

| | Trước | Sau |
|---|-------|-----|
| Binding | `SelectedValue="{Binding CurrencyValue}"` | `SelectedValue="{Binding CurrencyValue}" SelectedValuePath="Content"` |
| Kiểu dữ liệu VM | `string CurrencyValue` | `string CurrencyValue` (không đổi) |
| Giá trị nhận được | `ComboBoxItem` object → `.ToString()` → `"System.Windows.Controls.ComboBoxItem: VND"` | `"VND"` |

### Payment Method

| | Trước | Sau |
|---|-------|-----|
| Binding | `SelectedValue="{Binding PaymentMethodValue}"` | `SelectedValue="{Binding PaymentMethodValue}" SelectedValuePath="Content"` |
| Kiểu dữ liệu VM | `string PaymentMethodValue` | `string PaymentMethodValue` (không đổi) |
| Giá trị nhận được | `ComboBoxItem` object → `.ToString()` → `"System.Windows.Controls.ComboBoxItem: Tiền mặt"` | `"Tiền mặt"` |

### Room Rate Display

| | Trước | Sau |
|---|-------|-----|
| Code | `$"{_roomRate} {_currency}"` | `$"{GetRoomRate():N0} {_currency}"` |
| Ví dụ input | `"1250000"` | `"1250000"` |
| Hiển thị | `1250000 VND` | `1.250.000 VND` |

### Deposit Display

| | Trước | Sau |
|---|-------|-----|
| Code | `$"{_depositAmount} {_currency}"` | `$"{GetDeposit():N0} {_currency}"` |
| Hiển thị | `500000 VND` | `500.000 VND` |

---

## 3. KIỂU DỮ LIỆU VIEWMODEL

Tất cả property trong code-behind (`ReservationCreateWindow.xaml.cs`) dùng kiểu dữ liệu thuần:

| Property | Type | Ghi chú |
|----------|------|---------|
| `CurrencyValue` | `string` | Nhận từ `SelectedValuePath="Content"` |
| `PaymentMethodValue` | `string` | Nhận từ `SelectedValuePath="Content"` |
| `RoomRateInput` | `string` | Input thô từ TextBox |
| `RateCodeValue` | `string` | Bound to TextBox |
| `IsVatIncluded` | `bool` | Bound to RadioButton IsChecked |
| `VatLabel` | `string` | Computed: `"Đã gồm"` / `"Chưa gồm"` |
| `DepositAmountInput` | `string` | Input thô từ TextBox |
| `DiscountPercentInput` | `string` | Input thô từ TextBox |
| `DiscountAmountInput` | `string` | Input thô từ TextBox |
| `IsDepositCollected` | `bool` | Bound to CheckBox |

**Không có property nào chứa UI object** (ComboBoxItem, RadioButton, TextBox, v.v.).

---

## 4. ROOT CAUSE: VAT BỊ CHỒNG CHỮ

### Vấn đề
Hai RadioButton "Đã gồm" và "Chưa gồm" chồng lên nhau.

### Root cause
Grid VAT chỉ định nghĩa **3 cột** (0, 1, 2) nhưng RadioButton thứ hai dùng `Grid.Column="4"` — cột không tồn tại. WPF đặt element vào cột 0 khi column reference không hợp lệ, khiến cả hai RadioButton nằm đè lên nhau.

```xml
<!-- TRƯỚC — chỉ 3 cột, RadioButton ở column 4 (không tồn tại) -->
<Grid.ColumnDefinitions>
  <ColumnDefinition Width="Auto"/>   <!-- Col 0: VAT label -->
  <ColumnDefinition Width="16"/>      <!-- Col 1: gap -->
  <ColumnDefinition Width="Auto"/>   <!-- Col 2: Radio 1 -->
  <!-- Col 3, 4: KHÔNG TỒN TẠI -->
</Grid.ColumnDefinitions>
<RadioButton Grid.Column="2" .../>   <!-- OK -->
<RadioButton Grid.Column="4" .../>   <!-- → rơi vào column 0, chồng lên label + Radio 1 -->
```

---

## 5. GRID VAT SAU KHI SỬA

```xml
<!-- SAU — 5 cột, mỗi RadioButton có cột riêng -->
<Grid.ColumnDefinitions>
  <ColumnDefinition Width="Auto"/>   <!-- Col 0: VAT label -->
  <ColumnDefinition Width="12"/>     <!-- Col 1: gap -->
  <ColumnDefinition Width="Auto"/>   <!-- Col 2: Radio "Đã gồm VAT" -->
  <ColumnDefinition Width="20"/>     <!-- Col 3: gap -->
  <ColumnDefinition Width="Auto"/>   <!-- Col 4: Radio "Chưa gồm VAT" -->
</Grid.ColumnDefinitions>
<TextBlock Text="VAT" .../>
<RadioButton Grid.Column="2" Content="Đã gồm VAT"
             IsChecked="{Binding IsVatIncluded}" GroupName="VAT"/>
<RadioButton Grid.Column="4" Content="Chưa gồm VAT"
             IsChecked="{Binding IsVatExcluded}" GroupName="VAT"/>
```

Layout: `[VAT] [12px] [○ Đã gồm VAT] [20px] [○ Chưa gồm VAT]`

- Không còn chồng chữ
- Khoảng cách giữa 2 radio đủ rộng (20px)
- GroupName="VAT" đảm bảo mutual exclusion

---

## 6. ROOT CAUSE: TIÊU ĐỀ CARD BỊ CẮT

### Vấn đề
Tiêu đề "Tài chính" và "Chi tiết tài chính" bị cắt phía trên.

### Root cause
1. Body Grid margin top là `16px` — quá sát với wizard bar phía trên
2. Left card padding top là `22px` — vừa đủ nhưng kết hợp với margin nhỏ gây cảm giác bị cắt
3. Right card padding top chỉ `20px` — tiêu đề quá sát mép trên của card

### Fix
- Body margin top: `16` → `20` (tăng 4px)
- Left card padding top: `22` → `24` (tăng 2px)
- Right card padding top: `20` → `24` (tăng 4px)

Tổng khoảng cách từ wizard bar đến tiêu đề card: **44px** (20 margin + 24 padding).

---

## 7. CẤU TRÚC GRID ROW SAU KHI SỬA

```
Grid
├── Row 0 (Auto): Header — "Đặt phòng mới"
├── Row 1 (Auto): Wizard Step Bar — 7 bước A-G
├── Row 2 (*):    ScrollViewer
│   └── Grid Margin="24,20,24,16"  ← top margin 20px (đã sửa từ 16)
│       ├── Left Card  Padding="26,24"  ← top padding 24px (đã sửa từ 22)
│       │   └── "Tài chính" title + 4 sections
│       └── Right Card Padding="22,24"  ← top padding 24px (đã sửa từ 20)
│           └── "Chi tiết tài chính" title + 11 rows + balance
└── Row 3 (Auto): Footer — navigation buttons
```

Không dùng:
- Margin âm
- Canvas
- TranslateTransform
- ClipToBounds
- RowSpan qua wizard

---

## 8. PROPERTYCHANGED ĐÃ BỔ SUNG

`UpdateFinancialSummary()` (line 72-79) đã gọi đầy đủ:

```csharp
public void UpdateFinancialSummary()
{
    OnPropertyChanged(nameof(EffectiveRoomRateDisplay));
    OnPropertyChanged(nameof(RoomRateDisplay));
    OnPropertyChanged(nameof(RoomChargeDisplay));
    OnPropertyChanged(nameof(DiscountDisplay));
    OnPropertyChanged(nameof(DepositDisplay));
    OnPropertyChanged(nameof(PaymentDisplay));
    OnPropertyChanged(nameof(BalanceDisplay));
    OnPropertyChanged(nameof(PriceSourceLabel));
    OnPropertyChanged(nameof(VatLabel));
}
```

Được gọi từ tất cả property setters: `CurrencyValue`, `RoomRateInput`, `IsVatIncluded`, `DepositAmountInput`, `PaymentMethodValue`, `DiscountPercentInput`, `DiscountAmountInput`, `IsDepositCollected`, `RateCodeValue`, `UseRateCode`.

---

## 9. FILES ĐÃ SỬA

| File | Thay đổi |
|------|----------|
| `ASMILE.BO.App/Views/ReservationCreateWindow.xaml` | 6 thay đổi: `SelectedValuePath` cho Currency + Payment, Grid VAT columns, body margin, card paddings |
| `ASMILE.BO.App/Views/ReservationCreateWindow.xaml.cs` | 2 thay đổi: `RoomRateDisplay` format `:N0`, `DepositDisplay` format `:N0` |

---

## 10. KẾT QUẢ RUNTIME

| Test | Kết quả |
|------|---------|
| 1. Mở Step E | PASS |
| 2. Tiêu đề không bị cắt | PASS |
| 3. Chọn Currency VND → card hiển thị `VND` | PASS |
| 4. Không còn `System.Windows.Controls.ComboBoxItem` | PASS |
| 5. Nhập Room Rate 1.250.000 → card hiển thị `1.250.000 VND` | PASS |
| 6. Chọn Đã gồm VAT → không chồng chữ | PASS |
| 7. Card hiển thị `Đã gồm` | PASS |
| 8. Chọn Chưa gồm VAT → card hiển thị `Chưa gồm` | PASS |
| 9. Chọn Payment Method → card hiển thị tên đúng | PASS |
| 10. Resize cửa sổ → không bị cắt | PASS |
| 11. Build PASS (0 errors) | PASS |
| 12. Không exception | PASS |

---

## 11. XÁC NHẬN

- [x] Không sửa backend
- [x] Không sửa Step A, B, C, D, F, G
- [x] Chỉ sửa Step E (XAML + code-behind)
- [x] Không thêm migration
- [x] Không hardcode "VND" trong summary
- [x] Không dùng string replace để che lỗi
- [x] Không dùng converter để che lỗi ComboBoxItem
- [x] ViewModel chỉ chứa dữ liệu thuần (string, bool, decimal)
- [x] Không có UI object trong ViewModel properties
