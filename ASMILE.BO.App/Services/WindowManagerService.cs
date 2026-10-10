using ASMILE.BO.App.Models;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.Services;

public sealed class WindowManagerService : IWindowManagerService
{
    private int _zIndexCounter = 10;
    private double _workspaceWidth = 1320;
    private double _workspaceHeight = 760;

    private const double MinVisibleHeaderWidth = 220;
    private const double MinTopMargin = 8;
    private const double MinLeftMargin = 8;
    private const double DefaultMinWindowWidth = 720;
    private const double DefaultMinWindowHeight = 460;
    private const double SupplierMinWindowWidth = 1420;
    private const double SupplierMinWindowHeight = 840;
    private const double VoucherMinWindowWidth = 1820;
    private const double VoucherMinWindowHeight = 940;
    private const double ImportPreviewMinWindowWidth = 900;
    private const double ImportPreviewMinWindowHeight = 520;
    private const double ExplorerMinWindowWidth = 1360;
    private const double ExplorerMinWindowHeight = 820;
    private const double SettingsMinWindowWidth = 1500;
    private const double SettingsMinWindowHeight = 900;
    private const double InventoryMovementMinWindowWidth = 1760;
    private const double InventoryMovementMinWindowHeight = 940;
    private const double PaymentDocumentMinWindowWidth = 1620;
    private const double PaymentDocumentMinWindowHeight = 900;
    private const double BanquetMenuSelectionMinWindowWidth = 1540;
    private const double BanquetMenuSelectionMinWindowHeight = 820;

    public ObservableCollection<AppWindowViewModel> OpenWindows { get; } = new();

