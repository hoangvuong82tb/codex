# Kế hoạch rà soát và triển khai tiếp theo — Module Quản lý khách sạn

## 1. Kết luận hiện trạng

Module đã có catalog tương đối đầy đủ nhưng chưa sẵn sàng vận hành toàn bộ.

Các vấn đề chính:

1. Một số card hiển thị `Đang hoạt động` nhưng khi bấm vẫn báo `Sắp có`.
2. Nhiều màn hình đang sử dụng dữ liệu mẫu hoặc dữ liệu sinh ngẫu nhiên.
3. Các nghiệp vụ tài chính như Folio, thu tiền, tách Folio và gộp Folio chưa kết nối backend thật.
4. Một số tài liệu audit cũ chưa phản ánh đúng trạng thái source hiện tại.

## 2. Danh sách module hiện tại

| Module | Trạng thái điều hướng | Trạng thái dữ liệu |
|---|---|---|
| Đặt phòng | Đã có route | Có luồng service thật |
| Lễ tân | Đã có route | Pha trộn thật và mock |
| Thu ngân | Đã có route | Phần lớn là mock |
| Folio | Nằm trong Thu ngân | Chưa có backend tài chính hoàn chỉnh |
| Tách Folio | Nằm trong Thu ngân | Giao diện có, nghiệp vụ còn mock |
| Gộp Folio | Nằm trong Thu ngân | Giao diện có, nghiệp vụ còn mock |
| Quản lý chìa khóa | Có route | Cần kiểm tra riêng nguồn dữ liệu |
| Đoàn | Card báo hoạt động nhưng route đang sai | Chủ yếu mock |
| Nhà buồng | Card báo hoạt động nhưng route đang sai | Board có dữ liệu thật, thao tác còn mock |
| Kế toán phải thu | Card báo hoạt động nhưng route đang sai | Ledger có dữ liệu thật, các màn hình khác còn mock |
| Tìm kiếm toàn hệ thống | Card báo hoạt động nhưng route đang sai | Pha trộn thật và mock |
| Marketing | Card báo hoạt động nhưng route đang sai | Dữ liệu trong bộ nhớ |
| Báo cáo | Card báo hoạt động nhưng route đang sai | Danh sách và thao tác còn mock |
| Night Audit | Card báo hoạt động nhưng route đang sai | Đang mô phỏng, chưa ghi dữ liệu |
| Tổng đài | Đang ẩn | Chưa triển khai |

## 3. Lỗi cần sửa trước tiên

File liên quan:

- `ASMILE.BO.App/ViewModels/FrontOfficeHubViewModel.cs`

Hàm `AddComingSoon` hiện tạo hub con và kiểm tra số lượng card để gán trạng thái `Đang hoạt động`, nhưng command vẫn gọi `ShowComingSoon(title)`.

### Cách xử lý

1. Nếu module có hub và có card con:
   - mở đúng hub tương ứng;
   - hiển thị `Đang hoạt động`;
   - cho phép bấm card.
2. Nếu module chưa có nghiệp vụ:
   - hiển thị `Sắp có` hoặc `Bản thử nghiệm`;
   - không gắn trạng thái đang hoạt động.
3. Không để trạng thái giao diện và command mở màn hình được cấu hình ở hai nơi khác nhau.

### Tiêu chí nghiệm thu

- Bấm vào mọi card `Đang hoạt động` đều mở được màn hình hoặc hub tương ứng.
- Không card nào vừa hiển thị `Đang hoạt động` vừa báo `Sắp có`.
- Có test click-through cho toàn bộ card cấp cao.

## 4. Chuẩn hóa các mục trùng

### Folio

Giữ Folio trong module `Thu ngân`, gồm:

- Folio;
- Tách Folio;
- Gộp Folio;
- Thu tiền Folio;
- Hóa đơn và giao dịch liên quan.

Không đưa Folio trở lại màn hình chính. Các file `CashieringWorkspaceView` và `CashieringWindow` cũ cần được đánh dấu legacy hoặc loại khỏi route chính sau khi xác nhận không còn nơi sử dụng.

### Tình trạng phòng

Lễ tân và Nhà buồng nên dùng chung nguồn dữ liệu room board. Không tạo hai màn hình quản lý trạng thái phòng độc lập.

### Báo cáo

Các module có thể mở cùng một trung tâm báo cáo, nhưng cần truyền rõ:

- module nguồn;
- loại báo cáo;
- bộ lọc;
- quyền xem;
- quyền xuất dữ liệu.

### Khách hàng và doanh nghiệp

Giữ thông tin khách hàng/doanh nghiệp ở Kế toán phải thu hoặc hồ sơ dùng chung. Không tạo bản sao trong Marketing.

### Nhật ký FO

Giữ trong nhóm `Tìm kiếm toàn hệ thống` hoặc `Nhật ký FO`. Không tạo thêm card độc lập ở màn hình chính.

## 5. Lộ trình triển khai

## Giai đoạn 0 — Sửa nền tảng và điều hướng

### Công việc

1. Sửa command của các card cấp cao.
2. Tạo catalog module duy nhất gồm:
   - mã module;
   - tên hiển thị;
   - trạng thái;
   - danh sách card con;
   - command mở màn hình;
   - quyền truy cập.
3. Bổ sung test cho toàn bộ route.
4. Cập nhật tài liệu audit và sơ đồ module.
5. Không gắn `Đang hoạt động` cho màn hình chỉ có giao diện mock.

