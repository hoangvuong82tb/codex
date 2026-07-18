using System.Windows;
using System.Windows.Controls;

namespace ASMILE.BO.App.Controls;

public partial class EnterpriseFilterPanel : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(EnterpriseFilterPanel), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PanelContentProperty =
        DependencyProperty.Register(nameof(PanelContent), typeof(object), typeof(EnterpriseFilterPanel), new PropertyMetadata(null));

    public static readonly DependencyProperty PanelPaddingProperty =
        DependencyProperty.Register(nameof(PanelPadding), typeof(Thickness), typeof(EnterpriseFilterPanel), new PropertyMetadata(new Thickness(16)));

    public EnterpriseFilterPanel()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object? PanelContent
    {
        get => GetValue(PanelContentProperty);
        set => SetValue(PanelContentProperty, value);
    }

    public Thickness PanelPadding
    {
        get => (Thickness)GetValue(PanelPaddingProperty);
        set => SetValue(PanelPaddingProperty, value);
    }
}
