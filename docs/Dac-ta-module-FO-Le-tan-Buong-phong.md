# ĐẶC TẢ MODULE FO - LỄ TÂN VÀ BUỒNG PHÒNG

**Dự án:** Asmile-BO  
**Phạm vi:** Front Office, lễ tân, đặt phòng, lưu trú, thu ngân, buồng phòng, Night Audit và các nghiệp vụ liên quan  
**Mục tiêu:** Xây dựng một module PMS khách sạn đầy đủ, vận hành thực tế, có khả năng tích hợp với POS, kế toán, CRM, khóa cửa, hóa đơn điện tử và trợ lý AI.

---

## 1. Định hướng tổng thể

Module FO không chỉ là màn hình nhận phòng và trả phòng. Đây phải là trung tâm điều phối toàn bộ vòng đời lưu trú:

**Đặt phòng → xếp phòng → check-in → lưu trú → phát sinh dịch vụ → buồng phòng → bảo trì → thanh toán → check-out → Night Audit → báo cáo.**

Ba nguyên tắc kiến trúc quan trọng:

1. Tách trạng thái đặt phòng, lưu trú, vệ sinh và khai thác phòng.
2. Folio phải là sổ giao dịch bất biến, không xóa giao dịch tài chính đã phát sinh.
3. Night Audit phải có checkpoint, chạy lại an toàn và không đăng trùng tiền phòng.

---

## 2. Cấu trúc menu tổng thể

Module FO nên có các phân hệ:

1. Tổng quan lễ tân
2. Sơ đồ phòng
3. Lịch phòng
4. Đặt phòng
5. Khách đến
6. Khách đang lưu trú
7. Khách đi
8. Thu ngân và folio
9. Buồng phòng
10. Yêu cầu dịch vụ và sự cố
11. Night Audit
12. Báo cáo FO
13. Hồ sơ khách hàng
14. Đoàn, công ty và đại lý
15. Giá phòng và chính sách
16. Cấu hình khách sạn
17. Nhật ký thao tác

---

## 3. Tổng quan lễ tân

### 3.1. KPI đầu trang

- Tổng số phòng
- Phòng có thể bán
- Phòng đang có khách
- Phòng trống sạch
- Phòng trống bẩn
- Phòng đang dọn
- Phòng chờ kiểm tra
- Phòng Out of Order
- Phòng Out of Service
- Khách đến hôm nay
- Khách đi hôm nay
- Khách chưa xếp phòng
- Khách đến sớm
- Khách trả muộn
- No-show dự kiến
- Công suất phòng
- Doanh thu phòng hôm nay
- Công nợ khách đang ở
- Tiền đặt cọc đang giữ

### 3.2. Các nhóm trạng thái phải tách riêng

**Trạng thái lưu trú:** Trống, đã đặt, đang ở, sắp trả, đã trả.  
**Trạng thái vệ sinh:** Bẩn, đã giao việc, đang dọn, sạch, đã kiểm tra, không đạt.  
**Trạng thái khai thác:** Bình thường, khóa phòng, Out of Service, Out of Order.  
**Trạng thái dịch vụ:** DND, yêu cầu dọn phòng, yêu cầu turndown.

Một phòng có thể đồng thời là:

- Trống + Bẩn + Đang khai thác
- Có khách + Sạch + Đang khai thác
- Trống + Sạch + Out of Order

### 3.3. Khu vực “Cần xử lý ngay”

- Khách đến nhưng chưa có phòng sạch
- Khách sắp đến chưa xếp phòng
- Phòng khách vừa trả chưa giao buồng phòng
- Phòng đã dọn nhưng chưa kiểm tra
- Khách quá giờ trả phòng
- Booking chưa đủ đặt cọc
- Folio có số dư bất thường
- Thanh toán thất bại
- Phòng có yêu cầu sửa chữa
- Khách VIP sắp đến
- Sinh nhật khách đang lưu trú
- Khách yêu cầu đánh thức
- Khách chưa khai báo giấy tờ
- Booking nghi ngờ trùng
- Nguy cơ overbooking

Mỗi cảnh báo cần có mức độ, người phụ trách, hạn xử lý, trạng thái và nút hành động nhanh.

