# ALACARTE SETTINGS REALDATA REPORT

## Mục tiêu

Chuyển các tab `Cài đặt` của module `Nhà hàng À La Carte` từ form mockup sang dữ liệu thật, theo layout chung Win 11 và dùng đúng service hiện có.

## Phạm vi đã làm

- Tab `Máy in / Bếp bar`
  - Đổi ô nhập tự do sang `ComboBox` dữ liệu thật từ danh sách máy in Windows.
  - Chuẩn hóa chọn khổ giấy qua danh sách dùng chung.
  - Giữ các nút test in bếp, bar, phiếu thanh toán và lưu cấu hình.

- Tab `Nhóm món`
  - Bind form tạo/sửa nhóm món vào `MenuCatalogService`.
  - Thêm checklist `Nhóm được phép bán trong module`.
  - Lưu thật danh sách nhóm được phép bán vào file cấu hình `Data/alacarte_restaurant_settings.json`.
  - DataGrid đã có sửa/ngừng dùng nhóm thật.

- Tab `Món bán`
  - Bind form tạo/sửa món bán vào `MenuCatalogService`.
  - Bổ sung phân loại bán hàng: `Đồ ăn`, `Rượu`, `Đồ uống`, `Dịch vụ`, `Khác`.
  - Lưu thật override loại món vào file cấu hình cục bộ.
  - DataGrid đã có sửa/ngừng bán món thật.

- Tab `Dịch vụ bổ sung`
  - Bind form tạo/sửa dịch vụ bổ sung vào `IAlacarteRestaurantService.SaveExtraServiceAsync(...)`.
  - Hiển thị và lưu thật loại dịch vụ, VAT, giá đã gồm VAT, trạng thái bán.
  - Nút `Ngừng` dùng soft-delete bằng `IsActive = false`.

## Dữ liệu thật đã dùng

- `IMenuCatalogService.GetCategoriesAsync`
- `IMenuCatalogService.CreateOrUpdateCategoryAsync`
- `IMenuCatalogService.DeleteOrDeactivateCategoryAsync`
- `IMenuCatalogService.GetMenuItemsAsync`
- `IMenuCatalogService.CreateOrUpdateMenuItemAsync`
- `IMenuCatalogService.DeleteOrDeactivateMenuItemAsync`
- `IAlacarteRestaurantService.GetExtraServicesAsync`
- `IAlacarteRestaurantService.SaveExtraServiceAsync`

## Lưu cấu hình cục bộ đã bổ sung

File: `Data/alacarte_restaurant_settings.json`

- `AllowedMenuCategoryIds`
- `MenuItemTypes`
- giữ nguyên các cấu hình cũ về tên nhà hàng, thông tin in, khổ giấy, khu vực dùng tay

## Ảnh hưởng nghiệp vụ

- Bộ lọc món của À La Carte giờ sẽ chỉ hiện các nhóm được phép bán.
- Phân loại `Rượu` và `Đồ uống` trong đơn mở sẽ ưu tiên cấu hình thật thay vì chỉ đoán theo tên nhóm.

## Ghi chú

- Build đầy đủ của `ASMILE.BO.App` cần đóng tiến trình app đang chạy trước khi copy lại file output.
- Sau khi restart app cần test lại trực tiếp tab `Cài đặt` của module À La Carte.

## Chuẩn hóa Win 11

- Các tab `Thanh toán`, `Đặt bàn / Đặt cọc`, `Hóa đơn / Chứng từ`, `Phân quyền`, `Đồng bộ / Tích hợp`, `Thông tin nhà hàng` đã được chuyển sang layout card 2 cột rõ ràng hơn.
- Đồng bộ lại khoảng cách, tiêu đề phụ, border, shadow nhẹ và nút lưu theo style chung của app.
- `Nhóm phục vụ` đã đổi các badge tròn dài sang badge bo góc nhẹ và dùng lại `EnterpriseModuleTextBox` / `EnterpriseModuleComboBox` để tránh lệch style.
