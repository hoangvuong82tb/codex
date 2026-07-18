# TỪ ĐIỂN DỮ LIỆU VÀ MÔ HÌNH DATABASE LOGIC FRONT OFFICE ASMILE-BO

## 1. Executive Summary

Tài liệu chuyển Domain Model Front Office đã duyệt thành mô hình dữ liệu logic đủ chi tiết cho bước thiết kế database vật lý, Entity/EF Core configuration, migration, CQRS và test dữ liệu sau này. Tài liệu không chứa SQL, code, migration hoặc thay đổi database.

Phương án nền được chọn là database logic theo 12 Bounded Context trong một Modular Monolith, ownership một chiều, schema logic theo context, khóa kỹ thuật toàn cục, mã hiển thị tách biệt, `PropertyBusinessDate`, optimistic concurrency, ledger bất biến, Outbox/Inbox bền vững và projection tách khỏi write model.

Các quyết định logic quan trọng:

- Aggregate Root và Entity có lifecycle/collection độc lập mới có bảng; Value Object nhỏ được lưu thành owned columns trong bảng cha.
- Front Office chỉ giữ ID và snapshot tối thiểu của Guest/Property/Room/RoomType/Company/Invoice/Journal/Notification; module sở hữu master vẫn là nguồn sự thật.
- Không có FK vật lý hoặc cascade delete xuyên module; không join database module khác để xác nhận command.
- Financial posting, event, audit và integration attempt không được sửa/xóa vật lý trong thời hạn lưu trữ.
- Read Model là projection có thể rebuild, không được dùng làm nguồn ghi hoặc là bằng chứng duy nhất cho invariant quan trọng.

### 1.1. MÂU THUẪN CẦN XÁC NHẬN

| Mã | Nội dung mâu thuẫn | Tài liệu liên quan | Ảnh hưởng | Phương án đề xuất | Ưu tiên |
|---|---|---|---|---|---|
| MC-001 | `Room` và `RoomType` được Business Dictionary đánh dấu là Aggregate của Rooms Availability, trong khi Domain Model coi Room/RoomType là master/reference và `RoomAvailability` mới sở hữu sellable inventory. | Business Dictionary mục 1.4; Domain Model mục 3.5, 4.2 và Final Decision | Nếu tạo lại bảng master trong Front Office sẽ có hai owner, đồng bộ sai và shared data. | Front Office không tạo bảng master Room/RoomType; chỉ giữ `MaPhong`, `MaHangPhong` và snapshot cần thiết. Đề nghị sửa phân loại ở Business Dictionary trong một bước governance riêng. | Critical |
| MC-002 | Tài liệu kiến trúc giai đoạn khái niệm liệt kê `Invoice`, `Notification`, `Guest` trong phạm vi phối hợp Front Office, còn Domain Model chốt chúng thuộc Accounting, Notification và CRM. | Architecture phần 2/3; Domain Model mục 1, 3, 13 | Có nguy cơ tạo Aggregate/bảng duplicate trong schema Front Office. | Lấy Domain Model chi tiết làm ownership hiện hành; chỉ lưu external reference/snapshot và Integration Event. Cần xác nhận đây là refinement, không phải thay đổi scope. | High |
| MC-003 | Business Dictionary dành trước History/Audit/Archive table cho mọi Aggregate, trong khi Prompt 04 và Domain Model yêu cầu không tạo history riêng máy móc. | Business Dictionary mục 2; Prompt 04 phần Audit/Archive; Domain Model mục 4/20 | Tăng dữ liệu, trùng Audit Store, khó xác định nguồn sự thật. | Chỉ tạo history chuyên biệt khi là business entity append-only; dùng Audit Store chung cho before/after và archive theo policy. | High |
| MC-004 | Tên `RoomBlock` có thể được hiểu là khối phòng đoàn hoặc khóa phòng kỹ thuật. | Cả ba tài liệu | FK, API và report có thể trỏ nhầm loại dữ liệu. | Chốt `KhoiPhongDoan`/`RoomBlock` cho Group; `KhoaVanHanhPhong`/`RoomOperationalBlock` cho Maintenance. | Critical |

Các phần còn lại áp dụng phương án đề xuất an toàn ở trên nhưng không coi các mâu thuẫn đã được phê duyệt. Chúng vẫn xuất hiện trong Decision Log/Open Questions.

---

## 2. Design Principles

1. **Owner duy nhất:** mỗi bảng write-model có một Bounded Context và một module được ghi.
2. **Consistency theo Aggregate:** transaction và FK vật lý tập trung trong Aggregate; cross-Aggregate dùng orchestration và event khi không cần strong consistency.
3. **Reference thay shared table:** cross-context/module dùng ID bất biến, snapshot có provenance và contract version.
4. **Identity tách display code:** GUID/khóa kỹ thuật không mang nghĩa; số xác nhận/số chứng từ unique trong scope riêng.
5. **Property scope bắt buộc:** mọi giao dịch Front Office mang `MaKhachSan`; global identity không thay Property authorization.
6. **Business Date là dữ liệu:** `NgayNghiepVu` không suy ra từ timestamp; timestamp hệ thống lưu UTC.
7. **Money đầy đủ:** amount dùng decimal cùng `MaTienTe`; không dùng `money`, `float`, `double` hoặc decimal đơn lẻ.
8. **Append-only cho sự thật bất biến:** Folio Entry, audit, event, attempt và checkpoint correction bằng reversal/new fact.
9. **Idempotency bền vững:** command/inbound/provider callback có key và payload hash lưu database, không chỉ cache/memory.
10. **Concurrency hiển nhiên:** Root mutable có `PhienBanDuLieu`; hot bucket/lease có uniqueness và transaction strategy riêng.
11. **PII tối thiểu:** mã hóa/masking theo field; event, log, projection không sao chép dữ liệu nhạy cảm nếu không cần.
12. **Projection có thể bỏ và rebuild:** lưu freshness/checkpoint; không cascade ngược vào write model.
13. **Không xóa máy móc:** close/deactivate/reversal/archive/anonymize được chọn theo semantics; không mặc định `DaXoa`.
14. **Không JSON để né relational design:** JSON chỉ dành payload versioned/opaque có lý do integration/audit rõ.
15. **Tên chuẩn:** Domain/code tiếng Anh; bảng/cột SQL tiếng Việt không dấu PascalCase đúng Business Dictionary.

### 2.1. Phân loại phạm vi persistence

| Phân loại | Quyết định | Lý do/tiêu chí |
|---|---|---|
| Aggregate Root cần persistence | Tất cả Root trong Domain Model Catalog và `PropertyBusinessDate` | Có identity, lifecycle, invariant và concurrency boundary độc lập |
| Entity nội bộ cần bảng | Collection có identity/lifecycle, số lượng thay đổi hoặc cần truy vết riêng | Tránh row phình lớn và bảo vệ uniqueness/FK trong Aggregate |
| Value Object lưu bảng cha | Money, Period, status snapshot, source reference đơn, reason, approval reference | Không có lifecycle; tách bảng sẽ tạo CRUD vô nghĩa |
| Value Object nên tách bảng | Chỉ khi là collection có số lượng không giới hạn hoặc cần truy vấn/retention riêng; khi đó phải xem lại nó có thực chất là Entity | Không mặc định tách chỉ vì nhiều cột |
| Lookup | Danh mục quản trị có mã/effective range/Property scope | Thay đổi không cần release code |
| Enum lưu mã | State/type hữu hạn, ổn định, không khác Property | Database lưu mã có constraint/governance, không string tự do |
| Read Model | 13 projection vận hành ở mục 16 | Tối ưu query, eventual consistency, rebuild được |
| Projection table | Khi latency/query volume không phù hợp view trực tiếp | Không owner invariant và không nhận command write |
| Audit data | Audit Store logic dùng chung, partition theo module/Property/time | Tránh history table cho mọi bảng |
| Outbox/Inbox | Bảng kỹ thuật bền vững theo module/schema integration | Atomic với local transaction/dedupe consumer |
| Integration data | Job, Attempt, Exception, ExternalReference, Idempotency, Replay | Cần retry, lease, reconciliation và manual control |
| Dữ liệu module khác | Guest/Company/Property/Room/RoomType/Invoice/Journal/AR/Notification | Chỉ ID + snapshot tối thiểu; không duplicate master |
| Không cần bảng | Action như CheckIn/Checkout/RoomMove, policy/service/specification, dashboard widget | Là behavior/logic hoặc projection, không phải persistent concept độc lập |

---

## 3. Database Ownership Matrix

| Bảng/nguồn dữ liệu | Domain tiếng Anh | Tên nghiệp vụ | Bounded Context | Aggregate sở hữu | Module sở hữu | Module được đọc | Module được ghi |
|---|---|---|---|---|---|---|---|
| HoSoKhachHang (external) | GuestProfile | Hồ sơ khách hàng | CRM | GuestProfile | CRM | Front Office, POS, Banquet, Reporting | Chỉ CRM |
| KhachSan (external) | Property | Cơ sở khách sạn | Property Master | Property | Core/Administration | Tất cả module | Chỉ Core/Administration |
| Phong (external) | Room | Phòng | Room Master | Room | Property/Room Master | Front Office, HK, Maintenance | Chỉ Property/Room Master |
| HangPhong (external) | RoomType | Hạng phòng | Room Master | RoomType | Property/Room Master | Reservation, Rooms Availability, Reporting | Chỉ Property/Room Master |
| DatPhong | Reservation | Đặt phòng | Reservation | Reservation | Front Office.Reservation | CRM, Reporting, Notification | Chỉ Front Office.Reservation |
| LuuTru | Stay | Lưu trú | Stay Operations | Stay | Front Office.Stay | CRM, POS, Banquet, Reporting | Chỉ Front Office.Stay |
| PhanPhong | RoomAssignment | Phân phòng | Stay Operations | RoomAssignment | Front Office.Stay | HK, Maintenance, POS, Reporting | Chỉ Front Office.Stay |
| SoGiaoDichKhach | Folio | Sổ giao dịch khách | Folio | Folio | Front Office.Folio | Cashiering, Accounting, POS, Reporting | Chỉ Front Office.Folio |
| ThanhToan | Payment | Thanh toán | Cashiering | Payment | Front Office.Cashiering | Folio, Accounting, Reporting | Chỉ Front Office.Cashiering |
| DatCoc | Deposit | Đặt cọc | Cashiering | Deposit | Front Office.Cashiering | Reservation, Folio, Accounting | Chỉ Front Office.Cashiering |
| TaiKhoanCongNo (external) | ARAccount | Tài khoản công nợ | Accounting | ARAccount | Accounting | Front Office, CRM, Reporting | Chỉ Accounting |
| HoaDon (external) | Invoice | Hóa đơn | Accounting | Invoice | Accounting | Front Office, CRM, Reporting | Chỉ Accounting |
| VanHanhBuongPhong | RoomOperation | Vận hành buồng phòng | Housekeeping | RoomOperation | Front Office.Housekeeping | Stay, Rooms, Maintenance, Dashboard | Chỉ Housekeeping |
| HoSoBaoTri | MaintenanceCase | Hồ sơ bảo trì | Maintenance | MaintenanceCase | Front Office.Maintenance | HK, Stay, Rooms, Purchasing | Chỉ Maintenance |
| PhienKiemToanDem | NightAuditRun | Phiên kiểm toán đêm | Night Audit | NightAuditRun | Front Office.NightAudit | Accounting, Reporting, Operations | Chỉ Night Audit |
| CongViecVanHanh | OperationalTask | Công việc vận hành | Operational Task | OperationalTask | Front Office.Task | Tất cả module theo scope | Chỉ Task Context; module khác gửi request |
| YeuCauThongBao (external) | NotificationRequest | Yêu cầu thông báo | Notification | NotificationRequest | Notification | Front Office/CRM xem trạng thái | Chỉ Notification |
| NhatKyKiemToan | AuditRecord | Bản ghi kiểm toán | System/Audit | AuditRecord | Platform Audit | Security/Compliance/owner module | Chỉ Audit writer contract |
| CongViecTichHop | IntegrationJob | Công việc tích hợp | Device & Integration | IntegrationJob | Front Office.Integration | Domain owner/Operations | Chỉ Integration |
| ThongDiepOutbox | OutboxMessage | Thông điệp đi | Context kỹ thuật cục bộ | Local transaction | Module phát sinh | Integration dispatcher | Chỉ module phát sinh/dispatcher state |
| ThongDiepInbox | InboxMessage | Thông điệp đến | Context kỹ thuật cục bộ | Consumer operation | Module nhận | Integration operations | Chỉ module nhận |

Mặc định không tạo FK vật lý cho các dòng đánh dấu `(external)`. ID và snapshot được validate qua Application Contract; dữ liệu stale tạo warning/reconciliation thay vì cross-module update.

## 4. Logical Table Catalog

### 4.1. Reservation

| Bảng SQL | Domain | Tên nghiệp vụ | Mục đích | Module/Aggregate sở hữu | Loại bảng | Bảng cha | Lifecycle | Volume | Tăng trưởng | Retention | PII | Tài chính | Mã hóa | Archive | Partition tương lai | Append-only | Sửa/xóa vật lý |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| DatPhong | Reservation | Đặt phòng | Lưu đặt phòng theo boundary Reservation | Front Office.Reservation / Reservation | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| KhachDatPhong | ReservationGuest | Khách trong đặt phòng | Lưu khách trong đặt phòng theo boundary Reservation | Front Office.Reservation / Reservation | Child Entity | DatPhong | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| NhuCauHangPhong | ReservationRoomDemand | Nhu cầu hạng phòng | Lưu nhu cầu hạng phòng theo boundary Reservation | Front Office.Reservation / Reservation | Child Entity | DatPhong | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| AnhChupGiaDatPhong | ReservationRateSnapshot | Ảnh chụp giá đặt phòng | Lưu ảnh chụp giá đặt phòng theo boundary Reservation | Front Office.Reservation / Reservation | Child Entity | DatPhong | Tạo mới bất biến; không sửa nội dung | Trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |
| ThamChieuBaoDamDatPhong | ReservationGuaranteeRef | Tham chiếu bảo đảm | Lưu tham chiếu bảo đảm theo boundary Reservation | Front Office.Reservation / Reservation | Child Entity | DatPhong | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |
| LienHeDatPhong | ReservationContact | Đầu mối liên hệ | Lưu đầu mối liên hệ theo boundary Reservation | Front Office.Reservation / Reservation | Child Entity | DatPhong | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| GhiChuDatPhong | ReservationNote | Ghi chú đặt phòng | Lưu ghi chú đặt phòng theo boundary Reservation | Front Office.Reservation / Reservation | Child Entity | DatPhong | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| YeuCauUuTienDatPhong | ReservationPreference | Yêu cầu ưu tiên | Lưu yêu cầu ưu tiên theo boundary Reservation | Front Office.Reservation / Reservation | Child Entity | DatPhong | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| LichSuThayDoiDatPhong | ReservationChangeRecord | Lịch sử thay đổi đặt phòng | Lưu lịch sử thay đổi đặt phòng theo boundary Reservation | Front Office.Reservation / Reservation | History | DatPhong | Tạo mới bất biến; không sửa nội dung | Cao | Cao | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |
| ChoPhong | WaitlistEntry | Yêu cầu chờ phòng | Lưu yêu cầu chờ phòng theo boundary WaitlistEntry | Front Office.Reservation / WaitlistEntry | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| LanMoiChoPhong | WaitlistOfferAttempt | Lần mời chờ phòng | Lưu lần mời chờ phòng theo boundary WaitlistEntry | Front Office.Reservation / WaitlistEntry | Child Entity | ChoPhong | Tạo mới bất biến; không sửa nội dung | Trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |

### 4.2. Stay Operations

| Bảng SQL | Domain | Tên nghiệp vụ | Mục đích | Module/Aggregate sở hữu | Loại bảng | Bảng cha | Lifecycle | Volume | Tăng trưởng | Retention | PII | Tài chính | Mã hóa | Archive | Partition tương lai | Append-only | Sửa/xóa vật lý |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| LuuTru | Stay | Lưu trú | Lưu lưu trú theo boundary Stay | Front Office.Stay Operations / Stay | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| KhachLuuTru | StayGuest | Khách tham gia lưu trú | Lưu khách tham gia lưu trú theo boundary Stay | Front Office.Stay Operations / Stay | Child Entity | LuuTru | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| GiaHanLuuTru | StayExtension | Lần gia hạn lưu trú | Lưu lần gia hạn lưu trú theo boundary Stay | Front Office.Stay Operations / Stay | History | LuuTru | Tạo mới bất biến; không sửa nội dung | Cao | Cao | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |
| HanCheLuuTru | StayRestriction | Hạn chế lưu trú | Lưu hạn chế lưu trú theo boundary Stay | Front Office.Stay Operations / Stay | Child Entity | LuuTru | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| QuyenKyPhong | StayPostingPermission | Quyền ký phòng | Lưu quyền ký phòng theo boundary Stay | Front Office.Stay Operations / Stay | Child Entity | LuuTru | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |
| ThietLapRiengTuLuuTru | StayPrivacySetting | Thiết lập riêng tư | Lưu thiết lập riêng tư theo boundary Stay | Front Office.Stay Operations / Stay | Child Entity | LuuTru | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| PhanPhong | RoomAssignment | Phân phòng | Lưu phân phòng theo boundary RoomAssignment | Front Office.Stay Operations / RoomAssignment | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| PhanDoanPhanPhong | AssignmentSegment | Phân đoạn phân phòng | Lưu phân đoạn phân phòng theo boundary RoomAssignment | Front Office.Stay Operations / RoomAssignment | Child Entity | PhanPhong | Tạo mới bất biến; không sửa nội dung | Trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |
| QuyenMoKhoaPhong | KeyCredential | Quyền mở khóa phòng | Lưu quyền mở khóa phòng theo boundary KeyCredential | Front Office.Stay Operations / KeyCredential | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| LanCapKhoa | CredentialAttempt | Lần thao tác khóa | Lưu lần thao tác khóa theo boundary KeyCredential | Front Office.Stay Operations / KeyCredential | Integration | QuyenMoKhoaPhong | Tạo mới bất biến; không sửa nội dung | Cao | Rất cao | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Có | Ứng viên theo Property/BusinessDate/time | Có | Không; correction/close/archive |
| ChuKyKhachLuuTru | GuestSignature | Chữ ký khách lưu trú | Lưu chữ ký khách lưu trú theo boundary Stay | Front Office.Stay Operations / Stay | Child Entity | LuuTru | Tạo mới bất biến; không sửa nội dung | Trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |

### 4.3. Rooms Availability

| Bảng SQL | Domain | Tên nghiệp vụ | Mục đích | Module/Aggregate sở hữu | Loại bảng | Bảng cha | Lifecycle | Volume | Tăng trưởng | Retention | PII | Tài chính | Mã hóa | Archive | Partition tương lai | Append-only | Sửa/xóa vật lý |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| TonPhongKhaDung | RoomAvailability | Tồn phòng khả dụng | Lưu tồn phòng khả dụng theo boundary RoomAvailability | Front Office.Rooms Availability / RoomAvailability | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| GiuTonPhong | AvailabilityHold | Giữ tồn phòng | Lưu giữ tồn phòng theo boundary RoomAvailability | Front Office.Rooms Availability / RoomAvailability | Child Entity | TonPhongKhaDung | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| PhanBoTonPhong | AvailabilityAllocation | Phân bổ tồn phòng | Lưu phân bổ tồn phòng theo boundary RoomAvailability | Front Office.Rooms Availability / RoomAvailability | Child Entity | TonPhongKhaDung | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| KiemSoatTonPhong | RoomInventoryControl | Kiểm soát tồn phòng | Lưu kiểm soát tồn phòng theo boundary RoomInventoryControl | Front Office.Rooms Availability / RoomInventoryControl | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| HanCheBanPhong | InventoryRestriction | Hạn chế bán phòng | Lưu hạn chế bán phòng theo boundary RoomInventoryControl | Front Office.Rooms Availability / RoomInventoryControl | Child Entity | KiemSoatTonPhong | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |

### 4.4. Folio

| Bảng SQL | Domain | Tên nghiệp vụ | Mục đích | Module/Aggregate sở hữu | Loại bảng | Bảng cha | Lifecycle | Volume | Tăng trưởng | Retention | PII | Tài chính | Mã hóa | Archive | Partition tương lai | Append-only | Sửa/xóa vật lý |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| SoGiaoDichKhach | Folio | Sổ giao dịch khách | Lưu sổ giao dịch khách theo boundary Folio | Front Office.Folio / Folio | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |
| NganSoGiaoDich | FolioWindow | Ngăn sổ giao dịch | Lưu ngăn sổ giao dịch theo boundary FolioWindow | Front Office.Folio / FolioWindow | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |
| ButToanSoGiaoDich | FolioEntry | Bút toán sổ giao dịch | Lưu bút toán sổ giao dịch theo boundary FolioWindow | Front Office.Folio / FolioWindow | Ledger | NganSoGiaoDich | Tạo mới bất biến; không sửa nội dung | Cao | Rất cao | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Có | Ứng viên theo Property/BusinessDate/time | Có | Không; correction/close/archive |
| ThueButToanSoGiaoDich | FolioEntryTax | Thuế bút toán | Lưu thuế bút toán theo boundary FolioWindow | Front Office.Folio / FolioWindow | Ledger | ButToanSoGiaoDich | Tạo mới bất biến; không sửa nội dung | Cao | Rất cao | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Có | Ứng viên theo Property/BusinessDate/time | Có | Không; correction/close/archive |
| LienKetChuyenButToan | FolioTransferLink | Liên kết chuyển bút toán | Lưu liên kết chuyển bút toán theo boundary FolioWindow | Front Office.Folio / FolioWindow | Ledger | ButToanSoGiaoDich | Tạo mới bất biến; không sửa nội dung | Cao | Rất cao | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Có | Ứng viên theo Property/BusinessDate/time | Có | Không; correction/close/archive |
| LienKetDieuChinhButToan | FolioAdjustmentLink | Liên kết điều chỉnh | Lưu liên kết điều chỉnh theo boundary FolioWindow | Front Office.Folio / FolioWindow | Ledger | ButToanSoGiaoDich | Tạo mới bất biến; không sửa nội dung | Cao | Rất cao | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Có | Ứng viên theo Property/BusinessDate/time | Có | Không; correction/close/archive |
| ChiDanDinhTuyen | RoutingInstruction | Chỉ dẫn định tuyến | Lưu chỉ dẫn định tuyến theo boundary RoutingInstruction | Front Office.Folio / RoutingInstruction | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |
| DieuKienDinhTuyen | RoutingCondition | Điều kiện định tuyến | Lưu điều kiện định tuyến theo boundary RoutingInstruction | Front Office.Folio / RoutingInstruction | Child Entity | ChiDanDinhTuyen | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |

### 4.5. Cashiering

| Bảng SQL | Domain | Tên nghiệp vụ | Mục đích | Module/Aggregate sở hữu | Loại bảng | Bảng cha | Lifecycle | Volume | Tăng trưởng | Retention | PII | Tài chính | Mã hóa | Archive | Partition tương lai | Append-only | Sửa/xóa vật lý |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| ThanhToan | Payment | Thanh toán | Lưu thanh toán theo boundary Payment | Front Office.Cashiering / Payment | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |
| LanThuThanhToan | PaymentAttempt | Lần thử thanh toán | Lưu lần thử thanh toán theo boundary Payment | Front Office.Cashiering / Payment | Ledger | ThanhToan | Tạo mới bất biến; không sửa nội dung | Cao | Rất cao | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Có | Ứng viên theo Property/BusinessDate/time | Có | Không; correction/close/archive |
| DatCoc | Deposit | Đặt cọc | Lưu đặt cọc theo boundary Deposit | Front Office.Cashiering / Deposit | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |
| PhanBoDatCoc | DepositAllocation | Phân bổ đặt cọc | Lưu phân bổ đặt cọc theo boundary Deposit | Front Office.Cashiering / Deposit | Ledger | DatCoc | Tạo mới bất biến; không sửa nội dung | Cao | Rất cao | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Có | Ứng viên theo Property/BusinessDate/time | Có | Không; correction/close/archive |
| YeuCauHoanTien | RefundRequest | Yêu cầu hoàn tiền | Lưu yêu cầu hoàn tiền theo boundary RefundRequest | Front Office.Cashiering / RefundRequest | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |
| BuocPheDuyetHoanTien | RefundApprovalStep | Bước duyệt hoàn tiền | Lưu bước duyệt hoàn tiền theo boundary RefundRequest | Front Office.Cashiering / RefundRequest | History | YeuCauHoanTien | Tạo mới bất biến; không sửa nội dung | Cao | Cao | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |
| LanThucHienHoanTien | RefundExecutionAttempt | Lần thực hiện hoàn tiền | Lưu lần thực hiện hoàn tiền theo boundary RefundRequest | Front Office.Cashiering / RefundRequest | Ledger | YeuCauHoanTien | Tạo mới bất biến; không sửa nội dung | Cao | Rất cao | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Có | Ứng viên theo Property/BusinessDate/time | Có | Không; correction/close/archive |
| CaThuNgan | CashierShift | Ca thu ngân | Lưu ca thu ngân theo boundary CashierShift | Front Office.Cashiering / CashierShift | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |
| TongHopPhuongThucThu | CashierTenderSummary | Tổng hợp phương thức thu | Lưu tổng hợp phương thức thu theo boundary CashierShift | Front Office.Cashiering / CashierShift | Child Entity | CaThuNgan | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |
| ChenhLechCaThuNgan | CashierVariance | Chênh lệch ca thu ngân | Lưu chênh lệch ca thu ngân theo boundary CashierShift | Front Office.Cashiering / CashierShift | History | CaThuNgan | Tạo mới bất biến; không sửa nội dung | Cao | Cao | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |

### 4.6. Housekeeping

| Bảng SQL | Domain | Tên nghiệp vụ | Mục đích | Module/Aggregate sở hữu | Loại bảng | Bảng cha | Lifecycle | Volume | Tăng trưởng | Retention | PII | Tài chính | Mã hóa | Archive | Partition tương lai | Append-only | Sửa/xóa vật lý |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| VanHanhBuongPhong | RoomOperation | Vận hành buồng phòng | Lưu vận hành buồng phòng theo boundary RoomOperation | Front Office.Housekeeping / RoomOperation | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| LichSuTrangThaiPhong | RoomStatusTransition | Lịch sử trạng thái phòng | Lưu lịch sử trạng thái phòng theo boundary RoomOperation | Front Office.Housekeeping / RoomOperation | History | VanHanhBuongPhong | Tạo mới bất biến; không sửa nội dung | Cao | Cao | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |
| SaiLechTrangThaiPhong | RoomDiscrepancy | Sai lệch trạng thái phòng | Lưu sai lệch trạng thái phòng theo boundary RoomOperation | Front Office.Housekeeping / RoomOperation | Child Entity | VanHanhBuongPhong | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| CongViecBuongPhong | HousekeepingTask | Công việc buồng phòng | Lưu công việc buồng phòng theo boundary HousekeepingTask | Front Office.Housekeeping / HousekeepingTask | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| PhienLamBuong | CleaningWorkSegment | Phiên làm buồng | Lưu phiên làm buồng theo boundary HousekeepingTask | Front Office.Housekeeping / HousekeepingTask | History | CongViecBuongPhong | Tạo mới bất biến; không sửa nội dung | Cao | Cao | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |
| MucKiemTraVeSinh | CleaningChecklistItem | Mục kiểm tra vệ sinh | Lưu mục kiểm tra vệ sinh theo boundary HousekeepingTask | Front Office.Housekeeping / HousekeepingTask | Child Entity | CongViecBuongPhong | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| KiemTraPhong | RoomInspection | Kiểm tra phòng | Lưu kiểm tra phòng theo boundary RoomInspection | Front Office.Housekeeping / RoomInspection | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| PhatHienKiemPhong | InspectionFinding | Phát hiện kiểm phòng | Lưu phát hiện kiểm phòng theo boundary RoomInspection | Front Office.Housekeeping / RoomInspection | Child Entity | KiemTraPhong | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |

### 4.7. Maintenance

| Bảng SQL | Domain | Tên nghiệp vụ | Mục đích | Module/Aggregate sở hữu | Loại bảng | Bảng cha | Lifecycle | Volume | Tăng trưởng | Retention | PII | Tài chính | Mã hóa | Archive | Partition tương lai | Append-only | Sửa/xóa vật lý |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| HoSoBaoTri | MaintenanceCase | Hồ sơ bảo trì | Lưu hồ sơ bảo trì theo boundary MaintenanceCase | Front Office.Maintenance / MaintenanceCase | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| NhatKyCongViecBaoTri | MaintenanceWorkLog | Nhật ký công việc bảo trì | Lưu nhật ký công việc bảo trì theo boundary MaintenanceCase | Front Office.Maintenance / MaintenanceCase | History | HoSoBaoTri | Tạo mới bất biến; không sửa nội dung | Cao | Cao | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |
| ThamChieuVatTuBaoTri | MaintenancePartRef | Tham chiếu vật tư | Lưu tham chiếu vật tư theo boundary MaintenanceCase | Front Office.Maintenance / MaintenanceCase | Child Entity | HoSoBaoTri | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |
| KhoaVanHanhPhong | RoomOperationalBlock | Khóa vận hành phòng | Lưu khóa vận hành phòng theo boundary RoomOperationalBlock | Front Office.Maintenance / RoomOperationalBlock | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| GiaHanKhoaPhong | BlockExtension | Lần gia hạn khóa phòng | Lưu lần gia hạn khóa phòng theo boundary RoomOperationalBlock | Front Office.Maintenance / RoomOperationalBlock | History | KhoaVanHanhPhong | Tạo mới bất biến; không sửa nội dung | Cao | Cao | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |

### 4.8. Guest Services

| Bảng SQL | Domain | Tên nghiệp vụ | Mục đích | Module/Aggregate sở hữu | Loại bảng | Bảng cha | Lifecycle | Volume | Tăng trưởng | Retention | PII | Tài chính | Mã hóa | Archive | Partition tương lai | Append-only | Sửa/xóa vật lý |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| YeuCauDichVu | ServiceRequest | Yêu cầu dịch vụ | Lưu yêu cầu dịch vụ theo boundary ServiceRequest | Front Office.Guest Services / ServiceRequest | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Có | Có | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |
| HangMucYeuCauDichVu | ServiceRequestItem | Hạng mục dịch vụ | Lưu hạng mục dịch vụ theo boundary ServiceRequest | Front Office.Guest Services / ServiceRequest | Child Entity | YeuCauDichVu | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |
| BangChungHoanThanhDichVu | ServiceCompletionEvidence | Bằng chứng hoàn thành | Lưu bằng chứng hoàn thành theo boundary ServiceRequest | Front Office.Guest Services / ServiceRequest | History | YeuCauDichVu | Tạo mới bất biến; không sửa nội dung | Cao | Cao | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |
| CuocGoiBaoThuc | WakeUpCall | Cuộc gọi báo thức | Lưu cuộc gọi báo thức theo boundary WakeUpCall | Front Office.Guest Services / WakeUpCall | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| LanGoiBaoThuc | WakeUpAttempt | Lần gọi báo thức | Lưu lần gọi báo thức theo boundary WakeUpCall | Front Office.Guest Services / WakeUpCall | History | CuocGoiBaoThuc | Tạo mới bất biến; không sửa nội dung | Cao | Cao | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |
| HoSoDoThatLac | LostFoundCase | Hồ sơ đồ thất lạc | Lưu hồ sơ đồ thất lạc theo boundary LostFoundCase | Front Office.Guest Services / LostFoundCase | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| VatPhamThatLac | LostFoundItem | Vật phẩm thất lạc | Lưu vật phẩm thất lạc theo boundary LostFoundCase | Front Office.Guest Services / LostFoundCase | Child Entity | HoSoDoThatLac | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| BanGiaoLuuGiu | CustodyHandover | Bàn giao lưu giữ | Lưu bàn giao lưu giữ theo boundary LostFoundCase | Front Office.Guest Services / LostFoundCase | History | HoSoDoThatLac | Tạo mới bất biến; không sửa nội dung | Cao | Cao | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |
| XacMinhNguoiNhanDo | ClaimVerification | Xác minh người nhận đồ | Lưu xác minh người nhận đồ theo boundary LostFoundCase | Front Office.Guest Services / LostFoundCase | History | HoSoDoThatLac | Tạo mới bất biến; không sửa nội dung | Cao | Cao | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |

### 4.9. Group Operations

| Bảng SQL | Domain | Tên nghiệp vụ | Mục đích | Module/Aggregate sở hữu | Loại bảng | Bảng cha | Lifecycle | Volume | Tăng trưởng | Retention | PII | Tài chính | Mã hóa | Archive | Partition tương lai | Append-only | Sửa/xóa vật lý |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| LuuTruDoan | GroupStay | Lưu trú đoàn | Lưu lưu trú đoàn theo boundary GroupStay | Front Office.Group Operations / GroupStay | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| LienKetThanhVienDoan | GroupMemberLink | Liên kết thành viên đoàn | Lưu liên kết thành viên đoàn theo boundary GroupStay | Front Office.Group Operations / GroupStay | Child Entity | LuuTruDoan | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| KhoiPhongDoan | RoomBlock | Khối phòng đoàn | Lưu khối phòng đoàn theo boundary RoomBlock | Front Office.Group Operations / RoomBlock | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |
| PhanBoKhoiPhongTheoNgay | RoomBlockDay | Phân bổ khối phòng theo ngày | Lưu phân bổ khối phòng theo ngày theo boundary RoomBlock | Front Office.Group Operations / RoomBlock | Child Entity | KhoiPhongDoan | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |
| DanhSachKhachDoan | RoomingList | Danh sách khách đoàn | Lưu danh sách khách đoàn theo boundary RoomingList | Front Office.Group Operations / RoomingList | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| ThanhVienDanhSachDoan | RoomingMember | Thành viên danh sách đoàn | Lưu thành viên danh sách đoàn theo boundary RoomingList | Front Office.Group Operations / RoomingList | Child Entity | DanhSachKhachDoan | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| ChiDanThanhToanDoan | GroupBillingInstruction | Chỉ dẫn thanh toán đoàn | Lưu chỉ dẫn thanh toán đoàn theo boundary GroupBillingInstruction | Front Office.Group Operations / GroupBillingInstruction | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |
| DinhTuyenThanhToanDoan | BillingRouteLine | Dòng định tuyến thanh toán | Lưu dòng định tuyến thanh toán theo boundary GroupBillingInstruction | Front Office.Group Operations / GroupBillingInstruction | Child Entity | ChiDanThanhToanDoan | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |

### 4.10. Night Audit