---

## 4. Sơ đồ phòng - Room Rack

### 4.1. Chế độ hiển thị

- Theo tầng hoặc tòa nhà
- Theo loại phòng
- Dạng danh sách

### 4.2. Nội dung card phòng

- Số phòng
- Loại phòng
- Trạng thái lưu trú
- Trạng thái vệ sinh
- Tên khách
- Ngày đến và ngày đi
- Số người lớn, trẻ em
- VIP hoặc cảnh báo đặc biệt
- Số dư folio
- Yêu cầu đặc biệt
- DND hoặc Make Up Room
- Phòng nối, phòng liền kề
- Cảnh báo bảo trì

### 4.3. Màu trạng thái đề xuất

- Xanh lá: trống sạch
- Xám: trống bẩn
- Vàng: đang dọn
- Xanh dương: đang có khách
- Tím: đã đặt, chưa đến
- Cam: khách sắp trả
- Đỏ: Out of Order
- Đỏ nhạt: có sự cố
- Viền vàng: VIP

### 4.4. Menu thao tác nhanh

- Tạo đặt phòng
- Xếp phòng
- Check-in
- Check-out
- Chuyển phòng
- Gia hạn lưu trú
- Thêm khách cùng phòng
- Đăng ký khách ghé thăm
- Đăng dịch vụ
- Xem folio
- Thu tiền
- Gửi yêu cầu buồng phòng
- Báo hỏng
- Đặt DND
- Khóa hoặc mở phòng
- In phiếu đăng ký
- Xem lịch sử phòng

---

## 5. Lịch phòng - Tape Chart

Hiển thị dạng Gantt, trong đó hàng ngang là phòng, cột là ngày và thanh booking kéo dài theo kỳ lưu trú.

### Chức năng bắt buộc

- Kéo booking để đổi phòng
- Kéo đầu hoặc cuối để đổi ngày
- Xếp phòng bằng drag-and-drop
- Phát hiện booking chồng
- Hiển thị booking chưa xếp phòng
- Lọc theo loại phòng, tầng, tòa nhà
- Hiển thị giá từng ngày
- Hiển thị phòng khóa bán
- Hiển thị OOO/OOS
- Hiển thị booking đoàn
- Tách và ghép booking
- Upgrade và downgrade
- Đổi phòng giữa kỳ lưu trú
- Giữ phòng tạm thời
- Ghim phòng theo yêu cầu khách

Mọi thao tác kéo thả làm thay đổi ngày, giá hoặc phòng đều phải có xác nhận và lưu audit log.

---

## 6. Quản lý đặt phòng

### 6.1. Thông tin chung

- Mã booking
- Nguồn booking
- Kênh bán
- Loại khách
- Cá nhân, đoàn hoặc công ty
- Ngày đặt
- Ngày đến, ngày đi, số đêm
- Giờ đến và giờ đi dự kiến
- Loại phòng, số lượng phòng
- Số người lớn, trẻ em, trẻ sơ sinh
- Mã giá và gói phòng
- Nhân viên bán
- Người giới thiệu

### 6.2. Thông tin khách

- Họ tên
- Quốc tịch
- Ngày sinh
- Giới tính
- CCCD hoặc hộ chiếu
- Ngày cấp và ngày hết hạn
- Số điện thoại
- Email
- Địa chỉ
- Biển số xe
- Ảnh giấy tờ
- Ảnh chân dung
- Thông tin xuất hóa đơn
- Dị ứng và yêu cầu đặc biệt

### 6.3. Tài chính

- Giá phòng theo từng ngày
- Thuế và phí dịch vụ
- Phụ thu người lớn, trẻ em
- Phụ thu cuối tuần, lễ Tết
- Giường phụ
- Ăn sáng
- Khuyến mại và chiết khấu
- Tiền đặt cọc
- Hình thức đảm bảo booking
- Hạn thanh toán
- Chính sách hủy

### 6.4. State machine booking

- Inquiry
- Tentative
- Option
- Confirmed
- Guaranteed
- Waitlist
- Cancelled
- No-show
- Checked-in
- In-house
- Due-out
- Checked-out

