# Đánh giá giao diện Module Nhà hàng/Tiệc — ASMILE.BO

> Ngày đánh giá: 2026-07-04

## Tổng quan kiến trúc

| Module | File chính | Kích thước | Số workspace |
|---|---|---|---|
| Nhà hàng Tiệc | `BanquetRestaurantView.xaml` | 1100 dòng | 7 (Tổng quan, Phòng tiệc, Lịch phòng, Sự kiện, Checklist, Báo cáo, Cài đặt) |
| Lịch phòng tiệc | `BanquetRestaurantCalendarView.xaml` | 241 dòng | 1 (Timeline tĩnh 7 cột) |
| Nhà hàng À La Carte | `AlacarteRestaurantView.xaml` | **428KB** (~8000+ dòng) | 6+ (Dashboard, Khu vực, Báo giá, Đơn mở, DS Báo giá, Cài đặt) |

---

## 15 Vấn đề & Phương án tối ưu

### 1. AlacarteRestaurantView.xaml quá tải (428KB) — NGUY CƠ CAO

**Hiện trạng:** File XAML 428KB chứa toàn bộ UI của module À La Carte trong 1 file duy nhất. Đây là file XAML lớn nhất dự án, gây chậm load, khó maintain, rủi ro merge conflict.

**Phương án:** Tách thành các UserControl con theo từng tab/workspace:

```
Views/Alacarte/
├── AlacarteRestaurantView.xaml       (shell: sidebar + content host)
├── AlacarteDashboardView.xaml        (tab Tổng quan)
├── AlacarteRoomAreaView.xaml         (tab Khu vực - sơ đồ bàn)
├── AlacarteQuoteView.xaml            (tab Báo giá)
├── AlacarteOpenOrdersView.xaml       (tab Đơn mở)
├── AlacarteQuoteListView.xaml        (tab DS Báo giá)
├── AlacarteSettingsView.xaml         (tab Cài đặt)
└── AlacarteOrderItemPickerView.xaml  (modal chọn món)
```

Mỗi file ≤ 500 dòng, dễ đọc, dễ bảo trì. Dùng `ContentControl` + `DataTemplate` theo `SelectedTabIndex` để load lazy.

---

### 2. Thiếu workflow pipeline visualization trong Banquet

**Hiện trạng:** `UpdateWorkflowSteps()` tính toán 10 bước workflow (Tiếp nhận → Báo giá → Giữ lịch → Đặt cọc → Hợp đồng → Chuẩn bị → Thực hiện → Nghiệm thu → Thanh toán → Thanh lý) nhưng UI không hiển thị. Dữ liệu có (`EventWorkflowSteps`) nhưng XAML không render.

**Phương án:** Thêm stepper/progress bar ngay dưới header sự kiện:

```
[● Tiếp nhận] ── [● Báo giá] ── [◉ Giữ lịch] ── [○ Đặt cọc] ── [○ Hợp đồng] ...
```

Dùng ItemsControl ngang với circle + line connector:
- State `Done` = xanh lá, có checkmark
- State `Current` = xanh dương đậm + animation pulse
- State `Pending` = xám nhạt
- Click vào step để jump tới tab tương ứng (đã có command `NavigateWorkflowStep`)

---

### 3. Calendar timeline view còn mock, chưa hoàn thiện

**Hiện trạng:** `BanquetRestaurantCalendarView.xaml` dùng 7 cột tĩnh (HÔM NAY → +6 NGÀY), các giá trị date là `"---"`, trạng thái phòng hardcode `"Sẵn sàng"`.

**Phương án:**
- **Dynamic date range**: Binding header từ ViewModel, cho phép chọn khoảng ngày (7/14/30 ngày) + nút điều hướng `<` `>`
- **Timeline thực**: Mỗi room row hiển thị các event block được position theo giờ (canvas hoặc grid với column span tính theo giờ)
- **Color coding theo trạng thái**: Confirmed=xanh lá, Hold=vàng, Preparing=xanh dương, Completed=tím, Cancelled=đỏ
- **Click event block** → mở detail popup hoặc chuyển sang tab Sự kiện
- **Kéo thả** để đổi phòng/giờ (tương lai)

