# ALACARTE VIP — KIẾN TRÚC TRIỂN KHAI (ARCHITECTURE PLAN)

## Ngày: 2026-06-18
## Vai trò: Chief Solution Architect / ERP Architect

---

## TÀI LIỆU THAM CHIẾU

- `ALACARTE_VIP_BUSINESS_FLOW_V3.md` — Nguồn sự thật nghiệp vụ
- `ALACARTE_VIP_INTEGRATION_ANALYSIS.md` — Ranh giới tích hợp
- Kiến trúc hiện tại: ASMILE-BO Clean Architecture (Core/Application/Infrastructure/App)

---

# PHẦN 1 — CORE DOMAIN MAP

## 1.1 Sơ đồ nghiệp vụ (không phải UML)

```
                           ┌──────────────────────────────┐
                           │         KHÁCH HÀNG           │
                           │    (CRM — dùng chung)        │
                           └──────────────┬───────────────┘
                                          │
                         ┌────────────────┼────────────────┐
                         ▼                ▼                ▼
                  ┌──────────┐    ┌──────────┐    ┌──────────────┐
                  │ GỌI ĐIỆN │    │ ĐẾN TRỰC │    │ KHÁCH QUEN    │
                  │ (đặt trước)│   │ TIẾP     │    │ (CRM suggest) │
                  └────┬─────┘    └────┬─────┘    └──────┬───────┘
                       │               │                  │
                       └───────┬───────┘                  │
                               ▼                          │
                    ┌─────────────────────┐               │
                    │     BÁO GIÁ         │◄──────────────┘
                    │   (Root Aggregate)  │
                    │                     │
                    │  • QuoteNo          │
                    │  • CustomerName     │
                    │  • Phone            │
                    │  • VipRoom          │
                    │  • GuestCount       │
                    │  • TableStandard    │
                    │  • ServiceStyle     │
                    │  • EventDate/Time   │
                    │  • Status (10 states)│
                    └──────────┬──────────┘
                               │
                    ┌──────────┴──────────┐
                    ▼                     ▼
           ┌─────────────┐       ┌─────────────┐
           │ QUOTE LINE  │       │QUOTE SERVICE│
           │ (Child)      │       │ (Child)     │
           │             │       │             │
           │ • MenuItem  │       │ • Service   │
           │ • PricingRule│      │ • Qty       │
           │ • Qty       │       │ • Price     │
           │ • UnitPrice │       │ • PER_ROOM  │
           │ • LineTotal │       └─────────────┘
           └──────┬──────┘
                  │
                  ▼
           ┌─────────────┐
           │ QUOTE TABLE │
           │ (Child)      │
           │             │
           │ • TableNo   │
           │ • GuestCount│
           │ • IsVirtual │
           └─────────────┘
                  │
                  │ (khi APPROVE)
                  ▼
           ┌─────────────┐
           │  ĐẶT CỌC    │──────────▶ Accounting Module
           │ (gọi service)│            (ReceiptVoucher)
           └──────┬──────┘
                  │
                  ▼
┌──────────────────────────────────────────────────────┐
│                  ĐƠN MỞ (Root Aggregate)              │
│                                                      │
│  • OrderNo, Status (Reserved→Serving→Completed→Paid) │
│  • ActualGuestCount (có thể khác Quote)              │
│  • Linked từ Quote (QuoteId)                         │
│                                                      │
│  ┌──────────────────┐  ┌──────────────────┐          │
│  │ BÀN TỔNG (ảo)    │  │ BÀN THẬT (1..N)  │          │
│  │ IsVirtual=true   │  │ IsVirtual=false  │          │
│  │ Dùng thanh toán  │  │ Dùng phục vụ     │          │
│  └────────┬─────────┘  └────────┬─────────┘          │
│           │                     │                     │
│           │              ┌──────┴──────┐              │
│           │              │ ORDER LINE  │              │
│           │              │ (Child)     │              │
│           │              │ • MenuItem  │              │
│           │              │ • Qty       │              │
│           │              │ • IsSupplement│            │
│           │              └─────────────┘              │
│           │                                          │
│           ▼                                          │
│    ┌─────────────┐                                   │
│    │ THANH TOÁN  │──────────▶ Accounting Module      │
│    │ (gọi service)│           (ReceiptVoucher)        │
│    └─────────────┘                                   │
└──────────────────────────────────────────────────────┘
```

