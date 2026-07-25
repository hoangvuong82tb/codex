# EMERGENCY FRONT OFFICE RECOVERY AUDIT

**Ngày:** 2026-07-21
**Loại:** READ-ONLY AUDIT
**Trạng thái:** PARTIAL FO RECOVERY POSSIBLE

---

## 1. Git State

| Property | Value |
|----------|-------|
| HEAD | `f4ac5cf` — Initial ASMILE-BO source sync |
| Branch | `main` (only branch) |
| Remote | `origin/main` — same commit |
| Commits | 2 total (`ef11c47` → `f4ac5cf`) |
| Stash | Empty |
| Tags | None |
| Submodules | None |

---

## 2. What Happened

```
git checkout HEAD -- .
```

Lệnh này revert tất cả **tracked files** về trạng thái commit. Các file **untracked** (mới tạo, chưa từng commit) không bị ảnh hưởng.

---

## 3. What Survived (INTACT)

Tất cả **file FO mới tạo** (untracked) vẫn còn nguyên:

| Layer | Files |
|-------|-------|
| `Core/FrontOffice/` | Allocation, Availability, Cashier, Concierge, Folio, Group, Guest, Housekeeping, Loyalty, Message, NightAudit, Notification, Payment, Rate, ServiceRequest, Settlement, Stay, TaskShift, WakeUpCall, LostFound, Feedback |
| `Application/FrontOffice/` | Allocation, Availability, Cashier, Channel, Concierge, DoorLock, Events, Executive, Folio, Group, Guest, Housekeeping, Maintenance, NightAudit, Payment, Posting, Rate, Reporting, RoomRack, Settlement, Stay |
| `Infrastructure/FrontOffice/` | Allocation, Availability, Cashier, Channel, Concierge, DoorLock, Events, Executive, Folio, Group, Guest, Housekeeping, Maintenance, NightAudit, Payment, Posting, Rate, Reporting, RoomRack, Settlement, Stay |
| `Application/Platform/` | Workflow, Observability |
| `Core/Platform/` | Rules |
| `Infrastructure/Platform/` | Workflow, Observability, Rules |
| `App/ViewModels/` | ~30 FO ViewModels |
| `App/Views/` | ~30 FO XAML views |
| `App/Services/` | ~10 FO UI services |
| `Tests.Unit/` | ~25 FO test files |
| `Migrations/` | 6 FO migration files |

---

## 4. What Was Lost (REVERTED)

Các **tracked file đã sửa** (có trong commit nhưng bị FO development thay đổi):

| File | Lost Changes |
|------|-------------|
| `AppDbContext.cs` | FO DbSets: HotelStays, HotelStayHistories, HotelFolios, HotelFolioEntries, HotelRoomTypes, HotelRooms, HotelPayments, HotelPaymentAllocations, HotelInvoices, RatePlans, RateCalendarEntries, RateRules, RateSeasons, GuestProfiles, GuestDocuments, GuestPreferences, VipProfiles, LoyaltyTransactions, GroupReservations, GroupRoomAssignments, ServiceRequests, ServiceHistories, WakeUpCalls, WakeUpCallHistories, LostFoundItems, LostFoundHistories, HousekeepingTasks, RoomHousekeepingStates, ProcessInstances, ProcessSteps, ProcessSignals, ProcessTimeouts, ProcessHistories, ProcessDeadLetters, ProcessCompensations, ProcessManualActions, NightAudits, NightAuditStepExecutions, BusinessDateRecords, RevenueSnapshots, CashierShifts, CashMovements + EF mappings cho tất cả |
| `DependencyInjection.cs` | FO service registrations: Stay, Folio, Payment, Settlement, NightAudit, Reporting, RoomRack, Rate, Allocation, Posting, Events, Guest |
| `Reservation.cs` | +`RatePlanId` field, +`RatePlanId` in `Create()` |
| `HotelRooms.cs` | +`RoomPhysicalAttributes` value object, +`BedConfiguration`, +`RoomView` enums, +`GetPhysicalAttributes()`, +`SetPhysicalAttributes()` |
| `ReservationContracts.cs` | +`RatePlanId` in `CreateReservationCommand`, +`AutoAssignReservationRoomCommand` |
| `ReservationHandlers.cs` | +`IRoomAllocationEngine` dependency, +`AutoAssignAsync()` |
| `ReservationCqrsHandlers.cs` | +`AutoAssignReservationRoomCommandHandler` |
| `ReservationUiService.cs` | +`RatePlanId` in `CreateAsync` signature |
| `ReservationWorkspaceViewModel.cs` | +`RatePlanId` parameter |

---

## 5. Recovery Sources Checked

