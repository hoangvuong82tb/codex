using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.Views;
using ASMILE.BO.Application.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.ViewModels;

public partial class StockIssueDetailModuleViewModel : ObservableObject
{
    private readonly StockIssueDto _issue;
    private readonly IWindowManagerService _windowManagerService;

    public StockIssueDetailModuleViewModel(StockIssueDto issue, IWindowManagerService windowManagerService)
    {
        _issue = issue;
        _windowManagerService = windowManagerService;
        Details = new ObservableCollection<StockTransactionDetailDto>(_issue.Details);
        DetailsPager.ItemName = "dòng phiếu xuất";
        DetailsPager.SetSource(_issue.Details);
    }

    public event Action? RequestClose;

    public ObservableCollection<StockTransactionDetailDto> Details { get; }
    public TablePagerViewModel<StockTransactionDetailDto> DetailsPager { get; } = new();
    public string IssueNo => _issue.IssueNo;
    public DateTime IssueDate => _issue.IssueDate;
    public string Description => _issue.Description;
    public string WarehouseCode => _issue.WarehouseCode;
    public string ToWarehouseCode => _issue.ToWarehouseCode;
    public string DepartmentCode => _issue.DepartmentCode;
    public decimal TotalAmount => _issue.TotalAmount;
    public string WindowTitle => $"Chi tiết phiếu xuất - {_issue.IssueNo}";

    [RelayCommand]
    private void ShowPreview()
    {
        var previewModuleKey = $"issue-print-preview-{_issue.Id}";
        var existing = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == previewModuleKey);
        if (existing is not null)
        {
            _windowManagerService.Close(existing);
        }

        var module = new AppModuleInfo(
            previewModuleKey,
            $"Xem trước in {_issue.IssueNo}",
            "\uE8A7",
            "Xem trước in phiếu xuất",
            "#3B6EF5",
            isPinned: false,
            showOnDesktop: false,
            iconPath: ApplicationAssetPaths.Icon(@"Icons\applications\export.ico"));

        var previewVm = new StockReceiptPrintPreviewModuleViewModel(
            previewModuleKey,
            _issue.IssueNo,
            StockIssuePrintDocumentFactory.Build(_issue),
            _windowManagerService);

        _windowManagerService.OpenOrActivate(module, () => previewVm);
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();

        var previewModuleKey = $"issue-print-preview-{_issue.Id}";
        var previewWindow = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == previewModuleKey);
        if (previewWindow is not null)
        {
            _windowManagerService.Close(previewWindow);
        }
    }
}