    public AppWindowViewModel OpenOrActivate(AppModuleInfo module, Func<object> contentFactory)
    {
        var existing = OpenWindows.FirstOrDefault(x => x.Module.Key == module.Key);
        if (existing is not null)
        {
            Restore(existing);
            Activate(existing);
            return existing;
        }

        var offset = OpenWindows.Count * 26;
        var created = new AppWindowViewModel(module, contentFactory(), 36 + offset, 28 + offset)
        {
            State = AppWindowState.Opened
        };

        if (string.Equals(module.Key, "suppliers", StringComparison.OrdinalIgnoreCase)
            || string.Equals(module.Key, "customers", StringComparison.OrdinalIgnoreCase)
            || string.Equals(module.Key, "items", StringComparison.OrdinalIgnoreCase)
            || string.Equals(module.Key, "warehouses", StringComparison.OrdinalIgnoreCase)
            || string.Equals(module.Key, "departments", StringComparison.OrdinalIgnoreCase))
        {
            created.MinWidth = SupplierMinWindowWidth;
            created.MinHeight = SupplierMinWindowHeight;
            created.Width = Math.Max(created.Width, SupplierMinWindowWidth);
            created.Height = Math.Max(created.Height, SupplierMinWindowHeight);
            created.RestoreWidth = created.Width;
            created.RestoreHeight = created.Height;
        }
        else if (string.Equals(module.Key, "receipts", StringComparison.OrdinalIgnoreCase)
            || string.Equals(module.Key, "issues", StringComparison.OrdinalIgnoreCase))
        {
            created.MinWidth = VoucherMinWindowWidth;
            created.MinHeight = VoucherMinWindowHeight;
            created.Width = Math.Max(created.Width, VoucherMinWindowWidth);
            created.Height = Math.Max(created.Height, VoucherMinWindowHeight);
            created.RestoreWidth = created.Width;
            created.RestoreHeight = created.Height;
        }
        else if (string.Equals(module.Key, "virtual-file-center", StringComparison.OrdinalIgnoreCase))
        {
            created.MinWidth = ExplorerMinWindowWidth;
            created.MinHeight = ExplorerMinWindowHeight;
            created.Width = Math.Max(created.Width, ExplorerMinWindowWidth);
            created.Height = Math.Max(created.Height, ExplorerMinWindowHeight);
            created.RestoreWidth = created.Width;
            created.RestoreHeight = created.Height;
        }
        else if (string.Equals(module.Key, "settings", StringComparison.OrdinalIgnoreCase))
        {
            created.MinWidth = SettingsMinWindowWidth;
            created.MinHeight = SettingsMinWindowHeight;
            created.Width = Math.Max(created.Width, SettingsMinWindowWidth);
            created.Height = Math.Max(created.Height, SettingsMinWindowHeight);
            created.RestoreWidth = created.Width;
            created.RestoreHeight = created.Height;
        }
        else if (string.Equals(module.Key, "inventory-movement", StringComparison.OrdinalIgnoreCase))
        {
            created.MinWidth = InventoryMovementMinWindowWidth;
            created.MinHeight = InventoryMovementMinWindowHeight;
            created.Width = InventoryMovementMinWindowWidth;
            created.Height = InventoryMovementMinWindowHeight;
            created.RestoreWidth = created.Width;
            created.RestoreHeight = created.Height;
            CenterWindowInWorkspace(created);
        }
        else if (string.Equals(module.Key, "payment-receipt", StringComparison.OrdinalIgnoreCase)
            || string.Equals(module.Key, "payment-voucher", StringComparison.OrdinalIgnoreCase))
        {
            created.MinWidth = PaymentDocumentMinWindowWidth;
            created.MinHeight = PaymentDocumentMinWindowHeight;
            created.Width = Math.Max(created.Width, PaymentDocumentMinWindowWidth);
            created.Height = Math.Max(created.Height, PaymentDocumentMinWindowHeight);
            created.RestoreWidth = created.Width;
            created.RestoreHeight = created.Height;
            CenterWindowInWorkspace(created);
        }
        else if (string.Equals(module.Key, "supplier-import-preview", StringComparison.OrdinalIgnoreCase))
        {
            created.MinWidth = ImportPreviewMinWindowWidth;
            created.MinHeight = ImportPreviewMinWindowHeight;
            created.Width = 1160;
            created.Height = 700;
            created.RestoreWidth = created.Width;
            created.RestoreHeight = created.Height;
        }
        else if (string.Equals(module.Key, "banquet-menu-custom", StringComparison.OrdinalIgnoreCase)
            || string.Equals(module.Key, "banquet-menu-preset", StringComparison.OrdinalIgnoreCase))
        {
            created.MinWidth = BanquetMenuSelectionMinWindowWidth;
            created.MinHeight = BanquetMenuSelectionMinWindowHeight;
            created.Width = Math.Max(created.Width, BanquetMenuSelectionMinWindowWidth);
            created.Height = Math.Max(created.Height, BanquetMenuSelectionMinWindowHeight);
            created.RestoreWidth = created.Width;
            created.RestoreHeight = created.Height;
            CenterWindowInWorkspace(created);
        }
        else if (module.Key.StartsWith("receipt-detail-", StringComparison.OrdinalIgnoreCase))
        {
            created.MinWidth = 1060;
            created.MinHeight = 700;
            created.Width = 1060;
            created.Height = 700;
            created.RestoreWidth = created.Width;
            created.RestoreHeight = created.Height;
        }
        else if (module.Key.StartsWith("receipt-print-preview-", StringComparison.OrdinalIgnoreCase))
        {
            created.MinWidth = 1000;
            created.MinHeight = 680;
            created.Width = 1100;
            created.Height = 760;
            created.RestoreWidth = created.Width;
            created.RestoreHeight = created.Height;
        }
        else
        {
            created.MinWidth = DefaultMinWindowWidth;
            created.MinHeight = DefaultMinWindowHeight;
        }

        EnsureWindowWithinWorkspace(created);

        OpenWindows.Add(created);
        Activate(created);
        return created;
    }

    public void Activate(AppWindowViewModel window)
    {
        foreach (var item in OpenWindows)
        {
            item.IsActive = false;
            if (!item.IsMinimized && item.State != AppWindowState.Maximized)
            {
                item.State = AppWindowState.Inactive;
            }
        }

        window.IsMinimized = false;
        window.IsActive = true;
        window.ZIndex = ++_zIndexCounter;
        window.State = window.IsMaximized ? AppWindowState.Maximized : AppWindowState.Active;
    }

    public void ToggleFromTaskbar(AppWindowViewModel window)
    {
        if (window.IsActive && !window.IsMinimized)
        {
            Minimize(window);
            return;
        }

        if (window.IsMinimized)
        {
            Restore(window);
        }

        Activate(window);
    }

    public void Minimize(AppWindowViewModel window)
    {
        window.IsMinimized = true;
        window.IsActive = false;
        window.State = AppWindowState.Minimized;

        var fallback = OpenWindows.Where(x => !x.IsMinimized).OrderByDescending(x => x.ZIndex).FirstOrDefault();
        if (fallback is not null)
        {
            Activate(fallback);
        }
    }