## 1.2 Phân loại Domain Objects

### Root Aggregate (có lifecycle độc lập)

| Aggregate | Mô tả | Lifecycle |
|---|---|---|
| **Quote** | Báo giá — tồn tại độc lập, có 10 trạng thái | Tạo → Thương lượng → Duyệt → Đã đóng |
| **VipOrder** | Đơn VIP — sinh từ Quote hoặc tạo trực tiếp (Walk-in) | Tạo → Phục vụ → Hoàn thành → Đã TT |
| **VipRoom** | Phòng VIP — quản lý độc lập với Quote/Order | Available → Reserved → Serving → Cleaning → Available |

### Child Entity (thuộc về Aggregate)

| Child | Thuộc Aggregate | Mô tả |
|---|---|---|
| **QuoteLine** | Quote | Dòng báo giá (món ăn) |
| **QuoteService** | Quote | Dòng báo giá (dịch vụ) |
| **QuoteTable** | Quote | Bàn trong báo giá (sinh từ số khách) |
| **QuoteRevision** | Quote | Lịch sử thương lượng |
| **OrderTable** | VipOrder | Bàn trong đơn (gồm bàn tổng ảo) |
| **OrderLine** | VipOrder | Dòng đơn (món + phát sinh) |
| **OrderSupplement** | VipOrder | Phát sinh thêm gắn với bàn thật |

### Workflow (State Machine)

| Workflow | Số trạng thái | Mô tả |
|---|---|---|
| **QuoteApproval** | 10 | Nháp → Nội duyệt → Gửi KH → Thương lượng → Chờ cọc → Đã cọc → Xác nhận → Tạo đơn → Hủy/Hết hạn |
| **OrderLifecycle** | 5 | Reserved → Serving → Completed → Paid → Cancelled |
| **RoomStatus** | 5 | Available → Hold → Reserved → Serving → Cleaning |

---

# PHẦN 2 — MODULE BOUNDARY (RANH GIỚI)

## 2.1 VIP Module — ĐƯỢC PHÉP

| Trách nhiệm | Mô tả |
|---|---|
| **Quản lý Báo giá** | Tạo, sửa, xóa, gửi, duyệt, hủy báo giá |
| **Tính toán menu** | Gọi MenuCalculationEngine để tính SL/giá |
| **Chia bàn** | Gọi TableAllocationEngine để sinh bàn từ số khách |
| **Quản lý Đơn VIP** | Tạo đơn (từ báo giá hoặc walk-in), phục vụ, phát sinh |
| **Quản lý Phòng VIP** | Calendar, booking, trạng thái phòng |
| **Workflow phê duyệt** | State machine cho báo giá và đơn |
| **Menu Template** | CRUD template thực đơn |
| **Room Package** | CRUD gói phòng + dịch vụ |
| **In ấn** | In báo giá, in phiếu bếp (gọi PrintService) |
| **Báo cáo VIP** | Doanh thu, tỷ lệ lấp đầy, hiệu suất |

## 2.2 VIP Module — KHÔNG ĐƯỢC PHÉP

| KHÔNG làm | Mà phải gọi |
|---|---|
| **Tạo khách hàng mới** | Gọi `ICustomerAppService` (CRM) |
| **Tạo chứng từ kế toán** | Gọi `IAccountingService` |
| **Trừ kho nguyên liệu** | Gọi `IInventoryMovementService` |
| **Quản lý quỹ tiền mặt** | Gọi `ICashFundService` |
| **Quản lý tài khoản ngân hàng** | Gọi `IBankAccountService` |
| **Quản lý danh mục món** | Gọi `IMenuCatalogService` |
| **Quản lý định lượng** | Gọi `IMenuRecipeService` |
| **Phân quyền** | Gọi `IPermissionService` |
| **Tạo nhà cung cấp** | Gọi `ISupplierService` |
| **Ghi audit log** | Gọi `IAuditLogService` |

## 2.3 Trách nhiệm từng module

