# TỪ ĐIỂN NGHIỆP VỤ ASMILE-BO

## 0. Mục đích, phạm vi và thẩm quyền

Tài liệu này là chuẩn thuật ngữ bắt buộc cho Database, Domain, Entity, ViewModel, API, Report, AI và giao tiếp nghiệp vụ của Asmile-BO. Tài liệu **không thay đổi** Aggregate, invariant, Bounded Context hay ownership đã chốt trong `THIET_KE_DOMAIN_MODEL_FRONT_OFFICE_ASMILE_BO.md`; nó chỉ chuẩn hóa tên và ánh xạ giữa các lớp biểu đạt.

Nguyên tắc nền tảng là **một khái niệm – một tên chuẩn – một module sở hữu**. Tên đồng nghĩa chỉ được ghi nhận để cấm hoặc phục vụ tìm kiếm; không tạo thêm model song song.

### 0.1. Cách đọc bảng từ điển

| Cột | Ý nghĩa |
|---|---|
| Aggregate/Entity/Value Object/Event/Read Model | `Có` khi thuật ngữ chính là loại đó trong model chuẩn; `Không` trong các trường hợp còn lại |
| Database Table | `Có` nghĩa là có thể có persistence riêng khi thiết kế database; không đồng nghĩa bắt buộc “mỗi khái niệm một bảng” |
| Enum | `Có` khi là tập trạng thái/loại hữu hạn ổn định trong domain; nếu cấu hình theo Property thì phải là Lookup, không đóng cứng Enum |
| Lookup | `Có` khi là danh mục quản trị/tham chiếu có mã, hiệu lực và ownership rõ |
| Module sở hữu | Module duy nhất quyết định semantics và lifecycle; module khác chỉ giữ reference/snapshot/read projection |

---

## 1. Từ điển nghiệp vụ toàn Asmile-BO

Mỗi bảng dưới đây dùng cùng cấu trúc 12 thuộc tính bắt buộc.

### 1.1. Front Office — Front Office

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Front Office | Tiền sảnh | Khái niệm nghiệp vụ dùng để mô tả tiền sảnh; không tự động là đối tượng persistence. | Front Office | Không | Không | Không | Không | Không | Không | Không | Không |
| Property | Cơ sở khách sạn | Khái niệm nghiệp vụ dùng để mô tả cơ sở khách sạn; không tự động là đối tượng persistence. | Front Office | Không | Không | Không | Không | Không | Không | Không | Không |
| Property Scope | Phạm vi cơ sở | Giá trị bất biến xác định cơ sở khách sạn mà người dùng, command và dữ liệu Front Office được phép tác động. | Front Office | Không | Không | Có | Không | Không | Không | Không | Không |
| Arrival | Khách dự kiến đến | Mô hình đọc tối ưu cho tra cứu hoặc điều hành khách dự kiến đến; không được dùng để ghi thay Aggregate. | Front Office | Không | Không | Không | Không | Có | Không | Không | Không |
| Departure | Khách dự kiến đi | Mô hình đọc tối ưu cho tra cứu hoặc điều hành khách dự kiến đi; không được dùng để ghi thay Aggregate. | Front Office | Không | Không | Không | Không | Có | Không | Không | Không |
| In-House Guest | Khách đang lưu trú | Mô hình đọc tối ưu cho tra cứu hoặc điều hành khách đang lưu trú; không được dùng để ghi thay Aggregate. | Front Office | Không | Không | Không | Không | Có | Không | Không | Không |
| Front Desk | Quầy lễ tân | Khái niệm nghiệp vụ dùng để mô tả quầy lễ tân; không tự động là đối tượng persistence. | Front Office | Không | Không | Không | Không | Không | Không | Không | Không |
| Receptionist | Nhân viên lễ tân | Khái niệm nghiệp vụ dùng để mô tả nhân viên lễ tân; không tự động là đối tượng persistence. | Front Office | Không | Không | Không | Không | Không | Không | Không | Không |
| Front Office Supervisor | Giám sát tiền sảnh | Khái niệm nghiệp vụ dùng để mô tả giám sát tiền sảnh; không tự động là đối tượng persistence. | Front Office | Không | Không | Không | Không | Không | Không | Không | Không |
| Front Office Manager | Quản lý tiền sảnh | Khái niệm nghiệp vụ dùng để mô tả quản lý tiền sảnh; không tự động là đối tượng persistence. | Front Office | Không | Không | Không | Không | Không | Không | Không | Không |
| Room Rack | Sơ đồ phòng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành sơ đồ phòng; không được dùng để ghi thay Aggregate. | Front Office | Không | Không | Không | Không | Có | Không | Không | Không |
| Room Plan | Kế hoạch phòng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành kế hoạch phòng; không được dùng để ghi thay Aggregate. | Front Office | Không | Không | Không | Không | Có | Không | Không | Không |
| Stay Timeline | Dòng thời gian lưu trú | Mô hình đọc tối ưu cho tra cứu hoặc điều hành dòng thời gian lưu trú; không được dùng để ghi thay Aggregate. | Front Office | Không | Không | Không | Không | Có | Không | Không | Không |
| Arrival List | Danh sách khách đến | Mô hình đọc tối ưu cho tra cứu hoặc điều hành danh sách khách đến; không được dùng để ghi thay Aggregate. | Front Office | Không | Không | Không | Không | Có | Không | Không | Không |
| Departure List | Danh sách khách đi | Mô hình đọc tối ưu cho tra cứu hoặc điều hành danh sách khách đi; không được dùng để ghi thay Aggregate. | Front Office | Không | Không | Không | Không | Có | Không | Không | Không |
| VIP List | Danh sách khách VIP | Mô hình đọc tối ưu cho tra cứu hoặc điều hành danh sách khách vip; không được dùng để ghi thay Aggregate. | Front Office | Không | Không | Không | Không | Có | Không | Không | Không |
| Overstay List | Danh sách khách quá hạn | Mô hình đọc tối ưu cho tra cứu hoặc điều hành danh sách khách quá hạn; không được dùng để ghi thay Aggregate. | Front Office | Không | Không | Không | Không | Có | Không | Không | Không |
| No-Show List | Danh sách không đến | Mô hình đọc tối ưu cho tra cứu hoặc điều hành danh sách không đến; không được dùng để ghi thay Aggregate. | Front Office | Không | Không | Không | Không | Có | Không | Không | Không |
| Front Office Dashboard | Bảng điều hành tiền sảnh | Mô hình đọc tối ưu cho tra cứu hoặc điều hành bảng điều hành tiền sảnh; không được dùng để ghi thay Aggregate. | Front Office | Không | Không | Không | Không | Có | Không | Không | Không |
| Checkout Readiness | Mức sẵn sàng trả phòng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành mức sẵn sàng trả phòng; không được dùng để ghi thay Aggregate. | Front Office | Không | Không | Không | Không | Có | Không | Không | Không |
| Front Office Shift | Ca tiền sảnh | Khái niệm nghiệp vụ dùng để mô tả ca tiền sảnh; không tự động là đối tượng persistence. | Front Office | Không | Không | Không | Không | Không | Không | Không | Không |
| Front Office Logbook | Sổ giao ca tiền sảnh | Mô hình đọc tổng hợp ShiftHandover và OperationalTask phục vụ giao ca; không tạo Aggregate mới ngoài Domain Model. | Front Office | Không | Không | Không | Không | Có | Không | Không | Không |
| Duty Manager | Quản lý trực | Khái niệm nghiệp vụ dùng để mô tả quản lý trực; không tự động là đối tượng persistence. | Front Office | Không | Không | Không | Không | Không | Không | Không | Không |
| Guest Journey | Hành trình khách | Khái niệm nghiệp vụ dùng để mô tả hành trình khách; không tự động là đối tượng persistence. | Front Office | Không | Không | Không | Không | Không | Không | Không | Không |
| Operational Day | Ngày vận hành | Khái niệm nghiệp vụ dùng để mô tả ngày vận hành; không tự động là đối tượng persistence. | Front Office | Không | Không | Không | Không | Không | Không | Không | Không |

### 1.2. Reservation — Đặt phòng

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Reservation | Đặt phòng | Aggregate quản lý đặt phòng và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Reservation | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Reservation Guest | Khách trong đặt phòng | Thành phần có định danh nghiệp vụ dùng để ghi nhận khách trong đặt phòng bên trong Aggregate sở hữu. | Reservation | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Reservation Room Demand | Nhu cầu hạng phòng | Thành phần có định danh nghiệp vụ dùng để ghi nhận nhu cầu hạng phòng bên trong Aggregate sở hữu. | Reservation | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Reservation Rate Snapshot | Ảnh chụp giá đặt phòng | Thành phần có định danh nghiệp vụ dùng để ghi nhận ảnh chụp giá đặt phòng bên trong Aggregate sở hữu. | Reservation | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Reservation Guarantee Reference | Tham chiếu bảo đảm đặt phòng | Thành phần có định danh nghiệp vụ dùng để ghi nhận tham chiếu bảo đảm đặt phòng bên trong Aggregate sở hữu. | Reservation | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Reservation Contact | Đầu mối liên hệ đặt phòng | Thành phần có định danh nghiệp vụ dùng để ghi nhận đầu mối liên hệ đặt phòng bên trong Aggregate sở hữu. | Reservation | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Reservation Note | Ghi chú đặt phòng | Thành phần có định danh nghiệp vụ dùng để ghi nhận ghi chú đặt phòng bên trong Aggregate sở hữu. | Reservation | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Reservation Preference | Yêu cầu ưu tiên đặt phòng | Thành phần có định danh nghiệp vụ dùng để ghi nhận yêu cầu ưu tiên đặt phòng bên trong Aggregate sở hữu. | Reservation | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Reservation Change Record | Lịch sử thay đổi đặt phòng | Thành phần có định danh nghiệp vụ dùng để ghi nhận lịch sử thay đổi đặt phòng bên trong Aggregate sở hữu. | Reservation | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Reservation Period | Khoảng thời gian đặt phòng | Giá trị bất biến biểu diễn khoảng thời gian đặt phòng, được so sánh theo nội dung và không có lifecycle độc lập. | Reservation | Không | Không | Có | Không | Không | Không | Không | Không |
| Confirmation Number | Số xác nhận đặt phòng | Giá trị bất biến biểu diễn số xác nhận đặt phòng, được so sánh theo nội dung và không có lifecycle độc lập. | Reservation | Không | Không | Có | Không | Không | Không | Không | Không |
| Reservation Status | Trạng thái đặt phòng | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái đặt phòng; thay đổi phải theo governance domain. | Reservation | Không | Không | Không | Không | Không | Không | Có | Không |
| Reservation Source | Nguồn đặt phòng | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho nguồn đặt phòng. | Reservation | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Reservation Channel | Kênh đặt phòng | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho kênh đặt phòng. | Reservation | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Reservation Type | Loại đặt phòng | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho loại đặt phòng. | Reservation | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Reservation Created | Đặt phòng đã được tạo | Sự kiện ở thì quá khứ xác nhận đặt phòng đã được tạo đã thực sự xảy ra trong module sở hữu. | Reservation | Không | Không | Không | Có | Không | Không | Không | Không |
| Reservation Confirmed | Đặt phòng đã được xác nhận | Sự kiện ở thì quá khứ xác nhận đặt phòng đã được xác nhận đã thực sự xảy ra trong module sở hữu. | Reservation | Không | Không | Không | Có | Không | Không | Không | Không |
| Reservation Modified | Đặt phòng đã được sửa | Sự kiện ở thì quá khứ xác nhận đặt phòng đã được sửa đã thực sự xảy ra trong module sở hữu. | Reservation | Không | Không | Không | Có | Không | Không | Không | Không |
| Reservation Cancelled | Đặt phòng đã được hủy | Sự kiện ở thì quá khứ xác nhận đặt phòng đã được hủy đã thực sự xảy ra trong module sở hữu. | Reservation | Không | Không | Không | Có | Không | Không | Không | Không |
| Reservation Reinstated | Đặt phòng đã được khôi phục | Sự kiện ở thì quá khứ xác nhận đặt phòng đã được khôi phục đã thực sự xảy ra trong module sở hữu. | Reservation | Không | Không | Không | Có | Không | Không | Không | Không |
| Reservation Search Result | Kết quả tìm kiếm đặt phòng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành kết quả tìm kiếm đặt phòng; không được dùng để ghi thay Aggregate. | Reservation | Không | Không | Không | Không | Có | Không | Không | Không |
| Reservation History | Lịch sử đặt phòng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành lịch sử đặt phòng; không được dùng để ghi thay Aggregate. | Reservation | Không | Không | Không | Không | Có | Không | Không | Không |
| Waitlist Entry | Yêu cầu chờ phòng | Aggregate quản lý yêu cầu chờ phòng và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Reservation | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Waitlist Offer Attempt | Lần mời từ danh sách chờ | Thành phần có định danh nghiệp vụ dùng để ghi nhận lần mời từ danh sách chờ bên trong Aggregate sở hữu. | Reservation | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Waitlist Status | Trạng thái chờ phòng | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái chờ phòng; thay đổi phải theo governance domain. | Reservation | Không | Không | Không | Không | Không | Không | Có | Không |
| Cancellation Policy | Chính sách hủy phòng | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho chính sách hủy phòng. | Reservation | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Cancellation Reason | Lý do hủy phòng | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho lý do hủy phòng. | Reservation | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| No-Show Policy | Chính sách không đến | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho chính sách không đến. | Reservation | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Booking Pace | Tốc độ nhận đặt phòng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành tốc độ nhận đặt phòng; không được dùng để ghi thay Aggregate. | Reservation | Không | Không | Không | Không | Có | Không | Không | Không |
| Reservation Forecast | Dự báo đặt phòng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành dự báo đặt phòng; không được dùng để ghi thay Aggregate. | Reservation | Không | Không | Không | Không | Có | Không | Không | Không |

### 1.3. Stay Operations — Vận hành lưu trú

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Stay | Lưu trú | Aggregate quản lý lưu trú và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Stay Operations | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Stay Guest | Khách tham gia lưu trú | Thành phần có định danh nghiệp vụ dùng để ghi nhận khách tham gia lưu trú bên trong Aggregate sở hữu. | Stay Operations | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Primary Guest | Khách chính | Khái niệm nghiệp vụ dùng để mô tả khách chính; không tự động là đối tượng persistence. | Stay Operations | Không | Không | Không | Không | Không | Không | Không | Không |
| Accompanying Guest | Khách đi cùng | Khái niệm nghiệp vụ dùng để mô tả khách đi cùng; không tự động là đối tượng persistence. | Stay Operations | Không | Không | Không | Không | Không | Không | Không | Không |
| Stay Period | Khoảng thời gian lưu trú | Giá trị bất biến biểu diễn khoảng thời gian lưu trú, được so sánh theo nội dung và không có lifecycle độc lập. | Stay Operations | Không | Không | Có | Không | Không | Không | Không | Không |
| Stay Extension | Lần gia hạn lưu trú | Thành phần có định danh nghiệp vụ dùng để ghi nhận lần gia hạn lưu trú bên trong Aggregate sở hữu. | Stay Operations | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Stay Restriction | Hạn chế lưu trú | Thành phần có định danh nghiệp vụ dùng để ghi nhận hạn chế lưu trú bên trong Aggregate sở hữu. | Stay Operations | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Stay Posting Permission | Quyền ký phòng | Thành phần có định danh nghiệp vụ dùng để ghi nhận quyền ký phòng bên trong Aggregate sở hữu. | Stay Operations | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Stay Privacy Setting | Thiết lập riêng tư lưu trú | Thành phần có định danh nghiệp vụ dùng để ghi nhận thiết lập riêng tư lưu trú bên trong Aggregate sở hữu. | Stay Operations | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Stay Status | Trạng thái lưu trú | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái lưu trú; thay đổi phải theo governance domain. | Stay Operations | Không | Không | Không | Không | Không | Không | Có | Không |
| Check-In | Nhận phòng | Khái niệm nghiệp vụ dùng để mô tả nhận phòng; không tự động là đối tượng persistence. | Stay Operations | Không | Không | Không | Không | Không | Không | Không | Không |
| Check-Out | Trả phòng | Khái niệm nghiệp vụ dùng để mô tả trả phòng; không tự động là đối tượng persistence. | Stay Operations | Không | Không | Không | Không | Không | Không | Không | Không |
| Walk-In | Khách đến không đặt trước | Khái niệm nghiệp vụ dùng để mô tả khách đến không đặt trước; không tự động là đối tượng persistence. | Stay Operations | Không | Không | Không | Không | Không | Không | Không | Không |
| Day Use | Lưu trú trong ngày | Khái niệm nghiệp vụ dùng để mô tả lưu trú trong ngày; không tự động là đối tượng persistence. | Stay Operations | Không | Không | Không | Không | Không | Không | Không | Không |
| Hourly Stay | Lưu trú theo giờ | Khái niệm nghiệp vụ dùng để mô tả lưu trú theo giờ; không tự động là đối tượng persistence. | Stay Operations | Không | Không | Không | Không | Không | Không | Không | Không |
| Early Check-In | Nhận phòng sớm | Khái niệm nghiệp vụ dùng để mô tả nhận phòng sớm; không tự động là đối tượng persistence. | Stay Operations | Không | Không | Không | Không | Không | Không | Không | Không |
| Late Check-Out | Trả phòng muộn | Khái niệm nghiệp vụ dùng để mô tả trả phòng muộn; không tự động là đối tượng persistence. | Stay Operations | Không | Không | Không | Không | Không | Không | Không | Không |
| Extend Stay | Gia hạn lưu trú | Khái niệm nghiệp vụ dùng để mô tả gia hạn lưu trú; không tự động là đối tượng persistence. | Stay Operations | Không | Không | Không | Không | Không | Không | Không | Không |
| Shorten Stay | Rút ngắn lưu trú | Khái niệm nghiệp vụ dùng để mô tả rút ngắn lưu trú; không tự động là đối tượng persistence. | Stay Operations | Không | Không | Không | Không | Không | Không | Không | Không |
| Stay Checked In | Lưu trú đã nhận phòng | Sự kiện ở thì quá khứ xác nhận lưu trú đã nhận phòng đã thực sự xảy ra trong module sở hữu. | Stay Operations | Không | Không | Không | Có | Không | Không | Không | Không |
| Stay Extended | Lưu trú đã được gia hạn | Sự kiện ở thì quá khứ xác nhận lưu trú đã được gia hạn đã thực sự xảy ra trong module sở hữu. | Stay Operations | Không | Không | Không | Có | Không | Không | Không | Không |
| Stay Checked Out | Lưu trú đã trả phòng | Sự kiện ở thì quá khứ xác nhận lưu trú đã trả phòng đã thực sự xảy ra trong module sở hữu. | Stay Operations | Không | Không | Không | Có | Không | Không | Không | Không |
| Stay Reopened | Lưu trú đã được mở lại | Sự kiện ở thì quá khứ xác nhận lưu trú đã được mở lại đã thực sự xảy ra trong module sở hữu. | Stay Operations | Không | Không | Không | Có | Không | Không | Không | Không |
| In-House Stay Result | Kết quả lưu trú hiện tại | Mô hình đọc tối ưu cho tra cứu hoặc điều hành kết quả lưu trú hiện tại; không được dùng để ghi thay Aggregate. | Stay Operations | Không | Không | Không | Không | Có | Không | Không | Không |
| Guest Stay History | Lịch sử lưu trú của khách | Mô hình đọc tối ưu cho tra cứu hoặc điều hành lịch sử lưu trú của khách; không được dùng để ghi thay Aggregate. | Stay Operations | Không | Không | Không | Không | Có | Không | Không | Không |
| Registration Card | Phiếu đăng ký lưu trú | Mô hình đọc tối ưu cho tra cứu hoặc điều hành phiếu đăng ký lưu trú; không được dùng để ghi thay Aggregate. | Stay Operations | Không | Không | Không | Không | Có | Không | Không | Không |
| Guest Signature | Chữ ký khách lưu trú | Thành phần có định danh nghiệp vụ dùng để ghi nhận chữ ký khách lưu trú bên trong Aggregate sở hữu. | Stay Operations | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Identity Verification | Xác minh danh tính | Khái niệm nghiệp vụ dùng để mô tả xác minh danh tính; không tự động là đối tượng persistence. | Stay Operations | Không | Không | Không | Không | Không | Không | Không | Không |
| Stay Origin | Nguồn hình thành lưu trú | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho nguồn hình thành lưu trú; thay đổi phải theo governance domain. | Stay Operations | Không | Không | Không | Không | Không | Không | Có | Không |
| Stay Summary | Tóm tắt lưu trú | Mô hình đọc tối ưu cho tra cứu hoặc điều hành tóm tắt lưu trú; không được dùng để ghi thay Aggregate. | Stay Operations | Không | Không | Không | Không | Có | Không | Không | Không |