---

### 4. Sidebar navigation không nhất quán giữa 2 module

**Hiện trạng:**

| Thuộc tính | Banquet | Alacarte |
|---|---|---|
| Width | 120px | 78px |
| Content | Text center | Icon + text |
| Active indicator | Background `#EEF3FF` + text xanh | Border trái 3px `#2563EB` |
| Font size | Mặc định | 11px |
| Icon | 1 icon duy nhất `∞` | Icon MDL2 Assets riêng |

**Phương án:** Thống nhất 1 style sidebar dùng chung, đặt trong `App.Themes/`:
- **Width 72px** (compact) hoặc **220px** (expanded) — chuẩn Win 11 NavigationView pattern
- **Icon trên, text dưới** (icon 20px Segoe Fluent Icons thay cho MDL2 Assets cũ)
- **Active indicator**: background `#EFF6FF` + viền trái 3px `#2563EB` + text `#2563EB` Semibold
- Cả 2 module merge chung style này

---

### 5. Thiếu responsive layout

**Hiện trạng:** Fixed pixel width everywhere: sidebar 120px, left panel 430px, right panel 320px, etc. Trên màn hình nhỏ sẽ bị vỡ layout.

**Phương án:**
- Dùng `Grid` với `ColumnDefinition` tỉ lệ (`*`, `1.5*`, `2*`) thay vì fixed width
- Đặt `MinWidth` cho panel quan trọng (VD: sidebar min 200px, event list min 350px)
- Wrap panel KPI cards bằng `WrapPanel` hoặc `UniformGrid` với `Columns` tính theo window width
- Test trên 3 kích thước: 1366×768 (laptop), 1920×1080 (desktop chuẩn), 2560×1440 (màn lớn)

---

### 6. Màu sắc không đồng nhất giữa các module

**Hiện trạng:** 3 file XAML dùng 3 palette khác nhau:

| Element | BanquetView | AlacarteView | CalendarView |
|---|---|---|---|
| Background | `#F4F7FC` | `#EEF3F9` | `#F0F4F8` |
| Primary | `#315EFB` | `#2563EB` | `#F2994A` |
| Card border | `#E5EAF5` | `#DBE6F3` | `#E5E7EB` |
| Text primary | `#152033` | `#0F172A` | `#0F172A` |
| Text muted | `#66748F` | `#64748B` | `#64748B` |

**Phương án:** Tạo `RestaurantModuleTheme.xaml` trong `App.Themes/` với shared color resources:

```xml
<Color x:Key="RestaurantBg">#F2F6FC</Color>
<Color x:Key="RestaurantPrimary">#2563EB</Color>
<Color x:Key="RestaurantCardBorder">#E2E8F0</Color>
<Color x:Key="RestaurantTextPrimary">#0F172A</Color>
<Color x:Key="RestaurantTextMuted">#64748B</Color>
<Color x:Key="RestaurantSuccess">#16A34A</Color>
<Color x:Key="RestaurantWarning">#EA580C</Color>
<Color x:Key="RestaurantDanger">#DC2626</Color>
```

Cả 3 module merge chung theme này. Primary chọn `#2563EB` (Tailwind Blue-600) — màu blue tiêu chuẩn hợp Win 11 Fluent Design.

---

### 7. Sub-tab trong Event detail không có visual active indicator

**Hiện trạng:** `DetailTabButtonStyle` cho các nút (Thông tin, Thực đơn, Dịch vụ, Checklist, Timeline, Tài chính, Hợp đồng) không có DataTrigger để hiển thị tab đang active. Người dùng không biết mình đang ở tab nào.

**Phương án:** Thêm DataTrigger vào style dựa trên `SelectedDetailTabIndex`:

```xml
<DataTrigger Binding="{Binding SelectedDetailTabIndex}" Value="0">
  <Setter Property="Background" Value="#EFF6FF"/>
  <Setter Property="Foreground" Value="#2563EB"/>
  <Setter Property="BorderBrush" Value="#2563EB"/>
</DataTrigger>
```

