# ALACARTE VIP — PHÂN TÍCH TÍCH HỢP VỚI ASMILE-BO

## Ngày: 2026-06-18
## Vai trò: Solution Architect / ERP Integration Specialist

---

## 1. TỔNG QUAN KIẾN TRÚC HIỆN TẠI

### 1.1 Các module hiện có trong ASMILE-BO

| Module | Trạng thái | Mức độ hoàn thiện |
|---|---|---|
| Bán đồ ăn sáng & Cà phê (POS) | Đã hoàn thiện | 90% |
| Món ăn & Định lượng (MenuRecipe) | Đã hoàn thiện | 85% |
| Nhà hàng À La Carte (đang phát triển) | Đang phát triển | 40% |
| Nhà hàng Tiệc (Banquet) | Khung cơ bản | 20% |
| Kế toán (Accounting) | Đã có | 70% |
| Quỹ / Ngân hàng (Cash/Bank) | Đã có | 70% |
| Khách hàng (CRM) | Đã có | 60% |
| Kho / Tồn kho (Inventory) | Đã có | 65% |
| Nhà cung cấp (Supplier) | Đã có | 70% |
| Phân quyền (Permission) | Đã có | 60% |

### 1.2 Các layer hiện có

```
ASMILE.BO.sln
├── ASMILE.BO.Core           — Domain Entities
├── ASMILE.BO.Application    — Interfaces, DTOs, App Services
├── ASMILE.BO.Infrastructure — EF Core, Service Impl, Migrations
├── ASMILE.BO.App            — WPF UI (Views, ViewModels, Controls)
├── ASMILE.BO.Shared         — Shared utilities
└── ASMILE.BO.Tests.*        — Unit & Integration tests
```

---

## 2. MA TRẬN TÁI SỬ DỤNG VS TÁCH RIÊNG

### 2.1 Entity — Cái nào dùng chung, cái nào tách riêng?

| Entity | POS Cà phê | À La Carte VIP | Tiệc | Quyết định |
|---|---|---|---|---|
| `MenuItem` | ✓ Dùng chung | ✓ Dùng chung | ✓ Dùng chung | **DÙNG CHUNG** — Đây là danh mục món toàn hệ thống |
| `MenuCategory` | ✓ | ✓ | ✓ | **DÙNG CHUNG** |
| `MenuItemRecipe` | ✓ | ✓ | ✓ | **DÙNG CHUNG** — Định lượng dùng chung |
| `DiningRoom` | ✗ Không dùng | ✓ Phòng VIP | ✓ Sảnh tiệc | **DÙNG CHUNG giữa À La Carte & Tiệc** — KHÔNG share với POS |
| `DiningTable` (cố định DB) | ✓ Bàn POS | ✗ Không dùng | ✗ Không dùng | **KHÔNG DÙNG cho À La Carte** — Phải có entity riêng |
| `AlacarteOrder` | ✗ | ✓ Đơn mở VIP | ✗ | **RIÊNG cho À La Carte** — Tên hiện tại đã là Alacarte |
| `AlacarteOrderLine` | ✗ | ✓ | ✗ | **RIÊNG** |
| `Customer` | ✓ Chung | ✓ Chung | ✓ Chung | **DÙNG CHUNG** — CRM toàn hệ thống |
| `Supplier` | ✓ | ✓ | ✓ | **DÙNG CHUNG** |
| `Warehouse` | ✓ | ✓ | ✓ | **DÙNG CHUNG** |
| `CashFund` | ✓ | ✓ | ✓ | **DÙNG CHUNG** |
| `BankAccount` | ✓ | ✓ | ✓ | **DÙNG CHUNG** |
| `Permission` | ✓ | ✓ | ✓ | **DÙNG CHUNG** |

**Entity MỚI cần tạo cho À La Carte VIP:**

| Entity mới | Mô tả | Share với Tiệc? |
|---|---|---|
| `VipQuote` | Báo giá (thay thế RestaurantContractDto đang dùng tạm) | ✓ Có thể |
| `VipQuoteLine` | Dòng báo giá (có PricingRule, IsCustomized) | ✓ |
| `VipQuoteRevision` | Lịch sử thương lượng | ✓ |
| `VipQuoteService` | Dịch vụ trong báo giá (tách khỏi món) | ✓ |
| `VipOrderTable` | Bàn trong đơn VIP (sinh động, không cố định) | ✓ |
| `VipRoom` | Mở rộng DiningRoom cho VIP (thêm TableStandard, Floor, MinGuests) | ✓ |
| `MenuTemplate` | Template thực đơn | ✓ |
| `RoomPackage` | Gói phòng + dịch vụ | ✓ |
| `OrderTableSupplement` | Phát sinh theo bàn | ✓ |
| `ServiceIncident` | Sự cố phục vụ | ✓ |