### 1.4. Rooms Availability — Phòng và tồn phòng

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Room | Phòng | Aggregate quản lý phòng và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Rooms Availability | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Room Type | Hạng phòng | Aggregate quản lý hạng phòng và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Rooms Availability | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Room Feature | Đặc tính phòng | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho đặc tính phòng. | Rooms Availability | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Room Amenity | Tiện nghi phòng | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho tiện nghi phòng. | Rooms Availability | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Room Capacity | Sức chứa phòng | Giá trị bất biến biểu diễn sức chứa phòng, được so sánh theo nội dung và không có lifecycle độc lập. | Rooms Availability | Không | Không | Có | Không | Không | Không | Không | Không |
| Room Location | Vị trí phòng | Giá trị bất biến biểu diễn vị trí phòng, được so sánh theo nội dung và không có lifecycle độc lập. | Rooms Availability | Không | Không | Có | Không | Không | Không | Không | Không |
| Room Number | Số phòng | Giá trị bất biến biểu diễn số phòng, được so sánh theo nội dung và không có lifecycle độc lập. | Rooms Availability | Không | Không | Có | Không | Không | Không | Không | Không |
| Room Status | Trạng thái phòng tổng hợp | Mô hình đọc tối ưu cho tra cứu hoặc điều hành trạng thái phòng tổng hợp; không được dùng để ghi thay Aggregate. | Rooms Availability | Không | Không | Không | Không | Có | Không | Không | Không |
| Room Availability | Tồn phòng khả dụng | Aggregate quản lý tồn phòng khả dụng và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Rooms Availability | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Availability Hold | Giữ tồn phòng | Thành phần có định danh nghiệp vụ dùng để ghi nhận giữ tồn phòng bên trong Aggregate sở hữu. | Rooms Availability | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Availability Allocation | Phân bổ tồn phòng | Thành phần có định danh nghiệp vụ dùng để ghi nhận phân bổ tồn phòng bên trong Aggregate sở hữu. | Rooms Availability | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Room Inventory Control | Kiểm soát tồn phòng | Aggregate quản lý kiểm soát tồn phòng và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Rooms Availability | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Inventory Restriction | Hạn chế bán phòng | Thành phần có định danh nghiệp vụ dùng để ghi nhận hạn chế bán phòng bên trong Aggregate sở hữu. | Rooms Availability | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Overbooking Limit | Ngưỡng bán vượt phòng | Giá trị bất biến biểu diễn ngưỡng bán vượt phòng, được so sánh theo nội dung và không có lifecycle độc lập. | Rooms Availability | Không | Không | Có | Không | Không | Không | Không | Không |
| Stop Sell | Dừng bán | Khái niệm nghiệp vụ dùng để mô tả dừng bán; không tự động là đối tượng persistence. | Rooms Availability | Không | Không | Không | Không | Không | Không | Không | Không |
| Close To Arrival | Đóng nhận ngày đến | Khái niệm nghiệp vụ dùng để mô tả đóng nhận ngày đến; không tự động là đối tượng persistence. | Rooms Availability | Không | Không | Không | Không | Không | Không | Không | Không |
| Close To Departure | Đóng nhận ngày đi | Khái niệm nghiệp vụ dùng để mô tả đóng nhận ngày đi; không tự động là đối tượng persistence. | Rooms Availability | Không | Không | Không | Không | Không | Không | Không | Không |
| Minimum Length Of Stay | Số đêm tối thiểu | Giá trị bất biến biểu diễn số đêm tối thiểu, được so sánh theo nội dung và không có lifecycle độc lập. | Rooms Availability | Không | Không | Có | Không | Không | Không | Không | Không |
| Maximum Length Of Stay | Số đêm tối đa | Giá trị bất biến biểu diễn số đêm tối đa, được so sánh theo nội dung và không có lifecycle độc lập. | Rooms Availability | Không | Không | Có | Không | Không | Không | Không | Không |
| Room Availability Changed | Tồn phòng đã thay đổi | Sự kiện ở thì quá khứ xác nhận tồn phòng đã thay đổi đã thực sự xảy ra trong module sở hữu. | Rooms Availability | Không | Không | Không | Có | Không | Không | Không | Không |
| Room Availability Calendar | Lịch tồn phòng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành lịch tồn phòng; không được dùng để ghi thay Aggregate. | Rooms Availability | Không | Không | Không | Không | Có | Không | Không | Không |
| Room Type Availability | Tồn theo hạng phòng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành tồn theo hạng phòng; không được dùng để ghi thay Aggregate. | Rooms Availability | Không | Không | Không | Không | Có | Không | Không | Không |
| Occupancy Forecast | Dự báo công suất phòng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành dự báo công suất phòng; không được dùng để ghi thay Aggregate. | Rooms Availability | Không | Không | Không | Không | Có | Không | Không | Không |
| Sellable Inventory | Tồn phòng có thể bán | Khái niệm nghiệp vụ dùng để mô tả tồn phòng có thể bán; không tự động là đối tượng persistence. | Rooms Availability | Không | Không | Không | Không | Không | Không | Không | Không |
| Physical Inventory | Tồn phòng vật lý | Khái niệm nghiệp vụ dùng để mô tả tồn phòng vật lý; không tự động là đối tượng persistence. | Rooms Availability | Không | Không | Không | Không | Không | Không | Không | Không |

### 1.5. Housekeeping — Buồng phòng

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Room Operation | Vận hành buồng phòng | Aggregate quản lý vận hành buồng phòng và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Housekeeping | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Room Operational Status | Trạng thái vận hành phòng | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái vận hành phòng; thay đổi phải theo governance domain. | Housekeeping | Không | Không | Không | Không | Không | Không | Có | Không |
| Room Status Transition | Lần chuyển trạng thái phòng | Thành phần có định danh nghiệp vụ dùng để ghi nhận lần chuyển trạng thái phòng bên trong Aggregate sở hữu. | Housekeeping | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Room Discrepancy | Sai lệch trạng thái phòng | Thành phần có định danh nghiệp vụ dùng để ghi nhận sai lệch trạng thái phòng bên trong Aggregate sở hữu. | Housekeeping | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Housekeeping Task | Công việc buồng phòng | Aggregate quản lý công việc buồng phòng và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Housekeeping | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Cleaning Work Segment | Phiên thực hiện vệ sinh | Thành phần có định danh nghiệp vụ dùng để ghi nhận phiên thực hiện vệ sinh bên trong Aggregate sở hữu. | Housekeeping | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Cleaning Checklist Item | Mục kiểm tra vệ sinh | Thành phần có định danh nghiệp vụ dùng để ghi nhận mục kiểm tra vệ sinh bên trong Aggregate sở hữu. | Housekeeping | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Room Inspection | Kiểm tra phòng | Aggregate quản lý kiểm tra phòng và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Housekeeping | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Inspection Finding | Phát hiện khi kiểm phòng | Thành phần có định danh nghiệp vụ dùng để ghi nhận phát hiện khi kiểm phòng bên trong Aggregate sở hữu. | Housekeeping | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Cleaning Status | Trạng thái vệ sinh | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái vệ sinh; thay đổi phải theo governance domain. | Housekeeping | Không | Không | Không | Không | Không | Không | Có | Không |
| Dirty Room | Phòng bẩn | Khái niệm nghiệp vụ dùng để mô tả phòng bẩn; không tự động là đối tượng persistence. | Housekeeping | Không | Không | Không | Không | Không | Không | Không | Không |
| Cleaning Room | Phòng đang làm | Khái niệm nghiệp vụ dùng để mô tả phòng đang làm; không tự động là đối tượng persistence. | Housekeeping | Không | Không | Không | Không | Không | Không | Không | Không |
| Clean Room | Phòng sạch | Khái niệm nghiệp vụ dùng để mô tả phòng sạch; không tự động là đối tượng persistence. | Housekeeping | Không | Không | Không | Không | Không | Không | Không | Không |
| Inspected Room | Phòng đã kiểm | Khái niệm nghiệp vụ dùng để mô tả phòng đã kiểm; không tự động là đối tượng persistence. | Housekeeping | Không | Không | Không | Không | Không | Không | Không | Không |
| Ready Room | Phòng sẵn sàng | Khái niệm nghiệp vụ dùng để mô tả phòng sẵn sàng; không tự động là đối tượng persistence. | Housekeeping | Không | Không | Không | Không | Không | Không | Không | Không |
| Do Not Disturb | Không làm phiền | Khái niệm nghiệp vụ dùng để mô tả không làm phiền; không tự động là đối tượng persistence. | Housekeeping | Không | Không | Không | Không | Không | Không | Không | Không |
| Turndown Service | Dịch vụ chỉnh trang buổi tối | Khái niệm nghiệp vụ dùng để mô tả dịch vụ chỉnh trang buổi tối; không tự động là đối tượng persistence. | Housekeeping | Không | Không | Không | Không | Không | Không | Không | Không |
| Housekeeping Zone | Khu vực buồng phòng | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho khu vực buồng phòng. | Housekeeping | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Housekeeping Priority | Mức ưu tiên buồng phòng | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho mức ưu tiên buồng phòng. | Housekeeping | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Cleaning Type | Loại vệ sinh | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho loại vệ sinh. | Housekeeping | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Housekeeping Task Assigned | Công việc buồng phòng đã được giao | Sự kiện ở thì quá khứ xác nhận công việc buồng phòng đã được giao đã thực sự xảy ra trong module sở hữu. | Housekeeping | Không | Không | Không | Có | Không | Không | Không | Không |
| Housekeeping Task Completed | Công việc buồng phòng đã hoàn tất | Sự kiện ở thì quá khứ xác nhận công việc buồng phòng đã hoàn tất đã thực sự xảy ra trong module sở hữu. | Housekeeping | Không | Không | Không | Có | Không | Không | Không | Không |
| Room Inspection Passed | Phòng đã kiểm đạt | Sự kiện ở thì quá khứ xác nhận phòng đã kiểm đạt đã thực sự xảy ra trong module sở hữu. | Housekeeping | Không | Không | Không | Có | Không | Không | Không | Không |
| Housekeeping Board | Bảng điều hành buồng phòng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành bảng điều hành buồng phòng; không được dùng để ghi thay Aggregate. | Housekeeping | Không | Không | Không | Không | Có | Không | Không | Không |
| Room Readiness List | Danh sách phòng sẵn sàng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành danh sách phòng sẵn sàng; không được dùng để ghi thay Aggregate. | Housekeeping | Không | Không | Không | Không | Có | Không | Không | Không |

### 1.6. Maintenance — Bảo trì

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Maintenance Case | Hồ sơ bảo trì | Aggregate quản lý hồ sơ bảo trì và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Maintenance | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Maintenance Work Log | Nhật ký công việc bảo trì | Thành phần có định danh nghiệp vụ dùng để ghi nhận nhật ký công việc bảo trì bên trong Aggregate sở hữu. | Maintenance | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Maintenance Part Reference | Tham chiếu vật tư bảo trì | Thành phần có định danh nghiệp vụ dùng để ghi nhận tham chiếu vật tư bảo trì bên trong Aggregate sở hữu. | Maintenance | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Room Operational Block | Khóa vận hành phòng | Aggregate quản lý khóa vận hành phòng và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Maintenance | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Block Extension | Lần gia hạn khóa phòng | Thành phần có định danh nghiệp vụ dùng để ghi nhận lần gia hạn khóa phòng bên trong Aggregate sở hữu. | Maintenance | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Maintenance Severity | Mức độ sự cố | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho mức độ sự cố. | Maintenance | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Maintenance Priority | Mức ưu tiên bảo trì | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho mức ưu tiên bảo trì. | Maintenance | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Maintenance Category | Nhóm bảo trì | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho nhóm bảo trì. | Maintenance | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Out Of Order | Ngưng khai thác | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho ngưng khai thác; thay đổi phải theo governance domain. | Maintenance | Không | Không | Không | Không | Không | Không | Có | Không |
| Out Of Service | Tạm ngưng phục vụ | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho tạm ngưng phục vụ; thay đổi phải theo governance domain. | Maintenance | Không | Không | Không | Không | Không | Không | Có | Không |
| Preventive Maintenance | Bảo trì phòng ngừa | Khái niệm nghiệp vụ dùng để mô tả bảo trì phòng ngừa; không tự động là đối tượng persistence. | Maintenance | Không | Không | Không | Không | Không | Không | Không | Không |
| Corrective Maintenance | Bảo trì khắc phục | Khái niệm nghiệp vụ dùng để mô tả bảo trì khắc phục; không tự động là đối tượng persistence. | Maintenance | Không | Không | Không | Không | Không | Không | Không | Không |
| Emergency Maintenance | Bảo trì khẩn cấp | Khái niệm nghiệp vụ dùng để mô tả bảo trì khẩn cấp; không tự động là đối tượng persistence. | Maintenance | Không | Không | Không | Không | Không | Không | Không | Không |
| Work Order | Lệnh công việc bảo trì | Khái niệm nghiệp vụ dùng để mô tả lệnh công việc bảo trì; không tự động là đối tượng persistence. | Maintenance | Không | Không | Không | Không | Không | Không | Không | Không |
| Asset Downtime | Thời gian tài sản ngừng hoạt động | Giá trị bất biến biểu diễn thời gian tài sản ngừng hoạt động, được so sánh theo nội dung và không có lifecycle độc lập. | Maintenance | Không | Không | Có | Không | Không | Không | Không | Không |
| Maintenance Case Reported | Sự cố bảo trì đã được báo | Sự kiện ở thì quá khứ xác nhận sự cố bảo trì đã được báo đã thực sự xảy ra trong module sở hữu. | Maintenance | Không | Không | Không | Có | Không | Không | Không | Không |
| Room Block Activated | Khóa phòng đã có hiệu lực | Sự kiện ở thì quá khứ xác nhận khóa phòng đã có hiệu lực đã thực sự xảy ra trong module sở hữu. | Maintenance | Không | Không | Không | Có | Không | Không | Không | Không |
| Room Block Released | Khóa phòng đã được giải phóng | Sự kiện ở thì quá khứ xác nhận khóa phòng đã được giải phóng đã thực sự xảy ra trong module sở hữu. | Maintenance | Không | Không | Không | Có | Không | Không | Không | Không |
| Maintenance Board | Bảng điều hành bảo trì | Mô hình đọc tối ưu cho tra cứu hoặc điều hành bảng điều hành bảo trì; không được dùng để ghi thay Aggregate. | Maintenance | Không | Không | Không | Không | Có | Không | Không | Không |
| Out Of Order Room List | Danh sách phòng ngưng khai thác | Mô hình đọc tối ưu cho tra cứu hoặc điều hành danh sách phòng ngưng khai thác; không được dùng để ghi thay Aggregate. | Maintenance | Không | Không | Không | Không | Có | Không | Không | Không |

### 1.7. Cashiering — Thu ngân

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Payment | Thanh toán | Aggregate quản lý thanh toán và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Cashiering | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Payment Attempt | Lần thử thanh toán | Thành phần có định danh nghiệp vụ dùng để ghi nhận lần thử thanh toán bên trong Aggregate sở hữu. | Cashiering | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Payment Method | Phương thức thanh toán | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho phương thức thanh toán. | Cashiering | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Payment Reference | Tham chiếu thanh toán | Giá trị bất biến biểu diễn tham chiếu thanh toán, được so sánh theo nội dung và không có lifecycle độc lập. | Cashiering | Không | Không | Có | Không | Không | Không | Không | Không |
| Payment Status | Trạng thái thanh toán | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái thanh toán; thay đổi phải theo governance domain. | Cashiering | Không | Không | Không | Không | Không | Không | Có | Không |
| Payment Authorization | Ủy quyền thanh toán | Khái niệm nghiệp vụ dùng để mô tả ủy quyền thanh toán; không tự động là đối tượng persistence. | Cashiering | Không | Không | Không | Không | Không | Không | Không | Không |
| Payment Capture | Ghi nhận thu tiền | Khái niệm nghiệp vụ dùng để mô tả ghi nhận thu tiền; không tự động là đối tượng persistence. | Cashiering | Không | Không | Không | Không | Không | Không | Không | Không |
| Payment Settlement | Đối soát thanh toán | Khái niệm nghiệp vụ dùng để mô tả đối soát thanh toán; không tự động là đối tượng persistence. | Cashiering | Không | Không | Không | Không | Không | Không | Không | Không |
| Payment Reconciliation | Đối chiếu thanh toán | Khái niệm nghiệp vụ dùng để mô tả đối chiếu thanh toán; không tự động là đối tượng persistence. | Cashiering | Không | Không | Không | Không | Không | Không | Không | Không |
| Deposit | Đặt cọc | Aggregate quản lý đặt cọc và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Cashiering | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Deposit Allocation | Phân bổ đặt cọc | Thành phần có định danh nghiệp vụ dùng để ghi nhận phân bổ đặt cọc bên trong Aggregate sở hữu. | Cashiering | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Deposit Status | Trạng thái đặt cọc | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái đặt cọc; thay đổi phải theo governance domain. | Cashiering | Không | Không | Không | Không | Không | Không | Có | Không |
| Refund Request | Yêu cầu hoàn tiền | Aggregate quản lý yêu cầu hoàn tiền và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Cashiering | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Refund Approval Step | Bước phê duyệt hoàn tiền | Thành phần có định danh nghiệp vụ dùng để ghi nhận bước phê duyệt hoàn tiền bên trong Aggregate sở hữu. | Cashiering | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Refund Execution Attempt | Lần thực hiện hoàn tiền | Thành phần có định danh nghiệp vụ dùng để ghi nhận lần thực hiện hoàn tiền bên trong Aggregate sở hữu. | Cashiering | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Refund Status | Trạng thái hoàn tiền | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái hoàn tiền; thay đổi phải theo governance domain. | Cashiering | Không | Không | Không | Không | Không | Không | Có | Không |
| Cashier Shift | Ca thu ngân | Aggregate quản lý ca thu ngân và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Cashiering | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Cashier Tender Summary | Tổng hợp phương thức thu ca | Thành phần có định danh nghiệp vụ dùng để ghi nhận tổng hợp phương thức thu ca bên trong Aggregate sở hữu. | Cashiering | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Cashier Variance | Chênh lệch ca thu ngân | Thành phần có định danh nghiệp vụ dùng để ghi nhận chênh lệch ca thu ngân bên trong Aggregate sở hữu. | Cashiering | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Opening Float | Tiền quỹ đầu ca | Giá trị bất biến biểu diễn tiền quỹ đầu ca, được so sánh theo nội dung và không có lifecycle độc lập. | Cashiering | Không | Không | Có | Không | Không | Không | Không | Không |
| Tender Declaration | Khai báo tiền cuối ca | Thành phần có định danh nghiệp vụ dùng để ghi nhận khai báo tiền cuối ca bên trong Aggregate sở hữu. | Cashiering | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Payment Captured | Thanh toán đã được ghi nhận | Sự kiện ở thì quá khứ xác nhận thanh toán đã được ghi nhận đã thực sự xảy ra trong module sở hữu. | Cashiering | Không | Không | Không | Có | Không | Không | Không | Không |
| Payment Uncertain | Kết quả thanh toán chưa xác định | Sự kiện ở thì quá khứ xác nhận kết quả thanh toán chưa xác định đã thực sự xảy ra trong module sở hữu. | Cashiering | Không | Không | Không | Có | Không | Không | Không | Không |
| Deposit Received | Đặt cọc đã được nhận | Sự kiện ở thì quá khứ xác nhận đặt cọc đã được nhận đã thực sự xảy ra trong module sở hữu. | Cashiering | Không | Không | Không | Có | Không | Không | Không | Không |
| Deposit Allocated | Đặt cọc đã được phân bổ | Sự kiện ở thì quá khứ xác nhận đặt cọc đã được phân bổ đã thực sự xảy ra trong module sở hữu. | Cashiering | Không | Không | Không | Có | Không | Không | Không | Không |
| Refund Completed | Hoàn tiền đã hoàn tất | Sự kiện ở thì quá khứ xác nhận hoàn tiền đã hoàn tất đã thực sự xảy ra trong module sở hữu. | Cashiering | Không | Không | Không | Có | Không | Không | Không | Không |
| Cashier Shift Opened | Ca thu ngân đã mở | Sự kiện ở thì quá khứ xác nhận ca thu ngân đã mở đã thực sự xảy ra trong module sở hữu. | Cashiering | Không | Không | Không | Có | Không | Không | Không | Không |
| Cashier Shift Closed | Ca thu ngân đã đóng | Sự kiện ở thì quá khứ xác nhận ca thu ngân đã đóng đã thực sự xảy ra trong module sở hữu. | Cashiering | Không | Không | Không | Có | Không | Không | Không | Không |
| Cashier Reconciliation Board | Bảng đối soát thu ngân | Mô hình đọc tối ưu cho tra cứu hoặc điều hành bảng đối soát thu ngân; không được dùng để ghi thay Aggregate. | Cashiering | Không | Không | Không | Không | Có | Không | Không | Không |
| Tender Variance Report | Báo cáo chênh lệch phương thức thu | Mô hình đọc tối ưu cho tra cứu hoặc điều hành báo cáo chênh lệch phương thức thu; không được dùng để ghi thay Aggregate. | Cashiering | Không | Không | Không | Không | Có | Không | Không | Không |

