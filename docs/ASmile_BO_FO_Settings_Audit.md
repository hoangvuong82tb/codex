# Audit cấu hình Front Office theo `ASmile_BO_FO_Settings.md`

Ngày rà soát: 2026-09-26  
Phạm vi: trung tâm **Cài đặt > Khách sạn & Front Office**, dữ liệu thật theo `PropertyId`, không dùng màn hình tác nghiệp thay cho master data.

## 1. Nguyên tắc hoàn thiện

- Mỗi danh mục nền phải có màn hình cấu hình độc lập, CRUD, bật/tắt, kiểm tra trùng mã, optimistic concurrency và audit.
- Màn hình vận hành như Tra cứu đặt phòng, Room Rack, Check-in, báo cáo không được đưa vào khu Cài đặt.
- Các danh mục có quan hệ phải chọn bằng khóa thật; không lưu tên hiển thị làm khóa nghiệp vụ.
- Giá, thuế, phí dịch vụ, charge code và payment method phải có hiệu lực theo ngày, property và audit; không ghi đè giao dịch đã posted.
- Mỗi mục chưa đủ backend phải hiển thị rõ trạng thái “Chưa có màn hình cấu hình”, không tạo một form giả.

## 1.1. Kết quả đọc lại tài liệu gốc

Đã đọc lại toàn bộ `ASmile_BO_FO_Settings.md` tại `C:\Users\webvi\Desktop` (các mục 01–54 và các tiểu mục). Kết quả đối chiếu:

- Đã hoàn thiện nền dữ liệu và workspace độc lập: tòa nhà, tầng, khu vực, hạng phòng, phòng, hồ sơ cơ sở, các master khách/đặt phòng/dịch vụ/đối tác và minibar theo property.
- Rate Engine, housekeeping/minibar, cashier/shift policy, night audit policy, approval, forms/printer, messaging/integration và Report Center đều đã có workspace hoặc domain riêng; phần còn lại là nối adapter runtime, kiểm thử triển khai và migration.
- Thuế/phí dịch vụ, folio/charge/payment/deposit/discount và các policy đã được tách thành workspace/aggregate riêng; cần tiếp tục thay các điểm nhận mã tự do bằng khóa master và kiểm soát hiệu lực khi tính giao dịch.
- Các khoảng trống thương mại còn lại là room relations nâng cao, policy check-in/out runtime, approval executor, dispatcher forms/messaging/API, permission matrix FO và test UI/persistence trên database triển khai; không coi đây là lý do để gom các master vào catalog chung.
- Các mục báo cáo ở phần 52 được giữ trong **Report Center**, không nhân bản vào Cài đặt theo phần 53.
- Loại khách đã được tách khỏi danh mục chung thành workspace độc lập `HotelGuestTypeProfile` → `fo.LoaiKhachFO`; còn nối giá trị mặc định vào Reservation và Guest Profile runtime.
- Loại hồ sơ đã được tách độc lập thành `HotelProfileTypeProfile` → `fo.LoaiHoSoFO`; không dùng chung bảng loại khách và còn nối default/required fields vào Guest Profile, Company, Agent và Group runtime.
- Hạng VIP đã được tách độc lập thành `HotelVipLevelProfile` → `fo.CapVipFO`; quyền lợi, upgrade, discount, priority và notification không còn là mã mô tả chung.
- Sở thích & yêu cầu chuẩn của khách đã được tách độc lập thành `HotelGuestPreferenceProfile` → `fo.SoThichKhachFO`; có nhóm, kiểu dữ liệu, default/options, scope, priority, cờ an toàn/tính phí và hiệu lực.
- Danh mục yêu cầu khách đã được tách độc lập thành `HotelGuestRequestProfile` → `fo.YeuCauKhachFO`; có loại yêu cầu, priority, department, SLA, auto assignment, notification channels và hiệu lực.
- Loại đặt phòng đã được tách độc lập thành `HotelReservationTypeProfile` → `fo.LoaiDatPhongFO`; có kind, auto confirm/cancel, deposit/guarantee, hold/cutoff, minimum/maximum stay và overbooking.

Các dòng “Đã có danh mục nền” bên dưới chỉ xác nhận CRUD danh mục thật; không có nghĩa là toàn bộ nghiệp vụ thương mại của mục MD đã hoàn thành.

## 2. Phần đã có và đang nối dữ liệu thật

| Nhóm | Màn hình | Dữ liệu/đường đi | Trạng thái |
|---|---|---|---|
| Không gian | Hạng phòng | `HotelRoomType` → `IHotelRoomMasterDataRepository` → EF/SQL | Đã có |
| Cơ sở lưu trú | Hồ sơ cơ sở & business date | `HotelPropertyProfile` → `IHotelStructureMasterDataRepository` → `fo.HoSoCoSoLuuTru`; property-scoped, giờ nhận/trả, timezone, currency, business date, CRUD, bật/tắt, rowversion, audit | Đã có cấu hình nền |
| Cơ sở lưu trú | Giấy phép & hồ sơ | `HotelPropertyDocument` → `IHotelStructureMasterDataRepository` → `fo.HoSoGiayPhepKhachSan`; loại/số, nơi cấp, ngày cấp/hết hạn, file tham chiếu, bắt buộc, CRUD, bật/tắt, rowversion, audit | Đã có cấu hình nền; upload bảo mật/cảnh báo hết hạn còn mở rộng |
| Không gian | Quản lý phòng | `HotelRoomConfigurationViewModel` → room handlers/repository | Đã có |
| Không gian | Trạng thái & quy tắc phòng | `HotelRoomStatusRule` → `IHotelStructureMasterDataRepository` → `fo.QuyTacTrangThaiPhong`; CRUD, bật/tắt, rowversion, audit, lọc FO/HK/OOS | Đã có cấu hình nền |
| Đặt phòng | Chính sách lưu trú | `HotelStayPolicy` → `IHotelStructureMasterDataRepository` → `fo.ChinhSachLuuTru`; hiệu lực, hạn hủy, no-show, đặt cọc, số đêm, trẻ em/thú cưng, CRUD, bật/tắt, rowversion, audit | Đã có cấu hình nền |
| Đặt phòng | Khuyến mại & package | `HotelPromotionPackage` → `IHotelStructureMasterDataRepository` → `fo.KhuyenMaiGoiDichVu`; loại ưu đãi, kiểu/giá trị giảm, hiệu lực, booking window, minimum stay, prepayment, included services, blackout, CRUD, bật/tắt, rowversion, audit | Đã có master độc lập; còn nối vào pricing decision engine |
| Không gian | Tòa nhà | `HotelBuilding` → `IHotelStructureMasterDataRepository` → `fo.ToaNhaKhachSan` | Đã có |
| Không gian | Tầng | `HotelFloor` → structure handlers/repository → `fo.TangKhachSan` | Đã có |
| Không gian | Khu vực | `HotelArea` → structure handlers/repository → `fo.KhuVucKhachSan`; phòng liên kết bằng `HotelAreaId` | Đã có |
| Danh mục FO | Loại khách, loại hồ sơ, VIP, sở thích | `HotelGuestTypeProfile`, `HotelProfileTypeProfile`, `HotelVipLevelProfile`, `HotelGuestPreferenceProfile` → các bảng `fo.*FO` riêng; CRUD, bật/tắt, rowversion, audit | Đã có workspace độc lập; còn nối default/required fields vào runtime |
| Đặt phòng | Source, channel, market segment, reservation type | `HotelBookingSourceProfile`, `HotelBookingChannelProfile`, `HotelMarketSegmentProfile`, `HotelReservationTypeProfile` → các bảng `fo.*FO`; Reservation chọn khóa master và giữ snapshot | Đã có workspace độc lập; status transition vẫn do reservation domain kiểm soát |
| Dịch vụ | Housekeeping rule, wake-up, guest request, complaint | Các profile `HotelHousekeepingRuleProfile`, `HotelWakeupRuleProfile`, `HotelGuestRequestProfile`, `HotelComplaintProfile` → bảng `fo.*FO` riêng | Housekeeping/wake-up/complaint đã có adapter runtime cơ bản; guest request và dispatcher notification vẫn cần nối tiếp |
| Đối tác | Company, travel agent, commission catalog | `HotelCompanyProfile`, `HotelTravelAgentProfile`, `HotelCommissionRuleProfile` → `fo.DoiTacCongTyFO`, `fo.DaiLyDuLichFO`, `fo.QuyTacHoaHongFO`; Reservation chọn khóa thật | Đã có workspace độc lập; còn nối credit/billing/commission vào runtime |
| Đoàn | Chính sách đoàn, allotment, cutoff, release, deposit, rooming list, routing, billing instruction | `HotelGroupPolicy` → `IHotelStructureMasterDataRepository` → `fo.ChinhSachDoanKhachSan`; hiệu lực, CRUD, bật/tắt, rowversion, audit | Đã có master mặc định; còn nối vào group booking decision và kiểm tra allotment thực thi |
| Kiểm soát ca | Cashier/shift policy, float, closing, variance, chuyển quỹ, bàn giao | `HotelCashierShiftPolicy` → `IHotelStructureMasterDataRepository` → `fo.ChinhSachCaThuNgan`; hiệu lực, float đầu ca, supervisor close, ngưỡng sai lệch, chuyển quỹ, ghi chú bàn giao, CRUD, bật/tắt, rowversion, audit | Đã có workspace cấu hình độc lập; còn nối vào thực thi mở/chốt ca và approval matrix |
| Kiểm soát ca | Night audit policy, khóa ngày, validation, retry, posting, chuyển ngày | `HotelNightAuditPolicy` → `IHotelStructureMasterDataRepository` → `fo.ChinhSachNightAudit`; hiệu lực, khóa business date, validation, resume/retry, supervisor approval, room charge, đóng folio, chuyển ngày, CRUD, bật/tắt, rowversion, audit | Đã có workspace cấu hình độc lập; còn nối policy vào executor và approval matrix |
| Kiểm soát ca | Approval matrix, ngưỡng tiền, vai trò, số người duyệt, tách người lập/người duyệt | `HotelApprovalMatrixRule` → `IHotelStructureMasterDataRepository` → `fo.MaTranPheDuyetKhachSan`; nghiệp vụ/hành động, hiệu lực, ngưỡng, số người duyệt, role, lý do, segregation of duties, CRUD, bật/tắt, rowversion, audit | Đã có workspace cấu hình độc lập; còn nối rule vào approval workflow thực thi |
| Kiểm soát ca | Audit & điều chỉnh posted, giới hạn, lý do, phê duyệt và vai trò | `HotelPostedCorrectionPolicy` → `IHotelStructureMasterDataRepository` → `fo.ChinhSachDieuChinhPosted`; domain, mức tiền tối đa, lý do, phê duyệt, role, hiệu lực, CRUD, bật/tắt, rowversion, audit | Đã có workspace cấu hình độc lập; còn nối policy vào `HotelFolio.AddCorrection` và correction approval |
| Rule Engine | Business rule theo property, condition reference, ưu tiên, blocking/exclusive, hiệu lực | `HotelBusinessRuleProfile` → `IHotelStructureMasterDataRepository` → `fo.QuyTacNghiepVuFO`; condition dạng tham chiếu declarative, không lưu mã thực thi, CRUD, bật/tắt, rowversion, audit | Đã có workspace cấu hình độc lập; còn adapter đọc profile vào RuleEngine, version/test và conflict monitoring |
| Biểu mẫu/tích hợp | Email/SMS/API, provider, endpoint, template, retry, timeout, secret reference | `HotelMessagingIntegrationProfile` → `IHotelStructureMasterDataRepository` → `fo.TichHopTinNhanFO`; secret chỉ lưu reference, không lưu plaintext, CRUD, bật/tắt, rowversion, audit | Đã có workspace cấu hình độc lập; còn nối dispatcher/health-check và notification template runtime |
| Biểu mẫu/tích hợp | Template, loại chứng từ, máy in, khổ giấy, số bản, duplex | `HotelFormPrinterProfile` → `IHotelStructureMasterDataRepository` → `fo.MauBieuMayInFO`; mapping theo property, hiệu lực, CRUD, bật/tắt, rowversion, audit | Đã có workspace cấu hình độc lập; còn nối print dispatcher/preview/PDF/email |
| Tài chính FO | Folio type, charge code, deposit/discount | `HotelFolioProfile` → `IHotelStructureMasterDataRepository` → `fo.LoaiFolioFO`; loại Guest/Company/Master/Group/City Ledger, auto create/routing, split/merge/transfer, charge/payment routing, hiệu lực, CRUD, bật/tắt, rowversion, audit | Folio type đã có workspace độc lập; còn nối khóa Folio thật vào posting/settlement và billing instruction |
| Khách & hồ sơ | Loại khách | `HotelGuestTypeProfile` → `IHotelStructureMasterDataRepository` → `fo.LoaiKhachFO`; Individual/Couple/Family/Group/Corporate/VIP/Government/Crew/Long Stay/Other, trường bắt buộc, default rate/payment/market/source, hiệu lực, CRUD, bật/tắt, rowversion, audit | Đã có workspace độc lập; còn nối các default và required fields vào Reservation/Guest Profile |
| Khách & hồ sơ | Loại hồ sơ | `HotelProfileTypeProfile` → `IHotelStructureMasterDataRepository` → `fo.LoaiHoSoFO`; Individual/Company/Travel Agent/OTA/Government/Group/VIP, trường bắt buộc, default rate/payment/market/source, hiệu lực, CRUD, bật/tắt, rowversion, audit | Đã có workspace độc lập; còn nối default/required fields vào Guest Profile, Company, Agent và Group |
| Khách & hồ sơ | Hạng VIP | `HotelVipLevelProfile` → `IHotelStructureMasterDataRepository` → `fo.CapVipFO`; cấp, Welcome, Amenity, Upgrade, Discount %, Priority score, Notification channels, hiệu lực, CRUD, bật/tắt, rowversion, audit | Đã có workspace độc lập; còn nối quyền lợi vào Guest Profile/Reservation/Housekeeping và notification runtime |
| Khách & hồ sơ | Sở thích & yêu cầu chuẩn | `HotelGuestPreferenceProfile` → `IHotelStructureMasterDataRepository` → `fo.SoThichKhachFO`; nhóm, kiểu giá trị, default/options, scope, priority, an toàn/dị ứng, tính phí, hiệu lực, CRUD, bật/tắt, rowversion, audit | Đã có workspace độc lập; còn nối giá trị khách vào Guest Profile/Reservation/Room Allocation và kiểm tra safety/charge runtime |
| Dịch vụ | Danh mục yêu cầu khách | `HotelGuestRequestProfile` → `IHotelStructureMasterDataRepository` → `fo.YeuCauKhachFO`; Request Type, priority, department, SLA phút, auto assignment, notification channels, hiệu lực, CRUD, bật/tắt, rowversion, audit | Đã có workspace độc lập; còn nối SLA snapshot/phân công/thông báo vào Concierge/Front Office runtime |
| Đặt phòng & giá | Loại đặt phòng | `HotelReservationTypeProfile` → `IHotelStructureMasterDataRepository` → `fo.LoaiDatPhongFO`; kind, auto confirm/cancel, deposit/guarantee, hold/cutoff, minimum/maximum stay, overbooking, hiệu lực, CRUD, bật/tắt, rowversion, audit | Đã có workspace độc lập; còn nối khóa loại và snapshot rule vào Reservation runtime |
| Tài chính FO | Payment method | `HotelPaymentMethodProfile` → `IHotelStructureMasterDataRepository` → `fo.PhuongThucThanhToanFO`; loại phương thức, tiền tệ, tài khoản ngân hàng, card terminal, phí, tham chiếu, phê duyệt, mapping BO, CRUD, bật/tắt, rowversion, audit | Đã có workspace độc lập; còn nối vào cashier/settlement và kiểm soát phương thức theo giao dịch |
| Tài chính FO | Deposit | `HotelDepositPolicy` → `IHotelStructureMasterDataRepository` → `fo.ChinhSachDatCocFO`; loại cọc, bắt buộc, tỷ lệ/số tiền, hạn nộp, hoàn trả, chuyển cọc, tịch thu, nhắc hạn, tự áp dụng Folio, hiệu lực, CRUD, bật/tắt, rowversion, audit | Đã có workspace độc lập; còn nối vào reservation/folio và nhắc hạn runtime |
| Tài chính FO | Discount | `HotelDiscountPolicy` → `IHotelStructureMasterDataRepository` → `fo.ChinhSachChietKhauFO`; loại giảm, tỷ lệ/số tiền, mức tối đa, vai trò, cấp duyệt, lý do, accounting treatment, hiệu lực, CRUD, bật/tắt, rowversion, audit | Đã có workspace độc lập; còn nối vào pricing/approval/posting runtime |
| Tài chính FO | Charge Code | `HotelChargeCodeProfile` → `IHotelStructureMasterDataRepository` → `fo.ChargeCodeFO`; nhóm, đơn vị, giá, tax/service charge, revenue/tax account, cost center, quyền manual posting/discount/void, hiệu lực, CRUD, bật/tắt, rowversion, audit | Đã có workspace độc lập; còn thay các điểm posting đang nhận mã tự do bằng khóa Charge Code thật |
| Tài chính FO | Tax & service charge | `HotelTaxServiceChargeRule` → `IHotelStructureMasterDataRepository` → `fo.QuyTacThuePhiDichVuFO`; phân loại thuế/phí, tỷ lệ, inclusive/exclusive, dịch vụ, rate plan, làm tròn, mapping BO, hiệu lực, CRUD, bật/tắt, rowversion, audit | Đã có workspace độc lập; còn nối posting/settlement và kiểm tra xung đột hiệu lực |
| Tài chính FO | Tiền tệ & tỷ giá | `HotelExchangeRateViewModel` dùng `IExchangeRateQueryService`/`IExchangeRateCommandService`; CRUD mua/bán/hotel rate, ngày hiệu lực, đặt mặc định và concurrency theo property | Đã có workspace tập trung; còn nguồn cập nhật tự động và khóa tỷ giá theo giao dịch |
| Giá | Rate Engine | `RateEngineViewModel` và rate handlers | Đã có, cần mở rộng các trường thương mại |
| Cấu hình Housekeeping | Định mức đồ vải & tiện nghi | `HotelHousekeepingSupplyCatalogViewModel` → `IHousekeepingSupplyMasterDataService` → `fo.LoaiDoVai` / `fo.LoaiTienNghi` | Đã có workspace master độc lập theo property, CRUD, trùng mã, bật/tạm ngưng, rowversion/audit; tồn kho và cấp bù vẫn ở màn vận hành riêng |
| Vận hành | Hàng hóa minibar | `HotelMinibarConfigurationViewModel` → `IHotelMinibarMasterDataService` → `fo.HangMinibarFO` → `Items` (liên kết tồn kho) | Đã có workspace riêng, CRUD thật, property-scoped, rowversion/audit; giá/VAT/định mức/mã tính tiền thuộc cơ sở |

## 3. Phân tích đầy đủ theo 54 mục trong tài liệu gốc

| Nhóm nghiệp vụ | Nội dung phải có | Hiện trạng | Việc cần hoàn thiện |
|---|---|---|---|
| Cơ sở lưu trú | Thông tin property, giấy phép, timezone, tiền tệ, business date, giờ nhận/trả | Hồ sơ cơ sở và giấy phép/tài liệu đã tách theo property; `HotelPropertyDocument` có CRUD, hiệu lực, trạng thái, rowversion và audit | Bổ sung upload file bảo mật, logo, hạng sao, cảnh báo hết hạn và rule business-date nâng cao |
| Không gian | Tòa nhà, khu vực, tầng, hạng phòng, phòng, quan hệ phòng | Tòa nhà/tầng/khu vực/hạng phòng/phòng đã tách; phòng có liên kết `HotelAreaId` | Tiếp tục bổ sung các quan hệ phòng connecting/adjacent/parent/child |
| Trạng thái phòng | FO status, HK status, OOS/OOS reason, transition rules | Có domain vận hành và màn hình cấu hình rule độc lập theo property; rule lưu from/to, role/event, approval và bật/tắt | Tiếp tục nối rule vào engine thực thi và bổ sung OOS reason/effective version |
| Khách | Guest type, profile type, VIP level, hồ sơ và trường bắt buộc | Đã có master danh mục nền cho guest/profile/VIP/preference; hồ sơ và rule trường bắt buộc vẫn là domain riêng | Bổ sung field/rule bắt buộc theo loại hồ sơ, không dùng catalog chung thay thế hồ sơ |
| Đặt phòng | Reservation type/status, source, channel, market segment | Đã có master nền cho type/source/channel/market; status transition vẫn nằm trong reservation aggregate | Tách bảng rule chuyển trạng thái và hiệu lực; không cho danh mục ghi đè trạng thái giao dịch |
| Giá | Rate plan, calendar, season/event, promotion, package, policy | Rate Engine có workspace thật cho plan, giá ngày, mùa/sự kiện và rule; promotion/package và chính sách lưu trú có master riêng theo property | Nối promotion/package và policy vào quyết định đặt phòng; tránh tạo thêm màn hình trùng Rate Engine |
| Check-in/out | Rules, approval, early/late, room move, upgrade/downgrade | Có nghiệp vụ/domain rời trong FO | Đưa tham số vào `Quy tắc lưu trú`, không tạo màn hình tác nghiệp trùng |
| Check-in/out | Early/late, grace time, giấy tờ, thanh toán, approval role | `HotelCheckInOutRule` → `fo.QuyTacNhanTraPhong`; workspace độc lập, CRUD, hiệu lực, bật/tắt, rowversion, audit; Stay và Check-in từ Reservation đã đọc rule theo property/ngày hiệu lực | Charge code early/late chưa tự động post Folio; approval matrix tổng quát vẫn cần nối thêm |
| Room move / upgrade | Đổi phòng, nâng/hạ hạng, chênh lệch giá, approval, housekeeping ready | `HotelRoomMoveUpgradeRule` → `fo.QuyTacDoiNangHangPhong`; workspace độc lập, CRUD, hiệu lực, bật/tắt, rowversion, audit | Còn nối rule vào command đổi phòng thực thi và kiểm soát chênh lệch giá theo folio |
| Folio & posted | Folio type, charge code, revenue/tax mapping, payment method, deposit | Folio type, Payment method, Deposit và Charge Code đã có master độc lập; domain posting/payment tách riêng | Bổ sung mapping sang BO, settlement và sửa posted chỉ bằng adjustment |
| Tiền tệ & thuế | Currency, exchange rate, tax, service charge | Tỷ giá dùng `HotelExchangeRate`; tax/service charge dùng `HotelTaxServiceChargeRule`, đều theo property và ngày hiệu lực | Nối tỷ giá/thuế/phí vào posting/settlement, nguồn cập nhật tự động và lịch sử thay đổi theo quyền |
| Dịch vụ | Housekeeping, minibar, wake-up, guest request, complaint | Guest request, housekeeping rule, wake-up rule và complaint catalog đã có master riêng; housekeeping/wake-up/complaint đã có adapter runtime cơ bản; minibar có `fo.HangMinibarFO` property-scoped, liên kết `Items`, cùng màn vận hành thật | Nối dispatcher notification, charge/service-charge mapping minibar, checklist/SLA guest request và định mức vào workflow; không đưa màn vận hành vào Settings |
| Đối tác | Company, travel agent, group, billing instruction, commission | Company/travel-agent/commission đã có master FO độc lập và workspace riêng; group/AR/billing có domain rời | Nối khóa vào Reservation/Rate/Folio runtime; hoàn thiện kiểm tra credit/billing/hợp đồng/commission |
| Kiểm soát ca | Night audit, cashier, shift, approval matrix, correction policy | Đã có workspace độc lập và bảng dữ liệu thật cho cả policy ca, night audit, approval matrix và posted correction | Nối policy/rule vào executor, approval workflow và folio correction thực thi |
| Biểu mẫu/tích hợp | Documents, printers, templates, email/SMS, internal notification, API | Documents, form/printer và Email/SMS/API đã có workspace độc lập trong trung tâm Cài đặt | Nối print/notification dispatcher, health-check, credential vault reference và internal notification |
| Phân quyền/audit | FO permissions, approval, audit trail, correction model | Có security/audit nền tảng | Bổ sung permission matrix theo chức năng FO và audit posted transaction |
| Báo cáo | FO reports theo vận hành, công suất, doanh thu, folio | Đã có Report Center | Chỉ giữ một trung tâm báo cáo; không tạo các mục báo cáo trùng trong Cài đặt |
| Rule Engine | Business rules, thứ tự ưu tiên, hiệu lực, điều kiện và mô phỏng | Đã có workspace `Business rules / Rule Engine`, condition reference declarative, priority, blocking/exclusive và hiệu lực | Bổ sung adapter runtime, version/test trước khi bật và giám sát conflict/hit/miss |
| API & tích hợp | Nội bộ ASmile-BO, POS/OTA/khóa cửa, webhook, API key, retry | Có service rời | Gom cấu hình endpoint/credential/mapping/health vào một nhóm, không để lẻ tẻ |

## 4. Cấu trúc trung tâm Cài đặt chuẩn

1. **Cơ sở lưu trú**: thông tin cơ sở, giấy phép, business date, timezone, giờ nhận/trả.
2. **Không gian & phòng**: tòa nhà, khu vực, tầng, hạng phòng, quản lý phòng, trạng thái và quan hệ phòng.
3. **Khách & hồ sơ**: loại khách, loại hồ sơ, trường bắt buộc, VIP, sở thích.
4. **Đặt phòng & giá**: reservation type/status, source, channel, market segment, rate plan, calendar, promotion, package, policy.
5. **Tài chính FO**: folio, charge code, payment method, deposit, discount, tax, service charge, currency, billing instruction.
6. **Dịch vụ & vận hành**: housekeeping, minibar, wake-up, request, complaint.
7. **Đối tác & đoàn**: company, travel agent, group, commission.
8. **Ca, kiểm soát & tích hợp**: cashier, night audit, approval, permissions, audit, forms, printers, messaging, API.

`Báo cáo FO` không nằm trong Cài đặt: chỉ mở từ **Trung tâm báo cáo** hiện có để tránh trùng menu và trùng logic truy vấn.

## 5. Thứ tự triển khai thương mại

### P0 — khóa dữ liệu nền

- Tạo thông tin cơ sở/lịch nghiệp vụ.
- Mở rộng hạng phòng và phòng theo các trường trong MD; bổ sung room class, diện tích, view, balcony, accessible, VIP, sales flags.
- Bổ sung validation cross-master: phòng phải thuộc hạng phòng, tòa nhà, tầng đang hoạt động; không cho tắt danh mục đang được sử dụng.
- Phần đã tách khỏi catalog chung: guest/profile/VIP/preference, guest request, reservation type, source/channel/market, housekeeping/wakeup/complaint, partner catalog và minibar. Minibar dùng `fo.HangMinibarFO` theo property, chỉ liên kết `Items` cho tồn kho; không dùng `fo.DanhMucThamChieuKhachSan` làm mô hình thương mại cuối cùng.
- Hoàn tất cấu hình nền trạng thái phòng: `fo.QuyTacTrangThaiPhong`, phân biệt FO/HK/OOS, có kiểm tra trùng mã, rowversion/audit và không đưa màn Room Rack vận hành vào Settings.

### P1 — bán phòng và tài chính FO

- Tách các master reservation/source/channel/market segment.
- Mở rộng Rate Engine: rate plan, ngày hiệu lực, mùa, promotion, package, cancellation/no-show/deposit/child policy.
- Tập trung folio, charge code, payment, deposit, discount, tax/service charge/currency; mapping rõ sang BO.

### P2 — kiểm soát và tích hợp

