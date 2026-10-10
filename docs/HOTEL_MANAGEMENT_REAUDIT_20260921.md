# Rà soát lại Quản lý khách sạn — cập nhật 22/09/2026

## Kết luận điều hành

Các luồng lễ tân cốt lõi đã có kiểm thử persistence SQL end-to-end; các báo cáo Front Office trong trung tâm báo cáo dùng truy vấn dữ liệu thật theo cơ sở đang chọn. Đã sửa các lỗi làm sai doanh thu/ngày hạch toán, phân loại tiền đặt cọc và số liệu vận hành; đồng thời dọn điều hướng báo cáo đặt phòng trùng lặp. Trung tâm báo cáo hiện có báo cáo thu ngân/đối soát ca, báo cáo kiểm soát Folio thiếu ngày hạch toán và xuất Excel định dạng sẵn.

**Chưa nên ký go-live tài chính/thương mại hoàn toàn** cho tới khi kế toán duyệt xử lý các Folio lịch sử thiếu ngày hạch toán, hoàn thành UAT trên dữ liệu gần production và xác nhận tích hợp vận hành thực tế. Hệ thống đã bắt đầu lưu lịch sử trạng thái buồng bất biến; báo cáo vận hành tái dựng được ngày quá khứ khi có lịch sử và công khai số phòng được bao phủ, không suy diễn phần còn thiếu.

## Kiểm tra danh mục và điều hướng báo cáo

- Trung tâm **Báo cáo Front Office** hiện có năm báo cáo dùng truy vấn dữ liệu thật: **Tổng hợp vận hành**, **Công suất phòng**, **Doanh thu & thanh toán**, **Thu ngân & đối soát ca**, **Kiểm soát Folio thiếu ngày**.
- Đã bỏ cửa sổ trung gian **Báo cáo đặt phòng** chỉ chuyển tiếp sang trung tâm báo cáo và bỏ trigger giao diện cũ còn sót ở các hub Đặt phòng/Lễ tân. Điều hướng báo cáo tổng hợp đi qua một trung tâm thống nhất.
- Không xóa báo cáo/biểu mẫu gắn với công việc đang xử lý: báo cáo đoàn theo hồ sơ đoàn và lệnh in tại trạng thái buồng là tài liệu tác nghiệp, không phải mục báo cáo tổng hợp bị trùng.
- Trung tâm có lọc/tìm, truy vấn theo khoảng ngày, xem trước, in, xuất PDF, Excel `.xlsx` định dạng sẵn và CSV UTF-8. Excel có tiêu đề, tóm tắt, hàng tiêu đề cố định, bộ lọc và độ rộng cột phù hợp.
- Quyền xem báo cáo và `PropertyId` được kiểm tra ở dịch vụ truy vấn; ngày mặc định lấy từ business date của cơ sở đang hoạt động.

## Kết quả theo báo cáo

### Tổng hợp vận hành

- Tách **đã đến/đã đi thực tế** theo thời điểm check-in/check-out UTC được quy đổi sang ngày địa phương của cơ sở khỏi **dự kiến đến/dự kiến đi** theo lịch lưu trú.
- Truy vấn chỉ lấy các lưu trú có liên quan đến ngày được chọn; tổng hợp trạng thái buồng trong cơ sở dữ liệu thay vì tải toàn bộ lịch sử.
- KPI thu ngân được lọc theo cơ sở và ngày hạch toán, tổng hợp từ ca/bút toán đã lưu; chênh lệch chỉ hiện khi mọi ca trong ngày đã có số tiền kiểm đếm thực tế. KPI buồng phòng lấy số công việc chờ, đã hoàn thành và thời lượng dọn từ dữ liệu tác vụ; thời lượng trừ thời gian tạm dừng.
- Mọi thay đổi trạng thái buồng qua workflow housekeeping được ghi thành lịch sử bất biến trong cùng transaction. Báo cáo theo ngày lấy trạng thái cuối ngày từ lịch sử và hiển thị số phòng có dữ liệu lịch sử. Migration chỉ tạo **baseline** từ trạng thái hiện có; không bịa lịch sử trước thời điểm trạng thái cuối cùng được biết.

### Công suất phòng

- Tính số phòng hoạt động, phòng bán, phòng còn, tỷ lệ lấp đầy, ADR và RevPAR theo ngày; loại lưu trú đã hủy khỏi số phòng bán.
- Tối ưu tính công suất bằng mốc tăng/giảm theo ngày và nhóm doanh thu phòng một lần cho cả khoảng ngày, tránh quét lại dữ liệu cho từng ngày.
- Báo cáo từ chối tính ADR/RevPAR nếu phát hiện bút toán doanh thu phòng lịch sử thiếu `BusinessDate`, tránh âm thầm xuất số liệu thiếu.
- Mẫu số phòng hoạt động hiện phản ánh danh mục phòng hiện tại; muốn báo cáo lịch sử kiểm toán được khi số phòng thay đổi cần lưu phiên bản/ngày hiệu lực của tồn phòng.

### Doanh thu & thanh toán

- Nhóm doanh thu phòng, doanh thu khác, tiền thu, tiền đặt cọc, hoàn tiền và số dư Folio theo `BusinessDate`; tiền đặt cọc không bị tính nhầm thành doanh thu.
- Bút toán điều chỉnh có liên kết bút toán gốc được phân loại theo loại gốc để tránh làm sai doanh thu/thu/hoàn.
- Truy vấn nhóm ngày và phân loại correction chạy trực tiếp trên SQL Server; bài integration đã phát hiện lỗi dịch LINQ/EF ở projection trung gian, đã sửa và chạy lại thành công.
- Các luồng ghi thu, hoàn, chiết khấu/thuế, dịch vụ đoàn, posting và dữ liệu demo đã được nối với business date do ngữ cảnh Front Office cung cấp.
- **Dữ liệu cũ:** các Folio entry đã lưu với `BusinessDate` rỗng không tự được sửa ngược. Báo cáo theo ngày sẽ không thể phân bổ chúng cho đến khi có quy trình đối chiếu và backfill được phê duyệt, có lưu dấu kiểm toán. Không tự động gán ngày theo giờ máy vì sẽ làm thay đổi sổ liệu.
- Công thức số dư theo ngày không thay thế đối soát công nợ cuối Folio hoặc báo cáo kế toán đã khóa sổ; cần xác nhận quy ước dấu, thuế, ngoại tệ và bút toán đảo với kế toán trước phát hành.

### Thu ngân & đối soát ca