### 1.8. Folio — Sổ giao dịch khách

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Folio | Sổ giao dịch khách | Aggregate quản lý sổ giao dịch khách và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Folio | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Folio Window | Ngăn sổ giao dịch | Aggregate quản lý ngăn sổ giao dịch và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Folio | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Folio Entry | Bút toán sổ giao dịch | Thành phần có định danh nghiệp vụ dùng để ghi nhận bút toán sổ giao dịch bên trong Aggregate sở hữu. | Folio | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Folio Entry Tax | Thuế của bút toán sổ giao dịch | Thành phần có định danh nghiệp vụ dùng để ghi nhận thuế của bút toán sổ giao dịch bên trong Aggregate sở hữu. | Folio | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Folio Transfer Link | Liên kết chuyển bút toán | Thành phần có định danh nghiệp vụ dùng để ghi nhận liên kết chuyển bút toán bên trong Aggregate sở hữu. | Folio | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Folio Adjustment Link | Liên kết điều chỉnh bút toán | Thành phần có định danh nghiệp vụ dùng để ghi nhận liên kết điều chỉnh bút toán bên trong Aggregate sở hữu. | Folio | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Routing Instruction | Chỉ dẫn định tuyến chi phí | Aggregate quản lý chỉ dẫn định tuyến chi phí và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Folio | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Routing Condition | Điều kiện định tuyến | Thành phần có định danh nghiệp vụ dùng để ghi nhận điều kiện định tuyến bên trong Aggregate sở hữu. | Folio | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Folio Status | Trạng thái sổ giao dịch | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái sổ giao dịch; thay đổi phải theo governance domain. | Folio | Không | Không | Không | Không | Không | Không | Có | Không |
| Folio Balance | Số dư sổ giao dịch | Giá trị bất biến biểu diễn số dư sổ giao dịch, được so sánh theo nội dung và không có lifecycle độc lập. | Folio | Không | Không | Có | Không | Không | Không | Không | Không |
| Posting Reference | Tham chiếu ghi sổ | Giá trị bất biến biểu diễn tham chiếu ghi sổ, được so sánh theo nội dung và không có lifecycle độc lập. | Folio | Không | Không | Có | Không | Không | Không | Không | Không |
| Charge | Khoản tính phí | Khái niệm nghiệp vụ dùng để mô tả khoản tính phí; không tự động là đối tượng persistence. | Folio | Không | Không | Không | Không | Không | Không | Không | Không |
| Credit | Khoản ghi giảm | Khái niệm nghiệp vụ dùng để mô tả khoản ghi giảm; không tự động là đối tượng persistence. | Folio | Không | Không | Không | Không | Không | Không | Không | Không |
| Posting | Ghi sổ giao dịch | Khái niệm nghiệp vụ dùng để mô tả ghi sổ giao dịch; không tự động là đối tượng persistence. | Folio | Không | Không | Không | Không | Không | Không | Không | Không |
| Void | Vô hiệu giao dịch | Khái niệm nghiệp vụ dùng để mô tả vô hiệu giao dịch; không tự động là đối tượng persistence. | Folio | Không | Không | Không | Không | Không | Không | Không | Không |
| Reversal | Bút toán đảo | Khái niệm nghiệp vụ dùng để mô tả bút toán đảo; không tự động là đối tượng persistence. | Folio | Không | Không | Không | Không | Không | Không | Không | Không |
| Adjustment | Điều chỉnh | Khái niệm nghiệp vụ dùng để mô tả điều chỉnh; không tự động là đối tượng persistence. | Folio | Không | Không | Không | Không | Không | Không | Không | Không |
| Allowance | Khoản miễn giảm | Khái niệm nghiệp vụ dùng để mô tả khoản miễn giảm; không tự động là đối tượng persistence. | Folio | Không | Không | Không | Không | Không | Không | Không | Không |
| Charge Transfer | Chuyển khoản tính phí | Khái niệm nghiệp vụ dùng để mô tả chuyển khoản tính phí; không tự động là đối tượng persistence. | Folio | Không | Không | Không | Không | Không | Không | Không | Không |
| Routing Rule | Quy tắc định tuyến | Khái niệm nghiệp vụ dùng để mô tả quy tắc định tuyến; không tự động là đối tượng persistence. | Folio | Không | Không | Không | Không | Không | Không | Không | Không |
| Master Folio | Sổ giao dịch tổng | Khái niệm nghiệp vụ dùng để mô tả sổ giao dịch tổng; không tự động là đối tượng persistence. | Folio | Không | Không | Không | Không | Không | Không | Không | Không |
| Guest Folio | Sổ giao dịch khách | Khái niệm nghiệp vụ dùng để mô tả sổ giao dịch khách; không tự động là đối tượng persistence. | Folio | Không | Không | Không | Không | Không | Không | Không | Không |
| Company Folio | Sổ giao dịch công ty | Khái niệm nghiệp vụ dùng để mô tả sổ giao dịch công ty; không tự động là đối tượng persistence. | Folio | Không | Không | Không | Không | Không | Không | Không | Không |
| Guest Ledger | Sổ công nợ khách lưu trú | Khái niệm nghiệp vụ dùng để mô tả sổ công nợ khách lưu trú; không tự động là đối tượng persistence. | Folio | Không | Không | Không | Không | Không | Không | Không | Không |
| City Ledger | Sổ công nợ phải thu | Khái niệm nghiệp vụ dùng để mô tả sổ công nợ phải thu; không tự động là đối tượng persistence. | Folio | Không | Không | Không | Không | Không | Không | Không | Không |
| Folio Opened | Sổ giao dịch đã mở | Sự kiện ở thì quá khứ xác nhận sổ giao dịch đã mở đã thực sự xảy ra trong module sở hữu. | Folio | Không | Không | Không | Có | Không | Không | Không | Không |
| Folio Charge Posted | Khoản tính phí đã ghi sổ | Sự kiện ở thì quá khứ xác nhận khoản tính phí đã ghi sổ đã thực sự xảy ra trong module sở hữu. | Folio | Không | Không | Không | Có | Không | Không | Không | Không |
| Folio Closed | Sổ giao dịch đã đóng | Sự kiện ở thì quá khứ xác nhận sổ giao dịch đã đóng đã thực sự xảy ra trong module sở hữu. | Folio | Không | Không | Không | Có | Không | Không | Không | Không |
| Folio Statement | Bảng kê sổ giao dịch | Mô hình đọc tối ưu cho tra cứu hoặc điều hành bảng kê sổ giao dịch; không được dùng để ghi thay Aggregate. | Folio | Không | Không | Không | Không | Có | Không | Không | Không |
| Folio Balance Board | Bảng theo dõi số dư sổ giao dịch | Mô hình đọc tối ưu cho tra cứu hoặc điều hành bảng theo dõi số dư sổ giao dịch; không được dùng để ghi thay Aggregate. | Folio | Không | Không | Không | Không | Có | Không | Không | Không |

### 1.9. Night Audit — Kiểm toán đêm

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Property Business Date | Ngày nghiệp vụ cơ sở | Aggregate quản lý ngày nghiệp vụ cơ sở và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Night Audit | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Night Audit Run | Phiên kiểm toán đêm | Aggregate quản lý phiên kiểm toán đêm và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Night Audit | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Audit Checkpoint | Điểm kiểm soát kiểm toán | Thành phần có định danh nghiệp vụ dùng để ghi nhận điểm kiểm soát kiểm toán bên trong Aggregate sở hữu. | Night Audit | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Audit Signoff | Xác nhận kiểm toán | Thành phần có định danh nghiệp vụ dùng để ghi nhận xác nhận kiểm toán bên trong Aggregate sở hữu. | Night Audit | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Audit Checklist | Danh mục kiểm toán | Aggregate quản lý danh mục kiểm toán và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Night Audit | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Checklist Item | Mục kiểm toán | Thành phần có định danh nghiệp vụ dùng để ghi nhận mục kiểm toán bên trong Aggregate sở hữu. | Night Audit | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Audit Exception | Ngoại lệ kiểm toán | Aggregate quản lý ngoại lệ kiểm toán và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Night Audit | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Exception Evidence | Bằng chứng ngoại lệ | Thành phần có định danh nghiệp vụ dùng để ghi nhận bằng chứng ngoại lệ bên trong Aggregate sở hữu. | Night Audit | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Business Date | Ngày nghiệp vụ | Giá trị bất biến biểu diễn ngày nghiệp vụ, được so sánh theo nội dung và không có lifecycle độc lập. | Night Audit | Không | Không | Có | Không | Không | Không | Không | Không |
| Audit Severity | Mức độ ngoại lệ kiểm toán | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho mức độ ngoại lệ kiểm toán. | Night Audit | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Audit Status | Trạng thái kiểm toán | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái kiểm toán; thay đổi phải theo governance domain. | Night Audit | Không | Không | Không | Không | Không | Không | Có | Không |
| Trial Audit | Chạy thử kiểm toán | Khái niệm nghiệp vụ dùng để mô tả chạy thử kiểm toán; không tự động là đối tượng persistence. | Night Audit | Không | Không | Không | Không | Không | Không | Không | Không |
| Night Posting | Ghi sổ cuối ngày | Khái niệm nghiệp vụ dùng để mô tả ghi sổ cuối ngày; không tự động là đối tượng persistence. | Night Audit | Không | Không | Không | Không | Không | Không | Không | Không |
| Rollover | Chuyển ngày nghiệp vụ | Khái niệm nghiệp vụ dùng để mô tả chuyển ngày nghiệp vụ; không tự động là đối tượng persistence. | Night Audit | Không | Không | Không | Không | Không | Không | Không | Không |
| Late Posting | Ghi sổ muộn | Khái niệm nghiệp vụ dùng để mô tả ghi sổ muộn; không tự động là đối tượng persistence. | Night Audit | Không | Không | Không | Không | Không | Không | Không | Không |
| Cross-Date Adjustment | Điều chỉnh khác ngày nghiệp vụ | Khái niệm nghiệp vụ dùng để mô tả điều chỉnh khác ngày nghiệp vụ; không tự động là đối tượng persistence. | Night Audit | Không | Không | Không | Không | Không | Không | Không | Không |
| Business Date Reopen | Mở lại ngày nghiệp vụ | Khái niệm nghiệp vụ dùng để mô tả mở lại ngày nghiệp vụ; không tự động là đối tượng persistence. | Night Audit | Không | Không | Không | Không | Không | Không | Không | Không |
| Night Audit Started | Kiểm toán đêm đã bắt đầu | Sự kiện ở thì quá khứ xác nhận kiểm toán đêm đã bắt đầu đã thực sự xảy ra trong module sở hữu. | Night Audit | Không | Không | Không | Có | Không | Không | Không | Không |
| Audit Exception Raised | Ngoại lệ kiểm toán đã phát sinh | Sự kiện ở thì quá khứ xác nhận ngoại lệ kiểm toán đã phát sinh đã thực sự xảy ra trong module sở hữu. | Night Audit | Không | Không | Không | Có | Không | Không | Không | Không |
| Night Audit Ready | Kiểm toán đêm đã sẵn sàng chuyển ngày | Sự kiện ở thì quá khứ xác nhận kiểm toán đêm đã sẵn sàng chuyển ngày đã thực sự xảy ra trong module sở hữu. | Night Audit | Không | Không | Không | Có | Không | Không | Không | Không |
| Business Date Advanced | Ngày nghiệp vụ đã được chuyển | Sự kiện ở thì quá khứ xác nhận ngày nghiệp vụ đã được chuyển đã thực sự xảy ra trong module sở hữu. | Night Audit | Không | Không | Không | Có | Không | Không | Không | Không |
| Night Audit Dashboard | Bảng điều hành kiểm toán đêm | Mô hình đọc tối ưu cho tra cứu hoặc điều hành bảng điều hành kiểm toán đêm; không được dùng để ghi thay Aggregate. | Night Audit | Không | Không | Không | Không | Có | Không | Không | Không |
| Audit Exception Board | Bảng ngoại lệ kiểm toán | Mô hình đọc tối ưu cho tra cứu hoặc điều hành bảng ngoại lệ kiểm toán; không được dùng để ghi thay Aggregate. | Night Audit | Không | Không | Không | Không | Có | Không | Không | Không |
| Daily Audit Pack | Bộ báo cáo kiểm toán ngày | Mô hình đọc tối ưu cho tra cứu hoặc điều hành bộ báo cáo kiểm toán ngày; không được dùng để ghi thay Aggregate. | Night Audit | Không | Không | Không | Không | Có | Không | Không | Không |
| Rollover History | Lịch sử chuyển ngày nghiệp vụ | Mô hình đọc tối ưu cho tra cứu hoặc điều hành lịch sử chuyển ngày nghiệp vụ; không được dùng để ghi thay Aggregate. | Night Audit | Không | Không | Không | Không | Có | Không | Không | Không |

### 1.10. Guest Services — Dịch vụ khách

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Service Request | Yêu cầu dịch vụ | Aggregate quản lý yêu cầu dịch vụ và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Guest Services | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Service Request Item | Hạng mục yêu cầu dịch vụ | Thành phần có định danh nghiệp vụ dùng để ghi nhận hạng mục yêu cầu dịch vụ bên trong Aggregate sở hữu. | Guest Services | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Service Completion Evidence | Bằng chứng hoàn thành dịch vụ | Thành phần có định danh nghiệp vụ dùng để ghi nhận bằng chứng hoàn thành dịch vụ bên trong Aggregate sở hữu. | Guest Services | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Service Request Status | Trạng thái yêu cầu dịch vụ | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái yêu cầu dịch vụ; thay đổi phải theo governance domain. | Guest Services | Không | Không | Không | Không | Không | Không | Có | Không |
| Service Type | Loại dịch vụ khách | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho loại dịch vụ khách. | Guest Services | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Wake-Up Call | Cuộc gọi báo thức | Aggregate quản lý cuộc gọi báo thức và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Guest Services | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Wake-Up Attempt | Lần gọi báo thức | Thành phần có định danh nghiệp vụ dùng để ghi nhận lần gọi báo thức bên trong Aggregate sở hữu. | Guest Services | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Wake-Up Status | Trạng thái gọi báo thức | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái gọi báo thức; thay đổi phải theo governance domain. | Guest Services | Không | Không | Không | Không | Không | Không | Có | Không |
| Lost Found Case | Hồ sơ đồ thất lạc | Aggregate quản lý hồ sơ đồ thất lạc và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Guest Services | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Lost Found Item | Vật phẩm thất lạc | Thành phần có định danh nghiệp vụ dùng để ghi nhận vật phẩm thất lạc bên trong Aggregate sở hữu. | Guest Services | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Custody Handover | Lần bàn giao lưu giữ | Thành phần có định danh nghiệp vụ dùng để ghi nhận lần bàn giao lưu giữ bên trong Aggregate sở hữu. | Guest Services | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Claim Verification | Xác minh người nhận đồ | Thành phần có định danh nghiệp vụ dùng để ghi nhận xác minh người nhận đồ bên trong Aggregate sở hữu. | Guest Services | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Lost Found Status | Trạng thái đồ thất lạc | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái đồ thất lạc; thay đổi phải theo governance domain. | Guest Services | Không | Không | Không | Không | Không | Không | Có | Không |
| Guest Complaint | Phản ánh của khách | Loại yêu cầu dịch vụ ghi nhận phàn nàn và service recovery trong ServiceRequest; không tạo Aggregate mới ngoài Domain Model. | Guest Services | Không | Không | Không | Không | Không | Không | Không | Không |
| Service Recovery | Khắc phục dịch vụ | Khái niệm nghiệp vụ dùng để mô tả khắc phục dịch vụ; không tự động là đối tượng persistence. | Guest Services | Không | Không | Không | Không | Không | Không | Không | Không |
| Concierge Request | Yêu cầu hỗ trợ khách | Khái niệm nghiệp vụ dùng để mô tả yêu cầu hỗ trợ khách; không tự động là đối tượng persistence. | Guest Services | Không | Không | Không | Không | Không | Không | Không | Không |
| Transportation Request | Yêu cầu vận chuyển | Khái niệm nghiệp vụ dùng để mô tả yêu cầu vận chuyển; không tự động là đối tượng persistence. | Guest Services | Không | Không | Không | Không | Không | Không | Không | Không |
| Service Request Raised | Yêu cầu dịch vụ đã được ghi nhận | Sự kiện ở thì quá khứ xác nhận yêu cầu dịch vụ đã được ghi nhận đã thực sự xảy ra trong module sở hữu. | Guest Services | Không | Không | Không | Có | Không | Không | Không | Không |
| Wake-Up Call Scheduled | Cuộc gọi báo thức đã được lên lịch | Sự kiện ở thì quá khứ xác nhận cuộc gọi báo thức đã được lên lịch đã thực sự xảy ra trong module sở hữu. | Guest Services | Không | Không | Không | Có | Không | Không | Không | Không |
| Lost Found Item Registered | Vật phẩm thất lạc đã được đăng ký | Sự kiện ở thì quá khứ xác nhận vật phẩm thất lạc đã được đăng ký đã thực sự xảy ra trong module sở hữu. | Guest Services | Không | Không | Không | Có | Không | Không | Không | Không |

### 1.11. CRM — Quản trị quan hệ khách hàng

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Guest Profile | Hồ sơ khách hàng | Aggregate quản lý hồ sơ khách hàng và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | CRM | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Guest Identity | Danh tính khách hàng | Thành phần có định danh nghiệp vụ dùng để ghi nhận danh tính khách hàng bên trong Aggregate sở hữu. | CRM | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Guest Contact | Thông tin liên hệ khách hàng | Thành phần có định danh nghiệp vụ dùng để ghi nhận thông tin liên hệ khách hàng bên trong Aggregate sở hữu. | CRM | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Guest Address | Địa chỉ khách hàng | Thành phần có định danh nghiệp vụ dùng để ghi nhận địa chỉ khách hàng bên trong Aggregate sở hữu. | CRM | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Guest Preference | Sở thích khách hàng | Thành phần có định danh nghiệp vụ dùng để ghi nhận sở thích khách hàng bên trong Aggregate sở hữu. | CRM | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Guest Consent | Chấp thuận của khách hàng | Thành phần có định danh nghiệp vụ dùng để ghi nhận chấp thuận của khách hàng bên trong Aggregate sở hữu. | CRM | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Guest Segment | Phân khúc khách hàng | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho phân khúc khách hàng. | CRM | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Guest Tier | Hạng thành viên | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho hạng thành viên. | CRM | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Loyalty Account | Tài khoản khách hàng thân thiết | Aggregate quản lý tài khoản khách hàng thân thiết và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | CRM | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Loyalty Transaction | Giao dịch điểm thưởng | Thành phần có định danh nghiệp vụ dùng để ghi nhận giao dịch điểm thưởng bên trong Aggregate sở hữu. | CRM | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Loyalty Balance | Số dư điểm thưởng | Giá trị bất biến biểu diễn số dư điểm thưởng, được so sánh theo nội dung và không có lifecycle độc lập. | CRM | Không | Không | Có | Không | Không | Không | Không | Không |
| Company Profile | Hồ sơ công ty | Aggregate quản lý hồ sơ công ty và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | CRM | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Travel Agent Profile | Hồ sơ đại lý du lịch | Aggregate quản lý hồ sơ đại lý du lịch và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | CRM | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Contact Person | Người liên hệ | Thành phần có định danh nghiệp vụ dùng để ghi nhận người liên hệ bên trong Aggregate sở hữu. | CRM | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Guest Name | Tên khách hàng | Giá trị bất biến biểu diễn tên khách hàng, được so sánh theo nội dung và không có lifecycle độc lập. | CRM | Không | Không | Có | Không | Không | Không | Không | Không |
| Contact Information | Thông tin liên hệ | Giá trị bất biến biểu diễn thông tin liên hệ, được so sánh theo nội dung và không có lifecycle độc lập. | CRM | Không | Không | Có | Không | Không | Không | Không | Không |
| Identity Document | Giấy tờ định danh | Giá trị bất biến biểu diễn giấy tờ định danh, được so sánh theo nội dung và không có lifecycle độc lập. | CRM | Không | Không | Có | Không | Không | Không | Không | Không |
| Guest Profile Merged | Hồ sơ khách hàng đã được hợp nhất | Sự kiện ở thì quá khứ xác nhận hồ sơ khách hàng đã được hợp nhất đã thực sự xảy ra trong module sở hữu. | CRM | Không | Không | Không | Có | Không | Không | Không | Không |
| Guest Preference Observed | Sở thích khách đã được ghi nhận | Sự kiện ở thì quá khứ xác nhận sở thích khách đã được ghi nhận đã thực sự xảy ra trong module sở hữu. | CRM | Không | Không | Không | Có | Không | Không | Không | Không |
| Guest Consent Changed | Chấp thuận khách hàng đã thay đổi | Sự kiện ở thì quá khứ xác nhận chấp thuận khách hàng đã thay đổi đã thực sự xảy ra trong module sở hữu. | CRM | Không | Không | Không | Có | Không | Không | Không | Không |
| Guest 360 View | Góc nhìn khách hàng toàn diện | Mô hình đọc tối ưu cho tra cứu hoặc điều hành góc nhìn khách hàng toàn diện; không được dùng để ghi thay Aggregate. | CRM | Không | Không | Không | Không | Có | Không | Không | Không |
| Guest Stay Summary | Tóm tắt lưu trú của khách | Mô hình đọc tối ưu cho tra cứu hoặc điều hành tóm tắt lưu trú của khách; không được dùng để ghi thay Aggregate. | CRM | Không | Không | Không | Không | Có | Không | Không | Không |
| Guest Value Segment | Phân nhóm giá trị khách hàng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành phân nhóm giá trị khách hàng; không được dùng để ghi thay Aggregate. | CRM | Không | Không | Không | Không | Có | Không | Không | Không |
| Duplicate Guest Candidate | Ứng viên hồ sơ khách trùng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành ứng viên hồ sơ khách trùng; không được dùng để ghi thay Aggregate. | CRM | Không | Không | Không | Không | Có | Không | Không | Không |
| Customer Lifetime Value | Giá trị vòng đời khách hàng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành giá trị vòng đời khách hàng; không được dùng để ghi thay Aggregate. | CRM | Không | Không | Không | Không | Có | Không | Không | Không |