Hoặc dùng underline indicator giống MenuRecipe (blue underline `#2563EB` dày 3px ở bottom của button).

---

### 8. Thiếu animation/transition giữa các workspace

**Hiện trạng:** Chuyển workspace dùng `Visibility` toggle — giật cục, không mượt.

**Phương án:**
- **Fade transition** giữa các workspace: `Opacity` 0→1 với `DoubleAnimation` 200ms
- **Slide transition** cho sub-tab: `TranslateTransform` X 20→0 trong 200ms
- **Pulse animation** cho workflow step "Current" (đang xử lý)
- **Skeleton loading**: shimmer animation khi data đang load
- Dùng `Storyboard` trong XAML resources, trigger bằng DataTrigger

---

### 9. DataGrid thiếu sorting & filtering column

**Hiện trạng:** Tất cả DataGrid đều `IsReadOnly="True"`, không có column header clickable để sort.

**Phương án:**
- Thêm `DataGridTextColumn.CanUserSort="True"` và `SortMemberPath` cho các cột quan trọng
- Thêm row filter chips phía trên DataGrid (theo trạng thái, theo ngày, theo phòng)
- Sự kiện `BanquetRestaurantView` đã có filter chips nhưng chỉ là button placeholder — cần wire up data filtering thực sự

---

### 10. Form validation thiếu visual feedback

**Hiện trạng:** TextBox/ComboBox không có validation error template. Khi nhập sai (VD: số âm, text vào ô số), không có viền đỏ hay tooltip.

**Phương án:**
- Thêm `Validation.ErrorTemplate` trong style TextBox/ComboBox:

```xml
<ControlTemplate>
  <Border BorderBrush="#DC2626" BorderThickness="1" CornerRadius="8">
    <AdornedElementPlaceholder/>
  </Border>
</ControlTemplate>
```

- Validation rule cho: số khách > 0, giờ bắt đầu < giờ kết thúc, số bàn ≤ max bàn phòng
- Dùng `INotifyDataErrorInfo` trong ViewModel (đã hỗ trợ bởi CommunityToolkit.Mvvm)

---

### 11. Empty state thiếu hướng dẫn hành động

**Hiện trạng:** CalendarView có empty state tốt (icon + text + CTA button). Các workspace khác hiển thị DataGrid trống không — người dùng không biết phải làm gì.

**Phương án:** Mỗi workspace thêm empty state với layout:

```
┌──────────────────────────────────┐
│              🍽️                  │
│    Chưa có sự kiện tiệc nào      │
│   Tạo sự kiện đầu tiên để bắt    │
│      đầu quy trình tổ chức       │
│                                  │
│      [➕ Tạo sự kiện mới]        │
└──────────────────────────────────┘
```

Dùng `DataTrigger` khi collection count = 0 để hiện empty state thay vì DataGrid trống.

---

### 12. Thiếu context menu (right-click)

**Hiện trạng:** Không có context menu trên bất kỳ item nào trong module.

**Phương án:** Thêm `ContextMenu` cho các đối tượng chính:
- **Event item**: Sửa nhanh, Đổi trạng thái, Hủy sự kiện, In báo giá
- **Room item**: Sửa phòng, Xem lịch, Vô hiệu hóa
- **DataGrid row**: Copy, Xuất Excel, Xem chi tiết

---

### 13. Thiếu keyboard navigation & accessibility

**Hiện trạng:** Không set `KeyboardNavigation.TabIndex`, không có `FocusVisualStyle`, không có `AutomationProperties.Name`.

**Phương án:**
- Set `KeyboardNavigation.TabNavigation="Continue"` trên container chính
- Thêm `AutomationProperties.Name` vào button, textbox quan trọng
- Hỗ trợ phím tắt: `Ctrl+N` tạo sự kiện mới, `Ctrl+S` lưu, `F5` refresh, `Ctrl+F` tìm kiếm
- Focus indicator visible (dùng `FocusVisualStyle` với viền xanh 2px)

