using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Documents;

namespace ASMILE.BO.App.ViewModels;

public partial class StockReceiptPrintPreviewModuleViewModel : ObservableObject
{
    private readonly IWindowManagerService _windowManagerService;
    private readonly string _moduleKey;

    public StockReceiptPrintPreviewModuleViewModel(
        string moduleKey,
        string receiptNo,
        FixedDocument document,
        IWindowManagerService windowManagerService)
    {
        _moduleKey = moduleKey;
        _windowManagerService = windowManagerService;
        ReceiptNo = receiptNo;
        PreviewDocument = document;
    }

    public string ReceiptNo { get; }
    public string WindowTitle => $"Xem trước in - {ReceiptNo}";
    public FixedDocument PreviewDocument { get; }

    [RelayCommand]
    private void Close()
    {
        var opened = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == _moduleKey);
        if (opened is not null)
        {
            _windowManagerService.Close(opened);
        }
    }
}

