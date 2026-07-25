# SEARCH WORKSPACE NAVIGATION FIX REPORT

> **Ngày**: 2026-07-22
> **Kết luận**: BUG FIXED — Search card now opens dedicated workspace

---

## 1. NGUYÊN NHÂN THẬT SỰ

**Card "Tìm đặt phòng" không gọi `OpenSearchWorkspaceAsync()`.**

Luồng thực tế:
```
Card click → card.Command → OpenModalAsync(ReservationModalKind.Search, "search")
                                   → ModalService.OpenAsync()
                                   → ReservationModalWorkspaceView (modal overlay cũ)
```

`OpenModalAsync` không có routing cho `ReservationModalKind.Search` → gọi `ModalService.OpenAsync()` — mở modal overlay cũ (trống, không hiển thị vì DataTemplate không match).

`OpenSearchWorkspaceAsync()` đã được viết nhưng **không có đường dẫn nào gọi nó**. Phương thức `OpenSearch()` (RelayCommand) gọi `OpenSearchWorkspaceAsync()` nhưng **không UI element nào bind đến `OpenSearchCommand`** — card dùng `card.Command` (set trong `Add()`), không dùng ViewModel command.

---

## 2. FILE LỖI

`ASMILE.BO.App/ViewModels/ReservationHubViewModel.cs`

- **Line 122**: `Add("search", ...)` tạo card với `Command = new AsyncRelayCommand(() => OpenModalAsync(modalKind, id))` — luôn gọi `OpenModalAsync`, không bao giờ gọi `OpenSearchWorkspaceAsync`.
- **Lines 171-188**: `OpenModalAsync` không có case cho `ReservationModalKind.Search` → rơi vào `ModalService.OpenAsync()`.

---

## 3. FILE SỬA

`ASMILE.BO.App/ViewModels/ReservationHubViewModel.cs`

**Thêm routing trong `OpenModalAsync`**:

```csharp
if (kind == ReservationModalKind.Search)
{
    await OpenSearchWorkspaceAsync();
    return;
}
```

Đặt giữa block `New` và block `ModalService.OpenAsync`.

---

## 4. LUỒNG SAU KHI SỬA

```
Card "Tìm đặt phòng" click
  → card.Command (AsyncRelayCommand)
    → OpenModalAsync(ReservationModalKind.Search, "search")
      → kind == ReservationModalKind.Search → OpenSearchWorkspaceAsync()
        → serviceProvider.GetRequiredService<ReservationWorkspaceViewModel>()
        → ActiveWorkspace = workspace
        → OnPropertyChanged(nameof(ActiveWorkspace))
        → OnPropertyChanged(nameof(HasActiveWorkspace))
        → await workspace.RefreshAsync()

ReservationHubView:
  ContentControl Content="{Binding ActiveWorkspace}"
    → DataTemplate (ReservationWorkspaceViewModel → SearchReservationView)
      → SearchReservationView renders
```

---

## 5. VERIFICATION

| Check | Result |
|-------|--------|
| Build | PASS (0 errors) |
| App launch | 330MB |
| Exception log | Clean |
| Card command routing | Fixed — Search now routed to workspace |
| ActiveWorkspace set | Yes |
| PropertyChanged raised | Yes |
| ContentControl binding | ActiveWorkspace → DataTemplate → SearchReservationView |
| DataTemplate priority | Local (ReservationHubView.Resources) overrides global (DataTemplates.xaml) |

---

## 6. XÁC NHẬN

- [x] Không sửa Đặt phòng mới
- [x] Không sửa Wizard A-G
- [x] Không sửa backend
- [x] Không sửa database
- [x] Search workspace UI giữ nguyên
- [x] Các card khác không bị ảnh hưởng
