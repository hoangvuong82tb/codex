# BQ-SPRINT-008 — ARCHITECTURE REVIEW BOARD

**Ngày:** 2026-07-21
**Loại:** ARCHITECTURE REVIEW — NO CODE
**Trạng thái:** COMPLETED ✅

---

## 1. ADR-001: BanquetContract is a Separate Aggregate

### Current Decision (from BQ-006)
BanquetContract is NOT owned by BanquetEvent. References by ID. Has own lifecycle.

### Challenge: Should Contract be a child entity of BanquetEvent instead?

### Option A: Separate Aggregate (Current)
```
BanquetContract (own aggregate)
  └── BanquetContractVersion[N]
  References BanquetEvent by FK
```

| Pros | Cons |
|------|------|
| Independent lifecycle | Requires SyncContractsAsync to keep in sync |
| Legal retention after event delete | Two aggregates can diverge |
| Can have its own versions | Extra complexity: sync logic |
| Can be legally frozen independently | Contract cannot exist without event |

### Option B: Child Entity of BanquetEvent
```
BanquetEvent
  └── BanquetContract[N] (owned)
        └── BanquetContractVersion[N]
```

| Pros | Cons |
|------|------|
| Always in sync (no SyncContractsAsync) | Cascaded on event delete (lose legal records) |
| Simpler — one less aggregate | Cannot freeze contract independently |
| Single transaction boundary | Contract+Event saved together (large aggregate) |

### Option C: Contract as Value Object Snapshot
```
BanquetEvent
  └── ContractSnapshot (JSON value object)
```
Store contract as immutable JSON snapshot at signing time. No separate table.

| Pros | Cons |
|------|------|
| Simplest — no separate entity | Cannot query contracts independently |
| Always consistent (snapshot at sign) | No version history (JSON blob) |
| Zero sync logic | Hard to report on contracts |

### Decision Matrix

| Criteria | Option A | Option B | Option C |
|----------|----------|----------|----------|
| DDD Purity | ✅ | ⚠️ Large AR | ❌ No entity |
| Legal Compliance | ✅ Retention | ❌ Cascade | ⚠️ Snapshot |
| Performance | ✅ Separate load | ⚠️ Large AR | ✅ Light |
| Complexity | ⚠️ Sync logic | ✅ Simple | ✅ Simple |
| Query-ability | ✅ Own table | ✅ Own table | ❌ JSON |
| Migration Risk | ✅ Additive | ❌ Move data | ✅ Additive |
| Long-term Maintainability | ✅ Clear boundary | ⚠️ AR bloat | ❌ Query issues |

### Board Decision: **Option A CONFIRMED** ✅
Contract separation is the correct DDD choice. The sync logic overhead is justified by independent lifecycle and legal compliance. The slight complexity of `SyncContractsAsync` is manageable.

**ADR-001 Status: ACCEPTED**

---

## 2. ADR-002: BanquetMenuPackage is a Separate Aggregate

### Current Decision (from BQ-006)
BanquetMenuPackage is shared across events as reusable template. Independent CRUD.

### Challenge: Should MenuPackage be embedded in event instead?

### Option A: Separate Aggregate (Current)
### Option B: MenuPackage embedded as event-owned value
### Option C: MenuPackage as read-only reference (no FK)

### Decision Matrix

| Criteria | Option A | Option B | Option C |
|----------|----------|----------|----------|
| Reuse across events | ✅ | ❌ Copy each time | ⚠️ |
| Update propagation | ❌ Manual | ✅ Auto | ✅ Auto |
| Query packages independently | ✅ | ❌ | ✅ |
| Snapshot at attach time | ⚠️ Must copy data | ✅ Always current | ✅ Always current |
| Complexity | ⚠️ | ✅ | ✅ |

### Board Decision: **Option A CONFIRMED** ✅
Despite the snapshot concern (menu changes after attach), the reuse benefit outweighs. The solution: `BanquetEventMenu` already stores a snapshot (UnitPrice, TableCount) at attachment time — this is the correct pattern.

**ADR-002 Status: ACCEPTED**

---

## 3. ADR-003: Discount is Owned by BanquetEvent

