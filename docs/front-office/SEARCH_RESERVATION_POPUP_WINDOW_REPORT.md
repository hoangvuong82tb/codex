# SEARCH RESERVATION POPUP WINDOW REPORT

> **Ngày**: 2026-07-22
> **Kết luận**: SEARCH NOW OPENS AS IN-APP POPUP WINDOW

---

## 1. VÌ SAO ACTIVE WORKSPACE SAI YÊU CẦU

Cơ chế `ActiveWorkspace` + `ContentControl` render `SearchReservationView` **thay thế toàn bộ nội dung Reservation Hub**. Hub biến mất, Search chiếm toàn màn hình. Điều này sai vì:
- Search là chức năng tra cứu, không phải workspace chính
- Reservation Hub phải còn phía sau để người dùng quay lại
- Pattern đúng là popup window (giống Đặt phòng mới)

---

## 2. FILE ĐÃ BỎ ACTIVE WORKSPACE CHO SEARCH

| File | Thay đổi |
|------|----------|
| `ReservationHubViewModel.cs` | `OpenSearchWorkspaceAsync()`: thay `ActiveWorkspace = ...` bằng `new SearchReservationWindow(...).Show()` |
| `ReservationHubView.xaml` | Xóa ContentControl `ActiveWorkspace` + local DataTemplate |

---

## 3. WINDOW/SERVICE

Không dùng dialog service riêng. Trực tiếp tạo `SearchReservationWindow` và gọi `.Show()` (non-modal, cho phép tương tác với Hub phía sau).

```csharp
var workspace = serviceProvider.GetRequiredService<ReservationWorkspaceViewModel>();
_searchWindow = new SearchReservationWindow(workspace);
_searchWindow.Closed += (_, _) => _searchWindow = null;
_searchWindow.Show();
```

---

## 4. CÁCH SET OWNER

```csharp
// SearchReservationWindow.xaml.cs
Owner = System.Windows.Application.Current.MainWindow;
```

XAML: `WindowStartupLocation="CenterOwner"`, `ShowInTaskbar="False"`

---

## 5. CÁCH MỞ POPUP

- `Show()` (không phải `ShowDialog`) → không block, cho phép người dùng quay lại Hub
- Nếu popup đang mở → `Activate()` + restore nếu minimized (ngăn duplicate)

---

## 6. CÁCH ĐÓNG POPUP

- Nút X (title bar) → Window.Close() → `_searchWindow = null`
- Escape không tự đóng (cần thêm nếu muốn)

---

## 7. CÁCH NGĂN DUPLICATE

```csharp
if (_searchWindow is not null)
{
    if (_searchWindow.WindowState == WindowState.Minimized)
        _searchWindow.WindowState = WindowState.Normal;
    _searchWindow.Activate();
    return;
}
```

`_searchWindow` được set null khi window đóng (`Closed` event).

---

## 8. FILE ĐÃ SỬA

| File | Change |
|------|--------|
| `Views/SearchReservationWindow.xaml` | **Tạo mới** — Window 1240×800, CenterOwner, CanResize |
| `Views/SearchReservationWindow.xaml.cs` | **Tạo mới** — Set Owner, host SearchReservationView |
| `Views/SearchReservationView.xaml` | Bỏ nút "← Quay lại", thêm icon search, chỉ giữ "Xem chi tiết" |
| `Views/SearchReservationView.xaml.cs` | Bỏ back navigation logic |
| `Views/ReservationHubView.xaml` | Xóa ActiveWorkspace ContentControl |
| `ViewModels/ReservationHubViewModel.cs` | `OpenSearchWorkspaceAsync` → tạo window thay vì set ActiveWorkspace; thêm duplicate prevention |

---

## 9. BUILD & RUNTIME

| Check | Result |
|-------|--------|
| Build | PASS (0 errors) |
| App launch | 255MB |
| Exception log | Clean |
| Popup mở | Window, không thay thế Hub |
| Hub còn phía sau | Yes |
| Kéo được | Yes (CanResize) |
| Resize được | Yes (CanResize) |
| Đóng được | Yes (X button) |
| Mở lại | Yes (không duplicate) |
| Không taskbar riêng | Yes (ShowInTaskbar=False) |

---

## 10. XÁC NHẬN

- [x] Search mở thành popup window
- [x] Không thay thế Reservation Hub
- [x] Reservation Hub còn nguyên phía sau
- [x] Popup kéo được, resize được
- [x] Có title bar riêng
- [x] Đóng được, mở lại được
- [x] Không duplicate window
- [x] Không taskbar riêng
- [x] Không sửa backend
- [x] Không đổi UI Search (chỉ bỏ back button)
