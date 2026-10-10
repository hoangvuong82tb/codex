# ASMILE-BO ENTERPRISE APPLICATION SHELL

## Kiến trúc lõi cho toàn bộ hệ sinh thái ASMILE ERP

**Phiên bản:** 1.0
**Ngày:** 2026-07-19
**Loại:** Enterprise Shell Architecture
**Mục tiêu chất lượng:** Visual Studio · JetBrains Rider · Oracle Opera PMS · SAP · Azure Portal

---

# PHẦN 1: TỔNG QUAN KIẾN TRÚC SHELL

## 1.1. Shell Composition

```
┌─────────────────────────────────────────────────────────────┐
│ MAIN WINDOW (ASMILE-Shell)                                   │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ RIBBON BAR                                      48px    │ │
│ │ [File] [Home] [Front Office] [Housekeeping] [Cashier]   │ │
│ └─────────────────────────────────────────────────────────┘ │
│ ┌──────────┬──────────────────────────────────┬───────────┐ │
│ │ SIDEBAR  │ WORKSPACE AREA                   │ DETAIL    │ │
│ │          │ ┌──────────────────────────────┐ │ PANEL     │ │
│ │ 78px     │ │ MODULE HOST                  │ │           │ │
│ │          │ │ ┌──────────────────────────┐ │ │ 0-400px   │ │
│ │ [FO] ●   │ │ │ Header (64px)            │ │ │           │ │
│ │ [HK]     │ │ │ Title + KPIs + Actions    │ │ │           │ │
│ │ [CS]     │ │ ├──────────────────────────┤ │ │           │ │
│ │ [RV]     │ │ │ Filter Bar (48px)        │ │ │           │ │
│ │ [GR]     │ │ │ [Search] [Combo▼] [Date] │ │ │           │ │
│ │ [RE]     │ │ ├──────────────────────────┤ │ │           │ │
│ │ [BK]     │ │ │ Content                  │ │ │           │ │
│ │ [AC]     │ │ │ DataGrid / RoomGrid /    │ │ │           │ │
│ │          │ │ │ Timeline / Dashboard      │ │ │           │ │
│ │          │ │ └──────────────────────────┘ │ │           │ │
│ │          │ └──────────────────────────────┘ │           │ │
│ └──────────┴──────────────────────────────────┴───────────┘ │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ STATUS BAR                                     32px    │ │
│ │ BD: 19/07/2026 | Shift: Morning | User: Mai Lan         │ │
│ │ Hotel: ASMILE Saigon | DB: ONLINE | Sync: OK | 14:35   │ │
│ └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## 1.2. Shell Layer Architecture

```
Layer 0: MainWindow (Window shell)
  └── Layer 1: ShellLayout (Grid with 4 zones)
        ├── Zone 1: RibbonBar (top)
        ├── Zone 2: Sidebar (left, collapsible)
        ├── Zone 3: WorkspaceArea (center — THIS IS THE CORE)
        │     └── WorkspaceManager
        │           ├── ActiveTabGroup (TabControl — open modules as tabs)
        │           │     ├── Tab: Front Office (ModuleHost)
        │           │     ├── Tab: Room Rack (ModuleHost)
        │           │     └── Tab: Night Audit (ModuleHost)
        │           └── DetailPanel (right, resizable)
        └── Zone 4: StatusBar (bottom)
  └── Layer 2: OverlayLayer (Canvas, Panel.ZIndex high)
        ├── NotificationCenter
        ├── GlobalSearch (Ctrl+K)
        ├── CommandPalette (Ctrl+Shift+P)
        └── ToastNotifications
```

## 1.3. Shell Dependency Graph

```
MainWindow
  ├── ShellViewModel (singleton, lives entire app lifetime)
  │     ├── RibbonViewModel
  │     ├── SidebarViewModel
  │     ├── WorkspaceManager (THE CORE)
  │     │     ├── ActiveWorkspace : IWorkspace
  │     │     ├── OpenDocuments : ObservableCollection<IDocument>
  │     │     └── DetailPanelViewModel
  │     ├── StatusBarViewModel
  │     ├── NotificationCenterViewModel
  │     └── GlobalSearchViewModel
  ├── WindowManager (singleton)
  │     ├── WindowRegistry : Dictionary<string, Window>
  │     ├── Open(IModule)
  │     ├── Close(Guid documentId)
  │     ├── Activate(Guid documentId)
  │     └── RestoreSession()
  ├── NavigationService : INavigationService
  │     ├── History : Stack<NavigationEntry>
  │     ├── NavigateTo(string route, object parameters)
  │     ├── GoBack()
  │     ├── GoForward()
  │     └── CanGoBack / CanGoForward
  └── ModuleRegistry : IModuleRegistry
        ├── Register(IModule module)
        ├── GetModule(string moduleId) : IModule
        └── Modules : IReadOnlyList<IModule>
```

---

# PHẦN 2: WORKSPACE MANAGER — TRÁI TIM CỦA SHELL

## 2.1. WorkspaceManager Interface

```csharp
/// <summary>
/// WorkspaceManager is the SINGLE SOURCE OF TRUTH for all open workspaces.
/// It manages the lifecycle of every document, module, and panel.
/// Architecture: Tab-based MDI (Multiple Document Interface) like Visual Studio.
/// </summary>
public interface IWorkspaceManager
{
    // ===== ACTIVE WORKSPACE =====
    IWorkspace ActiveWorkspace { get; }
    event EventHandler<WorkspaceChangedEventArgs> ActiveWorkspaceChanged;

    // ===== DOCUMENTS =====
    ObservableCollection<IDocument> OpenDocuments { get; }

    // ===== OPERATIONS =====
    Task<IDocument> OpenModuleAsync(string moduleId, object parameters = null);
    Task CloseDocumentAsync(Guid documentId, bool force = false);
    Task ActivateDocumentAsync(Guid documentId);
    Task PinDocumentAsync(Guid documentId);
    Task UnpinDocumentAsync(Guid documentId);

