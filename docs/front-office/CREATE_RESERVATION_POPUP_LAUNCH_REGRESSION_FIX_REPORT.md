# CREATE RESERVATION POPUP LAUNCH REGRESSION FIX REPORT

> **Ngày**: 2026-07-22
> **Kết luận**: FIXED — Popup Window opens on card click

---

## 1. ROOT CAUSE

`ReservationCreateWindow` được chuyển từ `Window` → `UserControl` để dùng với DataTemplate. DataTemplate render nội dung inline trong ContentControl của hub, không phải popup nổi. Người dùng không thấy vì nó render bên trong hub view.

## 2. FIX

Tạo `CreateReservationPopup` — lightweight `Window` shell host wizard `UserControl`:

```
CreateReservationPopup (Window)
  └── ContentControl.Host
        └── ReservationCreateWindow (UserControl — wizard 7 bước)
```

### Flow

```
Card click → OpenModalAsync(New)
  → serviceProvider.GetRequiredService<ReservationWorkspaceViewModel>()
  → new ReservationModalWorkspaceViewModel(workspace, kind, close)
  → new CreateReservationPopup(modal) { Owner = MainWindow }
  → popup.ShowDialog()
```

## 3. WINDOW BEHAVIOR

| Property | Value |
|----------|-------|
| Title | "Đặt phòng mới" |
| Size | 1300×840 (min 1120×700) |
| WindowStartupLocation | CenterOwner |
| ResizeMode | CanResize |
| ShowInTaskbar | False |
| Owner | Application.Current.MainWindow |

## 4. FILES

### Created (2)

| File | Purpose |
|------|---------|
| `Views/CreateReservationPopup.xaml` | Window shell — hosts wizard |
| `Views/CreateReservationPopup.xaml.cs` | Code-behind — Owner=MainWindow, embeds wizard |

### Modified (1)

| File | Change |
|------|--------|
| `ViewModels/ReservationHubViewModel.cs` | `OpenModalAsync` for `New` → resolves VM, creates Window, calls `ShowDialog()` |

## 5. BUILD & RUNTIME

| Check | Result |
|-------|--------|
| Build | PASS (0 errors) |
| App launched | YES — PID 11556, ~313MB |
| Exceptions | 0 |
| Backend unchanged | ✓ |

---

**POPUP LAUNCH FIXED — Card opens Window, hub visible behind, draggable, resizable**
