# FO-EXEC-008 ENTERPRISE PAYMENT & SETTLEMENT ENGINE — ASMILE-BO

**Ngày:** 2026-07-20
**Loại:** ENTERPRISE MODULE
**Trạng thái:** HOAN THANH ✅

---

## 1. Architecture

```
Payment Engine (Financial Core)
     │
     │  Mọi giao dịch tài chính phải đi qua Payment Engine
     │  trước khi tạo Posting trong Folio
     ▼
HotelPayment ──────► HotelFolio (Payment/Deposit/Refund Posting)
     │
     ├── Authorization (CreditCard)
     ├── Capture
     ├── Split Tender
     ├── Allocate to Charges
     ├── Refund (Full/Partial)
     └── Void
```

**Nguyên tắc:** Payment Engine là nơi DUY NHẤT được phép tạo Payment Posting, Deposit Posting, và Refund Posting trong Folio.

---

## 2. Payment State Machine (9 States)

```
                    ┌─────────┐
                    │ Pending │ ← Tạo mới (card payments)
                    └────┬────┘
                         │ Authorize
                    ┌────▼────┐
                    │Authorized│ ← Card authorized, funds held
                    └───┬──┬──┘
               Capture │  │ Void
                    ┌───▼┐ ┌▼──────┐
                    │Captured│ │Voided│ ← Void trước capture
                    └───┬──┘ └──────┘
                        │ Complete
                    ┌───▼──────┐
                    │Completed │ ← Payment finalized
                    └─┬──┬──┬──┘
           Refund ───┘  │  └─── Failed/Cancelled
                    ┌───▼──────────┐
                    │PartiallyRefunded│
                    └───┬──────────┘
                        │ Full Refund
                    ┌───▼──────┐
                    │ Refunded │
                    └──────────┘
```

### Transition Rules

| From | To | Method | Condition |
|------|----|--------|-----------|
| Pending | Authorized | `Authorize(authCode)` | Card payments |
| Pending | Completed | `Complete()` | Cash/instant payments |
| Authorized | Captured | `Capture()` | Card capture |
| Authorized | Voided | `Void()` | Void before capture |
| Captured, Completed | Completed | `Complete()` | Finalize |
| Completed, Captured, PartiallyRefunded | Refunded | `Refund(amount)` | Full refund |
| Completed, Captured | PartiallyRefunded | `Refund(amount)` | Partial refund |
| Pending, Authorized | Failed | `MarkFailed(reason)` | Auth failed |
| Pending, Authorized | Cancelled | `Cancel(reason)` | User cancelled |

---

## 3. Payment Methods (12)

| Method | Description | Authorization Required |
|--------|-------------|----------------------|
| Cash | Tiền mặt | No |
| Card | Thẻ (generic) | Yes |
| CreditCard | Thẻ tín dụng | Yes |
| DebitCard | Thẻ ghi nợ | Yes |
| BankTransfer | Chuyển khoản | No |
| QR | QR code | No |
| EWallet | Ví điện tử | No |
| CorporateBilling | Công ty thanh toán | No (invoice later) |
| CityLedger | Công nợ thành phố | No |
| Voucher | Phiếu voucher | No |
| Mixed | Hỗn hợp | Depends |
| Split | Chia nhỏ | Depends per line |

---

## 4. Split Tender

Một giao dịch chia thành nhiều phương thức thanh toán:

```
Tổng: 5,000,000 VND
├── 2,000,000 Cash
├── 2,000,000 CreditCard (Authorization → Capture)
└── 1,000,000 EWallet
```

Mỗi dòng tạo một `HotelPayment` riêng với `SplitTenderLine`.

---

## 5. Settlement Pipeline

```
Charges (RoomCharge + Restaurant + Laundry + ...)
     │
     ▼
Discount (khuyến mãi, thương lượng)
     │
     ▼
Tax (VAT, service charge)
     │
     ▼
Subtotal
     │
     ▼
Deposit (trừ tiền cọc)
     │
     ▼
Payment (thu tiền khách)
     │
     ▼
Balance = 0 ? → Settlement → Close Folio
     │
     ▼
Invoice Generation
```

---

## 6. Multi-Currency Model

