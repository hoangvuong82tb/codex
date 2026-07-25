# FO-018 ENTERPRISE CONCIERGE & GUEST SERVICES PLATFORM — ASMILE-BO

**Ngày:** 2026-07-20
**Loại:** ENTERPRISE MODULE
**Trạng thái:** HOÀN THÀNH ✅

---

## 1. Architecture

```
┌────────────────────────────────────────────────────────────┐
│            CONCIERGE & GUEST SERVICES PLATFORM              │
│                                                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │   Service    │  │  Wake-Up     │  │  Transportation  │  │
│  │   Request    │  │   Call       │  │                  │  │
│  └──────┬───────┘  └──────┬───────┘  └────────┬─────────┘  │
│         │                 │                    │            │
│  ┌──────┴─────────────────┴────────────────────┴─────────┐  │
│  │  Luggage │ BellService │ Package │ Mail │ VIP Service │  │
│  └───────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐   │
│  │         PLATFORM INTEGRATION                          │   │
│  │  Guest │ Stay │ Workflow │ RuleEngine │ EventBus     │   │
│  └──────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────┘
```

---

## 2. Service Request Workflow (7 States)

```
Requested → Assigned → Accepted → InProgress → Completed
     │          │           │            │
     └──────────┴───────────┴────────────┴── Cancelled / Escalated
```

### Priority Levels
`Normal → High → Urgent → Vip`

### SLA Tracking
| Status | Description |
|--------|-------------|
| `OnTrack` | Within SLA deadline |
| `AtRisk` | Nearing SLA deadline (80%+) |
| `Breached` | Past SLA deadline |

### Service Types (15)
`WakeUpCall`, `AirportTransfer`, `Taxi`, `Limousine`, `Luggage`, `BellService`, `PackageHandling`, `MailMessage`, `SpecialRequest`, `VipService`, `RestaurantBooking`, `SpaBooking`, `TourBooking`, `TicketBooking`, `Other`

---

## 3. Wake-Up Call

### Lifecycle
```
Scheduled → InProgress → Completed
               │
               ├── NoAnswer → Retry (up to 3x)
               │
               └── Failed / Cancelled
```

### Dashboard
```
ScheduledToday | Completed | NoAnswer | Failed | Pending | CompletionRate
```

---

## 4. Luggage Management

### Status Flow
```
Stored → InTransit → Delivered / Collected
                         │
                         └── Lost (escalation)
```

### Key Features
- **Tag Number**: Auto-generated tracking ID
- **Storage Location**: Tracked per item
- **Pieces**: Count + description
- **History**: Store → Deliver → Collect audit trail

---

## 5. Transportation

### Transport Types
`AirportPickup` | `AirportDropoff` | `Taxi` | `Limousine` | `Shuttle` | `PrivateDriver`

### Booking Details
```csharp
TransportationDto(BookingId, BookingNumber, Type, GuestId, GuestName, StayId,
                  RoomNumber, DriverName, VehicleNumber, VehicleType,
                  ScheduledAtUtc, PickupLocation, DropoffLocation,
                  FlightNumber, PassengerCount, Status, Cost, Notes)
```

---

## 6. Events (11)

| Event | Trigger |
|-------|---------|
| `ServiceRequested` | New service request created |
| `ServiceAssigned` | Staff assigned |
| `ServiceCompleted` | Service finished |
| `ServiceEscalated` | Service escalated |
| `WakeupScheduled` | Wake-up call set |
| `WakeupCompleted` | Call answered |
| `WakeupNoAnswer` | Guest didn't answer |
| `LuggageStored` | Luggage checked in |
| `LuggageDelivered` | Luggage delivered to room |
| `TransportationBooked` | Transport arranged |
| `SlaBreached` | SLA deadline missed |

---

## 7. Dashboards (2)

| Dashboard | Key Metrics |
|-----------|-------------|
| Concierge | Open requests, Assigned/Completed today, Wakeups/Luggage/Transport counts, Avg response time, SLA%, Urgent/Breached lists |
| Service Performance | By type: Volume, Avg rating, Completion rate, Avg minutes, SLA compliance |

---

## 8. Integration Map

| Module | Integration |
|--------|------------|
| **Guest** | Guest profile lookup, VIP detection |
| **Stay** | Room number, in-house status |
| **Housekeeping** | Luggage delivery coordination |
| **Payment** | Transport/luggage charges |
| **Workflow (EP-001)** | Service request workflow |
| **Rule Engine (EP-002)** | SLA thresholds, escalation rules |
| **Event Bus (FO-011)** | 11 concierge events |
| **Observability (EP-003)** | Service response time, SLA metrics |

---

## 9. Tests (12)

| Test | Description |
|------|-------------|
| CON_001 | Create VIP service request |
| CON_002 | Assign service |
| CON_003 | Complete service |
| CON_004 | SLA report metrics |
| CON_005 | Schedule wake-up |
| CON_006 | Complete wake-up |
| CON_007 | Retry wake-up |
| CON_008 | Store luggage with tag |
| CON_009 | Deliver luggage |
| CON_010 | Book transport |
| CON_011 | Concierge dashboard |
| CON_012 | Events unique |

---

## 10. Files Changed

| File | Action | Lines |
|------|--------|-------|
| `Application/FrontOffice/Concierge/ConciergeContracts.cs` | NEW — 5 services, DTOs, 11 events, 2 dashboards | 140 |
| `Infrastructure/FrontOffice/Concierge/ConciergeEngine.cs` | NEW — 5 implementations | 80 |
| `Tests.Unit/ConciergeTests.cs` | NEW — 12 tests | 30 |

**Total:** 3 new files

---

## 11. Build & Test

| Check | Result |
|-------|--------|
| Debug Build | PASS (0 errors) |
| Concierge Tests | 12/12 PASS |

---

*Enterprise Concierge & Guest Services Platform complete. 7-state service workflow, 15 service types, SLA tracking, wake-up call with retry, luggage management with tagging, transportation booking, 2 dashboards, 11 events. Build 0 errors, 12/12 tests pass.*
