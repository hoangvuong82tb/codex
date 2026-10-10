# FRONT OFFICE ROADMAP V2 — ASMILE-BO

> **Ngày**: 2026-07-21
> **Nguyên tắc**: Không ảnh hưởng Restaurant/Banquet (Feature Freeze)
> **Quy tắc**: Tuần tự từng milestone — đạt GREEN → khóa → chuyển milestone tiếp theo

---

## MILESTONE 1: RESERVATION FOUNDATION

**Mục tiêu**: Hoàn thiện đặt phòng end-to-end — CRUD reservation, tìm phòng trống, tự động gán phòng, group reservation.

**Sub-module**:
- Reservation
- Availability
- Rate
- Room Allocation

**Điều kiện PASS**:
- [ ] Tạo reservation
- [ ] Sửa reservation
- [ ] Hủy reservation
- [ ] Tìm phòng trống
- [ ] Tự động gán phòng
- [ ] Group reservation

**Không ảnh hưởng Restaurant/Banquet**: Đúng — module độc lập.

---

## MILESTONE 2: STAY LIFECYCLE

**Mục tiêu**: Vòng đời khách — Reservation → Check In → Stay → Check Out xuyên suốt.

**Sub-module**:
- Check In
- Stay
- Guest
- Room Move
- Extension
- Departure (Check Out)

**Điều kiện PASS**:
- [ ] Reservation → Check In → Stay → Check Out chạy end-to-end không lỗi

**Không ảnh hưởng Restaurant/Banquet**: Đúng.

---

## MILESTONE 3: FINANCIAL ENGINE

**Mục tiêu**: Tài chính — Folio, Posting, Payment, Settlement, Cashiering.

**Sub-module**:
- Folio
- Posting
- Payment
- Settlement
- Cashiering

**Điều kiện PASS**:
- [ ] Check In → Post Charge → Payment → Settlement → Check Out không lỗi

**Không ảnh hưởng Restaurant/Banquet**: Đúng — chỉ ĐỌC dữ liệu Restaurant nếu cần.

---

## MILESTONE 4: HOTEL OPERATION

**Mục tiêu**: Vận hành khách sạn — Housekeeping, Maintenance, Concierge, Door Lock, WakeUp, Lost & Found, Room Status.

**Sub-module**:
- Housekeeping
- Maintenance
- Concierge
- Door Lock
- WakeUp Call
- Lost & Found
- Room Status

**Điều kiện PASS**:
- [ ] Room status flow (clean/dirty/inspected/OOO)
- [ ] Housekeeping assignment
- [ ] Maintenance work order
- [ ] Concierge services

**Không ảnh hưởng Restaurant/Banquet**: Đúng.

---

## MILESTONE 5: NIGHT AUDIT

**Mục tiêu**: Kiểm toán đêm — business date, night audit, revenue snapshot, posting batch, audit trail.

**Sub-module**:
- Business Date
- Night Audit
- Revenue Snapshot
- Posting Batch
- Audit Trail

**Điều kiện PASS**:
- [ ] Night audit workflow hoàn chỉnh
- [ ] Auto room charge posting
- [ ] Revenue snapshot chính xác

**Không ảnh hưởng Restaurant/Banquet**: Đúng.

---

## MILESTONE 6: MANAGEMENT

**Mục tiêu**: Báo cáo quản lý — executive dashboard, reporting, forecast, occupancy, flash report.

**Sub-module**:
- Executive Dashboard
- Reporting
- Forecast
- Occupancy
- Flash Report

**Điều kiện PASS**:
- [ ] Occupancy report
- [ ] Revenue report
- [ ] Manager flash report
- [ ] Forecast accuracy

**Không ảnh hưởng Restaurant/Banquet**: Đúng — chỉ đọc dữ liệu.

---

## MILESTONE 7: SYSTEM HARDENING

**Mục tiêu**: Ổn định hệ thống — regression test, integration test, performance, memory, stress test.

**Sub-module**:
- Regression Test
- Integration Test
- Performance
- Memory
- Stress Test

**Điều kiện PASS**:
- [ ] Toàn bộ regression test PASS
- [ ] Integration test PASS
- [ ] Performance benchmarks đạt
- [ ] Không memory leak

---

## Quy tắc sau mỗi Milestone

1. Build Debug PASS
2. Build Release PASS
3. Không có warning mới
4. Không sửa Restaurant
5. Không sửa Banquet
6. Không thay đổi schema các module đã Feature Freeze
7. Xuất báo cáo: `docs/front-office/FO_MILESTONE_x_REPORT.md`

---

## Kiến trúc Front Office

```
ASMILE.BO.Core/FrontOffice/          — 25 sub-modules
ASMILE.BO.Application/FrontOffice/   — CQRS handlers, DTOs
ASMILE.BO.Infrastructure/FrontOffice/ — EF repositories
ASMILE.BO.App/Views/                 — WPF UI
ASMILE.BO.App/ViewModels/            — MVVM ViewModels
ASMILE.BO.App/Services/              — UI services
```