### 2.2 Service — Cái nào dùng chung, cái nào viết mới?

| Service | POS Cà phê | À La Carte VIP | Quyết định |
|---|---|---|---|
| `IMenuCatalogService` | ✓ | ✓ | **DÙNG CHUNG** |
| `IMenuRecipeService` | ✓ | ✓ | **DÙNG CHUNG** |
| `IBreakfastCafeSalesService` | ✓ | ✗ | **KHÔNG DÙNG** — POS riêng |
| `IAlacarteRestaurantService` | ✗ | ✓ (cần mở rộng) | **GIỮ LẠI + MỞ RỘNG** |
| `ICustomerAppService` | ✓ | ✓ | **DÙNG CHUNG** |
| `ISupplierService` | ✓ | ✓ | **DÙNG CHUNG** |
| `IInventoryMovementService` | ✓ | ✓ | **DÙNG CHUNG** — Trừ kho khi phục vụ |
| `ICashFundService` | ✓ | ✓ | **DÙNG CHUNG** — Nhận cọc, thanh toán |
| `IBankAccountService` | ✓ | ✓ | **DÙNG CHUNG** |
| `IPermissionService` | ✓ | ✓ | **DÙNG CHUNG** |
| `IReconciliationEngine` | ✓ | ✓ | **DÙNG CHUNG** |
| `ITrainingDataService` | ✓ | ✓ | **DÙNG CHUNG** |

**Service MỚI cần tạo:**

| Service mới | Mô tả |
|---|---|
| `IVipQuoteService` | CRUD báo giá + workflow + pricing calculation |
| `IVipOrderService` | CRUD đơn VIP + table allocation + recalculation |
| `IVipRoomService` | Quản lý phòng VIP (calendar, booking, status) |
| `IMenuTemplateService` | CRUD menu template |
| `IRoomPackageService` | CRUD room package |
| `ITableAllocationEngine` | Engine chia bàn (stateless, pure function) |
| `IMenuCalculationEngine` | Engine tính món (stateless, pure function) |
| `IVipApprovalWorkflow` | State machine cho approval workflow |
| `IVipQuotePrintService` | In báo giá chuyên nghiệp (FlowDocument → XPS → PDF) |

**Lưu ý:** `ITableAllocationEngine` và `IMenuCalculationEngine` nên là **stateless service** (pure function) — không dependency injection phức tạp, chỉ nhận input → tính toán → trả output. Điều này giúp:

1. Dễ test (unit test không cần mock DB)
2. Dùng chung giữa Quote và Order
3. Dùng chung với Module Tiệc sau này

### 2.3 ViewModel — Cái nào giữ, cái nào tách?

| ViewModel hiện tại | Dùng cho | Quyết định |
|---|---|---|
| `AlacarteRestaurantViewModel` | Đang chứa TẤT CẢ (tab, settings, báo giá, đơn mở...) | **TÁCH RA** — Quá lớn, vi phạm Single Responsibility |
| `BanquetRestaurantViewModel` | Tiệc | Giữ nguyên |

**ViewModel MỚI cần tách từ AlacarteRestaurantViewModel:**

| ViewModel mới | Trách nhiệm | Tab tương ứng |
|---|---|---|
| `VipRoomViewModel` | Hiển thị phòng VIP + calendar + filter | Tab Khu vực |
| `VipQuoteBuilderViewModel` | Tạo/sửa báo giá + chọn món + chia bàn | Tab Báo giá |
| `VipQuoteListViewModel` | Danh sách báo giá + detail + bổ sung | Tab DS Báo giá |
| `VipOrderListViewModel` | Danh sách đơn mở + filter + chọn đơn | Tab Đơn mở |
| `VipOrderDetailViewModel` | Chi tiết đơn + phát sinh + thanh toán | Panel phải |
| `VipSettingsViewModel` | Cấu hình: phòng, bàn, menu template, package... | Tab Cài đặt |
| `VipDashboardViewModel` | KPI + tổng quan | Tab Tổng quan |
| `VipServiceMenuViewModel` | Thực đơn + dịch vụ (dùng chung menu catalog) | Tab Dịch vụ |