### Current Decision (from BQ-006)
BanquetDiscount is a child entity of BanquetEvent.

### Challenge: Should discounts be applied via RuleEngine instead of stored entities?

### Option A: Discount as Child Entity (Current)
```
BanquetEvent
  └── BanquetDiscount[N]
```

### Option B: Discount via EP-002 RuleEngine
```
RuleEngine evaluates discount rules → computes discount amount
No entity stored — computed dynamically
```

### Option C: Discount as Separate Aggregate with Campaign linking
```
BanquetDiscount (own aggregate)
  References Campaign, Voucher, Event
```

### Decision Matrix

| Criteria | Option A | Option B | Option C |
|----------|----------|----------|----------|
| Audit trail | ✅ Every discount stored | ❌ Computed ephemeral | ✅ |
| Approval workflow | ✅ Per discount | ❌ No entity to approve | ✅ |
| Campaign/voucher linking | ⚠️ Manual code | ✅ RuleEngine | ✅ |
| Stacking rules | ⚠️ Manual code | ✅ RuleEngine priority | ⚠️ |
| Query: total discounts | ✅ SUM | ❌ Must log separately | ✅ |
| Performance | ✅ Direct | ✅ Fast compute | ✅ |

### Board Decision: **HYBRID — Option A + B** 🔄
- **Option A** for discount entities (storage, audit, approval)
- **Option B (RuleEngine)** for automatic discount rules (campaign, promotion, voucher validation)

Discount entities store the result. RuleEngine validates and computes. This gives us both audit trail AND rule-based automation.

**ADR-003 Status: ACCEPTED (with hybrid enhancement)**

---

## 4. ADR-004: RoomRental is Owned by BanquetEvent

### Current Decision (from BQ-006)
BanquetRoomRental is a child entity of BanquetEvent.

### Challenge: Should room pricing live on BanquetRoom instead?

### Option A: Rental on Event (Current)
### Option B: Pricing on BanquetRoom entity
### Option C: Separate RoomPricing aggregate

### Decision Matrix

| Criteria | Option A | Option B | Option C |
|----------|----------|----------|----------|
| Event-specific pricing | ✅ Negotiated rate | ❌ Room base rate only | ✅ |
| Seasonal pricing | ✅ Per rental | ⚠️ Room-wide | ✅ |
| Multi-room events | ✅ N rentals | ❌ N rooms × 1 rate | ✅ |
| Room base price | ❌ Duplicated | ✅ Single source | ✅ |
| Complexity | ✅ Simple | ✅ Simple | ❌ Over-engineered |

### Board Decision: **Option A CONFIRMED** ✅
Room pricing is inherently event-specific (negotiated rates, seasonal adjustments). Storing on the room would lose this flexibility. For the room's base/default price, `BanquetRoom` can add a `DefaultRentalRate` field as a convenience (pre-fill on event create).

**ADR-004 Status: ACCEPTED**

---

## 5. ADR-005: Approval via EP-001 Workflow

### Current Decision (from BQ-006)
Banquet uses EP-001 Workflow Platform for approval, not custom code.

### Challenge: Is EP-001 too heavy? Should we use simpler status-based approval?

### Option A: EP-001 Workflow (Current)
### Option B: Simple status field + validation
### Option C: Dedicated BanquetApproval entity

### Decision Matrix

| Criteria | Option A | Option B | Option C |
|----------|----------|----------|----------|
| Audit trail | ✅ Built-in | ❌ Manual | ⚠️ Manual |
| Timeout/escalation | ✅ Built-in | ❌ Custom code | ❌ Custom code |
| Multi-level approval | ✅ Configurable | ⚠️ Hardcoded | ⚠️ Hardcoded |
| Integration complexity | ⚠️ Dependency on EP-001 | ✅ Zero deps | ✅ Zero deps |
| Learning curve | ⚠️ Must learn EP-001 | ✅ Simple | ✅ Simple |
| Future-proofing | ✅ Reuse platform | ❌ Duplicate effort | ❌ Duplicate |

