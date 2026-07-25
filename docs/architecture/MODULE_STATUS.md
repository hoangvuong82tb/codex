# MODULE STATUS — ASMILE-BO

> **Ngày**: 2026-07-21
> **Sự kiện**: Restaurant/Banquet Feature Freeze

---

## 1. Danh sách module & Trạng thái

| # | Module | Trạng thái | Mô tả |
|---|--------|-----------|-------|
| 1 | **Restaurant** | Feature Freeze | MenuRecipe, Alacarte, BreakfastCafe — đặt món, định lượng, giá vốn, bán hàng |
| 2 | **Banquet** | Feature Freeze | Tiệc — calendar, events, rooms, checklist, báo cáo, cài đặt |
| 3 | **Kitchen** | Feature Freeze | Bếp — một phần của Restaurant/MenuRecipe |
| 4 | **Inventory** | Feature Freeze | Kho — nhập/xuất, tồn kho, costing, item, warehouse, supplier |
| 5 | **Accounting** | Feature Freeze | Kế toán — GL, payables, receivables, cashbank, VAT, financial statements, fixed assets |
| 6 | **Platform** | Feature Freeze | Shell, desktop, dashboard, settings, navigation, virtual explorer |
| 7 | **Security** | Feature Freeze | Auth, roles, permissions, security admin, audit log |
| 8 | **Audit** | Feature Freeze | Audit center, timeline, correlation, replay |
| 9 | **Reporting** | Feature Freeze | Report center, report engine |
| 10 | **HR/People** | Feature Freeze | Department, person, department people |
| 11 | **Operations** | Feature Freeze | Operations center, notification center, task center, message center |
| 12 | **Assets** | Feature Freeze | Asset management, fixed assets, tool assets |
| 13 | **CRM** | Pending | Customer, VIP loyalty, guest feedback |
| 14 | **AI Assistant** | Not Started | Chưa có file nào |
| 15 | **Front Office** | **Development** | Reservation, CheckIn/Out, Stay, Folio, Posting, Payment, Cashiering, Housekeeping, Night Audit, Reporting |

---

## 2. Quy tắc Feature Freeze

### Module Feature Freeze áp dụng cho:

Restaurant, Banquet, Kitchen, Inventory, Accounting, Platform, Security, Audit, Reporting, HR, Operations, Assets

### CHỈ ĐƯỢC

- Sửa bug gây lỗi (crash, data corruption)
- Sửa lỗi bảo mật
- Sửa lỗi compile nếu phát sinh từ thay đổi hạ tầng chung

### KHÔNG ĐƯỢC

- Thêm chức năng mới
- Refactor code
- Đổi UI/UX
- Đổi workflow
- Đổi database schema
- Đổi entity
- Đổi CQRS handler
- Đổi service implementation
- Tối ưu khi không có bug
- Đổi API contract
- Đổi DTO / data contract
- Đổi migration
- "Làm đẹp" code

---

## 3. Quy trình sửa bug trên module Feature Freeze

1. **Xác nhận**: Bug phải gây crash, sai dữ liệu, hoặc lỗi bảo mật
2. **Isolate**: Sửa đúng file gây lỗi, không sửa file liên quan không cần thiết
3. **Minimal fix**: Thay đổi nhỏ nhất có thể để sửa bug
4. **Build verify**: `dotnet build` phải PASS
5. **Không scope creep**: Không "tiện thể sửa luôn" các vấn đề khác

Nếu phát hiện ý tưởng cải tiến: **KHÔNG thực hiện**. Ghi vào `docs/backlog/BANQUET_FUTURE_IMPROVEMENTS.md`

---

## 4. Module được phép phát triển

| Module | Trạng thái | Ghi chú |
|--------|-----------|--------|
| **Front Office** | Development | Module phát triển chính — toàn bộ sub-module FO |
| CRM | Pending | Có thể khởi động sau FO |
| AI Assistant | Not Started | Chưa có kế hoạch |

### Front Office sub-modules đang phát triển:

| Sub-module | Status |
|------------|--------|
| Reservation | Active (FO-EXEC-002 complete) |
| Availability Engine | Active (FO-EXEC-003 complete) |
| Rate Engine | Active (FO-EXEC-004) |
| Room Allocation | Active (FO-EXEC-005) |
| Check In / Check Out | Planned |
| Stay Lifecycle | Planned |
| Folio | Planned |
| Payment & Settlement | Planned |
| Cashiering | Planned |
| Housekeeping | Planned |
| Night Audit | Planned |
| Posting Orchestration | Planned |

---

## 5. Build Status (2026-07-21)

| Project | Status |
|---------|--------|
| ASMILE.BO.Core | PASS |
| ASMILE.BO.Shared | PASS |
| ASMILE.BO.Application | PASS |
| ASMILE.BO.Infrastructure | PASS |
| ASMILE.BO.App | PASS (0 warnings, 0 errors) |
| ASMILE.BO.Tests.Integration | PASS |
| ASMILE.BO.Tests.E2E | PASS |
| ASMILE.BO.Tests.Unit | FAIL (Front Office test files cần update tham số `Source` mới — không ảnh hưởng Restaurant/Banquet) |

---

## 6. Dependency Injection Status

Tất cả service đều đăng ký trong `DependencyInjection.cs` — PASS.

## 7. DbContext Status

`AppDbContext` — PASS. Tất cả entity đều có DbSet hoặc được discover qua navigation property.

---

RESTAURANT/BANQUET FEATURE FREEZE ENABLED

FRONT OFFICE IS NOW THE PRIMARY DEVELOPMENT MODULE