**Lợi ích của việc tách:**
- Mỗi ViewModel ~200-300 dòng (thay vì 1800+ dòng như hiện tại)
- Dễ test từng phần
- Dễ bảo trì
- Có thể load lazy (chỉ load ViewModel khi vào tab đó)

---

## 3. TÍCH HỢP VỚI CÁC MODULE KHÁC

### 3.1 Accounting / Cash / Bank

**Dùng cho:**
- Nhận đặt cọc từ báo giá → ghi nhận vào CashFund hoặc BankAccount
- Thanh toán đơn VIP → tạo PaymentVoucher
- Hoàn cọc khi hủy → tạo RefundVoucher

**Tích hợp:**
```
VipQuoteService.ApproveQuote()
  → Nếu Quote.DepositAmount > 0:
    → Gọi IAccountingService.CreateReceiptVoucher({
        Amount: Quote.DepositAmount,
        CustomerId: Quote.CustomerId,
        PaymentMethod: Quote.PaymentMethod,
        CashFundId: Quote.CashFundId,
        Description: $"Đặt cọc báo giá {Quote.QuoteNo}"
      })

VipOrderService.SettleOrder()
  → Gọi IAccountingService.CreateReceiptVoucher({
      Amount: Order.RemainingAmount,
      CustomerId: Order.CustomerId,
      ...
    })
```

**Entity mapping:**
| À La Carte | Accounting |
|---|---|
| `VipQuote.DepositAmount` | `ReceiptVoucher.Amount` (Dr Cash/Bank, Cr CustomerDeposit) |
| `VipOrder.TotalAmount` | `SalesRevenue` (Dr Cash/Bank, Cr Revenue) |
| `VipOrder.VatAmount` | `VatPayable` (Dr Customer, Cr VatPayable) |
| `VipOrder.ServiceChargeAmount` | `ServiceRevenue` |
| Hoàn cọc khi hủy | `RefundVoucher` (Dr CustomerDeposit, Cr Cash/Bank) |

### 3.2 CRM (Customer Relationship Management)

**Dùng cho:**
- Lưu thông tin khách hàng
- Lịch sử đặt phòng
- Phân hạng khách VIP
- Tự động điền thông tin khi nhập SĐT

**Tích hợp:**
```
VipQuoteBuilderViewModel
  → Khi nhập SĐT:
    → Gọi ICustomerAppService.GetByPhone(phone)
    → Nếu có: tự động điền tên, phân hạng, dị ứng, món ưa thích
    → Nếu không: tạo mới Customer (quick create)

  → Khi lưu báo giá:
    → Gắn CustomerId vào Quote
    → Cập nhật Customer.LastOrderDate
    → Tăng Customer.TotalOrders, Customer.TotalRevenue
```

**Entity mapping:**
| À La Carte | CRM |
|---|---|
| `Quote.CustomerName` | `Customer.Name` |
| `Quote.Phone` | `Customer.Phone` |
| `Quote.CustomerId` | `Customer.Id` |
| Lịch sử báo giá | `Customer.Orders` (query from Quote + Order) |

### 3.3 Inventory (Kho / Tồn kho)

**Dùng cho:**
- Kiểm tra tồn kho trước khi duyệt báo giá
- Trừ kho khi gửi bếp
- Cảnh báo thiếu nguyên liệu

**Tích hợp:**
```
VipQuoteService.ApproveQuote()
  → Với mỗi QuoteLine có MenuItemId:
    → Tính lượng nguyên liệu cần = MenuItemRecipe × QuoteLine.Qty
    → Kiểm tra IInventoryService.GetStockQuantity(itemId, warehouseId)
    → Nếu thiếu → cảnh báo "Thiếu X kg Tôm hùm cho báo giá này"
    → KHÔNG tự động trừ kho (chỉ trừ khi gửi bếp)

VipOrderService.SendToKitchen()
  → Với mỗi OrderLine gửi bếp:
    → IInventoryService.ConsumeStock({
        ItemId: recipeLine.IngredientId,
        Quantity: recipeLine.Quantity × orderLine.Qty,
        WarehouseId: order.WarehouseId,
        Reference: $"Đơn {order.OrderNo} - {orderLine.MenuItemName}"
      })
```