Không cho sửa trực tiếp trạng thái. Mỗi thay đổi phải đi qua một nghiệp vụ và kiểm tra điều kiện hợp lệ.

---

## 7. Quy trình check-in

Thiết kế dạng wizard 5 bước.

### Bước 1. Xác minh booking

Tìm theo tên, số điện thoại, mã booking, mã OTA, CCCD/hộ chiếu, biển số xe hoặc QR.

### Bước 2. Kiểm tra khách

- Đối chiếu giấy tờ
- Quét CCCD/hộ chiếu
- Nhận diện thông tin tự động
- Kiểm tra hạn giấy tờ
- Kiểm tra blacklist
- Phát hiện hồ sơ trùng
- Xác định khách cũ hoặc khách mới
- Chuẩn bị khai báo lưu trú

### Bước 3. Xác nhận phòng

- Phòng đã xếp
- Trạng thái vệ sinh
- Loại giường
- Hướng và tầng
- Hút thuốc hoặc không hút thuốc
- Phòng liền kề
- Phòng hỗ trợ người khuyết tật
- Upgrade hoặc downgrade

Không cho check-in phòng Dirty hoặc OOO, trừ khi người có quyền override và nhập lý do.

### Bước 4. Tài chính

- Xác nhận giá phòng
- Kiểm tra tiền cọc
- Thu thêm đặt cọc
- Tiền mặt, thẻ, chuyển khoản
- Công nợ công ty
- Hạn mức tín dụng
- Routing charge

### Bước 5. Hoàn tất

- Chụp chữ ký
- In phiếu đăng ký
- In phiếu đặt cọc
- Phát thẻ phòng
- Gán mã khóa
- Gửi Wi-Fi và tin nhắn chào mừng
- Đăng ký biển số xe
- Gửi yêu cầu hành lý
- Thông báo buồng phòng

---

## 8. Khách đang lưu trú - In-house

### 8.1. Bộ lọc

- Tất cả khách đang ở
- Đến hôm nay
- Đi hôm nay
- Ở quá hạn
- VIP
- Khách đoàn
- Khách công ty
- Còn công nợ
- DND
- Có yêu cầu đang xử lý
- Sinh nhật
- Khách nước ngoài
- Phòng có trẻ em
- Phòng có giường phụ

### 8.2. Thao tác

- Xem hồ sơ khách
- Thêm khách cùng phòng
- Đổi người đại diện
- Gia hạn hoặc rút ngắn kỳ ở
- Chuyển phòng
- Đổi loại phòng
- Thay đổi giá có kiểm soát
- Tách hoặc gộp folio
- Chuyển chi phí
- Đăng dịch vụ
- Thu tiền, hoàn tiền
- Điều chỉnh giao dịch
- Gửi tin nhắn
- Ghi chú bàn giao ca
- Đặt wake-up call
- Đặt DND
- Yêu cầu dọn phòng
- Báo mất đồ
- Đăng ký khách ghé thăm
- Đăng ký xe

---

## 9. Chuyển phòng

### Quy trình

1. Chọn phòng hiện tại
2. Chọn lý do chuyển
3. Chọn phòng mới
4. Kiểm tra trạng thái phòng mới
5. Xác định thời điểm chuyển
6. Xác định giá sau khi chuyển
7. Chuyển hoặc giữ folio
8. Gửi nhiệm vụ hành lý
9. Đánh dấu phòng cũ Dirty
10. Cập nhật khóa cửa
11. Lưu lịch sử

### Lý do mẫu

- Khách yêu cầu
- Điều hòa hỏng
- Tiếng ồn
- Nâng hoặc hạ hạng
- Sai loại giường
- Phòng có mùi
- Sự cố kỹ thuật
- Phục vụ VIP
- Điều phối công suất

Audit log phải lưu người thao tác, thời gian, phòng cũ, phòng mới, giá cũ, giá mới, lý do và người phê duyệt.

---

## 10. Folio và thu ngân

### 10.1. Cấu trúc folio

Một booking có thể có nhiều folio:

- Folio khách cá nhân
- Folio công ty
- Folio đại lý
- Folio trưởng đoàn
- Folio phụ
- Folio incidentals
- Master folio đoàn

