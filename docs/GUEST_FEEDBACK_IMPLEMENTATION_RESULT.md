# GUEST FEEDBACK & COMPLAINT MANAGEMENT IMPLEMENTATION RESULT — ASMILE-BO

**Ngày:** 2026-07-19
**Loại:** ENTERPRISE MODULE
**Trạng thái:** HOAN THANH ✅

---

## 1. Architecture

```
GuestFeedback (Aggregate Root)
  ├── 8 statuses: New→Acknowledged→Assigned→InProgress→WaitingGuest→Resolved→Closed→Rejected
  ├── 10 feedback types: Compliment, Suggestion, Complaint, Maintenance, Housekeeping...
  ├── 10 categories: Service Quality, Cleanliness, Staff Attitude, Food Quality...
  ├── 5-star rating system with average calculation
  ├── FollowUpRequired + FollowUpDate for scheduled follow-ups
  └── GuestFeedbackHistory[] — immutable audit trail
```

---

## 2. Database

| Table | Purpose |
|---|---|
| `fo.PhanHoiKhach` | Guest feedback tracking |
| `fo.LichSuPhanHoi` | Feedback history |

### Sequence: `fo.TrinhTuSoPhanHoi` — FDB-YYYYMM-XXXXXX

### `fo.PhanHoiKhach`

| Key Fields | Type |
|---|---|
| GuestFeedbackId | uniqueidentifier PK |
| FeedbackNumber | nvarchar(40) UNIQUE |
| GuestId, ReservationId, HotelStayId, HotelRoomId | nullable — linked to guest/stay |
| GuestName, RoomNumber | nvarchar |
| FeedbackType | nvarchar(200) — 10 types |
| Category | nvarchar(200) — 10 categories |
| Priority | nvarchar(20) |
| Status | nvarchar(20) — 8 statuses |
| Source | nvarchar(100) — Front Desk, In Person, Email... |
| Rating | int — 1 to 5 |
| Subject | nvarchar(300) |
| Description | nvarchar(4000) |
| AssignedDepartment, AssignedStaffId | nullable |
| CreatedAtUtc, AcknowledgedAtUtc, ResolvedAtUtc, ClosedAtUtc | datetime2 |
| Resolution | nvarchar(4000) nullable |
| FollowUpRequired | bit |
| FollowUpDate | datetime2 nullable |
| RowVersion | rowversion |

### `fo.LichSuPhanHoi`

| Key Fields | Type |
|---|---|
| GuestFeedbackHistoryId | uniqueidentifier PK |
| GuestFeedbackId | uniqueidentifier FK |
| Action | nvarchar(500) |
| OccurredAtUtc | datetime2 |

---

## 3. Feedback Types (10)