- Báo cáo chi tiết theo ca đọc trực tiếp từ `CashierShift`, các movement đã lưu và biên bản đối soát: đầu ca, thu, chi, hoàn, chuyển quỹ, điều chỉnh có dấu, tiền dự kiến, tiền thực tế, chênh lệch và trạng thái đối soát.
- Dữ liệu bị giới hạn theo cơ sở, quyền báo cáo, khoảng ngày tối đa 366 ngày và tối đa 2.000 ca; không có dữ liệu mock hoặc tổng hợp từ UI.

### Kiểm soát Folio thiếu ngày hạch toán

- Báo cáo chỉ đọc liệt kê các bút toán `HotelFolioEntry` chưa có `BusinessDate`, cùng thời điểm tạo UTC và ngày địa phương được đề xuất từ timezone của cơ sở.
- Đây là danh sách phục vụ kế toán đối chiếu, **không tự sửa dữ liệu** và không tự gán ngày theo giờ máy. Backfill chỉ được thực hiện bằng quy trình riêng có preview, phê duyệt và audit trail.

## Cập nhật bổ sung — 2026-09-23

- Backfill Folio hiện có cửa sổ riêng trong trung tâm báo cáo: chọn dòng, kiểm tra/sửa ngày đề xuất, nhập lý do và số tham chiếu phê duyệt; cập nhật được giới hạn theo cơ sở, transaction, retry idempotent và audit `FOLIO_BUSINESS_DATE_BACKFILL`.
- Quyền xuất báo cáo đã tách khỏi quyền xuất trường nhạy cảm. Báo cáo thu ngân tự che tên nhân sự khi xuất/in nếu thiếu `reports.export.sensitive`; audit xuất ghi `sensitive=masked` hoặc `sensitive=full`.
- Hub Báo cáo đã nối đủ năm báo cáo của trung tâm, gồm Thu ngân & đối soát ca và Kiểm soát Folio thiếu ngày; không còn chỉ hiển thị ba lối vào đầu tiên.
- Night Audit đã siết property scope khi khởi động và yêu cầu quyền `FrontOffice.NightAudit.Execute` cho thao tác đánh dấu thất bại; tránh chạy/giải phóng audit ngoài cơ sở hoặc ngoài quyền vận hành.
- Concierge đã hoàn thiện SLA end-to-end: deadline được lưu ở `fo.YeuCauDichVu`, thời điểm phản hồi đầu tiên được ghi khi phân công/tiếp nhận/bắt đầu xử lý, báo cáo SLA đọc dữ liệu thật theo loại dịch vụ và màn hình hiển thị đúng hạn/sắp quá hạn/quá SLA. Yêu cầu không truyền deadline dùng chính sách mặc định: VIP 15 phút, khẩn 30 phút, cao 60 phút, thường 120 phút, thấp 240 phút.
- Concierge đã chặn chuyển trạng thái sau khi yêu cầu đã hoàn tất/hủy/từ chối; thao tác hủy bắt buộc có lý do và phân công bắt buộc có bộ phận. Điều này tránh ghi đè trạng thái cuối hoặc tạo lịch sử giả khi người dùng bấm lặp lại.
- Khung giá đã bổ sung sửa kế hoạch giá, mở/ngừng bán kế hoạch mà không xóa lịch sử và dùng chung bộ tính giá domain cho màn hình preview. Danh mục minibar dùng dữ liệu thật của hàng hóa; khi lưu hàng minibar bắt buộc có nhóm, giá bán, VAT và định mức min/max hợp lệ.
- Luồng `FinancialProcessService` không còn tạo batch rỗng hoặc trả kết quả thuế/dịch vụ giả: service lấy lưu trú đang ở, Folio thật, kế hoạch giá đúng hạng phòng/ngày, ghi phí phòng idempotent theo ngày hạch toán và ghi thuế/phí dịch vụ sau khi làm mới `RowVersion`. Folio, property scope, rate plan hoặc row version thiếu đều dừng rõ ràng; service đã được đăng ký trong DI production.
- Đặt phòng mới nay truyền và lưu `RatePlanId`; khi chạy production có rate repository, wizard chỉ hiển thị kế hoạch giá active đúng cơ sở, hạng phòng và ngày đến, tự chọn theo hierarchy và từ chối xác nhận nếu không có giá hợp lệ. Điều này đóng lỗi trước đây: UI/service có trường rate plan nhưng command tạo bỏ qua nên reservation xác nhận không có nguồn giá.
- Tiền cọc đặt phòng đã nối vào persistence thật: repository map `GuaranteeType`, `GuaranteeExpiryDate`, `DepositAmount`, `DepositDueDate`, `DepositStatus` từ các cột đã có của `fo.DatPhong`; wizard ghi nhận yêu cầu tiền cọc sau khi tạo và hiển thị rõ trạng thái chờ thu. Phương thức thu tiền không còn giả lập tại bước đặt phòng; thanh toán thực tế phải đi qua Thu ngân/Folio.

## Phát hiện ngoài danh mục báo cáo cần giữ trong danh sách phát hành

1. **Backfill dữ liệu Folio cũ:** công cụ backfill có preview, quyền riêng, phê duyệt có lý do/tham chiếu và audit trail đã có; vẫn cần kế toán đối chiếu chứng từ/nhật ký và ký duyệt từng đợt trước khi dùng báo cáo tài chính chính thức.
2. **Lịch sử buồng trước migration:** dữ liệu sau migration có baseline và các thay đổi mới có lịch sử đầy đủ; các ngày cũ chỉ đáng tin trong phạm vi số phòng hiển thị là đã có lịch sử. Không được dùng baseline để khẳng định trạng thái quá khứ.
3. **Chưa kiểm chứng quy mô thương mại:** kiểm thử SQL xác nhận luồng nghiệp vụ trên database test cô lập, không thay thế đo tải, kiểm thử đồng thời nhiều người dùng, UAT theo ca/ngày kinh doanh hay đối soát trên dữ liệu production-like.
4. **Kiểm soát xuất báo cáo:** đã có quyền tách riêng xem/xuất, quyền xuất trường nhạy cảm, nhật ký xuất có tiêu chí/khoảng ngày/số dòng/thời điểm và che tên thu ngân khi chưa được cấp quyền; vẫn cần kiểm thử dữ liệu lớn ở staging.
5. **Cổng thương mại của toàn module:** cần UAT có đại diện lễ tân, buồng phòng và kế toán; xác nhận tích hợp hóa đơn điện tử thực tế, sao lưu/khôi phục, phân quyền theo cơ sở và quy trình xử lý khi database/integration gián đoạn trước khi phát hành.

## Kiểm chứng đã chạy