    // ===== LAYOUT =====
    void SplitHorizontally(Guid documentId);
    void SplitVertically(Guid documentId);
    void FloatDocument(Guid documentId);
    void DockDocument(Guid documentId, DockPosition position);

    // ===== DETAIL PANEL =====
    IDetailPanel DetailPanel { get; }
    Task OpenDetailAsync(DetailContent content);
    void CloseDetail();

    // ===== PERSISTENCE =====
    Task<SessionSnapshot> CaptureSessionAsync();
    Task RestoreSessionAsync(SessionSnapshot snapshot);
}
```

## 2.2. IDocument — The Unit of Work

```csharp
/// <summary>
/// IDocument represents ONE open workspace in the shell.
/// Every module, every screen, every view is an IDocument.
/// Documents are displayed as tabs in the workspace area.
/// </summary>
public interface IDocument
{
    Guid DocumentId { get; }
    string Title { get; }
    string ModuleId { get; }
    object Icon { get; }           // Geometry or ImageSource
    object Content { get; }        // The actual WPF View/ViewModel
    bool IsDirty { get; }          // Has unsaved changes?
    bool IsPinned { get; }
    DateTime OpenedAt { get; }
    object Parameters { get; }     // Navigation parameters

    // Lifecycle
    Task OnOpenedAsync();
    Task OnActivatedAsync();
    Task OnDeactivatedAsync();
    Task<bool> OnClosingAsync();   // Return false to prevent close
    Task OnClosedAsync();
}
```

## 2.3. Document Lifecycle

```
Create → Opened → Activated → Deactivated → Activated → Closing → Closed
  │         │          │            │             │          │         │
  │         └─OnOpened └─OnActivated └─OnDeactivated│         └─OnClosed
  │                                                  │
  └── new Document()                          └─ OnClosing (can cancel)
```

## 2.4. Document Dirty State

```csharp
public interface IDirtyDocument : IDocument
{
    // When IsDirty = true:
    //   - Tab shows "*" before title
    //   - Close triggers confirmation dialog
    //   - Save command is enabled
    //   - Session restore warns about unsaved changes

    Task SaveAsync();
    Task DiscardChangesAsync();
    DateTime? LastSavedAt { get; }
}

// Example: Reservation being edited
// Tab shows: "* Đặt phòng RSV-202607-000001"
```

## 2.5. Workspace Profiles

```csharp
public enum WorkspaceProfile
{
    Reception,        // Lễ tân: FO Hub + Check-in + Room Rack
    NightAudit,       // Night Audit: Audit dashboard + Reports
    Manager,          // Quản lý: Dashboard + Revenue + Occupancy
    Cashier,          // Thu ngân: Cashier shift + Payment + Folio
    Housekeeping,     // Buồng phòng: Task board + Room status
    Reservation,      // Đặt phòng: Reservation list + Group + Rate
    Sales,            // Kinh doanh: Rate engine + Corporate + Forecast
    Custom            // User-defined
}

public interface IWorkspaceProfile
{
    WorkspaceProfile ProfileId { get; }
    string DisplayName { get; }
    string[] DefaultModules { get; }     // Auto-open these modules
    string[] DefaultDetailPanels { get; } // Default right panels
    string[] FavoriteCommands { get; }   // Ribbon quick actions
    string LayoutPreset { get; }         // JSON layout definition
}
```

---

# PHẦN 3: MODULE HOST — MÔI TRƯỜNG THỰC THI MODULE

## 3.1. ModuleHost Structure

Mọi module FO đều chạy bên trong `ModuleHost` — một container chuẩn hóa:

```
┌─ ModuleHost ──────────────────────────────────────────────┐
│ ┌─ MODULE HEADER ────────────────────────────────── 64px ─┐│
│ │ [Icon] Module Title (23px)      [Action1] [Action2]     ││
│ │ KPI1: 12  |  KPI2: 45  |  KPI3: 8  |  KPI4: 2.5M VND  ││
│ └─────────────────────────────────────────────────────────┘│
│ ┌─ MODULE TOOLBAR ─────────────────────────────── 48px ───┐│
│ │ [New] [Edit] [Delete] [Refresh] | [Filter▼] [Export▼]   ││
│ └─────────────────────────────────────────────────────────┘│
│ ┌─ MODULE FILTER BAR ──────────────────────────── 48px ───┐│
│ │ [SearchBox] [Combo▼] [Combo▼] [Date] [Checkbox]         ││
│ └─────────────────────────────────────────────────────────┘│
│ ┌─────────────────────────────┬─ DETAIL PANEL ────────────┐│
│ │ MODULE CONTENT              │ (0-400px, resizable)      ││
│ │                             │                           ││
│ │ (UserControl injected       │ Tabs: Info, History,      ││
│ │  by module)                 │ Actions, Related          ││
│ │                             │                           ││
│ └─────────────────────────────┴───────────────────────────┘│
│ ┌─ MODULE STATUS ──────────────────────────────── 32px ───┐│
│ │ Last refresh: 14:32 | Showing 1-50 of 150 | Connected   ││
│ └─────────────────────────────────────────────────────────┘│
└────────────────────────────────────────────────────────────┘
```

## 3.2. ModuleHost Interface

```csharp
public interface IModuleHost
{
    // ===== HEADER =====
    string Title { get; set; }
    object Icon { get; set; }
    ObservableCollection<IKpiIndicator> Kpis { get; }
    ObservableCollection<ICommand> HeaderActions { get; }

    // ===== TOOLBAR =====
    ObservableCollection<IToolbarItem> ToolbarItems { get; }
    ObservableCollection<ICommand> FilterCommands { get; }

    // ===== FILTER =====
    IFilterBar FilterBar { get; }

    // ===== CONTENT =====
    object Content { get; set; }       // Module's UserControl

    // ===== DETAIL PANEL =====
    IDetailPanel DetailPanel { get; }
    bool IsDetailOpen { get; set; }