### 10.2. Nghiệp vụ

- Đăng tiền phòng
- Minibar
- Nhà hàng
- Giặt là
- Spa
- Hội nghị
- Đưa đón
- Phụ thu
- Bồi thường
- Điều chỉnh
- Giảm giá
- Void
- Hoàn tiền
- Chuyển giao dịch
- Tách hoặc gộp hóa đơn
- Chuyển sang phòng khác
- Chuyển sang master folio
- Thu tiền nhiều phương thức

### 10.3. Routing charge

Cho phép định tuyến theo nhóm dịch vụ, mã dịch vụ, ngày phát sinh, phòng, khách, công ty và hạn mức.

Ví dụ:

- Công ty trả tiền phòng và ăn sáng
- Khách trả minibar và giặt là
- Trưởng đoàn trả tiền ăn
- Đại lý trả giá net
- Khách trả phần upgrade

### 10.4. Kiểm soát gian lận

Các nghiệp vụ nhạy cảm phải có quyền hoặc phê duyệt:

- Sửa giá sau check-in
- Void hoặc hoàn tiền
- Giảm giá
- Đổi ngày giao dịch
- Check-out khi còn số dư
- Mở lại folio đã đóng
- Backdate
- Chuyển công nợ
- Điều chỉnh thuế

Không xóa giao dịch thực tế; chỉ tạo giao dịch đảo hoặc điều chỉnh.

---

## 11. Check-out

### 11.1. Checklist trước khi trả phòng

- Minibar đã cập nhật
- Buồng phòng đã kiểm tra
- Mất mát hoặc hư hỏng
- Giặt là chưa trả
- Chìa khóa hoặc thẻ phòng
- Bill nhà hàng chưa chuyển
- Dịch vụ chưa ghi nhận
- Công nợ còn lại
- Tiền cọc và hoàn cọc
- Hóa đơn VAT
- Thông tin người nhận hóa đơn
- Phương thức thanh toán
- Khảo sát hài lòng

### 11.2. Các hình thức check-out

- Bình thường
- Quick check-out
- Express check-out
- Early departure
- Late check-out
- Check-out một phần
- Check-out một phòng trong booking nhiều phòng
- Check-out đoàn
- Check-out giữ công nợ
- Balance transfer

### 11.3. Sau check-out

- Phòng chuyển Dirty
- Sinh nhiệm vụ dọn phòng
- Thu hồi quyền khóa cửa
- Lưu lịch sử lưu trú
- Gửi hóa đơn và khảo sát
- Cập nhật điểm thành viên
- Cập nhật công nợ
- Cho phép ghi nhận đồ thất lạc

---

## 12. Module buồng phòng

### 12.1. Housekeeping Board

Hiển thị:

- Số phòng, tầng, loại phòng
- Front Desk Status
- Housekeeping Status
- Khách đang ở, khách đến, khách đi
- Giờ đến và giờ đi dự kiến
- VIP, DND, Make Up Room
- Số người, trẻ em, giường phụ
- Ngày dọn gần nhất
- Nhân viên phụ trách
- Mức độ ưu tiên
- Thời gian bắt đầu và hoàn thành
- Kết quả kiểm tra

### 12.2. Trạng thái buồng phòng

- Dirty
- Assigned
- Cleaning
- Paused
- Clean
- Inspected
- Rejected
- Pickup
- Turndown required
- Turndown completed
- DND
- Refused service

### 12.3. Loại nhiệm vụ

- Dọn phòng khách trả
- Dọn phòng khách đang ở
- Dọn nhanh
- Dọn VIP
- Turndown
- Bổ sung amenities
- Thay ga
- Thêm giường hoặc nôi
- Kiểm minibar
- Kiểm tra trước check-in
- Kiểm tra sau check-out
- Vệ sinh sâu
- Khử mùi
- Xử lý côn trùng
- Dọn khu vực công cộng

### 12.4. Phân công

- Thủ công
- Theo tầng hoặc khu vực
- Theo ca
- Theo năng lực
- Theo số điểm công việc
- Tự động cân bằng workload

Điểm gợi ý:

