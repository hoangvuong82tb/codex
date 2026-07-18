# ALACARTE_SETTINGS_AUDIT.md

## Audit toàn bộ 13 tab Settings — Module Nhà hàng À La Carte

Ngày: 24/06/2026

---

# KẾT QUẢ TỔNG QUAN

**13 tab Settings. 2 REAL. 2 PARTIAL. 9 MOCK.**

---

# BẢNG TÓM TẮT

| # | Tab | Trạng thái | DB Entity | Service | UI Command | % | Đề xuất |
|---|---|---|---|---|---|---|---|
| 0 | Khu vực / Phòng ăn | **PARTIAL** | DiningRoom | REAL | Wired | 90% | GIỮ LẠI |
| 1 | Bàn ăn | **REAL** | DiningTable | REAL | Wired | 100% | GIỮ LẠI |
| 2 | Máy in / Bếp bar | **MOCK** | Không có | Không có | Code-behind | 0% | TÁI SỬ DỤNG |
| 3 | Nhóm món | **MOCK** | MenuCategory (không dùng) | Không có | **Không có** | 0% | XÓA BỎ |
| 4 | Món bán | **MOCK** | MenuItem (không dùng) | Không có | **Không có** | 0% | XÓA BỎ |
| 5 | Dịch vụ bổ sung | **PARTIAL** | RestaurantExtraService | REAL | **Không có** | 40% | LÀM TIẾP |
| 6 | Thanh toán | **MOCK** | Không có | Không có | In-memory | 0% | LÀM TIẾP |
| 7 | Đặt bàn / Đặt cọc | **MOCK** | Không có | Không có | In-memory | 0% | LÀM TIẾP |
| 8 | Hóa đơn / Chứng từ | **MOCK** | Không có | Không có | In-memory | 0% | LÀM TIẾP |
| 9 | Phân quyền | **MOCK** | AppRole/Permission (không dùng) | Không có | In-memory | 0% | XÓA BỎ |
| 10 | Đồng bộ / Tích hợp | **MOCK** | Không có | Không có | Display-only | 0% | XÓA BỎ |
| 11 | Thông tin nhà hàng | **MOCK** | Company/Branch (không dùng) | File JSON | Wired (file) | 10% | LÀM TIẾP |
| 12 | Nhóm phục vụ | **REAL** | RestaurantServingGroup | REAL | Wired | 85% | GIỮ LẠI |

---

# CHI TIẾT TỪNG TAB

---

## Tab 0 — Khu vực / Phòng ăn (SettingsSubTabIndex=0)

**Trạng thái: PARTIAL — 90%**

### Khu vực

- **Dữ liệu**: `List<string> _manualDiningAreas` — lưu vào file JSON local (`RestaurantSettings`)
- **Không có Entity riêng** cho DiningArea. Khái niệm "khu vực" được suy ra từ `DiningRoom.RoomType`
- **Nút Lưu / Mới**: gọi `SaveSettingDiningAreaCommand`, `NewSettingDiningAreaCommand` → thao tác trên list in-memory rồi ghi file JSON

### Phòng ăn

- **REAL**: `SaveSettingAreaCommand` → `_service.SaveRoomAsync()` → EF Core `_dbContext.DiningRooms`
- **REAL**: `DeleteSettingAreaAsync()` → `_service.DeleteRoomAsync()` → kiểm tra ràng buộc (bàn, reservation, order)
- **DataGrid**: `SettingDiningAreas` (ObservableCollection in-memory), `FilteredSettingRooms` (từ DB)

### File liên quan

| Lớp | File |
|---|---|
| ViewModel | `RestaurantViewModels.cs` — `SaveSettingDiningAreaCommand`, `SaveSettingAreaCommand` |
| Service | `RestaurantServices.cs` — `SaveRoomAsync()`, `DeleteRoomAsync()`, `GetRoomsAsync()` |
| Entity | `RestaurantEntities.cs` — `DiningRoom` |
| DbContext | `AppDbContext.cs` — `DbSet<DiningRoom> DiningRooms` |

### Đề xuất: **GIỮ LẠI**

