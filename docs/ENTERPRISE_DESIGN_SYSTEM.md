# ASMILE-BO ENTERPRISE DESIGN SYSTEM & WPF STANDARDIZATION

**Phiên bản:** 1.0
**Ngày:** 2026-07-19
**Loại:** UI Specification & Design System
**Trạng thái:** Specification Complete

---

## 1. Design Principles

### 1.1. Core Principles

| # | Nguyên tắc | Mô tả |
|---|---|---|
| 1 | **Chuyên nghiệp PMS** | Giao diện phải toát lên sự chuyên nghiệp của một PMS khách sạn 5 sao |
| 2 | **Thông tin trước, trang trí sau** | Mỗi pixel phải phục vụ thông tin nghiệp vụ |
| 3 | **Nhất quán tuyệt đối** | Cùng một control phải giống hệt nhau trên mọi module |
| 4 | **Responsive** | Hỗ trợ mọi độ phân giải từ 1366×768 đến 4K |
| 5 | **Keyboard-first** | Mọi thao tác phải có phím tắt |
| 6 | **Không phá vỡ hiện tại** | Modernize từng bước, không rewrite |

### 1.2. Design Language

```
Tone:     Chuyên nghiệp, tin cậy, hiện đại
Mood:     Sáng, thoáng, tập trung vào dữ liệu
Inspiration: Windows 11 Fluent Design + Material Design 3
```

---

## 2. Color Palette

### 2.1. Brand Colors

| Token | Hex | Usage |
|---|---|---|
| `Primary` | `#2563EB` | Primary actions, active states, selected items |
| `PrimaryHover` | `#1D4ED8` | Button hover |
| `PrimaryLight` | `#DBEAFE` | Selected backgrounds, badges |
| `Secondary` | `#64748B` | Secondary text, icons |
| `Accent` | `#F2994A` | Highlights, VIP, warnings |

### 2.2. Semantic Colors

| Token | Hex | Usage |
|---|---|---|
| `Success` | `#16A34A` | Completed, confirmed, paid |
| `SuccessLight` | `#DCFCE7` | Success backgrounds |
| `Warning` | `#EA580C` | Pending, due soon, attention needed |
| `WarningLight` | `#FFF7ED` | Warning backgrounds |
| `Danger` | `#DC2626` | Error, voided, cancelled, OOO |
| `DangerLight` | `#FEE2E2` | Error backgrounds |
| `Info` | `#2563EB` | Information, neutral status |
| `InfoLight` | `#DBEAFE` | Info backgrounds |

### 2.3. Neutral Colors

| Token | Hex | Usage |
|---|---|---|
| `TextPrimary` | `#0F172A` | Headings, body text |
| `TextSecondary` | `#64748B` | Labels, metadata, timestamps |
| `TextMuted` | `#94A3B8` | Placeholder, disabled text |
| `TextInverse` | `#FFFFFF` | Text on dark backgrounds |
| `Background` | `#F0F4F8` | Page background |
| `Surface` | `#FFFFFF` | Cards, panels, modals |
| `Border` | `#DDE7F3` | Card borders, dividers |
| `BorderLight` | `#E5E7EB` | Subtle separators |

### 2.4. Room Rack Status Colors

| Status | Color | Hex |
|---|---|---|
| Vacant Clean | Green | `#16A34A` |
| Vacant Dirty | Gray | `#94A3B8` |
| Occupied | Blue | `#2563EB` |
| Reserved | Purple | `#7C3AED` |
| Due Out | Orange | `#EA580C` |
| Out Of Order | Red | `#DC2626` |
| Out Of Service | Dark Gray | `#475569` |
| VIP Border | Gold | `#F2994A` |

---

## 3. Typography

### 3.1. Font Family

```
FontFamily: "Segoe UI Variable", "Segoe UI", sans-serif
Fallback:   "Arial" (cho máy không có Segoe UI Variable)
```

### 3.2. Type Scale

