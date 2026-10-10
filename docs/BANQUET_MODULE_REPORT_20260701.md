# Banquet Module Report 2026-07-01

## Mục tiêu

Hoàn thiện module `Nhà hàng Tiệc` từ trạng thái giao diện khung sang module có dữ liệu thật và thao tác nghiệp vụ thật trong WPF/MVVM.

## Phân tích hiện trạng trước khi sửa

- `BanquetRestaurantViewModel` chỉ có các thao tác cơ bản cho phòng, sự kiện, cọc, hợp đồng và checklist.
- Giao diện `BanquetRestaurantView.xaml` có nhiều tab nhưng một số phần vẫn là mockup:
  - `Dịch vụ` dùng checkbox cứng, không đọc/ghi dữ liệu thật.
  - `Timeline` chỉ là placeholder.
  - `Hợp đồng` chưa hiển thị danh sách hợp đồng thật.
  - `Đặt cọc` chưa có lịch sử thu tiền thật.
  - `Thực đơn` chỉ hiển thị catalog, chưa quản lý danh sách thực đơn đã gắn vào sự kiện.
- ViewModel có command lưu phòng/lưu sự kiện nhưng UI chưa mở form thao tác tương ứng một cách đầy đủ.
- Entity đã có sẵn `BanquetTimeline`, `BanquetEventMenu`, `BanquetServiceLine`, `BanquetDeposit`, `BanquetContract` nhưng service và UI chưa khai thác hết.

## Phần đã triển khai

### 1. Mở rộng contract dữ liệu

Đã bổ sung trong `ASMILE.BO.Application/DTOs/RestaurantDtos.cs`:

- `BanquetEventMenuDto`
- `BanquetTimelineItemDto`
- `BanquetEventDetailDto`
- `SaveBanquetTimelineItemRequest`

### 2. Mở rộng service `IBanquetRestaurantService`

Đã bổ sung các API:

- `GetEventDetailAsync(...)`
- `UpdateEventStatusAsync(...)`
- `FinalizeSettlementAsync(...)`
- `DetachMenuPackageAsync(...)`
- `DeleteServiceLineAsync(...)`
- `GetTimelineAsync(...)`
- `SaveTimelineItemAsync(...)`
- `UpdateTimelineItemStatusAsync(...)`

### 3. Nâng cấp `BanquetRestaurantService`

Trong `ASMILE.BO.Infrastructure/Services/RestaurantServices.cs`:

- Tải detail đầy đủ cho từng sự kiện gồm:
  - sự kiện
  - thực đơn đã gắn
  - dịch vụ phát sinh
  - đặt cọc/thu tiền
  - hợp đồng
  - checklist
  - timeline
- Khi tạo sự kiện mới:
  - tự sinh checklist mặc định
  - tự sinh timeline vận hành mặc định
- Khi gắn thực đơn hoặc lưu/xóa dịch vụ:
  - tự đồng bộ lại tổng tiền hợp đồng
- Khi tạo hợp đồng:
  - nếu đã có hợp đồng thì cập nhật lại số tiền thay vì tạo bản ghi rác
- Khi ghi nhận cọc:
  - tự cập nhật trạng thái sự kiện sang `Deposited` nếu phù hợp
  - đồng bộ lại tổng số tiền cọc và công nợ hợp đồng
- Khi quyết toán:
  - tự thu nốt phần còn lại bằng phiếu thu cuối
  - chuyển sự kiện sang `Completed`
  - đưa hợp đồng về số dư còn lại bằng `0`
- Bổ sung thao tác cập nhật trạng thái timeline.

### 4. Mở rộng ViewModel cho thao tác thật

Đã thêm file:

- `ASMILE.BO.App/ViewModels/BanquetRestaurantViewModel.Partial.cs`

Nội dung chính:

- Collection dữ liệu thật cho:
  - `EventServices`
  - `EventDeposits`
  - `EventContracts`
  - `EventMenus`
  - `EventTimelineItems`
- Form state cho:
  - sự kiện
  - phòng tiệc
  - cọc/thanh toán
  - dịch vụ
  - hợp đồng
- Command mới:
  - tạo sự kiện mới
  - lưu dịch vụ
  - xóa dịch vụ
  - gắn thực đơn vào sự kiện
  - gỡ thực đơn khỏi sự kiện
  - quyết toán sự kiện
  - chuyển sự kiện sang chuẩn bị
  - hủy sự kiện
  - đổi trạng thái timeline
- Giữ lại selection sau refresh để thao tác không bị bật ra khỏi màn hình đang làm việc.

### 5. Thiết kế lại giao diện `BanquetRestaurantView.xaml`

Đã thay các khối mockup bằng UI thao tác thật:

- Danh sách tiệc:
  - thêm nút `Tiệc mới`
- Tab `Thông tin`:
  - form sửa/tạo sự kiện thật
  - form sửa/tạo phòng thật
- Tab `Hợp đồng`:
  - ngày ký
  - tạo/cập nhật hợp đồng
  - danh sách hợp đồng thật
- Tab `Đặt cọc`:
  - form thu tiền/cọc
  - lịch sử thu tiền thật
- Tab `Checklist`:
  - giữ thao tác thật
- Tab `Timeline`:
  - hiển thị timeline thật
  - đổi trạng thái từng mốc
- Tab `Dịch vụ`:
  - form thêm dịch vụ phát sinh thật
  - danh sách dịch vụ thật
  - xóa dịch vụ
- Tab `Thực đơn`:
  - chọn gói thực đơn
  - gắn vào sự kiện
  - hiển thị thực đơn đã gắn
  - gỡ thực đơn khỏi sự kiện
- Footer:
  - `Lưu sự kiện`
  - `Chuyển chuẩn bị`
  - `Quyết toán`
  - `Hủy sự kiện`

## Kiểm thử

### Build

- `dotnet build D:\ASMILE-BO\ASMILE.BO.sln`
- Kết quả: thành công.

### Unit test

Đã chạy:

- `dotnet test D:\ASMILE-BO\ASMILE.BO.Tests.Unit\ASMILE.BO.Tests.Unit.csproj --filter "Banquet_"`

Kết quả:

- Passed: `4`
- Failed: `0`

## Lưu ý

- Một test cũ không thuộc phạm vi Banquet là `Alacarte_ShouldReuseBanquetMenuPackagesAsRealServingSets` đang fail khi chạy chung filter mở rộng với Banquet. Mình không sửa test đó trong đợt này để tránh làm lan sang module À La Carte khi chưa xác minh nghiệp vụ liên quan.
- File giao diện Banquet đã được chỉnh để vượt qua script kiểm tra mojibake XAML.