**Lưu ý:** Tích hợp kho là **P2** (sau khi module chạy ổn định). P0/P1 có thể bỏ qua, chỉ cảnh báo thủ công.

### 3.4 MenuRecipeModule (Món ăn & Định lượng)

**Dùng cho:**
- Lấy danh sách món để chọn vào báo giá
- Lấy định lượng để tính nguyên liệu
- Lấy giá vốn để tính lợi nhuận

**Tích hợp:**
```
VipQuoteBuilderViewModel
  → Load menu items:
    → Gọi IMenuCatalogService.GetMenuItemsAsync()
    → Hiển thị trong tab Chọn món (có filter, search, category chips)
    → Mỗi món hiển thị: tên, giá bán, giá vốn, trạng thái

  → Khi thêm món vào báo giá:
    → Lấy PricingRule từ MenuItem (nếu có) hoặc hỏi nhân viên chọn
    → Tự động tính SL dựa trên PricingRule × số khách/số bàn

  → Xem giá vốn:
    → Gọi IMenuRecipeService.CalculateCost(menuItemId)
    → Hiển thị "Giá vốn: X đ — Lợi nhuận dự kiến: Y đ (Z%)"
```

### 3.5 Permission (Phân quyền)

**Dùng chung Permission framework hiện có.**

**Quyền mới cần thêm cho À La Carte VIP:**

| Permission Key | Mô tả | Mặc định |
|---|---|---|
| `VipQuote.Create` | Tạo báo giá mới | Nhân viên KD, Manager |
| `VipQuote.Edit` | Sửa báo giá | Nhân viên KD, Manager |
| `VipQuote.Delete` | Xóa báo giá (chỉ khi NHÁP) | Manager |
| `VipQuote.Submit` | Gửi báo giá cho khách | Nhân viên KD |
| `VipQuote.Approve` | Duyệt báo giá → tạo đơn | Manager, Supervisor |
| `VipQuote.Cancel` | Hủy báo giá | Manager |
| `VipQuote.Override` | Ghi đè khóa sửa (khi đã duyệt) | Manager |
| `VipOrder.View` | Xem đơn mở | Tất cả |
| `VipOrder.Supplement` | Thêm phát sinh vào đơn | Nhân viên phục vụ |
| `VipOrder.Settle` | Thanh toán đơn | Thu ngân, Manager |
| `VipOrder.Cancel` | Hủy đơn | Manager |
| `VipRoom.Manage` | Quản lý phòng/calendar | Manager |
| `MenuTemplate.Manage` | Quản lý menu template | Manager |
| `RoomPackage.Manage` | Quản lý room package | Manager |
| `VipSettings.Manage` | Cấu hình module VIP | Admin, Manager |
| `VipReports.View` | Xem báo cáo VIP | Manager |

**Role mặc định:**

| Role | Quyền |
|---|---|
| **Admin** | Tất cả |
| **Manager** | Tất cả trừ Admin-only |
| **Supervisor** | Quote.Approve, Quote.Submit, Order.Supplement, Order.View |
| **Sales Executive** | Quote.Create, Quote.Edit, Quote.Submit |
| **Cashier** | Order.Settle, Order.View |
| **Waiter** | Order.View, Order.Supplement |

---

## 4. TÁCH RIÊNG KHỎI POS BÁN ĐỒ ĂN SÁNG & CÀ PHÊ

### 4.1 Những phần PHẢI tách riêng

| Thành phần | Lý do |
|---|---|
| **Entity `DiningTable`** | POS dùng bàn cố định (Bàn 101, Bàn 102...). À La Carte dùng bàn động sinh từ số khách (Bàn 01, Bàn 02... của đơn). KHÔNG thể share cùng entity. |
| **Service `IBreakfastCafeSalesService`** | POS có logic riêng (chọn bàn → gọi món → thanh toán). À La Carte có logic khác (báo giá → chia bàn → phục vụ phòng → thanh toán tổng). |
| **ViewModel `BreakfastCafeSalesViewModel`** | ViewModel POS quá đặc thù (sidebar POS, table card, order panel...). Không reuse được cho VIP. |
| **UI `BreakfastCafeSalesView.xaml`** | Giao diện POS khác hoàn toàn VIP (bàn lẻ vs phòng, đơn giản vs phức tạp). |