- Nối policy cashier/shift và night audit vào executor thật; bổ sung checklist nghiệp vụ, approval matrix và correction-only cho posted transaction.
- Template biểu mẫu, printer mapping, email/SMS, API/integration health.
- Báo cáo chỉ còn một trung tâm dùng dữ liệu thật; các mục trùng phải bỏ khỏi menu.

## 6. Tiêu chí nghiệm thu cho từng mục

- Có route trong trung tâm Cài đặt và tên route không trỏ vào màn hình vận hành.
- Có repository/service thật theo property, không seed/mock trực tiếp trong ViewModel.
- Có tạo/sửa/kích hoạt/ngừng sử dụng, kiểm tra trùng mã, lỗi dễ hiểu và concurrency.
- Có log audit trước/sau thay đổi.
- Có test domain, test persistence và test UI route; nếu chưa đạt phải hiển thị rõ “Chưa sẵn sàng”, không giả lập trạng thái “Đã cấu hình”.

### Cập nhật 2026-09-26 — Booking Source

`HotelBookingSourceProfile` → `fo.NguonDatPhongFO` đã có workspace và master độc lập theo property, gồm loại nguồn, cờ hoa hồng, mã tham chiếu, market mặc định, tracking, hiệu lực, CRUD, bật/tắt, rowversion và audit. Booking Channel và Market Segment cũng giữ workspace/bảng riêng, không gộp vào Source.
### Cập nhật tiếp 2026-09-26 — Booking Channel và Market Segment

- `HotelBookingChannelProfile` → `fo.KenhDatPhongFO`: loại kênh, online, xác nhận ngoài hệ thống, connector, hiệu lực, CRUD, bật/tắt, rowversion và audit.
- `HotelMarketSegmentProfile` → `fo.PhanKhucThiTruongFO`: loại phân khúc, revenue bucket, rate mặc định, forecast category, hiệu lực, CRUD, bật/tắt, rowversion và audit.
- Ba khái niệm Source/Channel/Market Segment hiện đã có master riêng; wizard Reservation đã tải danh sách theo property và truyền khóa master cùng snapshot khi tạo/sửa. Các màn hình nghiệp vụ khác vẫn cần rà soát tương tự trước nghiệm thu.

### Cập nhật 2026-09-27 — Housekeeping, Wake-up và Complaint

- `HotelHousekeepingRuleProfile` → `fo.QuyTacHousekeepingFO`: rule công việc, SLA, ưu tiên, kiểm tra và tự tạo task.
- `HotelWakeupRuleProfile` → `fo.QuyTacWakeupFO`: lead time, escalation, xác nhận và kênh thông báo.
- `HotelComplaintProfile` → `fo.DanhMucKhieuNaiFO`: category, severity, SLA, escalation và bồi hoàn.
- Cả ba có workspace riêng, repository/property scope, CRUD, bật/tắt, hiệu lực, rowversion, audit, migration và unit test. Housekeeping đã đọc rule `AutoCreateTask` khi xử lý phòng trả và `RequiresInspection` khi hoàn tất task; wake-up đã áp `DefaultLeadMinutes` cho lịch gọi; complaint đã lấy SLA/escalation/department/severity/compensation vào audit nghiệp vụ. Log/task vận hành vẫn là nghiệp vụ riêng, không bị đưa vào Settings.

### Cập nhật 2026-09-27 — kiểm tra định tuyến trung tâm Cài đặt

- 47 route cấu hình khách sạn đang được khai báo trong `SettingsViewModel`; mọi card cấu hình đều có mapping tới workspace riêng, nhánh tạo content và `DataTemplate` tương ứng.
- Kiểm tra tĩnh hiện tại ghi nhận 48 target trên các card (gồm route và mục trạng thái không bấm được); 0 target cấu hình bị thiếu route map, 0 workspace thiếu `DataTemplate`, và không còn target active nào dùng `HotelReferenceCatalogViewModel`.
- Các route mới cho giấy phép, promotion/package, check-in/out, đổi/nâng hạng, tỷ giá, đoàn, cashier/shift, night audit, approval, biểu mẫu/máy in, messaging, correction và business rules đã được bổ sung; không còn card nào rơi vào màn tra cứu phòng hoặc catalog chung.
- Route legacy `hotel-room-master`/tồn kho housekeeping không còn là đích của trung tâm Cài đặt; luồng vận hành vẫn giữ riêng trong Front Office.

### Cập nhật 2026-09-27 — nối quy tắc trạng thái phòng vào nghiệp vụ

- `Quy tắc trạng thái phòng` không còn chỉ là master độc lập: khi đổi trạng thái vận hành phòng, service đọc các rule đang kích hoạt theo property ở domain `OOS` từ `fo.QuyTacTrangThaiPhong`.
- Khi property đã có rule `OOS`, mọi chuyển trạng thái phải khớp cặp trạng thái nguồn/đích; rule có `RequiredRole`, `RequiredEvent` hoặc `RequiresApproval` được kiểm tra trước khi ghi `fo.PhongKhachSan`. Nếu property chưa cấu hình rule nào, hệ thống giữ fallback tương thích dữ liệu cũ và không tự sinh dữ liệu giả.
- Đây là lớp nối runtime của nhóm cấu hình trạng thái. Domain `OOS` được kiểm tra khi đổi trạng thái phòng; các domain `FO`/`HK` vẫn cần mở rộng adapter theo từng nút nghiệp vụ trước nghiệm thu toàn bộ policy engine.

### Cập nhật 2026-09-27 — rule check-in/out được dùng trong Stay

- `HotelCheckInOutRule` hiện được đọc theo ngày hiệu lực/property khi check-in và check-out thật. Hệ thống kiểm tra giấy tờ khách, payment/deposit authorization, cửa sổ early check-in/late check-out theo giờ cấu hình của `HotelPropertyProfile` và role phê duyệt.
- Nếu chưa có rule active hoặc chưa có property profile, luồng cũ vẫn được giữ để không làm hỏng dữ liệu legacy; không có fallback dữ liệu giả. Charge code early/late hiện đã được kiểm tra trong cấu hình nhưng chưa tự động post vào Folio; cần hoàn thiện adapter Posting/Folio trước khi bật thu phí tự động.

### Cập nhật 2026-09-27 — kết quả triển khai đối tác và liên kết Reservation

`Company` hiện có là tenant/AR company; FO đã có bounded context master riêng để tránh dùng nhầm tenant company. Reservation không còn chỉ có snapshot: đã bổ sung các khóa nullable tới Source, Channel, Market Segment, Company, Travel Agent và Commission Rule; snapshot vẫn giữ để bảo toàn lịch sử và dữ liệu cũ.

Ngày 27/09/2026 đã hoàn thành lớp domain/persistence/command, workspace Settings độc lập, validation/property scope/audit/rowversion và unit test cho `HotelCompanyProfile` → `fo.DoiTacCongTyFO`, `HotelTravelAgentProfile` → `fo.DaiLyDuLichFO` và `HotelCommissionRuleProfile` → `fo.QuyTacHoaHongFO`. Migration `20260927075412_AddReservationHotelMasterLinks` tạo bảng partner, index/FK và cột liên kết trên `fo.DatPhong`; migration đang Pending và chưa tự động áp dụng vào database. Wizard Reservation hiện đã có bộ chọn Company/Travel Agent/Commission và truyền khóa master; Rate/Folio cùng các luồng khác vẫn cần rà soát bộ chọn tương ứng trước nghiệm thu thương mại.

### Cập nhật 2026-09-27 — master minibar theo cơ sở

`Hàng hóa minibar` đã được tách khỏi màn quản lý hàng hóa chung ở tầng giao diện thành `HotelMinibarConfigurationViewModel`/`HotelMinibarConfigurationView`. Workspace quản lý dữ liệu thật qua `IHotelMinibarMasterDataService` và bảng `fo.HangMinibarFO`, gồm mã hàng liên kết tồn kho, tên, đơn vị, nhóm, giá bán, VAT, định mức chuẩn, tồn tối thiểu/tối đa, mã tính tiền, phí dịch vụ, tự động ghi Folio, yêu cầu duyệt, thời gian bổ sung, hiệu lực, kích hoạt, concurrency và audit. Màn hình vận hành minibar ưu tiên master theo property để tính giá/VAT, kiểm kê, cấp bù và ghi Folio; dữ liệu `Items` chỉ là nguồn tồn kho liên kết.

Migration `20260927083803_AddHotelMinibarPropertyMaster` và `20260927085243_ExtendHotelMinibarCommercialFields` đã tạo bảng và bổ sung các trường thương mại với unique `(PropertyId, ItemCode)` và FK tồn kho. `EfMinibarQueryService`/`EfMinibarCommandService` đã đọc/kiểm tra master theo property; fallback `Items.IsMinibarItem` chỉ giữ tương thích dữ liệu cũ khi cơ sở chưa có profile. Migration mới được sinh, chưa tự động áp dụng vào database triển khai.

### Cập nhật 2026-09-29 — loại bỏ nhánh mock legacy

- Đã xóa `FoSettingsViewModel` và `SettingsFoundationView` cũ. Nhánh này từng nạp cứng Toà A/B, WIFI, Corporate, OTA, VN/US vào `ObservableCollection` và chỉ thêm dữ liệu trong bộ nhớ, không có repository/DbContext; đồng thời không còn được route active sử dụng.
- Trung tâm Cài đặt hiện tiếp tục dùng các ViewModel master theo property và repository/service EF tương ứng. Không đánh dấu “dữ liệu thật” chỉ dựa trên text giao diện: nghiệm thu phải kiểm tra đường đi ViewModel → service/repository → `AppDbContext` và migration của database triển khai.
- Build Release sau khi dọn legacy: 0 warning, 0 error. Nhóm unit test master/settings/status chạy: 53 passed.
- Phần còn phụ thuộc môi trường triển khai: xác nhận các migration FO ngày 27/09/2026 đã được áp dụng trên database thật; hiện chưa có quyền/kết nối database triển khai trong workspace để kết luận thay cho preflight runtime.

### Cập nhật tiếp 2026-09-29 — nối policy đổi phòng/nâng hạng vào runtime

- `IHotelStructureMasterDataRepository`/`EfHotelStructureMasterDataRepository` đã có truy vấn policy đang hiệu lực theo `PropertyId`, ngày hiệu lực và `IsActive`; không lấy bản ghi tùy ý hoặc dựng policy trong ViewModel.
- `HotelStayApplicationService.MoveRoomAsync` hiện đọc policy active trước khi ghi: phân biệt đổi cùng hạng, nâng hạng và hạ hạng; kiểm tra cờ cho phép, giới hạn số cấp thay đổi, role phê duyệt và trạng thái housekeeping của phòng đích (`VacantClean`/`Inspected`/`Clean`). Thiếu room type thật cũng bị từ chối thay vì mặc định cấp `0`.
- `HotelRoomDto` đã mang theo `HousekeepingStatus` từ entity `HotelRoom` qua projection EF để điều kiện “phòng đã sẵn sàng” dùng dữ liệu thật.
- Nếu property chưa có policy active, hệ thống giữ fallback tương thích dữ liệu legacy; không tự sinh policy hoặc hiển thị trạng thái giả. Chênh lệch giá (`PriceDifferenceMode`) vẫn chưa tự động post vào Folio vì cần adapter pricing/charge-code/folio; đây là việc thương mại còn lại, không đánh dấu hoàn tất.
- Test bổ sung kiểm tra policy cấm nâng hạng và policy yêu cầu housekeeping/approval. Kết quả kiểm tra sau thay đổi: build Release 0 warning/0 error; nhóm test hồi quy liên quan 132 passed; toàn bộ unit suite 806 passed, 0 failed, 0 skipped.

### Cập nhật tiếp 2026-09-29 — post chênh lệch giá đổi/nâng hạng

- Với policy có `PriceDifferenceMode=ChargeDifference`, `HotelStayApplicationService.MoveRoomAsync` đọc các `RatePlan` active thật qua `IRateRepository`, chọn plan theo room type (ưu tiên plan riêng, sau đó plan cấp property), tính lại giá theo ngày ở, số đêm và số khách của stay, rồi lấy `giá phòng đích - giá phòng hiện tại`.
- Nếu chênh lệch khác 0, service tải Folio thật của stay và ghi `Adjustment` dương hoặc âm vào Folio trong cùng persistence session với việc đổi phòng; thiếu Folio, rate plan hoặc rate service thì trả lỗi nghiệp vụ rõ ràng, không tự dùng giá mặc định. `Manual`/`NoCharge`/`None` được giữ đúng nghĩa là không tự động post.
- DI production đã đăng ký `EfRateRepository`/`IRateRepository`; build Release sau thay đổi đạt 0 warning, 0 error. Toàn bộ unit suite hiện tại: 806 passed, 0 failed, 0 skipped.

### Cập nhật tiếp 2026-09-29 — preflight database triển khai

- Đã chạy preflight read-only bằng connection string đã cấu hình trong môi trường hiện tại; secret được giải mã DPAPI chỉ trong bộ nhớ, không ghi/log giá trị bí mật.
- Kết quả: kết nối thành công, `__EFMigrationsHistory` có 205 migration đã áp dụng, đối chiếu với migration code hiện tại còn 0 migration pending. Chính sách môi trường vẫn là `ValidateOnly` và `AllowApplyPendingMigrations=false`, nên không tự chạy `database update`.

### Cập nhật tiếp 2026-09-29 — Virtual File Center dùng dữ liệu thật

- Drive trong `This PC` đọc path và cờ bật/tắt từ Settings Engine (`VFC.*`), không còn dùng metadata dung lượng/tài liệu hardcode trong ViewModel.
- Path được kiểm tra bằng filesystem thật; dung lượng dùng `DriveInfo`, số tài liệu lấy từ `BusinessDocuments`. Path thiếu hoặc không có quyền hiển thị `Chưa cấu hình`/`Không khả dụng`, không dựng thanh dung lượng giả.
- Network Locations đọc cấu hình SQL Server, API, POS, FO và `Storage.DataRootPath`. SQL Server được kiểm tra bằng `DbContext.Database.CanConnectAsync`; endpoint chỉ hiển thị `Đã cấu hình` khi URI hợp lệ, không giả lập trạng thái online; thư mục dùng chung được kiểm tra tồn tại thực tế.
- Các thư mục nghiệp vụ như Kế toán, Sales, HR và Logs vẫn là taxonomy ảo có chủ đích, dùng để điều hướng tài liệu/nghiệp vụ trong database; chúng không được coi là thư mục Windows thật.
- Bản build Release đạt 0 warning/0 error; toàn bộ unit suite hiện tại đạt 806 passed, 0 failed, 0 skipped. UI runtime đã khởi động từ bản build mới; cần UAT trực tiếp để xác nhận dữ liệu path trên đúng môi trường triển khai.

### Cập nhật tiếp 2026-09-29 — sửa lỗi khởi tạo VFC dùng DbContext đồng thời

- Nguyên nhân: `VirtualExplorerViewModel.InitializeAsync` gọi tải `This PC` và `Network` bằng `Task.WhenAll`, trong khi `VirtualExplorerRepository`/Settings Engine dùng cùng scoped `AppDbContext`.
- Hậu quả: EF Core phát sinh `A second operation was started on this context instance before a previous operation completed`, khiến cửa sổ VFC mở trắng và hiển thị hộp thoại lỗi.
- Đã sửa bằng cách tải tuần tự hai nhánh trong cùng lifecycle repository; không nuốt exception và không thay đổi dữ liệu.
- Build Release đạt 0 warning/0 error; test hồi quy Settings/Virtual/Navigation đạt 60 passed, 0 failed. App đã khởi động lại từ bản build mới.

### Cập nhật tiếp 2026-09-29 — thuế và phí dịch vụ phòng dùng rule thật

- `IHotelStructureMasterDataRepository.GetEffectiveTaxServiceChargeRulesAsync` đã được bổ sung để đọc các rule đang hiệu lực từ `fo.QuyTacThuePhiDichVuFO`, lọc theo `PropertyId`, ngày nghiệp vụ, `IsActive`, dịch vụ áp dụng và rate plan; rule cụ thể được ưu tiên trước rule dùng chung.
- `FinancialProcessService.PostTaxesAndChargesAsync` không còn tự áp cứng 10% thuế và 5% phí dịch vụ. Tỷ lệ được lấy từ rule thật, có xử lý `IsInclusive` và `RoundingMode`; nếu thiếu rule hoặc tỷ lệ bằng 0 thì phát cảnh báo và không tự sinh bút toán giả.
- Build Release đạt 0 warning/0 error; toàn bộ unit suite đạt 806 passed, 0 failed, 0 skipped.
- Phần còn cần nghiệm thu thương mại: cấu hình rule thuế/phí thực tế theo từng property, đối chiếu với hóa đơn/Folio và kiểm tra các trường hợp nhiều rate plan trong cùng ngày ở môi trường UAT. Không đánh dấu nhóm này hoàn tất chỉ dựa trên build.

### Cập nhật tiếp 2026-09-29 — siết rule và loại bỏ trạng thái Home giả

- Bộ lọc rule thuế/phí trong `EfHotelStructureMasterDataRepository` đã nhận thống nhất các biểu diễn `ROOM`, `ROOMS`, `ROOMCHARGE`, `Phòng`, wildcard `*`, `Tất cả` và rate plan dùng chung; rule cụ thể vẫn được ưu tiên trước rule dùng chung.
- `NightAuditApplicationService` không còn bỏ qua im lặng khi thiếu rule hoặc rule tạo ra số tiền không hợp lệ. Bước thuế/phí preflight toàn bộ các stay trước khi ghi, sau đó mới post các dòng mới; trường hợp thiếu cấu hình thật sẽ fail rõ ràng và không tạo bút toán giả.
- Night Audit dùng cùng nguyên tắc chọn rate plan với Posting: ưu tiên plan theo room type, sau đó mới fallback plan cấp property. `RatePlanId` được truyền xuyên suốt vào metadata Folio để truy nguyên rule theo rate plan.
- `PostingPipeline` lõi không còn tự áp tỷ lệ thuế/phí mặc định; caller phải truyền tỷ lệ tường minh, tỷ lệ ngoài khoảng hợp lệ bị từ chối.
- Home Settings không còn hiển thị cứng `API Server Running`, `Backup OK`, `Update Checked`, các mốc activity mẫu hoặc `Central Server`; các trạng thái lấy từ service/setting/log thật, còn thiếu dữ liệu hiển thị đúng trạng thái chưa cấu hình/chưa có dữ liệu. Card Home hiện hiển thị tên database thực.
- Xác nhận sau thay đổi: build Release 0 warning/0 error; toàn bộ unit suite 806 passed, 0 failed, 0 skipped.

### Cập nhật tiếp 2026-09-29 — hoàn thiện policy người dùng và dữ liệu máy in

- Các route `users-policy`, `users-approval` và `users-audit` đã nối vào các definition thật `Security.*`/`Logs.*`; màn hình đọc giá trị persisted và lưu qua Settings Engine, không còn là card trạng thái không có tác dụng.
- Mục In ấn hiện hiển thị danh sách máy in từ Windows Print Spooler và ACL đọc từ security descriptor của từng máy in thông qua `WindowsPrinterSettingsService`. Không tạo máy in, queue hoặc quyền mẫu khi Windows không trả dữ liệu.
- Template phiếu/invoice vẫn được đánh dấu `Chưa cấu hình` vì ứng dụng chưa có provider lưu template thương mại; không giả vờ đã hỗ trợ chỉnh sửa.
- Các mục máy trạm/POS/FO/Warehouse hiển thị heartbeat từ `IServiceStatusService` khi có dữ liệu; phần đăng ký máy trạm và mapping đồng bộ vẫn khóa ở chế độ chỉ đọc vì chưa có nguồn persistence tương ứng.
- Xác nhận sau thay đổi: Release build 0 warning/0 error; toàn bộ unit suite 806 passed, 0 failed, 0 skipped. Cần UAT trên Windows có máy in thật để kiểm tra hiển thị spooler/ACL theo quyền tài khoản triển khai.

### Cập nhật tiếp 2026-09-29 — cấu hình module dùng Settings Engine thật

- `ModuleSettingsService` không còn ghi cấu hình module vào JSON trong `AppContext.BaseDirectory\Data\Settings` như một nguồn dữ liệu chính. Mỗi mục cấu hình được đăng ký thành `SettingDefinition` với `Scope=App`, sau đó giá trị được lưu qua `ISettingsService` vào `SettingValues` và có audit reason riêng cho update/reset/migrate.
- Schema module được đặt tên ổn định theo dạng `Module.{appKey}.{settingKey}`; khi mở màn hình, service đồng bộ definition trước rồi đọc giá trị đã lưu. Giá trị mặc định chỉ dùng khi chưa có giá trị persisted, không bị trình bày như cấu hình operator đã lưu.
- JSON cũ chỉ được đọc để migrate một lần khi Settings Engine chưa có giá trị. JSON hỏng/không đọc được không làm crash màn hình; Settings Engine tiếp tục dùng default an toàn. Sau migrate, SQL/Settings Engine là nguồn authoritative.
- Đã đổi `IModuleSettingsService` sang scoped để dùng cùng lifecycle với repository/DbContext và loại bỏ fallback người dùng giả `Administrator`; khi chưa có session, UI hiển thị `Chưa xác định` và audit dùng actor hệ thống.
- Xác nhận sau thay đổi: Release build 0 warning/0 error; toàn bộ unit suite 806 passed, 0 failed, 0 skipped. Phần cần UAT: mở từng module, sửa/lưu/reset, kiểm tra bản ghi `SettingValues` và audit trên database triển khai; không dùng file JSON để kết luận dữ liệu đã lưu.

### Cập nhật tiếp 2026-09-29 — serialize EF operations khi mở VFC từ Settings

- `VirtualExplorerRepository` serialize toàn bộ `GetChildrenAsync`/`SearchAsync`/`GetNodeAsync` trong cùng scoped `AppDbContext`; các hàm nội bộ dùng core method để không tự khóa lồng nhau. Điều này bao phủ cả trường hợp WPF phát sinh navigation/search đồng thời.
- `EfSettingsRepository` serialize đọc/ghi definition và value, đặc biệt giai đoạn seed Settings Engine. Vì vậy VFC có thể mở trong lúc trang Settings còn đang load mà không khởi động hai truy vấn trên cùng context.
- Không thêm retry hoặc biến lỗi thành danh sách rỗng; nếu database/path thật lỗi, UI vẫn nhận trạng thái lỗi để xử lý rõ ràng.
- Xác nhận sau thay đổi: Release build 0 warning/0 error; unit suite 806 passed, 0 failed, 0 skipped; smoke process sống 8 giây sau khi khởi động bản Release rồi được đóng có chủ đích.

### Cập nhật tiếp 2026-09-29 — không gắn nhãn dữ liệu thật khi chưa kiểm tra workspace

- Các card tổng quan FO trong `SettingsViewModel` không còn gắn cứng trạng thái `Đã có dữ liệu thật`/`Đã có nền tảng thật`. Chúng chỉ dẫn tới workspace tương ứng và hiển thị `Mở workspace dữ liệu` hoặc `Mở workspace cấu hình`; số bản ghi/trạng thái thật phải do workspace đọc từ repository theo property.
- Điều này tránh tình trạng landing page khẳng định dữ liệu đã tồn tại trong khi database đang rỗng, migration chưa áp dụng hoặc truy vấn workspace đang lỗi.

### Cập nhật tiếp 2026-09-29 — phân biệt default và cấu hình VFC đã lưu

- `VirtualExplorerRepository` now tracks whether each VFC path/endpoint exists in persisted Settings values. A registry default such as `D:\ACCOUNTING` or `https://localhost` may still be used as a safe fallback, but VFC displays `Mặc định` and `chưa lưu trong Settings Engine` instead of presenting it as an operator-configured endpoint/path.
- Persisted paths continue to be checked with the real filesystem and persisted endpoints are validated as URIs; database connectivity continues to use `CanConnectAsync`. This keeps the virtual taxonomy usable while making configuration provenance visible to operators.
- Final verification after this pass: Release build 0 warning/0 error; unit suite 806 passed, 0 failed, 0 skipped; application smoke process remained alive for 8 seconds after launch and was then closed.

### Cập nhật tiếp 2026-09-29 — dọn command toolbar VFC giả

- Đã loại bỏ 8 command dead-code `New/Cut/Copy/Paste/Rename/Share/Delete/More` từng chỉ hiển thị thông báo `đang mô phỏng` trong `VirtualExplorerViewModel`. VFC không còn quảng bá thao tác ghi/xóa chưa có contract dữ liệu.
- Các thao tác được hỗ trợ thật vẫn giữ nguyên: điều hướng, tìm kiếm, xem chi tiết, copy virtual path, in và xuất PDF; thao tác chứng từ tiếp tục kiểm tra quyền và lấy dữ liệu từ service thật.
- Sau khi dọn command, binary Release tiếp tục build 0 warning/0 error, unit suite đạt 806 passed/0 failed/0 skipped, và smoke launch giữ process sống 8 giây không thoát sớm.
- Vẫn còn các tỷ lệ mặc định ở nghiệp vụ nhà hàng/đồ uống và dữ liệu training/seed có chủ đích; đây không phải fallback của FO Night Audit nhưng cần được phân loại riêng trước khi gọi toàn hệ thống là hoàn toàn cấu hình động.

### Cập nhật tiếp 2026-09-29 — VFC không suy diễn chứng từ và Settings không dựng đường dẫn ảo

- `VirtualExplorerViewModel.TryResolvePaymentVoucherFromDocumentNodeAsync` không còn biến tên hiển thị/số tiền trên node VFC thành `PaymentVoucherRow` khi không tìm thấy giao dịch phải trả tương ứng. Node VFC chỉ là metadata; chứng từ tài chính phải được xác nhận từ `IPayableService`/nguồn dữ liệu thật.
- `VirtualExplorerRepository` không còn đổi lỗi đọc `BusinessDocuments` hoặc danh sách phiếu nhập đã duyệt thành danh sách rỗng. Lỗi database/ truy vấn được phát lên UI để người dùng biết VFC đang không tải được dữ liệu, thay vì nhìn như thư mục không có chứng từ.
- Các node phiếu nhập dẫn xuất có ID ổn định theo mã phiếu (`stock-receipt:{Id}`), tránh mỗi lần mở lại cache sinh ra một ID ảo khác cho cùng chứng từ.
- Home Settings lấy số module từ `IAppModuleService.GetAppsAsync`; catalog 16 module cứng trước đây đã được loại bỏ khỏi ViewModel.
- Chi tiết nâng cao của ứng dụng không còn tự ghép các đường dẫn `D:\ACCOUNTING\Apps`, `Exports`, `Templates`. Màn hình chỉ hiển thị `Storage.DataRootPath` đã lưu trong Settings Engine và trạng thái “Chưa cấu hình” nếu chưa có setting riêng; không trình bày đường dẫn chưa từng được cấu hình như dữ liệu thật.
- Xác nhận sau thay đổi: build Release 0 warning/0 error; toàn bộ unit suite 806 passed, 0 failed, 0 skipped. VFC vẫn có taxonomy nghiệp vụ ảo có chủ đích (Accounting/Sales/HR/Logs); các taxonomy này không được đánh giá là thư mục vật lý thật. Cần UAT mở VFC trên database triển khai để xác nhận quyền truy cập path và dữ liệu BusinessDocuments.

### Cập nhật tiếp 2026-09-29 — loại bỏ catalog app và ma trận quyền giả