    // ===== STATUS =====
    string StatusText { get; set; }
    bool IsLoading { get; set; }
    string ErrorMessage { get; set; }
}
```

## 3.3. Module Registration

```csharp
/// <summary>
/// IModule — every module in the ASMILE ecosystem implements this.
/// Registration is ONE-TIME at startup. No MainWindow modification needed.
/// </summary>
public interface IModule
{
    string ModuleId { get; }           // "front-office-reservations"
    string DisplayName { get; }        // "Đặt phòng"
    string Category { get; }           // "Front Office", "Back Office", "POS"
    object Icon { get; }
    int DisplayOrder { get; }
    string[] RequiredPermissions { get; }
    string DefaultRoute { get; }

    Task<IModuleHost> CreateHostAsync(IServiceProvider services, object parameters);
}

public interface IModuleRegistry
{
    void Register(IModule module);
    IModule GetModule(string moduleId);
    IReadOnlyList<IModule> GetModulesByCategory(string category);
    IReadOnlyList<IModule> GetAuthorizedModules(IPrincipal user);
    IReadOnlyList<IModule> SearchModules(string query);
}
```

---

# PHẦN 4: NAVIGATION SYSTEM

## 4.1. Navigation Architecture

```
NavigationService (INavigationService)
  ├── RouteTable : Dictionary<string, Type>
  │     "front-office" → FrontOfficeHubViewModel
  │     "front-office-reservations" → ReservationHubViewModel
  │     "front-office/reception" → ReceptionHubViewModel
  │     "front-office/reception/room-rack" → RoomRackViewModel
  │     "front-office/reception/check-in" → CheckInStayModalViewModel
  ├── NavigationStack : Stack<NavigationEntry>
  │     ┌─────────────────────────────────┐
  │     │ Entry 3: Room Rack (current)    │ ← Top
  │     │ Entry 2: Reception Hub          │
  │     │ Entry 1: Front Office Hub       │
  │     │ Entry 0: Desktop                │ ← Bottom
  │     └─────────────────────────────────┘
  └── ForwardStack : Stack<NavigationEntry>
```

## 4.2. INavigationService

```csharp
public interface INavigationService
{
    // ===== CORE =====
    Task<NavigationResult> NavigateToAsync(string route, object parameters = null);
    Task<NavigationResult> GoBackAsync();
    Task<NavigationResult> GoForwardAsync();
    bool CanGoBack { get; }
    bool CanGoForward { get; }

    // ===== HISTORY =====
    IReadOnlyList<NavigationEntry> History { get; }
    Task ClearHistoryAsync();

    // ===== BOOKMARKS =====
    Task AddBookmarkAsync(string route, string title);
    Task RemoveBookmarkAsync(string route);
    IReadOnlyList<Bookmark> Bookmarks { get; }

    // ===== DEEP LINK =====
    Uri GenerateDeepLink(string route, object parameters);
    Task NavigateToDeepLinkAsync(Uri uri);

    // ===== RECENTS =====
    IReadOnlyList<NavigationEntry> RecentDestinations { get; }
}
```

## 4.3. Route Definition

```csharp
// Routes use hierarchical pattern: {category}/{module}/{sub-module}
public static class ShellRoutes
{
    // Desktop
    public const string Desktop = "/";

    // Front Office
    public const string FrontOffice = "/front-office";
    public const string Reservation = "/front-office/reservation";
    public const string Reception = "/front-office/reception";
    public const string RoomRack = "/front-office/reception/room-rack";
    public const string TapeChart = "/front-office/reception/tape-chart";
    public const string CheckIn = "/front-office/reception/check-in";
    public const string CheckOut = "/front-office/reception/check-out";
    public const string GroupReservation = "/front-office/reception/group";
    public const string Cashier = "/front-office/cashier";
    public const string NightAudit = "/front-office/night-audit";
    public const string Housekeeping = "/front-office/housekeeping";

    // Back Office
    public const string Accounting = "/back-office/accounting";
    public const string Inventory = "/back-office/inventory";
    public const string Reports = "/back-office/reports";

    // POS
    public const string Restaurant = "/pos/restaurant";
    public const string Banquet = "/pos/banquet";
    public const string BreakfastCafe = "/pos/breakfast-cafe";
}
```

## 4.4. Navigation Guards

```csharp
public interface INavigationGuard
{
    /// <summary>
    /// Called BEFORE navigation. Return false to block.
    /// Example: unsaved changes guard
    /// </summary>
    Task<bool> CanNavigateAsync(NavigationEntry from, NavigationEntry to);
}

// Example: Dirty document guard
public class DirtyDocumentGuard : INavigationGuard
{
    public async Task<bool> CanNavigateAsync(NavigationEntry from, NavigationEntry to)
    {
        if (from?.Document is IDirtyDocument dirty && dirty.IsDirty)
        {
            var result = await DialogService.ShowAsync(
                "Unsaved Changes",
                "You have unsaved changes. Save before leaving?",
                DialogButtons.SaveDiscardCancel);
            return result != DialogResult.Cancel;
        }
        return true;
    }
}
```

---

# PHẦN 5: WINDOW MANAGER

## 5.1. Window Manager Architecture

```csharp
public interface IWindowManager
{
    // ===== REGISTRY =====
    IReadOnlyDictionary<Guid, Window> ActiveWindows { get; }

    // ===== OPERATIONS =====
    Task<Window> OpenWindowAsync(IModuleHost host, WindowOptions options = null);
    Task CloseWindowAsync(Guid windowId);
    Task ActivateWindowAsync(Guid windowId);
    Task MinimizeWindowAsync(Guid windowId);
    Task MaximizeWindowAsync(Guid windowId);
    Task RestoreWindowAsync(Guid windowId);

    // ===== LIFETIME =====
    event EventHandler<WindowEventArgs> WindowOpened;
    event EventHandler<WindowEventArgs> WindowClosed;
    event EventHandler<WindowEventArgs> WindowActivated;

    // ===== LAYOUT =====
    Task TileWindowsAsync();
    Task CascadeWindowsAsync();
    Task FloatWindowAsync(Guid windowId);
    Task DockWindowAsync(Guid windowId, DockPosition position);