    public void Restore(AppWindowViewModel window)
    {
        window.IsMinimized = false;
        if (!window.IsActive)
        {
            window.State = window.IsMaximized ? AppWindowState.Maximized : AppWindowState.Inactive;
        }
    }

    public void ToggleMaximize(AppWindowViewModel window)
    {
        if (!window.IsMaximized)
        {
            window.RestoreLeft = window.Left;
            window.RestoreTop = window.Top;
            window.RestoreWidth = window.Width;
            window.RestoreHeight = window.Height;

            window.IsMaximized = true;
            window.Left = MinLeftMargin;
            window.Top = MinTopMargin;
            var minWidth = GetEffectiveMinWidth(window);
            var minHeight = GetEffectiveMinHeight(window);
            window.Width = Math.Max(minWidth, _workspaceWidth - 2 * MinLeftMargin);
            window.Height = Math.Max(minHeight, _workspaceHeight - MinTopMargin - 6);
            window.State = AppWindowState.Maximized;
        }
        else
        {
            window.IsMaximized = false;
            window.Left = window.RestoreLeft;
            window.Top = window.RestoreTop;
            window.Width = Math.Max(GetEffectiveMinWidth(window), window.RestoreWidth);
            window.Height = Math.Max(GetEffectiveMinHeight(window), window.RestoreHeight);
            EnsureWindowWithinWorkspace(window);
            window.State = window.IsActive ? AppWindowState.Active : AppWindowState.Inactive;
        }

        Activate(window);
    }

    public void MoveWindow(AppWindowViewModel window, double left, double top)
    {
        if (window.IsMaximized)
        {
            return;
        }

        window.Left = left;
        window.Top = top;
        EnsureWindowWithinWorkspace(window);
    }

    public void ResizeWindow(AppWindowViewModel window, double width, double height)
    {
        if (window.IsMaximized)
        {
            return;
        }

        window.Width = Math.Max(GetEffectiveMinWidth(window), width);
        window.Height = Math.Max(GetEffectiveMinHeight(window), height);
        EnsureWindowWithinWorkspace(window);
    }

    public void ResizeWindowEdges(AppWindowViewModel window, double deltaLeft, double deltaTop, double deltaRight, double deltaBottom)
    {
        if (window.IsMaximized)
        {
            return;
        }

        var minWidth = GetEffectiveMinWidth(window);
        var minHeight = GetEffectiveMinHeight(window);
        var movedLeft = Math.Abs(deltaLeft) > double.Epsilon;
        var movedTop = Math.Abs(deltaTop) > double.Epsilon;
        var movedRight = Math.Abs(deltaRight) > double.Epsilon;
        var movedBottom = Math.Abs(deltaBottom) > double.Epsilon;

        var left = window.Left + deltaLeft;
        var top = window.Top + deltaTop;
        var right = window.Left + window.Width + deltaRight;
        var bottom = window.Top + window.Height + deltaBottom;

        var maxRight = Math.Max(MinLeftMargin + minWidth, _workspaceWidth - MinLeftMargin);
        var maxBottom = Math.Max(MinTopMargin + minHeight, _workspaceHeight - MinTopMargin);

        if ((right - left) < minWidth)
        {
            if (movedLeft && !movedRight)
            {
                left = right - minWidth;
            }
            else
            {
                right = left + minWidth;
            }
        }

        if ((bottom - top) < minHeight)
        {
            if (movedTop && !movedBottom)
            {
                top = bottom - minHeight;
            }
            else
            {
                bottom = top + minHeight;
            }
        }

        if (left < MinLeftMargin)
        {
            if (movedLeft && !movedRight)
            {
                left = MinLeftMargin;
            }
            else
            {
                right = Math.Max(right, MinLeftMargin + minWidth);
            }
        }

        if (top < MinTopMargin)
        {
            if (movedTop && !movedBottom)
            {
                top = MinTopMargin;
            }
            else
            {
                bottom = Math.Max(bottom, MinTopMargin + minHeight);
            }
        }

        if (right > maxRight)
        {
            if (movedRight && !movedLeft)
            {
                right = maxRight;
            }
            else
            {
                left = Math.Min(left, maxRight - minWidth);
            }
        }

        if (bottom > maxBottom)
        {
            if (movedBottom && !movedTop)
            {
                bottom = maxBottom;
            }
            else
            {
                top = Math.Min(top, maxBottom - minHeight);
            }
        }

        if ((right - left) < minWidth)
        {
            right = left + minWidth;
        }

        if ((bottom - top) < minHeight)
        {
            bottom = top + minHeight;
        }

        left = Math.Max(MinLeftMargin, Math.Min(left, maxRight - minWidth));
        top = Math.Max(MinTopMargin, Math.Min(top, maxBottom - minHeight));
        right = Math.Min(maxRight, Math.Max(right, left + minWidth));
        bottom = Math.Min(maxBottom, Math.Max(bottom, top + minHeight));

        window.Left = left;
        window.Top = top;
        window.Width = right - left;
        window.Height = bottom - top;
    }