### 1.12. POS — Bán hàng tại điểm

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Point Of Sale | Điểm bán hàng | Khái niệm nghiệp vụ dùng để mô tả điểm bán hàng; không tự động là đối tượng persistence. | POS | Không | Không | Không | Không | Không | Không | Không | Không |
| Outlet | Điểm kinh doanh | Aggregate quản lý điểm kinh doanh và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | POS | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Sales Order | Đơn bán hàng | Aggregate quản lý đơn bán hàng và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | POS | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Sales Order Line | Dòng đơn bán hàng | Thành phần có định danh nghiệp vụ dùng để ghi nhận dòng đơn bán hàng bên trong Aggregate sở hữu. | POS | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Menu Item | Món bán | Aggregate quản lý món bán và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | POS | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Menu Category | Nhóm món | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho nhóm món. | POS | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Modifier | Tùy chọn món | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho tùy chọn món. | POS | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Price List | Bảng giá bán | Aggregate quản lý bảng giá bán và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | POS | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Table | Bàn phục vụ | Aggregate quản lý bàn phục vụ và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | POS | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Table Session | Phiên phục vụ bàn | Aggregate quản lý phiên phục vụ bàn và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | POS | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Kitchen Order | Phiếu chế biến | Aggregate quản lý phiếu chế biến và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | POS | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Kitchen Order Line | Dòng phiếu chế biến | Thành phần có định danh nghiệp vụ dùng để ghi nhận dòng phiếu chế biến bên trong Aggregate sở hữu. | POS | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Order Status | Trạng thái đơn bán | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái đơn bán; thay đổi phải theo governance domain. | POS | Không | Không | Không | Không | Không | Không | Có | Không |
| Payment Status POS | Trạng thái thanh toán điểm bán | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái thanh toán điểm bán; thay đổi phải theo governance domain. | POS | Không | Không | Không | Không | Không | Không | Có | Không |
| Room Charge Request | Yêu cầu ký phòng | Aggregate quản lý yêu cầu ký phòng và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | POS | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Room Charge Eligibility | Điều kiện được ký phòng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành điều kiện được ký phòng; không được dùng để ghi thay Aggregate. | POS | Không | Không | Không | Không | Có | Không | Không | Không |
| Check Number | Số hóa đơn điểm bán | Giá trị bất biến biểu diễn số hóa đơn điểm bán, được so sánh theo nội dung và không có lifecycle độc lập. | POS | Không | Không | Có | Không | Không | Không | Không | Không |
| Order Total | Tổng tiền đơn bán | Giá trị bất biến biểu diễn tổng tiền đơn bán, được so sánh theo nội dung và không có lifecycle độc lập. | POS | Không | Không | Có | Không | Không | Không | Không | Không |
| Sales Order Created | Đơn bán hàng đã được tạo | Sự kiện ở thì quá khứ xác nhận đơn bán hàng đã được tạo đã thực sự xảy ra trong module sở hữu. | POS | Không | Không | Không | Có | Không | Không | Không | Không |
| Room Charge Requested | Yêu cầu ký phòng đã được gửi | Sự kiện ở thì quá khứ xác nhận yêu cầu ký phòng đã được gửi đã thực sự xảy ra trong module sở hữu. | POS | Không | Không | Không | Có | Không | Không | Không | Không |
| Room Charge Accepted | Yêu cầu ký phòng đã được chấp nhận | Sự kiện ở thì quá khứ xác nhận yêu cầu ký phòng đã được chấp nhận đã thực sự xảy ra trong module sở hữu. | POS | Không | Không | Không | Có | Không | Không | Không | Không |
| Room Charge Rejected | Yêu cầu ký phòng đã bị từ chối | Sự kiện ở thì quá khứ xác nhận yêu cầu ký phòng đã bị từ chối đã thực sự xảy ra trong module sở hữu. | POS | Không | Không | Không | Có | Không | Không | Không | Không |
| Outlet Sales Summary | Tổng hợp doanh thu điểm bán | Mô hình đọc tối ưu cho tra cứu hoặc điều hành tổng hợp doanh thu điểm bán; không được dùng để ghi thay Aggregate. | POS | Không | Không | Không | Không | Có | Không | Không | Không |
| Open Check List | Danh sách hóa đơn đang mở | Mô hình đọc tối ưu cho tra cứu hoặc điều hành danh sách hóa đơn đang mở; không được dùng để ghi thay Aggregate. | POS | Không | Không | Không | Không | Có | Không | Không | Không |
| Room Charge Exception Board | Bảng ngoại lệ ký phòng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành bảng ngoại lệ ký phòng; không được dùng để ghi thay Aggregate. | POS | Không | Không | Không | Không | Có | Không | Không | Không |

### 1.13. Inventory — Kho

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Inventory Item | Hàng hóa kho | Aggregate quản lý hàng hóa kho và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Inventory | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Inventory Category | Nhóm hàng hóa | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho nhóm hàng hóa. | Inventory | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Stock Location | Kho lưu trữ | Aggregate quản lý kho lưu trữ và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Inventory | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Stock Balance | Tồn kho | Aggregate quản lý tồn kho và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Inventory | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Stock Lot | Lô hàng | Thành phần có định danh nghiệp vụ dùng để ghi nhận lô hàng bên trong Aggregate sở hữu. | Inventory | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Stock Transaction | Giao dịch kho | Aggregate quản lý giao dịch kho và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Inventory | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Stock Transaction Line | Dòng giao dịch kho | Thành phần có định danh nghiệp vụ dùng để ghi nhận dòng giao dịch kho bên trong Aggregate sở hữu. | Inventory | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Stock Issue | Phiếu xuất kho | Aggregate quản lý phiếu xuất kho và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Inventory | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Stock Receipt | Phiếu nhập kho | Aggregate quản lý phiếu nhập kho và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Inventory | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Stock Transfer | Phiếu chuyển kho | Aggregate quản lý phiếu chuyển kho và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Inventory | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Stock Count | Phiếu kiểm kê | Aggregate quản lý phiếu kiểm kê và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Inventory | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Stock Count Line | Dòng kiểm kê | Thành phần có định danh nghiệp vụ dùng để ghi nhận dòng kiểm kê bên trong Aggregate sở hữu. | Inventory | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Unit Of Measure | Đơn vị tính | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho đơn vị tính. | Inventory | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Reorder Level | Mức tồn đặt hàng lại | Giá trị bất biến biểu diễn mức tồn đặt hàng lại, được so sánh theo nội dung và không có lifecycle độc lập. | Inventory | Không | Không | Có | Không | Không | Không | Không | Không |
| Quantity | Số lượng | Giá trị bất biến biểu diễn số lượng, được so sánh theo nội dung và không có lifecycle độc lập. | Inventory | Không | Không | Có | Không | Không | Không | Không | Không |
| Stock Status | Trạng thái tồn kho | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái tồn kho; thay đổi phải theo governance domain. | Inventory | Không | Không | Không | Không | Không | Không | Có | Không |
| Minibar Item | Hàng minibar | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho hàng minibar. | Inventory | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Amenity Item | Vật phẩm tiện nghi | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho vật phẩm tiện nghi. | Inventory | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Laundry Item | Hàng giặt là | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho hàng giặt là. | Inventory | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Stock Issued | Hàng đã được xuất kho | Sự kiện ở thì quá khứ xác nhận hàng đã được xuất kho đã thực sự xảy ra trong module sở hữu. | Inventory | Không | Không | Không | Có | Không | Không | Không | Không |
| Stock Received | Hàng đã được nhập kho | Sự kiện ở thì quá khứ xác nhận hàng đã được nhập kho đã thực sự xảy ra trong module sở hữu. | Inventory | Không | Không | Không | Có | Không | Không | Không | Không |
| Stock Adjusted | Tồn kho đã được điều chỉnh | Sự kiện ở thì quá khứ xác nhận tồn kho đã được điều chỉnh đã thực sự xảy ra trong module sở hữu. | Inventory | Không | Không | Không | Có | Không | Không | Không | Không |
| Inventory Balance View | Góc nhìn tồn kho | Mô hình đọc tối ưu cho tra cứu hoặc điều hành góc nhìn tồn kho; không được dùng để ghi thay Aggregate. | Inventory | Không | Không | Không | Không | Có | Không | Không | Không |
| Low Stock List | Danh sách hàng sắp hết | Mô hình đọc tối ưu cho tra cứu hoặc điều hành danh sách hàng sắp hết; không được dùng để ghi thay Aggregate. | Inventory | Không | Không | Không | Không | Có | Không | Không | Không |
| Consumption By Room Report | Báo cáo tiêu hao theo phòng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành báo cáo tiêu hao theo phòng; không được dùng để ghi thay Aggregate. | Inventory | Không | Không | Không | Không | Có | Không | Không | Không |

### 1.14. Accounting — Kế toán

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| General Ledger | Sổ cái | Khái niệm nghiệp vụ dùng để mô tả sổ cái; không tự động là đối tượng persistence. | Accounting | Không | Không | Không | Không | Không | Không | Không | Không |
| Journal | Chứng từ kế toán | Aggregate quản lý chứng từ kế toán và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Accounting | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Journal Line | Dòng chứng từ kế toán | Thành phần có định danh nghiệp vụ dùng để ghi nhận dòng chứng từ kế toán bên trong Aggregate sở hữu. | Accounting | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Account | Tài khoản kế toán | Aggregate quản lý tài khoản kế toán và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Accounting | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Chart Of Accounts | Hệ thống tài khoản | Aggregate quản lý hệ thống tài khoản và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Accounting | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Accounting Period | Kỳ kế toán | Aggregate quản lý kỳ kế toán và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Accounting | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Invoice | Hóa đơn | Aggregate quản lý hóa đơn và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Accounting | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Invoice Line | Dòng hóa đơn | Thành phần có định danh nghiệp vụ dùng để ghi nhận dòng hóa đơn bên trong Aggregate sở hữu. | Accounting | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Electronic Invoice | Hóa đơn điện tử | Aggregate quản lý hóa đơn điện tử và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Accounting | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Accounts Receivable | Công nợ phải thu | Khái niệm nghiệp vụ dùng để mô tả công nợ phải thu; không tự động là đối tượng persistence. | Accounting | Không | Không | Không | Không | Không | Không | Không | Không |
| AR Account | Tài khoản công nợ | Aggregate quản lý tài khoản công nợ và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Accounting | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| AR Transaction | Giao dịch công nợ | Aggregate quản lý giao dịch công nợ và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Accounting | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Credit Limit | Hạn mức tín dụng | Giá trị bất biến biểu diễn hạn mức tín dụng, được so sánh theo nội dung và không có lifecycle độc lập. | Accounting | Không | Không | Có | Không | Không | Không | Không | Không |
| Direct Billing | Thanh toán công nợ | Khái niệm nghiệp vụ dùng để mô tả thanh toán công nợ; không tự động là đối tượng persistence. | Accounting | Không | Không | Không | Không | Không | Không | Không | Không |
| Tax Code | Mã thuế | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho mã thuế. | Accounting | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Tax Rate | Thuế suất | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho thuế suất. | Accounting | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Cost Center | Trung tâm chi phí | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho trung tâm chi phí. | Accounting | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Revenue Center | Trung tâm doanh thu | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho trung tâm doanh thu. | Accounting | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Currency | Tiền tệ | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho tiền tệ. | Accounting | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Exchange Rate | Tỷ giá | Giá trị bất biến biểu diễn tỷ giá, được so sánh theo nội dung và không có lifecycle độc lập. | Accounting | Không | Không | Có | Không | Không | Không | Không | Không |
| Invoice Issued | Hóa đơn đã được phát hành | Sự kiện ở thì quá khứ xác nhận hóa đơn đã được phát hành đã thực sự xảy ra trong module sở hữu. | Accounting | Không | Không | Không | Có | Không | Không | Không | Không |
| Invoice Rejected | Yêu cầu hóa đơn đã bị từ chối | Sự kiện ở thì quá khứ xác nhận yêu cầu hóa đơn đã bị từ chối đã thực sự xảy ra trong module sở hữu. | Accounting | Không | Không | Không | Có | Không | Không | Không | Không |
| Journal Posted | Chứng từ kế toán đã ghi sổ | Sự kiện ở thì quá khứ xác nhận chứng từ kế toán đã ghi sổ đã thực sự xảy ra trong module sở hữu. | Accounting | Không | Không | Không | Có | Không | Không | Không | Không |
| Credit Status Changed | Trạng thái tín dụng đã thay đổi | Sự kiện ở thì quá khứ xác nhận trạng thái tín dụng đã thay đổi đã thực sự xảy ra trong module sở hữu. | Accounting | Không | Không | Không | Có | Không | Không | Không | Không |
| Trial Balance | Bảng cân đối phát sinh | Mô hình đọc tối ưu cho tra cứu hoặc điều hành bảng cân đối phát sinh; không được dùng để ghi thay Aggregate. | Accounting | Không | Không | Không | Không | Có | Không | Không | Không |
| Revenue Report | Báo cáo doanh thu | Mô hình đọc tối ưu cho tra cứu hoặc điều hành báo cáo doanh thu; không được dùng để ghi thay Aggregate. | Accounting | Không | Không | Không | Không | Có | Không | Không | Không |
| AR Aging Report | Báo cáo tuổi nợ | Mô hình đọc tối ưu cho tra cứu hoặc điều hành báo cáo tuổi nợ; không được dùng để ghi thay Aggregate. | Accounting | Không | Không | Không | Không | Có | Không | Không | Không |
| Tax Summary | Tổng hợp thuế | Mô hình đọc tối ưu cho tra cứu hoặc điều hành tổng hợp thuế; không được dùng để ghi thay Aggregate. | Accounting | Không | Không | Không | Không | Có | Không | Không | Không |
| Daily Journal Summary | Tổng hợp chứng từ ngày | Mô hình đọc tối ưu cho tra cứu hoặc điều hành tổng hợp chứng từ ngày; không được dùng để ghi thay Aggregate. | Accounting | Không | Không | Không | Không | Có | Không | Không | Không |
| Financial Reconciliation Board | Bảng đối chiếu tài chính | Mô hình đọc tối ưu cho tra cứu hoặc điều hành bảng đối chiếu tài chính; không được dùng để ghi thay Aggregate. | Accounting | Không | Không | Không | Không | Có | Không | Không | Không |

### 1.15. Banquet — Tiệc

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Banquet Event | Sự kiện tiệc | Aggregate quản lý sự kiện tiệc và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Banquet | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Banquet Booking | Đặt tiệc | Aggregate quản lý đặt tiệc và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Banquet | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Event Function | Chương trình tiệc | Aggregate quản lý chương trình tiệc và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Banquet | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Function Space | Không gian tổ chức | Aggregate quản lý không gian tổ chức và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Banquet | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Event Schedule | Lịch trình sự kiện | Thành phần có định danh nghiệp vụ dùng để ghi nhận lịch trình sự kiện bên trong Aggregate sở hữu. | Banquet | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Banquet Menu | Thực đơn tiệc | Aggregate quản lý thực đơn tiệc và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Banquet | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Menu Package | Gói thực đơn | Aggregate quản lý gói thực đơn và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Banquet | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Banquet Service | Dịch vụ tiệc | Aggregate quản lý dịch vụ tiệc và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Banquet | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Event Guest Count | Số khách dự tiệc | Giá trị bất biến biểu diễn số khách dự tiệc, được so sánh theo nội dung và không có lifecycle độc lập. | Banquet | Không | Không | Có | Không | Không | Không | Không | Không |
| Guaranteed Guest Count | Số khách bảo đảm | Giá trị bất biến biểu diễn số khách bảo đảm, được so sánh theo nội dung và không có lifecycle độc lập. | Banquet | Không | Không | Có | Không | Không | Không | Không | Không |
| Event Status | Trạng thái sự kiện | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái sự kiện; thay đổi phải theo governance domain. | Banquet | Không | Không | Không | Không | Không | Không | Có | Không |
| Event Type | Loại sự kiện | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho loại sự kiện. | Banquet | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Seating Style | Kiểu bố trí bàn ghế | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho kiểu bố trí bàn ghế. | Banquet | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Pricing Rule | Quy tắc tính giá | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho quy tắc tính giá. | Banquet | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Per Guest Pricing | Tính giá theo khách | Khái niệm nghiệp vụ dùng để mô tả tính giá theo khách; không tự động là đối tượng persistence. | Banquet | Không | Không | Không | Không | Không | Không | Không | Không |
| Event Contract | Hợp đồng sự kiện | Aggregate quản lý hợp đồng sự kiện và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Banquet | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Event Deposit | Đặt cọc sự kiện | Khái niệm nghiệp vụ dùng để mô tả đặt cọc sự kiện; không tự động là đối tượng persistence. | Banquet | Không | Không | Không | Không | Không | Không | Không | Không |
| Event Checklist | Danh mục công việc sự kiện | Aggregate quản lý danh mục công việc sự kiện và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Banquet | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Event Timeline Item | Mốc thời gian sự kiện | Thành phần có định danh nghiệp vụ dùng để ghi nhận mốc thời gian sự kiện bên trong Aggregate sở hữu. | Banquet | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Banquet Event Confirmed | Sự kiện tiệc đã được xác nhận | Sự kiện ở thì quá khứ xác nhận sự kiện tiệc đã được xác nhận đã thực sự xảy ra trong module sở hữu. | Banquet | Không | Không | Không | Có | Không | Không | Không | Không |
| Banquet Charge Requested | Yêu cầu tính phí tiệc đã được gửi | Sự kiện ở thì quá khứ xác nhận yêu cầu tính phí tiệc đã được gửi đã thực sự xảy ra trong module sở hữu. | Banquet | Không | Không | Không | Có | Không | Không | Không | Không |
| Event Linked To Stay | Sự kiện đã liên kết lưu trú | Sự kiện ở thì quá khứ xác nhận sự kiện đã liên kết lưu trú đã thực sự xảy ra trong module sở hữu. | Banquet | Không | Không | Không | Có | Không | Không | Không | Không |
| Banquet Event Board | Bảng điều hành sự kiện tiệc | Mô hình đọc tối ưu cho tra cứu hoặc điều hành bảng điều hành sự kiện tiệc; không được dùng để ghi thay Aggregate. | Banquet | Không | Không | Không | Không | Có | Không | Không | Không |
| Function Space Calendar | Lịch không gian tổ chức | Mô hình đọc tối ưu cho tra cứu hoặc điều hành lịch không gian tổ chức; không được dùng để ghi thay Aggregate. | Banquet | Không | Không | Không | Không | Có | Không | Không | Không |
| Banquet Revenue Report | Báo cáo doanh thu tiệc | Mô hình đọc tối ưu cho tra cứu hoặc điều hành báo cáo doanh thu tiệc; không được dùng để ghi thay Aggregate. | Banquet | Không | Không | Không | Không | Có | Không | Không | Không |

