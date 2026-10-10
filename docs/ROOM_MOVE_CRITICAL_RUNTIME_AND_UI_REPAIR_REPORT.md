# ROOM MOVE — CRITICAL RUNTIME AND UI REPAIR REPORT

Ngày xác nhận: 24/07/2026  
Phạm vi: `Lễ tân → Điều chỉnh lưu trú → Đổi phòng`  
Project root duy nhất: `D:\ASMILE-BO`

## Kết luận

Luồng Đổi phòng đã được sửa tại nguồn: điểm mở chỉ còn một `Button.Click`, có single-instance protection, không còn `MessageBox` trong các file của luồng, wizard dùng duy nhất `CurrentStep`, năm body được tạo cố định trong XAML, pager khách/phòng nằm ở row cố định ngoài vùng cuộn và confirmation là in-app dialog không gọi backend.

Build cuối PASS với `0 Warning(s)`, `0 Error(s)` và kiểm tra encoding XAML PASS. Runtime harness PASS toàn bộ thao tác mở window, duplicate protection, restore minimized, đóng/mở lại, 120 khách, 80 phòng, chuyển trang, chọn khách, preview/chọn phòng, Step A–E, footer, confirmation dialog và hai kích thước QA.

## 1. Exception type thực tế

Log thật tại `ASMILE.BO.App\bin\Debug\net8.0-windows\logs\ui-exceptions.log` ghi nhận hai lỗi gốc:

1. `System.InvalidOperationException: Specified element is already the logical child of another element. Disconnect it first.`
2. `System.Windows.ResourceReferenceKeyNotFoundException: 'GrayBtn' resource not found.`

Đây là lỗi WPF visual/logical tree và resource lookup, không phải lỗi backend hay database.

## 2. StackTrace

Stack chính của lỗi body trắng:

```text
System.InvalidOperationException: Specified element is already the logical child of another element.
   at System.Windows.FrameworkElement.ChangeLogicalParent(DependencyObject newParent)
   at System.Windows.FrameworkElement.AddLogicalChild(Object child)
   at System.Windows.Controls.UIElementCollection.AddInternal(UIElement element)
   at ASMILE.BO.App.Views.RoomMoveWindow.Card(String title, FrameworkElement child)
   at ASMILE.BO.App.Views.RoomMoveWindow.ShowAdjustStep()
   at ASMILE.BO.App.Views.RoomMoveWindow.ShowStep()
   at ASMILE.BO.App.Views.RoomMoveWindow.GoTo(Int32 step)
```

Stack Step E thay `ShowAdjustStep()` bằng `ShowConfirmStep()`. Log cũng ghi các lần `GrayBtn resource not found` tại 21:36–21:39. Global `DispatcherUnhandledException` trong `App.xaml.cs` nhận các exception này và hiển thị câu báo lỗi chung.

## 3. Nguyên nhân cảm giác mở Window hai lần

Không có bằng chứng một click đã tạo hai `RoomMoveWindow`. Điểm mở cũ chỉ có một `MouseLeftButtonDown="OnRoomMove"` và handler gọi `new RoomMoveWindow().Show()` đúng một lần, nhưng không có duplicate protection.

Cảm giác phải xử lý hai lần đến từ các exception render liên tiếp được global handler bắt và hiển thị dialog lỗi; sau khi exception được đánh dấu handled, lệnh show/window message loop vẫn tiếp tục nên Window cuối cùng vẫn xuất hiện. Thiết kế cũ cũng không ngăn click lặp tạo thêm instance.

## 4. Event/command bị gọi bao nhiêu lần

- Trước sửa: một `MouseLeftButtonDown` handler tại card; không có `ICommand` thứ hai trong chính card và không tìm thấy bằng chứng handler mở window chạy hai lần cho một routed event.
- Sau sửa: đúng một `Button.Click="OnRoomMove"`.
- Runtime QA raise một click đầu tiên và thu được đúng một `RoomMoveWindow`; click lần hai khi window đang mở vẫn có tổng đúng một window.

