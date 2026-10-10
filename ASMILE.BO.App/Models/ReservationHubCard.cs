using System.Windows.Input;

namespace ASMILE.BO.App.Models;

/// <summary>
/// UI-only description of one Front Office command. It never represents a reservation domain record.
/// </summary>
public sealed class ReservationHubCard
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string IconGlyph { get; init; }
    public required string Shortcut { get; init; }
    public required int DisplayOrder { get; init; }
    public required ICommand Command { get; set; }
    public bool IsVisible { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsComingSoon { get; init; }
    public string? BadgeText { get; init; }
    public string? DisabledReason { get; init; }
    public string AutomationName => Title;
}