### 1.16. Purchasing — Mua hàng

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Purchase Requisition | Đề nghị mua hàng | Aggregate quản lý đề nghị mua hàng và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Purchasing | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Requisition Line | Dòng đề nghị mua hàng | Thành phần có định danh nghiệp vụ dùng để ghi nhận dòng đề nghị mua hàng bên trong Aggregate sở hữu. | Purchasing | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Request For Quotation | Yêu cầu báo giá | Aggregate quản lý yêu cầu báo giá và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Purchasing | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Supplier Quotation | Báo giá nhà cung cấp | Aggregate quản lý báo giá nhà cung cấp và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Purchasing | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Quotation Line | Dòng báo giá nhà cung cấp | Thành phần có định danh nghiệp vụ dùng để ghi nhận dòng báo giá nhà cung cấp bên trong Aggregate sở hữu. | Purchasing | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Purchase Order | Đơn mua hàng | Aggregate quản lý đơn mua hàng và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Purchasing | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Purchase Order Line | Dòng đơn mua hàng | Thành phần có định danh nghiệp vụ dùng để ghi nhận dòng đơn mua hàng bên trong Aggregate sở hữu. | Purchasing | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Goods Receipt | Phiếu nhận hàng | Aggregate quản lý phiếu nhận hàng và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Purchasing | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Goods Receipt Line | Dòng nhận hàng | Thành phần có định danh nghiệp vụ dùng để ghi nhận dòng nhận hàng bên trong Aggregate sở hữu. | Purchasing | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Supplier | Nhà cung cấp | Aggregate quản lý nhà cung cấp và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Purchasing | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Supplier Category | Nhóm nhà cung cấp | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho nhóm nhà cung cấp. | Purchasing | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Procurement Status | Trạng thái mua hàng | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái mua hàng; thay đổi phải theo governance domain. | Purchasing | Không | Không | Không | Không | Không | Không | Có | Không |
| Approval Limit | Hạn mức phê duyệt | Giá trị bất biến biểu diễn hạn mức phê duyệt, được so sánh theo nội dung và không có lifecycle độc lập. | Purchasing | Không | Không | Có | Không | Không | Không | Không | Không |
| Lead Time | Thời gian cung ứng | Giá trị bất biến biểu diễn thời gian cung ứng, được so sánh theo nội dung và không có lifecycle độc lập. | Purchasing | Không | Không | Có | Không | Không | Không | Không | Không |
| Expected Delivery Date | Ngày giao hàng dự kiến | Giá trị bất biến biểu diễn ngày giao hàng dự kiến, được so sánh theo nội dung và không có lifecycle độc lập. | Purchasing | Không | Không | Có | Không | Không | Không | Không | Không |
| Purchase Order Approved | Đơn mua hàng đã được duyệt | Sự kiện ở thì quá khứ xác nhận đơn mua hàng đã được duyệt đã thực sự xảy ra trong module sở hữu. | Purchasing | Không | Không | Không | Có | Không | Không | Không | Không |
| Goods Received | Hàng mua đã được nhận | Sự kiện ở thì quá khứ xác nhận hàng mua đã được nhận đã thực sự xảy ra trong module sở hữu. | Purchasing | Không | Không | Không | Có | Không | Không | Không | Không |
| Open Purchase Order List | Danh sách đơn mua hàng đang mở | Mô hình đọc tối ưu cho tra cứu hoặc điều hành danh sách đơn mua hàng đang mở; không được dùng để ghi thay Aggregate. | Purchasing | Không | Không | Không | Không | Có | Không | Không | Không |
| Supplier Performance Report | Báo cáo hiệu quả nhà cung cấp | Mô hình đọc tối ưu cho tra cứu hoặc điều hành báo cáo hiệu quả nhà cung cấp; không được dùng để ghi thay Aggregate. | Purchasing | Không | Không | Không | Không | Có | Không | Không | Không |
| Purchase Spend Report | Báo cáo chi tiêu mua hàng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành báo cáo chi tiêu mua hàng; không được dùng để ghi thay Aggregate. | Purchasing | Không | Không | Không | Không | Có | Không | Không | Không |

### 1.17. Notification — Thông báo

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Notification Request | Yêu cầu thông báo | Aggregate quản lý yêu cầu thông báo và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Notification | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Notification Template | Mẫu thông báo | Aggregate quản lý mẫu thông báo và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Notification | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Notification Recipient | Người nhận thông báo | Thành phần có định danh nghiệp vụ dùng để ghi nhận người nhận thông báo bên trong Aggregate sở hữu. | Notification | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Delivery Attempt | Lần gửi thông báo | Thành phần có định danh nghiệp vụ dùng để ghi nhận lần gửi thông báo bên trong Aggregate sở hữu. | Notification | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Notification Channel | Kênh thông báo | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho kênh thông báo. | Notification | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Notification Status | Trạng thái thông báo | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái thông báo; thay đổi phải theo governance domain. | Notification | Không | Không | Không | Không | Không | Không | Có | Không |
| Delivery Status | Trạng thái gửi | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái gửi; thay đổi phải theo governance domain. | Notification | Không | Không | Không | Không | Không | Không | Có | Không |
| Notification Preference | Tùy chọn nhận thông báo | Thành phần có định danh nghiệp vụ dùng để ghi nhận tùy chọn nhận thông báo bên trong Aggregate sở hữu. | Notification | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Notification Schedule | Lịch gửi thông báo | Giá trị bất biến biểu diễn lịch gửi thông báo, được so sánh theo nội dung và không có lifecycle độc lập. | Notification | Không | Không | Có | Không | Không | Không | Không | Không |
| Notification Requested | Thông báo đã được yêu cầu | Sự kiện ở thì quá khứ xác nhận thông báo đã được yêu cầu đã thực sự xảy ra trong module sở hữu. | Notification | Không | Không | Không | Có | Không | Không | Không | Không |
| Notification Delivered | Thông báo đã gửi thành công | Sự kiện ở thì quá khứ xác nhận thông báo đã gửi thành công đã thực sự xảy ra trong module sở hữu. | Notification | Không | Không | Không | Có | Không | Không | Không | Không |
| Notification Failed | Thông báo gửi thất bại | Sự kiện ở thì quá khứ xác nhận thông báo gửi thất bại đã thực sự xảy ra trong module sở hữu. | Notification | Không | Không | Không | Có | Không | Không | Không | Không |
| Notification Inbox | Hộp thông báo | Mô hình đọc tối ưu cho tra cứu hoặc điều hành hộp thông báo; không được dùng để ghi thay Aggregate. | Notification | Không | Không | Không | Không | Có | Không | Không | Không |
| Delivery Exception Board | Bảng ngoại lệ gửi thông báo | Mô hình đọc tối ưu cho tra cứu hoặc điều hành bảng ngoại lệ gửi thông báo; không được dùng để ghi thay Aggregate. | Notification | Không | Không | Không | Không | Có | Không | Không | Không |
| Notification Delivery Report | Báo cáo gửi thông báo | Mô hình đọc tối ưu cho tra cứu hoặc điều hành báo cáo gửi thông báo; không được dùng để ghi thay Aggregate. | Notification | Không | Không | Không | Không | Có | Không | Không | Không |

### 1.18. AI Assistant — Trợ lý AI

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| AI Recommendation | Khuyến nghị AI | Aggregate quản lý khuyến nghị ai và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | AI Assistant | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| AI Recommendation Context | Bối cảnh khuyến nghị AI | Thành phần có định danh nghiệp vụ dùng để ghi nhận bối cảnh khuyến nghị ai bên trong Aggregate sở hữu. | AI Assistant | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| AI Decision Feedback | Phản hồi quyết định AI | Thành phần có định danh nghiệp vụ dùng để ghi nhận phản hồi quyết định ai bên trong Aggregate sở hữu. | AI Assistant | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| AI Use Case | Trường hợp sử dụng AI | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho trường hợp sử dụng ai. | AI Assistant | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Model Version | Phiên bản mô hình AI | Giá trị bất biến biểu diễn phiên bản mô hình ai, được so sánh theo nội dung và không có lifecycle độc lập. | AI Assistant | Không | Không | Có | Không | Không | Không | Không | Không |
| Confidence Score | Điểm tin cậy | Giá trị bất biến biểu diễn điểm tin cậy, được so sánh theo nội dung và không có lifecycle độc lập. | AI Assistant | Không | Không | Có | Không | Không | Không | Không | Không |
| Reason Code AI | Mã lý do khuyến nghị AI | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho mã lý do khuyến nghị ai. | AI Assistant | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Recommendation Expiry | Thời hạn khuyến nghị | Giá trị bất biến biểu diễn thời hạn khuyến nghị, được so sánh theo nội dung và không có lifecycle độc lập. | AI Assistant | Không | Không | Có | Không | Không | Không | Không | Không |
| Upsell Recommendation | Khuyến nghị bán thêm | Khái niệm nghiệp vụ dùng để mô tả khuyến nghị bán thêm; không tự động là đối tượng persistence. | AI Assistant | Không | Không | Không | Không | Không | Không | Không | Không |
| Upgrade Recommendation | Khuyến nghị nâng hạng | Khái niệm nghiệp vụ dùng để mô tả khuyến nghị nâng hạng; không tự động là đối tượng persistence. | AI Assistant | Không | Không | Không | Không | Không | Không | Không | Không |
| Checkout Risk Score | Điểm rủi ro trả phòng | Giá trị bất biến biểu diễn điểm rủi ro trả phòng, được so sánh theo nội dung và không có lifecycle độc lập. | AI Assistant | Không | Không | Có | Không | Không | Không | Không | Không |
| Overstay Prediction | Dự đoán lưu trú quá hạn | Mô hình đọc tối ưu cho tra cứu hoặc điều hành dự đoán lưu trú quá hạn; không được dùng để ghi thay Aggregate. | AI Assistant | Không | Không | Không | Không | Có | Không | Không | Không |
| Guest Profile Summary AI | Tóm tắt hồ sơ khách bằng AI | Mô hình đọc tối ưu cho tra cứu hoặc điều hành tóm tắt hồ sơ khách bằng ai; không được dùng để ghi thay Aggregate. | AI Assistant | Không | Không | Không | Không | Có | Không | Không | Không |
| Anomaly Detection | Phát hiện bất thường | Mô hình đọc tối ưu cho tra cứu hoặc điều hành phát hiện bất thường; không được dùng để ghi thay Aggregate. | AI Assistant | Không | Không | Không | Không | Có | Không | Không | Không |
| AI Recommendation Requested | Khuyến nghị AI đã được yêu cầu | Sự kiện ở thì quá khứ xác nhận khuyến nghị ai đã được yêu cầu đã thực sự xảy ra trong module sở hữu. | AI Assistant | Không | Không | Không | Có | Không | Không | Không | Không |
| AI Recommendation Produced | Khuyến nghị AI đã được tạo | Sự kiện ở thì quá khứ xác nhận khuyến nghị ai đã được tạo đã thực sự xảy ra trong module sở hữu. | AI Assistant | Không | Không | Không | Có | Không | Không | Không | Không |
| AI Recommendation Accepted | Khuyến nghị AI đã được chấp nhận | Sự kiện ở thì quá khứ xác nhận khuyến nghị ai đã được chấp nhận đã thực sự xảy ra trong module sở hữu. | AI Assistant | Không | Không | Không | Có | Không | Không | Không | Không |
| AI Recommendation Rejected | Khuyến nghị AI đã bị từ chối | Sự kiện ở thì quá khứ xác nhận khuyến nghị ai đã bị từ chối đã thực sự xảy ra trong module sở hữu. | AI Assistant | Không | Không | Không | Có | Không | Không | Không | Không |
| AI Recommendation Board | Bảng khuyến nghị AI | Mô hình đọc tối ưu cho tra cứu hoặc điều hành bảng khuyến nghị ai; không được dùng để ghi thay Aggregate. | AI Assistant | Không | Không | Không | Không | Có | Không | Không | Không |
| AI Performance Report | Báo cáo hiệu quả AI | Mô hình đọc tối ưu cho tra cứu hoặc điều hành báo cáo hiệu quả ai; không được dùng để ghi thay Aggregate. | AI Assistant | Không | Không | Không | Không | Có | Không | Không | Không |

### 1.19. Reporting — Báo cáo

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Report Definition | Định nghĩa báo cáo | Aggregate quản lý định nghĩa báo cáo và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Reporting | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Report Parameter | Tham số báo cáo | Thành phần có định danh nghiệp vụ dùng để ghi nhận tham số báo cáo bên trong Aggregate sở hữu. | Reporting | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Report Schedule | Lịch chạy báo cáo | Aggregate quản lý lịch chạy báo cáo và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Reporting | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Report Delivery | Lần phân phối báo cáo | Thành phần có định danh nghiệp vụ dùng để ghi nhận lần phân phối báo cáo bên trong Aggregate sở hữu. | Reporting | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Report Format | Định dạng báo cáo | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho định dạng báo cáo. | Reporting | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Report Status | Trạng thái báo cáo | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái báo cáo; thay đổi phải theo governance domain. | Reporting | Không | Không | Không | Không | Không | Không | Có | Không |
| Operational Report | Báo cáo vận hành | Khái niệm nghiệp vụ dùng để mô tả báo cáo vận hành; không tự động là đối tượng persistence. | Reporting | Không | Không | Không | Không | Không | Không | Không | Không |
| Financial Report | Báo cáo tài chính | Khái niệm nghiệp vụ dùng để mô tả báo cáo tài chính; không tự động là đối tượng persistence. | Reporting | Không | Không | Không | Không | Không | Không | Không | Không |
| Management Report | Báo cáo quản trị | Khái niệm nghiệp vụ dùng để mô tả báo cáo quản trị; không tự động là đối tượng persistence. | Reporting | Không | Không | Không | Không | Không | Không | Không | Không |
| Consolidated Report | Báo cáo hợp nhất | Khái niệm nghiệp vụ dùng để mô tả báo cáo hợp nhất; không tự động là đối tượng persistence. | Reporting | Không | Không | Không | Không | Không | Không | Không | Không |
| Report Snapshot | Ảnh chụp báo cáo | Thành phần có định danh nghiệp vụ dùng để ghi nhận ảnh chụp báo cáo bên trong Aggregate sở hữu. | Reporting | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Report Generated | Báo cáo đã được tạo | Sự kiện ở thì quá khứ xác nhận báo cáo đã được tạo đã thực sự xảy ra trong module sở hữu. | Reporting | Không | Không | Không | Có | Không | Không | Không | Không |
| Report Failed | Tạo báo cáo thất bại | Sự kiện ở thì quá khứ xác nhận tạo báo cáo thất bại đã thực sự xảy ra trong module sở hữu. | Reporting | Không | Không | Không | Có | Không | Không | Không | Không |
| Report Catalog | Danh mục báo cáo | Mô hình đọc tối ưu cho tra cứu hoặc điều hành danh mục báo cáo; không được dùng để ghi thay Aggregate. | Reporting | Không | Không | Không | Không | Có | Không | Không | Không |
| Scheduled Report Board | Bảng báo cáo định kỳ | Mô hình đọc tối ưu cho tra cứu hoặc điều hành bảng báo cáo định kỳ; không được dùng để ghi thay Aggregate. | Reporting | Không | Không | Không | Không | Có | Không | Không | Không |

### 1.20. System — Hệ thống

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| User Account | Tài khoản người dùng | Aggregate quản lý tài khoản người dùng và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | System | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Role | Vai trò | Aggregate quản lý vai trò và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | System | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Permission | Quyền thao tác | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho quyền thao tác. | System | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Role Assignment | Gán vai trò | Thành phần có định danh nghiệp vụ dùng để ghi nhận gán vai trò bên trong Aggregate sở hữu. | System | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Approval Request | Yêu cầu phê duyệt | Aggregate quản lý yêu cầu phê duyệt và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | System | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Approval Step | Bước phê duyệt | Thành phần có định danh nghiệp vụ dùng để ghi nhận bước phê duyệt bên trong Aggregate sở hữu. | System | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Audit Record | Bản ghi kiểm toán | Aggregate quản lý bản ghi kiểm toán và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | System | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Property Configuration | Cấu hình cơ sở | Aggregate quản lý cấu hình cơ sở và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | System | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Number Sequence | Dãy số chứng từ | Aggregate quản lý dãy số chứng từ và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | System | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Lookup Catalog | Danh mục dùng chung | Aggregate quản lý danh mục dùng chung và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | System | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Feature Flag | Cờ tính năng | Aggregate quản lý cờ tính năng và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | System | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Workstation | Máy trạm | Aggregate quản lý máy trạm và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | System | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Session | Phiên đăng nhập | Aggregate quản lý phiên đăng nhập và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | System | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Concurrency Version | Phiên bản đồng thời | Giá trị bất biến biểu diễn phiên bản đồng thời, được so sánh theo nội dung và không có lifecycle độc lập. | System | Không | Không | Có | Không | Không | Không | Không | Không |
| Correlation Identifier | Mã tương quan | Giá trị bất biến biểu diễn mã tương quan, được so sánh theo nội dung và không có lifecycle độc lập. | System | Không | Không | Có | Không | Không | Không | Không | Không |
| Idempotency Key | Khóa chống trùng | Giá trị bất biến biểu diễn khóa chống trùng, được so sánh theo nội dung và không có lifecycle độc lập. | System | Không | Không | Có | Không | Không | Không | Không | Không |
| Permission Granted | Quyền đã được cấp | Sự kiện ở thì quá khứ xác nhận quyền đã được cấp đã thực sự xảy ra trong module sở hữu. | System | Không | Không | Không | Có | Không | Không | Không | Không |
| Configuration Changed | Cấu hình đã thay đổi | Sự kiện ở thì quá khứ xác nhận cấu hình đã thay đổi đã thực sự xảy ra trong module sở hữu. | System | Không | Không | Không | Có | Không | Không | Không | Không |
| Audit Search Result | Kết quả tra cứu kiểm toán | Mô hình đọc tối ưu cho tra cứu hoặc điều hành kết quả tra cứu kiểm toán; không được dùng để ghi thay Aggregate. | System | Không | Không | Không | Không | Có | Không | Không | Không |
| System Health Dashboard | Bảng sức khỏe hệ thống | Mô hình đọc tối ưu cho tra cứu hoặc điều hành bảng sức khỏe hệ thống; không được dùng để ghi thay Aggregate. | System | Không | Không | Không | Không | Có | Không | Không | Không |

### 1.21. Group Operations — Vận hành khách đoàn

| English | Tiếng Việt | Giải thích nghiệp vụ | Module sở hữu | Aggregate | Entity | Value Object | Event | Read Model | Database Table | Enum | Lookup |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Group Stay | Lưu trú đoàn | Aggregate quản lý lưu trú đoàn và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Group Operations | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Group Member Link | Liên kết thành viên đoàn | Thành phần có định danh nghiệp vụ dùng để ghi nhận liên kết thành viên đoàn bên trong Aggregate sở hữu. | Group Operations | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Room Block | Khối phòng đoàn | Aggregate quản lý khối phòng đoàn và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Group Operations | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Room Block Day | Phân bổ khối phòng theo ngày | Thành phần có định danh nghiệp vụ dùng để ghi nhận phân bổ khối phòng theo ngày bên trong Aggregate sở hữu. | Group Operations | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Allotment | Hạn ngạch phòng | Khái niệm nghiệp vụ dùng để mô tả hạn ngạch phòng; không tự động là đối tượng persistence. | Group Operations | Không | Không | Không | Không | Không | Không | Không | Không |
| Pickup | Số phòng đoàn đã sử dụng | Khái niệm nghiệp vụ dùng để mô tả số phòng đoàn đã sử dụng; không tự động là đối tượng persistence. | Group Operations | Không | Không | Không | Không | Không | Không | Không | Không |
| Cut-Off Date | Ngày chốt giữ phòng | Giá trị bất biến biểu diễn ngày chốt giữ phòng, được so sánh theo nội dung và không có lifecycle độc lập. | Group Operations | Không | Không | Có | Không | Không | Không | Không | Không |
| Rooming List | Danh sách khách đoàn | Aggregate quản lý danh sách khách đoàn và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Group Operations | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Rooming Member | Thành viên danh sách đoàn | Thành phần có định danh nghiệp vụ dùng để ghi nhận thành viên danh sách đoàn bên trong Aggregate sở hữu. | Group Operations | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Group Billing Instruction | Chỉ dẫn thanh toán đoàn | Aggregate quản lý chỉ dẫn thanh toán đoàn và bảo vệ vòng đời, trạng thái cùng invariant nghiệp vụ của khái niệm này. | Group Operations | Có | Có (Root) | Không | Không | Không | Có (đề xuất) | Không | Không |
| Billing Route Line | Dòng định tuyến thanh toán đoàn | Thành phần có định danh nghiệp vụ dùng để ghi nhận dòng định tuyến thanh toán đoàn bên trong Aggregate sở hữu. | Group Operations | Không | Có | Không | Không | Không | Có (đề xuất) | Không | Không |
| Group Status | Trạng thái đoàn | Tập trạng thái hoặc loại hữu hạn chuẩn hóa cho trạng thái đoàn; thay đổi phải theo governance domain. | Group Operations | Không | Không | Không | Không | Không | Không | Có | Không |
| Group Type | Loại đoàn | Danh mục tham chiếu có mã và hiệu lực, dùng thống nhất cho loại đoàn. | Group Operations | Không | Không | Không | Không | Không | Có (đề xuất) | Không | Có |
| Group Coordinator | Điều phối viên đoàn | Khái niệm nghiệp vụ dùng để mô tả điều phối viên đoàn; không tự động là đối tượng persistence. | Group Operations | Không | Không | Không | Không | Không | Không | Không | Không |
| Group Stay Created | Lưu trú đoàn đã được tạo | Sự kiện ở thì quá khứ xác nhận lưu trú đoàn đã được tạo đã thực sự xảy ra trong module sở hữu. | Group Operations | Không | Không | Không | Có | Không | Không | Không | Không |
| Rooming List Imported | Danh sách đoàn đã được nhập | Sự kiện ở thì quá khứ xác nhận danh sách đoàn đã được nhập đã thực sự xảy ra trong module sở hữu. | Group Operations | Không | Không | Không | Có | Không | Không | Không | Không |
| Group Member Matched | Thành viên đoàn đã được ghép | Sự kiện ở thì quá khứ xác nhận thành viên đoàn đã được ghép đã thực sự xảy ra trong module sở hữu. | Group Operations | Không | Không | Không | Có | Không | Không | Không | Không |
| Group Arrival Board | Bảng điều hành đoàn đến | Mô hình đọc tối ưu cho tra cứu hoặc điều hành bảng điều hành đoàn đến; không được dùng để ghi thay Aggregate. | Group Operations | Không | Không | Không | Không | Có | Không | Không | Không |
| Group Pickup Report | Báo cáo sử dụng khối phòng | Mô hình đọc tối ưu cho tra cứu hoặc điều hành báo cáo sử dụng khối phòng; không được dùng để ghi thay Aggregate. | Group Operations | Không | Không | Không | Không | Có | Không | Không | Không |
| Group Billing Summary | Tổng hợp thanh toán đoàn | Mô hình đọc tối ưu cho tra cứu hoặc điều hành tổng hợp thanh toán đoàn; không được dùng để ghi thay Aggregate. | Group Operations | Không | Không | Không | Không | Có | Không | Không | Không |

