# ASMILE-BO ENTERPRISE WPF UI FRAMEWORK V2

**Phiên bản:** 2.0
**Ngày:** 2026-07-19
**Loại:** Production UI Framework Specification
**Mục tiêu chất lượng:** Microsoft Outlook · Visual Studio · Oracle Opera PMS · Mews PMS

---

## PHẦN 1: KIẾN TRÚC TỔNG THỂ

### 1.1. Project Structure

```
ASMILE.BO.UI/
├── ASMILE.BO.Theme/              ← Design tokens (không code)
│   ├── Colors.xaml
│   ├── Brushes.xaml
│   ├── Typography.xaml
│   ├── Spacing.xaml
│   ├── Radius.xaml
│   ├── Elevation.xaml
│   ├── Opacity.xaml
│   ├── Thickness.xaml
│   ├── ZIndex.xaml
│   ├── Animation.xaml
│   ├── Transitions.xaml
│   └── ControlSizes.xaml
│
├── ASMILE.BO.Controls/           ← Custom WPF controls
│   ├── Primitives/
│   │   ├── EnterpriseButton.cs
│   │   ├── EnterpriseIconButton.cs
│   │   ├── EnterpriseSplitButton.cs
│   │   ├── EnterpriseSearchBox.cs
│   │   ├── EnterpriseTextBox.cs
│   │   ├── EnterpriseComboBox.cs
│   │   ├── EnterpriseDatePicker.cs
│   │   ├── EnterpriseNumericBox.cs
│   │   ├── EnterpriseMoneyBox.cs
│   │   ├── EnterprisePhoneBox.cs
│   │   ├── EnterpriseEmailBox.cs
│   │   └── EnterprisePasswordBox.cs
│   │
│   ├── Data/
│   │   ├── EnterpriseDataGrid.cs
│   │   ├── EnterpriseTreeGrid.cs
│   │   └── EnterprisePropertyGrid.cs
│   │
│   ├── Display/
│   │   ├── EnterpriseCard.cs
│   │   ├── EnterpriseStatisticCard.cs
│   │   ├── EnterpriseDashboardCard.cs
│   │   ├── EnterpriseStatusBadge.cs
│   │   ├── EnterpriseAvatar.cs
│   │   ├── EnterpriseBreadcrumb.cs
│   │   ├── EnterpriseTimeline.cs
│   │   ├── EnterpriseKpiCard.cs
│   │   ├── EnterpriseRoomCard.cs
│   │   ├── EnterpriseGuestCard.cs
│   │   └── EnterpriseReservationCard.cs
│   │
│   ├── Panels/
│   │   ├── EnterpriseDrawer.cs
│   │   ├── EnterpriseFilterPanel.cs
│   │   └── EnterpriseSummaryPanel.cs
│   │
│   ├── Navigation/
│   │   ├── EnterpriseTab.cs
│   │   ├── EnterpriseSidebar.cs
│   │   ├── EnterpriseRibbon.cs
│   │   └── EnterpriseToolbar.cs
│   │
│   └── Menus/
│       └── EnterpriseContextMenu.cs
│
├── ASMILE.BO.Dialogs/            ← Dialog system
│   ├── EnterpriseDialog.cs
│   └── EnterpriseWizard.cs
│
├── ASMILE.BO.Notifications/      ← Notification system
│   ├── EnterpriseToast.cs
│   ├── EnterpriseSnackbar.cs
│   ├── EnterpriseBanner.cs
│   └── EnterpriseProgressNotification.cs
│
├── ASMILE.BO.Animations/         ← Motion system
│   ├── FadeAnimation.cs
│   ├── SlideAnimation.cs
│   ├── ScaleAnimation.cs
│   ├── RippleAnimation.cs
│   └── TransitionService.cs
│
├── ASMILE.BO.Converters/         ← Value converters
│   ├── BoolToVisibilityConverter.cs
│   ├── StatusToColorConverter.cs
│   ├── AmountToCurrencyConverter.cs
│   ├── DateToRelativeConverter.cs
│   └── ... (25+ converters)
│
├── ASMILE.BO.Icons/              ← Icon service
│   ├── IconService.cs
│   └── IconProvider.cs
│
├── ASMILE.BO.Resources/          ← Localization
│   ├── Strings.vi-VN.resx
│   ├── Strings.en-US.resx
│   ├── Strings.zh-CN.resx
│   └── Strings.ja-JP.resx
│
├── ASMILE.BO.AttachedBehaviors/  ← Attached behaviors
│   ├── KeyboardNavigation.cs
│   ├── FocusBehavior.cs
│   ├── ScrollBehavior.cs
│   └── DragDropBehavior.cs
│
└── ASMILE.BO.Navigation/         ← Navigation abstractions
    └── NavigationManager.cs
```

### 1.2. Dependency Graph

```
ASMILE.BO.App (WPF Application)
  ├── ASMILE.BO.Controls
  │     ├── ASMILE.BO.Theme (Colors, Brushes, Typography...)
  │     ├── ASMILE.BO.Icons
  │     └── ASMILE.BO.Converters
  ├── ASMILE.BO.Dialogs
  │     └── ASMILE.BO.Controls
  ├── ASMILE.BO.Notifications
  │     ├── ASMILE.BO.Controls
  │     └── ASMILE.BO.Animations
  ├── ASMILE.BO.Animations
  │     └── ASMILE.BO.Theme
  ├── ASMILE.BO.Resources
  ├── ASMILE.BO.AttachedBehaviors
  └── ASMILE.BO.Navigation
```

---

## PHẦN 2: DESIGN TOKENS

### 2.1. Color System — `Colors.xaml`

