# Quy trình làm việc của lễ tân

**Phiên bản:** 1.0  
**Ngày rà soát:** 21/09/2026  
**Phạm vi:** Quản lý khách sạn / Front Office trong ASMILE BO.

Tài liệu này hướng dẫn các bước vận hành từ đầu ca, chuẩn bị khách đến, tạo và xử lý đặt phòng, nhận phòng, phục vụ khách đang lưu trú, thanh toán, trả phòng và bàn giao ca. Thao tác cụ thể có thể khác theo quyền của tài khoản và cấu hình từng cơ sở.

## 1. Bắt đầu ca

1. Đăng nhập bằng tài khoản được cấp; chọn đúng cơ sở/chi nhánh Front Office nếu hệ thống chưa tự chọn.
2. Kiểm tra ngày kinh doanh đang chạy và nội dung bàn giao từ ca trước.
3. Mở **Lễ tân** và rà soát:
   - **Sơ đồ phòng vận hành:** phòng trống, đang có khách, bẩn/đang dọn, phòng khóa hoặc ngoài dịch vụ.
   - **Khách đến hôm nay** và **Khách đi hôm nay:** kiểm tra trạng thái đặt phòng/lưu trú, tên khách, số người và ghi chú cần xử lý.
   - **Khách hiện tại:** đối chiếu các lượt đang lưu trú và yêu cầu còn mở.
4. Báo quản lý hoặc nhà buồng nếu trạng thái phòng trên hệ thống không khớp tình trạng thực tế.

> Các nút nghiệp vụ phụ thuộc ngữ cảnh cơ sở và quyền của người dùng. Nếu nút bị ẩn/khóa, báo quản trị phân quyền; không dùng tài khoản khác để ghi thay.

## 2. Tiếp nhận đặt phòng

### Tạo đặt phòng mới

1. Vào **Đặt phòng → Đặt phòng mới**.
2. Hoàn tất các bước trong wizard: thông tin lưu trú, giấy tờ theo chính sách cơ sở, hồ sơ khách, thông tin đặt phòng, tài chính và ghi chú.
3. Kiểm tra tối thiểu:
   - Có tên khách; ngày đi sau ngày đến.
   - Có ít nhất một người lớn; số trẻ em không âm.
   - Loại phòng, nguồn đặt, giá/phương thức thanh toán và thông tin liên hệ đúng yêu cầu của khách sạn.
   - Không tạo trùng hồ sơ khi đã có đặt phòng cùng khách và ngày lưu trú.
4. Xem lại trang xác nhận rồi tạo một lần. Chờ thông báo lưu thành công; sau đó kiểm tra mã đặt phòng và trạng thái trong danh sách.

Luồng tạo đặt phòng hiện gọi thao tác tạo với xác nhận ngay. Đặt phòng cũ ở trạng thái Nháp/Tạm giữ cần được xử lý bằng **Xác nhận đặt phòng**. Sau khi lưu, luôn kiểm tra trạng thái thực tế thay vì suy đoán từ việc wizard đã đóng.

### Sửa, phân phòng, xác nhận hoặc hủy

- **Sửa đặt phòng:** mở **Sửa đặt phòng**, chọn đúng hồ sơ trong danh sách, cập nhật và lưu; kiểm tra lại ngày, khách, số người, loại phòng và ghi chú.
- **Phân phòng:** chọn đặt phòng, gán phòng đúng loại, còn hoạt động và không xung đột ngày lưu trú.
- **Xác nhận đặt phòng:** dùng cho hồ sơ Nháp/Tạm giữ. Kiểm tra khách, ngày, giá và nguồn trước khi xác nhận.
- **Hủy đặt phòng:** chọn đúng hồ sơ và ghi lý do theo quy định cơ sở. Không hủy thay vì sửa khi khách chỉ đổi ngày/phòng.

## 3. Chuẩn bị khách đến và check-in

