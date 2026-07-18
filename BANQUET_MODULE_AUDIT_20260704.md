# Banquet Module Audit - 2026-07-04

## Mục tiêu

Rà soát module `Nhà hàng Tiệc` theo 3 góc:

- hiệu năng và độ mượt
- lỗi giao diện / tính ổn định WPF
- mức độ sẵn sàng để tiếp tục hoàn thiện thành màn vận hành thực tế

## Vấn đề đã xác định

### 1. Binding WPF sai với property chỉ đọc

Các property:

- `SelectedEventContractValue`
- `SelectedEventPaidAmount`
- `SelectedEventOutstandingAmount`

đã bị bind theo kiểu có thể ghi ngược, làm phát sinh `InvalidOperationException` và bật popup lỗi khi mở màn.

### 2. Query lấy danh sách sự kiện quá nặng

`BanquetRestaurantService.GetEventsAsync(...)` trước đó dùng:

- `Include(Room)`
- `Include(Menus)`
- `Include(Services)`
- `Include(Deposits)`

để tải danh sách sự kiện tổng quát.

Điều này gây:

- query lớn
- cảnh báo EF về multiple collection include
- tăng thời gian mở màn
- giật lag khi refresh

### 3. Nạp detail thừa khi không cần

`RefreshPreservingSelectionAsync(...)` trước đó:

- luôn load `MenuPackages`
- luôn load detail đầy đủ của `SelectedEvent`

kể cả khi người dùng chỉ đứng ở:

- `Tổng quan`
- `Phòng tiệc`
- `Lịch phòng`

Phần này gây chi phí load không cần thiết.

### 4. Mất virtualization ở DataGrid

View Banquet có `DataGrid` nằm bên trong `ScrollViewer` bọc ngoài toàn màn, làm:

- DataGrid khó tận dụng virtualization
- layout pass nặng hơn
- cuộn và render bị giật hơn khi dữ liệu tăng

### 5. Giao diện chưa đạt chuẩn dùng thực tế

Các lỗi nhìn thấy rõ:

- nhiều text tiếng Việt bị lỗi mã hóa
- typography chưa đồng nhất
- sidebar và nhãn màn chưa sạch
- một số màn vẫn ở mức skeleton/layout, chưa đủ độ hoàn thiện nghiệp vụ

## Phần đã sửa trong đợt này

### 1. Vá lỗi popup binding

Đã chuyển binding của các property chỉ đọc sang `Mode=OneWay` trong:

- `D:\ASMILE-BO\ASMILE.BO.App\Views\BanquetRestaurantView.xaml`

### 2. Tối ưu query danh sách sự kiện

Đã refactor `GetEventsAsync(...)` để:

- bỏ `Include` nhiều collection
- dùng projection trực tiếp sang `BanquetEventDto`
- tính `MenuAmount`, `ServiceAmount`, `DepositAmount`, `TotalAmount`, `RemainingAmount` ngay trong projection

File:

- `D:\ASMILE-BO\ASMILE.BO.Infrastructure\Services\RestaurantServices.cs`

### 3. Giảm query join lớn ở phần detail

Đã thêm `AsSplitQuery()` cho:

- `LoadEventAsync(...)`
- `LoadEventDetailAsync(...)`
- `SyncContractsAsync(...)`

Mục tiêu:

- giảm cartesian explosion
- bớt warning multiple collection include
- giảm độ nặng khi mở chi tiết sự kiện

### 4. Cắt tải detail thừa ở ViewModel

Đã thay đổi luồng refresh:

- chỉ load `MenuPackages` khi tab hiện tại thực sự cần
- chỉ load detail đầy đủ của `SelectedEvent` khi người dùng đang ở các tab nghiệp vụ cần detail
- thêm `EnsureSelectedEventDetailLoadedAsync()` để lazy-load khi chuyển tab

Files:

- `D:\ASMILE-BO\ASMILE.BO.App\ViewModels\BanquetRestaurantViewModel.Partial.cs`
- `D:\ASMILE-BO\ASMILE.BO.App\ViewModels\RestaurantViewModels.cs`

### 5. Trả lại virtualization cho DataGrid

Đã:

- thêm style mặc định cho `DataGrid` bật virtualization / recycling
- bỏ `ScrollViewer` bọc ngoài toàn bộ module

File:

- `D:\ASMILE-BO\ASMILE.BO.App\Views\BanquetRestaurantView.xaml`

## Đánh giá hiện trạng sau sửa

### Đã tốt hơn

- không còn popup binding read-only khi mở màn
- query danh sách sự kiện nhẹ hơn đáng kể
- giảm tải detail không cần thiết
- DataGrid có điều kiện render mượt hơn

### Chưa đạt mức “đi vào sử dụng được luôn”

Các điểm còn thiếu hoặc chưa đạt:

- text tiếng Việt lỗi mã hóa còn nhiều trong Banquet
- giao diện mới chỉ ở mức layout native, chưa polish theo chuẩn Win11 hoàn chỉnh
- nhiều tab còn là bản cơ bản, chưa đủ thao tác nghiệp vụ sâu
- chưa có vòng kiểm thử thao tác thực tế đầy đủ cho toàn bộ quy trình Banquet
- chưa đo định lượng thời gian tải / tương tác sau tối ưu

## Kết luận kỹ thuật

Module Banquet trước khi sửa có cả lỗi ổn định UI lẫn lỗi kiến trúc tải dữ liệu. Đợt sửa này đã xử lý các điểm gây đơ/lag rõ nhất và đưa view về trạng thái native WPF ổn định hơn.

Tuy nhiên, để đạt mục tiêu:

- chuẩn Win11
- dễ dùng
- đủ chức năng
- hoàn thiện để vận hành thực tế

thì vẫn cần thêm ít nhất một vòng hoàn thiện nữa, tập trung vào:

1. chuẩn hóa toàn bộ text Unicode
2. đồng bộ UI/UX từng tab theo cùng design system
3. hoàn thiện nghiệp vụ từng màn
4. kiểm thử thao tác thực tế end-to-end

## File chính đã chỉnh trong đợt này

- `D:\ASMILE-BO\ASMILE.BO.App\Views\BanquetRestaurantView.xaml`
- `D:\ASMILE-BO\ASMILE.BO.App\ViewModels\BanquetRestaurantViewModel.Partial.cs`
- `D:\ASMILE-BO\ASMILE.BO.App\ViewModels\RestaurantViewModels.cs`
- `D:\ASMILE-BO\ASMILE.BO.Infrastructure\Services\RestaurantServices.cs`


## B? sung 14:18 ng?y 04/07/2026

- N?i sidebar t? `92px` l?n `120px` ?? tr?nh c?t ch? ? menu tr?i.
- B? sung `TextWrapping` v? `TextAlignment` cho nh?n sidebar.
- Chuy?n danh s?ch s? ki?n ch?nh t? `ItemsControl` trong `ScrollViewer` sang `ListBox` c? virtualization ?? gi?m render lag khi danh s?ch d?i.
- Chu?n h?a l?i chu?i hi?n th? ti?ng Vi?t trong `BanquetRestaurantViewModel.Partial.cs` cho c?c tr?ng th?i, workflow step, c?nh b?o, checklist group v? timeline milestone.
- Build l?i module th?nh c?ng v?i `0 error` v? `0 warning` ? project app.
