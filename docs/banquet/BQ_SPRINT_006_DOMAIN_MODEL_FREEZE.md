# BQ-SPRINT-006 — ENTERPRISE DOMAIN MODEL FREEZE

**Ngày:** 2026-07-21
**Loại:** ARCHITECTURE ONLY — NO IMPLEMENTATION
**Trạng thái:** COMPLETED ✅

---

## 1. Architecture Principles

| # | Principle | Rationale |
|---|-----------|-----------|
| 1 | **Single Aggregate Root** — `BanquetEvent` owns its children | Consistent transactional boundary |
| 2 | **Reference by ID** for cross-aggregate relationships | Avoid large object graphs |
| 3 | **Event-Driven** for cross-module integration | Loose coupling, independent deployability |
| 4 | **Platform-First** — reuse EP-001/002/003 and FO EventBus | No new infrastructure |
| 5 | **Additive Only** — new entities extend, never replace | Zero regression on existing behavior |
| 6 | **Backward Compatible** — existing API contracts preserved | All new fields default to null/0 |
| 7 | **One Migration per Entity** — no multi-step migrations | Clean rollback path |

---

## 2. Aggregate Boundary (DDD Analysis)

### BanquetEvent — Aggregate Root

**Owns (Child Entities — cascaded, loaded eagerly):**

| Child Entity | Relationship | Reason |
|-------------|-------------|--------|
| `BanquetEventMenu[N]` | 1:N Owned | Cannot exist without event; event defines pricing context |
| `BanquetServiceLine[N]` | 1:N Owned | Cannot exist without event; total depends on event |
| `BanquetDeposit[N]` | 1:N Owned | Financial child; sum contributes to event total |
| `BanquetChecklist[N]` | 1:N Owned | Operational child; event-scoped |
| `BanquetTimeline[N]` | 1:N Owned | Operational child; event-scoped |
| `BanquetRoomRental[N]` | 1:N Owned | NEW — financial child; contributes to total |
| `BanquetDiscount[N]` | 1:N Owned | NEW — financial child; subtracts from total |
| `BanquetFloorPlan[N]` | 1:N Owned | Layout child; event-scoped |

**References by ID (Independent Aggregates):**

| Entity | Relationship | Reason |
|--------|-------------|--------|
| `BanquetRoom` | N:1 Reference | Independent lifecycle; rooms exist without events |
| `BanquetMenuPackage` | N:1 Reference | Reusable template; shared across events |
| `Customer` | N:1 Reference | Independent aggregate; shared across modules |

**Separate Aggregates (own roots):**

| Aggregate | Reason |
|-----------|--------|
| `BanquetContract` | Has its own lifecycle (Draft→Signed→Settled); independently auditable |
| `BanquetQuantificationBatch` | Complex calculation engine; versioned; large data |
| `BanquetPurchaseRequest` | Procurement lifecycle; links to Inventory |

### DDD Rationale

- **`BanquetContract`** is a separate aggregate because:
  - Has independent lifecycle (signing, settlement, legal freeze)
  - May exist after event is deleted (legal retention)
  - Has its own version history
  - Reference: `BanquetContract.BanquetEventId`

- **`BanquetMenuPackage`** is a separate aggregate because:
  - Defined once, reused across many events
  - Has independent CRUD lifecycle
  - Reference: `BanquetEventMenu.BanquetMenuPackageId`

- **`Customer`** is a separate aggregate because:
  - Shared across Banquet, Alacarte, FO
  - Has independent lifecycle (created via FO or Banquet)

---

## 3. Entity Relationship Diagram