- Full unit suite: **712 passed, 0 failed, 0 skipped** sau lượt hardening Khung giá, validation danh mục minibar và bảo vệ biên giá khi tắt yield.
- Rate Engine bổ sung kiểm thử cho `SeasonMultiplier`, mùa `Blackout`, chặn
  `HolidaySurcharge` khi chưa có lịch ngày lễ và validation ở domain boundary;
  bộ riêng Rate Engine hiện **22 passed, 0 failed, 0 skipped**; lớp Front Office
  SQL Server đã chạy lại sau hardening và đạt **23 passed, 0 failed, 0 skipped**.
- Integration suite toàn project: **38 passed, 0 failed, 0 skipped**; riêng lớp Front Office SQL Server hiện có **23 passed, 0 failed, 0 skipped** sau lượt kiểm tra lại persistence thật.
- Nhóm kiểm thử báo cáo/thu ngân/Folio/housekeeping: **11 passed** trong lượt xác minh cuối — gồm KPI ca có dữ liệu thật, điều chỉnh đúng dấu, báo cáo Folio chỉ đọc, lịch sử trạng thái buồng bất biến, tái dựng trạng thái theo ngày và fail-closed khi ledger cũ thiếu business date.
- SQL Server Front Office integration: **38 passed, 0 failed, 0 skipped** trên database test cô lập — gồm migration, property isolation, reservation, check-in/check-out, Folio/hóa đơn, thu ngân, night audit, housekeeping, lost & found, concierge, khóa cửa, maintenance, minibar và invoice adjustment.
- Lượt chạy lại toàn bộ sau khi đưa migration VAT/SLA vào danh sách EF và bổ sung tiền điều kiện kho/hàng minibar rõ ràng: **38 passed, 0 failed, 0 skipped**; riêng ba luồng migration/Concierge/Minibar đạt 3/3.
- Hạ tầng liên quan phát hành: **4 passed, 0 failed** — backup SQL thật, accounting flow, cash/bank receipt-payment và reconciliation.
- EF migration tooling: **không còn pending model changes** sau khi đồng bộ snapshot với model hiện tại; migration housekeeping vẫn là migration bổ sung có kiểm tra tồn tại và baseline không phá dữ liệu.
- Migration `20260923130000_AlignCommercialModelSnapshot` bổ sung lớp căn chỉnh snapshot có SQL guard cho các schema minibar/SLA đã từng được triển khai bằng migration thủ công; không chạy lại `AddColumn` và không cập nhật đè dữ liệu seed/hàng đang dùng.
- Build toàn solution cấu hình Release: **thành công, 0 warning, 0 error** trong output folder cô lập để không phải đóng app đang chạy.
- Gói Release mới `E:\ASMILE-BO-commercial-publish-20260923-rate-minibar` đã qua `verify_commercial_package.ps1`: **PASS, 10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Sau khi nối lại financial process với Folio/rate plan thật: **712 unit**, **38 integration** và **23 Front Office SQL** đều passed, 0 failed/0 skipped. Gói Release mới `E:\ASMILE-BO-commercial-publish-20260923-financial-posting-v2` cũng qua **10 checks, 0 PDB**; SHA-256 executable giữ nguyên `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Sau khi hoàn thiện bộ chọn kế hoạch giá trong wizard: **713 unit**, **38 integration** và **23 Front Office SQL** đều passed, 0 failed/0 skipped. Gói cuối `E:\ASMILE-BO-commercial-publish-20260923-rate-plan-ui-v4` qua **10 checks, 0 PDB**; SHA-256 executable vẫn là `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Sau khi nối persistence tiền cọc và khóa các trường tài chính chưa có persistence ở wizard: **713 unit**, **38 integration** và bài SQL tạo đặt phòng → ghi cọc → đọc lại đạt **1/1**; lịch sử reservation kiểm tra đủ `DepositRecorded`. Gói `E:\ASMILE-BO-commercial-publish-20260923-commercial-gates-v6` qua verifier **10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- E-invoice đã được kiểm tra ở mức mã nguồn: provider mặc định là fail-closed,
  cấu hình secret được bảo vệ, queue issue có claim nguyên tử và ghi log lỗi;
  chưa thể xác nhận phát hành thương mại nếu chưa có adapter MISA/VNPT/Viettel
  thật cùng credential staging được phê duyệt.
- Build chạy kèm unit test thành công; kiểm tra encoding XAML thông báo pass.
- Đã dọn cảnh báo nullable `CS8602` trong bài SQL end-to-end bằng kiểm tra/annotation null rõ ràng; lượt build và chạy lại cuối không còn cảnh báo đó.

## Bằng chứng mã nguồn chính

- `ASMILE.BO.App/Views/FrontOfficeReportCenterWindow.xaml(.cs)`, `ASMILE.BO.App/Helpers/ExcelExportHelper.cs` — danh mục 5 báo cáo, bộ lọc ngày, truy vấn, in/PDF/Excel/CSV.
- `ASMILE.BO.Infrastructure/FrontOffice/Reporting/EfReportingQueryService.cs` — truy vấn tổng hợp có property scope, business date, ngày địa phương và các KPI.
- `ASMILE.BO.Core/FrontOffice/Housekeeping/Housekeeping.cs`, `ASMILE.BO.Infrastructure/FrontOffice/Housekeeping/EfHousekeepingWorkflowService.cs`, `ASMILE.BO.Infrastructure/Persistence/Migrations/20260922100000_AddHousekeepingRoomStatusHistory.cs` — lịch sử trạng thái buồng bất biến và baseline có kiểm soát.
- `ASMILE.BO.Core/FrontOffice/Folio/HotelFolio.cs`, `ASMILE.BO.Application/FrontOffice/{Payment,Folio,Settlement}/` và `ASMILE.BO.Infrastructure/FrontOffice/{Posting,Group}/` — ghi ngày hạch toán cho bút toán mới.
- `ASMILE.BO.App/ViewModels/FrontOfficeHubViewModel.cs`, `ReservationHubViewModel.cs`, `ReceptionHubViewModel.cs` và các view tương ứng — điều hướng báo cáo tập trung, không còn mục đặt phòng trung gian.
- `ASMILE.BO.Tests.Unit/FrontOfficeReportingQueryServiceTests.cs`, `HotelFolioTests.cs` và `ASMILE.BO.Tests.Integration/FrontOfficeSqlServerIntegrationTests.cs` — bằng chứng kiểm thử.

## Quyết định phát hành đề xuất