- Chuyển quản lý khu vực từ file JSON sang DB (thêm entity `DiningArea` hoặc dùng `RoomType` field)
- Hoàn thiện: 90% → 100%

---

## Tab 1 — Bàn ăn (SettingsSubTabIndex=1)

**Trạng thái: REAL — 100%**

### Hoàn chỉnh

- **CRUD đầy đủ**: `SaveSettingTableCommand`, `EditSettingTableCommand`, `DeleteSettingTableCommand`, `NewSettingTableCommand`
- **`GenerateSettingTablesCommand`**: tạo nhanh N bàn cho phòng
- **Service**: `SaveTableAsync()` — validate trùng code, kiểm tra sức chứa phòng
- **DB**: `DiningTable` entity với `DbSet<DiningTable> DiningTables`

### File liên quan

| Lớp | File |
|---|---|
| ViewModel | `RestaurantViewModels.cs` — `SaveSettingTableCommand`, `EditSettingTableCommand`, `DeleteSettingTableCommand` |
| Service | `RestaurantServices.cs` — `SaveTableAsync()`, `DeleteTableAsync()`, `GetTablesAsync()` |
| Entity | `RestaurantEntities.cs` — `DiningTable` |
| DbContext | `AppDbContext.cs` — `DbSet<DiningTable> DiningTables` |

### Đề xuất: **GIỮ LẠI**

---

## Tab 2 — Máy in / Bếp bar (SettingsSubTabIndex=2)

**Trạng thái: MOCK — 0%**

### Phân tích

- `SettingPrinters` được populate từ `SyncSettingPrintersFromConfig()` — tạo 3 entry cứng: "Máy in bếp", "Máy in bar", "Máy in hóa đơn"
- **Không có Entity `Printer`** — không có DbSet, không có bảng DB
- **Không có Service method** nào cho printer
- Lưu cấu hình qua file JSON (`RestaurantInfoSettings`)
- Nút Test Printer gọi code-behind (`TestKitchenPrinterButton_Click`) → gọi ViewModel command test in

### File liên quan

| Lớp | File |
|---|---|
| ViewModel | `RestaurantViewModels.cs` — `PreviewKitchenSampleTicketCommand`, `PrintKitchenSampleTicketCommand` |
| Service | **Không có** |
| Entity | **Không có** |
| DbContext | **Không có** |

### Đề xuất: **TÁI SỬ DỤNG**

- Module POS Bữa sáng & Cà phê đã có Kitchen Ticket in — nên dùng chung cơ chế
- Nếu cần quản lý printer riêng, tạo entity `RestaurantPrinter` với CRUD

---

## Tab 3 — Nhóm món (SettingsSubTabIndex=3)

**Trạng thái: MOCK — 0%**

### Phân tích

- `SettingMenuGroups` được populate từ `SeedDefaultSettings()` với 10 entry cứng ("Khai vị", "Món chính", "Hải sản"...)
- **Form KHÔNG có Command binding**: nút "Lưu" và "Mới" không có `Command=""`, textbox không có `Text=""` binding
- **Không gọi service nào**
- **Entity `MenuCategory` đã có** (`DbSet<MenuCategory> MenuCategories`) nhưng tab này không dùng

### Trùng lặp module

Module **MenuRecipe** (`MenuRecipeViewModel`) đã quản lý đầy đủ:
- CRUD `MenuCategory`
- CRUD `MenuItem`
- Định lượng, giá vốn
- Hình ảnh món

### File liên quan

| Lớp | File |
|---|---|
| ViewModel | `RestaurantViewModels.cs` — `SettingMenuGroups` (hardcoded) |
| Service | **Không có** |
| Entity | `MenuCategory` (có nhưng không dùng) |
| Module trùng | `MenuRecipeViewModel.cs` |

### Đề xuất: **XÓA BỎ**

- Tab này là mockup hoàn toàn
- Điều hướng người dùng sang module **MenuRecipe** để quản lý nhóm món

---

## Tab 4 — Món bán (SettingsSubTabIndex=4)

**Trạng thái: MOCK — 0%**

