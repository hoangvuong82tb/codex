# CREATE RESERVATION STEP E — FINANCIAL / FOLIO REPORT

> **Ngày**: 2026-07-22
> **Kết luận**: STEP E COMPLETE — 4 sections, realtime summary

---

## 1. LAYOUT (same as A/B/C/D)

| Left (62%) | Right (38%) |
|------------|-------------|
| 4 sections: Giá phòng, Đặt cọc, Giảm giá, Công nợ | Tóm tắt Folio: 6 summary blocks |

## 2. LEFT CARD SECTIONS

### Giá phòng
| Field | Type | Status |
|-------|------|--------|
| Giá phòng | TextBox | Placeholder |
| Tiền tệ | ComboBox (VND/USD) | Placeholder |
| Rate Code | Readonly TextBox | Bound |
| Số đêm | Readonly TextBox | **Bound** (from Step A) |
| Tạm tính | Computed text | Placeholder |

### Đặt cọc
| Field | Type | Status |
|-------|------|--------|
| Đặt cọc | TextBox | Placeholder |
| Hình thức | ComboBox (Tiền mặt/CK/Thẻ) | Placeholder |
| Trạng thái | ComboBox (Đã thu/Chưa thu/Refund) | Placeholder |

### Giảm giá
| Field | Type | Status |
|-------|------|--------|
| Discount % | TextBox | Placeholder |
| Discount tiền | TextBox | Placeholder |
| Promotion | ComboBox (disabled) | Placeholder (no backend) |
| Lý do giảm | TextBox | Placeholder |

### Công nợ
| Field | Type | Status |
|-------|------|--------|
| Phải trả | Readonly TextBox | Placeholder |
| Đã thanh toán | Readonly TextBox | Placeholder |
| Còn lại | Computed text (red) | Placeholder |
| Ghi chú TC | TextBox | Placeholder |

## 3. RIGHT CARD (Tóm tắt Folio)

| Block | Content |
|-------|---------|
| Giá phòng | 1,200,000 VND · Số đêm: N · Rate: RACK |
| Discount | 0 VND (0%) |
| Deposit | 500,000 VND · Tiền mặt · Đã thu |
| Đã thanh toán | 500,000 VND |
| Còn phải thu | **700,000 VND** (red, bold, 18px) |
| Status | ⚠ Chưa có thông tin chi tiết |

## 4. NOT INCLUDED (post-check-in)

- AR / Ledger / Posting
- Transfer Folio / Split Folio
- Routing / Window / Tax Detail
- Voucher Posting

## 5. FILES

| File | Change |
|------|--------|
| `Views/ReservationCreateWindow.xaml` | Step E rewrite — 4 sections, summary |

## 6. BUILD & RUNTIME

| Check | Result |
|-------|--------|
| Build | PASS (0 errors) |
| App launched | YES — PID 44372, ~341MB |
| Steps A-D unchanged | ✓ |
| Backend unchanged | ✓ |

---

**STEP E COMPLETE**