Đánh dấu **đạt kiểm thử chức năng trong phạm vi đã chạy** và đã hoàn thiện các hạng mục mã nguồn còn thiếu của báo cáo/vận hành. Tuy nhiên **chưa ký go-live tài chính/thương mại** cho tới khi hoàn tất backfill Folio được phê duyệt, UAT kế toán trên dữ liệu gần production và xác nhận các tích hợp vận hành thực tế. Trước go-live vẫn cần kiểm thử tải báo cáo nhiều tháng/nhiều cơ sở, diễn tập backup/restore và kiểm tra phân quyền theo cơ sở.

### Bổ sung lượt kiểm tra Điều chỉnh lưu trú

Nhóm Điều chỉnh lưu trú đã được smoke-test trên STA/WPF: phát hiện và sửa lỗi gán `Owner` cho chính cửa sổ hoặc gán owner chưa hiển thị, đồng thời bổ sung xử lý lỗi/log cho Đổi phòng, Gia hạn, Đổi phòng + Gia hạn và Điều chỉnh thông tin. Sau sửa: **713 unit**, **38 integration** passed.

Gói bàn giao sau hardening Điều chỉnh lưu trú: `E:\ASMILE-BO-commercial-publish-20260923-stay-adjustment-v7` qua verifier **10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.

### Bổ sung hardening lớp mở cửa sổ Front Office

- Các route dùng chung của Thu ngân/Tổng đài/Khung giá/Danh mục minibar nay chỉ gán `Owner` khi `MainWindow` thực sự đang hiển thị; nếu không, cửa sổ mở giữa màn hình. Điều này loại bỏ lỗi WPF khi mở từ workspace hoặc trong lúc app chưa hoàn tất hiển thị.
- Full unit suite sau thay đổi: **713 passed, 0 failed, 0 skipped**; build Release thành công và smoke E2E mở ô Quản lý khách sạn trên đúng executable phát hành đạt **1/1**.
- Gói mới `E:\ASMILE-BO-commercial-publish-20260923-owner-hardening-v8b` qua `verify_commercial_package.ps1`: **PASS, 10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Sau khi rà tiếp các route tra cứu của Lễ tân, `ReceptionHubViewModel` cũng dùng owner an toàn cho các cửa sổ Hồ sơ lưu trú/Khách hiện tại/Khách đến–đi; full unit suite đạt lại **713/713**, smoke E2E **1/1**.
- Gói mới nhất `E:\ASMILE-BO-commercial-publish-20260923-reception-owner-hardening-v9` qua verifier **10 checks, 0 PDB**, cùng SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Nhật ký FO được harden thêm: các cửa sổ Nhật ký ca, Sự cố, Lost & Found, VIP và Bàn giao ca chỉ nhận owner visible, nếu không sẽ căn giữa màn hình. Smoke E2E trên gói v10 đạt **1/1** sau khi làm ổn định thao tác nhập PasswordBox của test fixture; unit suite vẫn **713/713**.
- Gói mới nhất `E:\ASMILE-BO-commercial-publish-20260923-fo-log-owner-hardening-v10` qua verifier **10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Dashboard Bảo trì đã được hoàn thiện dữ liệu thật: `EfWorkOrderService.GetDashboardAsync` đọc tài sản đang hoạt động và lịch bảo trì từ `fo.TaiSanBaoTri`/`fo.LichBaoTriDinhKy`, tính quá hạn, kế hoạch trong 7 ngày, tài sản cần bảo trì, tài sản rủi ro và danh sách tài sản nghiêm trọng; không còn trả cố định 0 cho các trường này.
- Sau thay đổi Dashboard Bảo trì: build Release **0 warning/0 error**, full unit **713/713**, smoke E2E trên executable phát hành **1/1**. Gói `E:\ASMILE-BO-commercial-publish-20260923-maintenance-dashboard-v11b` qua verifier **10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Dashboard điều hành Buồng phòng đã nối thêm ba KPI từng trả cứng bằng 0: Đồ thất lạc đang xử lý đọc `LostFoundItems`, vải loại trong tháng đọc `HousekeepingLinenTransactions`, yêu cầu bổ sung amenities hôm nay đọc `HousekeepingAmenityRefillRequests`; tất cả đều lọc theo cơ sở và mốc UTC hiện tại.
- Sau thay đổi Dashboard Buồng phòng: full unit **713/713**, build Release **0 warning/0 error**, smoke E2E trên đúng gói phát hành **1/1**. Gói `E:\ASMILE-BO-commercial-publish-20260923-housekeeping-dashboard-v12` qua verifier **10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Integration suite sau lượt nối KPI Buồng phòng: **38/38 passed, 0 failed, 0 skipped**.
- Dashboard giám sát Buồng phòng không còn dùng tên nhân viên cố định `Housekeeper` hoặc supervisor cố định: tên được lấy từ `AppUsers`, danh sách chỉ tính nhân viên có công việc đang hoạt động; các trường skill/capacity vẫn để trống có chủ đích vì mô hình hiện chưa có nguồn dữ liệu tương ứng.
- Sau hardening này: build Release **0 warning/0 error**, full unit **713/713**, smoke E2E **1/1** trên gói `E:\ASMILE-BO-commercial-publish-20260923-housekeeping-supervisor-v13`; verifier **10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.

### Bổ sung rà soát Dashboard Buồng phòng và Báo cáo trạng thái phòng

- Dashboard nhân viên Buồng phòng đã bỏ tên mặc định `Housekeeper`: tên người dùng hiện được đọc theo `AppUsers` trong phạm vi cơ sở; nếu chưa có tài khoản liên kết thì hiển thị dấu gạch ngang thay vì tạo dữ liệu giả.
- `GetRoomStatusCountsAtAsync` của trung tâm Báo cáo đã có fallback có kiểm soát: khi database cũ chưa có lịch sử `RoomHousekeepingState`, KPI trạng thái phòng đọc từ `HotelRooms.HousekeepingStatus` và `OperationalStatus`; OOO/OOS vẫn được phân biệt, không còn trả toàn bộ số 0 chỉ vì thiếu bản ghi lịch sử.
- Sau hai thay đổi: build Release **0 warning/0 error**, full unit **713/713**, integration **38/38**, smoke E2E trên đúng executable phát hành **1/1**.
- Gói phát hành mới `E:\ASMILE-BO-commercial-publish-20260924-reporting-housekeeping-v14` qua `verify_commercial_package.ps1`: **PASS, 10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.

### Bổ sung hardening owner cho các route còn lại của Lễ tân