| Bảng SQL | Domain | Tên nghiệp vụ | Mục đích | Module/Aggregate sở hữu | Loại bảng | Bảng cha | Lifecycle | Volume | Tăng trưởng | Retention | PII | Tài chính | Mã hóa | Archive | Partition tương lai | Append-only | Sửa/xóa vật lý |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| NgayNghiepVuCoSo | PropertyBusinessDate | Ngày nghiệp vụ cơ sở | Lưu ngày nghiệp vụ cơ sở theo boundary PropertyBusinessDate | Front Office.Night Audit / PropertyBusinessDate | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |
| PhienKiemToanDem | NightAuditRun | Phiên kiểm toán đêm | Lưu phiên kiểm toán đêm theo boundary NightAuditRun | Front Office.Night Audit / NightAuditRun | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |
| DiemKiemSoatKiemToan | AuditCheckpoint | Điểm kiểm soát kiểm toán | Lưu điểm kiểm soát kiểm toán theo boundary NightAuditRun | Front Office.Night Audit / NightAuditRun | History | PhienKiemToanDem | Tạo mới bất biến; không sửa nội dung | Cao | Cao | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |
| XacNhanKiemToan | AuditSignoff | Xác nhận kiểm toán | Lưu xác nhận kiểm toán theo boundary NightAuditRun | Front Office.Night Audit / NightAuditRun | History | PhienKiemToanDem | Tạo mới bất biến; không sửa nội dung | Cao | Cao | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |
| DanhMucKiemToan | AuditChecklist | Danh mục kiểm toán | Lưu danh mục kiểm toán theo boundary AuditChecklist | Front Office.Night Audit / AuditChecklist | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| MucKiemToan | ChecklistItem | Mục kiểm toán | Lưu mục kiểm toán theo boundary AuditChecklist | Front Office.Night Audit / AuditChecklist | Child Entity | DanhMucKiemToan | Theo Root cha hoặc trạng thái riêng | Trung bình | Theo giao dịch | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| NgoaiLeKiemToan | AuditException | Ngoại lệ kiểm toán | Lưu ngoại lệ kiểm toán theo boundary AuditException | Front Office.Night Audit / AuditException | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không; correction/close/archive |
| BangChungNgoaiLe | ExceptionEvidence | Bằng chứng ngoại lệ | Lưu bằng chứng ngoại lệ theo boundary AuditException | Front Office.Night Audit / AuditException | History | NgoaiLeKiemToan | Tạo mới bất biến; không sửa nội dung | Cao | Cao | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Có | Có | Mã hóa field nhạy cảm | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |

### 4.11. Operational Task

| Bảng SQL | Domain | Tên nghiệp vụ | Mục đích | Module/Aggregate sở hữu | Loại bảng | Bảng cha | Lifecycle | Volume | Tăng trưởng | Retention | PII | Tài chính | Mã hóa | Archive | Partition tương lai | Append-only | Sửa/xóa vật lý |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| CongViecVanHanh | OperationalTask | Công việc vận hành | Lưu công việc vận hành theo boundary OperationalTask | Front Office.Operational Task / OperationalTask | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| PhanCongCongViec | TaskAssignment | Phân công công việc | Lưu phân công công việc theo boundary OperationalTask | Front Office.Operational Task / OperationalTask | History | CongViecVanHanh | Tạo mới bất biến; không sửa nội dung | Cao | Cao | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |
| BangChungCongViec | TaskEvidence | Bằng chứng công việc | Lưu bằng chứng công việc theo boundary OperationalTask | Front Office.Operational Task / OperationalTask | History | CongViecVanHanh | Tạo mới bất biến; không sửa nội dung | Cao | Cao | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |
| BanGiaoCa | ShiftHandover | Bàn giao ca | Lưu bàn giao ca theo boundary ShiftHandover | Front Office.Operational Task / ShiftHandover | Aggregate Root | — | Theo state machine của Root | Thấp–trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Cân nhắc theo volume | Chưa cần; đánh giá thực tế | Không | Không cascade; deactivate/close hoặc purge theo policy |
| HangMucBanGiao | HandoverItemSnapshot | Hạng mục bàn giao | Lưu hạng mục bàn giao theo boundary ShiftHandover | Front Office.Operational Task / ShiftHandover | Child Entity | BanGiaoCa | Tạo mới bất biến; không sửa nội dung | Trung bình | Theo giao dịch | Theo mục đích/consent — CẦN XÁC NHẬN | Có | Không | Mã hóa field nhạy cảm | Có | Chưa cần; đánh giá thực tế | Có | Không; correction/close/archive |

### 4.12. Device & Integration Operations

| Bảng SQL | Domain | Tên nghiệp vụ | Mục đích | Module/Aggregate sở hữu | Loại bảng | Bảng cha | Lifecycle | Volume | Tăng trưởng | Retention | PII | Tài chính | Mã hóa | Archive | Partition tương lai | Append-only | Sửa/xóa vật lý |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| CongViecTichHop | IntegrationJob | Công việc tích hợp | Lưu công việc tích hợp theo boundary IntegrationJob | Front Office.Device & Integration Operations / IntegrationJob | Integration | — | Theo Root cha hoặc trạng thái riêng | Cao | Rất cao | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Có | Ứng viên theo Property/BusinessDate/time | Không | Không cascade; deactivate/close hoặc purge theo policy |
| LanThucHienTichHop | IntegrationAttempt | Lần thực hiện tích hợp | Lưu lần thực hiện tích hợp theo boundary IntegrationJob | Front Office.Device & Integration Operations / IntegrationJob | Integration | CongViecTichHop | Tạo mới bất biến; không sửa nội dung | Cao | Rất cao | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Có | Ứng viên theo Property/BusinessDate/time | Có | Không; correction/close/archive |
| NgoaiLeTichHop | IntegrationException | Ngoại lệ tích hợp | Lưu ngoại lệ tích hợp theo boundary IntegrationException | Front Office.Device & Integration Operations / IntegrationException | Integration | — | Theo Root cha hoặc trạng thái riêng | Cao | Rất cao | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Có | Ứng viên theo Property/BusinessDate/time | Không | Không; correction/close/archive |
| XuLyNgoaiLeTichHop | ExceptionResolution | Xử lý ngoại lệ tích hợp | Lưu xử lý ngoại lệ tích hợp theo boundary IntegrationException | Front Office.Device & Integration Operations / IntegrationException | Integration | NgoaiLeTichHop | Tạo mới bất biến; không sửa nội dung | Cao | Rất cao | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Có | Ứng viên theo Property/BusinessDate/time | Có | Không; correction/close/archive |
| ThamChieuBenNgoai | ExternalReference | Tham chiếu bên ngoài | Lưu tham chiếu bên ngoài theo boundary ExternalReference | Front Office.Device & Integration Operations / ExternalReference | Integration | — | Theo Root cha hoặc trạng thái riêng | Cao | Rất cao | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Có | Ứng viên theo Property/BusinessDate/time | Không | Không cascade; deactivate/close hoặc purge theo policy |
| PhienBanAnhXaNgoai | ExternalMappingVersion | Phiên bản ánh xạ ngoài | Lưu phiên bản ánh xạ ngoài theo boundary ExternalReference | Front Office.Device & Integration Operations / ExternalReference | Integration | ThamChieuBenNgoai | Tạo mới bất biến; không sửa nội dung | Cao | Rất cao | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Có | Ứng viên theo Property/BusinessDate/time | Có | Không; correction/close/archive |
| BanGhiChongTrung | IdempotencyRecord | Bản ghi chống trùng | Lưu bản ghi chống trùng theo boundary IdempotencyRecord | Front Office.Device & Integration Operations / IdempotencyRecord | Integration | — | Theo Root cha hoặc trạng thái riêng | Cao | Rất cao | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Có | Ứng viên theo Property/BusinessDate/time | Không | Không; correction/close/archive |
| KetQuaChongTrung | IdempotencyOutcome | Kết quả chống trùng | Lưu kết quả chống trùng theo boundary IdempotencyRecord | Front Office.Device & Integration Operations / IdempotencyRecord | Integration | BanGhiChongTrung | Tạo mới bất biến; không sửa nội dung | Cao | Rất cao | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Có | Ứng viên theo Property/BusinessDate/time | Có | Không; correction/close/archive |
| ThongDiepOutbox | OutboxMessage | Thông điệp đi | Lưu thông điệp đi theo boundary Local Context | Front Office.Device & Integration Operations / Local Context | Outbox | — | Tạo mới bất biến; không sửa nội dung | Cao | Rất cao | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Có | Ứng viên theo Property/BusinessDate/time | Có | Không; correction/close/archive |
| ThongDiepInbox | InboxMessage | Thông điệp đến | Lưu thông điệp đến theo boundary Local Context | Front Office.Device & Integration Operations / Local Context | Inbox | — | Tạo mới bất biến; không sửa nội dung | Cao | Rất cao | Theo lifecycle nghiệp vụ — CẦN XÁC NHẬN | Không | Không | TDE/at-rest nền; field-level khi secret | Có | Ứng viên theo Property/BusinessDate/time | Có | Không; correction/close/archive |
| YeuCauPhatLai | ReplayRequest | Yêu cầu phát lại | Lưu yêu cầu phát lại theo boundary IntegrationException | Front Office.Device & Integration Operations / IntegrationException | Integration | NgoaiLeTichHop | Tạo mới bất biến; không sửa nội dung | Cao | Rất cao | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Không | Có | TDE/at-rest nền; field-level khi secret | Có | Ứng viên theo Property/BusinessDate/time | Có | Không; correction/close/archive |
| NhatKyKiemToan | AuditRecord | Bản ghi kiểm toán | Lưu bản ghi kiểm toán theo boundary Audit Store | Front Office.Device & Integration Operations / Audit Store | Audit | — | Tạo mới bất biến; không sửa nội dung | Cao | Rất cao | Theo chính sách tài chính/pháp lý — CẦN XÁC NHẬN | Có | Có | Mã hóa field nhạy cảm | Có | Ứng viên theo Property/BusinessDate/time | Có | Không; correction/close/archive |

### 4.13. Tổng hợp

Danh mục write/technical store gồm **100 bảng logic**. Projection ở mục 16 và Lookup ở mục 17 được quản lý riêng vì lifecycle, rebuild và quyền ghi khác write model. Không có bảng master Guest/Property/Room/RoomType/Company/Invoice/Journal/Notification trong ownership Front Office.

## 5. Detailed Data Dictionary

Mỗi bảng liệt kê cột logic chung và cột nghiệp vụ chính. `Rule` bao gồm validation, snapshot/reference, masking và khả năng cập nhật. Kích thước “Theo policy” phải chốt ở thiết kế vật lý; không mặc định kiểu MAX.

### 5.1. DatPhong — Reservation

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaDatPhong | ReservationId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | SoXacNhan | ConfirmationNumber | Số xác nhận | nvarchar | 30 | Không | — | Không | — | Có | Có | Không | Không | Có | Unique Property |
| 5 | NgayDen | ArrivalDate | Ngày đến | date | — | Không | — | Không | — | Không | Có | Không | Không | Có | NgàyDi sau NgayDen |
| 6 | NgayDi | DepartureDate | Ngày đi | date | — | Không | — | Không | — | Không | Có | Không | Không | Có | NgàyDi sau NgayDen |
| 7 | MaHoSoKhachHang | GuestProfileId | Khách chính CRM | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Có | Field-level; masking | Có | External reference |
| 8 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.2. KhachDatPhong — ReservationGuest

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaKhachDatPhong | ReservationGuestId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDatPhong | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | DatPhong | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | DatPhong | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.3. NhuCauHangPhong — ReservationRoomDemand

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaNhuCauHangPhong | ReservationRoomDemandId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDatPhong | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | DatPhong | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | DatPhong | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.4. AnhChupGiaDatPhong — ReservationRateSnapshot

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaAnhChupGiaDatPhong | ReservationRateSnapshotId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDatPhong | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | DatPhong | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | DatPhong | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.5. ThamChieuBaoDamDatPhong — ReservationGuaranteeRef

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaThamChieuBaoDamDatPhong | ReservationGuaranteeRefId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDatPhong | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | DatPhong | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | DatPhong | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.6. LienHeDatPhong — ReservationContact

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLienHeDatPhong | ReservationContactId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDatPhong | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | DatPhong | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | DatPhong | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.7. GhiChuDatPhong — ReservationNote

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaGhiChuDatPhong | ReservationNoteId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDatPhong | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | DatPhong | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | DatPhong | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.8. YeuCauUuTienDatPhong — ReservationPreference

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaYeuCauUuTienDatPhong | ReservationPreferenceId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDatPhong | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | DatPhong | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | DatPhong | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.9. LichSuThayDoiDatPhong — ReservationChangeRecord

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLichSuThayDoiDatPhong | ReservationChangeRecordId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDatPhong | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | DatPhong | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.10. ChoPhong — WaitlistEntry

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaChoPhong | WaitlistEntryId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | Root | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.11. LanMoiChoPhong — WaitlistOfferAttempt

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLanMoiChoPhong | WaitlistOfferAttemptId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaChoPhong | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | ChoPhong | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | ChoPhong | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.12. LuuTru — Stay

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLuuTru | StayId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | SoLuuTru | StayNumber | Số lưu trú | nvarchar | 30 | Không | — | Không | — | Có | Có | Không | Không | Có | Unique Property |
| 5 | MaDatPhong | ReservationId | Reservation nguồn | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Có | Logical reference |
| 6 | NgayDenDuKien | PlannedArrivalDate | Ngày đến dự kiến | date | — | Không | — | Không | — | Không | Có | Không | Không | Có | Business calendar |
| 7 | NgayDiDuKien | PlannedDepartureDate | Ngày đi dự kiến | date | — | Không | — | Không | — | Không | Có | Không | Không | Có | Sau ngày đến |
| 8 | ThoiDiemNhanPhongUtc | ActualArrivalUtc | Nhận phòng thực tế | datetime2 | 7 | Có | — | Không | — | Không | Có | Không | Không | Có | UTC set once |
| 9 | ThoiDiemTraPhongUtc | ActualDepartureUtc | Trả phòng thực tế | datetime2 | 7 | Có | — | Không | — | Không | Có | Không | Không | Có | UTC set once |
| 10 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 11 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 12 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 13 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 14 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 15 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.13. KhachLuuTru — StayGuest

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaKhachLuuTru | StayGuestId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaLuuTru | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | LuuTru | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | LuuTru | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.14. GiaHanLuuTru — StayExtension

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaGiaHanLuuTru | StayExtensionId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaLuuTru | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | LuuTru | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.15. HanCheLuuTru — StayRestriction

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaHanCheLuuTru | StayRestrictionId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaLuuTru | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | LuuTru | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | LuuTru | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.16. QuyenKyPhong — StayPostingPermission

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaQuyenKyPhong | StayPostingPermissionId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaLuuTru | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | LuuTru | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | LuuTru | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.17. ThietLapRiengTuLuuTru — StayPrivacySetting

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaThietLapRiengTuLuuTru | StayPrivacySettingId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaLuuTru | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | LuuTru | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | LuuTru | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.18. PhanPhong — RoomAssignment

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhanPhong | RoomAssignmentId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | MaLuuTru | StayId | Lưu trú được gán | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | Logical ref |
| 5 | MaPhong | RoomId | Phòng vật lý | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | External ref |
| 6 | ThoiDiemBatDauUtc | StartUtc | Bắt đầu gán | datetime2 | 7 | Không | — | Không | — | Không | Có | Không | Không | Có | UTC |
| 7 | ThoiDiemKetThucUtc | EndUtc | Kết thúc gán | datetime2 | 7 | Có | — | Không | — | Không | Có | Không | Không | Có | No overlap |
| 8 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.19. PhanDoanPhanPhong — AssignmentSegment

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhanDoanPhanPhong | AssignmentSegmentId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaPhanPhong | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | PhanPhong | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | PhanPhong | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.20. QuyenMoKhoaPhong — KeyCredential

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaQuyenMoKhoaPhong | KeyCredentialId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | Root | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.21. LanCapKhoa — CredentialAttempt

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLanCapKhoa | CredentialAttemptId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaQuyenMoKhoaPhong | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | QuyenMoKhoaPhong | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.22. ChuKyKhachLuuTru — GuestSignature

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaChuKyKhachLuuTru | GuestSignatureId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaLuuTru | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | LuuTru | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | LuuTru | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |

### 5.23. TonPhongKhaDung — RoomAvailability

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaTonPhongKhaDung | RoomAvailabilityId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | MaHangPhong | RoomTypeId | Hạng phòng | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | External ref |
| 5 | NgayTonPhong | InventoryDate | Ngày tồn phòng | date | — | Không | — | Không | — | Có | Có | Không | Không | Có | Unique Property+Type+Date |
| 6 | SoLuongVatLy | PhysicalQuantity | Số lượng vật lý | smallint | — | Không | — | Không | — | Không | Có | Không | Không | Có | Không âm |
| 7 | SoLuongGiu | HeldQuantity | Số đang giữ | smallint | — | Không | — | Không | — | Không | Có | Không | Không | Có | Không âm |
| 8 | SoLuongPhanBo | AllocatedQuantity | Số đã phân bổ | smallint | — | Không | — | Không | — | Không | Có | Không | Không | Có | Không âm |
| 9 | SoLuongBiKhoa | BlockedQuantity | Số bị khóa | smallint | — | Không | — | Không | — | Không | Có | Không | Không | Có | Không âm |
| 10 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 11 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 12 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 13 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 14 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 15 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.24. GiuTonPhong — AvailabilityHold

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaGiuTonPhong | AvailabilityHoldId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaTonPhongKhaDung | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | TonPhongKhaDung | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | TonPhongKhaDung | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.25. PhanBoTonPhong — AvailabilityAllocation

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhanBoTonPhong | AvailabilityAllocationId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaTonPhongKhaDung | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | TonPhongKhaDung | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | TonPhongKhaDung | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.26. KiemSoatTonPhong — RoomInventoryControl

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaKiemSoatTonPhong | RoomInventoryControlId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | Root | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.27. HanCheBanPhong — InventoryRestriction

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaHanCheBanPhong | InventoryRestrictionId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaKiemSoatTonPhong | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | KiemSoatTonPhong | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | KiemSoatTonPhong | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.28. SoGiaoDichKhach — Folio

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaSoGiaoDichKhach | FolioId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | SoFolio | FolioNumber | Số Folio | nvarchar | 30 | Không | — | Không | — | Có | Có | Không | Không | Có | Unique Property |
| 5 | MaLuuTru | StayId | Lưu trú liên quan | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Có | Cross-context ref |
| 6 | SoTienNo | DebitTotal | Tổng nợ | decimal | 19,4 | Không | — | Không | — | Không | Có | Không | Không | Có | Money |
| 7 | SoTienCo | CreditTotal | Tổng có | decimal | 19,4 | Không | — | Không | — | Không | Có | Không | Không | Có | Money |
| 8 | MaTienTe | CurrencyCode | Tiền tệ | char | 3 | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | ISO |
| 9 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.29. NganSoGiaoDich — FolioWindow

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaNganSoGiaoDich | FolioWindowId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | MaSoGiaoDichKhach | FolioId | Folio cha | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | Same context |
| 5 | SoNgan | WindowNumber | Số ngăn | smallint | — | Không | — | Không | — | Có | Có | Không | Không | Có | Unique Folio |
| 6 | MaNguoiThanhToan | PayerId | Payer reference | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Có | Snapshot/ref |
| 7 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.30. ButToanSoGiaoDich — FolioEntry

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaButToanSoGiaoDich | FolioEntryId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaNganSoGiaoDich | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | NganSoGiaoDich | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaNganSoGiaoDich | FolioWindowId | Ngăn Folio | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | Physical FK |
| 5 | SoThuTuButToan | EntrySequence | Sequence | bigint | — | Không | — | Không | — | Có | Có | Không | Không | Có | Unique Window |
| 6 | LoaiButToan | EntryType | Loại bút toán | varchar | 30 | Không | — | Không | — | Không | Không | Không | Không | Có | Enum |
| 7 | SoTien | Amount | Số tiền | decimal | 19,4 | Không | — | Không | — | Không | Có | Không | Không | Có | Money |
| 8 | MaTienTe | CurrencyCode | Tiền tệ | char | 3 | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | ISO |
| 9 | MaNguonNgoai | ExternalSourceReference | Nguồn ngoài | nvarchar | 120 | Có | — | Không | Theo owner | Có | Có | Không | Không | Có | Unique scoped |
| 10 | MaButToanGoc | OriginalEntryId | Bút toán gốc | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Có | Reversal chain |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.31. ThueButToanSoGiaoDich — FolioEntryTax

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaThueButToanSoGiaoDich | FolioEntryTaxId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaButToanSoGiaoDich | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | ButToanSoGiaoDich | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.32. LienKetChuyenButToan — FolioTransferLink

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLienKetChuyenButToan | FolioTransferLinkId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaButToanSoGiaoDich | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | ButToanSoGiaoDich | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.33. LienKetDieuChinhButToan — FolioAdjustmentLink

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLienKetDieuChinhButToan | FolioAdjustmentLinkId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaButToanSoGiaoDich | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | ButToanSoGiaoDich | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.34. ChiDanDinhTuyen — RoutingInstruction

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaChiDanDinhTuyen | RoutingInstructionId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | Root | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.35. DieuKienDinhTuyen — RoutingCondition

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaDieuKienDinhTuyen | RoutingConditionId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaChiDanDinhTuyen | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | ChiDanDinhTuyen | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | ChiDanDinhTuyen | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.36. ThanhToan — Payment

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaThanhToan | PaymentId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | SoThanhToan | PaymentNumber | Số thanh toán | nvarchar | 30 | Không | — | Không | — | Có | Có | Không | Không | Có | Unique Property |
| 5 | SoTien | Amount | Số tiền | decimal | 19,4 | Không | — | Không | — | Không | Có | Không | Không | Có | Money |
| 6 | MaTienTe | CurrencyCode | Tiền tệ | char | 3 | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | ISO |
| 7 | MaPhuongThucThanhToan | PaymentMethodCode | Phương thức | varchar | 30 | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | Lookup |
| 8 | ThamChieuNhaCungCap | ProviderReference | Tham chiếu provider | nvarchar | 120 | Có | — | Không | — | Có | Không | Không | Không | Có | Unique provider scope |
| 9 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.37. LanThuThanhToan — PaymentAttempt

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLanThuThanhToan | PaymentAttemptId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaThanhToan | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | ThanhToan | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.38. DatCoc — Deposit

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaDatCoc | DepositId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | SoDatCoc | DepositNumber | Số đặt cọc | nvarchar | 30 | Không | — | Không | — | Có | Có | Không | Không | Có | Unique Property |
| 5 | SoTienDaNhan | ReceivedAmount | Đã nhận | decimal | 19,4 | Không | — | Không | — | Không | Có | Không | Không | Có | Money |
| 6 | SoTienDaPhanBo | AllocatedAmount | Đã phân bổ | decimal | 19,4 | Không | — | Không | — | Không | Có | Không | Không | Có | Không vượt received |
| 7 | MaTienTe | CurrencyCode | Tiền tệ | char | 3 | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | ISO |
| 8 | MaDatPhong | ReservationId | Reservation | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Có | Logical ref |
| 9 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.39. PhanBoDatCoc — DepositAllocation

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhanBoDatCoc | DepositAllocationId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDatCoc | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | DatCoc | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.40. YeuCauHoanTien — RefundRequest

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaYeuCauHoanTien | RefundRequestId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | SoYeuCauHoanTien | RefundNumber | Số yêu cầu | nvarchar | 30 | Không | — | Không | — | Có | Có | Không | Không | Có | Unique Property |
| 5 | MaGiaoDichNguon | SourceTransactionId | Giao dịch nguồn | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | Logical ref |
| 6 | SoTienYeuCau | RequestedAmount | Số tiền hoàn | decimal | 19,4 | Không | — | Không | — | Không | Có | Không | Không | Có | Không vượt refundable |
| 7 | MaTienTe | CurrencyCode | Tiền tệ | char | 3 | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | ISO |
| 8 | LyDo | ReasonCode | Lý do | varchar | 40 | Không | — | Không | — | Không | Không | Không | Không | Có | Lookup |
| 9 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.41. BuocPheDuyetHoanTien — RefundApprovalStep

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBuocPheDuyetHoanTien | RefundApprovalStepId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaYeuCauHoanTien | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | YeuCauHoanTien | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.42. LanThucHienHoanTien — RefundExecutionAttempt

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLanThucHienHoanTien | RefundExecutionAttemptId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaYeuCauHoanTien | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | YeuCauHoanTien | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.43. CaThuNgan — CashierShift

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaCaThuNgan | CashierShiftId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | SoCaThuNgan | ShiftNumber | Số ca | nvarchar | 30 | Không | — | Không | — | Có | Có | Không | Không | Có | Unique Property+Date |
| 5 | MaThuNgan | CashierId | Thu ngân | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | User ref |
| 6 | MaMayTram | WorkstationId | Máy trạm | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | System ref |
| 7 | SoTienQuyDauCa | OpeningFloat | Quỹ đầu ca | decimal | 19,4 | Không | — | Không | — | Không | Có | Không | Không | Có | Money |
| 8 | MaTienTe | CurrencyCode | Tiền tệ | char | 3 | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | ISO |
| 9 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.44. TongHopPhuongThucThu — CashierTenderSummary

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaTongHopPhuongThucThu | CashierTenderSummaryId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaCaThuNgan | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | CaThuNgan | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | CaThuNgan | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.45. ChenhLechCaThuNgan — CashierVariance

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaChenhLechCaThuNgan | CashierVarianceId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaCaThuNgan | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | CaThuNgan | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.46. VanHanhBuongPhong — RoomOperation

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaVanHanhBuongPhong | RoomOperationId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | MaPhong | RoomId | Phòng | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | External unique Property |
| 5 | TrangThaiVanHanh | OperationalStatus | Trạng thái vận hành | varchar | 30 | Không | — | Không | — | Không | Có | Không | Không | Có | Enum |
| 6 | CoKhongLamPhien | IsDnd | Không làm phiền | bit | — | Không | — | Không | — | Không | Không | Không | Không | Có | Default 0 |
| 7 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.47. LichSuTrangThaiPhong — RoomStatusTransition

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLichSuTrangThaiPhong | RoomStatusTransitionId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaVanHanhBuongPhong | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | VanHanhBuongPhong | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.48. SaiLechTrangThaiPhong — RoomDiscrepancy

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaSaiLechTrangThaiPhong | RoomDiscrepancyId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaVanHanhBuongPhong | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | VanHanhBuongPhong | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | VanHanhBuongPhong | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.49. CongViecBuongPhong — HousekeepingTask

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaCongViecBuongPhong | HousekeepingTaskId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | MaPhong | RoomId | Phòng | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | External ref |
| 5 | MaNguoiDuocGiao | AssigneeId | Người được giao | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Có | User ref |
| 6 | MucUuTien | PriorityCode | Ưu tiên | varchar | 20 | Không | — | Không | — | Không | Không | Không | Không | Có | Lookup |
| 7 | ThoiDiemDenHanUtc | DueAtUtc | Hạn | datetime2 | 7 | Có | — | Không | — | Không | Có | Không | Không | Có | UTC |
| 8 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.50. PhienLamBuong — CleaningWorkSegment

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhienLamBuong | CleaningWorkSegmentId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaCongViecBuongPhong | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | CongViecBuongPhong | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.51. MucKiemTraVeSinh — CleaningChecklistItem

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaMucKiemTraVeSinh | CleaningChecklistItemId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaCongViecBuongPhong | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | CongViecBuongPhong | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | CongViecBuongPhong | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.52. KiemTraPhong — RoomInspection

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaKiemTraPhong | RoomInspectionId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | Root | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.53. PhatHienKiemPhong — InspectionFinding

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhatHienKiemPhong | InspectionFindingId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaKiemTraPhong | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | KiemTraPhong | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | KiemTraPhong | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.54. HoSoBaoTri — MaintenanceCase

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaHoSoBaoTri | MaintenanceCaseId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | SoHoSoBaoTri | CaseNumber | Số hồ sơ | nvarchar | 30 | Không | — | Không | — | Có | Có | Không | Không | Có | Unique Property |
| 5 | MaPhong | RoomId | Phòng ảnh hưởng | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Có | External ref |
| 6 | MucDo | SeverityCode | Mức độ | varchar | 20 | Không | — | Không | — | Không | Không | Không | Không | Có | Lookup |
| 7 | MoTa | Description | Mô tả | nvarchar | 1000 | Không | — | Không | — | Không | Không | Không | Không | Có | Không max |
| 8 | ThoiDiemDenHanUtc | DueAtUtc | SLA | datetime2 | 7 | Có | — | Không | — | Không | Có | Không | Không | Có | UTC |
| 9 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.55. NhatKyCongViecBaoTri — MaintenanceWorkLog

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaNhatKyCongViecBaoTri | MaintenanceWorkLogId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaHoSoBaoTri | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | HoSoBaoTri | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.56. ThamChieuVatTuBaoTri — MaintenancePartRef

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaThamChieuVatTuBaoTri | MaintenancePartRefId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaHoSoBaoTri | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | HoSoBaoTri | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | HoSoBaoTri | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.57. KhoaVanHanhPhong — RoomOperationalBlock

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaKhoaVanHanhPhong | RoomOperationalBlockId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | SoKhoaPhong | BlockNumber | Số khóa | nvarchar | 30 | Không | — | Không | — | Có | Có | Không | Không | Có | Unique Property |
| 5 | MaPhong | RoomId | Phòng bị khóa | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | External ref |
| 6 | LoaiKhoa | BlockType | OOO/OOS | varchar | 10 | Không | — | Không | — | Không | Không | Không | Không | Có | Enum |
| 7 | ThoiDiemHieuLucTuUtc | EffectiveFromUtc | Bắt đầu | datetime2 | 7 | Không | — | Không | — | Không | Có | Không | Không | Có | UTC |
| 8 | ThoiDiemHieuLucDenUtc | EffectiveToUtc | Kết thúc | datetime2 | 7 | Không | — | Không | — | Không | Có | Không | Không | Có | Sau from |
| 9 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.58. GiaHanKhoaPhong — BlockExtension

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaGiaHanKhoaPhong | BlockExtensionId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaKhoaVanHanhPhong | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | KhoaVanHanhPhong | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.59. YeuCauDichVu — ServiceRequest

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaYeuCauDichVu | ServiceRequestId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | SoYeuCauDichVu | RequestNumber | Số yêu cầu | nvarchar | 30 | Không | — | Không | — | Có | Có | Không | Không | Có | Unique Property |
| 5 | MaLuuTru | StayId | Lưu trú | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Có | Logical ref |
| 6 | MaLoaiDichVu | ServiceTypeCode | Loại dịch vụ | varchar | 30 | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | Lookup |
| 7 | ThoiDiemDenHanUtc | DueAtUtc | SLA | datetime2 | 7 | Có | — | Không | — | Không | Có | Không | Không | Có | UTC |
| 8 | NoiDung | Description | Nội dung an toàn | nvarchar | 1000 | Có | — | Không | — | Không | Không | Có | Field-level; masking | Có | Không secret |
| 9 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.60. HangMucYeuCauDichVu — ServiceRequestItem

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaHangMucYeuCauDichVu | ServiceRequestItemId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaYeuCauDichVu | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | YeuCauDichVu | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | YeuCauDichVu | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.61. BangChungHoanThanhDichVu — ServiceCompletionEvidence

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBangChungHoanThanhDichVu | ServiceCompletionEvidenceId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaYeuCauDichVu | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | YeuCauDichVu | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |

### 5.62. CuocGoiBaoThuc — WakeUpCall

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaCuocGoiBaoThuc | WakeUpCallId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | SoYeuCauBaoThuc | WakeUpNumber | Số yêu cầu | nvarchar | 30 | Không | — | Không | — | Có | Có | Không | Không | Có | Unique Property |
| 5 | MaLuuTru | StayId | Lưu trú | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | Logical ref |
| 6 | ThoiDiemGoiUtc | ScheduledAtUtc | Thời điểm gọi | datetime2 | 7 | Không | — | Không | — | Không | Có | Không | Không | Có | UTC+timezone snapshot |
| 7 | SoLanThuToiDa | MaximumAttempts | Số lần thử | tinyint | — | Không | — | Không | — | Không | Có | Không | Không | Có | Lớn hơn 0 |
| 8 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.63. LanGoiBaoThuc — WakeUpAttempt

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLanGoiBaoThuc | WakeUpAttemptId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaCuocGoiBaoThuc | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | CuocGoiBaoThuc | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.64. HoSoDoThatLac — LostFoundCase

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaHoSoDoThatLac | LostFoundCaseId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | SoHoSoThatLac | CaseNumber | Số hồ sơ | nvarchar | 30 | Không | — | Không | — | Có | Có | Không | Không | Có | Unique Property |
| 5 | LoaiHoSo | CaseType | Lost/Found | varchar | 10 | Không | — | Không | — | Không | Có | Không | Không | Có | Enum |
| 6 | ThoiDiemPhatHienUtc | FoundAtUtc | Thời điểm phát hiện | datetime2 | 7 | Không | — | Không | — | Không | Có | Không | Không | Có | UTC |
| 7 | ViTriPhatHien | FoundLocation | Vị trí | nvarchar | 200 | Có | — | Không | — | Không | Không | Không | Không | Có | Không PII quá mức |
| 8 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.65. VatPhamThatLac — LostFoundItem

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaVatPhamThatLac | LostFoundItemId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaHoSoDoThatLac | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | HoSoDoThatLac | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | HoSoDoThatLac | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.66. BanGiaoLuuGiu — CustodyHandover

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBanGiaoLuuGiu | CustodyHandoverId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaHoSoDoThatLac | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | HoSoDoThatLac | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |

### 5.67. XacMinhNguoiNhanDo — ClaimVerification

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaXacMinhNguoiNhanDo | ClaimVerificationId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaHoSoDoThatLac | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | HoSoDoThatLac | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |

