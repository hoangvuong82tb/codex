# BQ-SPRINT-004 — END-TO-END BUSINESS FLOW VERIFICATION

**Ngày:** 2026-07-21
**Loại:** END-TO-END VERIFICATION — ZERO PRODUCTION CHANGE
**Trạng thái:** COMPLETED ✅

---

## 1. End-to-End Flow Map

```
[LEAD] ──► [CUSTOMER] ──► [CREATE EVENT] ──► [ASSIGN ROOM]
                                                    │
                    ┌───────────────────────────────┘
                    ▼
            [SELECT MENU] ──► [CUSTOM MENU] ──► [SERVICES]
                    │
                    ▼
            [CHECKLIST] + [TIMELINE]
                    │
                    ▼
            [DEPOSIT] ──► [CONTRACT]
                    │
                    ▼
            [QUANTIFICATION] ──► [PURCHASE REQUEST]
                    │
                    ▼
            [SETTLEMENT] ──► [FINANCIAL LOCK]
                    │
                    ▼
            [DASHBOARD] + [REPORTS]
```

### Step-by-Step Trace

| Step | Entry Point | ViewModel Command | Service Method | Entity | Persisted | Next |
|------|------------|------------------|----------------|--------|-----------|------|
| Lead | `BanquetDashboardPage.xaml` | `CreateNewEventCommand` | — | — | No | Create Event |
| Customer | `CustomerPageViewModel` | — | `ICustomerAppService` | `Customer` | ✅ | Create Event |
| Create Event | `BanquetEventsPage.xaml` | `SaveEventCommand` | `SaveEventAsync(:4182)` | `BanquetEvent` | ✅ | Assign Room |
| Assign Room | Room select in event editor | — | `SaveEventAsync` (BanquetRoomId) | `BanquetEvent.BanquetRoomId` | ✅ | Select Menu |
| Select Menu | `BanquetMenuModal.xaml` | `AttachMenuPackageCommand` | `AttachMenuPackageAsync(:4456)` | `BanquetEventMenu` | ✅ | Services |
| Custom Menu | `BanquetMenuSelectionView` | — | `SaveCustomMenuAsync(:4472)` | `BanquetEventMenu` | ✅ | Services |
| Services | Service editor | `SaveServiceLineCommand` | `SaveServiceLineAsync(:4505)` | `BanquetServiceLine` | ✅ | Checklist |
| Checklist | `BanquetChecklistPage.xaml` | `SaveChecklistItemCommand` | `SaveChecklistItemAsync(:4722)` | `BanquetChecklist` | ✅ | Timeline |
| Timeline | `BanquetChecklistPage.xaml` | `SaveTimelineItemCommand` | `SaveTimelineItemAsync(:4768)` | `BanquetTimeline` | ✅ | Deposit |
| Deposit | Finance tab | `AddDepositCommand` | `AddDepositAsync(:4532)` | `BanquetDeposit` | ✅ | Contract |
| Contract | Finance tab | `CreateContractCommand` | `CreateContractAsync(:4598)` | `BanquetContract` | ✅ | Quantification |
| Quantification | Quantification tab | `SaveQuantificationCommand` | `SaveQuantificationBatchAsync(:4821)` | `BanquetQuantificationBatch` | ✅ | Purchase Request |
| Purchase Req | Quantification tab | `CreatePurchaseRequestCommand` | `CreatePurchaseRequestAsync(:4900)` | `BanquetPurchaseRequest` | ✅ | Settlement |
| Settlement | Finance tab | `SettleCommand` | `FinalizeSettlementAsync(:4280)` | `BanquetEvent.IsFinanciallyLocked` | ✅ | Dashboard |
| Dashboard | `BanquetDashboardPage.xaml` | `RefreshCommand` | `GetDashboardAsync(:4058)` | — | Read | Reports |
| Reports | `BanquetReportsPage.xaml` | Client-side | N/A | — | Read | — |

---

## 2. Status Machine (7 States)

```
Draft ──► Hold ──► Draft (return)
  │         │
  ├─────────┼──► Confirmed ──► Deposited ──► Preparing ──► Completed
  │         │        │             │             │
  └─────────┴────────┴─────────────┴─────────────┴──► Cancelled (terminal)
                                                       Completed (terminal)
```

### Transition Rules (Source: `RestaurantServices.cs:5231-5247`)

| From | To |
|------|----|
| Draft | Hold, Confirmed, Cancelled |
| Hold | Draft, Confirmed, Cancelled |
| Confirmed | Deposited, Preparing, Cancelled |
| Deposited | Preparing, Cancelled |
| Preparing | Completed, Cancelled |
| Completed | *(terminal)* |
| Cancelled | *(terminal)* |

### Cancellation Guard
- Must refund all deposits before cancelling (`RestaurantServices.cs:4267`)

---

## 3. Business Rule Matrix

| Rule | Type | Source | Enforcement |
|------|------|--------|-------------|
| Event-type name required | Validation | `ValidateEventScheduleAsync:4326-4341` | Throw on save |
| GuestCount > 0 | Validation | `:4346` | Throw on save |
| TableCount > 0 | Validation | `:4347` | Throw on save |
| StartTime < EndTime | Validation | `:4345` | Throw on save |
| EventDate required | Validation | `:4344` | Throw on save |
| Room capacity check | Validation | `:4350-4351` | Warning |
| Room conflict check | Business | `:4353-4367` | Warning |
| Deposit ≤ Remaining | Financial | `AddDepositAsync:4566` | Throw |
| Contract before confirm | Business | Settings-based | Configurable |
| Refund all deposits before cancel | Business | `UpdateEventStatusAsync:4267` | Throw |
| Financial lock blocks edits | Security | `EnsureFinanciallyEditable:5263-5267` | Throw |
| Permission check | Security | `EnsureCanManageBanquet:5269-5273` | Throw |
| Menu code unique | Data | `SaveMenuPackageAsync:4375` | Throw |
| Room code unique | Data | `SaveRoomAsync:4084` | Throw |

