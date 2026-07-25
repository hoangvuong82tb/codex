# FO MILESTONE 2 PHASE 2.4 REPORT — DEPARTURE FOUNDATION

> **Ngày**: 2026-07-21
> **Kết luận**: PHASE 2.4 GREEN

---

## 1. BUILD & TEST

| Metric | Value |
|--------|-------|
| Debug Build | PASS (0 errors) |
| HotelStay Tests | **61/61 ALL PASS** |
| New Departure Tests | 9 (7 domain + 2 application) |

---

## 2. DEPARTURE TYPE

```csharp
public enum DepartureType { Normal, Early, Late }
```

Auto-detected in `CheckOutAsync`:
- `ActualCheckOutAtUtc < ScheduledDepartureDate` → `Early`
- Otherwise → `Normal`

---

## 3. ENHANCED DEPARTURE PROPERTIES

| Property | Type | Purpose |
|----------|------|---------|
| `DepartureReason` | string? | Why guest is leaving (business complete, emergency, etc.) |
| `DepartureNotes` | string? | Additional checkout notes |
| `HasPendingCharges` | bool | Pending charges flag for Financial Engine (M3) |
| `HasOutstandingBalance` | bool | Outstanding balance flag for Financial Engine (M3) |
| `IsDepartureApproved` | bool | Departure authorized by front desk |

---

## 4. DEPARTURE OPERATIONS

| Operation | Method | Status Transition |
|-----------|--------|-------------------|
| Standard Check-Out | `MarkCheckedOut()` | InHouse*/ExpectedDeparture → CheckedOut |
| Early Departure | `MarkEarlyDeparture()` | InHouse* → CheckedOut |
| Expected Departure | `MarkExpectedDeparture()` | InHouse* → ExpectedDeparture |
| Late Check-Out Request | `RequestLateCheckout()` | (no status change, sets LateCheckoutUntilUtc) |
| Departure Approval | `ApproveDeparture()` | (no status change, sets IsDepartureApproved) |
| Complete | `Complete()` | CheckedOut → Completed |

\* InHouse, CheckedIn, RoomMoved, Extended

---

## 5. ENHANCED CHECKOUT COMMAND

```csharp
public sealed record CheckOutHotelStayCommand(
    Guid HotelStayId,
    byte[] ExpectedRowVersion,
    DateTime? ActualCheckOutAtUtc = null,
    string? DepartureReason = null,
    string? DepartureNotes = null,
    bool HasPendingCharges = false,
    bool HasOutstandingBalance = false
) : ICommand<HotelStayDto>;
```

---

## 6. DOMAIN EVENTS

| Event | Trigger |
|-------|---------|
| `StayCheckedOut` | Standard checkout with departure type, reason, pending charges/balance flags |
| `StayEarlyDeparture` | Early departure with scheduled vs actual date |
| `StayExpectedDeparture` | Night audit marks today as departure day |
| `StayDepartureApproved` | Departure authorized by front desk |
| `StayLateCheckoutRequested` | Guest requests late checkout time |
| `StayCompleted` | Post-checkout processing complete |

---

## 7. STATE MACHINE (FULL)

| From | To | Method | Guard |
|------|----|--------|-------|
| PendingArrival | PreArrival | `PreCheckIn()` / `ConfirmPreArrival()` | Identity verified |
| PendingArrival | Cancelled | `CancelPreparation()` | — |
| PreArrival | InHouse | `CheckIn()` | Guest + Room valid |
| PreArrival | Cancelled | `CancelPreparation()` | — |
| InHouse* | RoomMoved | `MoveRoom()` | Target room valid |
| InHouse* | Extended | `ExtendStay()` | New date > current |
| InHouse* | ExpectedDeparture | `MarkExpectedDeparture()` | — |
| InHouse* | CheckedOut | `MarkCheckedOut()` / `MarkEarlyDeparture()` | — |
| ExpectedDeparture | CheckedOut | `MarkCheckedOut()` | — |
| CheckedOut | Completed | `Complete()` | — |
| CheckedOut | CheckedOut | `Reopen()` | — |
| Completed | Archived | `Archive()` | — |
| Completed | CheckedOut | `Reopen()` | — |

\* InHouse* = InHouse, CheckedIn, RoomMoved, Extended

---

## 8. FINANCIAL ENGINE INTERFACE (Milestone 3)

Các điểm mở rộng cho Financial Engine:

| Hook | Where | Purpose |
|------|-------|---------|
| `HasPendingCharges` | CheckOut command | If true, M3 blocks checkout until charges resolved |
| `HasOutstandingBalance` | CheckOut command | If true, M3 requires payment before allowing checkout |
| `DepartureReason` | CheckOut event | M3 Folio auto-generates final bill on checkout |
| `StayCheckedOut` event | Domain event | M3 listens to trigger folio finalization |
| `IsDepartureApproved` | CheckOut validation | M3 checks approval before processing final payment |

---

## 9. FILES MODIFIED

| File | Changes |
|------|---------|
| `Core/FrontOffice/Stay/StayTypes.cs` | +`DepartureType` enum |
| `Core/FrontOffice/Stay/HotelStay.cs` | +5 properties, enhanced `MarkCheckedOut()`, `MarkEarlyDeparture()`, +`ApproveDeparture()` |
| `Application/FrontOffice/Stay/HotelStayContracts.cs` | Enhanced `CheckOutHotelStayCommand` with 4 new fields |
| `Application/FrontOffice/Stay/HotelStayHandlers.cs` | Enhanced `CheckOutAsync` with departure type detection |
| `Tests.Unit/HotelStayFoundationTests.cs` | +9 tests (DEPARTURE_DOM 7, DEPARTURE_APP 2) |

---

**PHASE 2.4 GREEN — DEPARTURE FOUNDATION READY**