### 5.68. LuuTruDoan — GroupStay

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLuuTruDoan | GroupStayId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | SoDoan | GroupNumber | Số đoàn | nvarchar | 30 | Không | — | Không | — | Có | Có | Không | Không | Có | Unique Property |
| 5 | TenDoan | GroupName | Tên đoàn | nvarchar | 200 | Không | — | Không | — | Không | Không | Không | Không | Có | PII thấp |
| 6 | NgayDen | ArrivalDate | Ngày đến | date | — | Không | — | Không | — | Không | Có | Không | Không | Có | Before departure |
| 7 | NgayDi | DepartureDate | Ngày đi | date | — | Không | — | Không | — | Không | Có | Không | Không | Có | After arrival |
| 8 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.69. LienKetThanhVienDoan — GroupMemberLink

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLienKetThanhVienDoan | GroupMemberLinkId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaLuuTruDoan | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | LuuTruDoan | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | LuuTruDoan | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.70. KhoiPhongDoan — RoomBlock

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaKhoiPhongDoan | RoomBlockId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | SoKhoiPhong | BlockNumber | Số khối | nvarchar | 30 | Không | — | Không | — | Có | Có | Không | Không | Có | Unique Property |
| 5 | MaLuuTruDoan | GroupStayId | Đoàn | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | Logical ref |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu | date | — | Không | — | Không | — | Không | Có | Không | Không | Có | Date |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc | date | — | Không | — | Không | — | Không | Có | Không | Không | Có | After from |
| 8 | NgayChotGiuPhong | CutOffDate | Ngày chốt | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Policy |
| 9 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.71. PhanBoKhoiPhongTheoNgay — RoomBlockDay

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhanBoKhoiPhongTheoNgay | RoomBlockDayId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaKhoiPhongDoan | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | KhoiPhongDoan | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | KhoiPhongDoan | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.72. DanhSachKhachDoan — RoomingList

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaDanhSachKhachDoan | RoomingListId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | SoPhienDanhSach | ListVersion | Phiên bản | int | — | Không | — | Không | — | Không | Có | Không | Không | Có | Lớn hơn 0 |
| 5 | MaLuuTruDoan | GroupStayId | Đoàn | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | Logical ref |
| 6 | MaVanTayTep | FileFingerprint | Fingerprint file | varbinary | 32 | Có | — | Không | Theo owner | Có | Có | Không | Không | Có | Unique group/version |
| 7 | TongSoDong | TotalRows | Số dòng | int | — | Không | — | Không | — | Không | Có | Không | Không | Có | Không âm |
| 8 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.73. ThanhVienDanhSachDoan — RoomingMember

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaThanhVienDanhSachDoan | RoomingMemberId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDanhSachKhachDoan | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | DanhSachKhachDoan | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | DanhSachKhachDoan | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.74. ChiDanThanhToanDoan — GroupBillingInstruction

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaChiDanThanhToanDoan | GroupBillingInstructionId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | Root | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.75. DinhTuyenThanhToanDoan — BillingRouteLine

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaDinhTuyenThanhToanDoan | BillingRouteLineId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaChiDanThanhToanDoan | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | ChiDanThanhToanDoan | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | ChiDanThanhToanDoan | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.76. NgayNghiepVuCoSo — PropertyBusinessDate

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaNgayNghiepVuCoSo | PropertyBusinessDateId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | NgayNghiepVuHienTai | CurrentBusinessDate | Ngày nghiệp vụ hiện tại | date | — | Không | — | Không | — | Không | Có | Không | Không | Có | One active Property |
| 5 | MuiGio | TimezoneId | Múi giờ | varchar | 64 | Không | — | Không | — | Không | Không | Không | Không | Có | IANA/Windows mapping |
| 6 | MaPhienKiemToanDangChay | ActiveNightAuditRunId | Run đang chạy | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Có | Logical ref |
| 7 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.77. PhienKiemToanDem — NightAuditRun

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhienKiemToanDem | NightAuditRunId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | SoPhienKiemToan | RunNumber | Số phiên | nvarchar | 30 | Không | — | Không | — | Có | Có | Không | Không | Có | Unique Property+Date |
| 5 | NgayDongSo | ClosingBusinessDate | Ngày đóng sổ | date | — | Không | — | Không | — | Không | Có | Không | Không | Có | Business Date |
| 6 | BuocHienTai | CurrentStep | Bước hiện tại | varchar | 40 | Có | — | Không | — | Không | Không | Không | Không | Có | Checkpoint |
| 7 | MaLease | LeaseToken | Lease | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Có | Exclusive |
| 8 | ThoiDiemHetLeaseUtc | LeaseExpiresAtUtc | Hết lease | datetime2 | 7 | Có | — | Không | — | Không | Có | Không | Không | Có | UTC |
| 9 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.78. DiemKiemSoatKiemToan — AuditCheckpoint

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaDiemKiemSoatKiemToan | AuditCheckpointId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaPhienKiemToanDem | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | PhienKiemToanDem | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.79. XacNhanKiemToan — AuditSignoff

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaXacNhanKiemToan | AuditSignoffId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaPhienKiemToanDem | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | PhienKiemToanDem | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.80. DanhMucKiemToan — AuditChecklist

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaDanhMucKiemToan | AuditChecklistId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | Root | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.81. MucKiemToan — ChecklistItem

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaMucKiemToan | ChecklistItemId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDanhMucKiemToan | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | DanhMucKiemToan | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | DanhMucKiemToan | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.82. NgoaiLeKiemToan — AuditException

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaNgoaiLeKiemToan | AuditExceptionId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | Root | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.83. BangChungNgoaiLe — ExceptionEvidence

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBangChungNgoaiLe | ExceptionEvidenceId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaNgoaiLeKiemToan | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | NgoaiLeKiemToan | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |

### 5.84. CongViecVanHanh — OperationalTask

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaCongViecVanHanh | OperationalTaskId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | SoCongViec | TaskNumber | Số công việc | nvarchar | 30 | Không | — | Không | — | Có | Có | Không | Không | Có | Unique Property |
| 5 | MaLoaiCongViec | TaskTypeCode | Loại việc | varchar | 30 | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | Lookup |
| 6 | MaNguon | SourceId | Nguồn | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | Cross-context |
| 7 | ThoiDiemDenHanUtc | DueAtUtc | Hạn | datetime2 | 7 | Có | — | Không | — | Không | Có | Không | Không | Có | UTC |
| 8 | MucUuTien | PriorityCode | Ưu tiên | varchar | 20 | Không | — | Không | — | Không | Không | Không | Không | Có | Lookup |
| 9 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.85. PhanCongCongViec — TaskAssignment

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhanCongCongViec | TaskAssignmentId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaCongViecVanHanh | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | CongViecVanHanh | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.86. BangChungCongViec — TaskEvidence

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBangChungCongViec | TaskEvidenceId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaCongViecVanHanh | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | CongViecVanHanh | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |

### 5.87. BanGiaoCa — ShiftHandover

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBanGiaoCa | ShiftHandoverId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | Ngày nghiệp vụ tạo/hiệu lực | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Có | Không suy từ UTC |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | Root | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | TrangThai | Status | Trạng thái lifecycle | varchar | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Có | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Có | Không | Có | Cùng transaction |

### 5.88. HangMucBanGiao — HandoverItemSnapshot

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaHangMucBanGiao | HandoverItemSnapshotId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaBanGiaoCa | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | BanGiaoCa | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | Loại nghiệp vụ | varchar | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Có | Governed |
| 5 | ThuTu | Order | Thứ tự trong Root | smallint | — | Không | 0 | Không | BanGiaoCa | Không | Có | Không | Không | Có | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | Bắt đầu hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | Kết thúc hiệu lực | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không trước from |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |

### 5.89. CongViecTichHop — IntegrationJob

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaCongViecTichHop | IntegrationJobId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDiemCuoi | EndpointId | Endpoint | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | Config ref |
| 4 | LoaiThaoTac | OperationType | Thao tác | varchar | 40 | Không | — | Không | — | Không | Không | Không | Không | Có | Governed |
| 5 | KhoaChongTrung | IdempotencyKey | Khóa chống trùng | nvarchar | 200 | Không | — | Không | — | Có | Không | Không | Không | Có | Unique endpoint |
| 6 | SoLanThu | AttemptCount | Số lần thử | int | — | Không | — | Không | — | Không | Có | Không | Không | Có | Không âm |
| 7 | ThoiDiemThuLaiUtc | NextRetryAtUtc | Lần thử tới | datetime2 | 7 | Có | — | Không | — | Không | Có | Không | Không | Có | UTC |
| 8 | MaLease | LeaseToken | Lease | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Có | Worker lease |
| 8A | TrangThai | Status | Trạng thái job | varchar | 30 | Không | Queued | Không | — | Không | Có | Không | Không | Có | State machine; không string tự do |
| 8B | MaNguoiGiuLease | LeaseOwner | Worker đang giữ lease | nvarchar | 100 | Có | — | Không | — | Không | Có | Không | Không | Có | Đi cùng thời điểm hết lease |
| 8C | ThoiDiemHetLeaseUtc | LeaseExpiresAtUtc | Thời điểm hết lease | datetime2 | 7 | Có | — | Không | — | Không | Có | Không | Không | Có | Worker khác chỉ reclaim sau expiry |
| 8D | PhanLoaiLoi | ErrorClassification | Phân loại lỗi gần nhất | varchar | 30 | Có | — | Không | Lookup | Không | Có | Không | Không | Có | Transient/Permanent/Unknown |
| 9 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.90. LanThucHienTichHop — IntegrationAttempt

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLanThucHienTichHop | IntegrationAttemptId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaCongViecTichHop | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | CongViecTichHop | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.91. NgoaiLeTichHop — IntegrationException

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaNgoaiLeTichHop | IntegrationExceptionId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 4 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 5 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 6 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 6A | TrangThai | Status | Trạng thái ngoại lệ | varchar | 30 | Không | Open | Không | — | Không | Có | Không | Không | Có | Open/Assigned/Investigating/Resolved/Accepted |
| 6B | MucDo | Severity | Mức độ ảnh hưởng | varchar | 20 | Không | — | Không | Lookup | Không | Có | Không | Không | Có | Điều khiển SLA/escalation |
| 6C | MaNguoiPhuTrach | OwnerId | Người/nhóm phụ trách | uniqueidentifier | — | Có | — | Không | Identity external | Không | Có | Không | Không | Có | Có thể reassign có audit |
| 6D | MaVanTayNgoaiLe | ExceptionFingerprint | Khóa gom lỗi tương đương | varbinary | 32 | Không | — | Không | — | Có trong active scope | Có | Không | Không | Có | Dedupe ngoại lệ mở |
| 7 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 10 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 11 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.92. XuLyNgoaiLeTichHop — ExceptionResolution

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaXuLyNgoaiLeTichHop | ExceptionResolutionId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaNgoaiLeTichHop | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | NgoaiLeTichHop | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.93. ThamChieuBenNgoai — ExternalReference

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaThamChieuBenNgoai | ExternalReferenceId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 4 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 5 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 6 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 6A | MaDiemCuoi | EndpointId | Điểm cuối tích hợp | uniqueidentifier | — | Không | — | Không | Integration config | Không | Có | Không | Không | Có | Bất biến trong mapping version |
| 6B | LoaiDoiTuongNgoai | ExternalObjectType | Loại đối tượng bên ngoài | varchar | 80 | Không | — | Không | — | Không | Có | Không | Không | Có | Contract-governed |
| 6C | MaDoiTuongNgoai | ExternalObjectId | ID đối tượng bên ngoài | nvarchar | 200 | Không | — | Không | — | Có theo endpoint/type | Có | Có | Mã hóa/masking nếu chứa PII | Có | Không chứa secret |
| 6D | LoaiDoiTuongNoiBo | LocalSubjectType | Loại subject nội bộ | varchar | 80 | Không | — | Không | — | Không | Có | Không | Không | Có | Canonical Aggregate type |
| 6E | MaDoiTuongNoiBo | LocalSubjectId | ID subject nội bộ | uniqueidentifier | — | Không | — | Không | Logical reference | Không | Có | Không | Không | Có | Không FK xuyên context |
| 6F | TrangThai | Status | Trạng thái mapping | varchar | 20 | Không | Active | Không | — | Không | Có | Không | Không | Có | Active/Superseded/Invalidated |
| 7 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 10 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 11 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.94. PhienBanAnhXaNgoai — ExternalMappingVersion

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhienBanAnhXaNgoai | ExternalMappingVersionId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaThamChieuBenNgoai | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | ThamChieuBenNgoai | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.95. BanGhiChongTrung — IdempotencyRecord

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBanGhiChongTrung | IdempotencyRecordId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | KhongGianTen | Namespace | Namespace key | varchar | 80 | Không | — | Không | — | Có | Không | Không | Không | Có | Unique scope |
| 4 | KhoaChongTrung | IdempotencyKey | Khóa chống trùng | nvarchar | 200 | Không | — | Không | — | Có | Không | Không | Không | Có | Unique namespace+Property |
| 5 | MaVanTayYeuCau | RequestHash | Hash request | varbinary | 32 | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | Mismatch reject |
| 6 | ThoiDiemHetHanUtc | ExpiresAtUtc | Hết retention | datetime2 | 7 | Có | — | Không | — | Không | Có | Không | Không | Có | Policy |
| 6A | TrangThai | Status | Trạng thái xử lý chống trùng | varchar | 30 | Không | Received | Không | — | Không | Có | Không | Không | Có | Received/Processing/Completed/Failed/Unknown |
| 6B | MaKetQua | ResultReference | Tham chiếu kết quả đã tạo | uniqueidentifier | — | Có | — | Không | Logical subject | Không | Có | Không | Không | Có | Duplicate trả đúng kết quả cũ |
| 6C | MaLease | LeaseToken | Lease khi đang xử lý | uniqueidentifier | — | Có | — | Không | — | Không | Có | Không | Không | Có | Reclaim sau expiry |
| 7 | PhienBanDuLieu | DataVersion | Optimistic concurrency | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Có | Không set client |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |
| 10 | ThoiDiemCapNhatUtc | UpdatedAtUtc | Thời điểm cập nhật UTC | datetime2 | 7 | Có | — | Không | — | Không | Không | Không | Không | Có | Cùng transaction |
| 11 | MaNguoiCapNhat | UpdatedBy | Actor cập nhật | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Có | Cùng transaction |

### 5.96. KetQuaChongTrung — IdempotencyOutcome

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaKetQuaChongTrung | IdempotencyOutcomeId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaBanGhiChongTrung | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | BanGhiChongTrung | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.97. ThongDiepOutbox — OutboxMessage

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaThongDiepOutbox | OutboxMessageId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaThongDiep | MessageId | ID message | uniqueidentifier | — | Không | — | Không | Theo owner | Có | Có | Không | Không | Có | Unique |
| 4 | LoaiThongDiep | MessageType | Loại contract | nvarchar | 200 | Không | — | Không | — | Không | Không | Không | Không | Có | Versioned |
| 5 | PhienBanPayload | PayloadVersion | Phiên bản | smallint | — | Không | — | Không | — | Không | Không | Không | Không | Có | Lớn hơn 0 |
| 6 | PayloadMaHoa | EncryptedPayload | Payload | varbinary | Theo policy | Không | — | Không | — | Không | Có | Không | Không | Có | No raw secret |
| 7 | MaTuongQuan | CorrelationId | Correlation | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | Index |
| 8 | ThoiDiemGuiLaiUtc | NextAttemptAtUtc | Thời điểm gửi | datetime2 | 7 | Có | — | Không | — | Không | Có | Không | Không | Có | UTC |
| 8A | TrangThai | Status | Trạng thái phát thông điệp | varchar | 30 | Không | Pending | Không | — | Không | Có | Không | Không | Có | Pending/Processing/Published/DeadLettered |
| 8B | SoLanThu | AttemptCount | Số lần phát | int | — | Không | 0 | Không | — | Không | Có | Không | Không | Có | Không âm; bounded retry |
| 8C | ThoiDiemXuLyUtc | ProcessedAtUtc | Thời điểm phát thành công | datetime2 | 7 | Có | — | Không | — | Không | Có | Không | Không | Có | UTC |
| 8D | MaLease | LeaseToken | Lease dispatcher | uniqueidentifier | — | Có | — | Không | — | Không | Có | Không | Không | Có | Không hai worker xử lý cùng message |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.98. ThongDiepInbox — InboxMessage

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaThongDiepInbox | InboxMessageId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaThongDiep | MessageId | ID message nguồn | uniqueidentifier | — | Không | — | Không | Theo owner | Có | Có | Không | Không | Có | Unique consumer |
| 4 | ModuleNguon | Producer | Module phát | varchar | 80 | Không | — | Không | — | Không | Không | Không | Không | Có | Governed |
| 5 | MaVanTayPayload | PayloadHash | Hash payload | varbinary | 32 | Không | — | Không | Theo owner | Không | Có | Không | Không | Có | Mismatch reject |
| 6 | ThoiDiemXuLyUtc | ProcessedAtUtc | Đã xử lý | datetime2 | 7 | Có | — | Không | — | Không | Có | Không | Không | Có | UTC |
| 7 | KetQua | Outcome | Kết quả | varchar | 20 | Có | — | Không | — | Không | Không | Không | Không | Có | Enum |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.99. YeuCauPhatLai — ReplayRequest

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaYeuCauPhatLai | ReplayRequestId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaNgoaiLeTichHop | ParentId | Root/bảng cha | uniqueidentifier | — | Không | — | Không | NgoaiLeTichHop | Không | Có | Không | Không | Có | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 6 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Có | Định danh service/user |

### 5.100. NhatKyKiemToan — AuditRecord

| STT | Tên cột SQL | Tên Domain | Ý nghĩa | Kiểu dữ liệu đề xuất | Độ dài/Precision | Null | Default | PK | FK logic | Unique | Index | PII | Mã hóa | Audit | Rule |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaNhatKyKiemToan | AuditRecordId | Khóa kỹ thuật | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | Phạm vi Property | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | SoThuTu | Sequence | Thứ tự trong Root/stream | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 4 | LoaiHanhDong | ActionType | Loại hành động/kết quả | varchar | 40 | Không | — | Không | — | Không | Có | Không | Không | Có | Mã governance |
| 5 | MaTuongQuan | CorrelationId | Mã tương quan | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Có | Trace |
| 6 | ThoiDiemXayRaUtc | OccurredAtUtc | Thời điểm xảy ra | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 7 | ThoiDiemTaoUtc | CreatedAtUtc | Thời điểm tạo UTC | datetime2 | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | MaNguoiTao | CreatedBy | Actor tạo | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Có | Không | Có | Định danh service/user |

---
## 6. Common Column Convention

