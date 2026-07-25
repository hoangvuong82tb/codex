# FO-EXEC-003 — AVAILABILITY ENGINE ENTERPRISE

**Date**: 2026-07-20
**Sprint**: FO-EXEC-003
**Status**: COMPLETE

---

## 1. Current Architecture

### Before (Pre-Engine)

```
Reservation ──→ CheckAsync (own conflict detection)
RoomRack    ──→ EfRoomRackQueryService (manual state computation)
TapeChart   ──→ Hardcoded fake data (no DB)
```

Each module computed availability independently. No single source of truth.

### After (With Availability Engine)

```
                    ┌─────────────────────┐
                    │  AvailabilityEngine  │ (Single Source of Truth)
                    └──────────┬──────────┘
           ┌───────────────────┼───────────────────┐
           ▼                   ▼                   ▼
    Reservation          RoomRack             TapeChart
    (AssignRoom)        (GetRoomRack)        (GetRoomAvailability)
```

---

## 2. Availability Model

### RoomAvailabilityState Enum (14 states)

| State | Source | Description |
|-------|--------|-------------|
| VacantClean | Housekeeping | Room empty, cleaned, ready for guest |
| VacantDirty | Housekeeping | Room empty, needs cleaning |
| OccupiedClean | HotelStay (InHouse) | Guest in room, clean |
| OccupiedDirty | HotelStay (InHouse) + HK | Guest in room, needs service |
| Reserved | Reservation / HotelStay (PendingArrival) | Room assigned to future booking |
| ExpectedArrival | Reservation / Stay arriving today | Guest arriving today |
| ExpectedDeparture | HotelStay departing tomorrow | Guest departing tomorrow |
| DueOut | HotelStay past departure date | Guest overstayed |
| OutOfOrder | HotelRoom.OperationalStatus | Room unavailable for maintenance |
| OutOfService | HotelRoom.OperationalStatus | Room out of service |
| Blocked | RoomHousekeepingState.Blocked | Management block |
| Maintenance | (Reserved for future) | Under maintenance |
| HouseUse | (Reserved for future) | Staff/house use |
| Complimentary | (Reserved for future) | Free/complimentary stay |

**State computation rule**: Room state is derived from 3 data sources in priority order:

1. **Operational** (HotelRoom.OperationalStatus): OOO → OutOfOrder, OOS → OutOfService
2. **Housekeeping** (RoomHousekeepingState.Status): Blocked → Blocked
3. **Occupancy** (HotelStay.Status → Reservation.Status):
   - InHouse → OccupiedClean/Dirty (depending on HK)
   - PendingArrival → Reserved or ExpectedArrival (if arrival = today)
   - Confirmed/Guaranteed with AssignedRoomId → Reserved or ExpectedArrival
   - None → VacantClean/Dirty (depending on HK)

---

## 3. Inventory Engine

### RoomTypeInventory (per room type)

```
PhysicalInventory   = total active rooms of this type
OutOfOrder/Service  = rooms OOO/OOS
Occupied            = InHouse rooms
Reserved            = future bookings
ExpectedDeparture   = departing tomorrow
ExpectedArrival     = arriving today
VacantClean/Dirty   = available now
AvailableRooms      = VacantClean + VacantDirty
RemainingInventory  = AvailableRooms + ExpectedDeparture (sellable tonight)
WalkInCapacity      = RemainingInventory (walk-in guests)
```

### DailyInventory (per date)

```
Available = VacantRooms
OccupancyPercent = (Total - Available) / Total * 100
Remaining = Available (sellable for tonight)
```

### AvailabilityForecast

Multi-day forecast with peak occupancy and lowest availability dates.

---

## 4. Reservation Integration

**Before**: `AssignRoomCommand` called `repository.CheckAsync()` for room conflict detection.

**After**: `AssignRoomCommand` calls `availability.CheckAvailabilityAsync()` — the AvailabilityEngine is now the single authority for room assignment validation.

```csharp
// Old: direct repository call
var conflict = await repository.CheckAsync(...);

// New: via AvailabilityEngine
var availResult = await availability.CheckAvailabilityAsync(
    new AvailabilityRequest(propertyId, businessDate, arrival, departure, ...));
```

