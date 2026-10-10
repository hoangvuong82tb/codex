using ASMILE.BO.Core.FrontOffice.Housekeeping;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class RoomInspectionTests
{
    [Fact]
    public void New_inspection_is_pending_and_keeps_task_scope()
    {
        var propertyId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var inspection = RoomInspection.Create(propertyId, taskId);

        inspection.PropertyId.Should().Be(propertyId);
        inspection.HousekeepingTaskId.Should().Be(taskId);
        inspection.Status.Should().Be("Pending");
        inspection.Result.Should().BeNull();
    }

    [Fact]
    public void Completing_inspection_persists_result_notes_and_timestamp()
    {
        var inspectedAt = DateTime.UtcNow;
        var inspectorId = Guid.NewGuid();
        var inspection = RoomInspection.Create(Guid.NewGuid(), Guid.NewGuid());

        inspection.Complete(inspectorId, "Rejected", "{\"score\":52}", "WC chưa sạch", inspectedAt);

        inspection.Status.Should().Be("Completed");
        inspection.Result.Should().Be("Rejected");
        inspection.InspectorId.Should().Be(inspectorId);
        inspection.ChecklistData.Should().Be("{\"score\":52}");
        inspection.Notes.Should().Be("WC chưa sạch");
        inspection.InspectedAtUtc.Should().Be(inspectedAt);
    }

    [Fact]
    public void Saving_evidence_keeps_pending_status_and_does_not_set_completion_time()
    {
        var inspectorId = Guid.NewGuid();
        var inspection = RoomInspection.Create(Guid.NewGuid(), Guid.NewGuid());

        inspection.SaveEvidence(inspectorId, "{\"photos\":[{\"url\":\"photo.jpg\"}]}", "Ảnh kiểm tra");

        inspection.Status.Should().Be("Pending");
        inspection.Result.Should().BeNull();
        inspection.InspectorId.Should().Be(inspectorId);
        inspection.ChecklistData.Should().Contain("photo.jpg");
        inspection.Notes.Should().Be("Ảnh kiểm tra");
        inspection.InspectedAtUtc.Should().BeNull();
    }
}