| Nhóm | Cột chuẩn | Áp dụng và quy tắc |
|---|---|---|
| Identity | `Ma{KháiNiệm}` | Mọi Root/Entity; GUID kỹ thuật bất biến |
| Display | `So...`/`Ma...HienThi` | Tách PK; unique theo Property/series |
| External | `MaThamChieuNgoai`, `HeThongNguon` | Scope + payload hash; không raw secret |
| Property | `MaKhachSan` | Bắt buộc cho mọi transaction Front Office |
| Tenant | `MaDonViThue` | Chỉ khi nền tảng thật sự multi-tenant; không thay Property |
| Legal entity | `MaPhapNhan` | Financial reference/snapshot; không thay Property |
| Branch | `MaChiNhanh` | Chỉ khi Core/Accounting định nghĩa khác Property |
| Business date | `NgayNghiepVu` | SQL date; lấy từ PropertyBusinessDate, không suy UTC |
| Timestamp | `ThoiDiem...Utc` | datetime2(7), UTC |
| Actor | `MaNguoiTao`, `MaNguoiCapNhat` | User/service identity |
| Concurrency | `PhienBanDuLieu` | rowversion trên Root mutable/hot row |
| Reason/approval | `MaLyDo`, `MaPheDuyet` | Override/financial/sensitive action |
| Soft delete | Không mặc định | Chỉ khi domain có semantics; ưu tiên deactivate/close/archive |

Ledger, Payment, Deposit, Refund, audit, event, attempt và Night Audit không hard delete. Aggregate giao dịch cancel/close/expire/anonymize; lookup deactivate/supersede; projection được truncate/rebuild; hold/job tạm chỉ purge sau retention và reconciliation có audit.

## 7. SQL Server Data Type Convention

| Kiểu | Dùng khi | Cấm/ghi chú |
|---|---|---|
| uniqueidentifier | Root ID, offline/cross-context/correlation ID | Cluster strategy là quyết định vật lý |
| bigint | Sequence append-only, cursor, internal high-volume key | Không làm public ID nếu cần offline |
| int/smallint/tinyint | Counter, version, quantity/order theo range | Check range; không dùng money |
| bit | Boolean `Co/Da/Can` | Tri-state nên dùng enum |
| decimal(19,4) | Money; precision khác cho FX khi cần | Luôn Currency; không float/double/money |
| date | BusinessDate/planned hotel date | Không timestamp/timezone |
| datetime2(7) | UTC system/event time | Tên phải hậu tố Utc |
| datetimeoffset | Cần bảo tồn offset external | Không thay TimezoneId của Property |
| nvarchar(n) | Unicode có giới hạn | Không mặc định max |
| varchar/char | Code ASCII/ISO | Không chứa tiếng Việt |
| varbinary | Ciphertext, signature, digest, payload | max chỉ có policy rõ |
| rowversion | Optimistic concurrency | Không phải business version |
| JSON | Payload integration/changed fields có schema/version | Không né Entity/VO/money/state/FK relational |

Money luôn amount + currency + rounding/tax snapshot. Thời điểm policy Property lưu UTC cùng timezone/version khi cần tái diễn giải.

## 8. Identity Strategy

Khuyến nghị Root dùng GUID sinh tại application; child dùng GUID khi có audit/reference độc lập. Display code sinh bởi NumberSequence bền vững theo Property/document type/BusinessDate policy và không làm FK.

| Aggregate Root | Bảng | PK | Loại | Display code | Unique scope | Được đổi | Offline |
|---|---|---|---|---|---|---|---|
| Reservation | DatPhong | MaDatPhong | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| WaitlistEntry | ChoPhong | MaChoPhong | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| Stay | LuuTru | MaLuuTru | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| RoomAssignment | PhanPhong | MaPhanPhong | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| KeyCredential | QuyenMoKhoaPhong | MaQuyenMoKhoaPhong | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| RoomAvailability | TonPhongKhaDung | MaTonPhongKhaDung | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| RoomInventoryControl | KiemSoatTonPhong | MaKiemSoatTonPhong | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| Folio | SoGiaoDichKhach | MaSoGiaoDichKhach | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| FolioWindow | NganSoGiaoDich | MaNganSoGiaoDich | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| RoutingInstruction | ChiDanDinhTuyen | MaChiDanDinhTuyen | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| Payment | ThanhToan | MaThanhToan | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| Deposit | DatCoc | MaDatCoc | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| RefundRequest | YeuCauHoanTien | MaYeuCauHoanTien | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| CashierShift | CaThuNgan | MaCaThuNgan | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| RoomOperation | VanHanhBuongPhong | MaVanHanhBuongPhong | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| HousekeepingTask | CongViecBuongPhong | MaCongViecBuongPhong | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| RoomInspection | KiemTraPhong | MaKiemTraPhong | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| MaintenanceCase | HoSoBaoTri | MaHoSoBaoTri | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| RoomOperationalBlock | KhoaVanHanhPhong | MaKhoaVanHanhPhong | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| ServiceRequest | YeuCauDichVu | MaYeuCauDichVu | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| WakeUpCall | CuocGoiBaoThuc | MaCuocGoiBaoThuc | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| LostFoundCase | HoSoDoThatLac | MaHoSoDoThatLac | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| GroupStay | LuuTruDoan | MaLuuTruDoan | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| RoomBlock | KhoiPhongDoan | MaKhoiPhongDoan | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| RoomingList | DanhSachKhachDoan | MaDanhSachKhachDoan | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| GroupBillingInstruction | ChiDanThanhToanDoan | MaChiDanThanhToanDoan | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| PropertyBusinessDate | NgayNghiepVuCoSo | MaNgayNghiepVuCoSo | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| NightAuditRun | PhienKiemToanDem | MaPhienKiemToanDem | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| AuditChecklist | DanhMucKiemToan | MaDanhMucKiemToan | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| AuditException | NgoaiLeKiemToan | MaNgoaiLeKiemToan | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| OperationalTask | CongViecVanHanh | MaCongViecVanHanh | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| ShiftHandover | BanGiaoCa | MaBanGiaoCa | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| IntegrationJob | CongViecTichHop | MaCongViecTichHop | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| IntegrationException | NgoaiLeTichHop | MaNgoaiLeTichHop | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| ExternalReference | ThamChieuBenNgoai | MaThamChieuBenNgoai | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |
| IdempotencyRecord | BanGhiChongTrung | MaBanGhiChongTrung | uniqueidentifier | Theo Business Dictionary hoặc không bắt buộc | Property/series hoặc natural scope riêng | Không | Sinh phía application; offline-safe |

Natural key chỉ dùng unique constraint cho dedupe: provider/source reference, Property+RoomType+Date, MessageId. Không dùng số phòng, điện thoại, giấy tờ, confirmation number làm PK/FK chính.
---
## 9. Foreign Key and Reference Strategy

| Quan hệ | FK vật lý | Cascade | Nullable | Nguồn vô hiệu/snapshot/đồng bộ |
|---|---|---|---|---|
| Trong cùng Aggregate | Có | Restrict; không cascade ledger/history | Theo invariant | Root quản lý; thường không snapshot |
| Cùng Context khác Root | Chỉ khi lifecycle phù hợp; mặc định logical | Không | Thường có | ID + snapshot/version khi cần |
| Sang Bounded Context | Không mặc định | Không | Theo use case | Không update ngược; stale warning/reconcile |
| Sang module khác | Không | Không | Theo nghiệp vụ | Guest/Room/Company/Invoice ID + snapshot tối thiểu |
| External system | Không | Không | Có khi pending | endpoint+external ID+version+payload hash |
| Snapshot reference | Không | Không | Snapshot bắt buộc theo transaction | Code/name/rate/tax/currency/provenance |

Compound FK nội bộ có thể gồm `MaKhachSan` để chặn cross-Property link. Không join database module khác để validate command.

## 10. Index and Unique Constraint Strategy

| Tên logic | Bảng | Key | Include/filter | Mục đích | Write risk | Kiểm chứng |
|---|---|---|---|---|---|---|
| UQ_DatPhong_SoXacNhan | DatPhong | MaKhachSan, SoXacNhan | status/dates | Tìm confirmation | Ghi booking | Có |
| IX_LienHe_SoDienThoai | LienHeDatPhong | Property, phone hash/normalized | Reservation, masked name; not null | Tìm điện thoại | PII/encryption | Bắt buộc |
| IX_KhachDatPhong_Khach | KhachDatPhong | Property, GuestProfileId | Reservation, role | Tìm theo khách | Fan-out | Có |
| IX_DatPhong_KhachDen | DatPhong | Property, NgayDen, status | confirmation/guest; active filter | Arrival | State/date write | Có |
| IX_LuuTru_KhachDi | LuuTru | Property, departure, status | StayNo/guest; in-house filter | Departure | Extend write | Có |
| IX_LuuTru_DangO | LuuTru | Property, status | guest/departure; InHouse filter | In-house | State write | Có |
| UQ_PhanPhong_Active | PhanPhong/segment | Property, Room, active interval surrogate | Stay/start/end | Gán/overlap | Hot conflict | Bắt buộc |
| UQ_TonPhong_Ngay | TonPhongKhaDung | Property, RoomType, date | quantities/version | Availability | Hot bucket | Bắt buộc |
| IX_Folio_Stay | SoGiaoDichKhach | Property, Stay, status | FolioNo/balance | Tìm Folio | Balance update | Có |
| UQ_FolioEntry_Source | ButToanSoGiaoDich | Property, source system/id/line | entry/amount; not null | Dedupe POS | Ledger write | Bắt buộc |
| UQ_Payment_Provider | ThanhToan | provider, merchant, reference | status/amount; not null | Reconcile/callback | Provider variability | Bắt buộc |
| UQ_Idempotency | BanGhiChongTrung | Property, namespace, key | request hash/result | Command dedupe | High write | Bắt buộc |
| IX_HK_Dirty | VanHanhBuongPhong | Property, operational status | Room/update; Dirty/Cleaning | HK board | Frequent write | Có |
| IX_Task_Overdue | CongViecVanHanh | Property, status, due UTC | assignee/priority; open | Quá hạn | State/due write | Có |
| IX_Maintenance_Active | HoSoBaoTri | Property, status, severity | Room/due; active | Sự cố active | State write | Có |
| UQ_NightAudit_Date | PhienKiemToanDem | Property, closing date | status/lease | One run/date | Thấp | Bắt buộc |
| IX_Integration_Due | CongViecTichHop | status, next retry UTC | endpoint/attempt/lease; due | Worker claim | Churn cao | Bắt buộc |
| IX_Exception_Open | NgoaiLeTichHop | Property, status, severity | owner/created; open | Exception board | State write | Có |
| IX_Outbox_Dispatch | ThongDiepOutbox | status, next attempt, created | type/correlation; pending | Dispatcher | Hot | Bắt buộc |
| UQ_Inbox_Message | ThongDiepInbox | consumer, MessageId | hash/outcome | Consumer dedupe | High write | Bắt buộc |

Không index tràn lan. Include/filter phải kiểm chứng bằng volume, execution plan và write latency thực tế.

## 11. Ledger and Append-Only Data

| Bảng | Append/update | Correction và chain | Dedupe/reconcile | Retention/archive |
|---|---|---|---|---|
| ButToanSoGiaoDich | Posting content bất biến | Reversal/adjustment mới; OriginalEntryId; sequence Window | Source reference unique; source reconcile | Financial policy; partition candidate |
| LanThuThanhToan | Attempt append-only; Payment Root đổi state | Refund/provider reversal là operation mới | Unknown query provider trước retry | Provider/financial policy |
| PhanBoDatCoc | Allocation/reversal rows bất biến | Link original; remaining balance Root/projection | Sum active <= received; Folio reconcile | Financial policy |
| LanThucHienHoanTien | Attempt append-only | Không sửa success | Unknown reconcile trước retry | Financial policy |
| Ca cashier transaction refs | Reference bất biến, summary versioned | Variance/explanation record mới | Provider/Folio reconcile | Cashier/legal policy |
| NhatKyKiemToan | Append-only | Redaction/anonymization overlay | Correlation/command dedupe | Compliance policy |
| Domain Event/Outbox | Event/payload bất biến; delivery state mutable | Compensation event mới | MessageId unique | Replay window + archive |
| Inbox | Receipt/outcome append-oriented | ReplayRequest | Consumer+MessageId | Dedupe window |
| LanThucHienTichHop | Attempt append-only | Retry/compensation qua Job | Provider correlation | High-volume archive |
| DiemKiemSoatKiemToan | Completed checkpoint bất biến | Resolution/new outcome | Run+Step+fingerprint | Audit retention |

Folio Root có thể cập nhật balance summary có version; ledger entry mới là source of truth. Không update amount/currency/tax/source của posting đã ghi.

## 12. Outbox, Inbox and Idempotency

| Store | Khóa/trạng thái | Payload/trace | Retry/lease | Dead-letter/replay | Security/retention |
|---|---|---|---|---|---|
| ThongDiepOutbox | MessageId; Pending/Processing/Published/DeadLettered | type/schema/Aggregate version/correlation/causation/hash | attempt/next/lease/processed | error class; approved replay | Encrypt; no card/raw ID; purge policy |
| ThongDiepInbox | Consumer+MessageId; Received/Completed/Failed | producer/version/correlation/hash | consumer lease | mismatch quarantine; same identity replay | retention >= producer retry window |
| BanGhiChongTrung | Property+namespace+key; Processing/Completed/Unknown | request hash/result ref | lease/expiry | hash mismatch reject; reconcile Unknown | Không lưu raw PII request |
| CongViecTichHop | JobId/dedupe; Queued/Processing/RetryDue/Succeeded/DeadLettered | operation/endpoint/subject/trace | backoff/next/lease | normalized error/operator action | Secret by reference |
| LanThucHienTichHop | Job+attempt | request/response digest/ref/timing | Không retry bản ghi attempt | Unknown triggers reconcile | Append/archive |
| NgoaiLeTichHop | fingerprint; Open/Assigned/Resolved | subject/impact/evidence | manual SLA | resolve/replay/compensate/approve | Encrypt evidence |
| ThamChieuBenNgoai | endpoint+type+external ID | local subject/version/validity | — | approved remap + history | Không secret |
| YeuCauPhatLai | RequestId; Pending/Approved/Executed | original message/job/reason/dedupe identity | executor lease | domain owner approval | Full audit; payload immutable |

Operation non-idempotent có kết quả Unknown tuyệt đối không tự retry; phải reconcile bằng original provider reference.
---
## 13. Audit Trail

Audit dùng logical Audit Store chung trên nền tảng nhưng partition/authorization theo module, Property và thời gian. Module ghi qua contract chuẩn trong local transaction/Outbox; không update audit. Technical log chỉ chẩn đoán, có thể sampling/purge và không phải bằng chứng nghiệp vụ.

| Thành phần | Quyết định |
|---|---|
| Identity/context | AuditId, source module/context, Aggregate type/ID/version, Property, BusinessDate |
| Time/actor | OccurredAtUtc, actor ID/type/role, workstation, session |
| Intent | Command/action, reason, permission result |
| Approval | ApprovalId, approvers, policy/band, dual-control evidence |
| Change | Changed fields/before-after chọn lọc hoặc digest; không full PII graph |
| Trace | CorrelationId, CausationId, ExternalSourceReference |
| AI | RecommendationId/model version/user decision |
| Sensitivity | Classification, encryption/masking, retention/legal hold |

Phân loại: Normal Change, Sensitive Action, Financial Action, Override, Approval, Dual Control, Sensitive Data View, Break-glass Access, Integration Replay, Night Audit và AI-assisted Action. Xem PII/break-glass cũng audit. History chuyên biệt chỉ tồn tại khi là business entity/invariant.

## 14. PII, Security and Retention

| Dữ liệu | Class | Bảo vệ/access | Event/log/AI | Retention |
|---|---|---|---|---|
| Họ tên | PII | Mask theo role; encryption theo threat model | Chỉ ID/tên masked nếu cần | Purpose/legal policy |
| Điện thoại/email | PII | Field encryption hoặc hash search; mask | Không raw log/event | Consent/purpose |
| Địa chỉ | PII | Encrypt; least privilege | Chỉ secure invoice contract | Tax/legal policy |
| CCCD/hộ chiếu | Sensitive PII | Encrypt, token/digest search, strict role, access audit | Không event/log | CẦN XÁC NHẬN CHÍNH SÁCH PHÁP LÝ |
| Ảnh giấy tờ | Sensitive PII | Encrypted object store; DB giữ secure ref/hash | Không event/log/training | CẦN XÁC NHẬN CHÍNH SÁCH PHÁP LÝ |
| Chữ ký | Sensitive PII | Encrypt, immutable evidence | Không event/log/AI | CẦN XÁC NHẬN CHÍNH SÁCH PHÁP LÝ |
| Payment info | Financial/Credential | Token/masked brand/last digits; không PAN/CVV | Provider ref only | PCI/provider/legal policy |
| Ghi chú riêng tư | Sensitive PII | Encrypt, category, expiry/redaction, field audit | Không projection/event | Purpose-limited |
| Khiếu nại/Lost & Found | Confidential/PII | Role scope, custody/legal hold, encrypt evidence | ID/status only | Case/legal policy |
| AI prompt/result | Confidential/PII-derived | Minimize/redact, model/version, access audit | Không train ngoài policy | AI governance |
| Integration payload | Theo field cao nhất | Encrypt, schema allowlist, masking | Error chỉ digest | Dedupe/replay window |
| Audit | Audit-only | Append-only, segregated access | Không product AI | Compliance policy |

Anonymization giữ referential/financial integrity bằng pseudonymous subject ID; legal hold ngăn purge. Encryption key không lưu cùng ciphertext.