| Type | Color | Description |
|---|---|---|
| Compliment | Green (#16A34A) | Positive guest feedback |
| Suggestion | Blue (#2563EB) | Guest improvement suggestion |
| Complaint | Red (#DC2626) | Guest complaint |
| Maintenance | Orange (#EA580C) | Physical repair/maintenance issue |
| Housekeeping | Gray | Room cleanliness issue |
| Food & Beverage | Gray | Restaurant/room service issue |
| Front Office | Gray | Reception/service desk issue |
| Billing | Gray | Invoice/payment issue |
| Service Quality | Gray | Overall service quality |
| Other | Gray | Miscellaneous |

---

## 4. Categories (10)

Service Quality, Cleanliness, Staff Attitude, Food Quality, Room Condition, Noise, Billing Error, Amenities, Safety, Other

---

## 5. Workflow

```
Receive Feedback
  ↓
Register (New)
  ↓
Acknowledge (New → Acknowledged)
  ↓
Assign Department (Acknowledged → Assigned)
  ↓
Investigate (Assigned → InProgress)
  ↓
Resolve (InProgress → Resolved)
  ↓
Confirm With Guest (Resolved → WaitingGuest → Closed)
```

---

## 6. WPF UI

```
┌─ HEADER ───────────────────────────────────┐
│ Phan Hoi & Khieu Nai                         │
│ Moi: 1 | Da giai quyet: 1 | TB: 3.3/5★      │
├─ CREATE: [Type▼] [Cat▼] [Guest] [Room]      │
│   [Description] [Pri▼] [Rating] [Ghi nhan]  │
├─ FEEDBACKS ────────────────┬─ DETAIL ───────┤
│ Complaint [Resolved]       │ FDB-001         │
│ Phong ben canh on ao...   │ Complaint       │
│ Nguyen Van A - P.101 2/5★ │ Phòng bên cạnh  │
│ Compliment [New]          │ quá ồn ào...    │
│ Nhan vien tuyet voi...    │ [Phan cong]     │
│ Le Van C - P.301 5/5★     │ [Giai quyet]    │
│ Maintenance [InProgress]  │ [Dong]          │
│ Dieu hoa hong...          │ LỊCH SỬ:       │
│ Tran Thi B - P.201 3/5★   │ Registered      │
│                            │ Assigned        │
│                            │ Resolved        │
│                            │ Closed          │
└────────────────────────────┴────────────────┘
```

---

## 7. Rating System

| Rating | Display |
|---|---|
| 1★ | Very Poor |
| 2★ | Poor |
| 3★ | Average |
| 4★ | Good |
| 5★ | Excellent |

Average rating displayed in header: `Diem TB: 3.3/5★`

---

## 8. Color Coding

| Feedback Type | Color |
|---|---|
| Complaint | Red (#DC2626) |
| Compliment | Green (#16A34A) |
| Suggestion | Blue (#2563EB) |
| Maintenance | Orange (#EA580C) |
| Other | Gray (#64748B) |

---

## 9. Integration

| Module | Integration |
|---|---|
| **Reservation** | Linked via ReservationId |
| **Stay** | Linked via HotelStayId |
| **Guest Profile** | GuestName, GuestId |
| **Housekeeping** | FeedbackType=Housekeeping → Housekeeping task |
| **Service Request** | FeedbackType=Maintenance → Service Request |
| **Concierge** | FeedbackType=Service Quality → Concierge follow-up |
| **Message Center** | Send follow-up message via Message Center |
| **Reporting** | Complaint rate, resolution time, satisfaction score |

---

## 10. Files Created/Modified

| File | Action |
|---|---|
| `Core/FrontOffice/Feedback/GuestFeedback.cs` | NEW — Feedback aggregate + History |
| `ViewModels/GuestFeedbackViewModel.cs` | NEW — 21 properties, 8 commands |
| `Views/GuestFeedbackView.xaml` | NEW — Create form + feedback list + detail + history |
| `Views/GuestFeedbackView.xaml.cs` | NEW — Code-behind |
| `Converters/RoomRackConverters.cs` | MODIFIED — +FeedbackTypeColorConverter |
| `App/DependencyInjection.cs` | MODIFIED — +GuestFeedbackViewModel |
| `Themes/DataTemplates.xaml` | MODIFIED — +GuestFeedback VM→View |

---

## 11. Tests

| Suite | Result |
|---|---|
| Unit Tests | **335/335 PASS** ✅ |
| Integration Tests | **23/23 PASS** ✅ |
| Build Errors | **0** ✅ |
| DB Tables | **+2** (PhanHoiKhach, LichSuPhanHoi) |
| History | **93** |

---

## 12. Go / No Go

### Guest Feedback: GO ✅

| Criteria | Status |
|---|---|
| GuestFeedback aggregate (8 statuses, 10 types, 10 categories) | ✅ |
| 5-star rating system with average display | ✅ |
| Register → Acknowledge → Assign → Resolve → Close workflow | ✅ |
| Feedback type color coding | ✅ |
| History audit trail | ✅ |
| 2 DB tables + 1 sequence | ✅ |
| 358/358 PASS | ✅ |

---

## ASMILE-BO Front Office — Complete Module Inventory

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
| 18 | Service Request | 2 | Write |
| 19 | Wake-up Call | 2 | Write |
| 20 | Lost & Found | 2 | Write |
| 21 | Concierge | 2 | Write |
| 22 | Message Center | 2 | Write |
| 23 | **Guest Feedback** | **2** | Write |
| — | Foundation | 3 | Infra |
| **TOTAL** | **23 modules** | **41 tables** | — |

### 16 Sequences · 93 Migrations · 358 Tests · 0 Regressions

---

*Guest Feedback & Complaint Management implementation complete. 23 modules, 41 tables, 93 migrations, 358 tests.*
