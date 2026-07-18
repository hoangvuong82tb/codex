using System.Windows;
using System.Windows.Controls;

namespace ASMILE.BO.App.Controls;

public partial class EnterpriseStatePresenter : UserControl
{
    public static readonly DependencyProperty StateContentProperty =
        DependencyProperty.Register(nameof(StateContent), typeof(object), typeof(EnterpriseStatePresenter), new PropertyMetadata(null));

    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(EnterpriseStatePresenter), new PropertyMetadata(false));

    public static readonly DependencyProperty IsEmptyProperty =
        DependencyProperty.Register(nameof(IsEmpty), typeof(bool), typeof(EnterpriseStatePresenter), new PropertyMetadata(false));

    public static readonly DependencyProperty HasErrorProperty =
        DependencyProperty.Register(nameof(HasError), typeof(bool), typeof(EnterpriseStatePresenter), new PropertyMetadata(false));

    public static readonly DependencyProperty LoadingTextProperty =
        DependencyProperty.Register(nameof(LoadingText), typeof(string), typeof(EnterpriseStatePresenter), new PropertyMetadata("\u0110ang t\u1EA3i d\u1EEF li\u1EC7u..."));

    public static readonly DependencyProperty EmptyTextProperty =
        DependencyProperty.Register(nameof(EmptyText), typeof(string), typeof(EnterpriseStatePresenter), new PropertyMetadata("Kh\u00F4ng c\u00F3 d\u1EEF li\u1EC7u."));

    public static readonly DependencyProperty ErrorTextProperty =
        DependencyProperty.Register(nameof(ErrorText), typeof(string), typeof(EnterpriseStatePresenter), new PropertyMetadata(string.Empty));

    public EnterpriseStatePresenter()
    {
        InitializeComponent();
    }

    public object? StateContent
    {
        get => GetValue(StateContentProperty);
        set => SetValue(StateContentProperty, value);
    }

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public bool IsEmpty
    {
        get => (bool)GetValue(IsEmptyProperty);
        set => SetValue(IsEmptyProperty, value);
    }

    public bool HasError
    {
        get => (bool)GetValue(HasErrorProperty);
        set => SetValue(HasErrorProperty, value);
    }

    public string LoadingText
    {
        get => (string)GetValue(LoadingTextProperty);
        set => SetValue(LoadingTextProperty, value);
    }

    public string EmptyText
    {
        get => (string)GetValue(EmptyTextProperty);
        set => SetValue(EmptyTextProperty, value);
    }

    public string ErrorText
    {
        get => (string)GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }
}