## 15. Multi-property and Business Date

| Nhóm | Property/BusinessDate/UTC | Legal Entity/Currency/Tax | Cross-property/report |
|---|---|---|---|
| Reservation/Stay/Group | MaKhachSan bắt buộc; creation fact và actual UTC; planned local dates | Rate/tax/currency snapshot | Transfer tạo Aggregate target mới |
| Availability/HK/Maintenance | Property bắt buộc; inventory/operation date; action UTC | Không tự sở hữu tax | Không duplicate Room master |
| Folio/Cashiering | Property và BusinessDate trên mọi financial fact; provider UTC | Legal entity/currency/tax snapshot/ref | Không cộng khác currency thiếu FX |
| Night Audit | Property+BusinessDate là business key; UTC checkpoints | Accounting period ref | Một active date/Property |
| Task/Guest Service | Property; business context date; SLA UTC | Charge ref nếu có | Explicit permission |
| Integration/Audit | Property copy từ subject; UTC bắt buộc | Không tự suy | Correlation giữ Property |
| Global lookup/config | Global hoặc Property override; effective time | Owner quyết định | Versioned inheritance |

Unique số chứng từ, availability, cashier shift và Night Audit gồm MaKhachSan. Reservation nhiều ngày dùng local-date semantics và room-night theo Property calendar. Late posting ghi BusinessDate hiện tại cùng OriginalBusinessDate/EffectiveServiceDate, không giả backdate. Consolidated report giữ Property, LegalEntity, Currency, BusinessDate và UTC; không merge transaction ledger.
---
## 16. Read Models and Projections

| Projection | Nguồn event | Độ trễ | Khóa/index | Denormalized | Rebuild/stale/failure | Command validation |
|---|---|---|---|---|---|---|
| SoDoPhong | readiness, assignment, block, Stay | 2–5 giây | Property+Room | room/type/status/guest masked/balance flag | Replay checkpoint; freshness; unknown-safe | Không |
| KeHoachPhong | Reservation/Stay/assignment/control | <=10 giây | Property+Room/Type+date | intervals/status | Date-range rebuild; stale banner | Không |
| DongThoiGianLuuTru | Stay/assignment/folio/task | <=5 giây | Property+Stay+time | timeline summaries | Subject replay; partial marker | Không |
| DanhSachKhachDen | Reservation/Stay/readiness | <=5 giây | Property+BusinessDate+status | guest masked/guarantee/room | Day rebuild; last refresh | Không |
| DanhSachKhachDi | Stay/Folio/POS/payment | <=5 giây | Property+date+readiness | balance/pending/risk | Day rebuild; unknown blocker | Không |
| DanhSachKhachDangLuuTru | Stay/assignment | <=5 giây | Property+status+guest | room/dates/VIP/privacy | Active-stay rebuild | Không |
| BangDieuHanhTienSanh | operational projections | <=10 giây | Property+BusinessDate | counts/alerts | Widget checkpoints | Không |
| BangDieuHanhBuongPhong | HK/Stay/Maintenance | <=3 giây | Property+room/status/priority | departure/block | Priority rebuild; stale badge | Không |
| BangTheoDoiSoDuKhach | Folio/payment/deposit | <=3 giây | Property+Folio/Stay | debit/credit/net/pending | Ledger replay/reconcile flag | Không |
| DanhSachCanSanSangTraPhong | Stay/Folio/POS/HK/key | <=3 giây | Property+departure/readiness | blocker list | Unknown if stale | Không |
| BangDoiSoatThuNgan | payment/deposit/refund/shift | <=10 giây | Property+shift+tender | provider/system/declared | Facts rebuild; mismatch exception | Không |
| BangNgoaiLeTichHop | job/attempt/exception | <=5 giây | status+severity+due | subject/attempt/owner | Integration-store rebuild | Không |
| BangKiemToanDem | run/checkpoint/exception/date | <=2 giây | Property+Date+step | blockers/signoff | Resume checkpoint; fail closed | Không |

Mỗi projection có `ThoiDiemCapNhatUtc`, source checkpoint/version và rebuild status. Rebuild ghi shadow generation rồi swap; failure giữ generation cũ với stale warning.

## 17. Enum and Lookup Classification

| Khái niệm | Loại | Lý do/lưu trữ |
|---|---|---|
| Reservation/Stay/Folio/Payment/Deposit/Refund/Cashier/HK/Task/Integration/NightAudit status | Enum | State machine hữu hạn; varchar code + mapping/check |
| Nguồn/kênh/loại đặt phòng | Lookup | Quản trị/effective/Property variation |
| Lý do hủy/no-show/room move/override | Lookup | Governance/reporting/reason required |
| Loại giấy tờ | Lookup | Jurisdiction/retention thay đổi |
| Phương thức thanh toán | Lookup | Provider/Property/effective configuration |
| Folio entry type | Enum | Debit/credit/reversal semantics ổn định |
| Charge code/Posting catalog | Lookup | Tax/GL/revenue mapping thay đổi |
| Tax code/rate | External Lookup | Accounting/Legal Entity sở hữu; posting snapshot |
| Cleaning type/priority | Lookup | Property configuration |
| Maintenance category/severity | Lookup | SLA/Property configuration |
| Task type/priority | Lookup | Routing/SLA configuration |
| Integration error class | Stable enum + Lookup detail | Retry class ổn định, operator type cấu hình |
| Night Audit blocker type | Lookup | Control configuration/version |
| OOO/OOS, GuestRole, source type | Enum | Hữu hạn và code-owned |

Lookup có Code, tên Việt/Anh, Property nullable, effective from/to, active/version, display order và audit. Không gom mọi lookup vào một generic table nếu làm mất type safety.

## 18. Archive and Deletion Policy

| Nhóm | Chính sách |
|---|---|
| Reservation/Stay/Group | Close/archive; anonymize snapshot khi policy; không hard delete |
| Identity document/signature | Encrypt, redact/anonymize, legal hold; CẦN XÁC NHẬN CHÍNH SÁCH PHÁP LÝ |
| Folio/Payment/Deposit/Refund | Append/reversal/close; không delete trong legal retention |
| Audit/event | Append-only; archive/partition; controlled redaction overlay |
| Integration payload | Purge ciphertext sau replay/dedupe window; giữ metadata/digest nếu cần |
| Lost & Found | Close/dispose/return; PII retention/legal hold riêng |
| Wake-up | Purge/anonymize outcome sau service/dispute window |
| HK/Maintenance | Hot period rồi archive; safety case policy riêng |
| Night Audit | Archive audit pack/checkpoint; không delete rollover |
| AI recommendation | Expire/anonymize; retain model/reason/decision metric tối thiểu |
| Lookup | Deactivate/supersede; không delete code đã reference |
| Projection | Truncate/rebuild; không legal source |

Mọi duration chưa có chính sách mang nhãn **CẦN XÁC NHẬN CHÍNH SÁCH PHÁP LÝ**.

## 19. Data Invariant Enforcement

| Invariant | Domain/Application | Database | Concurrency/reconcile | Chỉ DB đủ? |
|---|---|---|---|---|
| Departure sau Arrival | Rule + validation | Check | Version amend | Chỉ row; không timezone policy |
| Không overlap active assignment | Assignment service | Filter/unique hỗ trợ; range khó check | Serializable/lock bucket/conflict | Không |
| External source unique | Dedupe policy | Unique scoped | Inbox/reconcile | Gần đủ |
| Idempotency unique namespace | Application | Unique Property+namespace+key | Lease/outcome | Không cho payload semantics |
| Refund <= refundable | Refund service | amount >0 check | Lock/version source + provider reconcile | Không |
| Allocation <= deposit remaining | Deposit Root | amount >0 check | Aggregate transaction/version | Không |
| Closed Folio không nhận entry | Folio domain | FK không đủ | Lock/version Window | Không |
| Một active BusinessDate/Property | Night Audit | Unique Property row | Lease/version | Rollover ngoài DB |
| Một NightAuditRun/date | Domain | Filtered unique | Lease | Có cho uniqueness |
| Một cashier open shift | Domain | Filtered unique theo policy | Version | Policy cần domain |
| Availability không âm | Domain | Check quantities | Hot-bucket transaction | Không cross-row |
| Posted entry immutable | Domain/repository | Permission/append design | Audit/reconcile | Không |
| Money có currency | VO | Conditional check | — | Presence only |
| Cấm cross-Property link | Application | Compound FK nội bộ | Audit | Không external ref |

## 20. Capacity and Performance

| Nhóm | Mức | Hot/cold và đề xuất |
|---|---|---|
| Reservation/Stay/Task roots | Trung bình–cao | Hot active; archive closed; filtered operational indexes |
| Availability by date | Cao/hot | Partition candidate Property/date; concurrency test |
| Folio Entry | Rất cao | Append, partition BusinessDate/Property, compression candidate |
| Payment/refund/deposit attempts | Cao | Hot unresolved; archive reconciled |
| Audit | Rất cao | Partition time/Property/module; compressed cold store |
| Event/Outbox/Inbox | Rất cao | Small pending filtered index; controlled purge/archive |
| Integration attempt | Rất cao | Partition time/endpoint; retain exception metadata |
| HK/Maintenance history | Cao | Hot current day; archive completed |
| Night Audit checkpoint | Thấp–trung bình, critical | Active run hot; immutable audit pack |
| Projection | Cao/rebuildable | Screen-specific index; generation/checkpoint |
| Reporting history | Rất cao | Reporting DB/columnstore candidate; avoid OLTP scan |

Index maintenance, partition, compression, batch size và rebuild phải benchmark theo property-size scenarios; không tối ưu vật lý trước dữ liệu.
---
## 21. Domain-to-Database Mapping Matrix