---
## 2. Danh mục đặt tên cho Aggregate

Bảng này chỉ đề xuất tên chuẩn cho bước thiết kế, không tự tạo table, API hoặc class và không thay đổi Aggregate boundary. History, Audit và Archive là tên dành trước; chỉ tạo vật lý khi retention, pháp lý và volume chứng minh cần.

| Aggregate | Entity | SQL Table | Primary Key | Display Code | View | Search View | History Table | Audit Table | Archive Table | Read Model | Report | Dashboard | API | Service | Repository | Domain Event | Integration Event |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Reservation | Reservation | DatPhong | MaDatPhong | SoXacNhan | vwDatPhong | vwTimKiemDatPhong | LichSuDatPhong | NhatKyDatPhong | LuuTruDatPhong | ReservationReadModel | Báo cáo Đặt phòng | Bảng điều hành Đặt phòng | /api/front-office/reservations | ReservationService | ReservationRepository | ReservationCreated | ReservationLifecycleChanged |
| WaitlistEntry | WaitlistEntry | ChoPhong | MaChoPhong | SoChoPhong | vwChoPhong | vwTimKiemChoPhong | LichSuChoPhong | NhatKyChoPhong | LuuTruChoPhong | WaitlistEntryReadModel | Báo cáo Yêu cầu chờ phòng | Bảng điều hành Yêu cầu chờ phòng | /api/front-office/waitlist-entries | WaitlistEntryService | WaitlistEntryRepository | WaitlistEntryCreated | WaitlistChanged |
| RoomBlock | RoomBlock | KhoiPhongDoan | MaKhoiPhongDoan | SoKhoiPhong | vwKhoiPhongDoan | vwTimKiemKhoiPhongDoan | LichSuKhoiPhongDoan | NhatKyKhoiPhongDoan | LuuTruKhoiPhongDoan | RoomBlockReadModel | Báo cáo Khối phòng đoàn | Bảng điều hành Khối phòng đoàn | /api/front-office/room-blocks | RoomBlockService | RoomBlockRepository | RoomBlockCreated | RoomBlockChanged |
| Stay | Stay | LuuTru | MaLuuTru | SoLuuTru | vwLuuTru | vwTimKiemLuuTru | LichSuLuuTru | NhatKyLuuTru | LuuTruLuuTru | StayReadModel | Báo cáo Lưu trú | Bảng điều hành Lưu trú | /api/front-office/stays | StayService | StayRepository | StayCreated | GuestStayUpdated |
| RoomAssignment | RoomAssignment | PhanPhong | MaPhanPhong | SoPhanPhong | vwPhanPhong | vwTimKiemPhanPhong | LichSuPhanPhong | NhatKyPhanPhong | LuuTruPhanPhong | RoomAssignmentReadModel | Báo cáo Phân phòng | Bảng điều hành Phân phòng | /api/front-office/room-assignments | RoomAssignmentService | RoomAssignmentRepository | RoomAssigned | GuestStayUpdated |
| KeyCredential | KeyCredential | QuyenMoKhoaPhong | MaQuyenMoKhoaPhong | SoCapKhoa | vwQuyenMoKhoaPhong | vwTimKiemQuyenMoKhoaPhong | LichSuQuyenMoKhoaPhong | NhatKyQuyenMoKhoaPhong | LuuTruQuyenMoKhoaPhong | KeyCredentialReadModel | Báo cáo Quyền mở khóa phòng | Bảng điều hành Quyền mở khóa phòng | /api/front-office/key-credentials | KeyCredentialService | KeyCredentialRepository | KeyCredentialIssued | KeyCredentialStatusChanged |
| Folio | Folio | SoGiaoDichKhach | MaSoGiaoDichKhach | SoFolio | vwSoGiaoDichKhach | vwTimKiemSoGiaoDichKhach | LichSuSoGiaoDichKhach | NhatKySoGiaoDichKhach | LuuTruSoGiaoDichKhach | FolioReadModel | Báo cáo Sổ giao dịch khách | Bảng điều hành Sổ giao dịch khách | /api/front-office/folios | FolioService | FolioRepository | FolioOpened | FolioStatusChanged |
| FolioWindow | FolioWindow | NganSoGiaoDich | MaNganSoGiaoDich | SoNgan | vwNganSoGiaoDich | vwTimKiemNganSoGiaoDich | LichSuNganSoGiaoDich | NhatKyNganSoGiaoDich | LuuTruNganSoGiaoDich | FolioWindowReadModel | Báo cáo Ngăn sổ giao dịch | Bảng điều hành Ngăn sổ giao dịch | /api/front-office/folio-windows | FolioWindowService | FolioWindowRepository | FolioWindowOpened | FolioFinancialActivityOccurred |
| RoutingInstruction | RoutingInstruction | ChiDanDinhTuyen | MaChiDanDinhTuyen | SoThuTu | vwChiDanDinhTuyen | vwTimKiemChiDanDinhTuyen | LichSuChiDanDinhTuyen | NhatKyChiDanDinhTuyen | LuuTruChiDanDinhTuyen | RoutingInstructionReadModel | Báo cáo Chỉ dẫn định tuyến | Bảng điều hành Chỉ dẫn định tuyến | /api/front-office/routing-instructions | RoutingInstructionService | RoutingInstructionRepository | RoutingInstructionActivated | FolioRoutingChanged |
| Payment | Payment | ThanhToan | MaThanhToan | SoThanhToan | vwThanhToan | vwTimKiemThanhToan | LichSuThanhToan | NhatKyThanhToan | LuuTruThanhToan | PaymentReadModel | Báo cáo Thanh toán | Bảng điều hành Thanh toán | /api/front-office/payments | PaymentService | PaymentRepository | PaymentCaptured | PaymentSettled |
| Deposit | Deposit | DatCoc | MaDatCoc | SoDatCoc | vwDatCoc | vwTimKiemDatCoc | LichSuDatCoc | NhatKyDatCoc | LuuTruDatCoc | DepositReadModel | Báo cáo Đặt cọc | Bảng điều hành Đặt cọc | /api/front-office/deposits | DepositService | DepositRepository | DepositReceived | DepositLiabilityChanged |
| CashierShift | CashierShift | CaThuNgan | MaCaThuNgan | SoCaThuNgan | vwCaThuNgan | vwTimKiemCaThuNgan | LichSuCaThuNgan | NhatKyCaThuNgan | LuuTruCaThuNgan | CashierShiftReadModel | Báo cáo Ca thu ngân | Bảng điều hành Ca thu ngân | /api/front-office/cashier-shifts | CashierShiftService | CashierShiftRepository | CashierShiftOpened | CashierShiftChanged |
| RefundRequest | RefundRequest | YeuCauHoanTien | MaYeuCauHoanTien | SoYeuCauHoanTien | vwYeuCauHoanTien | vwTimKiemYeuCauHoanTien | LichSuYeuCauHoanTien | NhatKyYeuCauHoanTien | LuuTruYeuCauHoanTien | RefundRequestReadModel | Báo cáo Yêu cầu hoàn tiền | Bảng điều hành Yêu cầu hoàn tiền | /api/front-office/refund-requests | RefundRequestService | RefundRequestRepository | RefundRequested | RefundSettled |
| RoomAvailability | RoomAvailability | TonPhongKhaDung | MaTonPhongKhaDung | MaNgayHangPhong | vwTonPhongKhaDung | vwTimKiemTonPhongKhaDung | LichSuTonPhongKhaDung | NhatKyTonPhongKhaDung | LuuTruTonPhongKhaDung | RoomAvailabilityReadModel | Báo cáo Tồn phòng khả dụng | Bảng điều hành Tồn phòng khả dụng | /api/front-office/room-availability | RoomAvailabilityService | RoomAvailabilityRepository | RoomAvailabilityChanged | RoomAvailabilityChanged |
| RoomInventoryControl | RoomInventoryControl | KiemSoatTonPhong | MaKiemSoatTonPhong | SoKiemSoat | vwKiemSoatTonPhong | vwTimKiemKiemSoatTonPhong | LichSuKiemSoatTonPhong | NhatKyKiemSoatTonPhong | LuuTruKiemSoatTonPhong | RoomInventoryControlReadModel | Báo cáo Kiểm soát tồn phòng | Bảng điều hành Kiểm soát tồn phòng | /api/front-office/room-inventory-controls | RoomInventoryControlService | RoomInventoryControlRepository | RoomInventoryControlled | RoomAvailabilityRestrictionChanged |
| RoomOperation | RoomOperation | VanHanhBuongPhong | MaVanHanhBuongPhong | MaPhongHienThi | vwVanHanhBuongPhong | vwTimKiemVanHanhBuongPhong | LichSuVanHanhBuongPhong | NhatKyVanHanhBuongPhong | LuuTruVanHanhBuongPhong | RoomOperationReadModel | Báo cáo Vận hành buồng phòng | Bảng điều hành Vận hành buồng phòng | /api/front-office/room-operations | RoomOperationService | RoomOperationRepository | RoomOperationalStatusChanged | RoomReadinessChanged |
| HousekeepingTask | HousekeepingTask | CongViecBuongPhong | MaCongViecBuongPhong | SoCongViec | vwCongViecBuongPhong | vwTimKiemCongViecBuongPhong | LichSuCongViecBuongPhong | NhatKyCongViecBuongPhong | LuuTruCongViecBuongPhong | HousekeepingTaskReadModel | Báo cáo Công việc buồng phòng | Bảng điều hành Công việc buồng phòng | /api/front-office/housekeeping-tasks | HousekeepingTaskService | HousekeepingTaskRepository | HousekeepingTaskAssigned | HousekeepingTaskChanged |
| RoomInspection | RoomInspection | KiemTraPhong | MaKiemTraPhong | SoKiemTra | vwKiemTraPhong | vwTimKiemKiemTraPhong | LichSuKiemTraPhong | NhatKyKiemTraPhong | LuuTruKiemTraPhong | RoomInspectionReadModel | Báo cáo Kiểm tra phòng | Bảng điều hành Kiểm tra phòng | /api/front-office/room-inspections | RoomInspectionService | RoomInspectionRepository | RoomInspectionPassed | RoomReadinessChanged |
| MaintenanceCase | MaintenanceCase | HoSoBaoTri | MaHoSoBaoTri | SoHoSoBaoTri | vwHoSoBaoTri | vwTimKiemHoSoBaoTri | LichSuHoSoBaoTri | NhatKyHoSoBaoTri | LuuTruHoSoBaoTri | MaintenanceCaseReadModel | Báo cáo Hồ sơ bảo trì | Bảng điều hành Hồ sơ bảo trì | /api/front-office/maintenance-cases | MaintenanceCaseService | MaintenanceCaseRepository | MaintenanceCaseReported | MaintenanceImpactChanged |
| RoomOperationalBlock | RoomOperationalBlock | KhoaVanHanhPhong | MaKhoaVanHanhPhong | SoKhoaPhong | vwKhoaVanHanhPhong | vwTimKiemKhoaVanHanhPhong | LichSuKhoaVanHanhPhong | NhatKyKhoaVanHanhPhong | LuuTruKhoaVanHanhPhong | RoomOperationalBlockReadModel | Báo cáo Khóa vận hành phòng | Bảng điều hành Khóa vận hành phòng | /api/front-office/room-operational-blocks | RoomOperationalBlockService | RoomOperationalBlockRepository | RoomBlockActivated | RoomAvailabilityRestrictionChanged |
| ServiceRequest | ServiceRequest | YeuCauDichVu | MaYeuCauDichVu | SoYeuCauDichVu | vwYeuCauDichVu | vwTimKiemYeuCauDichVu | LichSuYeuCauDichVu | NhatKyYeuCauDichVu | LuuTruYeuCauDichVu | ServiceRequestReadModel | Báo cáo Yêu cầu dịch vụ | Bảng điều hành Yêu cầu dịch vụ | /api/front-office/service-requests | ServiceRequestService | ServiceRequestRepository | ServiceRequestRaised | FrontOfficeTaskRequested |
| WakeUpCall | WakeUpCall | CuocGoiBaoThuc | MaCuocGoiBaoThuc | SoYeuCauBaoThuc | vwCuocGoiBaoThuc | vwTimKiemCuocGoiBaoThuc | LichSuCuocGoiBaoThuc | NhatKyCuocGoiBaoThuc | LuuTruCuocGoiBaoThuc | WakeUpCallReadModel | Báo cáo Cuộc gọi báo thức | Bảng điều hành Cuộc gọi báo thức | /api/front-office/wake-up-calls | WakeUpCallService | WakeUpCallRepository | WakeUpCallScheduled | NotificationRequested |
| LostFoundCase | LostFoundCase | HoSoDoThatLac | MaHoSoDoThatLac | SoHoSoThatLac | vwHoSoDoThatLac | vwTimKiemHoSoDoThatLac | LichSuHoSoDoThatLac | NhatKyHoSoDoThatLac | LuuTruHoSoDoThatLac | LostFoundCaseReadModel | Báo cáo Hồ sơ đồ thất lạc | Bảng điều hành Hồ sơ đồ thất lạc | /api/front-office/lost-found-cases | LostFoundCaseService | LostFoundCaseRepository | LostFoundItemRegistered | NotificationRequested |
| GroupStay | GroupStay | LuuTruDoan | MaLuuTruDoan | SoDoan | vwLuuTruDoan | vwTimKiemLuuTruDoan | LichSuLuuTruDoan | NhatKyLuuTruDoan | LuuTruLuuTruDoan | GroupStayReadModel | Báo cáo Lưu trú đoàn | Bảng điều hành Lưu trú đoàn | /api/front-office/group-stays | GroupStayService | GroupStayRepository | GroupStayCreated | GroupStayChanged |
| RoomingList | RoomingList | DanhSachKhachDoan | MaDanhSachKhachDoan | SoPhienDanhSach | vwDanhSachKhachDoan | vwTimKiemDanhSachKhachDoan | LichSuDanhSachKhachDoan | NhatKyDanhSachKhachDoan | LuuTruDanhSachKhachDoan | RoomingListReadModel | Báo cáo Danh sách khách đoàn | Bảng điều hành Danh sách khách đoàn | /api/front-office/rooming-lists | RoomingListService | RoomingListRepository | RoomingListImported | GroupRoomingChanged |
| GroupBillingInstruction | GroupBillingInstruction | ChiDanThanhToanDoan | MaChiDanThanhToanDoan | SoPhienChiDan | vwChiDanThanhToanDoan | vwTimKiemChiDanThanhToanDoan | LichSuChiDanThanhToanDoan | NhatKyChiDanThanhToanDoan | LuuTruChiDanThanhToanDoan | GroupBillingInstructionReadModel | Báo cáo Chỉ dẫn thanh toán đoàn | Bảng điều hành Chỉ dẫn thanh toán đoàn | /api/front-office/group-billing-instructions | GroupBillingInstructionService | GroupBillingInstructionRepository | GroupBillingInstructionChanged | FolioRoutingChanged |
| NightAuditRun | NightAuditRun | PhienKiemToanDem | MaPhienKiemToanDem | SoPhienKiemToan | vwPhienKiemToanDem | vwTimKiemPhienKiemToanDem | LichSuPhienKiemToanDem | NhatKyPhienKiemToanDem | LuuTruPhienKiemToanDem | NightAuditRunReadModel | Báo cáo Phiên kiểm toán đêm | Bảng điều hành Phiên kiểm toán đêm | /api/front-office/night-audit-runs | NightAuditRunService | NightAuditRunRepository | NightAuditStarted | BusinessDateAdvanced |
| AuditChecklist | AuditChecklist | DanhMucKiemToan | MaDanhMucKiemToan | SoPhienDanhMuc | vwDanhMucKiemToan | vwTimKiemDanhMucKiemToan | LichSuDanhMucKiemToan | NhatKyDanhMucKiemToan | LuuTruDanhMucKiemToan | AuditChecklistReadModel | Báo cáo Danh mục kiểm toán | Bảng điều hành Danh mục kiểm toán | /api/front-office/audit-checklists | AuditChecklistService | AuditChecklistRepository | AuditChecklistActivated | NightAuditControlChanged |
| AuditException | AuditException | NgoaiLeKiemToan | MaNgoaiLeKiemToan | SoNgoaiLe | vwNgoaiLeKiemToan | vwTimKiemNgoaiLeKiemToan | LichSuNgoaiLeKiemToan | NhatKyNgoaiLeKiemToan | LuuTruNgoaiLeKiemToan | AuditExceptionReadModel | Báo cáo Ngoại lệ kiểm toán | Bảng điều hành Ngoại lệ kiểm toán | /api/front-office/audit-exceptions | AuditExceptionService | AuditExceptionRepository | AuditExceptionRaised | FrontOfficeTaskRequested |
| OperationalTask | OperationalTask | CongViecVanHanh | MaCongViecVanHanh | SoCongViec | vwCongViecVanHanh | vwTimKiemCongViecVanHanh | LichSuCongViecVanHanh | NhatKyCongViecVanHanh | LuuTruCongViecVanHanh | OperationalTaskReadModel | Báo cáo Công việc vận hành | Bảng điều hành Công việc vận hành | /api/front-office/operational-tasks | OperationalTaskService | OperationalTaskRepository | OperationalTaskCreated | FrontOfficeTaskChanged |
| ShiftHandover | ShiftHandover | BanGiaoCa | MaBanGiaoCa | SoBanGiaoCa | vwBanGiaoCa | vwTimKiemBanGiaoCa | LichSuBanGiaoCa | NhatKyBanGiaoCa | LuuTruBanGiaoCa | ShiftHandoverReadModel | Báo cáo Bàn giao ca | Bảng điều hành Bàn giao ca | /api/front-office/shift-handovers | ShiftHandoverService | ShiftHandoverRepository | ShiftHandoverPublished | ShiftHandoverChanged |
| IntegrationJob | IntegrationJob | CongViecTichHop | MaCongViecTichHop | SoCongViecTichHop | vwCongViecTichHop | vwTimKiemCongViecTichHop | LichSuCongViecTichHop | NhatKyCongViecTichHop | LuuTruCongViecTichHop | IntegrationJobReadModel | Báo cáo Công việc tích hợp | Bảng điều hành Công việc tích hợp | /api/front-office/integration-jobs | IntegrationJobService | IntegrationJobRepository | IntegrationJobQueued | IntegrationJobStatusChanged |
| IntegrationException | IntegrationException | NgoaiLeTichHop | MaNgoaiLeTichHop | SoNgoaiLeTichHop | vwNgoaiLeTichHop | vwTimKiemNgoaiLeTichHop | LichSuNgoaiLeTichHop | NhatKyNgoaiLeTichHop | LuuTruNgoaiLeTichHop | IntegrationExceptionReadModel | Báo cáo Ngoại lệ tích hợp | Bảng điều hành Ngoại lệ tích hợp | /api/front-office/integration-exceptions | IntegrationExceptionService | IntegrationExceptionRepository | IntegrationExceptionRaised | IntegrationExceptionChanged |
| ExternalReference | ExternalReference | ThamChieuBenNgoai | MaThamChieuBenNgoai | MaThamChieuHienThi | vwThamChieuBenNgoai | vwTimKiemThamChieuBenNgoai | LichSuThamChieuBenNgoai | NhatKyThamChieuBenNgoai | LuuTruThamChieuBenNgoai | ExternalReferenceReadModel | Báo cáo Tham chiếu bên ngoài | Bảng điều hành Tham chiếu bên ngoài | /api/front-office/external-references | ExternalReferenceService | ExternalReferenceRepository | ExternalReferenceRegistered | ExternalReferenceChanged |
| IdempotencyRecord | IdempotencyRecord | BanGhiChongTrung | MaBanGhiChongTrung | KhoaChongTrungHienThi | vwBanGhiChongTrung | vwTimKiemBanGhiChongTrung | LichSuBanGhiChongTrung | NhatKyBanGhiChongTrung | LuuTruBanGhiChongTrung | IdempotencyRecordReadModel | Báo cáo Bản ghi chống trùng | Bảng điều hành Bản ghi chống trùng | /api/front-office/idempotency-records | IdempotencyRecordService | IdempotencyRecordRepository | IdempotencyRecordCompleted | IdempotencyOutcomeChanged |
| PropertyBusinessDate | PropertyBusinessDate | NgayNghiepVuCoSo | MaNgayNghiepVuCoSo | NgayNghiepVu | vwNgayNghiepVuCoSo | vwTimKiemNgayNghiepVuCoSo | LichSuNgayNghiepVuCoSo | NhatKyNgayNghiepVuCoSo | LuuTruNgayNghiepVuCoSo | PropertyBusinessDateReadModel | Báo cáo Ngày nghiệp vụ cơ sở | Bảng điều hành Ngày nghiệp vụ cơ sở | /api/front-office/property-business-dates | PropertyBusinessDateService | PropertyBusinessDateRepository | BusinessDateAdvanced | BusinessDateAdvanced |