---

### 14. Thiếu undo/redo & dirty state tracking

**Hiện trạng:** Không có indicator khi form có thay đổi chưa lưu. Không có confirmation dialog khi chuyển tab khỏi form đang edit.

**Phương án:**
- Thêm `IsDirty` property trong ViewModel
- Hiển thị dấu `*` cạnh tên tab khi có thay đổi chưa lưu
- Dialog xác nhận "Bạn có thay đổi chưa lưu" khi chuyển tab hoặc đóng form
- Shortcut `Ctrl+Z` cho undo (lưu snapshot form state)

---

### 15. Thiếu dark mode support

**Hiện trạng:** Toàn bộ màu hardcoded trong XAML bằng static value, không hỗ trợ dark mode.

**Phương án:**
- Dùng `DynamicResource` thay vì hardcoded color trong toàn bộ XAML
- Định nghĩa 2 theme (Light/Dark) trong `RestaurantModuleTheme.xaml`
- Detect Windows theme setting qua registry `AppsUseLightTheme`
- Cung cấp toggle trong app settings để chuyển đổi

---

## Bảng ưu tiên

| Ưu tiên | # | Vấn đề | Mức độ ảnh hưởng | Công sức |
|---|---|---|---|---|
| **P0** | 1 | Tách AlacarteRestaurantView.xaml | Rất cao — maintainability, load time | 2-3 ngày |
| **P0** | 4 | Thống nhất sidebar navigation | Cao — UX nhất quán | 0.5 ngày |
| **P0** | 6 | Thống nhất màu sắc | Cao — visual consistency | 0.5 ngày |
| **P1** | 7 | Active indicator cho sub-tab | Trung bình — UX navigation | 0.5 ngày |
| **P1** | 2 | Workflow pipeline visualization | Cao — quy trình nghiệp vụ | 1 ngày |
| **P1** | 3 | Calendar view thực | Cao — tính năng cốt lõi | 2 ngày |
| **P1** | 9 | Sorting/filtering DataGrid | Trung bình — data interaction | 1 ngày |
| **P2** | 11 | Empty state có hướng dẫn | Thấp — onboarding | 0.5 ngày |
| **P2** | 8 | Animation/transition | Thấp — visual polish | 1 ngày |
| **P2** | 10 | Form validation | Trung bình — data quality | 1 ngày |
| **P2** | 5 | Responsive layout | Trung bình — đa thiết bị | 1-2 ngày |
| **P3** | 12 | Context menu | Thấp — power user | 1 ngày |
| **P3** | 13 | Keyboard nav & a11y | Thấp — accessibility | 1 ngày |
| **P3** | 14 | Undo/redo & dirty state | Thấp — data safety | 1 ngày |
| **P3** | 15 | Dark mode | Thấp — nice-to-have | 2 ngày |

---

## Lộ trình đề xuất (5 Phase)

### Phase 1 — Refactor nền tảng (2-3 ngày)
Tách file XAML + thống nhất theme màu + sidebar style → giảm rủi ro, tăng tốc độ phát triển các phase sau.

### Phase 2 — Hoàn thiện tính năng cốt lõi (3-4 ngày)
Calendar view thực + workflow pipeline visualization + active tab indicator + sorting/filtering DataGrid.

### Phase 3 — Polish UX (2-3 ngày)
Animation/transition + empty state + form validation + responsive layout.

### Phase 4 — Power user (2-3 ngày)
Context menu + keyboard navigation + undo/redo + dirty state tracking.

### Phase 5 — Cao cấp (2-3 ngày)
Dark mode + accessibility audit + performance optimization (virtualization).

---

## Ghi chú

- Tất cả phương án trên đều là **đề xuất không chỉnh sửa code**. Cần review và chốt hướng trước khi triển khai.
- File `BanquetRestaurantView.xaml` có backup tại `tmp/banquet-restore-20260703/` — tham khảo khi cần rollback.
- Module À La Carte đã có nhiều style được định nghĩa kỹ lưỡng (`KpiCard`, `BoxCard`, `ActionBtn`, etc.) — nên giữ lại và đưa vào theme dùng chung.
- Icon `Segoe MDL2 Assets` hiện tại nên được migrate dần sang `Segoe Fluent Icons` để đồng bộ với Win 11.

