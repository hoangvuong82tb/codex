using System.Windows.Input;

namespace ASMILE.BO.App.Models;

/// <summary>Presentation-only card for the Front Office enterprise navigation hub.</summary>
public sealed class FrontOfficeHubCard
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string IconGlyph { get; init; }
    public required string StatusText { get; init; }
    public required ICommand Command { get; init; }
    public bool IsActive { get; init; }
    public bool IsVisible { get; set; } = true;
    public bool IsEnabled { get; init; } = true;
    public string? DisabledReason { get; init; }
    public string AutomationName => Title;
}