```xml
<!-- ===== PRIMARY ===== -->
<Color x:Key="PrimaryColor">#2563EB</Color>
<Color x:Key="PrimaryHoverColor">#1D4ED8</Color>
<Color x:Key="PrimaryPressedColor">#1E40AF</Color>
<Color x:Key="PrimaryLightColor">#DBEAFE</Color>
<Color x:Key="PrimarySubtleColor">#EFF6FF</Color>

<!-- ===== SECONDARY ===== -->
<Color x:Key="SecondaryColor">#64748B</Color>
<Color x:Key="SecondaryHoverColor">#475569</Color>
<Color x:Key="SecondaryLightColor">#F1F5F9</Color>

<!-- ===== ACCENT ===== -->
<Color x:Key="AccentColor">#F2994A</Color>
<Color x:Key="AccentHoverColor">#D97706</Color>
<Color x:Key="AccentLightColor">#FFF7ED</Color>

<!-- ===== SEMANTIC ===== -->
<Color x:Key="SuccessColor">#16A34A</Color>
<Color x:Key="SuccessLightColor">#DCFCE7</Color>
<Color x:Key="WarningColor">#EA580C</Color>
<Color x:Key="WarningLightColor">#FFF7ED</Color>
<Color x:Key="DangerColor">#DC2626</Color>
<Color x:Key="DangerLightColor">#FEE2E2</Color>
<Color x:Key="InfoColor">#2563EB</Color>
<Color x:Key="InfoLightColor">#DBEAFE</Color>

<!-- ===== NEUTRAL ===== -->
<Color x:Key="TextPrimaryColor">#0F172A</Color>
<Color x:Key="TextSecondaryColor">#64748B</Color>
<Color x:Key="TextMutedColor">#94A3B8</Color>
<Color x:Key="TextInverseColor">#FFFFFF</Color>
<Color x:Key="BackgroundColor">#F0F4F8</Color>
<Color x:Key="SurfaceColor">#FFFFFF</Color>
<Color x:Key="BorderColor">#DDE7F3</Color>
<Color x:Key="BorderLightColor">#E5E7EB</Color>
<Color x:Key="DividerColor">#F1F5F9</Color>

<!-- ===== ROOM RACK ===== -->
<Color x:Key="RoomVacantClean">#16A34A</Color>
<Color x:Key="RoomVacantDirty">#94A3B8</Color>
<Color x:Key="RoomOccupied">#2563EB</Color>
<Color x:Key="RoomReserved">#7C3AED</Color>
<Color x:Key="RoomDueOut">#EA580C</Color>
<Color x:Key="RoomOOO">#DC2626</Color>
<Color x:Key="RoomOOS">#475569</Color>
<Color x:Key="RoomVIP">#F2994A</Color>

<!-- ===== MEMBERSHIP ===== -->
<Color x:Key="MembershipStandard">#64748B</Color>
<Color x:Key="MembershipSilver">#94A3B8</Color>
<Color x:Key="MembershipGold">#F2994A</Color>
<Color x:Key="MembershipPlatinum">#7C3AED</Color>
<Color x:Key="MembershipDiamond">#2563EB</Color>
```

### 2.2. Brush System — `Brushes.xaml`

```xml
<!-- Pattern: Mọi Color đều có Brush tương ứng -->
<SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}"/>
<SolidColorBrush x:Key="PrimaryHoverBrush" Color="{StaticResource PrimaryHoverColor}"/>
<SolidColorBrush x:Key="PrimaryLightBrush" Color="{StaticResource PrimaryLightColor}"/>
<SolidColorBrush x:Key="SuccessBrush" Color="{StaticResource SuccessColor}"/>
<SolidColorBrush x:Key="DangerBrush" Color="{StaticResource DangerColor}"/>
<SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimaryColor}"/>
<SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource TextSecondaryColor}"/>
<SolidColorBrush x:Key="SurfaceBrush" Color="{StaticResource SurfaceColor}"/>
<SolidColorBrush x:Key="BackgroundBrush" Color="{StaticResource BackgroundColor}"/>
<SolidColorBrush x:Key="BorderBrush" Color="{StaticResource BorderColor}"/>

<!-- Special: FO Design System prefix -->
<SolidColorBrush x:Key="FO.PrimaryBrush" Color="{StaticResource PrimaryColor}"/>
<SolidColorBrush x:Key="FO.SuccessBrush" Color="{StaticResource SuccessColor}"/>
<SolidColorBrush x:Key="FO.DangerBrush" Color="{StaticResource DangerColor}"/>
<SolidColorBrush x:Key="FO.WarningBrush" Color="{StaticResource WarningColor}"/>
<SolidColorBrush x:Key="FO.Status.ErrorBrush" Color="{StaticResource DangerColor}"/>
```

### 2.3. Typography — `Typography.xaml`

```xml
<FontFamily x:Key="PrimaryFont">Segoe UI Variable Text, Segoe UI, sans-serif</FontFamily>
<FontFamily x:Key="DisplayFont">Segoe UI Variable Display, Segoe UI, sans-serif</FontFamily>
<FontFamily x:Key="MonoFont">Cascadia Code, Consolas, monospace</FontFamily>

<sys:Double x:Key="FontSizeDisplay">28</sys:Double>
<sys:Double x:Key="FontSizeTitle">23</sys:Double>
<sys:Double x:Key="FontSizeHeading">18</sys:Double>
<sys:Double x:Key="FontSizeSubheading">16</sys:Double>
<sys:Double x:Key="FontSizeBody">14</sys:Double>
<sys:Double x:Key="FontSizeCaption">12</sys:Double>
<sys:Double x:Key="FontSizeSmall">11</sys:Double>
<sys:Double x:Key="FontSizeMicro">10</sys:Double>

<FontWeight x:Key="WeightRegular">Regular</FontWeight>
<FontWeight x:Key="WeightSemiBold">SemiBold</FontWeight>
<FontWeight x:Key="WeightBold">Bold</FontWeight>

<Style x:Key="DisplayText" TargetType="TextBlock">
    <Setter Property="FontFamily" Value="{StaticResource DisplayFont}"/>
    <Setter Property="FontSize" Value="{StaticResource FontSizeDisplay}"/>
    <Setter Property="FontWeight" Value="{StaticResource WeightSemiBold}"/>
    <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
</Style>

<Style x:Key="PageTitle" TargetType="TextBlock">
    <Setter Property="FontFamily" Value="{StaticResource PrimaryFont}"/>
    <Setter Property="FontSize" Value="{StaticResource FontSizeTitle}"/>
    <Setter Property="FontWeight" Value="{StaticResource WeightSemiBold}"/>
    <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
</Style>

<Style x:Key="BodyText" TargetType="TextBlock">
    <Setter Property="FontFamily" Value="{StaticResource PrimaryFont}"/>
    <Setter Property="FontSize" Value="{StaticResource FontSizeBody}"/>
    <Setter Property="Foreground" Value="{StaticResource TextSecondaryBrush}"/>
</Style>

<!-- Hungarian: FO-specific typography -->
<Style x:Key="FO.HubPageTitle" BasedOn="{StaticResource PageTitle}" TargetType="TextBlock"/>
<Style x:Key="FO.CardTitle" TargetType="TextBlock">
    <Setter Property="FontSize" Value="{StaticResource FontSizeSubheading}"/>
    <Setter Property="FontWeight" Value="{StaticResource WeightSemiBold}"/>
</Style>
```