| Token | Size | Weight | Line Height | Usage |
|---|---|---|---|---|
| `Display` | 28px | SemiBold (600) | 36px | Dashboard titles |
| `Title` | 23px | SemiBold (600) | 32px | Module headers |
| `Heading` | 18px | Bold (700) | 26px | Section headers |
| `Subheading` | 16px | SemiBold (600) | 24px | Card titles, drawer headers |
| `Body` | 14px | Regular (400) | 20px | Content text |
| `BodyBold` | 14px | SemiBold (600) | 20px | Emphasized content |
| `Caption` | 12px | Regular (400) | 18px | Labels, metadata |
| `Small` | 11px | Regular (400) | 16px | Badges, timestamps |
| `Micro` | 10px | Regular (400) | 14px | Very small labels |

---

## 4. Spacing Scale

| Token | Value | Usage |
|---|---|---|
| `space-0` | 0px | No spacing |
| `space-1` | 4px | Tight grouping |
| `space-2` | 8px | Related elements |
| `space-3` | 12px | Standard gap |
| `space-4` | 16px | Section padding |
| `space-5` | 20px | Card internal padding |
| `space-6` | 24px | Page margin |
| `space-8` | 32px | Section separation |
| `space-10` | 40px | Major section break |
| `space-12` | 48px | Page-level spacing |

---

## 5. Border Radius

| Token | Value | Usage |
|---|---|---|
| `radius-sm` | 4px | Status badges, small tags |
| `radius-md` | 8px | Inputs, small cards |
| `radius-lg` | 10px | Standard cards, dialogs |
| `radius-xl` | 14px | Main cards, modal shells |
| `radius-full` | 999px | Pills, chips |

---

## 6. Elevation (Shadows)

| Level | X | Y | Blur | Color | Opacity |
|---|---|---|---|---|---|
| `elevation-1` | 0 | 1 | 3 | `#000000` | 0.06 |
| `elevation-2` | 0 | 2 | 8 | `#000000` | 0.08 |
| `elevation-3` | 0 | 4 | 14 | `#000000` | 0.10 |
| `elevation-4` | 0 | 8 | 24 | `#000000` | 0.12 |

---

## 7. WPF Control Specifications

### 7.1. Primary Button (`FluentPrimaryButton`)

```xml
<Style x:Key="FluentPrimaryButton" TargetType="Button">
  <Setter Property="Background" Value="#2563EB"/>
  <Setter Property="Foreground" Value="White"/>
  <Setter Property="FontSize" Value="14"/>
  <Setter Property="FontWeight" Value="SemiBold"/>
  <Setter Property="Padding" Value="16,10"/>
  <Setter Property="BorderThickness" Value="0"/>
  <Setter Property="CornerRadius" Value="10"/>
  <Setter Property="Cursor" Value="Hand"/>
</Style>
```