- Trang `Ứng dụng đã cài` hiện chỉ lấy module từ `IAppModuleService.GetAppsAsync`/`AppModules` persisted; `ModuleSettingsService` chỉ còn cung cấp schema cấu hình, không được dùng để làm giả module đã cài. Các mục `training-data`/`general-ledger` không còn bị tự chèn riêng vào danh sách.
- `Permission matrix` không còn hiển thị chuỗi quyền cố định hoặc trạng thái `Configured`. `IRoleService.GetRolePermissionCodesAsync` đọc trực tiếp `AppRolePermissions` (hoặc store tương ứng trong test), rồi hiển thị đúng số lượng và mã quyền đã gán theo từng role.
- Đã bỏ nút lưu quyền kiểu mẫu từng tự lấy 8 quyền đầu và ghi vào role đầu tiên; thao tác phân quyền phải đi qua màn hình chọn quyền cụ thể, tránh thay đổi quyền ngoài ý muốn.
- Xác nhận sau thay đổi: build Release 0 warning/0 error; toàn bộ unit suite 806 passed, 0 failed, 0 skipped.

### Cập nhật tiếp 2026-09-29 — nối các luồng VFC còn thiếu vào Settings

- `vfc-retention` hiện gọi `IArchiveService.PreviewRetentionAsync`, đọc policy đang bật và số lượng bản ghi hết hạn từ database; không còn hiển thị retention mẫu.
- `vfc-index` hiển thị số root/drive node do `IVirtualExplorerService` materialize từ cấu hình VFC, kèm kết quả integrity scan gần nhất từ `DataIntegrityScanResults`. Đây là index on-demand hiện có, không phải số liệu index giả.
- `vfc-repair` chỉ cho phép tạo yêu cầu vào `DataRepairOperations` với trạng thái `Queued`; UI nêu rõ đây là hàng đợi để worker xử lý và không tự ý sửa/xóa file. `vfc-rebuild` nay thực hiện làm mới cache on-demand: đọc lại document sources, dựng lại root/drive và trả số liệu thật; không gọi đó là index worker/persistence khi codebase chưa có lớp index riêng.
- `vfc-root` đã nối vào `Storage.DataRootPath` của Settings Engine; nguồn mặc định và nguồn đã lưu tiếp tục được phân biệt.
- Xác nhận sau thay đổi: Release build 0 warning/0 error. Cần chạy unit suite và UAT với database triển khai để xác nhận policy retention, kết quả scan và worker repair thực tế; không đánh dấu rebuild hoàn tất chỉ vì nút đã hiển thị.

### Cập nhật tiếp 2026-09-29 — audit và nối thêm database/system/apps/security

- Các route database `db-info`, `db-schema`, `db-integrity`, `db-maint` và `db-security` không còn rơi vào card generic: chúng đọc connection/health/migration status/settings thật; maintenance chỉ chạy preflight hiện có, không tự reindex/drop/cleanup.
- Integrity checkpoint ghi thật vào `DataIntegrityCheckpoints`; database integrity vẫn hiển thị rõ đây là kết quả scan đã lưu, không gọi một DBCC giả khi chưa có provider tương ứng.
- `system-server` và `system-performance` đọc Windows runtime, connection và infrastructure health; `net-printer` dùng cùng Windows Print Spooler/ACL provider thật.
- Các mục `apps-permissions` và `apps-paths` hiển thị role/permission và `Storage.DataRootPath` đã lưu. `apps-default`/`apps-startup`/một phần advanced vẫn chỉ rõ thiếu provider persistence thay vì tạo trạng thái giả.
- Các mục `sec-sync`/`sec-export` đọc heartbeat/export executions khi có dữ liệu; các route bảo mật dùng chung `logs-security` đã được chuẩn hóa về route theo từng dòng để không mở nhầm toàn bộ editor.
- Maintenance cache có thao tác thật qua `WorkspaceService.ClearWorkspaceCacheAsync`; version/runtime/about lấy từ assembly và Windows runtime. Update channel/license/export diagnostics vẫn khóa nếu chưa có provider thương mại.
- Xác nhận sau thay đổi: Release build 0 warning/0 error; unit suite 806 passed, 0 failed, 0 skipped; smoke process sống 8 giây sau launch. Cần UAT mở từng route trên SQL Server/Windows triển khai để nghiệm thu dữ liệu và quyền thực tế.

### Cập nhật tiếp 2026-09-29 — hoàn tất phân loại các route network còn lại

- `net-shared` đọc `Storage.DataRootPath` và các `VFC.*DrivePath` từ Settings Engine; NAS/SMB chỉ hiển thị `Chưa sẵn sàng` vì chưa có provider kiểm tra share/credential/quyền mạng.
- `net-fw` không dựng danh sách port hoặc tự mở firewall. Màn hình nêu rõ thiếu adapter Windows Firewall/policy persistence và giữ chỉ-đọc để tránh thay đổi hạ tầng ngoài ý muốn.
- Như vậy các route Settings không còn rơi vào placeholder generic do quên nối handler; route chưa có provider đều được ghi rõ nguồn còn thiếu và không sinh dữ liệu giả.

### Cập nhật tiếp 2026-09-29 — database metadata và kiểm tra path/firewall thật

- `db-objects` đã có `IDatabaseAdminService.GetDatabaseObjectsAsync`, đọc table/view/procedure qua `DbConnection.GetSchema` của database đang cấu hình; UI hiển thị số lượng/tên object thật và loại lỗi provider nếu metadata không hỗ trợ.
- `net-shared` kiểm tra `Directory.Exists` cho từng path VFC đã lưu, nên phân biệt `Accessible`, chưa tồn tại/không truy cập được và chưa cấu hình; không coi path mặc định là share thật.
- `net-fw` đọc trạng thái service `MpsSvc` nếu Windows provider trả về, nhưng chưa tự hiển thị danh sách rule hoặc thay đổi firewall khi chưa có adapter policy riêng.

### Cập nhật tiếp 2026-09-29 — Windows Firewall provider chỉ-đọc

- Bổ sung `IWindowsFirewallService`/`WindowsFirewallService`, dùng `HNetCfg.FwPolicy2` để đọc rule Windows Firewall thật (direction, action, enabled, protocol, local ports, application/service) và trạng thái provider.
- Settings giới hạn hiển thị 250 rule, đánh dấu số liệu bị cắt khi vượt giới hạn, giải phóng COM objects và không expose API mở/sửa/xóa firewall.
- Nếu Windows Firewall COM provider không khả dụng, UI hiển thị lỗi provider/không khả dụng; không fallback sang rule mẫu. Release build 0 warning/0 error; unit suite 806 passed, 0 failed, 0 skipped; smoke launch sống 8 giây.

### Cập nhật tiếp 2026-09-29 — VFC refresh on-demand có kiểm chứng

- Bổ sung `VirtualExplorerRefreshResultDto` và `RefreshAsync` xuyên suốt interface/service/repository.
- Thao tác `vfc-rebuild-run` xóa cache tài liệu, đọc lại `BusinessDocuments` cùng các nguồn phiếu/công nợ/tổng hợp đã có, dựng lại cây `This PC` và hiển thị số root/drive/document thực tế.
- Bổ sung unit test xác nhận service truyền lệnh refresh và cancellation token xuống repository; không tạo index count hoặc trạng thái hoàn thành giả.

### Cập nhật tiếp 2026-09-29 — cô lập DbContext cho Virtual File Center

- `VirtualExplorerRepository` không còn giữ và dùng chung `AppDbContext` của scope cho các truy vấn đọc VFC. Repository dùng `IDbContextFactory<AppDbContext>` scoped để tạo context riêng cho từng nhánh đọc (drive, network, supplier, customer, documents, connectivity).
- Vẫn giữ semaphore nội bộ cho navigation/cache, nhưng không còn phụ thuộc semaphore để bảo vệ khỏi các service khác trong shell dùng cùng `DbContext`. Điều này loại bỏ lỗi `A second operation was started on this context instance...` khi VFC mở đồng thời với Settings hoặc một luồng khởi tạo khác.

### Cập nhật tiếp 2026-09-29 — nối VFC với catalog persisted

- `VirtualDrives` đã có migration/seed thật cho các drive C–G và có unique index theo `Code`. `VirtualExplorerRepository.BuildDriveNodesAsync` hiện đọc các dòng `VirtualDrives` bằng `IDbContextFactory<AppDbContext>`, lọc `IsEnabled`, sắp xếp theo `SortOrder`, rồi mới áp dụng path/cờ bật tắt `VFC.*` từ Settings Engine.
- Metadata hiển thị drive (`Code`, `Name`, `DisplayName`, `Icon`, `SortOrder`) không còn lấy từ danh sách drive hard-code trong repository. Danh sách mapping còn lại chỉ dùng để liên kết drive persisted với setting path/module tương ứng.
- `VirtualFolders` được đọc theo `VirtualPath`; các node có `Id`, `FolderType`, `EntityType`, `EntityId`, thứ tự và trạng thái con lấy từ catalog persisted. Catalog hệ thống được bổ sung idempotent ở startup migration; nếu catalog chưa tồn tại thì repository fail-closed và không dựng taxonomy hard-code trong bộ nhớ.
- Đánh giá: phần drive và folder đều đi qua catalog persisted + Settings Engine + kiểm tra filesystem. Các folder động như Nhà cung cấp/Khách hàng vẫn phải được đánh giá theo nhánh truy vấn entity/document thật; không được báo cáo “index vật lý” chỉ từ số node materialize trong cây ảo.
- Xác nhận sau thay đổi: Release build 0 warning/0 error; toàn bộ unit suite 811 passed, 0 failed, 0 skipped; smoke process sống 8 giây sau khi khởi động bản Release.

### Cập nhật tiếp 2026-09-29 — registry máy trạm từ session thật

- `clients-pos`, `clients-fo`, `clients-wh` không còn hiển thị ghi chú “chưa có nguồn registry” như kết luận cuối cùng. `ClientRegistryService` đọc `ActiveCompanySessions` bằng `IDbContextFactory<AppDbContext>`, lọc `IsActive`/`IsArchived` và dùng query filter company/branch hiện tại.
- UI hiển thị machine, source, app version, status và `LastActivityAtUtc`; khi không có session active thì hiển thị trạng thái rỗng, không dựng máy trạm mẫu. Phần mapping thiết bị-nghiệp vụ và thao tác quản trị vẫn chỉ đọc vì chưa có contract persisted riêng.
- DI đã đăng ký `IClientRegistryService`; build Release 0 warning/0 error, unit suite 808 passed/0 failed/0 skipped và smoke launch sống 8 giây sau thay đổi.
- Factory được đăng ký tương thích với `DbContextOptions` scoped hiện tại và đã được DI validation kiểm tra qua unit suite. Release build 0 warning/0 error; unit suite 806 passed, 0 failed, 0 skipped; smoke launch sống 8 giây.

### Cập nhật tiếp 2026-09-29 — chẩn đoán path NAS/SMB bằng Windows provider

- `net-shared` tiếp tục đọc path từ Settings Engine, sau đó kiểm tra trực tiếp bằng `Directory.Exists`; path được phân loại là chưa cấu hình, không tuyệt đối, local/mapped, UNC/SMB không truy cập được hoặc accessible.
- Với local/mapped drive, UI cố gắng đọc dung lượng trống bằng `DriveInfo`; với UNC/SMB, UI chỉ báo khả năng truy cập của process hiện tại. Không đọc, lưu hoặc suy diễn credential mạng.
- Card NAS/SMB đã đổi từ placeholder “chưa có provider” sang chẩn đoán read-only có giới hạn rõ ràng: đây chưa phải quản lý share, credential, mount hoặc phân quyền mạng.
- Xác nhận sau thay đổi: Release build 0 warning/0 error; unit suite 808 passed, 0 failed, 0 skipped; smoke launch sống 8 giây sau khởi động. Cần UAT trên máy triển khai với local path, mapped drive và UNC/SMB thật để nghiệm thu quyền truy cập.

### Cập nhật tiếp 2026-09-29 — nối dữ liệu thật cho apps-default/apps-startup

- `apps-default` hiện đọc `AppModules` qua `IAppModuleService`, chỉ hiển thị module đã persisted, đang bật và `ShowOnDesktop`; không còn dùng danh sách app mặc định hard-code để giả trạng thái runtime.
- `apps-startup` dùng `IWindowsStartupAppsService` read-only, đọc các nguồn Windows Registry `HKCU/HKLM Run` và User/Common Startup folder. UI hiển thị tên, command, nguồn và trạng thái; không ghi registry, không tạo entry giả và không đọc credential.
- Provider có giới hạn 500 entry, loại trùng, xử lý quyền đọc/IO lỗi theo từng nguồn và báo rõ trạng thái provider. Trên môi trường không phải Windows, route báo provider không khả dụng thay vì trả danh sách mẫu.
- Xác nhận sau thay đổi: Release build 0 warning/0 error; unit suite 810 passed, 0 failed, 0 skipped; smoke launch sống 8 giây sau khởi động. Cần UAT trên máy triển khai để xác nhận danh sách startup thực tế và policy registry của doanh nghiệp.

### Cập nhật tiếp 2026-09-29 — persist update channel và rà lại đường dẫn ứng dụng

- Bổ sung definition `Maintenance.UpdateChannel` trong Settings Engine với giá trị mặc định `Stable`; route `maint-channel` giờ đi qua editor persisted thay vì thông báo thiếu setting.
- `maint-check` chỉ kiểm tra manifest local/UNC đã ký khi `Maintenance:UpdateManifestPath` và public key được cấu hình; thiếu cấu hình hoặc chữ ký không hợp lệ thì fail-closed, không quảng bá cập nhật.
- `apps-paths` hiện liệt kê Data root, Backup path và toàn bộ VFC path đã lưu, kèm kết quả kiểm tra filesystem; không còn chỉ hiển thị một Data root rồi suy diễn mapping export/template.
- Xác nhận sau thay đổi: Release build 0 warning/0 error; unit suite 810 passed, 0 failed, 0 skipped; smoke launch sống 8 giây sau khởi động.

### Cập nhật tiếp 2026-09-29 — bổ sung số liệu scheduler thật cho sec-sync

- Audit graph/source xác nhận các interface OTA `ISyncMonitor`/`IChannelDashboardService` hiện chưa có implementation/DI; không gắn nhãn “đã đồng bộ” từ các contract rỗng.
- `sec-sync` hiện đọc thêm `IBackgroundSchedulerService.GetDashboardAsync`, hiển thị running/queued/failed/next scheduled jobs từ execution store thật. Payload sync log vẫn được ghi rõ là chưa có repository riêng.
- Nếu execution store lỗi, UI hiển thị lỗi provider thay vì trả các counter mặc định. Xác nhận: Release build 0 warning/0 error; unit suite 810 passed, 0 failed, 0 skipped; smoke launch sống 8 giây.

### Ma trận phần còn thiếu sau audit hiện tại

- **Đã nối dữ liệu/provider thật:** database connection/schema/objects, VFC catalog và filesystem, Windows services/firewall/printers, app catalog/startup entries, client sessions, scheduler dashboard, persisted update channel, backup và security role/audit.
- **Chỉ đọc có giới hạn:** NAS/SMB path (chưa quản lý credential/share), export audit khi pipeline không đăng ký, sync payload history (OTA contracts chưa có implementation/DI).
- **Đã nối provider persisted:** các route generic `print-voucher`, `print-invoice`, `print-debt`, `print-hr` mở chung workspace `HotelFormPrinterProfile`; default printer/ACL vẫn đọc trực tiếp Windows Spooler.
- **Provider cần cấp dữ liệu triển khai:** license/entitlement và update manifest đã có provider đọc file manifest ký RSA; khi thiếu path/public key, sai chữ ký, hết hạn hoặc sai hash thì fail-closed. Việc phát hành license/package vẫn thuộc hệ thống ký/phát hành thương mại bên ngoài ứng dụng.
- **Gate hiện tại:** Release build 0 warning/0 error; unit suite 810 passed, 0 failed, 0 skipped; smoke launch sống 8 giây. UAT SQL Server/Windows vẫn là bước bắt buộc để xác nhận quyền và cấu hình triển khai thật.

### Cập nhật tiếp 2026-09-29 — cô lập Settings Engine khi VFC khởi tạo

- `VirtualExplorerRepository` không còn gọi `ISettingsService` dùng chung `AppDbContext` của shell để đọc cấu hình VFC/Network.
- VFC đọc `SettingDefinitions` và `SettingValues` qua `IDbContextFactory<AppDbContext>` riêng, hợp nhất với default definitions khi database chưa có bản ghi; giá trị encrypted bị loại khỏi đường đọc read-only.
- Thay đổi này xử lý đúng nguyên nhân còn lại của dialog `A second operation was started on this context instance...` khi VFC mở đồng thời với Settings page đang seed/load Settings Engine.
- Xác nhận sau thay đổi: Release build 0 warning/0 error; unit suite 810 passed, 0 failed, 0 skipped; smoke process sống 8 giây sau khởi động bản Release.

### Cập nhật tiếp 2026-09-29 — cô lập toàn bộ repository của Settings Engine

- `EfSettingsRepository`, `EfAppModuleRepository` và `EfSettingChangeLogRepository` được DI tạo bằng `IDbContextFactory<AppDbContext>`; mỗi thao tác đọc/ghi mở context riêng và dispose sau thao tác.
- Constructor nhận `AppDbContext` cũ vẫn được giữ cho các test/host legacy, nhưng production DI không còn dùng context scoped chung cho Settings Engine.
- Điều này loại bỏ một nguồn tranh chấp khác giữa Settings page, VFC, database health và các provider runtime chạy đồng thời; không thay đổi dữ liệu, quyền, mã hóa hoặc semantics persisted settings.
- Xác nhận sau thay đổi: Release build 0 warning/0 error; unit suite 810 passed, 0 failed, 0 skipped; smoke process sống 8 giây sau khởi động bản Release.

### Cập nhật tiếp 2026-09-29 — đồng nhất đường dẫn backup persisted

- `SqlServerBackupService` ưu tiên đọc `Backup.BackupPath` từ Settings Engine trước khi fallback về `Database:BackupDirectory` dành cho host legacy.
- `GetRecentAsync`, `BackupNowAsync`, `ValidateBackupAsync` và `RestoreAsync` dùng cùng một resolver/path-security boundary, nên UI và provider không còn chạy trên hai thư mục khác nhau.
- Không nới root restore: file vẫn phải là `.bak` và nằm dưới thư mục backup đã resolve; restore database vẫn bị khóa theo policy hiện hữu.
- Bổ sung test factory-backed cho Settings repository để kiểm tra đọc/ghi qua context cô lập.
- Xác nhận sau thay đổi: Release build 0 warning/0 error; targeted backup tests 2 passed; Settings repository tests 3 passed; unit suite 811 passed, 0 failed, 0 skipped.

### Cập nhật tiếp 2026-09-29 — cô lập scope khi mở VFC cửa sổ mới và sửa migration startup

- Tab VFC mở sang cửa sổ mới không còn resolve `VirtualExplorerViewModel` từ scope của shell hiện tại. Mỗi cửa sổ tạo `IServiceScope` riêng và gắn scope đó với lifecycle của `AppWindowViewModel`; khi đóng cửa sổ, scope được dispose cùng cửa sổ.
- Đây là lớp bảo vệ bổ sung cho `IDbContextFactory`: các service scoped như tài liệu, Settings và company context của hai cửa sổ không còn dùng chung `DbContext`, nên không tái phát lỗi `A second operation was started on this context instance...` khi mở tab mới đồng thời với cửa sổ cũ.
- E2E đã phát hiện thêm lỗi migration thật: `20260927075412_AddReservationHotelMasterLinks` alter `fo.DatPhong.MarketSegment` khi index legacy `IX_fo_DatPhong_CoSo_PhanKhuc` còn tồn tại. Migration đã được sửa để drop index trước `ALTER COLUMN`; phần rollback tạo lại index cũ sau khi đổi cột.
- Xác nhận: Release build 0 warning/0 error; unit suite 811/811 pass; smoke startup trên database đã migrate pass 1/1; shell surface pass 1/1 sau khi database smoke được khởi tạo sạch. E2E toàn bộ trên database cũ còn phụ thuộc mật khẩu admin đã tồn tại, nên không dùng kết quả đó để kết luận lỗi ứng dụng.

### Cập nhật tiếp 2026-09-29 — materialize catalog folder VFC vào database

- `VirtualDrives` đã là catalog persisted, nhưng `VirtualFolders` trước đây chưa được materialize khi database cũ không có dòng; repository vì vậy có thể rơi xuống taxonomy hard-code theo từng drive.
- Startup migration service hiện bổ sung idempotent catalog seeding cho 28 system folders C/D/E/F/G. Seeder chỉ thêm path hệ thống còn thiếu, giữ nguyên folder tùy biến của triển khai và dùng stable ID theo `VirtualPath`.
- Khi catalog folder không tồn tại (ví dụ host chạy `ValidateOnly`/`Disabled`), repository không còn dựng lại cây folder trong bộ nhớ; nó trả rỗng để fail-closed thay vì hiển thị đường dẫn ảo không thể audit hoặc cấu hình.
- Các folder động như Nhà cung cấp/Khách hàng vẫn lấy entity/document thật ở nhánh truy vấn chuyên biệt; catalog chỉ định nghĩa node gốc, không tạo dữ liệu nghiệp vụ mẫu.

### Cập nhật tiếp 2026-09-29 — signed provider cho license và update manifest

- `SignedLicenseService` đọc `Licensing:ManifestPath` và `Licensing:PublicKeyPem`, xác thực payload bằng RSA-SHA256, kiểm tra product, LicenseId, customer và thời hạn. Không có file/key hoặc chữ ký sai đều trả trạng thái không hợp lệ/không cấu hình, không tự sinh license.
- `SignedUpdateManifestService` đọc `Maintenance:UpdateManifestPath` và `Maintenance:UpdatePublicKeyPem`, kiểm tra chữ ký, product, channel, semantic version, URI gói và SHA-256 trước khi báo có bản cập nhật.
- Settings UI đã nối `about-license` và `maint-check` vào provider thật; manifest chỉ cung cấp metadata đã ký, ứng dụng chưa tự tải/cài package nên không tạo hành vi update ngoài quy trình phát hành được phê duyệt.
- Contract file dùng dạng `{ "payload": { ... }, "signature": "base64-RSA-SHA256" }`; chữ ký được tính trên JSON canonical của `payload` (object key sort ordinal). License payload gồm `product`, `licenseId`, `customerName`, `expiresAtUtc`, `features`; update payload gồm `product`, `channel`, `version`, `publishedAtUtc`, `packageUri`, `packageSha256`, `notes`.
- Bổ sung test ký/xác minh manifest, tamper detection và channel mismatch; cấu hình mẫu nằm trong `appsettings.example.json`.

### Cập nhật tiếp 2026-09-29 — khóa đọc EF trong từng cửa sổ VFC

- `VirtualExplorerViewModel` hiện tuần tự hóa các luồng đọc có thể phát sinh đồng thời trong cùng cửa sổ: khởi tạo, điều hướng, tìm kiếm, lazy-load cây thư mục và preview. Điều này bao phủ cả các service nghiệp vụ dùng `AppDbContext` scoped như business document, inventory và payable.
- Khi mở tab sang cửa sổ riêng, VFC tiếp tục tạo `IServiceScope` độc lập; repository VFC dùng `IDbContextFactory<AppDbContext>` cho các truy vấn catalog/document. Hai lớp bảo vệ này xử lý cả tranh chấp giữa các cửa sổ và tranh chấp giữa list/preview trong một cửa sổ.
- Xác nhận hiện tại: Release build 0 warning/0 error; unit suite `814 passed, 0 failed, 0 skipped`; smoke startup trước đó pass `1/1`. UI/UAT trên máy Windows triển khai thật vẫn cần mở đồng thời Settings và nhiều cửa sổ VFC để nghiệm thu thao tác người dùng.

### Cập nhật tiếp 2026-09-29 — E2E mở VFC sau khi cô lập DbContext

- Bổ sung smoke test `AlacarteApp_ShouldOpenVirtualFileCenterWithoutDbContextConcurrencyError`; test mở tile VFC bằng UI Automation, chờ dialog lỗi và kiểm tra process vẫn sống.
- Kết quả chạy đúng executable Release với database smoke riêng: `1 passed, 0 failed` trong 16,4 giây. Lần chạy mặc định trước đó bị loại khỏi đánh giá vì harness dùng database cũ làm đăng nhập thất bại, không đi tới VFC.
- Kết luận hiện tại: lỗi tranh chấp DbContext trong luồng khởi tạo VFC đã có bằng chứng runtime Release không tái hiện; lần đầu dùng database smoke mới chỉ vượt timeout 45 giây ở bước migration, lần chạy lại sau khi migration hoàn tất đã pass `1/1` trong 16 giây. Vẫn cần UAT trên database triển khai thật và thao tác mở song song Settings/VFC nhiều cửa sổ.

### Cập nhật tiếp 2026-09-30 — hoàn thiện provider backup file

- Route `backup-files` không còn là card khóa cứng. `FileBackupService` đọc các root đã lưu trong Settings Engine (`Storage.DataRootPath` và các `VFC.*DrivePath`), loại reparse point, tạo archive `asmile_files_backup_*.zip` và ghi manifest cho từng file.
- Manifest kiểm tra product, đường dẫn tương đối, kích thước và SHA-256. File backup ngoài thư mục backup, zip-slip, entry ngoài manifest, hash sai hoặc archive hỏng đều bị từ chối; danh sách lịch sử chỉ hiển thị archive hợp lệ.
- Restore file dùng quyền `Database.Restore`, yêu cầu `Security.RequireAdminConfirmation`, ghi audit `FileRestore`, ghi file tạm rồi move vào đích và không bao giờ xóa file. Mặc định service không overwrite; UI chỉ truyền overwrite sau xác nhận nguy hiểm của quản trị viên.
- UI đã có `Backup file ngay`, verify hash và danh sách restore theo từng archive. Không có root cấu hình hoặc root không tồn tại thì thao tác báo lỗi rõ ràng, không dựng dữ liệu giả.
- Bổ sung `FileBackupServiceTests`: round-trip tạo/verify/restore trên file thật và archive hỏng bị fail-closed. Xác nhận: Release build 0 warning/0 error; targeted file-backup tests 2/2 pass; toàn bộ unit suite 816/816 pass.
- Các phần còn cần contract bên ngoài ứng dụng vẫn giữ fail-closed: adapter/credential e-invoice thật (queue đã có nhưng provider mặc định là `Unavailable`), quản lý credential/share NAS, lịch sử payload OTA, phát hành license và phân phối package update. Đây không được đánh dấu đã hoàn tất chỉ vì UI có route.

### Cập nhật tiếp 2026-09-30 — nối policy backup database vào scheduler runtime

- `Backup.EnableAutoBackup` và `Backup.Schedule` trước đây chỉ được lưu trong Settings Engine; chưa có job runtime đọc hai giá trị này để tạo backup. Scheduler hiện seed job `DATABASE_BACKUP`, đọc policy persisted, parse cron năm trường và tính `NextRunAtUtc`.
- Khi policy tắt hoặc cron không hợp lệ, job bị disable và `NextRunAtUtc` được xóa; không tự chạy theo lịch mặc định khi operator đã tắt hoặc cấu hình sai. Khi job đến hạn, scheduler gọi `IBackupService.BackupNowAsync`, tạo file backup SQL thật và verify theo policy, rồi ghi execution log.
- `SqlServerBackupService` hiện cũng áp dụng đầy đủ `Backup.CompressBackup`, `Backup.VerifyAfterBackup` và `Backup.RetentionCount`: câu lệnh SQL chọn `COMPRESSION/NO_COMPRESSION`, verify theo policy và retention chỉ dọn các file backup do ứng dụng tạo trong đúng thư mục đã cấu hình.
- DTO dashboard đã trả thêm `IntervalMinutes`/`CronExpression` để UI hoặc health endpoint không phải suy diễn lịch từ mô tả.
- Bổ sung test policy bật với `*/15 * * * *`, policy tắt/cron sai và execution gọi provider thật; targeted scheduler tests `9/9` pass. Xác nhận lại: Release build `0 warning/0 error`; toàn bộ unit suite `819/819` pass; VFC E2E trên database smoke đã migrate `1/1` pass trong `15` giây.
- File backup vẫn là thao tác thủ công có verify/restore. Chưa tự động đưa file root lớn vào scheduler vì Settings Engine hiện chưa có policy retention/dung lượng/khung giờ riêng cho file backup; tự bật ngầm sẽ tạo rủi ro đầy ổ và không đạt yêu cầu vận hành thương mại.