### CRM Module
- Lưu thông tin khách hàng (tên, SĐT, địa chỉ, email)
- Tra cứu khách theo SĐT
- Lịch sử giao dịch (tổng hợp từ Quote + Order)
- Phân hạng khách (VIP Platinum/Gold/Silver)
- KHÔNG: tạo báo giá, quản lý phòng

### Accounting Module
- Tạo chứng từ thu (ReceiptVoucher) khi nhận cọc
- Tạo chứng từ thu khi thanh toán
- Tạo chứng từ chi (RefundVoucher) khi hoàn cọc
- Tính VAT, doanh thu, công nợ
- KHÔNG: tự ý tạo giao dịch không có reference từ VIP Module

### Inventory Module
- Kiểm tra tồn kho (số lượng hiện có)
- Trừ kho khi có lệnh xuất (từ VIP Order gửi bếp)
- Cảnh báo tồn kho thấp
- KHÔNG: tự ý trừ kho khi chưa có lệnh

### CashBank Module
- Quản lý quỹ tiền mặt (thu/chi)
- Quản lý tài khoản ngân hàng
- Đối chiếu giao dịch
- KHÔNG: tạo giao dịch không có chứng từ kế toán

### Permission Module
- Quản lý role và permission
- Kiểm tra quyền trước mỗi action
- KHÔNG: hardcode quyền trong VIP Module

---

# PHẦN 3 — ROADMAP TRIỂN KHAI

## P0 — NỀN TẢNG (Bắt buộc để demo được)

**Mục tiêu:** Có thể tạo báo giá, duyệt → đơn mở, xem đơn.

### Quote Engine (Tuần 1-2)

| Task | Output |
|---|---|
| Entity: `VipQuote`, `VipQuoteLine`, `VipQuoteService` | Migration mới |
| Service: `IVipQuoteService` | Create, Update, Get, List, Delete |
| UI: Tab Báo giá — form tạo mới (Wizard step 1-3) | Khách hàng + Phòng + Món |
| UI: Tab DS Báo giá — danh sách + detail | 3 cột như hiện tại |
| Workflow: NHÁP → GỬI KHÁCH → DUYỆT (3 trạng thái đơn giản) | Đủ để demo |
| Quy tắc tính món: PER_PERSON, PER_TABLE, PER_ROOM | 3 rules cơ bản |
| In báo giá: FlowDocument → XPS → DocumentViewer | Đã có code |

### Order Engine (Tuần 2-3)

| Task | Output |
|---|---|
| Entity: `VipOrder`, `VipOrderTable`, `VipOrderLine` | Migration mới |
| Service: `IVipOrderService` | Create (từ Quote), Get, List |
| UI: Tab Đơn mở — danh sách + click xem detail | Panel phải hiển thị |
| Logic: Duyệt báo giá → tự động tạo VipOrder | Approve flow |
| Bàn tổng ảo: tạo tự động khi tạo đơn | IsVirtual = true |
| Thanh toán đơn giản: ghi nhận Paid | Chưa tích hợp Accounting |

### Room Management (Tuần 2-3)

| Task | Output |
|---|---|
| Entity: Mở rộng `DiningRoom` → thêm `TableStandard`, `Floor` | Migration |
| Service: `IVipRoomService` | GetAvailableRooms, GetRoomCalendar |
| UI: Tab Khu vực — hiển thị phòng theo tầng + filter | Đã có UI cơ bản |

**Kết quả P0:** Demo được flow: Tạo báo giá → Duyệt → Xem đơn mở → Xem phòng.

---

## P1 — VẬN HÀNH (Dùng được thực tế)

### Menu Calculation Engine (Tuần 3-4)

| Task | Output |
|---|---|
| Stateless service: `IMenuCalculationEngine` | Tính SL/giá cho mọi quy tắc |
| 9 pricing rules đầy đủ | PER_PERSON, PER_TABLE, PER_ROOM, PER_HOURLY, PER_PACKAGE, PER_EVENT, PER_ADULT/CHILD, PER_TIERED, PER_MINIMUM |
| Tích hợp vào QuoteBuilder | Tự động tính khi chọn món |
| Tích hợp vào OrderDetail | Tự động tính lại khi đổi số khách |

### Table Allocation Engine (Tuần 4)

