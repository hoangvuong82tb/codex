using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class NotificationTaskCenterView : UserControl
{
    public NotificationTaskCenterView()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not NotificationTaskCenterViewModel vm) return;

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.N: vm.ToggleCreateTaskCommand.Execute(null); e.Handled = true; break;
                case Key.R: vm.RefreshCommand.Execute(null); e.Handled = true; break;
                case Key.D: vm.MarkAllReadCommand.Execute(null); e.Handled = true; break;
            }
        }
    }

    private void OnInboxItemClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is NotificationInboxItem item)
        {
            if (DataContext is NotificationTaskCenterViewModel vm)
            {
                foreach (var i in vm.InboxItems) i.IsSelected = false;
                item.IsSelected = true;
                vm.SelectedInboxItem = item;
            }
        }
    }
}
