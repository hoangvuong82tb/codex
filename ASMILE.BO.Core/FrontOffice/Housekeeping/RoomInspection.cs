namespace ASMILE.BO.Core.FrontOffice.Housekeeping;

/// <summary>Quality inspection performed against a housekeeping task.</summary>
public sealed class RoomInspection
{
    private RoomInspection() { }

    public Guid InspectionId { get; private set; }
    public Guid HousekeepingTaskId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid? InspectorId { get; private set; }
    public string Status { get; private set; } = "Pending";
    public string? Result { get; private set; }
    public string? ChecklistData { get; private set; }
    public DateTime? InspectedAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static RoomInspection Create(Guid propertyId, Guid housekeepingTaskId) => new()
    {
        InspectionId = Guid.NewGuid(),
        PropertyId = propertyId,
        HousekeepingTaskId = housekeepingTaskId,
        Status = "Pending"
    };

    public void Complete(Guid? inspectorId, string result, string? checklistData, string? notes, DateTime inspectedAtUtc)
    {
        InspectorId = inspectorId;
        Result = result;
        ChecklistData = checklistData;
        Notes = notes;
        InspectedAtUtc = inspectedAtUtc;
        Status = "Completed";
    }

    public void SaveEvidence(Guid? inspectorId, string? checklistData, string? notes)
    {
        InspectorId = inspectorId;
        ChecklistData = checklistData;
        Notes = notes;
    }
}
