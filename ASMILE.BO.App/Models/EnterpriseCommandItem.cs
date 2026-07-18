using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ASMILE.BO.App.Models;

public sealed class EnterpriseCommandItem
{
    public string Text { get; set; } = string.Empty;

    public string? IconGlyph { get; set; }

    public ICommand? Command { get; set; }

    public object? CommandParameter { get; set; }

    public bool IsPrimary { get; set; }

    public ObservableCollection<EnterpriseCommandItem> Children { get; } = [];
}
