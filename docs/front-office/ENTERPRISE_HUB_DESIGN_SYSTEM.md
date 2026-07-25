# ENTERPRISE HUB DESIGN SYSTEM

> **Ngày**: 2026-07-21
> **Phiên bản**: 1.0
> **Source of Truth**: FrontOfficeHub (approved by user)

---

## 1. DESIGN LANGUAGE

Tất cả Hub trong ASMILE-BO phải tuân theo Design System này. FrontOfficeHub là chuẩn gốc.

### Typography

| Element | FontSize | Weight | Color |
|---------|----------|--------|-------|
| Hub Header | 38px | SemiBold | #102E5A |
| Hub Subtitle | 18px | Regular | #5E6E82 |
| Card Title | 14px | SemiBold | #16325C |

### Card Component

| Property | Value |
|----------|-------|
| Width | 236px |
| Height | 92px |
| CornerRadius | 14px |
| Background | #FFFFFFF2 |
| Border | 1px #D5E3F5 |
| Padding | 16,0,14,0 |
| Margin | 9,10 |

### Icon Container

| Property | Value |
|----------|-------|
| Size | 54×54 |
| CornerRadius | 14px |
| Icon font | 28px Segoe MDL2 Assets |
| Color | White on gradient |
| Shadow | Blur 10, Opacity 0.24 |

### Grid

| Property | Value |
|----------|-------|
| Layout | WrapPanel, 4 cols |
| ItemWidth | 256px |
| ItemHeight | 118px |
| Container MaxWidth | 1060px |

### Shadow

| State | Blur | Opacity |
|-------|------|---------|
| Default | 16 | 0.08 |
| Hover | 22 | 0.12 |
| Hover lift | -3px | — |

### Background

```xml
<RadialGradientBrush GradientOrigin="0.04,0.04" RadiusX="0.32" RadiusY="0.32">
    <GradientStop Color="#106aa7ff" Offset="0"/>
    <GradientStop Color="#00ffffff" Offset="1"/>
</RadialGradientBrush>
```

### Animation

| Trigger | Effect |
|---------|--------|
| Hover | TranslateTransform Y="-3" + shadow increase |
| Duration | Instant (no animation) |

---

## 2. SHARED COMPONENTS

| Component | Location |
|-----------|----------|
| `HubCardStyle` | `Themes/HubCardStyles.xaml` |
| `HubIconBoxStyle` | `Themes/HubCardStyles.xaml` |
| `HubCardTitleStyle` | `Themes/HubCardStyles.xaml` |
| `HubHeaderStyle` | `Themes/HubCardStyles.xaml` |
| `HubSubtitleStyle` | `Themes/HubCardStyles.xaml` |
| `HubGridStyle` | `Themes/HubCardStyles.xaml` |
| `HubContainerStyle` | `Themes/HubCardStyles.xaml` |
| `HubBackButtonStyle` | `Themes/HubCardStyles.xaml` |
| `HubCardShadow` | `Themes/HubCardStyles.xaml` |

### Hub Architecture

```
Dashboard → Hub → Workspace → Modal
```

```
Front Office
├── Reservation Hub ──→ Reservation Workspace ──→ Reservation Modal
└── Reception Hub ──→ Reception Workspace ──→ Reception Modal
```

---

## 3. FUTURE HUBS

Các Hub sau khi triển khai phải dùng chung Design System này:

- Cashiering Hub
- Housekeeping Hub
- Group Hub
- Marketing Hub
- Reports Hub
- Night Audit Hub
- Accounting Hub

Tất cả dùng `Themes/HubCardStyles.xaml` và cùng layout pattern.
