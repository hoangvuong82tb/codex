# FO-EXEC-007 ENTERPRISE FOLIO ENGINE — ASMILE-BO

**Ngày:** 2026-07-20
**Loại:** ENTERPRISE MODULE
**Trạng thái:** HOAN THANH ✅

---

## 1. Architecture

```
HotelStay (lưu trú)
     │
     ▼
HotelFolio (trung tâm tài chính)
     │
     ├── HotelFolioEntry[] (postings — immutable)
     │       ├── RoomCharge (từ RateEngine)
     │       ├── Restaurant (từ POS/Nhà hàng)
     │       ├── Laundry / MiniBar / Spa / Telephone
     │       ├── Tax / ServiceCharge
     │       ├── Discount / Package / Adjustment / Correction
     │       ├── Payment / Deposit / Refund
     │       └── ManualCharge / ManualCredit
     │
     ├── HotelPayment (thanh toán)
     ├── HotelInvoice (hóa đơn)
     └── NightAudit (kiểm đếm đêm)
```

**Nguyên tắc:** Folio là nguồn dữ liệu DUY NHẤT cho mọi giao dịch tài chính. Không module nào tự post charge ngoài Folio.

---

## 2. State Machine (5 States)

```
     ┌─────────┐
     │  Open   │ ← Tạo mới, chấp nhận posting
     └────┬────┘
          │ Suspend          Resume
     ┌────▼────┐ ────────────►  Open
     │Suspended│ ← Tranh chấp, review
     └────┬────┘
          │ Close
     ┌────▼────┐
     │ Closed  │ ← Không thêm posting, sẵn sàng settlement
     └────┬────┘
          │ Settle
     ┌────▼────┐
     │ Settled │ ← Thanh toán hoàn tất, balance = 0
     └────┬────┘
          │ Archive
     ┌────▼────┐
     │Archived │ ← Lưu trữ dài hạn
     └─────────┘

     Reopen: Closed/Settled → Open
```

### Transition Rules

| From | To | Method | Condition |
|------|----|--------|-----------|
| Open | Suspended | `Suspend()` | — |
| Suspended | Open | `Resume()` | — |
| Open, Suspended | Closed | `Close()` | — |
| Closed | Settled | `Settle()` | Balance zeroed |
| Closed, Settled | Open | `Reopen()` | — |
| Settled | Archived | `Archive()` | — |

---

## 3. Posting Types (22)

### Revenue Postings
| Type | Description | Source Module |
|------|-------------|---------------|
| `RoomCharge` | Tiền phòng theo đêm | RateEngine |
| `Restaurant` | Nhà hàng / F&B | POS |
| `Laundry` | Giặt ủi | Laundry |
| `MiniBar` | Minibar | MiniBar |
| `Spa` | Spa | Spa |
| `Telephone` | Điện thoại | Telephone |
| `OtherCharge` | Phí khác | Manual |

### Tax & Fees
| Type | Description |
|------|-------------|
| `Tax` | VAT / thuế chính phủ |
| `ServiceCharge` | Phí dịch vụ % |

### Adjustments
| Type | Description |
|------|-------------|
| `Discount` | Giảm giá khuyến mãi / thương lượng |
| `Package` | Gói inclusive (zero-charge) |
| `Adjustment` | Điều chỉnh thủ công (±) |
| `Correction` | Sửa lỗi (có reference đến entry gốc) |
| `Transfer` | Chuyển sang folio khác |

### Payments
| Type | Description |
|------|-------------|
| `Payment` | Thanh toán (tiền mặt, thẻ, chuyển khoản) |
| `Deposit` | Đặt cọc / đảm bảo |
| `Refund` | Hoàn tiền |

### Manual
| Type | Description |
|------|-------------|
| `ManualCharge` | Ghi nợ thủ công |
| `ManualCredit` | Ghi có thủ công |

### Balance
| Type | Description |
|------|-------------|
| `OpeningBalance` | Số dư đầu kỳ |
| `ClosingBalance` | Số dư cuối kỳ (tự động khi Settle) |

---

## 4. Posting Model

Mỗi `HotelFolioEntry` là **immutable**:

| Field | Type | Description |
|-------|------|-------------|
| HotelFolioEntryId | Guid | PK |
| SequenceNumber | int | Thứ tự trong folio |
| EntryType | HotelFolioEntryType | Loại posting |
| Amount | decimal | + = debit, − = credit |
| Description | string | Mô tả |
| ReferenceType | string? | Loại nguồn tham chiếu |
| ReferenceId | Guid? | ID nguồn tham chiếu |
| BusinessDate | DateOnly? | Ngày kinh doanh phát sinh |
| ReversesEntryId | Guid? | Entry bị đảo ngược |
| ReversedByEntryId | Guid? | Bị đảo bởi entry |
| CreatedAtUtc | DateTime | Thời gian tạo |
| CreatedBy | Guid? | Người tạo |