### Phân tích

- `SettingMenuItems` được populate từ `SeedDefaultSettings()` với 7 entry cứng
- **Form KHÔNG có Command binding**: giống hệt Tab 3 — nút và textbox không được wire
- **Không gọi service nào**
- **Entity `MenuItem` đã có** với `DbSet<MenuItem> MenuItems` nhưng không dùng

### Trùng lặp module

Module **MenuRecipe** đã quản lý toàn bộ MenuItem.

### File liên quan

| Lớp | File |
|---|---|
| ViewModel | `RestaurantViewModels.cs` — `SettingMenuItems` (hardcoded) |
| Service | **Không có** |
| Entity | `MenuItem` (có nhưng không dùng) |
| Module trùng | `MenuRecipeViewModel.cs` |

### Đề xuất: **XÓA BỎ**

- Điều hướng sang module **MenuRecipe**

---

## Tab 5 — Dịch vụ bổ sung (SettingsSubTabIndex=5)

**Trạng thái: PARTIAL — 40%**

### Phân tích

- **DataGrid hiển thị dữ liệu THẬT**: `SettingExtServices` được populate từ `AlacarteServices`, load qua `_service.GetExtraServicesAsync()` — query DB thật
- **Form KHÔNG có Command binding**: nút "Lưu" và "Mới" không được wire trong XAML
- **Service đã sẵn sàng**: `SaveExtraServiceAsync()` có validation, CRUD đầy đủ
- **Entity**: `RestaurantExtraService` với `DbSet<RestaurantExtraService>`
- **Seed data**: 11 dịch vụ mặc định (LED, MC, Ca sĩ, Âm thanh, Ánh sáng, Trang trí, Hoa tươi, Máy chiếu, Phí phòng VIP, Phí phục vụ, Photobooth)

### File liên quan

| Lớp | File |
|---|---|
| ViewModel | `RestaurantViewModels.cs` — `SettingExtServices` |
| Service | `RestaurantServices.cs` — `SaveExtraServiceAsync()`, `GetExtraServicesAsync()` |
| Entity | `RestaurantEntities.cs` — `RestaurantExtraService` |
| DbContext | `AppDbContext.cs` — `DbSet<RestaurantExtraService>` |

### Đề xuất: **LÀM TIẾP**

- Wire Command binding trong XAML cho nút Lưu / Mới
- Hoàn thiện: 40% → 100%

---

## Tab 6 — Thanh toán (SettingsSubTabIndex=6)

**Trạng thái: MOCK — 0%**

### Phân tích

- `SettingPaymentMethods` populate từ `SeedDefaultSettings()` với 6 entry: "Tiền mặt", "Chuyển khoản", "Thẻ tín dụng", "Ví điện tử", "Công nợ", "Ghi phòng"
- Các setting (`DefaultServiceChargeRate`, `DefaultVatRate`, `DefaultDepositRate`...) là `[ObservableProperty]` trong ViewModel — giá trị mặc định từ code, không load từ DB
- **Không có Entity** cho payment settings
- **Không lưu DB**: giá trị tồn tại trong RAM, mất khi tắt app

### Đề xuất: **LÀM TIẾP**

- Lưu vào `BranchSettings` table (đã có trong hệ thống, lưu JSON key-value)
- Payment methods có thể giữ nguyên (tham khảo), settings cần persist

---

## Tab 7 — Đặt bàn / Đặt cọc (SettingsSubTabIndex=7)

**Trạng thái: MOCK — 0%**

### Phân tích

- Tất cả setting là `[ObservableProperty]` trong ViewModel: `DefaultHoldMinutes=15`, `AlertBeforeMinutes=30`, `DefaultDepositRate=30`, `SettingAutoCancelMinutes`, `SettingAllowMultiDeposit`, `SettingAutoCancelNoShow`
- **Không có Entity** — không lưu DB

### Đề xuất: **LÀM TIẾP**

- Lưu vào `BranchSettings`

---

## Tab 8 — Hóa đơn / Chứng từ (SettingsSubTabIndex=8)