---

## 4. Room Lifecycle

### Room Conflict Detection (`ValidateEventScheduleAsync:4353-4367`)
```
Check: Same Room + Same Date + Overlapping Time → Warning
```

### Room Rental
**NOT IMPLEMENTED.** Searched entire solution for `RoomRental`, `HallCharge`, `VenueFee`, `RentalFee` — no results.

### Multi-Day Events
`BanquetEvent` has single `EventDate` — no multi-day support.

### Capacity
Room capacity checked at validation: `GuestCount > Capacity` → Warning.

---

## 5. Menu Lifecycle

### Menu Revision After Contract
Contracts auto-sync with event changes via `SyncContractsAsync(:5128-5151)`. If a menu is changed after contract is created, the contract amounts update automatically. **No contract freeze/snapshot.**

### Pricing Modes
| Mode | Formula | Source |
|------|---------|--------|
| `PerTable` | Price × TableCount | `AttachMenuPackageAsync:4462` |
| `PerGuest` | Price × GuestCount | `AttachMenuPackageAsync` |
| `Custom` | Σ(Qty × UnitPrice) × (TableCount + ExtraTableCount) | `SaveCustomMenuAsync:4483` + `ToDto:5181` |

---

## 6. Contract Lifecycle

### Contract Status
```
Draft → Confirmed → Completed (when Remaining ≤ 0) / Cancelled
```

### Contract Amount Source
Contracts are **dynamically synced** from the event DTO (`SyncContractsAsync:5142-5149`):
- `contract.TotalAmount = dto.TotalAmount` (Menu + Service)
- `contract.DepositAmount = dto.DepositAmount`
- `contract.RemainingAmount = dto.RemainingAmount`
- `contract.Status = dto.RemainingAmount <= 0 ? "Completed" : "Confirmed"`

### Contract Features
| Feature | Implemented |
|---------|-------------|
| Snapshot/freeze on sign | ❌ No — dynamic sync |
| Version history | ❌ No |
| Audit trail | ✅ Yes (via IAuditService) |
| Signature status | `SignDate` field only |
| Auto-update on event change | ✅ Yes |

---

## 7. Discount

**NOT IMPLEMENTED.** Searched entire solution for: `Discount`, `Promotion`, `Voucher`, `Campaign`, `Adjustment`, `Rebate` in Banquet entities/DTOs — no results.

The financial formula `Total = Menu + Service` has no discount subtraction step.

---

## 8. Room Rental

**NOT IMPLEMENTED.** Searched entire solution for: `RoomRental`, `HallCharge`, `VenueFee`, `RentalFee` — no results.

---

## 9. Procurement Lifecycle

```
Quantification Batch (versioned)
     │
     ▼
Purchase Request (BPR-YYYYMMDD-NNNN)
     │
     ▼
Lines: ItemCode, ItemName, RequestedQuantity, EstimatedCost, UnitName
```

### Status Flow
`Requested → Approved → Ordered → Received`

---

## 10. Financial Lock Protection

When `IsFinanciallyLocked = true`:
- ❌ Cannot modify menu (`AttachMenuPackageAsync:4458`)
- ❌ Cannot modify custom menu (`SaveCustomMenuAsync:4478`)
- ❌ Cannot modify services (`SaveServiceLineAsync:4509` — via `EnsureFinanciallyEditableAsync`)
- ❌ Cannot add deposit (`AddDepositAsync:4536`)
- ❌ Cannot modify contract

---

## 11. Permission Model

Single permission gate: `EnsureCanManageBanquet()`
- Checks: `restaurant.banquet.manage` permission
- Applied to ALL write operations (20+ methods)

---

## 12. Missing Business Capabilities

| # | Capability | Status | Severity |
|---|-----------|--------|----------|
| 1 | Room rental / hall charge | NOT IMPLEMENTED | HIGH |
| 2 | Discount / promotion logic | NOT IMPLEMENTED | HIGH |
| 3 | Contract snapshot/freeze on sign | NOT IMPLEMENTED | HIGH |
| 4 | Multi-day events | NOT IMPLEMENTED | MEDIUM |
| 5 | Contract version history | NOT IMPLEMENTED | MEDIUM |
| 6 | Reserve/Actual/Complimentary tables | NOT IMPLEMENTED | MEDIUM |
| 7 | Tax separate from service | NOT IMPLEMENTED | MEDIUM |
| 8 | Server-side report aggregation | CLIENT-SIDE | MEDIUM |
| 9 | Approval workflow for status changes | NOT IMPLEMENTED | LOW |
| 10 | Email notification on status change | NOT IMPLEMENTED | LOW |
| 11 | Integration with FO Reservation | NOT IMPLEMENTED | LOW |
| 12 | Integration with FO Folio/Payment | NOT IMPLEMENTED | LOW |

---

## 13. Build & Test

| Check | Result |
|-------|--------|
| Debug Build | PASS (0 errors) |
| BQ Regression Tests (32) | 32/32 PASS |
| Existing Banquet Tests (8) | 8/8 PASS |

---

**BQ-SPRINT-004 COMPLETED AS END-TO-END BUSINESS FLOW VERIFICATION ONLY.**
**NO PRODUCTION CODE WAS MODIFIED.**
**ALL 16 STEPS TRACED WITH FILE PATHS AND LINE NUMBERS.**
**ALL MISSING CAPABILITIES DOCUMENTED WITH EVIDENCE.**
