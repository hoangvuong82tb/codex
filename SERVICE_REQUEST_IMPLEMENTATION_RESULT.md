# SERVICE REQUEST / GUEST REQUEST IMPLEMENTATION RESULT — ASMILE-BO

**Ngày:** 2026-07-19
**Loại:** ENTERPRISE MODULE
**Trạng thái:** HOAN THANH ✅

---

## 1. Architecture

```
ServiceRequest (Aggregate Root)
  ├── ServiceHistory[] — immutable audit trail
  ├── 7 statuses: Open → Assigned → Accepted → InProgress → Completed/Cancelled/Rejected
  ├── 5 priorities: Low → Normal → High → Urgent → Emergency
  ├── 9 categories: Housekeeping, Engineering, FrontOffice, Laundry, Transportation...
  └── 3 charge types: Complimentary, ImmediateCharge, ChargeAtCheckout
```

---

## 2. Database

### New Tables

| Table | Purpose |
|---|---|
| `fo.YeuCauDichVu` | Service request tracking |
| `fo.LichSuYeuCau` | Immutable request history |

### New Sequence

`fo.TrinhTuSoYeuCau` — SRV-YYYYMM-XXXXXX

### `fo.YeuCauDichVu`

| Key Fields | Type |
|---|---|
| ServiceRequestId | uniqueidentifier PK |
| RequestNumber | nvarchar(40) UNIQUE |
| HotelStayId, HotelRoomId, RoomNumber | nullable — linked to stay/room |
| GuestName | nvarchar(200) |
| ServiceType | nvarchar(200) |
| Category | nvarchar(20) — 9 categories |
| Priority | nvarchar(20) — 5 levels |
| Status | nvarchar(20) — 7 statuses |
| Description | nvarchar(1000) |
| AssignedDepartment, AssignedStaffId | nullable |
| EstimatedCost, ActualCost | decimal(18,2) |
| ChargeType | nvarchar(20) — 3 types |
| FolioEntryId | nullable — linked to folio charge |
| RowVersion | rowversion |

### `fo.LichSuYeuCau`

| Key Fields | Type |
|---|---|
| ServiceHistoryId | uniqueidentifier PK |
| ServiceRequestId | uniqueidentifier FK |
| Action | nvarchar(200) |
| Detail | nvarchar(500) nullable |
| OccurredAtUtc | datetime2 |

---

## 3. Service Types (15 predefined, unlimited custom)

Extra Bed, Baby Cot, Extra Pillow, Wake-up Call, Airport Pickup, Laundry, Room Cleaning, Late Checkout, Early Check-in, Technical Support, Food Delivery, Birthday Decoration, Flowers, Taxi, Iron

---

## 4. WPF UI

### Layout

```
┌─ HEADER: Yeu Cau Dich Vu ─────────────────────┐
│ Mo: 1  |  Dang lam: 1  |  Xong: 1   [Lam moi] │
├─ CREATE FORM ──────────────────────────────────┤
│ [Service ▼] [Room] [Guest] [Cat ▼] [Pri ▼]   │
│ [Charge ▼] [Cost] [Tao Yeu Cau]               │
├─ REQUEST LIST ────────────────┬─ DETAIL ───────┤
│ SRV-001 [Completed] [Normal]  │ CHI TIET       │
│ Extra Pillow - Phong 101      │ SRV-001        │
│ SRV-002 [InProgress] [High]   │ Extra Pillow   │
│ Airport Pickup - Phong 103    │ Nguyen Van A   │
│ SRV-003 [Open] [Urgent]       │ [Phan cong]    │
│ Room Cleaning - Phong 201     │ [Bat đau]      │
│                               │ [Hoan thanh]   │
│                               │ [Huy]           │
│                               │ LICH SU:       │
│                               │ Created         │
│                               │ Assigned        │
│                               │ Completed       │
└───────────────────────────────┴────────────────┘
```

---

## 5. Workflow Toolbar

| Button | Status Transition |
|---|---|
| Phan cong | Open → Assigned |
| Bat đau | Assigned → InProgress |
| Hoan thanh | InProgress → Completed |
| Huy | Any → Cancelled |

