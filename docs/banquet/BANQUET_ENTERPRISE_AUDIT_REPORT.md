# BANQUET MANAGEMENT PLATFORM — ENTERPRISE AUDIT REPORT

**Ngày:** 2026-07-20
**Loại:** ZERO REGRESSION AUDIT
**Trạng thái:** NEEDS IMPROVEMENT ⚠️

---

## 1. Tổng Quan Kiến Trúc

### 1.1 Domain Model (17 Entities)

| Entity | Vai trò | Status |
|--------|---------|--------|
| `BanquetRoom` | Phòng tiệc | PASS |
| `BanquetEvent` | **Aggregate Root** — Sự kiện tiệc | PASS |
| `BanquetMenuPackage` | Gói thực đơn | PASS |
| `BanquetMenuGroup` | Nhóm thực đơn | PASS |
| `BanquetMenuPackageLine` | Dòng gói thực đơn (có PricingRule) | PASS |
| `BanquetEventMenu` | Thực đơn gán vào sự kiện | PASS |
| `BanquetServiceLine` | Dịch vụ gán vào sự kiện | PASS |
| `BanquetContract` | Hợp đồng | PASS |
| `BanquetDeposit` | Đặt cọc | PASS |
| `BanquetChecklist` | Checklist công việc | PASS |
| `BanquetFloorPlan` | Sơ đồ phòng | PASS |
| `BanquetFloorPlanTable` | Bàn trong sơ đồ | PASS |
| `BanquetTimeline` | Timeline sự kiện | PASS |
| `BanquetQuantificationBatch` | Đợt xuất định lượng | PASS |
| `BanquetQuantificationLine` | Dòng định lượng | PASS |
| `BanquetPurchaseRequest` | Yêu cầu thu mua | PASS |
| `BanquetPurchaseRequestLine` | Dòng thu mua | PASS |
| `BanquetModuleSetting` | Cài đặt module | PASS |

### 1.2 Layer Structure

```
Presentation (WPF)
  ├── BanquetRestaurantView.xaml (Main view — 7 tabs)
  │     ├── BanquetDashboardPage      — Tổng quan
  │     ├── BanquetEventsPage         — Sự kiện
  │     ├── BanquetRoomsPage          — Sơ đồ phòng
  │     ├── BanquetCalendarPage       — Lịch
  │     ├── BanquetChecklistPage      — Checklist
  │     ├── BanquetReportsPage        — Báo cáo
  │     └── BanquetSettingsPage       — Cài đặt
  ├── BanquetMenuModal.xaml           — Modal chọn thực đơn
  ├── BanquetMenuSelectionView.xaml   — View chọn món
  └── BanquetContractPreviewDialog    — Dialog hợp đồng

ViewModels
  ├── BanquetRestaurantViewModel.Partial.cs  (Main VM)
  ├── BanquetMenuSelectionViewModel.cs
  └── Banquet/BanquetViewModels.cs

Application (interfaces)
  └── IBanquetRestaurantService (~20 methods)

Infrastructure
  └── BanquetRestaurantService (EF implementation)

Database (18 tables)
  └── All Banquet* entities mapped via EF Core
```

### 1.3 Technology Assessment

| Aspect | Rating | Note |
|--------|--------|------|
| DDD Aggregate Design | PARTIAL | BanquetEvent là aggregate root với 10 child collections — khá lớn |
| CQRS | NOT IMPLEMENTED | Không có Command/Query separation |
| Repository Pattern | YES | Qua IBanquetRestaurantService |
| Domain Events | NOT IMPLEMENTED | Không tích hợp với FO EventBus |
| Workflow | NOT IMPLEMENTED | Status dùng string, không có state machine |
| Rule Engine | NOT IMPLEMENTED | Pricing rule đơn giản, không dùng EP-002 |
| Observability | NOT IMPLEMENTED | Không metrics |
| EventBus | NOT IMPLEMENTED | Không publish event khi tạo tiệc/cọc/hợp đồng |

---

## 2. Điểm Mạnh

1. **Domain Coverage Đầy Đủ**: 17 entities bao phủ toàn bộ nghiệp vụ tiệc (phòng → sự kiện → menu → dịch vụ → cọc → hợp đồng → checklist → định lượng → thu mua)

2. **UI Đa Tab Trực Quan**: 7 tabs + modal + dialog, workflow rõ ràng

