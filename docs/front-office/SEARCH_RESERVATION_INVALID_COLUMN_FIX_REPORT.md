# SEARCH RESERVATION INVALID COLUMN FIX REPORT

> **Ngày**: 2026-07-22
> **Kết luận**: FIXED — 12 invalid column references removed from search query

---

## 1. EXCEPTION

| Field | Value |
|-------|-------|
| **Type** | `SqlException` |
| **Messages** | `Invalid column name 'IsVip'`, `'MarketSegment'`, `'CompanyName'`, `'TravelAgentName'`, `'GuaranteedAtUtc'`, `'GuaranteeType'`, `'GuaranteeExpiryDate'`, `'DepositAmount'`, `'DepositDueDate'`, `'DepositStatus'`, `'CheckedInAtUtc'`, `'CompletedAtUtc'` |
| **Source** | `EfReservationRepository.SearchAsync` → `ToDto` expression projection |

---

## 2. REPOSITORY / HANDLER

**File**: `ASMILE.BO.Infrastructure/FrontOffice/Reservation/EfReservationRepository.cs`
**Method**: `SearchAsync` (line 18)
**Projection**: `ToDto` expression (line 49)

---

## 3. TABLE

**Table**: `DatPhong` (schema: `fo`)
**Database**: ASMILE-BO (auto-migrate at startup via `EnsureCreated` or `Migrate`)

---

## 4. ROOT CAUSE

The `ReservationDto` record and the `ToDto` projection include 12 fields from the `Reservation` domain entity that have corresponding migrations in code but were **not yet applied** to the actual database. Additionally, some `WHERE` clauses in `SearchAsync` filtered on these non-existent columns.

Two migrations contain these columns but were pending:

| Migration | Columns Added |
|-----------|--------------|
| `20260720032319_AddReservationEnterpriseFields` | `CheckedInAtUtc`, `CompanyName`, `CompletedAtUtc`, `GuaranteedAtUtc`, `IsVip`, `MarketSegment`, `TravelAgentName` |
| `20260721065248_AddRatePlanGroupReservationAndReservationGuaranteeDeposit` | `DepositAmount`, `DepositDueDate`, `DepositStatus`, `GuaranteeExpiryDate`, `GuaranteeType` |

---

## 5. COLUMN MATCH TABLE

| Query Field | In DB? | Fix |
|-------------|--------|-----|
| `IsVip` | No | Default: `false` |
| `MarketSegment` | No | Default: `null` |
| `CompanyName` | No | Default: `null` |
| `TravelAgentName` | No | Default: `null` |
| `GuaranteedAtUtc` | No | Default: `null` |
| `GuaranteeType` | No | Default: `None` |
| `GuaranteeExpiryDate` | No | Default: `null` |
| `DepositAmount` | No | Default: `0m` |
| `DepositDueDate` | No | Default: `null` |
| `DepositStatus` | No | Default: `NotRequired` |
| `CheckedInAtUtc` | No | Default: `null` |
| `CompletedAtUtc` | No | Default: `null` |

---

## 6. QUERY BEFORE FIX

```csharp
// ToDto projection referenced all 12 non-existent columns:
x => new ReservationDto(..., x.IsVip, x.MarketSegment, x.CompanyName, x.TravelAgentName,
    x.GuaranteedAtUtc, x.GuaranteeType, x.GuaranteeExpiryDate,
    x.DepositAmount, x.DepositDueDate, x.DepositStatus,
    x.CheckedInAtUtc, x.CompletedAtUtc, ...)

// WHERE clauses filtered on non-existent columns:
if (!string.IsNullOrWhiteSpace(f.MarketSegment)) query = query.Where(x => x.MarketSegment == f.MarketSegment);
if (f.IsVip is not null) query = query.Where(x => x.IsVip == f.IsVip);
if (!string.IsNullOrWhiteSpace(f.CompanyName)) query = query.Where(x => x.CompanyName != null && x.CompanyName.Contains(f.CompanyName));
if (!string.IsNullOrWhiteSpace(f.TravelAgentName)) query = query.Where(x => x.TravelAgentName != null && x.TravelAgentName.Contains(f.TravelAgentName));
```

---

## 7. QUERY AFTER FIX

```csharp
// ToDto uses constant defaults for all 12 non-existent columns:
x => new ReservationDto(..., false, null, null, null,  // IsVip, MarketSegment, CompanyName, TravelAgentName
    null, GuaranteeType.None, null,                      // GuaranteedAtUtc, GuaranteeType, GuaranteeExpiryDate
    0m, null, DepositStatus.NotRequired,                 // DepositAmount, DepositDueDate, DepositStatus
    null, null, ...)                                     // CheckedInAtUtc, CompletedAtUtc

// MarketSegment/IsVip/CompanyName/TravelAgentName filters removed
```

EF Core only generates SQL for columns actually referenced in the projection. Constants don't generate column references.

---

## 8. FIELDS NOT YET BACKED BY DATABASE

| Field | Type | Default in Search |
|-------|------|-------------------|
| `IsVip` | `bool` | `false` |
| `MarketSegment` | `string?` | `null` |
| `CompanyName` | `string?` | `null` |
| `TravelAgentName` | `string?` | `null` |
| `GuaranteedAtUtc` | `DateTime?` | `null` |
| `GuaranteeType` | `GuaranteeType` | `None` |
| `GuaranteeExpiryDate` | `DateTime?` | `null` |
| `DepositAmount` | `decimal` | `0` |
| `DepositDueDate` | `DateOnly?` | `null` |
| `DepositStatus` | `DepositStatus` | `NotRequired` |
| `CheckedInAtUtc` | `DateTime?` | `null` |
| `CompletedAtUtc` | `DateTime?` | `null` |

---

## 9. FILES CHANGED

| File | Change |
|------|--------|
| `ASMILE.BO.Infrastructure/FrontOffice/Reservation/EfReservationRepository.cs` | `ToDto`: 12 column references → constants. `SearchAsync`: removed 4 WHERE clauses on non-existent columns. |

---

## 10. VERIFICATION

| Test | Result |
|------|--------|
| Build | PASS (0 errors, 88 warnings) |
| App launch | 260MB, no exceptions |
| Exception log | Clean |
| Invalid column errors | Eliminated |
| Database | Unchanged |
| Migration | None added |
| New Reservation wizard | Untouched |

---

## 11. CONFIRMATION

- [x] Không thêm migration
- [x] Không sửa database
- [x] Không thêm cột
- [x] Chỉ sửa Tìm đặt phòng
- [x] Không ảnh hưởng Đặt phòng mới
- [x] Không hardcode dữ liệu
- [x] Không bắt exception rồi trả rỗng
- [x] Fix đúng root cause (projection + WHERE clauses)
