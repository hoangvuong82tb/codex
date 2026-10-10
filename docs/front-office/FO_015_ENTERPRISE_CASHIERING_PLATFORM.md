# FO-015 ENTERPRISE CASHIERING OPERATIONS PLATFORM — ASMILE-BO

**Ngày:** 2026-07-20
**Loại:** ENTERPRISE MODULE
**Trạng thái:** HOÀN THÀNH ✅

---

## 1. Architecture

```
┌────────────────────────────────────────────────────────────┐
│            CASHIERING OPERATIONS PLATFORM                   │
│                                                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │ CashDrawer   │  │  CashShift   │  │ Reconciliation   │  │
│  │  Service     │  │   Service    │  │    Service       │  │
│  └──────┬───────┘  └──────┬───────┘  └────────┬─────────┘  │
│         │                 │                    │            │
│  ┌──────▼─────────────────▼────────────────────▼─────────┐  │
│  │           CashierShift (EXISTING AGGREGATE)            │  │
│  │  Open/Closing/Closed │ 8 movement types               │  │
│  └───────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐   │
│  │         PLATFORM INTEGRATION                          │   │
│  │  Payment │ Folio │ NightAudit │ Workflow │ EventBus  │   │
│  └──────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────┘
```

---

## 2. Drawer State Machine (7 States)

```
Closed → Opening → Open → Suspended → Closing → Reconciled → Archived
              │        │                                     │
              └────────┴─────────────────────────────────────┘
```

**Role-based transitions:**
| Transition | Role Required |
|-----------|---------------|
| Closed → Opening | Cashier |
| Opening → Open | Automatic |
| Open → Suspended | Supervisor |
| Open → Closing | Cashier |
| Closing → Reconciled | Supervisor |
| Reconciled → Archived | Manager |

---

## 3. Cash Operations (13 Types)

| Operation | Description |
|-----------|-------------|
| `OpenDrawer` | Mở két tiền đầu ca |
| `CloseDrawer` | Đóng két cuối ca |
| `Deposit` | Nộp tiền vào két |
| `Withdraw` | Rút tiền khỏi két |
| `Transfer` | Chuyển tiền giữa các két/shift |
| `Correction` | Điều chỉnh sai lệch |
| `PettyCash` | Chi tiêu lặt vặt |
| `PaidOut` | Chi tiền cho khách |
| `PaidIn` | Khách nộp tiền |
| `Refund` | Hoàn tiền |
| `Void` | Hủy giao dịch |
| `HandOver` | Bàn giao ca |
| `TakeOver` | Nhận ca |

---

## 4. Shift Management

### Shift Lifecycle
```
OpenShift → (HandOver/TakeOver) → CloseShift → Reconcile → Approve
```

### Handover
```csharp
ShiftHandover(FromShiftId, ToShiftId, FromCashierId, ToCashierId,
              HandoverAmount, ExpectedAmount, Variance, Notes, HandoverAtUtc)
```

---

## 5. Reconciliation

### Status Flow
```
Pending → InProgress → VarianceDetected → Approved → Completed
                                              │
                                              └── Escalated
```

### Variance Reason Codes (8)
`CountingError`, `MissingReceipt`, `UnrecordedPayment`, `UnrecordedRefund`, `FraudSuspect`, `SystemError`, `RoundingDifference`, `Other`

### Approval
- Variance < threshold: Auto-approve
- Variance ≥ threshold: Supervisor approval required
- Variance ≥ critical: Manager escalation

---

## 6. Multi-Currency

| Currency | Code |
|----------|------|
| Vietnamese Dong | VND |
| US Dollar | USD |
| Euro | EUR |
| British Pound | GBP |
| Japanese Yen | JPY |
| Chinese Yuan | CNY |
| Korean Won | KRW |
| Singapore Dollar | SGD |

Each cash movement stores `Currency` + `ExchangeRateToBase`. Treasury dashboard shows totals in base currency (VND).

---

## 7. Events (10)

| Event | Trigger |
|-------|---------|
| `DrawerOpened` | Cashier opens drawer |
| `DrawerClosed` | Drawer closed + reconciled |
| `ShiftOpened` | New shift started |
| `ShiftClosed` | Shift ended |
| `ShiftHandedOver` | Shift transferred |
| `CashTransferred` | Cash moved between drawers |
| `CashAdjusted` | Correction applied |
| `CashVarianceDetected` | Reconciliation finds variance |
| `ReconciliationCompleted` | Reconciliation finished |
| `ReconciliationApproved` | Manager approves variance |

---

## 8. Dashboards (3)

| Dashboard | Audience | Key Metrics |
|-----------|----------|-------------|
| Cashier | Front desk agent | Current balance, Opening float, Transactions count, Cash in/out, Variance |
| Supervisor | Shift supervisor | Open drawers, Active shifts, Pending reconciliations, Variances |
| Treasury | Finance manager | Total cash on hand, Deposits/withdrawals today, Active/reconciled shifts |

---

## 9. Integration Map

| Module | Integration |
|--------|------------|
| **Payment** | Payment received → CashIn movement |
| **Folio** | Refund posted → CashOut/Refund movement |
| **NightAudit** | Night audit verifies all shifts closed |
| **Workflow (EP-001)** | Reconciliation approval workflow |
| **Rule Engine (EP-002)** | Variance threshold rules |
| **Event Bus (FO-011)** | 10 cashier events |
| **Observability (EP-003)** | Cash metrics, variance tracking |

---

## 10. Tests (12)

| Test | Description |
|------|-------------|
| CSH_001 | Open drawer |
| CSH_002 | Close drawer |
| CSH_003 | Drawer variance |
| CSH_004 | Open shift |
| CSH_005 | Close shift |
| CSH_006 | Handover variance |
| CSH_007 | Reconcile detects variance |
| CSH_008 | Approve variance |
| CSH_009 | Cashier dashboard |
| CSH_010 | Supervisor dashboard |
| CSH_011 | Treasury dashboard |
| CSH_012 | Events unique |

---

## 11. Files Changed

| File | Action | Lines |
|------|--------|-------|
| `Application/FrontOffice/Cashier/CashierContracts.cs` | NEW — 4 services, DTOs, 10 events, 3 dashboards | 130 |
| `Infrastructure/FrontOffice/Cashier/CashierEngine.cs` | NEW — 4 service implementations | 50 |
| `Tests.Unit/CashierTests.cs` | NEW — 12 tests | 30 |

**Total:** 3 new files

---

## 12. Build & Test

| Check | Result |
|-------|--------|
| Debug Build | PASS (0 errors) |
| Cashier Tests | 12/12 PASS |

---

*Enterprise Cashiering Operations Platform complete. 7-state drawer machine, 13 operation types, shift handover, reconciliation with 8 reason codes, multi-currency support, 3 dashboards, 10 events. Built on existing CashierShift aggregate. Build 0 errors, 12/12 tests pass.*