| Task | Output |
|---|---|
| Stateless service: `ITableAllocationEngine` | Chia bàn từ input |
| 3 strategies: MIN_TABLES, BALANCED, ROOM_LAYOUT | + CUSTOM |
| Tích hợp vào QuoteBuilder | Tự động chia bàn khi nhập số khách |
| UI: Hiển thị sơ đồ bàn đã chia | Visual table layout |

### Approval Workflow (Tuần 4-5)

| Task | Output |
|---|---|
| State machine: `IVipApprovalWorkflow` | 10 trạng thái đầy đủ |
| Permission matrix: ai được làm gì ở trạng thái nào | Tích hợp PermissionService |
| Audit log: mọi thay đổi trạng thái | QuoteRevision |
| UI: Hiển thị trạng thái + nút hành động tương ứng | Dynamic action buttons |

### Guest Recalculation (Tuần 5)

| Task | Output |
|---|---|
| Logic: đổi số khách → tính lại món + bàn | Pre-service + During-service |
| UI: Hiển thị delta (trước/sau) + xác nhận | Confirm dialog |
| Cảnh báo: món đã gửi bếp không thể giảm | Warning |

**Kết quả P1:** Module hoạt động được trong thực tế với đầy đủ quy tắc tính món, chia bàn, workflow.

---

## P2 — CHUYÊN NGHIỆP (Hoàn thiện)

| Task | Mô tả |
|---|---|
| **Calendar View** | Lịch phòng theo ngày/tuần, cảnh báo trùng, drag-drop |
| **Menu Template** | CRUD + áp dụng vào báo giá |
| **Room Package** | CRUD + áp dụng vào báo giá |
| **Tích hợp Accounting** | Tự động tạo chứng từ cọc, thanh toán, hoàn cọc |
| **Tích hợp CRM** | Nhập SĐT → tự động điền, lịch sử khách, phân hạng |
| **Kitchen Integration** | In phiếu bếp theo bàn, phiếu bổ sung, phiếu hủy |
| **Print Service** | In báo giá A4 chuyên nghiệp (đã có code, cần tích hợp MenuCalculation) |
| **Inventory Check** | Cảnh báo thiếu nguyên liệu khi duyệt báo giá (chưa tự động trừ kho) |

**Kết quả P2:** Module chuyên nghiệp, tích hợp đầy đủ với hệ thống.

---

## P3 — CAO CẤP (Sẵn sàng cho Banquet)

| Task | Mô tả |
|---|---|
| **Refactor Shared Components** | MenuCalculationEngine, TableAllocationEngine, CalendarView → tách thành project riêng |
| **Waitlist** | Danh sách chờ khi phòng đầy |
| **Recurring Quotes** | Báo giá định kỳ (hàng tuần/tháng) |
| **Loyalty** | Tự động ưu đãi khách quen |
| **Service Recovery** | Ghi nhận sự cố + đề xuất đền bù |
| **Advanced Reports** | Doanh thu/phòng, lợi nhuận, hiệu suất nhân viên |
| **Banquet Foundation** | Dùng chung: Calendar, MenuTemplate, RoomPackage, ApprovalWorkflow |

---

# PHẦN 4 — VIEWMODEL ARCHITECTURE

## 4.1 Vấn đề hiện tại

`AlacarteRestaurantViewModel` — 1800+ dòng, chứa TẤT CẢ: tab navigation, settings, báo giá, đơn mở, dịch vụ, KPI, filter... Vi phạm Single Responsibility, khó test, khó bảo trì.

## 4.2 Giải pháp: Tách thành 8 ViewModel

