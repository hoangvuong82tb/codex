using ASMILE.BO.App.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class PaymentVouchersView : UserControl
{
    public PaymentVouchersView()
    {
        InitializeComponent();
    }

    private void DebtGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not PaymentVouchersViewModel vm || vm.SelectedDebtSummary is null)
        {
            return;
        }

        if (vm.OpenDebtDetailCommand.CanExecute(vm.SelectedDebtSummary))
        {
            vm.OpenDebtDetailCommand.Execute(vm.SelectedDebtSummary);
        }
    }

    private void VoucherGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not PaymentVouchersViewModel vm || vm.SelectedVoucher is null)
        {
            return;
        }

        if (vm.OpenVoucherDetailCommand.CanExecute(vm.SelectedVoucher))
        {
            vm.OpenVoucherDetailCommand.Execute(vm.SelectedVoucher);
        }
    }
}
