# FO MILESTONE 1 FREEZE — RESERVATION FOUNDATION

> **Ngày**: 2026-07-21
> **Trạng thái**: FROZEN (có điều kiện)
> **Review**: docs/front-office/FO_MILESTONE_1_ARCHITECTURE_REVIEW.md

---

## 1. MODULE ĐƯỢC ĐÓNG BĂNG

| # | Module | Scope |
|---|--------|-------|
| 1 | **Reservation Domain** | `ASMILE.BO.Core/FrontOffice/Reservation/` |
| 2 | **Reservation Application** | `ASMILE.BO.Application/FrontOffice/Reservation/` |
| 3 | **Reservation Infrastructure** | `ASMILE.BO.Infrastructure/FrontOffice/Reservation/` |
| 4 | **Reservation UI** | `ASMILE.BO.App/ViewModels/ReservationWorkspaceViewModel.cs`, `ReservationHubViewModel.cs`, `ReservationModalWorkspaceViewModel.cs` |
| 5 | **Reservation UI Service** | `ASMILE.BO.App/Services/ReservationUiService.cs`, `ReservationWorkspaceModalService.cs` |
| 6 | **Availability** | `ASMILE.BO.Core/FrontOffice/Availability/` |
| 7 | **Rate Engine** | `ASMILE.BO.Core/FrontOffice/Rate/` |
| 8 | **Room Allocation** | `ASMILE.BO.Core/FrontOffice/Allocation/` |
| 9 | **Group Reservation** | `ASMILE.BO.Core/FrontOffice/Group/` |

---

## 2. QUY TẮC KHÔNG ĐƯỢC THAY ĐỔI

### TUYỆT ĐỐI CẤM

- Thêm chức năng mới vào Reservation
- Thêm status mới vào ReservationStatus
- Thêm field mới vào Reservation entity
- Thêm command/query mới
- Thay đổi DTO contract
- Thay đổi database schema (bảng `fo.DatPhong`, `fo.LichSuDatPhong`)
- Thay đổi API của `IReservationRepository`
- Thay đổi API của `IReservationUiService`
- Thay đổi EF configuration
- Thay đổi migration

### CHỈ ĐƯỢC PHÉP

- Sửa bug gây crash
- Sửa lỗi dữ liệu
- Sửa lỗi bảo mật
- Sửa lỗi compile

### QUY TRÌNH SỬA BUG

1. Bug phải reproducible
2. Sửa đúng file gây lỗi — không sửa file liên quan
3. Minimal fix — thay đổi nhỏ nhất
4. Build PASS + affected tests PASS
5. Ghi nhận trong `FO_MILESTONE_1_FREEZE.md` (History section)

---

## 3. ĐIỀU KIỆN FREEZE (4 HIGH ISSUES)

Các issues này đã được chấp nhận defer sang Milestone 2, nhưng PHẢI giải quyết **trước khi bắt đầu Check In** (Milestone 2 Phase 1):

| # | Issue | Deadline |
|---|-------|----------|
| 1 | Thêm `DbSet<RatePlan>`, `DbSet<GroupReservation>` + EF config + migration | Trước M2 CheckIn |
| 2 | Thêm `Guarantee()` method vào Reservation status machine | Trước M2 CheckIn |
| 3 | Enforce `OverbookingLimit` trong `CreateAsync` | Trước M2 CheckIn |
| 4 | Thêm `DepositAmount`, `DepositDueDate` vào Reservation | Trước M2 CheckIn |

---

## 4. TEST BASELINE (FREEZE POINT)

| Module | Tests | Status |
|--------|-------|--------|
| Reservation Acceptance | 3 | GREEN |
| Reservation ViewModel | 8 | GREEN |
| Reservation Hub | 7 | GREEN |
| Reservation Modal | 4 | GREEN |
| Front Office Hub | 9 | GREEN |
| Rate Engine | 15 | GREEN |
| Room Allocation | 16 | GREEN |
| Group Reservation | 10 | GREEN |
| Front Office Foundation | 5 | GREEN |
| **Total** | **77** | **GREEN** |

Tất cả test phải tiếp tục PASS sau mỗi thay đổi trong Milestone 2+.

---

## 5. ARCHITECTURE BASELINE (FREEZE POINT)

| Layer | Key File | Frozen |
|-------|---------|--------|
| Domain | `Reservation.cs` (81 lines) | YES |
| Domain | `RatePlan.cs` (137 lines) | YES |
| Domain | `GroupReservation.cs` (92 lines) | YES |
| Domain | `AvailabilityModels.cs` (102 lines) | YES |
| Domain | `AllocationScoringEngine.cs` (284 lines) | YES |
| Application | `ReservationContracts.cs` (27 lines) | YES |
| Application | `ReservationCqrsHandlers.cs` (24 lines) | YES |
| Application | `ReservationHandlers.cs` (93 lines) | YES |
| Infrastructure | `EfReservationRepository.cs` (36 lines) | YES |
| Infrastructure | `AppDbContext.cs` (Reservation config, line 927) | YES |
| UI | `ReservationWorkspaceViewModel.cs` | YES |
| UI | `ReservationHubViewModel.cs` | YES |
| UI | `ReservationUiService.cs` | YES |

---

## 6. CHANGE HISTORY

| Date | Change | Reason | Approved |
|------|--------|--------|----------|
| 2026-07-21 | Initial freeze | Architecture review passed | YES |

---

MILESTONE 1 FROZEN — 2026-07-21

NEXT: MILESTONE 2 — STAY LIFECYCLE (sau khi giải quyết 4 HIGH issues)