```
AlacarteRestaurantViewModel (1800 dòng)
          │
          ▼ TÁCH RA
          
┌─────────────────────────────────────────────────────────────┐
│  AlacarteShellViewModel (← giữ lại, chỉ làm điều hướng)     │
│  • SelectedTabIndex                                         │
│  • Sidebar navigation                                       │
│  • Chứa các sub-ViewModel dưới dạng properties              │
└─────────────────────────────────────────────────────────────┘
          │
          ├── VipRoomViewModel          (~250 dòng)
          │   • Tab Khu vực
          │   • Floor filter, Room list, Table cards
          │   • SelectRoom, OpenOrder
          │
          ├── VipQuoteBuilderViewModel  (~350 dòng)
          │   • Tab Báo giá (tạo mới)
          │   • Customer form, Room selector, Menu selector
          │   • QuoteLines, QuoteServices, QuoteTables
          │   • SaveQuote, SubmitQuote, ApproveQuote
          │
          ├── VipQuoteListViewModel     (~200 dòng)
          │   • Tab DS Báo giá (danh sách)
          │   • SavedQuotes list, SelectedQuote
          │   • Quote detail (center), Supplement (right)
          │   • EditQuote, PrintQuote, ApproveQuote
          │
          ├── VipOrderListViewModel     (~200 dòng)
          │   • Tab Đơn mở
          │   • OpenOrders list, filter
          │   • SelectOrder (→ right panel)
          │
          ├── VipOrderDetailViewModel   (~300 dòng)
          │   • Panel phải (Order detail)
          │   • OrderTables, OrderLines, Supplements
          │   • AddSupplement, SettleOrder
          │   • Sub-tabs: Món ăn, Dịch vụ, Thông tin
          │
          ├── VipServiceMenuViewModel   (~150 dòng)
          │   • Tab Dịch vụ (Thực đơn & Dịch vụ)
          │   • Menu items từ MenuCatalog
          │   • Services list
          │
          ├── VipSettingsViewModel      (~250 dòng)
          │   • Tab Cài đặt (11 sub-tabs)
          │   • Room/Table/Printer/Menu/Service settings
          │   • Restaurant info
          │
          └── VipDashboardViewModel     (~150 dòng)
              • Tab Tổng quan
              • KPIs, Alerts, Today's summary
```

## 4.3 Cơ chế giao tiếp giữa các ViewModel

```
AlacarteShellViewModel (cha)
  │
  │  Event / Messenger pattern
  │
  ├── Khi duyệt báo giá ở VipQuoteListViewModel:
  │     → Gửi message "QuoteApproved"
  │     → VipOrderListViewModel nhận → Refresh OpenOrders
  │
  ├── Khi chọn Order ở VipOrderListViewModel:
  │     → Set SelectedOrder trên VipOrderDetailViewModel
  │     → Right panel tự động cập nhật
  │
  └── Khi thay đổi Settings:
        → Gửi message "SettingsChanged"
        → Các ViewModel liên quan refresh
```

**Dùng CommunityToolkit.Mvvm Messenger:**
```text
WeakReferenceMessenger.Default.Send(new QuoteApprovedMessage(quote));
WeakReferenceMessenger.Default.Register<QuoteApprovedMessage>(this, (r, m) => { ... });
```

---

# PHẦN 5 — UI ARCHITECTURE

## 5.1 Các màn hình / tab

### Màn hình chính (luôn hiển thị)

| Tab | ViewModel | Mức độ ưu tiên |
|---|---|---|
| **Calendar** | VipCalendarViewModel | P2 — Màn hình chính sau này |
| **Khu vực** | VipRoomViewModel | P0 — Đã có |
| **Báo giá** | VipQuoteBuilderViewModel | P0 — Cần viết lại |
| **DS Báo giá** | VipQuoteListViewModel | P0 — Đã có layout |
| **Đơn mở** | VipOrderListViewModel | P0 — Đã có |
| **Dịch vụ** | VipServiceMenuViewModel | P0 — Đã có |
| **Cài đặt** | VipSettingsViewModel | P0 — Đã có layout |
| **Tổng quan** | VipDashboardViewModel | P1 |

### Màn hình phụ (popup / dialog)

| Màn hình | Mô tả | Ưu tiên |
|---|---|---|
| **Print Preview** | Xem trước in báo giá A4 | P0 — Đã có |
| **Guest Recalculation** | Xác nhận đổi số khách | P1 |
| **Table Allocation Visual** | Sơ đồ bàn kéo thả | P2 |
| **Calendar Day View** | Chi tiết 1 ngày của 1 phòng | P2 |
| **Quick Customer Create** | Tạo nhanh khách hàng mới | P1 |
| **Supplement Entry** | Thêm phát sinh cho bàn | P1 |

## 5.2 Layout tổng thể