**Trạng thái: MOCK — 0%**

### Phân tích

- Setting: `SettingPrintCopies`, `SettingPrintLogo`, `SettingPrintVat`
- Tương tự Tab 7 — in-memory, không lưu DB

### Đề xuất: **LÀM TIẾP**

- Lưu vào `BranchSettings`

---

## Tab 9 — Phân quyền (SettingsSubTabIndex=9)

**Trạng thái: MOCK — 0%**

### Phân tích

- `SettingPermissions` populate từ `SeedDefaultSettings()` với 9 entry cứng: "Quản lý phòng", "Quản lý bàn", "Tạo báo giá"...
- Checkbox bind `IsGranted` — không lưu DB, không có service

### Trùng lặp module

Hệ thống **đã có** hệ thống phân quyền đầy đủ:
- `AppRole` — vai trò (Admin, Manager, Staff...)
- `AppPermission` — quyền (mã quyền, mô tả)
- `AppRolePermission` — quyền theo vai trò
- `AppUserPermission` — quyền theo user
- `AppDataPermission` — quyền dữ liệu (theo chi nhánh, công ty)

Tất cả đã có Entity + DbSet + Migration.

### Đề xuất: **XÓA BỎ**

- Dùng hệ thống `AppRole`/`AppPermission` có sẵn
- Nếu cần UI, tạo một màn hình Permission Management riêng, không nhét vào Settings ALC

---

## Tab 10 — Đồng bộ / Tích hợp (SettingsSubTabIndex=10)

**Trạng thái: MOCK — 0%**

### Phân tích

- `SettingIntegrations` populate từ `SeedDefaultSettings()` với 5 entry: "Kết nối máy in bếp", "Đồng bộ kho", "Đồng bộ kế toán", "Email/SMS", "Đồng bộ đặt bàn online"
- Chỉ hiển thị trạng thái `IsReady` (true/false) — không có action

### Đề xuất: **XÓA BỎ**

- Nếu cần tích hợp thật → làm module Integration riêng
- Tab này là placeholder, không có giá trị

---

## Tab 11 — Thông tin nhà hàng (SettingsSubTabIndex=11)

**Trạng thái: MOCK — 10%**

### Phân tích

- `RestaurantName`, `RestaurantAddress`, `RestaurantPhone`, `RestaurantEmail`, `RestaurantTaxCode`, `RestaurantLogoPath` — lưu vào **file JSON local** (`RestaurantInfoSettings`)
- `SaveSettingsCommand` gọi `SaveRestaurantSettings()` → `File.WriteAllText()`
- `LoadRestaurantSettings()` → `File.ReadAllText()`

### Trùng lặp module

Hệ thống **đã có**:
- `Company` entity: Name, Address, TaxCode, Phone, Email
- `Branch` entity: Name, Address, Phone

### Đề xuất: **LÀM TIẾP**

- Load từ `Company`/`Branch` entities thay vì file JSON
- Nếu cần thông tin riêng cho nhà hàng (khác công ty), tạo entity `RestaurantInfo`

---

## Tab 12 — Nhóm phục vụ (SettingsSubTabIndex=12)

**Trạng thái: REAL — 85%**

### Phân tích

- **CRUD đầy đủ**: `NewServingGroupCommand`, `SaveServingGroupCommand`, `DeleteServingGroupCommand`, `SelectServingGroupEditorCommand`
- **Service**: `SaveServingGroupAsync()` — validate, phân biệt system group (không cho xóa), cập nhật `RestaurantServingGroup` và `RestaurantServingGroupTable`
- **Entity**: `RestaurantServingGroup` (4 system group: ALL-SERVING, STANDARD, ODD, WAITING) + `RestaurantServingGroupTable`
- **DbSet**: `DbSet<RestaurantServingGroup>`, `DbSet<RestaurantServingGroupTable>`
- **Hạn chế**: Yêu cầu đang có `SelectedOrder` — nếu chưa chọn đơn thì tab không hoạt động

### File liên quan

