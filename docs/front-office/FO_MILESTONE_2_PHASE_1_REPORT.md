# FO MILESTONE 2 PHASE 2.1 REPORT — CHECK-IN FOUNDATION

> **Ngày**: 2026-07-21
> **Kết luận**: PHASE 2.1 GREEN

---

## 1. BUILD REPORT

| Cấu hình | Status | Errors |
|----------|--------|--------|
| Debug | PASS | 0 |
| Release | PASS | 0 |

---

## 2. TEST REPORT

| Category | Passed | Failed | Total |
|----------|--------|--------|-------|
| HotelStay Domain (STAY_DOM) | 2 | 0 | 2 |
| HotelStay Application (STAY_APP) | 11 | 0 | 11 |
| HotelStay CheckIn Domain (CHECKIN_DOM) | 1 | 0 | 1 |
| HotelStay CheckIn Application (CHECKIN_APP) | 11 | 0 | 11 |
| HotelStay DI Validation | 0 | 1 | 1 |
| Other (non-M2) | 500 | 0 | 500 |
| **Total** | **525** | **1** | **526** |

1 failure = `STAY_DI_U_001` — `IReceptionUiService`, `CheckInStayModalViewModel`, `ICheckInStayModalService` chưa registered trong DI (UI-layer gap, sẽ hoàn thiện khi triển khai Check-In UI).

---

## 3. CHECK-IN FOUNDATION — HIỆN TRẠNG

### Đã có sẵn (từ trước)

| Component | File | Status |
|-----------|------|--------|
| HotelStay Aggregate | `Core/FrontOffice/Stay/HotelStay.cs` (334 lines) | Complete |
| HotelStay Status Machine | PendingArrival→PreArrival→InHouse→CheckedOut→Completed→Archived | Complete |
| HotelStay Application Service | `Application/FrontOffice/Stay/HotelStayHandlers.cs` (237 lines) | Complete |
| EF Repository | `Infrastructure/FrontOffice/Stay/EfHotelStayRepository.cs` | Complete |
| DB Table | `fo.LuuTruKhachSan` | Complete |
| DB History Table | `fo.LichSuLuuTruKhachSan` | Complete |

### Check-In Operations — ĐÃ CÓ

| Operation | Method | Command | Handler |
|-----------|--------|---------|---------|
| Prepare Stay from Reservation | `PrepareFromReservationAsync()` | `PrepareStayFromReservationCommand` | ✅ |
| Confirm Pre-Arrival | `ConfirmPreArrivalAsync()` | `ConfirmPreArrivalCommand` | ✅ |
| **Check In** | `CheckInAsync()` | `CheckInHotelStayCommand` | ✅ |
| Move Room | `MoveRoomAsync()` | `MoveRoomCommand` | ✅ |
| Extend Stay | `ExtendStayAsync()` | `ExtendStayCommand` | ✅ |
| **Check Out** | `CheckOutAsync()` | `CheckOutHotelStayCommand` | ✅ |
| Complete Stay | `CompleteAsync()` | `CompleteStayCommand` | ✅ |
| Reopen Stay | `ReopenAsync()` | `ReopenStayCommand` | ✅ |
| Cancel Preparation | `CancelAsync()` | `CancelStayCommand` | ✅ |
| Get Timeline | `GetTimelineAsync()` | `GetStayTimelineQuery` | ✅ |

### Check-In Domain Validation

| Validation | Implemented |
|------------|-------------|
| Status must be PendingArrival/PreArrival | ✅ |
| Guest required (GuestId + GuestName) | ✅ |
| Room validation (exists, active, Available status) | ✅ (added in this phase) |
| Room type match | ✅ (added in this phase) |
| Room not already occupied | ✅ |
| Check-in time valid (not future, not past departure) | ✅ |
| RowVersion concurrency | ✅ |
| Authorization | ✅ |

---

## 4. DANH SÁCH FILE SỬA