### Kết quả cần đạt

Giao diện phản ánh đúng khả năng sử dụng thực tế của từng module.

## Giai đoạn 1 — Hoàn thiện vận hành cốt lõi

### Đặt phòng

Triển khai và nghiệm thu:

1. Tạo đặt phòng.
2. Tìm đặt phòng.
3. Sửa đặt phòng.
4. Phân phòng.
5. Xác nhận đặt phòng.
6. Hủy đặt phòng.
7. No-show.

### Lễ tân

Triển khai và nghiệm thu:

1. Check-in.
2. Walk-in.
3. Check-out.
4. Điều chỉnh lưu trú.
5. Hồ sơ khách.
6. Khách đến hôm nay.
7. Khách đi hôm nay.
8. Khách hiện tại.

### Nhà buồng

Triển khai các command ghi dữ liệu:

1. Chuyển trạng thái phòng.
2. Giao việc buồng.
3. Hoàn tất dọn phòng.
4. Kiểm tra phòng.
5. Minibar.
6. Lost & Found.
7. Bảo trì.

## Giai đoạn 2 — Thu ngân và Folio

Đây là nhóm có rủi ro tài chính cao nhất.

### Phạm vi

1. Folio.
2. Thu tiền.
3. Tiền đặt cọc.
4. Hoàn tiền.
5. Hóa đơn.
6. Phiếu thu.
7. Phiếu chi.
8. Giao dịch.
9. Tách Folio.
10. Gộp Folio.

### Yêu cầu backend

1. Transaction bất biến và có lịch sử audit.
2. Chống ghi nhận giao dịch trùng.
3. Kiểm soát đồng thời khi nhiều thu ngân thao tác.
4. Phân quyền theo vai trò và cơ sở.
5. Liên kết thống nhất giữa `Guest`, `Reservation`, `Stay`, `Folio` và `Payment`.
6. Khóa dữ liệu phù hợp khi Night Audit.
7. Có cơ chế hoàn tác hoặc điều chỉnh có kiểm soát.

Không nên coi module Thu ngân là hoàn tất chỉ vì giao diện và card đã mở được.

## Giai đoạn 3 — Dữ liệu dùng chung và báo cáo

### Kế toán phải thu

1. Kết nối khách hàng công nợ.
2. Kết nối doanh nghiệp.
3. Kết nối hóa đơn.
4. Kết nối thu công nợ.
5. Kết nối đối chiếu.
6. Kết nối tuổi nợ.
7. Đảm bảo tất cả dùng chung mã khách hàng, doanh nghiệp, hóa đơn và giao dịch.

### Tra cứu

1. Hồ sơ lưu trú.
2. Tra cứu giao dịch.
3. Nhật ký FO.
4. Tra cứu khách, phòng và đặt phòng.

### Báo cáo

Ưu tiên trước các báo cáo vận hành:

1. Khách đến.
2. Khách đi.
3. Công suất phòng.
4. Tình trạng phòng.
5. Folio mở.
6. Doanh thu.
7. Giao dịch thu ngân.

Sau đó mới triển khai lịch chạy báo cáo, xuất file và báo cáo nâng cao.

## Giai đoạn 4 — Module nâng cao

1. Quản lý đoàn.
2. Room block và rooming list.
3. Check-in/check-out đoàn.
4. Night Audit thật.
5. Tổng đài.
6. Đặt phòng Web.
7. Availability Engine.
8. Tích hợp hệ thống bên ngoài.

### Night Audit cần có

1. Kiểm tra điều kiện trước khi chạy.
2. Khóa thời điểm chạy.
3. Ghi nhận các bút toán cần thiết.
4. Đối chiếu Folio và giao dịch.
5. Chạy lặp an toàn, không ghi trùng.
6. Đóng ngày kinh doanh.
7. Lưu audit log.

## 6. Thứ tự ưu tiên đề xuất

```text
P0  Sửa điều hướng và trạng thái card
P1  Đặt phòng + Lễ tân
P1  Nhà buồng và cập nhật trạng thái phòng
P1  Backend Thu ngân + Folio
P2  Kế toán phải thu
P2  Tra cứu và báo cáo
P2  Marketing có dữ liệu thật
P3  Đoàn + Night Audit
P3  Tổng đài + Web reservation
```

## 7. Điều kiện trước khi phát hành production

- Không còn card hoạt động nhưng không mở được.
- Không còn dữ liệu random trong luồng production.
- Các thao tác tạo/sửa/xác nhận/hủy/check-in/check-out có ghi dữ liệu thật.
- Thu tiền, hoàn tiền, tách/gộp Folio có transaction và audit log.
- Có kiểm tra quyền truy cập.
- Có kiểm tra cơ sở và ngày kinh doanh.
- Có xử lý lỗi rõ ràng, không chỉ hiện thông báo chung chung.
- Có test unit cho ViewModel/service.
- Có smoke test UI cho các luồng chính.
- Có tài liệu route matrix và trạng thái dữ liệu được cập nhật.

## 8. Khuyến nghị cuối cùng

Không nên tiếp tục bổ sung thêm card mới ở thời điểm này. Nên hoàn thành theo thứ tự:

**Sửa điều hướng → hoàn thiện Đặt phòng/Lễ tân → hoàn thiện Nhà buồng → xây backend Thu ngân/Folio → hoàn thiện Kế toán phải thu và Báo cáo → triển khai Đoàn/Night Audit/Tổng đài.**