### 2.4. Spacing — `Spacing.xaml`

```xml
<sys:Double x:Key="SpaceNone">0</sys:Double>
<sys:Double x:Key="Space1">4</sys:Double>
<sys:Double x:Key="Space2">8</sys:Double>
<sys:Double x:Key="Space3">12</sys:Double>
<sys:Double x:Key="Space4">16</sys:Double>
<sys:Double x:Key="Space5">20</sys:Double>
<sys:Double x:Key="Space6">24</sys:Double>
<sys:Double x:Key="Space8">32</sys:Double>
<sys:Double x:Key="Space10">40</sys:Double>
<sys:Double x:Key="Space12">48</sys:Double>

<sys:Double x:Key="PageMargin">24</sys:Double>
<sys:Double x:Key="CardPadding">20</sys:Double>
<sys:Double x:Key="InputHeight">38</sys:Double>
<sys:Double x:Key="ButtonHeight">40</sys:Double>
<sys:Double x:Key="DataGridRowHeight">42</sys:Double>
<sys:Double x:Key="HeaderHeight">64</sys:Double>
<sys:Double x:Key="FilterBarHeight">48</sys:Double>
<sys:Double x:Key="StatusBarHeight">32</sys:Double>
<sys:Double x:Key="SidebarWidth">78</sys:Double>
<sys:Double x:Key="SidebarExpandedWidth">240</sys:Double>
<sys:Double x:Key="DrawerWidth">380</sys:Double>
<sys:Double x:Key="ModalWidth">640</sys:Double>
<sys:Double x:Key="RoomCardWidth">220</sys:Double>
<sys:Double x:Key="RoomCardHeight">150</sys:Double>
```

### 2.5. Border Radius — `Radius.xaml`

```xml
<CornerRadius x:Key="RadiusNone">0</CornerRadius>
<CornerRadius x:Key="RadiusSm">4</CornerRadius>
<CornerRadius x:Key="RadiusMd">8</CornerRadius>
<CornerRadius x:Key="RadiusLg">10</CornerRadius>
<CornerRadius x:Key="RadiusXl">14</CornerRadius>
<CornerRadius x:Key="RadiusFull">999</CornerRadius>

<!-- Application-specific radius tokens -->
<CornerRadius x:Key="ButtonRadius" StaticResource="RadiusLg"/>
<CornerRadius x:Key="CardRadius" StaticResource="RadiusXl"/>
<CornerRadius x:Key="InputRadius" StaticResource="RadiusLg"/>
<CornerRadius x:Key="BadgeRadius" StaticResource="RadiusSm"/>
```

### 2.6. Elevation — `Elevation.xaml`

```xml
<!-- Shadow definitions as reusable resources -->
<DropShadowEffect x:Key="ShadowLevel1" BlurRadius="3" ShadowDepth="1"
    Opacity="0.06" Color="#000000"/>
<DropShadowEffect x:Key="ShadowLevel2" BlurRadius="8" ShadowDepth="2"
    Opacity="0.08" Color="#000000"/>
<DropShadowEffect x:Key="ShadowLevel3" BlurRadius="14" ShadowDepth="4"
    Opacity="0.10" Color="#000000"/>
<DropShadowEffect x:Key="ShadowLevel4" BlurRadius="24" ShadowDepth="8"
    Opacity="0.12" Color="#000000"/>

<!-- Elevation mapping for semantic usage -->
<!-- Card = Level1, Dropdown = Level2, Dialog = Level3, Drawer = Level4 -->
```

### 2.7. Animation Tokens — `Animation.xaml`

```xml
<Duration x:Key="AnimationFast">0:0:0.15</Duration>
<Duration x:Key="AnimationNormal">0:0:0.25</Duration>
<Duration x:Key="AnimationSlow">0:0:0.40</Duration>
<Duration x:Key="AnimationEntrance">0:0:0.35</Duration>
<Duration x:Key="AnimationExit">0:0:0.20</Duration>

<IEasingFunction x:Key="EaseOutCubic">
    <CubicEase EasingMode="EaseOut"/>
</IEasingFunction>
<IEasingFunction x:Key="EaseInOutCubic">
    <CubicEase EasingMode="EaseInOut"/>
</IEasingFunction>
```

### 2.8. Z-Index — `ZIndex.xaml`

```xml
<sys:Int32 x:Key="ZBase">0</sys:Int32>
<sys:Int32 x:Key="ZDropdown">100</sys:Int32>
<sys:Int32 x:Key="ZSticky">200</sys:Int32>
<sys:Int32 x:Key="ZDrawer">300</sys:Int32>
<sys:Int32 x:Key="ZModal">400</sys:Int32>
<sys:Int32 x:Key="ZDialog">500</sys:Int32>
<sys:Int32 x:Key="ZToast">600</sys:Int32>
<sys:Int32 x:Key="ZTooltip">700</sys:Int32>
<sys:Int32 x:Key="ZLoading">800</sys:Int32>
```

---

## PHẦN 3: CUSTOM CONTROL SPECIFICATIONS

### 3.1. Control Inheritance Tree