### 4.2 Những phần CÓ THỂ dùng chung

| Thành phần | Cách dùng chung |
|---|---|
| **Styles: `EnterpriseModule*`** | Dùng chung style cho TextBox, ComboBox, DataGrid, Button... |
| **Converters** | `BooleanToVisibilityConverter`, `InverseBooleanConverter`... |
| **Controls: `SearchBoxControl`, `TablePagerControl`** | Dùng chung controls |
| **Themes: `App.Themes/`** | Màu sắc, font — đồng bộ toàn app |
| **`ViewModelBase`** | Base class cho mọi ViewModel |

### 4.3 Sơ đồ tách biệt

```
┌─────────────────────────────────────────────────────────────┐
│                    ASMILE-BO (Shared)                       │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌───────────────┐  │
│  │MenuCatalog│ │   CRM    │ │Inventory │ │Accounting     │  │
│  │(dùng chung)│ │(dùng chung)│ │(dùng chung)│ │(dùng chung)   │  │
│  └──────────┘ └──────────┘ └──────────┘ └───────────────┘  │
│                                                             │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌───────────────┐  │
│  │Permission │ │ Supplier │ │Cash/Bank │ │MenuRecipe     │  │
│  │(dùng chung)│ │(dùng chung)│ │(dùng chung)│ │(dùng chung)   │  │
│  └──────────┘ └──────────┘ └──────────┘ └───────────────┘  │
└─────────────────────────────────────────────────────────────┘
         │                    │                    │
         ▼                    ▼                    ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│ POS Cà phê      │ │ À La Carte VIP  │ │ Tiệc (Banquet)  │
│ (Module riêng)  │ │ (Module riêng)  │ │ (Module riêng)  │
│                 │ │                 │ │                 │
│ Entity riêng:   │ │ Entity riêng:   │ │ Entity riêng:   │
│ • PosTable      │ │ • VipQuote      │ │ • BanquetEvent  │
│ • PosOrder      │ │ • VipOrderTable │ │ • BanquetRoom   │
│ • PosOrderLine  │ │ • VipRoom       │ │ • BanquetMenu   │
│                 │ │ • MenuTemplate  │ │                 │
│ Service riêng:  │ │ • RoomPackage   │ │ Service riêng:  │
│ • BreakfastCafe │ │                 │ │ • Banquet       │
│   SalesService  │ │ Service riêng:  │ │   Service       │
│                 │ │ • VipQuote      │ │                 │
│ VM/View riêng:  │ │ • VipOrder      │ │ VM/View riêng:  │
│ • BreakfastCafe │ │ • TableAlloc    │ │ • Banquet       │
│   SalesView     │ │ • MenuCalc      │ │   Restaurant   │
│                 │ │                 │ │   View          │
│                 │ │ VM/View riêng:  │ │                 │
│                 │ │ • VipRoomView   │ │                 │
│                 │ │ • VipQuoteView  │ │                 │
│                 │ │ • VipOrderView  │ │                 │
│                 │ │ • VipCalendar   │ │                 │
└─────────────────┘ └─────────────────┘ └─────────────────┘
```

---

## 5. DÙNG CHUNG VỚI MODULE TIỆC (BANQUET)

### 5.1 Những phần nên dùng chung

| Thành phần | À La Carte VIP | Tiệc | Mức độ share |
|---|---|---|---|
| **VipRoom / DiningRoom** | Phòng VIP với TableStandard | Sảnh tiệc với layout | **Chung entity, khác config** |
| **MenuTemplate** | Set Hải Sản VIP, Set Doanh Nghiệp... | Set Tiệc Cưới, Set Hội Nghị... | **DÙNG CHUNG hoàn toàn** |
| **RoomPackage** | Gói Sinh Nhật VIP, Gói Tiếp Khách... | Gói Tiệc Cưới, Gói Hội Nghị... | **DÙNG CHUNG hoàn toàn** |
| **TableAllocationEngine** | Chia bàn từ số khách | Chia mâm từ số khách | **DÙNG CHUNG** — cùng thuật toán |
| **MenuCalculationEngine** | 9 quy tắc tính món | PER_TABLE (chủ yếu) | **DÙNG CHUNG** — engine hỗ trợ cả 2 |
| **ApprovalWorkflow** | 10 trạng thái báo giá | Hợp đồng tiệc | **DÙNG CHUNG** — cùng state machine |
| **PrintService** | In báo giá A4 | In hợp đồng A4 | **DÙNG CHUNG** — FlowDocument engine |
| **CalendarView** | Lịch phòng VIP | Lịch sảnh tiệc | **DÙNG CHUNG** — khác data source |
| **CRM Integration** | Lịch sử khách VIP | Lịch sử khách tiệc | **DÙNG CHUNG** |
| **Accounting Integration** | Cọc + thanh toán | Cọc + thanh toán | **DÙNG CHUNG** |

