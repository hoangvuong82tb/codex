# BQ-SPRINT-007 — IMPLEMENTATION MASTER PLAN

**Ngày:** 2026-07-21
**Loại:** MASTER PLAN ONLY — NO CODE CHANGE
**Trạng thái:** COMPLETED ✅

---

## 1. Epic Dependency Graph

```
Epic 1: Contract Snapshot (no deps)
     │
     ▼
Epic 2: Discount Engine (no deps, parallel with Epic 1)
     │
     ▼
Epic 3: Room Allocation & Rental (no deps, parallel with Epic 1-2)
     │
     ▼
Epic 4: Table Model (depends on Epic 2 — formula uses discount)
     │
     ▼
Epic 5: Approval Workflow (depends on Epic 1-4 — all entities must exist)
     │
     ▼
Epic 6: FO Integration (depends on Epic 1-5 — all domain events must be defined)
```

### Parallelizable Work
```
Phase 1 (parallel): Epic 1 + Epic 2 + Epic 3
Phase 2 (parallel with dep): Epic 4 (needs Epic 2)
Phase 3: Epic 5 (needs all entities)
Phase 4: Epic 6 (needs all events)
```

---

## 2. Epic 1 — Contract Snapshot

### Story Map

```
As a Banquet Manager
  I want contracts to freeze when signed
  So that financial terms are legally binding

├── Feature 1.1: Contract Version Entity
│   ├── US 1.1.1: Create BanquetContractVersion entity
│   │   └── Tasks: Entity file, migration, DTO, EF mapping
│   └── US 1.1.2: Auto-create version on contract save
│       └── Tasks: Service method, version increment, JSON snapshot
│
├── Feature 1.2: Contract Freeze
│   ├── US 1.2.1: Freeze contract on Sign
│   │   └── Tasks: Status check in SyncContractsAsync, skip frozen
│   └── US 1.2.2: Unfreeze for amendment
│       └── Tasks: Amendment status, new version on unfreeze
│
├── Feature 1.3: Version History UI
│   ├── US 1.3.1: View version history
│   │   └── Tasks: ViewModel property, XAML list
│   └── US 1.3.2: Compare versions
│       └── Tasks: Diff logic, comparison view
│
└── Feature 1.4: Legal Freeze
    └── US 1.4.1: Manager can legally freeze contract
        └── Tasks: LegalFreeze flag, reason field, audit log
```

### Sprint Breakdown — Epic 1

| Sprint | Stories | Files | Migration | Tests | Est. LOC |
|--------|---------|-------|-----------|-------|----------|
| **BQ-IMPL-1A** | US 1.1.1, US 1.1.2 | Entity, DTO, Service (30 lines) | 1 (new table) | 8 | ~200 |
| **BQ-IMPL-1B** | US 1.2.1, US 1.2.2 | Service (40 lines), ViewModel (40) | 0 | 8 | ~250 |
| **BQ-IMPL-1C** | US 1.3.1, US 1.3.2, US 1.4.1 | ViewModel (80), XAML (60) | 0 | 4 | ~350 |

### Files
| File | Action | Sprint |
|------|--------|--------|
| `Core/Entities/RestaurantEntities.cs` | +BanquetContractVersion | 1A |
| `Application/DTOs/RestaurantDtos.cs` | +BanquetContractVersionDto | 1A |
| `Infrastructure/Services/RestaurantServices.cs` | +CreateVersion, freeze logic | 1A |
| `App/ViewModels/BanquetRestaurantViewModel.Partial.cs` | +VersionHistory, CompareVersions | 1B-1C |
| `App/Views/Banquet/BanquetEventsPage.xaml` | +Version viewer | 1C |

### Rollback
- Drop `BanquetContractVersions` table
- Remove version creation from Service
- UI: hide version tab

---

## 3. Epic 2 — Discount Engine

### Story Map

