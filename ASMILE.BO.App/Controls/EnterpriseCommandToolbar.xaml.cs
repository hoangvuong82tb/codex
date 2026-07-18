using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Controls;

public partial class EnterpriseCommandToolbar : UserControl
{
    public static readonly DependencyProperty SearchTextProperty =
        DependencyProperty.Register(
            nameof(SearchText),
            typeof(string),
            typeof(EnterpriseCommandToolbar),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty SearchPlaceholderProperty =
        DependencyProperty.Register(nameof(SearchPlaceholder), typeof(string), typeof(EnterpriseCommandToolbar), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SearchMinWidthProperty =
        DependencyProperty.Register(nameof(SearchMinWidth), typeof(double), typeof(EnterpriseCommandToolbar), new PropertyMetadata(280d));

    public static readonly DependencyProperty SearchHeightProperty =
        DependencyProperty.Register(nameof(SearchHeight), typeof(double), typeof(EnterpriseCommandToolbar), new PropertyMetadata(46d));

    public static readonly DependencyProperty ShowSearchProperty =
        DependencyProperty.Register(nameof(ShowSearch), typeof(bool), typeof(EnterpriseCommandToolbar), new PropertyMetadata(true));

    public static readonly DependencyProperty FilterContentProperty =
        DependencyProperty.Register(nameof(FilterContent), typeof(object), typeof(EnterpriseCommandToolbar), new PropertyMetadata(null));

    public static readonly DependencyProperty ActionsContentProperty =
        DependencyProperty.Register(nameof(ActionsContent), typeof(object), typeof(EnterpriseCommandToolbar), new PropertyMetadata(null));

    public static readonly DependencyProperty RefreshCommandProperty =
        DependencyProperty.Register(nameof(RefreshCommand), typeof(ICommand), typeof(EnterpriseCommandToolbar), new PropertyMetadata(null));

    public static readonly DependencyProperty RefreshTextProperty =
        DependencyProperty.Register(nameof(RefreshText), typeof(string), typeof(EnterpriseCommandToolbar), new PropertyMetadata("L\u00E0m m\u1EDBi"));

    public static readonly DependencyProperty RefreshIconGlyphProperty =
        DependencyProperty.Register(nameof(RefreshIconGlyph), typeof(string), typeof(EnterpriseCommandToolbar), new PropertyMetadata("\uE72C"));

    public static readonly DependencyProperty ShowRefreshProperty =
        DependencyProperty.Register(nameof(ShowRefresh), typeof(bool), typeof(EnterpriseCommandToolbar), new PropertyMetadata(true));

    public static readonly DependencyProperty ShowExportImportProperty =
        DependencyProperty.Register(nameof(ShowExportImport), typeof(bool), typeof(EnterpriseCommandToolbar), new PropertyMetadata(true));

    public static readonly DependencyProperty ExportImportTextProperty =
        DependencyProperty.Register(nameof(ExportImportText), typeof(string), typeof(EnterpriseCommandToolbar), new PropertyMetadata("Xu\u1EA5t - Nh\u1EADp"));

    public static readonly DependencyProperty ExportImportIconGlyphProperty =
        DependencyProperty.Register(nameof(ExportImportIconGlyph), typeof(string), typeof(EnterpriseCommandToolbar), new PropertyMetadata("\uE118"));

    public static readonly DependencyProperty ExportTemplateCommandProperty =
        DependencyProperty.Register(nameof(ExportTemplateCommand), typeof(ICommand), typeof(EnterpriseCommandToolbar), new PropertyMetadata(null));

    public static readonly DependencyProperty ExportTemplateTextProperty =
        DependencyProperty.Register(nameof(ExportTemplateText), typeof(string), typeof(EnterpriseCommandToolbar), new PropertyMetadata("Xu\u1EA5t d\u1EEF li\u1EC7u m\u1EABu"));

    public static readonly DependencyProperty ExportTemplateIconGlyphProperty =
        DependencyProperty.Register(nameof(ExportTemplateIconGlyph), typeof(string), typeof(EnterpriseCommandToolbar), new PropertyMetadata("\uE78C"));

    public static readonly DependencyProperty AlternateExportTemplateCommandProperty =
        DependencyProperty.Register(nameof(AlternateExportTemplateCommand), typeof(ICommand), typeof(EnterpriseCommandToolbar), new PropertyMetadata(null));

    public static readonly DependencyProperty AlternateExportTemplateTextProperty =
        DependencyProperty.Register(nameof(AlternateExportTemplateText), typeof(string), typeof(EnterpriseCommandToolbar), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty AlternateExportTemplateIconGlyphProperty =
        DependencyProperty.Register(nameof(AlternateExportTemplateIconGlyph), typeof(string), typeof(EnterpriseCommandToolbar), new PropertyMetadata("\uE78C"));

    public static readonly DependencyProperty ExportCurrentViewCommandProperty =
        DependencyProperty.Register(nameof(ExportCurrentViewCommand), typeof(ICommand), typeof(EnterpriseCommandToolbar), new PropertyMetadata(null));

    public static readonly DependencyProperty ExportCurrentViewTextProperty =
        DependencyProperty.Register(nameof(ExportCurrentViewText), typeof(string), typeof(EnterpriseCommandToolbar), new PropertyMetadata("Xu\u1EA5t b\u1EA3ng d\u1EEF li\u1EC7u hi\u1EC7n t\u1EA1i"));

    public static readonly DependencyProperty ExportCurrentViewIconGlyphProperty =
        DependencyProperty.Register(nameof(ExportCurrentViewIconGlyph), typeof(string), typeof(EnterpriseCommandToolbar), new PropertyMetadata("\uE7C3"));

    public static readonly DependencyProperty ImportDataCommandProperty =
        DependencyProperty.Register(nameof(ImportDataCommand), typeof(ICommand), typeof(EnterpriseCommandToolbar), new PropertyMetadata(null));

    public static readonly DependencyProperty ImportDataTextProperty =
        DependencyProperty.Register(nameof(ImportDataText), typeof(string), typeof(EnterpriseCommandToolbar), new PropertyMetadata("Nh\u1EADp d\u1EEF li\u1EC7u"));

    public static readonly DependencyProperty ImportDataIconGlyphProperty =
        DependencyProperty.Register(nameof(ImportDataIconGlyph), typeof(string), typeof(EnterpriseCommandToolbar), new PropertyMetadata("\uE8E5"));

    public EnterpriseCommandToolbar()
    {
        InitializeComponent();
    }

    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public string SearchPlaceholder
    {
        get => (string)GetValue(SearchPlaceholderProperty);
        set => SetValue(SearchPlaceholderProperty, value);
    }

    public double SearchMinWidth
    {
        get => (double)GetValue(SearchMinWidthProperty);
        set => SetValue(SearchMinWidthProperty, value);
    }

    public double SearchHeight
    {
        get => (double)GetValue(SearchHeightProperty);
        set => SetValue(SearchHeightProperty, value);
    }

    public bool ShowSearch
    {
        get => (bool)GetValue(ShowSearchProperty);
        set => SetValue(ShowSearchProperty, value);
    }

    public object? FilterContent
    {
        get => GetValue(FilterContentProperty);
        set => SetValue(FilterContentProperty, value);
    }

    public object? ActionsContent
    {
        get => GetValue(ActionsContentProperty);
        set => SetValue(ActionsContentProperty, value);
    }

    public ICommand? RefreshCommand
    {
        get => (ICommand?)GetValue(RefreshCommandProperty);
        set => SetValue(RefreshCommandProperty, value);
    }

    public string RefreshText
    {
        get => (string)GetValue(RefreshTextProperty);
        set => SetValue(RefreshTextProperty, value);
    }

    public string RefreshIconGlyph
    {
        get => (string)GetValue(RefreshIconGlyphProperty);
        set => SetValue(RefreshIconGlyphProperty, value);
    }

    public bool ShowRefresh
    {
        get => (bool)GetValue(ShowRefreshProperty);
        set => SetValue(ShowRefreshProperty, value);
    }

    public bool ShowExportImport
    {
        get => (bool)GetValue(ShowExportImportProperty);
        set => SetValue(ShowExportImportProperty, value);
    }

    public string ExportImportText
    {
        get => (string)GetValue(ExportImportTextProperty);
        set => SetValue(ExportImportTextProperty, value);
    }

    public string ExportImportIconGlyph
    {
        get => (string)GetValue(ExportImportIconGlyphProperty);
        set => SetValue(ExportImportIconGlyphProperty, value);
    }

    public ICommand? ExportTemplateCommand
    {
        get => (ICommand?)GetValue(ExportTemplateCommandProperty);
        set => SetValue(ExportTemplateCommandProperty, value);
    }

    public string ExportTemplateText
    {
        get => (string)GetValue(ExportTemplateTextProperty);
        set => SetValue(ExportTemplateTextProperty, value);
    }

    public string ExportTemplateIconGlyph
    {
        get => (string)GetValue(ExportTemplateIconGlyphProperty);
        set => SetValue(ExportTemplateIconGlyphProperty, value);
    }

    public ICommand? AlternateExportTemplateCommand
    {
        get => (ICommand?)GetValue(AlternateExportTemplateCommandProperty);
        set => SetValue(AlternateExportTemplateCommandProperty, value);
    }

    public string AlternateExportTemplateText
    {
        get => (string)GetValue(AlternateExportTemplateTextProperty);
        set => SetValue(AlternateExportTemplateTextProperty, value);
    }

    public string AlternateExportTemplateIconGlyph
    {
        get => (string)GetValue(AlternateExportTemplateIconGlyphProperty);
        set => SetValue(AlternateExportTemplateIconGlyphProperty, value);
    }

    public ICommand? ExportCurrentViewCommand
    {
        get => (ICommand?)GetValue(ExportCurrentViewCommandProperty);
        set => SetValue(ExportCurrentViewCommandProperty, value);
    }

    public string ExportCurrentViewText
    {
        get => (string)GetValue(ExportCurrentViewTextProperty);
        set => SetValue(ExportCurrentViewTextProperty, value);
    }

    public string ExportCurrentViewIconGlyph
    {
        get => (string)GetValue(ExportCurrentViewIconGlyphProperty);
        set => SetValue(ExportCurrentViewIconGlyphProperty, value);
    }

    public ICommand? ImportDataCommand
    {
        get => (ICommand?)GetValue(ImportDataCommandProperty);
        set => SetValue(ImportDataCommandProperty, value);
    }

    public string ImportDataText
    {
        get => (string)GetValue(ImportDataTextProperty);
        set => SetValue(ImportDataTextProperty, value);
    }

    public string ImportDataIconGlyph
    {
        get => (string)GetValue(ImportDataIconGlyphProperty);
        set => SetValue(ImportDataIconGlyphProperty, value);
    }

    private void ExportImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.ContextMenu is null)
        {
            return;
        }

        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.IsOpen = true;
        e.Handled = true;
    }
}