1. Mở **Khách đến hôm nay** hoặc **Check-in**; lọc/chọn hồ sơ đúng mã đặt phòng và đối chiếu tên khách.
2. Kiểm tra trạng thái đủ điều kiện (thường là **Đã xác nhận** hoặc **Đã bảo đảm**), ngày đến/ngày đi, số người, yêu cầu đặc biệt và tình trạng thanh toán/đặt cọc.
3. Xác minh giấy tờ nhận dạng theo chính sách lưu trú; cập nhật khách chính và khách đi cùng nếu cần.
4. Xác nhận phòng được phân đúng loại, đang hoạt động, sẵn sàng và chưa bị khách khác chiếm dụng. Nếu chưa gán phòng, thực hiện phân phòng trước hoặc theo luồng check-in được cấp.
5. Bấm tiếp tục/xác nhận check-in một lần. Chờ kết quả, rồi kiểm tra:
   - Đặt phòng chuyển sang **Đã check-in**.
   - Lượt lưu trú chuyển sang **Đang lưu trú (In-house)**, có số lưu trú và phòng.
   - Hồ sơ khách, lịch sử, audit và folio liên kết đúng lượt lưu trú.

Check-in bị từ chối nếu thiếu tên khách, loại phòng/phòng hợp lệ, sai trạng thái, ngày lưu trú không hợp lệ, phòng khóa/không sẵn sàng, xung đột đặt phòng hoặc dữ liệu phiên bản đã cũ. Khi gặp lỗi phiên bản, tải lại hồ sơ rồi xác nhận trạng thái trước khi thao tác lại.

## 4. Khách vãng lai (Walk-in)

1. Chọn **Walk-In** khi khách chưa có đặt phòng.
2. Nhập thông tin khách và thời gian lưu trú; chọn loại/phòng đang sẵn sàng, số người và phương thức thanh toán/đặt cọc.
3. Kiểm tra lại giá, giấy tờ theo quy định và thông tin liên hệ trước khi tạo lượt lưu trú.
4. Sau khi hoàn tất, xác minh khách xuất hiện ở **Khách hiện tại**, phòng chuyển sang đang có khách và folio được tạo/liên kết.

Nếu không có phòng phù hợp hoặc tạo lượt thất bại, không tạo lặp. Kiểm tra lại danh sách khách hiện tại và đặt phòng trước khi thử lại.

## 5. Phục vụ khách đang lưu trú

- Dùng **Khách hiện tại/Hồ sơ lưu trú** để tra cứu khách, số phòng, ngày trả và lịch sử.
- Gia hạn ngày ở hoặc đổi phòng qua **Điều chỉnh lưu trú**; kiểm tra khả năng phòng và công nợ trước khi xác nhận.
- Ghi nhận yêu cầu, cuộc gọi đánh thức, lời nhắn, khiếu nại hoặc yêu cầu dịch vụ trên đúng hồ sơ khách/lượt lưu trú.
- Phối hợp nhà buồng qua **Nhắn nhà buồng**; theo dõi việc dọn phòng, kiểm tra phòng và đồ thất lạc.
- Với minibar/tiện nghi, xác nhận số lượng thực tế trước khi ghi nhận chi phí vào folio.
- Khi cấp/thu hồi thẻ khóa, đối chiếu khách, phòng, lượt lưu trú và thời hạn thẻ.

## 6. Thu tiền và đối soát

1. Vào **Thu ngân → Folio/Thu tiền Folio** và chọn đúng khách/lượt lưu trú.
2. Rà soát tiền phòng, dịch vụ, minibar, thuế, giảm giá, tiền đặt cọc và các khoản đã thu.
3. Ghi nhận khoản thu theo đúng phương thức thực nhận; kiểm tra số tiền, tiền tệ, mã tham chiếu và ca thu ngân.
4. Với đặt cọc, hoàn tiền, hủy/void giao dịch hoặc điều chỉnh, phải chọn giao dịch gốc và ghi lý do. Không tạo phiếu bù trừ mới nếu thao tác hoàn/void đã thành công.
5. Đối chiếu số dư folio và trạng thái hóa đơn sau mỗi giao dịch. Nếu màn hình báo lỗi nhưng chưa rõ giao dịch đã lưu hay chưa, tra cứu lịch sử trước khi bấm lại.

