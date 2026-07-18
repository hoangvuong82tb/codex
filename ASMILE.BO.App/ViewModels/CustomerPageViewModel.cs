using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.Services;

namespace ASMILE.BO.App.ViewModels;

public class CustomerPageViewModel : CustomersViewModel
{
    public CustomerPageViewModel(
        ICustomerAppService service,
        INotificationService notificationService,
        IDialogService dialogService,
        ICustomerImportExportService importExportService,
        ICustomerImportValidator customerImportValidator,
        IWindowManagerService windowManagerService)
        : base(service, notificationService, dialogService, importExportService, customerImportValidator, windowManagerService)
    {
    }
}
