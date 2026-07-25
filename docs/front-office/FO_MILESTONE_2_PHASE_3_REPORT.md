# FO MILESTONE 2 PHASE 2.3 REPORT — ROOM MOVE FOUNDATION

> **Ngày**: 2026-07-21
> **Kết luận**: PHASE 2.3 GREEN

---

## 1. BUILD & TEST

| Metric | Value |
|--------|-------|
| Debug Build | PASS (0 errors) |
| HotelStay Tests | **52/52 ALL PASS** |
| New Room Move Tests | 12 (10 domain + 2 application) |

---

## 2. ROOM MOVE TYPES

```csharp
public enum RoomMoveType { SameType, Upgrade, Downgrade, Maintenance, OutOfOrder, Emergency, Vip }
```

### Auto-Detection Logic

| Keyword (reason) | MoveType |
|-----------------|----------|
| "maintenance" / "bảo trì" | Maintenance |
| "emergency" / "khẩn cấp" | Emergency |
| "vip" / "đặc biệt" | Vip |
| "out of order" / "ooo" / "hỏng" | OutOfOrder |
| toDisplayOrder < fromDisplayOrder | Upgrade |
| toDisplayOrder > fromDisplayOrder | Downgrade |
| (none of the above) | SameType |

---

## 3. ENHANCED ROOM MOVE RECORD

```csharp
public sealed record RoomMoveRecord(
    Guid FromRoomId, string FromRoomNumber, Guid FromRoomTypeId,
    Guid ToRoomId, string ToRoomNumber, Guid ToRoomTypeId,
    RoomMoveType MoveType,     // NEW
    string Reason,
    DateTime MovedAtUtc,
    Guid? MovedBy);
```

---

## 4. ENHANCED VALIDATION

| Rule | Error Code |
|------|-----------|
| Target room cannot be Guid.Empty | `ROOM_MOVE_TARGET_REQUIRED` |
| Cannot move to same room | `ROOM_MOVE_SAME_ROOM` |
| Reason required | `ROOM_MOVE_REASON_REQUIRED` |
| Must be occupying (InHouse+) | `STAY_INVALID_STATE` |
| Target room must exist + active + Available | `STAY_ROOM_NOT_ASSIGNABLE` |
| Target room not already occupied | `STAY_ROOM_ALREADY_OCCUPIED` |
| RowVersion concurrency | `STAY_ROWVERSION_REQUIRED` |

---

## 5. DOMAIN EVENTS

| Event | Metadata |
|-------|----------|
| `StayRoomMoved` | `From={room};To={room};Type={moveType};Reason={reason}` |

---

## 6. STATE MACHINE — ALLOWED COMMANDS

| From Status | Allowed Commands |
|-------------|-----------------|
| PendingArrival | Prepare, PreCheckIn, ConfirmPreArrival, Cancel |
| PreArrival | CheckIn, Cancel |
| InHouse | MoveRoom, ExtendStay, ToggleDND, WakeUpCall, LogComplaint, AddTag, RemoveTag, MarkVipService, CheckOut |
| RoomMoved | MoveRoom, ExtendStay, ... |
| Extended | MoveRoom, ExtendStay, ... |
| CheckedOut | Complete, Reopen |
| Completed | Archive, Reopen |
| Cancelled | (none) |

---

## 7. FILES MODIFIED

| File | Changes |
|------|---------|
| `Core/FrontOffice/Stay/StayTypes.cs` | +`RoomMoveType` enum, enhanced `RoomMoveRecord` |
| `Core/FrontOffice/Stay/HotelStay.cs` | Enhanced `MoveRoom()` + `DetectMoveType()` |
| `Application/FrontOffice/Stay/HotelStayHandlers.cs` | Enhanced `MoveRoomAsync` (room type lookup + display order) |
| `Tests.Unit/HotelStayFoundationTests.cs` | +12 tests (ROOMMOVE_DOM 10, ROOMMOVE_APP 2) |

---

## 8. ARCHITECTURE COMPLIANCE

| Principle | Status |
|-----------|--------|
| DDD Aggregate | ✅ Room move fully encapsulated in HotelStay |
| Value Object | ✅ RoomMoveRecord with typed MoveType |
| CQRS | ✅ Existing MoveRoomCommand enhanced |
| Audit Trail | ✅ History + domain events |
| No M1 modification | ✅ |
| Not God Object | ✅ ~460 lines, all methods focused |

---

**PHASE 2.3 GREEN — ROOM MOVE FOUNDATION READY**