---

## 3. Quy tắc đặt tên Database

### 3.1. Chuẩn bắt buộc

- Database dùng **tiếng Việt không dấu, PascalCase**: `DatPhong`, `LuuTru`, `ThanhToan`, `DatCoc`, `BaoTri`, `BuongPhong`.
- Tên số ít cho Aggregate/table header; cấm hậu tố mơ hồ `Info`, `Data`, `Header`, `Detail`, `Master` nếu không có nghĩa nghiệp vụ.
- View dùng `vw{TenNghiepVu}`; search view dùng `vwTimKiem{TenNghiepVu}`. Read projection không được ghi thay Aggregate.
- Từ kỹ thuật như Outbox/Inbox chỉ dùng trong schema kỹ thuật và phải có ownership riêng.

| Mục đích | Quy tắc | Ví dụ đúng | Cấm |
|---|---|---|---|
| Primary Key | `Ma{KháiNiệm}` | `MaDatPhong`, `MaLuuTru`, `MaThanhToan` | `Id` hoặc tên tiếng Anh |
| Foreign Key | đúng tên PK được tham chiếu | `MaKhachHang`, `MaKhachSan`, `MaPhong` | `RefId`, `GuestKey` |
| Display Code | `So...` hoặc `Ma...HienThi` | `SoXacNhan`, `SoLuuTru` | dùng PK làm số chứng từ |
| Boolean | `Co...`, `Da...`, `Can...` | `CoBaoDam`, `DaKiemTra`, `CanPheDuyet` | `IsActive`, phủ định kép |
| Calendar date | `Ngay...` | `NgayDen`, `NgayDi`, `NgayNghiepVu` | tên không rõ semantics |
| Instant | `ThoiDiem...` | `ThoiDiemNhanPhong` | dùng `Ngay...` cho timestamp |
| Time of day | `Gio...` | `GioBaoThuc` | giờ local thiếu timezone khi cần instant |
| Status | `TrangThai` trong scope table | `TrangThai` | `Status1` hoặc trạng thái chồng nghĩa |
| Amount | `SoTien...` và currency | `SoTienThanhToan`, `MaTienTe` | decimal không currency |
| Quantity | `SoLuong...` và unit | `SoLuongPhong`, `MaDonViTinh` | `Qty1` |
| Effective range | `NgayHieuLucTu/Den` hoặc `ThoiDiemHieuLucTu/Den` | theo semantics | `StartDate/EndDate` |
| Version | `PhienBan` | `PhienBan` | trộn business version và concurrency token |
| Audit | `NgayTao`, `NguoiTao`, `NgayCapNhat`, `NguoiCapNhat` | thống nhất | biến thể tiếng Anh |
| Soft deletion | Không mặc định | `DaVoHieu` kèm reason/effective time | `IsDeleted` che xóa financial fact |

PK dùng `PK_{TenBang}`; FK dùng `FK_{BangCon}_{BangCha}_{TenCot}`; unique dùng `UQ_{TenBang}_{Cot}`; check dùng `CK_{TenBang}_{QuyTac}`; index dùng `IX_{TenBang}_{Cot}`. Database name không quyết định ownership; schema/module boundary và quyền ghi vẫn theo Bounded Context.

---

## 4. Quy tắc đặt tên Entity và Value Object

- Code dùng tiếng Anh, PascalCase, số ít: `Reservation`, `Stay`, `Payment`, `Deposit`, `RefundRequest`, `HousekeepingTask`, `MaintenanceCase`.
- Aggregate Root dùng canonical term; cấm hậu tố `Info`, `Model`, `Header`, `Data`, `BO`.
- Entity con thể hiện vai trò: `ReservationGuest`, `DepositAllocation`, `PaymentAttempt`.
- Value Object là danh từ giá trị: `Money`, `BusinessDate`, `StayPeriod`, `IdentityDocument`; không thêm `ValueObject`.
- Identity trong code là `{Type}Id`; database ánh xạ `Ma{KháiNiệm}`. Display code không phải ID.
- Aggregate chuẩn là `RefundRequest`, không tạo `Refund` song song. `RoomBlock` là khối phòng đoàn; khóa kỹ thuật là `RoomOperationalBlock`.

## 5. Quy tắc đặt tên ViewModel

- Dùng `{UseCase|Screen|ReadModel}ViewModel`: `ReservationViewModel`, `StayViewModel`, `GuestViewModel`, `PaymentViewModel`.
- Use case: `CheckInViewModel`, `RoomMoveViewModel`, `CheckoutViewModel`, `NightAuditViewModel`.
- Search/list: `ReservationSearchViewModel`, `ArrivalListViewModel`; item: `ReservationListItemViewModel`.
- ViewModel không phải domain object hay persistence model; cấm `VM`, `ModelView`, `DataContext1`.
- UI label dùng tiếng Việt có dấu.

## 6. Quy tắc đặt tên Service

- Application service: `ReservationService`, `StayService`, `PaymentService`; capability lớn tách `CheckInService`, `CheckoutService`.
- Domain Service: `AvailabilityService`, `RoomAssignmentService`, `CheckoutReadinessService`; cấm `Helper`, `Manager`, `Utility`.
- Integration adapter: `KeyProviderAdapter`, `PaymentGatewayAdapter`; không gọi là Domain Service.
- Query/report: `ReservationQueryService`, `FrontOfficeReportingService`.
- Nếu dùng prefix interface `I` thì áp dụng nhất quán; business name vẫn là phần sau prefix.

## 7. Quy tắc đặt tên Repository

- Chỉ Aggregate Root có repository: `ReservationRepository`, `StayRepository`, `PaymentRepository`.
- Entity con không có repository độc lập nếu chỉ truy cập qua Root.
- Read model dùng `...QueryService` hoặc `...ReadRepository`; không đánh tráo với domain repository.
- Cấm `GenericRepository`, `BaseRepository<T>` làm public domain abstraction và cấm query tùy ý xuyên layer.

## 8. Quy tắc đặt tên Event

- Event ở thì quá khứ: `ReservationCreated`, `StayCheckedIn`, `PaymentCaptured`, `DepositAllocated`, `NightAuditCompleted`.
- Chủ thể trước, sự thật sau; cấm `On...`, `Handle...`, `Process...`, `...EventData`.
- Domain Event là sự thật nội bộ; Integration Event là contract ổn định, ví dụ `GuestStayStarted`.
- Canonical domain event là `StayCheckedIn`, không dùng `GuestCheckedIn`. `GuestStayStarted` chỉ dành cho integration contract.
- Version thuộc metadata/contract; không tùy tiện thêm `V2` vào business name.

## 9. Quy tắc đặt tên API, Command và Query

| Mục đích | Chuẩn | Ví dụ |
|---|---|---|
| Đọc collection | GET + danh từ số nhiều | `GET /api/front-office/reservations` |
| Đọc một resource | GET /{id} | `GET /api/front-office/stays/{stayId}` |
| Tạo resource/intention | POST collection | `POST /api/front-office/reservations` |
| Hành động domain | POST /{id}/{action-kebab-case} | `POST /stays/{id}/check-in` |
| Replace đầy đủ | PUT chỉ khi thật sự replace | Không dùng PUT để né command |
| Partial update | PATCH nếu domain cho phép | Không dùng cho transition tài chính |
| Xóa | DELETE chỉ khi domain cho phép | Không DELETE FolioEntry/Payment/Audit |
| Search phức tạp | POST /search; đơn giản dùng GET query | Tách khỏi command |

Command dùng `CreateReservation`, `ConfirmReservation`, `CheckInStay`, `MoveRoom`, `CapturePayment`, `AdvanceBusinessDate`. Query dùng `GetStay`, `SearchReservations`, `ListArrivals`. DTO theo use case: `CheckInStayRequest`, `CheckInStayResult`. URI tiếng Anh lowercase kebab-case; UI/error tiếng Việt có dấu; API không công khai tên SQL table và không cho client set state trực tiếp.

## 10. Quy tắc đặt tên File, Code, UI và Report

| Phạm vi | Ngôn ngữ/kiểu | Chuẩn và ví dụ |
|---|---|---|
| Markdown | Tiếng Việt không dấu, UPPER_SNAKE_CASE | `THIET_KE_DATABASE.md`, `DAC_TA_UI.md`, `TU_DIEN_NGHIEP_VU_ASMILE_BO.md` |
| Code file/type | Tiếng Anh, PascalCase | `Reservation.cs`, `CheckInViewModel.cs`, `PaymentService.cs` |
| Database | Tiếng Việt không dấu, PascalCase | `DatPhong`, `MaDatPhong`, `NgayDen` |
| UI label | Tiếng Việt có dấu | “Nhận phòng”, “Số dư sổ giao dịch” |
| Report title | Tiếng Việt có dấu | “Báo cáo khách đến”, “Báo cáo chênh lệch ca thu ngân” |
| Report technical key | Việt không dấu có prefix | `rpKhachDen`, `rpChenhLechCaThuNgan` |
| View | Database style với prefix | `vwDatPhong`, `vwTimKiemDatPhong` |
| Attachment/export | Tên hiển thị Việt; filename sanitize | có ngày/property/report key, không chứa PII |

Không đổi canonical term chỉ để ngắn hơn. Viết tắt chỉ dùng theo mục 13 và không thay canonical type name.
---
## 11. Bảng ánh xạ chuẩn toàn bộ Front Office

Chuỗi ánh xạ chuẩn là: **English domain → tiếng Việt nghiệp vụ → Database → Entity → ViewModel → Service → Repository → Event → View → Report**.

| Domain | Tiếng Việt | Database | Entity | ViewModel | Service | Repository | Event | View | Report |
|---|---|---|---|---|---|---|---|---|---|
| Reservation | Đặt phòng | DatPhong | Reservation | ReservationViewModel | ReservationService | ReservationRepository | ReservationCreated | vwDatPhong | rpDatPhong |
| WaitlistEntry | Yêu cầu chờ phòng | ChoPhong | WaitlistEntry | WaitlistViewModel | WaitlistService | WaitlistEntryRepository | WaitlistEntryCreated | vwChoPhong | rpChoPhong |
| RoomBlock | Khối phòng đoàn | KhoiPhongDoan | RoomBlock | RoomBlockViewModel | RoomBlockService | RoomBlockRepository | RoomBlockCreated | vwKhoiPhongDoan | rpSuDungKhoiPhong |
| Stay | Lưu trú | LuuTru | Stay | StayViewModel | StayService | StayRepository | StayCheckedIn | vwLuuTru | rpLuuTru |
| RoomAssignment | Phân phòng | PhanPhong | RoomAssignment | RoomAssignmentViewModel | RoomAssignmentService | RoomAssignmentRepository | RoomAssigned | vwPhanPhong | rpPhanPhong |
| KeyCredential | Quyền mở khóa phòng | QuyenMoKhoaPhong | KeyCredential | KeyCredentialViewModel | KeyCredentialService | KeyCredentialRepository | KeyCredentialIssued | vwQuyenMoKhoaPhong | rpCapKhoaPhong |
| Folio | Sổ giao dịch khách | SoGiaoDichKhach | Folio | FolioViewModel | FolioService | FolioRepository | FolioOpened | vwSoGiaoDichKhach | rpSoGiaoDichKhach |
| FolioWindow | Ngăn sổ giao dịch | NganSoGiaoDich | FolioWindow | FolioWindowViewModel | FolioWindowService | FolioWindowRepository | FolioWindowOpened | vwNganSoGiaoDich | rpNganSoGiaoDich |
| RoutingInstruction | Chỉ dẫn định tuyến | ChiDanDinhTuyen | RoutingInstruction | RoutingInstructionViewModel | RoutingService | RoutingInstructionRepository | RoutingInstructionActivated | vwChiDanDinhTuyen | rpDinhTuyenChiPhi |
| Payment | Thanh toán | ThanhToan | Payment | PaymentViewModel | PaymentService | PaymentRepository | PaymentCaptured | vwThanhToan | rpThanhToan |
| Deposit | Đặt cọc | DatCoc | Deposit | DepositViewModel | DepositService | DepositRepository | DepositAllocated | vwDatCoc | rpDatCoc |
| CashierShift | Ca thu ngân | CaThuNgan | CashierShift | CashierShiftViewModel | CashierShiftService | CashierShiftRepository | CashierShiftClosed | vwCaThuNgan | rpCaThuNgan |
| RefundRequest | Yêu cầu hoàn tiền | YeuCauHoanTien | RefundRequest | RefundRequestViewModel | RefundService | RefundRequestRepository | RefundRequested | vwYeuCauHoanTien | rpHoanTien |
| RoomAvailability | Tồn phòng khả dụng | TonPhongKhaDung | RoomAvailability | RoomAvailabilityViewModel | AvailabilityService | RoomAvailabilityRepository | RoomAvailabilityChanged | vwTonPhongKhaDung | rpTonPhong |
| RoomInventoryControl | Kiểm soát tồn phòng | KiemSoatTonPhong | RoomInventoryControl | RoomInventoryControlViewModel | InventoryControlService | RoomInventoryControlRepository | RoomInventoryControlled | vwKiemSoatTonPhong | rpKiemSoatTonPhong |
| RoomOperation | Vận hành buồng phòng | VanHanhBuongPhong | RoomOperation | RoomOperationViewModel | RoomOperationService | RoomOperationRepository | RoomOperationalStatusChanged | vwVanHanhBuongPhong | rpTrangThaiBuongPhong |
| HousekeepingTask | Công việc buồng phòng | CongViecBuongPhong | HousekeepingTask | HousekeepingTaskViewModel | HousekeepingService | HousekeepingTaskRepository | HousekeepingTaskAssigned | vwCongViecBuongPhong | rpCongViecBuongPhong |
| RoomInspection | Kiểm tra phòng | KiemTraPhong | RoomInspection | RoomInspectionViewModel | RoomInspectionService | RoomInspectionRepository | RoomInspectionPassed | vwKiemTraPhong | rpKiemTraPhong |
| MaintenanceCase | Hồ sơ bảo trì | HoSoBaoTri | MaintenanceCase | MaintenanceCaseViewModel | MaintenanceService | MaintenanceCaseRepository | MaintenanceCaseReported | vwHoSoBaoTri | rpBaoTri |
| RoomOperationalBlock | Khóa vận hành phòng | KhoaVanHanhPhong | RoomOperationalBlock | RoomOperationalBlockViewModel | RoomBlockService | RoomOperationalBlockRepository | RoomBlockActivated | vwKhoaVanHanhPhong | rpPhongNgungKhaiThac |
| ServiceRequest | Yêu cầu dịch vụ | YeuCauDichVu | ServiceRequest | ServiceRequestViewModel | GuestService | ServiceRequestRepository | ServiceRequestRaised | vwYeuCauDichVu | rpDichVuKhach |
| WakeUpCall | Cuộc gọi báo thức | CuocGoiBaoThuc | WakeUpCall | WakeUpCallViewModel | WakeUpCallService | WakeUpCallRepository | WakeUpCallScheduled | vwCuocGoiBaoThuc | rpBaoThuc |
| LostFoundCase | Hồ sơ đồ thất lạc | HoSoDoThatLac | LostFoundCase | LostFoundViewModel | LostFoundService | LostFoundCaseRepository | LostFoundItemRegistered | vwHoSoDoThatLac | rpDoThatLac |
| GroupStay | Lưu trú đoàn | LuuTruDoan | GroupStay | GroupStayViewModel | GroupStayService | GroupStayRepository | GroupStayCreated | vwLuuTruDoan | rpLuuTruDoan |
| RoomingList | Danh sách khách đoàn | DanhSachKhachDoan | RoomingList | RoomingListViewModel | RoomingListService | RoomingListRepository | RoomingListImported | vwDanhSachKhachDoan | rpDanhSachKhachDoan |
| GroupBillingInstruction | Chỉ dẫn thanh toán đoàn | ChiDanThanhToanDoan | GroupBillingInstruction | GroupBillingViewModel | GroupBillingService | GroupBillingInstructionRepository | GroupBillingInstructionChanged | vwChiDanThanhToanDoan | rpThanhToanDoan |
| NightAuditRun | Phiên kiểm toán đêm | PhienKiemToanDem | NightAuditRun | NightAuditViewModel | NightAuditService | NightAuditRunRepository | NightAuditCompleted | vwPhienKiemToanDem | rpKiemToanDem |
| AuditChecklist | Danh mục kiểm toán | DanhMucKiemToan | AuditChecklist | AuditChecklistViewModel | AuditChecklistService | AuditChecklistRepository | AuditChecklistCompleted | vwDanhMucKiemToan | rpDanhMucKiemToan |
| AuditException | Ngoại lệ kiểm toán | NgoaiLeKiemToan | AuditException | AuditExceptionViewModel | AuditExceptionService | AuditExceptionRepository | AuditExceptionRaised | vwNgoaiLeKiemToan | rpNgoaiLeKiemToan |
| OperationalTask | Công việc vận hành | CongViecVanHanh | OperationalTask | TaskCenterViewModel | OperationalTaskService | OperationalTaskRepository | OperationalTaskCreated | vwCongViecVanHanh | rpCongViecVanHanh |
| ShiftHandover | Bàn giao ca | BanGiaoCa | ShiftHandover | ShiftHandoverViewModel | ShiftHandoverService | ShiftHandoverRepository | ShiftHandoverPublished | vwBanGiaoCa | rpBanGiaoCa |
| IntegrationJob | Công việc tích hợp | CongViecTichHop | IntegrationJob | IntegrationJobViewModel | IntegrationJobService | IntegrationJobRepository | IntegrationJobQueued | vwCongViecTichHop | rpCongViecTichHop |
| IntegrationException | Ngoại lệ tích hợp | NgoaiLeTichHop | IntegrationException | IntegrationExceptionViewModel | IntegrationExceptionService | IntegrationExceptionRepository | IntegrationExceptionRaised | vwNgoaiLeTichHop | rpNgoaiLeTichHop |
| ExternalReference | Tham chiếu bên ngoài | ThamChieuBenNgoai | ExternalReference | ExternalReferenceViewModel | ExternalReferenceService | ExternalReferenceRepository | ExternalReferenceRegistered | vwThamChieuBenNgoai | rpThamChieuBenNgoai |
| IdempotencyRecord | Bản ghi chống trùng | BanGhiChongTrung | IdempotencyRecord | IdempotencyViewModel | IdempotencyService | IdempotencyRecordRepository | IdempotencyRecordCompleted | vwBanGhiChongTrung | rpChongTrung |
| PropertyBusinessDate | Ngày nghiệp vụ cơ sở | NgayNghiepVuCoSo | PropertyBusinessDate | BusinessDateViewModel | BusinessDateService | PropertyBusinessDateRepository | BusinessDateAdvanced | vwNgayNghiepVuCoSo | rpLichSuNgayNghiepVu |