## 5. Cách sửa duplicate event và duplicate window

- Thay `Border.MouseLeftButtonDown` bằng một Button có `Click` rõ ràng.
- `OnRoomMove` đặt `e.Handled = true`.
- Trước khi tạo mới, tìm window còn sống trong `Application.Current.Windows`.
- Nếu đang minimized: chuyển về `WindowState.Normal`.
- Sau đó `Activate()` và `Focus()`, không tạo instance thứ hai.
- Không giữ static reference nên window đã `Close()` không bị giữ lại; lần mở tiếp theo tạo instance sạch.
- Runtime QA đã kiểm tra cả click lặp, restore minimized và close/reopen.

## 6. Nguyên nhân MessageBox xuất hiện hai lần

Các exception render phát sinh liên tiếp, không phải hai lệnh `MessageBox.Show` trong Room Move. `App.OnDispatcherUnhandledException` hiển thị:

```text
Thao tác không thành công. Vui lòng kiểm tra nhật ký hệ thống.
```

cho unhandled UI exception. Ví dụ log có các lỗi `GrayBtn` ở 21:36:40.861, 21:36:42.056, 21:36:42.689 và nhiều lần sau đó. Khi hai exception cách nhau đủ khoảng debounce, người dùng thấy hai dialog. Luồng Room Move mới không còn phát sinh hai exception gốc và không gọi MessageBox.

## 7. Nguyên nhân Step B/C/E body trắng

- Step B/danh sách: pager động gọi `Application.Current.FindResource("GrayBtn")` trong khi `GrayBtn` chỉ thuộc `RoomMoveWindow.Resources`; resource lookup ném exception giữa quá trình dựng nội dung.
- Step C: factory động tái dùng một `FrameworkElement` đã có logical parent rồi đưa vào `Card(...)`, gây `InvalidOperationException`.
- Step E: cùng lỗi tái dùng logical child trong `ShowConfirmStep()`.
- Sau khi exception thoát khỏi hàm dựng body, step bar/footer có thể đã đổi nhưng content chưa gắn xong, tạo trạng thái step active nhưng body trắng/không đồng bộ.

## 8. Cách sửa từng step

- Step A: body cố định trong XAML; toolbar đầy đủ, 120 mock guests, không preselect, detail 65/35, pager riêng.
- Step B: body cố định trong XAML; 80 mock rooms; row chỉ preview; nút `Chọn phòng này` mới commit; thiếu khách chỉ hiện inline warning.
- Step C: hai panel lớn được tạo một lần trong XAML; luôn render; thiếu dữ liệu chỉ hiện warning/disable field phụ thuộc.
- Step D: bốn nhóm checklist với template checkbox hiện đại; body stretch.
- Step E: summary/readiness được tạo cố định; thiếu dữ liệu không crash và confirm disabled; click step không mở dialog.

Không còn factory tạo lại/reparent control khi chuyển step.

## 9. Root Grid mới

`RoomMoveWindow.xaml` dùng đúng năm row:

```text
Row 0  Header         Auto
Row 1  Step bar       Auto
Row 2  Summary strip  Auto
Row 3  Step content   *
Row 4  Footer         Auto
```

Không có ScrollViewer bao toàn window. Mỗi step là root Grid stretch; chỉ DataGrid/nội dung chi tiết quản lý cuộn nội bộ.

## 10. Pager trước và sau

Trước:

- Pager được dựng động.
- Dùng resource `GrayBtn` sai scope.
- Nằm sau nội dung cuộn, chỉ thấy khi kéo xuống.
- Nút xám kiểu mặc định, chủ yếu Trước/Sau.

Sau:

- Pager là XAML cố định.
- Có summary chính xác, mũi tên, page number, ellipsis và page-size selector.
- Không dùng `Application.Current.FindResource`.
- Style nằm đúng trong window resources và dùng custom template.

## 11. Vị trí pager cố định

Cả Step A và Step B dùng:

```text
Row 0  Filter toolbar  Auto
Row 1  DataGrid        *
Row 2  Pager           Auto
```