    // ===== PERSISTENCE =====
    Task<WindowStateSnapshot> CaptureWindowStatesAsync();
    Task RestoreWindowStatesAsync(WindowStateSnapshot snapshot);
}

public class WindowOptions
{
    public double? Width { get; set; }
    public double? Height { get; set; }
    public double? Left { get; set; }
    public double? Top { get; set; }
    public WindowStartupLocation StartupLocation { get; set; }
    public bool IsFloating { get; set; }
    public bool PreventDuplicate { get; set; }  // Only one instance
}
```

## 5.2. Window Cache

```csharp
/// <summary>
/// WindowCache keeps recently-closed windows in memory
/// for instant re-open. Configurable cache size and TTL.
/// </summary>
public interface IWindowCache
{
    Task<Window> GetOrCreateAsync(string moduleId, Func<Task<Window>> factory);
    void Invalidate(string moduleId);
    void Clear();
    int CacheSize { get; set; }     // Default: 5
    TimeSpan CacheTtl { get; set; } // Default: 5 minutes
}
```

---

# PHẦN 6: RIBBON BAR

## 6.1. Ribbon Architecture

```
┌────────────────────────────────────────────────────────────┐
│ [≡ File] [Home] [Front Office] [Housekeeping] [Cashier]   │
│ [Night Audit] [Reports] [Settings]                         │
│                                                    [👤 ML]│
├────────────────────────────────────────────────────────────┤
│ CONTEXTUAL TAB (appears based on active module)            │
│ [New Res] [CheckIn] [CheckOut] [Room Move] | [Refresh]    │
│ [Folio] [Payment] [Guest Profile] | [Export] [Print]      │
└────────────────────────────────────────────────────────────┘
```

## 6.2. Ribbon Interface

```csharp
public interface IRibbonService
{
    // ===== TABS =====
    ObservableCollection<IRibbonTab> Tabs { get; }
    IRibbonTab ActiveTab { get; set; }

    // ===== CONTEXTUAL =====
    void ShowContextualTab(IRibbonTab tab);
    void HideContextualTab();

    // ===== QUICK ACCESS =====
    ObservableCollection<ICommand> QuickAccessCommands { get; }
    void AddToQuickAccess(ICommand command);
    void RemoveFromQuickAccess(ICommand command);

    // ===== FAVORITES =====
    ObservableCollection<ICommand> FavoriteCommands { get; }
    void ToggleFavorite(ICommand command);
}

public interface IRibbonTab
{
    string Header { get; }
    object Icon { get; }
    ObservableCollection<IRibbonGroup> Groups { get; }
    bool IsContextual { get; }
    string ContextualColor { get; }   // Hex color for tab highlight
}

public interface IRibbonGroup
{
    string Header { get; }
    ObservableCollection<IRibbonCommand> Commands { get; }
}
```

---

# PHẦN 7: SIDEBAR

## 7.1. Sidebar Architecture

```
┌──────┐
│  ≡   │ ← ASMILE Logo / App menu
│      │
│ [FO] │ ← Front Office (active — blue indicator)
│ [HK] │ ← Housekeeping
│ [CS] │ ← Cashier
│ [RV] │ ← Reservation
│ [GR] │ ← Group
│ [RE] │ ← Rate Engine
│ [BK] │ ← Back Office
│ [AC] │ ← Accounting
│      │
│ ──── │ ← Separator
│      │
│ [★] │ ← Favorites
│ [🕐] │ ← Recent
│      │
│ [⚙] │ ← Settings
│ [👤] │ ← User menu
└──────┘
```

## 7.2. Sidebar Interface

```csharp
public interface ISidebarService
{
    ObservableCollection<ISidebarItem> Items { get; }
    ISidebarItem ActiveItem { get; set; }
    bool IsExpanded { get; set; }
    double ExpandedWidth { get; set; }   // Default: 240px
    double CollapsedWidth { get; set; }  // Default: 78px

    event EventHandler<SidebarItemClickedEventArgs> ItemClicked;
}

public interface ISidebarItem
{
    string Id { get; }
    string Label { get; }
    object Icon { get; }
    string Badge { get; }              // "3" for notification count
    string BadgeColor { get; }         // Hex color
    bool IsEnabled { get; }
    bool IsVisible { get; }
    IEnumerable<string> RequiredPermissions { get; }
    string Route { get; }              // Navigation route
}
```

---

# PHẦN 8: STATUS BAR

## 8.1. Status Bar Layout

```
┌────────────────────────────────────────────────────────────┐
│ BD: 19/07/2026 | Ca: Sáng | User: Mai Lan                 │
│ Hotel: ASMILE Saigon | DB: ONLINE | Sync: ✅ | 14:35:22   │
│ Pending tasks: 3 | Alerts: 2 |                             │
└────────────────────────────────────────────────────────────┘
```

## 8.2. StatusBar Interface

```csharp
public interface IStatusBarService
{
    ObservableCollection<IStatusBarItem> LeftItems { get; }
    ObservableCollection<IStatusBarItem> RightItems { get; }
    void ShowProgress(string message, int percent);
    void HideProgress();
    void ShowTemporaryMessage(string message, TimeSpan duration);
}

public interface IStatusBarItem
{
    string Label { get; }
    string Value { get; }
    object Icon { get; }
    ICommand ClickCommand { get; }    // Optional: click to open
}
```

---

# PHẦN 9: COMMAND PALETTE (Ctrl+Shift+P)

## 9.1. Command Palette Architecture

```
┌─────────────────────────────────────────┐
│ ▸ check in                              │
├─────────────────────────────────────────┤
│ 🏨 Check In — Nhận phòng                │
│ 🏨 Check Out — Trả phòng                │
│ 📋 Create Reservation — Tạo đặt phòng   │
│ 🔍 Search Guest — Tìm khách             │
│ 💰 Receive Payment — Nhận thanh toán    │
│ 🧹 Housekeeping Task — Giao việc dọn    │
│ 📊 Night Audit — Kiểm đêm               │
│ ⚙ Settings — Cài đặt                    │
└─────────────────────────────────────────┘
```

## 9.2. Command Palette Interface

```csharp
public interface ICommandPaletteService
{
    Task ShowAsync();
    void Hide();
    bool IsOpen { get; }