```
Control
├── ContentControl
│   ├── EnterpriseButton
│   │   ├── EnterprisePrimaryButton
│   │   ├── EnterpriseSecondaryButton
│   │   ├── EnterpriseDangerButton
│   │   ├── EnterpriseSuccessButton
│   │   └── EnterpriseIconButton
│   ├── EnterpriseSplitButton
│   ├── EnterpriseCard
│   │   ├── EnterpriseStatisticCard
│   │   ├── EnterpriseKpiCard
│   │   ├── EnterpriseRoomCard
│   │   ├── EnterpriseGuestCard
│   │   └── EnterpriseReservationCard
│   └── EnterpriseStatusBadge
│
├── ItemsControl
│   ├── EnterpriseDataGrid
│   │   └── EnterpriseTreeGrid
│   ├── EnterpriseSidebar
│   ├── EnterpriseRibbon
│   ├── EnterpriseBreadcrumb
│   └── EnterpriseTimeline
│
├── TextBoxBase
│   ├── EnterpriseTextBox
│   │   ├── EnterpriseSearchBox
│   │   ├── EnterpriseNumericBox
│   │   ├── EnterpriseMoneyBox
│   │   ├── EnterprisePhoneBox
│   │   └── EnterpriseEmailBox
│   └── EnterprisePasswordBox
│
├── Selector
│   ├── EnterpriseComboBox
│   └── EnterpriseTab
│
├── HeaderedContentControl
│   ├── EnterpriseDrawer
│   └── EnterpriseFilterPanel
│
└── Window
    ├── EnterpriseDialog
    └── EnterpriseWizard
```

### 3.2. EnterpriseButton — ALL States

```xml
<Style TargetType="{x:Type controls:EnterpriseButton}">
    <!-- Default -->
    <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
    <Setter Property="Foreground" Value="{StaticResource TextInverseBrush}"/>
    <Setter Property="Height" Value="{StaticResource ButtonHeight}"/>
    <Setter Property="CornerRadius" Value="{StaticResource ButtonRadius}"/>
    <Setter Property="Padding" Value="16,0"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Setter Property="FontWeight" Value="{StaticResource WeightSemiBold}"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="{x:Type controls:EnterpriseButton}">
                <Border x:Name="border"
                    Background="{TemplateBinding Background}"
                    CornerRadius="{TemplateBinding CornerRadius}"
                    Padding="{TemplateBinding Padding}">
                    <Grid>
                        <!-- Loading Spinner (shown when IsLoading=True) -->
                        <controls:LoadingSpinner x:Name="spinner"
                            Visibility="Collapsed" Width="16" Height="16"/>
                        <!-- Content -->
                        <ContentPresenter x:Name="content"
                            HorizontalAlignment="Center"
                            VerticalAlignment="Center"
                            RecognizesAccessKey="True"/>
                    </Grid>
                    <Border.Effect>
                        <DropShadowEffect x:Name="shadow" BlurRadius="0"
                            ShadowDepth="2" Opacity="0.1"/>
                    </Border.Effect>
                </Border>
                <ControlTemplate.Triggers>
                    <!-- HOVER -->
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter Property="Background"
                            Value="{StaticResource PrimaryHoverBrush}"/>
                        <Setter TargetName="shadow" Property="BlurRadius" Value="4"/>
                    </Trigger>
                    <!-- PRESSED -->
                    <Trigger Property="IsPressed" Value="True">
                        <Setter Property="Background"
                            Value="{StaticResource PrimaryPressedBrush}"/>
                        <Setter TargetName="shadow" Property="BlurRadius" Value="0"/>
                    </Trigger>
                    <!-- FOCUSED -->
                    <Trigger Property="IsKeyboardFocused" Value="True">
                        <Setter TargetName="border" Property="BorderBrush"
                            Value="{StaticResource PrimaryBrush}"/>
                        <Setter TargetName="border" Property="BorderThickness" Value="2"/>
                    </Trigger>
                    <!-- DISABLED -->
                    <Trigger Property="IsEnabled" Value="False">
                        <Setter Property="Background"
                            Value="{StaticResource SecondaryLightBrush}"/>
                        <Setter Property="Foreground"
                            Value="{StaticResource TextMutedBrush}"/>
                    </Trigger>
                    <!-- LOADING -->
                    <DataTrigger Binding="{Binding IsLoading, RelativeSource={RelativeSource Self}}"
                                 Value="True">
                        <Setter TargetName="spinner" Property="Visibility" Value="Visible"/>
                        <Setter TargetName="content" Property="Visibility" Value="Collapsed"/>
                        <Setter Property="IsEnabled" Value="False"/>
                    </DataTrigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

### 3.3. EnterpriseSearchBox

```xml
<Style TargetType="{x:Type controls:EnterpriseSearchBox}">
    <Setter Property="Height" Value="{StaticResource InputHeight}"/>
    <Setter Property="CornerRadius" Value="{StaticResource InputRadius}"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate>
                <Border Background="{StaticResource SurfaceBrush}"
                    BorderBrush="{StaticResource BorderBrush}"
                    BorderThickness="1"
                    CornerRadius="{TemplateBinding CornerRadius}">
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        <!-- Search Icon -->
                        <TextBlock Grid.Column="0" Text="&#xE721;" FontFamily="Segoe MDL2 Assets"
                            Foreground="{StaticResource TextMutedBrush}"
                            VerticalAlignment="Center" Margin="12,0,8,0" FontSize="16"/>
                        <!-- Text Input -->
                        <TextBox Grid.Column="1" x:Name="PART_TextBox"
                            BorderThickness="0" Background="Transparent"
                            VerticalAlignment="Center" FontSize="14"/>
                        <!-- Clear Button -->
                        <Button Grid.Column="2" x:Name="PART_ClearButton"
                            Content="&#xE711;" Style="{StaticResource EnterpriseIconButton}"
                            Visibility="Collapsed" Margin="0,0,8,0"/>
                        <!-- Placeholder -->
                        <TextBlock Grid.Column="1" x:Name="PART_Placeholder"
                            Text="{TemplateBinding Placeholder}"
                            Foreground="{StaticResource TextMutedBrush}"
                            VerticalAlignment="Center" IsHitTestVisible="False"
                            Visibility="Visible"/>
                    </Grid>
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsFocused" Value="True">
                        <Setter Property="BorderBrush" Value="{StaticResource PrimaryBrush}"/>
                    </Trigger>
                    <DataTrigger Binding="{Binding Text.Length, ElementName=PART_TextBox}" Value="0">
                        <Setter TargetName="PART_ClearButton" Property="Visibility" Value="Collapsed"/>
                    </DataTrigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

### 3.4. EnterpriseDataGrid — Virtualization, Grouping, Sorting