3. **Event-Type-Specific Fields**: BrideName, GroomName, ConferenceName, BirthdayPersonName — hỗ trợ đầy đủ loại sự kiện

4. **Quantification Engine**: Xuất định lượng + thu mua — tính năng phức tạp đã implement

5. **Floor Plan**: Sơ đồ phòng với kéo thả bàn — UI nâng cao

6. **Financial Lock**: `IsFinanciallyLocked` — bảo vệ dữ liệu tài chính sau khi chốt

7. **Contract + Deposit + Settlement**: Đầy đủ flow tài chính

8. **20 migrations đã deploy**: Database schema ổn định

---

## 3. Điểm Yếu

### 3.1 Domain Model Issues

| # | Issue | Severity | Detail |
|---|-------|----------|--------|
| 1 | **Status là string, không phải enum** | WARNING | `BanquetEvent.Status = "Draft"` — dễ lỗi typo, không có compile-time check |
| 2 | **Aggregate Root quá lớn** | WARNING | `BanquetEvent` có 10 child collections + 10+ properties — nên split |
| 3 | **Không có Domain Events** | WARNING | Tạo tiệc/cọc/hợp đồng không publish event → không tích hợp được với FO |
| 4 | **Không có State Machine** | WARNING | Status flow không được enforced — có thể nhảy trạng thái sai |
| 5 | **Không có Value Objects** | INFO | `EventDate + StartTime + EndTime` nên là `EventPeriod` value object |
| 6 | **PricingMode là string** | WARNING | "PerTable" / "PerGuest" — nên là enum |

### 3.2 Architecture Issues

| # | Issue | Severity | Detail |
|---|-------|----------|--------|
| 7 | **Không dùng Platform Layer** | WARNING | EP-001 (Workflow), EP-002 (RuleEngine), EP-003 (Observability) đều có sẵn nhưng không được dùng |
| 8 | **Không có EventBus integration** | WARNING | BanquetEvent→FO Reservation không tự động |
| 9 | **Repository pattern không chuẩn FO** | INFO | Dùng `IBanquetRestaurantService` thay vì `IBanquetRepository` |
| 10 | **Không có CQRS** | INFO | Mọi thao tác qua Service lớn, không phân biệt Command/Query |

### 3.3 Code Quality Issues

| # | Issue | Severity | Detail |
|---|-------|----------|--------|
| 11 | **Hardcode trong ViewModel** | WARNING | Mock data trong `PopulateSampleDataInMemory()` — không có fallback DB |
| 12 | **Settings lưu JSON file** | INFO | `banquet_module_settings.json` — không dùng DB |
| 13 | **ViewModel quá lớn** | INFO | `BanquetRestaurantViewModel.Partial.cs` — 10,000+ dòng |
| 14 | **Không có Unit Test cho Banquet** | WARNING | 0 tests cho module Banquet |

---

## 4. Nguy Cơ

| Risk | Impact | Mitigation |
|------|--------|------------|
| Status typo gây lỗi runtime | Medium | Chuyển sang enum |
| Aggregate quá lớn gây performance issue | Medium | Split child collections |
| Không event → không đồng bộ với FO | High | Tích hợp EventBus |
| Không state machine → sai workflow | High | Dùng EP-001 Workflow |
| Không test → regression nguy hiểm | High | Viết unit test |

---

## 5. Thiếu Nghiệp Vụ

| # | Missing | Priority | Note |
|---|---------|----------|------|
| 1 | Room block cho phòng tiệc nhiều ngày | P1 | Chưa có check trùng phòng theo ngày |
| 2 | Tính giá tự động: Menu × Tables + Services + Extras - Discount - Deposit | P1 | Đã có UI nhưng chưa có engine riêng |
| 3 | Workflow Approval: Draft → Sent → Approved → Signed → Active | P1 | Chưa có flow duyệt |
| 4 | Tích hợp với FO Reservation | P2 | Tiệc có thể tạo đặt phòng khách sạn |
| 5 | Tích hợp với FO Folio/Payment | P2 | Thanh toán tiệc qua Folio |
| 6 | Tích hợp Inventory (xuất kho NVL) | P2 | Định lượng → xuất kho tự động |
| 7 | AI Sales Assistant readiness | P3 | Chưa có data model cho AI |

---

## 6. Đề Xuất Mở Rộng (Không phá vỡ hiện trạng)