    // Commands are registered by modules — no central registration needed
    void RegisterCommand(ICommandPaletteCommand command);
    void UnregisterCommand(string commandId);
}

public interface ICommandPaletteCommand
{
    string CommandId { get; }
    string DisplayName { get; }
    string Category { get; }
    string Description { get; }
    string[] Keywords { get; }
    object Icon { get; }
    ICommand ExecuteCommand { get; }
    string[] RequiredPermissions { get; }
    bool IsAvailable { get; }     // Context-aware: only show when applicable
}
```

---

# PHẦN 10: GLOBAL SEARCH (Ctrl+K)

## 10.1. Global Search Architecture

```
┌──────────────────────────────────────────────┐
│ ▸ nguyen van a                               │
├──────────────────────────────────────────────┤
│ 👤 Nguyễn Văn A — Guest Profile              │
│ 🏨 STY-202607-000001 — Stay, Room 101        │
│ 📋 RSV-202607-000001 — Reservation           │
│ 💰 PMT-202607-000001 — Payment, 5,000,000    │
│ 📄 INV-202607-000001 — Invoice               │
│ 🏢 ABC Corp — Company                        │
│ 📝 SRV-202607-000001 — Service Request       │
└──────────────────────────────────────────────┘
```

## 10.2. GlobalSearch Interface

```csharp
public interface IGlobalSearchService
{
    Task ShowAsync();
    void Hide();
    bool IsOpen { get; }

    // Search providers register themselves
    void RegisterProvider(ISearchProvider provider);
    void UnregisterProvider(string providerId);

    Task<SearchResults> SearchAsync(string query, CancellationToken ct);
}

public interface ISearchProvider
{
    string ProviderId { get; }
    string DisplayName { get; }
    int Priority { get; }       // Higher = shown first
    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int maxResults, CancellationToken ct);
}

public class SearchResult
{
    public string Title { get; set; }
    public string Subtitle { get; set; }
    public string Category { get; set; }
    public object Icon { get; set; }
    public string Route { get; set; }        // Navigation route on click
    public object Parameters { get; set; }
    public double RelevanceScore { get; set; }
}
```

## 10.3. Registered Search Providers

| Provider | Searches | Priority |
|---|---|---|
| GuestSearchProvider | Guest name, phone, email, guest number | 10 |
| ReservationSearchProvider | Reservation number, guest | 9 |
| StaySearchProvider | Stay number, room, guest | 9 |
| RoomSearchProvider | Room number, type, floor | 8 |
| PaymentSearchProvider | Payment number, amount, reference | 7 |
| InvoiceSearchProvider | Invoice number, guest | 7 |
| CompanySearchProvider | Company name, tax code | 6 |
| AgentSearchProvider | Agency name, code | 5 |
| ServiceRequestSearchProvider | Request number, type | 5 |
| ConciergeSearchProvider | Concierge request number | 4 |
| LostFoundSearchProvider | Item name, number | 4 |
| MessageSearchProvider | Message content, guest | 3 |
| FeedbackSearchProvider | Feedback content, guest | 3 |
| AuditSearchProvider | Audit entry, entity | 2 |

---

# PHẦN 11: NOTIFICATION CENTER

## 11.1. Notification Drawer

```
┌─ Notification Center ────────────────────────┐
│ 🔔 Thông báo (3)          [Mark All Read]    │
├──────────────────────────────────────────────┤
│ ● VIP Arrival             14:00              │
│   Lê Văn C sẽ đến lúc 14:00. Chuẩn bị P301  │
│                                              │
│ ● Outstanding Balance     13:45              │
│   Phạm Thị D còn nợ 2,500,000 VND           │
│   [Open Folio]                               │
│                                              │
│ ✓ Check-in Completed      13:30              │
│   Nguyễn Văn A — Phòng 101                  │
│                                              │
│ ⚠ Room 105 OOO            13:00              │
│   Phòng 105 Out Of Order — cần bảo trì       │
│   [Open Maintenance]                         │
└──────────────────────────────────────────────┘
```

---

# PHẦN 12: SESSION RESTORE

## 12.1. Session Snapshot

```csharp
public class SessionSnapshot
{
    public DateTime CapturedAt { get; set; }
    public string UserId { get; set; }
    public string MachineName { get; set; }

    // Active workspace state
    public string ActiveModuleRoute { get; set; }
    public List<OpenDocumentState> OpenDocuments { get; set; }

    // Layout state
    public string LayoutJson { get; set; }          // Dock layout
    public string SidebarState { get; set; }         // Expanded/collapsed
    public string DetailPanelState { get; set; }     // Open/closed, width

    // Filter state
    public Dictionary<string, string> FilterStates { get; set; }

    // Window positions
    public List<WindowStateSnapshot> FloatingWindows { get; set; }
}

public class OpenDocumentState
{
    public string ModuleId { get; set; }
    public string Route { get; set; }
    public string Title { get; set; }
    public bool IsPinned { get; set; }
    public bool IsDirty { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
}

public class WindowStateSnapshot
{
    public Guid WindowId { get; set; }
    public string ModuleId { get; set; }
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsMaximized { get; set; }
    public bool IsFloating { get; set; }
}
```

## 12.2. Session Persistence

```csharp
public interface ISessionService
{
    // Auto-capture on interval (every 30s) and on close
    Task<SessionSnapshot> CaptureCurrentSessionAsync();

    // Restore on startup
    Task RestoreSessionAsync(SessionSnapshot snapshot);

    // Persistence
    Task SaveSessionAsync(SessionSnapshot snapshot, string filePath);
    Task<SessionSnapshot> LoadSessionAsync(string filePath);