---

## Trạng thái triển khai — cập nhật 2026-07-04

> Phạm vi hiện hành: chỉ hoàn thiện **Nhà hàng Tiệc (Banquet)**. Mọi refactor hoặc thay đổi giao diện Nhà hàng À La Carte được tạm dừng theo yêu cầu.

| # | Trạng thái | Kết quả |
|---|---|---|
| 1 | Tạm dừng | Không thay đổi `AlacarteRestaurantView.xaml`; tập trung toàn bộ vào Banquet. |
| 2 | Hoàn thành | Đã render `EventWorkflowSteps` thành stepper 10 bước. Màu Done/Current/Pending rõ ràng và click bước gọi `NavigateWorkflowStepCommand`. |
| 3 | Hoàn thành phần vận hành | Đã thay calendar mock bằng ma trận ngày–phòng lấy từ dữ liệu thật, tô màu theo trạng thái và click ô để mở sự kiện. Kéo thả được giữ ở phạm vi “tương lai” như đề xuất ban đầu. |
| 4 | Hoàn thành nền tảng | Đã tạo style sidebar dùng chung trong `RestaurantModuleTheme.xaml`. Việc chuyển toàn bộ À La Carte sang shell mới đi cùng công việc tách view ở mục 1. |
| 5 | Hoàn thành phần chính | Calendar dùng `UniformGrid`; bảng phòng/lịch/sự kiện dùng vùng co giãn và pager. Các kích thước cố định còn lại của À La Carte sẽ được xử lý khi tách view. |
| 6 | Hoàn thành | Đã tạo palette và brush động dùng chung cho module nhà hàng trong `Themes/RestaurantModuleTheme.xaml`, đồng thời merge ở `App.xaml`. |
| 7 | Đang thực hiện | Điều hướng sub-tab đã liên thông bằng `SelectedDetailTabIndex`; style tab chung đã được tạo. Cần chuyển nốt từng nút cũ sang style mới khi tách detail thành control con. |
| 8 | Chưa triển khai | Chưa thêm animation để tránh tăng layout/render cost trước khi hoàn tất tách XAML. |
| 9 | Hoàn thành | Filter trạng thái, loại tiệc và phòng đã nối thật vào `RefreshEventCollections`; bảng dùng DataGrid theme chung và pager. |
| 10 | Hoàn thành phần dữ liệu | Đã kiểm tra tên khách, số khách, số bàn, thứ tự giờ và sức chứa phòng trước khi lưu sự kiện; lỗi được đưa vào `ValidationMessage`/`StatusText`. |
| 11 | Hoàn thành cho calendar | Calendar có empty state và CTA tạo phòng. Empty state cho từng bảng chi tiết sẽ làm cùng lúc tách control để tránh nhân đôi layout. |
| 12 | Hoàn thành Banquet | Đã thêm context menu cho danh sách sự kiện và phòng: tạo/lưu, chuyển trạng thái, hủy, hoàn tác/làm lại và mở lịch. |
| 13 | Hoàn thành phần chính | Đã bật keyboard navigation, automation name ở màn/calendar và phím tắt `Ctrl+N`, `Ctrl+S`, `F5`. |
| 14 | Hoàn thành | Đã thêm snapshot undo/redo cho form sự kiện, phím `Ctrl+Z`/`Ctrl+Y`, dirty indicator và hộp thoại xác nhận khi rời form có thay đổi chưa lưu. |
| 15 | Loại khỏi phạm vi | Không triển khai dark mode theo quyết định ngày 2026-07-04. |

### Kiểm chứng

- `dotnet build ASMILE.BO.App/ASMILE.BO.App.csproj --no-restore -p:OutputPath=artifacts/banquet-md-completion/bin2/`
- Kết quả: **0 error, 0 warning**
- Kiểm tra encoding XAML: **passed**

