# ASMILE-BO — Dự án ERP Nhà hàng / Khách sạn

## Kiến trúc

```
ASMILE.BO.sln
├── ASMILE.BO.App          — WPF UI (Views, ViewModels, Controls, Themes)
├── ASMILE.BO.Application   — Interfaces, DTOs, App Services
├── ASMILE.BO.Core          — Domain entities
├── ASMILE.BO.Infrastructure — EF Core, Services implementation, Migrations
├── ASMILE.BO.Shared        — Shared utilities
└── ASMILE.BO.Tests.*       — Unit & Integration tests
```

## Module Món ăn & Định lượng (MenuRecipe)

### File chính

| File | Vai trò |
|---|---|
| `Views/MenuRecipeView.xaml` | Giao diện 4 cột: sidebar + nhóm món + chi tiết + giá vốn |
| `ViewModels/MenuRecipeViewModel.cs` | ViewModel chính + các class phụ (`MenuItemCardState`, `RecipeLineEditor`) |
| `Application/Interfaces/IMenuCatalogService.cs` | CRUD nhóm món, món ăn, lookup nguyên liệu |
| `Application/Interfaces/IMenuRecipeService.cs` | Định lượng, tính giá vốn, validation |
| `Infrastructure/Services/MenuCatalogService.cs` | Implementation — EF Core |
| `Infrastructure/Services/MenuRecipeService.cs` | Implementation — EF Core + cost calculation |
| `Application/DTOs/BreakfastCafeSalesDtos.cs` | Tất cả DTO của Menu/MenuRecipe trong file này |

### DTOs quan trọng

- `CreateMenuItemRequest` — Tạo/sửa món ăn. Không có `Description` riêng, chỉ có `Note`.
- `MenuRecipeLineDto` — Dòng định lượng. `LossRate` là decimal (0..1), không phải %.
- `MenuCostPreviewDto` — Kết quả tính giá vốn. `EstimatedCost`, `MarginAmount`, `MarginRate`, `Warnings`.
- `IngredientLookupDto` — Nguyên liệu cho combo. `ReferenceUnitCost` là giá vốn tham khảo.

### Layout MenuRecipeView (4 cột)

```
[52px sidebar] [270px category] [* main content] [290px right]
```

1. **Sidebar**: Icon module navigation (Segoe MDL2 Assets). Active = `SidebarActiveBtn` style.
2. **Category panel**: Tìm nhóm món + chip lọc (FilterChips) + danh sách món (FilteredMenuCards).
3. **Main content**: Header (tên + meta + status pill + 4 buttons) + TabControl (underline xanh #2563EB).
4. **Right panel**: Cost card + Warnings card.

### Tab order + SelectedWorkspaceTab

- Index 0: Thông tin chung
- Index 1: Định lượng (được chọn mặc định khi click món)
- Index 2: Giá vốn
- Index 3: Lịch sử (disabled)

### DataGrid định lượng

Dùng `EnterpriseModuleDataGrid` style với `IsReadOnly="False"`. Các cột editable (SL định mức, Hao hụt %, Ghi chú) dùng `DataGridTemplateColumn` + `CellEditingTemplate`. `EffectiveQuantity` tự động tính nhờ `[NotifyPropertyChangedFor]` trên `Quantity` và `LossRatePercent`.

Action buttons (↑↓✕) bind RelativeSource lên UserControl DataContext.

### Commands quan trọng

| Command | Chức năng |
|---|---|
| `ToggleActiveCommand` | Bật/tắt trạng thái kinh doanh |
| `SaveMenuItemCommand` | Lưu thông tin món |
| `SaveAndCalculateCostCommand` | Lưu + tính giá vốn |
| `AddRecipeLineCommand` | Thêm dòng định lượng |
| `RemoveRecipeLineCommand` | Xóa dòng định lượng |
| `MoveRecipeLineUp/DownCommand` | Di chuyển dòng |
| `SaveRecipeCommand` | Lưu định lượng lên server |
| `CalculateCostCommand` | Tính giá vốn từ service |
| `BootstrapCategoriesCommand` | Tạo nhóm món mặc định |

### Quy tắc code

- ViewModel dùng `[ObservableProperty]`, `[RelayCommand]` (CommunityToolkit.Mvvm).
- **WARNING**: Command parameter type phải khớp với XAML. XAML `CommandParameter="0"` là string, không phải int. Luôn dùng `string` cho command parameter từ XAML, parse trong method.
- Sự kiện thay đổi View → ViewModel dùng `partial void On<Property>Changed`.
- `NotifyComputedState()` notify tất cả computed properties bằng tay.
- RecipeLineEditor field `LossRatePercent` là % (0-100), DTO `LossRate` là decimal (0-1). Có conversion ở `ToEditor`/`ToDtoLine`.
- Mọi text UI dùng tiếng Việt UTF-8, font Segoe UI. Dùng Unicode escapes (`&#xHHHH;`) trong XAML để tránh mojibake.

### Sidebar navigation

Sidebar có 9 section với style `SBtn0`-`SBtn8`, mỗi style có `DataTrigger` cho `SelectedSection` tương ứng.
Command dùng `NavigateSectionCommand` với `CommandParameter` là string (số section).
KHÔNG dùng `Style.Triggers` để đổi `Style` property (WPF không support).
KHÔNG dùng `int` parameter trong `[RelayCommand]` nếu XAML dùng `CommandParameter="..."`.

### Style system

Global themes trong `App.Themes/`. App merge ~35 resource dictionaries.
- `EnterpriseModule*` trong `EnterpriseControls.xaml` — dùng cho module UI.
- `Accounting*` trong `AccountingModule.xaml` — base styles (panel, button, tab).
- `RecipeTabButton` — local style trong `MenuRecipeView.xaml` với blue underline.

### Theme colors

- Nền chính: `#F0F4F8`
- Panel trắng: `White` + border `#DDE7F3` + corner 14 + soft shadow
- Button primary: orange `#F2994A`
- Accent blue: `#2563EB`
- Text primary: `#0F172A`
- Text muted: `#64748B`, `#94A3B8`
- Border: `#E5E7EB`, `#CBD5E1`
- Warning: `#EA580C`
- Success: `#16A34A`, `#22C55E`
- Danger: `#DC2626`, `#EF4444`

### Build & Run

```bash
dotnet build ASMILE.BO.sln
# App exe: ASMILE.BO.App/bin/Debug/net8.0-windows/ASMILE.BO.App.exe
# Cần SQL Server — migration tự động chạy khi start
```

### Hạn chế / TODO

- `CreateMenuItemRequest` thiếu `Description` field — cần thêm entity + migration
- Chưa có upload ảnh món ăn
- Chưa có history/log service
- Module sidebar navigation chưa implement thật (chỉ icon display)
- Menu ba chấm (Xuất Excel, In định mức, Xóa) chưa implement