    // Auto-save
    bool AutoSaveEnabled { get; set; }
    TimeSpan AutoSaveInterval { get; set; }  // Default: 30 seconds
    string AutoSavePath { get; }             // %AppData%/ASMILE/sessions/
}
```

---

# PHẦN 13: DOCKING SYSTEM

## 13.1. Dock Positions

```csharp
public enum DockPosition
{
    Left,
    Right,
    Top,
    Bottom,
    Fill,        // Takes remaining space
    Float        // Independent window
}

public enum DockState
{
    Docked,
    Floating,
    AutoHidden,
    Tabbed,
    Hidden
}
```

## 13.2. Docking Layout XML

```xml
<DockingLayout>
  <LayoutRoot>
    <LayoutPanel Orientation="Horizontal">
      <!-- Left dock: Sidebar -->
      <LayoutAnchorablePane DockWidth="78" DockMinWidth="78">
        <LayoutAnchorable Title="Sidebar" ContentId="sidebar"/>
      </LayoutAnchorablePane>

      <!-- Center: Document area -->
      <LayoutDocumentPaneGroup>
        <LayoutDocumentPane>
          <LayoutDocument Title="Room Rack" ContentId="room-rack"/>
          <LayoutDocument Title="Front Office" ContentId="front-office"/>
        </LayoutDocumentPane>
      </LayoutDocumentPaneGroup>

      <!-- Right dock: Detail panel -->
      <LayoutAnchorablePane DockWidth="380" DockMinWidth="300">
        <LayoutAnchorable Title="Detail" ContentId="detail-panel"/>
      </LayoutAnchorablePane>
    </LayoutPanel>

    <!-- Bottom dock: Status -->
    <LayoutAnchorablePaneGroup DockHeight="32" Orientation="Horizontal">
      <LayoutAnchorablePane>
        <LayoutAnchorable Title="Status Bar" ContentId="status-bar"/>
      </LayoutAnchorablePane>
    </LayoutAnchorablePaneGroup>
  </LayoutRoot>
</DockingLayout>
```

---

# PHẦN 14: PLUGIN / MODULE SYSTEM

## 14.1. Module Loading Pipeline

```
Application Start
  ↓
ModuleRegistry.DiscoverModules()
  ├── Scan assemblies for IModule implementations
  ├── Validate module metadata
  ├── Register route → module mapping
  └── Build sidebar item list
  ↓
User clicks module
  ↓
ModuleRegistry.CreateHostAsync(moduleId)
  ├── Check permissions
  ├── Create DI scope
  ├── Resolve IModuleHost
  ├── Module.InitializeAsync(host)
  └── Return configured IModuleHost
  ↓
WorkspaceManager.OpenDocumentAsync(moduleHost)
  ├── Create IDocument
  ├── Add to OpenDocuments collection
  ├── Activate tab
  └── Raise ActiveWorkspaceChanged
```

## 14.2. Module Discovery

```csharp
public class ModuleDiscoveryService
{
    public async Task<IReadOnlyList<IModule>> DiscoverModulesAsync()
    {
        var modules = new List<IModule>();

        // Scan all loaded assemblies for IModule implementations
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var moduleTypes = assembly.GetTypes()
                .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var type in moduleTypes)
            {
                var module = (IModule)Activator.CreateInstance(type);
                modules.Add(module);
            }
        }

        // Sort by category, then display order
        return modules
            .OrderBy(m => m.Category)
            .ThenBy(m => m.DisplayOrder)
            .ToList();
    }
}
```

## 14.3. Module Manifest Example

```csharp
[ModuleMetadata(
    ModuleId = "front-office-reservations",
    DisplayName = "Đặt phòng",
    Category = "Front Office",
    DisplayOrder = 10,
    RequiredPermissions = new[] { "FrontOffice.Reservation.View" },
    DefaultRoute = "/front-office/reservation"
)]
public class ReservationModule : IModule
{
    public async Task<IModuleHost> CreateHostAsync(IServiceProvider services, object parameters)
    {
        var host = services.GetRequiredService<IModuleHost>();
        var vm = services.GetRequiredService<ReservationWorkspaceViewModel>();
        await vm.InitializeAsync(parameters);
        host.Title = "Đặt phòng";
        host.Content = new ReservationWorkspaceView { DataContext = vm };
        return host;
    }
}
```

---

# PHẦN 15: PERFORMANCE STRATEGY

## 15.1. Shell Performance Rules

```
1. LAZY MODULE LOADING
   - Modules chỉ load khi user click lần đầu
   - Assembly chỉ scan khi cần
   - View chỉ tạo khi module được mở

2. MODULE CACHING
   - ModuleHost cache: giữ 5 module gần nhất trong memory
   - ViewModel cache: tái sử dụng VM nếu cùng tham số
   - Data cache: kết quả query cache 30 giây

3. NAVIGATION CACHE
   - Keep-alive: module đã mở không reload khi chuyển tab
   - Background data refresh khi module không active

4. ASYNC LOADING
   - Mọi thao tác I/O (DB, file, network) → async
   - Shell không block UI thread > 16ms
   - Loading indicator cho thao tác > 200ms

5. MEMORY MANAGEMENT
   - Dispose module khi đóng (nếu không trong cache)
   - WeakReference cho data cache
   - Memory limit: 800MB → force GC + trim cache
   - Auto-close inactive modules after 30 minutes

6. STARTUP OPTIMIZATION
   - Splash screen hiển thị trong < 500ms
   - Shell ready trong < 2 giây
   - Module đầu tiên mở trong < 1 giây
   - Background loading cho module cache warm-up
```

---

# PHẦN 16: SECURITY STRATEGY

## 16.1. Permission-Aware Shell

```csharp
// Mọi thành phần shell đều permission-aware
public class PermissionAwareSidebarItem : ISidebarItem
{
    public bool IsVisible => _authService.HasAllPermissions(RequiredPermissions);
    // Module tự động ẩn nếu user không có quyền
}

public class PermissionAwareRibbonCommand : IRibbonCommand
{
    public bool IsEnabled => _authService.HasPermission(RequiredPermission);
    // Command tự động disabled nếu user không có quyền
}