---

## 5. Room Rack Integration

**Before**: `EfRoomRackQueryService` manually loaded stays + housekeeping + rooms and computed state with string mappings.

**After**: Delegates to `IAvailabilityEngine.GetRoomAvailabilityAsync()` which provides computed `RoomAvailability` records. The query service now only handles filtering and DTO mapping.

---

## 6. Tape Chart Integration

**Before**: `TapeChartViewModel` hardcoded 12 rooms (101-112) with fake guest names, fake reservation numbers, fake rates.

**After**: `TapeChartViewModel` injects `IAvailabilityEngine` and `IFrontOfficePropertyContextHost`, loads real room availability data from the current property context.

---

## 7. Tests

### Test Results

| Check | Result |
|-------|--------|
| Unit Tests | 335/335 PASS |
| Debug Build | PASS (0 errors) |
| Release Build | PASS (0 errors) |
| Warning Count | No increase |

### Test Coverage

- ReservationAcceptanceTests: Added `TestAvailability` stub implementing `IAvailabilityEngine`
- HotelStayFoundationTests: Updated with AvailabilityEngine integration
- Integration tests: Updated constructor to inject `AvailabilityEngine`

---

## 8. Files Modified

### New Files

| File | Description |
|------|-------------|
| `Core/FrontOffice/Availability/AvailabilityModels.cs` | RoomAvailabilityState enum (14 states), RoomAvailability, RoomTypeInventory, AvailabilityRequest/Result, DailyInventory, AvailabilityForecast records |
| `Application/FrontOffice/Availability/AvailabilityContracts.cs` | IAvailabilityEngine interface (5 methods) |
| `Infrastructure/FrontOffice/Availability/AvailabilityEngine.cs` | Implementation — computes state from HotelRoom + HotelStay + Reservation + Housekeeping |

### Modified Files

| File | Change |
|------|--------|
| `Infrastructure/DependencyInjection.cs` | Registered AvailabilityEngine as IAvailabilityEngine (scoped) |
| `Infrastructure/FrontOffice/RoomRack/EfRoomRackQueryService.cs` | Uses IAvailabilityEngine instead of manual state computation |
| `Application/FrontOffice/Reservation/ReservationHandlers.cs` | Injects IAvailabilityEngine; AssignAsync uses CheckAvailabilityAsync |
| `App/ViewModels/TapeChartViewModel.cs` | Injects IAvailabilityEngine + IFrontOfficePropertyContextHost; loads real data |
| `Tests.Unit/ReservationAcceptanceTests.cs` | Added TestAvailability stub; updated service constructor |
| `Tests.Integration/FrontOfficeSqlServerIntegrationTests.cs` | Updated CreateReservationService with AvailabilityEngine |

---

## 9. Remaining Gaps

| Gap | Priority | Notes |
|-----|----------|-------|
| HouseUse/Complimentary/Maintenance states | P2 | Enum defined but not yet computed from existing data |
| Overbooking limit per room type | P2 | RoomTypeInventory.OverbookingLimit = 0 (not yet configurable) |
| Rate/ADR/RevPar in forecast | P2 | DailyInventory.Adr/RevPar = 0 (needs Rate Engine integration) |
| Real-time availability cache | P3 | Stateless computation; may need caching for performance on large hotels |
| Walk-in integration | P2 | WalkInCapacity computed but Walk-in module not yet using it |
| Group block integration | P3 | Group reservations may temporarily block rooms |
| Availability alerts/notifications | P3 | No push notification when availability drops below threshold |

---

## 10. Recommended Next Sprint

### FO-EXEC-004: Walk-in & Check-in Integration

1. Walk-in uses AvailabilityEngine.CheckAvailabilityAsync
2. Check-in creates HotelStay and updates availability
3. End-to-end reservation → availability → check-in flow

### FO-EXEC-005: Rate Engine Integration

1. RatePlan linked to availability queries
2. ADR/RevPar computation in DailyInventory
3. Rate-aware availability search
