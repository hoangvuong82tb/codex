using ASMILE.BO.App.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ASMILE.BO.App.Views.MenuRecipe.Screens;

public partial class MenuCategoriesScreen : UserControl
{
    private MenuRecipeViewModel? _viewModel;

    public MenuCategoriesScreen()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = e.NewValue as MenuRecipeViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MenuRecipeViewModel.EditingCategory) || _viewModel?.EditingCategory is null)
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            CategoryListBox.UpdateLayout();
            CategoryListBox.ScrollIntoView(_viewModel.EditingCategory);
        }, DispatcherPriority.Background);
    }
}