```xml
<Style TargetType="{x:Type controls:EnterpriseDataGrid}">
    <Setter Property="RowHeight" Value="{StaticResource DataGridRowHeight}"/>
    <Setter Property="ColumnHeaderHeight" Value="44"/>
    <Setter Property="EnableRowVirtualization" Value="True"/>
    <Setter Property="VirtualizingPanel.ScrollUnit" Value="Pixel"/>
    <Setter Property="VirtualizingPanel.VirtualizationMode" Value="Recycling"/>
    <Setter Property="CanUserSortColumns" Value="True"/>
    <Setter Property="SelectionMode" Value="Extended"/>
    <Setter Property="SelectionUnit" Value="FullRow"/>
    <Setter Property="GridLinesVisibility" Value="None"/>
    <Setter Property="Background" Value="{StaticResource SurfaceBrush}"/>
    <Setter Property="BorderThickness" Value="0"/>
    <Setter Property="AlternatingRowBackground" Value="#F8FAFC"/>
    <Setter Property="RowStyle">
        <Setter.Value>
            <Style TargetType="DataGridRow">
                <Setter Property="Background" Value="Transparent"/>
                <Style.Triggers>
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter Property="Background" Value="{StaticResource PrimaryLightBrush}"/>
                    </Trigger>
                    <Trigger Property="IsSelected" Value="True">
                        <Setter Property="Background" Value="#DBEAFE"/>
                    </Trigger>
                </Style.Triggers>
            </Style>
        </Setter.Value>
    </Setter>
    <Setter Property="ColumnHeaderStyle">
        <Setter.Value>
            <Style TargetType="DataGridColumnHeader">
                <Setter Property="Background" Value="{StaticResource BackgroundBrush}"/>
                <Setter Property="Foreground" Value="{StaticResource TextSecondaryBrush}"/>
                <Setter Property="FontWeight" Value="{StaticResource WeightSemiBold}"/>
                <Setter Property="FontSize" Value="{StaticResource FontSizeCaption}"/>
                <Setter Property="Padding" Value="12,0"/>
                <Setter Property="BorderThickness" Value="0,0,0,1"/>
                <Setter Property="BorderBrush" Value="{StaticResource BorderLightBrush}"/>
            </Style>
        </Setter.Value>
    </Setter>
</Style>
```

### 3.5. EnterpriseDrawer

```xml
<Style TargetType="{x:Type controls:EnterpriseDrawer}">
    <Setter Property="Width" Value="{StaticResource DrawerWidth}"/>
    <Setter Property="Background" Value="{StaticResource SurfaceBrush}"/>
    <Setter Property="BorderBrush" Value="{StaticResource BorderBrush}"/>
    <Setter Property="BorderThickness" Value="1,0,0,0"/>
    <Setter Property="Panel.ZIndex" Value="{StaticResource ZDrawer}"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate>
                <Grid>
                    <!-- Scrim overlay -->
                    <Border x:Name="scrim" Background="#80000000" Opacity="0"
                        Visibility="Collapsed"/>
                    <!-- Drawer panel -->
                    <Border x:Name="drawer"
                        Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}"
                        HorizontalAlignment="Right"
                        Width="{TemplateBinding Width}">
                        <Border.RenderTransform>
                            <TranslateTransform x:Name="slideTransform" X="380"/>
                        </Border.RenderTransform>
                        <ContentPresenter/>
                    </Border>
                </Grid>
                <ControlTemplate.Triggers>
                    <DataTrigger Binding="{Binding IsOpen, RelativeSource={RelativeSource Self}}" Value="True">
                        <DataTrigger.EnterActions>
                            <BeginStoryboard>
                                <Storyboard>
                                    <DoubleAnimation To="0" Storyboard.TargetName="slideTransform"
                                        Storyboard.TargetProperty="X"
                                        Duration="{StaticResource AnimationNormal}"
                                        EasingFunction="{StaticResource EaseOutCubic}"/>
                                    <DoubleAnimation To="0.5" Storyboard.TargetName="scrim"
                                        Storyboard.TargetProperty="Opacity"
                                        Duration="{StaticResource AnimationNormal}"/>
                                </Storyboard>
                            </BeginStoryboard>
                        </DataTrigger.EnterActions>
                    </DataTrigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

---

## PHẦN 4: DIALOG SYSTEM

### 4.1. Dialog Types

| Type | Icon | Buttons | Usage |
|---|---|---|---|
| `ConfirmDialog` | Info | [Cancel] [Confirm] | Xác nhận hành động |
| `DeleteDialog` | Danger | [Cancel] [Delete] | Xác nhận xóa |
| `WarningDialog` | Warning | [OK] | Cảnh báo |
| `InfoDialog` | Info | [OK] | Thông báo |
| `ProgressDialog` | Spinner | [Cancel] | Đang xử lý |
| `WizardDialog` | — | [Back] [Next] [Finish] | Multi-step |

### 4.2. Dialog Template

```xml
<Style TargetType="{x:Type dialogs:EnterpriseDialog}">
    <Setter Property="Width" Value="480"/>
    <Setter Property="MaxHeight" Value="600"/>
    <Setter Property="WindowStyle" Value="None"/>
    <Setter Property="AllowsTransparency" Value="True"/>
    <Setter Property="ResizeMode" Value="NoResize"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate>
                <!-- Modal overlay -->
                <Border Background="#80000000">
                    <!-- Dialog card -->
                    <Border Background="{StaticResource SurfaceBrush}"
                        CornerRadius="{StaticResource RadiusXl}"
                        MaxWidth="{TemplateBinding Width}"
                        MaxHeight="{TemplateBinding MaxHeight}"
                        Padding="24">
                        <Grid>
                            <Grid.RowDefinitions>
                                <RowDefinition Height="Auto"/>
                                <RowDefinition Height="*"/>
                                <RowDefinition Height="Auto"/>
                            </Grid.RowDefinitions>
                            <!-- Header -->
                            <StackPanel Grid.Row="0" Orientation="Horizontal">
                                <controls:EnterpriseStatusBadge
                                    Severity="{TemplateBinding DialogType}"/>
                                <TextBlock Text="{TemplateBinding Title}"
                                    Style="{StaticResource PageTitle}"
                                    Margin="12,0,0,0"/>
                            </StackPanel>
                            <!-- Content -->
                            <ContentPresenter Grid.Row="1"
                                Content="{TemplateBinding Content}" Margin="0,20"/>
                            <!-- Actions -->
                            <StackPanel Grid.Row="2" Orientation="Horizontal"
                                HorizontalAlignment="Right" Margin="0,16,0,0">
                                <Button Content="Cancel" Style="{StaticResource EnterpriseSecondaryButton}"
                                    Command="{TemplateBinding CancelCommand}"/>
                                <Button Content="{TemplateBinding ConfirmText}"
                                    Style="{StaticResource EnterprisePrimaryButton}"
                                    Command="{TemplateBinding ConfirmCommand}" Margin="8,0,0,0"/>
                            </StackPanel>
                        </Grid>
                    </Border>
                </Border>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

