# FO-020 ENTERPRISE EXECUTIVE DASHBOARD & BUSINESS INTELLIGENCE PLATFORM — ASMILE-BO

**Ngày:** 2026-07-20
**Loại:** ENTERPRISE CAPSTONE
**Trạng thái:** HOÀN THÀNH ✅

---

## 1. Architecture — Read-Only CQRS

```
┌────────────────────────────────────────────────────────────┐
│        EXECUTIVE DASHBOARD & BI PLATFORM (READ-ONLY)        │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐   │
│  │                CQRS READ MODEL                        │   │
│  │  IExecutiveQueryService │ IDashboardService          │   │
│  │  IAlertCenterService                                   │   │
│  └──────────────────────────────────────────────────────┘   │
│                            │                                │
│         ┌──────────────────┼──────────────────┐             │
│         │                  │                  │             │
│  ┌──────▼─────┐  ┌────────▼────┐  ┌─────────▼─────────┐   │
│  │Reservation │  │    Stay     │  │     Folio/Payment │   │
│  ├────────────┤  ├─────────────┤  ├───────────────────┤   │
│  │Rate        │  │Housekeeping │  │Cashiering         │   │
│  │Allocation  │  │Maintenance  │  │Group Reservation  │   │
│  │Channel Mgr │  │Door Lock    │  │Concierge          │   │
│  └────────────┘  └─────────────┘  └───────────────────┘   │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              DASHBOARD OUTPUT                         │   │
│  │  GM │ Front Office │ Operations │ Finance │ Eng      │   │
│  └──────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────┘
```

**Nguyên tắc:** Read-only. Không Aggregate mới. Không CRUD. CQRS Read Model tổng hợp từ toàn bộ 20 Front Office modules.

---

## 2. Widget-Based Dashboard System

### Widget Types
| Type | Description |
|------|-------------|
| `KPI` | Single metric with trend indicator |
| `Chart` | Bar/Line/Pie/Gauge/Heatmap |
| `Table` | Tabular data with pagination |
| `Alert` | Alert notification |
| `Trend` | Sparkline / trend line |
| `Gauge` | Radial gauge |
| `Forecast` | Projection chart |

### Widget Sizes
`Small` (1×1) | `Medium` (2×1) | `Large` (2×2) | `Full` (4×2)

### Layout
```csharp
DashboardLayout(LayoutId, Name, Role, Widgets[], IsDefault)
```
Role-based layouts: GM, FrontOffice, Operations, Finance, Engineering.

---

## 3. Executive KPIs

| KPI Group | Metrics |
|-----------|---------|
| **Occupancy** | Occupancy%, Yesterday%, Trend |
| **Revenue** | ADR, RevPAR, Today/Month/YTD Revenue |
| **Bookings** | Arrivals, Departures, InHouse, Created, Cancelled, NoShow |
| **Revenue Split** | Room, F&B, Other |
| **Forecast** | 7-day Occupancy/Revenue/RevPAR |

### Trend Indicators
`Up ▲` | `Down ▼` | `Flat →` | `Unknown`

---

## 4. Operational KPIs

| KPI | Source Module |
|-----|--------------|
| Room Readiness % | Housekeeping |
| Housekeeping SLA % | Housekeeping |
| Open Work Orders | Maintenance |
| Maintenance SLA % | Maintenance |
| Cash Variance | Cashiering |
| Night Audit Status | NightAudit |
| Door Health % | DoorLock |
| Channel Health % | Channel Manager |
| Concierge Open Requests | Concierge |

---

## 5. Financial KPIs

| KPI | Source Module |
|-----|--------------|
| Total Revenue/Charges/Payments | Folio |
| Deposits/Refunds | Payment |
| Outstanding Balance | Folio |
| Cash Position | Cashiering |
| Payment Mix (Cash/Card/Transfer %) | Payment |
| Open/Settled Folios | Folio |
| Deposit/Refund Liability | Payment |

---

## 6. Forecast

| KPI | Horizon |
|-----|---------|
| Occupancy Forecast | 7 / 30 / 90 days |
| Revenue Forecast | 7 / 30 / 90 days |
| Pickup Forecast | 7 / 30 days |
| Housekeeping Demand | Tomorrow |
| Arrivals/Departures | Tomorrow |

---

## 7. Executive Cockpit (5 Role-Based Dashboards)

### GM Dashboard
```
Row 1: [Revenue KPI] [Occupancy KPI] [ADR KPI] [RevPAR KPI]
Row 2: [Occupancy Chart ──────────────────────────────]
Row 3: [Revenue Chart ─────────────────────────────────]
Row 4: [Critical Alerts ───────────────────────────────]
```

### Front Office Dashboard
```
Row 1: [Arrivals KPI] [Departures KPI] [InHouse KPI] [VIP KPI]
Row 2: [Occupancy Timeline ────────────────────────────]
Row 3: [Room Status Pie] [Arrival Alerts] [Departure Alerts]
```