- Các cửa sổ mở trực tiếp từ `ReceptionHubViewModel` (Tình trạng buồng, Check-in, Check-in đoàn, Nhắn nhà buồng, Tỷ giá, Hóa đơn và Walk-in) nay dùng cùng quy tắc owner an toàn: chỉ gán `Owner` khi MainWindow đang hiển thị; nếu không thì dùng CenterScreen.
- Các route còn lại của `FrontOfficeHubViewModel` (Đoàn, Buồng phòng, Công nợ, Hồ sơ lưu trú, Tra cứu đặt phòng, Báo cáo, Nhân sự & ca, Marketing và Night Audit) đã bỏ việc gán cứng MainWindow ẩn; các cửa sổ mới dùng visible-owner helper, không phát sinh lỗi WPF khi mở từ workspace.
- Sau thay đổi: build Release **0 warning/0 error**, full unit **713/713**, smoke E2E trên đúng executable phát hành **1/1**.
- Gói phát hành mới `E:\ASMILE-BO-commercial-publish-20260924-reception-owner-hardening-v15` qua `verify_commercial_package.ps1`: **PASS, 10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.

### Bổ sung dữ liệu thật cho Dashboard Thu ngân

- `EfCashierDashboardService.GetSupervisorDashboardAsync` không còn trả cứng `PendingReconciliations = 0` và `Unresolved = []`; dashboard đọc các đối soát `Pending`, `VarianceDetected`, `Escalated` theo đúng property từ `CashReconciliations`.
- Các ngăn mở lấy tên thu ngân từ `AppUsers`; không còn hiển thị `null` dù ca đã có `CashierId`.
- Test persistence `CSH_REAL_001` đã mở rộng để tạo ca đóng có chênh lệch, ghi đối soát và xác nhận dashboard hiển thị đúng 1 đối soát chưa xử lý; test mục tiêu **1/1**, full unit **713/713**, integration **38/38**.
- Gói `E:\ASMILE-BO-commercial-publish-20260924-cashier-dashboard-v16` qua verifier **10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Smoke E2E trên cùng executable đạt **1/1** khi database đã migrate. Lần cold-start đầu tiên mất khoảng 42 giây để migrate/seed và vượt ngưỡng chờ 45 giây của fixture; log không có lỗi ứng dụng. Đây là gate cần theo dõi riêng khi staging migration lần đầu, không được coi là bằng chứng cold-start đã đạt SLA.

E2E trên chính executable v16 đã khởi động app, đăng nhập test, mở module Quản lý khách sạn và đạt **1/1** sau khi database đã migrate; không xuất hiện dialog lỗi.

### Bổ sung dữ liệu thật cho Dashboard Concierge và kiểm tra lại DoorLock

- `EfConciergeDashboardService.GetDashboardAsync` đã bỏ các giá trị cứng `Escalated`, `AvgResponseMinutes`, `SlaCompliancePct` và danh sách `BreachedSla`; các chỉ số hiện tính từ `FrontOfficeServiceRequests`, `SlaDeadlineAtUtc`, `FirstResponseAtUtc` và `CompletedAtUtc`, có property scope.
- `GetPerformanceAsync` cũng đã tính `CompletionRate`, `AvgMinutes` và `SlaCompliance` theo từng loại dịch vụ từ dữ liệu đã lưu; `AvgRating` vẫn giữ 0 có chủ đích vì mô hình hiện chưa có bảng đánh giá dịch vụ.
- Đã bổ sung test tạo yêu cầu Concierge quá hạn và yêu cầu hoàn tất đúng SLA để chứng minh dashboard đọc đúng dữ liệu thật: test mục tiêu **1/1**; full unit sau thay đổi **713/713**.
- Đã đối chiếu lại DoorLock với hợp đồng hiện hữu: `AccessDeniedToday` và `FailedAttempts` cùng biểu diễn lượt truy cập bị từ chối, nên không tách sai thành một chỉ số khác. Bài SQL DoorLock sau khi hoàn tác thay đổi thử nghiệm đạt **1/1**.
- Gói phát hành mới `E:\ASMILE-BO-commercial-publish-20260924-concierge-dashboard-v17` qua verifier **10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Smoke E2E warm trên đúng executable v17, database đã migrate, đạt **1/1**. Gate cold-start migration lần đầu vẫn cần theo dõi riêng như ghi nhận ở v16.

### Bổ sung tính thời gian còn lại cho Dashboard nhân viên buồng phòng

- `EfHousekeepingBoardService.GetAttendantDashboardAsync` hiện lấy thời gian dọn trung bình từ các tác vụ đã có `StartedAtUtc`/`CompletedAtUtc`, tính `EstimatedMinutesLeft` theo số tác vụ còn hoạt động và tách danh sách `CurrentTasks`/`NextUp` từ trạng thái thật.
- Bổ sung test persistence `HK_REAL_001`: dữ liệu thật gồm một phòng hoàn tất trong 20 phút và một phòng đang chờ, dashboard trả đúng 20 phút còn lại; full unit đạt **714/714**.
- Full integration sau thay đổi đạt **38/38**, không lỗi/không skip.
- Gói `E:\ASMILE-BO-commercial-publish-20260924-housekeeping-attendant-v18` qua verifier **10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Smoke E2E trên đúng executable v18 và database đã migrate đạt **1/1**. Cold-start lần đầu vẫn là gate riêng do thời gian migration/seed có thể vượt ngưỡng fixture.

### Bổ sung tính tiền restock cho Minibar

- `EfMinibarQueryService.GetSnapshotsAsync` không còn trả cố định `RestockAmount = 0`. Số tiền restock hiện được tính từ các giao dịch restock đã lưu, nhân với `RetailPrice` của danh mục minibar đang hoạt động; số lượng vẫn được cộng theo từng phòng.
- Bổ sung test persistence `MinibarRestockPersistenceTests`: một phòng có 2 sản phẩm restock và đơn giá danh mục tương ứng, kết quả dashboard đọc đúng **2 sản phẩm / 50.000 VND**; nhóm test minibar mục tiêu đạt **4/4**.
- Full unit suite sau thay đổi đạt **714/714**, full integration đạt **38/38**, không lỗi/không skip.
- Gói Release mới `E:\ASMILE-BO-commercial-publish-20260924-minibar-restock-amount-v19` qua `verify_commercial_package.ps1`: **PASS, 10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Smoke E2E trên đúng executable v19 và database đã migrate đạt **1/1**. Gate cold-start migration/seed lần đầu vẫn cần theo dõi riêng vì có thể vượt ngưỡng chờ của fixture; chưa dùng kết quả warm này để kết luận SLA cold-start.

### Bổ sung dữ liệu VIP cho bảng Minibar