// Authorization flow
// 1. Module đăng ký required permissions
// 2. Shell kiểm tra permissions khi render
// 3. Module kiểm tra permissions khi execute command
// 4. Audit log mọi lần permission check thất bại
```

---

# PHẦN 17: FOLDER STRUCTURE

```
ASMILE.BO.Shell/
├── MainWindow.xaml                    ← Shell window
├── MainWindow.xaml.cs
├── ShellViewModel.cs                  ← Master ViewModel
│
├── Workspace/
│   ├── WorkspaceManager.cs            ← IWorkspaceManager
│   ├── Document.cs                    ← IDocument implementation
│   ├── WorkspaceProfile.cs            ← Profile definitions
│   └── SessionService.cs              ← Session capture/restore
│
├── Navigation/
│   ├── NavigationService.cs           ← INavigationService
│   ├── RouteRegistry.cs               ← Route table
│   ├── NavigationEntry.cs
│   └── NavigationGuard.cs
│
├── Ribbon/
│   ├── RibbonService.cs               ← IRibbonService
│   ├── RibbonTab.cs
│   └── RibbonGroup.cs
│
├── Sidebar/
│   ├── SidebarService.cs              ← ISidebarService
│   └── SidebarItem.cs
│
├── StatusBar/
│   └── StatusBarService.cs
│
├── Search/
│   ├── GlobalSearchService.cs         ← IGlobalSearchService
│   └── SearchProviders/
│       ├── GuestSearchProvider.cs
│       ├── ReservationSearchProvider.cs
│       ├── RoomSearchProvider.cs
│       └── ...
│
├── Commands/
│   ├── CommandPaletteService.cs       ← ICommandPaletteService
│   └── CommandRegistry.cs
│
├── Windows/
│   ├── WindowManager.cs               ← IWindowManager
│   └── WindowCache.cs
│
├── Docking/
│   ├── DockingService.cs
│   └── DockLayoutSerializer.cs
│
├── Notifications/
│   ├── NotificationCenter.cs
│   └── NotificationProviders/
│
├── Modules/
│   ├── IModule.cs                     ← Module contract
│   ├── ModuleRegistry.cs
│   ├── ModuleHost.cs                  ← IModuleHost
│   ├── ModuleDiscoveryService.cs
│   └── ModuleMetadataAttribute.cs
│
├── Session/
│   ├── SessionSnapshot.cs
│   └── SessionRestoreService.cs
│
└── Security/
    ├── PermissionAwareRenderer.cs
    └── ShellAuthorizationService.cs
```

---

# PHẦN 18: CLASS DIAGRAM (Core)

```
┌────────────────────┐
│    MainWindow      │
│  (Window Shell)    │
└────────┬───────────┘
         │ DataContext
         ▼
┌────────────────────┐
│  ShellViewModel    │← singleton, lifetime = app
├────────────────────┤
│ + Ribbon           │──→ IRibbonService
│ + Sidebar          │──→ ISidebarService
│ + WorkspaceManager │──→ IWorkspaceManager
│ + StatusBar        │──→ IStatusBarService
│ + Navigation       │──→ INavigationService
│ + Notifications    │──→ INotificationCenter
│ + GlobalSearch     │──→ IGlobalSearchService
│ + CommandPalette   │──→ ICommandPaletteService
│ + WindowManager    │──→ IWindowManager
│ + ModuleRegistry   │──→ IModuleRegistry
│ + Session          │──→ ISessionService
└────────────────────┘

┌────────────────────────┐
│   IWorkspaceManager    │
├────────────────────────┤
│ + ActiveWorkspace      │
│ + OpenDocuments        │
│ + OpenModuleAsync()    │
│ + CloseDocumentAsync() │
│ + ActivateDocumentAsync│
│ + PinDocumentAsync()   │
│ + CaptureSessionAsync()│
└────────┬───────────────┘
         │ manages
         ▼
┌────────────────────────┐
│      IDocument         │
├────────────────────────┤
│ + DocumentId           │
│ + Title                │
│ + ModuleId             │
│ + Content              │← IModuleHost
│ + IsDirty              │
│ + IsPinned             │
│ + OnOpenedAsync()      │
│ + OnActivatedAsync()   │
│ + OnClosingAsync()     │
│ + OnClosedAsync()      │
└────────────────────────┘
```

---

# PHẦN 19: MVVM FLOW (Sequence Diagram)

```
User clicks "Room Rack" in sidebar
  │
  ▼
SidebarService.ItemClicked → event
  │
  ▼
ShellViewModel.OnSidebarItemClicked(route: "/front-office/reception/room-rack")
  │
  ▼
NavigationService.NavigateToAsync("/front-office/reception/room-rack")
  │
  ├── Check NavigationGuards (dirty document check)
  │
  ▼
ModuleRegistry.GetModule("room-rack")
  │
  ▼
ModuleRegistry.CreateHostAsync(services, parameters)
  │
  ├── Create DI scope
  ├── Resolve RoomRackViewModel
  ├── Resolve RoomRackView
  ├── Create IModuleHost
  ├── Set host.Content = roomRackView
  └── Return configured host
  │
  ▼
WorkspaceManager.OpenDocumentAsync(moduleHost)
  │
  ├── Create IDocument from moduleHost
  ├── Add to OpenDocuments collection
  ├── ActivateDocumentAsync(documentId)
  ├── Set ShellViewModel.ActiveWorkspace
  └── Raise ActiveWorkspaceChanged event
  │
  ▼
View renders:
  - Ribbon updates contextual tab
  - Sidebar highlights active item
  - Workspace shows Room Rack
  - Status bar updates