```
┌──────────────────────┐
│      Customer        │ (Independent Aggregate)
│  Id, Name, Phone...  │
└──────────┬───────────┘
           │ 1:N Reference
           ▼
┌──────────────────────────────────────────────────────────┐
│                   BanquetEvent (Aggregate Root)           │
│  Id, EventNo, EventType, Status, GuestCount, TableCount,  │
│  MainTableCount, ReserveTableCount, ActualTableCount,     │
│  ComplimentaryTableCount, VipTableCount, ExtraTableCount, │
│  IsFinanciallyLocked, RowVersion                          │
│                                                           │
│  Owned Children (1:N, cascaded):                          │
│  ┌─────────────────────┐  ┌────────────────────────────┐ │
│  │BanquetEventMenu[N]  │  │BanquetServiceLine[N]       │ │
│  │ MenuItemId, Qty...  │  │ ServiceType, Qty, Price... │ │
│  └─────────────────────┘  └────────────────────────────┘ │
│  ┌─────────────────────┐  ┌────────────────────────────┐ │
│  │BanquetDeposit[N]    │  │BanquetRoomRental[N] (NEW)  │ │
│  │ Amount, Method...   │  │ Mode, Rate, Units...       │ │
│  └─────────────────────┘  └────────────────────────────┘ │
│  ┌─────────────────────┐  ┌────────────────────────────┐ │
│  │BanquetDiscount[N](N)│  │BanquetFloorPlan[N]         │ │
│  │ Type, Value...      │  │ Canvas, Tables[]...        │ │
│  └─────────────────────┘  └────────────────────────────┘ │
│  ┌─────────────────────┐  ┌────────────────────────────┐ │
│  │BanquetChecklist[N]  │  │BanquetTimeline[N]          │ │
│  │ Title, IsDone...    │  │ Title, StartTime...        │ │
│  └─────────────────────┘  └────────────────────────────┘ │
└──────────────────────────────────────────────────────────┘
           │                          │
     N:1 Reference             1:N Reference
           ▼                          ▼
┌──────────────────┐    ┌──────────────────────────────┐
│   BanquetRoom    │    │   BanquetMenuPackage          │
│   (Independent)  │    │   (Independent Aggregate)     │
└──────────────────┘    └──────────────────────────────┘

Separate Aggregates (reference BanquetEvent by ID):

┌──────────────────────────────┐
│ BanquetContract              │
│ BanquetEventId (FK Reference) │
│ └── BanquetContractVersion[N]│
└──────────────────────────────┘

┌──────────────────────────────┐
│ BanquetQuantificationBatch   │
│ BanquetEventId (FK Reference) │
│ └── BanquetQuantificationLine│
└──────────────────────────────┘

┌──────────────────────────────┐
│ BanquetPurchaseRequest       │
│ BanquetEventId (FK Reference) │
│ └── BanquetPurchaseRequestLn │
└──────────────────────────────┘
```

---

## 4. Table Model Design

### Entity Definition

```csharp
// On BanquetEvent (existing + new fields):
int TableCount;              // [EXISTING] — renamed to MainTableCount
int MainTableCount;          // Confirmed main tables — billable
int ReserveTableCount;       // Backup — not billable until used
int ActualTableCount;        // Actually set up — billable
int ComplimentaryTableCount; // Free (host, organizer table)
int VipTableCount;           // VIP tables — premium pricing?
int ExtraTableCount;         // Same-day additions — billable
```

### Business Rules

| Rule | Description |
|------|-------------|
| `ActualTableCount ≥ MainTableCount` | Must set up at least main tables |
| `ReserveTableCount ≤ MainTableCount × 0.2` | Max 20% reserve |
| `ComplimentaryTableCount ≤ 2` | Max 2 free tables |
| `ExtraTableCount ≥ 0` | Can't be negative |

### Financial Rules

| Formula | Description |
|---------|-------------|
| `BillableTables = ActualTableCount + ExtraTableCount` | Only actual + extras are charged |
| `MenuAmount = BillableTables × MenuUnitPrice` | Per-table pricing |
| `ComplimentaryTables × Price = 0` | Free tables |
| `ReserveTables × Price = 0` | Not charged unless used |

---

## 5. Room Model Design

### Room Rental Entity (on BanquetEvent)

```csharp
public sealed class BanquetRoomRental
{
    Guid Id;
    Guid BanquetEventId;
    Guid BanquetRoomId;
    RentalMode Mode;              // FlatFee, Hourly, HalfDay, FullDay, IncludedInPkg, Free
    decimal BaseRate;
    int Units;                    // Days or Hours
    decimal TotalAmount;          // = BaseRate × Units × SeasonalMultiplier
    decimal SeasonalMultiplier;   // 1.0 = normal, 1.5 = Tet
    TimeSpan StartTime;
    TimeSpan EndTime;
    bool IsIncludedInPackage;
    string? Notes;
}
```

### Room Combinations

| Scenario | Implementation |
|----------|---------------|
| Single hall | 1 BanquetRoomRental |
| Split hall (A+B) | 2 BanquetRoomRental, same event |
| Combined hall (A+B merged) | 1 BanquetRoomRental for combined room |
| Multi-room event | N BanquetRoomRental |
| Outdoor event | BanquetRoomRental with Outdoor type |

