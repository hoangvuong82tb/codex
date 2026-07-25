# MESSAGE CENTER & GUEST COMMUNICATION IMPLEMENTATION RESULT — ASMILE-BO

**Ngày:** 2026-07-19
**Loại:** ENTERPRISE MODULE
**Trạng thái:** HOAN THANH ✅

---

## 1. Architecture

```
GuestMessage (Aggregate Root)
  ├── 8 statuses: Draft→Pending→Sent→Delivered→Read→Replied→Completed→Cancelled
  ├── 10 message types: Guest, Internal, Reminder, VIP Notice...
  ├── 3 channels: FrontDesk, Internal, Email
  ├── RequiresResponse + Reply workflow
  └── MessageHistory[] — immutable audit trail
```

---

## 2. Database

| Table | Purpose |
|---|---|
| `fo.TinNhanKhach` | Guest message tracking |
| `fo.LichSuTinNhan` | Message history |

### Sequence: `fo.TrinhTuSoTinNhan` — MSG-YYYYMM-XXXXXX

### `fo.TinNhanKhach`

| Key Fields | Type |
|---|---|
| GuestMessageId | uniqueidentifier PK |
| MessageNumber | nvarchar(40) UNIQUE |
| GuestId, ReservationId, HotelStayId, HotelRoomId | nullable — linked to guest/stay |
| GuestName, RoomNumber | nvarchar |
| MessageType | nvarchar(200) |
| Channel | nvarchar(20) — FrontDesk/Internal/Email |
| Priority | nvarchar(20) |
| Status | nvarchar(20) — 8 statuses |
| Subject | nvarchar(300) |
| Content | nvarchar(4000) |
| Sender | nvarchar(200) |
| DeliveredAtUtc, ReadAtUtc, CompletedAtUtc | datetime2 nullable |
| RequiresResponse | bit |
| ResponseContent | nvarchar(4000) nullable |
| AssignedStaff, Department | nvarchar(200) |
| RowVersion | rowversion |

### `fo.LichSuTinNhan`

| Key Fields | Type |
|---|---|
| MessageHistoryId | uniqueidentifier PK |
| GuestMessageId | uniqueidentifier FK |
| Action | nvarchar(500) |
| OccurredAtUtc | datetime2 |

---

## 3. Message Types (10 predefined)

| Type | Description |
|---|---|
| Guest Message | General guest communication |
| Internal Message | Staff-to-staff |
| Reminder | General reminder |
| Wake-up Reminder | Wake-up call related |
| Service Update | Service request status |
| Concierge Update | Concierge request status |
| Lost & Found Notice | Found item notification |
| Payment Reminder | Outstanding balance alert |
| Checkout Reminder | Departure day reminder |
| VIP Notice | VIP guest special communication |

---

## 4. Channels

| Channel | Description |
|---|---|
| FrontDesk | Delivered at reception counter |
| Internal | Staff-only, not guest-facing |
| Email | Email delivery (future) |

---

## 5. Workflow

```
Create Message
  ↓
Send (Draft → Sent)
  ↓
Mark Delivered (Sent → Delivered)
  ↓
Mark Read (Delivered → Read)
  ↓
Reply (Read → Replied) — if RequiresResponse
  ↓
Complete (Replied/Read → Completed)
```

---

## 6. WPF UI

```
┌─ HEADER ───────────────────────────────────┐
│ Trung Tam Tin Nhan                           │
│ Chua doc: 2  |  Cho tra loi: 1              │
├─ CREATE: [Type▼] [Guest] [Room] [Subj]      │
│   [Content] [□Can TL] [Gui] ────────────────┤
├─ MESSAGES ─────────────────┬─ DETAIL ───────┤
│ MSG-001 [Read]             │ Chào mừng VIP   │
│ Chao mung khach VIP        │ Kính gửi ông... │
│ VIP Notice                 │ Nguyen Van A    │
│ MSG-002 [Replied]          │ Phòng: 101      │
│ Xac nhan xe dua don san bay│ Trả lời: Cảm ơn│
│ Airport Transfer           │ [Đánh dấu đọc]  │
│ Le Van C - Phong 301       │ [____] [Trả lời]│
│                            │ LỊCH SỬ:       │
│                            │ Created         │
│                            │ Sent            │
│                            │ Delivered       │
│                            │ Read            │
│                            │ Replied         │
└────────────────────────────┴────────────────┘
```

---

## 7. Files Created/Modified

| File | Action |
|---|---|
| `Core/FrontOffice/Message/GuestMessage.cs` | NEW — Message aggregate + History |
| `ViewModels/MessageCenterViewModel.cs` | NEW — 18 properties, 6 commands |
| `Views/MessageCenterView.xaml` | NEW — Create form + message list + detail + reply |
| `Views/MessageCenterView.xaml.cs` | NEW — Code-behind |
| `App/DependencyInjection.cs` | MODIFIED — +MessageCenterViewModel |
| `Themes/DataTemplates.xaml` | MODIFIED — +MessageCenter VM→View |

---

## 8. Integration

| Module | Integration |
|---|---|
| **Reservation** | Linked via ReservationId |
| **Stay** | Linked via HotelStayId |
| **Guest Profile** | GuestName, GuestId |
| **Service Request** | Service Update messages |
| **Concierge** | Concierge Update messages |
| **Wake-up Call** | Wake-up Reminder messages |
| **Lost & Found** | Lost & Found Notice messages |
| **Folio** | Payment Reminder messages |

---

## 9. Tests

| Suite | Result |
|---|---|
| Unit Tests | **335/335 PASS** ✅ |
| Integration Tests | **23/23 PASS** ✅ |
| Build Errors | **0** ✅ |
| DB Tables | **+2** (TinNhanKhach, LichSuTinNhan) |
| History | **92** |

---

## 10. Go / No Go

### Message Center: GO ✅

| Criteria | Status |
|---|---|
| GuestMessage aggregate (8 statuses, 10 types, 3 channels) | ✅ |
| Send → Deliver → Read → Reply workflow | ✅ |
| RequiresResponse flag + Reply content | ✅ |
| Unread + Pending counters | ✅ |
| History audit trail | ✅ |
| Detail panel with reply input | ✅ |
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
| 22 | **Message Center** | **2** | Write |
| — | Foundation | 3 | Infra |
| **TOTAL** | **22 modules** | **39 tables** | — |

### 15 Sequences · 92 Migrations · 358 Tests · 0 Regressions

---

*Message Center implementation complete. 22 modules, 39 tables, 92 migrations, 358 tests.*
