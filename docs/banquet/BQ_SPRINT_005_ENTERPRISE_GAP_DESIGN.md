# BQ-SPRINT-005 — ENTERPRISE GAP DESIGN

**Ngày:** 2026-07-21
**Loại:** ENTERPRISE DESIGN ONLY — NO CODE CHANGE
**Trạng thái:** COMPLETED ✅

---

## 1. Current State Summary (from Sprints 001-004)

| Finding | Source |
|---------|--------|
| `BanquetEvent` is the central aggregate | Audit |
| 7-state status machine: Draft→Hold→Confirmed→Deposited→Preparing→Completed/Cancelled | BQ-004 |
| `Total = Menu + Service` (no room rental, no discount) | BQ-003 |
| Contracts auto-sync dynamically, no snapshot | BQ-003 |
| Only `TableCount` + `ExtraTableCount` used in formulas | BQ-003 |
| Status-based validation (no approval workflow) | BQ-004 |
| No FO platform integration (Workflow, EventBus, RuleEngine available) | BQ-004 |

---

## 2. Gap 1: Room Rental

### Current State
Room rental does NOT exist in the Banquet domain. `BanquetRoom` has `Code`, `Name`, `Capacity`, `MaxTables` but no pricing fields.

### Enterprise Solution

Add `BanquetRoomRental` as a child entity of `BanquetEvent`:

```csharp
public sealed class BanquetRoomRental
{
    Guid Id;
    Guid BanquetEventId;
    Guid BanquetRoomId;
    RentalMode Mode;          // FlatFee, HourlyRate, HalfDay, FullDay, IncludedInPackage, Free
    decimal BaseRate;         // Base price per unit
    int Units;                // Hours, Days, etc.
    decimal TotalAmount;      // BaseRate × Units
    TimeSpan StartTime;       // Rental start
    TimeSpan EndTime;         // Rental end
    decimal? SeasonalMultiplier; // ×1.5 for holidays
    bool IsIncludedInPackage;    // Free when menu package covers it
}
```

### Pricing Modes
| Mode | Formula |
|------|---------|
| `FlatFee` | Fixed price |
| `HourlyRate` | BaseRate × Hours |
| `HalfDay` | BaseRate (≤ 4 hours) |
| `FullDay` | BaseRate (≤ 12 hours) |
| `IncludedInPackage` | 0 (covered by menu) |
| `Free` | 0 |

### Seasonal Pricing
```csharp
BanquetSeasonalPrice(DateTime From, DateTime To, decimal Multiplier, string Name)
// Example: Tet 2027: ×1.5, Christmas: ×1.3
```

### Financial Impact
```
New Total = Menu + Service + RoomRental - Discount
```

### Pros
- Simple additive model, no breaking changes
- Seasonal pricing reuses existing date-range pattern
- Room rental can be 0 (backward compatible)

### Cons
- New entity + migration required
- Existing events have 0 room rental (no data migration needed)

### Migration Risk: LOW (additive only)
### Regression Risk: LOW (existing Total formula still works)
### Priority: P1

---

## 3. Gap 2: Discount Engine

### Current State
No discount mechanism exists in Banquet. The financial formula has no subtraction for discounts.

### Enterprise Solution

Add `BanquetDiscount` as a child entity of `BanquetEvent`:

```csharp
public sealed class BanquetDiscount
{
    Guid Id;
    Guid BanquetEventId;
    DiscountType Type;          // Percentage, FixedAmount, Promotion, Voucher, Campaign, Manual
    decimal Value;              // Amount or %
    decimal CalculatedAmount;   // Type = Fixed: Value; Type = %: Total × Value/100
    string? VoucherCode;
    string? CampaignCode;
    bool RequiresApproval;      // Manager approval needed
    ApprovalStatus Status;      // Pending, Approved, Rejected
    string? ApprovedBy;
    DateTime? ApprovedAtUtc;
    string? Reason;
}
```

### Discount Types
| Type | Formula | Example |
|------|---------|---------|
| `Percentage` | Total × Value / 100 | 10% off = Total × 0.10 |
| `FixedAmount` | Value | Flat 5,000,000 VND off |
| `Promotion` | Auto from campaign | "Book 30 days early: 5% off" |
| `Voucher` | Linked to voucher code | "WELCOME2026: -2,000,000" |
| `Campaign` | Bulk discount | "5 events: 8% off" |
| `Manual` | Manager override | Negotiated discount |

### Financial Impact
```
New Total = Menu + Service + RoomRental - Σ(BanquetDiscount.CalculatedAmount)
```

### Pros
- Each discount is individually tracked and auditable
- Approval workflow for large discounts
- Voucher/campaign codes enable marketing automation
- Reuses EP-002 RuleEngine for automatic discount rules

### Cons
- Can make Total negative (needs guard: `Max(0, totalAfterDiscount)`)
- Multiple discounts could overlap (needs priority ordering)

### Migration Risk: LOW (additive only, default = 0 discount)
### Regression Risk: LOW
### Priority: P1