| Lớp | File |
|---|---|
| ViewModel | `RestaurantViewModels.cs` — `NewServingGroupCommand`, `SaveServingGroupCommand` |
| Service | `RestaurantServices.cs` — `SaveServingGroupAsync()`, `DeleteServingGroupAsync()` |
| Entity | `RestaurantEntities.cs` — `RestaurantServingGroup`, `RestaurantServingGroupTable` |
| DbContext | `AppDbContext.cs` — `DbSet<RestaurantServingGroup>`, `DbSet<RestaurantServingGroupTable>` |

### Đề xuất: **GIỮ LẠI**

- Cho phép quản lý serving group ngay cả khi không có order đang chọn

---

# PHÂN TÍCH ĐẶC BIỆT

## A. PHÂN QUYỀN (Tab 9)

- **Không dùng `AppRole`/`AppPermission` có sẵn**
- Mockup hoàn toàn — 9 dòng cứng, checkbox không lưu
- **Không có User Mapping** — không biết ai đang có quyền gì
- **Kết luận**: XÓA BỎ tab này. Dùng hệ thống phân quyền chung của app.

## B. NHÓM MÓN (Tab 3)

- **Không dùng chung `MenuCategory`** — mặc dù entity đã có
- **Không liên kết với MenuRecipe module**
- **Kết luận**: XÓA BỎ. Dùng MenuRecipe module.

## C. MÓN BÁN (Tab 4)

- **Không đọc `MenuItem` thật** từ DB
- **Không đồng bộ với định lượng**
- **Kết luận**: XÓA BỎ. Dùng MenuRecipe module.

## D. DỊCH VỤ BỔ SUNG (Tab 5)

- **Có entity riêng** `RestaurantExtraService` — độc lập với MenuItem
- **Có CRUD service thật** — nhưng UI chưa wire
- **Kết luận**: LÀM TIẾP. Service đã sẵn sàng, chỉ cần wire UI.

## E. MÁY IN / BẾP BAR (Tab 2)

- **Không có Kitchen Printer Routing entity**
- **Không có cấu hình máy in trong DB**
- Cấu hình lưu file JSON, không quản lý được nhiều máy in
- Module POS Bữa sáng có in bếp — kiểm tra xem có dùng chung được không
- **Kết luận**: TÁI SỬ DỤNG cơ chế in từ POS, hoặc tạo entity `RestaurantPrinter`

---

# THỐNG KÊ

| Trạng thái | Số lượng | Tab |
|---|---|---|
| **REAL** | 2 | Bàn ăn (1), Nhóm phục vụ (12) |
| **PARTIAL** | 2 | Khu vực/Phòng ăn (0), Dịch vụ bổ sung (5) |
| **MOCK** | 9 | Máy in (2), Nhóm món (3), Món bán (4), Thanh toán (6), Đặt bàn (7), Hóa đơn (8), Phân quyền (9), Đồng bộ (10), Thông tin NH (11) |

**Tỉ lệ hoàn thiện trung bình: ~25%**

---

# KHUYẾN NGHỊ HÀNH ĐỘNG

## P0 — Làm ngay

1. **Wire Tab 5 — Dịch vụ bổ sung**: Service đã có, chỉ cần thêm Command binding trong XAML. Chi phí thấp, giá trị cao.
2. **Wire Tab 0 — Khu vực**: Chuyển từ file JSON sang DB (dùng `RoomType` hoặc entity mới)

## P1 — Làm sau

3. **Tab 6-7-8 — Lưu settings vào `BranchSettings`**: Tất cả setting thanh toán, đặt cọc, hóa đơn nên persist trong DB
4. **Tab 11 — Load thông tin từ `Company`/`Branch`**: Thay vì file JSON
5. **Tab 2 — Quản lý máy in**: Tạo entity + CRUD

## P2 — Xóa/Thay thế

6. **Tab 3 — Nhóm món**: XÓA, dùng MenuRecipe
7. **Tab 4 — Món bán**: XÓA, dùng MenuRecipe
8. **Tab 9 — Phân quyền**: XÓA, dùng AppRole/Permission
9. **Tab 10 — Đồng bộ**: XÓA (placeholder)