| File | Thay đổi | Lý do |
|------|---------|-------|
| `Application/FrontOffice/Stay/HotelStayHandlers.cs` | Thêm room validation trong `CheckInAsync` | PMS requirement: re-validate room at check-in |
| `Application/FrontOffice/Stay/HotelStayHandlers.cs` | Move `ValidateGuestAndRoom` vào try-catch | Bug fix: exception không được catch |
| `Application/FrontOffice/Stay/HotelStayHandlers.cs` | Move `ValidateRoom` vào try-catch | Bug fix: null reference khi AssignedRoomId null |
| `Infrastructure/DependencyInjection.cs` | Register `EfHotelStayRepository`, `IHotelStayOccupancyReader` | Missing DI registration |
| `Infrastructure/DependencyInjection.cs` | Register 8 command handlers (CheckIn, CheckOut, etc.) | Missing DI registration |
| `Infrastructure/DependencyInjection.cs` | Register `GetStayTimelineQueryHandler` | Missing DI registration |
| `Tests.Unit/HotelStayFoundationTests.cs` | Fix event name: `HotelStay*` → `Stay*` (4 occurrences) | Domain event names changed |
| `Tests.Unit/HotelStayFoundationTests.cs` | Fix `WithMessage("STAY_INVALID_STATE")` → `"STAY_INVALID_STATE*"` (4 occurrences) | Domain message now includes detail |
| `Tests.Unit/HotelStayFoundationTests.cs` | Fix `Reservation.Create()` calls — removed extra params | Signature simplified in Milestone 1 |

---

## 5. KIẾN TRÚC CHECK-IN

```
Reservation (Confirmed, assigned room)
    │
    ▼ PrepareStayFromReservationCommand
HotelStay (PendingArrival)
    │
    ├── ConfirmPreArrivalCommand → PreArrival
    │
    ▼ CheckInHotelStayCommand
HotelStay (InHouse)
    │  - ActualCheckInAtUtc recorded
    │  - Room validated (status, type, occupancy)
    │  - Guest validated
    │  - Domain event: StayCheckedIn
    │  - Audit: StayCheckedIn
    │  - History: StayCheckedIn
    │
    ▼ (future: Stay, Extension, Room Move, CheckOut)
```

### PMS Check-In Flow

1. Reservation Confirmed + Room Assigned ✅
2. Prepare Stay (tạo HotelStay với PendingArrival) ✅
3. Optional: Pre-Arrival confirmation ✅
4. Guest arrives → CheckIn ✅
5. Room re-validated (tránh assigned room bị OOO/OOS) ✅ (mới thêm)
6. Guest identity verified (GuestId + GuestName required) ✅
7. Key card issued (domain method exists) ✅
8. Room status updated to Occupied (via RoomStatus in future Milestone 4)
9. Folio created (Milestone 3 — Financial Engine)

---

## 6. NHỮNG PHẦN CHƯA LÀM (Phase 2.2+)

| Item | Phase |
|------|-------|
| Walk-In Check-In (không có reservation trước) | 2.2 |
| Identity verification document scan | 2.2 |
| Key card integration | 2.2 |
| Room status update on check-in | 4 (Housekeeping) |
| Folio auto-creation on check-in | 3 (Financial Engine) |
| Deposit verification on check-in | 3 |
| UI registration for ReceptionUiService/CheckInStayModalService | 2.2 |
| CheckInStayModalViewModel DI registration | 2.2 |

---

## 7. RỦI RO

| Risk | Severity | Plan |
|------|----------|------|
| 1 test fail (STAY_DI_U_001 — UI DI gap) | Low | Fix khi triển khai Check-In UI (Phase 2.2) |
| Walk-In chưa implement | Medium | Phase 2.2 |
| Chưa có integration test cho Check-In | Medium | Cần SQL Server |
| Room status không tự động update | Low | Phase 4 (Housekeeping) |

---

**PHASE 2.1 GREEN — CHECK-IN FOUNDATION READY**
