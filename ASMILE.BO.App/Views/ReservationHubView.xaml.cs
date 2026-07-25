using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class ReservationHubView : UserControl
{
    private ReservationHubViewModel? viewModel;

    public ReservationHubView() => InitializeComponent();

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ReservationHubViewModel hub || ReferenceEquals(viewModel, hub)) return;
        DetachViewModel();
        viewModel = hub;
        viewModel.ModalService.PropertyChanged += OnModalServicePropertyChanged;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => DetachViewModel();

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || viewModel?.ModalService.IsOpen != true) return;
        viewModel.ModalService.Close();
        e.Handled = true;
    }

    private void OnModalServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IReservationWorkspaceModalService.FocusRestoreCardId)
            || string.IsNullOrWhiteSpace(viewModel?.ModalService.FocusRestoreCardId)) return;

        var cardId = viewModel.ModalService.FocusRestoreCardId;
        Dispatcher.BeginInvoke(() =>
        {
            var button = FindCardButton(this, cardId!);
            button?.Focus();
        });
    }

    private static Button? FindCardButton(DependencyObject root, string cardId)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is Button { Tag: string tag } button && string.Equals(tag, cardId, StringComparison.Ordinal)) return button;
            var found = FindCardButton(child, cardId);
            if (found is not null) return found;
        }

        return null;
    }

    private void DetachViewModel()
    {
        if (viewModel is null) return;
        viewModel.ModalService.PropertyChanged -= OnModalServicePropertyChanged;
        viewModel = null;
    }
}