### 5.2 Những phần PHẢI tách riêng giữa VIP và Tiệc

| Thành phần | Lý do tách |
|---|---|
| **Quote template/flow** | VIP: hybrid menu, linh hoạt. Tiệc: set menu cố định, ít thay đổi |
| **Table allocation** | VIP: bàn trong phòng, 4 strategies. Tiệc: mâm trong sảnh, chủ yếu đều |
| **Pricing model** | VIP: PER_PERSON chủ đạo. Tiệc: PER_TABLE chủ đạo |
| **Service charge** | VIP: theo Service Style (5-10%). Tiệc: thường cố định 5-8% |
| **Kitchen ticket** | VIP: in theo bàn trong phòng. Tiệc: in theo sảnh/mâm |

### 5.3 Lộ trình tích hợp

```
Giai đoạn 1 (Hiện tại):
  À La Carte VIP — phát triển độc lập, dùng entity riêng

Giai đoạn 2 (Sau khi VIP ổn định):
  Refactor để tách shared components:
  - TableAllocationEngine → shared service
  - MenuCalculationEngine → shared service
  - CalendarView → shared control

Giai đoạn 3 (Phát triển Tiệc):
  Tiệc kế thừa:
  - MenuTemplate, RoomPackage
  - ApprovalWorkflow
  - PrintService
  - CRM/Accounting integration
```

---

## 6. RỦI RO TÍCH HỢP

| Rủi ro | Mức độ | Giảm thiểu |
|---|---|---|
| `AlacarteRestaurantViewModel` quá lớn (1800+ dòng) | **Cao** | Tách thành 8 ViewModel nhỏ trước khi thêm tính năng mới |
| `DiningTable` bị dùng sai cho VIP | **Cao** | Tạo `VipOrderTable` riêng, không map vào DiningTable |
| MenuCalculation gắn cứng vào UI | **Trung bình** | Tách thành stateless service, test được độc lập |
| Workflow state machine không có sẵn | **Trung bình** | Dùng pattern State Machine, không if-else |
| Xung đột với module POS khi cùng truy cập MenuItem | **Thấp** | MenuItem là read-only với cả 2 module |
| Migration mới conflict với migration cũ | **Trung bình** | Tạo migration riêng cho VipQuote, VipOrderTable... |

---

## 7. KẾ HOẠCH TRIỂN KHAI TÍCH HỢP

### Phase 0 — Refactor hiện tại (1 tuần)
- Tách `AlacarteRestaurantViewModel` → 8 ViewModel nhỏ
- Tách XAML settings, quote, order thành các file riêng
- Clean up code trước khi thêm tính năng mới

### Phase 1 — Core VIP (2-3 tuần)
- Entity mới: `VipQuote`, `VipQuoteLine`, `VipOrderTable`, `VipRoom`
- Service: `VipQuoteService`, `VipOrderService`, `TableAllocationEngine`, `MenuCalculationEngine`
- UI: Tab Báo giá (Wizard), Tab Đơn mở (bàn tổng + bàn thật)
- Workflow: 10 trạng thái cơ bản

### Phase 2 — Nâng cao (2-3 tuần)
- Calendar View
- Menu Template + Room Package
- Approval Workflow đầy đủ
- Guest Recalculation Engine

### Phase 3 — Tích hợp sâu (2 tuần)
- Accounting: tự động tạo chứng từ cọc, thanh toán
- CRM: lịch sử khách, phân hạng
- Inventory: kiểm tra tồn kho, cảnh báo
- Print: in báo giá A4 chuyên nghiệp

### Phase 4 — Chia sẻ với Tiệc (2-3 tuần)
- Refactor shared components
- Tiệc kế thừa MenuTemplate, RoomPackage, CalendarView
- Tiệc dùng chung Accounting/CRM/Inventory integration
