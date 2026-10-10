using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.ViewModels;

public partial class PaymentVoucherDetailModuleViewModel : ObservableObject
{
    private readonly PaymentVoucherRow _voucher;
    private readonly IWindowManagerService _windowManagerService;
    private readonly string _moduleKey;

    public PaymentVoucherDetailModuleViewModel(
        PaymentVoucherRow voucher,
        IWindowManagerService windowManagerService,
        string moduleKey)
    {
        _voucher = voucher;
        _windowManagerService = windowManagerService;
        _moduleKey = moduleKey;

        Details = new ObservableCollection<PaymentVoucherDetailRow>
        {
            new()
            {
                LineNo = 1,
                DocumentNo = voucher.DocumentNo,
                Description = string.IsNullOrWhiteSpace(voucher.Note)
                    ? $"Ghi chú: thanh toán cho phiếu nhập {voucher.DocumentNo}"
                    : voucher.Note,
                Method = voucher.Method,
                Amount = voucher.Amount
            }
        };
    }

    public event Action? RequestClose;

    public ObservableCollection<PaymentVoucherDetailRow> Details { get; }
    public string SupplierName => _voucher.SupplierName;
    public string SupplierCode => _voucher.SupplierCode;
    public string DocumentNo => _voucher.DocumentNo;
    public DateTime VoucherDate => _voucher.Date;
    public decimal TotalAmount => _voucher.Amount;
    public string Status => _voucher.Status;
    public string DebitAccountCode => string.IsNullOrWhiteSpace(_voucher.DebitAccountCode) ? "331" : _voucher.DebitAccountCode;
    public string CreditAccountCode => string.IsNullOrWhiteSpace(_voucher.CreditAccountCode) ? "111" : _voucher.CreditAccountCode;
    public string AccountPairDisplay => $"{DebitAccountCode} / {CreditAccountCode}";
    public string Note => string.IsNullOrWhiteSpace(_voucher.Note)
        ? $"Ghi chú: thanh toán cho phiếu nhập {DocumentNo}"
        : _voucher.Note;
    public string WindowTitle => $"Chi tiết {DocumentNo}";

    [RelayCommand]
    private void ShowPreview()
    {
        var previewModuleKey = $"payment-voucher-print-preview-{DocumentNo.ToLowerInvariant()}";
        var existing = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == previewModuleKey);
        if (existing is not null)
        {
            _windowManagerService.Close(existing);
        }

        var module = new AppModuleInfo(
            previewModuleKey,
            $"Xem trước in {DocumentNo}",
            "\uE8A7",
            "Xem trước in phiếu chi",
            "#3B6EF5",
            isPinned: false,
            showOnDesktop: false,
            iconPath: ApplicationAssetPaths.Icon(@"Icons\applications\payment-voucher.ico"));

        var previewVm = new StockReceiptPrintPreviewModuleViewModel(
            previewModuleKey,
            DocumentNo,
            PaymentVoucherPrintDocumentFactory.Build(this),
            _windowManagerService);

        _windowManagerService.OpenOrActivate(module, () => previewVm);
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }
}

public sealed class PaymentVoucherDetailRow
{
    public int LineNo { get; set; }
    public string DocumentNo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
