# RECEPTION — PHÒNG KHÓA — WINDOW OPEN FIX REPORT

## 1. Phạm vi đã kiểm tra

- Repository thực tế: `D:\ASMILE-BO`
- Solution: `D:\ASMILE-BO\ASMILE.BO.sln`
- Project chạy: `D:\ASMILE-BO\ASMILE.BO.App\ASMILE.BO.App.csproj`
- View chứa card: `ASMILE.BO.App\Views\ReceptionHubView.xaml`
- DataContext/ViewModel: `ASMILE.BO.App\ViewModels\ReceptionHubViewModel.cs`
- Window đích đã có sẵn: `ASMILE.BO.App\Views\LockedRoomWindow.xaml`

Không thay đổi Banquet, Restaurant, backend, database, migration hoặc connection string.

## 2. Luồng trước khi sửa và nguyên nhân gốc

Card `Phòng khóa` dùng `MouseBinding` của card:

`ReceptionHubView.xaml` → `Command="{Binding Command}"`

Trước khi sửa, card không truyền action riêng vào `AddCard`. Vì vậy nó rơi vào command mặc định:

`AddCard(...)` → `OpenModalAsync("locked-room")` → `ModalService.OpenAsync(ReceptionWorkspaceKind.StayProfiles, "locked-room")`

Có hai vấn đề trong luồng này:

1. Workspace kind bị cố định thành `StayProfiles`, không phải luồng mở cửa sổ Phòng khóa.
2. `ReceptionHubView.xaml` chỉ render `CheckInModal.ActiveModal`, `CheckOutModal.ActiveModal` và `ActiveOverlay`; view không render `ModalService.ActiveModal`.

Do đó click/command có chạy nhưng không tạo thay đổi UI nào mà người dùng nhìn thấy. Đây không phải lỗi hit-test hay card bị control khác che.

## 3. Luồng sau khi sửa

`Phòng khóa` có action riêng:

`ReceptionHubCard.Command` → `ILockedRoomWindowService.Open()` → resolve `LockedRoomWindow` từ DI → `Window.Show()`

`LockedRoomWindowService` được đăng ký singleton để giữ duy nhất một tham chiếu cửa sổ trong toàn ứng dụng, kể cả khi `ReceptionHubViewModel` được tạo lại vì ViewModel này có lifetime transient.

Khi cửa sổ đã tồn tại:

- Không tạo instance thứ hai.
- Nếu đang minimized thì chuyển về `WindowState.Normal`.
- Gọi `Activate()` và `Focus()`.

Khi cửa sổ đóng:

- Gỡ event `Closed`.
- Xóa tham chiếu để lần click sau có thể tạo và mở lại cửa sổ mới.

Service còn có cờ `isOpening` để chặn click liên tiếp trong lúc cửa sổ đang được khởi tạo.

## 4. Thiết lập cửa sổ

Luồng mở áp dụng rõ ràng:

- `Owner = Application.Current.MainWindow` khi main window đã load.
- `WindowStartupLocation = CenterOwner`.
- `ResizeMode = CanResize`.
- `ShowInTaskbar = false`.
- Mở bằng `Show()`, không dùng overlay và không thay `ActiveWorkspace`.

Việc gán `Owner` cũ trong constructor của `LockedRoomWindow` đã được bỏ để trách nhiệm quản lý owner/vòng đời nằm tập trung tại service mở cửa sổ.

## 5. DI và xử lý lỗi

- `LockedRoomWindow`: transient, tạo mới sau khi cửa sổ cũ đã đóng.
- `ILockedRoomWindowService`: singleton, bảo vệ single-instance ở phạm vi application.
- Lỗi khởi tạo/mở cửa sổ được ghi bằng Serilog với exception thật rồi được ném lại; không bị nuốt bằng thông báo chung của modal path cũ.

## 6. File thay đổi

- `ASMILE.BO.App\ViewModels\ReceptionHubViewModel.cs`
- `ASMILE.BO.App\DependencyInjection.cs`
- `ASMILE.BO.App\Views\LockedRoomWindow.xaml.cs`
- `ASMILE.BO.App\Services\LockedRoomWindowService.cs` (mới)
- `docs\RECEPTION_LOCKED_ROOM_WINDOW_OPEN_FIX_REPORT.md` (mới)
- `docs\reception-locked-room-before.png` (bằng chứng trước sửa)
- `docs\reception-locked-room-after.png` (bằng chứng sau sửa)

`LockedRoomWindow.xaml` đã tồn tại và không cần dựng lại UI.

## 7. Kết quả build và encoding

Lệnh:

```powershell
dotnet build .\ASMILE.BO.App\ASMILE.BO.App.csproj --no-restore
```

Kết quả cuối:

```text
Build succeeded.
99 Warning(s)
0 Error(s)
Encoding check passed: no mojibake patterns found in XAML.
Time Elapsed 00:01:23.88
```

Các warning là warning sẵn có ở các phần FO/ứng dụng khác sau full rebuild; không có error mới từ luồng Phòng khóa.

## 8. Kết quả runtime

- Đã quan sát trực tiếp `LockedRoomWindow` mở phía trước cửa sổ `Lễ tân`.
- Owner vẫn hiển thị phía sau; cửa sổ được căn giữa, có resize và không tạo taskbar item riêng.
- Ảnh runtime sau sửa thể hiện đúng window `Phòng khóa` với danh sách, bộ lọc, chi tiết và footer.
- Không thấy lỗi binding trên UI/runtime đã quan sát.
- Focused WPF smoke test trực tiếp trên service trả về `LOCKED_ROOM_WINDOW_SMOKE_PASS`.
- Smoke test xác nhận owner/cấu hình window, mở hai lần vẫn chỉ có một instance, minimized rồi mở lại được restore, đóng xong mở lại tạo instance mới và tham chiếu cũ đã được xóa.
- Harness smoke test chỉ dùng để xác minh, đã được dọn khỏi source sau khi chạy.
- Build cuối sau khi bổ sung single-instance service đã pass.

Khi dừng toàn bộ process để thử lại từ trạng thái sạch, ứng dụng quay về LoginPanel và không còn phiên đăng nhập của người dùng. Không sử dụng hoặc đoán thông tin đăng nhập; thay vào đó, vòng đời single-instance được xác minh bằng focused WPF smoke test nêu trên.

## 9. Bằng chứng hình ảnh

- [Trước sửa — click card không hiện cửa sổ](reception-locked-room-before.png)
- [Sau sửa — LockedRoomWindow mở phía trước Lễ tân](reception-locked-room-after.png)
