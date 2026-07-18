using ASMILE.BO.Application.DTOs;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.ViewModels;

public partial class StockReceiptDetailModuleViewModel : ObservableObject
{
    private readonly StockReceiptDto _receipt;
    private readonly IWindowManagerService _windowManagerService;
    private readonly string _moduleKey;

    public StockReceiptDetailModuleViewModel(StockReceiptDto receipt, IWindowManagerService windowManagerService, string moduleKey)
    {
        _receipt = receipt;
        _windowManagerService = windowManagerService;
        _moduleKey = moduleKey;
        Details = new ObservableCollection<StockTransactionDetailDto>(_receipt.Details);
        DetailsPager.ItemName = "dòng phiếu nhập";
        DetailsPager.SetSource(_receipt.Details);
    }

    public event Action? RequestClose;

    public ObservableCollection<StockTransactionDetailDto> Details { get; }
    public TablePagerViewModel<StockTransactionDetailDto> DetailsPager { get; } = new();
    public string SupplierName => _receipt.SupplierName;
    public string ReceiptNo => _receipt.ReceiptNo;
    public DateTime ReceiptDate => _receipt.ReceiptDate;
    public string ReceiverName => _receipt.ReceiverName;
    public decimal TotalAmount => _receipt.TotalAmount;
    public string WindowTitle => $"Chi tiết phiếu nhập - {_receipt.ReceiptNo}";

    [RelayCommand]
    private void ShowPreview()
    {
        var previewModuleKey = $"receipt-print-preview-{_receipt.Id}";
        var existing = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == previewModuleKey);
        if (existing is not null)
        {
            _windowManagerService.Close(existing);
        }

        var module = new AppModuleInfo(
            previewModuleKey,
            $"Xem trước in {_receipt.ReceiptNo}",
            "\uE8A7",
            "Xem trước in phiếu nhập",
            "#3B6EF5",
            isPinned: false,
            showOnDesktop: false,
            iconPath: @"D:\ASMILE-BO\Windows_Icon\Icons\applications\import.ico");

        var previewVm = new StockReceiptPrintPreviewModuleViewModel(
            previewModuleKey,
            _receipt.ReceiptNo,
            StockReceiptPrintDocumentFactory.Build(_receipt),
            _windowManagerService);

        _windowManagerService.OpenOrActivate(module, () => previewVm);
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();

        var previewModuleKey = $"receipt-print-preview-{_receipt.Id}";
        var previewWindow = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == previewModuleKey);
        if (previewWindow is not null)
        {
            _windowManagerService.Close(previewWindow);
        }
    }
}