```
As a Banquet Sales Person
  I want to apply discounts to events
  So that I can negotiate competitive pricing

├── Feature 2.1: Discount Entity
│   ├── US 2.1.1: Create BanquetDiscount entity
│   │   └── Tasks: Entity, migration, DTO, EF mapping, 6 discount types
│   └── US 2.1.2: Calculate discount amount
│       └── Tasks: Calculation per type (%, fixed, voucher, campaign)
│
├── Feature 2.2: Discount CRUD
│   ├── US 2.2.1: Add discount to event
│   │   └── Tasks: Service method, validation (max, stacking)
│   └── US 2.2.2: Remove discount
│       └── Tasks: Service method, audit log
│
├── Feature 2.3: Discount Approval
│   ├── US 2.3.1: Auto-approve small discounts (< 10%)
│   │   └── Tasks: Threshold rule, auto status
│   └── US 2.3.2: Request approval for large discounts
│       └── Tasks: Approval flag, notification
│
├── Feature 2.4: Update Total Formula
│   └── US 2.4.1: Total = Menu + Service + RoomRental − Discount
│       └── Tasks: Update ToDto formula, SyncContractsAsync
│
└── Feature 2.5: Discount UI
    └── US 2.5.1: Discount editor in event detail
        └── Tasks: ViewModel commands, XAML form
```

### Sprint Breakdown — Epic 2

| Sprint | Stories | Files | Migration | Tests | Est. LOC |
|--------|---------|-------|-----------|-------|----------|
| **BQ-IMPL-2A** | US 2.1.1, US 2.1.2 | Entity, DTO, Service (60) | 1 (new table) | 10 | ~300 |
| **BQ-IMPL-2B** | US 2.2.1, US 2.2.2, US 2.4.1 | Service (80), ToDto update | 0 | 10 | ~250 |
| **BQ-IMPL-2C** | US 2.3.1, US 2.3.2, US 2.5.1 | ViewModel (100), XAML (80) | 0 | 5 | ~400 |

### Files
| File | Action | Sprint |
|------|--------|--------|
| `Core/Entities/RestaurantEntities.cs` | +BanquetDiscount, DiscountType enum | 2A |
| `Application/DTOs/RestaurantDtos.cs` | +BanquetDiscountDto, SaveBanquetDiscountRequest | 2A |
| `Infrastructure/Services/RestaurantServices.cs` | +AddDiscount, RemoveDiscount, UpdateTotalFormula | 2A-2B |
| `App/ViewModels/BanquetRestaurantViewModel.Partial.cs` | +DiscountCommands, DiscountEditor | 2C |
| `App/Views/Banquet/BanquetEventsPage.xaml` | +Discount form | 2C |

### Rollback
- Drop `BanquetDiscounts` table
- Revert ToDto formula (remove discount subtraction)
- UI: hide discount section

---

## 4. Epic 3 — Room Allocation & Rental

### Story Map

```
As a Banquet Manager
  I want to charge room rental fees
  So that venue costs are included in the event total

├── Feature 3.1: Room Rental Entity
│   ├── US 3.1.1: Create BanquetRoomRental entity
│   │   └── Tasks: Entity, migration, DTO, 6 pricing modes
│   └── US 3.1.2: Calculate rental amount
│       └── Tasks: Mode × Units × SeasonalMultiplier
│
├── Feature 3.2: Room Rental CRUD
│   ├── US 3.2.1: Add rental to event
│   │   └── Tasks: Service method, conflict check
│   └── US 3.2.2: Edit/remove rental
│       └── Tasks: Service methods, audit
│
├── Feature 3.3: Room Combinations
│   ├── US 3.3.1: Multi-room event support
│   │   └── Tasks: Multiple rentals per event
│   └── US 3.3.2: Split/merge hall
│       └── Tasks: Hall combination logic
│
├── Feature 3.4: Seasonal Pricing
│   └── US 3.4.1: Seasonal multiplier on room rental
│       └── Tasks: BanquetSeasonalPrice config, auto-apply
│
└── Feature 3.5: Update Total Formula
    └── US 3.5.1: Include room rental in Total
        └── Tasks: Update ToDto
```