### Cập nhật tiếp 2026-09-30 — nối policy security và khóa cấu hình chưa có runtime provider

- `AuthenticationService` đọc `Security.MaxFailedLoginAttempts` và `Security.SessionTimeoutMinutes` từ Settings Engine, có default/bounds an toàn. Số lần đăng nhập sai, `LoginSession.ExpiresAt` và `ActiveCompanySession.ExpiresAtUtc` dùng cùng policy persisted; không còn hard-code 5 lần hoặc 8 giờ trong luồng đăng nhập.
- Audit key-to-consumer cho thấy một số nhóm vẫn chỉ có definition + lưu DB nhưng chưa có consumer runtime: thông tin kết nối Database, endpoint Network/POS/FO, log policy, prefix phiếu nhập/xuất và các cờ/report policy General Ledger chưa được áp dụng vào nghiệp vụ thật. Appearance.Theme, WallpaperPath và ShowTaskbar đã được nối provider runtime ở cập nhật sau.
- Các nhóm còn thiếu provider nay được đánh dấu `IsReadOnly` ngay tại `SettingsRegistry`, UI generic vô hiệu hóa control và ghi rõ “chưa có provider runtime”. `SettingsService.SaveValueAsync` cũng chặn ghi ở backend; vì vậy gọi API/service trực tiếp không thể tạo cấu hình ảo. `ResetEditableAsync` bỏ qua các key chỉ đọc.
- Không khóa nhầm các key đã có consumer: policy backup, path VFC, security runtime, ngưỡng tồn kho, recipients cảnh báo và mapping tài khoản sổ cái vẫn editable; registry test kiểm tra cả hai nhóm.
- Bổ sung `SettingsCommercialConfigurationTests` và `SettingsReadOnlyEnforcementTests`, gồm kiểm tra UI model `CanEdit` và kiểm tra backend không tạo `SettingValue` khi save key chỉ đọc.
- Xác nhận sau thay đổi: Release build `0 warning/0 error`; toàn bộ unit suite `823/823` pass, `0` failed, `0` skipped. E2E VFC smoke trước đó vẫn pass `1/1` trên database smoke đã migrate.
- Các nhóm chỉ đọc là backlog tích hợp có chủ đích, không phải “đã hoàn thiện”: muốn mở lại phải triển khai provider thật, test runtime tương ứng và sau đó bỏ `IsReadOnly` cho từng key; không nên bật theo cấu hình UI trước khi có consumer.

### Cập nhật tiếp 2026-09-30 — hardening thao tác Settings và kiểm tra commercial package

- UI Settings không còn để lỗi provider/permission nổi thành unhandled exception khi save hoặc reset. `SettingsViewModel` ghi log key/operation, hiển thị thông báo và giữ nguyên màn hình; các command generic Toggle/Browse/Reveal cũng tự chặn trên item chỉ đọc.
- Đã publish lại Release vào thư mục kiểm tra tạm và chạy `scripts/verify_commercial_package.ps1`: `COMMERCIAL_PACKAGE=PASS`, `CHECK_COUNT=10`, `PDB_COUNT=0`, connection string được externalize, migration package giữ `ValidateOnly`/fail-closed và không phát hiện plaintext secret trong file cấu hình.
- Kết quả xác nhận sau hardening: unit suite `823/823` pass; Release build/publish `0 warning/0 error`; XAML encoding check pass.

### Cập nhật tiếp 2026-09-30 — rà soát đường dẫn và dữ liệu Virtual File Center

- Các drive C/D/E/F/G không lấy đường dẫn vật lý từ chuỗi hard-code của giao diện: `VirtualExplorerRepository` đọc catalog `VirtualDrives` đã lưu trong database, sau đó resolve `VFC.*DrivePath` và `VFC.*DriveEnabled` từ Settings Engine bằng `IDbContextFactory<AppDbContext>` riêng.
- Health của từng drive không phải số liệu mẫu: service gọi `Directory.Exists`, `DriveInfo` để tính dung lượng trống/đã dùng, và đếm document đã load từ các bảng nghiệp vụ theo `VirtualPath`. Đường dẫn chưa lưu được ghi rõ “Mặc định, chưa lưu trong Settings Engine”; đường dẫn đã lưu nhưng không tồn tại trả “Không khả dụng”, không tự coi là sẵn sàng.
- `VirtualFileCenterSeed` chỉ materialize 28 dòng catalog folder hệ thống vào `VirtualFolders` (Database Schemas, Tables, Hóa đơn, Audit Logs...). Seeder không tạo `BusinessDocument`, supplier, customer hay giao dịch mẫu. Các thư mục entity/document được dựng từ truy vấn database thật.
- Cần phân biệt “đường dẫn ảo” với “file vật lý”: `This PC\...` là định danh điều hướng của VFC; các URI `storage://`/`vfc://` trong DTO là logical reference cho tài liệu tổng hợp hoặc tài liệu chưa có file vật lý, không được coi là file đã tồn tại. Muốn preview/download file thật phải nối tiếp provider storage và chỉ trả thành công khi file tồn tại dưới root đã cấu hình.
- Các mục Network trong VFC hiện là health/diagnostic node: SQL dùng `CanConnectAsync`, endpoint/path được kiểm tra theo cấu hình. Chúng chưa phải trình duyệt SMB/NAS; không được quảng bá như share thật khi chưa có provider credential/share.
- Xác nhận runtime sau khi cô lập DbContext: startup smoke `1/1` pass; test mở tile VFC trên database smoke đã migrate `1/1` pass trong `14` giây, không xuất hiện dialog concurrency và process vẫn sống. Lần đầu trên database mới vượt timeout 45 giây ở bước migration, không phải lỗi VFC.

### Cập nhật tiếp 2026-09-30 — loại bỏ exception còn sót trong converter UI

- Rà soát production source phát hiện các `IValueConverter.ConvertBack` của Room Rack/FO và `BoolToVipConverter` còn ném `NotImplementedException`. Đây là lỗi tiềm ẩn: binding hiện tại phần lớn một chiều nhưng khi có binding hai chiều hoặc framework gọi ngược, exception có thể nổi lên thành lỗi UI.
- Các converter chỉ dùng để hiển thị nay trả `Binding.DoNothing` khi bị yêu cầu convert ngược; converter không tự ghi ngược giá trị hoặc tạo dữ liệu ngoài ý muốn. Sau thay đổi, production source còn `0` occurrence `NotImplementedException`.
- Bổ sung `ConverterSafetyTests` cho 25 converter hiển thị và converter VIP; targeted test `2/2` pass. Toàn bộ unit suite sau thay đổi `825/825` pass, XAML encoding check pass; E2E mở VFC `1/1` pass trong `16` giây.

### Cập nhật tiếp 2026-09-30 — kiểm tra lại consumer của các key chỉ đọc

- Audit literal trên production source, loại trừ registry, `SettingsViewModel` và test, xác nhận nhóm key chỉ đọc còn lại chưa có consumer runtime: Logs, nhiều policy General Ledger, các trường credential/database chưa được phép đổi trong app đang chạy, cùng Network discovery policy. Quy tắc mã phiếu hiện lấy từ `Module.receipts/issues.voucherCodePattern` của màn hình “Ứng dụng đã cài” và được renderer/sequencer dùng khi tạo phiếu; các key legacy `ImportVoucher.*`/`ExportVoucher.*` vẫn chỉ đọc để tránh tạo hai nguồn cấu hình. Nhóm Appearance không còn thuộc danh sách này sau khi có provider runtime.
- Đây là kết quả có chủ đích chứ không phải thiếu dấu vết: các key trên tiếp tục bị khóa ở backend và generic editor hiển thị “chưa có provider runtime”. Không mở lại chỉ vì Settings UI đã có option; mỗi key chỉ được mở sau khi có consumer nghiệp vụ và test runtime tương ứng.
- Các key đã có consumer được phân biệt riêng: VFC dùng `Database.ServerName/Database.DatabaseName`, health dùng các endpoint Network/POS/FO; backup, scheduler, security, VFC paths, warehouse alert, GL account mapping và update channel vẫn là nhóm writable đã có đường chạy thật.
- Audit này giúp loại bỏ nhầm lẫn giữa “đã lưu được vào Settings Engine” và “đã áp dụng vào nghiệp vụ”; kết quả hiện tại không phát hiện thêm cấu hình editable nào không có consumer, nhưng vẫn còn backlog provider có chủ đích ở trên.

### Cập nhật tiếp 2026-09-30 — hardening toàn bộ converter hiển thị

- Rà soát tiếp phát hiện các converter một chiều còn ném `NotSupportedException` khi bị gọi ngược: trạng thái, Quote tab, Stay nights, String equality, index, ngày tạo supplier và icon VFC.
- Các converter này nay trả `Binding.DoNothing` (với multi-value converter trả mảng sentinel), giữ đúng semantics chỉ hiển thị và không phát sinh exception nếu binding bị đổi mode.
- Không thay đổi các `NotSupportedException` còn lại trong domain/import: đó là lỗi nghiệp vụ có thông báo rõ cho holiday calendar hoặc định dạng file không hỗ trợ, không phải fallback UI/mock.
- `ConverterSafetyTests` hiện có 4 test pass; toàn bộ unit suite sau thay đổi `827/827` pass, E2E mở VFC `1/1` pass trong `16` giây và commercial package verifier `PASS` với `10` checks, `0` PDB.

### Cập nhật tiếp 2026-09-30 — rà soát placeholder/mock còn sót và xác nhận lỗi VFC

- Quét production source theo các nhãn `Coming soon`, `Chưa đăng ký`, `Mẫu dữ liệu` và `mock layout`. Không phát hiện route nào đang lặng lẽ đổ dữ liệu mẫu vào màn hình nghiệp vụ: các ViewModel lỗi provider đều fail-closed và hiển thị lý do; các cửa sổ phiếu thu/chi và Task & Shift có tham số mock chỉ để preview thiết kế, còn constructor không tham số và route production đều dùng service/VM dữ liệu thật.
- Nhóm `Coming soon` trong hub là các card chưa có workspace/adapter nghiệp vụ tương ứng. Đây là trạng thái công khai để chặn thao tác, không phải danh sách hay số liệu giả; không nên đổi thành “đang hoạt động” nếu chưa có query/command và test persistence.
- Các chuỗi `Chưa đăng ký` còn lại được phân loại là guard đúng: email outbox, report engine, Folio routing, command nghiệp vụ hoặc provider ngoài hệ thống. Không thay bằng mock; hướng hoàn thiện là đăng ký provider thật và giữ thông báo fail-closed cho tới khi có contract.
- Lỗi `A second operation was started on this context instance` của VFC đã được kiểm tra lại từ source và runtime: VM khóa tuần tự khởi tạo/điều hướng/tìm kiếm/lazy-load/preview; repository dùng `IDbContextFactory<AppDbContext>` và semaphore; cửa sổ mới có scope độc lập. E2E hiện tại trên database smoke đã migrate mở tile VFC `1/1` pass trong `16,7` giây, không xuất hiện error dialog và process vẫn sống.
- Sau khi rà lại đường chạy thực tế, đã build lại cả `bin\\Debug\\net8.0-windows\\ASMILE.BO.App.exe` và `bin\\Release\\net8.0-windows\\ASMILE.BO.App.exe`; Debug E2E chỉ rõ executable mới cũng pass `1/1`. Nếu máy người dùng còn thấy dialog cũ, cần đóng tiến trình đang chạy và khởi động đúng binary/build mới, không dùng bản copy trong `bin-commercial-*` cũ.
- Kiểm tra hồi quy toàn bộ `ASMILE.BO.Tests.E2E` trên database smoke đã migrate: `6/6` pass, `0` failed, `0` skipped trong `1m28s`; commercial package publish/verifier mới nhất: `COMMERCIAL_PACKAGE=PASS`, `CHECK_COUNT=10`, `PDB_COUNT=0`.
- Một lần smoke trên database hoàn toàn mới có thể dừng ở policy migration trước khi tạo MainWindow nếu host không cấp `Database:AllowApplyPendingMigrations=true`; đó là lỗi bootstrap môi trường, không phải lỗi VFC. Sau khi database được migrate/materialize company scope, toàn bộ E2E chạy lại đạt `6/6`.
- Nếu máy người dùng vẫn hiện ảnh lỗi cũ, executable đang chạy chưa phải bản Release vừa build (bản Release tại `ASMILE.BO.App/bin/Release/net8.0-windows/ASMILE.BO.App.exe` được build lúc `2026-09-30 07:40:31`). Cần đóng toàn bộ tiến trình cũ rồi chạy lại bản này; không dùng các thư mục `bin-commercial-*` cũ làm bằng chứng runtime.

### Cập nhật tiếp 2026-09-30 — nối provider runtime cho Appearance

- `Appearance.WallpaperPath` và `Appearance.ShowTaskbar` nay là cấu hình ghi được và được `DesktopShellViewModel` đọc trực tiếp từ Settings Engine khi khởi động, đồng thời phản ứng live với `SettingChanged`.
- Wallpaper chỉ được áp dụng khi đường dẫn tồn tại và load được bằng `BitmapImage`; file thiếu, không hợp lệ hoặc không truy cập được sẽ quay về nền mặc định và báo rõ cho người dùng, không giữ một đường dẫn ảo như thể đã áp dụng thành công.
- `Appearance.ShowTaskbar` điều khiển cả visibility và chiều cao hàng taskbar; khi tắt, hàng được thu về `0` và desktop icon được sắp xếp lại. Khi bật lại, taskbar trở về layout bình thường.
- `Appearance.Theme` nay hỗ trợ `Light`, `Dark` và `System` bằng cách cập nhật palette brush dùng chung trong shell khi khởi động hoặc nhận `SettingChanged`; `System` đọc policy `AppsUseLightTheme` của Windows. Các provider tích hợp ngoài ứng dụng (e-invoice, NAS/share credential, OTA/license) vẫn phải giữ fail-closed cho tới khi có contract, credential và test runtime tương ứng.
- Xác nhận sau thay đổi: Debug và Release build đều `0 warning/0 error`; toàn bộ unit suite `827/827` pass, `0` failed, `0` skipped; full E2E `6/6` pass trên database smoke đã migrate; commercial verifier `PASS`, `10` checks, `0` PDB.
- Kết luận: ba cấu hình Appearance (`Theme`, `WallpaperPath`, `ShowTaskbar`) đã có đường chạy thật trong shell; nhóm credential/kết nối và policy nghiệp vụ chưa có consumer vẫn bị khóa đúng chủ đích.

### Cập nhật tiếp 2026-09-30 — hoàn thiện pattern mã phiếu tồn kho và xác nhận artifact Release

- Mã phiếu nhập/xuất không mở lại các key legacy `ImportVoucher.*`/`ExportVoucher.*`; UI “Ứng dụng đã cài” vẫn là nguồn cấu hình duy nhất qua `Module.receipts.voucherCodePattern` và `Module.issues.voucherCodePattern`.
- `InventoryDocumentCodePattern` đã dùng pattern persisted để render mã, lọc đúng ngày chứng từ và tìm sequence kế tiếp từ các mã đã tồn tại. Pattern sai hoặc thiếu token sequence fail-closed về `PN-{yyyyMMdd}-{0000}` / `PX-{yyyyMMdd}-{0000}`.
- Bằng chứng kiểm thử: targeted Settings `12/12`, toàn bộ unit `829/829`, full E2E `6/6`, VFC Release smoke `1/1`; `git diff --check` pass.
- Artifact Release độc lập đã publish và vượt `verify_commercial_package.ps1`: `COMMERCIAL_PACKAGE=PASS`, `CHECK_COUNT=10`, `PDB_COUNT=0`. Lần smoke đầu trên database mới chạm timeout do migration; chạy lại sau khi schema đã materialize pass trong `15` giây.

### Cập nhật tiếp 2026-09-30 — probe thật cho Network locations của VFC

- Các endpoint đã lưu trong Settings Engine (`Network.ApiBaseUrl`, `POS.ApiEndpoint`, `FO.ApiEndpoint`) nay được kiểm tra bằng HTTP `HEAD` read-only với timeout 2 giây. HTTP 2xx/3xx trả `Đã kết nối`; HTTP lỗi, timeout, TLS/DNS hoặc connection failure trả trạng thái cảnh báo tương ứng.
- Endpoint mặc định chưa được lưu không bị gọi mạng và vẫn hiển thị `Mặc định, chưa lưu trong Settings Engine`; vì vậy UI không biến một URL mẫu thành kết nối thật.
- Probe chạy song song sau khi đọc settings bằng context riêng, không dùng lại `AppDbContext` của shell và không làm thay đổi dữ liệu endpoint.
- Xác nhận: build Debug `0 warning/0 error`, unit `829/829`, VFC E2E `1/1`, full E2E trên artifact Release `6/6`, commercial package verifier `PASS` với `10` checks và `0` PDB. Kết luận hiện tại: Network locations là health nodes có kiểm chứng, chưa phải SMB/NAS browser; credential/share provider vẫn cần tích hợp riêng trước khi quảng bá là thư mục mạng thật.

### Cập nhật tiếp 2026-09-30 — đồng bộ UI Sổ cái với provider thật

- Phát hiện `SettingsViewModel` trước đây vẫn cho sửa rồi gọi `SaveValuesAsync` với các key General Ledger đã được registry đánh dấu chỉ đọc; thao tác này dừng ở key đầu tiên và tạo lỗi runtime thay vì lưu thành công.
- Core settings, report settings và các cờ `AllowQuickPosting`/`AutoBalanceCheck` nay được hiển thị chỉ đọc, có thông báo rõ “chưa có provider runtime”; lệnh toggle không còn đổi trạng thái cục bộ rồi báo lưu thành công giả.
- Mapping tài khoản (`GeneralLedger.*Account`) vẫn giữ đường lưu thật qua Settings Engine và `EfGeneralLedgerService`. Kỳ kế toán không còn lưu vào snapshot Settings Engine; UI đọc/khởi tạo kỳ qua `AccountingPeriodService`, nên tạo kỳ, khóa/mở khóa và trạng thái hiển thị bám bảng `AccountingPeriods`/`PeriodLocks`.
- Xác nhận sau thay đổi: Debug build `0 warning/0 error`, toàn bộ unit suite `829/829` pass và VFC E2E `1/1` pass; chưa mở lại các policy GL chưa có consumer chỉ vì giao diện đã có control cũ.

### Cập nhật tiếp 2026-09-30 — khóa ghi giả cho kênh cập nhật

- Ghi chú cũ này đã bị lỗi thời bởi triển khai sau đó: `Maintenance.UpdateChannel` đang writable, có validator cho các channel hỗ trợ, và luồng `maint-check` đọc giá trị đã lưu để gọi `IUpdateManifestService`.
- Provider `SignedUpdateManifestService` đã đăng ký trong DI; provider xác minh chữ ký, product, channel, version, URI gói và SHA-256. Khi thiếu đường dẫn manifest hoặc public key ở cấu hình triển khai, trạng thái trả về là `NotConfigured` (fail-closed), không tuyên bố đã kiểm tra update thành công.

### Cập nhật 2026-10-01 — đối chiếu lại trạng thái và sửa thiếu sót VFC

- Đối chiếu source và test đã xác nhận `Maintenance.UpdateChannel` không phải setting read-only: `SettingsCommercialConfigurationTests` xác nhận definition writable và validator; `SettingsViewModel.BuildMaintenanceDetailAsync` đọc channel đã lưu; DI đăng ký `IUpdateManifestService` với `SignedUpdateManifestService`. Cần cấu hình manifest đã ký và khóa công khai khi triển khai để kiểm tra update hoạt động thực tế.
- VFC dùng ký tự Segoe UI cho bốn nút điều hướng thay vì phụ thuộc Segoe MDL2 Assets, để glyph không biến mất khi máy thiếu font icon.
- Khi catalog `VirtualFolders` có một phần, VFC ghép các thư mục nghiệp vụ mặc định còn thiếu với thư mục tùy chỉnh đã lưu. Danh sách khách hàng/chứng từ vẫn truy vấn từ các bảng nghiệp vụ, không tạo fixture giả.
- `AppModule.IsEnabled`, `ShowOnDesktop`, `PinToTaskbar`, `RequiredPermission` và danh sách pin rỗng có chủ ý đã được nối vào shell; test phân biệt “chưa có cấu hình pin” với “đã bỏ pin tất cả”.
- Khoảng trống cần xử lý tiếp: `AppModule.AllowClientAccess` hiện được lưu/hiển thị nhưng chưa có consumer runtime. Chưa triển khai enforcement vì chưa có định nghĩa runtime đáng tin cậy để phân biệt central server với client workstation; cần hoàn thiện workstation identity/mode trước khi áp chính sách để tránh khóa nhầm module.
- Xác nhận hiện tại: full unit suite `866/866` pass trên artifacts-path tạm; Release publish vượt commercial verifier (`10` checks, `0` PDB); VFC UIA smoke trên đúng executable Release pass `1/1`, xác nhận bốn glyph điều hướng, các ổ nghiệp vụ và năm thư mục kế toán (nhà cung cấp/khách hàng/hóa đơn/phiếu nhập/phiếu xuất) cùng hiển thị.
- Chưa thu được ảnh pixel runtime độc lập trong lượt này; UIA smoke xác nhận control và text glyph hiện hữu. `ASMILE.BO.App/bin/Debug/net8.0-windows/ASMILE.BO.App.dll` vẫn là bản cũ; artifact mới nằm trong `%TEMP%` để không ghi build lên ổ D: còn ít dung lượng.

### Cập nhật 2026-10-01 — tăng độ tin cậy icon VFC và kiểm thử truy cập

- Bốn nút điều hướng VFC chuyển từ glyph font sang `Path` vector để không phụ thuộc Segoe MDL2/Segoe UI; từng hình có tên và AutomationId riêng để hỗ trợ UI Automation/accessibility.
- Smoke test VFC đã cập nhật assertion: kiểm tra vector icon con cụ thể theo AutomationId, ngoài việc kiểm tra nút và danh sách ổ/thư mục kế toán.
- Xác nhận source hiện tại: App Release WPF/XAML build ở output tạm thành công `0 warning/0 error`; E2E test project build thành công `0 warning/0 error`; 6 unit test repository VFC pass khi chạy binary đã build.
- Không chạy lại UI smoke trong lượt này: app do người dùng mở vẫn chạy (PID `19620`) và test chủ động từ chối attach/điều khiển process đó. Vì vậy chưa xác minh pixel/runtime của vector icons hoặc nội dung database trên cửa sổ đang mở; cần một lượt smoke sau khi process đó được người dùng đóng.
- Xác nhận tiếp artifact sau cập nhật vector: `dotnet publish` Release độc lập tại `%TEMP%\VfcCommercialPublish` thành công; verifier đạt `COMMERCIAL_PACKAGE=PASS`, `CHECK_COUNT=10`, `PDB_COUNT=0`. `ASMILE.BO.App.dll` trong artifact được tạo lúc `2026-10-01 17:04:54` (SHA-256 `ED7C92A372C6CE4667BF2A38F64212181A634F411FE13EF92C86632B67BC14CB`); smoke UI vẫn chưa chạy vì guard thấy PID `19620`.

### Cập nhật tiếp 2026-10-01 — loại bỏ tuyên bố sai về server mode

- `system-mode` từng quảng bá “Central Server, maintenance mode” nhưng không có mode/provider runtime; route còn ánh xạ nhầm sang `Database.EnableAutoMigration` và `Database.CommandTimeout` dù hai key đó read-only.
- Đã bỏ mapping giả; detail nay nêu rõ vai trò server/workstation và maintenance mode “Chưa được triển khai”, không suy đoán theo tên máy/cấu hình cục bộ và không cung cấp nút sửa.
- Thêm test `SETTINGS_U_020` cho route không editable và cả hai trạng thái là non-clickable/chưa triển khai. Full unit suite hiện tại `872/872` pass (0 fail/skip).
- Release publish mới tại `%TEMP%\SystemModePublish` đạt verifier `COMMERCIAL_PACKAGE=PASS`, `CHECK_COUNT=10`, `PDB_COUNT=0`. UI runtime smoke cho VFC vẫn chưa chạy được do instance người dùng PID `19620` còn mở; chưa dùng trạng thái này để khẳng định hình ảnh sau thay đổi.

### Cập nhật tiếp 2026-09-30 — chặn dự phòng ghi vào setting chỉ đọc

- `SettingsViewModel.SaveEditableAsync` nay bỏ qua cả item được đánh dấu chỉ đọc ở UI và definition chỉ đọc từ Settings Engine. Như vậy một binding cũ hoặc trạng thái dirty sót lại cũng không thể đưa key chưa có provider vào `SaveValuesAsync`.

### Cập nhật tiếp 2026-09-30 — cô lập scope cho Group Checkout

- Rà DI xác nhận `IGroupBillingService`, `IGroupDepositService`, `IGroupServiceOrderQueryService`, `IGroupCheckOutCommandService` và `IHotelPaymentRepository` đều có provider EF thật; cảnh báo “chưa đăng ký” chỉ xuất hiện khi provider không resolve được.
- `GroupCheckoutWindow` trước đây resolve các service scoped từ `App.Services`, khiến các cửa sổ dùng chung graph/`AppDbContext` của application scope. Nay mỗi cửa sổ tạo `IServiceScope` riêng, dispose sau khi các thao tác đang chạy kết thúc.
- Các lần tải group, chi tiết billing/deposit/payment và thao tác xác nhận checkout dùng semaphore chung; không chạy đồng thời các EF query/command trên cùng scope. Dữ liệu fallback vẫn fail-closed, không thay bằng số liệu mẫu.
- Group Check-in cũng đã được rà cùng nguyên tắc: tạo scope riêng, tuần tự hóa load/command, và không thay số liệu thiếu bằng mock.
- `GroupDepositWindow` cũng đã được chuyển khỏi application-scope service graph sang scope riêng của cửa sổ; đọc, ghi nhận và hoàn tác tiền đặt cọc được tuần tự hóa để không dùng chung `AppDbContext` với shell/workflow khác.
- Xác nhận sau thay đổi: App Debug build `0 warning/0 error`; nhóm unit test Group `40/40` pass; full unit `829/829`; full E2E chạy lại trên database đã materialize `6/6` pass. Lần chạy đầu trên database mới có `5/6` do smoke shell timeout khi bootstrap; test shell chạy riêng sau đó `1/1` pass.

### Cập nhật tiếp 2026-09-30 — cô lập các cửa sổ nghiệp vụ còn dùng service scoped

