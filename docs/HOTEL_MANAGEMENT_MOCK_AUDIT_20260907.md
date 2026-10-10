# Audit dữ liệu thật / mock — Module Quản lý khách sạn

**Ngày kiểm tra:** 07/09/2026  
**Phạm vi:** 190 code-behind View của `ASMILE.BO.App/Views`, các route từ `FrontOfficeHubViewModel`, DI production và các service EF tương ứng.  
**Nguyên tắc:** chỉ đánh dấu **Real** khi có đường `View/ViewModel -> application service -> repository/query -> EF/AppDbContext`; `Random`, DTO tĩnh hoặc thông báo “mockup” là bằng chứng ngược lại.

> Các mục phía dưới được ghi theo thời điểm rà soát. Những phần **Cập nhật/Rà soát** mới hơn ở cuối tài liệu là kết luận hiện hành và thay thế snapshot ban đầu nếu có khác biệt.

## Kết luận

Module chưa thể coi là hoàn tất toàn bộ. Có **50/190 View có tín hiệu mock** (`Random`, `LoadMockData`, `Mockup` hoặc thông báo chưa nối backend), nhưng không phải cả 50 đều đang chạy trên đường production:

- một nhóm là **fallback chết** vì constructor production đã resolve được service EF;
- một nhóm là **đã đọc DB thật nhưng thao tác vẫn mock**;
- một nhóm là **mock đang chạy thật**, cần ưu tiên thay ngay;
- một nhóm là **external integration stub có chủ đích**, không nên giả mạo thành dữ liệu thật.

## Kiểm tra sâu lần này: mock nào thực sự nằm trên production path

Đã đối chiếu thêm graph caller và đăng ký DI, không chỉ dựa trên tên file:

| Khu vực | Kết quả kiểm tra | Trạng thái cần hiển thị |
|---|---|---|
| `ASMILE.BO.Infrastructure/FrontOffice/Group/GroupEngine.cs` | Các lớp `RoomBlockService`, `RoomingListService`, `GroupBillingService`, `GroupDashboardService` trả DTO mẫu; caller hiện tại trong graph chỉ là bộ test `GRP_*`. DI production không đăng ký các lớp này. | Mock chết/test-only; không phải đường chạy production, nhưng nên xoá hoặc cô lập để tránh test giả tạo cảm giác đã hoàn thiện. |
| `EfGroupDashboardService` / `EfRoomBlockService` | Đọc `fo.DatPhongDoan` và `fo.PhongDoan` bằng EF, có property scope; dashboard và danh sách nhóm là dữ liệu thật. Room block đang là projection cấp đoàn, chưa phải aggregate block độc lập. | `Real read-only` |
| `EfRoomingListService` | Đọc rooming list thật; Import/Assign/Arrival/Departure trả trạng thái chưa sẵn sàng hoặc từ chối có chủ đích, không báo thành công giả. | `Real read-only`, write `Unsupported` |
| `EfGroupBillingService` | Tổng charge/deposit/outstanding đọc từ đoàn thật; cấu hình billing từ chối vì chưa có aggregate routing/billing. | `Real read-only`, write `Unsupported` |
| `CreateGroupWindow` | Tạo mã `GRP-MOCK-*`, khả dụng phòng bằng `Random(42)`, Save/Create chỉ hiện MessageBox và không ghi DB. | Mock đang chạy thật, P0 |
| `GroupBookingWizardWindow`, `GroupCheckInWindow`, `GroupCheckoutWindow`, `GroupRoomAssignmentWindow`, `GroupGuestManagementWindow` | Nạp danh sách/khách/phòng bằng bộ nhớ hoặc `Random`; các nút thao tác báo `UI MOCKUP`. | Mock đang chạy thật, P0/P1 |

Vì vậy không được kết luận “module Đoàn đã dùng dữ liệu thật” chỉ vì `GroupReservationViewModel` đã đọc được danh sách thật. Read path của danh sách đã thật, nhưng các cửa sổ vận hành và toàn bộ write path vẫn còn khoảng trống.

## Phân loại hiện tại

### A. Đã có đường dữ liệu thật, nhưng còn fallback hoặc thao tác phụ chưa hoàn thiện

| Màn hình | Bằng chứng | Kết luận |
|---|---|---|
| `CashierPaymentWindow` | `IHotelFolioRepository.SearchAsync` + `HotelPaymentApplicationService`; `LoadFolios()` chỉ còn helper không được gọi trên production path | Đọc/ghi thanh toán thật; cần xoá fallback chết và bổ sung UAT tài chính |
| `DepositManagementWindow` | Repository Folio + `HotelFolioApplicationService`; `LoadMockData` chỉ còn helper | Đọc/ghi đặt cọc thật; cần đối chiếu allocation/audit |
| `RefundManagementWindow` | Folio/payment repository + payment service; generator còn là helper | Đọc/ghi hoàn tiền thật theo contract hiện có; cần kiểm tra idempotency |
| `TransactionCenterWindow` | `Loaded` nạp `HotelFolioEntry` từ `IHotelFolioRepository`; `LoadTransactions()` không được gọi | Đọc giao dịch Folio thật; các nút void/reconcile/export còn placeholder |
| `CashierVoucherWindow` | Production DI đăng ký `PaymentVouchersViewModel`; cửa sổ chuyển sang `PaymentVouchersView` khi resolve được VM | Đường chính là payable/EF thật; danh sách 2.500 dòng là fallback khi DI hỏng, không được phép dùng trong production |
| `FolioWorkspaceWindow` | Loader persisted dùng `IHotelFolioRepository`; generator cũ không nằm trên loader chính; có thể nhận `StayId` để tự chọn Folio thật | Đọc thật; renderer in/xuất file chưa hoàn tất |
| `SplitFolioWindow`, `MergeFolioWindow`, `TransferPostingWindow` | Có loader persisted và application service cho mutation; còn nhánh mock target/fallback | Mixed; phải fail-closed khi service/context/schema không có |
| `LockedRoomWindow`, `StayProfileWindow` | Loader property-scoped thật; `LoadMockData` còn tồn tại nhưng không được gọi | Đọc thật; xoá helper để tránh hồi quy |
| `GroupReservationViewModel` / danh sách đoàn | `IGroupReservationQueryService -> EfGroupDashboardService -> fo.DatPhongDoan` | Đọc thật; mutation tạo/sửa đoàn chưa nối |
| Housekeeping board, Lost & Found, Minibar, Workforce | Các query/command EF đã đăng ký trong DI và có property scope | Real-path theo từng contract; mutation còn phải staging/UAT |

### B. Đã đọc Stay thật nhưng thao tác trên màn hình còn mock

| Màn hình | Phần còn mock |
|---|---|
| `TodayArrivalsWindow` | Đã chuyển nút **Check-in** sang `IReceptionUiService.CheckInAsync` với `StayId` + `RowVersion`; đổi phòng, booking detail, registration card, gửi Housekeeping/Bellman vẫn chưa có command thật |
| `TodayDeparturesWindow` | Danh sách đọc Stay thật; nút **Chuẩn bị Check-out** đã mở workflow dữ liệu thật và truyền đúng `StayId`/`RowVersion`; gia hạn, xem Folio, gửi buồng, in tạm tính và hồ sơ vẫn là placeholder |
| `CurrentGuestsWindow` | Danh sách đọc Stay thật; nút **Chuẩn bị Check-out** đã truyền hồ sơ thật vào workflow chuẩn; **Xem Folio** mở Folio Workspace và chọn đúng `StayId`; hồ sơ, thêm khách, đổi phòng, gia hạn và một số quick action vẫn chưa có mutation/query đầy đủ |
| `StayProfileWindow` | Tìm Stay thật; chỉnh sửa hồ sơ chưa có mutation đầy đủ |
| `QuickCheckoutWindow`, `ExpressCheckoutWindow` | Có loader thật hoặc liên quan Stay/Folio nhưng workflow thanh toán + checkout chưa hoàn chỉnh; vẫn fail-closed |
| `CheckoutPreparationWindow` | Đã kiểm tra Folio thật, số dư, dịch vụ và bàn giao; nút **Thực hiện Check-out** gọi `IReceptionUiService.CheckOutAsync` với `StayId` + `RowVersion`, không tạo giao dịch giả |

### C. Mock đang chạy thật — ưu tiên P0/P1

| Nhóm | Màn hình / file chính | Rủi ro |
|---|---|---|
| Group create/workflow | `CreateGroupWindow`, `GroupBookingWizardWindow`, `GroupCheckInWindow`, `GroupCheckoutWindow`, `GroupRoomAssignmentWindow`, `GroupGuestManagementWindow`, `GroupServiceViewModel` | Có thể hiển thị và cho thao tác trên dữ liệu giả; đặc biệt nguy hiểm vì card đang ghi “Đang hoạt động” |
| Folio preview/export | `ProformaFolioWindow` | Sinh Folio, posting, payment, deposit, refund bằng `Random`; print/export/email chỉ hiện thông báo |
| Phiếu thu | `CreateReceiptVoucherWindow`, một phần `ReceiptVoucherWindow` | Có thể báo “đã tạo” nhưng chưa ghi payment/document thật |
| Tỷ giá | `ExchangeRateWindow` | Toàn bộ danh sách là static; chưa có bảng/contract exchange-rate property-scoped trong `AppDbContext` |
| Shift / FO operation | `ShiftHandoverWindow`, `ShiftLogWindow`, `IncidentLogWindow`, `VipNotesWindow` | Dữ liệu và xác nhận chỉ ở bộ nhớ, chưa có transaction/audit persistence |
| Housekeeping request | `HousekeepingMessageWindow` và các quick action tương ứng | Chưa có command/task contract thống nhất |
| Hóa đơn | `InvoiceManagementWindow`, `IssueInvoiceWizard` | UI/mock provider; `StubEInvoiceProvider` là external stub có chủ đích, nhưng không được báo “đã phát hành” như hóa đơn thật |

### D. Không nên sửa bằng cách “nối đại” vào DB

- **Tỷ giá:** cần migration/entity/property scope, effective date, buy/sell/hotel rate, lịch sử, approval và cách các payment/posting dùng cùng một rate. Hiện chỉ có `ExchangeRate` trên payment/posting, không có master-rate store tương ứng.
- **Group service:** schema hiện có hồ sơ đoàn/phân phòng/rooming-list, nhưng chưa có aggregate service-order để lưu dịch vụ đoàn. Không được thay `Random` bằng cách ghi tạm vào `GroupReservation`.
- **Room block/billing instruction:** query projection đã có nhưng mutation contract, allocation, tax/commission và audit chưa đủ.
- **Phiếu thu/hóa đơn/e-invoice:** phải dùng document/payment service hiện có, idempotency và audit; không tạo bản ghi giả chỉ để làm nút bấm chạy.

## Bằng chứng đã kiểm tra

1. Quét toàn bộ 190 View code-behind, sau đó lọc các file có `Random`, `LoadMockData`, `Mockup`, `BuildFakeData`.
2. Đối chiếu route hub trong `FrontOfficeHubViewModel` với registration trong `ASMILE.BO.Infrastructure/DependencyInjection.cs` và `ASMILE.BO.App/DependencyInjection.cs`.
3. Truy vết graph các contract chính: Stay, Folio, Payment, Group, Rooming List, Reporting và Rate.
4. Kiểm tra caller của các helper mock: một số helper còn trong file nhưng không được gọi khi DI production đầy đủ; không được dùng việc helper còn tồn tại làm bằng chứng rằng màn hình đang mock.
5. Build sau thay đổi Check-in và badge trạng thái: **PASS, 0 errors, 740 warnings cũ/không chặn build**.
6. Đã sửa mapping `LostFoundItem.RowVersion` dùng cấu hình theo provider; test SQLite nền tảng trước đây lỗi `near "max"` đã pass.
7. Unit suite sau kiểm tra: **581/583 pass**. Hai test còn lại chỉ lệch kỳ vọng chuỗi không dấu so với thông báo tiếng Việt có dấu trong `ReservationWorkspaceViewModel`; không liên quan mock, DB hoặc nghiệp vụ Front Office.

## Thứ tự hoàn thiện đề xuất

### P0 — Chặn dữ liệu giả lọt vào vận hành

1. Không hiển thị `Đang hoạt động` cho các card Group/Proforma/Receipt/Exchange Rate/Shift nếu command còn mock.
2. Xoá hoặc tách rõ các fallback `Random` khỏi production path; khi thiếu DI/context thì hiển thị lỗi cấu hình, không nạp dữ liệu mẫu.
3. Chuẩn hóa log/readiness để phân biệt `Real`, `Real-read-only`, `Unsupported` và `Demo`.

### P1 — Hoàn thiện luồng vận hành có backend sẵn

1. Today Arrivals: đã nối Check-in; tiếp theo nối xem reservation thật và room-move thật.
2. Today Departures: đã nối workflow chuẩn bị + Check-out thật; tiếp theo nối Current Guests và các quick action có contract; các nút chưa có contract phải disabled/fail-closed.
3. Hoàn thiện transaction center: query paging thật, void/reversal/reconcile bằng application service và audit.
4. Hoàn thiện Split/Merge/Transfer: bỏ mock target, kiểm thử concurrency/idempotency và reload sau mutation.

### P2 — Bổ sung schema/contract còn thiếu

1. Group create → `GroupReservation` mutation thật.
2. Room block/assignment/rooming-list write transaction.
3. Group billing instruction và posting.
4. Receipt/invoice document workflow.
5. Exchange-rate master và lịch sử approval.

### P3 — Kiểm thử release

- Integration test trên database test có schema fingerprint.
- UAT liên tục: Reservation → Stay → Check-in → Folio → Payment → Invoice → Check-out → Housekeeping.
- Test property scope, permission, row-version, retry/idempotency, audit và không có mock row trên production DI.

## Kết luận triển khai

Không nên tiếp tục sửa giao diện theo từng card riêng lẻ. Bước đúng tiếp theo là **đóng P0**, sau đó hoàn thiện P1 theo các contract EF đã có. Group workflow, tỷ giá, phiếu thu và e-invoice cần migration/contract riêng; nếu chưa chốt schema thì phải giữ trạng thái `Chưa sẵn sàng`, không tạo dữ liệu giả để làm cho giao diện có vẻ hoàn thiện.

## Cập nhật sau lần rà sâu

- Đã triển khai vertical slice tạo đoàn thật: `CreateGroupRequest` → `EfGroupReservationCommandService` → domain `GroupReservation` → `fo.DatPhongDoan`, có kiểm tra property scope, mã đoàn không trùng, persistence session và audit.
- `CreateGroupWindow` không còn sinh `GRP-MOCK-*`, không còn hiển thị ma trận khả dụng bằng `Random(42)`, và không còn báo thành công khi chưa ghi DB. Save/Create lỗi sẽ hiện lỗi; các thao tác chưa có command vẫn bị báo chưa sẵn sàng.
- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite: **581/583 pass**. Hai lỗi còn lại là assertion cũ không dấu trong `ReservationWorkspaceViewModelTests`, không phát sinh từ thay đổi Group.

Các phần Group còn chưa đủ để gọi là hoàn tất nghiệp vụ: update/cancel/confirm, room block write, rooming-list write, group service, billing routing, check-in/check-out đoàn và folio mapping. Các phần này cần contract/mutation/audit riêng; không nên nối tạm vào bản ghi đoàn.

## Cập nhật triển khai tiếp theo — Check-in đoàn

- `GroupCheckInWindow` đã bỏ `LoadMockData()` và `Random`; danh sách đoàn/phân phòng đọc từ `IGroupReservationQueryService`/`EfGroupDashboardService` và lọc theo property, ngày đến, trạng thái thật.
- Đã bổ sung `IGroupCheckInCommandService`/`EfGroupCheckInCommandService`: kiểm tra execution context, property scope, trạng thái Confirmed, ngày đến/ngày đi, assignment hợp lệ và không check-in phòng đã trả; sau đó cập nhật `fo.DatPhongDoan` + `fo.PhongDoan` trong persistence session và stage audit `GroupCheckedIn`.
- Domain có transition `Confirmed -> CheckedIn`, assignment có `MarkCheckedIn`/`MarkCheckedOut` và `AssignGuest`; không có nhánh báo thành công nếu thiếu dữ liệu hoặc service.
- Đã thêm 3 unit test workflow domain: **3/3 pass**.
- Build `ASMILE.BO.App`: **PASS, 0 errors**. Warning còn lại là warning nền của solution, không chặn build.

Trạng thái mới của nhóm Đoàn: **create thật**, **query/dashboard thật**, **check-in theo assignment thật**; vẫn còn thiếu confirm/update/cancel, room assignment write, rooming-list import/guest assignment, check-out đoàn, billing instruction, service order và folio mapping.

## Cập nhật chống dữ liệu giả lọt vào vận hành

- `EfRoomingListService` đã bật các mutation có schema sẵn: gán GuestProfile thật, đánh dấu arrival/departure trên `fo.PhongDoan`, tự chuyển trạng thái đoàn khi phù hợp, kiểm tra property scope và stage audit.
- `GroupCheckInWindow` không còn KPI/card mẫu trong XAML; số liệu xác nhận được tính từ rooming list thật.
- `GroupBookingWizardWindow`, `GroupRoomAssignmentWindow`, `GroupCheckoutWindow`, `GroupGuestManagementWindow`, `GroupServiceViewModel` không còn nạp/hiển thị dữ liệu mẫu trên đường chạy chính; các thao tác chưa có command thật đều fail-closed.
- Card hub `Đoàn` đã đổi từ `Đang hoạt động` sang `Đang hoàn thiện`, vì module hiện chỉ có một phần vertical slice thật.
- Không phát sinh migration ở bước này: các mutation check-in/rooming-list dùng các cột đã tồn tại (`IsCheckedIn`, `IsCheckedOut`, `GuestId`, `GuestName`, `Status`). Migration chỉ cần khi bổ sung room-block aggregate, service order, billing instruction hoặc checkout timestamp/audit detail riêng.
- Đã bổ sung command thật cho Confirm/Cancel đoàn và Assign/Remove phòng đoàn: kiểm tra property scope, room active/available, khách thật, trùng phòng theo khoảng ngày, giới hạn số phòng và audit.
- `GroupRoomingListWindow` không còn fallback sinh đoàn/khách/phòng bằng `Random`; khi thiếu query/mutation thật thì để rỗng và fail-closed.
- `GroupReservationViewModel` không còn tạo 320 đoàn mẫu khi thiếu `IGroupReservationQueryService`; các thao tác auto-assign, bulk check-in/out, billing và thanh toán không còn cập nhật số liệu chỉ trong bộ nhớ.
- `ReceptionHubViewModel`: `Tỷ giá` và `Nhắn nhà buồng` đã đổi badge từ `Sẵn sàng` sang `Đang hoàn thiện`; hai cửa sổ không còn nạp danh sách mẫu.
- `ProformaFolioWindow`, `InvoiceManagementWindow`, `IssueInvoiceWizard`, `ShiftHandoverWindow` và `StayInformationAdjustmentWindow` không còn dựng dữ liệu mẫu trên đường chạy; các thao tác chưa có query/command persistence thật đều thông báo và không báo thành công giả.
- Build `ASMILE.BO.App`: **PASS, 0 errors**. Full unit suite hiện tại: **588/588 pass**. Đây là kiểm tra compile/unit; chưa thay thế integration/UAT trên database test đã fingerprint.
- Rà soát runtime constructor lần cuối đã chặn thêm fallback mẫu ở `CashierVoucherWindow`, `ReceiptVoucherWindow`, `IncidentLogWindow`, `ShiftLogWindow` và `VipNotesWindow`; các màn hình này hiện chỉ hiển thị dữ liệu khi có query thật/VM thật.
- Các dấu hiệu `Random` còn lại trong source được phân loại là: helper mock không còn gọi trên đường production, fallback demo/test trong một số Housekeeping/AR ViewModel cần staging contract, hoặc generator ở nghiệp vụ chưa có schema (tỷ giá, service đoàn, billing, invoice, shift/incident/VIP notes). Không được xóa máy móc nếu chưa có query/command thay thế.

## Cập nhật kiểm tra tiếp theo — Hóa đơn và Housekeeping

- `InvoiceManagementWindow` đã nối `IEInvoiceService.GetInvoicesAsync` thật, đọc tối đa 1.000 hóa đơn từ `AppDbContext.Invoices` và hiển thị rỗng khi thiếu service hoặc query lỗi; không còn gọi generator 5.000 hóa đơn `Random`.
- `InvoiceSummaryDto`/projection đã bổ sung buyer tax code, currency, subtotal, tax, total, provider invoice id và issued-at để UI không dựng số tiền/trạng thái giả. Chi tiết Folio/phòng/thanh toán vẫn ghi rõ chưa có trong summary; chưa bật thao tác phát hành mới vì cần nguồn Folio và command đầy đủ.
- `CleaningInProgressViewModel`, `CleanRoomViewModel`, `DirtyRoomViewModel`, `HousekeepingRoomStatusViewModel` đã chặn preview fallback khi thiếu `IHousekeepingBoardService`; production path chỉ nhận `GetTaskBoardAsync` thật hoặc hiển thị lỗi cấu hình/rỗng.
- Đã bỏ số liệu ngẫu nhiên trong KPI Housekeeping (`AvgMinutes`, `IsOnline`); các giá trị này giờ chỉ suy ra từ task thật.
- `LostFoundViewModel` đã chặn fallback `Random` khi thiếu `ILostFoundQueryService`; `MinibarViewModel` trước đó đã fail-closed tương tự.
- Build ứng dụng sau thay đổi: **PASS, 0 errors**. Unit suite: **588/588 pass**.

### Kết luận audit hiện tại


## Cập nhật rà soát AR — 07/09/2026

- `IReceivableService` production đã đăng ký `EfReceivableService`; không phát hiện DI đang trỏ nhầm `MockReceivableService`.
- `ArLedgerViewModel` và `ArCustomerViewModel` đọc `GetDebtSummariesAsync` thật. `ArCompanyViewModel` nay cũng lấy cùng projection công nợ thật; các trường CRM không có trong projection để trống, không tự dựng.
- `ArInvoiceViewModel` đọc `IEInvoiceService.GetInvoicesAsync` thật. Chỉ map các trường có trong `InvoiceSummaryDto`; dòng hàng, thanh toán và Folio chưa có projection thì để rỗng và thông báo rõ trên UI.
- `ArPaymentViewModel` đọc các giao dịch có credit từ `IReceivableService`; biểu đồ theo tháng dùng tổng tiền thật, không còn nhân số ngẫu nhiên.
- `ArReconcileViewModel` đọc giao dịch công nợ thật. Trạng thái đối chiếu mặc định là `Unmatched` vì chưa có bảng allocation/match; không được suy diễn là đã khớp.
- Các generator `Random` của 5 màn hình AR vẫn còn trong file chỉ để tham chiếu migration nhưng đã nằm sau nhánh `return`; khi thiếu service, màn hình fail-closed và không hiển thị mẫu.

### AR còn chưa được coi là hoàn tất

- Các lệnh thu tiền, phân bổ, hoàn tiền, hủy hóa đơn, gửi email, in/xuất và ghép đối chiếu ở một số ViewModel vẫn là thao tác trong bộ nhớ hoặc thông báo placeholder; cần nối command/application service thật trước UAT.
- `SimulateBankImport` đã được thay bằng nạp sao kê thật và auto-match thật; manual match từng dòng, Receipt Center, provider in/email và allocation persistence mở rộng vẫn còn gap riêng.
- Mapping hiện tại là projection tối thiểu: `ArInvoice` chưa có payment/folio lines; `ArCompany` chưa có CRM master; `ArReconcile` chưa có allocation entity. Không dùng các màn này làm bằng chứng rằng toàn bộ AR đã ghi dữ liệu thật.

### Kiểm chứng lần này

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution không chặn build.
- Full unit suite: **588/588 pass**.
- Không chạy migration, không ghi production database và không thay connection string trong đợt rà soát.

### Cập nhật kiểm tra sâu tiếp theo — AR mutation và fallback mock còn sót

- `CreatePaymentReceiptAsync` của `EfReceivableService` đã kiểm tra số tiền dương và, khi hệ thống đang dùng `ReceivableTransactions` làm nguồn đọc, ghi thêm một dòng credit tương ứng trong cùng lần `SaveChangesAsync`. Nhờ đó phiếu thu không bị ghi ở `BusinessDocuments` nhưng biến mất khỏi các màn AR.
- `ArCustomerViewModel`, `ArCompanyViewModel` và `ArLedgerViewModel` không còn cập nhật số dư trong bộ nhớ. Các nút thu tiền gọi `IReceivableService.CreatePaymentReceiptAsync`, kiểm tra mã khách hàng thật, sau đó tải lại từ DB.
- `ArPaymentViewModel` không còn tự sinh PaymentNo/ReceiptNo và không còn tự đánh dấu allocation. Payment Report báo rõ khi chưa có Customer/Folio selector thật; allocation/reconciliation chưa có backend persistence nên fail-closed.
- `ArInvoiceViewModel` không còn tạo phiếu thu ngẫu nhiên hoặc đổi trạng thái thanh toán trong bộ nhớ; hủy hóa đơn/gửi email/in vẫn cần command/document provider thật trước UAT.
- `ArReconcileViewModel` không còn tự đánh dấu Match/AutoMatch/Undo trong bộ nhớ khi chưa có bảng allocation/match.
- Rà soát `Random` còn lại phát hiện hai fallback có thể chạy khi DI/query rỗng: `RoomMasterViewModel` và `WorkforceViewModel`. Đã chuyển cả hai sang rỗng + lỗi cấu hình; không còn nạp phòng/nhân viên mẫu. Các `Random` khác trong nhóm đã được xác nhận nằm sau nhánh fail-closed hoặc là fixture tham chiếu migration.
- Ba nút ghi Workforce (phân công phòng, đổi ca, đổi khu vực) đã chuyển sang `Unsupported` rõ ràng vì hiện mới có query service, chưa có command persistence.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Full unit suite: **588/588 pass**.
- Không chạy migration, không ghi production database và không thay connection string.

## Dọn fallback nền tảng không còn được sử dụng — 09/09/2026

- Đã xoá `UnavailablePlatformServices.cs` vì không còn caller hoặc đăng ký DI; các implementation thật của system settings, backup, service status và printer settings đã được đăng ký ở nơi tương ứng.
- Không xoá `UnavailableEInvoiceProvider`, `NoOpProcessFaultInjector` hoặc repository rule in-memory dùng cho test: đây là fallback/provider chưa cấu hình có chủ đích và phải tiếp tục fail-closed, không được báo thành công giả.

### Kiểm chứng

- Full unit suite sau khi xoá fallback dead code: **625/625 pass, 0 skipped**.

## Cập nhật triển khai — Email outbox và gửi hóa đơn — 08/09/2026

- Bổ sung `IEmailDispatchService` và `EmailOutboxMessage`; email được validate người nhận, lưu bền vững cùng `Status`, số lần thử, thời điểm thử lại, lỗi cuối và liên kết nguồn (`Invoice`/`SourceId`).
- `InvoiceManagementWindow` đã thay nút gửi email fail-closed bằng form nhập người nhận, tiêu đề và nội dung; thao tác lưu vào outbox thật, không thông báo gửi thành công khi SMTP chưa bật.
- Bổ sung `EmailDispatchHostedService` và SMTP adapter dùng cấu hình `Email:*`. Worker chỉ gửi khi `Email:Enabled=true`, host và địa chỉ gửi hợp lệ; lỗi được lưu lại để retry, không làm mất request.
- Tạo migration `20260908142116_AddEmailOutboxPersistence` cho bảng `EmailOutboxMessages`, index trạng thái/retry và liên kết nguồn; chưa áp dụng vào database thật.

### Kiểm chứng

- Test email: **2/2 pass**; kiểm tra lưu outbox thật và từ chối recipient không hợp lệ.
- Full unit suite: **673/673 pass, 0 skipped**.
- Build qua trong full test, các cảnh báo legacy hiện hữu vẫn còn.
- SMTP mặc định tắt; không gửi email ra ngoài và không ghi production database trong lần triển khai này.

## Bổ sung — Chi tiết hóa đơn đọc từ dữ liệu thật — 08/09/2026

- `InvoiceSummaryDto` đã mang theo các dòng `InvoiceLine` thật, gồm mã hàng, diễn giải, đơn vị, số lượng, đơn giá, tiền dòng, thuế và tài khoản hạch toán.
- `EInvoiceService.GetInvoicesAsync` nạp các dòng từ EF cùng hóa đơn; `InvoiceManagementWindow` không còn suy ra phần chi tiết bằng số 0 cố định. Các khoản phòng, F&B, giặt là, minibar, spa, vận chuyển, giảm giá, phí dịch vụ và khác được tổng hợp từ diễn giải/mã dòng thật; không có dòng tương ứng thì giữ rỗng/0.
- Bổ sung test hồi quy chứng minh dòng hóa đơn và số tiền/tài khoản được trả về từ persistence.

### Kiểm chứng

- Build App qua với **0 lỗi**.
- Unit suite: **670/670 pass, 0 skipped**.
- Không thêm migration; không ghi production database.

### Kết luận rà soát mock sau cùng

- Các `new Random` còn được graph tìm thấy trong một số `LoadAsync` của AR, Housekeeping, Minibar, Lost & Found và Room Master đều nằm ở nhánh fixture sau đường `return` dữ liệu thật hoặc helper không có caller production; không được dùng để bind dữ liệu hiện tại.
- `TransactionCenterWindow` và `FolioReviewWindow` còn helper cũ `LoadTransactions`/`LoadGuests` không có caller; đường chạy thực tế lần lượt là `IHotelFolioRepository.SearchAsync` và `IHotelStayRepository.GetCurrentInHouseAsync` kết hợp `IHotelFolioRepository.GetByStayAsync`.
- `CreateGroupWindow` chỉ giữ collection tài liệu trong bộ nhớ vì chưa có aggregate/repository/command tài liệu nhóm; nút tải tài liệu chủ động báo thiếu contract và không tạo bản ghi giả.
- Vì vậy phần còn lại không phải là “bật mock lên”, mà là các contract nghiệp vụ cần thiết kế riêng: tài liệu nhóm, payment allocation/reversal, chứng từ, email, một số tiện ích Group/Housekeeping và các command Room Detail/Locked Room. Các nút này đang fail-closed.

## Cập nhật triển khai — Group Checkout đọc billing/dịch vụ/đặt cọc thật — 08/09/2026

- `GroupCheckoutWindow` đã resolve `IGroupBillingService`, `IGroupServiceOrderQueryService` và `IGroupDepositService` từ DI khi chọn đoàn.
- Bước `Kiểm tra Folio` hiển thị summary từ `EfGroupBillingService` và ledger thật khi có dữ liệu; fallback chỉ dùng tổng đã đọc từ `GroupReservation`, không sinh số liệu mẫu.
- Bước `Dịch vụ & bàn giao` hiển thị các `GroupServiceOrderDto` thật; Minibar, tài sản và bàn giao Housekeeping vẫn được đánh dấu thiếu contract, không xác nhận giả.
- Bước `Hóa đơn & thanh toán` hiển thị billing/deposit thật và nêu rõ chưa có composite command cho invoice/payment allocation.
- Bước xác nhận mô tả đúng phạm vi của `EfGroupCheckOutCommandService`: ghi check-out assignment/group thật, không tự ghi hóa đơn, payment allocation, minibar hoặc bàn giao.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**, 742 warnings hiện hữu.
- Unit suite: **657/657 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

## Rà soát bổ sung — Không còn mock trên đường chạy DI Hotel — 08/09/2026

- Đã kiểm tra các service có tên `Mock*`, `SampleDevice` và các `LoadMockData` trong App/Infrastructure. Các service mock kế toán, Door Lock và các loader dữ liệu mẫu không được đăng ký trong production DI; DI hiện dùng `Ef*`/`Unavailable*` tương ứng.
- Các View/Window như Current Guests, Today Arrivals/Departures, Housekeeping Message và Locked Room đều tải dữ liệu qua repository/service thật khi chạy; helper `LoadMockData` còn lại chỉ là mã tương thích nguồn và không được gọi trên constructor/Loaded path.
- Các generator `new Random(...)` còn trong một số ViewModel Hotel đều nằm sau `return` của nhánh dữ liệu thật hoặc nhánh fail-closed, được giữ làm tham chiếu di trú; không thể tạo dòng hiển thị trong đường chạy hiện tại. `RoomMasterViewModel` đã đổi tên bộ lọc fallback thành `ApplyInMemoryFilters` để không ngụ ý đang lọc mock.
- Door Lock đang resolve `EfDoorLockService`, `EfDoorEventService`, `EfDoorLockDashboardService`; các truy vấn này đọc `DoorLockDevices`/event persistence và không dùng `DoorLockEngine.SampleDevice`.

## Hoàn thiện room block thật cho Đặt phòng đoàn — 08/09/2026

- Đã bổ sung aggregate `GroupRoomBlock` và DbSet/mapping cho bảng `fo.RoomBlockDoan`; room block có property scope, hạng phòng thật, số yêu cầu/đã block/pickup, ngày ở, cutoff, rate, trạng thái và row version.
- `EfRoomBlockService` đã thay toàn bộ DTO mẫu bằng query/command EF thật: tạo, sửa, release, pickup, dashboard và trend đều đọc/ghi database, có audit và commit qua `IFrontOfficePersistenceSession`.
- `CreateGroupWindow` không còn danh sách hạng phòng hard-code: tải hạng phòng active từ `IHotelRoomMasterDataRepository`; khi tạo đoàn sẽ xác thực từng hạng phòng thật và lưu room block trong cùng transaction với group/service draft.
- `Đặt phòng đoàn` hiện đi vào `CreateGroupWindow`; `GroupBookingWizardWindow`/`GroupEngine` còn tồn tại chỉ là legacy/test code và không được đăng ký trong DI production.
- Nút kiểm tra khả dụng hiện đọc số phòng vật lý active theo hạng phòng thật. Nó cố ý chưa trừ booking theo ngày và không tuyên bố đã giữ phòng; việc gán phòng vật lý/kiểm tra xung đột vẫn thuộc Rooming List/Room Assignment.
- Khi group được confirm/cancel, room block được confirm/release theo cùng command và được audit.
- Tạo migration `20260908090000_AddGroupRoomBlocks`; chỉ tạo `fo.RoomBlockDoan` và index/FK liên quan. Chưa chạy migration và chưa ghi production database.

### Kiểm chứng sau room block

- Build `ASMILE.BO.App`: **PASS, 0 errors** (749 warnings hiện hữu của solution).
- Test riêng `GroupReservationWorkflowTests`: **7/7 pass**.
- Full unit suite: **604/605 pass**; 1 test timing-sensitive cũ `BackgroundSchedulerServiceTests.Cancellation_PropagatesToRunningExecution` fail, không liên quan room block.
- Rà DI: các service runtime của Group/Hotel Master dùng implementation EF; các `GroupEngine`/`CashierEngine`/`ConciergeEngine`/`MaintenanceEngine` chứa DTO mẫu không được đăng ký làm implementation production. Các helper `LoadMockData` còn lại là code chết/legacy hoặc fallback không được gọi trên đường nạp dữ liệu thật.

### Phần còn thiếu để hoàn thiện module

- Chạy migration trên database test, tạo hạng phòng/phòng/đoàn mẫu và smoke test tạo đoàn → confirm → room block → assignment → pickup.
- Tính availability theo ngày bằng cách giao room block với reservation/stay thật; hiện mới đọc số phòng active tổng.
- Nối group deposit/master folio, invoice/report/export/print/email và các tiện ích Group còn fail-closed; không bật lại DTO mẫu.

### Ghi chú phạm vi màn hình tạo đoàn

- `CreateGroupWindow` đã ghi thật phần lõi của đoàn qua `IGroupReservationCommandService.CreateAsync`; các trường form mở rộng như đặt cọc, commission/tax, dịch vụ, tài liệu và các ghi chú theo bộ phận hiện chưa có command persistence tương ứng. Thông báo sau khi tạo đã sửa để nói đúng phạm vi, không còn khẳng định toàn bộ form đã được lưu.

## Cập nhật — Ghi dịch vụ đoàn cùng transaction tạo đoàn — 08/09/2026

- Bổ sung `CreateGroupServiceDraft` vào `CreateGroupRequest` và xử lý trong `EfGroupReservationCommandService`.
- Các yêu cầu dịch vụ có nội dung trong `CreateGroupWindow` giờ được chuyển thành `GroupServiceOrder` thật, dùng ngày/giờ, số lượng, bộ phận, phân loại, routing và thuế/phí đã nhập; chúng được add trước cùng lần `CommitAsync` với `GroupReservation`.
- Audit tạo đoàn ghi thêm audit intent cho từng service được tạo cùng đoàn. Nếu service không hợp lệ, toàn bộ commit bị lỗi thay vì tạo đoàn thiếu một phần dịch vụ.
- Vẫn chưa bật room block thật vì `EfRoomBlockService` hiện chỉ có read projection và cố ý fail-closed ở các lệnh create/modify/release; đặt cọc/tài liệu vẫn cần aggregate/contract riêng.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite: **603/603 pass, 0 skipped**.

## Cập nhật triển khai — Group Service sửa/hủy và Housekeeping kiểm tra hàng loạt — 08/09/2026

- `GroupServiceViewModel` không còn dựng lịch sử trạng thái bằng timestamp/user giả. Tab lịch sử hiện thông báo rõ cần API audit chi tiết; timeline ngày vẫn lấy từ danh sách dịch vụ thật.
- Dịch vụ đoàn đã có sửa thật qua EF với property scope, chỉ cho sửa khi `Pending`, chặn khi đã phát sinh thanh toán, kiểm tra `RowVersion` và ghi audit. Thao tác `Xóa` được chuyển thành hủy nghiệp vụ, không xóa vật lý và không báo thành công giả.
- `CleanRoomViewModel.BatchInspect` đã dùng `IHousekeepingWorkflowService.ExecuteTaskAsync` cho từng `HousekeepingTaskId` thật. Chỉ những phòng ở trạng thái `Inspection` và có task thật mới được xử lý; nếu command bị từ chối thì không cập nhật trạng thái giao diện thành công.
- Quét lại mã App hiện còn các helper `Random`/generator legacy trong một số màn hình. Các đường mở màn hình chính đã được kiểm tra theo constructor/Loaded: Folio, Proforma, Issue Invoice, Transaction, Express/Quick Checkout, Folio Review và các màn hình phiếu thu/chi đều có nhánh đọc DB/DI thật hoặc rỗng fail-closed; generator còn lại là helper fallback chết hoặc code tham chiếu cũ, không được dùng làm bằng chứng dữ liệu đang hiển thị.
- Khoảng trống nghiệp vụ thật còn lại cần triển khai theo vertical slice: audit query cho Group Service/Housekeeping, Housekeeping ảnh/liên kết Reservation-Folio-Maintenance-Minibar-LostFound, tiện ích Room Master (sửa hồ sơ/batch/QR/lịch sử/liên kết/export), Group Folio/deposit/invoice/report/rooming list, và các command Shift/VIP/Incident/Marketing/e-invoice/payment allocation-reversal còn thiếu schema hoặc contract.

### Kiểm chứng sau vòng này

- Build `ASMILE.BO.App`: **PASS, 0 errors**, 740 warnings hiện hữu của solution.
- Test Housekeeping + Group Service: **26/26 pass**.
- Unit suite (loại integration): **635/635 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string trong vòng này.

## Cập nhật triển khai — Room Master batch OOO/OOS dùng command thật — 08/09/2026

- Room Master đã có checkbox chọn nhiều phòng trong bảng dữ liệu thật.
- Batch `OOO` và `OOS` gọi `ChangeHotelRoomOperationalStatusCommandHandler` cho từng phòng, truyền `RowVersion`, ghi audit qua persistence và tải lại danh sách sau khi xử lý.
- Kết quả batch phân biệt thành công/từ chối; phòng lỗi hoặc xung đột không bị đánh dấu thành công trên giao diện.
- Batch `Sạch/Bẩn` vẫn bị khóa có chủ đích vì command hiện có chỉ đổi operational status, chưa có Housekeeping task/state command tương ứng. Không ánh xạ sai `Sạch/Bẩn` sang `Available`.
- QR, export/print và liên kết Reservation/Folio/Room Move của Room Master tiếp tục fail-closed do chưa có generator/storage hoặc query/command liên kết đủ dữ liệu.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warnings hiện hữu không phát sinh từ lát cắt Maintenance mới.
- Unit suite (loại integration): **637/637 pass, 0 skipped**.
- Chưa chạy migration và chưa ghi production database.

## Cập nhật — Rooming List mở quản lý khách thật — 08/09/2026

- Nút `+ Thêm khách` không còn hiển thị thông báo chặn giả. Nó mở `GroupGuestManagementWindow`, nơi hồ sơ và giấy tờ khách được lưu qua repository thật; sau khi đóng, Rooming List tải lại dữ liệu.
- Việc mở màn hình quản lý khách không tự thêm bản ghi hoặc tự báo thành công nếu người dùng không lưu.

## Cập nhật — Rooming List kiểm tra hồ sơ, giấy tờ và sức chứa bằng dữ liệu thật — 08/09/2026

- Rooming List hiện đọc `GuestProfile` thật (kèm `Gender`, `Documents`, phone, email, nationality, notes); Passport/Visa không còn để trống giả khi hồ sơ có document thật.
- Kiểm tra sức chứa không còn trả `true` cố định: màn hình đọc Room Master/Room Type, cộng occupancy người lớn/trẻ em theo assignment và đánh dấu thiếu master hoặc vượt sức chứa.
- Kiểm tra sharing không khác giới chỉ đạt khi dữ liệu giới tính thật đủ cho các phòng sharing; nếu thiếu giới tính hoặc phát hiện khác giới, kết quả không được coi là hợp lệ.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 750 warning hiện hữu của solution.
- Chưa chạy migration và chưa ghi production database.

## Cập nhật triển khai — Room Master mở Reservation/Folio theo dữ liệu thật — 08/09/2026

- Context menu `Mở Reservation` truy vấn `IReservationRepository.SearchAsync` theo `RoomId`, lọc Reservation còn hiệu lực theo business date, rồi mở Reservation workspace với bản ghi thật được chọn.
- Context menu `Mở Folio` truy vấn Stay đang ở thật qua `IHotelStayRepository.GetCurrentInHouseAsync`, đối chiếu `RoomId/RoomNumber`, rồi mở Folio workspace theo `HotelStayId` thật.
- Khi thiếu property context, service, Reservation hoặc Stay thật, thao tác dừng với thông báo lỗi; không tự tạo mã hoặc mở dữ liệu mẫu.
- Room Move từ Room Master vẫn giữ fail-closed vì màn hình hiện tại chưa nhận `HotelStayId`/RowVersion từ phòng được chọn để bảo đảm command chuyển phòng đúng aggregate.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 740 warnings hiện hữu.
- Unit suite (loại integration): **635/635 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Cập nhật kiểm chứng toàn module sau khi giải phóng dung lượng — 09/09/2026

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite: **625/625 pass, 0 skipped**.
- Integration suite: **22/22 pass, 0 skipped**.
- E2E suite: **4/4 pass, 0 skipped**.
- Rà lại các màn hình từng có cảnh báo chưa sẵn sàng: đường chạy chính dùng service/EF thật; cảnh báo còn lại là fail-closed khi thiếu context, định danh Stay/row-version hoặc không có bản ghi thật.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật — Tách Folio chỉ xác nhận khi đủ dữ liệu thật — 09/09/2026

- `SplitFolioWindow` không còn coi các mục Readiness như “Quy tắc tách hợp lệ”, “Cân”, “Không vượt nguồn”, “Payer mapping” hoặc “Approval” là đạt chỉ vì cờ giao diện được gán sẵn `true`.
- Bước xác nhận hiện kiểm tra trực tiếp: Folio nguồn/đích có GUID và `rowversion` thật, đích khác nguồn, chỉ một Folio đích, khoản mục có `EntryGuid`, khoản mục không khóa và cho phép tách, phân bổ dương không vượt `Net`, tổng tách không vượt balance nguồn, currency nguồn/đích khớp và rule thật là `ByItem`.
- Khi không tải được Folio thật, màn hình vẫn có thể đi qua các bước để người dùng biết thiếu dữ liệu, nhưng nút xác nhận bị khóa và không có fallback mock.
- Nghiệp vụ backend `HotelFolioApplicationService.SplitAsync` tiếp tục là điểm ghi thật duy nhất; UI truyền `FolioSplitAllocation` theo entry thật và row-version để backend kiểm tra concurrency, entry tồn tại và số tiền còn được phép tách.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution: 744.
- Unit suite: **625/625 pass, 0 skipped**.
- Đã reindex graph sau thay đổi: 53,084 nodes / 158,724 edges.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật rà soát dữ liệu giả — 09/09/2026

- Re-index lại workspace `D-ASMILE-BO` sau các đợt dọn mock; graph hiện có 53.084 node và 158.788 quan hệ.
- Xác nhận `RoomDetailWindow` là cửa sổ legacy không có caller và chỉ dựng phòng/tiện nghi/lịch sử bảo trì tĩnh; đã xoá cửa sổ này. `RoomMasterView` hiện mở panel chi tiết từ `RoomMasterViewModel` và repository thật.
- Xoá fixture sinh phòng bằng `Random` nằm sau nhánh `return` trong `RoomMasterViewModel`; khi thiếu repository/property, màn hình chỉ rỗng và báo rõ, không tạo phòng giả.
- Rà lại các màn Folio/Receipt/Proforma/Transfer/Split: đường chạy hiện tại đọc `IHotelFolioRepository`/`HotelFolioApplicationService` thật hoặc fail-closed. Các generator lịch sử còn trong `#if false` không được biên dịch/gọi.
- `RoomMoveWindow` không còn tự gán giá cũ theo loại phòng (ví dụ Deluxe = 1.250.000). Vì chưa có rate plan thật gắn với stay/phòng trong workflow này, giá và chênh lệch hiện báo “Chưa có giá thật”; thao tác đổi phòng vẫn chỉ ghi qua `MoveRoomAsync`, không phát sinh phí giả.
- Các khoảng trống còn lại được phân loại rõ: Channel/Executive chưa có adapter production/DI; Marketing, export/print/email và một số payment allocation/reversal vẫn fail-closed; `TrainingDataService` là công cụ seed demo có chủ đích, không phải nguồn dữ liệu vận hành.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors** (744 warnings hiện hữu của solution).
- Unit suite sau cập nhật: **625/625 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

## Rà soát mock tiếp theo — loại bỏ legacy engine không được DI — 09/09/2026

- Đã re-index graph sau các thay đổi để tránh kết luận từ chỉ mục cũ.
- Đã xoá `ConciergeEngine`, `MaintenanceEngine`, `ChannelEngine`, `ExecutiveEngine` và `DoorLockEngine` cũ. Đây là các implementation sinh DTO/metric cố định bằng `Task.FromResult`, `Guid.NewGuid()` hoặc số liệu mẫu; trace inbound cho thấy chỉ được gọi từ các unit test giả, không có caller production.
- Concierge và Maintenance đã có implementation `Ef*` đang được đăng ký trong DI production; các test giả tương ứng đã được loại bỏ, giữ lại test state machine/event thuần.
- Channel và Executive hiện chưa có adapter/repository dữ liệu thật trong DI production. Không giữ implementation giả để làm cho màn hình “có vẻ hoạt động”; các contract này cần được triển khai riêng khi có schema/provider OTA và query dashboard thật.
- Các provider `Unavailable*`, e-invoice chưa cấu hình provider thật, và pipeline print/PDF vẫn được giữ fail-closed hoặc không đăng ký; đây là trạng thái chưa hoàn thiện có chủ đích, không phải dữ liệu mẫu.

### Kiểm chứng

- Full unit suite sau đợt dọn legacy: **625/625 pass, 0 skipped**.
- Build qua khi chạy test, không có lỗi biên dịch; các warning hiện hữu của solution vẫn còn.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật — loại bỏ các service Group synthetic còn sót — 09/09/2026

- Đã loại bỏ `ASMILE.BO.Infrastructure/FrontOffice/Group/GroupEngine.cs`. File này chứa bốn implementation trả DTO cố định hoặc `Task.CompletedTask`: `RoomBlockService`, `RoomingListService`, `GroupBillingService` và `GroupDashboardService`.
- Graph xác nhận các lớp này chỉ còn được gọi bởi `GroupTests.cs`; không có caller production. DI production đã dùng `EfRoomBlockService`, `EfRoomingListService`, `EfGroupBillingService` và `EfGroupDashboardService`.
- Đã bỏ các unit test kiểm chứng dữ liệu synthetic. Các test dữ liệu thật vẫn nằm trong `GroupReservationWorkflowTests` và `GroupBillingRealDataTests`; test kiểm tra `GroupEvents` vẫn giữ lại.
- Unit suite sau thay đổi: **687/687 pass, 0 skipped**. Build có cảnh báo hiện hữu nhưng không có lỗi.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật — dọn mock class không có caller production — 09/09/2026

- Đã xoá 11 class `Mock*Service` khỏi `ASMILE.BO.Infrastructure` (backup, current user, database admin, general ledger, inventory movement, payable, printer, receivable, security settings, service status, system settings).
- Graph inbound và caller search không tìm thấy đường production nào tới các class này; DI production đang dùng implementation Windows/EF thật hoặc implementation fail-closed có tên `Unavailable*`.
- Không xoá `StubEInvoiceProvider` vì đó là provider test/reference chưa đăng ký production; production hiện vẫn dùng `UnavailableEInvoiceProvider` cho đến khi có adapter/credential nhà cung cấp thật.
- Unit suite sau khi dọn: **687/687 pass, 0 skipped**; không có lỗi compile/test.

## Cập nhật — tách provider test khỏi production — 09/09/2026

- `StubEInvoiceProvider` đã được chuyển khỏi `ASMILE.BO.Infrastructure` vào `EInvoiceServiceTests` dưới dạng test double nội bộ.
- Production assembly không còn class provider giả; DI chỉ đăng ký `UnavailableEInvoiceProvider` cho VIETTEL/VNPT/MISA để không phát hành hóa đơn giả khi chưa có endpoint và credential thật.
- Kiểm thử hồi quy sau thay đổi: **687/687 pass, 0 skipped**.

## Cập nhật — loại bỏ NoOpOutboxPublisher chết — 09/09/2026

- Đã xoá `NoOpOutboxPublisher`, interface `IOutboxPublisher` không có caller và registration DI tương ứng.
- Outbox/audit persistence thật vẫn do `EfFrontOfficePersistenceSession` quản lý; không còn API `PublishAsync` trả thành công rỗng trong production.
- Unit suite sau thay đổi: **687/687 pass, 0 skipped**.

## Rà soát mock Night Audit — 09/09/2026

- `NightAuditCenterWindow` trước đây khởi tạo sáu kiểm tra bằng số liệu tĩnh (Folio, chênh lệch thanh toán, doanh thu, khách, giao dịch chờ post và thời điểm đồng bộ). Các số liệu này không có nguồn EF và có thể làm người vận hành hiểu nhầm là dữ liệu thật.
- Đã loại bỏ toàn bộ số liệu mẫu. Folio đang mở đọc từ `IHotelFolioRepository`, khách quá ngày dự kiến trả được tính từ `IHotelStayRepository`, chênh lệch thanh toán đọc từ `IHotelPaymentRepository.GetSettlementSummaryAsync`, và doanh thu/occupancy/ADR đọc từ `INightAuditRepository.GetRevenueSnapshotAsync`.
- “Đối soát thu ngân chờ xử lý” đã dùng `ICashReconciliationService.GetPendingAsync` để đọc các ca Pending/VarianceDetected/Escalated từ EF. “Đồng bộ hệ thống” dùng `IInfrastructureHealthService.CheckAsync` để đọc health-check database/migration thật; cảnh báo hoặc lỗi hạ tầng vẫn chặn chạy.
- Snapshot doanh thu không còn là điều kiện chặn trước khi chạy: nếu chưa có snapshot của ngày hiện tại, giao diện ghi rõ “Sẽ tạo” và để bước `GenerateRevenueSnapshot` tạo snapshot bằng dữ liệu thật.

## Rà soát toàn bộ mock còn sót — 09/09/2026

Đã rà lại các điểm thường dễ bị nhầm là dữ liệu giả trong Hotel/Front Office:

- `GroupCheckoutWindow` đọc đoàn CheckedIn, stay, folio, housekeeping, minibar, payment và key-card từ các service/repository thật; xác nhận cuối gọi `CheckOutAndIssueInvoicesAsync`, không tạo dòng checkout giả.
- `GroupGuestManagementWindow` đọc đoàn/rooming list từ `IGroupReservationQueryService`; khi chọn khách, hồ sơ được enrich từ `IGuestProfileRepository`. Lưu hồ sơ dùng `UpdateAsync` và chỉ cho phép các trường có command thật.
- Housekeeping, Minibar và AR (customer/ledger/payment/invoice) đều có nhánh tải dữ liệu thật trước; khi thiếu service/context thì hiển thị empty/error state. Các generator mẫu còn lại nằm sau `return` hoặc `#if false`, không có đường gọi runtime.
- Các lớp `Mock*` trong Infrastructure đã kiểm tra inbound production call: không có caller runtime; không đăng ký trong production DI của Hotel/Front Office. Không xóa ngay để tránh ảnh hưởng test/legacy reference.
- Xác nhận hồi quy: `dotnet test ASMILE.BO.Tests.Unit/ASMILE.BO.Tests.Unit.csproj --no-restore -v:minimal` đạt 696/696, 0 failed, 0 skipped; build thành công với cảnh báo hiện hữu.

Kết luận: không còn phát hiện mock đang cấp dữ liệu cho luồng production Hotel/Front Office trong phạm vi audit này. Các khoảng trống còn lại là capability thật chưa có backend/provider (đặc biệt E-Invoice bên ngoài), không được thay bằng dữ liệu mẫu.
- Build `ASMILE.BO.App`: **PASS, 0 errors**. Cảnh báo còn lại là cảnh báo tồn tại của solution; không có lỗi biên dịch do thay đổi này.
- Chưa chạy migration, không ghi production database.

## Rà soát mock/fallback bổ sung — 09/09/2026

- Đã tái lập graph từ workspace hiện tại: `D-ASMILE-BO` có 53.540 node và 161.071 edge. Các lớp `Mock*Service` trong Infrastructure không có inbound production reference và không xuất hiện trong đăng ký production DI; các contract công nợ, phải trả, tồn kho và sổ cái đang dùng `Ef*`.
- Các generator `Random` còn lại trong Room Master, AR, Minibar, Lost & Found, Housekeeping và các cửa sổ Folio đều nằm sau `return`, `#if false` hoặc nhánh fail-closed. Không có đường runtime hiện tại dùng chúng để hiển thị dữ liệu nghiệp vụ.
- `ISecuritySettingsService` đã được chuyển từ `SafeDefaultSecuritySettingsService` sang `EfSecuritySettingsService`, đọc key `Security.RequireAdminConfirmation` từ Settings Engine/EF; khi thiếu hoặc sai giá trị vẫn giữ mặc định an toàn `true`.
- E-Invoice vẫn là gap tích hợp có chủ đích: production dùng `UnavailableEInvoiceProvider` cho Viettel/VNPT/MISA cho tới khi có endpoint, credential và adapter được phê duyệt. Không sinh mã provider hoặc trạng thái phát hành giả.

### Kiểm chứng sau rà soát

- Unit suite sau thay đổi: **694/694 pass, 0 skipped**.
- Test riêng `EfSecuritySettingsService`: **2/2 pass**.
- E2E: **4/4 pass**; Integration LocalDB gần nhất: **22/22 pass**.
- Không bật restart service, không áp migration và không ghi production database.

## Hoàn thiện quyền máy in Windows — 09/09/2026

- `WindowsPrinterSettingsService.GetPrinterPermissionsAsync` hiện đọc ACL thật của từng printer qua `OpenPrinter`/`GetPrinter(PRINTER_INFO_3)`, phân tích DACL/ACE và trả principal, Allow/Deny, inherited flag cùng các quyền Print/ManagePrinter/ChangePermissions.
- Khi tài khoản không đủ quyền đọc ACL, kết quả giữ tên máy in và mã lỗi Win32 (`ReadStatus=Win32:*`), không biến việc không đọc được thành danh sách quyền rỗng hoặc quyền giả.
- DTO `PrinterPermissionDto` đã bổ sung `Principal`, `Rights`, `AccessType`, `IsInherited`, `ReadStatus`; đường non-Windows vẫn trả rỗng có chủ đích.

### Kiểm chứng

- Test riêng Windows printer ACL: **4/4 pass**.
- Full Unit sau thay đổi: **696/696 pass, 0 skipped**.
- Build Infrastructure: **0 lỗi**; chỉ còn warning tồn tại trước đó của solution.

## Cập nhật triển khai — Rate Engine dùng occupancy thật — 09/09/2026

- `RateEngineViewModel` không còn hard-code `PropertyId` hoặc số liệu `12 phòng / 10 đã bán`.
- ViewModel hiện yêu cầu Front Office property context, đọc rate plan theo property thật, lấy danh sách phòng active và Stay đang ở thật để tính `TotalRooms`/`RoomsSold`; rate plan theo loại phòng được lọc đúng `RoomTypeId`.
- Đã đăng ký `RateEngineViewModel` trong presentation DI để màn hình có thể resolve đúng các repository thật.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **0 lỗi**.
- Rate Engine unit tests: **15/15 pass**.
- Không phát sinh migration và không ghi production database.

## Cập nhật triển khai tiếp — Settings, backup và máy in — 09/09/2026

- `SystemSettings` đã đọc từ bảng `AppSettings` thật qua `EfSystemSettingsService`; không còn dùng `UnavailableSystemSettingsService` trong production DI.
- Settings Engine (`SettingDefinitions`, `SettingValues`, `AppModules`, `SettingChangeLogs`) đã chuyển sang EF repository thật. Bổ sung migration hẹp `20260909100000_AddSettingsEnginePersistence`; chưa áp dụng vào production.
- Backup đã dùng `SqlServerBackupService`, tạo `COPY_ONLY + CHECKSUM` và xác thực bằng `RESTORE VERIFYONLY`; restore vẫn bị khóa mặc định.
- Danh sách máy in đã đọc trực tiếp từ Windows Print Spooler qua `WindowsPrinterSettingsService`, bao gồm máy mặc định và trạng thái Online/Offline/Error/Busy. Không dựng quyền ACL vì contract hiện tại chưa mô hình hóa quyền Windows.
- `ServiceStatus` vẫn fail-closed: chưa liệt kê/restart Windows Service thật để tránh thao tác đặc quyền hoặc restart nhầm dịch vụ. E-Invoice vẫn fail-closed cho tới khi có adapter/credential nhà cung cấp thật.

### Kiểm chứng sau cập nhật

- Unit: **689/689 pass**.
- Integration LocalDB: **22/22 pass**.
- E2E: **4/4 pass**.
- Test máy in Spooler thật: **2/2 pass**.
- Chưa chạy migration, chưa ghi production database, chưa thay connection string.

## Bổ sung kiểm tra — Split Folio không còn hardcode currency/dữ liệu thanh toán — 09/09/2026

- `SplitFolioWindow` tiếp tục lấy Folio và ledger qua `IHotelFolioRepository`/`HotelFolioApplicationService`; các helper `Random` còn lại nằm trong `#if false` và không được biên dịch.
- Mapping dữ liệu thật đã bỏ currency `VND`, payer và billing instruction dựng sẵn: currency lấy từ property context, payer/billing hiển thị `—` khi `FolioLedgerDto` chưa cung cấp nguồn tương ứng; trạng thái settlement suy ra từ trạng thái Folio thật.
- Khi database không có Folio thật, wizard giữ danh sách rỗng và fail-closed; không tự chèn dữ liệu mẫu để làm sai kết quả kiểm thử.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 746 warning hiện hữu của solution.

## Điều chỉnh kết luận mới nhất — ShiftLog đã có persistence riêng — 09/09/2026

Các ghi chú lịch sử trước đó nói `ShiftLogWindow` chưa có schema/command riêng hoặc đang tạo Incident đã được supersede. Trạng thái hiện tại là:

- `Tạo mới` mở `CreateShiftLogWindow` và ghi vào `fo.NhatKyCa` qua `IShiftLogEntryCommandService`.
- Danh sách hợp nhất vẫn giữ CashShift và Incident để xem vận hành, nhưng không trộn chúng thành cùng một loại dữ liệu khi ghi.
- Migration `20260908204823_AddFrontOfficeShiftLogEntries` mới chỉ được sinh; cần review/preflight và apply riêng trên database triển khai sau khi chốt release.

## Cập nhật — Tách nhật ký ca khỏi Incident và CashShift — 09/09/2026

- `ShiftLogWindow` hiện đọc ba nguồn dữ liệu thật theo property: `CashShift` cho mở/đóng ca, `FrontOfficeIncident` cho sự cố và `FrontOfficeShiftLogEntry` cho ghi chú/bàn giao vận hành.
- Bổ sung entity `FrontOfficeShiftLogEntry`, query/command contract, EF service, DI registration và bảng/migration `fo.NhatKyCa`. Tạo mới không còn ghi nhầm một Incident.
- Bổ sung modal `CreateShiftLogWindow`; nội dung tạo mới được commit qua `IShiftLogEntryCommandService`, sau đó tải lại từ database. Có chuyển trạng thái và audit cho create/status change.
- Migration được sinh tại `20260908204823_AddFrontOfficeShiftLogEntries`; chỉ mới tạo mã migration, chưa apply lên production.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite: **681/681 pass, 0 skipped**; test mới xác nhận create/query/status/audit bằng SQLite thật.
- Integration suite: **20/20 pass, 0 skipped**.
- Graph `D-ASMILE-BO` đã reindex sau thay đổi: **62.054 nodes, 174.625 edges**.

## Cập nhật — Nhật ký ca dùng dữ liệu CashShift và Incident thật — 09/09/2026

- `ShiftLogWindow` không còn chỉ đọc `CashShift` rồi chặn nút `Tạo mới`. Màn hình hiện tải đồng thời ca tiền mặt qua `ICashShiftService` và sự cố vận hành qua `IIncidentQueryService`, cùng giới hạn property và khoảng 31 ngày.
- Nút `Tạo mới` mở `CreateIncidentWindow` và ghi qua `IIncidentCommandService.CreateAsync` vào `fo.FrontOfficeIncidents`; sau khi commit thành công màn hình tải lại từ database. Không tạo bảng nhật ký giả và không biến một incident thành CashShift.
- Bản ghi Incident được đưa vào cùng danh sách để lọc/xem chi tiết; trạng thái, ưu tiên, phòng, khách, reservation, người báo và người phụ trách lấy từ DTO thật. Bộ lọc đã nhận thêm trạng thái/danh mục của Incident.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 744 warning hiện hữu của solution.
- Không chạy migration và không ghi production database.

## Cập nhật kiểm tra sâu — dữ liệu thật trên Khách đến và phân loại mock — 09/09/2026

- `TodayArrivalsWindow` nay đọc thêm `IHousekeepingBoardService.GetRoomSnapshotAsync` theo property để hiển thị trạng thái vận hành và trạng thái housekeeping của phòng được gán. Nếu truy vấn snapshot lỗi, danh sách Stay thật vẫn được giữ nguyên và các trường trạng thái hiển thị rõ là chưa có dữ liệu; không thay bằng mẫu.
- Cùng màn hình này đã đọc `GuestProfile` và `Folio` thật theo `GuestId`/`StayId`. Điện thoại, email, quốc tịch, công ty, VIP, ghi chú, số Folio, trạng thái Folio và số dư không còn dựng trong UI.
- Rà soát đường chạy production DI xác nhận các seam Front Office chính đang dùng `Ef*`: Stay, Reservation, Room, Housekeeping, Folio, Payment, Guest, Group, Group Checkout, Concierge, Key Card, Marketing, Night Audit và Reporting. Các `Mock*`/Training/Seed còn lại không được đăng ký làm implementation production của các contract này.
- Các điểm còn `Unavailable` cần tách thành backlog tích hợp hoặc command, không xem là mock dữ liệu: provider e-invoice ngoài, SMTP/printer/backup/system settings, tạo Shift Log mới, một số tiện ích in/gửi email và các trường nghiệp vụ chưa có contract. Màn hình phải fail-closed và báo thiếu API thay vì hiển thị số liệu giả.

### Kiểm chứng sau vòng rà soát

- Build `ASMILE.BO.App`: **PASS, 0 warning, 0 error**.
- Full unit suite: **679/679 pass, 0 skipped**.
- Knowledge graph đã re-index sau thay đổi: **53.157 nodes / 159.978 edges**.
- Các migration mới nhất đã được áp dụng và kiểm tra trên **ASMILE_BO_DesignTime LocalDB**; chưa apply migration và chưa ghi production database.

## Cập nhật — Phòng khóa dùng dữ liệu thật và gia hạn khóa — 09/09/2026

- Bổ sung trên `HotelRoom` các trường thời điểm đổi trạng thái, thời hạn khóa và lý do khóa; mở khóa sẽ xóa metadata khóa cũ thay vì giữ dữ liệu mồ côi.
- Thêm `ExtendHotelRoomLockCommand`/handler, kiểm tra property scope, row-version, phòng còn ở `OutOfOrder`/`OutOfService`, thời hạn mới phải lớn hơn thời hạn hiện tại và bắt buộc có lý do; thao tác ghi audit với action `OperationalLockExtended`.
- `ReceptionUiService` và `LockedRoomWindow` đọc thời hạn/lý do thật từ EF; nút `Gia hạn` mở dialog nhập thời hạn và lý do rồi commit qua command thật, sau đó tải lại từ database.
- Tạo migration `20260908192247_AddHotelRoomOperationalLockDetails`, gồm 3 cột nullable và index tra cứu trạng thái hết hạn. Đã apply và kiểm tra trên `ASMILE_BO_DesignTime` LocalDB; chưa apply production.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- Full unit suite: **678/678 pass, 0 skipped**, gồm test domain khóa/gia hạn/mở khóa và test đọc metadata qua repository EF mới.
- Không chạy `database update`, không backup/ghi production database và không thay connection string.

## Cập nhật — Lost & Found lưu nghiệp vụ liên hệ khách — 09/09/2026

- Đã bổ sung trạng thái domain `Contacted` và các trường persistence `ContactDate`, `ContactedBy`, `ContactMethod`, `ContactResult`, `ContactNotes` trên tài sản Lost & Found.
- Nút “Liên hệ khách” giờ gọi `ContactLostFoundItemCommand`/`EfLostFoundCommandService`, ghi database và history; sau khi tải lại vẫn giữ đúng trạng thái/kết quả liên hệ.
- Bổ sung migration `20260908190232_AddLostFoundContactPersistence`; chưa apply vào production database.
- Query service và ViewModel đã map dữ liệu liên hệ thật; không còn cập nhật collection memory trên đường chạy production.

### Kiểm chứng

- Build App: **PASS, 0 errors**.
- Unit suite: **676/676 pass, 0 skipped**.
- Test round-trip đã kiểm tra đăng ký → cập nhật → nhập kho → liên hệ → query lại → trả khách trên SQLite in-memory.

## Cập nhật — Bellman giữ liên kết GuestProfile thật — 09/09/2026

- `TodayArrivalsWindow` đã kiểm tra lại: thao tác Bellman gọi `IGuestMessageService.CreateAndSendAsync` và ghi yêu cầu thật, không phải thông báo giả.
- Bổ sung `GuestId` từ `HotelStayDto` vào `ArrivalRow`; khi tạo yêu cầu Bellman, màn hình truyền `GuestId` thật cùng `StayId`, phòng và khách. Điều này giữ được liên kết hồ sơ khách trong Message Center.
- E-invoice provider production hiện được đăng ký bằng `UnavailableEInvoiceProvider` cho đến khi có adapter/credential thật; `StubEInvoiceProvider` chỉ còn dùng trong unit test, không được đăng ký production và không thể tạo số hóa đơn giả khi chạy ứng dụng.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- Đã re-index graph sau thay đổi: **53,002 nodes / 159,480 edges**, trạng thái indexed, persistence bật.
- Không migration và không ghi production database.

## Cập nhật — Walk-In tra cứu GuestProfile thật và phân loại placeholder — 09/09/2026

- `WalkInWindow` đã bỏ placeholder ở nút `Tìm khách`: mở `AddStayOccupantWindow`, tìm hồ sơ qua `IGuestProfileRepository.SearchAsync`, đọc lại `GuestProfile` thật theo property và nạp tên, điện thoại, giấy tờ vào form.
- Khi tạo Walk-In, `ReservationUiService.CreateAsync` nhận `GuestProfileId` đã chọn thay vì luôn truyền `null`. Nếu service/property context/hồ sơ không sẵn sàng, màn hình báo lỗi và không tạo dữ liệu giả.
- `TransferPostingWindow`, `SplitFolioWindow` và `MergeFolioWindow` đã kiểm tra lại: các nút xác nhận gọi lần lượt `HotelFolioApplicationService.TransferAsync`, `SplitAsync`, `MergeAsync`, kèm Folio id và row-version thật. Không còn đường ghi mock ở ba workflow này.
- Các thông báo `ShowUnavailable` còn lại không phải dữ liệu mock: chúng là các điểm fail-closed cho năng lực chưa có backend/provider (ví dụ upload scan giấy tờ, tạo nhật ký ca, print/export/adjust/cancel receipt legacy, Bellman và provider hóa đơn). `ReceiptVoucherWindow` hiện không có caller trong graph; đường thanh toán đang dùng `PaymentReceiptsView` thật.
- `ModulePlaceholderView` và `GroupCheckoutWindow.BuildStep3Detail` không có caller runtime; được phân loại là mã chết/backlog dọn dẹp, không phải nguồn dữ liệu đang hiển thị.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- Unit suite gần nhất trước thay đổi UI: **674/674 pass, 0 skipped**; thay đổi Walk-In chỉ nối picker và truyền `GuestProfileId`, chưa thay đổi schema/domain command.
- `Transfer/Split/Merge` đã được kiểm tra qua source graph và không tạo dữ liệu khi thiếu id/row-version/context.
- Không chạy migration và không ghi production database.

## Cập nhật — Group Checkout đối chiếu payment allocation thật — 09/09/2026

- `GroupCheckoutWindow` đã đọc payment theo từng Folio phòng qua `IHotelPaymentRepository.GetForFolioAsync`, không dùng số liệu dựng sẵn.
- Màn hình đối chiếu riêng payment đã hoàn tất, loại trừ payment chưa hoàn tất/void/cancel, trừ refund và hiển thị: số payment thật, số tiền nhận ròng, số đã phân bổ và số chưa phân bổ.
- Nếu còn payment chưa phân bổ, giao diện chỉ cảnh báo và hướng xử lý tại Cashier; không tự tạo payment, không tự phân bổ vào khoản mục và không làm thay đổi điều kiện check-out hiện tại.
- Payment allocation vẫn dùng command thật `HotelPaymentApplicationService.AllocateAsync` tại Cashier, có kiểm tra quyền, Folio/payment row-version và giới hạn `UnallocatedAmount`.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; chỉ còn warning tồn tại sẵn trong solution và một số helper/field cũ chưa dùng của Group Checkout.
- Unit suite sau cập nhật: **674/674 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật — Proforma Folio đọc payment detail thật — 09/09/2026

- `ProformaFolioWindow` đã dùng `IHotelPaymentRepository.GetForFolioAsync` để nạp payment theo từng Folio, hiển thị payment number, phương thức, trạng thái, currency và số tiền từ bản ghi thật.
- Đã loại bỏ mã receipt, phương thức, trạng thái và operator tự ghép từ sequence ledger. Khi payment detail không tải được, màn hình hiển thị cảnh báo “Chưa đọc được” thay vì số 0 hoặc dữ liệu thay thế.
- Currency posting/deposit/refund lấy từ currency của property context; mã deposit/refund chỉ còn là tham chiếu ledger, không giả lập số chứng từ.
- Số dư Proforma tính theo charge - payment - deposit + refund từ ledger thật.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật — Group Checkout phát hành hóa đơn theo Folio thật — 09/09/2026

- Bổ sung `IGroupCheckOutCommandService.CheckOutAndIssueInvoicesAsync` và nối `GroupCheckoutWindow` vào command này.
- Khi xác nhận check-out, hệ thống đọc ledger thật của từng Folio, phát hành một `HotelInvoice` cho Folio chưa có hóa đơn và commit cùng transaction với trạng thái Stay/Rooming List/Group. Folio đã có hóa đơn được giữ nguyên, không phát hành trùng.
- Hóa đơn lưu subtotal theo đầy đủ loại charge/discount/adjustment, thuế, payment/deposit/refund và số dư còn lại; payload giữ chi tiết các dòng ledger để đối chiếu.
- Không tự sinh payment và không tự phân bổ payment. Payment allocation vẫn phải đi qua giao dịch payment thật của từng Folio; không còn thông báo UI gây hiểu nhầm rằng checkout sẽ tạo allocation.
- `EfGroupBillingService` đã sửa cách tính signed ledger: chỉ `Payment` được tính là payment, chỉ `Deposit` là deposit, refund được trừ khỏi payment ròng; discount/manual credit không bị tính nhầm thành payment hoặc trừ deposit hai lần.

### Kiểm chứng sau cập nhật

- Build solution: **PASS, 0 errors**; chỉ còn warning hiện hữu.
- Billing regression: **4/4 pass**.
- Full unit: **674/674 pass, 0 skipped**.
- Integration EF: **20/20 pass, 0 skipped**.
- E2E: **4/4 pass, 0 skipped**.
- Đã re-index codebase sau thay đổi: **52.973 nodes / 163.356 edges**.
- Chưa chạy migration, chưa ghi production database; toàn bộ kiểm chứng dùng build/test path hiện có.

## Cập nhật — Group Checkout tạo bàn giao Housekeeping thật — 09/09/2026

- `EfGroupCheckOutCommandService` nay kiểm tra task Housekeeping đang hoạt động theo từng phòng trả và tạo `DepartureClean` task thật cho phòng chưa có task.
- Task được thêm vào cùng transaction với cập nhật Stay, Rooming List, Group và hóa đơn; nếu commit thất bại thì toàn bộ thay đổi được rollback.
- Không tạo task trùng khi phòng đã có task chưa hoàn tất; số task mới được trả về trong `GroupCheckOutResult` và ghi audit.
- Minibar vẫn không bị tự post: nếu có consumption, nhân viên phải kiểm tra/post bằng command Minibar thật trước hoặc sau theo quy trình được cấu hình.

## Cập nhật kiểm tra ledger thật — Folio Workspace — 09/09/2026

- `FolioWorkspaceWindow` không còn dựng lại tổng tiền từ danh sách charge không đầy đủ. Số tổng hiển thị được suy ra từ `FolioLedgerDto.RunningBalance` cùng payment/deposit signed ledger, nên không bỏ sót Laundry, Minibar, Spa, Telephone, OtherCharge, Package hoặc OpeningBalance và vẫn phản ánh refund.
- Payment, deposit và refund trên các bảng chi tiết được chuẩn hóa về số tiền dương để phù hợp với cách hiển thị nghiệp vụ; ledger gốc vẫn giữ nguyên dấu (+ debit, - credit).
- Bộ phân loại transaction đã nhận diện thêm các loại charge thật thay vì đẩy chúng vào dữ liệu mẫu/không xác định. Các fixture lịch sử của Folio Workspace vẫn nằm trong `#if false`, không được gọi trên đường chạy production.
- Settlement/invoice trước đó cũng đã dùng đầy đủ ledger thật: invoice bao phủ các loại charge, refund giảm paid và settlement không mutate Folio nếu còn outstanding mà chưa có manager override.

### Kiểm chứng sau cập nhật

- Build solution: **PASS, 0 errors**.
- Unit: **673/673 pass, 0 skipped**.
- Integration: **20/20 pass, 0 skipped**.
- E2E: **4/4 pass, 0 skipped**.
- Không chạy migration và không ghi production database.
- Gap còn lại không phải dữ liệu mock của Folio Workspace: composite settlement/payment allocation cho Group Checkout và liên kết chứng từ/tài liệu vẫn cần contract nghiệp vụ riêng; hiện UI tiếp tục fail-closed thay vì tự tạo giao dịch giả.

## Cập nhật đối chiếu Group Billing — signed ledger — 09/09/2026

- `EfGroupBillingService` đã phân biệt đúng `Payment`, `Deposit` và các credit adjustment. Discount/manual credit không còn bị tính thành payment; deposit không còn bị trừ hai lần trong `Outstanding`.
- Billing summary hiện tính charge net từ các entry không phải Payment/Deposit/ClosingBalance, payment chỉ từ entry `Payment`, deposit chỉ từ entry `Deposit`, và vẫn giữ property/group scope.
- Đã thêm hồi quy dữ liệu thật trong `GroupBillingRealDataTests` cho chuỗi charge + discount + payment + deposit.

### Kiểm chứng

- Nhóm billing: **4/4 pass**.
- Full unit: **674/674 pass, 0 skipped**.
- Chưa chạy migration và chưa ghi production database.

## Cập nhật xác nhận hiện trạng — Marketing và Group Checkout — 08/09/2026

- `MarketingCenterWindow` hiện đã có đường dữ liệu thật: `IMarketingQueryService.SearchAsync` đọc theo property/section; `IMarketingCommandService` thực hiện Create/Update/ChangeStatus với row-version và reload lại từ persistence. Các nhận định cũ trong các đoạn lịch sử của tài liệu về “28 bản ghi trong memory” không còn đại diện cho source hiện tại.
- `GroupCheckoutWindow` hiện đọc Stay, Folio, Minibar, Housekeeping snapshot và key-card thật theo từng phòng. Các phần composite invoice/payment/service settlement của cả đoàn vẫn chưa được tự động hóa; UI không giả vờ đã hoàn tất và checkout chỉ đi qua command assignment thật khi điều kiện dữ liệu tối thiểu đạt.

### Kiểm chứng hiện trạng

- Build solution: **PASS, 0 errors**.
- Unit: **673/673 pass, 0 skipped**.
- Integration: **20/20 pass, 0 skipped**.
- E2E: **4/4 pass, 0 skipped**.

## Cập nhật triển khai — Group Checkout đọc Minibar/HSK/thẻ khóa thật — 08/09/2026

- `GroupCheckoutWindow` đã nối `IHousekeepingBoardService.GetRoomSnapshotAsync` để hiển thị Housekeeping status theo `HotelRoomId`.
- Đã nối `IMinibarQueryService.QueryAsync` để hiển thị snapshot Minibar và trạng thái đã/chưa post. Không có snapshot hoặc service thì hiển thị rõ trạng thái thiếu dữ liệu.
- Đã nối `IKeyCardService.GetKeysForStayAsync` để đếm thẻ đang hiệu lực theo Stay; bỏ hoàn toàn giá trị kỳ vọng cố định kiểu `2/2`.
- Các query vận hành được phép thiếu dữ liệu mà không làm mất danh sách phòng; trạng thái lỗi/thiếu service được hiển thị fail-closed để nhân viên không hiểu nhầm là đã kiểm tra xong.

### Kiểm chứng

- Build solution: **PASS, 0 errors**.
- Unit suite: **673/673 pass, 0 skipped**.
- Integration suite: **20/20 pass, 0 skipped**.
- E2E suite: **4/4 pass, 0 skipped**.
- Đã re-index graph `D-ASMILE-BO`: **52.961 nodes / 159.322 edges**.
- Không chạy migration và không ghi production database.

## Cập nhật triển khai — Group Checkout đồng bộ Stay/Folio thật — 09/09/2026

- `EfGroupCheckOutCommandService` không còn chỉ đánh dấu `GroupRoomAssignment`. Command hiện bắt buộc từng assignment đã check-in phải liên kết tới `HotelStay` và Folio thật trong đúng property.
- Khi xác nhận, command cập nhật `HotelStay.MarkCheckedOut`, `GroupRoomAssignment.MarkCheckedOut`, đóng nhóm khi không còn phòng đang ở, ghi `HotelStayHistory` và stage domain event/audit trong cùng lần commit.
- `HasOutstandingBalance` của Stay được tính từ tổng bút toán Folio thực tế; nếu thiếu Stay/Folio hoặc Stay không cho phép chuyển trạng thái, toàn bộ thao tác bị từ chối trước khi đổi assignment.
- Không tạo invoice, payment allocation, minibar settlement hoặc Folio settlement giả. Các nghiệp vụ đó vẫn phải xử lý qua contract tương ứng; checkout đoàn hiện đã nhất quán ở lớp Stay/assignment và fail-closed ở phần chưa có composite command.

### Kiểm chứng bổ sung

- Build solution: **PASS, 0 errors**.
- Unit suite: **673/673 pass, 0 skipped**.
- Integration suite: **20/20 pass, 0 skipped**.
- E2E suite: **4/4 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Cập nhật triển khai — Group Checkout đọc Folio thật theo từng phòng — 08/09/2026

- `GroupCheckoutWindow` không còn gán số dư phòng bằng `0`. Màn hình lấy Stay đang ở thật theo `RoomingListItemDto.ReservationId`, sau đó đọc Folio thật bằng `IHotelFolioRepository.GetByStayAsync`.
- Số dư hiển thị lấy từ `HotelFolioDto.OutstandingBalance`; có Folio number và liên kết mở `FolioWorkspaceWindow` cho từng phòng để thực hiện thanh toán/phân bổ theo Folio thật.
- Phòng đã check-in nhưng thiếu Stay/Folio bị đánh dấu `Thiếu Folio` và không thể chọn để check-out. Không còn hiển thị thẻ khóa `2/2`, Minibar hoặc HSK bằng giá trị dựng sẵn; các phần chưa có API hiển thị rõ `Chưa có dữ liệu`.
- Check-out đoàn vẫn ghi qua `IGroupCheckOutCommandService` thật. Việc thanh toán/phân bổ theo từng Folio được thực hiện trong workspace Folio; chưa tạo composite command gộp đoàn để tránh ghi thiếu ledger.

## Cập nhật sửa lệch dữ liệu — Folio Workspace — 09/09/2026

- `FolioWorkspaceWindow.MapPersistedFolio` đã chuẩn hóa payment/deposit từ signed ledger âm sang số tiền đã thu/đặt cọc dương trước khi tính Balance.
- Thanh tổng không còn hiển thị `FOL-26001` hoặc nhãn Master dựng sẵn; luôn dùng mã Folio thật của ledger đang chọn.
- Thanh tổng giao dịch cũng chuẩn hóa dấu Payment/Deposit và tính Balance theo `charge + tax + fee + refund + discount - payment - deposit`.
- Build solution: **PASS, 0 errors**; Unit: **673/673 pass**; E2E: **4/4 pass**.

## Cập nhật settlement/invoice — dùng đủ ledger thật — 09/09/2026

- `SettlementApplicationService.GenerateInvoiceAsync` hiện tính subtotal từ toàn bộ charge hợp lệ trong Folio (phòng, F&B, laundry, minibar, spa, telephone, adjustment, discount, transfer và manual entries), không chỉ `RoomCharge`.
- Payment/deposit được tính là khoản đã thu dương; refund được trừ khỏi `PaidAmount`; tax vẫn tách riêng và outstanding lấy từ running balance thật.
- `SettleAsync` tính số dư dự kiến trước khi mutate aggregate. Nếu còn dư và không có manager override, command trả lỗi mà không để lại thay đổi tracked trong Folio.
- Kiểm chứng: Build **0 lỗi**, Unit **673/673**, Integration **20/20**, E2E **4/4**; không migration/production write.

### Kiểm chứng

- Build solution: **PASS, 0 errors**.
- Unit suite: **673/673 pass, 0 skipped**.
- Integration suite: **20/20 pass, 0 skipped**.
- E2E suite: **4/4 pass, 0 skipped**.
- Đã re-index graph `D-ASMILE-BO`: **52.951 nodes / 163.465 edges**.
- Không chạy migration và không ghi production database.

## Cập nhật tiếp — Chuẩn hóa số dư Folio theo ledger có dấu — 08/09/2026

- Re-index lại codebase `D:\ASMILE-BO`: **52.943 node / 163.557 quan hệ**; audit payment allocation xác nhận `CashierPaymentWindow` đang ghi `ReceiveAsync` và phân bổ thật qua `AllocateAsync`, không phải dữ liệu mock.
- Sửa `EfHotelPaymentRepository.GetOutstandingAsync` và `GetSettlementSummaryAsync`: payment/deposit được lưu là credit âm nên số dư phải tính theo tổng ledger có dấu, không cộng `Abs(payment)` vào công nợ.
- Charge bucket hiện bao phủ mọi entry thật ngoài Payment/Deposit, gồm RoomCharge, Restaurant/F&B, Laundry, MiniBar, Spa, Tax, ServiceCharge, ManualCharge, Refund và các điều chỉnh; entry đã đảo được loại khỏi tổng.
- Đây là sửa sai lệch số liệu thật, ảnh hưởng trực tiếp đến Thu ngân, Folio, Check-out và báo cáo settlement; không thay đổi schema, không chạy migration, không ghi production database.

### Kiểm chứng sau sửa

- Solution build: **PASS, 0 errors** (833 warnings hiện hữu).
- Unit suite: **673/673 pass, 0 skipped**.
- Integration suite: **20/20 pass, 0 skipped**.
- Các gap còn lại được phân loại là contract thật chưa có, không phải mock đang chạy: provider hóa đơn điện tử ngoài hệ thống; composite settlement cho Group Check-out; export/print/email; Housekeeping photo/batch/link; Room Master utilities. Các màn hình tương ứng tiếp tục fail-closed.

## Cập nhật — Checkout Preparation dùng bằng chứng thật — 08/09/2026

- `CheckoutPreparationWindow` tiếp tục đọc Stay, Folio và ledger thật; currency lấy từ property context, số phát sinh/thanh toán/số dư không còn cố định VND.
- Bước bàn giao đã bỏ checklist chỉ lưu trong bộ nhớ. Task `DepartureClean` được tạo qua `IHousekeepingWorkflowService`; key-card được đọc theo Stay và thu hồi qua `IKeyCardService.ReturnKeyAsync`.
- Bước Hóa đơn & Thanh toán đã bỏ các ô nhập/ComboBox không có command lưu. Màn hình chỉ hiển thị bằng chứng Folio thật và mở `FolioWorkspaceWindow` để xử lý giao dịch thật.
- Điều kiện xác nhận Check-out hiện yêu cầu Folio cân bằng, dịch vụ đã chốt, task hậu kiểm thật đã tạo và không còn key-card đang hoạt động; thiếu service thì fail-closed.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 744 warning hiện hữu của solution.
- Unit suite: **673/673 pass, 0 skipped**.
- E2E suite: **4/4 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

### Cập nhật Quick Check-out trong cùng vòng

- Các dấu kiểm cố định ở bước tiền kiểm đã được thay bằng số khoản mục/ledger, số dư Folio và trạng thái Stay thật.
- Bước thanh toán/hóa đơn không còn hiển thị ô hoặc phương thức giả; giao diện chỉ điều hướng người dùng sang Folio/Chuẩn bị Check-out có workflow thật.

Kiểm chứng lại sau thay đổi Quick Check-out: Build **0 errors**, E2E **4/4 pass**.

### Sửa lệch mapping báo cáo hóa đơn

- `InvoiceManagementWindow.MapInvoice` đã được chỉnh đúng thứ tự `RoomCharge → FoodBeverage → Laundry → MiniBar → Spa → Transport → Other → Discount → Tax → ServiceCharge → GrandTotal`.
- Trước đó `roomCharge` bị truyền lặp và làm lệch toàn bộ các cột doanh thu sau đó; lỗi này đã được phát hiện bằng kiểm tra constructor `IvInvoice` và sửa trước khi chốt build.

Kiểm chứng: Build **0 errors**, Unit **673/673 pass**.

## Cập nhật triển khai — Express Checkout ghi nhận thanh toán thật — 08/09/2026

- `ExpressCheckoutWindow` đã nối `HotelPaymentApplicationService.ReceiveAsync` với `ReceivePaymentCommand`, dùng `HotelFolioId` và `RowVersion` của Folio thật; người dùng chọn phương thức, số tiền và tham chiếu ngay tại bước `Folio & TT`.
- Giao dịch được ghi qua Payment Engine và bút toán Payment của Folio trong cùng workflow hiện có; sau khi commit, màn hình đọc lại Stay/Folio/Ledger từ database để cập nhật số dư, row-version và trạng thái “Có thanh toán thật”. Không sinh dữ liệu mẫu.
- Bước xác nhận cuối đã nối `CheckOutHotelStayCommandHandler`; chỉ khi Stay có approval hợp lệ, Folio cân bằng và row-version hợp lệ mới thực hiện check-out thật, ghi lịch sử/audit qua Stay service rồi đóng cửa sổ.
- Bước `DV & hậu kiểm` có thể tạo task `DepartureClean` thật qua `IHousekeepingWorkflowService`, lưu task id trong phiên và dùng nó trong readiness; không còn đánh dấu kế hoạch hậu kiểm bằng boolean tĩnh.
- Bước `HĐ & bàn giao` đọc key-card thật theo Stay qua `IKeyCardService`, hiển thị số thẻ đang hoạt động; sau checkout gọi `AutoRevokeOnCheckoutAsync`. Nút tạo nháp hóa đơn dùng `IEInvoiceService.CreateDraftAsync` với các khoản mục Folio thật. Phát hành điện tử vẫn không giả lập vì provider production đang `Unavailable` cho tới khi cấu hình adapter/credential thật.
- Bổ sung readiness thực tế cho “Có PT thanh toán”, “Phạm vi ủy quyền hợp lệ” và “Hạn mức đủ”; các bước hậu kiểm, hóa đơn và bàn giao vẫn fail-closed vì chưa có contract/repository thật.
- Không cần migration mới cho thay đổi UI này; không chạy migration và không ghi production database.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- Unit suite sau khi nối check-out: **673/673 pass, 0 skipped**.
- Integration: **12/20 pass, 8 fail** do test fixture LocalDB không dọn được bảng nền `fo.PhongKhachSan`, `fo.LoaiPhongKhachSan`, `fo.DatPhong` vì còn FK; lỗi xảy ra ở bước setup/cleanup trước các kịch bản, không liên quan compile Express.
- E2E chạy riêng lại đạt **4/4 pass**; lần chạy solution trước đó có 1 lỗi trong tổng 4 do test database timing/teardown, nên vẫn cần ổn định harness trước khi dùng làm gate tuyệt đối.

## Cập nhật triển khai — Email outbox cho Accounting, Aging và Group Reservation — 08/09/2026

- Bổ sung `EmailOutboxMessage`, `IEmailDispatchService` và migration `20260908142116_AddEmailOutboxPersistence`. Các yêu cầu email được lưu bền vững với trạng thái, số lần thử, lỗi cuối, nguồn nghiệp vụ và thời điểm gửi; không đánh dấu “đã gửi” khi SMTP chưa cấu hình.
- Nút gửi hóa đơn ở `InvoiceManagementWindow`, gửi sao kê tại AR Invoice/Company/Customer/Ledger, gửi trong Aging và gửi xác nhận đoàn ở Group Reservation đều chuyển sang queue thật. Các mã nghiệp vụ dạng chuỗi không bị ép sai sang `Guid`; `SourceId` để trống khi chưa có khóa liên kết tương ứng.
- Hosted worker `EmailDispatchHostedService` chỉ chạy khi `Email:Enabled=true`, đọc các bản ghi Pending/Failed đến hạn và gửi qua SMTP. Mặc định vẫn tắt để tránh gửi nhầm; cần cấu hình SMTP ngoài mã nguồn trước khi bật.
- Các nút email còn thiếu người nhận thật (ví dụ receipt AR Payment, dịch vụ đoàn) vẫn giữ fail-closed và được ghi nhận là backlog, không tạo nội dung hoặc lịch sử gửi giả.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite: **673/673 pass, 0 skipped**.
- Migration email đã tạo nhưng **chưa apply**; không chạy migration, không ghi production database và không thay connection string.

## Bổ sung — AR Payment gửi receipt qua email outbox — 08/09/2026

- `ArPaymentViewModel.EmailReceipt` và `EmailReceiptCenter` không còn chỉ hiển thị thông báo giả. Hệ thống tra khách hàng thật qua `ICustomerAppService.GetAsync`, kiểm tra email, rồi queue phiếu thu vào `IEmailDispatchService`.
- Bản ghi outbox có `SourceType=ArPayment` và dùng `TransactionId` làm `SourceId` khi mã giao dịch là `Guid`; nếu dữ liệu thiếu mã/email thì thao tác dừng rõ ràng, không đánh dấu đã gửi.
- Các fixture `new Random(...)` còn xuất hiện trong Proforma/Split/Transfer/Receipt đều nằm trong `#if false`; các màn hình production hiện đã dùng repository/service thật hoặc chuyển sang màn hình thật tương ứng. Chúng được giữ lại chỉ làm tài liệu migration, không phải đường chạy.

### Kiểm chứng bổ sung

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite sau tích hợp AR Payment email: **673/673 pass, 0 skipped**.

## Bổ sung — Group Service gửi email theo Group Reservation thật — 08/09/2026

- `GroupServiceViewModel.EmailService` lấy `ContactEmail` bằng `IGroupReservationQueryService` theo `GroupReservationId`, không còn chặn bằng thông báo “chưa có email service”.
- Email được queue với `SourceType=GroupService`, `SourceId=ServiceId`; nếu thiếu property context, group hoặc email liên hệ thì dừng rõ ràng và không tạo lịch sử giả.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite sau tích hợp: **673/673 pass, 0 skipped**.

## Bổ sung — Proforma Folio gửi email qua outbox — 08/09/2026

- `ProformaFolioWindow.OnEmail` đã chuyển từ toast “chưa có email service” sang form nhập người nhận/tiêu đề/nội dung và queue vào `EmailOutboxMessage` với `SourceType=ProformaFolio`.
- Nếu chưa chọn Folio thật hoặc chưa đăng ký outbox, thao tác dừng rõ ràng; không sinh dữ liệu Folio/email giả.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite sau thay đổi Proforma: **673/673 pass, 0 skipped**.

## Bổ sung — Aging: phân tích và xuất dữ liệu thật — 08/09/2026

- `ArAgingViewModel.Analyze` đã tính trực tiếp số tài khoản và tổng dư quá hạn từ `FilteredItems` đang nạp từ `IReceivableService`, không còn chỉ hiển thị “đang thực hiện”.
- `GenerateStatement` và `ExportExcel` xuất CSV từ bản ghi Aging thật; nếu không có dữ liệu thì dừng rõ ràng. CSV là định dạng xuất hiện có, mở được bằng Excel.
- PDF và in chưa có provider nền tảng nên giữ fail-closed, không tạo file hoặc đánh dấu thành công giả. Thu tiền và nhắc nợ cũng báo rõ thiếu command/template workflow thay vì giả lập.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite sau thay đổi Aging: **673/673 pass, 0 skipped**.

## Bổ sung — Express Checkout phê duyệt trả phòng trên Stay thật — 08/09/2026

- Mở rộng `HotelStayDto` để đọc `IsDepartureApproved` từ aggregate/repository thật.
- Bổ sung `ApproveDepartureCommand`, application service, CQRS handler và DI registration. Command dùng permission trả phòng, property context, row-version và ghi domain history/audit qua pipeline hiện có.
- `ExpressCheckoutWindow` hiển thị trạng thái phê duyệt thật và có nút `Phê duyệt trả phòng`; sau khi command thành công, trạng thái Stay được cập nhật kèm row-version mới. Chỉ khi phê duyệt hợp lệ và số dư Folio trong hạn mức thì hồ sơ mới chuyển sang “Sẵn sàng”.
- Các bước Express khác (phương thức thanh toán sau khi rời, hậu kiểm minibar, bàn giao thẻ, invoice) vẫn fail-closed vì chưa có contract dữ liệu tương ứng; không đánh dấu hoàn tất giả.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite sau tích hợp Express approval: **673/673 pass, 0 skipped**.
- Không tạo migration mới vì cột `IsDepartureApproved` đã tồn tại trong schema Stay hiện hành.

## Cập nhật triển khai — Điều chỉnh hóa đơn bằng dữ liệu thật — 08/09/2026

- Bổ sung `InvoiceAdjustmentDraftDto` và `IEInvoiceService.CreateAdjustmentDraftAsync`; nghiệp vụ đọc hóa đơn nguồn thật, chỉ cho điều chỉnh hóa đơn đã phát hành/có mã provider, bắt buộc loại điều chỉnh, lý do, diễn giải, số tiền và thuế hợp lệ.
- Bản điều chỉnh được lưu thành `Invoice` thật với liên kết `AdjustsInvoiceId`, loại `Increase/Decrease/Information`, lý do, dòng hóa đơn và bảng thuế; điều chỉnh giảm được lưu số tiền/dòng/thuế âm.
- `InvoiceManagementWindow` đã thay nút Điều chỉnh từ thông báo chưa triển khai bằng form nhập liệu và gọi service thật; sau khi lưu sẽ tải lại danh sách từ database. Provider điện tử vẫn fail-closed nếu chưa cấu hình adapter/credential thật.
- Tạo migration `20260908140710_AddEInvoiceAdjustmentPersistence` gồm ba cột, index và khóa ngoại tự liên kết trên `Invoices`; migration chỉ được tạo, chưa áp dụng database.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 755 warning hiện hữu của solution.
- Test luồng điều chỉnh giảm: **1/1 pass**; kiểm tra liên kết hóa đơn nguồn, trạng thái `Adjusted`, tổng tiền `-110.000`, dòng tiền `-100.000` và thuế `-10.000`.
- `git diff --check`: **PASS**.
- Không chạy migration, không ghi production database và không thay connection string.

## Rà soát mock lần cuối — Folio, đổi phòng và provider hóa đơn — 08/09/2026

- Đã kiểm tra đường chạy Folio bằng graph và source: `SplitFolioWindow` nạp Folio/khoản mục từ `HotelFolioApplicationService`/EF; danh sách Folio đích chỉ lấy bản ghi đang `Open` hoặc `Suspended`, không tạo Folio giả.
- Provider hóa đơn điện tử giả (`StubEInvoiceProvider`) không được đăng ký trong production DI. DI hiện dùng `UnavailableEInvoiceProvider` cho VIETTEL/VNPT/MISA để phát hành hóa đơn phải dừng an toàn khi chưa có adapter và credential thật; không sinh mã hóa đơn nhà cung cấp giả.
- Các fixture `#if false`, helper `Random` sau `return`, và màn hình Dữ liệu mẫu là mã lịch sử hoặc chức năng seed có chủ đích, không nằm trên đường hiển thị dữ liệu Hotel mặc định.
- Dọn hai dấu vết gây hiểu nhầm trong UI: đổi `MockFormPanel` thành `TargetFolioInfoPanel`; đổi thông báo “bản mockup” ở xác nhận đổi phòng thành “điều chuyển phòng”. Không thay đổi nghiệp vụ dữ liệu.
- Các nút còn fail-closed được xác nhận là gap contract thật, không phải mock: điều chỉnh/gửi email hóa đơn, tạo Shift Log riêng và một số tiện ích export/print/email. Không bật giả dữ liệu để che thiếu service.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors, 748 warnings hiện hữu**.
- Unit suite: **669/669 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật triển khai — Khách đi cùng trong Stay dùng hồ sơ thật — 08/09/2026

- `CurrentGuestsWindow` không còn trả về trạng thái “chưa có backend” ở nút `Thêm khách`. Màn hình tải Stay đang ở thật và các khách đi cùng đang hoạt động từ repository EF; không tạo dòng khách trong bộ nhớ.
- Bổ sung `HotelStayOccupant` với snapshot tên/điện thoại, loại người lớn/trẻ em, trạng thái active, người thêm/xóa và `RowVersion`. Guest được chọn từ `IGuestProfileRepository.SearchAsync`, loại hồ sơ blacklist và không cho chọn lại khách chính/khách đã đăng ký.
- `HotelStayApplicationService.AddOccupantAsync` kiểm tra quyền, property scope, Stay đang lưu trú, concurrency, sức chứa theo room type, ghi lịch sử/audit/domain event và commit transaction thật. Sau khi ghi, UI đọc lại từ database.
- Context menu của khách đi cùng đã có `Xóa khách đi cùng`; command dùng soft-delete, cập nhật số người khi phù hợp, ghi lịch sử/audit/domain event và kiểm tra đồng thời RowVersion của Stay và dòng occupant.
- Bổ sung migration `20260908131232_AddHotelStayOccupants` cho bảng `fo.KhachDiCungLuuTru`, index truy vấn, unique filtered index chống đăng ký trùng và FK đến `fo.LuuTruKhachSan`/`fo.HoSoKhach`.
- Đã apply migration trên database thiết kế riêng `ASMILE_BO_DesignTime` bằng LocalDB; không chạy migration hoặc ghi database production.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- Nhóm test Stay sau thay đổi: **66/66 pass**.
- Full unit suite: **669/669 pass, 0 skipped**.

## Rà soát dữ liệu giả lần cuối — 08/09/2026

### Kết luận đường chạy hiện tại

- Các màn hình Hotel/Front Office đã được kiểm tra theo chuỗi `View/ViewModel → contract → service EF/DbContext`. Những màn hình chính như Stay/Current Guests, Room Master, Housekeeping, Group/Rooming List, Folio, Invoice, Cash Shift, Key Card và Concierge đều đọc hoặc ghi qua service thật; không dùng danh sách khách/phòng/doanh thu dựng trong bộ nhớ để hiển thị production.
- Các `Random`, `LoadMockData`, `PopulateSampleData` còn thấy trong một số ViewModel AR/Housekeeping/Room Master là fixture legacy nằm sau `return` hoặc trong `#if false`, có chú thích `CS0162`; chúng không được gọi trên nhánh có service thật. Các nút seed mẫu của Restaurant/Banquet vẫn là thao tác người dùng chủ động và được tách khỏi load production.
- DI production không đăng ký `Mock*Service` cho các contract Front Office cốt lõi; các vùng chưa có backend dùng `Unavailable*`/fail-closed và không tự tạo bản ghi giả.

### Gap thật, không phải mock

1. `CurrentGuestsWindow` đã có thêm/xóa khách đi cùng trên bảng `fo.KhachDiCungLuuTru`, hồ sơ GuestProfile thật, row-version, audit và command transaction. Còn cần smoke test trên database test có bản ghi Stay/GuestProfile thật; không được chỉ tăng số `Adults` hoặc ghi đè khách chính.
2. Hóa đơn: hủy hóa đơn nháp đã ghi thật qua `IEInvoiceService.CancelAsync`; điều chỉnh hóa đơn và gửi email vẫn chưa có command/provider/audit persistence tương ứng nên đang fail-closed.
3. Phiếu thu/ca: các màn hình đọc dữ liệu thật; một số thao tác fallback (điều chỉnh/hủy phiếu thu, tạo nhật ký ca) chưa có command chuyên biệt. Không xem các thông báo “chưa hỗ trợ” là dữ liệu mock.
4. Group Guest còn thiếu document storage/upload scan và một số tiện ích billing/report/email; Rooming List import đã dùng command thật và kiểm tra property/assignment/room/guest.

### Thứ tự triển khai tiếp theo

1. Smoke test thêm/xóa khách đi cùng trên database test có Stay/GuestProfile/room type thật.
2. Bổ sung command điều chỉnh/gửi email hóa đơn và command phiếu thu; mỗi command phải có audit, quyền và idempotency.
3. Bổ sung document storage cho hồ sơ Group Guest, sau đó xoá fixture legacy chết theo từng module sau khi smoke test.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite: **669/669 pass, 0 skipped**.
- Migration chain và migration occupant đã được kiểm tra trên `ASMILE_BO_DesignTime` bằng LocalDB; không ghi production database.

## Cập nhật — Hóa đơn và kiểm tra dữ liệu thật — 08/09/2026

- `InvoiceManagementWindow` vẫn đọc danh sách hóa đơn thật qua `IEInvoiceService`; bản xem trước và xuất CSV dùng bản ghi thật.
- Đã bổ sung command `IEInvoiceService.CancelAsync`: hủy được hóa đơn ở trạng thái `Draft`, `Adjusted` hoặc `Signed` trước khi provider phát hành; bắt buộc nhập lý do, ghi trạng thái `Cancelled` và nhật ký phát hành vào database.
- Hóa đơn đã phát hành hoặc đã có `ProviderInvoiceId` tiếp tục bị chặn, không giả lập hủy tại chỗ; phải đi qua nghiệp vụ điều chỉnh/hủy của provider hóa đơn điện tử. Gửi email vẫn fail-closed vì chưa có email service và audit delivery thật.
- Kiểm tra DI cho các `Mock*Service`: production đang dùng `Ef*` hoặc implementation `Unavailable*/SafeDefault` có chủ ý; không có mock service nào được đăng ký thay cho các contract kế toán/công nợ/tồn kho chính.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; cảnh báo hiện hữu của solution vẫn còn.
- Full unit suite sau bổ sung hủy hóa đơn: **662/662 pass, 0 skipped**.
- Không thay connection string và không ghi production database.

## Cập nhật — Thao tác khác của Khách đang lưu trú — 08/09/2026

- Nút `Thao tác khác` không còn hiện thông báo chung; nó mở menu gọi các workflow đã có dữ liệu/command thật: xem Folio, gia hạn lưu trú, đổi phòng, Housekeeping và chuẩn bị Check-out.
- Mỗi workflow tiếp tục tự kiểm tra `StayId`/`RowVersion` và fail-closed khi hồ sơ thật không đủ định danh; không tạo dữ liệu trong bộ nhớ.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 748 cảnh báo hiện hữu của solution.
- Full unit suite: **662/662 pass, 0 skipped**.

## Cập nhật — Kiểm tra và sửa chuỗi migration dữ liệu thật — 08/09/2026

- Phát hiện migration `20260721065248_AddRatePlanGroupReservationAndReservationGuaranteeDeposit` được sinh lệch snapshot và tạo lại các bảng đã có từ `20260720032319_AddReservationEnterpriseFields` (`DatPhongDoan`, rate plan, stay, folio...).
- Đã thu gọn migration lệch còn đúng 5 cột bảo đảm/đặt cọc trên `fo.DatPhong`: `DepositAmount`, `DepositDueDate`, `DepositStatus`, `GuaranteeExpiryDate`, `GuaranteeType`; không xoá hoặc tạo lại dữ liệu nền tảng.
- Database kiểm chứng riêng `ASMILE_BO_DesignTime` trên `(localdb)\MSSQLLocalDB` đã được tạo lại từ rỗng và chạy thành công đến migration mới nhất `20260908170000_AddConciergePersistence`.
- Hậu kiểm SQL xác nhận lịch sử migration mới nhất, 5 cột đặt cọc và các bảng `fo.DatPhongDoan`, `fo.LichDatCocDoan` tồn tại. Không chạy migration và không ghi database ứng dụng/production.
- Test riêng Group Reservation/Folio: **16/16 pass, 0 skipped**; build App: **0 lỗi** (cảnh báo hiện hữu của solution vẫn còn).

## Cập nhật — Check-out nhanh chuyển sang workflow thật — 08/09/2026

- `QuickCheckoutWindow` tiếp tục nạp danh sách khách, Stay và Folio từ dữ liệu thật như trước; nút cuối không còn kết thúc bằng thông báo “chưa có workflow”.
- Khi Folio đã cân và Stay có `StayId`/`RowVersion` hợp lệ, màn hình mở `CheckoutPreparationWindow` theo đúng Stay đang chọn. Quy trình chuẩn tiếp tục kiểm tra dịch vụ chưa chốt, Housekeeping, thẻ phòng và gọi `IReceptionUiService.CheckOutAsync` để ghi trạng thái thật.
- Nếu thiếu dữ liệu thật, Folio còn số dư hoặc RowVersion không hợp lệ, thao tác vẫn dừng fail-closed; không tạo payment, hóa đơn hoặc Check-out giả.

### Kiểm chứng

- Build `ASMILE.BO.App`: **0 lỗi**, 748 cảnh báo hiện hữu.
- Full unit suite: **662/662 pass, 0 skipped**.

## Cập nhật — Hiển thị số tiền dự kiến theo lịch cọc — 08/09/2026

- Cột “Số tiền” trong Group Booking wizard không còn hiển thị dấu `—` khi đã có Room Block và rate thật.
- Mỗi đợt hiển thị số tiền dự kiến = doanh thu dự kiến từ rate/Room Block thật × phần trăm đợt cọc. Giá trị này được đánh dấu là dự kiến; khoản thu thực tế chỉ phát sinh qua nghiệp vụ Group Deposit/Folio, không bị ghi giả vào lịch.
- Build App: **0 lỗi**; full unit suite: **662/662 pass, 0 skipped**.

## Cập nhật — Lưu lịch đặt cọc Group Booking bằng dữ liệu thật — 08/09/2026

- Trước đây `GbDepositSchedule` chỉ là collection trong `GroupBookingWizardWindow`, không có bảng/domain nên lịch đặt cọc biến mất sau khi đóng wizard.
- Bổ sung chính sách đặt cọc vào `GroupReservation`: yêu cầu đặt cọc, loại bảo đảm, ngày cut-off, ngày release và các dòng lịch đặt cọc.
- Bổ sung aggregate `GroupDepositSchedule` với property/group scope, ngày đến hạn, tỷ lệ và trạng thái `Pending`; lịch chỉ được chấp nhận khi tổng tỷ lệ bằng 100%, có loại bảo đảm và đúng chủ sở hữu.
- `ConfigureGroupBookingRequest` nhận `GroupDepositPolicyDraft`; `EfGroupReservationCommandService.ConfigureBookingAsync` tạo và commit lịch cùng room block trong một lần lưu. Không dùng `Notes` để nhét JSON và không tạo dữ liệu mẫu.
- Tạo migration `20260908120304_AddGroupDepositSchedules`: thêm cột chính sách vào `fo.DatPhongDoan` và bảng `fo.LichDatCocDoan` cùng index/FK. Migration mới chỉ được sinh, chưa chạy trên database production.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors** (cảnh báo hiện hữu của solution vẫn còn).
- Test domain Group Booking: **13/13 pass**.
- Full unit suite loại integration: **662/662 pass, 0 skipped**.
- Có thêm test round-trip EF InMemory: lưu context 1, đọc lại bằng context 2 và xác nhận đủ 2 lịch cùng policy thật.
- Đã kiểm tra nhánh không yêu cầu đặt cọc: cho phép lưu không có schedule; nhánh yêu cầu đặt cọc nhưng thiếu lịch/tổng tỷ lệ khác 100% bị chặn rõ ràng.

## Cập nhật — cấp quyền Folio cho role Accounting — 08/09/2026

- `SecuritySeedService` đã cấp cho role `Accounting` các quyền Folio cần cho vận hành thu ngân: xem Folio, ghi phát sinh/thanh toán, quản lý trạng thái và đảo giao dịch.
- Việc cấp quyền không bypass authorization; các CQRS handler Folio vẫn kiểm tra permission và property context như trước.
- Bổ sung test seed quyền để tránh tái phát lỗi `Folio view permission is required` khi người dùng Accounting mở Tách/Gộp Folio.

### Kiểm chứng

- Chưa chạy migration và chưa ghi production database.
- Unit suite (loại integration): **658/658 pass, 0 skipped**.

## Cập nhật rà soát mock — bỏ auto-seed dịch vụ dùng chung — 08/09/2026

- `MenuRecipeViewModel.LoadAllExtraServicesAsync` không còn tự gọi `SeedDefaultExtraServicesAsync` khi danh mục dịch vụ bổ sung rỗng; màn hình chỉ đọc dữ liệu thật và hiển thị trạng thái chưa có dữ liệu.
- `BanquetRestaurantViewModel.LoadBanquetServicesWithRetryAsync` cũng chỉ đọc dịch vụ bổ sung thật. Khi chưa có dữ liệu, danh sách tiệc để trống và báo rõ trạng thái, không ghi mẫu vào database.
- `AlacarteRestaurantViewModel.BootstrapSampleDataAsync` vẫn tồn tại nhưng chỉ là thao tác khởi tạo mẫu chủ động; không được gọi từ đường mở màn hình.

### Kiểm chứng

- Đã rà lại tất cả lời gọi `SeedDefaultExtraServicesAsync` trong `ASMILE.BO.App`: chỉ còn lệnh khởi tạo mẫu chủ động.
- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite (loại integration): **657/657 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Cập nhật rà soát mock — Rate Engine và Extra Services không auto-seed — 08/09/2026

- `RateEngineViewModel.LoadAsync` đã bỏ việc tự tạo các rate plan `RACK`, `CORP`, `GROUP` khi database rỗng. Màn hình chỉ đọc rate plan thật; nếu chưa có dữ liệu thì báo rõ để người dùng tạo/import cấu hình.
- `AlacarteRestaurantViewModel.LoadExtraServicesAsync` đã bỏ fallback gọi `SeedDefaultExtraServicesAsync`. Danh mục dịch vụ rỗng không còn làm phát sinh bản ghi mẫu; trạng thái được hiển thị là chưa có dịch vụ thật.
- Hai thay đổi này ngăn dữ liệu mẫu được ghi ngầm khi mở module, đồng thời giữ nguyên đường seed chủ động phục vụ môi trường demo/test nếu được gọi rõ ràng.

### Kiểm chứng

- Đã rà lại các `SeedDefault*`/`Random` trong đường chạy production: các generator còn lại không được gọi tự động hoặc nằm sau nhánh kết thúc/fixture legacy.
- Chưa chạy migration và chưa ghi production database.

## Cập nhật — Rooming List nối command phân phòng thật — 08/09/2026

- `GroupRoomingListWindow` đã lấy `IGroupRoomAssignmentCommandService` từ DI và không còn để các nút Auto Pair/Swap/Split chỉ hiển thị cảnh báo giả.
- Từ bước Ghép phòng, người dùng có thể mở `GroupRoomAssignmentWindow` theo đúng `GroupReservationId` để ghép, đổi và tách phòng bằng command `ReplaceAsync` thật; sau khi đóng màn hình, Rooming List tải lại từ database.
- Xóa ghép phòng đã có thao tác trực tiếp qua `RemoveAsync`, yêu cầu chọn phòng, xác nhận và chặn phòng đã check-in/check-out. Không sửa `_roomPairs` để giả lập kết quả.
- Bổ sung `HotelRoomId` và trạng thái sử dụng vào projection card phòng để command luôn dùng khóa thật và hiển thị đúng trạng thái.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 750 warning hiện hữu của solution.
- Chưa chạy migration và chưa ghi production database.

## Cập nhật — Rooming List loại bỏ thay đổi giả trong bộ nhớ — 08/09/2026

- Các thao tác ghép, tách và xóa ghép phòng trong `GroupRoomingListWindow` không còn tự sửa `_roomPairs` rồi hiển thị như đã lưu. Vì màn hình hiện chưa có đủ assignment/room-pair command và khóa đồng thời, các thao tác này dừng rõ ràng và không làm thay đổi dữ liệu trong bộ nhớ.
- Quick edit khách chỉ cho sửa điện thoại, email và ghi chú; các trường tên, Passport và Visa được hiển thị chỉ đọc vì chưa có command tài liệu/hồ sơ tương ứng. Dữ liệu được cập nhật qua `IGuestProfileRepository.UpdateAsync`, sau đó tải lại từ database.
- Đây là fail-closed có chủ đích: không báo thành công khi chưa có đường ghi dữ liệu thật.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 750 warning hiện hữu của solution.
- Unit suite (loại integration): **657/657 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Cập nhật — Room Status nối task và người phụ trách thật — 08/09/2026

- `HousekeepingRoomStatusViewModel` đã đọc task board theo đúng property, ánh xạ task đang hoạt động, `AssignedHousekeeperId` và tên nhân viên vào trạng thái phòng; không còn gán người phụ trách/timestamp dựng sẵn.
- Đổi người phụ trách dùng danh sách nhân sự Housekeeping đang hoạt động, gọi `HkTaskAction.Reassign`, truyền `RowVersion` qua workflow thật và tải lại snapshot phòng sau khi commit.
- Nếu thiếu task thật, context, service hoặc kết quả commit không thành công thì giao diện dừng và giữ nguyên dữ liệu; không báo thành công giả.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 750 warning hiện hữu của solution.
- Unit suite (loại integration): **657/657 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật — Rooming List nạp hồ sơ khách thật — 08/09/2026

- `GroupRoomingListWindow` đã resolve `IGuestProfileRepository` và nạp hồ sơ theo `GuestId` + property hiện tại khi tải Rooming List.
- Các trường tên dự phòng, quốc tịch, điện thoại, email và ghi chú lấy từ `GuestProfileDto`; trạng thái check-in/check-out vẫn lấy từ assignment thật. Hồ sơ thiếu hoặc lỗi riêng lẻ không làm mất danh sách Rooming List.
- Rà soát lại các `Random` còn sót: AR, Housekeeping và Folio/Proforma là fixture legacy nằm sau nhánh `return` hoặc sau đường tải dữ liệu thật; `ReceiptVoucherWindow` chuyển sang `PaymentReceiptsView` thật khi DI khả dụng và fallback chỉ để rỗng, không sinh 3.000 phiếu mẫu.
- Các gap chưa có contract vẫn được giữ fail-closed: thêm khách mới, Auto Pair/Swap/Split Room trong Rooming List; allocation/reversal AR; chỉnh/hủy hóa đơn; Marketing CRUD; một số tiện ích Express/Quick Checkout. Không bật mock để che thiếu command.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 750 warning hiện hữu của solution.
- Unit suite (loại integration): **657/657 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật — Nhập Rooming List từ Quản lý khách đoàn — 08/09/2026

- Nút `Import Excel` trước đây chỉ hiển thị `Unavailable` dù backend đã có `IRoomingListService.ImportAsync`; đã đổi tên thành `Nhập Rooming List CSV` để không quảng cáo sai định dạng.
- Luồng nhập gọi `ExportAsync` lấy danh sách assignment hiện có, kiểm tra header, `AssignmentId`, phòng/hạng phòng, GuestId, Adults/Children và VIP trước khi gọi `ImportAsync`.
- Dữ liệu chỉ được ghi qua `EfRoomingListService`, aggregate kiểm tra property/group/assignment/guest/trạng thái, stage audit và commit một lần. Không tạo guest/profile giả và không đổi phòng từ file nhập.
- Sau commit, màn hình nạp lại group summary/guest list từ dữ liệu thật; lỗi hoặc file không hợp lệ không thay đổi giao diện thành công giả.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors** (750 warnings hiện hữu của solution).
- Unit suite loại integration: **657/657 pass, 0 skipped**.
- Chưa chạy migration và chưa ghi production database.

## Cập nhật — Checklist Phòng bẩn lưu bằng chứng thật — 08/09/2026

- `DirtyRoomViewModel.ToggleChecklistItem` giờ đổi trạng thái mục checklist đã nạp từ `InspectionChecklistData`, gửi lại toàn bộ checklist qua `HkTaskAction.SaveInspectionEvidence`, rồi mới giữ thay đổi trên giao diện.
- Nếu workflow/backend từ chối, trạng thái mục được hoàn tác; không còn thông báo khóa giả hoặc cập nhật memory-only.
- VIP/Rush và “Lock phòng” vẫn được phân loại là gap backend thật vì chưa có command/schema sở hữu dữ liệu tương ứng.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**, 750 warnings hiện hữu.
- Unit suite loại integration: **656/656 pass, 0 skipped**.

## Cập nhật — VIP/Rush Housekeeping dùng priority thật — 08/09/2026

- Bổ sung `RoomPriority.Rush` và `HkTaskAction.SetPriority`. Trường `fo.DonDep.Priority` đã tồn tại và đang lưu dạng chuỗi, nên không cần thêm cột/migration cho thay đổi này.
- `DirtyRoomViewModel` đã nối các nút VIP/Rush vào workflow thật; command kiểm tra task/property và chỉ cập nhật UI sau khi commit thành công. Task hoàn tất/hủy bị từ chối đổi ưu tiên.
- Board đọc lại priority từ task thật; không còn tình trạng `IsRush` luôn không thể phát sinh do enum thiếu giá trị `Rush`.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**, 757 warnings hiện hữu.
- Unit suite loại integration: **657/657 pass, 0 skipped**; bổ sung `HK_018` cho chuyển priority và chặn task hoàn tất.
- Không chạy migration và không ghi production database.

## Cập nhật — Khóa phòng trong Phân phòng đoàn dùng command thật — 08/09/2026

- Nút `Lock Selected` và menu `Lock Room` trong `GroupRoomAssignmentWindow` đã nối vào `ChangeHotelRoomOperationalStatusCommandHandler`.
- Trước khi ghi, màn hình đọc lại phòng và `RowVersion` từ Hotel Master Data, không cho khóa phòng đã được phân cho khách, cho chọn `OutOfOrder`/`OutOfService`, rồi cập nhật card sau khi command commit thành công.
- Xung đột hoặc lỗi backend không được coi là thành công; trạng thái hiển thị chỉ đổi sau kết quả thật.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**, 750 warnings hiện hữu.
- Unit suite loại integration: **656/656 pass, 0 skipped**.
- `git diff --check`: **PASS**.

## Cập nhật — Housekeeping batch dùng workflow thật — 08/09/2026

- `CleaningInProgressViewModel.BatchChangeStaff` không còn bị khóa bằng thông báo placeholder. Màn hình tải danh sách tài khoản đang hoạt động từ `IHousekeepingWorkflowService`, cho chọn một nhân sự qua picker dùng chung, rồi gọi `HkTaskAction.Reassign` cho từng HousekeepingTask đủ điều kiện (`Assigned`, `Cleaning`, `Paused`). Chỉ task ghi thành công mới cập nhật tên/người phụ trách trên giao diện.
- `RoomInspectionViewModel.BatchAssignInspector` cũng đã dùng danh sách nhân sự đang hoạt động và lưu inspector thật qua `HkTaskAction.SaveInspectionEvidence` cho từng task. Các task bị từ chối không bị đánh dấu thành công cục bộ.
- Hai thao tác vẫn fail-closed khi không có task thật, không có workflow service, không có nhân sự active hoặc backend từ chối; không quay lại dữ liệu mẫu.

### Kiểm chứng sau vòng này

- Build `ASMILE.BO.App`: **PASS, 0 errors**, 750 warnings hiện hữu của solution.
- Unit suite loại integration: **656/656 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Cập nhật — Rà soát Housekeeping không còn tự suy đoán dữ liệu — 08/09/2026

- `CleaningInProgressViewModel`, `CleanRoomViewModel`, `DirtyRoomViewModel` và `RoomInspectionViewModel` tiếp tục đọc task/room master/stay/reservation thật; fixture `Random` chỉ còn nằm trong khối comment legacy sau nhánh fail-closed, không chạy trên đường mở màn hình.
- Bỏ nhãn công việc, SLA 45 phút và checklist 13 mục tự sinh. Khi dữ liệu task không có estimate/checklist thì hiển thị rỗng hoặc 0, không thay bằng dữ liệu mẫu.
- Ảnh bằng chứng của Cleaning/Dirty Room được copy vào thư mục ứng dụng và chỉ giữ lại khi `SaveInspectionEvidence` commit thành công; lỗi workflow sẽ xóa file tạm và không đổi trạng thái UI.
- Các thao tác Room Status chỉ đổi giao diện sau khi workflow thật trả về thành công. Trạng thái hiển thị lấy từ `HkWorkflowResult` trả về từ persistence; lỗi, thiếu context, thiếu `RoomId` hoặc thiếu service đều fail-closed.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 746 warning hiện hữu của solution.
- Unit suite loại integration: **654/654 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không bật lại dữ liệu mẫu.

## Cập nhật — Phân công lại Housekeeping bằng workflow thật — 08/09/2026

- Bổ sung action `Reassign` cho task Housekeeping; domain chỉ cho đổi nhân sự ở các trạng thái đang hoạt động và từ chối task chưa được phân công hoặc đã kết thúc.
- Các màn hình Đang dọn, Phòng bẩn và Kiểm tra phòng lấy danh sách AppUser đang hoạt động, yêu cầu chọn người, gọi workflow thật rồi mới cập nhật UI.
- Phân công Supervisor được lưu qua `SaveInspectionEvidence` với `InspectorId` thật, không còn hiển thị nút nhưng không có tác dụng.
- Thêm picker dùng chung và kiểm thử domain cho đổi nhân sự hợp lệ/không hợp lệ.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 755 warning hiện hữu.
- Unit suite loại integration: **656/656 pass, 0 skipped**.

## Cập nhật kiểm tra dữ liệu thật — Room Inspection hiển thị Inspector từ AppUsers — 08/09/2026

- `EfHousekeepingBoardService` tra cứu `RoomInspection.InspectorId` trong `AppUsers` và truyền `DisplayName`/`UserName` thật vào `HkTaskBoardDto`.
- `RoomInspectionViewModel` hiển thị tên Inspector thật; không còn dùng GUID làm tên người kiểm tra.
- Nếu InspectorId không còn bản ghi `AppUsers` tương ứng, trường để trống theo nguyên tắc fail-closed; không sinh tên mẫu.
- `HkTaskActionRequest` hiện nhận `InspectorId` và `InspectionChecklistData`; command Approve/Reject lưu các giá trị này vào `RoomInspection` trong cùng transaction.
- Checklist UI chỉ đọc các item từ JSON đã lưu; khi chưa có item thật, danh sách để rỗng và Approve bị chặn. Không còn dựng 24 item, điểm số, Inspector hoặc ảnh mẫu.
- Nút Lost & Found của Room Inspection đã truy vấn `ILostFoundQueryService` thật theo property và số phòng; Approve cũng kiểm tra lại trước khi duyệt. Không có service/context thì fail-closed.
- Upload ảnh kiểm tra đã copy file người dùng chọn vào `LocalApplicationData/ASMILE-BO/HousekeepingInspectionPhotos` và gọi action `SaveInspectionEvidence` để lưu metadata vào `RoomInspection.ChecklistData` ngay, không đổi trạng thái task. Approve/Reject tiếp tục ghi lại evidence trong cùng transaction; không tạo path/ảnh giả.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 error**; warning hiện hữu của solution vẫn còn.
- Unit test Housekeeping/Room Inspection: **23/23 pass, 0 skipped**.
- Toàn bộ unit test loại integration: **654/654 pass, 0 skipped**.
- Không chạy migration, không ghi production database.

## Cập nhật kiểm tra mock — Room Inspection dùng master phòng thật — 08/09/2026

- `RoomInspectionViewModel` vẫn lấy task/inspection từ `IHousekeepingBoardService` thật và ghi Approve/Reject qua `IHousekeepingWorkflowService` thật.
- Bổ sung `IHotelRoomMasterDataRepository` vào đường runtime để map `RoomType`, `Building`, `Floor` và `Zone` từ `HotelRoom`/`HotelRoomType` thật; không còn mặc định `STD`, `A`, `Khu A` cho task thật.
- `InspectionChecklistData` được đọc từ dữ liệu đã lưu dưới dạng JSON nếu có. Khi DB chưa có checklist/minibar, giao diện giữ trạng thái chưa đủ dữ liệu và fail-closed; không còn gán giả `24` checklist.
- Các mảng tên Supervisor/Housekeeper và fixture `Random` chỉ còn là mã thiết kế cũ không chạy sau nhánh production; đây vẫn là backlog dọn mã, không phải nguồn dữ liệu màn hình. Nút phân công Supervisor, ảnh kiểm tra và Lost & Found tiếp tục bị khóa vì chưa có command/repository thật.

### Kiểm chứng

- Build và unit suite không integration: **653/653 pass, 0 skipped**.
- Không chạy migration, không đổi connection string và không ghi production database.

## Cập nhật kiểm tra mock — hoàn thiện đường dữ liệu Housekeeping — 08/09/2026

- `DirtyRoomViewModel`, `CleaningInProgressViewModel` và `CleanRoomViewModel` tiếp tục lấy task từ `IHousekeepingBoardService` thật; bổ sung map `RoomType`, `Floor`, `Building/Zone` từ `IHotelRoomMasterDataRepository` theo `RoomId`/`RoomNumber`.
- Checklist của Phòng bẩn, Đang vệ sinh và Phòng sạch chỉ đọc từ `HkTaskBoardDto.InspectionChecklistData` đã lưu. Khi không có JSON hợp lệ, tổng checklist là `0`, không còn dựng 13 mục hoặc tự đặt tiến độ `8/13`.
- `DirtyRoomView.xaml` đã đổi từ 13 CheckBox cố định sang danh sách item persisted chỉ đọc; khi không có dữ liệu thật sẽ hiển thị thông báo rõ ràng.
- `HousekeepingRoomStatusViewModel` map loại phòng từ Room Master thật và không còn mặc định `STD`, `Standard`, `King`, `Khu A` hay coi trạng thái rỗng là `Clean`.
- Các fixture `Random`/danh sách khách, nhân viên, phòng và checklist của bốn màn hình Housekeeping đã bị loại khỏi đường chạy production; các command chưa có persistence tiếp tục fail-closed thay vì giả cập nhật trên UI.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 746 warnings hiện hữu.
- Toàn bộ unit test loại integration: **654/654 pass, 0 skipped**.
- `git diff --check` không phát hiện lỗi whitespace trong thay đổi; các cảnh báo LF/CRLF là cảnh báo line-ending hiện hữu của worktree.
- Không chạy migration, không đổi connection string và không ghi production database.

## Cập nhật triển khai — bằng chứng ảnh và ngữ cảnh khách Housekeeping — 08/09/2026

- Nút Upload ảnh của `CleaningInProgressViewModel` đã chọn file thật, lưu vào `%LOCALAPPDATA%/ASMILE-BO/HousekeepingInspectionPhotos`, sau đó ghi metadata vào `InspectionChecklistData` bằng `SaveInspectionEvidence`; nếu workflow thất bại thì xóa file và rollback danh sách ảnh trên UI.
- `DirtyRoomView.xaml` đã nối hai nút ảnh trước/sau vào command thật với cùng cơ chế lưu và rollback.
- Dirty Room, Cleaning In Progress, Clean Room và Room Inspection bổ sung đối chiếu `HotelStayDto`/`ReservationDto` theo phòng; tên khách, reservation, ngày đến/đi, VIP, yêu cầu đặc biệt và công ty/market chỉ hiển thị khi có bản ghi thật.
- Khi thiếu Stay/Reservation hoặc không có phòng được gán, các trường giữ rỗng; không sinh tên khách, mã reservation hoặc thời gian giả.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 746 warnings hiện hữu.
- Toàn bộ unit test loại integration: **654/654 pass, 0 skipped**.
- Không chạy migration, không đổi connection string và không ghi production database.

## Cập nhật triển khai — Đóng đoàn bằng command EF thật — 08/09/2026

- Bổ sung `CloseAsync` vào `IGroupReservationCommandService` và `EfGroupReservationCommandService`.
- `GroupReservation.Close` chỉ cho phép chuyển sang `Closed` sau `CheckedOut` hoặc `Cancelled`; không thể đóng đoàn đang hoạt động.
- `GroupReservationViewModel` gọi command thật, property-scope, commit/audit và tải lại danh sách sau khi đóng; không còn thông báo chặn giả cho thao tác đã có aggregate hỗ trợ.
- Xóa vật lý đoàn nháp, email và các thao tác chưa có contract vẫn giữ fail-closed.

### Kiểm chứng

- Test workflow Group: **10/10 pass**.
- Build qua trong test run, không có lỗi biên dịch.
- Chưa chạy migration và không ghi production database.

## Cập nhật audit mock — Door Lock — 08/09/2026

- Phát hiện `DoorLockEngine.cs` còn ba implementation mock có thể trả dữ liệu giả cố định: `DoorLockService` trả `LCK-101`, `DoorEventService` trả sự kiện phòng 101/102, và `DoorLockDashboardService` trả KPI 120/115.
- Các implementation này không được đăng ký trong DI production; DI đã dùng `EfDoorLockService`, `EfDoorEventService` và `EfDoorLockDashboardService`, đều đọc `DoorLockDevices`/`DoorLockEvents` theo property context thật.
- Đã xoá ba implementation mock và các test cũ kiểm thử dữ liệu giả. Test persistence `DoorLockPersistenceTests` tiếp tục kiểm tra thiết bị, sự kiện, cảnh báo và KPI từ SQLite database thật.
- `GenericLockAdapter` vẫn fail-closed cho thao tác với phần cứng (encode/revoke/ping/emergency open trả thất bại) vì chưa có driver vendor và cấu hình credentials; đây là tích hợp ngoài database, không được thay bằng dữ liệu mẫu.

### Kết luận đường chạy

- Đọc danh sách khóa, thiết bị offline, pin yếu, sự kiện truy cập, failed attempts, cảnh báo bảo mật và dashboard: **dữ liệu thật qua EF**.
- Đồng bộ/điều khiển khóa vật lý: **chưa triển khai**, đang fail-closed có chủ đích.
- Không chạy migration, không ghi production database.

## Cập nhật triển khai — Current Guests / Điều chỉnh lưu trú — 08/09/2026

- Các nút `Gia hạn lưu trú` và `Đổi phòng` trong `CurrentGuestsWindow` hiện truyền `HotelStayId` của dòng đang chọn vào modal; hồ sơ thiếu định danh hoặc row-version thật sẽ dừng trước khi mở workflow.
- `RoomMoveExtendWindow` đã nạp rate plan active thật theo property, tính giá phòng từ `RatePlan.CalculatePrice`, hiển thị currency của property và không còn dùng giá 0 mặc định để cho qua xác nhận.
- Trạng thái `Có booking tiếp 30/07` dựng sẵn đã được loại bỏ. Màn hình chỉ thông báo sẽ kiểm tra availability bằng command thật khi xác nhận; không hiển thị booking giả.
- Không thay đổi schema/migration; thao tác lưu cuối vẫn dùng `MoveRoomAsync`/`ExtendStayAsync` với row-version và fail-closed khi thiếu rate plan hoặc dữ liệu thật.

### Kiểm chứng

- Build `ASMILE.BO.App`: **0 lỗi**; chỉ còn warning hiện hữu của solution.
- Không chạy migration và không ghi production database.

## Cập nhật tiếp — Group Booking Wizard không còn tạo Rooming List giả — 08/09/2026

- `GroupBookingWizardWindow` đã được đối chiếu với `IGroupReservationCommandService.ConfigureBookingAsync`: bước lưu/xác nhận hiện ghi thật đoàn, room block và giá negotiated. Cảnh báo UI đã sửa cho đúng phạm vi command.
- Đã bỏ nút `Auto-Fill` sinh hàng loạt khách `Guest 1...` trong bộ nhớ. Rooming List trong wizard chỉ còn là bản nháp tham khảo; Rooming List chính thức phải nhập từ assignment thật sau khi phân phòng.
- Kiểm tra booking không còn fail vì Rooming List nháp trống; người dùng chỉ cần xác nhận rằng Rooming List sẽ được nhập ở bước sau. Lịch đặt cọc và chính sách dịch vụ vẫn không bị báo là đã lưu vì chưa có contract trong command cấu hình booking.
- `GroupRoomAssignmentWindow`: `Gọi HK` mở `HousekeepingMessageWindow` với số phòng đã phân; `In danh sách HK` tạo preview từ phòng/assignment/khách thật. Các thao tác khóa phòng và lịch sử vẫn fail-closed do chưa có command/query tương ứng.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite (loại integration): **657/657 pass, 0 skipped**.

## Cập nhật tiếp — Housekeeping phòng bẩn export/print — 08/09/2026

- `DirtyRoomViewModel` đã xuất CSV UTF-8 và preview bản in từ `FilteredRooms` có `HousekeepingTaskId` thật.
- Các thao tác batch assign/start/complete tiếp tục dùng workflow thật; checklist, VIP/Rush và đánh dấu theo từng mục vẫn fail-closed vì chưa có command persistence tương ứng.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.

## Cập nhật tiếp — Room Inspection dùng workflow thật — 08/09/2026

- `RoomInspectionViewModel` vẫn đọc danh sách từ `HousekeepingTask` thật; nay Approve/Reject đơn lẻ và hàng loạt đều fail-closed khi thiếu workflow/task thật, không còn cập nhật trạng thái chỉ trong memory.
- Bổ sung cột chọn trên `RoomInspectionView.xaml`, batch Approve/Reject gọi `IHousekeepingWorkflowService.ExecuteTaskAsync` với kiểm tra checklist, minibar, maintenance và Lost & Found trước khi Approve.
- `Xuất Excel` đã xuất CSV UTF-8 từ danh sách kiểm tra đang lọc; `In danh sách` mở preview từ cùng dữ liệu thật.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite (loại integration): **657/657 pass, 0 skipped**.

## Cập nhật tiếp — Rooming List và Housekeeping export/print — 08/09/2026

- `GroupRoomingListWindow`: nút `Nhập CSV` hiện đọc file Rooming List đã xuất, đối chiếu `AssignmentId` với dữ liệu thật của đoàn, không cho đổi phòng/hạng phòng qua import, kiểm tra số người và VIP rồi gọi `IRoomingListService.ImportAsync`. Service cập nhật aggregate thật, audit và commit; lỗi trả về theo từng batch, không reload thành công giả.
- `CleaningInProgressViewModel`: `Xuất Excel` đã chuyển thành CSV UTF-8 từ `FilteredRooms` lấy từ `HousekeepingTask`; `In danh sách` mở preview từ đúng dữ liệu đang lọc. Không còn tạo file hoặc bản in từ fixture khi không có task thật.
- `GroupRoomAssignmentWindow`: `In danh sách` mở preview từ các assignment/guest/room thật đang được phân trong phiên; nút `Lock Selected`, lịch sử phòng và gọi Housekeeping vẫn fail-closed vì chưa có command/query contract tương ứng.
- Các nút `Auto Pair`, `Swap`, `Split Room` của Rooming List vẫn được giữ khóa: backend hiện chỉ có import cập nhật assignment hiện hữu, chưa có command an toàn để tạo/tách/đổi assignment từ màn hình này. Phân phòng thật vẫn thực hiện qua `GroupRoomAssignmentWindow` và `ReplaceAsync` sau bước xác nhận.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- Unit suite (loại integration): **657/657 pass, 0 skipped**.
- `git diff --check`: không phát hiện lỗi whitespace; chỉ có cảnh báo line-ending CRLF/LF hiện hữu.
- Không chạy migration và không ghi production database.

## Cập nhật triển khai — Housekeeping đang dọn: batch workflow thật — 08/09/2026

- `CleaningInProgressView` có checkbox chọn nhiều phòng có `HousekeepingTaskId` thật.
- `Batch Assign` dùng `HkTaskAction.Assign` với tài khoản operator hiện tại.
- `Batch Complete` dùng `HkTaskAction.CompleteCleaning` và chuyển các phòng thành `WaitingInspection`.
- `Batch Inspect` dùng `HkTaskAction.ApproveInspection` và chuyển các phòng thành `Completed/Ready`.
- Mỗi dòng chỉ cập nhật trạng thái giao diện sau khi command tương ứng thành công; các dòng lỗi không bị đánh dấu thành công giả. `Batch Change Staff` vẫn khóa vì chưa có command chọn nhân viên mới.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- Unit suite (loại integration): **657/657 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Cập nhật triển khai — Transaction Center ghi giao dịch Folio thủ công thật — 08/09/2026

- Nút `Tạo giao dịch thủ công` đã nối các command thật `Manual charge`, `Manual credit` và `Adjustment`.
- Màn hình chỉ cho ghi khi giao dịch đang chọn có `HotelFolioId` và `RowVersion` thật; bắt buộc số tiền dương và diễn giải.
- Sau khi command thành công, cửa sổ tải lại ledger từ database; lỗi quyền, xung đột rowversion hoặc thiếu handler được báo rõ và không cập nhật trạng thái giả trong bộ nhớ.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- Unit suite (loại integration): **657/657 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Cập nhật triển khai — Transaction Center và nhật ký thẻ xuất/in dữ liệu thật — 08/09/2026

- `TransactionCenterWindow` tính KPI theo ngày kinh doanh trong execution context, không còn ngày hard-code cũ; phần xuất CSV dùng đúng giao dịch Folio sau bộ lọc.
- `KeyCardAuditLogWindow` tiếp tục đọc `KeyCardEvents` thật theo property context; nút Xuất tạo CSV UTF-8 từ tập sự kiện đã lọc, nút In mở preview từ cùng tập dữ liệu.
- Khi không có dữ liệu thật phù hợp, cả hai màn hình đều báo rõ và không sinh dòng mẫu.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- Unit suite (loại integration): **657/657 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Cập nhật triển khai — Phân công Housekeeping bằng AppUser thật — 08/09/2026

- Bổ sung `HkAssigneeDto` và `IHousekeepingWorkflowService.GetActiveAssigneesAsync` để lấy tài khoản đang hoạt động từ `AppUsers`.
- Nút `Phân công` trong `HousekeepingMessageWindow` cho chọn nhân sự thật, truyền đúng `AppUser.Id` vào `HkTaskAction.Assign`, kiểm tra active ở service rồi tải lại task board.
- Không còn thông báo khóa giả; nếu không có tài khoản active hoặc thiếu dữ liệu, thao tác vẫn dừng fail-closed.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 750 cảnh báo hiện hữu của solution.
- Unit suite (loại integration): **657/657 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Cập nhật triển khai — Giữ ngữ cảnh Stay/phòng khi chuyển màn hình — 08/09/2026

- `TodayArrivalsWindow` truyền `StayId` thật sang `StayProfileWindow`; màn hình hồ sơ tự chọn đúng dòng lưu trú sau khi tải từ repository.
- `TodayArrivalsWindow` truyền số phòng thật sang `HousekeepingMessageWindow`; bảng Housekeeping tự lọc theo phòng đang chọn sau khi tải task thật.
- `CurrentGuestsWindow` cũng truyền `StayId`/số phòng thật khi mở hồ sơ lưu trú và Housekeeping, tránh mở màn hình chung không theo đúng khách đang chọn.
- `CurrentGuestsWindow` truyền tiếp `StayId` thật sang `ExtendStayWindow`, để màn hình gia hạn tự chọn đúng Stay sau khi tải dữ liệu lưu trú thật.
- Không tạo Stay/task giả và không thay đổi dữ liệu khi chỉ mở các màn hình liên quan.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; cảnh báo hiện hữu của solution vẫn còn.
- Unit suite (loại integration): **657/657 pass, 0 skipped**.

## Cập nhật triển khai — Bellman, Rooming List và Ghi chú VIP — 08/09/2026

- `TodayArrivalsWindow` đã nối nút `Gửi Bellman` vào `IGuestMessageService.CreateAndSendAsync`, sử dụng `StayId`, khách và phòng của dòng đến thật; không còn chỉ hiện thông báo thành công giả.
- `GroupRoomingListWindow` đã nối `In danh sách` vào `IRoomingListService.ExportAsync` và bản xem trước in từ dữ liệu rooming list thật. Các thao tác Auto Pair/Swap/Split vẫn giữ fail-closed vì chưa có command backend tương ứng.
- `VipNotesWindow` đã giữ `GuestProfileId` từ repository, cho phép tạo ghi chú mới trên khách được chọn, cập nhật `GuestProfile.Notes` qua `IGuestProfileRepository.UpdateAsync`, rồi tải lại dữ liệu thật. Không còn chỉ thêm ghi chú trong bộ nhớ phiên.
- Các nút export/print của `IncidentLogWindow`, `ShiftHandoverWindow` và `ProformaFolioWindow` đã xuất/xem trước từ tập dữ liệu thật đang hiển thị; email Proforma và các command chưa có contract vẫn fail-closed.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite (loại integration): **657/657 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật triển khai — Loại bỏ mock chết và hoàn thiện xuất/in dữ liệu thật — 08/09/2026

- Đã loại bỏ ba helper dữ liệu mẫu không còn caller trong `TodayArrivalsWindow`, `TodayDeparturesWindow` và `CurrentGuestsWindow`; các màn hình này tiếp tục dùng dữ liệu Stay thật và không còn đường quay lại mock cũ.
- `IncidentLogWindow` đã xuất CSV và xem trước bản in từ danh sách sự cố thật sau khi áp dụng bộ lọc.
- `ShiftHandoverWindow` đã xuất CSV và xem trước báo cáo từ ca mở, khách, việc Housekeeping, sự cố và checklist thật; thiếu context/dữ liệu thì fail-closed.
- `ProformaFolioWindow` đã xuất CSV và xem trước bản in từ Folio thật cùng posting/payment/deposit/refund thật. Gửi email vẫn khóa vì chưa có email service.
- Tài liệu đoàn trong `CreateGroupWindow` vẫn hiển thị trạng thái chưa có contract lưu trữ thật và không tạo tài liệu giả.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**, 742 warnings hiện hữu của solution.
- Unit suite (loại integration): **657/657 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Đính chính kết quả audit mới nhất — 08/09/2026

> Phần này supersede các nhận định cũ hơn trong tài liệu khi chúng nói Group Booking hoặc Marketing chưa có persistence.

- `GroupBookingWizardWindow` đã dùng `IGroupReservationCommandService.ConfigureBookingAsync` với room-block/rate/property validation thật; không còn xếp vào nhóm mock đang chạy.
- `MarketingCenterWindow` đã dùng `IMarketingQueryService` và `IMarketingCommandService` qua `EfMarketingService` cho đọc/tạo/sửa/đổi trạng thái theo property; không còn xếp vào nhóm mock đang chạy.
- `TodayArrivalsWindow` và `TodayDeparturesWindow` đọc Stay thật. Các nút Check-in/chuẩn bị Check-out đã dùng workflow thật; các nút liên quan nay mở `RoomMoveExtendWindow`, `FolioWorkspaceWindow`, `StayProfileWindow`, `HousekeepingMessageWindow` hoặc in dữ liệu dòng đang chọn. Bellman và report service chuyên dụng vẫn fail-closed vì chưa có backend tương ứng.
- `CurrentGuestsWindow` đọc Stay thật; nút Nhắn nhà buồng nay mở `HousekeepingMessageWindow` dùng task/shift persistence thật. Thêm khách và các thao tác chưa có contract riêng vẫn fail-closed.
- `PaymentReceiptsView`, `VipNotesWindow`, `InvoiceManagementWindow` và `ShiftLogWindow` đã có export/in từ dữ liệu thật theo bộ lọc; không dùng dòng mẫu để báo hoàn tất.
- Các gap còn lại cần ưu tiên vẫn là: Group Checkout allocation Folio/payment/invoice, Housekeeping photo/batch/liên kết, Room Master tiện ích, Exchange Rate, provider e-invoice, payment allocation/reversal, các command Shift/Incident và provider nền tảng `Unavailable*`.

### Kiểm chứng vòng mới nhất

- Build `ASMILE.BO.App`: **PASS, 0 errors** (742 warnings hiện hữu).
- Unit suite không tích hợp: **657/657 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

## Đính chính audit — Group Booking và In nhật ký ca — 08/09/2026

- Kết luận cũ cho rằng Group Booking chưa có command persistence đã được supersede: `GroupBookingWizardWindow.SaveBookingAsync` hiện gọi `IGroupReservationCommandService.ConfigureBookingAsync` với `ConfigureGroupBookingRequest`, sau khi dựng room-block draft thật và kiểm tra room type/rate/property.
- `CanConfirm` vẫn chặn đúng khi thiếu giá, sai số phòng, trạng thái đoàn không phù hợp, thiếu xác nhận đặt cọc/rooming/policy hoặc lỗi tải dữ liệu; không tự tạo giá hay dữ liệu mẫu.
- `ShiftLogWindow` đã có cả xuất CSV và xem trước báo cáo in từ các dòng ca thật sau bộ lọc; thao tác tạo nhật ký riêng vẫn chặn vì chưa có command backend riêng.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 742 warning hiện hữu.
- Unit suite: **657/657 pass, 0 skipped**.

## Cập nhật — Xuất và xem trước in ghi chú VIP thật — 08/09/2026

- `VipNotesWindow` vẫn nạp ghi chú theo guest profile thật; đã bổ sung xuất CSV và xem trước báo cáo từ kết quả sau bộ lọc tìm kiếm/VIP/nhóm/ưu tiên/trạng thái.
- Khi không có dữ liệu thật, thao tác dừng với thông báo rõ; không sinh ghi chú hay báo cáo mẫu.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 742 warning hiện hữu.
- Unit suite: **657/657 pass, 0 skipped**.

## Cập nhật — Xuất phiếu thu thật từ PaymentReceiptsView — 08/09/2026

- `ReceiptVoucherWindow` khi có DI sẽ hiển thị `PaymentReceiptsView` với `PaymentReceiptsViewModel`; dữ liệu danh sách, tạo mới, duyệt, mở lại và hủy đều đi qua `IReceivableService` thật.
- Đã bổ sung nút xuất CSV cho danh sách đang hiển thị, dùng đúng các dòng đã lọc và `CsvExportHelper` với UTF-8 BOM. Không có dòng thật thì không tạo file.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 742 warning hiện hữu.
- Unit suite: **657/657 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Cập nhật — Xuất nhật ký ca thật theo bộ lọc — 08/09/2026

- `ShiftLogWindow` nạp ca thật bằng `ICashShiftService.GetShiftHistoryAsync` theo property hiện hành; danh sách không dùng fixture runtime.
- Nút `Xuất` đã được nối thành CSV UTF-8 từ kết quả lọc hiện tại, gồm thời gian, ca, khách/phòng, trạng thái, mô tả, VIP, khiếu nại và tiến độ công việc.
- Khi không có bản ghi, thao tác dừng với thông báo rõ; không sinh báo cáo giả. Nút tạo nhật ký và in vẫn chặn vì chưa có command/report backend tương ứng.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 742 warning hiện hữu của solution.
- Unit suite: **657/657 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Cập nhật — Task & Shift dùng persistence thật — 08/09/2026

- Đã loại bỏ fixture 3 công việc trong `TaskShiftViewModel`; danh sách, tạo mới, phân công, bắt đầu và hoàn thành đều đi qua `ITaskShiftService`/`EfTaskShiftService` và đọc lại từ `AppDbContext` theo `PropertyId`.
- Bổ sung mapping thật cho `fo.TaskAssignment` và `fo.TaskHistory`, lịch sử thao tác được lưu cùng công việc; UI đã sửa binding Priority và có trường nhập nhân sự/ca khi phân công.
- Tạo migration `20260908045220_AddTaskShiftPersistence`, chỉ gồm 2 bảng, khóa ngoại và index cần thiết. Migration chưa được áp dụng vào database vận hành.
- Đã xử lý lỗi EF coi lịch sử mới là bản ghi cũ khi aggregate tự sinh ID, gây `DbUpdateConcurrencyException`; service đánh dấu đúng các history row mới là `Added`.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- Test round-trip Task & Shift: **1/1 pass**, gồm create → assign → start → complete, lịch sử và property scope.
- Chưa chạy migration và chưa ghi production database.

## Cập nhật — Workforce bỏ fixture và lưu ca/khu vực thật — 08/09/2026

- `EfWorkforceQueryService` vẫn đọc danh sách nhân sự thật từ `People/Departments`, nhưng không còn gán cứng `Sáng`, `Online` hoặc dùng mã tòa/tầng của phòng ban như dữ liệu vận hành.
- Đã bỏ hoàn toàn fixture ngẫu nhiên 12 supervisor/60 housekeeper cùng assignment, KPI, timeline và audit giả khỏi `WorkforceViewModel`; các số liệu chưa có nguồn nghiệp vụ vẫn để rỗng/0.
- Bổ sung metadata ca, tòa, tầng và trạng thái trên `People`; đổi ca và đổi khu vực đi qua `IWorkforceCommandService`/`EfWorkforceCommandService`, sau đó tải lại từ EF để xác nhận round-trip.
- Tạo migration `20260908043516_AddWorkforceMetadataPersistence`. Migration mới chỉ thay đổi cột metadata trên `People`; chưa chạy trên database production.
- Bổ sung `WorkforcePersistenceTests`: ca/khu vực lưu và đọc lại đúng từ database test.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors** (752 warnings hiện hữu của solution).
- Unit suite: **656/656 pass, 0 skipped**.
- Chưa chạy migration và chưa ghi production database.

## Cập nhật — Minibar kiểm kê chi tiết dùng persistence thật — 08/09/2026

- Bổ sung `MinibarInventoryAudit` và `MinibarInventoryLine` trong schema `fo`, lưu theo property/phòng/stay/người thao tác/thời điểm; migration `20260908041554_AddMinibarInventoryPersistence` đã tạo nhưng chưa áp dụng production.
- `IMinibarCommandService.InventoryAsync` kiểm tra scope, phòng active, stay cùng phòng, số lượng không âm, tối thiểu một dòng và idempotency theo `InventoryId`.
- Màn hình Minibar đã có form thêm/sửa/xóa dòng kiểm kê, ghi chú, lưu thật rồi tải lại chi tiết từ EF; bảng phòng hiển thị lần kiểm kê gần nhất và số dòng. Nút In tạo CSV từ dữ liệu thật; các thao tác thiếu session/stay/Folio không còn fallback cập nhật bộ nhớ.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- Unit suite: **655/655 pass, 0 skipped**; test mới xác nhận lưu dòng chi tiết, đọc round-trip đầy đủ và idempotency.
- `migrations has-pending-model-changes`: **No changes**.
- `git diff --check`: không có lỗi whitespace. Không áp dụng migration và không ghi production database.

## Cập nhật — Workforce report không còn thao tác in giả — 08/09/2026

- `WorkforceViewModel.PrintReport` trước đây chỉ hiện MessageBox mô tả “In báo cáo”, không tạo đầu ra. Đã chuyển thành tạo báo cáo CSV từ danh sách nhân sự thật đang hiển thị, gồm mã nhân viên, vai trò, ca, vị trí, trạng thái, phân công, hoàn thành, KPI và xếp loại.
- Khi không có dữ liệu thật, thao tác dừng với trạng thái rõ ràng; không tạo báo cáo mẫu hoặc báo thành công giả.
- `ViewSchedule` vẫn giữ trạng thái chưa triển khai vì chưa có contract persistence lịch làm việc; không dựng lịch giả.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- Unit suite: **654/654 pass, 0 skipped**.
- Kiểm tra migration: **không có thay đổi model chưa có migration**; không áp dụng migration và không ghi production database.

## Cập nhật triển khai — UI Đồ vải & tiện nghi dùng dữ liệu thật — 08/09/2026

- Bổ sung màn hình `HousekeepingSupplyWindow` vào hub `Nhà buồng` với hai bảng tồn kho: Đồ vải và Tiện nghi trong phòng.
- ViewModel gọi trực tiếp `ILinenService`/`IAmenityService` production; service production là `EfHousekeepingSupplyService`, đọc theo property context từ các bảng persistence mới.
- Hiển thị số phòng thật từ `HotelRoom.RoomNumber`; chỉ dùng mã phòng rút gọn từ `RoomId` khi dữ liệu phòng không có số. Không tạo dòng mẫu khi database rỗng.
- Nút `Tạo yêu cầu cấp bù` chỉ hoạt động sau khi chọn dòng tiện nghi và gọi `CreateRefillRequestAsync`; chỉ tạo yêu cầu khi tồn kho thật dưới ngưỡng. Sau ghi thành công, màn hình tải lại từ DB.
- Migration `20260908032252_AddHousekeepingSupplyPersistence` vẫn chưa được áp dụng vào database thật; cần apply có kiểm soát trên database test trước khi smoke test UI.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors** (749 warnings hiện hữu của solution).
- Unit suite: **652/652 pass, 0 skipped**; test persistence kiểm tra tổng hợp tồn đồ vải, số phòng thật, tồn tiện nghi và tạo yêu cầu cấp bù dưới ngưỡng.
- `dotnet ef migrations has-pending-model-changes`: **No changes have been made to the model since the last migration**.
- `git diff --check`: không có lỗi whitespace; các cảnh báo LF/CRLF là cảnh báo line-ending của working tree.

### Bổ sung vận hành đồ vải — 08/09/2026

- Bảng Đồ vải trong màn hình mới đã có thao tác ghi nhận `Issued`, `Returned`, `Damaged`, `Discarded`, `Restocked` và `Washed` qua `ILinenService.RecordLinenTransactionAsync`; không cập nhật collection giả sau thao tác.
- Service kiểm tra mã đồ vải, property/phòng, action hợp lệ và từ chối xuất/hỏng/bỏ vượt tồn hiện tại bằng lỗi `HK_LINEN_STOCK_INSUFFICIENT`.
- Bổ sung test chống âm tồn; unit suite sau cập nhật đạt **653/653 pass, 0 skipped**.

## Cập nhật triển khai — Minibar cấp bù dùng persistence thật — 08/09/2026

- Nút `Bổ sung Minibar` trước đây chỉ thay đổi trạng thái trong bộ nhớ; nay gọi `IMinibarCommandService.RestockAsync` và ghi append-only vào `fo.MinibarRestockTransaction`.
- Command kiểm tra property, phòng active, actor, số lượng dương và idempotency theo `TransactionId`; lỗi không làm UI báo thành công giả.
- Nút `Kiểm kê Minibar` chưa có model dòng kiểm kê chi tiết nên đã chuyển sang fail-closed, hiển thị rõ chưa có contract persistence thay vì ghi thành công giả.
- Tạo migration `20260908035245_AddMinibarRestockPersistence`; chưa áp dụng vào database thật.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite: **654/654 pass, 0 skipped**; test mới kiểm tra transaction cấp bù được ghi thật và gọi lặp không tạo bản ghi trùng.

### Bổ sung round-trip và export — 08/09/2026

- Query Minibar đọc tổng số lượng cấp bù đã ghi từ `fo.MinibarRestockTransaction`, nên refresh không mất dấu vết nghiệp vụ.
- Export Minibar, Lost & Found và Workforce đã chuyển từ MessageBox mô tả sang file CSV thực tế từ các collection dữ liệu thật; trường hợp rỗng/mất nguồn dữ liệu không xuất fixture.
- Unit suite sau kiểm tra round-trip cấp bù vẫn đạt **654/654 pass, 0 skipped**.

## Cập nhật triển khai — Linen/Amenity chuyển sang persistence thật — 08/09/2026

- Đã xác minh `LinenService` và `AmenityService` trong `HousekeepingEngine.cs` là fixture cũ: chỉ được test trực tiếp ở `HousekeepingPlatformTests`, không được đăng ký trong production DI.
- Đã bổ sung `EfHousekeepingSupplyService`, đăng ký `ILinenService` và `IAmenityService` trỏ vào EF service. Các truy vấn đều lọc theo `PropertyId`, phòng đang hoạt động và tầng; thiếu dữ liệu thật trả danh sách rỗng, không sinh `Guid`, `Random` hoặc mốc thời gian giả.
- Linen dùng master data + ledger giao dịch append-only; số lượng, số lần giặt, số hư hỏng và lần nhập gần nhất được tính từ giao dịch đã lưu. Giao dịch trùng `TransactionId` được xử lý idempotent.
- Amenity dùng master data + tồn theo phòng + yêu cầu cấp bù và chi tiết yêu cầu. Chỉ tạo yêu cầu khi tồn thật ở dưới ngưỡng; không có tồn thật thì trả yêu cầu rỗng và không ghi bản ghi giả.
- Bổ sung migration `20260908032252_AddHousekeepingSupplyPersistence`, gồm 6 bảng thuộc schema `fo`, index unique theo cơ sở/phòng/mã và foreign key về phòng. Migration mới chỉ được sinh, chưa áp dụng vào database.

### Kiểm chứng sau cập nhật

- `dotnet build ASMILE.BO.App --no-restore`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- Unit suite: **652/652 pass, 0 skipped** (gồm 2 test persistence Linen/Amenity mới).
- `dotnet ef migrations has-pending-model-changes`: **No changes have been made to the model since the last migration**.
- Quét call-site: production chỉ còn DI tới `EfHousekeepingSupplyService`; các `new LinenService()`/`new AmenityService()` còn lại chỉ nằm trong test legacy.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật triển khai — Housekeeping mở Reservation/Room History từ dữ liệu thật — 08/09/2026

- `HousekeepingRoomStatusViewModel.OpenReservation` đã được nối `IReservationRepository.SearchAsync` theo `RoomId`, lọc Reservation còn hiệu lực theo business date, rồi mở `ReservationStatusWindow` với Reservation thật. Khi không có Reservation hoặc thiếu `RoomId`, hệ thống báo rõ và không tạo dữ liệu giả.
- `OpenRoomHistory` đã được nối `IAuditService.QueryAsync` theo `HotelRoom` và `RoomId`; chỉ mở preview khi có audit thật. Không có audit thì thông báo “chưa có audit thật”, không dựng lịch sử mẫu.
- Timeline chi tiết phòng đã bỏ các mốc giờ dựng sẵn (07:00, +30 phút, +1 giờ 15 phút...). Chỉ dùng `ArrivalDate`, `DepartureDate`, `DirtySince`, `LastCleaned`, Inspector đã lưu; snapshot chỉ có trạng thái mà chưa có timestamp sẽ hiển thị “—”.
- DI Housekeeping đã bổ sung `IReservationRepository`, `IAuditService` và `IServiceProvider` để các thao tác trên resolve đúng repository/service thật.

### Kiểm chứng sau vòng này

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 744 warning legacy hiện hữu.
- Full unit suite: **650/650 pass, 0 skipped**, duration khoảng 29 giây.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật triển khai — Kích hoạt xuất/in từ dữ liệu thật và hoàn thiện DI payment — 08/09/2026

- `GroupServiceViewModel`: `In dịch vụ đoàn`, `Xuất Excel` và `Xuất CSV` dùng `FilteredServices` đã tải từ `IGroupServiceOrderQueryService`/EF. `Xuất Excel` xuất CSV UTF-8 tương thích Excel; `In` mở preview từ đúng bộ lọc hiện tại. Không có dòng thật thì chỉ thông báo, không tạo file giả.
- `HousekeepingRoomStatusViewModel`: `Xuất Excel` và `In báo cáo` dùng `FilteredRooms` từ `IHousekeepingBoardService`/snapshot thật, kèm FO, Housekeeping, khách và ngày lưu trú. Email/PDF và các lệnh chưa có hạ tầng vẫn fail-closed.
- `GroupReservationViewModel`: `In đoàn` và `Xuất Excel` dùng `FilteredGroups` từ query đoàn thật, gồm tiến độ phòng/khách, doanh thu, đặt cọc và công nợ.
- Payment DI đã bổ sung `IHotelPaymentNumberGenerator -> SqlHotelPaymentNumberGenerator`; các màn hình Cashier/Receipt/Refund hiện resolve được payment application service đầy đủ thay vì fail-closed do thiếu dependency.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors**; cảnh báo legacy hiện hữu vẫn còn.
- Full unit suite: **650/650 pass, 0 skipped** sau khi bắt và sửa lỗi DI payment.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật triển khai — Room Master xuất/in dữ liệu thật — 08/09/2026

- `RoomMasterViewModel.ExportExcel` không còn chỉ hiển thị thông báo khóa; nút này xuất các phòng thật đang hiển thị theo property/bộ lọc hiện tại ra CSV tương thích Excel.
- `RoomMasterViewModel.PrintReport` tạo nội dung xem trước từ chính các dòng Room Master thật đang tải, qua `PrintPreviewDialog`; không tạo báo cáo từ fixture hoặc dữ liệu suy diễn.
- Khi thiếu repository/context hoặc không có dòng thật, cả hai thao tác vẫn fail-closed và không tạo file/bản in giả.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite sau cập nhật: **650/650 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Cập nhật triển khai — Kích hoạt Settlement và phát hành hóa đơn nội bộ — 08/09/2026

- `IssueInvoiceWizard` đã có luồng đọc Folio thật và backend `SettlementApplicationService`, nhưng trước đây `SettlementApplicationService`, `EfHotelInvoiceRepository` và `SqlHotelInvoiceNumberGenerator` chưa được đăng ký trong DI; vì vậy phát hành hóa đơn không thể đi tới command thật.
- Đã đăng ký repository, number generator, application service và các handler settle/close/generate/print/query invoice. Không bật provider hóa đơn điện tử giả; phần provider ngoài vẫn fail-closed khi chưa có adapter/cấu hình thật.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite sau cập nhật: **650/650 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Cập nhật triển khai — Kích hoạt payment service thật trong DI — 08/09/2026

- Đã xác minh `CashierPaymentWindow`, `CreateReceiptVoucherWindow` và `RefundManagementWindow` đều có luồng EF thật, nhưng trước đó `HotelPaymentApplicationService` chưa được đăng ký trong `AddInfrastructure`; vì vậy các màn hình nhận `null` và tự fail-closed dù repository/payment domain đã tồn tại.
- Đã đăng ký `HotelPaymentApplicationService` cùng toàn bộ command/query handler của Payment (receive, split tender, authorize, capture, complete, cancel, allocate, refund, void và các query payment) vào production DI.
- `PaymentReceiptsViewModel` vẫn dùng `EfReceivableService` thật cho phiếu thu công nợ; không dùng `MockReceivableService` trong production DI. Phân bổ thanh toán tại Thu ngân gọi `HotelPaymentApplicationService.AllocateAsync` và ghi allocation thật vào payment repository.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; chỉ còn warning hiện hữu của solution.
- Unit suite: **650/650 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Kiểm tra lại đường chạy mock — 08/09/2026

Đã re-index code graph `D-ASMILE-BO` và kiểm tra caller của các điểm có `Random`, `LoadMockData`, `Task.FromResult` và các nút báo thành công/đã xuất/in.

### Phân loại để không sửa nhầm

- Các generator `Random` còn trong `RoomMasterViewModel`, AR và một số ViewModel Housekeeping chỉ nằm sau nhánh đã `return` khi dùng repository thật hoặc sau nhánh fail-closed. Đây là legacy reference, không phải nguồn dữ liệu runtime hiện tại.
- Các helper dựng danh sách trong `CheckoutPreparationWindow`, `ExpressCheckoutWindow`, `FolioReviewWindow`, `QuickCheckoutWindow`, `MergeFolioWindow`, `IssueInvoiceWizard`, `TransactionCenterWindow`, `CashierVoucherWindow`, `CurrentGuestsWindow`, `TodayArrivalsWindow` và `TodayDeparturesWindow` hiện không có caller. Các màn hình đã chuyển sang collection nạp từ Stay/Folio/Reservation thật hoặc để rỗng; cần dọn code sau smoke test, không cần nối database cho helper chết.
- Các engine cũ `CashierEngine`, `ConciergeEngine`, `ChannelEngine`, `MaintenanceEngine`, `GroupEngine` và `DoorLockEngine` trả DTO dựng sẵn nhưng không được đăng ký trong production DI. Production đang đăng ký các service EF tương ứng; không xem các engine cũ là bằng chứng đường chạy hiện tại.
- Các service `Unavailable*`/`SafeDefault*` trong DI là trạng thái chưa có năng lực nền tảng (backup, printer, service status, system/security settings), không phải dữ liệu mẫu. Cần triển khai provider thật theo phạm vi nền tảng riêng.

### Điểm còn mock hoặc placeholder đang có thể nhìn thấy trên UI

1. **Group Booking/Rooming List/Group Guest**: wizard tạo booking đang chủ động để rỗng vì chưa có command persistence; xác nhận Rooming List và import khách đoàn đang bị khóa. Đây là gap nghiệp vụ thật, ưu tiên cao.
2. **Export/Print/Email**: Group, Housekeeping status/inspection/cleaning, Room Master, Workforce, AR receipt, Invoice, Proforma, Shift, VIP Notes và một số màn hình Key-card vẫn chỉ báo “chưa có service” hoặc MessageBox mô phỏng. Không được coi thao tác đã hoàn tất.
3. **Lost & Found, Minibar, Workforce, KeyCard Audit**: dữ liệu danh sách đã có đường đọc thật, nhưng nút in/xuất hoặc xem báo cáo hiện vẫn chỉ hiển thị MessageBox, chưa tạo file hoặc print job từ snapshot dữ liệu thật.
4. **Room Detail/Locked Room**: đọc dữ liệu thật nhưng sửa hồ sơ phòng, khóa/mở/gia hạn phòng và lịch sử khóa chưa có command/query persistence đầy đủ.
5. **Folio tiện ích**: Receipt Voucher/Proforma còn thiếu query thật; Split/Merge đã có guard và command Folio thật nhưng phải có Folio đích thật trong database; routing đã có cấu hình/query thật nhưng chưa được tự động áp dụng vào posting.
6. **Đối soát và chứng từ**: AR allocation/reversal mở rộng, invoice export/email, payment allocation và document provider vẫn chưa hoàn thiện; các nút hiện fail-closed hoặc báo unavailable.

### Kết luận ưu tiên

- **P0 — hoàn thiện trước khi UAT**: Group Booking persistence, Rooming List mutation, Room Detail/Locked Room command, Folio Receipt/Proforma query, và routing application vào Posting Engine.
- **P1 — sau khi nghiệp vụ ổn định**: một export/print pipeline dùng snapshot query thật, sau đó dùng chung cho Housekeeping, Group, Room Master, Workforce, Lost & Found, Minibar, Shift, Invoice và Key-card.
- **P2 — dọn kỹ thuật**: xóa helper mock không có caller và các engine DTO legacy sau khi có smoke test xác nhận production DI không tham chiếu chúng.

### Bằng chứng hiện tại

- Production DI đăng ký repository/service EF cho Stay, Reservation, Folio, Payment/Cash Shift, Exchange Rate, Marketing, Key-card, Group, Maintenance, Door Lock và Concierge.
- Full unit suite gần nhất: **648/648 pass, 0 skipped**; build App/Infrastructure không có error.
- Không chạy migration và không ghi production database trong đợt kiểm tra này.

## Cập nhật triển khai — Folio routing và xuất/in dữ liệu thật — 08/09/2026

- Bổ sung aggregate `HotelFolioRoutingRule` với property scope, Folio nguồn/đích, loại khoản mục, tỷ lệ, độ ưu tiên, trạng thái active, ghi chú, audit timestamp và rowversion.
- Bổ sung `IFolioRoutingQueryService`/`IFolioRoutingCommandService` và `EfFolioRoutingService`: truy vấn theo property, kiểm tra Folio nguồn/đích tồn tại, chống rule active trùng, cập nhật có rowversion và ghi audit.
- Folio Workspace đã đọc routing rule từ service thật; nếu property chưa có rule thì hiển thị rõ “Không có routing rule”, không dựng danh sách mẫu. Inspector lọc theo đúng mã Folio nguồn.
- Migration mới: `20260908014449_AddFolioRoutingRules`; đã cập nhật model snapshot. Migration chỉ được tạo, chưa chạy vào database.
- Nút **Xuất** của Folio Workspace đã xuất CSV UTF-8 từ các giao dịch thật đang lọc, có escape dữ liệu CSV và hộp chọn file. Nút **In** tạo statement phân trang từ ledger thật qua WPF print pipeline; khi không có dữ liệu vẫn fail-closed.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning nền của solution không chặn build.
- Full unit suite sau khi bổ sung routing domain tests: **648/648 pass, 0 skipped**.
- `dotnet ef migrations has-pending-model-changes`: **No changes have been made to the model since the last migration**.
- Không chạy `database update`, không ghi production database và không đổi connection string.

### Khoảng trống còn lại

- Routing hiện đã có cấu hình/query thật nhưng chưa tự động áp dụng rule vào từng posting; bước tiếp theo là nối posting engine với rule đã duyệt, kèm idempotency và kiểm thử allocation.
- Email/chứng từ điện tử và export các module khác vẫn cần document/provider contract riêng; không dùng CSV Folio làm bằng chứng rằng mọi module đã có export.

## Rà soát mock sâu lần 2 — 08/09/2026

Đã quét lại `ASMILE.BO.App` bằng code graph, đối chiếu caller của từng hàm tạo dữ liệu và đọc nhánh `Loaded/DI -> service/repository -> DbContext` thay vì chỉ tìm chuỗi `Random`.

### Kết quả phân loại

| Nhóm | Kết quả | Kết luận |
|---|---:|---|
| Điểm `new Random(...)` trong App | 38 | 15 nằm trong các ViewModel có đường đọc thật và bị chặn bởi `return`, `#if false` hoặc nhánh legacy không thể chạy; 23 nằm trong helper View cũ không có caller production theo graph. |
| Folio Workspace | Đã khóa generator cũ | Constructor/Loaded dùng `IHotelFolioRepository`; Folio, ledger entry, payment, deposit, refund đọc từ DB theo property context. |
| Tách Folio | Đã khóa generator cũ | Dùng `HotelFolioApplicationService.SearchAsync`; item/rowversion lấy từ ledger thật. Nếu thiếu service/context hoặc query lỗi thì rỗng và báo lỗi. |
| Chuyển posting | Đã khóa generator cũ | Posting và Folio đích lấy từ ledger thật; ghi dùng `AddFolioTransferCommand` với rowversion. |
| Proforma Folio | Đã khóa generator cũ | Dùng `IHotelFolioRepository.SearchAsync/GetLedgerAsync`; không tạo proforma/payment/deposit/refund mẫu. |
| Receipt Voucher | Đã khóa generator cũ | Cửa sổ cũ không còn compile generator 3.000 phiếu mẫu; route production là `PaymentReceiptsViewModel`. Tạo phiếu gọi `HotelPaymentApplicationService.ReceiveAsync`. |
| AR/Housekeeping/Room Master/Minibar/Lost & Found | Không còn mock chạy trên nhánh đã kiểm tra | Các ViewModel có query service thật; thiếu service/property thì hiển thị rỗng hoặc lỗi, không tự sinh dòng. Các đoạn generator legacy còn lại chỉ là mã tham chiếu chưa được xóa hẳn. |
| Channel/Executive/Cashier/Concierge engine mẫu cũ | Không có route production/DI tương ứng | Không dùng làm bằng chứng dữ liệu thật; tiếp tục giữ ngoài đường chạy cho tới khi có contract + persistence riêng. |

### Thay đổi chống hồi quy

- Bao các generator chết của `ReceiptVoucherWindow`, `SplitFolioWindow`, `TransferPostingWindow`, `ProformaFolioWindow` và `FolioWorkspaceWindow` bằng `#if false`, kèm chú thích rõ route dữ liệu thật. Như vậy fallback DI lỗi không thể quay lại hiển thị dữ liệu giả.
- Không bật fallback mẫu để che thiếu migration, thiếu property context hoặc thiếu query/command. UI tiếp tục fail-closed.
- Không chạy migration, không ghi production database và không đổi connection string.

### Kiểm chứng

- `dotnet build ASMILE.BO.App/ASMILE.BO.App.csproj --no-restore`: **PASS, 0 errors**; 745 warnings hiện hữu của solution.
- `dotnet test ASMILE.BO.Tests.Unit/ASMILE.BO.Tests.Unit.csproj --no-build --no-restore`: **644/644 pass, 0 skipped**.
- Graph đã re-index sau thay đổi: **51.636 nodes / 153.739 edges**.

### Phần còn cần hoàn thiện để đạt mức dùng thật đầy đủ

1. Các tiện ích chưa có backend riêng: export/print/email, routing và lịch sử chứng từ; allocation payment đã có đường service thật, còn đối chiếu allocation mở rộng vẫn thiếu.
2. Các màn hình legacy không có caller production nhưng còn helper mock nguồn: Transaction Center, Cashier Voucher, Checkout Preparation, Express/Quick Checkout, Folio Review, Issue Invoice, Merge Folio và một số màn hình Housekeeping/Today Arrivals/Departures. Cần smoke test navigation trước khi xóa hẳn helper.
3. Các engine mẫu không đăng ký DI cần được xóa hoặc thay bằng persistence service khi triển khai đúng module tương ứng; hiện không được phép đưa vào đường chạy chỉ để có dữ liệu hiển thị.

## Cập nhật triển khai — Hoàn tác Folio trên đường chạy thật — 08/09/2026

- Backend `HotelFolioApplicationService.ReverseEntryAsync` và `ReverseFolioEntryCommandHandler` đã có kiểm tra quyền `FrontOffice.Folio.Reverse`, property context, `EntryId`, rowversion 8 byte, trạng thái đã hoàn tác và commit giao dịch correction.
- `FolioWorkspaceWindow` trước đây chỉ đọc ledger thật nhưng không có caller cho `ReverseEntryAsync`. Đã nối UI tab **Audit** với service thật; mỗi dòng ledger giữ `HotelFolioId`, `HotelFolioEntryId` và `Folio.RowVersion` lấy từ `FolioLedgerDto`.
- Thao tác yêu cầu xác nhận và nhập lý do, tạo correction qua application service, không xóa giao dịch gốc; sau khi thành công tải lại ledger từ DB. Nếu thiếu service/context/rowversion thật thì nút không bật và không có fallback mock.
- Vì vậy phần **reversal Folio** không còn là gap UI mock. Các gap Folio còn lại là routing query, xuất/in/email/chứng từ và đối chiếu allocation mở rộng.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App/ASMILE.BO.App.csproj --no-restore`: **PASS, 0 errors**; 744 warnings nền của solution.
- Full unit suite: **644/644 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không đổi connection string.

## Cập nhật triển khai — Concierge mở rộng dùng dữ liệu thật — 08/09/2026

- Ba đường chạy trước đây trả `Task.FromResult`/DTO mẫu trong `ConciergeEngine` đã được thay bằng service EF và đăng ký DI: wake-up call, hành lý và vận chuyển.
- Bổ sung các bảng có khóa cơ sở `PropertyId`: `fo.CuocGoiBaoThuc`, `fo.HanhLyConcierge` và `fo.DatXeConcierge`; có index theo trạng thái/thời gian và số nghiệp vụ duy nhất theo cơ sở.
- Wake-up call đã có schedule, complete theo kết quả, retry có kiểm tra trạng thái, danh sách theo ngày và dashboard tính từ bản ghi thật. Hành lý có store/deliver/collect; vận chuyển có book/complete/upcoming.
- Dashboard Concierge không còn số cố định: open/assigned/completed request lấy từ `fo.YeuCauDichVu`, còn wake-up, hành lý và vận chuyển lấy từ ba bảng mới. SLA/rating/escalation chưa có nguồn dữ liệu đủ nên giữ `0`/rỗng, không dựng số.
- Tất cả truy vấn/command đều kiểm tra context và lọc `PropertyId`; test đã kiểm tra dữ liệu của cơ sở khác không bị lẫn.
- Migration mới: `20260908170000_AddConciergePersistence` (**Pending**). Chưa chạy migration và chưa ghi production database.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.Infrastructure`: **PASS, 0 errors**.
- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Test Concierge persistence: **1/1 pass**.
- Full unit suite: **644/644 pass, 0 skipped**.

### Khoảng trống còn lại sau rà soát Concierge

- `ConciergeEngine.cs` vẫn được giữ làm legacy/test fixture nhưng không còn đăng ký production DI; không dùng làm bằng chứng dữ liệu thật.
- Các phần còn cần triển khai theo audit: Channel/OTA, Executive dashboard, Concierge các nghiệp vụ mở rộng ngoài wake-up/hành lý/vận chuyển, Linen/Amenity và adapter phần cứng khóa cửa.
- Trước khi bật migration ở môi trường dùng thật, cần preflight đúng database, backup/verify và smoke test bằng dữ liệu test có phân quyền; không dùng `dotnet ef database update` tùy tiện trên production.

## Rà soát sâu mock sau khi nối Concierge — 08/09/2026

Đã quét lại `Task.FromResult` trong Infrastructure (213 raw matches, 208 function results) và truy vết inbound caller, không kết luận chỉ từ tên file:

| Nhóm | Kết quả đường chạy | Kết luận |
|---|---|---|
| Channel/OTA (`ChannelEngine`) | Không có production caller/DI registration; caller hiện có chỉ là test | Mock fixture chưa chạy production. Cần triển khai khi mở màn hình Channel/OTA, không sửa mù ở vòng này |
| Executive (`ExecutiveEngine`) | Không có production caller/DI registration; KPI/dashboard hiện chỉ được gọi bởi test | Chưa được phép hiển thị số mẫu; cần thay bằng query tổng hợp khi có route thật |
| Cashier legacy (`CashierEngine`) | Caller graph của các shift/dashboard mock chỉ là test; DI đang dùng `EfCashier*`/`EfCashReconciliation*` | Giữ ngoài đường production, không đánh đồng với Cashier real |
| Concierge legacy (`ConciergeEngine`) | Đã bị thay khỏi DI bởi `EfWakeUpCallService`, `EfLuggageService`, `EfTransportationService`, `EfConciergeDashboardService` | Legacy fixture, không còn nguồn dữ liệu production |
| Housekeeping/Maintenance legacy engines | Caller mock chỉ xuất hiện ở test hoặc không có production registration; đường thật là các `Ef*` service đã test | Không bật lại fallback mock; tiếp tục hoàn thiện các phần còn thiếu như Linen/Amenity/Inspection mở rộng |

Các điểm có rủi ro thực tế còn lại nằm nhiều hơn ở UI cũ (`ReceiptVoucherWindow`, `SplitFolioWindow`, `TransferPostingWindow`, các tiện ích export/print/email) và các service nền có tên `Mock*`. Mỗi điểm phải được trace tiếp từ View/VM → DI → service → DbContext/API trước khi thay; nếu thiếu contract/schema thì phải fail-closed, không thay bằng số mẫu.

## Cập nhật — Door Lock device/event chuyển sang persistence thật — 08/09/2026

- Bổ sung aggregate và bảng EF `fo.ThietBiKhoaCua` và `fo.SuKienKhoaCua`; query thiết bị, offline/low-battery, event thất bại, security alert và dashboard hiện đọc theo `PropertyId` từ database thật.
- Đăng ký `EfDoorLockService`, `EfDoorEventService` và `EfDoorLockDashboardService` trong DI; test SQLite xác nhận thiết bị, event, cảnh báo và số liệu dashboard không còn lấy từ danh sách mẫu.
- `GenericLockAdapter` không còn trả thành công giả cho encode/revoke/sync/ping/emergency open; khi chưa có driver hãng, các thao tác này fail-closed. Tích hợp phần cứng thực tế vẫn là hạng mục riêng cần triển khai theo vendor.
- Tạo migration `20260908160000_AddDoorLockPersistence`; migration đang **Pending**, chưa chạy trên database và không ghi production.

### Kiểm chứng

- Build `ASMILE.BO.App` và `ASMILE.BO.Infrastructure`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- `DoorLockPersistenceTests`: **1/1 pass**.
- Full unit suite cần chạy lại sau khi chốt migration; không chạy migration trên database thật.

## Rà soát cuối — phân loại mock theo đường chạy thực tế — 08/09/2026

### Đã xác nhận là dữ liệu thật hoặc fail-closed

- Reservation, Stay, Room Master, Folio, Split/Merge/Transfer Folio, Proforma, Transaction Center, Express/Quick Checkout, Extend Stay, Check-in/Check-out và các màn hình đến/đi/khách đang ở đều có nhánh đọc repository theo property context. Các hàm `LoadMockData`/`Random` còn nằm trong source là fixture cũ sau nhánh production hoặc fallback không được gọi khi service thật đã đăng ký.
- Cashier shift/dashboard/reconciliation, Receipt/Voucher thông qua `PaymentReceiptsViewModel`/`PaymentVouchersViewModel`, Group, Housekeeping board/workflow, Lost & Found, Minibar, Workforce, Maintenance work order/asset/preventive, Incident, Rate/Marketing, Exchange Rate, Guest Message, Key Card và Door Lock query đã có service EF hoặc fail-closed tương ứng trong DI.
- Door Lock chỉ mới hoàn tất persistence/query thiết bị-event-dashboard. Encode/revoke/sync/ping/emergency open vẫn dừng an toàn vì chưa có driver/vendor adapter thật; không được coi là đã tích hợp phần cứng.

### Còn mock hoặc chưa có nguồn dữ liệu thật — cần triển khai tiếp

1. **Channel/OTA**: `ChannelEngine` vẫn là các service `Task.FromResult`/mẫu Booking.com; chưa có repository, bảng mapping, lịch sử đồng bộ, retry/dead-letter và adapter OTA thật. Đây là hạng mục ưu tiên cao nếu cần vận hành Booking/Agoda/Expedia.
2. **Executive/Dashboard quản trị**: `ExecutiveEngine` hiện trả KPI/layout/alert dựng sẵn và chưa có đăng ký DI production; cần query tổng hợp từ Stay, Room, Folio, Revenue/Night Audit và quyền theo property.
3. **Concierge mở rộng**: Wake-up call, Luggage và Transportation còn service mẫu, chưa có persistence/command/query. `Service Request` chính đã có EF; không gộp nhầm hai phạm vi này.
4. **Housekeeping mở rộng**: Linen/Amenity còn service mẫu; Board, workflow, inspection, Lost & Found, Minibar và workforce đã có đường thật. Cần bảng tồn kho linen/amenity, cấp phát/thu hồi, refill request và audit.
5. **Door Lock hardware**: cần vendor adapter thật, credential/key lifecycle integration, polling/webhook event ingestion và cơ chế retry/health check; bảng thiết bị/event đã sẵn sàng làm persistence seam.

### Không phải mock đang chạy, nhưng nên dọn sau khi smoke test

- Các generator `Random` trong `TransactionCenterWindow`, `SplitFolioWindow`, `ProformaFolioWindow`, `TransferPostingWindow`, `CheckoutPreparationWindow`, `FolioReviewWindow`, `ExpressCheckoutWindow`, `QuickCheckoutWindow`, `ReceiptVoucherWindow`, `CashierVoucherWindow` và các `LoadMockData` của Arrival/Departure/Current Guests/Housekeeping Message/Locked Room vẫn tồn tại như mã legacy. Constructor/Loaded hiện nạp repository/ViewModel thật hoặc để rỗng fail-closed; nên xoá sau khi có smoke test UI để tránh người sửa sau này gọi nhầm.
- Các lớp mock trong `CashierEngine`, `MaintenanceEngine`, `HousekeepingEngine`, `GroupEngine`, `DoorLockEngine`, `ConciergeEngine`, `ExecutiveEngine`, `ChannelEngine` hiện không phải implementation được DI production chọn. Không xoá vội trước khi hoàn tất kiểm tra test/legacy reference; chỉ xoá trong một đợt dọn mã riêng.

### Kết luận và thứ tự triển khai đề nghị

1. Chạy migration theo quy trình có backup trên database test: `AddCashReconciliation`, `AddMaintenanceAssetsAndPreventive`, `AddDoorLockPersistence`; xác nhận schema/index/property scope rồi mới cân nhắc production.
2. Làm Channel/OTA persistence và adapter trước nếu mục tiêu là vận hành đặt phòng đa kênh; nếu chưa có OTA trong phạm vi go-live thì làm Concierge mở rộng và Linen/Amenity trước.
3. Làm Executive query thật sau khi Night Audit/Revenue snapshot đã có dữ liệu ổn định, tránh dựng KPI mới trên các nguồn còn thiếu.
4. Bổ sung hardware Door Lock theo vendor cụ thể, giữ fail-closed cho tới khi có credential và test sandbox thiết bị.
5. Sau mỗi nhóm: thêm migration idempotent, test SQLite/property isolation/concurrency, smoke test UI với dữ liệu thật và chỉ sau đó xoá fixture legacy.

### Kiểm chứng vòng audit

- Build `ASMILE.BO.App` và `ASMILE.BO.Infrastructure`: **PASS, 0 errors**.
- Full unit suite loại Integration: **643/643 pass, 0 skipped**.
- Codebase graph đã re-index sau thay đổi: **51,505 nodes / 152,554 edges**.
- Các migration mới nhất đều đang **Pending**; không chạy migration, không ghi production database và không thay connection string.

## Cập nhật — Maintenance Asset/Preventive chuyển sang persistence thật — 08/09/2026

- Bổ sung aggregate vận hành riêng cho tài sản bảo trì, không dùng `FixedAsset` của kế toán thay thế: `MaintenanceAsset` và `MaintenanceAssetFailure` lưu tài sản, tình trạng, số lần hỏng và lịch sử sự cố theo `PropertyId`.
- Bổ sung `PreventiveMaintenancePlan`, checklist và `PreventiveMaintenanceSchedule`; các query kế hoạch, lịch, quá hạn và tài sản theo phòng đã đọc từ EF thật, có property scope.
- `EfMaintenanceAssetService` và `EfPreventiveMaintenanceService` đã được đăng ký trong DI production; `MaintenanceEngine` còn dữ liệu mẫu nhưng không còn là implementation runtime của hai contract này.
- Ghi nhận sự cố cập nhật tài sản, thêm lịch sử append-only, ghi audit trong cùng persistence boundary; bản ghi history có GUID sinh phía client được đánh dấu `Added` tường minh để tránh EF suy diễn sai trạng thái.
- Tạo migration `20260908150000_AddMaintenanceAssetsAndPreventive` với 5 bảng: `fo.TaiSanBaoTri`, `fo.LichSuHongHocBaoTri`, `fo.KeHoachBaoTriDinhKy`, `fo.ChiTietKeHoachBaoTri`, `fo.LichBaoTriDinhKy`. Migration đang **Pending**, chưa apply database thật.

### Kiểm chứng sau khi nối Maintenance

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution không phát sinh lỗi build.
- Test persistence Maintenance Asset/Preventive trên SQLite: **1/1 pass**.
- Full unit suite loại Integration: **642/642 pass, 0 skipped**.
- Danh sách migration xác nhận `20260908150000_AddMaintenanceAssetsAndPreventive` đứng sau `20260908130000_AddCashReconciliation`.
- Không chạy `dotnet ef database update`, không ghi production database, không thay connection string.

### Phân loại mock còn lại sau vòng này

| Nhóm | Trạng thái hiện tại | Phương án tiếp theo |
|---|---|---|
| Door Lock device/event | Còn mock legacy, chưa có adapter hãng và persistence event/sync | Chốt provider, credential, command issue/disable/revoke và audit trước khi bật UI |
| Channel/OTA | Còn mock legacy, chưa có schema/DI production | Làm credential, mapping, sync queue/dead-letter và import reservation idempotent |
| Housekeeping legacy Linen/Amenity | Engine cũ còn DTO mẫu; board/workflow hiện tại đã đi EF | Chỉ tách triển khai khi chốt nghiệp vụ và schema; không đăng ký engine mẫu |
| Concierge ngoài Service Request | Wake-up/luggage/transport còn thiếu persistence thật | Tách từng aggregate, query/command và audit; không dùng chung Service Request tùy tiện |
| Group tiện ích | Core/group service/room block đã có lát cắt thật; folio/deposit/invoice/report/export còn thiếu | Rà từng cửa sổ, nối dữ liệu thật hoặc giữ fail-closed theo từng contract |
| Shift/Incident/VIP phụ trợ | Một số màn hình rỗng/fail-closed, không báo thành công giả | Bổ sung schema/query/command theo vertical slice; ưu tiên Incident/Shift trước dashboard VIP |

Kết luận: Maintenance Asset/Preventive không còn là mock trên đường DI production; phần còn lại cần tiếp tục theo từng vertical slice, không chỉ xóa các class legacy có DTO mẫu.

## Rà soát toàn bộ mock sau khi nối Cashier — 08/09/2026

### Kết luận đường chạy production

- Cashier dashboard, đối soát ca và duyệt variance hiện đã dùng `CashierShifts`, `CashMovements` và bảng mới `fo.DoiSoatTienMat` qua `EfCashierDashboardService`/`EfCashReconciliationService`; DI không còn trỏ vào implementation dựng số liệu.
- Service Request, Message Center và Maintenance Work Order đã có đường EF thật và được đăng ký trực tiếp trong `AddInfrastructure`.
- Các lớp cũ `CashierEngine`, `ConciergeEngine`, `GroupEngine`, `MaintenanceEngine`, `ChannelEngine` vẫn còn `Task.FromResult`, `Guid.NewGuid()` và DTO mẫu, nhưng không được đăng ký vào DI production hiện tại. Đây là legacy/test seam, không phải dữ liệu đang hiển thị ở các ViewModel đã kiểm chứng. Không nên xoá ngay trước khi chạy smoke test các entry point cũ.

### Các mock còn tồn tại và mức độ ảnh hưởng

| Nhóm | Bằng chứng | Phân loại | Việc tiếp theo |
|---|---|---|---|
| Door Lock device/event/dashboard | `FrontOffice/DoorLock/DoorLockEngine.cs` dùng `SampleDevice`, số liệu dashboard cố định | Mock legacy, chưa có đăng ký `IDoorLockService`/dashboard trong DI | Tạo persistence cho thiết bị, sự kiện, trạng thái đồng bộ và adapter hãng trước khi bật UI quản lý khóa |
| Channel/OTA | `FrontOffice/Channel/ChannelEngine.cs` trả Booking.com, mapping, sync result và OTA KPI dựng sẵn | Mock legacy, chưa có schema/DI production | Làm theo pha riêng: channel credential, mapping, sync queue/dead-letter, import reservation idempotent rồi mới mở OTA |
| Maintenance Asset/Preventive | `MaintenanceEngine.AssetService` và `PreventiveService` trả TV/AC/elevator/plan/schedule mẫu | Mock legacy; Work Order đã thật nhưng asset/preventive chưa có persistence | Bổ sung bảng tài sản vận hành và kế hoạch/lịch bảo trì; liên kết với Work Order, không dùng `FixedAsset` kế toán thay thế máy móc vận hành |
| Housekeeping legacy engine | `HousekeepingEngine.cs` còn linen/amenity/assignment/dashboard mẫu | Không phải đường board/workflow hiện tại; board/workflow đang qua EF service | Không đăng ký engine cũ; chỉ triển khai Linen/Amenity khi có nghiệp vụ và schema được chốt |
| Concierge ngoài Service Request | Wake-up, luggage, transportation/dashboard trong `ConciergeEngine.cs` còn mock | Chưa được bật trên đường UI đã kiểm chứng | Tách từng aggregate và persistence; không dùng chung bảng Service Request nếu khác vòng đời |
| Group legacy engine | `GroupEngine.cs` còn rooming/billing/dashboard mẫu | Legacy; các chức năng đoàn đã có nhiều EF service riêng | Tiếp tục rà từng cửa sổ Guest/Rooming/Folio/Deposit/Invoice, chỉ bật sau khi có command/query thật |
| Exchange Rate, Shift/Incident/VIP phụ trợ | Một số cửa sổ đã chủ động rỗng/fail-closed vì thiếu API/query hoàn chỉnh | Chưa phải mock đang báo thành công | Bổ sung contract và query thật theo thứ tự: Exchange Rate → Incident/Shift → VIP dashboard |

### Migration và kiểm chứng

- Migration mới `20260908130000_AddCashReconciliation` chỉ tạo bảng đối soát và index/FK cần thiết; chưa apply vào database.
- Thứ tự migration đã được kiểm tra: `AddGroupRoomBlocks` → `AddGuestMessagePersistence` → `AddCashReconciliation`.
- Targeted persistence test đối soát: **1/1 pass**.
- Full unit suite sau thay đổi: **641/641 pass, 0 skipped**.
- Không chạy `dotnet ef database update`, không ghi production database, không thay connection string.

### Khuyến nghị triển khai tiếp theo

1. Chạy smoke test trên database test có ca đóng, folio/payment thật và kiểm tra migration fingerprint trước khi apply staging.
2. Dọn legacy mock sau khi xác nhận không còn entry point/DI nào gọi trực tiếp; không dùng việc “còn class mock” để kết luận màn hình production đang mock.
3. Ưu tiên tiếp theo là Maintenance Asset/Preventive hoặc Door Lock persistence; Channel/OTA nên làm sau cùng vì cần credential, idempotency và adapter external provider.

## Cập nhật triển khai — thay mock Service Request và Message Center bằng dữ liệu thật — 08/09/2026

- `ServiceRequestViewModel` không còn tạo các yêu cầu mẫu trong `LoadAsync`. Đường production hiện gọi `IServiceRequestService` qua `EfServiceRequestService`, đọc `fo.YeuCauDichVu`, ghi create/assign/transition và ghi `fo.LichSuYeuCau` cùng audit.
- Đã sửa lỗi persistence khi chuyển trạng thái: lịch sử domain dùng GUID tạo sẵn nên EF từng nhận nhầm là `Modified`, phát sinh `FO_CONCURRENCY_CONFLICT`; service hiện thêm history pending rõ ràng với trạng thái `Added`. Test SQLite đã bao phủ create → query → complete.
- `MessageCenterViewModel` không còn nạp hai tin nhắn cố định hoặc sửa `ObservableCollection` giả. Đã nối `IGuestMessageService`/`EfGuestMessageService`, đọc và ghi `GuestMessage` thật, hỗ trợ tạo/gửi, đánh dấu đã đọc, trả lời và lưu lịch sử/audit.
- Đã bổ sung mapping EF cho `GuestMessage`/`MessageHistory` và migration `20260908110000_AddGuestMessagePersistence`. Migration chỉ tạo/reconcile `fo.TinNhanKhach` và `fo.LichSuTinNhanKhach`; không tạo lại các bảng Service Request đã có trong migration lịch sử. Chưa chạy migration vào database production.

### Kiểm chứng sau triển khai

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- Test persistence Service Request: **1/1 pass**.
- Test persistence Message Center: **1/1 pass**.
- Unit suite loại Integration: **639/639 pass, 0 skipped**.
- Danh sách migration xác nhận `20260908110000_AddGuestMessagePersistence` đứng sau `20260908090000_AddGroupRoomBlocks`; chưa chạy `database update`.

### Mock còn lại cần ưu tiên

- Cashier drawer/reconciliation/dashboard: backend EF riêng chưa có, dù cash shift đã có dữ liệu thật.
- Maintenance asset/preventive, Channel/OTA và door-lock device dashboard: hiện chưa có schema/provider thật; tiếp tục fail-closed, không bật fixture.
- Một số prototype Concierge khác ngoài Service Request và các tiện ích export/print/email vẫn cần contract/command thật trước khi vận hành.

## Cập nhật triển khai — Cashier dashboard đọc ca thu ngân thật — 08/09/2026

- Đã bổ sung `EfCashierDashboardService` và đăng ký `ICashierDashboardService` trong DI.
- Dashboard cashier/supervisor/treasury hiện truy vấn `CashierShifts` và `CashMovements` thật, tính số dư kỳ vọng, tiền vào/ra, số ca đang mở và variance của ca đã đóng.
- Các chỉ tiêu reconciliation chưa có bảng/contract persistence riêng vẫn trả `0`/danh sách rỗng có chủ đích; không dùng số liệu hard-code. Drawer hiện được biểu diễn bằng shift thật (`CashierShiftId/ShiftNumber`) cho đến khi có master data drawer riêng.
- Test SQLite `CSH_REAL_001_dashboard_reads_open_shift_from_database` xác nhận dashboard đọc đúng ca vừa mở từ database.

### Kiểm chứng cập nhật

- Unit suite loại Integration: **640/640 pass, 0 skipped**.

## Cập nhật triển khai — Dirty Room mở Lost & Found/Minibar thật — 08/09/2026

- `DirtyRoomViewModel.OpenLostFound` mở `LostFoundWindow` với số phòng đang chọn để truy vấn qua `ILostFoundQueryService`/`EfLostFoundQueryService` theo property; không còn MessageBox dữ liệu mẫu.
- `DirtyRoomViewModel.OpenMinibar` mở `MinibarWindow` với `RoomId` thật; `MinibarViewModel` chọn đúng snapshot phòng sau khi đọc `IMinibarQueryService`/`EfMinibarQueryService`.
- Maintenance vẫn fail-closed có chủ đích vì `WorkOrderService` hiện là implementation mẫu và chưa có persistence; không hiển thị work order giả.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 740 warnings hiện hữu.
- Unit suite (loại integration): **635/635 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Rà soát toàn bộ mock còn lại và phân loại đường chạy — 08/09/2026

### Kết luận theo implementation và DI

| Nhóm | Bằng chứng | Kết luận |
|---|---|---|
| Reservation, Stay, Folio, Room Master, Housekeeping board/workflow, Group, Lost & Found, Minibar, Rate/Exchange Rate, Key-card, Cash shift | Có repository/service EF, truy vấn hoặc ghi `AppDbContext`, và được đăng ký trong `ASMILE.BO.Infrastructure/DependencyInjection.cs` | Đang chạy dữ liệu thật ở các đường đã nối; không đánh đồng các helper mock chết với dữ liệu runtime |
| Cashier drawer/reconciliation/dashboard | `CashierEngine.cs` còn DTO mẫu, nhưng đường production hiện chỉ đăng ký `EfCashShiftService` cho `ICashShiftService`; không có implementation EF tương ứng cho drawer/reconciliation/dashboard | Chưa hoàn thiện, nhưng phần mock cũ chưa được DI gọi. Cần schema/contract riêng trước khi bật |
| Channel/OTA: channel, inventory/rate mapping, reservation sync, sync monitor, dashboard | `ChannelEngine.cs` chỉ trả `Task.FromResult`, `Guid.NewGuid()` và số liệu cố định; không có `DbSet`/repository/migration và không có registration production cho các interface Channel | Mock/prototype chưa được dùng production. Đây là gap thật cần thiết kế persistence + adapter OTA, không thể chỉ thay vài dòng UI |
| Maintenance: asset health, preventive | `MaintenanceEngine.cs` vẫn có DTO mẫu cho asset/preventive; chưa có entity/DbSet/repository thật hoặc registration production cho hai contract này | Chưa hoàn thiện; tiếp tục giữ fail-closed, không dùng dữ liệu mẫu |
| Maintenance: work order | Đã có `MaintenanceWorkOrder`/`MaintenanceWorkOrderNote`, EF mapping + migration `20260907230403_AddMaintenanceWorkOrders`, `EfWorkOrderService` được đăng ký cho `IWorkOrderService`; có tạo, giao, chuyển trạng thái, truy vấn active, dashboard, audit và test domain. `DirtyRoom` và `RoomInspection` đã truy vấn Work Order active thật theo `RoomId/RoomNumber`. | Backend và cờ/nút liên kết trên Housekeeping đã đi theo dữ liệu thật; màn hình quản trị Work Order riêng và việc apply migration vẫn là phần cần triển khai/duyệt tiếp |
| Concierge và Executive | `ConciergeEngine.cs`/`ExecutiveEngine.cs` còn DTO mẫu; không có registration production tương ứng | Legacy/prototype, không phải bằng chứng UI đang hiển thị dữ liệu giả; chỉ triển khai sau khi chốt nguồn dữ liệu báo cáo thật |
| Concierge/VIP Loyalty/Wake-up/Task Shift prototype ở App | `ConciergeViewModel`, `VipLoyaltyViewModel`, `WakeUpCallViewModel`, `TaskShiftViewModel` tự tạo DTO bằng `Guid.NewGuid()`/dữ liệu cố định và không có DI route/repository production; riêng wake-up nghiệp vụ thật đã có `RequestWakeUpCallCommandHandler` trong Reception | Đây là các UserControl/prototype chưa được nối production. Không được dùng các ViewModel này làm dữ liệu thật; nếu mở module thì phải thay bằng query/command thật |
| Service Request và Message Center prototype ở App | `ServiceRequestViewModel.LoadAsync` và `MessageCenterViewModel.LoadAsync` luôn tạo các DTO cố định trong bộ nhớ; Create/Assign/Complete/Reply chỉ sửa `ObservableCollection`, không gọi repository/service và không có DI route production | Đây là mock đang chạy nếu mở các UserControl tương ứng, không phải fixture chết phía sau `return`; cần đưa vào backlog P0/P1 và fail-closed hoặc nối query/command thật trước khi sử dụng vận hành |
| Door lock device/dashboard | `DoorLockEngine.cs` còn `SampleDevice`/`Task.FromResult`; production hiện đăng ký `EfKeyCardService`, không đăng ký device/dashboard mock | Key-card hiện có đường thật; quản lý thiết bị khóa cửa chưa có persistence/integration thật |
| Group/Housekeeping engine cũ | Engine cũ còn fixture nhưng production dùng các `Ef...Service` tương ứng | Không bật lại engine cũ; chỉ dọn sau khi có smoke test hồi quy |

### Mock/fixture còn trong source và trạng thái runtime

- Receipt Voucher, Cashier Voucher, Proforma, Folio Review, Issue Invoice, Split/Merge Folio, Quick/Express/Checkout Preparation và Refund đã được đối chiếu constructor/`Loaded`: các đường này đọc service/repository thật hoặc rỗng fail-closed. `new Random(...)`/danh sách mẫu còn trong source là fixture legacy chưa dọn, không phải dữ liệu runtime đã xác nhận.
- AR Ledger, AR Invoice và AR Payment đã có nhánh query service thật rồi `return`; các generator `Random` bên dưới là code legacy unreachable và không được xếp cùng nhóm mock runtime.
- Ngược lại, `ServiceRequestViewModel` và `MessageCenterViewModel` chưa có nhánh query thật: `LoadAsync` đang nạp dữ liệu cố định trực tiếp và các command chỉ cập nhật bộ nhớ. Đây là hai điểm cần xử lý trước khi coi các màn này là vận hành thật.
- Một số nút Housekeeping/Group/Room Master vẫn chỉ là thông báo `UI MOCKUP` hoặc đã bị khóa fail-closed: ảnh, batch inspection/attendant, export/print, Group Folio/deposit/invoice/report/email/print, QR/history/export. Link Maintenance ở Dirty Room/Room Inspection đã chuyển sang đọc Work Order active thật; các thao tác tạo/giao/chuyển trạng thái cần màn hình Work Order và migration được apply. Đây là khoảng trống chức năng thật, không nên mở lại bằng DTO trong RAM.
- Các khối `Random` phía sau nhánh `return` trong AR/Current Guests/Housekeeping/Minibar/Room Master và các helper `LoadMockData` của Locked Room/Exchange Rate cần được coi là legacy fixture; các màn hình tương ứng đã có nhánh đọc thật hoặc fail-closed.

### Thứ tự nên triển khai tiếp

1. **P0 — Dọn và khóa regression của fixture:** xóa/đánh dấu rõ các generator legacy trong Receipt Voucher, Cashier Voucher, Proforma/Issue Invoice và Folio sau khi có smoke test; bổ sung test bảo đảm khi thiếu service hoặc database rỗng thì UI vẫn rỗng/fail-closed, không quay lại dữ liệu mẫu.
2. **P1 — Hoàn thiện Housekeeping/Room Master liên kết:** ảnh/checklist, batch inspection/attendant, lịch sử, export/print; link Maintenance đã nối đọc Work Order thật, bước kế tiếp là màn hình tạo/giao/chuyển Work Order sau khi migration được apply.
3. **P1 — Cashier:** bổ sung drawer/reconciliation thật trên nền `CashierShifts`/`CashMovements`, sau đó mới bật các dashboard và thao tác còn thiếu.
4. **P2 — Maintenance:** thiết kế migration riêng cho work order, asset, preventive schedule và audit; không tái sử dụng `MaintenanceEngine` mẫu.
5. **P2 — Channel/OTA:** tách channel credentials, mapping, sync queue/dead-letter, idempotency và adapter provider; cần test sandbox trước khi bật push/pull thật.
6. **P3 — Concierge/Executive/Door-lock device:** chỉ làm sau khi chốt nguồn dữ liệu và integration thật; không ưu tiên hơn các luồng vận hành tiền/phòng.

### Kiểm chứng vòng rà soát

- `D-ASMILE-BO` graph index: **ready**, 51.067 nodes / 151.834 edges; dùng graph để truy tìm implementation, sau đó đối chiếu DI/DbContext và source call-site.
- Build `ASMILE.BO.App`: **PASS, 0 errors** (740 warnings hiện hữu).
- Unit suite loại Integration: **637/637 pass, 0 skipped**.
- Không chạy migration, không ghi production database, không thay connection string.

## Cập nhật triển khai — Room Master batch Sạch/Bẩn qua Housekeeping workflow thật — 08/09/2026

- Đã xác nhận `IHousekeepingWorkflowService` có contract thật `ExecuteAsync(HkRoomActionRequest)` và production DI dùng `EfHousekeepingWorkflowService`.
- Batch `Đánh dấu sạch` và `Đánh dấu bẩn` trong Room Master không còn hiện thông báo khóa thao tác; mỗi phòng được gửi qua workflow thật với `PropertyId` và `RoomId`, sau đó tải lại từ repository.
- Workflow EF kiểm tra property scope, phòng active, tạo/cập nhật `RoomHousekeepingState`, lưu DB và xử lý lỗi nghiệp vụ/concurrency. Kết quả UI phân biệt số phòng thành công và bị từ chối; không cập nhật thành công giả.
- Giữ nguyên batch OOO/OOS dùng `ChangeHotelRoomOperationalStatusCommandHandler`; không ánh xạ Sạch/Bẩn sang `Available` vì đó là hai trạng thái nghiệp vụ khác nhau.

### Kết quả rà soát mock tiếp theo

- Đường chạy Room Master, Housekeeping board/task, Stay/Folio/Reservation liên kết, Group Service và các luồng Folio chính hiện đã đọc/ghi qua service/repository thật hoặc fail-closed.
- Điểm còn là gap thật, cần ưu tiên theo vertical slice: `Dirty/Clean Room` liên kết Maintenance/Minibar/Lost & Found và ảnh/checklist; Room Master QR/history/export/print; Group Folio/deposit/invoice/report/rooming-list; chứng từ/e-invoice/provider; Express/Quick checkout; Shift/Incident/VIP/Marketing command.
- `DoorLockEngine` trong `ASMILE.BO.Infrastructure/FrontOffice/DoorLock/DoorLockEngine.cs` vẫn là sample DTO (`SampleDevice`, `Task.FromResult`) nhưng hiện chưa được đăng ký trong production DI; không được bật đăng ký trước khi có bảng/query/property scope thật.
- Các `LoadMockData`/`Random` còn lại cần dọn sau smoke test; qua rà call-site chúng không phải nguồn dữ liệu của các đường `Loaded` hiện hành. Không dùng chúng làm fallback production.

## Cập nhật triển khai — Bàn giao ca dùng KPI vận hành thật — 08/09/2026

- `ShiftHandoverWindow` đã nối `IHotelStayOccupancyReader` để đọc tổng phòng, OOO/OOS và tính Occupancy theo property; không còn hiển thị dấu `—` cho KPI OOO khi dữ liệu occupancy thật đã có.
- Dự báo Arrival/Departure ngày mai đọc trực tiếp từ `IHotelStayRepository` theo business date của execution context. KPI Check-in được sửa để đếm Arrival, không nhầm với In-House.
- Các tab Tổng quan và Inspector hiển thị số Incident từ `IIncidentQueryService`; khi thiếu query vẫn fail-closed, không sinh số mẫu.
- Build `ASMILE.BO.App` đạt **0 lỗi**; unit suite đạt **657/657 pass, 0 skipped**. Không chạy migration hay thay đổi production database.
- Các dòng “Chưa có cờ VIP trong Stay query thật”, “Chưa nối Group Reservation query…” và “chưa có audit log query” là giới hạn contract còn thiếu, không phải dữ liệu mẫu. Bước kế tiếp nên là bổ sung query/contract tương ứng nếu muốn đưa các chỉ số đó vào bàn giao ca.

## Đính chính phân loại — Service Request và Guest Message — 08/09/2026

- Rà lại call graph cho thấy `ServiceRequestViewModel` đã gọi `IServiceRequestService.GetAllAsync/CreateAsync/AssignAsync/TransitionAsync`; `EfServiceRequestService` là implementation production. Không còn xếp màn hình này vào nhóm mock đang chạy.
- `MessageCenterViewModel` đã gọi `IGuestMessageService.GetAllAsync/CreateAndSendAsync/MarkReadAsync/ReplyAsync`; `EfGuestMessageService` ghi các thay đổi và audit vào database. Các nhận định cũ trong phần audit trước đó chỉ là snapshot đã lỗi thời.
- Khi service hoặc execution context thiếu, hai màn hình fail-closed với thông báo lỗi và collection rỗng; không có fallback DTO mẫu trên production path.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 740 warnings hiện hữu.
- Unit suite (loại integration): **635/635 pass, 0 skipped**.
- `EfHousekeepingWorkflowService` đã được đối chiếu trực tiếp: thao tác Sạch/Bẩn truy vấn `HotelRooms`/`RoomHousekeepingStates` và gọi `SaveChangesAsync`; không phải cập nhật chỉ trong memory.
- Không chạy migration và không ghi production database.

## Cập nhật triển khai — Phòng bẩn giữ RoomId và mở Folio/Reservation thật — 08/09/2026

- `DirtyRoomViewModel.MapTask` không còn bỏ qua `HkTaskBoardDto.RoomId`; liên kết hiện giữ đúng RoomId từ task Housekeeping thật.
- `Mở Folio` tìm Stay đang ở theo property scope và `RoomId/RoomNumber`, sau đó mở Folio workspace theo `HotelStayId` thật.
- `Mở Reservation` truy vấn Reservation theo `RoomId`, lọc bản ghi còn hiệu lực theo business date và mở Reservation workspace với bản ghi thật.
- Thiếu RoomId, context, service hoặc bản ghi thật đều dừng fail-closed; không hiển thị số dư/mã đặt phòng dựng sẵn.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 740 warnings hiện hữu.
- Chưa chạy migration và chưa ghi production database.

## Rà soát tiếp — Mock còn lại và Marketing đã chuyển sang dữ liệu thật — 08/09/2026

### Kết quả kiểm tra mới

- Quét lại `ASMILE.BO.App` hiện còn **49 dấu hiệu** `UI MOCKUP`/`LoadMockData`/`Random`/`mock data` trong **37 file**, gồm **38 vị trí `new Random(...)`**. Đây là inventory mã nguồn, không đồng nghĩa 49 màn hình đang hiển thị dữ liệu giả.
- Chỉ còn **3 thông báo thao tác chưa có backend** trên đường UI đã xác định rõ: kiểm tra hàng loạt Clean Room, export Incident và print Incident. Các thông báo này fail-closed, không báo thành công giả.
- Các nhóm còn cần hoàn thiện nghiệp vụ thật: tiện ích Housekeeping (batch inspection, ảnh, liên kết Maintenance/Minibar/Lost & Found/Folio, export/print), tiện ích Room Master (QR, lịch sử/liên kết, batch/export/print), Group utilities (guest document/import, service/billing, Folio/Invoice/Report/Email/Print), Proforma/Issue Invoice và các phần Shift/VIP/Housekeeping request chưa có schema/contract phù hợp.
- `ISystemSettingsService`, `IBackupService`, `IServiceStatusService`, `ISecuritySettingsService`, `IPrinterSettingsService` production hiện dùng implementation `Unavailable`/safe-default. Đây là trạng thái chưa sẵn sàng có chủ đích, không phải dữ liệu thật.
- `IEInvoiceProvider` production vẫn là `UnavailableEInvoiceProvider`; không được coi thao tác phát hành hóa đơn điện tử là đã hoàn thành chỉ vì đã có invoice record/query.

### Marketing — đã hoàn thiện vertical slice dữ liệu thật

- Bổ sung entity `fo.TiepThi` có property scope, trạng thái, audit timestamp, `RowVersion` và unique key theo `PropertyId + Section + Code`.
- Bổ sung `IMarketingQueryService`/`IMarketingCommandService` và `EfMarketingService`: tìm kiếm, tạo, sửa, đổi trạng thái đều đi qua EF, kiểm tra property scope, concurrency và audit; UI không còn sinh 28 dòng mẫu, KPI lấy từ bản ghi truy vấn thật.
- Nối các nút Thêm/Sửa/Đổi trạng thái của `MarketingCenterWindow` vào command thật và modal nhập liệu; sau mutation UI reload lại từ DB.
- Tạo migration `20260907215339_AddFrontOfficeMarketingData`; **chưa áp dụng migration vào database nào**. Khi triển khai cần chạy preflight/fingerprint/backup theo quy trình migration hiện hành rồi mới apply.
- Test SQLite persistence/duplicate/property-scope path: **2/2 pass**; build app: **0 errors, 740 warnings nền của solution**.

### Kết luận triển khai tiếp theo

1. Không xóa máy móc toàn bộ helper `Random`: giữ các helper chết ngoài production path để dọn ở đợt riêng, nhưng tiếp tục fail-closed khi thiếu query/command.
2. Ưu tiên P0: đưa migration Marketing và các migration Hotel đang chờ vào quy trình preflight; tạo integration test schema thật trước khi apply.
3. Ưu tiên P1: Housekeeping evidence/links và Group guest/service/billing vì đây là các luồng vận hành còn thiếu write path.
4. Ưu tiên P2: Proforma/Invoice provider, Shift/Incident/VIP/Housekeeping Message và export/print; chỉ bật khi có query/command/audit thật.

## Cập nhật kiểm tra mock và nối AR Bank với dữ liệu thật — 08/09/2026

- `ReconciliationEngine.RunAutoMatchAsync` không còn sinh `Guid.NewGuid()` để ghi cặp đối soát giả. Phiên phải có `BankStatementId` thật trong `MetadataJson`; engine đọc `BankStatementLine` và `ReceivableTransaction` cùng company/branch, chỉ khớp khi số tiền/ngày có điều kiện rõ ràng và reference khớp hoặc chỉ còn đúng một ứng viên.
- Khi khớp, hệ thống ghi `ReconciliationMatch` với `SourceType=BankStatementLine`, `TargetType=ReceivableTransaction`, ID thật, score/fuzzy metadata; cập nhật `BankStatementLine.MatchStatus` và chống khớp trùng khi chạy lại. Phiên được hoàn tất có timestamp và audit.
- `DetectAnomaliesAsync` không còn tạo cảnh báo `NoData` giả. Chỉ tạo anomaly cho dòng sao kê thật chưa khớp, có `RelatedEntityId` là ID dòng sao kê và chống tạo trùng.
- Panel import trong `ArPaymentView` đã bỏ nút `Simulate Import`; nay gọi `IBankingService.GetStatementsAsync` để nạp sao kê/dòng sao kê thật. `Auto Match` tạo phiên đối soát thật cho từng sao kê và tải lại trạng thái từ database; khi chưa có sao kê/service, UI báo rõ và không dựng dòng giả.
- Nút ghép thủ công từng dòng đã gọi command thật khi hệ thống tìm được đúng một phiếu thu theo số tiền/ngày/reference; khi có nhiều ứng viên, UI hiển thị selector các phiếu thu thật để người dùng chọn. Command kiểm tra amount, scope, trạng thái, chống ghép trùng và ghi audit; không có ứng viên thì fail-closed.

### Kiểm chứng vòng này

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- `ReconciliationEngineTests`: **4/4 pass**, gồm auto/manual khớp ID thật, idempotency, fail-closed khi thiếu sao kê, amount mismatch và anomaly dòng chưa khớp.
- Nhóm kiểm thử AR trọng tâm hiện đạt **10/10 pass** khi chạy `ReconciliationEngineTests` cùng `ReceivablePaymentAllocationTests`.
- Không chạy migration, không ghi production database và không thay connection string.

### Mock/gap còn lại sau vòng này

1. Receipt/invoice provider, email/in và export vẫn cần provider/command thật; hiện tiếp tục fail-closed.
2. Manual bank matching đã có selector rõ ràng khi có nhiều phiếu thu cùng số tiền/ngày; vẫn cần approval riêng nếu chính sách vận hành yêu cầu maker-checker.
3. Các tiện ích Housekeeping/Room Master/Group chưa có query/command đầy đủ vẫn phải giữ fail-closed; không bật fallback mock.
4. **Đã xử lý P1 — đổi phòng:** `RoomMoveConfirmationWindow` đã đổi nhãn từ “Mockup UI/Xác nhận mockup” sang xác nhận chuyển phòng thật; lệnh phía sau tiếp tục gọi `MoveRoomAsync` thật.

## Cập nhật — rà soát đường chạy mock toàn module — 08/09/2026

### Kết luận kiểm tra

Đã rà soát các chuỗi và hàm có dấu hiệu mock/sample trong `ASMILE.BO.App`, sau đó kiểm tra đường gọi runtime thay vì chỉ dựa vào tên hàm:

| Khu vực | Kết quả | Bằng chứng thực tế |
|---|---|---|
| Khách đang lưu trú | Dữ liệu thật | `CurrentGuestsWindow.LoadPersistedAsync` gọi `IHotelStayRepository.GetCurrentInHouseAsync` khi `Loaded` |
| Khách đến/đi hôm nay | Dữ liệu thật | `TodayArrivalsWindow`/`TodayDeparturesWindow` gọi repository theo property và business date |
| Nhà buồng | Dữ liệu thật | `HousekeepingMessageWindow.LoadPersistedTasksAsync` gọi `IHousekeepingBoardService.GetTaskBoardAsync` |
| Phòng khóa | Dữ liệu thật | `LockedRoomWindow` gọi `IReceptionUiService.GetLockedRoomsAsync` |
| Folio Workspace | Dữ liệu thật | `LoadPersistedFoliosAsync` gọi `IHotelFolioRepository.SearchAsync` và `GetLedgerAsync` |
| Proforma Folio | Dữ liệu thật | `LoadPersistedFoliosAsync` gọi repository Folio; các hàm sinh mẫu không có caller runtime |
| Room Master | Dữ liệu thật | `_usingRealData` gọi `LoadFromRepositoryAsync`; nhánh sinh phòng mẫu nằm sau `return` không thể chạy |
| Import sao kê | Dữ liệu thật | `CashBankViewModel` xem trước/commit qua import engine; engine ghi `BankStatement`/`BankStatementLine` xuống DB |
| AR Payment/Allocation | Dữ liệu thật | Tạo receipt, allocation, kiểm tra số dư và customer đều đi qua service/DB thật |

### Mã mẫu còn tồn tại nhưng không phải dữ liệu đang chạy

- `LoadMockData` trong các cửa sổ arrivals/departures/current guests/housekeeping chỉ còn mã tương thích; kiểm tra caller cho thấy không có lời gọi runtime.
- `LoadFolios`, `LoadTransactions`, `LoadPostings`, `LoadPayments`, `LoadDeposits`, `LoadRefunds` trong Folio/Proforma cũng không có lời gọi runtime; dữ liệu màn hình được nạp từ repository khi `Loaded`.
- Các generator `Random` trong AR và Room Master đều nằm ở nhánh legacy sau `return` hoặc chỉ để tham chiếu migration.
- `TrainingData` và nút seed sample của F&B là chức năng dữ liệu đào tạo có chủ đích, không phải fallback vận hành.
- Catalog fallback của Report Center chỉ là metadata tên báo cáo; phần chạy báo cáo vẫn dùng ReportEngine/query dữ liệu persisted.

### Điểm còn thiếu để gọi là hoàn thiện 100%

1. Group Profile chưa có contract lưu file tài liệu thật; giao diện hiện chủ động cảnh báo và không tạo tài liệu giả.
2. Một số thao tác nâng cao của Group/Housekeeping/Room Master (file attachment, một số utility action) cần contract/service riêng trước khi nối DB; không nên bật bằng mock.
3. AR Payment hiện xuất CSV thật và xem trước receipt thật; xuất PDF chuyên dụng/email/reversal cần provider hoặc command contract riêng.
4. Payment method/bank account của receipt chưa có cột persisted tương ứng trong mô hình hiện tại; không nên hiển thị như đã lưu thật cho tới khi có schema/contract được phê duyệt.

### Kiểm thử sau rà soát

- `dotnet test ASMILE.BO.Tests.Unit/ASMILE.BO.Tests.Unit.csproj --no-build -v minimal`
- Kết quả: **620/620 passed, 0 failed, 0 skipped**.

Không phát hiện màn hình vận hành nào đang tự động fallback sang bộ dữ liệu giả trong đường chạy đã kiểm tra. Các mục còn thiếu ở trên là gap nghiệp vụ/contract thật, không phải lỗi cần che bằng sample data.

## Cập nhật — AR Payment reversal trên dữ liệu thật — 08/09/2026

- Nút `Đảo ngược` của AR Payment đã được nối vào `IReceivableService`, không còn chỉ cập nhật thông báo trong bộ nhớ.
- Service nhận đúng cả `BusinessDocument.Id` và `ReceivableTransaction.Id`, xử lý lệch ID giữa màn hình và persistence layer.
- Receipt ở trạng thái chờ duyệt được hủy thật; receipt đã duyệt được reopen rồi hủy theo service thật.
- Receipt đã có `PaymentAllocation` bị từ chối đảo ngược để không làm sai số dư công nợ; hiện cần hoàn tác allocation bằng command riêng trước.
- `Hoàn tác phân bổ` đã được nối vào `IDebtTransactionService`: xóa allocation thật, hoàn lại `BalanceAmount` của công nợ đích và cập nhật trạng thái/audit.
- Mapping UI hiển thị trạng thái `Reversed` khi transaction thật có trạng thái `Cancelled`.
- Bổ sung test cho reversal thành công, nhận transaction ID và chặn receipt đã phân bổ.

Kiểm chứng sau cập nhật:

- Build `ASMILE.BO.App`: **0 warning, 0 error**.
- Unit suite: **623/623 passed, 0 skipped**.
- Không chạy migration và không ghi production database.

## Cập nhật — AR Payment/Allocation và kiểm tra wiring mock — 08/09/2026

- `ArPaymentWindow` không còn tự tạo `ArPaymentViewModel` từ XAML. Cửa sổ lấy ViewModel qua DI nên `IReceivableService`/`IDebtTransactionService` thật được nối khi mở từ Front Office.
- Màn hình AR Payment đã bỏ trạng thái chỉ xem ở form nhận thanh toán: khách hàng được chọn từ `Customers`, phiếu thu ghi qua `IReceivableService.CreatePaymentReceiptAsync`, sau đó reload lại từ DB.
- `EfReceivableService` luôn tạo dòng `ReceivableTransaction` cho phiếu thu mới, kể cả khi database trước đó chưa có dòng công nợ nào. `SourceType=PaymentReceipt` để mapping không bị nhầm thành hóa đơn.
- Allocation hiện đọc các khoản phải thu còn dư theo đúng khách hàng và ghi `PaymentAllocation` thật. `DebtTransactionService` đã chặn thiếu giao dịch nguồn, vượt số dư khoản thu, vượt công nợ đích, phân bổ sai khách hàng và tự phân bổ vào chính giao dịch nguồn.
- `ReceivableTransactionDto` có `AllocatedAmount`; AR Payment không còn hiển thị tất cả khoản thu là `Unallocated` khi backend đã có allocation thật.
- Các export/in/email/hoàn/đảo/import sao kê ngân hàng trong AR Payment vẫn chưa có provider/command ghi thật; các nút này tiếp tục fail-closed, không báo thành công giả.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Test riêng phiếu thu/phân bổ: **2/2 pass**.
- Full unit suite: **620/620 pass, 0 skipped**. Một lần chạy trước đó có test scheduler timing-sensitive 617/618; chạy riêng và chạy lại toàn bộ đều pass.
- Không chạy migration, không ghi production database và không thay connection string.

### Phân loại mock còn lại trên đường chạy

- Không còn mock fallback chủ động trong các loader đã được chuyển sang repository/service thật ở Arrival/Departure/Current Guest, Folio, Housekeeping, Room Master, Group Service, AR Aging và AR Payment.
- Các generator `Random` còn trong source chủ yếu nằm sau `return`, trong nhánh legacy không được gọi, hoặc thuộc màn hình dữ liệu mẫu/training có chủ đích. Cần dọn riêng sau smoke test để giảm nhiễu, nhưng không phải nguồn dữ liệu hiển thị của các đường chạy trên.
- Các vùng còn thiếu backend thật cần ưu tiên tiếp: AR bank import/matching và receipt print/email/reversal; tiện ích Group/Room Master/Housekeeping export-print; Shift/Incident/VIP/Marketing và Exchange Rate nếu chưa có provider tương ứng. Không nên bật lại mock để che các khoảng trống này.

## Cập nhật — Rà soát route cài đặt Breakfast & Coffee và dữ liệu À La Carte — 08/09/2026

- Route `breakfast-cafe-settings` trước đây khởi tạo `BreakfastCafeSettingsViewModel` với số liệu mock (bàn, món, khu vực, máy in, nhân viên, doanh thu và trạng thái thiết bị). Route đã được chuyển sang `BreakfastCafeSalesViewModel`, tự mở tab Cài đặt và dùng `IBreakfastCafeSalesService`/`IMenuCatalogService` để đọc bàn, khu vực, nhóm món và món thật.
- Đã loại bỏ VM mock cũ khỏi DI và DataTemplate, tránh trường hợp mở nhầm màn hình vẫn hiển thị số liệu giả.
- `AlacarteRestaurantViewModel` không còn tạo khu vực/bàn mẫu khi database rỗng trong `InitSettingsReferenceData`; các collection cấu hình sẽ rỗng cho đến khi service trả dữ liệu thật. Các nút seed dịch vụ/mẫu vẫn là thao tác chủ động dành cho training, không chạy ngầm như dữ liệu vận hành.
- Các `Random`/`LoadMockData` còn lại trong một số cửa sổ Folio, Arrival/Departure/Current Guest, Housekeeping và AR đã đối chiếu call-site: đều là helper legacy không còn được gọi trên đường `Loaded`/DI hiện tại hoặc nằm sau nhánh `return`/`#if false`. Chưa xoá hàng loạt trong vòng này để tránh thay đổi ngoài đường chạy; đây là backlog dọn mã, không phải dữ liệu đang hiển thị.
- Khoảng trống thực sự còn lại cần triển khai tiếp: persistence cho allocation/reversal/import bank của AR; API/repository cho Marketing, Exchange Rate, Shift, Incident/VIP và một số tiện ích Housekeeping/Group/Room Master; printer/backup/settings POS cần provider thật nếu muốn vận hành ngoài cấu hình cục bộ.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 740 warning hiện hữu của solution.
- Unit suite: **618/618 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

## Rà soát mock sau khi hoàn thiện Group Service — 08/09/2026

### Còn mock đang chạy thật, cần ưu tiên

1. `BreakfastCafeSettingsViewModel`: constructor vẫn gọi `InitMockData()` và gán dashboard/trạng thái hệ thống/menu/bàn/nhân sự bằng số liệu cố định. Đây là điểm mock xác nhận trực tiếp còn chạy trên đường mở màn hình; cần query/settings repository và schema riêng.
2. `ArAgingViewModel`: constructor gọi `BuildFakeData()` trực tiếp, không có property-scoped query thật. Đây là điểm rủi ro cao vì hiển thị công nợ quá hạn và KPI tài chính giả; cần chuyển sang projection AR/Receivable thật trước khi bật các thao tác thu hồi.

### Còn thiếu nghiệp vụ nhưng hiện đã fail-closed, không phải đang ghi giả

- Group Booking: lưu nháp/xác nhận chưa có command persistence thật.
- Group Reservation: nhân bản, đóng đoàn, xóa đoàn nháp chưa có command.
- Rooming List/Group Assignment: thêm khách, auto-pair, swap, split room, lock room và gọi Housekeeping chưa có command thật.
- Group Service: CRUD edit/delete còn khóa; các batch trạng thái và batch post Folio đã chuyển sang command thật ở cập nhật này.
- Housekeeping, Room Master tiện ích, Receipt Voucher, Exchange Rate, Shift/Incident/VIP và một số payment allocation/invoice/export vẫn cần contract/query/command riêng.

### Không nên nhầm là mock đang chạy

- 5 helper `LoadMockData` ở Current Guests, Housekeeping Message, Locked Room, Today Arrivals, Today Departures là helper fallback/legacy; đường tải chính đã đọc repository thật hoặc fail-closed.
- 41 vị trí `new Random` còn trong App chủ yếu nằm sau nhánh `return` có chú thích legacy, hoặc helper fallback khi DI real không có. Các màn hình cần tiếp tục kiểm tra call path trước khi xóa, không dùng riêng số lượng literal để kết luận.

### Thứ tự triển khai đề xuất tiếp theo

1. Chuyển `ArAgingViewModel` sang dữ liệu AR thật, vì liên quan trực tiếp số dư/công nợ.
2. Chuyển `BreakfastCafeSettingsViewModel` sang settings/menu/table/printer repository thật hoặc đánh dấu rõ module chưa sẵn sàng.
3. Hoàn thiện Group Booking → Rooming List → Assignment lock/Housekeeping theo cùng transaction và property scope.
4. Sau đó mới mở rộng payment allocation, invoice/receipt và các tiện ích export/print; không bật các nút hiện đang fail-closed bằng dữ liệu dựng sẵn.

## Cập nhật — Chọn Folio thật và post dịch vụ đoàn hàng loạt — 08/09/2026

- Bổ sung contract `PostGroupServiceOrdersToFolioRequest` và `PostBatchToFolioAsync`; command tải toàn bộ dịch vụ theo property, kiểm tra đủ mã, tất cả đều `Completed`, không có entry `GroupServiceOrder` đã post và Folio đích đang `Open` trước khi ghi.
- Batch post tạo `HotelFolioEntry` thật cho từng dịch vụ, giữ `ReferenceType=GroupServiceOrder`, cập nhật trạng thái từng order sang `PostedToFolio`, ghi audit và commit một lần. Nếu một dòng không hợp lệ thì không tạo entry nào.
- Các nút Batch xác nhận/hoàn thành/hủy dịch vụ đoàn cũng đã nối command thật, kiểm tra trước toàn bộ trạng thái chuyển tiếp rồi mới commit chung; không còn chỉ đổi collection trong bộ nhớ.
- `GroupServiceViewModel` tải danh sách Folio mở từ `IHotelFolioRepository`/ledger thật, không tạo Folio mock; DataGrid hỗ trợ chọn nhiều dòng và nút `Batch: Folio` dùng Folio đích đã chọn.
- Bổ sung kiểm thử EF: batch 2 dịch vụ vào Folio thật, tổng 1.350 VND; order chưa hoàn thành bị từ chối không ghi dở dang; và batch trạng thái Pending → Confirmed → Completed.

### Kiểm chứng sau cập nhật

- Test `GroupServiceFolioPostingTests`: **6/6 pass**.
- Full unit suite: **615/615 pass, 0 skipped**.
- Build dây chuyền test kéo theo `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- Không chạy migration, không ghi production database và không thay connection string.

## Rà soát sâu dữ liệu mock và billing đoàn — 08/09/2026

- `EfGroupBillingService` đã bỏ việc coi `GroupReservation.TotalDeposit` là nguồn duy nhất. Billing hiện đọc các dòng `fo.DatCocDoan` còn `Posted`, các `GroupServiceOrder` chưa hủy/chưa post vào Folio và các ledger Folio thật liên kết qua reservation của assignment đoàn; mọi truy vấn đều giới hạn theo property context.
- Khi dữ liệu cũ chưa có dòng `DatCocDoan`, `TotalDeposit` snapshot chỉ được dùng làm fallback tương thích dữ liệu cũ. Khi đã có ledger, số dư ledger là nguồn chính; đặt cọc đã `Reversed` không còn tính vào công nợ.
- Bổ sung test dữ liệu EF thật cho billing summary và revenue dashboard: dịch vụ đoàn 200, đặt cọc 150, công nợ 50; doanh thu Breakfast 80 và Transport 60. Kết quả **2/2 pass**.
- `RoomInspectionViewModel` đã khóa đường preview `Random` khi thiếu `IHousekeepingBoardService`; trường hợp thiếu service nay hiển thị trạng thái lỗi/rỗng, không sinh phòng mẫu. Các ViewModel Housekeeping Dirty/Clean/Cleaning/Room Status đã có cùng nguyên tắc fail-closed.

### Phân loại sau quét call-site

- **Đang chạy dữ liệu mẫu ngoài phạm vi Hotel/Front Office:** `BreakfastCafeSettingsViewModel.InitMockData()` vẫn được gọi trực tiếp trong constructor. Đây là phần Nhà hàng/Café, cần tách thành backlog riêng nếu mục tiêu là toàn bộ hệ thống.
- **Legacy mock không nằm trong DI production:** `GroupEngine`, `CashierEngine`, `ConciergeEngine`, `MaintenanceEngine`, `ChannelEngine`, `DoorLockEngine`, `ExecutiveEngine` còn `Task.FromResult`/DTO mẫu, nhưng `DependencyInjection.cs` không đăng ký các implementation đó cho đường chạy hiện tại. Các test cũ gọi trực tiếp `GroupEngine` không phải bằng chứng màn hình production đang dùng mock.
- **Helper mock chết trong Hotel/Front Office:** các `LoadMockData`/`Random` trong Folio Workspace, Folio Review, Express Checkout, Proforma/Issue Invoice, Split/Merge Folio và một số Housekeeping/Room Master ViewModel nằm sau nhánh tải repository thật hoặc sau `return`. Chúng không được gọi ở constructor/Loaded hiện tại; có thể dọn sau khi hoàn tất smoke test, nhưng không được dùng làm fallback runtime.
- **Đã fail-closed:** các màn thiếu contract thật như payer mapping, express authorization, invoice/provider, marketing, key-card history mở rộng, room links và tiện ích export/print không được che bằng dữ liệu mẫu.

### Kết luận và phần còn thiếu thực sự

1. Ưu tiên tiếp theo là command post dịch vụ đoàn vào Folio thật và mapping payer/routing; hiện báo cáo đã đọc được hai nguồn thật nhưng chưa có workflow ghi `PostedToFolio`.
2. Hoàn thiện liên kết check-in đoàn → Stay/Folio, thay vì chỉ cập nhật trạng thái assignment đoàn.
3. Tiếp tục Housekeeping nâng cao (ảnh/checklist/maintenance/minibar/lost-found/link Folio-Reservation), Room Master tiện ích, Guest import/document và provider hóa đơn điện tử.
4. Sau mỗi nhóm chỉ chạy migration trên database test, seed dữ liệu có kiểm soát và smoke test property scope; chưa chạy migration hoặc ghi production database.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors** (744 warning hiện hữu của solution).
- Full unit suite: **609/609 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.
- Không chạy migration và không ghi production database.

## Rà soát tiếp — thu ngân, đặt cọc, hoàn tiền và Folio Review — 07/09/2026

- `CashierPaymentWindow`: Folio, ledger entries, payment allocations và confirm payment đã đi qua repository/application service thật; bước phân bổ dùng FIFO và row version mới đọc lại. Không còn số phòng, số tiền, cashier, MST/email hay mã biên lai mẫu trên đường chạy. Report/export vẫn fail-closed vì chưa có report service thật.
- `DepositManagementWindow`: danh sách Folio và thao tác ghi đặt cọc đã dùng `IHotelFolioRepository`/`HotelFolioApplicationService`. Đã bỏ bảng phân bổ hai Folio cố định, số tiền/currency/tham chiếu mẫu; hiện chỉ hiển thị Folio đang chọn và nói rõ API hiện chưa hỗ trợ phân bổ nhiều Folio hoặc persist phương thức/chứng từ.
- `RefundManagementWindow`: danh sách payment và `RefundAsync` vốn đã là đường thật; đã bỏ generator 120 giao dịch, số tiền/VND/ref mẫu, điện thoại/email mẫu, và refresh giờ tải lại DB. Số tiền hoàn/phí có thể chỉnh trong UI nhưng backend vẫn là nơi quyết định cuối.
- `FolioReviewWindow`: phần chọn khách đã đọc Stay/Folio/ledger thật nhưng các bước B–F từng dựng Folio, khoản mục, payer mapping và cảnh báo cố định. Đã thay bằng entries thật; payer mapping và phân bổ deposit chưa có nguồn/API nên hiển thị rõ là chưa có dữ liệu, không suy diễn thành dữ liệu mẫu.
- `FolioWorkspaceWindow`, `SplitFolioWindow`, `MergeFolioWindow`, `TransferPostingWindow`: đường Loaded/confirm đang dùng Folio thật và command thật; các `Random`/`Load*` còn lại là helper legacy không được gọi bởi constructor/Loaded hiện tại. Cần xoá sau smoke test để tránh tái sử dụng nhầm.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors** (744 warnings hiện hữu của solution).
- Unit suite: một lần chạy có **602/603 pass** do test scheduler timing; chạy riêng test lỗi đạt **1/1**. Cần chạy lại full suite trong CI để chốt ổn định.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật triển khai — VIP Notes đọc dữ liệu thật — 07/09/2026

- `VipNotesWindow` đã bỏ generator `Random(42)` và không còn hiển thị 1.000 ghi chú giả.
- Màn hình lấy `GuestProfile` theo property context qua `IGuestProfileRepository.SearchAsync`, lọc khách từ Bronze trở lên và chỉ tạo dòng hiển thị khi `GuestProfile.Notes` có dữ liệu thật.
- `GuestProfileDto` đã bổ sung `Notes` từ projection EF; aggregate có `SetNotes(...)` để chuẩn bị cho command ghi ở bước tiếp theo, không cần thêm migration vì cột `Notes` đã tồn tại.
- Các trường không có nguồn dữ liệu thật (phòng, reservation, category chi tiết, priority, owner, follow-up, preference/dịch vụ) không còn được điền giả; UI hiển thị `—` hoặc thông báo chưa có dữ liệu.
- Tạo/sửa ghi chú, export và print vẫn bị chặn có chủ đích vì chưa có command/service tương ứng; không báo thành công giả.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors** (742 warnings hiện hữu của solution).
- Unit suite: **603/603 pass, 0 skipped**.

## Cập nhật — Hồ sơ khách đoàn và khóa mock nền tảng — 08/09/2026

- `GroupGuestManagementWindow` đã nối `IGuestProfileRepository.GetAsync/UpdateAsync` theo `PropertyId + GuestId` thật. Các trường được hỗ trợ gồm giới tính, ngày sinh, quốc tịch, phone, email và địa chỉ; sau khi lưu màn hình đọc lại DTO thật để xác nhận.
- Số Passport, Visa và CMND/CCCD mới được ghi qua aggregate `GuestProfile.AddDocument`, có kiểm tra loại + số để không tạo bản ghi trùng. File scan/preview, đổi họ tên, công ty, VIP/Leader, thông tin y tế và sở thích lưu trú vẫn không ghi vì chưa có command/repository phù hợp; UI đã khóa hoặc cảnh báo rõ.
- Bước xác nhận khách đoàn không còn báo chung rằng toàn bộ hồ sơ chưa có persistence. Nó gọi command lưu thật cho phần được hỗ trợ và chỉ báo thành công sau khi repository hoàn tất.
- Rà lại toàn bộ đường chạy Hotel: `CurrentGuestsWindow`, `TodayArrivalsWindow`, `TodayDeparturesWindow`, `HousekeepingMessageWindow`, Group service/assignment/rooming list và Room Master đều tải dữ liệu thật khi `Loaded`; các `LoadMockData` còn lại trong những file này là helper legacy không được gọi bởi constructor/Loaded hiện tại.
- 5 registration mock production nền tảng đã được thay bằng fallback an toàn: settings/printer/service status trả danh sách rỗng, backup/restore/validate và restart service bị chặn, security mặc định yêu cầu xác nhận admin. Như vậy không còn dữ liệu mẫu hoặc báo thành công giả trên đường production; backend thật cho các contract này vẫn là backlog nền tảng riêng.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors** (warning hiện hữu của solution).
- Unit suite: **603/603 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

## Rà soát call-site sau build — phân biệt mock runtime và helper cũ — 07/09/2026

- `IncidentLogWindow` đang tải sự cố qua `IIncidentQueryService` và tạo/cập nhật qua command service thật; không còn sinh 3.000 dòng `Random` khi mở màn hình.
- `ShiftHandoverWindow` đang lấy ca, Stay, Housekeeping và Incident từ service/query thật; khi thiếu dependency hoặc dữ liệu thì hiển thị rỗng/fail-closed.
- `ShiftLogWindow` dựng nhật ký từ các ca thật qua `ICashShiftService`; tạo nhật ký, in và xuất chưa có command tương ứng nên chỉ cảnh báo, không ghi dữ liệu giả.
- Quét lại literal `Mockup:`/`mockup` trong `Views` và `ViewModels` không còn kết quả. Các tên `LoadMockData` còn lại (`CurrentGuests`, `LockedRoom`, `TodayArrivals`, `TodayDepartures`, `HousekeepingMessage`) phải được xem là legacy inventory và chỉ xóa sau smoke test; bản thân tên helper không đủ để kết luận đang mock runtime.
- Kết luận hiện hành: các phần đã chuyển sang query/service thật không cần tạo thêm sample data. Phần còn cần triển khai tiếp là các command/schema thật đã nêu ở các mục backlog (Marketing, Exchange Rate, Group/Housekeeping tiện ích, Key-card history và các thao tác invoice/payment còn thiếu).

### Kiểm chứng sau khi nối phát hành hóa đơn

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite: **603/603 pass, 0 skipped**.

## Rà soát call-site bổ sung — 07/09/2026

- `TransferPostingWindow` chỉ nạp posting/Folio từ `HotelFolioApplicationService`; đã xoá kiểu đích Folio mock và các nhánh fallback mock không còn đường gọi.
- `PrintPreviewDialog` không còn gắn nhãn “In-memory sample mode”; footer hiện mô tả trung tính rằng nội dung do chức năng gọi cung cấp.
- Các `LoadMockData`/`LoadGuests`/`LoadFolios`/`Random` còn lại trong một số view đã đối chiếu call-site: không có caller sau khi Loaded/DI thật chạy. Chúng là mã fallback chết cần dọn riêng, không phải nguồn dữ liệu hiện tại.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 748 warning hiện hữu của solution.
- Unit suite: **603/603 pass, 0 skipped**.

## Cập nhật triển khai — Room Master mở workflow Room Move theo Stay thật — 08/09/2026

- Context menu `Đổi phòng` tìm Stay đang ở thật theo `RoomId/RoomNumber`, rồi mở `RoomMoveWindow` với `HotelStayId` đã chọn.
- `RoomMoveWindow` nhận Stay khởi tạo, tự chọn đúng khách và vẫn sử dụng `HotelStayApplicationService.MoveRoomAsync` với phòng đích thật và `RowVersion` của Stay.
- Không có Stay thật, thiếu context/service hoặc thiếu RowVersion thì thao tác dừng; không tạo luồng đổi phòng giả.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 740 warnings hiện hữu.
- Unit suite (loại integration): **635/635 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Cập nhật bổ sung — Service Status đọc Windows thật — 09/09/2026

- `IServiceStatusService` production đã chuyển sang `WindowsServiceStatusService`.
- Danh sách dịch vụ lấy trực tiếp từ Windows Service Control Manager; trạng thái được map từ trạng thái dịch vụ thật, không dựng `LastHeartbeatAt` giả.
- `RestartServiceAsync` đã có đường Win32 thật nhưng chỉ chạy khi bật `ServiceSupervisor:AllowRestart=true` và service nằm trong `AllowedServiceNames`; mặc định vẫn fail-closed để tránh restart nhầm.
- E-Invoice tiếp tục dùng provider `Unavailable*` cho tới khi có adapter và credential nhà cung cấp thật; không phát hành số hóa đơn giả.

### Kiểm chứng sau cập nhật

- Unit: **691/691 pass**.
- Integration LocalDB: **22/22 pass**.
- E2E: **4/4 pass**.
- Chưa chạy migration và chưa ghi production database.

## Cập nhật kiểm tra sâu — Khách đi hôm nay không còn suy đoán dữ liệu — 09/09/2026

- `TodayDeparturesWindow` đã tải thêm `IHousekeepingBoardService.GetRoomSnapshotAsync` để đối chiếu trạng thái vận hành và trạng thái buồng phòng theo số phòng thật. Khi room snapshot không có hoặc lỗi, màn hình giữ bản ghi Stay nhưng hiển thị thiếu nguồn dữ liệu, không tự kết luận phòng sẵn sàng.
- Folio của từng Stay hiện được đọc thêm bằng `GetLedgerAsync`; phần chi tiết hiển thị tổng phát sinh và tổng hợp giao dịch theo loại từ ledger thật. Các dòng tiền phòng/minibar/dịch vụ/thuế/phương thức không còn ghi “xem chi tiết” như thể đã có dữ liệu khi chưa tải ledger.
- Số khách trong thẻ chi tiết lấy từ `HotelStayDto.Adults + Children`, thay cho suy đoán `VIP = 2 khách`.
- Phần bàn giao và yêu cầu vận hành đã tách dữ liệu có thật (room snapshot, Folio, ghi chú hồ sơ, công ty) khỏi phần chưa có nguồn task/request riêng. Không còn chuỗi mô phỏng “Chưa tải task” dùng như trạng thái nghiệp vụ.
- `TodayArrivalsWindow.OnSendBell` đã xác nhận là đường ghi thật: tạo `GuestMessage` loại `Bellman` qua `IGuestMessageService.CreateAndSendAsync`; đây không phải mock wake-up call.
- `ShiftLogWindow` đang là báo cáo đọc lịch sử CashShift thật. Nút “Tạo nhật ký mới” được giữ fail-closed vì chưa có command/domain riêng cho nhật ký ca; không tự ghi vào bảng khác để che gap.

### Kiểm chứng sau vòng này

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 744 warning legacy hiện hữu.
- Unit suite: **679/679 pass, 0 skipped**.
- Chưa chạy migration và không ghi production database trong vòng này.

## Cập nhật — Marketing phân trang và xuất dữ liệu thật — 09/09/2026

- MarketingCenterWindow tiếp tục dùng IMarketingQueryService/IMarketingCommandService và dữ liệu EfMarketingService; không sinh bản ghi mẫu.
- Phân trang đã hoạt động trên kết quả lọc thật theo tìm kiếm, trạng thái và nhóm; nút trước/sau cập nhật đúng trang hiện tại.
- Nút Xuất Excel nay xuất CSV UTF-8 từ toàn bộ kết quả Marketing thật đang lọc, gồm mã, phân hệ, tên, nhóm, người phụ trách, trạng thái, doanh thu, ghi chú và thời điểm cập nhật.

### Kiểm chứng

- Build ASMILE.BO.App: **PASS, 0 error**; 744 warning legacy hiện hữu.
- Full unit suite: **679/679 pass, 0 skipped**.

## Cập nhật kiểm tra sâu — Group Checkout và HotelInvoice — 09/09/2026

- Bổ sung test tích hợp nội bộ cho chuỗi dữ liệu thật: Group Check-in → Reservation/Stay/Folio → Folio Entry → Group Checkout → HotelInvoice → Housekeeping DepartureClean task.
- Test đã phát hiện lỗi nền tảng: migration cũ đã tạo bảng `fo.HoaDon` nhưng `AppDbContext` thiếu `DbSet<HotelInvoice>` và mapping tương ứng, khiến checkout phát hành hóa đơn thất bại với lỗi `Cannot create a DbSet for 'HotelInvoice'`.
- Đã bổ sung đăng ký DbSet, mapping bảng/index/decimal/concurrency theo model snapshot và migration hiện hữu. Không tạo lại bảng, không thay đổi connection string, không chạy migration trên production.
- Test mục tiêu: **1/1 pass**. Full unit suite sau sửa: **679/679 pass, 0 skipped**.

### Trạng thái còn lại

- Các phần dữ liệu nghiệp vụ chính của Hotel/Front Office tiếp tục dùng EF/DbContext thật hoặc fail-closed khi thiếu contract.
- Các điểm còn phụ thuộc tích hợp ngoài gồm provider E-Invoice, máy in, email và system/backup settings; chưa được giả lập thành công.
- Năm migration mới nhất vẫn chưa áp dụng vào database: EInvoice adjustment, Email Outbox, Housekeeping status, Lost & Found contact và Operational Lock details. Cần preflight/backup và phê duyệt riêng trước khi chạy trên database mục tiêu.

## Kiểm chứng migration và SQL Server LocalDB — 09/09/2026

- Đã áp dụng 5 migration pending vào database kiểm thử `ASMILE_BO_DesignTime` trên SQL Server LocalDB; EF history không còn migration pending tại database kiểm thử.
- Đã xác nhận schema thật có các cột liên hệ Lost & Found, các cột hạn khóa vận hành phòng và các bảng liên quan hóa đơn/outbox.
- Bộ integration test SQL Server LocalDB đạt **20/20 pass, 0 skipped**, gồm kiểm tra migration, transaction, concurrency, property isolation, process manager và các ràng buộc Room/Reservation.
- Đây chỉ là database kiểm thử cục bộ. Chưa chạy migration, chưa backup và chưa ghi vào database production; triển khai production vẫn cần preflight, backup và phê duyệt riêng.

## Cập nhật dữ liệu GuestProfile trên danh sách khách đến/đi — 09/09/2026

- `TodayArrivalsWindow` và `TodayDeparturesWindow` đã truy vấn `IGuestProfileRepository.GetDtoAsync` theo `PropertyId` và `GuestId` của Stay thật.
- Điện thoại, email, quốc tịch, công ty, cấp VIP và ghi chú hồ sơ nay lấy từ `GuestProfile`; dữ liệu thiếu vẫn hiển thị rõ là chưa có, không dùng giá trị mẫu.
- Nếu truy vấn hồ sơ khách lỗi, danh sách Stay vẫn được giữ lại để không làm mất nghiệp vụ chính; phần chi tiết tương ứng hiển thị trạng thái thiếu dữ liệu.
- Build App: **PASS, 0 errors**. Unit suite sau thay đổi: **679/679 pass, 0 skipped**.
- Tab Tài chính của `TodayArrivalsWindow` đã đọc Folio thật qua `IHotelFolioRepository.GetByStayAsync`; hiển thị số Folio, trạng thái và số dư phải thu nếu hồ sơ tồn tại, không còn báo “chưa tải Folio” mặc định.

## Rà soát mock sau khi nối Lost & Found — 09/09/2026

- Các `Mock*Service` còn tồn tại trong Infrastructure không có inbound reference trong graph và không được đăng ký production. DI hiện dùng `EfReceivableService`, `EfPayableService`, `EfInventoryMovementService` và `EfGeneralLedgerService`; các capability chưa có backend dùng `Unavailable*`/safe-default để fail-closed.
- Các generator `Random` còn lại trong Lost & Found là fixture legacy nằm sau `return`, không thể chạy trên đường load thật. Đường đang chạy là `LostFoundViewModel → ILostFoundQueryService/ILostFoundCommandService → EfLostFound* → AppDbContext`.
- Đã hoàn thiện nốt nghiệp vụ `Liên hệ khách`: lưu người liên hệ, ngày, kênh, kết quả, ghi chú, status `Contacted` và history; query tải lại các trường này từ database.
- Migration `20260908190232_AddLostFoundContactPersistence` đã tạo 5 cột nullable cho `LostFoundItems`, nhưng **chưa áp dụng** vào database thật.

### Kiểm chứng sau rà soát

- Index graph `D-ASMILE-BO`: **ready**, 53.082 node / 164.883 quan hệ.
- Build qua `dotnet test`: **PASS**.
- Full unit suite: **676/676 pass, 0 skipped**.
- `git diff --check`: **PASS**; chỉ còn cảnh báo chuyển đổi CRLF của worktree.
- Không ghi production database, không chạy migration và không thay connection string.

### Kết luận còn lại

- Không còn mock đang được chứng minh là active trong đường Hotel/Front Office đã audit.
- Cần xử lý riêng các capability đang fail-closed (`Unavailable*`), đặc biệt provider hóa đơn điện tử, backup/restore, printer và service status; không nên bật bằng dữ liệu giả.

## Cập nhật — Lost & Found hoàn tất đường ghi dữ liệu thật — 09/09/2026

- `LostFoundViewModel` trước đây đọc dữ liệu thật nhưng các thao tác Ghi nhận mới, Chỉnh sửa, Xóa và Liên hệ khách vẫn sửa collection trong memory. Ba thao tác đầu đã chuyển qua `ILostFoundCommandService` và `EfLostFoundCommandService`; mở lại màn hình sẽ đọc lại từ database.
- Bổ sung command/EF cho đăng ký tài sản, cập nhật thông tin, xóa bản ghi mới, cùng kiểm tra property scope và trạng thái. Xóa chỉ cho phép tài sản còn ở trạng thái `Found`; bản ghi đã nhập kho/trả/thanh lý không bị xóa lẫn khỏi lịch sử vận hành.
- Bổ sung `LostFoundItem.UpdateDetails` để ghi history `Details updated`. Khi chưa có context hoặc command service, UI fail-closed và không tạo dữ liệu giả.
- Luồng Liên hệ khách hiện chưa có contract persistence tương ứng nên đã chặn ghi memory trên dữ liệu thật, hiển thị rõ gap thay vì báo thành công giả.
- Bổ sung `LostFoundPersistenceTests`: đăng ký/cập nhật/nhập kho/trả khách và xóa bản ghi mới đều kiểm tra trên SQLite in-memory.

### Kiểm chứng

- Build solution: **PASS, 0 errors**.
- Unit suite: **676/676 pass, 0 skipped**.
- `git diff --check`: **PASS**; chỉ còn cảnh báo line-ending hiện hữu.
- Không chạy migration và không ghi production database.

## Cập nhật chốt vòng Express Checkout và migration — 08/09/2026

- Express Check-out đã nối dữ liệu thật cho thanh toán Folio qua `ReceivePaymentCommand`, xác nhận Check-out qua `CheckOutHotelStayCommandHandler`, tạo task `DepartureClean` qua Housekeeping workflow và tự thu hồi key-card qua `IKeyCardService`.
- Hóa đơn điện tử trong Express chỉ tạo **nháp** từ các khoản Folio thật qua `IEInvoiceService`; phát hành ra nhà cung cấp vẫn fail-closed vì production provider đang `Unavailable` nếu chưa cấu hình kết nối ngoài.
- Bổ sung migration `20260908153709_AddHotelRoomHousekeepingStatus` cho cột `fo.PhongKhachSan.HousekeepingStatus` (`nvarchar(30)`, nullable). Phát hiện này đến từ integration test: snapshot đã có thuộc tính nhưng migration lịch sử chưa tạo cột.
- Sửa fixture LocalDB: migrate database sạch tới đúng migration ngay trước `CreateFrontOfficeFoundationTechnicalStores`, sau đó nâng lên bản hiện tại; không còn dùng `EnsureCreated` rồi xóa bảng cha trước bảng con.

### Kiểm chứng hiện tại

- Build `ASMILE.BO.App`: **PASS, 0 warning, 0 error** với lệnh build quiet.
- Unit suite: **673/673 pass, 0 skipped**.
- Integration LocalDB: **20/20 pass, 0 skipped**.
- E2E standalone: **4/4 pass, 0 skipped**.
- Chưa chạy migration trên database ứng dụng và chưa ghi production database. Migration mới chỉ được tạo trong source để áp dụng qua quy trình preflight/backup riêng.

## Cập nhật đường chạy Khách hiện tại — 08/09/2026

- `CurrentGuestsWindow` vẫn tải danh sách khách từ Stay và occupant thật; nay bổ sung đọc `OutstandingBalance` từ Folio theo từng Stay, nên số dư và bộ lọc Folio không còn hiển thị trạng thái tĩnh.
- Hai nút `Gia hạn lưu trú` và `Đổi phòng` trong tab chi tiết đã được gắn vào `ExtendStayWindow`/`RoomMoveExtendWindow`; trước đó chỉ tạo card giao diện nhưng không có handler click.
- Build App: **PASS, 0 errors**. E2E sau thay đổi: **4/4 pass, 0 skipped**.

## Cập nhật đường chạy Khách đi hôm nay — 08/09/2026

- `TodayDeparturesWindow` nay đọc Folio theo từng Stay thật để hiển thị `FolioNumber`, `OutstandingBalance`, trạng thái Folio và cảnh báo nợ; không còn gắn “Chưa kiểm tra”/cảnh báo cho mọi khách khi chưa có dữ liệu.
- Nút xem Folio, yêu cầu chốt Minibar/Housekeeping, gia hạn và xem hồ sơ khách đã truyền đúng Stay/phòng đang chọn vào workflow tương ứng. Khi thiếu định danh thật, thao tác dừng rõ ràng.
- Build App sau sửa: **PASS, 0 errors**. E2E: **4/4 pass, 0 skipped**.

## Cập nhật — Điều chỉnh thông tin lưu trú ghi dữ liệu thật — 08/09/2026

- `StayInformationAdjustmentWindow` đã nối `StayAdjustmentApplicationService` thay cho MessageBox mock ở nút Xác nhận.
- Màn hình truyền `HotelStayId`, `RowVersion`, phòng hiện tại, ngày đi mới và lý do vào command; service commit thay đổi ngày đi, lịch sử Stay và audit trong cùng persistence session.
- Phạm vi cố ý giới hạn ở ngày đi/lý do. Các trường GuestProfile, rate, discount, conflict và yêu cầu đặc biệt chưa có command tương ứng vẫn chỉ hiển thị dữ liệu đọc được hoặc trạng thái thiếu API, không ghi giả.
- Bổ sung kiểm tra fail-closed cho Stay, RowVersion, ngày đi phải tăng và lý do bắt buộc; sau commit hiển thị Stay đã đọc lại từ database.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 748 warning hiện hữu của solution.
- Unit suite gần nhất sau luồng khách đi cùng: **669/669 pass, 0 skipped**.
- Không chạy migration và không ghi production database trong thay đổi UI này.

### Gap còn lại đã xác nhận qua call-site

- `InvoiceManagementWindow.OnAdjust`: chưa có command persistence điều chỉnh hóa đơn; đang fail-closed.
- `ShiftLogWindow.OnCreateLog`: chưa có schema/command nhật ký ca riêng; danh sách hiện dùng lịch sử CashShift thật, nút tạo mới vẫn fail-closed.
- Receipt Voucher/Invoice export-print-email và một số tiện ích Group/Housekeeping vẫn cần contract/provider tương ứng; không bật mock để che thiếu backend.

## Cập nhật rà soát mock — À La Carte không còn tự seed dữ liệu mẫu — 08/09/2026

- `AlacarteRestaurantViewModel.LoadAsync` trước đây gọi `SeedDefaultExtraServicesAsync` ngay khi danh sách phòng/bàn rỗng. Đây là ghi dữ liệu mẫu tự động trong đường chạy mở màn hình, có thể làm database mới xuất hiện dịch vụ dù người dùng chưa yêu cầu.
- Đã bỏ auto-seed. Màn hình chỉ đọc phòng, bàn, đơn, hợp đồng, dịch vụ và menu qua service/catalog thật; dữ liệu rỗng được giữ rỗng. Lệnh `BootstrapSampleDataAsync` vẫn là thao tác chủ động riêng, không chạy tự động.
- `LoadMenuItemsAsync` không còn nuốt lỗi với chú thích “fallback sample”; lỗi đọc menu được log và hiển thị rõ trong `StatusText`, không chuyển sang dữ liệu giả.
- Các `Random` còn lại trong `RoomMaster`, AR, Housekeeping và một số cửa sổ chứng từ đều đã được xác định là nhánh sau `return`/fixture legacy; không phải dữ liệu active của màn hình. Không xóa trong vòng này để tránh làm nhiễu thay đổi hiện hữu.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- Unit suite (loại integration): **657/657 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Cập nhật triển khai — Điều chỉnh lưu trú ghi Folio thật cùng transaction — 08/09/2026

- Bổ sung `StayAdjustmentApplicationService` và `AdjustStayCommand` cho màn `Gia hạn lưu trú`/`Điều chỉnh lưu trú`.
- Workflow đọc Stay, phòng đích và Folio thật; nếu Stay chưa có Folio thì tạo Folio thật trong cùng transaction.
- Đổi phòng + gia hạn + phát sinh tiền phòng được commit chung: số dương ghi `RoomCharge`, số âm ghi `Discount`, số 0 không tạo khoản giả.
- Có kiểm tra quyền, Stay `RowVersion`, Folio `RowVersion`, phòng khả dụng, Stay đang ở và ngày đi mới; lỗi hoặc concurrency conflict không để lại cập nhật nửa chừng.
- UI không còn thông báo “chưa có contract posting tariff”; sau khi lưu hiển thị số Folio và khoản đã ghi thật.
- Không cần migration mới; chỉ dùng các bảng Stay/Folio hiện hữu. Không chạy migration và không ghi production database.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite hiện tại (loại integration): **652/652 pass, 0 skipped**.
- Đã thêm `StayAdjustmentPersistenceTests`: test InMemory dùng repository EF thật, xác nhận một commit ghi đồng thời lịch sử Stay và khoản `RoomCharge` vào Folio.
- Đã chuẩn hóa provider-safe `RowVersion` về 8 byte (đúng hình dạng SQL Server); trước đó InMemory/SQLite tạo 16 byte từ GUID làm workflow fail-closed dù dữ liệu hợp lệ.

## Cập nhật triển khai — Transaction Center xuất giao dịch Folio thật — 08/09/2026

- Nút `Xuất` trong `TransactionCenterWindow` đã dùng đúng danh sách giao dịch sau bộ lọc hiện tại (`GetFiltered()`), không còn chỉ hiển thị thông báo giả.
- Dữ liệu xuất là CSV UTF-8 có BOM, gồm mã giao dịch, thời gian, ngày nghiệp vụ, loại, nhóm, diễn giải, khách/phòng, Folio, số tiền, tiền tệ, trạng thái và nguồn.
- Nút `Void/Reverse` vẫn dùng command Folio thật và tải lại ledger sau khi thành công.
- Nút `Đối soát` tiếp tục fail-closed vì chưa có query/command đối soát tương ứng; không báo thành công giả.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite (loại integration): **657/657 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Đính chính audit dữ liệu mẫu và triển khai tiếp — 08/09/2026

- Rà soát graph xác nhận `ExpressCheckoutWindow`, `QuickCheckoutWindow`, `CheckoutPreparationWindow` và `ExtendStayWindow` đều nạp Stay/Folio/ledger/rate plan thật trong `Loaded`; khi thiếu service hoặc property context thì fail-closed. Các helper `LoadGuests()` sinh 120 khách mẫu trong bốn view này không có caller runtime và đã được gỡ.
- `MergeFolioWindow` nạp Folio thật qua `HotelFolioApplicationService.SearchAsync` và gọi `MergeAsync` với FolioGuid/RowVersion thật; helper `LoadFolios()` dựng 120 Folio mẫu đã được gỡ.
- `CashierVoucherWindow` và `IssueInvoiceWizard` khởi tạo view model/repository thật; các helper `LoadVouchers()`/`LoadFolios()` cũ không có caller và đã được gỡ, tránh nhầm là nguồn dữ liệu vận hành.
- `GroupCheckoutWindow` đã có query đoàn và command checkout thật, nhưng các bước Folio/Payment/Invoice chưa có composite command phù hợp; chỉ được mở khi đủ trạng thái phòng và assignment, không xác nhận giả.
- `IExchangeRateQueryService`/`IExchangeRateCommandService` và `EfExchangeRateService` đã tồn tại, nên Exchange Rate không được xếp vào nhóm mock; cần chỉ kiểm tra dữ liệu/property context và smoke test thật trước khi mở rộng.

### Kiểm chứng sau dọn fixture

- Build `ASMILE.BO.App`: **PASS, 0 errors**, 742 warnings hiện hữu.
- Unit suite: **657/657 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật — Xuất danh sách hóa đơn thật theo bộ lọc — 08/09/2026

- `InvoiceManagementWindow` đã đọc danh sách qua `IEInvoiceService.GetInvoicesAsync`; không còn fallback tạo hóa đơn mẫu khi thiếu service.
- Nút `Xuất` đã được triển khai thành xuất CSV UTF-8 từ đúng danh sách hóa đơn thật sau khi áp dụng tìm kiếm, loại, trạng thái và nhóm nghiệp vụ hiện tại. File có BOM để mở đúng tiếng Việt trong Excel.
- Khi không có dòng dữ liệu, hệ thống dừng và báo rõ; không tạo file rỗng giả. Email vẫn fail-closed vì chưa có email service và audit persistence.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 742 warning hiện hữu của solution.
- Không chạy migration và không ghi production database.

## Cập nhật tiếp — Lịch sử phòng khóa đọc audit thật — 08/09/2026

- `LockedRoomWindow` đã bổ sung nút xem lịch sử: đọc lại `HotelRoom` theo số phòng, sau đó truy vấn `IAuditService.QueryAsync` theo `HotelRoomId`.
- Lịch sử hiển thị tối đa 50 bản ghi gần nhất gồm thời điểm, thao tác, người thực hiện và chi tiết; không tạo timeline giả.
- Nếu audit service hoặc bản ghi phòng không tồn tại, màn hình báo rõ trạng thái thay vì coi dữ liệu trống là thành công.
- Gia hạn khóa vẫn chưa bật vì `HotelRoom` hiện chỉ có trạng thái vận hành, chưa có kỳ hạn/ngày bắt đầu/ngày kết thúc hoặc command tương ứng.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 742 cảnh báo hiện hữu của solution.
- Không phát sinh migration/model change và không ghi production database.

## Dọn fixture chết — Housekeeping Message — 08/09/2026

- Xóa `HousekeepingMessageWindow.LoadMockData`, vì màn hình đã tải board task qua `IHousekeepingBoardService` và helper không còn caller runtime.
- Không thay đổi đường dữ liệu thật; mục tiêu là tránh fixture cũ quay lại đường chạy trong các lần refactor sau.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.

## Cập nhật triển khai — Phòng khóa dùng command trạng thái thật — 08/09/2026

- `LockedRoomWindow` tiếp tục đọc danh sách phòng khóa từ `IReceptionUiService`/Hotel Master Data, không dùng fixture runtime.
- Bổ sung chọn phòng thật, chọn trạng thái `OutOfOrder` hoặc `OutOfService`, rồi gọi `ChangeHotelRoomOperationalStatusCommandHandler` với `HotelRoomId` và `RowVersion` thật.
- Bổ sung mở khóa phòng đã chọn bằng cách đọc lại phòng theo số phòng và chuyển trạng thái về `Available`; thao tác thành công sẽ tải lại danh sách từ database.
- Xóa helper `LoadMockData` chết khỏi màn hình; các chức năng gia hạn/lịch sử khóa vẫn fail-closed vì chưa có model/query lịch sử tương ứng.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; cảnh báo hiện hữu của solution vẫn còn.
- Nhóm test `HotelStayFoundationTests`: **60/60 pass, 0 skipped**.
- `dotnet ef migrations has-pending-model-changes`: **không có thay đổi model chờ migration**.
- Không chạy migration và không ghi production database.

## Cập nhật audit và triển khai tiếp — Group Booking/Checkout — 08/09/2026

- `GroupBookingWizardWindow` không còn để trống danh sách đoàn: khi có property context, màn hình đọc `IGroupReservationQueryService` thật; loại phòng được đối chiếu qua `IHotelRoomMasterDataRepository`; giá lấy từ rate plan active và tính qua `RatePlan.CalculatePrice`.
- Bổ sung `ConfigureGroupBookingAsync` trong `IGroupReservationCommandService` và `EfGroupReservationCommandService`. Lưu nháp/xác nhận kiểm tra trạng thái đoàn, property, tổng số phòng, loại phòng active, ngày và room block; ghi audit và commit trong một transaction. Không ghi thành công giả.
- Xác nhận rõ phạm vi: wizard hiện lưu thật đoàn + room block + negotiated rate. Lịch đặt cọc, rooming list khách dạng tự do và policy/service checkbox chưa có bảng/contract phù hợp nên vẫn hiển thị cảnh báo và không ghi giả.
- `GroupCheckoutWindow` đã có query/command EF thật; đã sửa nút xác nhận bị disable cố định, nối chọn tất cả/bỏ chọn assignment và chỉ bật xác nhận khi có đoàn, phòng hợp lệ và command thật.
- Rà lại các `LoadMockData` còn tên trong Current Guests, Today Arrivals/Departures, Locked Room và Housekeeping Message: đều không có caller runtime sau khi màn hình đã chuyển sang repository/service thật; giữ lại như fixture chết để dọn riêng, không xem là dữ liệu đang hiển thị.
- Khoảng trống còn có ảnh hưởng vận hành: command lock/unlock/extend/history của Locked Room; folio/payment allocation và chứng từ Receipt/Proforma; export/print/email; một số bước dịch vụ/bàn giao của Group Checkout; Housekeeping photo/batch/link; Group guest/service/billing tiện ích; các `Unavailable*` của cấu hình nền tảng.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Full unit suite sau patch Check-out: **650/650 pass, 0 skipped**.
- `dotnet ef migrations has-pending-model-changes`: **No changes**; không tạo migration mới cho contract/command này.
- Đã giải phóng dung lượng ổ C; các test vẫn chủ động dùng thư mục tạm trên `D:\ASMILE-BO\.tmp\unit-test-temp-20260908` để không phụ thuộc ổ hệ thống.
- Chưa chạy migration và chưa ghi production database.

## Cập nhật — khóa posting theo BusinessDate thật — 08/09/2026

- Đã kiểm tra lại toàn bộ đường posting sau khi thay `PropertyLockManager` từ stub sang đọc/ghi `BusinessDateRecord` thật.
- `FrontOfficePostingEngine.ValidateAsync` và `PostAsync` hiện từ chối giao dịch thường khi property đang `Closing` vì Night Audit; lỗi trả về rõ ràng và không chạm vào Folio.
- Room charge do chính Night Audit tạo được phép đi qua bằng cặp `ReferenceType = NightAudit` và `ReferenceId = NightAuditId` đang giữ lock. Bước Night Audit cũng bắt buộc `LockAsync` thành công trước khi đọc stay và post charge.
- `CanPostAsync` hiện trả `false` khi property bị khóa; không còn chỉ kiểm tra context có BusinessDate.
- Bổ sung test xác nhận trạng thái lock thật được lưu/release và posting engine chặn khi khóa; full unit suite hiện đạt **632/632 pass, 0 skipped**.

### Kết luận audit hiện tại

- Các đường đã kiểm chứng đang đọc dữ liệu thật hoặc fail-closed khi thiếu backend; các helper `Random`/`LoadMockData` còn lại trong nhóm đã quét là mã chết, không phải dữ liệu hiển thị runtime.
- Các stub legacy `Task.FromResult` của Concierge/Executive/Maintenance/Cashier chưa đăng ký DI production; không được bật trước khi có repository/query/command và migration thật.
- Khoảng trống cần triển khai tiếp: Marketing persistence, export/print/email provider, Incident/Shift write, lịch sử Key-card chi tiết, liên kết Housekeeping và các bước thanh toán/hóa đơn Express/Quick checkout.
- Chưa chạy migration, chưa ghi database production và không thay connection string.

### Phạm vi kiểm thử solution

- Unit suite sau khi khóa posting theo BusinessDate: **632/632 pass, 0 skipped**.
- Khi chạy toàn bộ solution, unit vẫn **632/632 pass**. Integration hiện có **12 pass, 8 fail** do fixture `PrepareSchemaAtFoundationPredecessorAsync` dựng schema nền không loại bỏ đầy đủ các bảng/sequence được tạo ở các migration sau; lỗi lặp lại tại bước `MigrateAsync`, không phải lỗi nghiệp vụ mock trên đường chạy.
- E2E hiện có **3 pass, 1 fail** ở smoke test điều hướng Restaurant với dialog lỗi; cần tách điều tra startup/seed của A La Carte khỏi audit Hotel/Front Office.

## Cập nhật — AR Master ghi thật và ổn định hóa scheduler cancellation — 08/09/2026

- `CustomerAppService` đã bổ sung đọc một khách hàng theo id; `ArCustomerViewModel` và `ArCompanyViewModel` không còn cập nhật tên/điện thoại/địa chỉ/trạng thái chỉ trong collection của màn hình. Các thao tác sửa và khóa/mở khóa hiện tải bản ghi thật, gọi `SaveAsync`, ghi audit và tải lại từ database.
- Nếu thiếu `CustomerId`, thiếu `ICustomerAppService` hoặc bản ghi không tồn tại, thao tác dừng với thông báo lỗi; không báo thành công giả. Email/export/PDF/print vẫn fail-closed vì chưa có provider thật.
- Bổ sung `CustomerAppServiceTests`: kiểm tra update thật bảo toàn các trường không sửa và kiểm tra duplicate code không làm thay đổi bản ghi hiện hữu.
- Job kiểm thử `CANCEL_TEST` của scheduler được giữ chạy đủ lâu để kiểm tra đúng chuỗi `Running -> Cancelled`; tránh tình trạng test quan sát được `Running` nhưng job đã hoàn tất trước khi `CancelAsync` được gọi.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Test riêng cancellation scheduler: **1/1 pass**.
- Full unit suite: **630/630 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

## Rà soát kỹ đường chạy mock còn lại — 08/09/2026

### Đã xác nhận không còn dùng mock trên đường mở màn hình

- `CurrentGuestsWindow`, `TodayArrivalsWindow`, `TodayDeparturesWindow`, `LockedRoomWindow`, `HousekeepingMessageWindow` và `ExchangeRateWindow` còn các helper tên `LoadMockData`, nhưng constructor không gọi chúng; dữ liệu được tải ở `Loaded` từ repository/service thật hoặc để trống khi thiếu context.
- `FolioReviewWindow`, `ProformaFolioWindow`, `IssueInvoiceWizard`, `TransactionCenterWindow`, `TransferPostingWindow`, `MergeFolioWindow`, `SplitFolioWindow`, `QuickCheckoutWindow` và `ExpressCheckoutWindow` còn generator `Random` cũ, nhưng đường khởi tạo thực tế dùng `Loaded` + Folio/Stay repository thật. Các helper này là mã chết cần dọn sau smoke test, không phải nguồn dữ liệu đang hiển thị.
- `ReceiptVoucherWindow` và `CashierVoucherWindow` chuyển sang `PaymentReceiptsViewModel`/`PaymentVouchersViewModel` khi DI resolve được; hai ViewModel này gọi `IReceivableService`/`IPayableService` thật. Nhánh fallback chỉ khởi tạo danh sách rỗng và thông báo thiếu query, không đổ dữ liệu giả.

### Còn mock/stub có rủi ro thực tế hoặc cần khóa phạm vi

1. **Đã xử lý rủi ro cao — Property lock của Night Audit:** `PropertyLockManager` đã nối đọc/ghi `BusinessDateRecord`, chỉ cho lock đúng business date đang mở, nhận diện đúng Night Audit đang giữ khóa, mở lại business date khi audit fail/cancel và không còn trả `true/false` cố định. Bổ sung test persistence lock–unlock; phần còn lại cần smoke test liên module để xác nhận mọi posting command đều tôn trọng lock.
2. **Rủi ro trung bình — engine legacy chưa có persistence:** `ConciergeEngine`, `ExecutiveQueryService`, `MaintenanceEngine` và `CashierDashboardService` trả DTO/số liệu dựng sẵn bằng `Task.FromResult`. Hiện chưa thấy chúng được đăng ký trong `ASMILE.BO.Infrastructure/DependencyInjection.cs`, nên chưa kết luận là dữ liệu đang hiển thị trên UI; tuy nhiên không được đăng ký chúng vào production trước khi có query/command, bảng và migration thật.
3. **Thiếu backend có chủ đích:** Marketing, export/print/email, lịch sử key-card chi tiết, incident/shift log write và một số bước Express/Quick checkout đang hiển thị “chưa có dữ liệu/API thật” hoặc fail-closed. Đây là trạng thái an toàn, không phải mock, nhưng vẫn là khoảng trống nghiệp vụ cần hoàn thiện.
4. **Mã mẫu cần dọn:** các generator `Random`/`LoadMockData` đã được xác nhận không có call-site runtime trong nhóm trên. Nên xóa theo từng nhóm sau khi hoàn tất smoke test để tránh tái sử dụng nhầm; không bật chúng làm fallback.

### Kết luận và thứ tự triển khai tiếp theo

1. Bổ sung smoke/integration test Night Audit không cho posting xuyên qua thời gian khóa.
2. Hoàn thiện các query/command thật còn thiếu cho Housekeeping liên kết, Marketing, Incident/Shift và Express/Quick checkout.
3. Sau đó mới dọn helper mock chết và chạy lại smoke test cho từng card; không dùng việc build/test xanh để kết luận dữ liệu production đã đầy đủ.

### Kiểm chứng rà soát

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Full unit suite sau các thay đổi hiện tại: **631/631 pass, 0 skipped**.
- Kết quả trên là kiểm thử code/in-memory; chưa chạy migration hoặc ghi database production.

## Cập nhật — khóa các thao tác AR còn ghi giả và nối Invoice preview thật — 08/09/2026

- `ArInvoiceViewModel` giữ `InvoiceId` thật từ `InvoiceSummaryDto`; nút In gọi `IEInvoiceService.GeneratePdfPreviewAsync` và mở preview từ dữ liệu backend. Không tự tăng `PrintCount`/audit khi chưa có command ghi nhận máy in.
- `ArInvoiceViewModel.ExportExcel` đã xuất CSV từ danh sách hóa đơn thật; Export PDF, Email, Hủy hóa đơn không còn báo thành công giả khi chưa có provider/command tương ứng.
- `ArCompanyViewModel`, `ArCustomerViewModel` và `ArLedgerViewModel` không còn sửa DTO, thêm audit/timeline/note hoặc báo Email/Print/Export thành công chỉ trong bộ nhớ. Các nút thiếu command hiện fail-closed.
- `ArReconcileViewModel` không còn báo đã xuất/in khi chưa có report/print service; Match/AutoMatch/UndoMatch vốn đã fail-closed khi chưa có backend allocation.
- Các loader chính của Company/Customer/Ledger/Invoice/Reconcile vẫn đọc projection thật hoặc hiển thị rỗng khi thiếu service; các block `Random` còn lại nằm sau nhánh return/legacy và không phải đường chạy runtime.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors**, 740 warnings hiện hữu của solution.
- Nhóm test AR trọng tâm: **10/10 pass** (`ReconciliationEngineTests` + `ReceivablePaymentAllocationTests`).
- Chưa chạy migration, chưa ghi production database và chưa thay connection string.

### Khoảng trống cần triển khai tiếp

1. Bổ sung command/service thật cho Company/Customer Master update, lock/unlock; AR Ledger close/note; email provider và print/report provider.
2. Bổ sung command hủy E-Invoice và command ghi nhận print/email có audit, quyền và idempotency.
3. Bổ sung projection payment/folio/line cho Invoice; hiện các trường không có trong `InvoiceSummaryDto` được để rỗng có chủ đích.
4. Sau khi có contract, viết integration test trên database test có dữ liệu thật; không dùng lại generator mẫu để làm fixture runtime.

## Rà soát thật kỹ mock còn lại và trạng thái hoàn thiện — 08/09/2026

### Kết luận theo đường chạy runtime

- Các loader đã được chuyển sang dữ liệu thật gồm Arrival/Departure/Current Guest, Stay/Folio, Housekeeping board, Room Master, Group Service, AR Aging và AR Payment. Các `Random`/`LoadMockData` còn lại trong những loader này nằm sau `return` hoặc không có caller runtime; không được dùng để đánh giá rằng giao diện đang hiển thị mock.
- `ReceiptVoucherWindow` và `CashierVoucherWindow` ưu tiên ViewModel thật qua DI. Nếu DI/query thật không có, chúng hiển thị rỗng và cảnh báo, không gọi generator mẫu.
- `CheckoutPreparationWindow`, `ExpressCheckoutWindow`, `FolioReviewWindow` và `QuickCheckoutWindow` khởi tạo collection rỗng, sau `Loaded` mới nạp Stay/Folio thật; helper sinh danh sách mẫu còn lại không có caller trong đường chạy hiện hành. Khi thiếu service/property context, UI dừng ở trạng thái thiếu dữ liệu.
- AR Reconcile đọc giao dịch công nợ thật qua `IReceivableService`; phần match/auto-match vẫn chưa có command ghi allocation/reconciliation thật nên đang khóa và không đánh dấu thành công giả.

### Mock đang chạy hoặc còn là gap thật cần hoàn thiện

1. **Đã xử lý P0 — AR đối chiếu ngân hàng:** panel AR Payment đã đọc `BankStatementLine` thật và gọi auto-match theo payment receipt thật; manual match/approval vẫn giữ fail-closed.
2. **P0 — chứng từ:** PDF/email receipt và invoice cần provider/command thật; hiện fail-closed, không tạo số chứng từ hay báo gửi thành công giả.
3. **Đã xử lý P0 — service đối soát nền:** `ReconciliationEngine.RunAutoMatchAsync` đã chuyển sang matching theo `BankStatementLine`/`ReceivableTransaction` thật, chống trùng và fail-closed khi thiếu `BankStatementId`.
4. **P1 — Housekeeping/Room Master/Group tiện ích:** ảnh kiểm tra, pause/resume, maintenance/minibar/lost-found/Folio/Reservation link, batch inspection, QR/history/export/print, cùng các tiện ích billing/report/group guest chưa có contract persistence đầy đủ.
5. **P1 — đổi phòng:** `RoomMoveWindow` đã gọi `MoveRoomAsync` thật sau xác nhận, nhưng `RoomMoveConfirmationWindow` vẫn hiển thị “Mockup UI” và nút “Xác nhận mockup”; cần thay modal bằng confirmation UI thật để không gây hiểu nhầm vận hành.
6. **P1 — nghiệp vụ nền:** Marketing CRUD, Shift/Incident/VIP note và một số cấu hình nền tảng còn read-only hoặc chưa có repository/command thật; không nên bật lại dữ liệu mẫu.
7. **Code hygiene:** các generator legacy sau `return` nên được xóa theo từng nhóm sau khi có smoke test để tránh tái kích hoạt nhầm; việc còn tồn tại trong source không đồng nghĩa đang dùng runtime.

### Kiểm chứng vòng này

- `ASMILE.BO.App`: build **PASS**.
- AR payment/allocation/reversal/undo-allocation/auto-allocation tests: **6/6 pass**.
- Số test được discover hiện tại: **624**.
- Test scheduler hủy execution chạy riêng: **1/1 pass**. Full suite vẫn có test timing-sensitive khi chạy cùng toàn bộ assembly; đây là gate kiểm thử cần ổn định hóa riêng, không phải bằng chứng dữ liệu Hotel đang mock.
- Không chạy migration, không ghi production database và không thay connection string.

### Phương án triển khai tiếp theo

Ưu tiên tạo một contract reconciliation thật cho BankStatementLine/payment receipt trước, sau đó làm provider chứng từ; tiếp theo mới triển khai các tiện ích Housekeeping/Room Master/Group theo cùng mẫu `query → command transaction → audit → reload`. Giữ toàn bộ phần chưa có schema/command ở trạng thái fail-closed cho tới khi có migration và integration test database riêng.

## Cập nhật — AR Aging dùng dữ liệu công nợ thật — 08/09/2026

- `ArAgingViewModel` không còn gọi `BuildFakeData()` khi khởi tạo; màn hình không còn tự sinh 800 công ty, hóa đơn, khoản thu và timeline mẫu.
- Loaded của `ArAgingView` gọi `IReceivableService` thật qua DI, lấy đồng thời `GetDebtSummariesAsync` và `GetTransactionsAsync`, có property scope nếu execution context hiện tại có property.
- Các dòng AR, hóa đơn, khoản thanh toán, timeline, aging bucket, KPI và collection trend được ánh xạ từ DTO công nợ thật. Các trường chưa có nguồn thật như collector, credit limit, documents và audit không còn bị điền giả.
- Recovery rate và outstanding trend không còn dùng `Random`; khi chưa có chuỗi dữ liệu lịch sử, trend chỉ hiển thị điểm hiện tại hoặc để trống.
- Khi thiếu service, lỗi truy vấn hoặc không có dữ liệu, màn hình rỗng và báo rõ trạng thái; không fallback sang mock.
- AR Payment không còn nút `SimulateBankImport`; panel đọc sao kê thật qua `IBankingService`, auto-match qua `IReconciliationEngine`, và chỉ giữ manual match ở trạng thái fail-closed khi chưa chọn phiếu thu đích.
- Các nút AR Payment cho email/in lại receipt, auto/undo allocation và reverse payment cũng đã chuyển sang fail-closed; chúng không còn tự đổi DTO trong bộ nhớ rồi báo thành công khi chưa có command/service ghi thật.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite: **618/618 pass, 0 skipped**; bổ sung 3 test AR Aging cho no-fake, mapping dữ liệu thật và fail-closed.
- Không chạy migration, không ghi production database và không thay connection string.

## Rà soát mock và liên kết dữ liệu lần cuối trong vòng này — 08/09/2026

### Kết luận theo đường chạy thực tế

- **Đang đọc dữ liệu thật:** Folio Workspace, Folio Review, Quick Check-out, Express Check-out, Issue Invoice, Proforma Folio, Tách Folio và Gộp Folio đều nạp từ repository/ledger thật khi mở màn hình. Các generator `LoadGuests`/`LoadFolios`/`LoadTransactions` còn nằm trong source là helper legacy không có call-site runtime; không được xem là dữ liệu đang hiển thị.
- **Đang fail-closed, không còn hiển thị mock:** Room Master, Housekeeping board/Dirty Room/Room Inspection, Group Service và các màn hình Group Room Assignment/Booking/Guest Management. Khi thiếu query/command/property context, UI để rỗng hoặc báo chưa sẵn sàng; không tự sinh dòng thành công.
- **Đã ghi dữ liệu thật:** phân phòng đoàn qua transaction; trạng thái dịch vụ đoàn; post dịch vụ đoàn vào `HotelFolioEntries` với `ReferenceType=GroupServiceOrder`, có property scope và chống ghi trùng; Folio split/merge theo Folio thật.
- **Đã hoàn thiện chuỗi nghiệp vụ check-in đoàn:** `EfGroupCheckInCommandService` dùng guest/room thật để tạo hoặc liên kết Reservation, tạo Stay thật, chuyển Stay sang `InHouse`, tạo Folio mở và chỉ sau đó mới đổi cờ assignment/group. Các bản ghi history/domain event/audit được stage trong cùng persistence session; dữ liệu thiếu guest, room, conflict hoặc chain không hợp lệ đều fail-closed.

### Các điểm còn cần triển khai, theo ưu tiên

1. **P0 — Group service:** bổ sung chọn Folio đích/master Folio thật và post hàng loạt; tiếp tục giữ fail-closed khi đoàn chưa có Stay/Folio.
2. **P1 — Housekeeping:** command thật cho ảnh kiểm tra, Maintenance/Minibar/Lost & Found/Folio/Reservation links và các thao tác batch còn khóa.
3. **P1 — Group/Room Master:** thêm khách, auto-pair/swap/split room, lock room, QR/history/liên kết Reservation/Folio/Room Move; chỉ mở từng nút sau khi có command + audit + test.
4. **P2 — đầu ra:** invoice/payment allocation/reversal, export/print/email; không dùng preview để báo đã ghi chứng từ.
5. **Dọn legacy:** xóa helper Random/LoadMockData sau khi có smoke test UI; không cần sửa để “làm cho có” vì chúng hiện không nằm trên call path.

### Ngoài phạm vi Hotel nhưng cần lưu ý

`BreakfastCafeSettingsViewModel` vẫn gọi `InitMockData()` ngay trong constructor. Đây là mock đang chạy thật của phân hệ Nhà hàng/Café, không phải nguồn dữ liệu của module Quản lý khách sạn; cần tách thành hạng mục riêng nếu muốn toàn bộ ứng dụng không còn mock runtime.

### Kiểm chứng vòng này

- Build `ASMILE.BO.App`: **PASS, 0 warning/error ở lệnh build riêng**.
- Test riêng Group check-in + Group service post + tạo chain thật: **3/3 pass**.
- Full unit suite cần chạy lại sau thay đổi chain check-in; lần kiểm chứng trước đó đạt **611/611 pass, 0 skipped** sau khi chạy lại test scheduler timing-sensitive.
- Không chạy migration, không ghi production database và không thay connection string.

## Rà soát sâu tiếp — Folio dịch vụ đoàn và đường chạy mock — 08/09/2026

- `GroupServiceOrder` đã có command `PostToFolioAsync`. Chỉ dịch vụ `Completed` mới được post; hệ thống ghi `HotelFolioEntry` thật với `ReferenceType=GroupServiceOrder`, phân loại khoản mục theo category, chuyển trạng thái sang `PostedToFolio` và ghi audit trong cùng persistence session.
- Command có kiểm tra property scope và chống ghi trùng theo `ReferenceType + ReferenceId`. Nếu không tìm được đúng một Folio mở liên kết qua assignment → reservation → stay thì dừng với mã rõ ràng; không tự tạo Folio mock và không chọn ngẫu nhiên một Folio khác.
- UI Dịch vụ đoàn đã gọi command thật. Khi dữ liệu thật chưa có liên kết Folio đích, UI báo nguyên nhân nghiệp vụ thay vì báo “thành công”. Luồng nhiều Folio vẫn cần bước chọn đích/master Folio thật trước khi mở rộng batch post.
- Đã thêm test EF `GroupServiceFolioPostingTests`: post 350 VND vào Folio thật, kiểm tra entry/category/reference/trạng thái và xác nhận lần hai không tạo thêm entry.

### Kết quả quét các helper mock còn lại

- Các helper `LoadMockData`/`LoadGuests`/`LoadFolios` trong Current Guests, Arrivals/Departures, Checkout, Folio Workspace, Quick/Express Checkout, Issue/Proforma/Split/Merge/Transfer là code fallback chết: constructor/Loaded hiện lấy repository/service thật hoặc fail-closed; helper không được gọi trên đường chạy hiện tại.
- `RoomMaster`, các màn hình Housekeeping và Minibar đã có nhánh fail-closed khi thiếu query service; các khối `Random` phía sau `return` chỉ là fixture lịch sử, không phải nguồn hiển thị runtime.
- `GroupEngine.cs` và các `Mock*Service` hạ tầng còn implementation mẫu nhưng không có registration production tương ứng trong `DependencyInjection.cs`; không được coi là dữ liệu thật. Cần dọn legacy sau smoke test, không bật lại làm fallback.
- Điểm mock đang chạy thật còn cần tách riêng: `BreakfastCafeSettingsViewModel.InitMockData()` của module Nhà hàng/Café; phần này nằm ngoài module Quản lý khách sạn và cần một pass dữ liệu master/config riêng.

### Khoảng trống nghiệp vụ còn lại cần làm tiếp

1. Bổ sung chọn Folio đích/master Folio cho dịch vụ đoàn và batch post sau khi mô hình liên kết đoàn–Folio đã có chain thật.
2. Bổ sung kiểm tra UI end-to-end với dữ liệu seed riêng: assignment có guest/room thật, check-in tạo đủ Reservation → Stay → Folio, rồi dịch vụ đoàn post vào Folio.
3. Sau đó mới xử lý batch payment allocation/reversal, invoice/provider và export/print; các nút chưa có contract vẫn phải fail-closed.

### Kiểm chứng vòng này

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Test riêng post dịch vụ đoàn: **1/1 pass**.
- Chưa chạy migration, chưa ghi production database và chưa tạo dữ liệu mẫu trong DB thật.

## Rà soát dữ liệu mock và hoàn thiện tiếp — 08/09/2026

### Kết luận đường chạy hiện tại

- Đã bổ sung nghiệp vụ **đặt cọc đoàn dùng dữ liệu thật**: `GroupReservation.TotalDeposit` được cập nhật trong cùng transaction với bản ghi `fo.DatCocDoan`; có property scope, audit, row-version/concurrency, ghi nhận và hoàn tác có lý do.
- Màn hình `Đặt cọc đoàn` đã thay MessageBox khóa thao tác bằng modal đọc/ghi thật. Khi chưa có bản ghi, danh sách để rỗng và hiển thị số lượng 0; không tự sinh dữ liệu mẫu.
- Migration `20260907181024_AddGroupDeposits` chỉ tạo `fo.DatCocDoan`, FK tới `fo.DatPhongDoan` và các index theo property/đoàn/ngày/trạng thái. Migration mới **chưa được apply** vào database thật.
- `CurrentGuestsWindow`, `TodayArrivalsWindow`, `TodayDeparturesWindow`, `HousekeepingMessageWindow`, `GroupCheckInWindow`, `GroupRoomAssignmentWindow`, `ExchangeRateWindow` và `LockedRoomWindow` hiện nạp qua repository/service thật hoặc rỗng/fail-closed. Các hàm tên `LoadMockData` còn lại không có caller trên đường `Loaded` hiện hành.
- Room Master khi thiếu repository/property context không chạy fixture; nhánh fixture cũ nằm sau `return` và không phải nguồn dữ liệu runtime. `ApplyMockFilters` chỉ là bộ lọc cục bộ cho snapshot đã nạp, không tự tạo phòng.

### Mock còn tồn tại nhưng đã phân loại

1. `CashierEngine`, `GroupEngine`, `ConciergeEngine`, `MaintenanceEngine`, `ChannelEngine`, `DoorLockEngine` và `ExecutiveEngine` vẫn chứa DTO/`Task.FromResult` mẫu. Đã xác nhận không có registration production tương ứng trong `DependencyInjection.cs`; đây là legacy/prototype, không được dùng làm nguồn dữ liệu.
2. `BreakfastCafeSettingsViewModel.InitMockData()` vẫn là mock đang chạy, nhưng thuộc màn hình cấu hình Breakfast/Cafe, không thuộc đường Hotel/Front Office đã audit. Nếu muốn đóng toàn bộ phạm vi ứng dụng thì đây là backlog riêng, cần schema/settings provider thật.
3. `CreateGroupWindow.MockDocument` chỉ là model tài liệu nhập trong form và chưa có persistence document; đây là phần thiếu nghiệp vụ, không được coi là chứng từ đã lưu thật.
4. E-Invoice và các provider tích hợp ngoài vẫn fail-closed (`Unavailable...`); không được đổi sang trạng thái thành công giả để che thiếu cấu hình.

### Thứ tự triển khai tiếp theo

1. **P0:** chạy migration trên database test có backup/verify, seed tối thiểu một property + group + folio thật, rồi smoke test ghi nhận/hoàn tác đặt cọc và concurrency.
2. **P0:** hoàn thiện Group Folio/invoice/payment allocation và liên kết khoản đặt cọc vào settlement; hiện `TotalDeposit` mới là ledger đặt cọc đoàn độc lập, chưa tự phân bổ vào hóa đơn.
3. **P1:** hoàn thiện Housekeeping batch/photo/maintenance/minibar/lost-found và liên kết Reservation/Folio; giữ fail-closed cho các nút chưa có contract.
4. **P1:** hoàn thiện Group Guest import/document và Room Master QR/history/link/export/print.
5. **P2:** thay provider E-Invoice/integration thật và dọn các helper mock chết sau khi smoke test hồi quy.

### Kiểm chứng vòng này

- Build `ASMILE.BO.Infrastructure`: **PASS, 0 errors**.
- Build `ASMILE.BO.App`: **PASS, 0 errors**; còn warning hiện hữu của solution.
- Unit `GroupReservationWorkflowTests`: **9/9 pass**.
- EF nhận migration `AddGroupDeposits`, `AddFrontOfficeIncidents`, `AddHotelExchangeRates` và `AddGroupRoomBlocks` ở trạng thái Pending; không có migration nào được apply trong vòng này.

## Rà soát thật kỹ đường mock sau các đợt triển khai — 08/09/2026

### Đã xác nhận không còn là mock trên đường chạy chính

- `SplitFolioWindow` và `MergeFolioWindow` tải Folio/khoản mục từ `HotelFolioApplicationService` và xác nhận bằng `SplitAsync`/`MergeAsync`; các hàm tên `LoadFolios`, `BuildMockForm`, `PreviewMockTarget` còn trong source nhưng không được gọi để nạp dữ liệu production. Khi thiếu context hoặc Folio thật, màn hình báo lỗi/fail-closed.
- `CurrentGuestsWindow`, `TodayArrivalsWindow` và `RoomMoveWindow` lấy Stay/Room từ repository EF trong `Loaded`; các `LoadMockData`/`CreateMockGuests`/`CreateMockRooms` còn lại là helper legacy không được gọi trên đường nạp dữ liệu hiện hành. Các thao tác chưa có command chỉ cảnh báo, không ghi thành công giả.
- `GroupEngine`, `CashierEngine`, `ConciergeEngine`, `MaintenanceEngine`, `ChannelEngine`, `HousekeepingEngine`, `DoorLockEngine` và `ExecutiveEngine` còn DTO mẫu/`Task.FromResult`, nhưng không có registration production tương ứng trong `DependencyInjection.cs`; chúng chỉ là legacy/prototype/test-only.

### Đã loại bỏ mock còn chạy thật

- `NotificationTaskCenterViewModel` trước đây tự ghi 6 notification và 5 task mẫu khi database rỗng. Cơ chế auto-seed đã được gỡ; kho rỗng nay hiển thị rỗng, chỉ dữ liệu do nghiệp vụ thật tạo mới được hiển thị.

### Vẫn còn thiếu dữ liệu thật hoặc cần triển khai tiếp

1. Group: deposit/master folio/invoice/report/export/print/email và guest import/document.
2. Housekeeping: batch/photo/maintenance/minibar/lost-found/Folio/Reservation links và các đầu ra export/print.
3. Room Master/Key-card: QR, lịch sử/liên kết nghiệp vụ, một số mutation và export/print.
4. Shift/Incident/VIP/Housekeeping Message và các provider tích hợp ngoài: cần query/command/provider thật; không bật lại fixture.
5. E-Invoice: production đang fail-closed qua `UnavailableEInvoiceProvider`; chưa được coi là đã phát hành thật.

### Kiểm chứng cập nhật

- Migration chain đã được EF nhận đủ `20260907203000_AddFrontOfficeIncidents`, `20260907221500_AddHotelExchangeRates` và `20260908090000_AddGroupRoomBlocks`; script kiểm tra có đủ `fo.SuCo`, `fo.TyGia`, `fo.RoomBlockDoan`.
- Build Infrastructure: **PASS, 0 errors**.
- Build App: **PASS, 0 errors**; còn warning hiện hữu của solution.
- Chưa chạy migration vào database thật và chưa tự tạo dữ liệu mẫu trong production.

## Rà soát đường mở module — Đặt phòng đoàn không còn đi vào prototype mock — 08/09/2026

- Card `Đặt phòng đoàn` trong `FrontOfficeHubViewModel` trước đây mở `GroupBookingWizardWindow`, một wizard legacy chỉ giữ đoàn/phòng block/giá/đặt cọc trong bộ nhớ và không có command persistence.
- Card này đã được chuyển sang `CreateGroupWindow`, là wizard đang ghi `GroupReservation` thật và ghi các `GroupServiceDraft` thành `fo.DichVuDoan` trong cùng transaction. Vì vậy người dùng không còn đi vào luồng cho phép nhập dữ liệu nhưng không lưu.
- `GroupBookingWizardWindow` và các engine mẫu (`GroupEngine`, `CashierEngine`, `ConciergeEngine`, `HousekeepingEngine`, `ExecutiveEngine`, `DoorLockEngine`) hiện không phải implementation production được đăng ký DI; chúng được xếp vào legacy/prototype, không dùng để kết luận dữ liệu thật của màn hình đang chạy.
- Các helper `LoadMockData`, `LoadGuests`, `LoadReceipts`, `LoadTransactions` còn trong source đều đã đối chiếu với constructor/`Loaded`: các màn hình Hotel đang dùng loader repository thật hoặc fallback rỗng/fail-closed. Chúng vẫn cần dọn mã sau khi smoke test, nhưng không được phép bật lại như fallback production.
- Khoảng trống nghiệp vụ còn lại có thật: room block độc lập theo loại phòng/cutoff/rate, cấu hình và thu đặt cọc đoàn, lưu chứng từ đoàn, cùng các tiện ích export/print/email. Các phần này tiếp tục bị khóa/fail-closed; không coi tổng `ExpectedRooms` của GroupReservation là room block tồn thật.

### Kiểm chứng sau cập nhật

- Cần build App và chạy unit suite sau thay đổi route này.
- Không chạy migration, không ghi production database và không thay connection string.

## Rà soát mock tiếp theo — Rooming List và DI production — 07/09/2026

### Kết quả kiểm tra đường chạy thật

- `GroupRoomingListWindow` lấy đoàn từ `IGroupReservationQueryService`, Rooming List từ `IRoomingListService` và production DI đăng ký `EfRoomingListService`; lớp `GroupEngine.RoomingListService` trả dữ liệu mẫu chỉ là legacy, không phải implementation production.
- Giao diện đã giữ nguyên nguyên tắc không tự sinh dữ liệu: `+ Thêm khách` và `Import Excel` tiếp tục khóa vì chưa có mutation/import contract cho hồ sơ khách đoàn.
- Nút `Xuất CSV` hiện gọi `IRoomingListService.ExportAsync` và chỉ ghi các dòng đọc được từ database thật; không còn xuất bản ghi mẫu.
- Khi assignment có `AssignmentId` và `GuestId` thật, giao diện đã nối `AssignGuestAsync` + `MarkArrivalAsync` cho ghi nhận đến và `MarkDepartureAsync` cho ghi nhận đi. Sau commit, màn hình tải lại từ database; lỗi không đổi collection để giả thành công.

### Mock còn thấy khi quét production path

- Các engine `CashierEngine`, `GroupEngine`, `ConciergeEngine`, `HousekeepingEngine`, `ChannelEngine`, `DoorLockEngine` và `ExecutiveEngine` còn `Task.FromResult`/DTO mẫu nhưng không có đăng ký production tương ứng trong `DependencyInjection.cs`; không được dùng làm bằng chứng dữ liệu thật.
- `DependencyInjection.cs` vẫn đăng ký mock thật cho các dịch vụ nền tảng `ISystemSettingsService`, `IBackupService`, `IServiceStatusService`, `ISecuritySettingsService`, `IPrinterSettingsService`; đây là phần quản trị hệ thống ngoài nghiệp vụ Hotel, cần thay bằng provider persistence/OS thật trước khi gọi nền tảng hoàn tất.
- Ba `IEInvoiceProvider` production registration hiện là `StubEInvoiceProvider`; đây là đường phát hành E-Invoice của module kế toán. Không được coi trạng thái `Issued` từ provider này là đã gửi cơ quan/nhà cung cấp thật; cần adapter provider thật và cấu hình credential/endpoint trước khi bật production.
- Trong Hotel/Front Office, các phần còn thiếu chủ yếu là import/thêm/sửa hồ sơ khách đoàn, document scan, một số tiện ích Room Master/Housekeeping, email/integration và các thao tác Group Folio chưa có contract. Các nút này phải tiếp tục fail-closed, không bật mock.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- Unit suite: **603/603 pass, 0 skipped**. Test scheduler đã chạy riêng đạt **1/1**.
- Chạy toàn bộ solution test: Integration SQL Server/LocalDB fail **8/20** do môi trường database integration hiện tại; E2E **4/4 pass**. Đây là blocker môi trường kiểm thử, không phải lỗi build của Rooming List.
- Không chạy migration, không ghi production database và không thay connection string.

## Khóa phát hành hóa đơn điện tử giả — 07/09/2026

- `StubEInvoiceProvider` vẫn được giữ để phục vụ unit test tường minh, nhưng đã bị gỡ khỏi production DI.
- Production DI hiện đăng ký `UnavailableEInvoiceProvider` cho các mã Viettel/VNPT/MISA. Provider này chủ động fail-closed; workflow ghi log `Failed`, trả hóa đơn về `Draft` và không thể ghi `ProviderInvoiceId`/`Issued` giả.
- Việc phát hành E-Invoice thật vẫn cần adapter API, endpoint, credential và kiểm thử tích hợp của nhà cung cấp trước khi bật lại.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- E-Invoice unit tests: **2/2 pass**.
- Full unit suite: **603/603 pass, 0 skipped**.

## Đính chính sau kiểm tra đường chạy — Phiếu thu FO và hóa đơn — 07/09/2026

- `CashierReceiptWindow` hiện mở `CreateReceiptVoucherWindow` đã có sẵn. Cửa sổ này tải Folio thật, kiểm tra quyền/số tiền/currency/row-version và ghi qua `HotelPaymentApplicationService.ReceiveAsync`; sau khi đóng thành công, danh sách phiếu thu được tải lại từ payment thật. Vì vậy kết luận cũ “nút tạo phiếu vẫn fail-closed vì chưa có form/command” ở snapshot bên trên đã được thay thế.
- `InvoiceManagementWindow` đã nối nút `In` vào `IEInvoiceService.GeneratePdfPreviewAsync` và mở preview từ dữ liệu service thật; các nút chưa có contract ghi tương ứng vẫn fail-closed, không báo thành công giả.
- Nút `Phát hành HĐ` của `InvoiceManagementWindow` nay mở `IssueInvoiceWizard` thật. Wizard tải Folio thật và gọi `SettlementApplicationService.GenerateInvoiceAsync` có kiểm tra row-version; sau khi thành công, danh sách hóa đơn được tải lại từ DB.
- Các nút `Tự động phân phòng`, `Gán phòng` và `Phân phòng hàng loạt` của `GroupReservationViewModel` nay mở `GroupRoomAssignmentWindow` thật với đúng đoàn đang chọn; wizard tải rooming list/phòng thật và ghi qua `IGroupRoomAssignmentCommandService`, thay vì chỉ báo chưa có command hoặc sửa state trong memory.
- Các nút `Bulk Check-in` và `Bulk Check-out` của dashboard đoàn nay mở `GroupCheckInWindow`/`GroupCheckoutWindow` thật; không còn dừng ở MessageBox “chưa có command”. Hai workflow tự tải nhóm/phòng thật và dùng command service riêng để ghi nghiệp vụ.

### Kiểm chứng sau khi nối workflow đoàn

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite chạy lại ổn định: **603/603 pass, 0 skipped**. Một lần chạy trước đó có lỗi timing đơn lẻ ở test scheduler; chạy riêng và chạy lại toàn bộ đều đạt.
- Các thao tác cũ `In/Xuất/Điều chỉnh/Hủy` của `ReceiptVoucherWindow` và `ShiftLogWindow` nay hiển thị rõ “chưa có command backend”, không còn chữ “Mockup” gây hiểu nhầm về dữ liệu danh sách.

### Kiểm chứng lại

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite: **603/603 pass, 0 skipped**.

## Cập nhật — Đối soát thật và Phiếu thu FO — 07/09/2026

- `ReconciliationDetailDialog` không còn dựng 8 dòng giao dịch tĩnh; bảng đối soát lấy opening balance, các giao dịch và tổng kết từ dữ liệu truyền vào từ workflow thật. DTO hiện tại chưa tách riêng phân loại company/customer nên hai tab dùng cùng tập giao dịch và không tự suy diễn phân loại.
- `StayProfileWindow` đọc Stay và `GuestProfile` thật theo property; nút chỉnh sửa hồ sơ ghi qua `IGuestProfileRepository.UpdateAsync` với row-version/actor hiện tại rồi tải lại từ DB. Phạm vi chỉnh sửa là hồ sơ khách, không giả vờ chỉnh ngày ở hoặc phòng khi chưa có command tương ứng.
- `TransferPostingWindow` đã bỏ fallback Folio đích mock; khi không có Folio thật thì hiển thị rỗng/fail-closed. `PrintPreviewDialog` cũng không còn gắn nhãn “sample mode”.
- `CashierReceiptWindow` đã đọc danh sách Folio thật rồi lấy các payment thật qua `IHotelPaymentRepository.GetForFolioAsync`; tìm kiếm chỉ lọc các dòng thật. Nút tạo phiếu vẫn fail-closed vì cửa sổ chưa có form/command ghi phiếu thu FO hoàn chỉnh, không tạo dữ liệu giả.
- `CashierVoucherWindow` và `ReceiptVoucherWindow` khi resolve được `PaymentVouchersViewModel`/`PaymentReceiptsViewModel` sẽ chuyển sang ViewModel đọc dữ liệu payable/receivable thật; nhánh code-behind cũ chỉ khởi tạo danh sách rỗng khi DI thiếu, không gọi generator `Random`.
- `RoomMoveWindow` đã bỏ chữ “mockup” trên trạng thái sẵn sàng; khi xác nhận hợp lệ, command `HotelStayApplicationService.MoveRoomAsync` ghi thay đổi thật với stay, phòng đích và row-version.
- `InvoiceManagementWindow` vẫn đọc danh sách hóa đơn thật qua `IEInvoiceService`; nút `In` nay gọi `GeneratePdfPreviewAsync` và mở `PrintPreviewDialog` bằng nội dung do service cung cấp. Các nút điều chỉnh/hủy/email/xuất chưa có contract đầy đủ nên chỉ báo unsupported, không còn gắn nhãn mockup hoặc giả báo thành công.

### Kết luận rà soát mock đang chạy

- Không còn dòng mẫu đang chạy trong các màn hình đã chuyển ở các vòng trên; các `Random`/`LoadMockData` còn lại chủ yếu là helper chết sau khi constructor/Loaded đã dùng repository thật. Cần dọn riêng sau smoke test để tránh nhầm lẫn khi bảo trì.
- Đối chiếu call-site riêng cho các nhóm `Room Master`, `Housekeeping`, `Current Guests`, `Arrivals/Departures`, `Folio`, `Cashier Voucher/Receipt Voucher` và `Transaction Center` cho thấy loader production đi qua service/repository thật hoặc chủ động để rỗng; các generator tĩnh không có caller trên đường Loaded production.
- Mock/thiếu backend còn thực sự cần hoàn thiện: Marketing (chưa có schema/repository), tạo phiếu thu FO và một số payment command, Express/Quick Checkout command thanh toán-hóa đơn, lịch sử Key-card theo Stay và các trường VIP/company/balance, Stay Information Adjustment command, cùng các tiện ích batch/upload/liên kết của Housekeeping, Group và Room Master.
- Các màn hình trên phải giữ trạng thái rỗng hoặc fail-closed cho tới khi có query/command thật; không bật lại sample data để che thiếu backend.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 748 warning hiện hữu của solution.
- Unit suite: **603/603 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật — Hồ sơ lưu trú đọc và sửa GuestProfile thật — 07/09/2026

- `StayProfileWindow` đã bỏ helper `LoadMockData`; danh sách lấy từ `IHotelStayRepository`, số điện thoại và giấy tờ lấy từ `IGuestProfileRepository` theo `GuestId`.
- Nút `Chỉnh sửa` mở form dữ liệu thật cho điện thoại, email, địa chỉ, quốc tịch, giới tính và ngày sinh; lưu qua `GuestProfile.UpdateProfile` và `IGuestProfileRepository.UpdateAsync`, giữ row-version để bắt lỗi cạnh tranh.
- Sau khi lưu, màn hình tải lại Stay và GuestProfile từ repository. Nếu thiếu GuestId/service hoặc lỗi lưu, thao tác bị chặn và không cập nhật giả.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 748 warning hiện hữu của solution.
- Unit suite: **603/603 pass, 0 skipped**.

## Cập nhật — Đối chiếu công nợ dùng transaction thật — 07/09/2026

- `ReconciliationDetailDialog` trước đây luôn dựng hai bảng công nợ cố định trong `BuildMockRows`, bỏ qua `summary` và `transactions` mà `ReceivableViewModel` truyền vào.
- Modal hiện dựng số dư đầu kỳ, từng giao dịch và tổng cộng từ `ReceivableSummaryDto`/`ReceivableTransactionDto` thật; khi không có summary thì để rỗng, không sinh số liệu mẫu.
- Hai tab hiện dùng cùng tập phát sinh thật vì DTO hiện tại chưa phân loại riêng company/customer; không nhân bản dữ liệu giả hoặc đổi số chứng từ.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 748 warning hiện hữu của solution.
- Unit suite trước vòng sửa: **603/603 pass, 0 skipped**; cần chạy lại full suite sau patch dialog trước khi chốt gate.
- `git diff --check`: không có lỗi whitespace.
- Chưa chạy migration và chưa ghi production database.

## Rà soát tiếp — màn hình khách đến/đi/đang lưu trú — 07/09/2026

- `TodayArrivalsWindow`, `TodayDeparturesWindow` và `CurrentGuestsWindow` đều có đường đọc thật qua `IHotelStayRepository` và property/business-date context; refresh hiện đã tải lại từ repository thay vì chỉ lọc collection trong bộ nhớ.
- Các action chưa có command/query đầy đủ (đổi phòng, gia hạn, gửi Housekeeping/Bellman, in tạm tính, mở Folio/Booking/Profile) đã chuyển sang thông báo “chưa có nghiệp vụ backend”, không còn giả lập thành công.
- Các `LoadMockData` còn nằm trong source là helper chết không được gọi trong constructor/Loaded path; cần dọn riêng sau smoke test UI để tránh nhầm với dữ liệu hiển thị thật.

## Triển khai tiếp — Room Master loại bỏ lịch sử và thuộc tính mặc định — 07/09/2026

- `RoomMasterViewModel` không còn tự dựng các mốc `Check In`, `Housekeeping`, `Bảo trì` và `Dọn phòng` từ trạng thái hiện tại. Khi chọn phòng, màn hình truy vấn `AuditService.QueryAsync` theo `EntityName = HotelRoom` và đúng `HotelRoomId`.
- Nếu phòng chưa có audit, thiếu service/context hoặc truy vấn lỗi, UI hiển thị trạng thái rỗng/lỗi rõ ràng; không chèn dòng lịch sử giả và không báo thành công.
- Các thuộc tính mà projection `HotelRoomDto` chưa cung cấp (diện tích, giường, sức chứa, tiện nghi, khách đang ở) không còn mang default kiểu mẫu như `28 m²`, `King`, `2 khách`, TV/Wifi/điều hòa. Mapping giữ giá trị rỗng/false/0 để phản ánh đúng dữ liệu thật hiện có.
- Housekeeping status được lấy từ `HotelRoomDto.HousekeepingStatus`; mô hình này vẫn chưa có projection occupancy/guest/reservation đầy đủ, nên các chỉ số FO occupied và liên kết khách không được coi là đã hoàn thiện.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors** (warnings nền hiện hữu của solution).
- Không chạy migration, không ghi production database và không tự chèn dữ liệu mẫu.

## Triển khai tiếp — Proforma Folio đọc dữ liệu thật — 07/09/2026

- `ProformaFolioWindow` giờ lấy danh sách Folio và ledger qua `IHotelFolioRepository`, giới hạn theo property context; posting, payment, deposit và refund được dựng từ `HotelFolioEntryDto` thật.
- Khi service/context không có hoặc truy vấn lỗi, cửa sổ giữ trạng thái rỗng và thông báo nguyên nhân; không gọi các generator `Random` để lấp dữ liệu.
- Preview chỉ tạo bản xem trước trong phiên làm việc từ Folio đã chọn. In, xuất file và gửi email vẫn bị khóa có chủ đích vì chưa có print/export/email provider; không báo thành công giả.
- Header property, routing rule, billing instruction và ngày chứng từ không còn hard-code từ dữ liệu minh họa; khi projection chưa cung cấp, UI hiển thị “chưa có dữ liệu”.
- Các helper `LoadFolios`, `LoadPostings`, `LoadPayments`, `LoadDeposits`, `LoadRefunds` còn trong file chỉ là legacy reference chưa được gọi trên đường chạy production; cần xóa sau khi có smoke test UI riêng.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors** (742 warnings nền hiện hữu của solution).
- Full unit suite: **603/603 pass, 0 skipped**.
- Không chạy migration và không ghi production database.

## Cập nhật triển khai — Nhật ký ca dùng lịch sử ca thật — 07/09/2026

- `ShiftLogWindow` đã bỏ đường chạy sinh 1.000 nhật ký bằng `Random(42)`; màn hình hiện lấy lịch sử từ `ICashShiftService.GetShiftHistoryAsync` → `EfCashShiftService` → `fo.CaLamViec` và `fo.BienDongTienMat` theo property context.
- Nhật ký hiển thị ca mở/đóng, người thu ngân, opening/expected/actual cash, variance và số movement; bộ lọc Hôm nay/Hôm qua dùng ngày hiện tại thay vì ngày cố định trong dữ liệu mẫu.
- Khi thiếu service/context hoặc query lỗi, màn hình để rỗng và báo lỗi cấu hình; không nạp dữ liệu giả. Implementation legacy `CashShiftService` trả danh sách rỗng cho history để không tạo log giả.
- Bổ sung test `CSH_015` xác nhận lịch sử đọc bản ghi đã persist, có variance thật và chặn truy vấn sai property.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors** (742 warnings nền hiện hữu).
- Targeted Cashier: **15/15 pass**; full unit suite: **601/601 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

## Xác nhận dữ liệu thật — kiểm tra lại ca thu ngân — 07/09/2026

- Test round-trip mới dùng SQLite thật trong bộ nhớ, không dùng service mock: mở ca, đọc ca đang mở, đóng ca và xác nhận ca đã đóng không còn xuất hiện trong danh sách active.
- Phát hiện và sửa lỗi persistence khi đóng ca: movement `ClosingCash` mới được thêm vào aggregate đang tracked phải được đăng ký `Added` rõ ràng; nếu không EF có thể gửi `UPDATE` cho khóa chưa tồn tại và báo concurrency giả.
- Mapping `RowVersion` của ca chỉ dùng trên SQL Server; provider test không mô phỏng rowversion. Kiểm soát đồng thời production không bị hạ.

### Kiểm chứng sau sửa lỗi

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Full unit suite: **597/597 pass, 0 skipped**.
- `git diff --check`: không phát hiện lỗi whitespace trong các file đã sửa.
- Không chạy migration, không ghi production database và không thay connection string.

### Phần mock còn phải hoàn thiện

1. `IncidentLogWindow`: cần aggregate/query/command sự cố thật, phân công, trạng thái và audit; không bật lại generator mẫu.
2. `ShiftHandoverWindow`/`ShiftLogWindow`: ca đã đọc thật, nhưng UI bàn giao còn thiếu ca nhận, tiền kiểm đếm, checklist và nhật ký audit; cần bổ sung schema/contract trước khi cho ghi từ màn hình.
3. Cashier/Folio: các đường `CashierPaymentWindow`, `CashierVoucherWindow`, Receipt/Refund/Transfer và Transaction Center cần tiếp tục đối chiếu từng action; phần chưa có command vẫn giữ fail-closed.
4. `ExchangeRateWindow`: cần kho tỷ giá theo property, currency, ngày hiệu lực và rate audit; không nối nhầm vào rate plan.
5. Tiện ích export/print/liên kết của Housekeeping, Group và Room Master chỉ triển khai sau khi query nguồn thật và quyền đã có.

Các `Random` còn lại trong helper legacy không tự động được xem là mock runtime; chỉ xử lý sau smoke test để tránh sửa nhầm đường không được gọi.

## Triển khai tiếp — Incident Log dùng dữ liệu thật — 07/09/2026

- Bổ sung aggregate `FrontOfficeIncident` với trạng thái `Open/Assigned/Investigating/Pending/Resolved/Closed/Cancelled`, mức độ, nguyên nhân, hành động khắc phục và optimistic concurrency.
- Bổ sung `IIncidentQueryService`/`IIncidentCommandService`, `EfIncidentService`, property scope và audit cho tạo sự cố/chuyển trạng thái.
- `IncidentLogWindow` không còn tạo 3.000 dòng `Random`; danh sách, KPI, bộ lọc, timeline và inspector đọc từ `fo.SuCo`. Nút `Sự cố mới` mở form và lưu bản ghi thật; `Bắt đầu xử lý` cập nhật trạng thái thật.
- Inspector đã nối các chuyển trạng thái còn lại: phân công, tạm chờ, đánh dấu đã xử lý, đóng và hủy; mỗi thao tác yêu cầu thông tin nghiệp vụ tương ứng và gửi `ExpectedRowVersion`.
- Các tham chiếu khách/phòng/đặt phòng hiện được lưu dạng text vì schema hiện tại chưa có khóa liên kết ổn định; inspector hiển thị rõ `Chưa liên kết`, không dựng trạng thái phòng giả.
- Thêm migration `20260907203000_AddFrontOfficeIncidents`; chưa chạy migration trên database nào.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Test riêng Incident: **2/2 pass**.
- Full unit suite lần chạy xác nhận: **600/600 pass, 0 skipped**. Hai lần chạy nhanh trước đó có 1 lỗi transient ở test hiện hữu; chạy lại với log chi tiết đạt đủ 600.
- Không ghi production database và không thay connection string.

## Cập nhật — Bàn giao ca ghi dữ liệu thật — 07/09/2026

- Bổ sung query người dùng active cho `ICashShiftService.GetCashierOptionsAsync`; màn hình không tự sinh người nhận.
- `ShiftHandoverWindow` đã có chọn ca nhận, nhập tiền kiểm đếm và ghi qua `HandOverAsync`. Thành công reload lại ca nguồn/ca nhận; lỗi không đổi trạng thái UI.
- Tab Incident của bàn giao ca dùng `IIncidentQueryService` nếu đã đăng ký và hiển thị số lượng từ `fo.SuCo`; thiếu service vẫn fail-closed.
- Legacy `CashShiftService` chỉ còn để tương thích test/đường chết và trả danh sách người nhận rỗng, không còn tạo lựa chọn cashier giả.

## Triển khai tiếp — Ca thu ngân và màn hình bàn giao ca dùng dữ liệu thật — 07/09/2026

- Khôi phục `CashierShift` và `CashMovement` vào `AppDbContext`, mapping đúng hai bảng đã có trong migration cũ: `fo.CaLamViec` và `fo.BienDongTienMat`.
- Bổ sung `EfCashShiftService` và đăng ký production DI cho `ICashShiftService`. Mở ca, đóng ca, truy vấn ca mở và bàn giao ca đều đọc/ghi aggregate thật, có property scope, kiểm tra ca trùng và transaction qua `SaveChangesAsync`.
- `ShiftHandoverWindow` không còn tạo 300 khách, 100 task, số dư thu ngân, incident hoặc nhật ký giả. Khách lấy từ `HotelStay`, task tồn lấy từ `HousekeepingBoard`, ca thu ngân lấy từ `CashierShift`; Incident/audit log vẫn hiển thị rõ là chưa có query thật.
- Bàn giao trên UI hiện chỉ cho phép xác nhận khi có dữ liệu ca nguồn thật; nút chưa ghi tự động vì giao diện chưa có lựa chọn ca nhận và tiền kiểm đếm. Không tự suy diễn người nhận để tránh ghi sai nghiệp vụ.
- Giới hạn schema đã xác nhận: contract cũ có `DrawerId` và currency/rate nhưng bảng ca hiện chưa có FK ngăn kéo hoặc cột tiền tệ. Service trả `DrawerId = Guid.Empty` khi đọc ca cũ và quy đổi theo currency property với rate 1; cần một migration thiết kế riêng trước khi coi cashier enterprise là hoàn tất.
- Không tạo migration mới ở bước này: bảng cash đã nằm trong `20260720032319_AddReservationEnterpriseFields`; việc cần làm tiếp là kiểm tra fingerprint database test để xác nhận migration cũ đã được áp dụng, sau đó mới lập baseline/sửa schema nếu thiếu.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors** (742 warnings hiện hữu của solution).
- Unit suite: **596/596 pass, 0 skipped**.
- `git diff --check`: không phát hiện lỗi whitespace trong các file đã sửa.
- Chưa chạy migration, chưa ghi production database và chưa thay connection string.

## Rà soát thực địa lần 2 — phân biệt mock đang chạy và helper legacy — 07/09/2026

Đã quét các call-site trong App và đối chiếu caller bằng graph, không kết luận chỉ từ tên LoadMockData hoặc Random:

- **Đã xác nhận đường đọc thật, mock chỉ còn là code legacy sau return**: ArCustomerViewModel, ArReconcileViewModel, TodayDeparturesWindow (danh sách khách dự kiến đi), cùng các slice Housekeeping/Room Master đã chuyển trước đó. Không cần bật lại generator mẫu.
- **Đang mock trên đường chạy người dùng**: IncidentLogWindow (tạo 3.000 sự cố bằng Random ngay khi mở), ShiftHandoverWindow (khách/task mẫu) và ShiftLogWindow. Đây là ba màn hình cần ưu tiên vì chưa có query thật tương ứng.
- **Đang đọc thật hoặc fail-closed, generator chỉ là legacy**: CashierPaymentWindow, CashierVoucherWindow, DepositManagementWindow, ExpressCheckoutWindow và CheckoutPreparationWindow đều có nhánh Loaded/DI đọc Folio/Stay/Payment thật; ExchangeRateWindow chủ động hiển thị rỗng vì chưa có kho tỷ giá thật. Không được dùng các hàm Random trong các màn hình này để kết luận runtime đang mock.
- **Đang mixed**: ReceiptVoucherWindow đã có list/workflow phiếu thu thật ở ViewModel mới, nhưng một số tiện ích cũ vẫn đi qua thông báo mock; TransactionCenterWindow đã đọc Folio thật và Void/Reverse thật, còn tạo thủ công/đối soát/xuất chưa có command. Các thao tác của TodayDeparturesWindow/InvoiceManagementWindow/KeyManagementWorkspace vẫn cần tách riêng theo action.
- **Không được đánh đồng helper chết với mock runtime**: CurrentGuestsWindow, StayProfileWindow, LockedRoomWindow, TodayArrivalsWindow và HousekeepingMessageWindow cần giữ nguyên phân loại theo constructor/Loaded path; các helper mẫu còn lại chỉ nên xóa sau smoke test hồi quy.

### Thứ tự hoàn thiện đề xuất

1. Cashier Payment/Voucher và các command folio/payment còn thiếu, vì ảnh hưởng trực tiếp đến số dư và audit.
2. Shift Handover/Shift Log, sau khi chốt schema ca, bàn giao và cash count.
3. Incident Log, sau khi chốt aggregate sự cố, phân công, trạng thái và audit.
4. Exchange Rate, vì cần quyết định schema currency/rate và ngày hiệu lực.
5. Các tiện ích xuất/in/liên kết chỉ triển khai sau khi nguồn dữ liệu thật đã ổn định.

Kết luận của lần quét này: còn mock runtime thật, nhưng không phải toàn bộ 48 vị trí có Random; nhiều vị trí đã nằm sau nhánh đọc EF thật. Các màn hình chưa có schema/command tiếp tục phải fail-closed, không dùng dữ liệu mẫu để che thiếu backend.

## Triển khai tiếp — Transaction Center Void/Reverse dùng Folio thật — 07/09/2026

- TransactionCenterWindow tiếp tục đọc ledger từ IHotelFolioRepository.SearchAsync; bản ghi hiển thị giữ thêm HotelFolioId, HotelFolioEntryId và RowVersion thật thay vì chỉ dùng mã Folio/int giả.
- Nút Void/Reverse không còn báo mock. Khi chọn giao dịch thật, UI xác nhận thao tác rồi gọi ReverseFolioEntryCommandHandler, tạo bút toán đảo qua HotelFolioApplicationService, kiểm tra quyền, rowversion và ghi audit theo pipeline hiện có.
- Sau khi thành công, Transaction Center tải lại ledger từ database; giao dịch đã đảo không thể đảo lại. Nếu thiếu liên kết Folio thật, rowversion hoặc handler thì dừng và báo rõ, không đổi trạng thái trong bộ nhớ.
- Tạo giao dịch thủ công, Đối soát và Xuất vẫn chưa có command/backend hoàn chỉnh; các nút này tiếp tục được giữ fail-closed, không giả vờ thành công.

### Kiểm chứng sau cập nhật

- Build ASMILE.BO.App: **PASS, 0 errors** (742 warnings hiện hữu của solution).
- Unit suite: **596/596 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

## Rà soát lại đường chạy thật và triển khai Housekeeping Message — 07/09/2026

### Kết luận theo call-site

- `CurrentGuests`, `TodayArrivals`, `TodayDepartures`, `StayProfile` và `LockedRoom` có helper `LoadMockData` cũ nhưng đường chạy mở màn hình đang dùng repository/service thật hoặc fail-closed; không xếp các màn này vào nhóm mock đang hoạt động.
- `GroupCheckIn`, `GroupGuestManagement`, `GroupRoomAssignment` và `GroupRoomingList` đã có query/EF path thật hoặc fail-closed.
- `HousekeepingMessage` trước đây là đường mock trực tiếp; đã chuyển phần đọc danh sách và chuyển trạng thái sang board/workflow thật.
- `ExchangeRate` hiện chưa có schema/query tỷ giá theo property; không nên nối nhầm vào repository rate plan.
- `InvoiceManagement`, `ProformaFolio`, `ReceiptVoucher`, `ShiftLog`, `ShiftHandover`, `IncidentLog`, `VipNotes`, các tiện ích Group và tiện ích Room Master vẫn cần tiếp tục đối soát command/query trước khi bật thao tác.

### Đã triển khai

- `HousekeepingMessageWindow` đọc `IHousekeepingBoardService.GetTaskBoardAsync` theo property context và map các task thật lên giao diện.
- Cập nhật tiến độ gọi `Accept`, `StartCleaning`, `ResumeCleaning`, `ApproveInspection` theo trạng thái; hoàn thành gọi `CompleteCleaning` hoặc `ApproveInspection`; hủy gọi `Cancel`.
- Sau command thành công màn hình tải lại từ database. Khi thiếu service/context hoặc backend chưa có contract, thao tác fail-closed, không sinh task giả và không báo thành công giả.
- `Tạo yêu cầu` và `Phân công` vẫn khóa có chủ đích vì chưa có form/chọn nhân sự và command tạo/phân công hoàn chỉnh.

### Kiểm chứng mới nhất

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Full unit suite sau thay đổi: **596/596 pass, 0 skipped**.
- `dotnet ef migrations has-pending-model-changes --no-build`: không có thay đổi model chưa tạo migration.
- Danh sách migration hiện vẫn pending trong database context được kiểm tra; **chưa chạy migration, chưa ghi production database và không thay connection string**.

### Thứ tự hoàn thiện còn lại

1. Receipt Voucher và payment allocation/reversal: chốt một cửa sổ chính, nối query/command thật, audit và idempotency.
2. Invoice/Proforma/Folio utilities: kiểm tra lại route DI, sau đó nối các thao tác còn khóa vào aggregate Folio thật.
3. Housekeeping: tạo task, phân công, ảnh, liên kết Maintenance/Minibar/Lost&Found/Folio/Reservation và export/print.
4. Group và Room Master: các tiện ích Folio/Deposit/Invoice/Report cùng QR/lịch sử/liên kết/export/print.
5. Shift/Incident/VIP/Housekeeping Message nâng cao và Exchange Rate: bổ sung schema/query/command thật trước khi mở UI.

## Triển khai tiếp — Phiếu thu dùng workflow lưu thật — 07/09/2026

- `ReceiptVoucherWindow` đã xác nhận route vào `PaymentReceiptsViewModel` thật; vấn đề còn lại là các nút duyệt/xóa/mở lại trước đây chỉ đổi object trong bộ nhớ.
- Bổ sung `PaymentReceiptDto` và các contract `GetPaymentReceiptsAsync`, `ApprovePaymentReceiptAsync`, `CancelPaymentReceiptAsync`, `ReopenPaymentReceiptAsync` trên `IReceivableService`.
- `EfReceivableService` đọc phiếu từ `BusinessDocuments` thật theo khách hàng và khoảng ngày; trạng thái được lưu bằng `Draft/Locked/Active/Cancelled` tương ứng `Lập phiếu/Chờ duyệt/Đã duyệt/Đã hủy`.
- Khi duyệt/hủy/mở lại, CSDL được cập nhật và ghi audit; nếu có dòng `ReceivableTransaction` liên kết thì trạng thái ledger cũng được đồng bộ.
- AR không còn tính phiếu `Locked` như khoản đã thu; chỉ phiếu `Active` mới được đưa vào số dư công nợ.
- ViewModel không tự báo thành công nữa: sau mỗi thao tác thành công đều tải lại từ CSDL; phiếu chưa lưu chỉ tồn tại như draft giao diện và không được coi là dữ liệu thật.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Full unit suite: **596/596 pass, 0 skipped**.
- Không phát sinh thay đổi model, không cần migration mới cho slice này.
- Chưa chạy thao tác ghi trên production database.

## Triển khai tiếp — Tạo yêu cầu Housekeeping thật — 07/09/2026

- Bổ sung `HkTaskCreateRequest` và `CreateTaskAsync` vào workflow contract/EF service.
- Nút `Tạo yêu cầu` trong `HousekeepingMessageWindow` mở form chọn phòng, loại task và mức ưu tiên từ danh sách phòng thật; command kiểm tra property scope, phòng tồn tại và không cho tạo trùng task đang hoạt động.
- Task được lưu vào `HousekeepingTasks`, sau khi thành công màn hình tải lại board từ CSDL; không còn MessageBox mock cho đường tạo task.
- Nút `Phân công` vẫn khóa vì cần bổ sung selector nhân sự thật và policy phân quyền riêng; không tự gán người giả.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Full unit suite: **596/596 pass, 0 skipped**.
- Không phát sinh migration mới; chưa ghi production database.


## Rà soát lại đường chạy thật và triển khai Housekeeping Message — 07/09/2026

### Kết luận theo call-site

- `CurrentGuestsWindow`, `TodayArrivalsWindow`, `TodayDeparturesWindow`, `StayProfileWindow` và `LockedRoomWindow` có helper `LoadMockData` còn sót nhưng constructor/`Loaded` hiện dùng repository hoặc service thật; không được xem các helper chết này là dữ liệu đang hiển thị.
- `GroupCheckInWindow`, `GroupGuestManagementWindow`, `GroupRoomAssignmentWindow` và `GroupRoomingListWindow` đã có query thật hoặc fail-closed khi thiếu contract; phần còn thiếu chủ yếu là command phụ/import/export.
- `HousekeepingMessageWindow` trước đây là màn hình trống kèm các handler `Mockup`; không có query backend và toàn bộ nút thao tác đều chỉ hiện thông báo.
- `ExchangeRateWindow` không còn hiển thị list mẫu khi mở nhưng vẫn chưa có schema/query tỷ giá theo property; các nút thêm/sửa/sao chép/đặt mặc định/lịch sử đều bị khóa có chủ đích.
- `InvoiceManagementWindow`, `ProformaFolioWindow`, `ReceiptVoucherWindow`, `ShiftLogWindow`, `ShiftHandoverWindow`, `IncidentLogWindow`, `VipNotesWindow` và `KeyManagementWorkspaceView` vẫn còn đường mock hoặc placeholder thật; cần hoàn thiện contract/persistence riêng, không nối tạm vào collection UI.
- `ConciergeEngine`, `ChannelEngine`, `GroupEngine` và phần engine Housekeeping cũ có `Task.FromResult`/DTO mẫu, nhưng kiểm tra DI production cho thấy các màn hình FO chính đang dùng adapter EF tương ứng. Các engine cũ cần dọn sau khi có smoke test, không dùng làm bằng chứng dữ liệu thật.

### Đã triển khai trong vòng này

- `HousekeepingMessageWindow` đọc `IHousekeepingBoardService.GetTaskBoardAsync` theo property context, map trực tiếp `HkTaskBoardDto` thật và hiển thị rỗng nếu database chưa có tác vụ.
- `Cập nhật tiến độ` gọi workflow thật theo trạng thái: `Accept`, `StartCleaning`, `ResumeCleaning` hoặc `ApproveInspection`.
- `Hoàn thành` gọi `CompleteCleaning` hoặc `ApproveInspection`; `Hủy yêu cầu` gọi `Cancel`. Sau mỗi command thành công, màn hình tải lại từ DB.
- `Phân công` và `Tạo yêu cầu` vẫn khóa vì chưa có bộ chọn nhân viên/form tạo request cùng contract persistence; không tạo task giả.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Full unit suite: **596/596 pass, 0 skipped**.
- Migration `20260907121544_AddHousekeepingPauseState` và `20260907130000_AddRoomInspectionTable` mới ở trạng thái pending; chưa chạy migration và chưa ghi production database.

## Cập nhật triển khai — Room Inspection persistence và đối soát migration — 07/09/2026

- Rà soát migration phát hiện `RoomInspection` đã có entity, DbSet và model snapshot nhưng chưa có lệnh `CreateTable` trong lịch sử migration. Đã bổ sung migration tối thiểu `20260907130000_AddRoomInspectionTable` gồm bảng `fo.RoomInspection`, unique scope theo `PropertyId + HousekeepingTaskId`, index và FK cascade tới `fo.DonDep`.
- `EfHousekeepingWorkflowService` giờ tạo bản ghi kiểm tra `Pending` khi hoàn thành dọn phòng; khi Approve/Reject sẽ cập nhật cùng bản ghi với kết quả, ghi chú và thời điểm. Không còn chỉ cập nhật `HousekeepingTask` rồi bỏ rơi entity kiểm tra.
- `EfHousekeepingBoardService` đọc thêm bản ghi `RoomInspection` và đưa Inspector/InspectionTime/Notes/ChecklistData vào projection; Room Inspection UI không còn tự dựng điểm số hoàn thành từ trạng thái task.
- Thêm unit test cho vòng đời `RoomInspection`; full unit suite hiện đạt **594/594 pass, 0 skipped**.
- `migrations list` đã nhận `20260907130000_AddRoomInspectionTable (Pending)`; `has-pending-model-changes` báo không có thay đổi model. Migration vẫn **chưa được apply** vào database, cần chạy qua quy trình backup/preflight đã thống nhất.

### Phân loại mock sau lần kiểm tra sâu này

- **Mock còn nằm trên đường chạy:** tiện ích Housekeeping (batch inspection, photo, pause/resume, đổi attendant, liên kết Maintenance/Minibar/Lost & Found/Folio/Reservation); Group tiện ích Folio/Deposit/Invoice/Report/Print/Email/Export; Room Master QR/history/link/export/print; Receipt Voucher; Shift/Incident/VIP/Housekeeping Message/Exchange Rate.
- **Mock nền tảng nhưng hiện chưa được DI gọi:** `InspectionEngine`, `AssignmentEngine`, `HousekeepingBoardService`, Linen/Amenity service trong `HousekeepingEngine.cs`; các engine Cashier/Concierge/Maintenance/Group tương tự cần tiếp tục giữ ngoài đường chạy hoặc thay bằng adapter thật trước khi đăng ký.
- **Fallback preview/legacy:** các nhánh sau `return` có `Random` trong Housekeeping/AR chỉ phục vụ designer hoặc code cũ; không được dùng làm bằng chứng dữ liệu runtime nếu đã xác minh constructor + DI + Loaded.

## Rà soát lần cuối theo call-site và khóa thành công giả — 07/09/2026

### Phạm vi kiểm tra

- Kiểm tra cả literal `mock/mockup`, `new Random(...)`, constructor/`Loaded` và service được resolve từ DI trong `ASMILE.BO.App`.
- Không xem riêng sự tồn tại của helper `Random` là bằng chứng màn hình đang hiển thị dữ liệu giả; chỉ kết luận mock đang chạy khi helper được gọi trên đường khởi tạo/tải hiện hành.
- Không chạy migration, không ghi production database và không tự chèn dữ liệu mẫu.

### Đã khóa thêm trong vòng này

- `GroupReservationView`, `GroupServiceView`, `RoomInspectionView` và `HousekeepingRoomStatusView`: bỏ modal double-click báo workspace giả; chi tiết thật đã có ngay trên panel binding.
- `GroupBookingWizardWindow`: nút xác nhận không còn dựng màn hình “Booking đã tạo (mock)”; hiện fail-closed vì chưa có booking persistence command.
- `GroupRoomingListWindow`: thêm khách/import/export/in không còn sửa collection hoặc báo thành công giả; xác nhận vẫn chặn khi thiếu command persistence.
- `GroupRoomAssignmentWindow`: Lock/History/Gọi HK/In danh sách không còn báo đã thực hiện giả; đường lưu phân phòng chính vẫn dùng transaction command thật.
- `CleaningInProgressView`: double-click không còn hiện modal mock.

### Phân loại hiện trạng sau call-site audit

1. **Đã đọc dữ liệu thật và có đường ghi thật ở phần chính:** Folio workspace, Current Guests/Arrivals/Departures, Invoice list, Receipt list khi DI resolve được ViewModel thật, Transaction list, Deposit/Folio query, Group Reservation/Rooming/Assignment query và lưu assignment, Room Master query/tạo phòng/đổi trạng thái, Housekeeping single-room workflow và Dirty Room batch.
2. **Đọc thật nhưng chức năng phụ hoặc ghi còn thiếu, hiện đã fail-closed:** Group Guest import/profile/document, Group Service CRUD/post, Group Booking wizard, Rooming List mutation/export/print, Group Assignment lock/history/HK/print, Room Inspection batch/photo/maintenance/lost-found, Housekeeping batch/photo/link/export/print, Room Master QR/history/link/export/print, Issue Invoice/Receipt allocation/provider chưa đủ.
3. **Màn hình còn thiếu query/schema thật, không được bật bằng fixture:** Housekeeping Message, Shift Log/Handover, Incident Log, VIP Notes, Exchange Rate, Proforma Folio và các phần Settings/AR chưa có projection/command phù hợp. Nhiều màn hình này hiện khởi tạo danh sách rỗng và footer cảnh báo; các `Load...`/`Random` cũ là fixture legacy không được gọi trên đường đọc thật hiện hành.
4. **Cần kiểm tra riêng trước khi bật vận hành:** năm đăng ký implementation nền tảng dạng mock trong DI (`ISystemSettingsService`, `IBackupService`, `IServiceStatusService`, `ISecuritySettingsService`, `IPrinterSettingsService`). Đây là khoảng trống nền tảng, không được coi là dữ liệu thật.

### Kết luận và thứ tự triển khai

- Không còn chấp nhận thao tác UI báo thành công nếu chưa có command ghi DB, transaction, property scope, row-version/idempotency và audit.
- Ưu tiên tiếp theo: (P0) invoice issue/payment allocation/reversal; (P1) Group Service + Group Booking persistence; (P1) Housekeeping photo/pause/batch/maintenance links; (P1) Guest document/profile command; (P2) audit/query cho Shift/Incident/VIP/Housekeeping Message và nền tảng settings.
- Sau mỗi nhóm phải có integration test trên database test có bản ghi thật; không dùng cách mở lại generator `Random` để làm dữ liệu mẫu trong production path.

### Kiểm chứng

- `dotnet build ASMILE.BO.App/ASMILE.BO.App.csproj --no-restore`: **PASS, 0 errors**.
- `dotnet test ASMILE.BO.Tests.Unit/ASMILE.BO.Tests.Unit.csproj --no-restore`: **589/589 pass, 0 skipped**.

## Cập nhật audit — kiểm tra lại đường chạy mock và khóa ghi giả — 07/09/2026

### Đã chuyển sang dữ liệu thật hoặc fail-closed

- `GroupGuestManagementWindow` không còn sinh đoàn/khách bằng `Random`. Danh sách đoàn đọc qua `IGroupReservationQueryService`; rooming list đọc từ `GroupReservationSummaryDto.RoomingList`; thông tin khách có `GuestId` được enrich qua `IGuestProfileRepository`.
- Export khách đoàn đã xuất CSV từ danh sách thật và nút In dùng `PrintDialog` trên dữ liệu đang hiển thị. Import Excel, upload/preview scan và xác nhận chỉnh hồ sơ chưa có command/document storage nên bị khóa, không ghi tạm.
- `DirtyRoomViewModel`: các đường batch Assign/Start/Complete trước đó đã dùng workflow thật; các toggle VIP/Rush, checklist từng mục, link Folio/Reservation/Minibar/Maintenance/Lost & Found và export/print chưa có command/query phù hợp nên đã khóa, không thay DTO trong RAM.
- `CleaningInProgressViewModel`: workflow đơn phòng vẫn gọi `IHousekeepingWorkflowService`; upload ảnh, batch chưa có command, export/print đã chuyển từ thông báo UI mockup sang cảnh báo chưa hỗ trợ.
- `RoomInspectionViewModel`: Approve/Reject đơn phòng vẫn dùng workflow thật; đổi Supervisor, upload ảnh, Maintenance/Lost & Found, batch và export/print chưa có contract nên đã khóa, không còn hiển thị kết quả giả.
- `GroupReservationViewModel`: Folio và Hóa đơn đã mở cửa sổ thật (`FolioWorkspaceWindow`, `InvoiceManagementWindow`); Dịch vụ đoàn mở cửa sổ thật nhưng hiện read path fail-closed; Đặt cọc/email/in/Excel vẫn bị chặn vì chưa có service tương ứng.
- `GroupServiceViewModel`: Load không hiển thị fixture; Add/Edit/Delete/Confirm/Complete/Cancel/Post và batch không còn sửa collection trong bộ nhớ, tất cả báo rõ thiếu command persistence.

### Còn mock hoặc thiếu nền tảng cần xử lý tiếp

1. **Group Service persistence**: cần schema/domain/query/command cho service order, trạng thái, charge-to và post Folio; đây là P0 tiếp theo vì hiện module chỉ đọc rỗng và mọi ghi bị khóa.
2. **Deposit Management**: `DepositManagementWindow` vẫn có fallback generator `Random`; phải thay bằng query Folio/deposit thật trước khi cho Group mở màn hình này.
3. **Cashier/Checkout legacy windows**: `CashierPaymentWindow`, `CashierVoucherWindow`, `CheckoutPreparationWindow`, `ExpressCheckoutWindow` còn helper `Random`; cần trace constructor/DI để loại bỏ fallback thật sự, không chỉ xóa helper chết.
4. **Housekeeping evidence**: cần document storage, maintenance/minibar/lost-found linking và batch command riêng; không được mở lại các nút cho đến khi có persistence và audit.
5. **Workforce, Lost & Found, Minibar, Room Inspection/Housekeeping fallback**: các read path thật đã có ở một số màn hình, nhưng code fixture `Random` còn nằm sau nhánh fail-closed hoặc preview-only; cần tách/xóa sau khi có smoke test DB test.
6. **Breakfast/Café Settings và một số AR screens**: còn `InitMockData`/legacy mock generator; không thuộc luồng Hotel FO tối thiểu nhưng phải audit riêng trước khi đưa vào vận hành.

### Kiểm chứng sau lần quét này

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Full unit suite: **589/589 pass, 0 skipped**.
- Không chạy migration, không ghi production database, không tạo dữ liệu mẫu trong DB thật và không thay connection string.

## Cập nhật triển khai — Folio Split/Merge ghi ledger thật — 07/09/2026

- `SplitFolioWindow` không còn chỉ gửi tổng tiền: khi chạy dữ liệu thật, UI gửi danh sách `FolioSplitAllocation` gồm `HotelFolioEntryId` và số tiền của từng khoản đã chọn.
- Backend kiểm tra source entry tồn tại trong đúng Folio, là khoản dương có thể tách, chưa bị reverse, không phải Transfer, không trùng và không vượt phần còn lại của khoản gốc; tổng allocation phải khớp tổng tách.
- Các dòng tách được ghi bằng `HotelFolioEntryType.Transfer`, giữ `ReferenceType=HotelFolioEntry` và `ReferenceId` trỏ về khoản gốc ở cả Folio nguồn/đích; toàn bộ commit là một persistence transaction. Đường gọi transfer cũ cũng chuyển từ Adjustment sang Transfer.
- `MergeFolioWindow` truyền các quy tắc đã chọn xuống `FolioMergeRules`. Với chế độ có quy tắc, backend chuyển từng dòng ledger sang Folio đích bằng các Transfer có reference dòng gốc rồi đóng Folio nguồn; rule không có mapping dữ liệu thật bị từ chối rõ ràng thay vì báo thành công giả.
- Bổ sung test domain xác nhận Transfer giữ liên kết tới source entry.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- `HotelFolioTests`: **13/13 pass**.
- Full unit suite: một lần chạy **587/588 pass**, chỉ fail không ổn định ở `BackgroundSchedulerServiceTests.Cancellation_PropagatesToRunningExecution`; chạy riêng lại **1/1 pass**. Đây là flaky timing của scheduler, không liên quan Folio.
- Không chạy migration, không ghi production database và không thay connection string.

### Kết luận rà soát mock hiện tại

- Đường Folio chính (`FolioWorkspace`, Tách Folio, Gộp Folio, Cashier payment) đã có đường đọc/ghi dữ liệu thật hoặc fail-closed; các generator mock còn lại trong file là helper không được dùng khi persisted mode thành công hoặc thuộc màn hình cũ cần xác nhận DI.
- Các nhóm còn mock đang chạy và nên triển khai tiếp theo thứ tự: **Group Service/Guest**, **Housekeeping links + ảnh + export/print**, **Room Master tiện ích/lịch sử**, rồi **Shift/Incident/VIP/Housekeeping Message/Exchange Rate**.
- Không nên xóa toàn bộ hàm `LoadMockData` ngay: cần kiểm tra call-site/constructor/Loaded từng cửa sổ, vì có helper mock đã chết nhưng cũng có màn hình vẫn thực sự dùng list mẫu.

## Cập nhật sau triển khai — Clean Room batch và đầu ra Housekeeping — 07/09/2026

- `CleanRoomViewModel` không còn báo thành công bằng MessageBox mock cho `Batch Assign` và `Batch Complete`. Hai thao tác dùng các dòng được chọn có `TaskId` thật, truyền `PropertyId` + tài khoản đăng nhập vào `IHousekeepingWorkflowService`, rồi chỉ cập nhật dòng hiển thị sau khi command EF thành công.
- `Batch Inspect` vẫn bị khóa có chủ đích vì chưa có contract nhập kết quả/ghi chú riêng cho từng phòng; không tự động Pass hàng loạt.
- `Xuất Excel` đã chuyển thành xuất CSV UTF-8 từ `FilteredRooms` đang tải từ board thật; `In` dùng dữ liệu hiện tại và `PrintDialog`, không dựng danh sách mẫu.
- Double-click Clean Room không còn mở hộp thoại “UI MOCKUP”; phần chi tiết dùng inspector đang bind với bản ghi thật.
- Quét lại sau thay đổi còn `80` dòng literal `UI MOCKUP/mock` và `53` vị trí `new Random(...)` trong `ASMILE.BO.App`; các số này là inventory source, chưa phải số màn hình đang chạy mock. Các nhóm còn đáng xử lý tiếp: Group Service/guest utilities, Housekeeping photo/pause/link, Room Master batch/QR/history/link, Shift/Incident/VIP, Exchange Rate và Folio/payment allocation.

### Kiểm chứng sau triển khai Clean Room

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite lần đầu có lỗi scheduler không ổn định; test riêng đạt **1/1**, chạy lại toàn bộ đạt **588/588 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

### Kiểm chứng cuối vòng rà soát 07/09/2026

- Build lại sau các thay đổi fail-closed: **PASS, 0 errors**.
- Unit suite chạy lại sau các thay đổi: **588/588 pass, 0 skipped**.

## Cập nhật triển khai — Group Checkout atomic theo assignment thật — 07/09/2026

- Bổ sung `IGroupCheckOutCommandService` và `EfGroupCheckOutCommandService`.
- Command kiểm tra property context, trạng thái đoàn `CheckedIn`, assignment trùng/không tồn tại, phòng chưa check-in hoặc đã check-out; sau đó cập nhật toàn bộ assignment trong **một** persistence commit và ghi audit `GroupCheckedOut`.
- Chỉ khi toàn bộ assignment được ghi thành công UI mới báo thành công; lỗi giữa chừng không còn phát sinh do vòng lặp gọi `MarkDepartureAsync` từng phòng.
- `GroupCheckoutWindow` truyền `GroupReservationId` và các `AssignmentId` thật vào command. Phần đối chiếu Folio/payment allocation và dịch vụ đoàn vẫn bị khóa có chủ đích, chưa giả lập số liệu.

### Kiểm chứng sau Group Checkout atomic

- Build `ASMILE.BO.App`: **PASS, 0 errors** (758 warnings hiện hữu của solution).
- Unit suite lần đầu gặp lỗi scheduler flaky; chạy riêng đạt **1/1**, chạy lại toàn bộ đạt **588/588 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật triển khai — Tách/Gộp Folio fail-closed, chỉ dùng Folio thật — 07/09/2026

- `SplitFolioWindow` không còn danh sách/record Folio mock, nút tạo Folio đích mockup hoặc thông báo “đã tách” giả. Folio đích phải được chọn từ dữ liệu thật đang mở, có `FolioGuid` và `RowVersion` hợp lệ.
- `MergeFolioWindow` không còn tạo/preview Folio đích mockup và không còn ghi nhận lịch sử gộp giả. Khi chưa có đường dữ liệu thật, nút xác nhận dừng với cảnh báo; khi có dữ liệu thật, thao tác đi qua `HotelFolioApplicationService.MergeAsync`.
- Hai wizard vẫn còn các helper generator mẫu không được gọi từ constructor/Loaded hiện tại; chúng được xem là dead code cần dọn riêng, không phải nguồn dữ liệu runtime. Việc kiểm tra runtime phải tiếp tục dựa trên đường `Loaded` → repository/service.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors** (750 warnings hiện hữu của solution).
- Unit suite: **588/588 pass, 0 skipped**.
- Chưa chạy migration, chưa ghi production database và chưa thay connection string.

## Rà soát bổ sung — các nhánh mock còn lại — 07/09/2026

- Thu ngân, đặt cọc và hoàn tiền đã có đường `Loaded` tải Folio/payment thật và command thật; các generator `Random` còn trong file chỉ là helper chết, không được gọi từ constructor/Loaded.
- `CashierVoucherWindow` ưu tiên `PaymentVouchersViewModel` thật. Fallback không còn thông báo thành công mock; các nút chưa có command thật chỉ cảnh báo và không ghi dữ liệu.
- `ExchangeRateWindow` hiện khởi tạo rỗng, không gọi `LoadMockData`; các nút thêm/sửa/sao chép/đặt mặc định/lịch sử chỉ fail-closed vì chưa có repository/command tỷ giá theo property.
- `CreateGroupWindow` vẫn lưu Group Profile qua đường thật; phần tài liệu chưa có contract lưu trữ nên nút tải tài liệu chỉ cảnh báo, không tạo `MockDocument` giả. Đây là phần cần bổ sung contract riêng, không nên gắn vào transaction tạo Group hiện tại.
- Các nhóm còn phải triển khai contract/query thật trước khi coi là hoàn tất: tỷ giá, phiếu chi, tài liệu đoàn, một số tiện ích housekeeping/group/report/export. Không dùng dữ liệu mẫu để che trạng thái thiếu backend.

## Cập nhật triển khai — Phiếu thu và hóa đơn dùng command thật — 07/09/2026

- `CreateReceiptVoucherWindow` không còn tạo phiếu thu mock hoặc đóng cửa sổ sau khi báo thành công giả. Wizard tải Folio thật, giữ `HotelFolioId`/`RowVersion`, kiểm tra số tiền/phương thức/currency và gọi `HotelPaymentApplicationService.ReceiveAsync`.
- Khoản thu thật được lưu qua `HotelPayment` và Folio payment entry trong transaction backend. Nếu thiếu context, thiếu Folio, xung đột RowVersion hoặc command lỗi, UI giữ nguyên và báo lỗi; không có fallback dữ liệu mẫu.
- Bước “Phân bổ” của phiếu thu được ghi rõ là chưa có allocation thủ công theo từng `FolioEntry`; không đánh dấu hoàn tất giả.
- `IssueInvoiceWizard` đã gọi `SettlementApplicationService.GenerateInvoiceAsync` với Folio thật, kiểm tra invoice đã tồn tại để tránh tạo trùng, và hiển thị số hóa đơn trả về. Các số tiền hiển thị lấy từ room charge/tax/payment/deposit trong ledger; bỏ VAT/phí/giảm giá hard-code.
- `SettlementApplicationService.GenerateInvoiceAsync` cũng kiểm tra `FolioExpectedRowVersion` ở backend và từ chối `INVOICE_ALREADY_EXISTS`; kiểm tra ở UI chỉ là trải nghiệm sớm, không phải hàng rào duy nhất.
- `CurrentGuestsWindow` mở các cửa sổ `ExtendStayWindow` và `RoomMoveExtendWindow` đã có đường service thật; các thao tác chưa có backend (`Thêm khách`, `Nhắn nhà buồng`, thao tác khác) chuyển sang cảnh báo fail-closed, không còn nói như đã thao tác thành công.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors** (còn các cảnh báo legacy hiện hữu của solution).
- Unit suite sau lần build cuối: **588/588 pass, 0 skipped**; lần chạy trước có một lỗi không ổn định ở scheduler nhưng lần full kế tiếp đã pass.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật tiếp — Group Checkout hiển thị tổng Folio thật — 07/09/2026

- `GroupCheckoutWindow` không còn chạy khối UI dựng sẵn 6 Folio, khách, số tiền và phân bổ thanh toán mẫu ở bước 3.
- Bước `Kiểm tra Folio` hiện đọc `TotalRevenue`, số thanh toán suy ra từ `TotalRevenue - OutstandingBalance`, `TotalDeposit` và `OutstandingBalance` từ `GroupReservationQueryService`/EF.
- UI ghi rõ phạm vi dữ liệu: tổng hợp nhóm đã có thật; liên kết Folio với từng assignment/phòng và invoice/payment allocation cho checkout đoàn chưa có schema/contract nên bước xác nhận cuối vẫn bị khóa.
- Kiểm tra DI xác nhận `IGroupBillingService` production đang trỏ vào `EfGroupBillingService`; `GroupEngine.cs` chỉ còn là implementation mock legacy được các test cũ khởi tạo trực tiếp, không phải service được DI đăng ký. Không dùng implementation này làm bằng chứng dữ liệu thật.
- Quét lại call-site hiện tại: `ASMILE.BO.App` còn 29 file có literal `mock/mockup`; 38 file còn `new Random(...)`. Đây là inventory cần xử lý theo call-site, không đồng nghĩa tất cả đều đang hiển thị mock vì một số helper nằm sau nhánh query thật hoặc không được gọi.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Full unit suite: **588/588 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật triển khai — Group Reservation command/report — 07/09/2026

- Nút `Rooming List` và `Danh sách khách` trong Group Reservation hiện mở `GroupRoomingListWindow` thật, thay vì mở lại workspace Group Reservation hoặc hiện MessageBox mock.
- Bổ sung nút `Xác nhận đoàn`; nút gọi `IGroupReservationCommandService.ConfirmAsync` thật, sau đó tải lại danh sách từ DB. Nếu thiếu service, thiếu context hoặc domain từ chối thì chỉ báo lỗi và không đổi trạng thái trong bộ nhớ.
- `Báo cáo đoàn` hiện gọi `IGroupBillingService.GetBillingSummaryAsync` thật và hiển thị tổng phát sinh, thanh toán, đặt cọc và công nợ. Không còn báo cáo số liệu mẫu.
- Master Folio, đặt cọc, hóa đơn, dịch vụ, email, in, nhân bản, đóng/xóa đoàn vẫn chưa có command thật tương ứng; tiếp tục bị chặn/fail-closed.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors** (754 warnings hiện hữu).
- Test nhóm Group: **21/21 pass**.
- Chưa chạy thao tác ghi trên production database; không chạy migration và không thay connection string.

## Cập nhật triển khai — Group Check-out không còn hiển thị dữ liệu giả — 07/09/2026

- `GroupCheckoutWindow` hiện tải các đoàn trạng thái `CheckedIn` và Rooming List từ `IGroupReservationQueryService`/`IRoomingListService` thật theo property hiện tại.
- Các bước Folio, dịch vụ/bàn giao, hóa đơn/thanh toán và xác nhận cuối đã được chuyển sang trạng thái fail-closed; không còn hiển thị các folio, minibar, hóa đơn, số tiền và nhân sự hard-code trước đây.
- Nút xác nhận check-out chưa bật vì hệ thống chưa có aggregate/command transaction cho toàn bộ Folio + dịch vụ + hóa đơn + bàn giao. Không cho phép chỉ đánh dấu phòng đã trả rồi báo hoàn tất toàn đoàn khi các phần còn lại chưa được kiểm soát.
- Backend Rooming List đã sẵn `MarkDepartureAsync`; khi bổ sung aggregate checkout transaction, có thể tái sử dụng command này nhưng cần bảo đảm atomicity/idempotency cho nhiều phòng.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors** (764 warnings hiện hữu).
- Unit suite trước thay đổi: **588/588 pass**; cần chạy lại sau khi hoàn tất nhóm checkout transaction.
- Không chạy migration và không ghi production database.

## Cập nhật kiểm tra sâu — Group/Rooming List và phân biệt code chết — 07/09/2026

### Đã xử lý thêm

- `GroupRoomingListWindow` không còn lấy danh sách đoàn từ `IGroupDashboardService.TopBlocks` (đây là projection theo phòng block nên có thể bỏ sót đoàn chưa Confirmed phòng). Màn hình hiện lấy đoàn từ `IGroupReservationQueryService` thật theo property, rồi mới tải Rooming List bằng `IRoomingListService` thật.
- `IRoomingListService` và `IGroupBillingService` production đang được đăng ký bằng các implementation EF; `EfRoomingListService` đọc assignment thật và ghi nhận Assign/Arrival/Departure thật qua persistence/audit. Import và các mutation chưa có aggregate contract vẫn fail-closed.
- `GroupEngine.cs` còn các implementation `Task.FromResult` trả dữ liệu giả, nhưng không phải implementation đang được DI production chọn cho các interface Group hiện đã có `Ef*Service`. Không dùng sự tồn tại của file này để kết luận màn hình đang hiển thị mock; vẫn cần giữ nó ngoài đường chạy hoặc loại bỏ sau khi kiểm tra toàn bộ call-site.

### Mock còn thực sự trên đường chạy

1. Group Reservation: Folio/Deposit/Invoice/Report/Guest/Service/Email/Print/Export; các command Duplicate/Close/Delete Draft, auto-assign, bulk check-in/out và billing/payment vẫn bị chặn hoặc là MessageBox mock.
2. Group Rooming List: đọc thật nhưng ghép phòng/sửa hồ sơ khách chỉ thay đổi collection trong bộ nhớ; bước Confirm vẫn báo chưa có mutation contract. Import/export mutation chưa ghi DB.
3. Group Service: `GroupServiceViewModel` không có query/command persistence; đường chạy hiện để rỗng và giữ generator `Random` sau `return` như code di trú cũ. Không được bật generator này.
4. Housekeeping: upload ảnh, pause/resume, maintenance/minibar/lost-found/Folio/Reservation links, export/print và một số batch/inspection còn thiếu command/schema tương ứng.
5. Room Master: tạo/sửa/đổi trạng thái đã đi command thật; QR, batch status, lịch sử, liên kết nghiệp vụ, export/print còn thiếu.
6. Cashier/Folio, Shift/Incident/VIP/Housekeeping Message/Exchange Rate: tiếp tục cần đối chiếu từng cửa sổ với DI và bổ sung query/command thật; các lớp `*Engine.cs` trả `Task.FromResult` không được coi là dữ liệu production.

### Kết luận kiểm tra

- Không còn bằng chứng rằng Group Rooming List đang lấy một đoàn/phòng mẫu từ `TopBlocks`; đường đọc đã chuyển sang query persisted.
- Vẫn chưa thể gọi toàn bộ module là “dùng dữ liệu thật hoàn toàn” vì còn nhiều write path fail-closed/mock như danh sách trên. Ưu tiên tiếp theo: Group Rooming List mutation + Group Service persistence, sau đó Housekeeping evidence/links và Folio/payment allocation.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors** (754 warnings hiện hữu của solution).
- Unit suite: **588/588 pass**.
- Không chạy migration, không ghi production database và không thay connection string.

## Triển khai tiếp — Tỷ giá dùng dữ liệu thật — 07/09/2026

- Bổ sung aggregate `HotelExchangeRate` theo `PropertyId`, mã tiền tệ, mua vào/bán ra/Hotel Rate, thời gian hiệu lực, trạng thái, mặc định, ghi chú và row-version.
- Bổ sung `IExchangeRateQueryService`/`IExchangeRateCommandService` và `EfExchangeRateService`: đọc/lọc thật, thêm, sửa, đặt mặc định duy nhất trong property, property scope, optimistic concurrency và audit.
- `ExchangeRateWindow` không còn static `LoadMockData()` hoặc số liệu 18/16/3/2/VND; màn hình lấy từ EF, hiển thị rỗng và lỗi cấu hình khi thiếu context/query, không hiển thị dữ liệu mẫu.
- Bổ sung form thêm/sửa/sao chép tỷ giá; sau command thành công UI tải lại từ database. Lịch sử trên UI chỉ tham chiếu audit log thật, không dựng timeline giả.
- Tạo migration `20260907221500_AddHotelExchangeRates` cho bảng `fo.TyGia` và các index property/currency/effective-date. Chưa chạy migration trên database nào.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors** (750 warnings nền hiện hữu).
- Test riêng Exchange Rate: **2/2 pass**.
- Full unit suite: **603/603 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

### Danh sách mock còn đang hiển thị trên đường chạy người dùng

Quét chuỗi `UI MOCKUP`, `LoadMockData`, `GRP-MOCK` và các hành động thay đổi DTO trong `ASMILE.BO.App` cho thấy các nhóm sau vẫn cần xử lý riêng; đây là các mục còn lại, không được đánh dấu “Đang hoạt động” khi đưa vào vận hành:

1. **Housekeeping mutation/UI:** `CleanRoomViewModel`, `DirtyRoomViewModel`, một phần `CleaningInProgressViewModel`, `RoomInspectionViewModel` và `HousekeepingRoomStatusViewModel` còn các lệnh phân công/đổi attendant/pause-resume/batch, upload ảnh, mở Minibar/Lost & Found/Folio/Reservation và export/print dạng mock hoặc chỉ đổi DTO trong bộ nhớ. Query board là thật nhưng write path chưa đồng nhất; cần dùng chung `IHousekeepingWorkflowService`/assignment/photo contract rồi reload từ DB.
2. **Room Master:** query phòng đã có repository thật, nhưng thêm phòng, đổi trạng thái, batch OOO/OOS, đổi phòng, history, QR, export/print vẫn là UI mock. Cần room master command + property scope + row-version.
3. **Group:** các nút Folio/Deposit/Invoice/Report/Guest/Service/Email/Print ở `GroupReservationViewModel`, cùng import/export/lock ở các cửa sổ Group, vẫn chưa có command hoàn chỉnh cho folio mapping, billing, rooming-list và service order.
4. **Thu ngân/chứng từ:** `CreateReceiptVoucherWindow` còn ghi rõ không lưu DB; Exchange Rate còn `LoadMockData`; Proforma/Receipt/Invoice wizard còn provider hoặc workflow giả. Không nối tạm vào `BusinessDocument` nếu chưa có source Folio/customer, approval và audit đầy đủ.
5. **Shift/Incident/VIP/Housekeeping request:** các cửa sổ ghi log hoặc gửi yêu cầu còn lưu bộ nhớ/static; cần schema/command/audit trước khi dùng vận hành.

Các dấu hiệu còn lại trong AR (`Random` nằm sau `return`) chỉ là fixture migration tham chiếu; các màn AR không còn được phép tự sinh khoản thu, allocation, match hoặc hóa đơn thành công trong bộ nhớ.

### Thứ tự tiếp theo

1. Tạo integration test database có một invoice, một payment receipt, một customer và một allocation thật; kiểm tra 6 màn AR hiển thị đúng số dòng/số tiền.
2. Nối mutation dùng `IReceivableService.CreatePaymentReceiptAsync` và bổ sung service allocation/reversal có transaction + audit; loại bỏ các thao tác in-memory.
3. Bổ sung projection invoice lines/payment/folio và CRM company master nếu muốn giữ các tab chi tiết hiện tại.
4. Sau AR mới quay lại các mock đang chạy thật còn lại trong Folio/Thu ngân, Shift/Incident/VIP notes và Exchange Rate.

### Kiểm tra DI production bổ sung

Trong `ASMILE.BO.Infrastructure/DependencyInjection.cs` vẫn có 5 đăng ký mock chạy production cho phần quản trị nền tảng: `ISystemSettingsService`, `IBackupService`, `IServiceStatusService`, `ISecuritySettingsService` và `IPrinterSettingsService`. Hiện repository không có implementation EF thật tương ứng; đây là khoảng trống nền tảng, không được đánh đồng với `IReceivableService` (đã là `EfReceivableService`). Cần tạo contract/schema/adapter thật cho từng service hoặc đổi card/trạng thái sang `Đang hoàn thiện`, không thay bằng dữ liệu giả.

## Cập nhật kiểm tra sâu — Housekeeping và phát hành hóa đơn — 07/09/2026

### Đã xử lý

- `CleanRoomViewModel` và `DirtyRoomViewModel` đã nhận `IHousekeepingWorkflowService` từ DI; các thao tác hoàn tất dọn, đạt/trượt kiểm tra và Accept → Start gọi command EF thật, truyền `PropertyId`/`TaskId`, rồi mới cập nhật UI.
- `HkTaskBoardDto` đã mang thêm `AssignedHousekeeperId` để không phải suy diễn ID từ tên hiển thị.
- Nếu bản ghi không có `HousekeepingTaskId` thật, thiếu context cơ sở, thiếu workflow service hoặc thiếu khóa nhân sự, thao tác bị fail-closed; không còn báo thành công bằng cách đổi DTO trong bộ nhớ.
- `Pause/Resume`, đổi attendant, batch, upload ảnh và các liên kết Minibar/Lost & Found/Folio/Reservation chưa có command tương ứng vẫn bị đánh dấu/chặn, không được coi là nghiệp vụ đã hoàn thiện.
- `HousekeepingRoomStatusViewModel.ChangeAttendant` đã bỏ cập nhật attendant giả.
- `IssueInvoiceWizard` đã tải danh sách Folio thật từ `IHotelFolioRepository`; nút phát hành không còn sinh số `INV-*` ngẫu nhiên và không còn báo đã phát hành khi chưa có invoice command/provider thật.

### Kết luận phân loại mock sau lần rà này

1. **Mock đang nằm trên đường chạy người dùng và cần ưu tiên P0/P1:** `IssueInvoiceWizard.OnIssue`, các nút upload ảnh/batch/liên kết trong Housekeeping, Group Service/Billing, Exchange Rate, Shift/Incident/VIP notes và nhóm quản trị nền tảng đang đăng ký implementation mock trong DI.
2. **Đã có đọc dữ liệu thật nhưng write path còn thiếu:** Housekeeping assignment/pause/photo/batch; Folio split/merge/transfer nếu chưa chọn đủ Folio thật; invoice issue/refund/receipt allocation; group service/billing.
3. **Helper mock chỉ còn để tham chiếu hoặc đã bị bỏ qua bởi production path:** các `Random` trong `Ar*`, Housekeeping fallback sau `return`, `FolioWorkspaceWindow`/`ProformaFolioWindow` helper không được gọi từ constructor/Loaded hiện tại. Không dùng các helper này làm bằng chứng rằng màn hình đang hiển thị mock; cần kiểm tra call site và DI.
4. **Không được mở lại fallback mẫu:** `RoomMasterViewModel`, `WorkforceViewModel`, `LostFoundViewModel`, `MinibarViewModel` phải tiếp tục rỗng + thông báo khi query thật không có; dữ liệu mẫu không phải phương án khắc phục thiếu migration/schema.

### Kiểm chứng sau lần cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite: **588/588 pass**.
- Không chạy migration, không ghi production database và không thay connection string.

### Việc còn cần làm để gọi là hoàn thiện module

1. Bổ sung định danh nhân sự Housekeeping thống nhất giữa `People` (hiện là `int`) và `HousekeepingTask.AssignedHousekeeperId` (hiện là `Guid`) trước khi bật UI phân công thật.
2. Tạo command + projection cho ảnh kiểm tra, pause/resume, batch action, maintenance/minibar/lost-found request và reload sau commit.
3. Tạo invoice issue command/provider thật, sau đó nối lại wizard với approval, idempotency và audit.
4. Hoàn thiện allocation/reversal cho payment, split/merge/transfer Folio và test integration trên database test có dữ liệu thật.
5. Chỉ sau các bước trên mới đổi badge module từ `Đang hoàn thiện` sang `Đang hoạt động`.

## Cập nhật kiểm tra đường chạy thật — Housekeeping identity và mock inventory — 07/09/2026

### Đã xử lý thêm

- `EfHousekeepingBoardService` resolve `HousekeepingTask.AssignedHousekeeperId` sang `AppUsers.DisplayName/UserName` từ database; không còn hiển thị tên nhân sự hard-code khi task đã phân công.
- `CleanRoomViewModel`, `DirtyRoomViewModel` và `CleaningInProgressViewModel` dùng `ICurrentUserService.CurrentUser.Id` khi nút phân công được thực hiện, truyền Guid đó vào `HkTaskAction.Assign`; nếu chưa đăng nhập thì dừng và báo lỗi, không tự tạo Guid giả.
- `CleaningInProgressViewModel` không còn dựng thống kê nhân viên từ mảng tên tĩnh ở đường chạy dữ liệu thật; thống kê được nhóm từ các task đã tải. Các mảng tên cũ chỉ còn phục vụ fixture legacy sau nhánh return.
- Mapping trạng thái Housekeeping đã phân biệt `Pending`, `Assigned`, `Accepted`, `Cleaning`, `WaitingInspection`, `Completed`; trạng thái `Assigned/Accepted` không còn bị hiển thị nhầm thành phòng bẩn.

### Kết quả inventory mock sau khi kiểm tra call-site

- **Mock còn chạy thật:** các nút batch/upload/liên kết ở Housekeeping; các nút Folio/Deposit/Invoice/Report/Guest/Service của Group; mutation Room Master; Create Receipt Voucher; các cửa sổ Group import/export/lock; Shift/Incident/VIP/Housekeeping Message nếu mở trực tiếp.
- **Đọc thật nhưng helper mock không chạy:** Current Guests, Today Arrivals, Today Departures và Stay Profile đều có `IHotelStayRepository` + `Loaded` query thật; helper `LoadMockData` còn trong source nhưng không có call-site runtime hiện hành. Locked Room đã dùng reception service và helper mock chỉ reset rỗng.
- **Đã fail-closed, không phải mock đang hiển thị:** Room Master/Workforce/Lost Found/Minibar khi thiếu query service; Issue Invoice khi thiếu invoice provider; các command Housekeeping khi thiếu `TaskId`, property context hoặc tài khoản nhân sự.
- **Exchange Rate và Housekeeping Message:** constructor hiện để danh sách rỗng và thông báo chưa có query thật; helper mẫu vẫn tồn tại nhưng không được dùng. Đây là thiếu query/schema, không được bật lại dữ liệu mẫu.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors** (750 warnings hiện hữu của solution).
- Unit suite: **588/588 pass**.
- Không chạy migration, không ghi production database và không thay connection string.

## Rà soát toàn bộ mock theo call-site — 07/09/2026

### Bằng chứng quét source

- Quét `ASMILE.BO.App` còn **87** literal `UI MOCKUP` trên đường UI/source và **49** vị trí `new Random(...)`/generator mẫu cần đối chiếu call-site.
- Các nhóm có mock đang được gọi trực tiếp hoặc có nguy cơ báo thành công giả: Housekeeping batch/upload/liên kết; Group Guest/Room Assignment/Rooming List/Service; Room Inspection và Room Master tiện ích; Cashier/Folio/Receipt/Deposit/Refund/Transfer; Shift/Incident/VIP/Exchange Rate/Housekeeping Message; Create Receipt Voucher.
- Không kết luận một màn hình là mock chỉ vì còn helper `Random`/`LoadMockData`: Current Guests, Today Arrivals, Today Departures, Stay Profile và một số màn hình đã có constructor/Loaded query thật. Các helper chết phải được xóa ở đợt dọn code riêng sau khi có test hồi quy.

### Cập nhật đã làm trong vòng này

- `GroupRoomAssignmentWindow` đã đọc danh sách đoàn qua `IGroupReservationQueryService`, Rooming List qua `IRoomingListService` và tồn kho phòng qua `HotelRoomMasterDataQueryHandlers`; không còn khởi tạo danh sách đoàn/phòng/khách giả khi mở màn hình.
- Các thao tác `Assign`, `Auto Assign`, `Swap`, `Release`, `Clear All` và `Xác nhận` ở chế độ dữ liệu thật đều fail-closed. Chúng không đổi collection để giả thành công và không ghi DB khi chưa có transaction command bao trùm assignment + kiểm tra phòng + audit.
- Group Reservation đã có đọc thật, Confirm thật, báo cáo công nợ thật và mở Rooming List thật. Group Checkout đã đọc thật nhưng bước Folio/payment/invoice/service chưa có contract đầy đủ nên vẫn khóa xác nhận cuối.

### Phân loại ưu tiên còn lại

1. **P0 — không được báo thành công:** Group Room Assignment ghi thật theo transaction; Group Checkout aggregate; Issue Invoice command/provider; payment allocation/reversal; Receipt Voucher; Folio split/merge/transfer nếu cửa sổ đang dùng mock generator.
2. **P1 — nghiệp vụ vận hành:** Housekeeping photo, pause/resume, batch inspection, maintenance/minibar/lost-found; Group Guest import/guest profile; Room Inspection batch/photo; Room Master QR/history/link/export/print.
3. **P2 — tiện ích/đầu ra:** export/print/email/duplicate ở Group và Housekeeping; Shift/Incident/VIP/Exchange Rate/Housekeeping Message cần query/schema thật hoặc phải giữ trạng thái “chưa triển khai”, không dùng dữ liệu mẫu.
4. **Hạ tầng:** `CashierEngine`, `GroupEngine`, `ChannelEngine` vẫn chứa implementation `Task.FromResult`/DTO giả. Chỉ cần giữ chúng nếu đã chứng minh không có production DI call-site; nếu có đăng ký production thì phải thay bằng adapter/query/command thật.

### Kiểm chứng sau vòng này

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Full unit suite: **588/588 pass, 0 skipped**.
- Chưa chạy migration, chưa ghi production database và chưa tự tạo dữ liệu mẫu trong DB thật.

## Triển khai P0 — lưu phân phòng đoàn thật theo transaction — 07/09/2026

- Bổ sung `ReplaceGroupRoomAssignmentsRequest` và `IGroupRoomAssignmentCommandService.ReplaceAsync`.
- `EfGroupRoomAssignmentCommandService.ReplaceAsync` kiểm tra property scope, trạng thái đoàn, assignment đã check-in/check-out, số phòng tối đa, phòng trùng, phòng tồn tại/đang Available, guest tồn tại và xung đột lịch với đoàn khác.
- Khi hợp lệ, service xóa các assignment cũ chưa sử dụng, tạo assignment mới, stage audit cho từng thay đổi và gọi **một** `IFrontOfficePersistenceSession.CommitAsync`; không còn ghi từng assignment rời rạc từ UI.
- `GroupRoomAssignmentWindow` cho phép thao tác cục bộ trên snapshot thật, gọi command replace khi bấm Xác nhận, tải lại từ DB sau commit và báo lỗi nếu transaction thất bại. Đoàn đã check-in/check-out/closed vẫn bị khóa.
- Các nút Lock/History/Housekeeping/Print trong cửa sổ này vẫn là phần tiện ích chưa có contract thật; không đánh đồng chúng với đường lưu phân phòng chính.

### Kiểm chứng P0

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Full unit suite sau thay đổi: **588/588 pass, 0 skipped**.
- Chưa chạy migration và chưa ghi production database.

## Cập nhật triển khai — Room Master sửa hồ sơ thật — 07/09/2026

- Nút `Sửa phòng` không còn mở `RoomDetailWindow` mock để báo thành công. Nó đọc lại hồ sơ phòng từ query theo property context, mở form chỉnh sửa các thuộc tính hồ sơ và ghi qua `UpdateHotelRoomCommandHandler`.
- Command truyền `HotelRoomId` và `RowVersion` từ bản ghi mới đọc; sau khi ghi thành công UI tải lại từ repository. Trường hợp không có phòng thật, thiếu handler/context hoặc lỗi optimistic concurrency đều dừng và báo lỗi.
- `RoomDetailWindow` cũ hiện không còn là đường chạy của nút Room Master; không được dùng nó làm bằng chứng rằng thao tác sửa đã hoàn thiện.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors** (750 warnings hiện hữu của solution).
- Unit suite lần đầu có một lỗi không ổn định ở test scheduler; chạy riêng test đó đạt, chạy lại toàn bộ đạt **588/588**.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật triển khai — Housekeeping Dirty Room batch thật — 07/09/2026

- `DirtyRoomViewModel` đã dùng các dòng multi-select có `TaskId` thật để gọi `IHousekeepingWorkflowService` cho `Batch Assign`, `Batch Start` và `Batch Complete`.
- Batch Assign dùng tài khoản đang đăng nhập, không tự sinh nhân sự; Batch Start thực hiện đúng chuỗi `Accept` → `StartCleaning`; Batch Complete chỉ chạy khi checklist hoàn tất và không còn Maintenance/Minibar issue.
- Mỗi dòng chỉ cập nhật trạng thái hiển thị sau khi command EF trả thành công; dòng thiếu `TaskId`, sai trạng thái, thiếu property context hoặc bị domain từ chối không được báo thành công.
- `Batch Inspect` vẫn bị khóa có chủ đích, vì tự động Pass hàng loạt sẽ làm mất kết quả/ghi chú kiểm tra riêng của từng phòng.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors** (754 warnings hiện hữu của solution).
- Unit suite: **588/588 pass**.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật triển khai — Room Master đổi trạng thái thật — 07/09/2026

- `RoomMasterDto` đã giữ `HotelRoomId` và `RowVersion` từ projection thật.
- Nút `Đổi trạng thái` không còn hiện thông báo mock. Nó mở bộ chọn trạng thái vận hành và gọi `ChangeHotelRoomOperationalStatusCommandHandler`, có property scope và optimistic concurrency; thành công sẽ tải lại từ repository.
- Backend vẫn kiểm tra row version qua command hiện có. Nếu command/room/context thiếu, UI fail-closed và không đổi DTO trong bộ nhớ.
- Các chức năng Room Master còn thiếu: thêm phòng, cập nhật hồ sơ, batch status, QR, lịch sử, Reservation/Folio/Room Move, export/print. Chưa đánh dấu module hoàn tất.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**.
- Unit suite trước và sau thay đổi: **588/588 pass**.
- Chưa chạy thao tác ghi trên production database; cần smoke test bằng database test có một phòng thật để xác nhận concurrency và quyền thao tác.

## Cập nhật triển khai — Room Master tạo phòng thật — 07/09/2026

- Nút `Thêm phòng` đã bỏ MessageBox mock và lấy danh sách loại phòng đang hoạt động qua `HotelRoomMasterDataQueryHandlers.SearchTypes` theo property context.
- Form tạo phòng chỉ cho lưu khi có số phòng và loại phòng thật; thao tác ghi đi qua `CreateHotelRoomCommandHandler`, không tự sinh Guid hoặc cập nhật collection trong bộ nhớ.
- Sau khi command thành công, Room Master tải lại từ repository để xác nhận bản ghi đã tồn tại; lỗi duplicate, thiếu property, thiếu loại phòng hoặc lỗi concurrency được giữ ở đường fail-closed.
- Các phần Room Master còn mock đang chạy: sửa hồ sơ phòng, QR, batch status, lịch sử nghiệp vụ, mở Reservation/Folio/Room Move và export/print. Nút đổi trạng thái và tạo phòng đã chuyển sang command thật.

### Inventory mock cập nhật sau khi quét lại call-site

Các nhóm vẫn còn MessageBox/mock data trực tiếp trên đường chạy người dùng gồm:

1. Housekeeping: batch assign/start/complete/inspect, upload ảnh, maintenance/minibar/lost-found/Folio/Reservation links, export/print.
2. Group: Folio/deposit/invoice/report/guest/service/email/print/export; rooming list/guest import-export và lock room.
3. Room Master: sửa hồ sơ, QR, batch, lịch sử/liên kết và export/print.
4. Cashier/Folio: một số cửa sổ cũ như receipt/proforma/refund/transfer vẫn có generator mock; cần xác nhận cửa sổ đó có được DI real view thay thế trước khi sửa, tránh đánh đồng helper chết với đường chạy thật.
5. Shift/Incident/VIP/Housekeeping Message và Exchange Rate còn thiếu query/schema thật hoặc đang dựng list mẫu.

Các `LoadMockData`/`Random` ở Current Guests, Today Arrivals, Today Departures, Stay Profile, Locked Room và một số AR/Folio helper cần tiếp tục đối chiếu với constructor/Loaded/DI; không dùng riêng sự tồn tại của helper để kết luận màn hình đang hiển thị mock.

## Triển khai tiếp — Dịch vụ đoàn dùng dữ liệu thật — 07/09/2026

- Bổ sung aggregate `GroupServiceOrder`, query/command contract và EF service có property scope, kiểm tra đoàn còn hoạt động, transition trạng thái và audit.
- Màn hình Dịch vụ đoàn không còn sinh dữ liệu `Random`; khi mở sẽ đọc `fo.DichVuDoan`, hiển thị rỗng có thông báo nếu database chưa có bản ghi thật.
- Form thêm dịch vụ đoàn dùng danh sách đoàn đọc từ database, ghi qua command thật; xác nhận, hoàn tất và hủy đều commit rồi tải lại từ database.
- Tạo migration `20260907114057_AddGroupServiceOrders`; migration đã được thu gọn chỉ còn bảng/index/FK `fo.DichVuDoan`, không replay các thay đổi nền tảng cũ. Chưa chạy migration trên database.
- Bổ sung unit test domain cho tính tiền, workflow trạng thái và điều kiện hủy; bộ test hiện đạt **592/592 pass, 0 skipped**.

### Rà soát còn lại sau triển khai

- Các engine `CashierEngine`, `ConciergeEngine`, `MaintenanceEngine`, `GroupEngine` có `Task.FromResult`/DTO mẫu nhưng chưa thấy đăng ký trong DI production hiện tại; cần giữ ngoài đường chạy hoặc xoá trong đợt dọn legacy riêng.
- Các helper `Random` sau nhánh `return` ở AR/Housekeeping/Cashier/Deposit/Checkout là code tham chiếu chết, không dùng làm dữ liệu hiển thị; ưu tiên xoá sau khi có smoke test hồi quy.
- Khoảng trống thực sự còn trên đường chạy: batch/upload/liên kết Housekeeping, tiện ích Group (Folio/Deposit/Invoice/Report/Email/Print), Room Master tiện ích, Receipt Voucher/Exchange Rate/Shift/Incident/VIP/Housekeeping Message và các command payment allocation/reversal.
- Không bật lại dữ liệu mẫu để che thiếu schema hoặc thiếu command; các phần chưa có query/command tiếp tục fail-closed và phải được đánh dấu `Đang hoàn thiện`.

### Kiểm chứng sau cập nhật

- Build `ASMILE.BO.App`: **PASS, 0 errors** (750 warnings hiện hữu của solution).
- Unit suite: **588/588 pass**.
- Không chạy migration, không ghi production database và không thay connection string.
## Rà soát bổ sung — Express/Quick Check-out, Housekeeping và Folio Workspace — 07/09/2026

- `ExpressCheckoutWindow`: danh sách hồ sơ và ledger đã đọc từ Stay/Folio thật. Các bước ủy quyền, thanh toán Express, hậu kiểm, hóa đơn và bàn giao không có contract dữ liệu trong module hiện tại; đã bỏ số liệu mẫu (Visa, hạn mức 5M, minibar/giặt là, email, thẻ) và hiển thị rõ “chưa có dữ liệu”, đồng thời giữ nút xác nhận ở trạng thái fail-closed.
- `QuickCheckoutWindow`: danh sách và Folio/ledger thật; đã bỏ các khoản đặt cọc, dịch vụ, công ty, MST và phương thức thanh toán dựng sẵn. Các workflow thanh toán/hóa đơn/Housekeeping chưa có API thật nên không cho giả lập xác nhận.
- `HousekeepingMessageWindow`: board tác vụ và thao tác tạo/cập nhật task dùng service thật. Chat, audit history, room master và thông tin khách trước đây dựng tĩnh; đã đổi thành trạng thái “chưa có API/dữ liệu” thay vì hiển thị hội thoại, mốc thời gian, khách hoặc hạng phòng giả.

## Cập nhật kiểm tra sâu và triển khai tiếp — 08/09/2026

- Đã lập chỉ mục lại `D-ASMILE-BO` trước khi audit: 51.758 node và 158.200 quan hệ. Kết luận chỉ lấy từ đường gọi UI → service → EF/DbContext, không suy diễn từ tên `Mock` hoặc sự tồn tại của `Random`.
- `GroupRoomingListWindow` vẫn đọc đoàn/assignment thật qua `IGroupReservationQueryService` và `IRoomingListService`. Gap còn lại đã được đóng: `EfRoomingListService.ImportAsync` giờ kiểm tra property/group/assignment/room/guest/trạng thái, cập nhật guest/occupancy/VIP qua aggregate, stage audit và commit một lần; UI Xác nhận gọi import thật rồi tải lại từ DB.
- Các generator `Random` còn trong một số `LoadAsync` của AR, Minibar, Lost & Found, Workforce, Housekeeping và Room Master nằm sau `return` có `#pragma warning disable CS0162`; chúng là fixture legacy chết, không phải dữ liệu hiển thị hiện tại. Không xóa trong đợt này để tránh làm nhiễu thay đổi người dùng.
- `CashierEngine`, `ConciergeEngine`, `GroupEngine`, `MaintenanceEngine`, `ExecutiveEngine` và các `Mock*Service` có dữ liệu mẫu nhưng không được đăng ký làm implementation production của các contract Front Office tương ứng; production DI đang dùng `Ef*` hoặc implementation fail-closed.
- Vẫn còn các gap thật cần hoàn thiện: Group Booking wizard chưa có composite command cho toàn bộ room block/rate/deposit/rooming draft; Receipt/Proforma query và provider chứng từ; payment allocation/reversal; export/print/email; Room Detail/Locked Room command; Housekeeping photo/batch/liên kết; Group guest/service/billing tiện ích; và một số cấu hình nền tảng đang dùng `Unavailable*`.

### Kiểm chứng

- Full unit suite sau thay đổi: **648/648 pass, 0 skipped**. Test bổ sung `GroupRoomAssignmentTests`: **2/2 pass**.
- Build qua khi chạy test, không có lỗi biên dịch; các cảnh báo hiện hữu của solution vẫn còn.
- Chưa chạy migration và chưa ghi production database. Cần chạy preflight/backup trên database test trước khi smoke test Rooming List import.
- `FolioWorkspaceWindow`: danh sách, transaction, payment, deposit, refund và history lấy từ ledger thật; đã bỏ history/documents mẫu, thay bằng dữ liệu giao dịch thật và thông báo chưa có chứng từ liên kết. Currency của mapping được lấy từ property context thay vì cố định VND.
- `StayInformationAdjustmentWindow`: danh sách hồ sơ đã chuyển sang Stay thật; điện thoại, email, giấy tờ, người đi cùng, company/TA, rate/discount và conflict không còn tự sinh hoặc dựng sẵn. Các phần này hiện được báo thiếu dữ liệu/API và không được xác nhận giả.
- Kiểm chứng sau vòng này: build App đạt 0 lỗi; test riêng luồng hủy scheduler đạt 1/1. Full unit suite có 602/603 ở một lần chạy do test scheduler timing-sensitive, cần xử lý ổn định hóa test/service trước khi dùng full suite làm gate tuyệt đối.
- Các `LoadGuests`/`LoadMockData` còn lại trong một số view là helper chết dùng cho fallback cũ; không được gọi sau khi Loaded tải repository thật. Cần giữ riêng trong backlog dọn mã, không xem chúng là bằng chứng màn hình đang chạy mock.

## Rà soát đường chạy thực tế — Marketing, Key-card và Gia hạn lưu trú — 07/09/2026

- `ExtendStayWindow`: đã nối `IRateRepository.GetAllAsync` để đọc rate plan active thật, tính tiền phòng theo từng đêm bằng `RatePlan.CalculatePrice`, và nối `IReservationRepository.CheckAsync` để kiểm tra đặt phòng trùng theo phòng/khoảng ngày. Nút xác nhận fail-closed nếu thiếu rate plan, chưa kiểm tra availability, có conflict hoặc thiếu stay/row-version. Lỗi truy vấn rate không làm mất danh sách stay thật.
- `MarketingCenterWindow`: trước đây là mock đang chạy thật trên đường mở màn hình: tạo 28 bản ghi, doanh thu, tỷ lệ chuyển đổi và thao tác thêm/sửa/đổi trạng thái chỉ trong memory. Đã loại bỏ dữ liệu mẫu và thao tác ghi giả; màn hình hiện hiển thị trạng thái chưa có API/repository Marketing thật. Cần triển khai contract, bảng/migration và command riêng trước khi bật CRUD.
- Các màn hình Issue/Disable/Reissue/Return Key Card đọc stay và key hiện tại thật qua `IHotelStayRepository`/`IKeyCardService`. Timeline history tĩnh đã được gỡ; chỉ còn audit thao tác trong phiên hiện tại và thông báo rõ chưa có API lịch sử key-card theo stay. Các trường VIP, membership, company, balance và giới hạn số thẻ vẫn chưa có nguồn dữ liệu riêng, không được dùng làm bằng chứng nghiệp vụ thật.
- `KeyCardAuditLogWindow` đã có đường đọc `AppDbContext.KeyCardEvents` thật; không suy luận lịch sử từ timeline mẫu của các cửa sổ thao tác.

### Kiểm chứng sau vòng này

- Build `ASMILE.BO.App`: **PASS, 0 errors**; warning hiện hữu của solution vẫn còn.
- Unit suite hiện tại: **603/603 pass, 0 skipped**.
- Không chạy migration, không ghi production database và không thay connection string.

## Cập nhật — Đọc GuestProfile thật trong Điều chỉnh thông tin lưu trú — 07/09/2026

- `StayInformationAdjustmentWindow` đã lấy `GuestId` từ Stay thật và gọi `IGuestProfileRepository.GetAsync` theo property để nạp điện thoại, email, quốc tịch, địa chỉ, ngày sinh, VIP, company và số giấy tờ thật khi hồ sơ tồn tại.
- Đã xoá dòng khách đi cùng dựng sẵn và toàn bộ helper sinh 120 hồ sơ mẫu trong màn hình này. Trường không có contract hoặc không có dữ liệu tiếp tục hiển thị “Chưa có dữ liệu”, không thay bằng giá trị giả.
- Nút xác nhận vẫn fail-closed vì chưa có command nghiệp vụ cho phép cập nhật hồ sơ khách/thời gian lưu trú từ màn hình này; không hiển thị thành công giả và không ghi database.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 748 warning hiện hữu của solution.
- Unit suite: **603/603 pass, 0 skipped**.

## Cập nhật triển khai — Room Master mở workflow Room Move theo Stay thật — 08/09/2026

- Context menu `Đổi phòng` tìm Stay đang ở thật theo `RoomId/RoomNumber`, rồi mở `RoomMoveWindow` với `HotelStayId` đã chọn.
- `RoomMoveWindow` nhận Stay khởi tạo, tự chọn đúng khách và vẫn sử dụng `HotelStayApplicationService.MoveRoomAsync` với phòng đích thật và `RowVersion` của Stay.
- Không có Stay thật, thiếu context/service hoặc thiếu RowVersion thì thao tác dừng; không tạo luồng đổi phòng giả.

### Kiểm chứng

- Build `ASMILE.BO.App`: **PASS, 0 errors**; 740 warnings hiện hữu.
- Unit suite (loại integration): **635/635 pass, 0 skipped**.
- Không chạy migration và không ghi production database.
