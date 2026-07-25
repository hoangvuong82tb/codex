using System.Windows.Input;

namespace ASMILE.BO.App.Models;

/// <summary>Presentation-only description of a Reception command; it never represents a hotel operation.</summary>
public sealed class ReceptionHubCard
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string IconGlyph { get; init; }
    public required string BadgeText { get; init; }
    public required ICommand Command { get; init; }
    public bool IsReadOnly { get; init; }
    public bool IsComingSoon { get; init; }
    public bool IsEnabled { get; init; } = true;
    public string? DisabledReason { get; init; }
    public string AutomationName => Title;
}

/// <summary>Read-only row projected from existing Front Office queries for the Reception workspace.</summary>
public sealed record ReceptionReadOnlyRow(string PrimaryText, string SecondaryText, string StatusText);
