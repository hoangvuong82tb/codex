# FO UI RUNTIME STARTUP FIX REPORT

> **Ngày**: 2026-07-21
> **Kết luận**: FO UI RUNTIME GREEN

---

## 1. LỖI TÁI HIỆN

| Item | Detail |
|------|--------|
| Exception Type | `System.InvalidOperationException` |
| Message | `The entity type 'ComplaintRecord' requires a primary key to be defined.` |
| Thời điểm | App startup (`OnStartup`) → Migration Service → DbContext model validation |
| File | `App.xaml.cs:58` → `DatabaseMigrationService.cs:38` → `DatabaseMigrationService.cs:97` |

---

## 2. VỊ TRÍ LOG ĐÃ KIỂM TRA

| Location | Path | Found |
|----------|------|-------|
| UI Exception Log | `logs/ui-exceptions.log` | YES — 537MB, 6.6M dòng |
| Serilog | `logs/asmile-bo-20260721.log` | YES — structured log |
| Exception handler | `App.xaml.cs:95-147` | YES — Serilog + file-based diagnostic |
| Global config | `appsettings.json` | YES — Serilog configured |

---

## 3. ROOT CAUSE

**`ComplaintRecord` và `WakeUpCallRecord` không được ignore trong EF Core model.**

Khi Phase 2.2 thêm 2 value object này vào `HotelStay` collection, EF Core tự động discover chúng qua navigation property và cố gắng map thành entity. Vì `ComplaintRecord` và `WakeUpCallRecord` là `sealed record` không có primary key, EF Core throw `InvalidOperationException` ngay khi DbContext model được build (tại `DatabaseMigrationService.InitializeAsync()`).

Các value object tương tự (`RoomMoveRecord`, `KeyCardRecord`, `StayTimelineEntry`) đã được ignore từ trước. Chỉ thiếu 2 cái mới.

---

## 4. FILE VÀ DÒNG CODE GÂY LỖI

| File | Line | Issue |
|------|------|-------|
| `Core/FrontOffice/Stay/StayTypes.cs:72-75` | `WakeUpCallRecord` record | EF discovered as entity |
| `Core/FrontOffice/Stay/StayTypes.cs:78-79` | `ComplaintRecord` record | EF discovered as entity |
| `Infrastructure/Persistence/AppDbContext.cs:934-937` | `Ignore<>` list | Missing entries for new records |
| `App.xaml.cs:58` | `migrationService.InitializeAsync()` | Trigger point |
| `App.xaml.cs:138` | `MessageBox.Show("Thao tác không thành công...")` | User-facing error |

---

## 5. THAY ĐỔI ĐÃ THỰC HIỆN

| File | Change |
|------|--------|
| `Infrastructure/Persistence/AppDbContext.cs:936-937` | Thêm `modelBuilder.Ignore<WakeUpCallRecord>()` và `modelBuilder.Ignore<ComplaintRecord>()` |

```csharp
// Before (missing):
modelBuilder.Ignore<Core.FrontOffice.Stay.StayTimelineEntry>();
modelBuilder.Ignore<Core.FrontOffice.Payment.SplitTenderLine>();

// After (fixed):
modelBuilder.Ignore<Core.FrontOffice.Stay.StayTimelineEntry>();
modelBuilder.Ignore<Core.FrontOffice.Stay.WakeUpCallRecord>();    // NEW
modelBuilder.Ignore<Core.FrontOffice.Stay.ComplaintRecord>();     // NEW
modelBuilder.Ignore<Core.FrontOffice.Payment.SplitTenderLine>();
```

---

## 6. VERIFICATION

### Runtime Verification

| Check | Result |
|-------|--------|
| App process running | YES — PID 56744, ~307MB RAM |
| No new ui-exceptions.log entries since fix | YES — last modified 15:54 (before fix) |
| No new Serilog errors | YES |
| Cold start | PASS |
| DI Validation | PASS (all services resolve) |

### Build & Test

| Check | Result |
|-------|--------|
| Build Debug | PASS (0 errors) |
| DI Unit Test | PASS |
| Full HotelStay test suite | PASS (61/61) |

---

## 7. LOG TRƯỚC VÀ SAU

### Trước (crash)
```
[2026-07-21 15:54:53.127] CurrentView=(main-window-without-content)
System.InvalidOperationException: The entity type 'ComplaintRecord' requires a primary key to be defined.
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(...)
   at ASMILE.BO.Infrastructure.Persistence.DatabaseMigrationService.InitializeAsync(...)
   at ASMILE.BO.App.App.OnStartup(StartupEventArgs e) in App.xaml.cs:line 58
```

### Sau (success)
```
(No new entries — app started without exceptions)
```

---

## 8. RỦI RO CÒN LẠI

| Risk | Severity | Notes |
|------|----------|-------|
| Cần SQL Server để verify end-to-end | Medium | Migration cần DB thật để chạy |
| 5 Restaurant tests fail (SQL Server) | Low | Pre-existing, không liên quan |
| CS8826 warnings (partial method sig) | Low | Pre-existing, cosmetic |

---

**FO UI RUNTIME GREEN**