### Board Decision: **Option A CONFIRMED with lightweight path** ✅
For simple events (Total < 50M, no discount), use **auto-approve** — bypass EP-001 entirely. For complex events, use EP-001. This gives us fast-path for 80% of events and full workflow for the 20% that need it.

Implementation: `if (totalAmount < 50M && discountAmount == 0) { autoApprove(); } else { startWorkflow(); }`

**ADR-005 Status: ACCEPTED (with lightweight bypass)**

---

## 6. Table Model: Column Explosion Risk

### Current Decision (from BQ-006)
Add 5 new columns to BanquetEvent: MainTableCount, ReserveTableCount, ActualTableCount, ComplimentaryTableCount, VipTableCount.

### Challenge: Is 7 table count columns too many? Should we use a child entity?

### Option A: Columns on BanquetEvent (Current)
```
BanquetEvent.TableCount (existing)
BanquetEvent.MainTableCount
BanquetEvent.ReserveTableCount
BanquetEvent.ActualTableCount
BanquetEvent.ComplimentaryTableCount
BanquetEvent.VipTableCount
BanquetEvent.ExtraTableCount (existing)
```

### Option B: BanquetTableAllocation child entity
```
BanquetEvent
  └── BanquetTableAllocation (1:1)
        Main, Reserve, Actual, Complimentary, VIP, Extra
```

### Option C: JSON column
```
BanquetEvent.TableAllocationJson
```

### Decision Matrix

| Criteria | Option A | Option B | Option C |
|----------|----------|----------|----------|
| Query simplicity | ✅ SELECT directly | ⚠️ JOIN | ❌ JSON parse |
| Migration simplicity | ✅ ALTER TABLE | ✅ New table | ✅ New column |
| Aggregate size | ⚠️ 7 int columns | ✅ 1 FK | ✅ 1 column |
| Reporting | ✅ Easy SUM | ⚠️ JOIN needed | ❌ JSON query |
| Backward compat | ✅ Default 0 | ✅ No change | ✅ Default null |

### Board Decision: **Option A CONFIRMED** ✅
7 int columns is acceptable. They're value types (4 bytes each = 28 bytes total). Int columns query directly without JOINs, making reporting fast. The alternative (child entity) adds a JOIN to every financial query with no benefit.

**Table Model Decision: ACCEPTED**

---

## 7. EventBus Integration: 18 Events vs Lazy Integration

### Current Decision (from BQ-006)
Publish 18 domain events from Banquet service to FO EventBus.

### Challenge: Is 18 events too many? Should we start with fewer?

### Option A: 18 Events (Current)
### Option B: Start with 5 Critical Events, add later
### Option C: No events — direct service calls

### Decision Matrix

| Criteria | Option A | Option B | Option C |
|----------|----------|----------|----------|
| Time to value | ⚠️ 3 sprints | ✅ 1 sprint (5 events) | ✅ 0 sprints |
| FO integration depth | ✅ Full | ⚠️ Partial | ❌ None |
| Coupling | ✅ Loose | ✅ Loose | ❌ Tight coupling |
| Maintainability | ⚠️ 18 subscribers | ✅ 5 subscribers | ❌ Direct deps |
| Rollback risk | ⚠️ Higher | ✅ Lower | ✅ Lowest |

### Board Decision: **Option B — Phased approach** 🔄

Phase 1 (Epic 6 Sprint A): 5 Critical Events
- `Banquet.EventConfirmed` → FO Guest profile
- `Banquet.DepositReceived` → FO Folio posting
- `Banquet.ContractSigned` → CRM update
- `Banquet.EventCompleted` → Guest history
- `Banquet.EventCancelled` → Cleanup

Phase 2 (Epic 6 Sprint B-C): Remaining 13 events (add incrementally)

**EventBus Decision: ACCEPTED (phased rollout)**

---

## 8. Migration Strategy: 5 Separate vs 1 Mega Migration

### Current Decision (from BQ-007)
5 separate migrations in dependency order.

### Challenge: Should we combine into fewer migrations?

### Option A: 5 Separate Migrations (Current)
### Option B: 1 Mega Migration (all tables + columns at once)
### Option C: 2 Migrations (Core tables + UI columns)

### Decision Matrix