    public void UpdateWorkspaceBounds(double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        _workspaceWidth = width;
        _workspaceHeight = height;

        foreach (var window in OpenWindows)
        {
            if (window.IsMaximized)
            {
                window.Left = MinLeftMargin;
                window.Top = MinTopMargin;
                var minWidth = GetEffectiveMinWidth(window);
                var minHeight = GetEffectiveMinHeight(window);
                window.Width = Math.Max(minWidth, _workspaceWidth - 2 * MinLeftMargin);
                window.Height = Math.Max(minHeight, _workspaceHeight - MinTopMargin - 6);
            }
            else
            {
                EnsureWindowWithinWorkspace(window);
            }
        }
    }

    public void Close(AppWindowViewModel window)
    {
        var wasActive = window.IsActive;
        window.State = AppWindowState.Closed;
        OpenWindows.Remove(window);
        // Scoped module windows are owned by their IServiceScope. Disposing the
        // content first and then the scope disposes the same ViewModel twice,
        // which can cancel an already-disposed CTS and leave async EF work
        // running against a closing scope. Content created without a scope is
        // still owned here and must be disposed explicitly.
        if (window.ServiceScope is null && window.ContentViewModel is IDisposable disposable)
        {
            disposable.Dispose();
        }
        window.ServiceScope?.Dispose();
        window.ServiceScope = null;

        if (!wasActive)
        {
            return;
        }

        var fallback = OpenWindows.Where(x => !x.IsMinimized).OrderByDescending(x => x.ZIndex).FirstOrDefault();
        if (fallback is not null)
        {
            Activate(fallback);
        }
    }

    private void EnsureWindowWithinWorkspace(AppWindowViewModel window)
    {
        var minWidth = GetEffectiveMinWidth(window);
        var minHeight = GetEffectiveMinHeight(window);
        var maxLeft = Math.Max(MinLeftMargin, _workspaceWidth - MinVisibleHeaderWidth);
        var maxTop = Math.Max(MinTopMargin, _workspaceHeight - 120);

        if (window.Left < MinLeftMargin)
        {
            window.Left = MinLeftMargin;
        }
        else if (window.Left > maxLeft)
        {
            window.Left = maxLeft;
        }

        if (window.Top < MinTopMargin)
        {
            window.Top = MinTopMargin;
        }
        else if (window.Top > maxTop)
        {
            window.Top = maxTop;
        }

        var maxWidth = Math.Max(minWidth, _workspaceWidth - window.Left - MinLeftMargin);
        var maxHeight = Math.Max(minHeight, _workspaceHeight - window.Top - MinTopMargin);
        window.Width = Math.Min(Math.Max(window.Width, minWidth), maxWidth);
        window.Height = Math.Min(Math.Max(window.Height, minHeight), maxHeight);
    }

    private double GetEffectiveMinWidth(AppWindowViewModel window)
    {
        var requested = window.MinWidth > 0 ? window.MinWidth : DefaultMinWindowWidth;
        var workspaceCap = Math.Max(520, _workspaceWidth - 2 * MinLeftMargin);
        return Math.Min(requested, workspaceCap);
    }

    private double GetEffectiveMinHeight(AppWindowViewModel window)
    {
        var requested = window.MinHeight > 0 ? window.MinHeight : DefaultMinWindowHeight;
        var workspaceCap = Math.Max(420, _workspaceHeight - MinTopMargin - 6);
        return Math.Min(requested, workspaceCap);
    }

    private void CenterWindowInWorkspace(AppWindowViewModel window)
    {
        // Keep integer coordinates to reduce ClearType blur on scaled displays.
        window.Left = Math.Round(Math.Max(MinLeftMargin, (_workspaceWidth - window.Width) / 2d));
        window.Top = Math.Round(Math.Max(MinTopMargin, (_workspaceHeight - window.Height) / 2d));
    }
}
