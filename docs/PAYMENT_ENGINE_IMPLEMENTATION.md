# PAYMENT ENGINE IMPLEMENTATION — ASMILE-BO

**Ngày:** 2026-07-19
**Loại:** ENTERPRISE IMPLEMENTATION
**Trạng thái:** HOAN THANH ✅

---

## 1. Architecture

```
Domain                  Application              Infrastructure              Database
HotelPayment          → PaymentHandlers        → EfPaymentRepository       → fo.ThanhToan
  │                     │ ReceiveAsync            │ GetDtoAsync              fo.TrinhTuSoThanhToan (SEQ)
  │                     │ VoidAsync               │ GetForFolioAsync
  │                     │                         │ ApplyExpectedRowVersion
  ▼                     ▼                         │
Folio Integration     PaymentCqrsHandlers       SqlPaymentNumberGenerator
  ↓                     ReceivePaymentCH          (NEXT VALUE FOR seq)
HotelFolio.AddPayment   VoidPaymentCH
  ↓                     GetPaymentQH
HotelFolioEntry         GetPaymentsForFolioQH
  (EntryType.Payment)
```

---

## 2. Domain

### HotelPayment Aggregate

| Feature | Implementation |
|---|---|
| Factory | `Create(propertyId, folioId, number, method, amount, extRef, notes, now, actor)` |
| Link to Folio | `LinkFolioEntry(folioEntryId)` — connects payment to ledger entry |
| Void | `Void(voidEntryId, reason, now, actor)` — creates compensating ledger entry |
| Status | Completed / Voided |
| Methods | Cash, Card, BankTransfer, DepositOffset |

### Ledger Integration

```
Receive Payment:
  HotelPayment.Create() → paymentRepo.AddAsync()
  folio.AddPayment() → creates FolioEntry (EntryType.Payment, negative amount)
  payment.LinkFolioEntry(entry.Id)
  → persistence.CommitAsync() (single transaction)

Void Payment:
  folio.AddAdjustment(originalPayment.Amount) → compensating entry (positive)
  payment.Void(voidEntryId, reason)
  → persistence.CommitAsync() (single transaction)
```

---

## 3. Database

### `fo.ThanhToan`

| Column | Type | Purpose |
|---|---|---|
| HotelPaymentId | uniqueidentifier PK | UUIDv7 |
| PropertyId | uniqueidentifier | Property isolation |
| HotelFolioId | uniqueidentifier | FK reference to folio |
| FolioEntryId | uniqueidentifier NULL | Original ledger entry |
| VoidEntryId | uniqueidentifier NULL | Void compensating entry |
| PaymentNumber | nvarchar(40) UNIQUE | PMT-YYYYMM-XXXXXX |
| PaymentMethod | nvarchar(30) | Cash/Card/BankTransfer/DepositOffset |
| Amount | decimal(18,2) | Amount paid |
| Status | nvarchar(20) | Completed/Voided |
| ExternalReference | nvarchar(200) NULL | Card tx ID, bank ref... |
| Notes | nvarchar(500) NULL | Payment notes |
| CreatedAtUtc | datetime2 | — |
| CreatedBy | uniqueidentifier NULL | — |
| VoidedAtUtc | datetime2 NULL | — |
| VoidedBy | uniqueidentifier NULL | — |
| VoidReason | nvarchar(500) NULL | — |
| RowVersion | rowversion | Concurrency |

### `fo.TrinhTuSoThanhToan`
SQL Server SEQUENCE: `START WITH 1 INCREMENT BY 1`
Format: `PMT-{yyyyMM}-{000001..999999}`

### Indexes
- `UQ_fo_ThanhToan_SoThanhToan` — UNIQUE on PaymentNumber
- `IX_fo_ThanhToan_CoSo_Folio_ThoiGian` — (PropertyId, HotelFolioId, CreatedAtUtc)
- `IX_fo_ThanhToan_CoSo_TrangThai` — (PropertyId, Status)

---

## 4. Application

### Commands

| Command | Permission |
|---|---|
| `ReceivePaymentCommand` | `FrontOffice.Payment.Receive` |
| `VoidPaymentCommand` | `FrontOffice.Payment.Void` |

