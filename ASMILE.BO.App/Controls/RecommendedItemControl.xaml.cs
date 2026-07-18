using ASMILE.BO.App.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Controls;

public partial class RecommendedItemControl : UserControl
{
    public RecommendedItemControl()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(
        nameof(Item),
        typeof(StartRecommendedItem),
        typeof(RecommendedItemControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty OpenCommandProperty = DependencyProperty.Register(
        nameof(OpenCommand),
        typeof(ICommand),
        typeof(RecommendedItemControl),
        new PropertyMetadata(null));

    public StartRecommendedItem? Item
    {
        get => (StartRecommendedItem?)GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    public ICommand? OpenCommand
    {
        get => (ICommand?)GetValue(OpenCommandProperty);
        set => SetValue(OpenCommandProperty, value);
    }
}