**States:** Normal → Hover (#1D4ED8) → Pressed (#1E40AF) → Disabled (#94A3B8)

### 7.2. Secondary Button (`FluentSecondaryButton`)

```
Background: White
Border: 1px #DDE7F3
Text: #0F172A
Hover: Background #F0F4F8
```

### 7.3. Icon Button (`FluentIconButton`)

```
Size: 32×32
Background: Transparent
Hover: Background #F0F4F8, corner radius 8
Icon size: 16px, color #64748B
```

### 7.4. Search Box (`FluentSearchBox`)

```
Height: 38px
Background: White
Border: 1px #DDE7F3
BorderRadius: 10
Padding: 8,0
Placeholder: "Tim kiem..."
FontSize: 14px
Focus: Border #2563EB
```

### 7.5. ComboBox

```
Height: 38px
Background: White
Border: 1px #DDE7F3
BorderRadius: 10
Padding: 8,0
FontSize: 14px
Dropdown: MaxHeight 300px, Virtualizing
Focus: Border #2563EB
```

### 7.6. DataGrid (`EnterpriseModuleDataGrid`)

```
RowHeight: 42px
HeaderHeight: 44px
HeaderBackground: #F0F4F8
HeaderFontWeight: SemiBold
HeaderFontSize: 12px
RowHover: #F8FAFC
SelectedRow: #DBEAFE
Border: #E5E7EB
CellPadding: 12,8
Virtualization: Recycling
```

### 7.7. Summary Card (`FluentCard`)

```
Background: White
Border: 1px #DDE7F3
CornerRadius: 14
Padding: 20
Shadow: elevation-1
```

### 7.8. Statistic Card (`KpiCard`)

```
Size: 220×120
Layout: Icon (top-left) + Value (large, bold) + Label (small, muted)
Color: Accent line (4px, top, rounded)
```

### 7.9. Status Badge

```
CornerRadius: 4
Padding: 4,2
FontSize: 11px
FontWeight: SemiBold
Colors: Success=#16A34A, Warning=#EA580C, Danger=#DC2626, Info=#2563EB
```

### 7.10. Room Card

```
Size: 220×150
Background: White
Border: 1px #DDE7F3
CornerRadius: 10
Padding: 12
Hover: Border #2563EB
Content:
  - Room Number (18px Bold)
  - Room Type (11px Muted)
  - Guest Name (14px SemiBold)
  - Stay Dates (11px)
  - Occupancy Badge (colored)
  - Housekeeping Badge (colored)
```

### 7.11. Timeline Item

```
Layout: Left bar (4px, colored) + Content
Bar Colors: Success=#16A34A, Warning=#F2994A, Info=#2563EB, Danger=#DC2626
Content: Action badge + Module + Description + Timestamp
```

---

## 8. Layout Specifications

### 8.1. Standard Module Layout

```
┌─ HEADER ────────────────────────────────────── 64px ─┐
│ Title (23px Bold)          [Actions] [Refresh]        │
│ Stats bar (12px: Occupancy, Revenue, Status counts)   │
├─ FILTER BAR ──────────────────────────────── 48px ────┤
│ [SearchBox] [ComboBox×N] [DatePicker] [CheckBox×N]    │
├─ CONTENT ──────────────────────────────────── flex ───┤
│ ┌─ Main Content ───────────┐ ┌─ Right Panel ────────┐ │
│ │                          │ │ 340-400px             │ │
│ │ DataGrid / RoomGrid /    │ │ Detail / Drawer /     │ │
│ │ ListView / Timeline      │ │ Actions / History      │ │
│ │                          │ │                       │ │
│ └──────────────────────────┘ └───────────────────────┘ │
├─ STATUS BAR ──────────────────────────────── 32px ────┤
│ Last refresh: HH:mm:ss     Page 1/5     Items: 150    │
└────────────────────────────────────────────────────────┘
```

### 8.2. Responsive Breakpoints

| Breakpoint | Width | Behavior |
|---|---|---|
| Compact | < 1024px | Right panel collapses to overlay drawer |
| Standard | 1024-1366px | 60/40 split |
| Wide | 1366-1920px | 65/35 split |
| UltraWide | > 1920px | 70/30 split, max content width 1600px |

### 8.3. Sidebar

```
Width: 78px (collapsed) / 240px (expanded)
Background: #1E293B (dark blue-gray)
Items: Icon + Label (collapsed shows icon only)
Active: #2563EB left border + light background
```

---

## 9. XAML Style Guidelines

### 9.1. Resource Organization

```
Themes/
├── Colors.xaml          — Color tokens
├── Brushes.xaml         — Brush resources
├── Typography.xaml      — Font families, text styles
├── Spacing.xaml         — Margin/Padding resources
├── Buttons.xaml         — Button styles
├── TextBox.xaml         — Input styles
├── ComboBox.xaml        — ComboBox styles
├── DataGrid.xaml        — DataGrid templates
├── Cards.xaml           — Card styles
├── Badges.xaml          — Status badges
├── Dialogs.xaml         — Modal/dialog shells
├── DataTemplates.xaml   — ViewModel→View mappings
├── RoomRackStyles.xaml  — Room Rack specific
├── FrontOfficeDesignSystem.xaml — FO design tokens
└── EnterpriseControls.xaml — Shared enterprise controls
```

### 9.2. Naming Convention

```
Module prefix for module-specific styles:
  FO.*    — Front Office shared
  HK.*    — Housekeeping
  RSV.*   — Reservation
  RR.*    — Room Rack
General styles without prefix:
  FluentPrimaryButton
  FluentSecondaryButton
  FluentCard
  KpiCard
```

### 9.3. Unicode Escapes

Mọi text tiếng Việt trong XAML phải dùng Unicode escapes:
```xml
<!-- ĐÚNG -->
<TextBlock Text="&#x110;&#x1EB7;t phòng" />

<!-- SAI — sẽ gây mojibake -->
<TextBlock Text="Đặt phòng" />
```

### 9.4. Color Usage Rules

- **Tuyệt đối không hardcode màu** trong XAML. Dùng `{StaticResource}` hoặc `{DynamicResource}`
- Color resource cho `Color` (vd: `TextPrimaryColor`)
- Brush resource cho `Brush` (vd: `TextPrimaryBrush`)
- **Không gán Color resource vào thuộc tính cần Brush** — đây là lỗi runtime phổ biến

---

## 10. Module UI Screens

### 10.1. Reservation Hub

```
┌─ ĐẶT PHÒNG ─────────────────────────────────────┐
│ [Đặt phòng mới] [Tìm] [Sửa] [Phân phòng]         │
│ [Xác nhận] [Hủy] [Trạng thái]                    │
└───────────────────────────────────────────────────┘
```

### 10.2. Reception Hub

```
┌─ LỄ TÂN ────────────────────────────────────────┐
│ [Tình trạng phòng] [Biểu đồ phòng] [Đặt phòng đoàn]│
│ [Check-in] [Check-out]                           │
│ [Hồ sơ lưu trú] [Phòng khóa] [Đang ở]            │
│ [Khách đến] [Khách đi]                           │
└───────────────────────────────────────────────────┘
```

### 10.3. Room Rack

```
┌─ SƠ ĐỒ PHÒNG ───────────────────────────────────┐
│ Tổng: 12 | Đang ở: 2 | Trống sạch: 8 | Bẩn: 1   │
│ [Search] [Building▼] [Floor▼] [Status▼] [VIP]    │
├──────────────────────────────────────────────────┤
│ ┌──┬──┬──┬──┬──┐  ┌─────────────────────────────┐│
│ │V1│V2│V3│V4│V5│  │ Detail Drawer              ││
│ │C │C │O │C │OO│  │ Room 101                    ││
│ ├──┼──┼──┼──┼──┤  │ Guest: Nguyễn Văn A         ││
│ │V6│O1│O2│C1│C2│  │ Stay: 19/07 → 22/07         ││
│ │C │B │B │C │C │  │ Balance: 2,500,000 VND      ││
│ └──┴──┴──┴──┴──┘  │ [Check-in] [Open Folio]     ││
│                    └─────────────────────────────┘│
└──────────────────────────────────────────────────┘
```

### 10.4. Tape Chart

```
┌─ BIỂU ĐỒ PHÒNG ─────────────────────────────────┐
│ ◀ Hôm nay ▶  [7 ngày ▼]                         │
├────────┬────┬────┬────┬────┬────┬────┬────┤
│ Phòng  │19/7│20/7│21/7│22/7│23/7│24/7│25/7│
├────────┼────┼────┼────┼────┼────┼────┼────┤
│ 101    │████████████░░░░░░░░░░░░░░░░░░░░│ N.V.A│
│ 102    │░░░░████████████████░░░░░░░░░░░░░│ T.T.B│
│ 103    │████████████████████████████████░░│ L.V.C│
└────────┴────┴────┴────┴────┴────┴────┴────┘
```

### 10.5. Group Reservation

```
┌─ ĐẶT PHÒNG ĐOÀN ───────────────────────────────┐
│ [Auto Phân Phòng] [Check-in Tất Cả]             │
│ [Check-out Tất Cả] [Master Folio]               │
├──────────────────────────────────────────────────┤
│ Group: ABC Corp | GRP-001 | Confirmed            │
│ Rooming List:                                    │
│ 101  Nguyễn Văn A  STD  Đã nhận                 │
│ 102  Trần Thị B    STD  Chưa nhận               │
│ ├─ Financial: Cọc 20M | DT 64M | Còn 44M ──────┤
└──────────────────────────────────────────────────┘
```

---

## 11. Icon System

### 11.1. Icon Sources

```
Primary:   Segoe MDL2 Assets (built-in Windows 10/11)
Fallback:  Unicode emoji (⚠ ✓ ✕ ● ▲ ▼ ▶ ◀)
Custom:    XAML Path geometries cho icon đặc biệt
```

### 11.2. Icon Sizes

| Size | Usage |
|---|---|
| 12px | Badges, inline indicators |
| 16px | Button icons, list indicators |
| 20px | Sidebar navigation |
| 24px | Header actions |
| 32px | Module launcher icons |

### 11.3. Key Icons (MDL2 Code Points)

| Icon | Code | Usage |
|---|---|---|
| Search | `` | Tìm kiếm |
| Refresh | `` | Làm mới |
| Settings | `` | Cài đặt |
| Add | `` | Thêm mới |
| Edit | `` | Chỉnh sửa |
| Delete | `` | Xóa |
| Save | `` | Lưu |
| Close | `` | Đóng / Hủy |
| CheckIn | `` | Nhận phòng |
| CheckOut | `` | Trả phòng |
| Room | `` | Phòng |
| Guest | `` | Khách |
| Calendar | `` | Lịch |
| Payment | `` | Thanh toán |
| Alert | `` | Cảnh báo |
| Lock | `` | Khóa |

---

## 12. Keyboard Shortcuts (Global)

| Shortcut | Action |
|---|---|
| `Ctrl+N` | Tạo mới |
| `Ctrl+F` | Tìm kiếm |
| `Ctrl+S` | Lưu |
| `F5` | Làm mới |
| `Escape` | Đóng modal / drawer |
| `Ctrl+1-9` | Chuyển tab |
| `Alt+←` | Quay lại |
| `Ctrl+P` | In |

---

## 13. Accessibility

| Requirement | Implementation |
|---|---|
| Keyboard navigation | Tab order, arrow keys, Enter/Space for actions |
| Focus states | 2px #2563EB outline on focused controls |
| High contrast | System theme detection, high-contrast color set |
| DPI scaling | WPF auto-scaling, test at 100%/125%/150%/200% |
| Screen reader | AutomationProperties.Name on all controls |

---

## 14. Migration Strategy

### 14.1. Phase 1: Design System Foundation (Current)
- ✅ Color tokens defined
- ✅ Typography scale established
- ✅ Button styles standardized
- ✅ Room Rack card design finalized

### 14.2. Phase 2: Component Library (Next)
- Extract reusable controls to `ASMILE.BO.Controls` project
- Create `FluentButton`, `FluentCard`, `KpiCard`, `StatusBadge` as custom controls
- Migrate all FO modules to use shared styles

### 14.3. Phase 3: Layout Standardization
- Apply standard module layout to all FO screens
- Standardize header + filter + content + status bar pattern
- Migrate sidebar to unified design

### 14.4. Phase 4: Polish
- Animation/transitions for modal open/close
- Toast notification system
- Loading skeleton screens
- Empty state illustrations

---

## 15. Remaining Limitations

| # | Limitation | Impact | Mitigation |
|---|---|---|---|
| 1 | Một số module vẫn dùng style cũ (`Accounting*`, `ActionBtn`) | Trải nghiệm không đồng nhất | Phase 2 migration |
| 2 | Chưa có Dark Mode | Không hỗ trợ làm việc ban đêm | Phase 4 |
| 3 | DropShadowEffect hiệu năng thấp | Lag trên máy yếu | Thay bằng `Border` + `Opacity` |
| 4 | Chưa có animation system | UI transitions không mượt | Phase 4 |
| 5 | WPF rendering limitations | Scroll performance với 5000+ items | VirtualizingPanel |
| 6 | Hardcoded Vietnamese strings | Không hỗ trợ đa ngôn ngữ | Resource file (.resx) |

---

## 16. XAML Example — Standard Module

```xml
<UserControl>
  <Grid>
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>  <!-- Header -->
      <RowDefinition Height="Auto"/>  <!-- Filters -->
      <RowDefinition Height="*"/>     <!-- Content -->
      <RowDefinition Height="Auto"/>  <!-- Status -->
    </Grid.RowDefinitions>
    
    <!-- HEADER -->
    <Border Style="{StaticResource ModuleHeader}">
      <Grid>
        <StackPanel>
          <TextBlock Text="Module Title" Style="{StaticResource PageTitle}"/>
          <StackPanel Orientation="Horizontal">
            <TextBlock Text="KPI 1" Style="{StaticResource StatLabel}"/>
            <TextBlock Text="KPI 2" Style="{StaticResource StatLabel}"/>
          </StackPanel>
        </StackPanel>
        <Button Content="Làm mới" Style="{StaticResource FluentSecondaryButton}"
                Command="{Binding RefreshCommand}"/>
      </Grid>
    </Border>
    
    <!-- FILTERS -->
    <Border Grid.Row="1" Style="{StaticResource FilterBar}">
      <WrapPanel>
        <TextBox Style="{StaticResource FluentSearchBox}" 
                 Text="{Binding SearchText}"/>
        <ComboBox ItemsSource="{Binding Filters}" Style="{StaticResource FluentComboBox}"/>
      </WrapPanel>
    </Border>
    
    <!-- CONTENT -->
    <Grid Grid.Row="2">
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>
      </Grid.ColumnDefinitions>
      <DataGrid ItemsSource="{Binding Items}" Style="{StaticResource EnterpriseModuleDataGrid}"/>
      <Border Grid.Column="1" Style="{StaticResource DetailPanel}"
              Visibility="{Binding HasSelection, Converter={StaticResource BoolToVis}}">
        <!-- Detail content -->
      </Border>
    </Grid>
    
    <!-- STATUS BAR -->
    <Border Grid.Row="3" Style="{StaticResource StatusBar}">
      <TextBlock Text="{Binding LastRefreshTime, StringFormat='Cập nhật: {0}'}"/>
    </Border>
  </Grid>
</UserControl>
```

---

## 17. Color Palette Reference Cards

### Room Card Color States

```
┌─────────────────────────────────────────────────┐
│ Vacant Clean  ████████  #16A34A  Green          │
│ Vacant Dirty  ████████  #94A3B8  Gray           │
│ Occupied      ████████  #2563EB  Blue           │
│ Reserved      ████████  #7C3AED  Purple         │
│ Due Out       ████████  #EA580C  Orange         │
│ Out Of Order  ████████  #DC2626  Red            │
│ Out Of Service████████  #475569  Dark Gray      │
│ VIP           ████████  #F2994A  Gold (border)  │
└─────────────────────────────────────────────────┘
```

### Status Badge Colors

```
┌─────────────────────────────────────────────────┐
│ Completed     ████████  #16A34A  Green          │
│ In Progress   ████████  #2563EB  Blue           │
│ Pending       ████████  #EA580C  Orange         │
│ Cancelled     ████████  #DC2626  Red            │
│ Draft         ████████  #94A3B8  Gray           │
│ VIP           ████████  #F2994A  Gold           │
└─────────────────────────────────────────────────┘
```

---

*Enterprise Design System specification complete. 17 sections covering color, typography, spacing, components, layout, icons, keyboard, accessibility, and migration strategy for ASMILE-BO Front Office.*
