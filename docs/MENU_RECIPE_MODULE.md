# Món ăn & Định lượng (`menu-recipe`)

## Mục tiêu
Module nền để quản lý danh mục món, định lượng nguyên vật liệu (recipe/BOM), và xem trước giá vốn dự kiến trước khi triển khai POS.

## Luồng chính
1. Tạo/cập nhật món (mã, tên, nhóm, đơn vị bán, giá bán, VAT, trạng thái).
2. Khai báo định lượng cho món theo dòng nguyên liệu.
3. Lưu recipe và kiểm tra hợp lệ.
4. Xem trước giá vốn + biên lợi nhuận + cảnh báo.
5. Ngưng bán món (soft deactivate).

## Định lượng
- Mỗi dòng có thể trỏ tới:
  - `ItemId` (nguyên vật liệu trực tiếp), hoặc
  - `ChildMenuItemId` (skeleton cho combo thành phẩm).
- Có `Quantity`, `LossRate`, `DefaultWarehouseId`, `IsRequired`, `Note`.
- Validation:
  - `Quantity > 0`
  - `LossRate` trong `0..1`
  - đúng một trong `ItemId` hoặc `ChildMenuItemId`.

## Tính giá vốn dự kiến
- Đơn giá ưu tiên:
  1. `InventoryCostLayer.UnitCost` (nếu có)
  2. `StockBalance.AvgPrice`
  3. mặc định `0` và sinh cảnh báo thiếu giá vốn.
- Không tạo bút toán, không tạo chứng từ kho, không post kế toán.
- Cảnh báo:
  - chưa có định lượng
  - nguyên liệu thiếu giá vốn
  - tồn kho không đủ
  - giá bán thấp hơn giá vốn.

## Tích hợp kho
- Lookup nguyên vật liệu lấy từ `Items`.
- Lookup kho lấy từ `Warehouses`.
- Chỉ đọc số dư tồn (`StockBalance`) và giá vốn (`InventoryCostLayer`) để preview.
- Không trừ kho trong phase này.

## Quan hệ với POS phase sau
- POS sẽ tái sử dụng `MenuItem` + `MenuItemRecipe` để tính tiêu hao/xuất kho.
- Phase hiện tại không xử lý order/payment/accounting của bán hàng.