Pager là sibling của DataGrid, không nằm trong ScrollViewer/DataGrid scroll content. Runtime QA xác nhận pager và footer nằm trong bounds tại cửa sổ 1320×720 (mô phỏng vùng làm việc 1366×768) và 1600×960 (mô phỏng 1920×1080).

## 12. Thiết kế pager mới

- Nội dung: `Hiển thị 1–20 trên tổng 120 khách/phòng`.
- Điều hướng: `←`, page number, ellipsis khi cần, `→`.
- Active page nền xanh, chữ trắng.
- Button cao 35 px, corner radius 8, không border đen.
- Page size: 20, 50, 100 dòng/trang.
- Filter thay đổi đưa pager về trang 1 và tính lại total/page count.

## 13. CurrentStep mapping

Một nguồn trạng thái duy nhất:

```text
1 = Chọn khách
2 = Chọn phòng
3 = Điều chỉnh
4 = Bàn giao
5 = Xác nhận
```

`CurrentStep` điều khiển visibility body, active step, footer, Back/Next/Confirm và summary. Click step chỉ đổi `CurrentStep` rồi render state; không gọi service, validation backend, lưu dữ liệu hoặc confirmation.

## 14. Footer synchronization

Footer lấy trực tiếp từ:

```csharp
FooterStepLabel.Text = $"Bước {CurrentStep}/5 · {StepNames[CurrentStep - 1]}";
```

Step 5 collapse `Tiếp tục` và chỉ hiện `Xác nhận đổi phòng`. Runtime QA assert chính xác footer Step C `3/5`, Step D `4/5`, Step E `5/5`.

## 15. In-app confirmation dialog

Thêm `RoomMoveConfirmationWindow`:

- Owner là chính `RoomMoveWindow`.
- `CenterOwner`, không có taskbar item.
- Card trắng, vector hai mũi tên, tóm tắt phòng cũ → phòng mới và cảnh báo.
- Nút `Quay lại` và `Xác nhận mockup`.
- Chỉ mở khi click thật nút confirm tại Step E.
- Không gọi backend và không ghi database.

## 16. MessageBox hệ thống đã loại bỏ khỏi luồng

Static scan trên sáu file Room Move/điểm mở không còn:

- `MessageBox`
- `System.Windows.MessageBox`
- WinForms MessageBox
- `Application.Current.FindResource`

Exception tại điểm mở và render được log chi tiết qua Serilog (`ExceptionType`, message, stack trace, inner exception, method, step) rồi hiển thị inline state. Global handler dùng chung toàn app không bị sửa vì nằm ngoài phạm vi; Room Move mới không đưa exception lên handler đó trong runtime QA.

## 17. File đã sửa/thêm

- `ASMILE.BO.App\Views\StayAdjustmentWorkspaceView.xaml`
- `ASMILE.BO.App\Views\StayAdjustmentWorkspaceView.xaml.cs`
- `ASMILE.BO.App\Views\RoomMoveWindow.xaml`
- `ASMILE.BO.App\Views\RoomMoveWindow.xaml.cs`
- `ASMILE.BO.App\Views\RoomMoveConfirmationWindow.xaml`
- `ASMILE.BO.App\Views\RoomMoveConfirmationWindow.xaml.cs`
- `docs\ROOM_MOVE_CRITICAL_RUNTIME_AND_UI_REPAIR_REPORT.md`

Không sửa card Gia hạn, backend, database hoặc Banquet.

## 18. Build result

Lệnh:

```powershell
dotnet build .\ASMILE.BO.App\ASMILE.BO.App.csproj --no-restore
```

