# ALACARTE_VIP_IMPLEMENTATION_AUDIT

Ngày audit: 18/06/2026

Phạm vi:
- Đọc tài liệu `ALACARTE_VIP_BUSINESS_FLOW.md`, `ALACARTE_VIP_BUSINESS_FLOW_V2.md`, `ALACARTE_VIP_BUSINESS_FLOW_V3.md`, `ALACARTE_VIP_INTEGRATION_ANALYSIS.md`, `ALACARTE_VIP_ARCHITECTURE_PLAN.md`
- Đối chiếu với module À La Carte hiện tại
- Không sửa code

## 1. Hiện tại đã có gì

Kết luận ngắn:
- Module hiện tại đã vượt mức mockup đơn giản.
- Đã có nền tảng DB/service/UI đủ cho một POS phòng-bàn cơ bản.
- Nhưng chưa đạt mô hình `À La Carte VIP` theo tài liệu vì vẫn bám tư duy `bàn cố định + order POS`, chưa phải `phòng VIP + báo giá + chia bàn động + workflow nhiều trạng thái`.

Đã có sẵn:
- Entity nền tảng:
  - `DiningRoom`
  - `DiningTable`
  - `TableReservation`
  - `AlacarteOrder`
  - `AlacarteOrderLine`
  - `RestaurantExtraService`
  - `RestaurantOrderExtraService`
  - `RestaurantContract`
  - `RestaurantDeposit`
  - `RestaurantQuote`
  - `RestaurantQuoteLine`
  - `RestaurantPreparationTicket`
- Service thật trong `IAlacarteRestaurantService` / `AlacarteRestaurantService`:
  - CRUD cơ bản cho phòng và bàn
  - Validate và lưu đặt bàn
  - Mở đơn, thêm món, thêm dịch vụ, gửi bếp/bar, hủy line, thanh toán đơn, hủy đơn
  - Lưu báo giá, duyệt báo giá, tạo hợp đồng cơ bản, nhận cọc cơ bản
- UI thật:
  - KPI dashboard
  - Tab khu vực/phòng-bàn
  - Tab đặt bàn
  - Tab báo giá
  - Tab đơn mở
  - Tab dịch vụ
  - Tab cài đặt
  - Panel phải có xem chi tiết đơn, món, dịch vụ
- Tích hợp thật:
  - Menu dùng `IMenuCatalogService`
  - Quỹ/cọc có điểm gọi `ICashFundService`
  - Permission `restaurant.alacarte.view/manage` đã seed
- Test:
  - Build pass
  - Test pass
  - Có coverage cho open order, reservation validation, quote save FK safety, pay/cancel order, extra service, contract basic

Nhận định kiến trúc:
- Phần hiện tại là `POS nhà hàng theo phòng/bàn cố định có thêm báo giá`.
- Tài liệu VIP yêu cầu `booking/quote/order engine theo phòng VIP`, đó là cấp mô hình khác.

## 2. Hiện tại đang sai gì

Sai lệch lớn nhất so với tài liệu VIP:

1. Sai mô hình đơn vị phục vụ
- Tài liệu VIP lấy `phòng VIP` làm đơn vị kinh doanh chính.
- Module hiện tại vẫn lấy `DiningTable` làm trục chính để mở đơn, giữ trạng thái, thanh toán.
- Điều này đúng cho POS bàn lẻ, không đúng cho VIP room service.

2. Chưa có bàn động trong báo giá/đơn
- Tài liệu yêu cầu:
  - `QuoteTable`
  - `OrderTable`
  - `Bàn tổng` ảo
  - Chia bàn tự động từ số khách + chuẩn bàn
- Hiện tại:
  - `RestaurantQuote` chỉ có `TableCount`, `GuestCount`, `RoomName`
  - Không có entity lưu từng bàn trong báo giá
  - Không có entity lưu từng bàn trong order
  - “Bàn tổng” hiện chỉ là logic hiển thị ở ViewModel, không phải dữ liệu nghiệp vụ thật

3. Chưa có engine tính món theo rule VIP
- Tài liệu yêu cầu pricing rule là lõi:
  - `PER_PERSON`
  - `PER_TABLE`
  - `PER_ROOM`
  - `MANUAL`
  - và V2/V3 mở rộng thêm rule nâng cao
- Hiện tại:
  - `RestaurantQuoteLine` chỉ có `LineType`, `Quantity`, `UnitPrice`, `Total`
  - `AlacarteOrderLine` cũng không có `PricingRule`
  - Toàn bộ tính toán trong báo giá hiện vẫn là UI-level summary, chưa phải calculation engine