- Rà soát các cửa sổ còn resolve service từ `App.Services` cho thấy `ExchangeRateWindow`, `IssueInvoiceWizard`, `NightAuditCenterWindow`, `AddStayOccupantWindow` và `CreateGroupWindow` đều gọi provider EF thật; chúng không phải mock, nhưng trước đây có nguy cơ giữ service/repository scoped quá lâu trên application scope.
- Các cửa sổ trên nay tạo `IServiceScope` riêng theo vòng đời cửa sổ và dispose sau khi thao tác đang chạy kết thúc. Các luồng đọc/ghi chính được tuần tự hóa bằng semaphore: reload/tạo-cập nhật tỷ giá, tải Folio/provider readiness/phát hành hóa đơn, kiểm tra/chạy/đóng Night Audit, tìm hồ sơ khách và tải/kiểm tra khả dụng/lưu đoàn.
- Không mở thêm fallback mẫu: thiếu context/provider tiếp tục hiển thị thông báo fail-closed. Riêng e-invoice vẫn chỉ lưu draft khi provider ngoài chưa sẵn sàng; không tự sinh trạng thái phát hành thật.
- Xác nhận hiện tại: App Debug build `0 warning/0 error`; targeted unit `110/110` pass; VFC E2E trên database smoke mới `1/1` pass trong `53` giây. Một lần chạy trên database smoke cũ dừng ở màn hình đăng nhập do mật khẩu test không khớp dữ liệu đã tồn tại, không phải lỗi VFC.
- Còn cần một lượt Release publish/verifier sau khi tiến trình app cũ đang khóa thư mục `bin/Release` được đóng; vì vậy chưa đánh dấu artifact Release mới là đã xác nhận thương mại trong lượt này.
- `ArPaymentWindow` cũng đã được chuyển sang resolve `ArPaymentViewModel` từ scope riêng của cửa sổ thay vì lấy trực tiếp từ application scope; các service Receivable/Debt/Banking/Reconciliation scoped vì vậy không còn bị giữ chung với shell. Việc dispose scope diễn ra khi cửa sổ đóng.

### Cập nhật tiếp 2026-09-30 — hoàn tất rà soát scope của các cửa sổ nghiệp vụ

- Rà soát toàn bộ `ASMILE.BO.App/Views/*.cs` có dấu vết resolve service từ `Application.Current` đã cho kết quả không còn cửa sổ nào vừa resolve service vừa thiếu `CreateScope()`. Đây là kiểm tra source-level cho lỗi dùng chung `DbContext`, không chỉ kiểm tra riêng VFC.
- Các cửa sổ còn lại đã được cô lập scope theo vòng đời cửa sổ, gồm Locked Room, Housekeeping Message, Marketing, Report Center, VIP Notes, Invoice Provider, Folio Business Date Backfill, các màn hình housekeeping/guest/room và các workflow group. `CheckInWindow` cũng đã bỏ việc `Dispose` trực tiếp service do DI quản lý; service được giữ trong scope của cửa sổ và scope được dispose khi đóng.
- Các thao tác async chính của các cửa sổ có nhiều luồng đọc/ghi được tuần tự hóa bằng semaphore trước khi dispose; mục tiêu là không còn hai EF operation chạy đồng thời trên cùng `DbContext` trong một workflow.
- Xác nhận sau batch thay đổi: App Debug build `0 warning/0 error`; full E2E trên database đã materialize `6/6` pass, và shell surface chạy riêng `1/1` pass. Không phát sinh dialog concurrency trong VFC.
- Artifact Release mới chưa được xác nhận lại vì tiến trình người dùng `ASMILE.BO.App.exe` (PID `26144`) đang khóa `ASMILE.BO.App/bin/Release/net8.0-windows`. Không tự ý dừng tiến trình; cần đóng phiên app cũ rồi chạy lại publish/verifier để có bằng chứng thương mại mới.

### Cập nhật tiếp 2026-09-30 — bổ sung nhóm cửa sổ bị bỏ sót trong audit scope

- Kiểm tra lại bằng mẫu rộng hơn, bao gồm cả kiểu ép `((App)Application.Current).Services`, phát hiện thêm 8 cửa sổ trước đó chưa nằm trong báo cáo: 6 cửa sổ AR, `InvoiceManagementWindow` và `WalkInWindow`.
- Các cửa sổ AR nay resolve ViewModel từ scope riêng và dispose scope khi đóng. `InvoiceManagementWindow` giữ scope cho `IEInvoiceService`/email service trong suốt phiên cửa sổ, chờ tác vụ tải hóa đơn kết thúc trước khi dispose. `WalkInWindow` cũng dùng scope riêng cho room query, reservation, stay và guest services thay vì giữ provider của shell.
- Kiểm tra lại toàn bộ `ASMILE.BO.App/Views/*.cs`: 99 file có tham chiếu `Application.Current`, nhưng `0` file vừa resolve service vừa thiếu `CreateScope()`.
- Xác nhận runtime sau batch: App Debug build `0 warning/0 error`; full E2E trên database đã materialize `6/6` pass trong `1m24s`.

### Cập nhật tiếp 2026-09-30 — xác nhận artifact Release hiện tại

- Không dừng tiến trình app cũ đang dùng thư mục `bin/Release`; thay vào đó publish Release hiện tại vào thư mục độc lập `C:\Users\webvi\AppData\Local\Temp\asmile-commercial-current16-publish` bằng `OutDir` tách biệt.
- `scripts/verify_commercial_package.ps1` chạy trên đúng thư mục publish mới: `COMMERCIAL_PACKAGE=PASS`, `CHECK_COUNT=10`, `PDB_COUNT=0`, hash executable `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Smoke VFC chạy bằng đúng executable Release vừa publish, trên database smoke đã materialize: `AlacarteApp_ShouldOpenVirtualFileCenterWithoutDbContextConcurrencyError` pass `1/1` trong `21` giây.
- Kết luận: lỗi concurrency của VFC đã được kiểm chứng trên artifact Release mới, không còn phụ thuộc vào binary cũ đang chạy. Các provider ngoài hệ thống (e-invoice, NAS/share credential, OTA/license) vẫn giữ fail-closed và chưa được quảng bá là đã tích hợp thật.

### Cập nhật tiếp 2026-09-30 — xác minh lại dấu vết Random/mock trong production

- Quét 25 file production có `Random`/`Random.Shared`, sau đó loại trừ đúng ngữ cảnh preprocessor `#if false` và block comment. 11 file UI/ViewModel chỉ còn fixture designer bị vô hiệu hóa; các fixture của Folio Workspace, Split Folio, Transfer Posting, Proforma và nhóm Housekeeping không nằm trong đường chạy production.
- 14 vị trí còn hoạt động đều là sinh mã định danh nghiệp vụ (JV/PB/KKQ/SKNH/DCNH/DEN/VIP/FB), không dựng danh sách, số liệu hay trạng thái màn hình. Các ViewModel khi thiếu provider đều trả trạng thái fail-closed và không nạp mẫu.
- Kết quả này bổ sung bằng chứng cho audit trước: không phát hiện đường chạy UI nào đang dùng dữ liệu ngẫu nhiên để làm giao diện trông như đã có dữ liệu thật.

### Cập nhật tiếp 2026-09-30 — sửa Group Service dùng ViewModel/root scope sai

- `GroupServiceWindow.xaml` trước đây khai báo trực tiếp `<vm:GroupServiceViewModel/>`; constructor rỗng của ViewModel lại lấy các service từ `Application.Current.Services`. Đây là đường khởi tạo bỏ qua scope DI của cửa sổ và có thể làm các truy vấn dịch vụ đoàn dùng chung `AppDbContext` với shell.
- Đã bỏ runtime DataContext khỏi XAML, chuyển `GroupServiceViewModel` sang constructor DI đầy đủ (`IGroupServiceOrderQueryService`, command service, execution context, folio repository, group query và email dispatch), đồng thời đăng ký ViewModel trong `AddPresentation`.
- `GroupServiceWindow` hiện tạo `IServiceScope` riêng, resolve ViewModel từ scope đó và dispose scope khi cửa sổ đóng. Các nơi mở bằng `new GroupServiceWindow()` vẫn an toàn vì scope được tạo trong constructor cửa sổ.
- Xác nhận: App Debug build `0 warning/0 error`, toàn bộ unit suite `829/829` pass. Smoke startup dùng executable Debug trên database smoke đã materialize pass `1/1` trong `12` giây; lần chạy database mới timeout ở bootstrap migration nên không dùng làm bằng chứng lỗi code.

### Cập nhật tiếp 2026-09-30 — xác nhận lại artifact Release sau sửa Group Service

- Publish Release độc lập tại `C:\Users\webvi\AppData\Local\Temp\asmile-commercial-current17-publish` đã vượt commercial verifier: `COMMERCIAL_PACKAGE=PASS`, `CHECK_COUNT=10`, `PDB_COUNT=0`.
- Smoke VFC chạy đúng executable Release vừa publish trên database smoke đã materialize: `AlacarteApp_ShouldOpenVirtualFileCenterWithoutDbContextConcurrencyError` pass `1/1` trong `17` giây.
- Helper FlaUI của bộ smoke được bổ sung xử lý phần tử UIA không hỗ trợ thuộc tính `Name`; lỗi này chỉ làm test harness fail khi dò dialog, không phải lỗi runtime của ứng dụng.

### Cập nhật tiếp 2026-09-30 — VFC không còn giả lập vị trí vật lý và kích thước

- `Open Location` trước đây chỉ điều hướng lại theo `VirtualPath`, nên giao diện tạo cảm giác đã mở vị trí thật dù chưa hề mở thư mục/file trên máy. Nay lệnh chỉ mở `PhysicalPath` đã được provider trả về, dùng Windows Explorer; đường dẫn `dbdoc://`, `storage://`, `vfc://`, đường dẫn rỗng hoặc không tồn tại đều bị chặn và thông báo rõ.
- Drive card truyền `PhysicalPath` từ node repository qua model UI; các document node truyền cả `PhysicalFilePath` và `FileSize` thực từ dữ liệu lưu. `Size` không còn dùng các hằng số giả 4 KB/32 KB/1 MB; chỉ dùng `FileSize` đã lưu hoặc đọc `FileInfo` khi file vật lý tồn tại, còn folder/đường dẫn không khả dụng được xem là 0.
- Các thao tác mở vị trí tiếp tục yêu cầu `OpenSensitiveFolder` và ghi audit log cho cả thành công, bị từ chối và đường dẫn không khả dụng. Không mở rộng quyền truy cập sang đường dẫn tùy ý.
- Xác nhận sau thay đổi: App Debug build `0 warning/0 error`; full unit `829/829` pass; commercial verifier trên `C:\Users\webvi\AppData\Local\Temp\asmile-commercial-current19-publish` đạt `COMMERCIAL_PACKAGE=PASS`, `CHECK_COUNT=10`, `PDB_COUNT=0`; smoke VFC bằng đúng executable Release vừa publish pass `1/1` trong `33` giây.
- Taxonomy ngày tháng cũng đã được làm fail-closed: khi nhà cung cấp chưa có chứng từ thật, VFC không tự chèn năm hiện tại và không hiển thị cây tháng rỗng như thể đã có thư mục dữ liệu. Năm/tháng chỉ xuất hiện từ `DocumentDate` của chứng từ đã đọc từ database.
- Xác nhận lại sau thay đổi taxonomy: unit `829/829` pass; publish Release current20 tại `C:\Users\webvi\AppData\Local\Temp\asmile-commercial-current20-publish` đạt commercial verifier `PASS` với `10` checks và `0` PDB; full E2E bằng đúng executable Release đạt `6/6` pass, `0` failed, `0` skipped trong `1m36s`.
- Tiếp tục hardening `Open Location`: node drive chỉ được mở root đang bật/lưu trong Settings; node tài liệu/thư mục chỉ được mở khi đường dẫn thực nằm dưới một drive đó. Resolver canonicalize full path, kiểm tra root containment và chặn reparse point bên trong root; absolute path do bản ghi tài liệu cung cấp không còn được tin độc lập.
- Xác nhận current22 sau hardening: App Debug build `0 warning/0 error`; unit `829/829`; commercial verifier `PASS` (`10` checks, `0` PDB); full E2E bằng executable Release current22 `6/6` pass. Lượt E2E đầu gặp lỗi riêng trong `DescribeUi` khi FlaUI đọc `Name` từ phần tử không hỗ trợ property; helper test đã bỏ qua node đó và lượt chạy lại đạt `6/6`.
- File đính kèm persisted nay được resolve bằng cùng Storage root/policy như `BusinessDocumentService`; file chỉ mở nếu tồn tại và nằm trong root `AppContext\Storage`. Các path drive/share cấu hình dạng tương đối báo `Không hợp lệ` thay vì bị kiểm tra tương đối theo working directory.
- Xác nhận current23 sau thay đổi: App Debug build `0 warning/0 error`; unit `829/829`; commercial verifier `PASS` (`10` checks, `0` PDB); full E2E bằng executable Release current23 `6/6` pass, `0` failed, `0` skipped.
- Tiếp tục loại bỏ node mạng máy in giả trong VFC: `VirtualExplorerRepository` nay dùng `IPrinterSettingsService` đã đăng ký trong DI để đọc danh sách/trạng thái thực từ Windows Spooler; hiển thị tối đa bốn printer cùng trạng thái và số lượng còn lại. Khi provider không có hoặc Windows trả danh sách rỗng, VFC báo rõ chưa phát hiện/không khả dụng và hướng kiểm tra Spooler/quyền tài khoản, không khẳng định printer đã được cấu hình.
- Xác nhận current24: App Debug build `0 warning/0 error`; unit `829/829`; commercial verifier trên `C:\Users\webvi\AppData\Local\Temp\asmile-commercial-current24-publish` đạt `COMMERCIAL_PACKAGE=PASS`, `CHECK_COUNT=10`, `PDB_COUNT=0`; full E2E chạy với đúng executable Release current24 đạt `6/6`, `0` failed, `0` skipped trong `1m33s`.
- Rà lại lỗi `A second operation was started on this context instance` theo ảnh: VFC repository hiện tạo DbContext riêng qua `IDbContextFactory<AppDbContext>` cho từng truy vấn; startup trong ViewModel giữ thứ tự gọi, repository cũng serialize thao tác materialize cây. Sửa comment cũ trong ViewModel vì nó mô tả sai rằng VFC còn dùng trực tiếp một scoped DbContext.
- Tăng thời gian quan sát hộp thoại lỗi trong `AlacarteApp_ShouldOpenVirtualFileCenterWithoutDbContextConcurrencyError` từ 4 lên 15 giây để không bỏ lọt dialog xuất hiện trễ. Trên đúng executable Release current24, smoke test này pass ba lượt liên tiếp (`1/1` mỗi lượt, `0` failed); artifact production không đổi ở vòng xác minh này.

### Cập nhật tiếp 2026-09-30 — nối mục Default apps tới Windows thật