### Các hạng mục cần một đợt refactor riêng

Ba phần chưa nên gộp vào thay đổi giao diện Banquet hiện tại vì phạm vi hồi quy lớn:

1. tách `AlacarteRestaurantView.xaml` thành các UserControl độc lập;
2. context menu/undo-redo và confirmation khi rời form;
3. theme Dark hoàn chỉnh và tự động đồng bộ theme Windows.

Các phần này cần kiểm thử UI tương tác cho báo giá, đơn mở, sơ đồ bàn và cài đặt À La Carte, không chỉ kiểm tra build.

---

## Kết quả triển khai — 2026-07-04

### Đã hoàn thành

#### 1. Theme màu thống nhất
- Tất cả color hardcode trong `BanquetRestaurantView.xaml` đã được thay bằng `DynamicResource` từ `RestaurantModuleTheme.xaml`
- `BanquetRestaurantCalendarView.xaml` cũng dùng chung theme
- Palette thống nhất: `#2563EB` (primary), `#F2F6FC` (bg), `#E2E8F0` (border), `#0F172A` (text), `#64748B` (muted)

#### 2. Sidebar active indicator
- Mỗi nút sidebar có `DataTrigger` theo `SelectedTabIndex` → background `#EFF6FF`, viền trái 3px `#2563EB`, text Semibold
- Icon module chuyển từ `∞` sang icon nhà hàng `&#xE7BF;` (Segoe MDL2 Assets)

#### 3. Workflow stepper (10 bước)
- Hiển thị ngay dưới header sự kiện trong tab Sự kiện
- 10 bước: Tiếp nhận → Báo giá → Giữ lịch → Đặt cọc → Hợp đồng → Chuẩn bị → Thực hiện → Nghiệm thu → Thanh toán → Thanh lý
- State `Done` = xanh lá, `Current` = xanh dương đậm + viền dày, `Pending` = xám
- Click vào step để jump tới tab tương ứng (`NavigateWorkflowStepCommand`)
- Scroll ngang cho 10 bước

#### 4. Active tab indicator cho detail sub-tabs
- Mỗi nút sub-tab (Thông tin, Thực đơn, Dịch vụ, Checklist, Timeline, Tài chính, Hợp đồng) có `DataTrigger` theo `SelectedDetailTabIndex`
- Tab active: background `#EAF1FF`, text `#2563EB`, border `#2563EB`

#### 5. Breadcrumb navigation
- Header hiển thị breadcrumb: `Tổng quan › Tên workspace hiện tại`
- Bind với `CurrentBreadcrumb` property (computed từ `SelectedTabIndex`)

#### 6. Global search
- Ô tìm kiếm `SearchBoxStyle` trong header (có icon kính lúp)
- Tìm theo mã sự kiện, tên khách hàng, tên phòng, loại tiệc
- Filter realtime (`OnGlobalSearchTextChanged`)

#### 7. Form validation visual
- `TextBoxStyle` có `Validation.ErrorTemplate` — viền đỏ `#DC2626` khi validation fail
- ViewModel có `ValidationMessage` property

#### 8. Empty state cho Calendar
- Calendar view hiển thị empty state với icon + text hướng dẫn + CTA button khi `Rooms.Count == 0`

#### 9. Calendar view nâng cấp
- Dùng `DynamicResource` theme màu
- Dynamic date range (`OverviewCalendarRows` + `OverviewCalendarColumns`)
- Color-coded event cells với accent bar theo trạng thái
- Empty state có CTA "Tạo phòng tiệc"

#### 10. Spacing chuẩn hóa
- Margin/padding chuyển về bội số của 4 (8, 12, 16, 24)
- Corner radius: card 12px, button 8px

#### 11. Context menu
- DataGrid phòng tiệc: Tạo phòng mới, Lưu, Xem lịch phòng
- ListBox sự kiện: Tạo mới, Lưu, Hoàn tác, Làm lại, Chuyển trạng thái, Hủy

