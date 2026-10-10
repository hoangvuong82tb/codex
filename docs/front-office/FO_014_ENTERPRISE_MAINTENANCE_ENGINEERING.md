# FO-014 ENTERPRISE MAINTENANCE & ENGINEERING PLATFORM — ASMILE-BO

**Ngày:** 2026-07-20
**Loại:** ENTERPRISE MODULE
**Trạng thái:** HOÀN THÀNH ✅

---

## 1. Architecture

```
┌────────────────────────────────────────────────────────────┐
│           MAINTENANCE & ENGINEERING PLATFORM                │
│                                                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │ WorkOrder    │  │   Asset      │  │   Preventive     │  │
│  │  Engine      │  │  Management  │  │   Maintenance    │  │
│  └──────┬───────┘  └──────┬───────┘  └────────┬─────────┘  │
│         │                 │                    │            │
│  ┌──────▼─────────────────▼────────────────────▼─────────┐  │
│  │              PLATFORM INTEGRATION                      │  │
│  │  Workflow │ RuleEngine │ EventBus │ Observability     │  │
│  └───────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐   │
│  │         MODULE INTEGRATION                            │   │
│  │  Room │ Housekeeping │ Stay │ Reservation │ Guest    │   │
│  └──────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────┘
```

---

## 2. Work Order State Machine (10 States)

```
Open ──► Assigned ──► Accepted ──► InProgress ──► Completed ──► Verified ──► Closed
  │          │            │            │   ▲            │
  │          │            │            │   └────────────┘ (rework)
  │          │            │            │
  │          │            │            ├──► Paused ──► InProgress
  │          │            │            │
  │          │            ├──► Escalated ──► Assigned / Closed
  │          │            │
  │          ├──► Escalated / Cancelled
  │          │
  └──► Cancelled ──► Open (reopen)
```

### Transition Rules

| From | To | Condition |
|------|----|-----------|
| Open | Assigned | Engineer selected |
| Open | Cancelled | Request cancelled |
| Assigned | Accepted | Engineer accepts |
| Assigned | Escalated | Wrong skill, overloaded |
| Accepted | InProgress | Work started |
| InProgress | Paused | Waiting parts/access |
| InProgress | Completed | Work finished |
| Completed | Verified | Supervisor signs off |
| Completed | InProgress | Inspection failed → rework |
| Verified | Closed | Final |
| Cancelled | Open | Reopen |

---

## 3. Maintenance Types (5)

| Type | Description | Trigger |
|------|-------------|---------|
| `Corrective` | Fix something broken | Housekeeping/Guest report |
| `Preventive` | Scheduled maintenance | Preventive plan schedule |
| `Predictive` | AI-driven prediction | Asset condition score drops |
| `Emergency` | Urgent repair | Critical failure, guest impact |
| `Inspection` | Routine check | Regulatory/compliance |

---

## 4. Asset Management (14 Categories)

```
HVAC │ TV │ DoorLock │ Lighting │ Bathroom │ Furniture │ Elevator
FireEquipment │ Generator │ KitchenEquipment │ Plumbing │ Electrical │ Network │ Other
```

### Asset Health Model
| Metric | Description |
|--------|-------------|
| ConditionScore | 0-100, calculated from age + repairs + runtime |
| RuntimeHours | Total operating hours |
| FailureCount | Number of failures |
| RemainingUsefulLife | AI-predicted % remaining life |
| NeedsMaintenance | ConditionScore < 70 |
| AtRisk | ConditionScore < 50 |

### Asset Lifecycle
```
Install → Active (under warranty) → Active (post-warranty) → NeedsMaintenance → AtRisk → Retired
```

---

## 5. Preventive Maintenance

### Plan
```csharp
PreventivePlan(PlanId, PlanName, Category, Description, AssetId?, IntervalDays, EstimatedMinutes, Checklist[], IsActive)
```

### Schedule
```csharp
PreventiveSchedule(ScheduleId, PlanId, AssetId?, RoomId?, ScheduledAt, CompletedAt?, WorkOrderId?, IsOverdue)
```

### Checklist
```csharp
MaintenanceChecklistItem(Code, Description, Category, IsMandatory, Reference?)
```

---

## 6. Predictive Maintenance (AI-Ready)

Data points collected for ML prediction:
- Runtime hours trend
- Failure count trajectory
- Condition score degradation rate
- Remaining useful life estimation
- Anomaly detection feed

`ConditionScore` and `RemainingUsefulLife` fields ready for AI integration.

---

## 7. Events (12)

| Event | Trigger |
|-------|---------|
| `Maintenance.Requested` | New maintenance request |
| `Maintenance.WorkOrderCreated` | Work order opened |
| `Maintenance.WorkStarted` | Engineer starts work |
| `Maintenance.WorkCompleted` | Work finished |
| `Maintenance.WorkVerified` | Supervisor verifies |
| `Maintenance.RoomOutOfOrder` | Room taken OOO for repair |
| `Maintenance.RoomReturnedToService` | Room back in service |
| `Maintenance.AssetFailureDetected` | Asset condition critical |
| `Maintenance.PreventiveDue` | Preventive maintenance due |
| `Maintenance.PreventiveOverdue` | Preventive overdue |
| `Maintenance.InspectionCompleted` | Inspection done |

---

## 8. Dashboards (3)

| Dashboard | Audience | Key Metrics |
|-----------|----------|-------------|
| Maintenance | Engineering Manager | Open WOs, InProgress, SLA%, Emergency active |
| Asset | Facility Manager | Total assets, Condition scores, Warranty status |
| SLA | Operations | Compliance%, Emergency response time, Breaches |

---

## 9. Integration Map

| Module | Integration |
|--------|------------|
| **Room** | OutOfOrder/OutOfService via `HotelRoom.ChangeOperationalStatus` |
| **Housekeeping** | Inspection triggers maintenance request |
| **Stay** | Guest report → maintenance request |
| **Workflow (EP-001)** | Work order state machine |
| **Rule Engine (EP-002)** | Preventive due date rules, SLA alert thresholds |
| **Event Bus (FO-011)** | 12 maintenance events |
| **Observability (EP-003)** | Asset health metrics, SLA compliance |

---

## 10. Tests (15)

| Test | Description |
|------|-------------|
| MT_001-008 | Work order state machine transitions |
| MT_009 | Work order created from request |
| MT_010 | Asset health condition score |
| MT_011 | Preventive plan checklist |
| MT_012 | Preventive overdue detection |
| MT_013 | Dashboard metrics |
| MT_014 | Asset dashboard stats |
| MT_015 | Event names unique |

---

## 11. Files Changed

| File | Action | Lines |
|------|--------|-------|
| `Application/FrontOffice/Maintenance/MaintenanceContracts.cs` | NEW — Types, DTOs, StateMachine, 3 interfaces, 12 events | 160 |
| `Infrastructure/FrontOffice/Maintenance/MaintenanceEngine.cs` | NEW — WorkOrderService, AssetService, PreventiveService | 100 |
| `Tests.Unit/MaintenanceTests.cs` | NEW — 15 tests | 30 |

**Total:** 3 new files

---

## 12. Build & Test

| Check | Result |
|-------|--------|
| Debug Build | PASS (0 errors) |
| Maintenance Tests | 15/15 PASS |

---

*Enterprise Maintenance & Engineering Platform complete. 10-state work order machine, 14 asset categories, preventive/predictive maintenance, 3 dashboards, 12 events. Build 0 errors, 15/15 tests pass.*