### Sprint Breakdown — Epic 3

| Sprint | Stories | Files | Migration | Tests | Est. LOC |
|--------|---------|-------|-----------|-------|----------|
| **BQ-IMPL-3A** | US 3.1.1, US 3.1.2 | Entity, DTO, Service (50) | 1 (new table) | 8 | ~250 |
| **BQ-IMPL-3B** | US 3.2.1, US 3.2.2, US 3.5.1 | Service (70), ToDto update | 0 | 8 | ~250 |
| **BQ-IMPL-3C** | US 3.3.1, US 3.3.2, US 3.4.1 | Service (60), ViewModel (60), XAML (80) | 0 | 4 | ~350 |

### Files
| File | Action | Sprint |
|------|--------|--------|
| `Core/Entities/RestaurantEntities.cs` | +BanquetRoomRental, RentalMode enum | 3A |
| `Application/DTOs/RestaurantDtos.cs` | +BanquetRoomRentalDto, SaveBanquetRoomRentalRequest | 3A |
| `Infrastructure/Services/RestaurantServices.cs` | +AddRoomRental, seasonal pricing | 3A-3B |
| `App/ViewModels/BanquetRestaurantViewModel.Partial.cs` | +RoomRentalCommands, RoomCombinations | 3C |
| `App/Views/Banquet/BanquetRoomsPage.xaml` | +Room rental form, seasonal config | 3C |

### Rollback
- Drop `BanquetRoomRentals` table
- Revert ToDto formula (remove room rental)
- UI: hide room rental section

---

## 5. Epic 4 — Table Model

### Story Map

```
As a Banquet Operations Manager
  I want to track exact table counts
  So that billing is accurate for actual setup

├── Feature 4.1: New Table Columns
│   ├── US 4.1.1: Add table count columns
│   │   └── Tasks: Migration (ALTER TABLE ADD COLUMN 5 fields)
│   └── US 4.1.2: Map existing TableCount → MainTableCount
│       └── Tasks: Data migration script
│
├── Feature 4.2: Update Pricing Formula
│   ├── US 4.2.1: BillableTables = ActualTableCount + ExtraTableCount
│   │   └── Tasks: Update ToDto menu calculation
│   └── US 4.2.2: Reserve/Complimentary/VIP = 0 charge
│       └── Tasks: Validation, business rules
│
├── Feature 4.3: Table Editor UI
│   ├── US 4.3.1: Table count editor
│   │   └── Tasks: ViewModel fields, XAML inputs
│   └── US 4.3.2: Table count validation
│       └── Tasks: Reserve ≤ 20% Main, Complimentary ≤ 2
│
└── Feature 4.4: Update Reports
    └── US 4.4.1: Include table breakdown in reports
        └── Tasks: Report ViewModel update
```

### Sprint Breakdown — Epic 4

| Sprint | Stories | Files | Migration | Tests | Est. LOC |
|--------|---------|-------|-----------|-------|----------|
| **BQ-IMPL-4A** | US 4.1.1, US 4.1.2 | Entity (+5 props), Migration (ALTER TABLE), Data migration | 1 (ALTER), 1 (data) | 8 | ~150 |
| **BQ-IMPL-4B** | US 4.2.1, US 4.2.2 | Service (40 — ToDto update) | 0 | 12 | ~200 |
| **BQ-IMPL-4C** | US 4.3.1, US 4.3.2, US 4.4.1 | ViewModel (60), XAML (60) | 0 | 5 | ~300 |

### Files
| File | Action | Sprint |
|------|--------|--------|
| `Core/Entities/RestaurantEntities.cs` | +5 table count properties on BanquetEvent | 4A |
| `Application/DTOs/RestaurantDtos.cs` | +5 table count fields on BanquetEventDto, SaveBanquetEventRequest | 4A |
| `Infrastructure/Services/RestaurantServices.cs` | +Table validation, updated ToDto formula | 4B |
| `App/ViewModels/BanquetRestaurantViewModel.Partial.cs` | +TableCountEditor | 4C |
| `App/Views/Banquet/BanquetEventsPage.xaml` | +Table count inputs | 4C |

