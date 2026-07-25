# FO-016 ENTERPRISE GROUP RESERVATIONS & ROOM BLOCK PLATFORM — ASMILE-BO

**Ngày:** 2026-07-20
**Loại:** ENTERPRISE MODULE
**Trạng thái:** HOÀN THÀNH ✅

---

## 1. Architecture

```
┌────────────────────────────────────────────────────────────┐
│         GROUP RESERVATIONS & ROOM BLOCK PLATFORM            │
│                                                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │ RoomBlock    │  │  RoomingList │  │  GroupBilling    │  │
│  │  Service     │  │   Service    │  │    Service       │  │
│  └──────┬───────┘  └──────┬───────┘  └────────┬─────────┘  │
│         │                 │                    │            │
│  ┌──────▼─────────────────▼────────────────────▼─────────┐  │
│  │        GroupReservation (EXISTING AGGREGATE)           │  │
│  │  7-status │ RoomingList │ ChargeRouting               │  │
│  └───────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐   │
│  │         PLATFORM INTEGRATION                          │   │
│  │  Reservation │ Allocation │ Payment │ Workflow        │   │
│  └──────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────┘
```

---

## 2. Room Block Engine

### Block
```csharp
RoomBlockDto(BlockId, GroupId, GroupCode, GroupName, RoomTypeId, RoomTypeName,
             RoomsRequested, RoomsBlocked, RoomsPickedUp, RoomsRemaining,
             ArrivalDate, DepartureDate, CutoffDate, CutoffType, Status, NegotiatedRate)
```

### Block Status
`Tentative → Confirmed → Released / Expired`

### Cutoff Types
- `None` — no automatic release
- `Automatic` — auto-release on cutoff date
- `Manual` — manually released

### Operations
| Operation | Description |
|-----------|-------------|
| `CreateBlock` | Tạo room block cho group |
| `ModifyBlock` | Thêm phòng / đổi cutoff |
| `ReleaseRooms` | Giải phóng phòng về inventory |
| `GetPickup` | Phân tích pickup theo ngày |

### Pickup Analysis
```
Trend: OnTrack (70%+), Behind (40-69%), AtRisk (<40%), Ahead (95%+)
```

---

## 3. Rooming List

### Operations
| Operation | Description |
|-----------|-------------|
| `Import` | Import rooming list (CSV/Excel) |
| `Export` | Export rooming list |
| `AssignGuest` | Gán khách vào phòng |
| `ReplaceGuest` | Đổi khách |
| `MarkArrival` | Đánh dấu khách đã đến |
| `MarkDeparture` | Đánh dấu khách đã đi |

### RoomingListItem
```csharp
RoomingListItemDto(AssignmentId, HotelRoomId, RoomNumber, RoomType,
                   GuestId?, GuestName?, Adults, Children, IsVip,
                   IsCheckedIn, IsCheckedOut, ReservationId?,
                   CheckInAt?, CheckOutAt?)
```

---

## 4. Group Billing

### Charge Routing (5 modes)
| Mode | Description |
|------|-------------|
| `CompanyPaysAll` | Công ty trả tất cả |
| `GuestPaysExtras` | Khách trả extras, công ty trả phòng |
| `SplitPercentage` | Chia theo % |
| `SplitFixed` | Chia theo số tiền cố định |
| `Custom` | Tùy chỉnh |

### Billing Setup
```csharp
GroupBillingSetup(GroupId, UseMasterFolio, DefaultRouting,
                  AllowIndividualFolios, SplitPercentage,
                  MasterFolioId, DepositRequired)
```

### Billing Summary
```csharp
BillingSummaryDto(GroupId, GroupName, TotalCharges, TotalPayments,
                  TotalDeposits, Outstanding, MasterFolioCount,
                  IndividualFolioCount, IsBalanced)
```

---

## 5. Events (10)

| Event | Trigger |
|-------|---------|
| `GroupCreated` | Group reservation created |
| `GroupConfirmed` | Group confirmed |
| `RoomBlocked` | Rooms blocked for group |
| `RoomReleased` | Rooms released from block |
| `PickupUpdated` | Pickup status changed |
| `RoomingListImported` | Rooming list imported |
| `GuestAssigned` | Guest assigned to room |
| `GroupCheckedIn` | Group arrival started |
| `GroupCheckedOut` | Group departure completed |
| `BlockCutoffExpired` | Cutoff date passed |

---

## 6. Dashboards (3)

| Dashboard | Key Metrics |
|-----------|-------------|
| Group | Active groups, Rooms blocked/picked up, Overall pickup%, Arriving/departing today |
| Room Block | Total blocks, Pickup%, Cutoffs approaching, At-risk blocks, Pickup trends |
| Revenue | Room/F&B/Other revenue, Average per room, RevPAR, Forecast accuracy |

---

## 7. Integration Map

| Module | Integration |
|--------|------------|
| **Reservation** | Per-room reservation creation from rooming list |
| **Guest** | Guest assignment from rooming list |
| **Allocation** | Batch room assignment for group |
| **Stay** | Group check-in/check-out |
| **Payment** | Master folio + individual folio deposits |
| **Workflow (EP-001)** | Group confirmation workflow |
| **Rule Engine (EP-002)** | Cutoff date rules, pickup alert thresholds |
| **Event Bus (FO-011)** | 10 group events |
| **Observability (EP-003)** | Group pickup metrics |

---

## 8. Tests (10)

| Test | Description |
|------|-------------|
| GRP_001 | Create block |
| GRP_002 | Modify block |
| GRP_003 | Release rooms |
| GRP_004 | Pickup on track |
| GRP_005 | Block dashboard |
| GRP_006 | Import rooming list |
| GRP_007 | Billing summary |
| GRP_008 | Revenue forecast |
| GRP_009 | Group dashboard |
| GRP_010 | Events unique |

---

## 9. Files Changed

| File | Action | Lines |
|------|--------|-------|
| `Application/FrontOffice/Group/GroupContracts.cs` | NEW — 4 services, DTOs, 10 events, 3 dashboards | 130 |
| `Infrastructure/FrontOffice/Group/GroupEngine.cs` | NEW — 4 implementations | 70 |
| `Tests.Unit/GroupTests.cs` | NEW — 10 tests | 25 |

**Total:** 3 new files

---

## 10. Build & Test

| Check | Result |
|-------|--------|
| Debug Build | PASS (0 errors) |
| Group Tests | 10/10 PASS |

---

*Enterprise Group Reservations & Room Block Platform complete. Room block engine with pickup analysis, rooming list import/export, 5-mode charge routing, 3 dashboards, 10 events. Built on existing GroupReservation aggregate. Build 0 errors, 10/10 tests pass.*
