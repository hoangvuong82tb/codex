# Banquet Full Workspace Mock Report

## Mục tiêu

Xóa mock giao diện cũ của module `Nhà hàng Tiệc` và làm lại theo spec workspace ERP tổng thể từ file yêu cầu đính kèm.

## Phạm vi thực hiện

- Thay toàn bộ:
  - `D:\ASMILE-BO\ASMILE.BO.App\Views\BanquetRestaurantView.xaml`
- Dựng lại mock WPF theo hướng:
  - sidebar ERP đầy đủ module,
  - top toolbar,
  - dashboard KPI,
  - lịch tiệc dạng calendar mock,
  - master-detail đơn đặt tiệc,
  - progress pipeline,
  - khu menu builder,
  - khu setup bàn / floor planner,
  - khu timeline / phòng sảnh / điều phối / BI.

## Ghi chú kỹ thuật

- Spec có nêu `React + TypeScript + Material UI`, nhưng codebase hiện tại là WPF desktop app.
- Vì người dùng yêu cầu mock để chốt giao diện trước, nên lần này triển khai mock trực tiếp bằng XAML để app chạy được ngay trong hệ hiện tại.
- Chưa nối dữ liệu thật và chưa chia component logic vì mục tiêu hiện tại là chốt layout/UX.

## Kiểm tra

- `dotnet build D:\ASMILE-BO\ASMILE.BO.sln -c Debug` thành công.
- Không còn lỗi XAML build.

## Hướng tiếp theo

Sau khi mock này được duyệt, có thể tách tiếp thành:

- Dashboard workspace
- Calendar workspace
- Orders master-detail workspace
- Menu builder workspace
- Floor planner workspace
- Resource coordination workspace
- Finance / reporting workspace

và sau đó mới nối dữ liệu/command thật theo từng khối.