```
┌──────────────────────────────────────────────────────────────┐
│ TOP BAR: Tiêu đề + KPI nhanh + Nút Làm mới                   │
├──────┬───────────────────────────────────────┬───────────────┤
│S     │                                       │               │
│I  78 │         NỘI DUNG CHÍNH                │  PANEL PHẢI   │
│D     │         (thay đổi theo tab)           │  520-650px    │
│E     │                                       │               │
│B     │  • Tab Khu vực: Room cards           │  • Order detail│
│A     │  • Tab Báo giá: Quote builder        │  • Hoặc ẩn     │
│R     │  • Tab DS BG: 3 cột list/detail/supp │  (Settings,    │
│      │  • Tab Đơn mở: Order list           │   DS Báo giá)  │
│7     │  • Tab Dịch vụ: Menu + Services      │               │
│TAB   │  • Tab Cài đặt: Settings menu + form │               │
│      │  • Tab Tổng quan: Dashboard KPIs     │               │
├──────┴───────────────────────────────────────┴───────────────┤
│ STATUS BAR: Trạng thái + Thông báo                          │
└──────────────────────────────────────────────────────────────┘
```

## 5.3 Quy tắc hiển thị Panel Phải

| Tab đang mở | Panel phải hiển thị |
|---|---|
| Khu vực | Order detail (khi có SelectedOrder) |
| Báo giá | Ẩn (full width cho builder) |
| DS Báo giá | Ẩn (3 cột đã đủ) |
| Đơn mở | Order detail (khi click đơn) |
| Dịch vụ | Order detail (khi có SelectedOrder) |
| Cài đặt | Ẩn (full width) |
| Tổng quan | Order detail (khi có SelectedOrder) |

---

# PHẦN 6 — SHARED COMPONENTS

## 6.1 Components nên làm SHARED từ đầu

Đây là các component mà cả À La Carte VIP và Banquet (tương lai) sẽ dùng. Làm đúng ngay từ đầu → tiết kiệm refactor sau này.

### Menu Calculation Engine

```
Dự án: ASMILE.BO.Application/Services/MenuCalculationEngine.cs

Interface:
  IMenuCalculationEngine {
    CalculateQty(MenuItem, PricingRule, GuestCount, TableCount) → decimal
    CalculateLineTotal(MenuItem, PricingRule, GuestCount, TableCount) → decimal
    ApplyTieredPricing(MenuItem, TotalQty) → UnitPrice
    RecalculateAll(QuoteLines, NewGuestCount, NewTableCount) → RecalculationResult
  }

Đặc điểm:
  - STATELESS (pure function)
  - KHÔNG dependency injection phức tạp
  - Có thể gọi từ QuoteService, OrderService, và BanquetService sau này
  - Unit test được 100% không cần mock
```

### Table Allocation Engine

```
Dự án: ASMILE.BO.Application/Services/TableAllocationEngine.cs

Interface:
  ITableAllocationEngine {
    Allocate(GuestCount, TableStandard, Strategy, RoomLayout?) → TableAllocation[]
    Reallocate(CurrentTables, NewGuestCount, Strategy) → ReallocationResult
  }

  TableAllocation {
    int TableNo, int GuestCount, bool IsVirtual
  }

  Strategy enum: MIN_TABLES | BALANCED | ROOM_LAYOUT | CUSTOM

Đặc điểm:
  - STATELESS (pure function)
  - 4 chiến lược rõ ràng
  - Dùng chung cho cả VIP và Tiệc
```

### Approval Workflow Engine

```
Dự án: ASMILE.BO.Application/Workflows/QuoteApprovalWorkflow.cs

Interface:
  IQuoteApprovalWorkflow {
    AllowedActions(QuoteStatus, UserRole) → Action[]
    Transition(QuoteStatus, Action) → (NewStatus, bool Success, string ErrorMessage)
    RequiresAudit(QuoteStatus, Action) → bool
  }

Đặc điểm:
  - State machine pattern
  - Không phụ thuộc UI
  - Có thể dùng cho Banquet Contract approval sau này
```

### Calendar Control