### 6.1 Short-term (Low Risk, High Value)
1. **Đổi Status string → enum** — không đổi DB (dùng HasConversion)
2. **Thêm Domain Events** — publish qua EventBus khi tạo tiệc/cọc/hợp đồng
3. **Viết Unit Test** — ít nhất 20 tests cho aggregate + service

### 6.2 Medium-term (Plan carefully)
4. **Tích hợp Workflow (EP-001)** — state machine cho BanquetEvent status
5. **Tách Aggregate** — đưa Menu, Service, Timeline ra khỏi BanquetEvent
6. **Dùng RuleEngine (EP-002)** — pricing rules, discount rules
7. **Tích hợp Observability (EP-003)** — banquet metrics

### 6.3 Long-term (Requires design doc)
8. **Tích hợp FO Reservation/Folio/Payment** — unified financial flow
9. **CQRS** — tách read/write model
10. **AI Platform integration** — sales assistant, menu recommendation

---

## 7. Đề Xuất Sửa Lỗi (nếu có)

| # | Bug | Status | Fix |
|---|-----|--------|-----|
| 1 | `SaveCustomMenuAsync` dùng EF Core → throw SqlException bị `RunBusyAsync` nuốt | Đã fix 2026-07-10 | ✅ |
| 2 | Event-type-specific fields không lưu vào DB | Đã fix (migration CompleteBanquetOverviewEventFields) | ✅ |
| 3 | Cascade delete BanquetContract, BanquetDeposit khi xóa event | Đã fix (Restrict) | ✅ |
| 4 | Hardcode "8 sảnh", "10h-22h", "12 khách" trong settings | Chưa fix | Nên đọc từ DB |
| 5 | `PopulateSampleDataInMemory()` — mock data khi DB trống | Chưa fix | Nên có fallback service |

---

## 8. Danh Sách Regression Test

| # | Test | Expected | Status |
|---|------|----------|--------|
| 1 | Tạo khách hàng mới | CustomerId != null | ⬜ |
| 2 | Tạo sự kiện tiệc mới | EventNo được generate | ⬜ |
| 3 | Chọn phòng tiệc | BanquetRoomId được set | ⬜ |
| 4 | Chọn gói thực đơn | BanquetEventMenu được tạo | ⬜ |
| 5 | Thêm món tùy chỉnh | Custom menu saved | ⬜ |
| 6 | Chọn dịch vụ (MC, âm thanh...) | BanquetServiceLine được tạo | ⬜ |
| 7 | Lưu sự kiện | Event persisted, status = "Draft" | ⬜ |
| 8 | Mở lại sự kiện đã lưu | Load đúng dữ liệu | ⬜ |
| 9 | Sửa sự kiện | Update thành công | ⬜ |
| 10 | Đặt cọc | BanquetDeposit được tạo | ⬜ |
| 11 | Tạo hợp đồng | BanquetContract được tạo | ⬜ |
| 12 | Checklist — thêm item | BanquetChecklist được tạo | ⬜ |
| 13 | Timeline — thêm item | BanquetTimeline được tạo | ⬜ |
| 14 | Xuất định lượng | BanquetQuantificationBatch được tạo | ⬜ |
| 15 | Tạo yêu cầu thu mua | BanquetPurchaseRequest được tạo | ⬜ |
| 16 | Thanh lý (financial lock) | IsFinanciallyLocked = true | ⬜ |
| 17 | Xóa sự kiện | Cascade đúng (Restrict cho Contract/Deposit) | ⬜ |

---

## 9. Kết Luận

### NEEDS IMPROVEMENT ⚠️

Module Banquet có domain model đầy đủ (17 entities), UI hoàn chỉnh (7 tabs + modals), và business flow rõ ràng.

Tuy nhiên còn 3 vấn đề chính:

1. **Chưa tích hợp với Platform Layer đã xây**: EP-001 (Workflow), EP-002 (RuleEngine), EP-003 (Observability), FO EventBus đều có sẵn nhưng không được dùng

2. **Không có Unit Test**: 0 tests — rủi ro regression cao khi sửa đổi

3. **Architecture debt**: Status string, aggregate quá lớn, không CQRS, không Domain Events

**Khuyến nghị:**
- **KHÔNG refactor ngay lập tức** — module đang chạy ổn, đừng phá
- Bắt đầu bằng việc thêm Domain Events (không phá vỡ gì)
- Viết ít nhất 20 unit tests
- Sau đó từng bước tích hợp Workflow, RuleEngine, EventBus
