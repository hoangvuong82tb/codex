using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Core.FrontOffice.Reservation;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class ReservationStatusWindow : Window
{
    private readonly ReservationWorkspaceViewModel _vm;

    public ReservationStatusWindow(ReservationWorkspaceViewModel viewModel)
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        _vm = viewModel;
        DataContext = viewModel;
        _vm.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(_vm.TotalCount)) UpdateKpiCounts(); };
    }

    private void OnKpiClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is string tag)
        {
            _vm.SelectedStatus = string.IsNullOrEmpty(tag) ? null
                : (ReservationStatus?)System.Enum.Parse(typeof(ReservationStatus), tag);
        }
    }

    private void UpdateKpiCounts()
    {
        var counts = _vm.Reservations.GroupBy(r => r.Status).ToDictionary(g => g.Key, g => g.Count());
        KpiAll.Text = _vm.TotalCount.ToString();
        KpiDraft.Text = counts.GetValueOrDefault(ReservationStatus.Draft, 0).ToString();
        KpiTentative.Text = counts.GetValueOrDefault(ReservationStatus.Tentative, 0).ToString();
        KpiConfirmed.Text = counts.GetValueOrDefault(ReservationStatus.Confirmed, 0).ToString();
        KpiCheckedIn.Text = counts.GetValueOrDefault(ReservationStatus.CheckedIn, 0).ToString();
        KpiNoShow.Text = counts.GetValueOrDefault(ReservationStatus.NoShow, 0).ToString();
        KpiCancelled.Text = counts.GetValueOrDefault(ReservationStatus.Cancelled, 0).ToString();
    }
}
