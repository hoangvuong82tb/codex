using System.Collections;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ASMILE.BO.App.Controls;

public partial class WarehouseDetailCard : UserControl
{
    private readonly Dictionary<TextBox, ComboBox> _editorOwners = new();

    public WarehouseDetailCard()
    {
        InitializeComponent();
    }

    private void SuggestionCombo_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox)
        {
            return;
        }

        AttachEditor(comboBox);
        ApplyFilter(comboBox);
        comboBox.IsDropDownOpen = true;
    }

    private void SuggestionCombo_OnKeyUp(object sender, KeyEventArgs e)
    {
        if (sender is not ComboBox comboBox)
        {
            return;
        }

        ApplyFilter(comboBox);
        comboBox.IsDropDownOpen = true;
    }

    private void SuggestionCombo_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox)
        {
            return;
        }

        ClearFilter(comboBox);
        comboBox.IsDropDownOpen = false;
    }

    private void AttachEditor(ComboBox comboBox)
    {
        var editableTextBox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
        if (editableTextBox is null || _editorOwners.ContainsKey(editableTextBox))
        {
            return;
        }

        _editorOwners.Add(editableTextBox, comboBox);
        editableTextBox.TextChanged += SuggestionEditor_OnTextChanged;
    }

    private void SuggestionEditor_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox editor || !_editorOwners.TryGetValue(editor, out var comboBox))
        {
            return;
        }

        ApplyFilter(comboBox);
        comboBox.IsDropDownOpen = true;
    }

    private static void ApplyFilter(ComboBox comboBox)
    {
        if (comboBox.ItemsSource is not IEnumerable source)
        {
            return;
        }

        var view = CollectionViewSource.GetDefaultView(source);
        if (view is null)
        {
            return;
        }

        var keyword = (comboBox.Text ?? string.Empty).Trim();
        view.Filter = item =>
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return true;
            }

            var display = GetDisplayText(item);
            return display.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        };
        view.Refresh();
    }

    private static void ClearFilter(ComboBox comboBox)
    {
        if (comboBox.ItemsSource is not IEnumerable source)
        {
            return;
        }

        var view = CollectionViewSource.GetDefaultView(source);
        if (view is null)
        {
            return;
        }

        view.Filter = null;
        view.Refresh();
    }

    private static string GetDisplayText(object? item)
    {
        if (item is null)
        {
            return string.Empty;
        }

        var type = item.GetType();
        return ReadStringProperty(type, item, "Display")
               ?? ReadStringProperty(type, item, "Name")
               ?? item.ToString()
               ?? string.Empty;
    }

    private static string? ReadStringProperty(Type type, object target, string name)
    {
        var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        if (property is null || property.PropertyType != typeof(string))
        {
            return null;
        }

        return property.GetValue(target) as string;
    }
}