4. Workflow báo giá chưa đúng mô hình VIP
- Tài liệu V3 yêu cầu workflow nhiều trạng thái:
  - `Draft`
  - `InternalReview`
  - `SentToClient`
  - `Negotiating`
  - `AwaitingDeposit`
  - `Deposited`
  - `Confirmed`
  - `Ordered`
  - `Cancelled`
  - `Expired`
- Hiện tại:
  - Quote UI/service mới ở mức `Nháp / Đã gửi / Đã duyệt / Đã hủy`
  - Không có state machine thật
  - Không có permission matrix theo trạng thái
  - Không có revision history

5. Room calendar/timeline chưa tồn tại đúng nghĩa
- Tài liệu V2 coi room calendar là phần vận hành cốt lõi.
- Hiện tại chưa có:
  - Day/Week room timeline
  - Buffer cleaning time
  - Hold room
  - Early check-in / late checkout
  - Overbook warning
  - Slot conflict theo phòng

6. Chưa tách rõ quote, order, room, workflow, calculation
- Tài liệu architecture plan yêu cầu tách engine/service theo ranh giới.
- Hiện tại hầu hết logic dồn trong:
  - `AlacarteRestaurantService`
  - `AlacarteRestaurantViewModel`
- Đây là mức tổ chức hợp với module đang bootstrap, không hợp với VIP flow phức tạp.

7. Hợp đồng hiện tại chưa phải hợp đồng VIP thật
- `RestaurantContract` đang là entity mỏng:
  - chưa mang đủ room/time/guest/workflow
  - chưa phản ánh hợp đồng dịch vụ VIP
- Tài liệu VIP xem báo giá là gốc, hợp đồng và đặt cọc là phần của workflow quote/order, không chỉ là bảng tổng tiền đơn giản.

8. Tích hợp kho/kế toán/CRM mới ở mức điểm nối
- Tài liệu integration yêu cầu boundary rõ ràng.
- Hiện tại đã có điểm nối, nhưng chưa có orchestration VIP-specific:
  - chưa có inventory check trước duyệt quote
  - chưa có room/package/template integration
  - chưa có accounting flow phân tầng theo deposit/settlement/refund chuẩn VIP

## 3. Mockup nào cần thay bằng logic thật

Các phần đang mang tính trình diễn hoặc nửa thật nửa mock, cần thay bằng logic VIP thật:

1. “Số bàn” trong báo giá
- Hiện đang dùng input + tính UI.
- Cần thay bằng `TableAllocationEngine` sinh `QuoteTable` thật.

2. “Bàn của phòng” trong đơn mở
- Hiện là danh sách hiển thị dựng từ ViewModel.
- Cần thay bằng `OrderTable` thật, có `IsVirtual`, `GuestCount`, `TableNo`, `IsSupplement`.

3. “Bàn tổng”
- Hiện là khái niệm UI.
- Cần thành entity hoặc child entity nghiệp vụ thật trong order.

4. Tổng tiền báo giá
- Hiện chủ yếu là summary ở ViewModel.
- Cần engine tính thật dựa trên:
  - pricing rule
  - quote tables
  - service style
  - discount
  - VAT
  - service charge
  - deposit

5. Trạng thái quote
- Hiện là label UI đơn giản.
- Cần state machine thật.

6. Dịch vụ và cài đặt trong một số tab
- Một phần đã DB-backed, nhưng nhiều phần settings vẫn là collection cục bộ.
- Với VIP, những cấu hình như room standard, service style, package, hold policy, buffer time phải là cấu hình thật.

7. “Menu có sẵn”
- Hiện là UI chọn món tiện dụng.
- Trong mô hình VIP cần lên thành:
  - `MenuTemplate`
  - `RoomPackage`
  - pricing preset thật

8. DS báo giá / bổ sung chi tiết
- Hiện đã dùng DB quote thật.
- Nhưng phần “bổ sung chi tiết” chưa phải revision/workflow thật theo VIP docs.

## 4. Entity/service nào có thể giữ

Giữ nguyên hoặc giữ làm nền:

Entity có thể giữ:
- `DiningRoom`
  - dùng làm nền cho `VipRoom`
- `MenuItem`, `MenuCategory`, `MenuItemRecipe`
  - dùng chung toàn hệ thống
- `RestaurantExtraService`
  - dùng làm danh mục dịch vụ gốc
- `RestaurantDeposit`
  - có thể giữ làm lớp nhận cọc chung nếu mở rộng reference đúng cách
- `RestaurantPreparationTicket`
  - vẫn dùng được cho bếp/bar

Entity có thể giữ nhưng phải mở rộng hoặc đổi vai trò:
- `RestaurantQuote`
  - có thể giữ như shell quote giai đoạn đầu
  - nhưng sẽ thiếu nếu không thêm child table/revision/workflow fields