| Criteria | Option A | Option B | Option C |
|----------|----------|----------|----------|
| Rollback granularity | ✅ Per-feature | ❌ All or nothing | ⚠️ Half |
| Deployment flexibility | ✅ Deploy one epic at a time | ❌ All at once | ⚠️ |
| Feature flag control | ✅ Per epic | ❌ | ⚠️ |
| Migration order control | ✅ Explicit | ❌ Implicit | ⚠️ |
| Complexity | ✅ Simple per migration | ✅ One script | ⚠️ |

### Board Decision: **Option A CONFIRMED** ✅
5 separate migrations is the correct approach. Each migration maps to one epic, each has a tested Down(), each is independently deployable. This is the safest path.

**Migration Decision: ACCEPTED**

---

## 9. Final Decision Status

| ID | Decision | Status | Notes |
|----|----------|--------|-------|
| ADR-001 | Contract separate AR | **ACCEPTED** | Legal compliance justifies sync overhead |
| ADR-002 | MenuPackage separate AR | **ACCEPTED** | Snapshot at attach is already in place |
| ADR-003 | Discount child entity | **ACCEPTED** | Hybrid with RuleEngine for auto-rules |
| ADR-004 | RoomRental child entity | **ACCEPTED** | Add DefaultRentalRate to BanquetRoom |
| ADR-005 | EP-001 Workflow | **ACCEPTED** | Lightweight bypass for simple events |
| Table-001 | 7 int columns | **ACCEPTED** | 28 bytes, no JOIN overhead |
| Event-001 | 18 events → phased | **ACCEPTED** | 5 critical first, 13 incremental |
| Migr-001 | 5 separate migrations | **ACCEPTED** | Per-epic deployability |

---

## 10. Risk Re-Assessment After Review

| Risk | Before Review | After Review | Mitigation |
|------|--------------|-------------|------------|
| Contract sync complexity | HIGH | MEDIUM | SyncContractsAsync is well-tested; freeze logic reduces risk |
| Discount stacking complexity | MEDIUM | LOW | Hybrid RuleEngine approach eliminates manual stacking logic |
| EP-001 dependency | MEDIUM | LOW | Lightweight bypass for 80% of cases |
| Table column explosion | LOW | LOW | 7 ints = 28 bytes — negligible |
| 18 events overwhelm | MEDIUM | LOW | Phased to 5 critical first |
| Migration order deadlock | LOW | LOW | No circular dependencies confirmed |

---

## 11. Approved Architecture — Final

### Aggregate Boundaries (unchanged from BQ-006)
- BanquetEvent: Aggregate Root (8 child entities)
- BanquetContract: Separate Aggregate (legal freeze, version history)
- BanquetMenuPackage: Separate Aggregate (reusable template)
- BanquetQuantificationBatch: Separate Aggregate (complex calculation)
- BanquetPurchaseRequest: Separate Aggregate (procurement lifecycle)

### New Enhancements from Review
1. **Discount + RuleEngine hybrid**: Entity for storage + RuleEngine for auto-computation
2. **Lightweight approval bypass**: Skip EP-001 for events < 50M, no discount
3. **Phased EventBus rollout**: 5 critical events in Phase 1, 13 incremental in Phase 2
4. **BanquetRoom.DefaultRentalRate**: Convenience field for pre-filling rental

### Implementation Plan (unchanged from BQ-007)
- 6 Epics → 18 sprints → 16 weeks (1 engineer)
- 5 migrations, 175 tests, 0 breaking changes

---

## 12. Build & Test (Current Baseline)

| Check | Result |
|-------|--------|
| Debug Build | PASS (0 errors) |
| BQ Regression Tests | 32/32 PASS |
| Existing Banquet Tests | 8/8 PASS |
| Production Changes | **0** |

---

**ARCHITECTURE REVIEW COMPLETED.**

**ALL 8 ADRs REVIEWED, CHALLENGED, AND CONFIRMED.**

**4 ENHANCEMENTS IDENTIFIED AND INCORPORATED.**

**FINAL ARCHITECTURE APPROVED.**

**READY FOR IMPLEMENTATION.**
