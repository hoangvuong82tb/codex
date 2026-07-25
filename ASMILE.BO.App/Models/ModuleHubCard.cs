using System.Windows.Input;
using System.Windows.Media;

namespace ASMILE.BO.App.Models;

public sealed class ModuleHubCard
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string IconGlyph { get; init; }
    public required Brush IconColor { get; init; }
    public required ICommand Command { get; init; }
    public bool IsVisible { get; set; } = true;
}