### Room Conflict Check (enhanced)
```
Same RoomId + Same Date + Overlapping Time → Conflict
+ Room capacity: (GuestCount + ExtraGuestCount) ≤ Capacity
```

---

## 6. Discount Model Design

### Discount Entity (on BanquetEvent)

```csharp
public sealed class BanquetDiscount
{
    Guid Id;
    Guid BanquetEventId;
    DiscountType Type;              // Percentage, Fixed, Promotion, Voucher, Campaign, Manual
    decimal Value;                  // Amount or percentage
    DiscountTarget Target;          // MenuOnly, ServiceOnly, RoomOnly, Total, Custom
    decimal CalculatedAmount;       // Computed discount amount
    int Priority;                   // 1 = apply first
    bool IsStackable;               // Can combine with other discounts
    decimal? MaxDiscountAmount;     // Cap
    ApprovalStatus Status;          // Pending, Approved, Rejected
    string? VoucherCode;
    string? CampaignCode;
    string? ApprovedBy;
    DateTime? ApprovedAtUtc;
}
```

### Discount Targets

| Target | Applies To |
|--------|-----------|
| `MenuOnly` | MenuAmount |
| `ServiceOnly` | ServiceAmount |
| `RoomOnly` | RoomRental total |
| `Total` | Full event total |
| `Custom` | Specific line items |

### Stacking Rules

| Priority | Behavior |
|----------|----------|
| 1 (highest) | Applied first |
| Non-stackable | Blocks lower-priority discounts |
| Stackable | Allowed to combine |
| MaxDiscount | Per-discount cap |
| GlobalMaxDiscount | Per-event cap (e.g., max 20% off total) |

### Updated Total Formula
```
Total = MenuAmount + ServiceAmount + Σ(RoomRental.TotalAmount)
      - Σ(Discount.CalculatedAmount)
```

---

## 7. Contract Model Design

### Contract Aggregate (Separate from Event)

```csharp
public sealed class BanquetContract
{
    Guid Id;
    string ContractNo;
    Guid BanquetEventId;           // FK Reference
    ContractStatus Status;         // Draft, Confirmed, Signed, Settled, Cancelled
    decimal TotalAmount;           // Snapshot (frozen after Signed)
    decimal MenuAmount;
    decimal ServiceAmount;
    decimal RoomRentalAmount;
    decimal DiscountAmount;
    decimal DepositAmount;
    decimal RemainingAmount;
    DateTime? SignDate;
    string? SignedBy;
    string? SignedByName;
    string? LegalFreezeReason;     // "Court order", "Audit requirement"
    DateTime? LegalFreezeAtUtc;
    ICollection<BanquetContractVersion> Versions;
}
```

### Contract Version

```csharp
public sealed class BanquetContractVersion
{
    Guid Id;
    Guid BanquetContractId;
    int VersionNumber;             // Auto-increment
    string SnapshotJson;           // Full state at version time
    string ChangeType;             // "Created", "AmountChanged", "Signed", "Amended"
    string? ChangeReason;
    Guid? ChangedBy;
    DateTime CreatedAtUtc;
}
```

### Contract Lifecycle

```
Draft ──► Confirmed ──► Signed (FROZEN) ──► Settled
  │           │              │
  │           │              ├── Amendment (new version, re-freeze)
  │           │              └── LegalFreeze (immutable)
  │           │
  │           └── Cancelled
  │
  └── Cancelled
```

### Sync Rules

| Status | Sync with Event | Reason |
|--------|----------------|--------|
| `Draft` | ✅ Auto-sync | Event still changing |
| `Confirmed` | ✅ Auto-sync | Pre-signing adjustments allowed |
| `Signed` | ❌ **FROZEN** | Legal commitment — no auto-change |
| `Settled` | ❌ **FROZEN** | Financial close — immutable |
| `Cancelled` | ❌ No sync | Terminal |

---

## 8. Event Catalog