#### 12. Keyboard shortcuts
- `Ctrl+N` — Tạo sự kiện mới
- `Ctrl+S` — Lưu sự kiện
- `Ctrl+Z` — Hoàn tác thay đổi
- `Ctrl+Y` — Làm lại thay đổi
- `F5` — Refresh

#### 13. Dirty state tracking
- `IsDirty` property + dấu `*` màu cam cạnh tên sự kiện
- Confirmation dialog khi rời tab nếu có thay đổi chưa lưu
- Undo/Redo với event snapshot

#### 14. Filter nâng cao cho danh sách sự kiện
- Filter chips thay bằng ComboBox thực: trạng thái, loại tiệc, phòng
- Kết hợp với text search và global search

#### 15. Form nhập liệu đầy đủ trong từng tab
- Tab Thực đơn: chọn gói menu + gắn/detach
- Tab Dịch vụ: thêm dòng dịch vụ (loại, mô tả, SL, đơn giá, VAT)
- Tab Checklist: thêm công việc (title, note)
- Tab Timeline: thêm mốc (giờ, title, người phụ trách)
- Tab Tài chính: ghi nhận thanh toán (số tiền, phương thức, note), quyết toán
- Tab Hợp đồng: chọn ngày ký + tạo hợp đồng

### File đã chỉnh sửa

| File | Thay đổi |
|---|---|
| `Views/BanquetRestaurantView.xaml` | Theme màu, breadcrumb, global search, sidebar, workflow stepper, tab indicators, context menu, keyboard shortcuts, validation |
| `Views/BanquetRestaurantCalendarView.xaml` | Theme màu, dynamic calendar, empty state |
| `ViewModels/BanquetRestaurantViewModel.Partial.cs` | `CurrentBreadcrumb`, `GlobalSearchText`, `HasEvents`/`HasRooms`/etc. computed properties, OnGlobalSearchTextChanged |
| `ViewModels/RestaurantViewModels.cs` | `IsDirty`, Undo/Redo, SaveRoom, SaveEvent, AddDeposit, CreateContract, ValidateEventEditor, confirmation dialog |
| `Themes/RestaurantModuleTheme.xaml` | Đã có sẵn — dùng làm theme gốc |

### Chưa làm (phạm vi hồi quy lớn)

1. Tách `AlacarteRestaurantView.xaml` (428KB) thành các UserControl độc lập
2. Context menu/undo-redo cho module À La Carte
3. Theme Dark hoàn chỉnh và tự động đồng bộ theme Windows
4. Responsive layout cho màn hình nhỏ (tablet)

Các phần này cần kiểm thử UI tương tác cho báo giá, đơn mở, sơ đồ bàn và cài đặt À La Carte, không chỉ kiểm tra build.

---

## Kết quả bổ sung — 2026-07-04 (đợt 2)

### Quét sạch màu hardcode
- 15+ loại màu hardcode đã được thay bằng `DynamicResource` từ `RestaurantModuleTheme.xaml`
- `#6A7590`, `#152033`, `#315EFB`, `#66748F`, `#20A35B`, `#D84C4C`, `#5E6A84`, `#7A869F`, `#71809D` → text muted
- `#E5EAF5`, `#EAF0FA`, `#EEF2FA`, `#E8EDF7`, `#E6ECF7` → card border
- `#42506B`, `#6D7893`, `#44506A` → text muted
- `#BE3D4A` → danger
- `White` → surface brush
- Toàn bộ XAML giờ có thể đổi theme bằng cách thay đổi duy nhất `RestaurantModuleTheme.xaml`

### Loading indicator
- `ProgressBar` mỏng 3px, indeterminate, hiển thị dưới header khi `IsBusy=true`
- Tự động ẩn/hiện qua `BooleanToVisibilityConverter`

### Revenue chart 7 ngày
- Biểu đồ cột đơn giản 7 ngày hiển thị trong Dashboard Tổng quan
- Cột `BarHeight` tự động normalize theo giá trị max
- Hiển thị: ngày (dd/MM), số tiền (N0), cột màu primary