### Rollback
- Drop new columns from BanquetEvents table
- Revert ToDto formula
- UI: hide new table fields

---

## 6. Epic 5 — Approval Workflow

### Story Map

```
As a Banquet Manager
  I want events to go through approval
  So that large events and discounts are properly authorized

├── Feature 5.1: Workflow Definition
│   ├── US 5.1.1: Register Banquet.EventApproval in EP-001
│   │   └── Tasks: ProcessDefinition, states, transitions
│   └── US 5.1.2: Define approval rules in EP-002
│       └── Tasks: RuleEngine rules for thresholds
│
├── Feature 5.2: Service Integration
│   ├── US 5.2.1: Start approval on event confirm
│   │   └── Tasks: Publish to ProcessManager
│   └── US 5.2.2: Handle approval callbacks
│       └── Tasks: Status update on approve/reject
│
├── Feature 5.3: Approval UI
│   ├── US 5.3.1: Approval dashboard
│   │   └── Tasks: ViewModel, XAML page
│   └── US 5.3.2: Approve/Reject actions
│       └── Tasks: Buttons, reason dialog
│
└── Feature 5.4: Escalation & Timeout
    └── US 5.4.1: Auto-escalate on timeout
        └── Tasks: ProcessTimeout config
```

### Sprint Breakdown — Epic 5

| Sprint | Stories | Files | Migration | Tests | Est. LOC |
|--------|---------|-------|-----------|-------|----------|
| **BQ-IMPL-5A** | US 5.1.1, US 5.1.2 | Workflow config, RuleEngine rules | 0 | 5 | ~100 |
| **BQ-IMPL-5B** | US 5.2.1, US 5.2.2, US 5.4.1 | Service (80), ProcessManager integration | 0 | 10 | ~300 |
| **BQ-IMPL-5C** | US 5.3.1, US 5.3.2 | ViewModel (80), XAML (100) | 0 | 5 | ~400 |

### Files
| File | Action | Sprint |
|------|--------|--------|
| `Application/Platform/Workflow/WorkflowContracts.cs` | +BanquetApproval workflow type | 5A |
| `Infrastructure/Services/RestaurantServices.cs` | +StartApproval, HandleApprovalCallback | 5B |
| `App/ViewModels/BanquetRestaurantViewModel.Partial.cs` | +ApprovalDashboard, ApproveCommand, RejectCommand | 5C |
| `App/Views/Banquet/BanquetEventsPage.xaml` | +Approval panel | 5C |

### Rollback
- Remove workflow registration
- Revert service integration (no effect on status transitions)
- UI: hide approval panel

---

## 7. Epic 6 — FO Integration

### Story Map

```
As a System Architect
  I want Banquet to publish domain events
  So that Front Office modules can react to banquet activities

├── Feature 6.1: Event Publishing in Banquet Service
│   ├── US 6.1.1: Inject IEventBus into BanquetRestaurantService
│   │   └── Tasks: Constructor injection, null check for backward compat
│   └── US 6.1.2: Publish 18 events at key service points
│       └── Tasks: Add PublishAsync calls after each business action
│
├── Feature 6.2: FO Guest Integration
│   ├── US 6.2.1: Create/update GuestProfile on event confirm
│   │   └── Tasks: Subscriber in FO Guest module
│   └── US 6.2.2: Update guest history on event complete
│       └── Tasks: GuestHistory entry
│
├── Feature 6.3: FO Reservation Integration
│   └── US 6.3.1: Create room block for banquet guests
│       └── Tasks: Reservation creation via AllocationEngine
│
├── Feature 6.4: FO Folio/Payment Integration
│   ├── US 6.4.1: Post banquet deposit to Folio
│   │   └── Tasks: Folio entry via PostingEngine
│   └── US 6.4.2: Post banquet payment to Folio
│       └── Tasks: Payment posting
│
└── Feature 6.5: Inventory Integration
    └── US 6.5.1: Create purchase order from PurchaseRequest
        └── Tasks: Inventory service integration
```