- `RestaurantQuoteLine`
  - có thể giữ nếu thêm `PricingRule`, metadata tính toán
- `AlacarteOrder`
  - có thể giữ như shell `VipOrder` tạm thời
  - nhưng phải bỏ tư duy order gắn 1 `DiningTableId`
- `AlacarteOrderLine`
  - có thể giữ nếu thêm `OrderTableId`, `PricingRule`, `IsSupplement`
- `RestaurantContract`
  - chỉ nên giữ nếu coi là wrapper báo cáo/tổng hợp
  - không đủ làm contract VIP trung tâm

Service có thể giữ:
- `IAlacarteRestaurantService`
  - giữ làm facade tương thích module cũ
- `IMenuCatalogService`
- `ICashFundService`
- `IPermissionService`
- các service integration dùng chung khác

Service có thể giữ nhưng nên giảm trách nhiệm:
- `AlacarteRestaurantService`
  - giữ để backward-compatible
  - nhưng không nên tiếp tục phình thêm toàn bộ logic VIP vào đây

## 5. Entity/service nào cần thêm

Theo tài liệu VIP, cần thêm tối thiểu:

Entity mới:
- `VipQuote`
  - nếu không muốn tiếp tục ép `RestaurantQuote` gánh tất cả
- `VipQuoteTable`
- `VipQuoteRevision`
- `VipQuoteService`
  - nếu muốn tách riêng service lines khỏi food lines rõ ràng
- `VipOrderTable`
- `VipOrderSupplement` hoặc cơ chế supplement rõ ràng theo bàn
- `MenuTemplate`
- `RoomPackage`
- metadata mở rộng cho room:
  - `TableStandard`
  - `Floor`
  - `MinGuests`
  - `BufferMinutes`
  - `HoldPolicy`

Field cần thêm nếu tận dụng entity cũ:
- `RestaurantQuote`:
  - workflow/status đầy đủ
  - customer link chuẩn hơn
  - service style
  - hold/deadline metadata
- `RestaurantQuoteLine`:
  - `PricingRule`
  - `DefaultQty`
  - `CalculatedQty`
  - `IsCustomized`
- `AlacarteOrder`:
  - `QuoteId`
  - `VipRoomId`
  - `ActualGuestCount`
  - `OrderType`
- `AlacarteOrderLine`:
  - `OrderTableId`
  - `PricingRule`
  - `IsSupplement`

Service mới nên thêm:
- `IVipQuoteService`
- `IVipOrderService`
- `IVipRoomService`
- `ITableAllocationEngine`
- `IMenuCalculationEngine`
- `IVipApprovalWorkflow`
- `IMenuTemplateService`
- `IRoomPackageService`
- `IVipQuotePrintService`

Nguyên tắc:
- Nếu giữ entity/service cũ để tương thích, vẫn nên thêm lớp VIP riêng thay vì nhồi tiếp vào lớp hiện tại.

## 6. ViewModel nào đang quá lớn

ViewModel quá lớn:
- `AlacarteRestaurantViewModel`
  - khoảng `2625` dòng
  - đang ôm:
    - dashboard
    - room/table filter
    - reservation
    - quote builder
    - saved quote list
    - quote print
    - open order
    - order detail
    - services
    - settings
    - restaurant info
    - local fallback/sample/settings JSON

Đánh giá:
- Đây là ViewModel “god object”.
- Với VIP flow theo V2/V3, ViewModel này sẽ không còn kiểm soát nổi lifecycle và state transitions.

Nên tách thành:
- `VipDashboardViewModel`
- `VipRoomViewModel`
- `VipQuoteBuilderViewModel`
- `VipQuoteListViewModel`
- `VipOrderListViewModel`
- `VipOrderDetailViewModel`
- `VipSettingsViewModel`
- có thể thêm `VipTimelineViewModel`

## 7. XAML nào cần tách

XAML cần tách:
- `ASMILE.BO.App/Views/AlacarteRestaurantView.xaml`
  - khoảng `1013` dòng

Lý do:
- Đang chứa quá nhiều vùng UI khác bản chất:
  - dashboard
  - room cards
  - quote builder
  - quote preview
  - quote list/detail
  - open order list
  - right panel order detail
  - settings

Theo VIP docs, nên tách thành các user control nhỏ:
- `VipDashboardSection.xaml`
- `VipRoomCalendarSection.xaml`
- `VipQuoteBuilderSection.xaml`
- `VipQuoteListSection.xaml`
- `VipOrderListSection.xaml`
- `VipOrderDetailPanel.xaml`
- `VipSettingsSection.xaml`
- `VipMenuTemplateSection.xaml`