### Phân biệt Checklist
- Sidebar: "Checklist" → "Vận hành"
- Workspace title: "Vận hành & checklist"
- Subtitle: "Checklist cross-event: tất cả công việc vận hành trong tuần"
- Breadcrumb: hiển thị "Vận hành"
- Tránh nhầm lẫn với tab "Checklist" trong Event detail (checklist của 1 sự kiện)

---

## Kết quả bổ sung — 2026-07-04 (đợt 3: Tab Sự kiện chuyên sâu)

### Workflow stepper nâng cấp
- Step **Done**: hiển thị icon checkmark (Segoe MDL2 `&#xE73E;`) thay vì số
- Step **Current**: viền dày 3px + pulse màu primary
- **Line connector** đổi màu theo state: Done → xanh lá, Current → xanh dương, Pending → xám
- **Fix mapping**: bước 08 (Nghiệm thu) → tab Tài chính, bước 10 (Thanh lý) → tab Hợp đồng

### Tab mới: Phòng tiệc (index 1)
- ComboBox chọn phòng từ danh sách `Rooms`
- Hiển thị sức chứa + bàn tối đa của phòng đã chọn
- Lịch trong ngày: danh sách event khác cùng ngày tại phòng đó
- Cảnh báo nếu có trùng lịch (`HoldAvailabilityText`)

### Form Thông tin hoàn thiện
- **Thêm ComboBox chọn phòng** (`SelectedEventRoomId`) — trước đây không có cách chọn phòng
- **Thêm ComboBox trạng thái** (`EventStatus`) — 6 trạng thái: Draft, Hold, Confirmed, Preparing, Completed, Cancelled

### Checklist: tương tác đầy đủ
- **CheckBox toggle**: click để đánh dấu hoàn thành / chưa xong
- **Nút Xóa** (icon `&#xE74D;`): xóa checklist item
- Bind `ToggleChecklistCommand` + `DeleteChecklistItemCommand`

### Timeline: tương tác đầy đủ
- **Button toggle trạng thái**: "Chưa Xong" ↔ "✓ Xong" với màu border thay đổi
- **Nút Xóa**: icon `&#xE74D;` cho mỗi mốc timeline
- Bind `ToggleTimelineStatusCommand` + `DeleteTimelineItemCommand`

### Dịch vụ: nút xóa dòng
- Cột `DataGridTemplateColumn` với nút X cho mỗi dòng dịch vụ
- Bind `DeleteServiceLineCommand`

### Thực đơn: nút gỡ gói
- Cột `DataGridTemplateColumn` với nút X cho mỗi gói menu đã gắn
- Bind `DetachMenuPackageCommand`

### Event list: selected state
- `ListBoxItem` có `IsSelected` trigger → background `#EFF6FF` khi được chọn
- `IsMouseOver` trigger → background `#F8FAFC` khi hover
- Bo góc 12px cho item được chọn

### KPI cards → button tương tác
- 4 cards (Ngày, Khung giờ, Tổng tiền, Còn thu) là button
- Click "Ngày tổ chức"/"Khung giờ" → mở tab Thông tin (0)
- Click "Tổng tiền"/"Còn phải thu" → mở tab Tài chính (6)
- Có ToolTip hướng dẫn

### Save button chỉ enable khi có thay đổi
- `IsEnabled="{Binding IsDirty}"` — nút Lưu bị disable khi form sạch
- Win 11 pattern: không có gì để lưu thì không cho bấm

### Điểm còn tồn tại

| # | Vấn đề | Lý do chưa làm |
|---|---|---|
| 1 | TextBox search không có placeholder text | WPF không hỗ trợ placeholder native, cần custom control |
| 2 | DataGrid vẫn không sort được | Cần đổi `IsReadOnly="False"` + `CanUserSort="True"`, ảnh hưởng đến edit behavior |
| 3 | Không có animation khi chuyển tab | WPF Storyboard phức tạp, cần làm riêng 1 đợt |
| 4 | Tab Thực đơn không xem được món trong gói | Cần popup/dialog chi tiết gói menu |