Kết quả cuối:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
Encoding check passed: no mojibake patterns found in XAML.
Time Elapsed 00:00:03.55
```

## 19. Runtime mở Window

PASS:

- Raise đúng một `RoomMoveCardButton.Click`.
- Window mở ngay, Owner đúng, `CenterOwner`, resizable, không taskbar item.
- Click card lần hai không tạo duplicate.
- Minimized được restore.
- Close rồi click lại tạo instance sạch.
- Không có window lỗi hệ thống trong luồng QA.

## 20. Runtime Step A

PASS:

- Wizard bắt đầu ở `CurrentStep=1`.
- Grid có 20 dòng trên trang và total 120 khách.
- Pager chuyển từ 1–20 sang 21–40.
- Chọn khách cập nhật detail/summary.
- Không preselect khi mở.

## 21. Runtime Step B

PASS:

- `CurrentStep=2`, body visible.
- Grid có 20 dòng trên trang và total 80 phòng.
- Chọn row chỉ cập nhật preview.
- Nút `Chọn phòng này` enabled cho phòng hợp lệ.
- Bấm nút mới cập nhật summary phòng cũ → phòng mới.

## 22. Runtime Step C

PASS:

- `CurrentStep=3`.
- `Step3Content` visible, không body trắng.
- Footer đúng `Bước 3/5 · Điều chỉnh`.
- Không MessageBox, không service/backend call.

## 23. Runtime Step D

PASS:

- `CurrentStep=4`.
- `Step4Content` visible.
- Bốn nhóm checklist render.
- Footer đúng `Bước 4/5 · Bàn giao`.

## 24. Runtime Step E

PASS:

- `CurrentStep=5`.
- `Step5Content` visible, không body trắng.
- Footer đúng `Bước 5/5 · Xác nhận`.
- Nút Tiếp tục collapsed.
- Confirm chỉ enabled sau guest + committed room.
- Click tab E không mở dialog; click thật nút confirm mới mở dialog.
- Dialog owner/center đúng và đóng quay lại an toàn.

Runtime harness output:

```text
ROOM_MOVE_RUNTIME_QA_PASS
OpenCount=1; Guests=120; Rooms=80; Steps=A-E; Confirmation=PASS; Sizes=1366x768,1920x1080
```

## 25. Ảnh mở Window không lỗi

BLOCKED bởi môi trường capture của Codex Desktop, không phải bởi Room Move. Computer Use trả:

```text
GetCursorPos failed: Access is denied. (0x80070005)
IGraphicsCaptureItemInterop.CreateForMonitor failed: Could not capture the given monitor. (0x80070057)
```

WPF `RenderTargetBitmap`, native `PrintWindow` và `CopyFromScreen` cũng không lấy được surface nội dung trong session này. Không lưu ảnh đen và không tạo ảnh giả. Bằng chứng thay thế là runtime assertion ở mục 19 và output PASS ở trên.

## 26. Ảnh pager khách

BLOCKED cùng hạn chế capture ở mục 25. Runtime assertion xác nhận pager khách visible/in-bounds, 20 dòng, tổng 120 và chuyển trang 21–40.

## 27. Ảnh pager phòng

BLOCKED cùng hạn chế capture ở mục 25. Runtime assertion xác nhận pager phòng visible/in-bounds, 20 dòng, tổng 80 và flow preview/commit.

## 28. Ảnh Step C

BLOCKED cùng hạn chế capture ở mục 25. Runtime assertion xác nhận `Step3Content.Visibility == Visible`, footer 3/5 và không xuất hiện window lỗi.

## 29. Ảnh Step E

BLOCKED cùng hạn chế capture ở mục 25. Runtime assertion xác nhận Step E visible, footer 5/5, Next collapsed, Confirm đúng điều kiện và custom dialog mở chỉ sau click thật.

## 30. Xác nhận không sửa backend

Đã xác nhận. Không sửa service/application/infrastructure và không thêm backend call vào Room Move.

## 31. Xác nhận không sửa database

Đã xác nhận. Không sửa migration, DbContext, schema, SQL hoặc dữ liệu.

## 32. Xác nhận không sửa Banquet

Đã xác nhận. Không mở rộng phạm vi và không sửa file Banquet.

## 33. Xác nhận project root

Mọi thao tác source/build/runtime/report đều thực hiện trong `D:\ASMILE-BO`. Không sử dụng `F:\ASMILE-BO`.