- Phòng stayover: 1 điểm
- Phòng checkout: 2 điểm
- Suite: 3 điểm
- VIP: cộng 1 điểm
- Extra bed: cộng 0,5 điểm

### 12.5. Ứng dụng cho nhân viên buồng

- Danh sách phòng được giao
- Thứ tự ưu tiên
- Thông tin khách cần thiết
- DND và yêu cầu đặc biệt
- Giờ check-in dự kiến
- Checklist dọn phòng
- Bắt đầu, tạm dừng, hoàn thành
- Chụp ảnh sự cố
- Báo minibar
- Báo Lost & Found
- Báo hỏng thiết bị
- Yêu cầu vật tư
- Xác nhận linen
- Ghi chú hoặc ghi âm

Không hiển thị dữ liệu tài chính và giấy tờ cá nhân không cần thiết.

### 12.6. Kiểm tra phòng

Supervisor mở danh sách chờ kiểm tra, thực hiện checklist, chụp ảnh, đánh dấu đạt hoặc không đạt. Phòng không đạt được trả lại cho nhân viên xử lý.

Cho phép cấu hình kiểm tra 100%, chỉ phòng VIP, chỉ phòng khách đến hoặc kiểm tra ngẫu nhiên.

---

## 13. Out of Order và Out of Service

### Out of Service - OOS

Dùng cho bảo trì nhỏ, vệ sinh sâu, giữ nội bộ hoặc chờ vật tư. Có thể cấu hình cách ảnh hưởng đến inventory.

### Out of Order - OOO

Dùng khi phòng không thể bán do hỏng điều hòa, rò nước, sửa chữa lớn, mất điện hoặc không bảo đảm an toàn.

Phiếu OOO/OOS phải có:

- Ngày bắt đầu và dự kiến kết thúc
- Lý do
- Người yêu cầu và người duyệt
- Bộ phận xử lý
- Chi phí dự kiến
- Ảnh trước và sau
- Biên bản hoàn thành

---

## 14. Yêu cầu dịch vụ và sự cố

Xây dựng một trung tâm ticket dùng chung cho lễ tân, buồng phòng, kỹ thuật, an ninh, nhà hàng, bell và chăm sóc khách hàng.

### Dữ liệu ticket

- Mã yêu cầu
- Phòng và khách
- Loại yêu cầu
- Nội dung
- Mức ưu tiên
- Người tiếp nhận
- Bộ phận phụ trách
- SLA
- Trạng thái
- Thời gian phản hồi và hoàn thành
- Hình ảnh
- Chi phí
- Kết quả
- Đánh giá khách

### Trạng thái

- Mới
- Đã tiếp nhận
- Đang xử lý
- Chờ vật tư
- Chờ khách
- Hoàn thành
- Khách xác nhận
- Đóng
- Mở lại

---

## 15. Wake-up Call

- Phòng và tên khách
- Thời gian gọi
- Một lần hoặc lặp lại
- Ngôn ngữ
- Gọi tự động hoặc nhân viên gọi
- Đã gọi, khách trả lời, không trả lời
- Gọi lại và escalation
- Nhật ký kết quả

---

## 16. Lost & Found

- Mã hồ sơ
- Ngày giờ phát hiện
- Phòng hoặc khu vực
- Người phát hiện
- Mô tả và hình ảnh
- Mức độ giá trị
- Nơi lưu giữ
- Khách liên quan
- Trạng thái
- Ngày hết hạn lưu giữ

Trạng thái gồm chưa xác định chủ, đã liên hệ, chờ nhận, đã giao, đã gửi vận chuyển, đã tiêu hủy hoặc chuyển cơ quan chức năng.

Khi bàn giao phải lưu giấy tờ người nhận, chữ ký, ảnh, nhân viên bàn giao và ngày giờ.

---

## 17. Quản lý đoàn

- Tạo block phòng
- Rooming list
- Import Excel
- Phân loại khách
- Xếp phòng tự động
- Ghép khách ở chung
- Trưởng đoàn, hướng dẫn viên
- Giờ đoàn đến và đi
- Xe vận chuyển
- Lịch ăn
- Wake-up call hàng loạt
- Giao chìa khóa hàng loạt
- Check-in và check-out đoàn
- Master folio
- Routing charge
- Danh sách hộ chiếu
- In danh sách phòng
- In phiếu ăn sáng
- Ghi chú đoàn