### Sprint Breakdown — Epic 6

| Sprint | Stories | Files | Migration | Tests | Est. LOC |
|--------|---------|-------|-----------|-------|----------|
| **BQ-IMPL-6A** | US 6.1.1, US 6.1.2 | Service (60 — event publishing) | 0 | 10 | ~200 |
| **BQ-IMPL-6B** | US 6.2.1, US 6.2.2, US 6.3.1 | FO Guest handler (60), FO Reservation handler (40) | 0 | 8 | ~300 |
| **BQ-IMPL-6C** | US 6.4.1, US 6.4.2, US 6.5.1 | FO Folio handler (50), Inventory handler (40) | 0 | 7 | ~300 |

### Files
| File | Action | Sprint |
|------|--------|--------|
| `Infrastructure/Services/RestaurantServices.cs` | +IEventBus field, 18 PublishAsync calls | 6A |
| `Infrastructure/FrontOffice/Guest/GuestEngines.cs` | +BanquetEventSubscriber | 6B |
| `Infrastructure/FrontOffice/Reservation/EfReservationRepository.cs` | +CreateFromBanquet handler | 6B |
| `Infrastructure/FrontOffice/Folio/EfHotelFolioRepository.cs` | +BanquetPaymentSubscriber | 6C |
| `Infrastructure/FrontOffice/Events/EventBus.cs` | +RegisterBanquetSubscribers | 6A |

### Rollback
- Remove IEventBus from BanquetRestaurantService constructor
- Remove subscriber registrations
- No data changes

---

## 8. Total Sprint Timeline

```
Week 1-2:  BQ-IMPL-1A, BQ-IMPL-2A, BQ-IMPL-3A  (entities + migrations)
Week 3-4:  BQ-IMPL-1B, BQ-IMPL-2B, BQ-IMPL-3B  (service logic)
Week 5-6:  BQ-IMPL-1C, BQ-IMPL-2C, BQ-IMPL-3C  (UI)
Week 7-8:  BQ-IMPL-4A, BQ-IMPL-4B              (table model)
Week 9:    BQ-IMPL-4C                            (table UI)
Week 10-11: BQ-IMPL-5A, BQ-IMPL-5B             (approval workflow)
Week 12:   BQ-IMPL-5C                            (approval UI)
Week 13-14: BQ-IMPL-6A, BQ-IMPL-6B             (FO integration part 1)
Week 15-16: BQ-IMPL-6C                           (FO integration part 2)

Total: 16 sprints / 16 weeks
```

---

## 9. Migration Order

| # | Migration | Epic | Table | Type | Dependencies |
|---|-----------|------|-------|------|-------------|
| 1 | AddBanquetContractVersions | 1 | `BanquetContractVersions` | New | None |
| 2 | AddBanquetDiscounts | 2 | `BanquetDiscounts` | New | None |
| 3 | AddBanquetRoomRentals | 3 | `BanquetRoomRentals` | New | None |
| 4 | AddBanquetEventTableColumns | 4 | `BanquetEvents` | ALTER | None |
| 5 | MigrateTableCountToMainTableCount | 4 | `BanquetEvents` | Data | Migration 4 |

---

## 10. Regression Strategy

### Pre-Implementation Baseline
- 32 BQ regression tests (BQ-SPRINT-002)
- 8 existing Banquet tests (RestaurantModuleTests)
- Total: 40 tests baseline

### Per-Sprint Regression
- Run full Banquet test suite before and after each sprint
- If any existing test fails → BLOCK merge
- New tests added per sprint (see Epic breakdown)

