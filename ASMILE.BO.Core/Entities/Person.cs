using System.ComponentModel.DataAnnotations;

namespace ASMILE.BO.Core.Entities;

public sealed class Person
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string PersonCode { get; set; } = string.Empty;

    [MaxLength(255)]
    public string FullName { get; set; } = string.Empty;

    public int DepartmentId { get; set; }

    [MaxLength(120)]
    public string Position { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Note { get; set; } = string.Empty;

    // Workforce metadata is kept on the canonical people record so the
    // directory and shift/area forms round-trip through the real database.
    [MaxLength(30)]
    public string WorkforceShift { get; set; } = "Chưa phân ca";

    [MaxLength(30)]
    public string WorkforceBuilding { get; set; } = string.Empty;

    [MaxLength(30)]
    public string WorkforceFloor { get; set; } = string.Empty;

    [MaxLength(30)]
    public string WorkforceStatus { get; set; } = "Chưa xác định";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