---

## 4. Gap 3: Contract Snapshot

### Current State
Contracts auto-sync with event DTO. Every menu/service change updates the contract. No version history.

### Enterprise Solution

Add `BanquetContractVersion` as a snapshot entity:

```csharp
public sealed class BanquetContractVersion
{
    Guid Id;
    Guid BanquetContractId;
    int VersionNumber;          // 1, 2, 3...
    decimal TotalAmount;        // Snapshot at version time
    decimal MenuAmount;
    decimal ServiceAmount;
    decimal RoomRentalAmount;
    decimal DiscountAmount;
    decimal DepositAmount;
    decimal RemainingAmount;
    string SnapshotJson;        // Full event state at version time
    string ChangeReason;        // "Menu changed", "Guest count updated"
    Guid? ChangedBy;
    DateTime CreatedAtUtc;
}
```

### Contract Status Flow (Redesigned)
```
Draft ──► Confirmed ──► Signed ──► Settled
               │            │
               │            ▼
               │      [FROZEN — no more auto-sync]
               │
               ▼
          (auto-sync active)
```

### Sync Rules
| Contract Status | Sync Behavior |
|-----------------|---------------|
| `Draft` | Auto-sync from event |
| `Confirmed` | Auto-sync from event |
| `Signed` | **FROZEN** — snapshot taken, no auto-sync |
| `Settled` | **FROZEN** — terminal |

### Version History
- Each save creates a `BanquetContractVersion` record
- Version history viewable in UI
- Rollback to previous version (manager only)

### Pros
- Legal compliance — signed contract is immutable
- Full audit trail with version history
- Backward compatible — existing contracts stay as Draft/Confirmed

### Cons
- `BanquetContractVersion` is a new entity + table
- Existing contracts have no versions (can be backfilled)
- UI needs "Sign" button + version viewer

### Migration Risk: MEDIUM (new entity, behavior change for signed contracts)
### Regression Risk: MEDIUM (signing flow must be tested)
### Priority: P1

---

## 5. Gap 4: Table Model

### Current State
Only `TableCount` + `ExtraTableCount` used. No Reserve/Actual/Complimentary/VIP table tracking.

### Enterprise Solution

Extend `BanquetEvent` with table breakdown:

```csharp
// On BanquetEvent entity:
int MainTableCount;           // Confirmed main tables (was TableCount)
int ReserveTableCount;        // Backup tables (not charged until used)
int ActualTableCount;         // Tables actually set up
int ComplimentaryTableCount;  // Free tables (host, organizer)
int VipTableCount;            // VIP tables
int ExtraTableCount;          // Added on the day (existing field)

// Computed:
int TotalTableSetup => ActualTableCount + ComplimentaryTableCount + VipTableCount;
int BillableTableCount => ActualTableCount;  // Only actual tables are charged
```

### Pricing Logic
```
Menu Amount = BillableTableCount × Menu Price Per Table
            + ExtraTableCount × Menu Price Per Table (same day additions)
// Reserve, Complimentary, VIP tables: $0
```

### Pros
- Accurate billing — only charge for actual tables used
- VIP/Complimentary tracking for reporting
- Reserve tables enable overbooking protection
- Backward compatible — `TableCount` maps to `MainTableCount`

### Cons
- Changes calculation formula (regression risk)
- UI needs new table input fields
- Staff training needed on table types

### Migration Risk: MEDIUM (formula change)
### Regression Risk: MEDIUM (financial calculation changes)
### Priority: P2

---

## 6. Gap 5: Approval Workflow

### Current State
Status transitions are direct (Draft → Confirmed by any authorized user). No approval chain.

### Enterprise Solution

Use **EP-001 Workflow & Saga Platform** for approval routing:

```csharp
// Workflow Definition
WorkflowType: "Banquet.EventApproval"

States:
  Pending → SalesReview → ManagerReview → AccountingReview → DirectorApproval → Approved

Steps:
  1. SalesReview: Verify menu, services, room assignment
  2. ManagerReview: Verify pricing, discount within policy
  3. AccountingReview: Verify deposit, payment terms, contract
  4. DirectorApproval: Final sign-off (required for > 100M VND)
```

### Approval Rules (via EP-002 RuleEngine)
| Condition | Approval Required |
|-----------|-------------------|
| Total < 50M | Sales only |
| Total 50M-100M | Sales + Manager |
| Total > 100M | Sales + Manager + Director |
| Discount > 10% | Manager + Director |
| VIP Customer | Sales + Manager |
| New Customer | Sales + Manager + Accounting |

### Escalation Rules
- Pending > 24h: Auto-escalate to next level
- Pending > 48h: Notify Director

### Pros
- Reuses EP-001 (Workflow) and EP-002 (RuleEngine) — no new infrastructure
- Audit trail built-in via ProcessManager
- Manual action support for approval/rejection

### Cons
- Integration with EP-001/002 required
- May slow down small events (configurable bypass)

### Migration Risk: MEDIUM (status machine change)
### Regression Risk: LOW (backward compatible via direct transition)
### Priority: P2