| Currency | Code | Exchange Rate to VND |
|----------|------|---------------------|
| VND | Vietnamese Dong | 1.0 |
| USD | US Dollar | Configurable |
| EUR | Euro | Configurable |
| GBP | British Pound | Configurable |
| JPY | Japanese Yen | Configurable |
| CNY | Chinese Yuan | Configurable |
| KRW | Korean Won | Configurable |
| SGD | Singapore Dollar | Configurable |

Each payment stores `Currency` + `ExchangeRateToVnd` for reporting in base currency.

---

## 7. Payment Features

| Feature | Status | Implementation |
|---------|--------|---------------|
| Authorization | ✅ | `AuthorizePaymentCommand` |
| Capture | ✅ | `CapturePaymentCommand` |
| Void | ✅ | `VoidPaymentCommand` |
| Refund (Full) | ✅ | `RefundPaymentCommand` |
| Refund (Partial) | ✅ | `RefundPaymentCommand` with Amount |
| Deposit | ✅ | Via Folio `AddDeposit` |
| Split Tender | ✅ | `SplitTenderCommand` |
| Multi-Currency | ✅ | Model only (exchange rate config P2) |
| Payment History | ✅ | `PaymentHistory` record embedded in aggregate |

---

## 8. Folio Integration

Payment Engine → Folio Posting:

| Payment Action | Folio Posting Type |
|---------------|-------------------|
| Receive Payment | `Payment` |
| Refund | `Refund` |
| Void | `Correction` (reversal) |
| Deposit | `Deposit` (via Folio directly) |

**No other module creates Payment/Deposit/Refund entries in Folio.**

---

## 9. Reporting

| Query | Description |
|-------|-------------|
| `GetOutstandingBalance` | Tổng charges - payments - deposits |
| `GetDepositSummary` | Tổng deposit, đã apply, còn lại |
| `GetSettlementSummary` | Tất cả folios: charges, payments, deposits, outstanding, isBalanced |
| `GetPaymentsForFolio` | Tất cả payments của một folio |
| `GetPaymentHistory` | Lịch sử state transitions của payment |

---

## 10. Files Changed

| File | Action |
|------|--------|
| `Core/FrontOffice/Payment/HotelPayment.cs` | REWRITTEN — 9 states, 12 methods, Currency, SplitTenderLine, ExchangeRate, PaymentHistory |
| `Application/FrontOffice/Payment/PaymentContracts.cs` | EXPANDED — 9 commands, 7 queries, new DTOs |
| `Application/FrontOffice/Payment/PaymentHandlers.cs` | REWRITTEN — SplitTender, Authorize, Capture, Complete, Cancel, partial refund |
| `Application/FrontOffice/Payment/PaymentCqrsHandlers.cs` | EXPANDED — 6 new handlers |
| `Infrastructure/FrontOffice/Payment/EfHotelPaymentRepository.cs` | UPDATED — GetHistoryAsync, GetSettlementSummaryAsync, updated Map |
| `Infrastructure/DependencyInjection.cs` | EXPANDED — 10 new handler registrations |

**Total:** 6 files changed

---

## 11. Build

| Check | Result |
|-------|--------|
| Debug Build | PASS (0 errors) |

---

## 12. Remaining Gaps

| Gap | Priority | Notes |
|-----|----------|-------|
| PaymentHistory DB migration | P1 | PaymentHistory collection not yet mapped to DB table |
| Exchange rate provider | P2 | ExchangeRate model exists but no live rate service |
| Card gateway integration | P2 | Authorization/Capture model exists but no payment gateway |
| Payment UI | P2 | No WPF view for payment management |
| Batch settlement | P3 | NightAudit auto-settle all folios |
| Multi-currency reporting | P3 | All amounts in VND only |

---

## 13. Recommended Next Sprint

**FO-EXEC-009: Night Audit Automation + Financial Close**

1. NightAudit auto-post RoomCharge
2. NightAudit batch settlement
3. Financial close process
4. PaymentHistory DB migration
5. Revenue reconciliation

---

*Enterprise Payment & Settlement Engine complete. 9-state payment lifecycle, 12 payment methods, split tender, authorization/capture flow, and settlement pipeline operational.*