## 7. Check-out và bàn giao phòng

1. Mở **Khách đi hôm nay/Check-out**, chọn đúng lượt **Đang lưu trú**.
2. Đối chiếu ngày trả, khách đi cùng, phòng, chi phí phát sinh và tài sản/thẻ cần thu hồi.
3. Hoàn tất thanh toán hoặc xử lý công nợ hợp lệ. Hệ thống chặn check-out nếu folio còn số dư phải thu.
4. Xác nhận check-out một lần; kiểm tra lượt lưu trú thành **Đã trả phòng**, giờ trả và lịch sử được ghi nhận.
5. Kiểm tra phòng chuyển sang trạng thái cần dọn/kiểm tra; báo nhà buồng và theo dõi phòng chỉ trở lại trạng thái sẵn sàng sau khi hoàn tất quy trình buồng.

## 8. Khách đoàn

1. Dùng **Đoàn/Đặt phòng đoàn** để kiểm tra danh sách đoàn, danh sách phòng và người phụ trách.
2. Hoàn thiện rooming list, phân phòng và tiền đặt cọc trước ngày đến.
3. Khi đoàn đến, đối chiếu danh sách khách/phòng rồi dùng **Check-in đoàn**; xử lý lỗi từng phòng/khách trước khi tiếp tục.
4. Trong thời gian ở, ghi nhận dịch vụ và phân bổ khoản thu đúng folio hoặc hướng dẫn thanh toán của đoàn.
5. Dùng **Check-out đoàn** để kiểm tra các lượt, hóa đơn, số dư và yêu cầu housekeeping; chỉ đóng đoàn khi các lượt liên quan đã xử lý xong.

## 9. Cuối ca và Night Audit

1. Hoàn tất các giao dịch thu/chi còn treo; đối chiếu tiền mặt, chuyển khoản và chênh lệch ca.
2. Rà soát khách chưa đến, khách chưa check-in, khách quá ngày trả, folio còn mở và yêu cầu dịch vụ chưa xử lý.
3. Ghi **Nhật ký FO/Bàn giao ca**: sự việc, tiền/chứng từ cần theo dõi, khách VIP/đặc biệt, phòng khóa/bảo trì, việc đang chờ và người nhận bàn giao.
4. Nhân sự được phân quyền mở **Night Audit → Kiểm tra**; xử lý các cảnh báo về phòng, folio, đối soát và dữ liệu trước khi chạy.
5. Chạy các bước theo thứ tự trên màn hình, kiểm tra kết quả từng bước, sau đó mới **Đóng ngày**. Không đóng ngày khi còn lỗi/chênh lệch chưa được giải quyết hoặc chưa có phê duyệt.

## 10. Khi có lỗi hoặc thao tác không rõ kết quả

1. Dừng thao tác lặp; ghi mã đặt phòng/lưu trú/folio và thời điểm xảy ra lỗi.
2. Tải lại danh sách hoặc mở lịch sử/audit để xác định thao tác đã ghi dữ liệu hay chưa.
3. Nếu thông báo liên quan quyền hoặc chi nhánh, kiểm tra cơ sở hiện hành và yêu cầu quản trị cấp quyền.
4. Nếu trạng thái đã thay đổi nhưng màn hình chưa cập nhật, làm mới trước khi thử lại; không tự sửa trực tiếp dữ liệu trong database.
5. Báo quản lý ca với giao dịch tài chính, trạng thái phòng hoặc check-in/out không nhất quán.

## 11. Kiểm tra nghiệm thu theo ca

