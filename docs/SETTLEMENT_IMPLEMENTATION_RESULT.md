# FOLIO SETTLEMENT & INVOICE IMPLEMENTATION RESULT — ASMILE-BO

**Ngày:** 2026-07-19
**Trạng thái:** HOAN THANH ✅

---

## 1. Architecture

```
Settlement Flow:
  Folio (Open) → BeginSettlement → Folio (Settling) → Close → Folio (Closed)
       │                │                    │               │
       │           Apply discounts        Apply tax      ClosingBalance entry
       │           Calculate outstanding  Verify balance
       │                                  Manager override if ≠ 0
       │
  Invoice:
    Folio ledger snapshot → HotelInvoice → MarkPrinted
```

---

## 2. Database — 14 FO tables + 5 sequences

| Table | Purpose |
|---|---|
| `fo.DatPhong` | Reservation |
| `fo.LichSuDatPhong` | Reservation history |
| `fo.PhongKhachSan` | Hotel rooms |
| `fo.LoaiPhongKhachSan` | Room types |
| `fo.LuuTruKhachSan` | Hotel stays |
| `fo.LichSuLuuTruKhachSan` | Stay history |
| `fo.SoFolio` | Folio (per stay) |
| `fo.ButToanFolio` | Immutable ledger entries |
| `fo.ThanhToan` | Payments |
| `fo.PhanBoThanhToan` | Payment allocations |
| **`fo.HoaDon`** | **Invoices** |
| `fo.fo_hop_thu_di` | Outbox |
| `fo.fo_hop_thu_den` | Inbox |
| `fo.fo_khoa_idempotency` | Idempotency |

| Sequence | Format |
|---|---|
| `fo.TrinhTuSoDatPhong` | RSV-YYYYMM-XXXXXX |
| `fo.TrinhTuSoLuuTru` | STY-YYYYMM-XXXXXX |
| `fo.TrinhTuSoFolio` | FOL-YYYYMM-XXXXXX |
| `fo.TrinhTuSoThanhToan` | PMT-YYYYMM-XXXXXX |
| **`fo.TrinhTuSoHoaDon`** | **INV-YYYYMM-XXXXXX** |

---

## 3. Settlement Workflow

```
Guest requests check-out
  ↓
SettleFolioCommand
  ├── Verify folio Open
  ├── Apply RowVersion concurrency
  ├── BeginSettlement (Open → Settling)
  ├── Apply discounts (if any)
  ├── Apply taxes (if any)
  ├── Calculate outstanding = SUM(ledger entries)
  ├── If outstanding ≠ 0 AND no manager override → REJECT
  ├── Commit
  └── Return SettlementResultDto

CloseFolioCommand
  ├── Verify folio Settling
  ├── Apply RowVersion
  ├── folio.Close(closingBalance) → Closed
  ├── ClosingBalance entry appended
  └── Commit
```

### Folio Status Flow
```
Open → Settling → Closed
  ↑       ↑         ↑
  │  BeginSettlement │ Close(closingBalance)
  │                  │
  └── Create Folio ──┘
```

---

## 4. Invoice

### HotelInvoice Aggregate

| Field | Purpose |
|---|---|
| InvoiceNumber | INV-YYYYMM-XXXXXX via SQL SEQUENCE |
| Subtotal | SUM(Charges) |
| TaxAmount | SUM(SettlementTax entries) |
| TotalAmount | Subtotal + Tax |
| PaidAmount | SUM(Payments + Deposits) |
| OutstandingAmount | RunningBalance |
| InvoiceDataJson | Full ledger snapshot as JSON |
| GuestName, RoomNumber, StayDates | Snapshot at invoice time |

### Invoice Status
```
Draft → Generated → Printed → Emailed
```

### GenerateInvoiceAsync Flow
1. Load folio ledger (all entries)
2. Compute subtotal, tax, paid, outstanding
3. Serialize ledger to JSON
4. Create HotelInvoice with snapshot
5. Return InvoiceDto

---

## 5. Files Created/Modified

| File | Action |
|---|---|
| `Core/FrontOffice/Settlement/HotelInvoice.cs` | NEW — Invoice aggregate |
| `Core/FrontOffice/Folio/HotelFolio.cs` | MODIFIED — +Settling status, +BeginSettlement, +entry types |
| `Application/FrontOffice/Settlement/SettlementContracts.cs` | NEW — DTOs, commands, interfaces |
| `Application/FrontOffice/Settlement/SettlementHandlers.cs` | NEW — Settlement application service |
| `Application/FrontOffice/Settlement/SettlementCqrsHandlers.cs` | NEW — 6 CQRS handlers |
| `Infrastructure/FrontOffice/Settlement/EfHotelInvoiceRepository.cs` | NEW — EF repository |
| `Infrastructure/FrontOffice/Settlement/SqlHotelInvoiceNumberGenerator.cs` | NEW — SQL SEQUENCE generator |
| `Infrastructure/Persistence/AppDbContext.cs` | MODIFIED — +DbSet, +sequence, +mapping |
| `Infrastructure/DependencyInjection.cs` | MODIFIED — +10 DI registrations |
| `Tests.Integration/FrontOfficeSqlServerIntegrationTests.cs` | MODIFIED — cleanup |
| `Persistence/Migrations/...AddFrontOfficePayment.cs` | NEW — includes Payment + Invoice |

---

## 6. Tests

| Suite | Result |
|---|---|
| Unit Tests | **335/335 PASS** ✅ |
| Integration Tests | **23/23 PASS** ✅ |
| **Total** | **358/358 PASS** ✅ |

---

## 7. Go / No Go

### Settlement & Invoice: GO ✅

| Criteria | Status |
|---|---|
| Settlement workflow (Open→Settling→Closed) | ✅ |
| Manager override for non-zero outstanding | ✅ |
| Invoice generation with ledger snapshot | ✅ |
| SQL SEQUENCE numbering (INV) | ✅ |
| RowVersion concurrency | ✅ |
| Property isolation | ✅ |
| 14 FO tables | ✅ |
| 5 sequences | ✅ |
| All tests PASS (358/358) | ✅ |

---

## Front Office — Full Transaction Lifecycle Complete ✅

```
Reservation → Prepare Stay → Check-in → Check-out
     ✅            ✅            ✅          ✅

  Folio → Payment → Allocation → Settlement → Invoice → Close
    ✅       ✅         ✅            ✅          ✅       ✅

14 tables • 5 sequences • 87 migrations • 358 tests
```

**Front Office ERP đầy đủ — sẵn sàng cho Night Audit.**

---
