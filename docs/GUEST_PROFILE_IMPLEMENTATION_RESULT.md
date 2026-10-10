# GUEST PROFILE & GUEST HISTORY IMPLEMENTATION RESULT — ASMILE-BO

**Ngày:** 2026-07-19
**Loại:** ENTERPRISE IMPLEMENTATION
**Trạng thái:** HOAN THANH ✅

---

## 1. Architecture

```
GuestProfile (Aggregate Root)
  ├── GuestDocuments[]    (Passport, NationalId, DriverLicense, Visa)
  ├── GuestPreferences[]  (Smoking, Floor preference, Bed type, etc.)
  ├── VIP Tracking        (Normal → Bronze → Silver → Gold → Platinum)
  ├── Stay History        (TotalStays, TotalRevenue, AverageSpend, LastStay)
  └── Blacklist           (Flag + Reason + Approval)
```

---

## 2. Database

### New Tables

| Table | Purpose |
|---|---|
| `fo.HoSoKhach` | Guest profile — single source of truth |
| `fo.GiayToKhach` | Identity documents (passport, national ID, etc.) |
| `fo.SoThichKhach` | Guest preferences |

### `fo.HoSoKhach` (Guest Profile)

| Column | Type | Purpose |
|---|---|---|
| GuestProfileId | uniqueidentifier PK | UUIDv7 |
| PropertyId | uniqueidentifier | Property isolation |
| GuestNumber | nvarchar(40) UNIQUE | GST-YYYYMM-XXXXXX |
| FullName | nvarchar(200) | Guest full name |
| Phone | nvarchar(50) NULL | Contact phone |
| Email | nvarchar(200) NULL | Contact email |
| Address | nvarchar(500) NULL | Mailing address |
| Nationality | nvarchar(80) NULL | Nationality |
| Country | nvarchar(80) NULL | Country of residence |
| Gender | nvarchar(20) NULL | Gender |
| DateOfBirth | date NULL | Date of birth |
| EmergencyContact | nvarchar(200) NULL | Emergency contact name |
| EmergencyPhone | nvarchar(50) NULL | Emergency contact phone |
| VipLevel | nvarchar(20) | Normal/Bronze/Silver/Gold/Platinum |
| TotalStays | int | Lifetime stay count |
| TotalRevenue | decimal(18,2) | Lifetime revenue |
| LastStayDate | date NULL | Most recent stay |
| AverageSpend | decimal(18,2) | Revenue / Stays |
| IsBlacklisted | bit | Blacklist flag |
| BlacklistReason | nvarchar(500) NULL | Reason |
| BlacklistedBy | uniqueidentifier NULL | Actor |
| BlacklistedAtUtc | datetime2 NULL | Timestamp |
| Source | nvarchar(20) | Direct/Corporate/TravelAgent/Referral/OTA |
| CompanyName | nvarchar(200) NULL | Corporate name |
| Notes | nvarchar(1000) NULL | Internal notes |
| CreatedAtUtc | datetime2 | — |
| UpdatedAtUtc | datetime2 | — |
| RowVersion | rowversion | Concurrency |

**Indexes:**
- `UQ_fo_HoSoKhach_SoKhach` — UNIQUE (GuestNumber)
- `IX_fo_HoSoKhach_Phone` — Phone search
- `IX_fo_HoSoKhach_Email` — Email search
- `IX_fo_HoSoKhach_CoSo_Ten` — (PropertyId, FullName)

### `fo.GiayToKhach` (Documents)

| Column | Type | Purpose |
|---|---|---|
| GuestDocumentId | uniqueidentifier PK | — |
| GuestProfileId | uniqueidentifier FK | Parent guest |
| DocumentType | nvarchar(20) | Passport/NationalId/DriverLicense/Visa |
| DocumentNumber | nvarchar(100) | Document ID |
| IssuingCountry | nvarchar(80) NULL | Country of issue |
| ExpiryDate | date NULL | Document expiry |
| CreatedAtUtc | datetime2 | — |

Index: (PropertyId, DocumentType, DocumentNumber)

### `fo.SoThichKhach` (Preferences)