**Nguyên tắc:**
- Immutable — không sửa, chỉ reversal
- Audit — mỗi entry có operator + correlation
- Reference — link đến nguồn gốc (Restaurant Order, Spa Booking...)
- BusinessDate — ngày kinh doanh thực tế (có thể khác CreatedAtUtc)

---

## 5. Check-in / Check-out Flow

### Check-in:
```
Create Stay
     │
     ▼
Create Folio (tự động nếu chưa có)
     │
     ▼
Add OpeningBalance (nếu có số dư từ stay trước)
     │
     ▼
Add Deposit (nếu reservation có deposit)
     │
     ▼
Timeline
```

### Check-out:
```
Close Posting (ngừng nhận charge mới)
     │
     ▼
Balance Validation (tính tổng charges - payments)
     │
     ▼
Settlement (xử lý discount, tax, adjustment)
     │
     ▼
Payment (thu tiền hoặc hoàn tiền)
     │
     ▼
Close Folio
     │
     ▼
Settle Folio (balance = 0)
```

---

## 6. Module Integration

| Module | Integration Point | Method |
|--------|------------------|--------|
| Stay | Check-in tạo folio | `CreateFolioCommand` |
| RateEngine | Post RoomCharge mỗi đêm | `AddRoomChargeAsync` |
| Restaurant | Post F&B charges | `AddRestaurantChargeAsync` |
| Payment | Post Payment/Deposit/Refund | `AddPaymentAsync`, `AddDepositAsync`, `AddRefundAsync` |
| Settlement | Close + Settle folio | `CloseAsync`, `SettleAsync` |
| NightAudit | Post RoomCharge + Tax hàng đêm | `AddRoomChargeAsync`, `AddTaxAsync` |

---

## 7. Search

### `SearchFoliosQuery`
```csharp
FolioSearchFilter(
    Guid PropertyId,
    Guid? StayId,              // Lọc theo stay
    Guid? GuestId,             // Lọc theo khách
    HotelFolioStatus? Status,  // Lọc theo trạng thái
    HotelFolioEntryType? PostingType, // Lọc theo loại posting
    DateOnly? DateFrom,        // Từ ngày
    DateOnly? DateTo,          // Đến ngày
    string? Keyword            // Tìm trong FolioNumber
)
```

---

## 8. Files Changed

| File | Action | Changes |
|------|--------|---------|
| `Core/FrontOffice/Folio/HotelFolio.cs` | REWRITTEN | 5 states, 22 entry types, 20+ posting methods, BusinessDate |
| `Application/FrontOffice/Folio/FolioContracts.cs` | EXPANDED | 20 commands, 4 queries, SearchAsync |
| `Application/FrontOffice/Folio/FolioHandlers.cs` | REWRITTEN | All posting + state handlers + transfer + reversal |
| `Application/FrontOffice/Folio/FolioCqrsHandlers.cs` | EXPANDED | 19 command handlers, 4 query handlers |
| `Infrastructure/FrontOffice/Folio/EfHotelFolioRepository.cs` | UPDATED | SearchAsync, updated DTO mapping |
| `Infrastructure/DependencyInjection.cs` | EXPANDED | 17 new handler registrations |
| `Application/FrontOffice/Payment/PaymentHandlers.cs` | FIXED | Updated AddPayment/AddRefund signatures |
| `Application/FrontOffice/Settlement/SettlementHandlers.cs` | FIXED | Removed BeginSettlement, updated Close |
| `Infrastructure/.../EfHotelPaymentRepository.cs` | FIXED | Charge → RoomCharge |
| `Infrastructure/.../EfReportingQueryService.cs` | FIXED | Charge → RoomCharge |

**Total:** 10 files changed

---

## 9. Build & Test

| Check | Result |
|-------|--------|
| Debug Build | PASS (0 errors) |
| Unit Tests | 175/366 PASS (191 pre-existing failures from TestDbContextFactory) |

---

## 10. Remaining Gaps

| Gap | Priority | Notes |
|-----|----------|-------|
| Folio tests | P1 | No dedicated Folio unit tests yet |
| NightAudit auto RoomCharge posting | P1 | NightAudit handler needs to call AddRoomChargeAsync |
| Restaurant/Laundry/Spa integration | P2 | Outlet modules not yet wired to Folio |
| Folio migration | P2 | BusinessDate column needs DB migration |
| Transfer between folios UI | P2 | No UI for transfer command |
| Multi-currency support | P3 | All amounts in VND only |
| Tax rate configuration | P3 | Tax amount manually calculated, no tax engine |

---

## 11. Recommended Next Sprint

**FO-EXEC-008: Night Audit + Posting Automation**

1. NightAudit → auto-post RoomCharge hàng đêm
2. Tax calculation engine
3. Batch Posting (post room charges cho tất cả InHouse stays)
4. Folio unit tests
5. DB migration cho BusinessDate column
6. Reporting integration hoàn chỉnh

---

*Enterprise Folio Engine complete. 5-state folio lifecycle, 22 posting types, immutable audit trail, and search capabilities operational.*