- [ ] Đúng cơ sở và ngày kinh doanh; bàn giao ca trước đã được đọc.
- [ ] Danh sách khách đến/đi khớp ngày kinh doanh và trạng thái.
- [ ] Đặt phòng mới có mã, khách, ngày, số người và trạng thái đúng; không bị nhân đôi.
- [ ] Check-in tạo đúng một lượt lưu trú và chiếm đúng một phòng.
- [ ] Phát sinh dịch vụ/minibar được gắn đúng folio; thu/hoàn/void có lịch sử và số dư đúng.
- [ ] Check-out chỉ thành công khi đủ điều kiện; phòng được bàn giao cho housekeeping.
- [ ] Giao dịch thu ngân và ghi chú bàn giao đã đối soát.
- [ ] Night Audit đã qua kiểm tra và từng bước hoàn tất trước khi đóng ngày.

## 12. Phạm vi kiểm thử phần mềm

Các bài test dùng dữ liệu kiểm thử độc lập; không lấy dữ liệu khách thật làm dữ liệu kiểm thử.

## 13. Kết quả rà soát ngày 21/09/2026

### Lỗi phát hiện và đã sửa

Danh sách **Khách đến hôm nay/Khách đi hôm nay** trước đây lấy `DateTime.Today` trên máy chạy ứng dụng. Sau Night Audit hoặc khi ngày/múi giờ máy khác ngày kinh doanh của cơ sở, danh sách có thể rỗng hoặc sai ngày. Đã đổi sang ngày `BusinessDate` trong ngữ cảnh Front Office của cơ sở.

Luồng **Xác nhận đặt phòng** hiển thị nút cho trạng thái Nháp nhưng domain chỉ cho chuyển từ Tạm giữ sang Đã xác nhận, khiến thao tác trả `RESERVATION_INVALID_STATE`. Đã cho phép chuyển trực tiếp Nháp → Đã xác nhận (vẫn bắt buộc tên khách và loại phòng), đồng thời hiển thị thông báo xác nhận thành công thay vì để dòng biến mất khỏi danh sách lọc Nháp mà không giải thích.

Khi rà soát lại ảnh thao tác thực tế, phát hiện màn xác nhận vẫn cho lọc/chọn hồ sơ **Đã xác nhận** trong khi nút chỉ áp dụng cho Nháp/Tạm giữ; thẻ chi tiết lại luôn ghi “đủ điều kiện”, gây cảm giác nút bị hỏng. Đã đặt đúng `Confirmation` mode khi mở màn, giới hạn bộ lọc ở Nháp/Tạm giữ và thay thẻ tĩnh bằng lý do dựa trên trạng thái/quyền (ví dụ: hồ sơ đã xác nhận không thể xác nhận lần nữa). Không cho phép ghi lặp trạng thái cho hồ sơ đã xác nhận.

### Kiểm thử sau sửa

- Unit: **678/678 đạt**.
- Integration SQL Server/LocalDB: lượt đầy đủ có **35/36 đạt**; ca còn lại là assertion thứ tự lịch sử với timestamp trùng. Sau khi làm dữ liệu test xác định, ca xác nhận Nháp → Đã xác nhận chạy riêng đạt **1/1**. Bỏ qua `TrainingDataSqlServerResetTests` vì đây là nhóm có khả năng đặt lại dữ liệu đào tạo.
- UI smoke: lượt tổng **3/5 đạt**; 2 test khác luồng xác nhận lỗi automation (UIA không lấy được cửa sổ chính/thuộc tính Name). Ca mở mục Quản lý khách sạn chạy riêng đạt **1/1**, dùng LocalDB E2E riêng.
- Build ứng dụng và kiểm tra encoding XAML: đạt.

Ca xác nhận đặt phòng đã được kiểm tra ở ViewModel và SQL persistence; unit test bổ sung xác nhận bộ lọc chỉ chứa Nháp/Tạm giữ và hồ sơ đã xác nhận được giải thích đúng. Hai lỗi automation UI smoke nêu trên vẫn chưa được xử lý vì nằm ngoài luồng này. Phạm vi UI smoke chỉ bao phủ khởi động/điều hướng vào module, không phải thao tác thủ công qua mọi màn hình và mọi quyền người dùng; khi nghiệm thu triển khai, vẫn cần đối chiếu cấu hình cơ sở, phân quyền và thiết bị thực tế.