### Total Tests After Implementation
| Epic | New Tests | Cumulative |
|------|-----------|------------|
| 1 — Contract Snapshot | 20 | 60 |
| 2 — Discount Engine | 25 | 85 |
| 3 — Room Rental | 20 | 105 |
| 4 — Table Model | 25 | 130 |
| 5 — Approval Workflow | 20 | 150 |
| 6 — FO Integration | 25 | 175 |

---

## 11. Rollback Strategy

### Per-Migration Rollback
1. Each migration has a verified `Down()` method
2. Test Down() in integration test before merge
3. Data migrations are reversible (MainTableCount ↔ TableCount)

### Per-Epic Rollback
1. Feature flag per epic (via `BanquetModuleSetting`)
2. Disable feature → revert to old behavior
3. DB columns can remain (nullable, default 0/null)

### Emergency Rollback
1. Revert migration: `dotnet ef database update <previous>`
2. Re-deploy previous version
3. Feature flags off for new features

---

## 12. Definition of Done

Per sprint:
- [ ] All new entities have EF mapping + migration
- [ ] All new DTOs have request/response contracts
- [ ] All service methods have validation + audit
- [ ] All ViewModel commands have error handling
- [ ] All regression tests pass (existing + new)
- [ ] Build: Debug + Release (0 errors)
- [ ] UI reviewed for consistency with existing
- [ ] Migration Up() and Down() tested
- [ ] Rollback procedure documented
- [ ] Code review completed

---

## 13. Go Live Checklist

### Pre-Go Live
- [ ] All 175 regression tests pass
- [ ] Database backup created
- [ ] Migrations tested on staging
- [ ] Feature flags configured
- [ ] Rollback tested on staging
- [ ] UI tested on staging
- [ ] Performance test (100 events with menus)

### Go Live
- [ ] Deploy migrations (order: 1→2→3→4→5)
- [ ] Deploy application
- [ ] Enable feature flags
- [ ] Monitor for 24h
- [ ] Verify all 175 tests pass on production DB snapshot

### Post-Go Live
- [ ] Monitor error rates
- [ ] Monitor contract sync behavior
- [ ] Verify financial totals match expected
- [ ] Collect user feedback

---

## 14. Complexity & Resource Estimate

| Epic | Complexity | Est. LOC | Sprints | Engineers |
|------|-----------|----------|---------|-----------|
| 1 — Contract | Medium | ~800 | 3 | 1 |
| 2 — Discount | Medium | ~950 | 3 | 1 |
| 3 — Room Rental | Medium | ~850 | 3 | 1 |
| 4 — Table Model | High | ~650 | 3 | 1 |
| 5 — Approval | Medium | ~800 | 3 | 1 |
| 6 — FO Integration | High | ~800 | 3 | 1-2 |
| **Total** | — | **~4,850** | **18** | **1-2** |

### Parallelization Opportunity
- Epics 1-3 can run in parallel (no dependencies)
- Requires 3 engineers for max speed
- With 1 engineer: 18 sprints (~4.5 months)
- With 2 engineers: 10 sprints (~2.5 months)

---

## 15. Build & Test (Current Baseline)

| Check | Result |
|-------|--------|
| Debug Build | PASS (0 errors) |
| BQ Regression Tests | 32/32 PASS |
| Existing Banquet Tests | 8/8 PASS |
| Production Changes | **0** |

---

**BQ-SPRINT-007 COMPLETED AS IMPLEMENTATION MASTER PLAN ONLY.**

**NO PRODUCTION SOURCE CODE WAS MODIFIED.**
**NO MIGRATIONS WERE CREATED.**
**NO DATABASE SCHEMA WAS CHANGED.**

**THIS PLAN DEFINES:**
- **6 Epics broken into 18 implementation sprints**
- **5 migrations in dependency order**
- **175 target regression tests**
- **Full rollback strategy per epic**
- **16-week timeline (1 engineer) or 10-week (2 engineers)**
- **Go-live checklist with 15 verification steps**