| Source | Status |
|--------|--------|
| Git commits | ❌ Only 2 commits — no FO code |
| Git reflog | ❌ Only initial setup entries |
| Git stash | ❌ Empty |
| Git unreachable blobs | ✅ 3 blobs with FO content found (partial) |
| Remote branches | ❌ Only `origin/main` — same commit |
| Backup `D:/ASMILE-BO - Copy` | ❌ Jul 13 — pre-FO |
| IDE local history | ⬜ Not checked |
| Windows File History | ⬜ Not checked |
| Recycle Bin | ⬜ Not checked |

---

## 6. Recovery Assessment

### PARTIAL FO RECOVERY POSSIBLE

**~95% of FO code is intact** — all new files survived as untracked.

**~5% lost** — modifications to existing tracked files (DbContext, DI, entities). These can be manually recreated since the lost changes are documented above.

---

## 7. Recovery Plan

### Phase 1: Add DbSets to AppDbContext
```csharp
// Add to AppDbContext:
public DbSet<HotelStay> HotelStays => Set<HotelStay>();
public DbSet<HotelStayHistory> HotelStayHistories => Set<HotelStayHistory>();
public DbSet<HotelFolio> HotelFolios => Set<HotelFolio>();
public DbSet<HotelFolioEntry> HotelFolioEntries => Set<HotelFolioEntry>();
public DbSet<HotelRoomType> HotelRoomTypes => Set<HotelRoomType>();
public DbSet<HotelRoom> HotelRooms => Set<HotelRoom>();
public DbSet<HotelPayment> HotelPayments => Set<HotelPayment>();
public DbSet<HotelPaymentAllocation> HotelPaymentAllocations => Set<HotelPaymentAllocation>();
public DbSet<HotelInvoice> HotelInvoices => Set<HotelInvoice>();
public DbSet<RatePlan> RatePlans => Set<RatePlan>();
public DbSet<RateCalendarEntry> RateCalendarEntries => Set<RateCalendarEntry>();
public DbSet<RateRule> RateRules => Set<RateRule>();
public DbSet<RateSeason> RateSeasons => Set<RateSeason>();
public DbSet<GuestProfile> GuestProfiles => Set<GuestProfile>();
public DbSet<GuestDocument> GuestDocuments => Set<GuestDocument>();
public DbSet<GuestPreference> GuestPreferences => Set<GuestPreference>();
public DbSet<VipProfile> VipProfiles => Set<VipProfile>();
public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();
public DbSet<GroupReservation> GroupReservations => Set<GroupReservation>();
public DbSet<GroupRoomAssignment> GroupRoomAssignments => Set<GroupRoomAssignment>();
public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
public DbSet<ServiceHistory> ServiceHistories => Set<ServiceHistory>();
public DbSet<WakeUpCall> WakeUpCalls => Set<WakeUpCall>();
public DbSet<WakeUpCallHistory> WakeUpCallHistories => Set<WakeUpCallHistory>();
public DbSet<LostFoundItem> LostFoundItems => Set<LostFoundItem>();
public DbSet<LostFoundHistory> LostFoundHistories => Set<LostFoundHistory>();
public DbSet<HousekeepingTask> HousekeepingTasks => Set<HousekeepingTask>();
public DbSet<RoomHousekeepingState> RoomHousekeepingStates => Set<RoomHousekeepingState>();
public DbSet<ProcessInstance> FrontOfficeProcesses => Set<ProcessInstance>();
public DbSet<ProcessStep> FrontOfficeProcessSteps => Set<ProcessStep>();
public DbSet<ProcessSignal> FrontOfficeProcessSignals => Set<ProcessSignal>();
public DbSet<ProcessTimeout> FrontOfficeProcessTimeouts => Set<ProcessTimeout>();
public DbSet<ProcessHistory> FrontOfficeProcessHistories => Set<ProcessHistory>();
public DbSet<ProcessDeadLetter> FrontOfficeProcessDeadLetters => Set<ProcessDeadLetter>();
public DbSet<ProcessCompensation> FrontOfficeProcessCompensations => Set<ProcessCompensation>();
public DbSet<ProcessManualAction> FrontOfficeProcessManualActions => Set<ProcessManualAction>();
public DbSet<NightAudit> NightAudits => Set<NightAudit>();
public DbSet<NightAuditStepExecution> NightAuditStepExecutions => Set<NightAuditStepExecution>();
public DbSet<BusinessDateRecord> BusinessDateRecords => Set<BusinessDateRecord>();
public DbSet<RevenueSnapshot> RevenueSnapshots => Set<RevenueSnapshot>();
public DbSet<CashierShift> CashierShifts => Set<CashierShift>();
public DbSet<CashMovement> CashMovements => Set<CashMovement>();
```

### Phase 2: Add FO DI registrations
### Phase 3: Restore entity modifications
### Phase 4: Build & test