### Queries

| Query | Permission |
|---|---|
| `GetPaymentQuery` | `FrontOffice.Payment.View` |
| `GetPaymentsForFolioQuery` | `FrontOffice.Payment.View` |

### Receive Payment Flow

1. Authorization: `FrontOffice.Payment.Receive`
2. Context validation: property context required
3. Amount validation: must be positive
4. Folio RowVersion: 8 bytes required
5. Load folio by property scope
6. Apply ExpectedRowVersion on folio
7. Generate payment number via SQL SEQUENCE
8. Create HotelPayment domain object
9. Add FolioEntry via `folio.AddPayment()` (EntryType.Payment, negative amount)
10. Link payment to entry
11. Persist both in single transaction
12. Return HotelPaymentDto

### Void Payment Flow

1. Authorization: `FrontOffice.Payment.Void`
2. Load payment by property scope
3. Apply ExpectedRowVersion on payment
4. Load associated folio
5. Create compensating FolioEntry via `folio.AddAdjustment()`
6. Mark payment as Voided
7. Persist in single transaction

---

## 5. Files Created/Modified

| File | Action | Purpose |
|---|---|---|
| `Core/FrontOffice/Payment/HotelPayment.cs` | NEW | Payment aggregate (60 lines) |
| `Application/FrontOffice/Payment/PaymentContracts.cs` | NEW | DTOs, commands, interfaces (30 lines) |
| `Application/FrontOffice/Payment/PaymentHandlers.cs` | NEW | Application service (70 lines) |
| `Application/FrontOffice/Payment/PaymentCqrsHandlers.cs` | NEW | CQRS handlers (20 lines) |
| `Infrastructure/FrontOffice/Payment/EfHotelPaymentRepository.cs` | NEW | EF repository (15 lines) |
| `Infrastructure/FrontOffice/Payment/SqlHotelPaymentNumberGenerator.cs` | NEW | SQL SEQUENCE generator (25 lines) |
| `Infrastructure/Persistence/AppDbContext.cs` | MODIFIED | +1 DbSet, +1 sequence, +1 mapping |
| `Infrastructure/DependencyInjection.cs` | MODIFIED | +8 DI registrations |
| `Tests.Integration/FrontOfficeSqlServerIntegrationTests.cs` | MODIFIED | +ThanhToan DROP + sequences |
| `Persistence/Migrations/...AddFrontOfficePayment.cs` | NEW | Migration |

---

## 6. Payment Methods

| Method | Description | Ledger Entry |
|---|---|---|
| **Cash** | Physical cash at reception | `folio.AddPayment(amount, "Payment: Cash")` |
| **Card** | Credit/debit card | `folio.AddPayment(amount, "Payment: Card (ref)")` |
| **BankTransfer** | Direct bank transfer | `folio.AddPayment(amount, "Payment: BankTransfer (ref)")` |
| **DepositOffset** | Use existing deposit | `folio.AddPayment(amount, "Payment: DepositOffset")` |

---

## 7. Immutable Ledger

| Operation | Ledger Entry | Amount Sign |
|---|---|---|
| Receive Payment | EntryType.Payment | **Negative** (reduces balance) |
| Void Payment | EntryType.Adjustment | **Positive** (reverses payment) |

Payment entries are **never edited or deleted**. Void = compensating entry only.

---

## 8. Tests

| Suite | Result |
|---|---|
| Unit Tests | **335/335 PASS** ✅ |
| Integration Tests | **23/23 PASS** ✅ |
| **Total** | **358/358 PASS** ✅ |

---

## 9. Go / No Go

### Payment Engine: GO ✅

| Criteria | Status |
|---|---|
| Payment → FolioEntry integration | ✅ |
| Void → compensating entry | ✅ |
| SQL SEQUENCE numbering | ✅ |
| RowVersion concurrency | ✅ |
| Property isolation | ✅ |
| 4 payment methods | ✅ |
| Immutable entries | ✅ |
| Build PASS | ✅ |
| All tests PASS (358/358) | ✅ |

---

*Payment Engine implementation complete. Integrated with Folio via immutable ledger entries. 358/358 tests pass.*