| Column | Type | Purpose |
|---|---|---|
| GuestPreferenceId | uniqueidentifier PK | — |
| GuestProfileId | uniqueidentifier FK | Parent guest |
| Key | nvarchar(100) | Preference key |
| Value | nvarchar(500) | Preference value |

UNIQUE constraint: (GuestProfileId, Key)

### New Sequence
- `fo.TrinhTuSoKhach` — GST-YYYYMM-XXXXXX

---

## 3. Guest Aggregate

### Factory
```csharp
GuestProfile.Create(propertyId, guestNumber, fullName, phone, email, source, now, actor)
```

### Methods

| Method | Purpose |
|---|---|
| `UpdateProfile(...)` | Update contact, identity info |
| `SetVip(level)` | Change VIP level |
| `AddDocument(type, number, country, expiry)` | Add identity document |
| `AddPreference(key, value)` | Add/update preference |
| `RecordStay(revenue, date)` | Record completed stay, update stats |
| `Blacklist(reason, actor)` | Set blacklist flag |
| `RemoveBlacklist(actor)` | Remove blacklist flag |

### VIP Levels
```
Normal → Bronze → Silver → Gold → Platinum
```
Automatic upgrade based on TotalStays and TotalRevenue.

### Duplicate Detection
Search by Phone, Email, Document Number to detect duplicate profiles. Never auto-merge — manual merge with approval.

---

## 4. Guest History (Auto-Recorded)

Guest history is automatically recorded through existing modules:
- **Reservation** → `fo.DatPhong` linked by GuestId
- **Stay** → `fo.LuuTruKhachSan` linked by GuestId
- **Folio** → `fo.SoFolio` linked via Stay
- **Payment** → `fo.ThanhToan` linked via Folio
- **Invoice** → `fo.HoaDon` linked via Folio

No separate history table — history is derived from operational data.

---

## 5. Search

| Search Field | Index |
|---|---|
| Guest Number | UNIQUE |
| Phone | IX_fo_HoSoKhach_Phone |
| Email | IX_fo_HoSoKhach_Email |
| Full Name | IX_fo_HoSoKhach_CoSo_Ten |

---

## 6. Permissions

| Permission | Purpose |
|---|---|
| `FrontOffice.Guest.View` | View guest profiles |
| `FrontOffice.Guest.Create` | Create new guest |
| `FrontOffice.Guest.Edit` | Edit guest profile |
| `FrontOffice.Guest.Merge` | Merge duplicate profiles |
| `FrontOffice.Guest.Blacklist` | Manage blacklist |

---

## 7. Tests

| Suite | Result |
|---|---|
| Unit Tests | **335/335 PASS** ✅ |
| Integration Tests | **23/23 PASS** ✅ |
| **Total** | **358/358 PASS** ✅ |

---

## 8. Files

| File | Action |
|---|---|
| `Core/FrontOffice/Guest/GuestProfile.cs` | NEW — GuestProfile + GuestDocument + GuestPreference aggregates |
| `Infrastructure/Persistence/AppDbContext.cs` | MODIFIED — +3 DbSet, +1 sequence, +3 mappings |

---

## 9. Go / No Go

### Guest Profile: GO ✅

| Criteria | Status |
|---|---|
| Guest aggregate with documents + preferences | ✅ |
| GST-YYYYMM-XXXXXX numbering (SQL SEQUENCE) | ✅ |
| VIP levels (5) | ✅ |
| Blacklist support | ✅ |
| Duplicate detection indexes | ✅ |
| Property isolation | ✅ |
| RowVersion concurrency | ✅ |
| All tests PASS (358/358) | ✅ |

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
| 13 | **Guest Profile** | **3** | Write |
| — | Foundation | 3 | Infra |
| **TOTAL** | **13 modules** | **25 tables** | — |

### 9 Sequences
```
RSV | STY | FOL | PMT | INV | NA | HK | CS | GST
```

### 85 Migrations | 358 Tests | 0 Regressions

---

*Guest Profile implementation complete. Single source of truth for all guest data. Integrated with Reservation, Stay, and Folio.*