---

## PHẦN 5: NOTIFICATION SYSTEM

### 5.1. Notification Types

```csharp
public enum NotificationType
{
    Toast,       // Góc phải dưới, tự động biến mất sau 5s
    Snackbar,    // Giữa dưới, có nút Action, 10s
    Banner,      // Đầu trang, không tự biến mất
    Progress,    // Có progress bar, theo dõi tác vụ dài
    Desktop      // Windows native notification
}

public enum NotificationSeverity
{
    Info,        // Xanh dương
    Success,     // Xanh lá
    Warning,     // Cam
    Error        // Đỏ
}
```

### 5.2. Toast Notification

```xml
<Style TargetType="{x:Type notif:EnterpriseToast}">
    <Setter Property="Width" Value="360"/>
    <Setter Property="HorizontalAlignment" Value="Right"/>
    <Setter Property="VerticalAlignment" Value="Bottom"/>
    <Setter Property="Margin" Value="0,0,20,20"/>
    <Setter Property="Panel.ZIndex" Value="{StaticResource ZToast}"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate>
                <Border Background="{Binding Severity, Converter={StaticResource SevToBg}}"
                    CornerRadius="{StaticResource RadiusLg}" Padding="16"
                    BorderBrush="{Binding Severity, Converter={StaticResource SevToBorder}}"
                    BorderThickness="1">
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Grid.Column="0" Text="{Binding Severity, Converter={StaticResource SevToIcon}}"
                            FontSize="20" VerticalAlignment="Center" Margin="0,0,12,0"/>
                        <StackPanel Grid.Column="1">
                            <TextBlock Text="{Binding Title}" FontWeight="SemiBold" FontSize="14"/>
                            <TextBlock Text="{Binding Message}" FontSize="12"
                                Foreground="{StaticResource TextSecondaryBrush}" TextWrapping="Wrap"/>
                        </StackPanel>
                        <Button Grid.Column="2" Content="✕"
                            Style="{StaticResource EnterpriseIconButton}"
                            Command="{TemplateBinding DismissCommand}"/>
                    </Grid>
                </Border>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

---

## PHẦN 6: ANIMATION / MOTION SYSTEM

### 6.1. Animation Catalog

| Animation | Duration | Easing | Usage |
|---|---|---|---|
| `FadeIn` | 150ms | EaseOut | Element appears |
| `FadeOut` | 150ms | EaseIn | Element disappears |
| `SlideInRight` | 250ms | EaseOutCubic | Drawer opens |
| `SlideOutRight` | 200ms | EaseInCubic | Drawer closes |
| `ScaleIn` | 250ms | EaseOutBack | Dialog opens |
| `ScaleOut` | 150ms | EaseIn | Dialog closes |
| `ExpandVertical` | 300ms | EaseOutCubic | Accordion open |
| `CollapseVertical` | 200ms | EaseInCubic | Accordion close |
| `RippleEffect` | 400ms | EaseOut | Button press feedback |
| `SnackbarSlideUp` | 300ms | EaseOutCubic | Snackbar appears |
| `NavTransition` | 350ms | EaseInOutCubic | Page navigation |

### 6.2. Transition Service

```csharp
public interface ITransitionService
{
    Task FadeInAsync(FrameworkElement element, Duration? duration = null);
    Task FadeOutAsync(FrameworkElement element, Duration? duration = null);
    Task SlideInAsync(FrameworkElement element, SlideDirection direction, Duration? duration = null);
    Task ScaleInAsync(FrameworkElement element, Duration? duration = null);
    void PlayRipple(Point origin, FrameworkElement target);
}
```

---

## PHẦN 7: LAYOUT SYSTEM

### 7.1. Standard Module Layout

```
┌──────────────────────────────────────────────────────────┐
│ HEADER                                     64px          │
│ ┌──────────────────────────────────────────────────────┐ │
│ │ [Icon] Module Title (23px Bold)        [Action Btns] │ │
│ │ KPI1: 12  |  KPI2: 45  |  KPI3: 8  |  KPI4: 2.5M   │ │
│ └──────────────────────────────────────────────────────┘ │
├──────────────────────────────────────────────────────────┤
│ FILTER BAR                                48px          │
│ ┌──────────────────────────────────────────────────────┐ │
│ │ [SearchBox] [Combo▼] [Combo▼] [Date] [Checkbox]     │ │
│ └──────────────────────────────────────────────────────┘ │
├───────────────────────────────────────────┬──────────────┤
│ CONTENT AREA                              │ RIGHT PANEL  │
│ ┌───────────────────────────────────────┐ │ ┌──────────┐ │
│ │                                       │ │ │ Detail   │ │
│ │ DataGrid / RoomGrid / Timeline        │ │ │ Drawer   │ │
│ │                                       │ │ │ 380px    │ │
│ │                                       │ │ │          │ │
│ └───────────────────────────────────────┘ │ └──────────┘ │
├───────────────────────────────────────────┴──────────────┤
│ STATUS BAR                                 32px          │
│ ┌──────────────────────────────────────────────────────┐ │
│ │ Last refresh: 14:32:05      Page 1/5     150 items  │ │
│ └──────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────┘
```

### 7.2. Responsive Breakpoints

```csharp
public enum LayoutBreakpoint
{
    Compact,      // < 1024px — drawer becomes overlay
    Standard,     // 1024-1366px — 60/40 split
    Wide,         // 1366-1920px — 65/35 split
    UltraWide     // > 1920px — 70/30 split, max-width 1600px
}
```

---

## PHẦN 8: ICON SYSTEM

### 8.1. Icon Service

```csharp
public interface IIconService
{
    // Returns XAML Path/Geometry for any icon
    Geometry GetIcon(string iconKey);

