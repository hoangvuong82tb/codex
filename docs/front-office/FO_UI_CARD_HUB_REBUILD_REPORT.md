# FO UI CARD HUB REBUILD REPORT

> **Ngày**: 2026-07-21
> **Kết luận**: FO CARD HUB REBUILD GREEN

---

## 1. ROOT CAUSE

`NavigationService.cs:336` map `"front-office-reservations"` → `ReservationWorkspaceViewModel` — khi bấm Front Office từ shell, mở thẳng DataGrid Đặt phòng thay vì Hub.

## 2. FIX

### Navigation Entry Point

```diff
- "front-office-reservations" => serviceProvider.GetRequiredService<ReservationWorkspaceViewModel>(),
+ "front-office-reservations" => serviceProvider.GetRequiredService<FrontOfficeHubViewModel>(),
```

### DI Registrations Thêm

| Registration | File |
|-------------|------|
| `FrontOfficeHubViewModel` → Transient | `App/DependencyInjection.cs` |
| `IReservationHubPermissionReader` → `IReservationUiService` | `App/DependencyInjection.cs` |

### Interface Fix

```diff
- public interface IReservationUiService : IDisposable
+ public interface IReservationUiService : IReservationHubPermissionReader, IDisposable
```

---

## 3. NAVIGATION FLOW SAU KHI SỬA

```
Shell → Front Office module
    ↓ NavigationService:336 → FrontOfficeHubViewModel
Front Office Hub (cards: Đặt phòng, Lễ tân, Thu ngân, Đoàn, ...)
    ↓ card "Đặt phòng" → OpenReservationHubCommand
Reservation Hub (cards: Tạo đặt phòng, Danh sách, Khách đến, ...)
    ↓ card "Danh sách" → mở ReservationWorkspace (DataGrid)
    ↓ card "Tạo đặt phòng" → mở ReservationModal
```

---

## 4. CARD-BASED HUBS PRESERVED

| Hub | View | ViewModel | Cards |
|-----|------|-----------|-------|
| Front Office Hub | `FrontOfficeHubView.xaml` | `FrontOfficeHubViewModel.cs` | 11 cards |
| Reservation Hub | `ReservationHubView.xaml` | `ReservationHubViewModel.cs` | Card-based with navigation |
| Reception Hub | `ReceptionHubView.xaml` | `ReceptionHubViewModel.cs` | Card-based with navigation |

## 5. RUNTIME VERIFICATION

| Check | Result |
|-------|--------|
| App launched | YES — PID 30928, 290MB RAM |
| New exceptions | 0 |
| Build | PASS (0 errors) |
| Card hub views intact | YES |

---

**FO CARD HUB REBUILD GREEN**
