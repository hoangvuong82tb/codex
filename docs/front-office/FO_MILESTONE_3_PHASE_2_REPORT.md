# FO MILESTONE 3 PHASE 2 REPORT — POSTING ENGINE FOUNDATION

> **Ngày**: 2026-07-21
> **Kết luận**: MILESTONE 3 PHASE 2 GREEN

---

## 1. BUILD & TEST

| Metric | Value |
|--------|-------|
| Debug Build | PASS (0 errors) |
| FO Tests (Stay + Folio + Posting) | **82/82 ALL PASS** |
| New Posting Engine Tests | 9 |

---

## 2. POSTING ENGINE ARCHITECTURE

```
Any Source (Room, Restaurant, Spa, etc.)
    │
    ▼ PostingRequest (immutable value object)
PostingPipeline (domain service)
    ├── Duplicate Detection (idempotent via RequestId)
    ├── Amount Validation
    ├── Source → RevenueBucket Mapping
    ├── Custom Rules (PostingRule pipeline)
    ├── Tax Calculation (by RevenueBucket)
    ├── Service Charge Calculation (by RevenueBucket)
    └── appendToFolio callback
        │
        ▼
    GuestFolio.AddEntry()
        │
        ▼
    PostingResult
```

### Single Source of Truth

Posting Engine is the **only** component allowed to write to GuestFolio. All charge sources route through it.

---

## 3. COMPONENTS

### PostingRequest (Value Object)

```csharp
public readonly record struct PostingRequest(
    Guid RequestId,           // Idempotency key
    PostingSource Source,     // Who initiated
    Guid TargetFolioId,       // Which folio
    decimal Amount,           // Base amount (before tax)
    string Description,
    RevenueBucket RevenueBucket,
    DateOnly BusinessDate,
    DateTime CreatedAtUtc,
    Guid? CreatedBy
);
```

### PostingResult (Value Object)

```csharp
public readonly record struct PostingResult(
    Guid RequestId,
    bool IsSuccess,
    Guid? FolioEntryId,
    string? ErrorCode,
    string? ErrorMessage,
    decimal TaxAmount,
    decimal ServiceChargeAmount,
    decimal NetAmount        // Amount + Tax + ServiceCharge
);
```

### PostingSource

```csharp
public enum PostingSourceType {
    RoomCharge, Restaurant, Banquet, Spa, Laundry,
    Telephone, MiniBar, Manual, NightAudit, System
}
```

### PostingRule (Configurable Pipeline Filter)

```csharp
public sealed class PostingRule {
    string RuleName;
    int Priority;
    Func<PostingRequest, string?> Validate;      // null = pass
    Func<PostingRequest, decimal> CalculateTax;
    Func<PostingRequest, decimal> CalculateServiceCharge;
}
```

### PostingPolicies (Static Business Rules)

| Policy | Logic |
|--------|-------|
| `MapSourceToBucket` | RoomCharge→Rooms, Restaurant→F&B, etc. |
| `DefaultTaxRate` | Rooms: 10%, F&B: 8%, Spa: 10%, Other: 0% |
| `DefaultServiceChargeRate` | Rooms: 5%, F&B: 5%, Other: 0% |
| `IsAmountValid` | Amount ≠ 0, between ±99,999,999 |

### PostingPipeline (Domain Service)

| Stage | Description |
|-------|-------------|
| 1. Duplicate Check | RequestId must be unique |
| 2. Amount Validation | Non-zero, within bounds |
| 3. Bucket Mapping | Auto-detect from source if not specified |
| 4. Custom Rules | Ordered by priority, any failure = reject |
| 5. Tax Calculation | By bucket policy + custom rule overrides |
| 6. Service Charge | By bucket policy + custom rule overrides |
| 7. Folio Append | Callback to infrastructure layer |

---

## 4. ERROR CODES

| Code | Description |
|------|-------------|
| `POSTING_DUPLICATE` | RequestId already processed |
| `POSTING_AMOUNT_INVALID` | Amount is zero or out of range |
| `POSTING_RULE_VIOLATION` | Custom validation rule failed |

---

## 5. DOMAIN EVENTS

Posting Engine is a domain service (no aggregates). Events are raised by the Folio when entries are added:
- `FolioEntryAdded` — on `HotelFolio.AddEntry()`

---

## 6. INTEGRATION DIAGRAM

```
[Restaurant POS] ──┐
[Room Rate Engine] ─┤
[Spa System] ───────┼── PostingRequest ──→ PostingPipeline ──→ GuestFolio
[Laundry] ──────────┤
[Manual Posting] ───┘

PostingPipeline
    │
    ├── Idempotency (Duplicate Detection)
    ├── Validation Pipeline
    ├── Tax Hook
    ├── Service Charge Hook
    └── Revenue Mapping
         │
         ▼
    GuestFolio.AddEntry()
```

---

## 7. FILES

### Files Created (2)

| File | Content |
|------|---------|
| `Core/FrontOffice/Posting/PostingEngine.cs` | PostingRequest, PostingResult, PostingSource, PostingSourceType, PostingRule, PostingPolicies, PostingPipeline, PostingBatchResult (157 lines) |
| `Tests.Unit/PostingEngineTests.cs` | 9 tests: valid posting, duplicate, zero amount, custom rule, source mapping, tax rates, batch, idempotency, service charge |

---

## 8. IDEMPOTENCY STRATEGY

- Each `PostingRequest` has a unique `RequestId` (Uuid7)
- `PostingPipeline` maintains a `HashSet<Guid>` of processed IDs
- Duplicate = `POSTING_DUPLICATE` error, no side effects
- For production: store processed IDs in DB with TTL

---

## 9. RISKS

| Risk | Severity | Mitigation |
|------|----------|------------|
| In-memory dedup (not persistent) | Medium | Store processed RequestIds in DB (Phase 3) |
| No async pipeline yet | Low | Pipeline is sync — add async in M3 Phase 3 |
| No integration with actual Folio | Low | Callback pattern — wire in M3 Phase 3 |

---

**MILESTONE 3 PHASE 2 GREEN — POSTING ENGINE READY**