    // Returns icon with dynamic color
    DrawingImage GetColoredIcon(string iconKey, Brush color);

    // Icon size variants
    DrawingImage GetIconAtSize(string iconKey, double size);

    // Theme-aware icons (light/dark)
    DrawingImage GetThemedIcon(string iconKey);
}

public enum IconSize
{
    Micro = 10,
    Small = 12,
    Medium = 16,
    Large = 20,
    XLarge = 24,
    Display = 32
}
```

### 8.2. Icon Inventory (Key Icons)

```
IconKey                  | Segoe MDL2 | Usage
─────────────────────────┼────────────┼──────────────────
Search                   |      | Tìm kiếm
Refresh                  |      | Làm mới
Add                      |      | Thêm mới
Edit                     |      | Chỉnh sửa
Delete                   |      | Xóa
Save                     |      | Lưu
Close                    |      | Đóng
CheckIn                  |      | Nhận phòng
CheckOut                 |      | Trả phòng
Room                     |      | Phòng
Guest                    |      | Khách
Calendar                 |      | Lịch
Payment                  |      | Thanh toán
Alert                    |      | Cảnh báo
Lock                     |      | Khóa
Settings                 |      | Cài đặt
Print                    |      | In
Export                   |      | Xuất
Filter                   |      | Lọc
More                     |      | Thêm
Folder                   |      | Thư mục
Document                 |      | Tài liệu
Phone                    |      | Điện thoại
Email                    |      | Email
Person                   |      | Người
Group                    |      | Đoàn
Housekeeping             |      | Dọn phòng
Maintenance              |      | Bảo trì
VIP                      |      | VIP
Star                     |      | Sao
Heart                    |      | Yêu thích
Clock                    |      | Giờ
Check                    |      | Đã chọn
Warning                  |      | Cảnh báo
Error                    |      | Lỗi
Info                     |      | Thông tin
```

---

## PHẦN 9: MVVM RULES

### 9.1. View Construction Rules

```
RULE 1: NO code-behind logic
  All event handlers → Commands (ICommand binding)

RULE 2: NO direct UI manipulation
  All visual changes → Data binding

RULE 3: NO hardcoded strings
  All text → Resources (.resx) or ViewModel properties

RULE 4: NO hardcoded values
  All sizes, colors, fonts → Theme tokens

RULE 5: ONE DataContext per view
  ViewModel = single source of truth

RULE 6: TEMPLATE over code
  DataTemplate for type-based rendering, not procedural

RULE 7: BINDING over code-behind
  {Binding} is always preferred over FindName/FindResource

RULE 8: ASYNC commands
  All I/O-bound commands → AsyncRelayCommand
```

### 9.2. XAML Checklist (MUST PASS)

```yaml
Before commit, verify:
  - [ ] No Margin="5" (use Space tokens)
  - [ ] No Padding="8" (use Space tokens)
  - [ ] No Background="#FFFFFF" (use SurfaceBrush)
  - [ ] No Foreground="Black" (use TextPrimaryBrush)
  - [ ] No CornerRadius="8" (use Radius token)
  - [ ] No FontSize="14" (use FontSize token)
  - [ ] No hardcoded Vietnamese text (use Resource or Unicode escape)
  - [ ] All Color resources used for Brush properties have Brush variant
  - [ ] All commands bound via {Binding}
  - [ ] No event handlers in code-behind
```

---

## PHẦN 10: ACCESSIBILITY

### 10.1. Checklist

```yaml
Implementation required:
  - [ ] All interactive elements have AutomationProperties.Name
  - [ ] Tab order follows visual layout
  - [ ] Focus visible on all controls (2px Primary outline)
  - [ ] Keyboard shortcuts for all primary actions
  - [ ] Screen reader announces: page title, status changes, errors
  - [ ] High contrast mode supported (system theme detection)
  - [ ] All text scales at 200% DPI without clipping
  - [ ] Minimum touch target: 40×40px for all interactive elements
  - [ ] Color not the only indicator (add icons/shapes)
  - [ ] Error messages linked to fields via AutomationProperties
```

### 10.2. Keyboard Navigation Standard

```
Tab         → Next focusable element
Shift+Tab   → Previous focusable element
Arrow Keys  → Navigate within grid/list
Enter       → Activate button / submit form
Space       → Toggle checkbox / open dropdown
Escape      → Close drawer / dialog / dropdown
Ctrl+N      → New item (context-dependent)
Ctrl+F      → Focus search box
F5          → Refresh data
Ctrl+S      → Save current item
Alt+←       → Navigate back
```

---

## PHẦN 11: PERFORMANCE

### 11.1. WPF Performance Rules

```
1. VirtualizingStackPanel for ALL large lists (100+ items)
   - VirtualizingPanel.ScrollUnit="Pixel"
   - VirtualizingPanel.VirtualizationMode="Recycling"

2. Deferred loading
   - x:DeferLoadStrategy="Lazy" for non-visible tabs
   - Load content on first access, not on initialization

3. Template reuse
   - Shared DataTemplates via ResourceDictionary
   - Avoid inline DataTemplates in large lists

4. Binding optimization
   - Mode=OneWay for display-only bindings
   - Mode=OneTime for static data
   - FallbackValue for nullable sources

5. Image optimization
   - Decode to PixelWidth/Height, not full resolution
   - Cache ImageSource objects

6. No unnecessary layout passes
   - Use Canvas for absolute positioning (Tape Chart)
   - Use UniformGrid for equal-sized items (Room Rack)

7. Async loading
   - All DB queries on background thread
   - Progress indicator during load > 200ms

8. Avoid DropShadowEffect on large lists
   - Use Border + Opacity for shadows
   - Shadow only on hover/selected items, not all items
```

---

## PHẦN 12: LOCALIZATION

### 12.1. Resource Strategy

```xml
<!-- Strings.vi-VN.resx -->
<data name="RoomRack.Title" xml:space="preserve">
    <value>Sơ Đồ Phòng</value>