```
Dự án: ASMILE.BO.App/Controls/VipCalendarControl.xaml

Control WPF có thể tái sử dụng:
  - Input: ItemsSource (list of calendar entries)
  - Hiển thị: Day View / Week View / Room View
  - Event: OnSlotClick, OnSlotDoubleClick
  - Style được qua DependencyProperty

Dùng cho:
  - VIP: Lịch phòng VIP
  - Banquet: Lịch sảnh tiệc
  - Có thể: Lịch nhân viên, lịch bảo trì...
```

### Print Service

```
Dự án: ASMILE.BO.App/Services/PrintService.cs

Interface:
  IPrintService {
    ShowPrintPreview(FlowDocument) → void
    PrintDirect(FlowDocument) → void
    BuildQuoteDocument(QuoteData) → FlowDocument
    BuildOrderDocument(OrderData) → FlowDocument
  }

Dùng cho:
  - VIP: In báo giá, in phiếu bếp
  - Banquet: In hợp đồng
  - POS: In hóa đơn (đã có code riêng)
```

## 6.2 Lộ trình tách Shared Components

```
HIỆN TẠI:
  Code trong AlacarteRestaurantViewModel

P0:
  → MenuCalculationEngine (stateless class trong Application layer)
  → PrintService (đã có code ShowPrintPreview)

P1:
  → TableAllocationEngine (stateless class)
  → ApprovalWorkflow (state machine)

P2:
  → CalendarControl (WPF UserControl)

P3:
  → Tách thành project riêng: ASMILE.BO.Shared.Services
```

---

# PHẦN 7 — RISK ANALYSIS

## Top 10 Rủi Ro

### R1: ViewModel quá lớn — KHÔNG thể mở rộng
**Mức độ:** CRITICAL
**Mô tả:** `AlacarteRestaurantViewModel` hiện 1800+ dòng. Nếu tiếp tục thêm tính năng sẽ thành 3000-4000 dòng → không thể bảo trì.
**Giảm thiểu:** Tách thành 8 ViewModel ngay ở P0. Mỗi VM ≤ 350 dòng.

### R2: Dùng chung DiningTable giữa POS và VIP
**Mức độ:** CRITICAL
**Mô tả:** Nếu VIP dùng `DiningTable` (bàn cố định) sẽ gây hỏng logic POS. VIP cần bàn động sinh từ số khách.
**Giảm thiểu:** Tạo entity `VipOrderTable` riêng, không liên quan `DiningTable`.

### R3: Workflow phê duyệt quá đơn giản
**Mức độ:** HIGH
**Mô tả:** Nếu chỉ có 3 trạng thái (Nháp → Gửi → Duyệt), không phản ánh đúng thực tế thương lượng của nhà hàng VIP.
**Giảm thiểu:** Implement đủ 10 trạng thái ở P1. P0 có thể dùng 3 trạng thái đơn giản.

### R4: Menu Calculation gắn cứng vào UI
**Mức độ:** HIGH
**Mô tả:** Nếu code tính món nằm trong ViewModel, không reuse được cho Banquet.
**Giảm thiểu:** Tách `MenuCalculationEngine` thành stateless service riêng ngay từ P0.

### R5: Tích hợp Accounting sai reference
**Mức độ:** HIGH
**Mô tả:** Tạo chứng từ kế toán sai loại (thu/chi), sai tài khoản, sai số tiền → lệch sổ sách.
**Giảm thiểu:** Dùng `IAccountingService` đã có. KHÔNG tự tạo entity kế toán mới. Test kỹ flow cọc + thanh toán + hoàn cọc ở P2.

### R6: Tích hợp Inventory sai thời điểm
**Mức độ:** MEDIUM
**Mô tả:** Trừ kho quá sớm (khi tạo báo giá) hoặc quá muộn (sau thanh toán).
**Giảm thiểu:** Chỉ trừ kho khi GỬI BẾP (có OrderLine.KitchenStatus = Sent). KHÔNG trừ kho khi tạo báo giá.

### R7: Không có Calendar View → trùng lịch phòng
**Mức độ:** MEDIUM
**Mô tả:** Nếu không có calendar, nhân viên không biết phòng nào trống → double booking.
**Giảm thiểu:** Calendar View ở P2. P0/P1 dùng cảnh báo thủ công khi tạo báo giá.