Tên trong bảng là canonical. Khi một màn hình/use case cần tên hẹp hơn, phần gốc vẫn phải giữ canonical term; ví dụ `ReservationSearchViewModel` là biến thể hợp lệ của `Reservation`, không tạo khái niệm `BookingInfo`.
---
## 12. Các thuật ngữ khó và quyết định dịch

| Thuật ngữ | Tên tiếng Việt đề xuất | Giải thích nghiệp vụ và lý do chọn tên | Giữ tiếng Anh? |
|---|---|---|---|
| Folio | Sổ giao dịch khách | Sổ phụ theo Stay/payer, chứa charge, credit, transfer, settlement representation và balance. “Hóa đơn” sai vì Invoice là chứng từ thuế; “phiếu tính tiền” chỉ là bản in. | Giữ `Folio` trong code/API; UI dùng “Sổ giao dịch khách”, có thể ghi “Folio” phụ chú giai đoạn chuyển đổi. |
| Ledger | Sổ kế toán/sổ công nợ theo ngữ cảnh | Ledger là tập bút toán bất biến có debit/credit và kỳ. Không dịch cố định thành “sổ cái” vì General Ledger mới là Sổ cái. | Giữ trong code của Accounting; UI phải nêu rõ loại sổ. |
| Routing | Định tuyến chi phí | Quyết định charge nào đi tới Folio Window/payer nào trước khi posting. Không phải “chuyển khoản” vì chưa có giao dịch chuyển sau ghi sổ. | Code giữ `Routing`; UI dùng “Định tuyến chi phí”. |
| Entitlement | Quyền hưởng dịch vụ | Quyền của guest đối với breakfast, lounge, parking... phát sinh từ package/rate/tier. Không dịch là “ưu đãi” vì entitlement có thể là quyền đã mua. | Code giữ `Entitlement`; UI dùng tiếng Việt. |
| Posting | Ghi sổ giao dịch | Hành vi tạo bút toán bất biến vào Folio/ledger với source, Business Date và reference. Không phải lưu record CRUD. | Code/event giữ `Posting/Post`; UI dùng “Ghi sổ”. |
| Allowance | Khoản miễn giảm | Credit thương mại hoặc service recovery làm giảm nghĩa vụ của khách; khác reversal sửa sai và refund trả tiền. | Code giữ `Allowance` để phân biệt discount/adjustment. |
| Adjustment | Điều chỉnh | Bút toán sửa sai hoặc điều chỉnh có reference, reason và approval; không sửa trực tiếp entry gốc. | Code giữ `Adjustment`; UI “Điều chỉnh”. |
| Business Date | Ngày nghiệp vụ | Ngày vận hành đang mở của từng Property, chỉ thay đổi bằng Night Audit; không đồng nghĩa ngày lịch hay ngày hạch toán. | Bắt buộc giữ `BusinessDate` trong code; UI “Ngày nghiệp vụ”. |
| Pickup | Số phòng đoàn đã sử dụng | Lượng room-night từ Room Block đã được chuyển thành reservation/stay thực tế. “Nhận phòng” sai vì pickup xảy ra từ lúc booking được ghi nhận. | Report có thể ghi “Pickup (đã sử dụng)” trong giai đoạn đào tạo. |
| Room Block | Khối phòng đoàn | Lượng hạng phòng dành cho đoàn theo ngày/cut-off. Không phải khóa một phòng kỹ thuật. | Code giữ `RoomBlock`; UI “Khối phòng đoàn”. |
| Allotment | Hạn ngạch phòng | Số lượng phòng được dành cho đoàn/đối tác theo từng ngày và điều kiện release. Khác Room Block là hồ sơ cam kết cụ thể. | Có thể giữ “Allotment” trong màn hình distribution chuyên sâu; phải có tooltip Việt. |
| No Show | Không đến | Reservation đã qua cutoff nhưng khách không đến và được xử lý theo policy. Không phải cancellation tự nguyện. | Code giữ `NoShow`; UI “Không đến (No-show)” khi cần nhận diện ngành. |
| Walk In | Khách đến không đặt trước | Khách xuất hiện tại quầy và bắt đầu quy trình giá, guarantee, Stay mà không có Reservation đã xác nhận trước đó. | Code giữ `WalkIn`; UI “Khách không đặt trước”. |
| Rate Plan | Gói giá | Cấu trúc thương mại gồm điều kiện ở, inclusions, cancellation/guarantee và cách tính giá; không chỉ là một con số. | Code giữ `RatePlan`; UI “Gói giá”. |
| Rate Code | Mã gói giá | Mã hiển thị/tra cứu của Rate Plan. Không dùng thay identity kỹ thuật. | Giữ `RateCode` trong code; UI “Mã gói giá”. |
| Rate Quote | Báo giá lưu trú | Snapshot có version, thời hạn, breakdown và điều kiện được khách chấp nhận; không phải Rate Plan master. | Code giữ `RateQuote`; UI “Báo giá lưu trú”. |
| House Use | Phòng sử dụng nội bộ | Phòng được khách sạn sử dụng cho mục đích nội bộ, không tạo doanh thu khách theo cách thông thường nhưng vẫn ảnh hưởng inventory/occupancy. | Code giữ `HouseUse`; UI tiếng Việt. |
| Out Of Order | Ngưng khai thác | Block nghiêm trọng, thường do kỹ thuật/safety, loại phòng khỏi sellable inventory và cần quy trình release chặt. | Bắt buộc giữ mã `OOO`; UI “Ngưng khai thác (OOO)”. |
| Out Of Service | Tạm ngưng phục vụ | Block nhẹ/ngắn hơn OOO theo Property policy; tác động thống kê có thể khác. Không được dùng hoán đổi với OOO. | Bắt buộc giữ mã `OOS`; UI “Tạm ngưng phục vụ (OOS)”. |
| Guest Ledger | Sổ công nợ khách lưu trú | Tổng hợp số dư/bút toán của guest/in-house folio; thuộc PMS subledger, không phải GL. | Report có thể dùng “Guest Ledger” phụ chú vì là thuật ngữ chuẩn ngành. |
| City Ledger | Sổ công nợ phải thu | Công nợ đã chuyển sang company/agency/AR sau checkout hoặc direct billing; Accounting/AR sở hữu. | Nên hiển thị “Công nợ phải thu (City Ledger)” khi đào tạo. |
| Master Folio | Sổ giao dịch tổng | Folio đầu mối nhận charge của đoàn/company theo routing; không phải General Ledger master record. | Code có thể dùng `MasterFolio` như role/read concept, không tạo Aggregate mới nếu Domain Model không có. |
| Window | Ngăn sổ giao dịch | Phân vùng logic trong Folio để nhóm charge theo payer/settlement/invoice; không phải cửa sổ UI. | Code dùng `FolioWindow`, không dùng `Window` đơn lẻ. |
| Guest Balance | Số dư khách phải thanh toán | Net obligation của guest tại một thời điểm/sequence sau debit, credit và settlement; phải kèm currency và as-of. | Code dùng `FolioBalance`; UI có thể dùng “Số dư khách”. |
| Comp | Miễn phí có phê duyệt | Complimentary service/room/charge theo thẩm quyền, khác discount theo chương trình và allowance khắc phục dịch vụ. | Không dùng `Comp` làm canonical type; chỉ là viết tắt UI được kiểm soát. |
| Package | Gói dịch vụ | Tập quyền hưởng/charge/inclusion bán cùng room/event. Phải nêu domain: Rate Package, Banquet Menu Package, Service Package. | Cấm `Package` đơn lẻ trong code; dùng tên đầy đủ. |
| Routing Rule | Quy tắc định tuyến chi phí | Điều kiện ưu tiên xác định payer/Folio Window đích cho charge category/source trong khoảng hiệu lực. | Canonical Aggregate là `RoutingInstruction`; `RoutingRule` là cách gọi nghiệp vụ, không model song song. |
| Charge Code | Mã khoản tính phí | Mã danh mục xác định loại doanh thu, thuế, accounting mapping và hành vi posting. Không phải Posting Master record. | Code dùng `ChargeCode` hoặc catalog reference; UI “Mã khoản tính phí”. |
| Posting Master | Danh mục khoản ghi sổ | Danh mục legacy thường trộn charge code, giá, thuế và GL mapping. Model mới nên tách semantics/catalog owner thay vì sao chép “master”. | Không dùng làm canonical Aggregate; chỉ giữ trong migration/anti-corruption glossary. |
| Guarantee | Bảo đảm đặt phòng | Cam kết bảo đảm reservation bằng deposit, card authorization, company credit hoặc phương thức khác. Không đồng nghĩa đặt cọc. | Code giữ `Guarantee`; UI “Bảo đảm”. |
| Deposit | Đặt cọc | Giá trị nhận/authorize trước, còn unallocated balance và có thể allocate/refund/forfeit. Nó có thể là một dạng guarantee nhưng không phải mọi guarantee. | Code giữ `Deposit`; UI “Đặt cọc”. |
| Direct Billing | Thanh toán công nợ | Chuyển nghĩa vụ sang AR/company theo credit approval; không phải cho phép khách nợ tùy ý. | Code giữ `DirectBilling`; UI “Thanh toán công nợ”. |
| Housekeeping Status | Trạng thái vận hành phòng | Dirty/Cleaning/Clean/Inspected/Ready/DND do Housekeeping sở hữu. Không trộn occupancy hoặc availability status. | Code canonical `RoomOperationalStatus`. |
| Room Status | Trạng thái phòng tổng hợp | Read Model kết hợp occupancy, housekeeping, maintenance và sellability để vận hành; không phải một cột owner chung. | Giữ `RoomStatus` chỉ cho projection/UI. |
| Room Move | Chuyển phòng | Kết thúc assignment cũ và kích hoạt assignment mới với key/HK/folio handoff. Không phải sửa `MaPhong` trên Stay. | Code giữ `RoomMove` cho use case; Root vẫn `RoomAssignment`. |
| Upgrade | Nâng hạng | Chuyển sang sản phẩm/hạng có giá trị cao hơn, có thể trả phí hoặc miễn phí. Không phải mọi chuyển phòng. | Giữ `Upgrade` trong code ngành; UI “Nâng hạng”. |
| Cut-Off Date | Ngày chốt giữ phòng | Ngày/instant sau đó allotment chưa pickup được release hoặc xử lý theo hợp đồng. Không phải cancellation cutoff của cá nhân. | Code dùng `CutOffDate` trong Group context; UI “Ngày chốt giữ phòng”. |
| Wash | Điều chỉnh giảm khối phòng dự kiến | Revenue/group operation giảm forecast/block theo ước tính số phòng sẽ không pickup, có approval và history. Không phải hủy từng Reservation. | Nếu áp dụng, code dùng `BlockWash`; UI “Điều chỉnh giảm khối phòng”. |
| Shoulder Date | Ngày liền kề kỳ chính | Ngày trước/sau event/group peak được xem xét mở rộng block/rate. | Có thể giữ “Shoulder date” phụ chú cho sales; canonical UI “Ngày liền kề”. |
| Turnaway | Nhu cầu bị từ chối | Nhu cầu reservation không được nhận do thiếu tồn/giá/policy; dùng phân tích lost business, không đồng nghĩa waitlist. | Code dùng `Turnaway` nếu tạo capability sau; chưa mặc định là Aggregate. |
| Skipper | Khách rời đi chưa thanh toán | Tình huống rủi ro khách rời Property khi nghĩa vụ chưa được xử lý; cần incident/accounting follow-up, không chỉ là balance. | Giữ “Skipper” trong đào tạo nếu cần; UI chính “Rời đi chưa thanh toán”. |

### 12.1. Các cặp tuyệt đối không dùng thay nhau

- Reservation ≠ Stay; Room ≠ Room Type; Room Assignment ≠ Room Availability.
- Room Block ≠ Room Operational Block; OOO ≠ OOS.
- Folio ≠ Invoice ≠ Journal; Payment ≠ Deposit ≠ Guarantee; Void ≠ Reversal ≠ Refund.
- Charge ≠ Posting; Routing Instruction ≠ Transfer; Guest Ledger ≠ City Ledger ≠ General Ledger.
- Business Date ≠ Calendar Date ≠ Accounting Posting Date.
- Service Request ≠ Operational Task ≠ Notification.
- Guest Profile ≠ Stay Guest snapshot; Company Profile ≠ payer approval.

---

## 13. Danh mục viết tắt

Viết tắt phục vụ hiển thị hẹp, log, rule code và trao đổi vận hành; không thay canonical class/entity name.

| Viết tắt | Thuật ngữ đầy đủ | Tiếng Việt/phạm vi |
|---|---|---|
| ASM | Asmile-BO | Hệ thống ERP Hospitality |
| FO | Front Office | Tiền sảnh |
| FD | Front Desk | Quầy lễ tân |
| FOM | Front Office Manager | Quản lý tiền sảnh |
| FOS | Front Office Supervisor | Giám sát tiền sảnh |
| RES | Reservation | Đặt phòng |
| STY | Stay | Lưu trú |
| ARR | Arrival | Khách đến |
| DEP | Departure | Khách đi; không dùng cho Deposit |
| WLK | Walk-In | Khách không đặt trước |
| NS | No Show | Không đến |
| RA | Room Assignment | Phân phòng; trong security context có thể xung đột Role Assignment nên phải nêu scope |
| RM | Room | Phòng |
| RT | Room Type | Hạng phòng |
| RBL | Room Block | Khối phòng đoàn |
| ROB | Room Operational Block | Khóa vận hành phòng |
| OOO | Out Of Order | Ngưng khai thác |
| OOS | Out Of Service | Tạm ngưng phục vụ |
| HK | Housekeeping | Buồng phòng |
| HKT | Housekeeping Task | Công việc buồng phòng |
| MT | Maintenance | Bảo trì |
| FOL | Folio | Sổ giao dịch khách |
| FLW | Folio Window | Ngăn sổ giao dịch |
| PST | Posting | Ghi sổ |
| PAY | Payment | Thanh toán |
| DPT | Deposit | Đặt cọc; dùng DPT để tránh xung đột Departure |
| RFD | Refund | Hoàn tiền |
| CSH | Cashiering | Thu ngân |
| NA | Night Audit | Kiểm toán đêm |
| BD | Business Date | Ngày nghiệp vụ |
| GRP | Group Stay | Lưu trú đoàn |
| RML | Rooming List | Danh sách khách đoàn |
| GS | Guest Services | Dịch vụ khách |
| WUC | Wake-Up Call | Cuộc gọi báo thức |
| LNF | Lost & Found | Đồ thất lạc |
| CRM | Customer Relationship Management | Quản trị quan hệ khách hàng |
| POS | Point Of Sale | Điểm bán hàng |
| INV | Inventory | Kho |
| PUR | Purchasing | Mua hàng |
| ACC | Accounting | Kế toán |
| AR | Accounts Receivable | Công nợ phải thu |
| GL | General Ledger | Sổ cái |
| BQT | Banquet | Tiệc |
| NTF | Notification | Thông báo |
| AI | Artificial Intelligence | Trí tuệ nhân tạo |
| RPT | Reporting | Báo cáo |
| INT | Integration | Tích hợp |
| API | Application Programming Interface | Giao diện lập trình ứng dụng |
| VO | Value Object | Đối tượng giá trị |
| ARoot | Aggregate Root | Chỉ dùng trong tài liệu kỹ thuật, không dùng tên type |
| RMV | Read Model/View | Mô hình đọc; tránh dùng RM vì xung đột Room |
| OTB | Outbox | Hộp sự kiện đi |
| INB | Inbox | Hộp sự kiện đến |
| IDP | Idempotency | Chống xử lý trùng |
| SLA | Service Level Agreement | Cam kết mức dịch vụ |
| VIP | Very Important Person | Khách quan trọng |
| DND | Do Not Disturb | Không làm phiền |
| CTA | Close To Arrival | Đóng nhận ngày đến |
| CTD | Close To Departure | Đóng nhận ngày đi |
| MLOS | Minimum Length Of Stay | Số đêm tối thiểu |
| LOS | Length Of Stay | Thời lượng lưu trú |

---

## 14. Quy tắc chung và quản trị từ điển

### 14.1. Một khái niệm, một tên

| Tên chuẩn | Tên bị cấm nếu cùng nghĩa | Cách xử lý |
|---|---|---|
| Reservation | Booking, BookingInfo, BookingHeader, ReservationData | Đổi về Reservation; Booking chỉ là synonym giao tiếp |
| Stay | OccupancyRecord, CheckInInfo, GuestStayHeader | Dùng Stay; check-in là action |
| GuestProfile | CustomerInfo, GuestMaster, ProfileData | CRM dùng GuestProfile |
| RoomAssignment | AssignedRoom, RoomAllocationRecord | Dùng RoomAssignment; Allocation dành availability |
| Folio | Bill, GuestBill, FolioHeader | Bill chỉ là bản trình bày; dùng Folio |
| Payment | ReceiptInfo, TenderTransaction | Dùng Payment; receipt là chứng từ |
| Deposit | AdvancePayment, PrepaidInfo nếu cùng lifecycle | Dùng Deposit; nếu accounting semantics khác phải định nghĩa riêng |
| RefundRequest | RefundInfo, RefundHeader | Dùng RefundRequest |
| HousekeepingTask | CleaningJob, MaidTask | Dùng HousekeepingTask |
| MaintenanceCase | RepairTicket, MaintenanceIssueInfo | Dùng MaintenanceCase |
| OperationalTask | Todo, WorkItem, TaskInfo | Dùng OperationalTask trong Task Center |
| BusinessDate | WorkingDate, HotelDate, SystemDate | Dùng BusinessDate |
| Property | Hotel, Branch, Site nếu cùng boundary | Dùng Property; Hotel chỉ UI brand nếu cần |

### 14.2. Quy trình thêm hoặc đổi thuật ngữ

1. Người đề xuất nêu definition, owner, phạm vi, loại model, tên đang có và lý do không dùng canonical term hiện tại.
2. Domain Owner kiểm tra trùng nghĩa và ảnh hưởng Bounded Context; Data/Architecture Owner kiểm tra database/API/report.
3. Nếu là thay đổi semantics, phải quay lại Domain Model decision; không sửa từ điển để lách Aggregate boundary.
4. Tên được duyệt phải cập nhật glossary, mapping, API contract, report catalog, AI prompt vocabulary và migration plan thuật ngữ.
5. Alias cũ chỉ tồn tại trong anti-corruption/migration window, có owner và ngày hết hạn.
6. CI/review checklist về sau phải phát hiện tên cấm, nhưng tài liệu này không triển khai rule hay code.

### 14.3. Quyền quyết định tên

- Domain Owner quyết định meaning và canonical English term.
- Product/Operations quyết định Vietnamese UI label, không được đổi meaning.
- Data Architect quyết định ánh xạ Database theo quy tắc tiếng Việt không dấu.
- API Owner quyết định resource/contract name dựa trên canonical term.
- Reporting Owner quản lý report technical key/title nhưng không tạo metric synonym.
- AI Owner dùng đúng vocabulary, synonym chỉ để hiểu input; output phải trả canonical term.
- Architecture Board phê duyệt exception xuyên module.

### 14.4. Quality gate bắt buộc

- Mọi concept mới có owner và định nghĩa phân biệt với concept gần nghĩa.
- Aggregate/Entity/VO/Event/ReadModel/Table/Enum/Lookup được phân loại, không suy đoán từ hậu tố.
- Tên Database, code, UI, report, API ánh xạ được về một dòng canonical.
- Không có table/API mới chỉ vì alias; không dùng shared table để né ownership.
- Event ở thì quá khứ; command là động từ; query thể hiện ý định đọc.
- Enum chỉ chứa tập ổn định; danh mục thay đổi theo Property phải là Lookup.
- PII, tài chính và audit dùng tên chính xác, không tên chung làm che mục đích.
- Từ viết tắt không gây xung đột trong cùng scope và không xuất hiện trong public type name nếu không phổ quát.

---

## 15. Kết luận chuẩn hóa

Từ điển này chốt 500 thuật ngữ thuộc 21 nhóm nghiệp vụ, danh mục tên cho toàn bộ Aggregate Front Office, quy tắc Database–Entity–ViewModel–Service–Repository–Event–API–File, bảng ánh xạ xuyên lớp, quyết định dịch thuật ngữ khó và cơ chế quản trị thay đổi.

Mọi bước thiết kế Database tiếp theo phải dùng bảng ánh xạ làm điểm xuất phát, nhưng vẫn phải tôn trọng Aggregate boundary, transaction boundary, owner và invariant trong Domain Model. Tên table không được dùng để làm thay đổi domain; tên code không được dùng để áp đặt tiếng Anh lên UI; AI và Reporting không được tạo synonym mới ngoài governance.

SẴN SÀNG THIẾT KẾ DATABASE FRONT OFFICE.