</data>
<data name="CheckIn.Confirm" xml:space="preserve">
    <value>Xác nhận nhận phòng</value>
</data>

<!-- Strings.en-US.resx -->
<data name="RoomRack.Title" xml:space="preserve">
    <value>Room Rack</value>
</data>
<data name="CheckIn.Confirm" xml:space="preserve">
    <value>Confirm Check-in</value>
</data>
```

### 12.2. Usage

```xml
<TextBlock Text="{Binding Source={x:Static resources:Strings.RoomRack_Title}}"/>
```

---

## PHẦN 13: NAMING CONVENTIONS

### 13.1. XAML File Names

```
Pattern: {Module}{Component}View.xaml
Examples:
  RoomRackView.xaml
  RoomRackCardControl.xaml
  CheckInStayModalView.xaml
  ReceptionHubView.xaml
```

### 13.2. Style Key Names

```
Pattern: {Scope}.{Element}.{Variant}
Examples:
  FO.HubCardButton          ← Front Office hub card
  FO.ModalScrim             ← Front Office modal overlay
  FO.Status.ErrorBrush      ← Error status brush
  HK.TaskCard               ← Housekeeping task card
  RSV.ReservationBar        ← Reservation Gantt bar
```

### 13.3. Namespace Convention

```
ASMILE.BO.Controls          ← Custom controls
ASMILE.BO.Controls.Primitives ← Base controls (Button, TextBox)
ASMILE.BO.Controls.Data     ← Data controls (DataGrid, TreeGrid)
ASMILE.BO.Controls.Display   ← Display controls (Card, Badge, Timeline)
ASMILE.BO.Controls.Panels    ← Layout controls (Drawer, FilterPanel)
ASMILE.BO.Controls.Navigation← Navigation (Tab, Sidebar, Ribbon)
```

---

## PHẦN 14: MIGRATION GUIDE

### 14.1. Phase 1: Theme Extraction (1 week)
- Extract all hardcoded colors → Theme tokens
- Extract all hardcoded font sizes → Typography tokens
- Extract all hardcoded margins/padding → Spacing tokens
- Update existing XAML to use tokens
- **Zero visual change. Zero regression.**

### 14.2. Phase 2: Component Library (2 weeks)
- Build EnterpriseButton, EnterpriseSearchBox, EnterpriseCard
- Replace inline button styles with EnterpriseButton
- Build EnterpriseDataGrid wrapper
- Replace inline DataGrid styles

### 14.3. Phase 3: Layout Standardization (2 weeks)
- Apply standard 4-row layout to all FO modules
- Standardize header + filter + content + status bar
- Migrate drawers to EnterpriseDrawer

### 14.4. Phase 4: Polish (1 week)
- Add animations for drawers/dialogs
- Implement toast notification system
- Add loading skeleton screens
- Empty state illustrations

---

## PHẦN 15: IMPLEMENTATION ROADMAP

| Sprint | Deliverable | Duration |
|---|---|---|
| UI-003 | Theme tokens: Colors.xaml, Brushes.xaml, Typography.xaml, Spacing.xaml, Radius.xaml | 3 ngày |
| UI-004 | EnterpriseButton + EnterpriseSearchBox + EnterpriseComboBox | 3 ngày |
| UI-005 | EnterpriseDataGrid + EnterpriseCard + EnterpriseStatusBadge | 3 ngày |
| UI-006 | EnterpriseDrawer + EnterpriseDialog + EnterpriseToast | 3 ngày |
| UI-007 | Animation system + TransitionService | 2 ngày |
| UI-008 | Icon service + EnterpriseSidebar + EnterpriseBreadcrumb | 2 ngày |
| UI-009 | Migration: Room Rack → Enterprise components | 2 ngày |
| UI-010 | Migration: Tape Chart, Group Reservation → Enterprise components | 2 ngày |
| UI-011 | Migration: All FO modules → Enterprise components | 3 ngày |
| UI-012 | Accessibility + Localization infrastructure | 2 ngày |
| UI-013 | Performance optimization + Final audit | 2 ngày |

**Total: ~27 working days (5.5 weeks)**

---

## PHẦN 16: RISK ANALYSIS

| # | Risk | Probability | Impact | Mitigation |
|---|---|---|---|---|
| 1 | Migration breaks existing UI | High | High | Phase-by-phase, zero visual change policy |
| 2 | Custom controls incompatible with existing ViewModels | Medium | Medium | Controls are pure UI — bind to any ViewModel |
| 3 | Performance regression from custom controls | Low | Medium | Virtualization, deferred loading, template reuse |
| 4 | WPF rendering limitations with 5000+ rooms | Medium | High | VirtualizingPanel + Recycling mode |
| 5 | Dark mode complexity | Low | Low | Phase 4 — separate theme dictionary swap |
| 6 | Team unfamiliar with custom WPF controls | Medium | Medium | Start with simple controls (Button, Card), build up |

---

## PHẦN 17: PRODUCTION CHECKLIST

```yaml
Before production release:
  Build:
    - [ ] dotnet build — 0 warnings, 0 errors
    - [ ] All XAML resources load without runtime errors
    - [ ] No missing Style keys at runtime
  
  Visual:
    - [ ] All modules render correctly at 1366×768
    - [ ] All modules render correctly at 1920×1080
    - [ ] All modules render correctly at 2560×1440 (150% DPI)
    - [ ] Dark mode (if implemented) renders correctly
  
  Performance:
    - [ ] Room Rack: 1000 rooms render in < 500ms
    - [ ] DataGrid: 10000 rows scroll at 60fps
    - [ ] Dialog open animation completes in < 250ms
    - [ ] Memory usage < 500MB after 1 hour of use
  
  Accessibility:
    - [ ] Full keyboard navigation through all modules
    - [ ] Screen reader announces all interactive elements
    - [ ] High contrast mode functional
  
  Localization:
    - [ ] Switch to English — all text updates correctly
    - [ ] No hardcoded Vietnamese strings remain
    - [ ] Date/number formats follow locale
```

---

*Enterprise WPF UI Framework V2 specification complete. 17 sections, production quality. This is the single source of truth for all ASMILE-BO UI development.*