---

## 7. Gap 6: Front Office Integration

### Current State
Banquet operates independently. No integration with Front Office Reservation, Guest, Folio, Payment, Inventory.

### Enterprise Solution — Event-Driven via FO-011 EventBus

### Integration Map

| Banquet Event | FO Module | Action |
|---------------|-----------|--------|
| `BanquetEvent.Confirmed` | FO Reservation | Create room block (if guest needs hotel rooms) |
| `BanquetEvent.Confirmed` | FO Guest | Create/update guest profile |
| `BanquetDeposit.Paid` | FO Payment | Create payment transaction in Folio |
| `BanquetDeposit.Refunded` | FO Payment | Create refund transaction |
| `BanquetPurchaseRequest.Created` | Inventory | Auto-generate purchase order |
| `BanquetEvent.Completed` | FO Guest | Update guest history, loyalty points |
| `BanquetContract.Signed` | CRM | Update customer contract history |
| `BanquetQuantification.Created` | AI | Training data for menu recommendation |

### Technical Design

```
BanquetRestaurantService
     │
     ▼
Publish via IEventBus (FO-011)
     │
     ├──► FO.Reservation (create room block via Allocation Engine)
     ├──► FO.Guest (update GuestProfile)
     ├──► FO.Folio (create payment posting)
     ├──► FO.Inventory (create purchase order)
     ├──► CRM (update customer history)
     └──► AI (training data pipeline)
```

### Events to Publish (10)
```csharp
BanquetEvents.EventCreated
BanquetEvents.EventConfirmed
BanquetEvents.DepositReceived
BanquetEvents.DepositRefunded
BanquetEvents.ContractSigned
BanquetEvents.EventCompleted
BanquetEvents.EventCancelled
BanquetEvents.PurchaseRequestCreated
BanquetEvents.QuantificationCompleted
BanquetEvents.MenuChanged
```

### Pros
- Loose coupling via EventBus — no direct dependencies
- Each integration is independently deployable
- Reuses FO-011 EventBus + FO-012 Guest + EP-001 Workflow

### Cons
- Requires EventBus infrastructure (already built in FO-011)
- Event payload design needed
- Testing across modules

### Migration Risk: LOW (events are additive)
### Regression Risk: LOW (publishing events doesn't change behavior)
### Priority: P3

---

## 8. Recommended Architecture

```
BanquetEvent (Aggregate Root)
  ├── BanquetEventMenu[N]         (existing)
  ├── BanquetServiceLine[N]       (existing)
  ├── BanquetDeposit[N]           (existing)
  ├── BanquetContract[N]          (existing)
  │     └── BanquetContractVersion[N]  (NEW — snapshot)
  ├── BanquetRoomRental[N]        (NEW — room rental)
  ├── BanquetDiscount[N]          (NEW — discount engine)
  ├── BanquetChecklist[N]         (existing)
  ├── BanquetTimeline[N]          (existing)
  ├── BanquetFloorPlan[N]         (existing)
  ├── BanquetQuantificationBatch[N] (existing)
  └── BanquetPurchaseRequest[N]   (existing)

Platform Integration:
  ├── EP-001 Workflow (approval)
  ├── EP-002 RuleEngine (discount rules, approval thresholds)
  ├── EP-003 Observability (metrics)
  └── FO-011 EventBus (FO integration events)
```

---

## 9. Recommended Roadmap

### Phase 1 — P1 (1-2 sprints)
1. Room Rental entity + migration
2. Discount Engine entity + migration
3. Contract Snapshot + version history
4. Update Total formula: `Menu + Service + RoomRental - Discount`

### Phase 2 — P2 (2-3 sprints)
5. Table Model: Main/Reserve/Actual/Complimentary/VIP
6. Approval Workflow via EP-001 + EP-002
7. Update pricing logic for new table model

### Phase 3 — P3 (2-3 sprints)
8. FO Integration via EventBus (Reservation, Guest, Folio, Payment)
9. Inventory & CRM integration
10. AI training data pipeline

### Phase 4 — Platform (ongoing)
11. Observability metrics
12. Dashboard enhancement
13. Contract template engine
14. Multi-property banquet management

---

## 10. Build & Test (Current Baseline)

| Check | Result |
|-------|--------|
| Debug Build | PASS (0 errors) |
| BQ Regression Tests | 32/32 PASS |
| Existing Banquet Tests | 8/8 PASS |

---

**BQ-SPRINT-005 COMPLETED AS ENTERPRISE DESIGN ONLY.**

**NO PRODUCTION SOURCE CODE WAS MODIFIED. NO MIGRATION WAS CREATED. NO DATABASE SCHEMA WAS CHANGED. NO API CONTRACT WAS CHANGED. NO USER INTERFACE WAS CHANGED.**

**ALL DESIGNS LEVERAGE EXISTING PLATFORM CAPABILITIES (EP-001, EP-002, EP-003, FO-011). NO NEW FRAMEWORKS PROPOSED.**
