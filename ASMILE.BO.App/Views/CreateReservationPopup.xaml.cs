using ASMILE.BO.App.ViewModels;
using System.Windows;

namespace ASMILE.BO.App.Views;

public partial class CreateReservationPopup : Window
{
    public ReservationCreateWindow Wizard { get; }

    public CreateReservationPopup(ReservationModalWorkspaceViewModel viewModel)
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        Wizard = new ReservationCreateWindow(viewModel);
        Host.Content = Wizard;
    }
}