- Snapshot Minibar hiện đọc `HotelStay.IsVipService` thay vì luôn trả `false`; tên khách lưu trú và cờ VIP được giữ đồng nhất với stay thật theo phòng.
- Test persistence Minibar đã mở rộng để tạo stay VIP, restock thực tế và xác nhận snapshot trả đúng khách, cờ VIP, số lượng **2** và tiền restock **50.000 VND**; nhóm test mục tiêu đạt **4/4**.
- Full unit suite đạt **714/714**, full integration đạt **38/38**; smoke E2E trên đúng executable v20 đạt **1/1**.
- Gói Release mới `E:\ASMILE-BO-commercial-publish-20260924-minibar-vip-v20` qua verifier: **PASS, 10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Các trường Housekeeper/Supervisor của snapshot vẫn để trống có chủ đích vì mô hình hiện chưa có liên kết công việc phân công tương ứng; không tạo dữ liệu giả để lấp UI.

### Sửa race khởi tạo shell sau đăng nhập và xác nhận cold-start

- `DesktopShellViewModel` nay tuần tự hóa các shell task bằng `SemaphoreSlim`; lần khởi tạo trước login không còn chạy song song và ghi đè context/module catalog sau khi đăng nhập. Đây là nguyên nhân khiến tile Quản lý khách sạn biến mất trên database mới dù quyền và branch đã tồn tại.
- Smoke test UIA tăng thời gian chờ riêng cho module catalog lên 45 giây để phản ánh migration/seed lần đầu; không thay đổi logic nghiệp vụ hay quyền truy cập.
- Full unit đạt **714/714**, full integration đạt **38/38**.
- Cold-start database mới trên đúng executable v23 đạt **1/1** sau khoảng **35 giây**; warm smoke cũng đạt **1/1**, không xuất hiện error dialog.
- Gói Release cuối `E:\ASMILE-BO-commercial-publish-20260924-shell-race-v23` qua verifier: **PASS, 10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Cold-start vẫn ghi cảnh báo seed consistency cho FinancialStatementMapping TT99 và các mã VAT mặc định; đây là dữ liệu nền kế toán cần được seed qua quy trình Accounting/Training Data trước go-live, không phải lỗi crash và không cản trở luồng Front Office đã smoke-test.

### Hoàn tất sửa scope dữ liệu nền khi khởi động database mới

- Đã xác định nguyên nhân thật của cảnh báo trên: các dòng foundation được migration tạo trước khi công ty mặc định được khởi tạo nên `CompanyId` để trống; global company filter vì vậy ẩn FinancialStatementMapping, TaxCodes và các danh mục nền khỏi module kế toán/báo cáo.
- `DatabaseMigrationService.EnsureLegacyFoundationScopeAsync` nay nhận các dòng legacy chưa gán scope vào công ty hiện hành, có bảo vệ không ghi đè công ty đã có cấu hình. Phạm vi gồm BankAccounts, CashFunds, CompanyAccountingProfiles, FinancialStatementMappings, FixedAssetCategories, ReportTemplates, TaxCodes và ToolAssetCategories.
- `CurrentCompanyContext` giữ scope desktop ổn định sau `await`; trước đây giá trị chỉ nằm trong AsyncLocal của continuation và trở về null ở caller, khiến bước sửa scope không chạy.
- Test hồi quy xác nhận dữ liệu legacy được nhận đúng company scope; full unit đạt **715/715**, full integration đạt **38/38**.
- Chạy trực tiếp đúng executable Release trên database mới: **64 dòng foundation** được nhận vào công ty mặc định, gồm **30 FinancialStatementMappings, 5 TaxCodes và 24 ReportTemplates**; log không còn cảnh báo seed consistency sau khi khởi tạo scope.

### Sửa tính giá lưu trú nhiều đêm theo dữ liệu thật

- Bộ tính giá và luồng ghi RoomCharge trước đây lấy đơn giá của ngày đến rồi nhân với số đêm. Điều này sai khi lịch giá, mùa vụ, ngày đóng phòng hoặc yield thay đổi trong thời gian lưu trú.
- `RatePlan.CalculateStayPrice` hiện tính từng ngày trong toàn bộ khoảng lưu trú, ghi audit log theo từng đêm và tổng cuối kỳ. `FinancialProcessService.CalculateAndPostRoomRateAsync` dùng tổng này khi ghi vào Folio; màn hình Khung giá cũng hiển thị breakdown theo từng đêm.
- Bổ sung hồi quy `RATE_DOM_022`: hai đêm có giá 1.500.000 và 2.000.000 phải ra tổng 3.500.000. Full unit sau thay đổi đạt **716/716**.
- Full integration đạt **37/38**; một bài migration bảo trì timeout khi SQL Server tạo schema (46 giây), không liên quan đến luồng Rate/Folio vừa sửa và cần chạy lại trên SQL Server staging ổn định trước khi đóng gate phát hành.
- Màn hình Khung giá được nối thêm lọc kế hoạch theo trạng thái và hạng phòng; danh sách lọc lấy từ `RatePlan` thật, không sinh danh sách mẫu. Release build đạt **0 warning/0 error**, full unit sau lượt UI đạt **716/716**.
- Gói `E:\ASMILE-BO-commercial-publish-20260924-rate-stay-v29` qua `verify_commercial_package.ps1`: **PASS, 10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`. Smoke E2E mở ô Quản lý khách sạn trên đúng executable đạt **1/1**.

### Nối dữ liệu thanh toán thật vào workspace Folio và chốt lại bộ kiểm thử

- `FolioWorkspaceWindow` tiếp tục tải danh sách và ledger từ `IHotelFolioRepository`; phần payment nay đọc thêm `IHotelPaymentRepository.GetForFolioAsync` và ghép theo `HotelPayment.FolioEntryId`.
- `HotelPaymentDto` đã mang theo `FolioEntryId`, vì vậy UI có thể hiển thị đúng số payment, tham chiếu ngoài, phương thức, loại tiền, trạng thái và thời điểm đã lưu; không còn tự gán `Không xác định` khi payment thật có đủ dữ liệu.
- Owner của cửa sổ Folio chỉ gán vào MainWindow khi MainWindow đang hiển thị, tránh lỗi mở cửa sổ con trong trạng thái startup/login hoặc owner không hợp lệ.
- Full unit đạt **716/716**, full integration chạy lại đạt **38/38**; smoke E2E mở module Quản lý khách sạn trên đúng executable v30 và database riêng đạt **1/1**.
- Gói `E:\ASMILE-BO-commercial-publish-20260924-folio-v30` qua `verify_commercial_package.ps1`: **PASS, 10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.

### Rà cuối nhóm Folio và chuẩn hóa mở cửa sổ con

- `SplitFolioWindow` đọc danh sách Folio và các khoản mục từ `HotelFolioApplicationService.SearchAsync`; `TransferPostingWindow` đọc Folio/posting thật và xác nhận bằng `TransferAsync`; `ProformaFolioWindow` đọc ledger, payment, tài liệu và audit từ các repository/service tương ứng. Các block `#if false` còn lại trong các màn này là fixture lịch sử bị vô hiệu hóa, không chạy runtime.
- Chuẩn hóa Owner của ba cửa sổ trên: chỉ gán MainWindow khi cửa sổ chính đang hiển thị, tránh lỗi mở modal/cửa sổ con ở trạng thái startup hoặc login.
- Build App Release đạt **0 lỗi**; full unit đạt **716/716**.
- Gói `E:\ASMILE-BO-commercial-publish-20260924-folio-v31` qua verifier: **PASS, 10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Smoke E2E trên đúng executable v31 và database riêng mở tile Quản lý khách sạn đạt **1/1**.

