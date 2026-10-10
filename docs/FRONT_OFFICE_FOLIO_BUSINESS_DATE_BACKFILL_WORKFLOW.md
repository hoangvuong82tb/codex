# Quy trình lễ tân/kế toán: đối chiếu và backfill ngày hạch toán Folio

**Phạm vi:** các dòng `HotelFolioEntry` lịch sử còn thiếu `BusinessDate`.
**Mục tiêu:** khôi phục dữ liệu đủ để báo cáo doanh thu, công suất và Night Audit
được đối chiếu, nhưng không cho phép màn hình báo cáo tự ý sửa sổ.

## Quy trình vận hành

1. Mở **Báo cáo Front Office → Kiểm soát Folio thiếu ngày**.
2. Chọn khoảng ngày tạo chứng từ để xem danh sách dòng thiếu ngày. Ngày đề xuất
   được suy ra từ `CreatedAtUtc` theo múi giờ của cơ sở; đây chỉ là gợi ý.
3. Đối chiếu từng dòng với Folio, phiếu thu/chi, ca thu ngân, chứng từ dịch vụ
   và biên bản nghiệp vụ. Bỏ chọn những dòng chưa đủ căn cứ.
4. Mở **Mở quy trình backfill**, chọn các dòng cần xử lý và kiểm tra lại ngày
   hạch toán. Có thể sửa ngày đề xuất trước khi phê duyệt.
5. Nhập **Lý do phê duyệt** và **Số tham chiếu** (biên bản/phiếu/ủy quyền),
   sau đó bấm **Phê duyệt & backfill**.
6. Tải lại báo cáo. Các dòng đã xử lý phải biến mất khỏi danh sách thiếu ngày;
   nếu báo cáo doanh thu/công suất còn chặn, tiếp tục đối chiếu các dòng còn lại.

## Kiểm soát dữ liệu

- Chỉ người có quyền `reports.backfill` được thực hiện thay đổi.
- Chỉ dòng đang thiếu `BusinessDate` mới được cập nhật; ngày đã có không thể bị
  ghi đè.
- Dữ liệu phải cùng `PropertyId` với cơ sở đang hoạt động.
- Một lần xử lý tối đa 2.000 dòng và được commit trong một transaction.
- Nếu có người khác xử lý trước, ngày khác nhau sẽ bị từ chối để tránh ghi đè;
  ngày giống nhau được coi là retry an toàn.
- Mỗi lần có thay đổi tạo audit log `FOLIO_BUSINESS_DATE_BACKFILL`, gồm người
  thao tác, lý do, số tham chiếu, số dòng và mã/ngày từng dòng.
- Mở hoặc xuất báo cáo không làm thay đổi dữ liệu.

Khi xuất báo cáo thu ngân, quyền `reports.export` chỉ cho phép xuất dữ liệu
đã che tên thu ngân. Quyền `reports.export.sensitive` phải được cấp riêng nếu
cần xuất tên đầy đủ; audit xuất báo cáo ghi rõ `sensitive=masked` hoặc
`sensitive=full`.

## Phân quyền đề xuất

| Vai trò | Xem báo cáo | Xuất báo cáo | Backfill |
|---|---:|---:|---:|
| Lễ tân | Có theo phạm vi được cấp | Theo chính sách đơn vị | Không mặc định |
| Kế toán trưởng/Quản trị | Có | Có | Có |
| Người kiểm tra | Có | Theo phê duyệt | Không |

## Điều kiện trước khi đưa vào vận hành thật

- Có biên bản phê duyệt cho từng đợt backfill.
- Đã sao lưu database và thử khôi phục ở môi trường staging.
- Đã đối chiếu tổng Folio, doanh thu phòng, thanh toán và Night Audit sau khi
  xử lý.
- Đã kiểm thử trên dữ liệu staging với hai người thao tác đồng thời.
- Không chạy migration trực tiếp trên production khi chưa qua quy trình phát
  hành database của đơn vị.