- `apps-default` trước đây chỉ liệt kê module ASMILE đang bật, không phải ứng dụng mặc định theo loại tệp/giao thức của Windows. Màn hình nay phân biệt rõ “Module ASMILE khả dụng trên desktop” với “Liên kết ứng dụng mặc định của Windows”.
- Thêm `IWindowsSettingsLauncher`/`WindowsSettingsLauncher` và đăng ký DI; hàng “Mở Default apps của Windows” mở `ms-settings:defaultapps` qua ShellExecute. ASMILE không tự ghi registry association hoặc tuyên bố đã lưu thay đổi; lỗi mở được ghi log và thông báo hướng dẫn mở Settings thủ công.
- Microsoft Learn xác nhận URI `ms-settings:defaultapps` mở đúng trang Default apps của Windows ([tài liệu](https://learn.microsoft.com/en-us/windows/apps/develop/launch/launch-default-apps-settings)).
- Xác nhận current25: Debug build `0 warning/0 error`; full unit suite `829/829` pass; Release publish sang `C:\Users\webvi\AppData\Local\Temp\asmile-commercial-current25-publish` đạt commercial verifier `PASS` (`10` checks, `0` PDB); full E2E với đúng executable current25 đạt `6/6`, `0` failed, `0` skipped trong `1m49s`.

### Cập nhật tiếp 2026-09-30 — sửa độ tin cậy trạng thái Startup apps

- `WindowsStartupAppsService` trước đây gán `IsEnabled=true` cho mọi mục tìm thấy trong Run keys và Startup folders. Sự hiện diện của entry không chứng minh Windows cho phép nó chạy; status bật/tắt có thể do Windows quản lý riêng.
- Bỏ cờ `IsEnabled` không có bằng chứng. UI nay ghi “Entry hiện diện · nguồn” và nêu rõ trạng thái hiệu lực phải xem trong Windows; bổ sung hành động mở `ms-settings:startupapps`, không tự sửa Registry.
- Microsoft Learn liệt kê URI `ms-settings:startupapps` cho trang Startup apps ([tài liệu](https://learn.microsoft.com/en-us/windows/apps/develop/launch/launch-settings)).
- Xác nhận current26: provider unit `2/2`; full unit `829/829`; Release verifier `PASS` (`10` checks, `0` PDB); full E2E chạy executable current26 `6/6`, `0` failed, `0` skipped trong `1m53s`.

### Cập nhật tiếp 2026-10-01 — khóa cờ AllowClientAccess chưa có runtime identity

- Rà consumer-to-provider xác nhận `AppModule.AllowClientAccess` chỉ được seed/đọc/ghi trong Settings Engine và hiển thị ở chi tiết module; `DesktopShellViewModel` không áp cờ này khi dựng catalog hay mở module. Đăng nhập hiện chỉ ghi `Environment.MachineName`; không có role/workstation registration để xác định máy khách một cách đáng tin cậy.
- Để không tạo cảm giác cấu hình đã có hiệu lực, chi tiết module nay ghi `Chưa áp dụng` cùng nguyên nhân, không còn trình bày giá trị legacy dưới dạng Bật/Tắt. `AppModuleService.UpdateAppAsync` từ chối thay đổi cờ này và không ghi DB cho đến khi có runtime identity/provider; các cập nhật khác vẫn giữ giá trị đã lưu.
- Thêm test hồi quy `SETTINGS_U_018` (không cho ghi cờ khi thiếu workstation identity) và `SETTINGS_U_019` (UI không quảng bá cờ là Bật/Tắt). Cả hai pass; `git diff --check` sạch ngoài cảnh báo chuẩn hóa line ending.
- Phạm vi còn mở: xây dựng đăng ký workstation/server và chính sách truy cập có xác thực ở tầng máy khách/API trước khi bật lại thao tác này. Không dùng `MachineName` làm biện pháp kiểm soát truy cập.
- Xác nhận lại sau thay đổi: full unit `868/868` pass; Release package mới đạt verifier `COMMERCIAL_PACKAGE=PASS`, `10` checks, `0` PDB; full E2E chạy trên đúng executable Release đạt `6/6`, `0` failed, `0` skipped trong `1m54s`.

### Cập nhật tiếp 2026-10-01 — cô lập backup retention theo database

- Phát hiện backup SQL Server trước đây dùng chung mẫu `asmile_backup_*.bak`; retention của một database có thể xóa file backup cùng thư mục do database khác tạo, và danh sách backup có thể trộn lẫn.
- Backup mới nay mang prefix theo SHA-256 của tên database (không lưu plaintext tên DB trong filename). Danh sách chỉ ghép backup mới thuộc database hiện hành cùng file legacy chưa gắn chủ sở hữu; retention chỉ quản lý file mang prefix của database hiện hành. File legacy được giữ nguyên, không bị retention tự xóa.
- Validation/restore từ chối file backup mới đã gắn mã database khác và từ chối `.bak` không nằm trong namespace backup; file legacy `asmile_backup_*.bak` vẫn tương thích để kiểm tra/khôi phục có chủ ý.
- Thêm hồi quy cho tách danh sách, ngăn restore chéo database và retention không xóa backup database khác/legacy. Đồng thời làm resolver source path trong test `SETTINGS_U_019` tự tìm repo root để suite không phụ thuộc current working directory.
- Xác nhận: backup-focused unit `5/5`; full unit `871/871`, không fail/skip; Release publish trên source hiện tại đạt `COMMERCIAL_PACKAGE=PASS`, `10` checks, `0` PDB. EXE SHA-256 `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66` (bootstrapper không đổi; thay đổi dịch vụ nằm trong assemblies kèm theo).
- Giới hạn vận hành còn lại: backup legacy không chứa định danh database nên vẫn được giữ và hiện để tương thích; không thể chứng minh chủ sở hữu của các file này. Trước khi restore một file legacy từ thư mục chia sẻ, quản trị viên phải xác minh nguồn/header backup đúng database đích. Retention tự động không xóa legacy.

### Cập nhật tiếp 2026-10-01 — giấy phép test assertions và smoke VFC

- Xác minh với [Xceed FAQ](https://xceed.com/fluent-assertions-faq/) và [NuGet FluentAssertions 7.2.2](https://www.nuget.org/packages/FluentAssertions/7.2.2): FluentAssertions 8.x cần subscription cho commercial use; nhánh 7.x tiếp tục Apache-2.0, còn nhận bugfix, và gói 7.2.2 tương thích .NET 8.
- Ba test project Unit/E2E/Integration đã chuyển từ 8.10.0 xuống 7.2.2; production app không tham chiếu FluentAssertions. Unit chạy tại output path chuẩn: `871/871` pass, không còn cảnh báo subscription; E2E và Integration build `0 warning/0 error`.
- VFC runtime smoke đã đạt `1/1` trên ứng dụng Windows trước thay đổi test-only package: không thấy dialog DbContext, UI Automation thấy glyph toolbar/ổ đĩa và xác nhận các folder Khách hàng, Hóa đơn, Phiếu nhập, Phiếu xuất. Sau khi đổi package, E2E compile thành công nhưng chưa chạy lại vì đang có instance ASMILE BO do Visual Studio mở; không đóng/chiếm instance đó.
- Không dùng các thư mục test `bin-commercial*` cũ làm bằng chứng hiện hành: chúng còn `.deps.json` được tạo từ FluentAssertions 8.10.0. Source/project assets chuẩn đã là 7.2.2; cần tái tạo các artifact test thương mại đó trước khi dùng lại.

### Rà soát tiếp 2026-10-01 — phân biệt lịch sử export với nhật ký kiểm toán

- Đối chiếu lại kết luận “export audit khi pipeline không đăng ký” với source hiện tại: `NavigationService.OpenTargetAsync` tạo scope sống cùng cửa sổ module, `SettingsViewModel` được resolve từ scope đó, và `IExportPipelineService` được đăng ký scoped trong `AddInfrastructure`. Route `sec-export` vì vậy có thể đọc `ExportExecution` persisted qua `GetExecutionsAsync(50)`; nhánh “provider chưa đăng ký” chỉ là fallback khi host không cấu hình provider.
- Dữ liệu của route này là lịch sử lần chạy export, không phải `AuditEvent`/nhật ký kiểm toán và cũng không thực thi tạo file. Đã sửa nhãn/mô tả trong chi tiết để không quảng bá nhầm phạm vi.
- Phân biệt pipeline: Audit Center đã có export replay audit CSV/JSON/NDJSON/ZIP qua `IAuditReplayService.ExportAsync`, kèm lịch sử, SHA-256, coverage và integrity manifest. Route Settings `sec-export` khác: nó hiển thị lịch sử `ExportExecution` của export pipeline, không phải audit log và không tự tạo file.
- Destination Audit Center đã được chuyển sang Save As để người dùng chọn vị trí, có xác nhận ghi đè và xử lý lỗi ghi file fail-safe. Quyền `Logs.Export` đã tồn tại trong catalog và seed: Administrator/Accounting có quyền, Viewer/ITSupport không được cấp mặc định. Đã nối `CanExport` để khóa ba nút JSON/CSV/ZIP và kiểm tra lại tại `AuditReplayService.ExportAsync`; thiếu quyền bị từ chối trước khi tạo file hay ghi lịch sử. Không tạo permission trùng `audit.export`. Đồng thời sửa binding CSV từ tên command sai `ExportCSVCommand` sang command thực `ExportCsvCommand`.
- Phát hiện và sửa lệch phạm vi export: trước đây các bộ lọc người dùng/module/hành động/ngày trên Audit Center không được truyền đủ sang file; lọc theo correlation còn có thể kéo theo sự kiện khác trong cùng workflow. Request nay mang đủ bộ lọc, và service chỉ ghép replay row với đúng AuditLog đã lọc theo action, user, module/detail và thời gian. FilterJson của history cũng ghi lại toàn bộ bộ lọc được dùng.
- Thêm hồi quy chứng minh hai action cùng correlation chỉ xuất action phù hợp, không trộn correlation của user khác và không xuất replay orphan không có AuditLog scope. Test audit-focused `21/21`; full unit Debug hiện tại `876/876` pass, `0` failed/skip; E2E project build `0` warning/error.
- Release publish trên source hiện tại tại `C:\Users\webvi\AppData\Local\Temp\ASMILE-SettingsAudit-Final-2fae2d8815444532a090816de2195314` đạt commercial verifier `PASS` (`10` checks, `0` PDB); EXE SHA-256 `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66` (bootstrapper không đổi; thay đổi nằm trong assemblies kèm theo).
- Chưa chạy UI runtime smoke lần này vì tiến trình người dùng PID `19620` vẫn mở binary cũ từ `3:37 PM`; không dừng hoặc chiếm phiên đó. Bản Release mới được publish riêng, sau các thay đổi hiện tại.

### Cập nhật tiếp 2026-10-01 — tách quyền xuất Audit Center

- Dùng lại permission catalog hiện có `Logs.Export` (Administrator/Accounting được cấp mặc định; Viewer/ITSupport không được cấp), không thêm mã quyền trùng. Ba nút JSON/CSV/ZIP bị disable khi thiếu quyền; command còn kiểm tra trước Save As và `AuditReplayService.ExportAsync` từ chối fail-closed trước khi tạo file/lịch sử.
- Sửa binding CSV gọi nhầm `ExportCSVCommand`; command được sinh từ `ExportCsv` là `ExportCsvCommand`. Thêm hồi quy cho từ chối export khi thiếu permission, không tạo file/history, và test seed xác nhận quyền được persist đúng cho vai trò.
- Xác minh: AuditEnterprise + permission seed `27/27`; full unit suite `876/876`; E2E build `0` warning/error; `git diff --check` sạch; release candidate `C:\Users\webvi\AppData\Local\Temp\ASMILE-SystemSettings-Candidate-20261001\publish` đạt commercial verifier `10/10`, không PDB. UI runtime vẫn chờ instance cũ PID `19620` được đóng tự nhiên; không tự dừng ứng dụng người dùng.

### Cập nhật tiếp 2026-10-01 — VFC toolbar và danh mục dữ liệu

- Làm rõ truyền `Content`, `ContentTemplate` và `ContentStringFormat` trong template nút điều hướng VFC để nội dung vector bốn nút không rơi vào trạng thái nút rỗng; giữ nguyên tooltips và AutomationId của Back/Forward/Up/Refresh.
- Rà nguồn danh mục: taxonomy ổ đĩa lấy từ cấu hình `VirtualDrives`/settings; thư mục khách hàng, hóa đơn và phiếu nhập/xuất lấy từ `Customers`/`BusinessDocuments` đã lưu. Khi catalog thư mục chưa materialize nhưng ổ đĩa đã cấu hình, repository có fallback taxonomy; không thêm dữ liệu nghiệp vụ mẫu.
- VFC-focused unit hiện tại `14/14` pass, gồm khởi động/truy vấn đồng thời, khách hàng và chứng từ persisted; sau chỉnh sửa, full unit Debug `876/876` pass, `0` fail/skip. App Debug build `0 warning/0 error`; Release candidate riêng tại `C:\Users\webvi\AppData\Local\Temp\ASMILE-VFC-UIFix-20261001\publish` đạt commercial verifier `10/10`, `0` PDB.
- Chưa xác minh runtime UI của candidate sau thay đổi template: PID `19620` vẫn dùng binary Release cũ và đang mở; không đóng tiến trình của người dùng. Cần smoke trên candidate mới để xác nhận icon nhìn thấy và các thư mục xuất hiện trên DB/cấu hình của phiên triển khai.
- E2E VFC được thử sau khi một lần truy vấn process chưa thấy tiến trình, nhưng `ResolveExePath` kiểm tra lại ngay trước launch và từ chối vì phát hiện ASMILE.BO đang chạy; harness không khởi chạy/chiếm instance. Giữ nguyên kết quả là runtime candidate chưa xác minh.

### Cập nhật tiếp 2026-10-01 — license inventory cho gói thương mại

- NuGet vulnerability audit trên solution, gồm dependency bắc cầu của cả 8 project, không báo package có advisory theo `https://api.nuget.org/v3/index.json` tại lần kiểm tra này.
- Release `.deps.json` hiện có 68 NuGet runtime packages (61 license expression MIT, 6 Apache-2.0, 1 license file riêng của `Microsoft.Data.SqlClient.SNI.runtime`). Candidate trước thay đổi không có license/notice file nào được publish.
- Bổ sung `scripts/generate_third_party_notices.ps1`, được gọi sau `dotnet publish`; file sinh liệt kê đúng package/version từ `.deps.json`, metadata license/copyright/source, và chép các license/notice file có trong NuGet package (gồm nguyên văn SNI `LICENSE.txt`). 8 package không khai báo copyright trong NuGet metadata được đánh dấu cần xác minh attribution upstream, không tự bịa chủ sở hữu.
- `verify_commercial_package.ps1` nay yêu cầu inventory khớp đủ 68 runtime packages và kiểm tra SNI license text. Publish tự động trên `OutDir` cách ly đạt `COMMERCIAL_PACKAGE=PASS`, `CHECK_COUNT=14`, `PDB_COUNT=0`; notice có 68 package entries và 1 SNI terms section.
- Chưa coi đây là phê duyệt pháp lý hoàn chỉnh: notice chỉ thể hiện inventory/terms, không thay thế customer-facing agreement. Upstream SNI terms có yêu cầu điều kiện phân phối và thỏa thuận với downstream users; cần legal/business review. Các package chỉ khai SPDX expression mà không đóng gói license/NOTICE text vẫn được đánh dấu để review nguồn upstream.

### Cập nhật tiếp 2026-10-01 — attribution theo phiên bản và xác minh artifact

- Mở rộng generator để ghi cả `Authors` của NuGet và một mục attribution upstream riêng, nhưng không dùng Authors để suy ra copyright. Đã xác minh đúng tag/version cho 8 gói từng thiếu metadata copyright: ClosedXML.Parser 1.2.0, ClosedXML 0.104.2, ExcelNumberFormat 1.1.0, QRCoder 1.6.0, Serilog.Extensions.Hosting 8.0.0, Serilog.Extensions.Logging 8.0.0, Serilog.Settings.Configuration 8.0.0 và Serilog.Sinks.File 5.0.0. Mỗi dòng notice giữ nguyên attribution/header quan sát được và trỏ tới source version-pinned.
- Bổ sung nguyên văn license upstream được pin version cho 4 gói MIT nói trên; generator gắn package identity và nguồn vào notice. Verifier kiểm tra đủ nội dung từng tệp, đủ marker, attribution, 2 license SPDX, SNI terms, runtime package inventory, cấu hình release và secret scan.
- `verify_commercial_package.ps1` kiểm tra mỗi attribution áp dụng, số lượng marker tổng hợp và UTF-8; sửa lỗi verifier trên Windows PowerShell 5.1 khi đọc tên có dấu (đọc ANSI làm sai chuỗi Unicode) và khi regex đếm marker CRLF.
- Publish Release độc lập tại `%TEMP%\ASMILE-Commercial-Notices-20261001-5` thành công; generator báo 68 runtime packages, 3 package license documents, 2 SPDX license texts, 8 upstream attributions và 4 version-pinned license files. Verifier đạt `COMMERCIAL_PACKAGE=PASS`, `CHECK_COUNT=36`, `PDB_COUNT=0`; EXE SHA-256 `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Trạng thái thương mại vẫn **chưa được legal sign-off**: cần rà lại điều khoản phân phối Microsoft.Data.SqlClient.SNI.runtime với luật sư/đơn vị phát hành; hoàn thành review 68 license/NOTICE upstream (generator chỉ gom package files, SPDX license text và các attribution đã curate nêu trên); đồng thời hoàn thiện EULA, privacy/data-processing terms, support/update policy, signing, installer/upgrade/rollback, migration/backup recovery và kiểm thử trên môi trường khách hàng. `SixLabors.Fonts/1.0.0` là transitive từ `ClosedXML/0.104.2` trong artifact hiện tại và không có package reference trực tiếp trong các project `.csproj` đã kiểm tra; trang licensing hiện hành của Six Labors quy định trigger cho dependency trực tiếp trong phần mềm closed-source vì lợi nhuận khi doanh thu năm vượt USD 1M, nhưng cần xác nhận mô hình phát hành/điều khoản cụ thể trước khi chốt. Verifier là package hygiene gate, không phải kết luận pháp lý hay bằng chứng sẵn sàng phát hành.

### Cập nhật 2026-10-01 — VFC concurrent DbContext và phiên bản đang chạy

- Mã VFC hiện tuần tự hóa khởi tạo/điều hướng/tìm kiếm/preview bằng operation gate; repository VFC cũng tuần tự hóa thao tác và tạo `AppDbContext` mới qua factory. Unit tests VFC chạy lại: `14/14` pass; test suite E2E có assertions cho 4 glyph automation IDs và nội dung This PC, nhưng chưa chạy lại trên máy trong phiên này.
- Phiên đang mở PID `19620` được khởi chạy 15:37 và nạp `ASMILE.BO.App.dll` có SHA-256 `487249AF8AEBEA3E031757B1F5EB0E2DE1871968F586D815DB0DECACBBDEE19F`. Artifact Release vừa build/publish lúc 19:14 có DLL hash `6985A9378D02E611081BF3F57631C0DA41219E5E51B895D539F2332D8B797C24`. Đây là hai binary khác nhau; không thay thế hay đóng tiến trình đang mở. Do đó chưa xác nhận bằng runtime screenshot rằng glyph và dữ liệu đã hiện trong cửa sổ người dùng. Cần đóng phiên cũ một cách bình thường rồi mở artifact mới để hoàn tất smoke UI/E2E.

### Cập nhật tiếp 2026-10-01 — sửa glyph VFC và xác minh nội dung thật

- Thay icon `Path` của bốn nút điều hướng bằng glyph Segoe MDL2 có `AutomationId`/accessible name riêng. UI E2E trước sửa tái hiện nút `VfcBackButton` không có child icon; sau sửa, smoke test chạy trên Release publish độc lập đã pass assertions cho cả bốn glyph, bốn ổ nghiệp vụ và các thư mục Khách hàng, Hóa đơn, Phiếu nhập, Phiếu xuất. Database smoke là LocalDB riêng của E2E, không phải database người dùng.
- Tập trung kiểm thử repository/service VFC pass `14/14`; assertions bao gồm đọc khách hàng từ bảng Customer và chỉ liệt kê chứng từ đã lưu ở các thư mục kế toán. E2E startup timeout tăng 45s lên 180s vì database E2E sạch phải chạy toàn bộ migration chain.
- Phiên người dùng PID `19620` vẫn chạy từ `D:\ASMILE-BO\ASMILE.BO.App\bin\Release\net8.0-windows\ASMILE.BO.App.exe`; không ghi đè binary bị khóa. Bản Release mới được publish ở `%TEMP%\ASMILE-VFC-Fix-20261001\publish` để kiểm thử tách biệt. Cần khởi động lại phiên ứng dụng từ build mới để cập nhật cửa sổ người dùng.
- Kết quả lịch sử tại thời điểm ghi nhận: full integration suite `36/38`, với lỗi training-data và LocalDB backup/`WITH COMPRESSION`. Phần backup đã được chạy xác minh lại ngày 2026-10-07 và pass; xem mục cập nhật bên dưới. Training-data cần chạy lại riêng trên source hiện tại trước khi kết luận còn là blocker.

### Cập nhật tiếp 2026-10-07 — hardening vận hành trong Cài đặt hệ thống

- `Máy trạm & dịch vụ`: phiên đang hoạt động lấy từ `ActiveCompanySessions`; bổ sung thu hồi theo `SessionId` persisted, yêu cầu `ManageUsers`, không cho tự thu hồi phiên hiện tại, cập nhật `LoginSession` và mọi presence liên quan, đồng thời thêm `AuditLog` trong cùng lần `SaveChanges`. Tên máy/version vẫn chỉ là metadata do client khai báo, không phải trusted-device identity.
- `Cập nhật & bảo trì`: `SignedUpdateManifestService` xác minh manifest và báo version/hash/URI; `UpdatePackageStagingService` kiểm tra lại chữ ký, yêu cầu quyền `ManageServices`, giới hạn tải 1 GiB, chỉ cho phép redirect HTTPS, kiểm tra SHA-256 rồi mới đổi tên file `.part` thành gói staging. URI từ manifest và từng redirect đều từ chối user-info/fragment/HTTP.
- `maint-check` có thao tác tải/xác minh nếu có bản mới và quyền phù hợp; giao diện ghi rõ gói staging chưa được giải nén/cài đặt. `appsettings.example.json` để trống `Maintenance:UpdateManifestPath` và `Maintenance:UpdatePublicKeyPem`; bản triển khai phải provision manifest/key công khai riêng. `Maintenance:UpdateStagingPath` tùy chọn, mặc định lưu trong LocalAppData.
- Cập nhật thương mại vẫn thiếu supply-chain hoàn chỉnh: artifact/package format và signing/release pipeline, updater helper xử lý file đang chạy/elevation, rollback, migration compatibility và E2E upgrade trên bản cài sạch/đang dùng. Chưa tự triển khai cài đặt tại chỗ vì hiện chưa có updater/helper hoặc quy ước package được ký để làm nền an toàn.

### Kiểm chứng Release 2026-10-07

- Tạo publish mới từ source hiện tại với `Release`, `win-x64`, self-contained tại `%TEMP%\ASMILE-Commercial-SystemSettings-a22e7c0a01244d03902390a7ad4bb887`; `scripts/verify_commercial_package.ps1` trả `COMMERCIAL_PACKAGE=PASS`, `CHECK_COUNT=35`, `PDB_COUNT=0`.
- `EXE_SHA256=479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66` là hash của `ASMILE.BO.App.exe`, không phải hash toàn bộ publish tree. Kết quả chứng minh hygiene gate hiện tại pass; không chứng minh binary/package được code-sign, có installer, hay đã chạy smoke trên máy khách.
- `verify_commercial_package.ps1` hiện in thêm inventory digest cho toàn bộ cây publish: sắp thứ tự đường dẫn tương đối, rồi băm từng file theo SHA-256 và băm nội dung inventory (path, size, hash). Candidate có 1.856 file; `PUBLISH_TREE_SHA256=C4BFDB6C2DFA16FC6DD3946375DAE86DFC1FEC3F9957B7F56A9B0A341C62481F`; chạy verifier hai lần cho cùng kết quả. Digest này nhận diện nội dung artifact nhưng chưa được ký/đối chiếu bằng kênh phát hành tin cậy; nó không bao gồm ACL hay timestamp và không thay thế code signing.
- Trong repo không tìm thấy installer project/script MSI/MSIX/WiX/Inno Setup; app publish đang là thư mục flat. Do đó auto-install/rollback chưa thể nối an toàn nếu chưa chốt contract package và signing/release pipeline.
- Test tích hợp luồng update staging với RSA manifest thật pass `7/7`; full unit suite sau thay đổi pass `975/975`, không fail/skip. Chưa có live update-feed hoặc customer-machine upgrade test.

### Cập nhật 2026-10-07 — tách dữ liệu ghi khỏi thư mục cài đặt

- `ApplicationDataPaths` thống nhất vị trí user settings, logs và exports bên ngoài thư mục binary; đặt containment cho các path export. ModuleSettings vẫn đọc bản JSON legacy trong thư mục app cũ nếu file mới chưa có; Breakfast Café đọc legacy nhưng lần lưu tiếp theo ghi sang vị trí mới.
- Serilog và UI exception logs dùng `%LOCALAPPDATA%\ASMILE-BO\Logs`; exports CSV/PDF dùng Documents\ASMILE-BO\Exports; export kho tách thư mục con. File/backup roots tiếp tục theo `Storage.DataRootPath` hoặc `Backup.BackupPath`; đường dẫn backup tương đối còn sót được neo dưới ProgramData data root, không còn dưới thư mục cài đặt.
- VFC lấy đường dẫn tài liệu vật lý qua `BusinessDocumentService.ResolvePhysicalPathAsync`, nơi đã áp dụng `Storage.DataRootPath` và tương thích đọc legacy `AppContext\Storage`; không còn tự suy ra root ổ đĩa vật lý từ thư mục app.
- Release không còn publish appsettings cục bộ: `appsettings.json` trong publish được lấy từ `appsettings.example.json`; kiểm tra candidate xác nhận connection string trống, file example riêng không xuất hiện. Điều này ngăn cấu hình developer/credential vô tình đi vào package.
- Full unit suite sau thay đổi: `978/978` pass, 0 skip. Release candidate mới tại `%TEMP%\ASMILE-Commercial-UserData-Final-55f2b17bcfcc41deb482b270372baf16` đạt `COMMERCIAL_PACKAGE=PASS`, `CHECK_COUNT=35`, `PDB_COUNT=0`, `PUBLISH_FILE_COUNT=1855`, `PUBLISH_TREE_SHA256=108090EDDBDA6D5AEBBF0490EE9B0681346EF6A0C0907E11D7AB996E1093D106`.
- Vẫn chưa có installer, code signing certificate/pipeline, automated upgrade/rollback hoặc customer-machine smoke. Các path user-local/shared đều cần được kiểm tra trên install clean và service account thật trước phát hành; unit/publish gates chưa chứng minh quyền ACL hoặc luồng nâng cấp thực tế.

### Cập nhật 2026-10-07 — installer đầu tiên và adapter settings legacy

- Thêm nguồn installer NSIS 3.13, pinned build script, kiểm tra version của app so với installer, sinh inventory đúng theo publish và gỡ file theo inventory (không xóa đệ quy thư mục cài); bổ sung notice license NSIS cạnh setup. Gói thử nghiệm tại `%TEMP%\ASMILE-Commercial-Installer-58075df5d479453282a1f76697d5613f` biên dịch thành công từ publish đã qua 35/35 checks; payload archive đối chiếu đúng 1.855/1.855 file. Setup SHA-256 `AA5CDFCB9ED2011448421E29EF10D0641421541D21C7D88C3A7619F123038172`.
- Gói trên chưa ký số. Không chạy install/uninstall vì token hiện tại không elevated; chưa có bằng chứng ACL, UAC, upgrade, rollback, giữ dữ liệu hoặc uninstall trên máy sạch. Chế độ production signing có trong script nhưng chưa kiểm tra với chứng thư doanh nghiệp.
- `EfSystemSettingsService` đọc bảng legacy `AppSettings` chỉ có `Id/Key/Value`, nhưng trước đây tự gán `UpdatedAt=UnixEpoch`, `UpdatedBy=AppSettings`, `IsEncrypted=false` và trả thẳng mọi value. Adapter nay trả metadata không có trong schema là `null` và mask key có password/secret/token/API key/connection string/credential; test xác nhận dữ liệu DB gốc không bị đổi. Field service trong `SettingsViewModel` hiện chỉ được inject/gán, không có consumer runtime.
- Kiểm chứng sau hardening: adapter `3/3`; Settings commercial configuration + read-only enforcement cùng adapter `71/71`; verifier của publish vẫn `35/35`. Cần tiếp tục rà các mục Settings chỉ đọc/không có provider, rồi chạy full unit/E2E trên Release mới.

### Xác minh bổ sung 2026-10-07 — installer khớp Release sau hardening

- Release publish mới tại `%TEMP%\ASMILE-Commercial-SystemSettings-d90c1faec0664a5e9537d2ac92526841`: commercial verifier `35/35`, `PDB_COUNT=0`, `PUBLISH_FILE_COUNT=1855`, tree SHA-256 `1687B12342ADA2D2C713FFFA1B1B5555957C5673081F30B0F24674693F6944C9`.
- Setup được build lại từ publish này bằng NSIS `v3.13`, tại `%TEMP%\ASMILE-Commercial-Installer-Release-d90c1faec0664a5e9537d2ac92526841\ASMILE-BO-Setup-1.0.0.0.exe`; SHA-256 `51C8685186E7117B795FE1426330E16CE014730C6678D916565278F003FBC20D`; metadata FileVersion `1.0.0.0`, Company/Product `ASMILE BO`; third-party notice kèm theo. NSIS Chocolatey package SHA-256 `CEFD57D4EBBC56D3AB104034044F54D61918642D51F6FCC44FF8D6C960533F91`; embedded official NSIS installer SHA-256 khớp verification manifest: `DB9A1038BBC0E53FEF24F423FD609979BD578E942F1BE1198DB186D1C42652A3`; installer source SHA-256 `7B34BBDBB957E0BF1D52DC2C9C28FE0D1786E2EEE9E61B3DD233618AEEF1245A`.
- Bung payload bằng 7-Zip, so inventory với publish: `1855/1855`, không thiếu/không dư payload. `uninstall.exe` là tệp do NSIS sinh và được loại riêng khỏi phép so sánh payload; `$PLUGINSDIR` chỉ chứa tài nguyên cài đặt.
- Đây vẫn là artifact kiểm thử, `NotSigned`; không chạy cài/gỡ trên máy hiện tại do không có quyền elevated. Không coi archive verification là bằng chứng cho UAC, ACL, upgrade/downgrade, rollback hoặc giữ dữ liệu. Cần certificate chính thức, EULA/release policy và clean-machine UAT trước thương mại.

### Xác minh integration bổ sung 2026-10-07 — sao lưu database

- Chạy lại `SqlServerBackupIntegrationTests.LocalDb_backup_provider_creates_and_validates_a_real_backup` trên source hiện tại: `1/1` pass, thời gian test `47s`. Luồng đã tạo backup SQL Server có dữ liệu thật trong database test LocalDB, kiểm tra file tồn tại/kích thước, chạy `RESTORE VERIFYONLY`, rồi đọc lại danh sách backup.
- Nhánh trong `SqlServerBackupService` phát hiện engine Express và tắt compression trước lệnh backup; có fallback hẹp cho SQL error `1844`. Kết quả hiện tại không tái hiện blocker LocalDB ghi trong log integration cũ; không sửa test để né lỗi.
- Chưa xác nhận khả năng ghi backup dưới Windows service identity/ACL của máy triển khai: integration riêng `SqlExpress_service_identity_can_write_and_validate_a_real_backup_when_opted_in` chỉ chạy khi có SQL Express và được bật rõ bằng `ASMILE_SQLSERVER_SERVICE_BACKUP_TEST=1`.
- Chạy lại toàn bộ `ASMILE.BO.Tests.Integration` trên cùng source sau đó: `39/39` pass, `0` fail, `0` skip, tổng thời gian `9m39s`. Cả `TrainingDataE2ETests.ResetFoundationFullTraining_ProvidesDemoReadyDataAndReports` và integration backup đều pass; kết quả `36/38` ở log cũ không còn là trạng thái hiện tại.

### Kiểm chứng E2E giao diện Cài đặt hệ thống 2026-10-07

- Thêm UI smoke `AlacarteApp_ShouldOpenSystemSettingsHomeWithLivePanels`: mở route `settings` bằng global search, xác nhận `Recommended settings`, `System health`, hành động `Cơ sở dữ liệu`, không có error dialog và app vẫn sống.
- E2E đăng nhập qua quy trình bảo mật thật: trên database mới dùng seed credential, hoàn tất dialog đổi mật khẩu bắt buộc, các test tiếp theo đăng nhập bằng mật khẩu đã lưu. Gắn AutomationId cho password inputs/actions để UIA chọn đúng control; DB mặc định test được cô lập theo ngày và có thể override tường minh.
- App Debug build: `0 warning`, `0 error`, XAML encoding check pass. System Settings smoke: `1/1` pass; toàn bộ E2E suite: `7/7` pass, `0` fail/skip (`2m23s`) trên LocalDB test cô lập `ASMILE_BO_E2E_SMOKE_20261007_SETTINGS3`.
- E2E này chứng minh luồng Home/render/đăng nhập của app trên LocalDB test, không chứng minh cấu hình máy trạm production, Windows service ACL, database khách hàng hoặc chứng thư ký số.
- Sau thay đổi UIA, chạy lại full unit `979/979` pass, `0` skip. Release publish mới tại `%TEMP%\ASMILE-Commercial-SystemSettings-E2E-0e59b755568c49a39b97dc71d41e016e` đạt verifier `35/35`, `PDB_COUNT=0`, `PUBLISH_FILE_COUNT=1855`, tree SHA-256 `8C372D8CEA4C79FEA1CBB78BB606E0F3328A04E1F0DDA3EE9EC8AC5100FBC9C5`.
- Setup được dựng lại bằng NSIS `v3.13` tại `%TEMP%\ASMILE-Commercial-Installer-E2E-0e59b755568c49a39b97dc71d41e016e\ASMILE-BO-Setup-1.0.0.0.exe`; SHA-256 `1DDF7DD4EEAEE59911871EA7EF5F56D85A168F04316F49F2577205C2989B32C9`, FileVersion `1.0.0.0`, Company `ASMILE BO`, notice kèm theo. Bung archive và so với publish: `1855/1855`, không thiếu/thừa; `uninstall.exe` là file NSIS sinh. Signature `NotSigned`; đây chưa phải setup phân phối khách hàng.
- Chạy UI smoke riêng trên chính `ASMILE.BO.App.exe` trong Release publish này (không phải Debug): System Settings Home `1/1` pass trên LocalDB E2E trong `19.9s`. Máy hiện không có chứng thư Code Signing hợp lệ có private key trong CurrentUser/LocalMachine và token không elevated; không ký hoặc cài lên Windows thật.

### Hardening pipeline xác minh bộ cài 2026-10-07

- Bổ sung `scripts/verify_commercial_installer.ps1` và nối vào installer builder sau khi tạo sidecar notices. Script bung NSIS archive vào một thư mục GUID riêng trong `%TEMP%`, đối chiếu inventory đường dẫn, kích thước và SHA-256 của từng payload với đúng publish đã verifier; loại trừ riêng `$PLUGINSDIR` và `Uninstall.exe` do NSIS sinh.
- Verifier còn kiểm tra FileVersion của executable và setup, chữ ký Authenticode của setup/app khi bật `-RequireSigned`, và hash của third-party notice cạnh setup. Builder hỗ trợ `-SevenZipPath`, tự tìm 7-Zip trên PATH hoặc vị trí cài mặc định, và xóa artifact mới nếu bước kiểm chứng thất bại.
- Chạy trên setup hiện tại `ASMILE-BO-Setup-1.0.0.0.exe`: `COMMERCIAL_INSTALLER=PASS`, `PAYLOAD_FILE_COUNT=1855`, `SIGNED_APP_HASH_EXCEPTIONS=0`; toàn bộ payload khớp publish. Setup và app vẫn `NotSigned`, nên kết quả này chỉ chứng minh tính toàn vẹn payload, không chứng minh khả năng cài đặt thực, chữ ký/phát hành hoặc ACL/UAC.
- Loại bỏ kiểm tra FileVersion bị lặp trong builder. Parse PowerShell của builder và verifier pass; `git diff --check` pass ngoài cảnh báo line-ending Windows thông thường.
- Dựng lại end-to-end bằng builder đã harden, NSIS `v3.13` đã xác minh và publish nói trên: commercial package `35/35`, installer compile pass, installer verifier `1855/1855`, notices sidecar hợp lệ; setup hash `1DDF7DD4EEAEE59911871EA7EF5F56D85A168F04316F49F2577205C2989B32C9`. Artifact mới ở `%TEMP%\ASMILE-Commercial-Installer-Verified-8bc15a4d53794530863add033343a076\ASMILE-BO-Setup-1.0.0.0.exe`.
- Toàn bộ pipeline package/verifier chạy được, nhưng trạng thái vẫn là `PRODUCTION_SIGNED=False`; không được phát hành artifact này. Cài/gỡ/upgrade trên clean Windows, transactional rollback, ACL dưới service identity, pháp lý/EULA và chữ ký doanh nghiệp vẫn là cổng bắt buộc chưa đạt.

### Sửa semantics cấu hình xác nhận restore và tạo Release mới 2026-10-07

- Phát hiện Security.RequireAdminConfirmation=false trước đây khóa khôi phục database/file, trái với nhãn setting; không có đường nào bỏ prompt theo lựa chọn cấu hình.
- Nay giá trị true vẫn yêu cầu xác nhận; false bỏ riêng dialog xác nhận nhưng không bỏ kiểm tra quyền Database.Restore hay kiểm tra tính hợp lệ của backup. Mọi restore tiếp tục ghi audit; khi bỏ prompt, audit nêu rõ chính sách đã áp dụng. Thiếu hoặc sai định dạng setting vẫn fallback true qua EfSecuritySettingsService.
- Mô tả cấu hình được làm rõ phạm vi là restore database/file; policy được tách thành SettingsConfirmationPolicy để test hai nhánh confirm/cancel và skip prompt. Test Settings/Security liên quan 73/73 pass; full unit 982/982, 0 failed/skip; build có XAML encoding check pass.
- Release mới tại `%TEMP%\\ASMILE-Commercial-SystemSettings-SecurityPolicy-f069a8805fe94546aea9926d8b857360`: verifier `35/35`, `1855` file, 0 PDB, tree SHA-256 `11856E1DC354711DECAEEDAD27A1D2F22116DC6E4C30F336043BCA878340ABE3`.
- Setup mới tại `%TEMP%\\ASMILE-Commercial-Installer-SecurityPolicy-1efbbadd5f434bb392790ff2a4c34044\\ASMILE-BO-Setup-1.0.0.0.exe`, hash `F97EEFBB6A03F8D30EA68C90407340901BA6F1FF2474AE6F36C7C0B03CB1C986`; builder và verifier `1855/1855` pass. Vẫn `NotSigned`, chưa chạy install/UAC trên Windows thật; không phát hành.
- Chạy UI smoke trên chính EXE từ Release publish mới (không dùng Debug): vào được System Settings Home, không exception/error dialog; E2E `1/1` pass trong `58s` trên LocalDB cô lập `ASMILE_BO_E2E_RELEASE_SETTINGS_20261007`. Đây xác nhận startup/login/render của artifact hiện tại, không thay cho kiểm thử restore database/file hoặc installer trên clean machine.
- Bổ sung test registry `SETTINGS_U_021` khóa contract mặc định bật, editable, nêu permission guard/audit; nhóm SettingsCommercial + SettingsConfirmation sau cập nhật test `61/61` pass.
- Chạy lại full unit suite sau test registry bổ sung: `983/983` pass, 0 failed, 0 skipped.

### Hardening quyền thư mục backup SQL 2026-10-07

- Rà ACL chỉ đọc phát hiện `%ProgramData%\ASMILE-BO\Backups` đang nhận quyền ghi kế thừa từ `BUILTIN\Users`; hướng dẫn triển khai trước đó yêu cầu không cấp quyền ghi rộng nhưng script provision chỉ thêm quyền cho service/app, không loại ACE kế thừa.
- `scripts/provision_sql_backup_directory.ps1` nay dùng `BackupAclProvisioning.psm1`: bảo vệ DACL khỏi kế thừa tiếp, giữ các ACE hiện có không phải broad-write (bao gồm read-only), loại quyền ghi từ `Everyone`, `Authenticated Users`, `BUILTIN\Users`, cấp `Modify` cho SQL Server service identity và app identity được khai báo; đọc/xác minh riêng DACL để không cần quyền ghi SACL. Vẫn yêu cầu elevated PowerShell; hỗ trợ `-WhatIf`.
- `scripts/test_backup_acl_provisioning.ps1` chạy trên thư mục temp riêng: loại 3 broad-write ACE, xác minh giữ quyền đọc broad, xác nhận app identity và SQL Express service identity có `Modify`, từ chối identity `Everyone`, và chạy lặp idempotent. Kết quả `BACKUP_ACL_POLICY=PASS`, `REMOVED_BROAD_WRITE_RULES=3`, `MANAGED_IDENTITY_COUNT=2`.
- Parse PowerShell của module/provision/test pass. Chưa áp dụng ACL lên thư mục production vì token hiện tại không elevated; do đó quyền ghi rộng trên máy này chưa được sửa. Chưa chạy integration `ASMILE_SQLSERVER_SERVICE_BACKUP_TEST=1`; cần elevated deployment/UAT trên SQL Server host để xác nhận service account backup/verify thật sau provision.

### Loại bỏ kỳ Sổ cái giả khi chưa có kỳ lưu 2026-10-07

- Audit `SettingsViewModel` phát hiện summary Sổ cái dùng `DateTime.Today` để tự tạo kỳ tháng hiện tại, hiển thị “Đang mở” và đếm bút toán theo tháng máy dù `AccountingPeriods` không có kỳ tương ứng.
- Trang chi tiết và summary nay lấy mã kỳ, ngày, trạng thái, ngày/người khóa từ `AccountingPeriodService`; nếu không có kỳ hiện hành thì hiện “Chưa cấu hình” và không tạo báo cáo kỳ giả. Khi có kỳ, query số bút toán lọc theo `FromDate`/`ToDate` persisted của kỳ.
- Definition cho sáu trường kỳ không còn default theo ngày máy hoặc giá trị mẫu; mô tả nêu rõ nguồn `AccountingPeriods`. Legacy `SettingValues` vẫn không được dùng để ghi đè snapshot runtime.
- Test `SETTINGS_U_022` xác minh definition read-only/null-default và projection cho không có kỳ/kỳ đã đóng. Nhóm Settings commercial/read-only `70/70`; full unit suite `984/984`, 0 failed/skip. Release build `0 warning/0 error`.
- Release publish tại `%TEMP%\ASMILE-Commercial-SystemSettings-Period-0530f040c8384a43a6f2f93442b862d9`: verifier `35/35`, `PDB_COUNT=0`, `PUBLISH_FILE_COUNT=1855`, `PUBLISH_TREE_SHA256=4234D85685134FF3604D388D487664710AF2C787B5287CDF541BA111EA1F0EEE`, `EXE_SHA256=479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Installer mới dựng từ đúng publish trên bằng NSIS `v3.13`; verifier `COMMERCIAL_INSTALLER=PASS`, payload `1855/1855`, `APP_SIGNATURE=NotSigned`, `INSTALLER_SIGNATURE=NotSigned`, `PRODUCTION_SIGNED=False`. Setup tại `%TEMP%\ASMILE-Commercial-Installer-SystemSettings-Period-9f8bfb3a71b34f22abefc8e35fa94443\ASMILE-BO-Setup-1.0.0.0.exe`, SHA-256 `D7862546DB23F0FFCA894431FB9B0AEF2E46AD39BBF7074D496C040ACFF22122`.
- UI smoke chạy trực tiếp Release publish trên LocalDB test mới, cô lập: System Settings Home `1/1` pass trong `1m01s`. Đây là xác nhận startup/login/render Home của artifact; chưa phải runtime test chi tiết view kỳ Sổ cái hoặc customer database.
- Vẫn chưa thể phát hành thương mại: thiếu code-signing certificate/private key và chữ ký production; chưa chạy elevated install/upgrade/uninstall/rollback, giữ dữ liệu và ACL trên clean Windows. ACL live `%ProgramData%\ASMILE-BO\Backups` vẫn cần provision elevated và backup/restore verify dưới SQL service identity. Cần chốt EULA/pháp lý và nghiệm thu môi trường khách hàng.

### Fail-closed preflight signing của installer 2026-10-07

- Rà installer builder cho thấy production certificate được tìm sau khi `OutputDirectory` đã được tạo; thiếu chứng thư làm build dừng nhưng để lại thư mục rỗng.
- Chuyển certificate/private-key và `signtool.exe` preflight lên trước mọi thao tác tạo output. Thêm `scripts/test_commercial_installer_production_gate.ps1` với thumbprint tổng hợp; xác nhận production mode từ chối certificate thiếu và `OUTPUT_DIRECTORY_CREATED=False`.
- Negative gate test chạy trên publish đã verify: `INSTALLER_PRODUCTION_GATE=PASS`; production build không có side effect. Đường build thường chạy lại thành công, installer verifier `1855/1855`, SHA-256 vẫn `D7862546DB23F0FFCA894431FB9B0AEF2E46AD39BBF7074D496C040ACFF22122`.
- Tại lần kiểm tra này, token vẫn `ELEVATED_ADMIN=False`; không có code-signing certificate private key hợp lệ trong CurrentUser/LocalMachine. Thay đổi cải thiện preflight nhưng không thay thế chứng thư doanh nghiệp, cài/gỡ/upgrade UAT, rollback, pháp lý/EULA hoặc xác minh ACL production.

### Rà soát tập trung Cài đặt hệ thống cho thương mại 2026-10-07

- Phân loại dữ liệu Home: trạng thái SQL đọc từ cấu hình kết nối đang dùng; backup, lịch sử đăng nhập và thay đổi cài đặt đọc từ repository/DB; API là kết quả probe endpoint; module count đọc từ `IAppModuleService`. Đây là các nguồn có provider thật, nhưng từng trạng thái chỉ chứng minh phạm vi tương ứng (ví dụ SQL `CanConnect` không chứng minh migration/backup hoặc quyền service account).
- `SettingsRegistry.GetDefaultApps()` là catalog module do mã nguồn seed (tên/description/cờ ban đầu); `AppModuleService.GetAppsAsync()` sau đó đọc cờ từ bảng `AppModules`. Đây là catalog logical modules persisted, không phải scan chương trình cài trong Windows. Source trước đó seed version giả `1.0.0`; nay seed không còn version đó và màn module khả dụng lấy version trực tiếp từ entry assembly đang chạy. Giá trị legacy đã lưu ở các hàng `AppModules` cũ chưa được migrate, nhưng màn này không còn dùng chúng để hiển thị version.
- Database connection fields là read-only và lấy từ connection string thực tế; không cho sửa server/auth rồi tạo cảm giác tiến trình đã đổi database. Network/API endpoint và POS/FO settings không có provider khởi chạy server/client; chúng đang read-only, probe URL hoặc báo chưa cấu hình, không được xem là dịch vụ đã triển khai.
- Từ source installer hiện tại, bản nâng cấp chép các file của publish mới vào `$INSTDIR`; uninstall xóa đúng inventory của gói đang chạy. Chưa có migration/cleanup inventory của phiên bản cũ hoặc transactional rollback trong quá trình nâng cấp. Do đó archive verifier `1855/1855` chứng minh payload trong setup khớp publish, không chứng minh upgrade sạch hoặc phục hồi sau khi cài bị gián đoạn.
- Ưu tiên kỹ thuật tiếp theo trong phạm vi Cài đặt hệ thống: (1) hoàn tất luồng upgrade có manifest sở hữu file, giữ dữ liệu ngoài thư mục cài và rollback khi thay thế thất bại; (2) nối đầy đủ release updater tới helper có xác minh publisher/signature, quyền nâng cấp và recovery; (3) nghiệm thu backup/restore dưới SQL Server service identity sau provision ACL. Chỉ bật trạng thái/phần điều khiển tương ứng khi provider thật đã sẵn sàng.
- Các cổng không thể chứng minh bằng unit/publish verifier: chứng thư Code Signing/private key và timestamp, elevated clean-machine install/upgrade/uninstall/rollback, kiểm tra dữ liệu khách hàng được giữ nguyên, ACL/backup trên SQL Server host, cùng EULA/privacy/support policy và legal review.
- Sửa version hiển thị trong `apps-default`: catalog không seed version `1.0.0` giả; view lấy version từ entry assembly đang chạy và ghi rõ đó là phiên bản sản phẩm. Test regression mới pass; full unit `985/985`, `0` failed/skip; Release app build `0` warning, `0` error và XAML encoding check pass.
- Recheck môi trường hiện tại: `ELEVATED_ADMIN=False`, `VALID_CODESIGNING_PRIVATE_KEY_COUNT=0` trong CurrentUser/LocalMachine. Không thực hiện thao tác ACL hoặc tạo artifact phát hành chưa ký.

### Release candidate Cài đặt hệ thống sau loại bỏ version seed giả 2026-10-07

- Full unit suite sau thay đổi: `985/985` pass, `0` failed/skip. `dotnet build ASMILE.BO.App/ASMILE.BO.App.csproj -c Release --no-restore`: `0` warning, `0` error; XAML encoding check pass.
- Publish ban đầu không có `--self-contained` bị loại khỏi commercial candidate dù generic verifier pass: runtimeconfig tham chiếu shared framework, `1405` file. Đã tạo lại đúng `win-x64 --self-contained true`; runtimeconfig hiện đóng gói `Microsoft.NETCore.App` và `Microsoft.WindowsDesktop.App` `8.0.22`.
- Self-contained publish `C:\Users\webvi\AppData\Local\Temp\ASMILE-Commercial-SystemSettings-SelfContained-a715d2993d0b4ee9af3da44eea14f7c0`: `COMMERCIAL_PACKAGE=PASS`, hiện `CHECK_COUNT=37` sau runtime profile gate, `PDB_COUNT=0`, `PUBLISH_FILE_COUNT=1855`, tree SHA-256 `825F7E9940BF421A2C88FDBC287F51DB1EA4323D6FFD0DDEA7F5B94CC8AA3B46`; EXE SHA-256 `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Installer được dựng lại qua builder đã gọi verifier mới tại `C:\Users\webvi\AppData\Local\Temp\ASMILE-Commercial-Installer-RuntimeGate-b60e6a1cca794473a516909eef82481d\ASMILE-BO-Setup-1.0.0.0.exe`: NSIS `3.13`, `INSTALLER_BUILD=PASS`, `COMMERCIAL_INSTALLER=PASS`, payload `1855/1855`, hash `450E27E6A555C7E2A40C472005EDEE1FDCE2E70FDAB5403EEB1CE7AF9EF7F60D`. Cả app và setup `NotSigned`, `PRODUCTION_SIGNED=False`; đây chỉ là candidate để kiểm tra, không phát hành.
- UI smoke chạy đúng EXE trong self-contained publish trên LocalDB test cô lập `ASMILE_BO_E2E_SETTINGS_LIVEVERSION_20261007`: `AlacarteApp_ShouldOpenSystemSettingsHomeWithLivePanels` pass `1/1` trong `1m01s`. Phạm vi chứng minh startup/login và render Home Cài đặt hệ thống, không chứng minh view module version cụ thể, clean-machine installation, migration hoặc upgrade.
- Hiện vẫn chưa có certificate/private key code-signing và quyền elevated; chưa chạy cài/gỡ/upgrade/rollback hoặc áp ACL production. Cổng này tiếp tục chặn commercial release.

### Chặn publish framework-dependent khỏi commercial package 2026-10-07

- Rà soát phát hiện publish mặc định framework-dependent vẫn vượt qua verifier `35/35`; runtimeconfig của gói đó yêu cầu shared framework và có `1405` file, trong khi release target trước đây là self-contained `win-x64` (`1855` file). Do đó verifier cũ chưa chứng minh candidate chạy được trên máy khách chưa cài .NET Desktop Runtime.
- `verify_commercial_package.ps1` nay yêu cầu runtimeconfig đóng gói đủ `Microsoft.NETCore.App` và `Microsoft.WindowsDesktop.App`, không khai báo `framework/frameworks` dùng shared runtime, đồng thời `deps.json` phải target `/win-x64`. Candidate self-contained hiện tại pass `37/37`; framework-dependent candidate bị từ chối đúng tại `SelfContainedRuntime`.
- Thêm `scripts/test_commercial_package_runtime_gate.ps1`; script regression tự tạo publish framework-dependent win-x64 tạm, xác nhận verifier fail đúng gate rồi dọn thư mục GUID đã xác minh nằm trong `%TEMP%`. Kết quả `FRAMEWORK_DEPENDENT_REJECTED=PASS`.
- Bổ sung hướng dẫn vào `installer/README.md`. Đây là hardening package preflight; không giải quyết chữ ký, upgrade cleanup/rollback, clean-machine install hoặc các cổng UAT/legal đã nêu ở trên.

### Home Cài đặt hệ thống hiển thị health từ provider 2026-10-07

- Kết nối Home `SettingsViewModel` với `IInfrastructureHealthService.CheckAsync()`; lấy trạng thái Migrations và E-invoice từ cùng health report của ứng dụng, không tạo trạng thái giả. Warning hiển thị thông điệp rút gọn; lỗi chỉ hiển thị trạng thái `Lỗi`, tránh lộ chi tiết exception trên dashboard. Hai dòng mới được thêm trong card System health.
- Unit regression kiểm tra warning thật, provider thiếu và không rò exception; full unit suite `986/986` pass, `0` failed/skip. Release build pass `0` warning, `0` error và XAML encoding check pass.
- Release self-contained publish `%TEMP%\ASMILE-Commercial-SystemSettings-HealthHome-b41dd8d0517d4b298fcafc69969b5ce5`: commercial verifier `37/37`, `PDB_COUNT=0`, `PUBLISH_FILE_COUNT=1855`, tree SHA-256 `7D630AFD71818FA6A23C5A3A196529DC161D9D4DF79664203A801CFCDD9C37CD`; app EXE SHA-256 `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- UI smoke chạy trên đúng EXE trong publish, đăng nhập và mở Home Cài đặt hệ thống trên LocalDB E2E cô lập: `1/1` pass (`19s`), gồm UIA assertion cho Migrations và E-invoice. Điều này chứng minh đường render và provider trả report trong môi trường test; không thay thế kiểm thử migration/adapter trên hệ thống khách hàng.
- Release code vẫn chưa ký; chưa có code-signing private key hoặc elevated token. Không coi installer candidate là bản có thể phát hành; clean-machine install/upgrade/uninstall/rollback và UAT production còn mở.
- Dựng setup từ đúng publish trên bằng NSIS `v3.13`: installer verifier `COMMERCIAL_INSTALLER=PASS`, payload `1855/1855`; artifact `%TEMP%\ASMILE-Commercial-Installer-SystemSettings-HealthHome-20261007\ASMILE-BO-Setup-1.0.0.0.exe`, SHA-256 `CB24E6ABA136B0302E1B88338FBB7AACBFC069E15DD079928B2B910294C6797A`. App/setup `NotSigned`, `PRODUCTION_SIGNED=False`; candidate chỉ để kiểm tra, không phân phối khách hàng.

### Guard phiên bản cho cập nhật trong Cài đặt hệ thống 2026-10-07

- `UpdatePackageStagingService` trước đây tin cờ `IsUpdateAvailable` của manifest provider sau khi provider chạy; nay tự kiểm tra `LatestVersion > currentVersion` và `CurrentVersion` provider phải bằng phiên bản runtime được yêu cầu. Kết quả mâu thuẫn bị từ chối trước khi gọi mạng hoặc tạo file staging. Thông báo lỗi không cung cấp đường staging, package vẫn cần manifest chữ ký hợp lệ/HTTPS/SHA-256 và quyền `ManageServices`.
- Thêm ba regression case: cùng phiên bản, downgrade và current-version không khớp. Nhóm update staging `10/10` pass; full unit suite `989/989`, không failed/skip. Release build `0` warning, `0` error, XAML encoding check pass.
- Self-contained `win-x64` publish `%TEMP%\ASMILE-Commercial-SystemSettings-UpdateGuard-a7a8b70c490b4e32855795689d5ea810`: verifier `37/37`, `PDB_COUNT=0`, `PUBLISH_FILE_COUNT=1855`, tree SHA-256 `5C69BCAD3CC2BD0493DA131F6865BD8F2CAE9CD3C240DAFD99EEE37F048B4344`, app EXE SHA-256 `1270897ADA5FF4D68BC036E2E31CE63E644B0FC663C48DC84DB34F4A90A5C92B`.
- UI smoke trên đúng EXE Release và LocalDB test cô lập: Home Cài đặt hệ thống `1/1` pass trong `1m02s`. Installer NSIS `3.13` từ đúng publish được kiểm tra đủ `1855/1855` payload; SHA-256 `124F269D3C5E953A4DF679C156757B45FD36D3ACE804E152A91528DA05EAD6B0` tại `%TEMP%\ASMILE-Commercial-Installer-SystemSettings-UpdateGuard-20261007\ASMILE-BO-Setup-1.0.0.0.exe`.
- Đây chỉ hoàn thiện phần kiểm tra trước tải/staging, không bổ sung cài đặt tự động, Authenticode publisher validation cho package, transactional rollback hay nâng cấp sạch. App/setup vẫn `NotSigned`; chưa đủ điều kiện phát hành thương mại.

### Phân biệt tình trạng hai loại backup trên Home Cài đặt hệ thống 2026-10-07

- Home trước đó chỉ hiện thời điểm file SQL backup gần nhất dù app còn có file backup riêng; số liệu dễ bị hiểu như một trạng thái chung “backup OK”. Nay dashboard tách `SQL backup file` (chỉ báo file gần nhất được tìm thấy, không tuyên bố restore đã verify) và `File backup đã kiểm tra` (provider chỉ trả record sau khi kiểm tra manifest/hash; kèm số lượng tệp). Mục không có bản sao lưu hiển thị rõ `Chưa tìm thấy`/`Chưa có`.
- Unit regression xác nhận format và semantics hai trạng thái, cùng nhóm version guard: nhóm Settings/update `72/72`; full unit `990/990`, không failed/skip. Release self-contained build không warning/error; XAML encoding check pass.
- UI smoke trên chính Release EXE xác nhận Home và dòng file backup hiện diện `1/1` pass trên LocalDB test cô lập. Lần chạy E2E đầu báo fail giả do assertion tiếng Việt so chuỗi có dấu sau helper bỏ dấu; UIA diagnostic ghi nhận dòng `File backup da kiem tra: Chua co` đang hiển thị; sửa assertion sang cùng dạng chuẩn hóa và lần chạy sau pass. Không thay đổi/giảm kiểm tra runtime UI.
- Publish `%TEMP%\ASMILE-Commercial-SystemSettings-BackupHealth-ec10295bd95445aba8f688f5aa33a3eb`: verifier `37/37`, `PDB_COUNT=0`, `PUBLISH_FILE_COUNT=1855`, tree SHA-256 `30C06DA83429CF565A78B28A2A6ED20D0D09E5BC86D9D4E4BDEEB034ACFDC8EA`, EXE SHA-256 `1270897ADA5FF4D68BC036E2E31CE63E644B0FC663C48DC84DB34F4A90A5C92B`.
- Setup NSIS `v3.13` dựng từ đúng publish và verifier đạt `1855/1855`: `%TEMP%\ASMILE-Commercial-Installer-SystemSettings-BackupHealth-20261007\ASMILE-BO-Setup-1.0.0.0.exe`, SHA-256 `79E97866E6DFAC17A4170565B46C37C561E73B9A1B8D78EF8A5F5D4C4A7D26A3`. `PRODUCTION_SIGNED=False`, app/setup `NotSigned`; đây vẫn là candidate nội bộ, chưa phát hành.

### SQL backup status và audit vận hành trong Cài đặt hệ thống 2026-10-07

- `SqlServerBackupService.GetRecentAsync()` trước đây gắn `Completed` cho mọi `.bak` chỉ vì file được liệt kê; nay trả `Chưa xác thực`. Backup mới chỉ mang trạng thái `Đã xác thực` nếu SQL `RESTORE VERIFYONLY WITH CHECKSUM` trả kết quả thành công; policy tắt verify thì ghi `Chưa xác thực`. Nếu hàm verify trả `false`, thao tác tạo backup báo lỗi thay vì đánh dấu đã verify. Restore cũng dừng trước bước đổi database nếu `ValidateBackupAsync` trả `false`.
- Sau Verify thủ công, trạng thái `Đã xác thực`/`Không hợp lệ`/`Không xác thực được` được giữ đúng cho file theo đường dẫn, tránh lệch khi thứ tự danh sách backup thay đổi. Thông báo sau khi tạo backup phản ánh trạng thái verify thực tế.
- Ghi audit sự kiện tạo backup bắt đầu/thành công/thất bại và xác minh database/file backup thành công/thất bại qua `SecurityEventType`; metadata chỉ gồm tên tệp, trạng thái verify, số lượng/size và loại exception, không ghi đường dẫn tuyệt đối. Audit backend lỗi được ghi Serilog, không làm mất backup không phá hủy.
- SQL backup regression `5/5`; full unit `990/990`, không failed/skip. Self-contained Release publish `%TEMP%\ASMILE-Commercial-SystemSettings-BackupAudit-1e3c7b540bc045f2910a16cf4c99965e` đạt package verifier `37/37`, `PDB_COUNT=0`, `PUBLISH_FILE_COUNT=1855`, tree SHA-256 `F2604B1AEEADBBF70487A7BD3CC81AD4278030AD89C8E2D5B45829523034A85A`, EXE SHA-256 `1270897ADA5FF4D68BC036E2E31CE63E644B0FC663C48DC84DB34F4A90A5C92B`.
- UI smoke chạy đúng EXE Release và LocalDB cô lập: `1/1` pass. Setup NSIS `v3.13` đúng publish được verify `1855/1855`: `%TEMP%\ASMILE-Commercial-Installer-SystemSettings-BackupAudit-20261007\ASMILE-BO-Setup-1.0.0.0.exe`, SHA-256 `F681346BA2F7B347363C07A28D45E312C1D81B9C08E149A3D7AEBE51FB72AF83`.
- Chưa có SQL Server production/identity khách hàng để chạy backup/restore drill; candidate app/setup vẫn `NotSigned`, chưa chạy elevated install/upgrade/rollback trên máy sạch.

### Audit luồng staging cập nhật trong Cài đặt hệ thống 2026-10-07

- Luồng `Tải và xác minh gói` nay ghi audit riêng khi bắt đầu, staging thành công/thất bại, hoặc bị hủy/quá thời gian. Metadata giới hạn ở channel, version, status, dung lượng, SHA-256 và loại exception; không ghi URL tải hay đường dẫn staging. Audit backend lỗi chỉ được ghi Serilog và không ngăn luồng staging.
- Thêm các event `UpdateStagingStarted`, `UpdateStagingCompleted`, `UpdateStagingFailed` ở cuối enum để giữ nguyên tên event cũ đã được lưu dưới dạng chuỗi. Tại thời điểm thay đổi: full unit `990/990`, 0 failed/skip; XAML encoding check pass.
- Self-contained `win-x64` publish được tạo lại từ mã hiện hành và đạt commercial verifier `37/37`, `PDB_COUNT=0`, `PUBLISH_FILE_COUNT=1855`, tree SHA-256 `F8F0B8D85918466C8F196EB5FBDB0E005F27263CB7BCFC732AFCCD936E840202`.
- Chưa tạo setup mới cho thay đổi này: môi trường hiện tại không tìm thấy `makensis`; builder dừng trước khi tạo output. Cũng chưa có code-signing private key/elevated token để chạy installer production/UAT. Artifact publish không phải installer phát hành.

### Kiểm tra quyền đọc đường dẫn VFC/Storage trong Cài đặt hệ thống 2026-10-07

- `Directory.Exists` trước đây được dùng như dấu hiệu path có thể sử dụng, dù không xác nhận tài khoản ứng dụng có quyền liệt kê nội dung. Nay `DescribePhysicalPath` và trạng thái ổ VFC thực hiện probe liệt kê chỉ-đọc (tối đa một entry), không tạo file và không suy diễn quyền ghi. Nếu không đọc được, trạng thái hiển thị bị từ chối/không khả dụng; không còn ghi `Accessible` chỉ dựa trên việc thư mục tồn tại. Cấu hình `Backup.BackupPath` nêu rõ đây chỉ là kiểm tra quyền đọc của ứng dụng, không chứng minh quyền ghi của tài khoản dịch vụ SQL Server.
- Thêm regression test cho đường dẫn hiện có/thiếu và test repository phân biệt đường dẫn trỏ tới file thay vì thư mục; status/mô tả đều khẳng định quyền đọc nội dung. Test boundary `SETTINGS_U_042` giữ rõ giới hạn quyền SQL Server. Full unit Release `992/992`, 0 failed/skip; XAML encoding check pass.
- Self-contained `win-x64` package mới nhất đạt verifier `37/37`, `PDB_COUNT=0`, `PUBLISH_FILE_COUNT=1855`, tree SHA-256 `39D1370FEFC36F023FA9ED3A2338BE028A05A0758F21801906861F87EEF41BF6`.
- Installer chưa được dựng từ package này vì NSIS `makensis` không có trên PATH hoặc vị trí cài đặt đã kiểm tra; không có installer artifact mới. Vẫn còn thiếu code-signing certificate/private key, elevated clean-machine install/upgrade/rollback UAT và backup/restore drill dưới SQL Server service identity.

### Fail-closed cài đặt lịch SQL backup trong Cài đặt hệ thống 2026-10-07

- `Backup.EnableAutoBackup` khai báo mặc định `false`, nhưng `BackgroundSchedulerService.ApplyDatabaseBackupPolicyAsync` trước đó dùng `true` khi setting bị thiếu/không parse được. Scheduler nay mặc định tắt và ghi warning nếu setting thiếu/không hợp lệ; chỉ giá trị bật hợp lệ mới kích hoạt lịch. Setting lịch cron sai tiếp tục tắt job.
- Thêm regression cho setting thiếu và `not-a-boolean`; regression xác nhận bật tường minh vẫn hoạt động và tắt vẫn không tạo `NextRunAtUtc`. Scheduler targeted `4/4`; full Release unit `994/994`, 0 failed/skip; XAML encoding check pass.
- Self-contained `win-x64` package mới nhất đạt verifier `37/37`, `PDB_COUNT=0`, `PUBLISH_FILE_COUNT=1855`, tree SHA-256 `114E899390137688E3597EA0B62679180206F1EEE01758AA739800E783BBC1A9`.
- Installer chưa được tạo: máy vẫn không có NSIS `makensis`; app package chưa ký, chưa có chứng thư ký mã/elevated token. Cần tiếp tục clean-machine installer/upgrade/rollback và backup/restore UAT trước khi phân phối.

### Thu hồi phiên khi bật chế độ bảo trì trong Cài đặt hệ thống 2026-10-07

- Trước đây `ValidateSessionAsync`/`RenewSessionAsync` chỉ trả `false` cho người không có `System.Maintenance`, còn `LoginSession` và `ActiveCompanySession` vẫn active. Vì vậy phiên cũ có thể hoạt động trở lại khi tắt bảo trì hoặc cấp quyền sau đó; nội dung setting nói đưa người dùng về đăng nhập nhưng hành vi chưa được thực thi.
- Nay khi policy bảo trì từ chối một phiên, hệ thống đóng session bền vững (`IsActive=false`, `EndedAt`), đóng presence đang hoạt động (`IsActive=false`, `CompletedAtUtc`, status `MaintenanceMode`), đồng bộ cache in-memory và ghi audit `SessionRevoked` không chứa token. Các lối Validate, Renew và lấy permission đều áp dụng cùng chính sách; phiên đã thu hồi không tự sống lại.
- Regression kiểm tra cả session, presence, audit và không khôi phục phiên sau khi tắt maintenance/cấp quyền. Test hồi quy pass; full Release unit `994/994`, không failed/skip; XAML encoding check pass.
- Self-contained `win-x64` publish từ mã cuối cùng tại `%TEMP%\ASMILE-Commercial-SystemSettings-Maintenance-20261007`: `COMMERCIAL_PACKAGE=PASS`, `CHECK_COUNT=37`, `PDB_COUNT=0`, `PUBLISH_FILE_COUNT=1855`, tree SHA-256 `857467E77DCFD72A824827445D78356AAAADC77A95F53533B25672BACF8D7B9F`, EXE SHA-256 `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`. `ASMILE.BO.Infrastructure.dll` trong publish trùng SHA-256 với Release output; app `NotSigned`.
- Trace source qua graph xác nhận `MainWindow` chạy `DispatcherTimer` mỗi 30 giây; khi Validate/Renew từ chối, shell bị khóa, phiên phía client bị clear và Login overlay hiện. Nếu không xác minh được do exception, shell vẫn bị khóa và hiển thị overlay kiểm tra lại. Đây là bằng chứng source-path, chưa phải UI runtime smoke trong chế độ maintenance.
- UIA smoke chạy `AlacarteApp_ShouldOpenSystemSettingsHomeWithLivePanels` trên đúng EXE publish, database LocalDB mới tên ngẫu nhiên: `1/1` pass trong `1m37s`, Home render health/recommended settings, không có error dialog. App đã thoát; database test riêng đã được xóa và truy vấn xác nhận không còn tồn tại. Smoke này xác nhận runtime Home/startup, chưa kích hoạt maintenance mode trên giao diện.
- Recheck integration trên LocalDB cô lập: toàn suite `37/39`; hai migration timeout ở Front Office maintenance-assets và SQL backup khi suite chạy đồng thời. Chạy từng test riêng sau đó đều pass (`1/1`, 44–45 giây), nên lỗi chưa tái hiện đơn lẻ nhưng toàn suite vẫn không xanh và cần ổn định hóa lịch chạy trước release gate.
- Ổn định hóa test gate: các class dùng SQL Server/LocalDB được đưa vào xUnit collection `DisableParallelization=true`, tránh cạnh tranh migration với nhau/collection khác mà không tăng command timeout. Chạy lại toàn Integration Release: `39/39` pass, 0 failed/skip, `10m25s`.
- Recheck cổng phát hành: không tìm thấy `makensis`, không có code-signing certificate/private key và token hiện tại không elevated. Chưa thể tạo setup đã ký/cài đặt trên máy sạch. Còn phải xác nhận SQL Server production/UAT (role permission, client bị đưa về đăng nhập và audit persistence) trước khi coi policy này đủ điều kiện phát hành.
- Bản đính chính: NSIS `v3.13` đã có sẵn trong thư mục công cụ tạm đã xác minh, dù không nằm trên `PATH`; đã dùng đúng compiler và 7-Zip để build candidate `%TEMP%\ASMILE-Commercial-Installer-SystemSettings-Maintenance-20261007\ASMILE-BO-Setup-1.0.0.0.exe`. `INSTALLER_BUILD=PASS`, `COMMERCIAL_INSTALLER=PASS`, payload `1855/1855`, SHA-256 `561E87D21C6D93ADB5BD29FC81F8E3FDD22474F20C77E06E8DE4733CDB4B4F3F`. App/setup vẫn `NotSigned`, `PRODUCTION_SIGNED=False`; chưa cài thử do không elevated và artifact này không phải bản phát hành.
- UIA E2E `AlacarteApp_ShouldReturnToLoginWhenMaintenanceRevokesTheCurrentSession` chạy app sau đăng nhập, bật maintenance trực tiếp trên LocalDB riêng và xác nhận trong 30 giây polling app trở lại Login, session bị kết thúc, `SessionRevoked` tồn tại và app không đóng. Test ban đầu phát hiện logout dọn dẹp ghi đè presence status `MaintenanceMode` thành `Completed`; đã sửa `CompleteActiveCompanySessionAsync` không ghi đè một presence terminal đã kết thúc và thêm regression mô phỏng `ValidateSessionAsync` rồi `LogoutAsync`. Unit regression `1/1`, toàn Release unit `994/994`, E2E `1/1` trên Debug và `1/1` trên đúng `ASMILE.BO.App.exe` từ publish mới; LocalDB E2E được xóa và truy vấn xác nhận còn `0` database theo prefix test.
- Sau sửa status, publish mới thay thế publish trước tại `%TEMP%\ASMILE-Commercial-SystemSettings-Maintenance-Fix-20261007`: verifier `37/37`, `PDB_COUNT=0`, `PUBLISH_FILE_COUNT=1855`, tree SHA-256 `EC2B21F51093D53878B93AF4BDC9A76E5F8920D6E5A65DE6561E5D182A0A17C3`; SHA-256 Infrastructure DLL trong package khớp Release output (`5BCA7FF608CC4048A78402F00FB342412832D85C41D8EFC4D01287FF7BA82075`).
- Installer candidate mới tương ứng tại `%TEMP%\ASMILE-Commercial-Installer-SystemSettings-Maintenance-Fix-20261007\ASMILE-BO-Setup-1.0.0.0.exe`: `INSTALLER_BUILD=PASS`, `COMMERCIAL_INSTALLER=PASS`, payload `1855/1855`, SHA-256 `4EC9032CCF3BBF5C46642595AC9A0BEB4230B7138864FCA1E43020BF4368A191`. Cả app và installer vẫn `NotSigned`, `PRODUCTION_SIGNED=False`; candidate không được quảng bá như bản phát hành thương mại.

### Tương thích Windows PowerShell cho provisioner ACL backup 2026-10-07

- Kiểm tra trên host hiện tại: `MSSQL$SQLEXPRESS` đang chạy dưới `NT Service\MSSQL$SQLEXPRESS`; probe SQL Express chỉ đọc thành công. Thư mục backup live `C:\ProgramData\ASMILE-BO\Backups` tồn tại nhưng đang kế thừa quyền ghi của `BUILTIN\Users`, nên chưa đạt least privilege.
- `scripts/test_backup_acl_provisioning.ps1` ban đầu phát hiện hai lỗi thật trong provisioner: `FileSystemAclExtensions` không có trong Windows PowerShell .NET Framework; thay bằng `Set-Acl` tổng quát lại chạm SACL và đòi `SeSecurityPrivilege`. `BackupAclProvisioning.psm1` nay dùng `DirectoryInfo.GetAccessControl/SetAccessControl` giới hạn `AccessControlSections.Access`, chỉ sửa DACL.
- Test policy chạy trên thư mục tạm có ACL tổng hợp và đã xóa sau test: `BACKUP_ACL_POLICY=PASS`, loại bỏ 3 broad write rules, idempotent, vẫn giữ broad read, cấp Modify cho đúng 2 managed identities (user chạy ứng dụng và SQL Express service), từ chối identity broad. `-WhatIf` của script provision xác nhận target/service identities nhưng không đổi ACL; kiểm tra lại live ACL cho thấy quyền `BUILTIN\Users:Write` vẫn còn.
- Trước khi cô lập test path, chưa chạy opt-in backup integration vì live directory còn broad-write. Không tự sửa ACL live. Bước nghiệm thu production vẫn cần elevated provisioning trên SQL host với application identity đã chốt, xác minh DACL thực tế, rồi backup/`RESTORE VERIFYONLY` và restore drill.

### Transaction rollback cho installer trong lỗi ghi file 2026-10-07

- NSIS installer nay tạo recovery directory cùng volume, sao lưu mọi file payload đã tồn tại và `Uninstall.exe`, đánh dấu file trước đó chưa tồn tại; nếu ghi payload/uninstaller thất bại thì phục hồi file cũ và xóa file mới. Nếu rollback không đầy đủ, giữ recovery directory và chỉ ra đường dẫn trong thông báo. Phạm vi hiện tại là lỗi ghi file có thể phục hồi; chưa bảo đảm phục hồi sau mất điện/process kill hoặc lỗi khi ghi registry/shortcut sau khi payload đã hoàn tất.
- Thêm test mode bị chặn khỏi production signing, chỉ cho output trong `%TEMP%`, cần fault injection tường minh và bỏ qua registry/shortcut. `scripts/test_commercial_installer_rollback.ps1` cố ý lỗi sau file payload thứ ba; test tự thoát và xác nhận file cũ cùng uninstaller được giữ nguyên, file mới bị gỡ, file unmanaged không đổi và recovery directory được dọn: `INSTALLER_ROLLBACK_TEST=PASS`.
- Build installer thường từ đúng publish `ASMILE-Commercial-SystemSettings-Maintenance-Fix-20261007`: NSIS `v3.13`, `COMMERCIAL_INSTALLER=PASS`, `PAYLOAD_FILE_COUNT=1855`, artifact `%TEMP%\ASMILE-Commercial-Installer-SystemSettings-Rollback-Final-20261007\ASMILE-BO-Setup-1.0.0.0.exe`, SHA-256 `27D8E6480B78FE6ED52ECAD338C9B83CE49A2016FBEE393ED7FCDA390D07EA11`. Chưa ký; đây là candidate kiểm thử, không phân phối thương mại.
- Cổng còn mở: rollback interruption/crash recovery, uninstall/upgrade trên máy sạch, elevated install/upgrade/rollback UAT, signature/private key, registry/shortcut failure handling và acceptance trên SQL Server/máy khách hàng. Test temp xác minh file transaction path, không thay cho các UAT này.

### Kiểm thử SQL backup bằng service identity trong thư mục cô lập 2026-10-07

- Sửa opt-in `SqlExpress_service_identity_can_write_and_validate_a_real_backup_when_opted_in`: không còn ghi thẳng vào thư mục backup dùng chung. Mỗi lượt dùng `.integration-test-<GUID>` bên dưới backup root hiện có, bảo vệ DACL riêng và chỉ cấp Modify cho identity đọc từ registry service `MSSQL$SQLEXPRESS`, FullControl cho user test/SYSTEM/Administrators; biến môi trường tùy chọn chỉ được dùng để assertion khớp identity. Cleanup chỉ xóa đúng thư mục GUID đó; thư mục backup gốc và ACL hiện hữu không bị sửa.
- Chạy trên `MSSQL$SQLEXPRESS` thật (`NT Service\MSSQL$SQLEXPRESS`) với database ngẫu nhiên `ASMILE_BO_BACKUP_TEST_*`: migration, SQL Server ghi backup qua service identity, kiểm tra file, `RESTORE VERIFYONLY` và đọc lại danh sách backup đều pass. Cả nhóm backup integration gồm LocalDB và service identity đạt `2/2`, `0` failed/skip, `1m09s` trên source test cuối; build sau Windows platform guard không có warning.
- Hậu kiểm bằng `sqlcmd` tới instance: `LeftoverTestDatabases=0`, `LeftoverTestDirectories=0`; DACL backup root vẫn còn rule `BUILTIN\Users:Write` như trước. Test chứng minh đường chạy SQL backup/verify với identity instance trong vùng cô lập; không chứng minh hay thay thế việc harden ACL thư mục live, restore drill dữ liệu khách hàng hoặc cấp quyền production.
- Sau các thay đổi hiện tại, toàn bộ Release unit suite đạt `994/994`, 0 failed/skip; Settings regression `63/63`; XAML encoding check pass. Release build của app hoàn tất không warning/error trong lượt unit suite.
- Recheck host phát hành: `ELEVATED_ADMIN=False`, `VALID_CODESIGNING_PRIVATE_KEY_COUNT=0`, SQL Express đang chạy. Vì vậy các cổng sửa ACL live và ký phát hành vẫn cần điều kiện ngoài workspace; không đánh dấu commercial-ready.

### Dọn phiên bản seed giả của module tích hợp 2026-10-07

- `SettingsRegistry.GetDefaultApps()` hiện không còn gán version giả, nhưng `AppModuleService.GetAppsAsync()` vẫn đọc `AppModules.Version`; các installation đã lưu từ seed cũ vì thế vẫn có thể hiển thị `1.0.0` như phiên bản sản phẩm. Thêm migration `20261007190000_ClearLegacyAppModuleSeedVersion`: chỉ xóa giá trị chính xác `1.0.0` trên 19 `AppKey` tích hợp đã biết. Module mở rộng/tùy chỉnh và phiên bản khác được giữ nguyên; rollback không tái tạo dữ liệu giả.
- Thêm kiểm thử SQL Server LocalDB tạo database GUID riêng, migrate đến migration trước đó, nạp một built-in `1.0.0`, một custom app `1.0.0` và một built-in `2.0.0`, sau đó migrate mới nhất. Assertion cả ba trường hợp đạt: `Passed 1/1`, `0` failed/skip, `46s`; database được xóa trong `finally`.
- Đây là data correction có phạm vi xác định và được kiểm tra trên LocalDB, chưa được áp dụng vào database khách hàng/production. Cần đưa migration vào quy trình deploy có preflight/backup, kiểm tra trạng thái migration sau triển khai và xác nhận UI không còn diễn giải `1.0.0` giả là phiên bản phát hành.

### Journal phục hồi crash cho installer Cài đặt hệ thống 2026-10-08

- Khép một gap release có thể sửa trong source: NSIS trước đây chỉ rollback khi lỗi ghi file được phát hiện trong cùng tiến trình; kill/crash giữa lúc thay payload có thể để bản cài nửa cũ/nửa mới. Installer nay tạo bản sao cùng volume cho toàn bộ payload + `Uninstall.exe`, export uninstall registry key, snapshot Start Menu shortcut, ghi recovery pointer và `transaction.active` trước lần ghi live đầu tiên. Lần setup sau chỉ chấp nhận journal là child `ns*.tmp` trực tiếp dưới install directory; nếu marker còn active thì restore payload, registry, shortcut rồi mới tiếp tục. Marker bị gỡ là commit point; journal tồn dư sau commit chỉ được cleanup, không rollback bản mới.
- Đường lỗi ghi tức thời cũng phục hồi metadata và shortcut trong cùng transaction. Nếu khôi phục không đủ, installer dừng và giữ journal để hỗ trợ; không tự xóa recovery data. `installer/README.md` ghi rõ commit semantics và giới hạn kiểm thử.
- `scripts/test_commercial_installer_rollback.ps1` chạy 3 installer test-only trên publish hiện hành: fault injection giữa payload, process kill sau khi `DisplayVersion`/`InstallLocation` đã đổi, rồi recovery-only launch. Kết quả `INSTALLER_ROLLBACK_TEST=PASS`: file có trước khôi phục đúng nội dung, file mới bị gỡ, uninstaller và unmanaged marker giữ nguyên, registry marker/version/path trở về trạng thái cũ và registry value mới được gỡ, recovery folder + hai unique HKCU test keys được dọn. Compiler NSIS `v3.13`; từng gói verifier `PAYLOAD_FILE_COUNT=1855`. Test này mô phỏng process termination, không mô phỏng power loss hoặc lỗi registry/Start Menu dưới elevation.
- Release self-contained `win-x64` được publish lại từ source hiện tại, verifier `37/37`, `PDB_COUNT=0`, `PUBLISH_FILE_COUNT=1855`, tree SHA-256 `15B057FDA9A28E9810D59D9AD6D68079D7B0DEBCD5B9513E358F4EBBABBC7D92`, EXE SHA-256 `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`. Installer candidate thường cuối cùng được build từ chính publish này, NSIS `v3.13`, verifier `COMMERCIAL_INSTALLER=PASS`, payload `1855/1855`, SHA-256 `5A1D429A779C7B54DDF4F6A3B42441D4AB07FCA869AB67BFC86258C28E483E8F`, ở `%TEMP%\ASMILE-Commercial-Installer-CrashRecovery-Final2-20261008\ASMILE-BO-Setup-1.0.0.0.exe`.
- Candidate vẫn `NotSigned` (`PRODUCTION_SIGNED=False`) và không được phân phối; host không elevated, chưa chạy elevated clean-machine install/upgrade/uninstall hoặc production registry/Start Menu recovery. Release gates còn mở: signing key/publisher, clean Windows UAT và hard-power-loss drill; backup ACL/service identity và deploy migration trên SQL khách hàng cũng còn cần nghiệm thu.

### Chặn nhầm phạm vi khi provision ACL backup 2026-10-08

- Rà soát lại script provision phát hiện trước đây chỉ yêu cầu đường dẫn tuyệt đối và từ chối UNC; chưa chặn volume root, mapped drive, hay đường dẫn đi qua junction/symlink. Vì script được chạy elevated và thay DACL, các trường hợp đó có thể sửa ACL sai phạm vi.
- `Resolve-AsmileBackupDirectoryPath` nay dùng cú pháp tương thích Windows PowerShell/.NET Framework, chỉ chấp nhận đường dẫn local dạng drive-qualified, từ chối volume root và mapped network drive, đồng thời kiểm tra từng ancestor hiện hữu để chặn reparse point trước khi tạo/thay DACL. Provisioner và hàm ACL đều gọi guard để kiểm tra lại ngay trước khi sửa.
- `scripts/test_backup_acl_provisioning.ps1` pass trên thư mục tạm: root/relative/UNC/junction bị từ chối, không tạo thư mục qua junction; policy DACL hiện hành vẫn pass (`3` broad write rules bị loại, Modify cấp cho 2 managed identities, read broad được giữ, chạy lần hai idempotent). Provisioner `-WhatIf` đọc đúng SQL service identity và application identity rồi báo rõ không đổi ACL. Không thay đổi ACL thư mục backup live; quyền `BUILTIN\Users:Write` ở live vẫn là release gate cần xử lý elevated cùng application identity đã chốt.

### Khóa liên-instance cho scheduler trong Cài đặt hệ thống 2026-10-08

- Xác minh source: `BackgroundSchedulerHostedService` poll queue từ process WPF; `QueueDueAsync` trước đây làm check-then-insert để tránh job trùng nhưng không có khóa cơ sở dữ liệu dùng chung giữa nhiều app/server instance. Vì vậy hai host có thể đồng thời nhìn thấy cùng schedule đến hạn; ngoài ra scheduler hiện không chạy khi process desktop đã đóng.
- Thêm transaction-owned `sp_getapplock` độc quyền theo database quanh khởi tạo default schedules và thao tác đọc lịch đến hạn/ghi execution/cập nhật lần chạy kế tiếp. Lock tự nhả khi transaction commit/rollback; nếu không lấy được thì thao tác fail rõ, không tạo execution ngoài transaction.
- LocalDB integration dùng hai `AppDbContext` và hai scheduler riêng trên cùng database: `Concurrent_scheduler_instances_queue_a_due_schedule_once` pass (`1/1`, 43s), tổng execution cho due schedule bằng đúng một. Unit `BackgroundSchedulerServiceTests`: `14/14` pass. `sqlcmd` hậu kiểm xác nhận không còn database test theo prefix `ASMILE_BO_SCHEDULER_QUEUE_`.
- Đây mới giải quyết race giữa các host đang chạy, không làm scheduler độc lập khỏi app desktop. Cần một worker/Windows Service hoặc Scheduled Task có định danh vận hành và quyền SQL/backup được provision rõ ràng trước khi có thể cam kết lịch chạy khi UI đóng; chưa tạo hay cài service trên máy người dùng.

### Claim execution an toàn giữa nhiều scheduler instance 2026-10-08

- Kiểm tra sâu `ExecuteNextQueuedAsync` phát hiện dù xếp lịch đã khóa, thao tác tiêu thụ execution vẫn chỉ đọc dòng `Queued` rồi gọi thực thi; hai process có thể cùng chọn một execution trước khi process nào ghi `Running`.
- Nay mỗi consumer mở transaction, lấy cùng transaction-owned `sp_getapplock`, chọn một execution queued có JobDefinition và ghi trạng thái `Running`/thời điểm bắt đầu trước khi commit; chỉ sau commit mới chạy job. Do đó process khác chỉ có thể nhận dòng kế tiếp, còn claim không bị giữ transaction mở trong suốt thời gian job chạy.
- SQL Server LocalDB integration mới `Concurrent_scheduler_instances_claim_a_queued_execution_once` chạy hai scheduler độc lập đồng thời trên cùng queued execution: đúng một lời gọi nhận claim và execution hoàn tất một lần (`1/1`, 46s). Unit `BackgroundSchedulerServiceTests` vẫn pass `14/14`.
- Worker tách riêng vẫn chưa được triển khai: WPF là process chủ của hosted services; ngoài scheduler, `AddInfrastructure` còn đăng ký email dispatch, process timeout và notification condition. Trước khi cài worker thương mại phải phân vai hosted services theo host mode, xử lý stale `Running`/lease sau crash, chốt danh tính Windows hoặc SQL và quyền backup, đồng thời kiểm tra không chạy nền trùng giữa desktop với worker.
- Bổ sung an toàn khôi phục: startup trước đây đánh dấu mọi `Running`/`Cancelling` quá 15 phút là lỗi, bất kể `TimeoutSeconds` trên job. Nay chỉ phục hồi khi đã quá cửa sổ lớn hơn giữa `staleAfter` và `TimeoutSeconds + 2 phút`; execution chưa hết timeout được giữ nguyên. Unit regression `Recovery_does_not_fail_a_running_execution_before_its_configured_timeout` cùng toàn bộ `BackgroundSchedulerServiceTests` đạt `15/15`.
- Trước khi tách worker, rà thêm `EmailDispatchService.DispatchPendingAsync`: các host từng cùng đọc `Pending/Failed` và gửi email trước khi cập nhật trạng thái. Bổ sung SQL Server session-owned application lock cho toàn batch; không mở transaction trong lúc gọi SMTP, timeout tranh khóa chỉ bỏ lượt hiện tại để poll sau, và connection luôn được đóng để giải phóng session lock. Integration LocalDB với hai `DbContext` và SMTP giả bị giữ handshake chứng minh chỉ có một kết nối/gửi cho một outbox row: `Concurrent_dispatchers_send_an_outbox_message_once` pass (`1/1`, 48s).
- Giới hạn delivery vẫn là at-least-once: nếu SMTP đã nhận thư nhưng process chết trước khi trạng thái `Sent` được lưu, lần poll sau có thể gửi lại; SMTP không cung cấp transaction chung với SQL để bảo đảm exactly-once. Trước khi chuyển toàn bộ hosted services sang worker vẫn cần kiểm tra race/idempotency riêng của process-timeout và notification processors.

### Chuyển runtime thương mại sang .NET 10 LTS 2026-10-08

- Toàn bộ solution được chuyển từ `net8.0`/`net8.0-windows` sang `net10.0`/`net10.0-windows`; EF Core, Microsoft.Extensions và test providers được căn phiên bản 10.0.12. Giữ Serilog ở các phiên bản hiện có đã khai báo attribution/license trong verifier. EF Core 10 yêu cầu SqlClient mới hơn nên ghim `Microsoft.Data.SqlClient` 6.1.6 cùng Azure.Identity 1.21.0 và Microsoft.Identity.Client 4.90.1.
- Lượt đầu chạy SQL integration bắt được EF10 từ chối migrate khi snapshot cũ ghi ProductVersion 8.0.7. EF tools 10.0.12 xác nhận không có thay đổi schema; migration rỗng `20261007190001_Ef10RuntimeAlignment` cập nhật mốc snapshot, xếp ngay sau migration hiện hữu `20261007190000_ClearLegacyAppModuleSeedVersion`. Sau đó settings LocalDB integration (`SqlServerSettingsIntegrationTests`) pass `2/2`, gồm migrate mới nhất và nâng từ migration trước.
- Xác minh hiện tại: unit suite Release pass `995/995`; settings LocalDB integration pass `2/2`; ba integration cạnh tranh scheduler/outbox pass `3/3`; `dotnet list package --vulnerable --include-transitive` không tìm thấy package vulnerable. EF tools 10.0.12 báo không còn pending model changes.
- Full Release build đã compile App/Infrastructure/Integration/E2E/Unit nhưng kết thúc lỗi `MSB3491` khi ghi file list của Unit do ổ D hết dung lượng; đây là lỗi ghi artifact, chưa phải compile error. Sau đó dọn riêng output Debug có thể tái tạo, không xóa dữ liệu hoặc Release output, giải phóng dung lượng và tạo thành công self-contained `win-x64` publish trên ổ E. Commercial verifier: `COMMERCIAL_PACKAGE=PASS`, `CHECK_COUNT=38`, `PDB_COUNT=0`, `PUBLISH_FILE_COUNT=1794`, tree SHA-256 `B90FD7BAA86D2A1E99A2A5EB0012D0D8F9CF236012AA21614EF1149CD7ABF848`, EXE SHA-256 `E3E82AC7C6DF6EEE7F3EB8D826350FB0A5F8BBCF061F7F83627CEE8187ECF2C1`; artifact tại `E:\ASMILE-BO-Commercial-Net10-20261008\publish-verified`.
- Installer NSIS `v3.13` được tạo từ đúng publish trên, sử dụng compiler đầy đủ trong thư mục `Bin` và temp trên ổ E. `INSTALLER_BUILD=PASS`, `COMMERCIAL_INSTALLER=PASS`, payload `1794/1794`, SHA-256 `2B34F69ED8F28B0AC0AF79F6D502662CFF6E00A86025C83EA7867DE6391DC992`, tại `E:\ASMILE-BO-Commercial-Net10-20261008\installer-unsigned-retry\ASMILE-BO-Setup-1.0.0.0.exe`; notice đi kèm. Ứng viên đầu dùng launcher NSIS nhỏ đã lỗi memory-map; đổi sang compiler `Bin` và temp E thì build/verifier pass.
- Recheck source-current bằng artifact sạch trên ổ E: build Integration Release pass; settings LocalDB chạy riêng `2/2` pass; EF CLI `10.0.12` báo `No changes have been made to the model since the last migration`. Full Integration ban đầu `14/43` pass, `29/43` fail với `PendingModelChangesWarning`; riêng SQL collection pass `29/29`. Nguyên nhân là các test collection khác chạy đồng thời với SQL/EF suite và làm kết quả bất ổn. Đã serialize toàn Integration assembly trong `SqlServerIntegrationCollection.cs`; chạy lại full suite source-current pass `43/43`, `0` failed/skip, `10m54s`. Đây là thay đổi test-only, không tác động runtime ứng dụng.
- Full Unit Release được build source-current vào artifact path riêng; lần chạy đầu có 7 lỗi test chỉ vì test source-inspection không tìm ra root ngoài `bin`, không phải assertion sản phẩm. Sau khi đặt `ASMILE_BO_SOLUTION_ROOT` đúng, toàn suite pass `995/995`, `0` failed/skip; XAML encoding check pass. Restore/build còn cảnh báo `NU1510` cho hai package explicit `System.Formats.Asn1` và `System.Text.Json` ở App/Infrastructure; cần đánh giá gỡ dependency trực tiếp an toàn ở lần hardening riêng.
- Gói và installer đã verify nhưng cả app/setup đều `NotSigned`, `PRODUCTION_SIGNED=False`; không phân phối khách hàng. Còn cần xử lý full Integration bất nhất, chạy E2E sạch bằng source/artifact mới, ký bằng certificate production, clean-machine install/upgrade/uninstall, và xác nhận cấu hình VFC/backup/worker cùng migration trên UAT/SQL Server đích. `dotnet ef` local tool mặc định ở repo là `8.0.7` nên các phép chẩn đoán EF cần gọi rõ tool `10.0.12` và connection LocalDB cô lập.
- Rủi ro thời hạn: .NET 8 LTS kết thúc hỗ trợ ngày 10/11/2026; .NET 10 LTS hiện là đích hỗ trợ dài hạn hơn. Cổng thương mại còn lại gồm full SQL integration/E2E, clean-machine install/upgrade, signing bằng certificate production, và kiểm chứng các cấu hình VFC/backup/worker trên môi trường đích.