| # | Event | Trigger | Payload |
|---|-------|---------|---------|
| 1 | `Banquet.EventCreated` | SaveEventAsync | EventId, EventNo, EventType, CustomerName |
| 2 | `Banquet.EventConfirmed` | Status → Confirmed | EventId, TotalAmount, MenuCount |
| 3 | `Banquet.EventCancelled` | Status → Cancelled | EventId, Reason |
| 4 | `Banquet.EventCompleted` | Status → Completed | EventId, TotalRevenue |
| 5 | `Banquet.MenuAttached` | AttachMenuPackageAsync | EventId, MenuPackageId, Amount |
| 6 | `Banquet.MenuCustomized` | SaveCustomMenuAsync | EventId, LineCount |
| 7 | `Banquet.ServiceChanged` | SaveServiceLineAsync | EventId, ServiceType |
| 8 | `Banquet.DepositReceived` | AddDepositAsync | EventId, Amount, PaymentMethod |
| 9 | `Banquet.DepositRefunded` | RefundDepositAsync | EventId, RefundAmount |
| 10 | `Banquet.ContractCreated` | CreateContractAsync | EventId, ContractNo |
| 11 | `Banquet.ContractSigned` | Status → Signed | EventId, ContractNo, SignedBy |
| 12 | `Banquet.DiscountApplied` | Discount saved | EventId, Type, Value |
| 13 | `Banquet.DiscountApproved` | Discount approved | EventId, ApprovedBy |
| 14 | `Banquet.RoomRentalChanged` | RoomRental saved | EventId, RoomId, TotalAmount |
| 15 | `Banquet.QuantificationCreated` | Batch created | EventId, BatchId |
| 16 | `Banquet.PurchaseRequestCreated` | PR created | EventId, PRNo, EstimatedTotal |
| 17 | `Banquet.SettlementCompleted` | FinalizeSettlementAsync | EventId |
| 18 | `Banquet.FinancialLocked` | IsFinanciallyLocked = true | EventId |

---

## 9. Workflow Model (via EP-001)

### Approval Workflow Definition

```csharp
WorkflowType: "Banquet.EventApproval"
Version: 1

States:
  Pending ──► SalesReview ──► ManagerReview ──► DirectorApproval ──► Approved

Transitions:
  Pending → SalesReview (auto on event create)
  SalesReview → ManagerReview (sales person approves)
  SalesReview → Rejected (sales person rejects with reason)
  ManagerReview → DirectorApproval (manager approves)
  ManagerReview → Rejected
  DirectorApproval → Approved
  DirectorApproval → Rejected

Conditions:
  ManagerReview: Required when TotalAmount > 50,000,000 VND
  DirectorApproval: Required when TotalAmount > 100,000,000 VND OR DiscountAmount > 10%
  Bypass: TotalAmount < 50,000,000 → Auto-approve (skip Manager/Director)

Timeouts:
  SalesReview: 24h → escalate to Manager
  ManagerReview: 24h → escalate to Director
  DirectorApproval: 48h → escalate to GM

Manual Actions:
  Approve, Reject (require reason), Resubmit (after rejection)
```

### Compensation
- Rejected → event status returns to `Draft`
- All approvals revoked on event modification (re-submit)

---

## 10. Migration Strategy

| Gap | Entity | Migration | Backward Compat | Data Migration | Rollback |
|-----|--------|-----------|-----------------|----------------|----------|
| Room Rental | `BanquetRoomRental` | New table | ✅ Default null | None needed | Drop table |
| Discount | `BanquetDiscount` | New table | ✅ Default null | None needed | Drop table |
| Contract Snapshot | `BanquetContractVersion` | New table | ✅ 0 versions | None | Drop table |
| Table Model | New columns on `BanquetEvents` | ALTER TABLE | ✅ Default = 0 | Set MainTableCount = TableCount | Remove columns |
| Approval Workflow | Via EP-001 ProcessManager | No DB change | ✅ Existing | None | N/A |
| FO Integration | Via FO-011 EventBus | No DB change | ✅ Existing | None | N/A |

### Migration Order
1. Table Model columns (ALTER TABLE — safest)
2. Room Rental table (new, no dependencies)
3. Discount table (new, no dependencies)
4. Contract Version table (new, FK to Contract)
5. Approval Workflow (platform config — no DB)
6. FO Integration (event publishing — no DB)

---

## 11. Implementation Roadmap

### Epic 1: Discount Engine (2 sprints)
| Item | Detail |
|------|--------|
| Entity | `BanquetDiscount` |
| Migration | New table |
| Tests | 15 regression tests |
| Files | Entity, DTO, Service method, ViewModel |