---

## 18. Night Audit

Night Audit phải là một phân hệ riêng, không chỉ là nút “đóng ngày”.

### 18.1. Pre-audit checklist

- Booking đến nhưng chưa check-in
- Booking chưa xử lý no-show
- Khách đến chưa xếp phòng
- Khách due-out chưa check-out
- Folio âm hoặc bất thường
- Giao dịch chưa cân
- POS chưa đóng ca
- Tiền mặt chưa đối chiếu
- Phòng có khách nhưng trạng thái sai
- Phòng trống nhưng folio còn mở
- Phòng Dirty quá lâu
- Thanh toán chưa hoàn thành
- Booking thiếu giá hoặc giá bằng 0
- Overbooking
- Giao dịch treo
- Công nợ vượt hạn mức
- Phiếu giảm giá chưa duyệt

### 18.2. Quy trình

1. Khóa tạm nghiệp vụ nhạy cảm
2. Chụp snapshot dữ liệu
3. Kiểm tra arrivals
4. Xử lý no-show
5. Kiểm tra departures
6. Đăng room charge
7. Đăng thuế và phí
8. Đăng package
9. Chuyển fixed charges
10. Đối chiếu POS
11. Đối chiếu thanh toán
12. Đối chiếu tiền mặt
13. Kiểm tra ledger
14. Cập nhật trạng thái phòng
15. Tạo báo cáo ngày
16. Chuyển business date
17. Mở ngày mới

### 18.3. Cơ chế an toàn

- Không chạy hai Night Audit đồng thời
- Checkpoint từng bước
- Lỗi bước nào dừng bước đó
- Cho phép chạy lại bước lỗi
- Không tạo trùng room charge
- Dùng idempotency key
- Lưu đầy đủ audit log
- Có chế độ simulation
- Có báo cáo trước và sau audit
- Rollback chỉ dành cho quản lý có quyền

---

## 19. Hồ sơ khách hàng 360 độ

Một khách chỉ có một hồ sơ dùng xuyên suốt nhiều lần đặt phòng.

### Thông tin tổng hợp

- Thông tin cá nhân
- Lịch sử lưu trú
- Tổng số đêm và tổng doanh thu
- ADR trung bình
- Hạng thành viên và điểm
- Sở thích phòng, giường
- Thói quen ăn sáng
- Dị ứng
- Yêu cầu đặc biệt
- Khiếu nại cũ
- Dịch vụ thường dùng
- No-show và hủy phòng
- Lịch sử công nợ
- Công ty liên quan
- Người đi cùng thường xuyên

### Cảnh báo

- VIP
- Blacklist
- Nợ xấu
- Từng làm hỏng tài sản
- Chăm sóc đặc biệt
- Yêu cầu bảo mật
- Người nổi tiếng
- Khách quay lại nhiều lần

Dữ liệu cá nhân phải được phân quyền và ghi nhật ký truy cập.

---

## 20. Báo cáo

### 20.1. Báo cáo vận hành

- Arrivals
- Departures
- In-house
- Room Status
- Housekeeping Status
- Room Assignment
- Room Move
- Early Arrival
- Late Departure
- No-show
- Cancellation
- VIP Arrival
- Guest Birthday
- Stayover
- OOO/OOS
- DND
- Wake-up Call
- Lost & Found

### 20.2. Báo cáo tài chính

- Room Revenue
- Revenue theo market, source, channel
- Cashier Shift
- Payment Summary
- Deposit Ledger
- Guest Ledger
- City Ledger
- Trial Balance
- Adjustment
- Void
- Discount
- Refund
- Tax
- Package Revenue
- Outstanding Balance

### 20.3. KPI

- Occupancy
- ADR
- RevPAR
- Average Length of Stay
- Cancellation Rate
- No-show Rate
- Upgrade Rate
- Early Check-in Revenue
- Late Check-out Revenue
- Room Turnaround Time
- Housekeeping Productivity
- Inspection Rejection Rate
- Maintenance Downtime
- Guest Request SLA

