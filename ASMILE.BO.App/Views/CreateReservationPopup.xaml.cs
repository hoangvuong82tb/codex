using ASMILE.BO.App.ViewModels;
using System.Windows;
using System.Windows.Data;

namespace ASMILE.BO.App.Views;

public partial class CreateReservationPopup : Window
{
    public ReservationCreateWindow Wizard { get; }

    public CreateReservationPopup(ReservationModalWorkspaceViewModel viewModel)
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        DataContext = viewModel;
        SetBinding(TitleProperty, new Binding(nameof(ReservationModalWorkspaceViewModel.Title)));
        Wizard = new ReservationCreateWindow(viewModel);
        Host.Content = Wizard;
    }
}