### Bổ sung dữ liệu lưu trú thật cho các màn hình Folio

- `FolioLedgerDto` nay truyền thêm số đặt phòng, ngày đến, ngày đi, mã hạng phòng và nguồn từ `HotelStay` snapshot; trước đây các trường này bị bỏ qua ở tầng repository nên UI phải hiển thị placeholder.
- `FolioWorkspaceWindow`, `SplitFolioWindow` và `ProformaFolioWindow` đã dùng các trường mới khi hiển thị chi tiết Folio; không thay đổi dữ liệu lịch sử hay sinh dữ liệu mẫu.
- Bổ sung hồi quy SQL Server vào luồng E2E lưu trú cá nhân: ledger phải đọc đúng reservation number, arrival/departure, room type code và source sau khi điều chỉnh lưu trú.
- Build App Release đạt **0 lỗi**; full unit đạt **716/716**; test E2E SQL Server mục tiêu đạt **1/1**.
- Gói `E:\ASMILE-BO-commercial-publish-20260924-folio-v32` qua verifier: **PASS, 10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Smoke E2E chạy đúng executable v32 và database riêng mở tile Quản lý khách sạn đạt **1/1**.

### Tối ưu tải Folio theo lô và hoàn thiện metadata người thanh toán

- `FolioLedgerDto` nay giữ đủ số đặt phòng, ngày đến/ngày đi, mã hạng phòng, nguồn, công ty, đại lý du lịch và thời điểm mở Folio từ `HotelStay`/`Reservation` thật.
- `EfHotelFolioRepository.SearchAsync` đã chuyển sang tải theo lô các stay, reservation và entry theo trang, thay cho truy vấn lặp từng Folio; `FolioWorkspaceWindow` và `ProformaFolioWindow` dùng trực tiếp ledger đã tải, loại bỏ N+1 khi mở danh sách.
- `SplitFolioWindow`, `TransferPostingWindow` và `MergeFolioWindow` dùng payer, billing instruction, company/TA và ngày mở từ dữ liệu thật; không còn gán cứng Guest hoặc số stay thay cho số reservation.
- App Release build đạt **0 warning/0 error**; full unit đạt **716/716**; full integration đạt **38/38**; SQL Server E2E mục tiêu lưu trú đạt **1/1**.
- Gói `E:\ASMILE-BO-commercial-publish-20260924-folio-v33` qua verifier: **PASS, 10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Smoke E2E chạy đúng executable v33 và database riêng đạt **1/1** trong 44 giây, không lỗi dialog khi mở module Quản lý khách sạn.

### Hoàn thiện giao diện Khung giá cho vận hành thương mại

- `RateEngineView` đã hiển thị phản hồi thành công/lỗi ngay trên màn hình, thông tin kế hoạch và hạng phòng đang chọn, occupancy yield và kết quả tính giá; không còn trạng thái xử lý bị im lặng khi thao tác thất bại.
- Màn hình cấu hình kế hoạch giá đã expose đầy đủ giá sàn, giá trần, ngưỡng occupancy và biên độ điều chỉnh yield; validation chặn giá trị ngoài khoảng 0–100%.
- Các hạng phòng, rate plan, lịch giá, mùa/sự kiện, quy tắc và danh mục minibar tiếp tục lấy từ repository/service thật; không bổ sung dữ liệu mẫu vào production path.
- App Release build đạt **0 warning/0 error**; nhóm test Rate Engine và Item/Minibar đạt **25/25**.
- Gói `E:\ASMILE-BO-commercial-publish-20260924-rate-ui-v34` qua verifier: **PASS, 10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Smoke E2E trên đúng executable v34 và database riêng đạt **1/1** trong 46 giây.

### Siết an toàn backup và restore trước khi phát hành

- `SqlServerBackupService.BackupNowAsync` không còn đánh dấu backup hoàn tất chỉ dựa trên file khác rỗng; sau khi tạo `COPY_ONLY, CHECKSUM`, hệ thống chạy `RESTORE VERIFYONLY ... WITH CHECKSUM` trước khi trả kết quả.
- `RestoreAsync` xác thực checksum/header trước khi chuyển database sang `SINGLE_USER`, nhờ đó file hỏng hoặc thiếu không thể đi vào bước restore phá hủy.
- Bài SQL Server backup thật đạt **1/1**, unit backup đạt **2/2**; package v35 verifier đạt **10 checks, 0 PDB** và smoke executable đạt **1/1**.

### Nối giao diện Sao lưu & phục hồi với dịch vụ thật

- Trang Cài đặt không còn rơi vào card TODO cho nhóm backup: `backup-now` gọi `IBackupService.BackupNowAsync`, lịch sử đọc từ `GetRecentAsync`, từng bản ghi có thao tác VERIFY checksum qua `ValidateBackupAsync`.
- Tạo backup có khóa chống thao tác trùng và thông báo lỗi vận hành rõ ràng; chỉ báo hoàn tất sau khi SQL Server đã xác thực checksum/header ở tầng service.
- Restore database và restore file vẫn hiển thị là bị khóa/chưa sẵn sàng ở UI. Đây là chủ ý an toàn vì restore database là thao tác phá hủy; chỉ mở sau khi có quy trình xác nhận, quyền admin và diễn tập staging được phê duyệt.
- Build Release đạt **0 warning/0 error**, full unit đạt **716/716**, smoke E2E đúng executable đạt **1/1**.
- Gói `E:\ASMILE-BO-commercial-publish-20260924-settings-backup-v36` qua verifier: **PASS, 10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.