### Epic 2: Room Rental (2 sprints)
| Item | Detail |
|------|--------|
| Entity | `BanquetRoomRental` |
| Migration | New table |
| Tests | 15 regression tests |
| Files | Entity, DTO, Service method, ViewModel |

### Epic 3: Contract Snapshot (2 sprints)
| Item | Detail |
|------|--------|
| Entity | `BanquetContractVersion` |
| Migration | New table |
| Contract freeze | Sync rules based on status |
| Tests | 20 regression tests |
| Files | Entity, DTO, Service, ViewModel |

### Epic 4: Table Model (2 sprints)
| Item | Detail |
|------|--------|
| Columns | MainTable, ReserveTable, ActualTable, Complimentary, VIP |
| Migration | ALTER TABLE ADD COLUMN |
| Formula update | BillableTables replaces TableCount |
| Tests | 20 regression tests |
| Files | Entity, DTO, Service, ViewModel |

### Epic 5: Approval Workflow (2 sprints)
| Item | Detail |
|------|--------|
| Platform | EP-001 Workflow definition |
| Rules | EP-002 RuleEngine conditions |
| Tests | 15 tests |
| Files | Workflow config, Service integration |

### Epic 6: FO Integration (2 sprints)
| Item | Detail |
|------|--------|
| Events | Publish 18 events via FO-011 |
| Handlers | FO-side subscription handlers |
| Tests | 10 integration tests |
| Files | Event publishing in Service, Handlers in FO modules |

---

## 12. Architecture Decision Record (ADR)

### ADR-001: BanquetContract is a Separate Aggregate
**Decision:** BanquetContract is NOT owned by BanquetEvent. It references BanquetEvent by ID.
**Rationale:** Contracts have independent lifecycle (legal retention, signing, settlement). Destroying an event should not cascade-delete contracts.
**Consequence:** Contract amounts are snapshot-based, not computed from event.

### ADR-002: BanquetMenuPackage is a Separate Aggregate
**Decision:** BanquetMenuPackage is shared across events as a reusable template.
**Rationale:** Defined once, referenced by many events. Independent CRUD.
**Consequence:** BanquetEventMenu stores a snapshot of package data at attachment time.

### ADR-003: Discount is Owned by BanquetEvent
**Decision:** BanquetDiscount is a child entity of BanquetEvent.
**Rationale:** Discounts are event-specific, no reuse case. Sum contributes to event total.
**Consequence:** Discounts are cascaded on event delete.

### ADR-004: RoomRental is Owned by BanquetEvent
**Decision:** BanquetRoomRental is a child entity of BanquetEvent.
**Rationale:** Rental is event-specific. Contributes to event total.
**Consequence:** Rental is cascaded on event delete.

### ADR-005: Approval via EP-001 Workflow
**Decision:** Banquet uses EP-001 Workflow Platform for approval, not custom code.
**Rationale:** EP-001 already provides state machine, timeout, escalation, manual actions.
**Consequence:** Banquet module depends on EP-001 platform.

---

## 13. Build & Test (Current Baseline)

| Check | Result |
|-------|--------|
| Debug Build | PASS (0 errors) |
| BQ Regression Tests | 32/32 PASS |
| Existing Banquet Tests | 8/8 PASS |
| Production Changes | **0** |

---

**BQ-SPRINT-006 COMPLETED AS DOMAIN MODEL FREEZE ONLY.**

**NO PRODUCTION SOURCE CODE WAS MODIFIED.**
**NO DATABASE CHANGES WERE MADE.**
**NO MIGRATIONS WERE CREATED.**
**THIS DOCUMENT DEFINES THE FINAL ENTERPRISE ARCHITECTURE BEFORE IMPLEMENTATION.**

**ALL 6 EPICS ARE DESIGNED WITH:**
- **Entity definitions**
- **Migration strategies**
- **Backward compatibility plans**
- **Rollback procedures**
- **Regression test counts**
- **File-level scope**

**6 ADRs DOCUMENT ALL KEY ARCHITECTURE DECISIONS.**

**18 DOMAIN EVENTS FORM THE INTEGRATION CATALOG.**

**1 AGGREGATE ROOT (BanquetEvent) WITH 8 CHILD ENTITIES + 3 REFERENCED AGGREGATES + 3 INDEPENDENT AGGREGATES.**
