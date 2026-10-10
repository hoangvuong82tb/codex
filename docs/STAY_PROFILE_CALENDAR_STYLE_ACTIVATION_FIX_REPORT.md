# StayProfile — Calendar style activation fix

## Kết luận

Lịch trong `StayProfileWindow` không dùng template Front Office dù các resource `FoModernCalendarStyle`, `FoCalendarItemStyle` và `FoCalendarDayButtonStyle` đã được nạp. Nguyên nhân là template `DatePicker` tự tạo một `Calendar` nội bộ rồi gán nó vào `PART_Popup.Child`; `Calendar` được nhúng thủ công trong popup của template chỉ là một control “chết”, bị WPF ghi đè trước khi popup hiển thị.

## Bằng chứng runtime trước khi sửa

- `Application.TryFindResource` trả về `System.Windows.Style` cho cả bốn key calendar, nên resource dictionary không bị thiếu.
- `PART_Popup.Child` là một `System.Windows.Controls.Calendar` khác với `Calendar` nhúng trong template (`PopupChildIsTemplateCalendar = false`).
- Calendar thực tế không có các setter/template Front Office; popup hiển thị calendar mặc định với header tiếng Anh kiểu `July 2026`, weekday `S M T W T F S` và nút điều hướng tam giác.
- Ảnh hiện trạng: [stay-profile-calendar-before.png](stay-profile-calendar-before.png).

Đây phù hợp với implementation của WPF `DatePicker`: control khởi tạo calendar riêng và liên kết nó với thuộc tính `CalendarStyle`, sau đó đặt calendar đó làm con của popup. Xem [DatePicker source](https://www.dotnetframework.org/default.aspx/4%400/4%400/DEVDIV_TFS/Dev10/Releases/RTMRel/wpf/src/Framework/System/Windows/Controls/DatePicker%40cs/1305600/DatePicker%40cs).

## Thay đổi đã thực hiện

### `ASMILE.BO.App/Views/StayProfileWindow.xaml`

- Thêm `CalendarStyle="{StaticResource FoModernCalendarStyle}"` và `FirstDayOfWeek="Monday"` vào style dùng chung `ModDatePicker`.
- Xóa `Border`/`Calendar` nhúng trong `PART_Popup`; để WPF DatePicker tự gán calendar nội bộ vào popup.

### `ASMILE.BO.App/Themes/FrontOfficeDesignSystem.xaml`

- Giữ nguyên resource style dùng chung và hoàn thiện contract template của WPF Calendar:
  - `FoModernCalendarStyle` truyền `CalendarItemStyle`, `CalendarDayButtonStyle`, ngôn ngữ `vi-VN`, ngày đầu tuần thứ Hai, nền/viền và shadow.
  - `FoCalendarItemStyle` có các part bắt buộc `PART_CalendarItem`, `PART_MonthView`, `PART_YearView`, `PART_PreviousButton`, `PART_HeaderButton`, `PART_NextButton`, `PART_DisabledVisual`.
  - Month view có 7 cột × 7 hàng kích thước cố định để các `CalendarDayButton` được layout đúng; year view có 4 cột × 3 hàng.

## Luồng style sau khi sửa

```text
ModDatePicker.CalendarStyle
        ↓
DatePicker private Calendar (WPF)
        ↓  PART_Popup.Child
FoModernCalendarStyle
        ↓
FoCalendarItemStyle → FoCalendarDayButtonStyle
```

## Kiểm chứng runtime sau khi sửa

Probe mở cả hai DatePicker bộ lọc và đọc trực tiếp `PART_Popup.Child`:

- Cả hai popup đều chứa `System.Windows.Controls.Calendar` nội bộ với `PopupChildTemplateIsResource = true`.
- Cả hai đều nhận các setter `CalendarDayButtonStyle`, `CalendarItemStyle`, `Language`, `FirstDayOfWeek`, `Background`, `BorderBrush`, `BorderThickness`, `Effect`, `Template`.
- Ngôn ngữ runtime là `vi-vn`, `FirstDayOfWeek` là `Monday`.
- Popup thực tế có kích thước `300 × 448`, month view `266 × 266`; footer hiển thị `Hôm nay`, `Ngày mai`, `Xóa ngày`.
- Ảnh sau sửa: [stay-profile-calendar-after.png](stay-profile-calendar-after.png).

## Build và encoding

- `dotnet build .\ASMILE.BO.App\ASMILE.BO.App.csproj --no-restore`: 0 warnings, 0 errors.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-xaml-encoding.ps1`: pass, không phát hiện mojibake trong XAML.
- Không thay đổi backend, database, view-model hoặc logic nghiệp vụ; chỉ sửa activation path/template của calendar và bổ sung bằng chứng kiểm thử.