### Operations Dashboard
```
Row 1: [Room Readiness] [Housekeeping SLA] [Maintenance SLA] [Door Health]
Row 2: [Housekeeping Status Chart ──────────────────────]
Row 3: [Maintenance by Category] [SLA Breaches] [Offline Devices]
```

### Finance Dashboard
```
Row 1: [Revenue Today] [Payments Today] [Outstanding] [Cash Position]
Row 2: [Revenue by Day Chart ───────────────────────────]
Row 3: [Payment Mix Pie] [Outstanding by Channel] [Cash Variances]
```

### Engineering Dashboard
```
Row 1: [Locks Online] [Locks Offline] [Battery Low] [Open Work Orders]
Row 2: [Work Orders by Status Chart ────────────────────]
Row 3: [Asset Condition Gauge] [Critical Alerts]
```

---

## 8. Alert Center

```
Total Alerts │ Critical: 2 │ Warning: 5 │ Info: 5
──────────────────────────────────────────────────────
[Critical] Night Audit Failed — Step 7 [Resume →]
[Critical] Room 305 Lock Offline — 2h [Investigate →]
[Warning]  Cash Variance 200K — Shift SHF-001 [Review →]
[Warning]  SLA Breach — Room 205 cleaning overdue
[Info]     Channel Sync Completed — Booking.com
```

### Alert Sources (20 modules)
Reservation, Stay, Folio, Payment, NightAudit, Housekeeping, Maintenance, Cashiering, DoorLock, Channel, Concierge, Group, Guest, Rate, Allocation, Workflow, Saga, EventBus, Observability

---

## 9. Data Sources (Read-Only from 20 Modules)

| # | Module | Data Consumed |
|---|--------|--------------|
| 1 | Reservation | Occupancy, arrivals, departures, cancellations |
| 2 | Availability | Room status counts |
| 3 | Rate | ADR, rate plans |
| 4 | Allocation | Room assignment status |
| 5 | Stay | InHouse count, VIP count |
| 6 | Folio | Revenue, charges, payments |
| 7 | Payment | Payment mix, deposits, refunds |
| 8 | NightAudit | Revenue snapshot, ADR, RevPAR |
| 9 | Guest | VIP count, repeat guest % |
| 10 | Housekeeping | Room readiness, SLA |
| 11 | Maintenance | Open work orders, SLA |
| 12 | Cashiering | Cash position, variance |
| 13 | Group Reservation | Group pickup, revenue |
| 14 | Channel Manager | Channel health, booking pace |
| 15 | Concierge | Open requests, SLA |
| 16 | Door Lock | Lock health, offline count |
| 17 | Workflow | Active workflows, failures |
| 18 | Saga | Active sagas, compensations |
| 19 | EventBus | Outbox pending, dead letter |
| 20 | Observability | System metrics |

---

## 10. Tests (14)

| Test | Description |
|------|-------------|
| EXEC_001-003 | Executive KPIs: Occupancy, ADR, RevPAR |
| EXEC_004 | Operational KPIs: Room readiness |
| EXEC_005 | Financial KPIs: Outstanding balance |
| EXEC_006 | Forecast: 7-day projection |
| EXEC_007 | GM Dashboard: Revenue KPIs |
| EXEC_008 | Front Office Dashboard: Arrivals |
| EXEC_009 | Operations Dashboard: Rooms Ready |
| EXEC_010 | Finance Dashboard: Revenue |
| EXEC_011 | Engineering Dashboard: Lock KPIs |
| EXEC_012 | Dashboard Layout: Widgets |
| EXEC_013 | Alert Center: Has alerts |
| EXEC_014 | Critical Alerts: Non-empty |

---

## 11. Files Changed

| File | Action | Lines |
|------|--------|-------|
| `Application/FrontOffice/Executive/ExecutiveContracts.cs` | NEW — Widget system, 3 services, 5 dashboards, AlertCenter | 140 |
| `Infrastructure/FrontOffice/Executive/ExecutiveEngine.cs` | NEW — Read-model aggregating 20 modules | 100 |
| `Tests.Unit/ExecutiveTests.cs` | NEW — 14 tests | 30 |

**Total:** 3 new files

---

## 12. Build & Test

| Check | Result |
|-------|--------|
| Debug Build | PASS (0 errors) |
| Executive Tests | 14/14 PASS |

---

*Enterprise Executive Dashboard & Business Intelligence Platform complete. Widget-based dashboard system, CQRS Read Model aggregating 20 Front Office modules, 5 role-based dashboards (GM/FO/Ops/Finance/Engineering), Alert Center, Forecast engine. Read-only, no new aggregates. Build 0 errors, 14/14 tests pass.*