---

## 21. Phân quyền

### Lễ tân

Đặt phòng, check-in, gia hạn, chuyển phòng, đăng dịch vụ và thu tiền trong giới hạn.

### Thu ngân

Thanh toán, tách/gộp folio, hoàn tiền và đóng ca.

### Trưởng ca FO

Override giá, check-in phòng chưa inspected, duyệt late check-out, discount và no-show.

### Nhân viên buồng

Xem nhiệm vụ, cập nhật tiến độ, báo minibar và báo hỏng.

### Giám sát buồng

Phân công, kiểm tra phòng, duyệt Inspected và mở lại nhiệm vụ.

### Night Auditor

Chạy audit, xử lý cảnh báo, in báo cáo và chuyển ngày.

### Quản lý

Override, mở folio đã đóng, điều chỉnh giao dịch, xem báo cáo tài chính và duyệt rollback.

---

## 22. Tích hợp với các module Asmile-BO

### POS và nhà hàng

- Chuyển bill về phòng
- Kiểm tra quyền ký phòng
- Kiểm tra hạn mức
- Đồng bộ void và discount
- Đối chiếu cuối ngày

### Tiệc

- Khách đoàn
- Phòng nghỉ kèm sự kiện
- Master folio
- Dịch vụ đưa đón
- Lịch ăn

### Kho

- Minibar
- Amenities
- Linen
- Vật tư buồng
- Hàng bồi thường

### Kế toán

- Tiền mặt và ngân hàng
- Công nợ
- Hóa đơn điện tử
- Doanh thu và thuế
- Deposit
- Guest Ledger
- City Ledger

### CRM

- Hồ sơ khách
- Lịch sử lưu trú
- Chăm sóc sau lưu trú
- Loyalty
- Chiến dịch khách quay lại

### Kỹ thuật

- Ticket sửa chữa
- OOO/OOS
- Bảo trì định kỳ
- Chi phí sửa chữa
- Thời gian downtime

### Khóa cửa

- Phát, gia hạn và hủy thẻ
- Đọc log mở cửa
- Khóa master
- Mobile key

---

## 23. Ứng dụng AI

AI đóng vai trò trợ lý và đề xuất, không tự ý thay đổi tiền hoặc trạng thái nghiệp vụ.

### AI cho lễ tân

- Tìm booking bằng ngôn ngữ tự nhiên
- Tóm tắt hồ sơ khách
- Cảnh báo yêu cầu đặc biệt
- Gợi ý phòng và upgrade
- Soạn tin nhắn chào mừng
- Dịch hội thoại
- Quét giấy tờ
- Gợi ý xử lý khiếu nại
- Tóm tắt bàn giao ca

### AI cho buồng phòng

- Ưu tiên phòng khách sắp đến
- Cân bằng workload
- Dự đoán thời gian hoàn thành
- Phát hiện thời gian dọn bất thường
- Phân loại sự cố từ ảnh
- Tạo ticket kỹ thuật
- Dự báo linen và amenities

### AI cho Night Audit

- Phân tích lỗi
- Giải thích chênh lệch
- Đề xuất cách xử lý
- Phát hiện bất thường
- Tóm tắt doanh thu ngày

Mọi bút toán và thay đổi tài chính phải do rule engine xác định và được người có quyền xác nhận.

---

## 24. Kiến trúc dữ liệu lõi

Không dùng một trường RoomStatus duy nhất.

```text
RoomInventoryStatus
- Available
- OutOfService
- OutOfOrder
- Blocked

RoomOccupancyStatus
- Vacant
- Reserved
- Occupied
- DueOut

RoomHousekeepingStatus
- Dirty
- Assigned
- Cleaning
- Clean
- Inspected
- Rejected
- Pickup

RoomServiceStatus
- Normal
- DND
- MakeUpRoom
- TurndownRequested
```

### Các bảng chính

