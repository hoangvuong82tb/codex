# CREATE RESERVATION IN-APP POPUP WINDOW REPORT

> **Ngày**: 2026-07-21
> **Kết luận**: IN-APP POPUP WINDOW — Same pattern as Chi tiết phiếu xuất

---

## 1. PATTERN REFERENCE

`StockReceiptDetailWindow` — WPF `Window`, `WindowStartupLocation="CenterOwner"`, `ShowInTaskbar="False"`, mở qua `new Window().ShowDialog()`.

## 2. FILES CREATED

| File | Type |
|------|------|
| `Views/ReservationCreateWindow.xaml` | Window — 120 dòng |
| `Views/ReservationCreateWindow.xaml.cs` | Code-behind — Owner=MainWindow |

## 3. FILES MODIFIED

| File | Change |
|------|--------|
| `ViewModels/ReservationHubViewModel.cs` | +`IServiceProvider`, +`serviceProvider` field, `OpenModalAsync` → khi `New` → mở `ReservationCreateWindow` với `ShowDialog()` |

## 4. WINDOW SPEC

| Property | Value |
|----------|-------|
| Type | `Window` (not UserControl) |
| Title | "Đặt phòng mới" |
| Width × Height | 1120 × 720 |
| MinWidth × MinHeight | 900 × 620 |
| WindowStartupLocation | CenterOwner |
| ResizeMode | CanResize |
| ShowInTaskbar | False |
| Owner | `System.Windows.Application.Current.MainWindow` |

## 5. COMMAND FLOW

```
Card "Đặt phòng mới"
  → OpenModalAsync(ReservationModalKind.New, "new")
    → scope.CreateScope() → ReservationWorkspaceViewModel
    → new ReservationModalWorkspaceViewModel(workspace, kind, close)
    → new ReservationCreateWindow(modal) { Owner = MainWindow }
    → window.ShowDialog()
```

## 6. VS OLD OVERLAY

| Before | After |
|--------|-------|
| UserControl + overlay #66000000 | Window + ShowDialog() |
| Fixed centered form | Draggable + resizable window |
| Hub hidden behind dark overlay | **Hub visible behind popup** |
| No title bar | **Window title bar** |

## 7. BUILD & RUNTIME

| Check | Result |
|-------|--------|
| Build | PASS (0 errors) |
| App launched | YES — PID 23164, ~202MB |
| New exceptions | 0 |
| Backend unchanged | ✓ |

---

**IN-APP POPUP WINDOW — Same pattern as Chi tiết phiếu xuất**