### R8: Table Allocation sai strategy mặc định
**Mức độ:** MEDIUM
**Mô tả:** Dùng BALANCED cho nhà hàng thích MIN_TABLES (hoặc ngược lại) → khách không hài lòng.
**Giảm thiểu:** Cho phép cấu hình strategy mặc định theo phòng. Có preview trước khi áp dụng.

### R9: UI quá phức tạp → nhân viên không dùng được
**Mức độ:** MEDIUM
**Mô tả:** Nhà hàng VIP cần UI đơn giản, nhanh. Nếu quá nhiều bước → nhân viên bỏ qua, dùng Excel.
**Giảm thiểu:** Wizard tạo báo giá ≤ 4 bước. Tab rõ ràng. Calendar là màn hình chính.

### R10: Không tách được shared component → Banquet phải code lại từ đầu
**Mức độ:** LOW (dài hạn)
**Mô tả:** Nếu không tách `MenuCalculationEngine`, `TableAllocationEngine`, `CalendarControl` ngay từ đầu → Banquet phải viết lại toàn bộ.
**Giảm thiểu:** Làm stateless service ngay từ P0/P1. Không gắn vào ViewModel.

---

# PHẦN 8 — FINAL RECOMMENDATION

## Lời nhắn của Chief Architect gửi đội phát triển

### Trước khi bắt đầu

1. **ĐỌC tài liệu nghiệp vụ trước** — ALACARTE_VIP_BUSINESS_FLOW_V3.md là nguồn sự thật. Mọi thắc mắc về "tại sao làm thế này" đều có câu trả lời trong đó.

2. **KHÔNG code ngay vào AlacarteRestaurantViewModel** — File này cần được TÁCH RA trước khi thêm bất kỳ tính năng nào. Mỗi lần thêm code vào đây là thêm nợ kỹ thuật.

3. **Làm P0 trước, demo được ngay** — Đừng sa vào P1/P2 khi P0 chưa chạy. Flow: Tạo báo giá → Duyệt → Đơn mở phải hoạt động end-to-end trước khi thêm Menu Template hay Calendar.

### Những gì NÊN làm ngay (tuần đầu)

- Tách `AlacarteRestaurantViewModel` thành 8 ViewModel nhỏ
- Tách XAML thành các file riêng cho từng tab
- Viết `MenuCalculationEngine` (stateless, test được)
- Clean up code hiện tại (xóa code thừa, gộp style trùng)

### Những gì NÊN hoãn

- Calendar View (P2) — dùng cảnh báo thủ công trước
- Tích hợp Accounting tự động (P2) — nhập tay trước
- Tích hợp Inventory (P2) — kiểm tra thủ công trước
- Banquet shared components (P3) — tập trung VIP trước

### Những gì KHÔNG NÊN làm

- ❌ Dùng `DiningTable` cho bàn trong đơn VIP
- ❌ Hardcode pricing rules trong ViewModel
- ❌ Tự tạo entity kế toán (dùng `IAccountingService`)
- ❌ Tự tạo entity kho (dùng `IInventoryMovementService`)
- ❌ Bỏ qua audit log (mọi thay đổi cần ghi lại)
- ❌ Thiết kế UI quá phức tạp (Wizard ≤ 4 bước)

### Ưu tiên phát triển

```
TUẦN 1:     Refactor + Quote Engine cơ bản
TUẦN 2:     Order Engine + Room Management
TUẦN 3:     Menu Calculation + Table Allocation
TUẦN 4:     Approval Workflow + Guest Recalculation
TUẦN 5-6:   Calendar + Menu Template + Room Package (P2)
TUẦN 7-8:   Accounting + CRM + Inventory Integration (P2)
TUẦN 9+:    Refactor Shared Components + Banquet Foundation (P3)
```

### Nguyên tắc vàng

> **"Làm ĐƠN GIẢN trước, làm ĐÚNG sau, làm ĐẸP cuối cùng."**
>
> P0: Đơn giản — chạy được end-to-end
> P1: Đúng — đủ nghiệp vụ thực tế
> P2: Đẹp — chuyên nghiệp, tích hợp
> P3: Mở rộng — sẵn sàng cho Banquet