```text
Hotel
Building
Floor
RoomType
Room
RoomFeature

GuestProfile
GuestDocument
GuestPreference
GuestAlert

Reservation
ReservationRoom
ReservationGuest
DailyRoomRate
ReservationPackage
ReservationNote
ReservationHistory

Stay
RoomAssignment
RoomMove

Folio
FolioWindow
FolioTransaction
Payment
Deposit
RoutingInstruction

HousekeepingTask
HousekeepingAssignment
RoomInspection
MinibarPosting

ServiceRequest
MaintenanceTicket
LostAndFound

CashierShift
BusinessDate
NightAuditRun
NightAuditStep
AuditLog
```

---

## 25. Thiết kế giao diện

### Thanh điều hướng trái

- Tổng quan
- Sơ đồ phòng
- Lịch phòng
- Đặt phòng
- Khách đến
- Đang lưu trú
- Khách đi
- Thu ngân
- Buồng phòng
- Yêu cầu
- Night Audit
- Báo cáo

### Thanh trên

- Ngày kinh doanh
- Công suất
- Khách đến và đi
- Tìm kiếm toàn cục
- Thông báo
- Ca làm việc
- Thu ngân hiện tại
- Trợ lý AI

### Panel chi tiết bên phải

Khi chọn khách hoặc phòng, mở drawer gồm thông tin nhanh, lưu trú, folio, yêu cầu, ghi chú, lịch sử và nút hành động. Cách này giúp lễ tân không mất ngữ cảnh.

---

## 26. Thứ tự triển khai

### Giai đoạn 1 - Lõi vận hành

- Danh mục phòng
- Sơ đồ phòng
- Lịch phòng
- Đặt phòng
- Xếp phòng
- Check-in
- In-house
- Check-out
- Folio
- Thanh toán
- Trạng thái phòng
- Buồng phòng cơ bản
- Night Audit

### Giai đoạn 2 - Nghiệp vụ nâng cao

- Booking đoàn
- Routing charge
- Room Move
- OOO/OOS
- Lost & Found
- Wake-up Call
- Ticket dịch vụ
- Kiểm tra phòng
- Mobile Housekeeping
- Quản lý ca thu ngân
- Khai báo lưu trú

### Giai đoạn 3 - Tích hợp

- POS
- Khóa cửa
- Hóa đơn điện tử
- Kế toán
- CRM
- Channel Manager
- OTA
- Payment Gateway
- Tổng đài
- Zalo, SMS, email

### Giai đoạn 4 - AI và tự động hóa

- AI concierge cho nhân viên
- AI xếp phòng
- AI dự đoán housekeeping
- AI phân tích Night Audit
- AI upsell
- AI tóm tắt bàn giao ca
- Self Check-in
- Mobile Key

---

## 27. Tiêu chí nghiệm thu cốt lõi

1. Không thể check-in phòng Dirty hoặc OOO nếu không có quyền override.
2. Không thể xếp hai booking chồng thời gian vào cùng phòng.
3. Mọi thay đổi giá, phòng, ngày và giao dịch đều có audit log.
4. Check-out tự động chuyển phòng sang Dirty và sinh nhiệm vụ dọn.
5. Folio không cho xóa bút toán đã phát sinh.
6. Routing charge phân bổ đúng theo người chịu phí.
7. Night Audit không đăng trùng tiền phòng khi chạy lại.
8. Không cho chuyển ngày khi còn lỗi bắt buộc chưa xử lý.
9. Trạng thái lễ tân và buồng phòng đồng bộ thời gian thực.
10. Phân quyền dữ liệu khách và nghiệp vụ tài chính rõ ràng.
11. Mọi tích hợp POS, khóa cửa, hóa đơn có cơ chế retry và đối soát.
12. AI chỉ đề xuất; nghiệp vụ quan trọng cần xác nhận của người dùng.

---

## 28. Kết luận

FO nên được xây dựng như một module điều phối trạng thái, nhiệm vụ và giao dịch xuyên suốt toàn khách sạn. Nền tảng cần ưu tiên tính chính xác nghiệp vụ, khả năng kiểm soát, audit log, xử lý ngoại lệ và tích hợp thời gian thực. Khi state machine, folio và Night Audit được thiết kế đúng ngay từ đầu, các phần buồng phòng, báo cáo, CRM và AI sẽ mở rộng thuận lợi hơn nhiều.
