using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.Services;

namespace ASMILE.BO.App.ViewModels;

public class SupplierPageViewModel : SuppliersViewModel
{
    public SupplierPageViewModel(
        ISupplierAppService service,
        INotificationService notificationService,
        IDialogService dialogService,
        ISupplierImportExportService supplierImportExportService,
        ISupplierImportValidator supplierImportValidator,
        IWindowManagerService windowManagerService)
        : base(service, notificationService, dialogService, supplierImportExportService, supplierImportValidator, windowManagerService)
    {
    }
}
