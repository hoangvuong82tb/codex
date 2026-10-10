# SEARCH RESERVATION LIVE FILTER + NIGHTS FIX REPORT

> **Ngày**: 2026-07-22
> **Kết luận**: NIGHTS DISPLAY FIXED + LIVE FILTERING ACTIVE

---

## 1. ROOT CAUSE CỘT SỐ ĐÊM

**Converter `StayNightsConverter` (MultiBinding) không hoạt động ổn định** với `DateOnly` trong WPF DataGrid. Cần giải pháp đơn giản hơn: thêm computed property `Nights` vào `ReservationDto`.

## 2. BINDING TRƯỚC VÀ SAU

**Trước** (DataGrid):
```xml
<MultiBinding Converter="{StaticResource NightsConv}">
  <Binding Path="ArrivalDate"/><Binding Path="DepartureDate"/>
</MultiBinding>
```
→ Converter không nhận đúng giá trị, hiển thị sai.

**Sau** (DataGrid):
```xml
Text="{Binding Nights, Mode=OneWay}"
```
→ Bind trực tiếp computed property.

**Trước** (Detail Panel):
```xml
<MultiBinding Converter="{StaticResource NightsConv}">
  <Binding Path="SelectedReservation.ArrivalDate"/>
  <Binding Path="SelectedReservation.DepartureDate"/>
</MultiBinding>
```

**Sau** (Detail Panel):
```xml
Text="{Binding SelectedReservation.Nights, Mode=OneWay, StringFormat='Số đêm: {0}'}"
```

## 3. CÁCH TÍNH NIGHTS

```csharp
// ReservationDto (ReservationContracts.cs)
public int Nights => Math.Max(0, DepartureDate.DayNumber - ArrivalDate.DayNumber);
```

- `DateOnly.DayNumber` trả về số ngày kể từ 01/01/0001
- `DepartureDate.DayNumber - ArrivalDate.DayNumber` = số đêm
- `Math.Max(0, ...)` đảm bảo không âm
- Không thêm database column
- Không migration

## 4. LIVE FILTERING

**Handler đã thêm** vào `ReservationWorkspaceViewModel`:

```csharp
partial void OnSearchTextChanged(string value) => StartDebouncedSearch();      // đã có
partial void OnSelectedStatusChanged(ReservationStatus? value) => StartDebouncedSearch();  // MỚI
partial void OnFilterArrivalFromChanged(DateTime? value) => StartDebouncedSearch();         // MỚI
partial void OnFilterArrivalToChanged(DateTime? value) => StartDebouncedSearch();           // MỚI
```

Mỗi khi filter thay đổi → `StartDebouncedSearch()` → 350ms debounce → `LoadAsync()`.

## 5. DEBOUNCE

- Thời gian: **350ms** (như hiện có)
- Cơ chế: `CancellationTokenSource` — request cũ bị cancel khi có request mới
- `StartDebouncedSearch` → `Task.Delay(350ms, ct)` → nếu không cancel → `LoadAsync()`

## 6. NÚT TÌM

Đã **bỏ** nút "Tìm" khỏi filter bar. Live filtering không cần nút này.

Nút "Xóa lọc" vẫn còn để reset tất cả filter.

## 7. FILE ĐÃ SỬA

| File | Change |
|------|--------|
| `Application/.../ReservationContracts.cs` | `ReservationDto`: thêm `public int Nights => ...` |
| `ViewModels/ReservationWorkspaceViewModel.cs` | Thêm 3 `On*Changed` → `StartDebouncedSearch()` handlers |
| `Views/SearchReservationView.xaml` | DataGrid + Detail Panel dùng `{Binding Nights}`; bỏ nút Tìm |

## 8. BUILD & RUNTIME

| Check | Result |
|-------|--------|
| Build | PASS (0 errors) |
| App | 263MB |
| Nights DataGrid | Integer |
| Nights Detail Panel | Đồng bộ |
| Từ khóa live | Yes (350ms debounce) |
| Trạng thái live | Yes |
| Ngày live | Yes |
| Xóa lọc | Reset + reload ngay |
| Phân trang | Reset về trang 1 khi filter đổi |

## 9. XÁC NHẬN

- [x] Chỉ sửa Tìm đặt phòng
- [x] Không sửa backend
- [x] Không migration
- [x] Cột Số đêm = integer
- [x] Live filtering hoạt động
- [x] Debounce có sẵn
- [x] Không cần bấm Tìm