Lợi ích:
- giảm binding chéo khó kiểm soát
- dễ thay dần mockup bằng logic thật
- giữ module cũ hoạt động trong khi xây VIP section mới

## 8. P0 cần sửa tối thiểu để đúng nghiệp vụ VIP

Nếu mục tiêu là “đúng hướng VIP tối thiểu” mà chưa đại phẫu module, P0 nên là:

1. Đổi trục nghiệp vụ từ `DiningTable` sang `DiningRoom` ở luồng báo giá
- Báo giá VIP phải bắt buộc gắn phòng.
- Không được coi `DiningTableId` là bản chất chính của quote.

2. Thêm mô hình `QuoteTable` thật
- Dù chưa làm full engine, vẫn cần bảng con lưu từng bàn sinh từ:
  - số khách
  - chuẩn bàn
  - bàn tổng

3. Thêm `PricingRule` thật cho quote line
- Không có field này thì không thể gọi là VIP engine.

4. Thêm workflow quote tối thiểu
- Ít nhất:
  - `Draft`
  - `SentToClient`
  - `Negotiating`
  - `AwaitingDeposit`
  - `Deposited`
  - `Confirmed`
  - `Ordered`
  - `Cancelled`
  - `Expired`

5. Tách order theo room, không theo fixed table
- `AlacarteOrder` hiện phải ngừng lệ thuộc vào `DiningTableId` như định danh trung tâm.
- Đơn VIP phải thuộc về phòng, các bàn là child động.

6. Bỏ logic “bàn của phòng” dựng ở ViewModel
- Thay bằng persisted `OrderTable`.

7. Thêm conflict check theo room timeslot
- Chưa cần full calendar UI, nhưng service phải chặn chồng slot phòng.

8. Khóa ranh giới module cũ
- Không thay đổi flow breakfast/cafe POS.
- Không đổi nghĩa `DiningTable` cho module khác.

## 9. P1/P2/P3 để sau

P1:
- `TableAllocationEngine` chuẩn
- `MenuCalculationEngine` cho `PER_PERSON/PER_TABLE/PER_ROOM/MANUAL`
- room timeline/calendar thật
- approval matrix + audit revision
- room hold / deposit deadline / expiry job

P2:
- service style nâng cao
- package/template
- pricing rule nâng cao của V2
- inventory warning trước duyệt quote
- contract/CRM enrich

P3:
- occupancy analytics
- profitability by quote/order
- manager override flow
- dual approval
- PDF/email/Zalo automation
- advanced reporting and operational incidents

## 10. Kế hoạch sửa an toàn, không phá module cũ

Nguyên tắc:
- Không rewrite trực tiếp module hiện tại thành VIP trong một bước.
- Không tái định nghĩa `DiningTable` toàn hệ thống.
- Không nhét tất cả logic VIP vào `AlacarteRestaurantViewModel` và `AlacarteRestaurantService`.

Kế hoạch an toàn:

1. Giữ module hiện tại như “Alacarte Basic POS”
- Toàn bộ luồng đang chạy cho phòng-bàn cố định tiếp tục hoạt động.

2. Thêm một nhánh VIP song song bên trong module À La Carte
- Dùng cùng menu, dịch vụ, quỹ, permission.
- Nhưng có quote/order/table model riêng cho VIP.

3. Dùng facade để tương thích
- `IAlacarteRestaurantService` vẫn tồn tại.
- Logic VIP mới đi qua service riêng:
  - `IVipQuoteService`
  - `IVipOrderService`
  - `IVipRoomService`

4. Triển khai additive ở DB
- Chỉ thêm bảng/field mới cho VIP.
- Không đổi nghĩa bảng cũ theo cách phá dữ liệu cũ.

5. Tách UI theo section mới
- Giữ shell `AlacarteRestaurantView`.
- Thay từng vùng bằng user control VIP-backed khi hoàn thiện.

6. Cắt dần local/sample logic
- settings local
- quote pseudo tables
- room pseudo tables
- status label đơn giản
- thay từng phần bằng persisted logic

7. Chốt boundary integration sớm
- CRM, Accounting, Cash/Bank, Inventory chỉ được gọi qua service interface
- VIP module không tự tạo logic của module khác

Kết luận:
- Module hiện tại dùng được như POS nhà hàng cơ bản có báo giá.
- Chưa phải implementation của `À La Carte VIP` theo 5 tài liệu.
- Cần coi VIP là một domain layer mở rộng có mô hình `Quote -> QuoteTable -> VipOrder -> OrderTable -> Calculation Engine -> Workflow`, không phải chỉ polish giao diện và thêm vài field trên quote/order hiện có.