---

## 6. Color Coding

| Status | Color |
|---|---|
| Open | Orange (#EA580C) |
| Assigned | Purple (#7C3AED) |
| InProgress | Blue (#2563EB) |
| Completed | Green (#16A34A) |
| Cancelled | Gray (#94A3B8) |

| Priority | Color |
|---|---|
| Emergency | Red (#DC2626) |
| Urgent | Orange (#EA580C) |
| High | Gold (#F2994A) |
| Normal | Blue (#2563EB) |
| Low | Gray (#94A3B8) |

---

## 7. Integration

| Module | Integration |
|---|---|
| **Folio** | Chargeable services link to `FolioEntryId` |
| **Stay** | Linked via `HotelStayId` |
| **Room** | Linked via `HotelRoomId` + `RoomNumber` |
| **Guest Profile** | `GuestName` snapshot |
| **Reporting** | Service volume, revenue, completion time |

---

## 8. Files

| File | Action |
|---|---|
| `Core/FrontOffice/ServiceRequest/ServiceRequest.cs` | NEW — Aggregate + History |
| `Infrastructure/Persistence/AppDbContext.cs` | MODIFIED — +2 DbSet, +1 sequence, +2 mappings |
| `ViewModels/ServiceRequestViewModel.cs` | NEW — 20 properties, 10 commands |
| `Views/ServiceRequestView.xaml` | NEW — Create form + request list + detail panel |
| `Views/ServiceRequestView.xaml.cs` | NEW — Code-behind |
| `Converters/RoomRackConverters.cs` | MODIFIED — +StatusToColor, +PriorityToColor |
| `App/DependencyInjection.cs` | MODIFIED — +ServiceRequestViewModel |
| `Themes/DataTemplates.xaml` | MODIFIED — +ServiceRequest VM→View |

---

## 9. Tests

| Suite | Result |
|---|---|
| Unit Tests | **335/335 PASS** ✅ |
| Integration Tests | **23/23 PASS** ✅ |
| Build Errors | **0** ✅ |
| DB Tables | **+2** (YeuCauDichVu, LichSuYeuCau) |
| History | **88** |

---

## 10. Go / No Go

### Service Request: GO ✅

| Criteria | Status |
|---|---|
| ServiceRequest aggregate (7 statuses, 5 priorities, 9 categories) | ✅ |
| 2 database tables + 1 sequence | ✅ |
| WPF: Create form + request list + detail panel + history | ✅ |
| Workflow toolbar (Assign/Start/Complete/Cancel) | ✅ |
| 15 predefined service types + custom | ✅ |
| Color-coded status + priority badges | ✅ |
| Folio integration (ChargeType + FolioEntryId) | ✅ |
| All tests PASS (358/358) | ✅ |

---

## ASMILE-BO Front Office — Final Module Inventory

| # | Module | Tables | Type |
|---|---|---|---|
| 1 | Reservation | 2 | Write |
| 2 | Hotel Master Data | 2 | Write |
| 3 | Hotel Stay | 2 | Write |
| 4 | Folio | 2 | Write |
| 5 | Payment | 2 | Write |
| 6 | Invoice | 1 | Write |
| 7 | Night Audit | 2 | Write |
| 8 | Business Date | 1 | Write |
| 9 | Revenue | 1 | Write |
| 10 | Housekeeping | 2 | Write |
| 11 | Cashier | 2 | Write |
| 12 | Reporting | 0 | Read |
| 13 | Guest Profile | 3 | Write |
| 14 | Room Rack | 0 | Read |
| 15 | Tape Chart | 0 | Read |
| 16 | Group Reservation | 2 | Write |
| 17 | Rate Engine | 4 | Write |
| 18 | **Service Request** | **2** | Write |
| — | Foundation | 3 | Infra |
| **TOTAL** | **18 modules** | **31 tables** | — |

### 11 Sequences · 88 Migrations · 358 Tests · 0 Regressions

---

*Service Request module complete. 18 modules, 31 tables, 88 migrations, 358 tests.*