### Kiểm tra khả dụng phòng thật cho workflow đặt đoàn

- `CreateGroupWindow` không còn báo số phòng vật lý thô với ghi chú “chưa trừ tồn theo booking cùng ngày”. Nút **Kiểm tra khả dụng** nay gọi `IAvailabilityEngine` thật.
- Kết quả kiểm tra theo từng hạng phòng sử dụng property/business date hiện hành, ngày đến/ngày đi và số phòng yêu cầu; engine trừ các `HotelStay` PendingArrival/InHouse và reservation Confirmed/Guaranteed/CheckedIn đang chồng thời gian.
- Inspector phản ánh trạng thái **Đủ phòng theo tồn thực**, **Thiếu phòng theo tồn thực** hoặc **Chưa kiểm tra**. Khi người dùng thay đổi thông tin đoàn/phòng, kết quả cũ bị invalidated để không hiển thị sai.
- Build Release đạt **0 warning/0 error**; full unit đạt **716/716**; smoke E2E đúng executable đạt **1/1**.
- Gói `E:\ASMILE-BO-commercial-publish-20260924-group-availability-v37` qua verifier: **PASS, 10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.

### Chặn overbooking room block ở tầng service

- `AvailabilityRequest` hỗ trợ loại trừ group hiện tại khi xác nhận để không tự xung đột với chính block của nó.
- `AvailabilityEngine` đã tính thêm các room block nhóm đang `Tentative`/`Confirmed`, trừ số phòng đã pickup và chỉ tính các block chồng ngày; nhờ đó tồn bán được không còn chỉ dựa trên phòng vật lý rảnh.
- `EfGroupReservationCommandService` kiểm tra lại từng room block ngay trước `ConfigureBooking(confirm)` và `ConfirmAsync`; nếu tồn không đủ, giao dịch bị từ chối bằng mã lỗi nghiệp vụ thay vì vẫn ghi nhận xác nhận.
- Luồng xác nhận dùng transaction SQL Server `Serializable`, bao trùm kiểm tra tồn và commit; persistence session dùng transaction hiện hành, không mở transaction rời.
- Build Release đạt **0 warning/0 error**; group workflow đạt **15/15**; full unit đạt **716/716**; SQL Server Front Office integration đạt **23/23**; smoke E2E đạt **1/1**.
- Gói `E:\ASMILE-BO-commercial-publish-20260924-group-concurrency-v39` qua verifier: **PASS, 10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.

### Nối các thao tác sau khi tạo đoàn vào màn hình thật

- Các nút hậu tạo trong `CreateGroupWindow` không còn hiển thị như chức năng thật nhưng chỉ bật cảnh báo TODO.
- **Xem hồ sơ đoàn** và **Quay lại danh sách đoàn** mở màn hình danh sách group thật; **Đặt phòng đoàn** mở phân phòng và truyền `GroupReservationId` vừa tạo; **Mở Rooming List** và **Thêm dịch vụ đoàn** mở đúng workspace dữ liệu thật; **Tạo đoàn khác** khởi tạo lại wizard sạch.
- Build Release đạt **0 warning/0 error**; full unit đạt **716/716**; smoke E2E đúng executable đạt **1/1**.
- Gói `E:\ASMILE-BO-commercial-publish-20260924-group-actions-v40` qua verifier: **PASS, 10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.

### Chặn fallback giá khi post tiền phòng

- `FinancialProcessService.CalculateAndPostRoomRateAsync` không còn dùng `baseRate` truyền từ UI làm nguồn thay thế khi không tìm thấy rate plan thật. Trường hợp thiếu kế hoạch giá hoặc kế hoạch không phủ đủ toàn bộ số đêm nay bị từ chối với lỗi nghiệp vụ rõ ràng, tránh ghi sai doanh thu.
- Bộ chọn rate plan hỗ trợ kế hoạch áp dụng toàn cơ sở (`RoomTypeId = null`) nhưng vẫn ưu tiên kế hoạch gắn đúng hạng phòng trước; Night Audit và các luồng post dùng chung quy tắc này.
- Công cụ tính giá trên UI chặn khoảng ngày nằm ngoài hiệu lực của kế hoạch đang chọn thay vì hiển thị một kết quả preview không được phép bán.
- App Release build đạt **0 warning/0 error**; nhóm Rate Engine đạt **23/23**.

### Khóa race khi tải phân bổ thanh toán ở Thu ngân

- `CashierPaymentWindow` nay hủy truy vấn phân bổ cũ khi người dùng đổi Folio, refresh hoặc đóng cửa sổ; kết quả tải nền chỉ được áp dụng nếu vẫn đúng Folio và phiên tải hiện hành.
- Dữ liệu phân bổ được gom vào bộ nhớ cục bộ trước khi commit vào UI, tránh Folio trước ghi đè Folio sau trong bước phân bổ.
- Ô số tiền hỗ trợ số nguyên và định dạng dấu phân cách phổ biến, không còn xóa đồng thời dấu chấm và dấu phẩy làm sai giá trị; nút xác nhận bị khóa nếu số tiền vượt công nợ hoặc phương thức chưa được hỗ trợ.
- Build Release đạt **0 warning/0 error**; full unit đạt **716/716**, 0 skipped; smoke E2E trên database smoke mới đạt **1/1**.
- Gói `E:\ASMILE-BO-commercial-publish-20260924-cashier-allocation-v42b` qua verifier: **PASS, 10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.

### Nối công nợ thật vào inspector Hóa đơn

- `EInvoiceService.GetInvoicesAsync` nay đọc summary của `ReceivableTransaction` liên kết với từng hóa đơn: Nợ phát sinh, đã ghi Có, số dư còn lại và trạng thái.
- Tab **Thanh toán** của `InvoiceManagementWindow` không còn hiển thị thông báo tĩnh “chưa có dữ liệu thanh toán trong summary” khi hóa đơn có liên kết công nợ; mã giao dịch và số liệu thật được hiển thị, còn hóa đơn không liên kết thì báo rõ trạng thái đó.
- Bổ sung unit test kiểm tra summary công nợ persisted và integration SQL Server kiểm tra các cột điều chỉnh hóa đơn sau migration.
- Build Release đạt **0 warning/0 error**; full unit đạt **717/717**, 0 skipped; EInvoice unit đạt **12/12**; integration invoice schema đạt **1/1**; smoke E2E đạt **1/1**.
- Gói `E:\ASMILE-BO-commercial-publish-20260924-invoice-ar-v43` qua verifier: **PASS, 10 checks, 0 PDB**, SHA-256 executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
