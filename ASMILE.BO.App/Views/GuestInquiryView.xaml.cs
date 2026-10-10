using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class GuestInquiryView : UserControl
{
    private GuestInquiryViewModel VM => (GuestInquiryViewModel)DataContext;
    private bool _initialized;

    public GuestInquiryView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
            return;

        _initialized = true;
        if (DataContext is GuestInquiryViewModel vm)
        {
            vm.BuildPagerItems();
            await vm.InitializeAsync();
        }
    }

    private void OnDetailTabClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && int.TryParse(tag, out var tabIndex))
        {
            VM.SelectDetailTabCommand.Execute(tabIndex);
        }
    }

    private void OnGuestSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is GuestInquiryViewModel vm &&
            e.AddedItems.Count > 0 &&
            e.AddedItems[0] is GuestInquiryDto guest)
        {
            vm.SelectGuestCommand.Execute(guest);
        }
    }

    private void OnDismissInfo(object sender, RoutedEventArgs e)
    {
        VM.DismissInfoCommand.Execute(null);
    }
}