```

---

# PHẦN 20: IMPLEMENTATION ROADMAP

| Sprint | Deliverable | Duration |
|---|---|---|
| SH-001 | Shell window + Grid layout + 4 zones | 3 ngày |
| SH-002 | Sidebar + Ribbon + StatusBar (static) | 3 ngày |
| SH-003 | WorkspaceManager + IDocument + TabControl | 4 ngày |
| SH-004 | ModuleHost + IModule + ModuleRegistry + Discovery | 3 ngày |
| SH-005 | NavigationService + RouteRegistry + History stack | 3 ngày |
| SH-006 | WindowManager + Float/Dock + Cache | 3 ngày |
| SH-007 | CommandPalette (Ctrl+Shift+P) + CommandRegistry | 2 ngày |
| SH-008 | GlobalSearch (Ctrl+K) + 14 search providers | 3 ngày |
| SH-009 | NotificationCenter + Notification providers | 2 ngày |
| SH-010 | Session capture/restore + Auto-save | 2 ngày |
| SH-011 | Docking system (AvalonDock integration) | 3 ngày |
| SH-012 | Permission-aware shell + Authorization guards | 2 ngày |
| SH-013 | Performance optimization + Startup time | 2 ngày |
| SH-014 | Migration: all FO modules register via IModule | 3 ngày |
| SH-015 | Integration testing + Bug fixes | 3 ngày |

**Total: ~38 working days (7.5 weeks)**

---

# PHẦN 21: RISK ANALYSIS

| # | Risk | Probability | Impact | Mitigation |
|---|---|---|---|---|
| 1 | Breaking existing window management | High | High | Keep existing WindowManagerService as fallback |
| 2 | Performance regression on startup | Medium | High | Lazy module loading, splash screen |
| 3 | AvalonDock integration complexity | Medium | Medium | Start with simple TabControl, upgrade later |
| 4 | Session restore fails after crash | Low | High | Auto-save every 30s, validate snapshot on load |
| 5 | Permission checks slow down UI | Low | Medium | Cache permission results, async check |
| 6 | Module migration too time-consuming | Medium | Medium | Phase by phase — old modules continue to work |
| 7 | Multiple window management bugs | Medium | High | Extensive integration testing, limited to 5 floating windows |

---

# PHẦN 22: PRODUCTION CHECKLIST

```yaml
Shell readiness:
  - [ ] MainWindow renders without errors
  - [ ] Sidebar shows all authorized modules
  - [ ] Ribbon shows contextual tabs per module
  - [ ] StatusBar shows live data (business date, shift, user)
  - [ ] WorkspaceManager opens/closes documents
  - [ ] TabControl shows document tabs with dirty indicators
  - [ ] Navigation works: forward, back, deep link
  - [ ] Command Palette (Ctrl+Shift+P) finds all commands
  - [ ] Global Search (Ctrl+K) finds all entity types
  - [ ] Notification center shows real alerts
  - [ ] Session restores on application restart
  - [ ] Permission checks hide unauthorized items
  - [ ] Multiple windows can be opened/floated/docked

Performance:
  - [ ] Shell startup < 2 seconds (cold start)
  - [ ] Module open < 1 second (first time)
  - [ ] Module open < 300ms (cached)
  - [ ] Tab switch < 100ms
  - [ ] Memory < 500MB after 1 hour heavy use

Accessibility:
  - [ ] Full keyboard navigation through shell
  - [ ] All interactive elements have AutomationProperties
  - [ ] Screen reader announces workspace changes
```

---

# PHẦN 23: ARCHITECTURE DECISION RECORD

### ADR-001: Tab-based MDI (not SDI, not floating windows)
**Decision:** Use Tab-based Multiple Document Interface (MDI) for workspace management.
**Rationale:** Hotel staff typically work with multiple modules simultaneously (Room Rack + Folio + Check-in). Tab-based MDI allows quick switching without losing context. Floating windows add complexity for minimal benefit in a hotel PMS.
**Alternatives considered:** Single Document Interface (too restrictive), pure floating windows (too complex for hotel staff).

### ADR-002: IModule plugin system over hardcoded navigation
**Decision:** All modules register via IModule/IModuleRegistry.
**Rationale:** The system will grow to 50+ modules (Restaurant, Banquet, POS, Accounting, CRM, HR...). Hardcoding every module into MainWindow is not scalable. Plugin architecture allows new modules to be added without modifying shell code.
**Alternatives considered:** Hardcoded switch statement (not scalable), MEF (overly complex for our needs).

### ADR-003: Session auto-save every 30 seconds
**Decision:** Auto-capture workspace state to disk every 30 seconds.
**Rationale:** Hotel staff may be interrupted at any moment. Auto-save ensures work is not lost on crash or power failure. 30-second interval balances safety with I/O overhead.
**Alternatives considered:** Save on close only (risk of data loss), save on every action (too much I/O).

### ADR-004: Permission checks at render time, not registration time
**Decision:** Modules register themselves regardless of current user's permissions. Shell hides/disables UI elements at render time.
**Rationale:** A manager may have different permissions than a receptionist. If we filter at registration, switching users would require re-registering all modules. Render-time filtering is simpler and supports user switching.
**Alternatives considered:** Filter at registration (doesn't support user switching).

---

# PHẦN 24: CODING STANDARDS (Shell-Specific)

```yaml
Shell ViewModels:
  - All Shell VMs are singletons (one instance for app lifetime)
  - VMs implement IDisposable for cleanup
  - VMs subscribe to events in constructor, unsubscribe in Dispose

Shell Services:
  - All services registered as singletons
  - Services are thread-safe (may be called from background threads)
  - Services use async/await for all I/O operations

Module Host:
  - ModuleHost is created per module instance
  - ModuleHost has its own DI scope (scoped)
  - ModuleHost disposes scope when document closes

Events:
  - Shell uses weak event pattern for long-lived subscriptions
  - EventAggregator for cross-module communication
  - No direct ViewModel-to-ViewModel references

XAML:
  - Shell XAML uses ResourceDictionary merges
  - Shell-specific styles prefixed with "Shell."
  - Module-specific styles use module prefix ("FO.", "HK.")
```

---

*Enterprise Application Shell architecture complete. 24 sections. This is the single source of truth for the ASMILE-BO application shell, workspace management, navigation, docking, and plugin system. Every future module will run within this framework.*