| Domain Context | Aggregate | Entity/Value Object | Tên bảng SQL | Tên PK | Property Scope | Business Date | Concurrency | Audit | Outbox | PII |
|---|---|---|---|---|---|---|---|---|---|---|
| Reservation | Reservation | Reservation | DatPhong | MaDatPhong | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Có |
| Reservation | ReservationGuest | ReservationGuest | KhachDatPhong | MaKhachDatPhong | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Có |
| Reservation | ReservationRoomDemand | ReservationRoomDemand | NhuCauHangPhong | MaNhuCauHangPhong | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Reservation | ReservationRateSnapshot | ReservationRateSnapshot | AnhChupGiaDatPhong | MaAnhChupGiaDatPhong | Có | Inherited/UTC | Append-only/unique | Có | Không trực tiếp | Không |
| Reservation | ReservationGuaranteeRef | ReservationGuaranteeRef | ThamChieuBaoDamDatPhong | MaThamChieuBaoDamDatPhong | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Reservation | ReservationContact | ReservationContact | LienHeDatPhong | MaLienHeDatPhong | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Có |
| Reservation | ReservationNote | ReservationNote | GhiChuDatPhong | MaGhiChuDatPhong | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Có |
| Reservation | ReservationPreference | ReservationPreference | YeuCauUuTienDatPhong | MaYeuCauUuTienDatPhong | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Có |
| Reservation | ReservationChangeRecord | ReservationChangeRecord | LichSuThayDoiDatPhong | MaLichSuThayDoiDatPhong | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Không trực tiếp | Không |
| Reservation | WaitlistEntry | WaitlistEntry | ChoPhong | MaChoPhong | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Có |
| Reservation | WaitlistOfferAttempt | WaitlistOfferAttempt | LanMoiChoPhong | MaLanMoiChoPhong | Có | Inherited/UTC | Append-only/unique | Có | Không trực tiếp | Không |
| Stay Operations | Stay | Stay | LuuTru | MaLuuTru | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Có |
| Stay Operations | StayGuest | StayGuest | KhachLuuTru | MaKhachLuuTru | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Có |
| Stay Operations | StayExtension | StayExtension | GiaHanLuuTru | MaGiaHanLuuTru | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Không trực tiếp | Không |
| Stay Operations | StayRestriction | StayRestriction | HanCheLuuTru | MaHanCheLuuTru | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Có |
| Stay Operations | StayPostingPermission | StayPostingPermission | QuyenKyPhong | MaQuyenKyPhong | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Stay Operations | StayPrivacySetting | StayPrivacySetting | ThietLapRiengTuLuuTru | MaThietLapRiengTuLuuTru | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Có |
| Stay Operations | RoomAssignment | RoomAssignment | PhanPhong | MaPhanPhong | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Stay Operations | AssignmentSegment | AssignmentSegment | PhanDoanPhanPhong | MaPhanDoanPhanPhong | Có | Inherited/UTC | Append-only/unique | Có | Không trực tiếp | Không |
| Stay Operations | KeyCredential | KeyCredential | QuyenMoKhoaPhong | MaQuyenMoKhoaPhong | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Stay Operations | CredentialAttempt | CredentialAttempt | LanCapKhoa | MaLanCapKhoa | Có | Inherited/UTC | Append-only/unique | Có | Không trực tiếp | Không |
| Stay Operations | GuestSignature | GuestSignature | ChuKyKhachLuuTru | MaChuKyKhachLuuTru | Có | Inherited/UTC | Append-only/unique | Có | Không trực tiếp | Có |
| Rooms Availability | RoomAvailability | RoomAvailability | TonPhongKhaDung | MaTonPhongKhaDung | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Rooms Availability | AvailabilityHold | AvailabilityHold | GiuTonPhong | MaGiuTonPhong | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Rooms Availability | AvailabilityAllocation | AvailabilityAllocation | PhanBoTonPhong | MaPhanBoTonPhong | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Rooms Availability | RoomInventoryControl | RoomInventoryControl | KiemSoatTonPhong | MaKiemSoatTonPhong | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Rooms Availability | InventoryRestriction | InventoryRestriction | HanCheBanPhong | MaHanCheBanPhong | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Folio | Folio | Folio | SoGiaoDichKhach | MaSoGiaoDichKhach | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Folio | FolioWindow | FolioWindow | NganSoGiaoDich | MaNganSoGiaoDich | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Folio | FolioEntry | FolioEntry | ButToanSoGiaoDich | MaButToanSoGiaoDich | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Có khi phát fact | Không |
| Folio | FolioEntryTax | FolioEntryTax | ThueButToanSoGiaoDich | MaThueButToanSoGiaoDich | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Có khi phát fact | Không |
| Folio | FolioTransferLink | FolioTransferLink | LienKetChuyenButToan | MaLienKetChuyenButToan | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Có khi phát fact | Không |
| Folio | FolioAdjustmentLink | FolioAdjustmentLink | LienKetDieuChinhButToan | MaLienKetDieuChinhButToan | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Có khi phát fact | Không |
| Folio | RoutingInstruction | RoutingInstruction | ChiDanDinhTuyen | MaChiDanDinhTuyen | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Folio | RoutingCondition | RoutingCondition | DieuKienDinhTuyen | MaDieuKienDinhTuyen | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Cashiering | Payment | Payment | ThanhToan | MaThanhToan | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Cashiering | PaymentAttempt | PaymentAttempt | LanThuThanhToan | MaLanThuThanhToan | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Có khi phát fact | Không |
| Cashiering | Deposit | Deposit | DatCoc | MaDatCoc | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Cashiering | DepositAllocation | DepositAllocation | PhanBoDatCoc | MaPhanBoDatCoc | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Có khi phát fact | Không |
| Cashiering | RefundRequest | RefundRequest | YeuCauHoanTien | MaYeuCauHoanTien | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Cashiering | RefundApprovalStep | RefundApprovalStep | BuocPheDuyetHoanTien | MaBuocPheDuyetHoanTien | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Không trực tiếp | Không |
| Cashiering | RefundExecutionAttempt | RefundExecutionAttempt | LanThucHienHoanTien | MaLanThucHienHoanTien | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Có khi phát fact | Không |
| Cashiering | CashierShift | CashierShift | CaThuNgan | MaCaThuNgan | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Cashiering | CashierTenderSummary | CashierTenderSummary | TongHopPhuongThucThu | MaTongHopPhuongThucThu | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Cashiering | CashierVariance | CashierVariance | ChenhLechCaThuNgan | MaChenhLechCaThuNgan | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Không trực tiếp | Không |
| Housekeeping | RoomOperation | RoomOperation | VanHanhBuongPhong | MaVanHanhBuongPhong | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Housekeeping | RoomStatusTransition | RoomStatusTransition | LichSuTrangThaiPhong | MaLichSuTrangThaiPhong | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Không trực tiếp | Không |
| Housekeeping | RoomDiscrepancy | RoomDiscrepancy | SaiLechTrangThaiPhong | MaSaiLechTrangThaiPhong | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Housekeeping | HousekeepingTask | HousekeepingTask | CongViecBuongPhong | MaCongViecBuongPhong | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Housekeeping | CleaningWorkSegment | CleaningWorkSegment | PhienLamBuong | MaPhienLamBuong | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Không trực tiếp | Không |
| Housekeeping | CleaningChecklistItem | CleaningChecklistItem | MucKiemTraVeSinh | MaMucKiemTraVeSinh | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Housekeeping | RoomInspection | RoomInspection | KiemTraPhong | MaKiemTraPhong | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Housekeeping | InspectionFinding | InspectionFinding | PhatHienKiemPhong | MaPhatHienKiemPhong | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Maintenance | MaintenanceCase | MaintenanceCase | HoSoBaoTri | MaHoSoBaoTri | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Maintenance | MaintenanceWorkLog | MaintenanceWorkLog | NhatKyCongViecBaoTri | MaNhatKyCongViecBaoTri | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Không trực tiếp | Không |
| Maintenance | MaintenancePartRef | MaintenancePartRef | ThamChieuVatTuBaoTri | MaThamChieuVatTuBaoTri | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Maintenance | RoomOperationalBlock | RoomOperationalBlock | KhoaVanHanhPhong | MaKhoaVanHanhPhong | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Maintenance | BlockExtension | BlockExtension | GiaHanKhoaPhong | MaGiaHanKhoaPhong | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Không trực tiếp | Không |
| Guest Services | ServiceRequest | ServiceRequest | YeuCauDichVu | MaYeuCauDichVu | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Có |
| Guest Services | ServiceRequestItem | ServiceRequestItem | HangMucYeuCauDichVu | MaHangMucYeuCauDichVu | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Guest Services | ServiceCompletionEvidence | ServiceCompletionEvidence | BangChungHoanThanhDichVu | MaBangChungHoanThanhDichVu | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Không trực tiếp | Có |
| Guest Services | WakeUpCall | WakeUpCall | CuocGoiBaoThuc | MaCuocGoiBaoThuc | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Có |
| Guest Services | WakeUpAttempt | WakeUpAttempt | LanGoiBaoThuc | MaLanGoiBaoThuc | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Không trực tiếp | Không |
| Guest Services | LostFoundCase | LostFoundCase | HoSoDoThatLac | MaHoSoDoThatLac | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Có |
| Guest Services | LostFoundItem | LostFoundItem | VatPhamThatLac | MaVatPhamThatLac | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Có |
| Guest Services | CustodyHandover | CustodyHandover | BanGiaoLuuGiu | MaBanGiaoLuuGiu | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Không trực tiếp | Có |
| Guest Services | ClaimVerification | ClaimVerification | XacMinhNguoiNhanDo | MaXacMinhNguoiNhanDo | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Không trực tiếp | Có |
| Group Operations | GroupStay | GroupStay | LuuTruDoan | MaLuuTruDoan | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Có |
| Group Operations | GroupMemberLink | GroupMemberLink | LienKetThanhVienDoan | MaLienKetThanhVienDoan | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Có |
| Group Operations | RoomBlock | RoomBlock | KhoiPhongDoan | MaKhoiPhongDoan | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Group Operations | RoomBlockDay | RoomBlockDay | PhanBoKhoiPhongTheoNgay | MaPhanBoKhoiPhongTheoNgay | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Group Operations | RoomingList | RoomingList | DanhSachKhachDoan | MaDanhSachKhachDoan | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Có |
| Group Operations | RoomingMember | RoomingMember | ThanhVienDanhSachDoan | MaThanhVienDanhSachDoan | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Có |
| Group Operations | GroupBillingInstruction | GroupBillingInstruction | ChiDanThanhToanDoan | MaChiDanThanhToanDoan | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Group Operations | BillingRouteLine | BillingRouteLine | DinhTuyenThanhToanDoan | MaDinhTuyenThanhToanDoan | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Night Audit | PropertyBusinessDate | PropertyBusinessDate | NgayNghiepVuCoSo | MaNgayNghiepVuCoSo | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Night Audit | NightAuditRun | NightAuditRun | PhienKiemToanDem | MaPhienKiemToanDem | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Night Audit | AuditCheckpoint | AuditCheckpoint | DiemKiemSoatKiemToan | MaDiemKiemSoatKiemToan | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Không trực tiếp | Không |
| Night Audit | AuditSignoff | AuditSignoff | XacNhanKiemToan | MaXacNhanKiemToan | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Không trực tiếp | Không |
| Night Audit | AuditChecklist | AuditChecklist | DanhMucKiemToan | MaDanhMucKiemToan | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Night Audit | ChecklistItem | ChecklistItem | MucKiemToan | MaMucKiemToan | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Night Audit | AuditException | AuditException | NgoaiLeKiemToan | MaNgoaiLeKiemToan | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Không |
| Night Audit | ExceptionEvidence | ExceptionEvidence | BangChungNgoaiLe | MaBangChungNgoaiLe | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Không trực tiếp | Có |
| Operational Task | OperationalTask | OperationalTask | CongViecVanHanh | MaCongViecVanHanh | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Có |
| Operational Task | TaskAssignment | TaskAssignment | PhanCongCongViec | MaPhanCongCongViec | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Không trực tiếp | Không |
| Operational Task | TaskEvidence | TaskEvidence | BangChungCongViec | MaBangChungCongViec | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Không trực tiếp | Có |
| Operational Task | ShiftHandover | ShiftHandover | BanGiaoCa | MaBanGiaoCa | Có | Có hoặc inherited theo fact | rowversion | Có | Có khi phát fact | Có |
| Operational Task | HandoverItemSnapshot | HandoverItemSnapshot | HangMucBanGiao | MaHangMucBanGiao | Có | Inherited/UTC | Append-only/unique | Có | Không trực tiếp | Có |
| Device & Integration Operations | IntegrationJob | IntegrationJob | CongViecTichHop | MaCongViecTichHop | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Device & Integration Operations | IntegrationAttempt | IntegrationAttempt | LanThucHienTichHop | MaLanThucHienTichHop | Có | Inherited/UTC | Append-only/unique | Có | Không trực tiếp | Không |
| Device & Integration Operations | IntegrationException | IntegrationException | NgoaiLeTichHop | MaNgoaiLeTichHop | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Device & Integration Operations | ExceptionResolution | ExceptionResolution | XuLyNgoaiLeTichHop | MaXuLyNgoaiLeTichHop | Có | Inherited/UTC | Append-only/unique | Có | Không trực tiếp | Không |
| Device & Integration Operations | ExternalReference | ExternalReference | ThamChieuBenNgoai | MaThamChieuBenNgoai | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Device & Integration Operations | ExternalMappingVersion | ExternalMappingVersion | PhienBanAnhXaNgoai | MaPhienBanAnhXaNgoai | Có | Inherited/UTC | Append-only/unique | Có | Không trực tiếp | Không |
| Device & Integration Operations | IdempotencyRecord | IdempotencyRecord | BanGhiChongTrung | MaBanGhiChongTrung | Có | Inherited/UTC | Qua Root/rowversion khi mutable | Có | Không trực tiếp | Không |
| Device & Integration Operations | IdempotencyOutcome | IdempotencyOutcome | KetQuaChongTrung | MaKetQuaChongTrung | Có | Inherited/UTC | Append-only/unique | Có | Không trực tiếp | Không |
| Device & Integration Operations | OutboxMessage | OutboxMessage | ThongDiepOutbox | MaThongDiepOutbox | Có | Inherited/UTC | Append-only/unique | Có | Có khi phát fact | Không |
| Device & Integration Operations | InboxMessage | InboxMessage | ThongDiepInbox | MaThongDiepInbox | Có | Inherited/UTC | Append-only/unique | Có | Không trực tiếp | Không |
| Device & Integration Operations | ReplayRequest | ReplayRequest | YeuCauPhatLai | MaYeuCauPhatLai | Có | Inherited/UTC | Append-only/unique | Có | Không trực tiếp | Không |
| Device & Integration Operations | AuditRecord | AuditRecord | NhatKyKiemToan | MaNhatKyKiemToan | Có | Có hoặc inherited theo fact | Append-only/unique | Có | Không trực tiếp | Có |
| Cross-context VO | — | Money | Không tạo bảng riêng: Owned columns SoTien + MaTienTe (+ rounding/tax snapshot) | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | BusinessDate | Không tạo bảng riêng: Owned column NgayNghiepVu | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | ReservationPeriod | Không tạo bảng riêng: Owned columns NgayDen + NgayDi + timezone semantics | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | StayPeriod | Không tạo bảng riêng: Owned planned/actual date and UTC columns | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | RoomAssignmentPeriod | Không tạo bảng riêng: Owned ThoiDiemBatDauUtc/ThoiDiemKetThucUtc | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | KeyValidityPeriod | Không tạo bảng riêng: Owned validity UTC columns | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | GuestName | Không tạo bảng riêng: Owned snapshot columns or encrypted PII reference | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | ContactInformation | Không tạo bảng riêng: Owned normalized/encrypted contact columns | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | IdentityDocument | Không tạo bảng riêng: Không mặc định trong FO core table; secure child/object reference — quyết định mở | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | Occupancy | Không tạo bảng riêng: Owned SoNguoiLon/SoTreEm/... | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | RateQuote | Không tạo bảng riêng: Snapshot columns + child breakdown | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | TaxBreakdown | Không tạo bảng riêng: Child tax rows on immutable posting | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | FolioBalance | Không tạo bảng riêng: Owned summary columns; ledger-derived | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | ExternalSourceReference | Không tạo bảng riêng: Owned source system/type/id/version/hash | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | IdempotencyKey | Không tạo bảng riêng: Owned namespace/key/hash | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | CorrelationId | Không tạo bảng riêng: Owned MaTuongQuan | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | ReasonCode | Không tạo bảng riêng: Owned MaLyDo + safe note | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | ApprovalReference | Không tạo bảng riêng: Owned MaPheDuyet; details Audit Store | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | SLA | Không tạo bảng riêng: Owned due/calendar/policy version | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | ConfidenceScore | Không tạo bảng riêng: Owned score/model/time; AI external reference | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | TimeWindow | Không tạo bảng riêng: Owned UTC start/end + timezone when needed | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | Quantity | Không tạo bảng riêng: Owned SoLuong + MaDonViTinh | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |
| Cross-context VO | — | MaskedIdentifier | Không tạo bảng riêng: Owned masked/token/digest columns | — | Theo bảng cha | Theo bảng cha | Bất biến | Theo bảng cha | Không | Theo field |

Ma trận bao phủ toàn bộ 100 bảng từ Aggregate/Entity đã duyệt và các Value Object persistence quan trọng. Các VO không có lifecycle được ghi rõ “Không tạo bảng riêng/Owned columns”; không biến thành CRUD table.

## 22. Architecture Decision Log

| Mã | Vấn đề/phương án | Khuyến nghị | Lý do/ảnh hưởng | Mức | Cần xác nhận |
|---|---|---|---|---|---|
| ADR-DB-001 | GUID hay bigint | GUID cho Root/public child; bigint cho sequence/internal high-volume khi đo đạc | Offline, event, modular boundary; clustered strategy ở physical phase | Critical | Có |
| ADR-DB-002 | Schema theo module | Một schema logic cho mỗi Bounded Context | Ownership/quyền/migration rõ | Critical | Có |
| ADR-DB-003 | Tên schema SQL | Tên Việt không dấu hoặc English technical prefix | Phải thống nhất với toàn Asmile-BO | High | Có |
| ADR-DB-004 | FK xuyên context/module | Không mặc định; logical ref + snapshot | Giảm coupling/deploy lock | Critical | Có |
| ADR-DB-005 | Enum storage | varchar code có mapping/check; lookup typed table | Dễ đọc và governance | High | Có |
| ADR-DB-006 | Audit Store | Logical store chung, partition/ACL theo module | Không history table tràn lan | Critical | Có |
| ADR-DB-007 | Event Store | Không full event sourcing; Outbox/domain facts cần thiết | Domain hiện không thiết kế event-sourced Aggregate | High | Có |
| ADR-DB-008 | Soft delete | Không mặc định; close/deactivate/archive | Bảo toàn financial/audit truth | Critical | Không, trừ exception |
| ADR-DB-009 | Partition | Candidate cho ledger/audit/event/integration; chưa bật trước benchmark | Tránh tối ưu sớm | Medium | Có ở physical phase |
| ADR-DB-010 | Mã hóa PII | Field-level cho sensitive fields + TDE nền | Least privilege/masking/search design | Critical | Có, kèm key management |
| ADR-DB-011 | IdentityDocument | Encrypted structured child + secure object ref cho ảnh | Không JSON/raw event; retention riêng | Critical | Có |
| ADR-DB-012 | Money | decimal(19,4)+Currency; tax/FX snapshot | Không money/float | Critical | Không |
| ADR-DB-013 | Timezone | UTC timestamp + Property timezone/version; BusinessDate date | Tái diễn giải đúng DST/property | Critical | Có mapping standard |
| ADR-DB-014 | Business Date | PropertyBusinessDate Root, không suy clock | Night Audit consistency | Critical | Không |
| ADR-DB-015 | Folio ledger | Append-only FolioEntry; reversal/adjustment | Financial integrity | Critical | Không |
| ADR-DB-016 | Outbox/Inbox | Mỗi local context có durable store; unified ops projection | Atomic/dedupe/replay | Critical | Có physical topology |
| ADR-DB-017 | Read Model | Rebuildable projection với checkpoint/freshness | CQRS phù hợp, không command source | High | Không |
| ADR-DB-018 | Archive | Policy-based hot/cold; legal hold/anonymize | Volume và compliance | High | Có |
| ADR-DB-019 | Reporting DB | Candidate riêng/read replica/ETL contracts | Không tải OLTP/cross-module join | Medium | Có sau sizing |

## 23. Open Questions

### 23.1. MÂU THUẪN CẦN XÁC NHẬN

- **MC-001 (Critical):** Room/RoomType là Aggregate trong Business Dictionary hay external master theo Domain Model? Khuyến nghị external master đối với Front Office.
- **MC-002 (High):** Guest/Invoice/Notification trong architecture là collaboration scope hay ownership? Khuyến nghị ownership ngoài Front Office.
- **MC-003 (High):** Có dành riêng physical History/Audit/Archive table cho mọi Root không? Khuyến nghị không; dùng Audit Store/policy.
- **MC-004 (Critical):** Phê duyệt cặp tên RoomBlock/KhoiPhongDoan và RoomOperationalBlock/KhoaVanHanhPhong để tránh ambiguity.

### 23.2. Quyết định vận hành/pháp lý còn mở

1. Tenant/LegalEntity/Property/Branch hierarchy chính thức và schema naming convention toàn hệ thống.
2. Retention cụ thể cho PII, financial, audit, event, integration payload, Lost & Found, wake-up và AI — **CẦN XÁC NHẬN CHÍNH SÁCH PHÁP LÝ**.
3. Key management, searchable encryption/tokenization và secure object store cho giấy tờ/chữ ký.
4. PCI scope và provider token/reference contract.
5. NumberSequence format, uniqueness và hành vi gap theo Property/BusinessDate.
6. Offline write thực tế của WPF: phạm vi command nào được phép queue offline.
7. Volume baseline theo Property: rooms, reservations/day, postings/stay, POS lines, audit/event/day.
8. SQL schema names và deployment/migration ownership trong Modular Monolith.
9. Reporting topology, freshness SLA và consolidated currency conversion policy.
10. Physical enforcement cho interval overlap RoomAssignment: lock bucket, serializable range hay alternative allocation structure.
11. Accounting period/late-posting/reopen contract với Accounting.
12. AI prompt/result retention, consent và prohibited training policy.

## 24. Quality Checklist

| Kiểm tra | Kết quả |
|---|---|
| Mọi bảng có một owner | Đạt; 100/100 có context/aggregate owner |
| Không bảng có hai owner | Đạt; cross-module dùng logical reference |
| Entity không thành CRUD vô nghĩa | Đạt có điều kiện; collection/lifecycle mới có table |
| VO không tách không cần thiết | Đạt; VO mapping ghi owned columns |
| Aggregate persistence boundary | Đạt; cross-Root được đánh dấu logical/orchestrated |
| FK xuyên module/cascade nguy hiểm | Không dùng mặc định; cascade ledger/audit bị cấm |
| Financial data sửa trực tiếp | Bị cấm; reversal/adjustment/reconciliation |
| State string tự do | Bị cấm; enum/lookup governance |
| Money thiếu Currency | Bị cấm; amount+currency |
| UTC/timezone/BusinessDate mơ hồ | Đã phân tách; các policy mở được ghi |
| PII trong event/log | Bị cấm; ID/digest/minimal snapshot |
| Query quan trọng thiếu index logic | 20 index patterns đã nêu; phải benchmark |
| Unique thiếu Property scope | Các business numbers/date buckets gồm Property |
| Idempotency memory-only | Bị cấm; durable record/Inbox |
| Read Model làm write source | Bị cấm |
| Thuật ngữ lệch dictionary | Đã dùng tên chuẩn; 4 mâu thuẫn được công khai |
| Bảng không có PK/Property/audit | Data dictionary đã áp dụng cho 100 bảng |
| Retention pháp lý bị tự đoán | Không; đánh dấu CẦN XÁC NHẬN |

## 25. Final Recommendation

1. Phê duyệt bốn mâu thuẫn MC-001–MC-004 trước physical schema, đặc biệt ownership Room/RoomType.
2. Chốt ADR-DB-001–006, 010–013 và 016 trước khi tạo Entity/EF configuration; đây là quyết định khó đổi.
3. Thiết kế vật lý theo từng vertical slice: Foundation/Audit/Outbox → Reservation/Availability → Stay/Assignment/Key → Folio/Cashiering → HK/Maintenance → Group/Guest Services → Night Audit.
4. Với mỗi slice, tạo physical DDL design, constraints/index rationale, migration/recovery plan, seed lookup và data tests từ invariant catalog; chưa triển khai cho đến khi được phê duyệt.
5. Benchmark Availability, FolioEntry, Audit, Outbox/Inbox và IntegrationAttempt bằng workload nhiều Property trước quyết định clustered index/partition/compression.
6. Security review bắt buộc trước lưu IdentityDocument, signature, payment reference, private notes, Lost & Found evidence và AI payload.
7. Read Model triển khai bằng checkpoint/freshness/rebuild; command luôn load/revalidate write model.
8. Không tạo shared table hoặc cross-module write để rút ngắn giai đoạn đầu; integration debt ở các boundary tài chính/PII sẽ rất khó sửa về sau.

SẴN SÀNG THIẾT KẾ DATABASE VẬT LÝ FRONT OFFICE.
