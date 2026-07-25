# FO MILESTONE 3 PHASE 1 REPORT — FINANCIAL ENGINE FOUNDATION

> **Ngày**: 2026-07-21
> **Kết luận**: MILESTONE 3 PHASE 1 GREEN

---

## 1. BUILD & TEST

| Metric | Value |
|--------|-------|
| Debug Build | PASS (0 errors) |
| FO Tests (Stay + Folio) | **73/73 ALL PASS** |
| New Folio Tests | 12 (all domain) |

---

## 2. DOMAIN — AGGREGATES

### GuestFolio (HotelFolio)

```
HotelFolio (Aggregate Root)
├── HotelFolioEntry (1:N)
│   ├── Debit entries (RoomCharge, Tax, ServiceCharge, etc.)
│   └── Credit entries (Payment, Deposit, Discount, etc.)
├── RevenueCategory — maps entries to accounting buckets
└── TaxRule — tax calculation rules
```

### Status Machine

```
Open → Suspended → Closed → Settled → Archived
  ↑        ↓          ↑
  └── Resume          └── Reopen
```

### Entry Types (22)

| Category | Types |
|----------|-------|
| Revenue | RoomCharge, Restaurant, Laundry, MiniBar, Spa, Telephone, OtherCharge |
| Tax & Fees | Tax, ServiceCharge |
| Adjustments | Discount, Package, Adjustment, Correction, Transfer |
| Payments | Payment, Deposit, Refund |
| Manual | ManualCharge, ManualCredit |
| Balance | OpeningBalance, ClosingBalance |

---

## 3. REVENUE CATEGORY

```csharp
public enum RevenueBucket {
    Rooms, FoodAndBeverage, SpaAndWellness, Laundry, Telephone,
    MiniBar, Parking, Entertainment, OtherRevenue,
    ServiceCharge, Tax, Discount, Adjustment
}

public sealed class RevenueCategory {
    RevenueBucket Bucket;
    decimal TaxRate;        // e.g., 10 for 10%
    bool IsTaxInclusive;    // true if price includes tax
}
```

---

## 4. TAX RULE

```csharp
public enum TaxRuleType {
    Vat, ServiceCharge, CityTax, TourismTax, EnvironmentalFee, ResortFee
}

public sealed class TaxRule {
    TaxRuleType Type;
    decimal Rate;           // percentage (e.g., 10 = 10%)
    RevenueBucket AppliesTo;
    decimal? MinimumCharge;
    decimal? MaximumCharge;
    
    decimal Calculate(decimal taxableAmount);  // with min/max clamping
}
```

---

## 5. CQRS

### Commands (20)

| Command | Purpose |
|---------|---------|
| CreateFolioCommand | Open new folio for a stay |
| AddFolioRoomChargeCommand | Post room charge |
| AddFolioRestaurantChargeCommand | Post F&B charge |
| AddFolioTaxCommand | Apply tax |
| AddFolioServiceChargeCommand | Apply service charge |
| AddFolioDiscountCommand | Apply discount |
| AddFolioPaymentCommand | Record payment |
| AddFolioDepositCommand | Record deposit |
| AddFolioAdjustmentCommand | Manual adjustment |
| AddFolioCorrectionCommand | Error correction with reversal |
| AddFolioManualChargeCommand | Manual debit |
| AddFolioManualCreditCommand | Manual credit |
| AddFolioRefundCommand | Process refund |
| AddFolioTransferCommand | Transfer between folios |
| ReverseFolioEntryCommand | Reverse an entry |
| SuspendFolioCommand | Freeze folio |
| ResumeFolioCommand | Unfreeze folio |
| CloseFolioCommand | Close for settlement |
| SettleFolioCommand | Finalize (zeros balance) |
| ReopenFolioCommand | Reopen closed folio |

### Queries (4)

| Query | Purpose |
|-------|---------|
| GetFolioQuery | Get folio by ID |
| GetFolioByStayQuery | Get folio by stay |
| GetFolioLedgerQuery | Full ledger with entries |
| SearchFoliosQuery | Search with filters |

---

## 6. INTEGRATION POINTS (Event-Based)

| HotelStay Event | Financial Engine Action |
|----------------|------------------------|
| `StayCheckedIn` | Auto-create folio via `CreateFolioCommand` |
| `StayRoomMoved` | Log room change on folio for reference |
| `StayCheckedOut` | Trigger folio review — check pending charges, block if needed |
| `StayCompleted` | Finalize folio — close + settle |

**Integration strategy**: 
- HotelStay publishes domain events
- Financial Engine subscribes (event consumer pattern)
- No reverse dependency from HotelStay → Folio
- Idempotency via event ID deduplication

---

## 7. DI REGISTRATION

All 24 command/query handlers + application service + repository + number generator registered as `Scoped`.

---

## 8. FILES

### Files Modified (2)

| File | Changes |
|------|---------|
| `Core/FrontOffice/Folio/HotelFolio.cs` | +RevenueCategory + RevenueBucket + TaxRule + TaxRuleType (80 lines) |
| `Infrastructure/DependencyInjection.cs` | +Folio namespace imports + 26 DI registrations |

### Files Created (1)

| File | Content |
|------|---------|
| `Tests.Unit/HotelFolioTests.cs` | 12 unit tests covering: create, charge, payment, tax, discount, correction, close/settle, closed-guard, transfer, revenue category, tax rule, min/max |

### Files Already Existing (Pre-built)

| File | Status |
|------|--------|
| `Core/FrontOffice/Folio/HotelFolio.cs` (HotelFolio + HotelFolioEntry) | Complete (197 lines) |
| `Application/FrontOffice/Folio/FolioContracts.cs` | Complete (59 lines) |
| `Application/FrontOffice/Folio/FolioCqrsHandlers.cs` | Complete (42 lines) |
| `Application/FrontOffice/Folio/FolioHandlers.cs` | Complete (application service) |
| `Infrastructure/FrontOffice/Folio/EfHotelFolioRepository.cs` | Complete |
| `Infrastructure/FrontOffice/Folio/SqlHotelFolioNumberGenerator.cs` | Complete |

---

## 9. RISKS

| Risk | Severity | Mitigation |
|------|----------|------------|
| Event consumer not yet implemented | Medium | Wire up in M3 Phase 2 |
| No integration test (needs SQL Server) | Medium | Add when DB available |
| Folio DbSet exists but migration may need update | Low | EF already has `DbSet<HotelFolio>` + `DbSet<HotelFolioEntry>` |

---

**MILESTONE 3 PHASE 1 GREEN — FINANCIAL ENGINE FOUNDATION READY**
