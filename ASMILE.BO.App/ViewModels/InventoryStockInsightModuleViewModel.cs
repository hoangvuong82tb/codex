using ASMILE.BO.App.Services;
using ASMILE.BO.Application.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class InventoryStockInsightModuleViewModel : ObservableObject
{
    private readonly IWindowManagerService _windowManagerService;
    private readonly string _moduleKey;

    public InventoryStockInsightModuleViewModel(
        string title,
        string subtitle,
        string moduleKey,
        IReadOnlyList<InventoryStockInsightDto> rows,
        bool hideIssueColumns,
        IWindowManagerService windowManagerService)
    {
        Title = title;
        Subtitle = subtitle;
        _moduleKey = moduleKey;
        _windowManagerService = windowManagerService;
        HideIssueColumns = hideIssueColumns;
        Rows = new ObservableCollection<InventoryStockInsightDto>(rows);
        RowsPager.ItemName = "dòng tồn kho";
        RowsPager.SetSource(rows);
    }

    public string Title { get; }
    public string Subtitle { get; }
    public bool HideIssueColumns { get; }
    public ObservableCollection<InventoryStockInsightDto> Rows { get; }
    public TablePagerViewModel<InventoryStockInsightDto> RowsPager { get; } = new();

    [RelayCommand]
    private void Close()
    {
        var window = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == _moduleKey);
        if (window is not null)
        {
            _windowManagerService.Close(window);
        }
    }
}
