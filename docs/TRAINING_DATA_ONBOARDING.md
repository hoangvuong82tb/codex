# Onboarding dữ liệu mẫu cho nhà hàng/khách sạn

## Định vị sản phẩm
`training-data` phục vụ onboarding dữ liệu demo cho phần mềm kế toán ngành **nhà hàng** và **khách sạn**.

## Chọn profile nào?
- `RestaurantDemo`: demo vận hành nhà hàng (kho bếp/bar, bán hàng, giá vốn, VAT).
- `HotelDemo`: demo vận hành khách sạn (doanh thu lưu trú/dịch vụ, công nợ khách/OTA, VAT).
- `RestaurantHotelFullDemo`: demo tổng thể cho khách sạn có nhà hàng.
- `FullTraining`: alias tương thích ngược, được map sang `RestaurantHotelFullDemo`.

## Production safety rules
- Full reset (`ResetToBlank`/`ClearBusinessData`/`DeleteTrainingData`) yêu cầu xác nhận: `RESET ALL BUSINESS DATA`.
- `ResetTrainingOnly` yêu cầu xác nhận: `RESET TRAINING DATA`.
- Full reset bị chặn khi có `HasRealDataRisk=true` và chưa bật `AllowResetWithRealDataRisk=true`.
- Luôn chạy preflight trước thao tác destructive.

## Xử lý lỗi `Invalid object name 'TrainingDataMarkers'`
- Khi DB chưa apply migration marker, service không để UI crash.
- Màn hình hiển thị cảnh báo thân thiện: cần cập nhật migration trước khi dùng chức năng phụ thuộc marker.
- Log kỹ thuật vẫn được ghi qua audit (`MarkerTableProbeFailed` / blocked reason).
- Cần chạy migration chuẩn của hệ thống (đã có `AddTrainingDataMarkers`), không tạo migration repair phá dữ liệu.

## Checklist demo cho sales/training
1. Chọn đúng mô hình: Nhà hàng / Khách sạn / Khách sạn + Nhà hàng.
2. Xem preflight summary (DB, environment, risk level, số chứng từ/journal).
3. Xem preview dữ liệu sẽ tạo.
4. Chạy seed dữ liệu demo.
5. Kiểm tra nhanh các màn: Dashboard, Sổ cái, Kho, Công nợ, VAT, Quỹ/Ngân hàng.
6. Sau demo, dùng `ResetTrainingOnly` để dọn dữ liệu training theo marker.
