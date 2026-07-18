using System.ComponentModel.DataAnnotations;

namespace ASMILE.BO.Core.Entities;

public sealed class Department
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string DepartmentCode { get; set; } = string.Empty;

    [MaxLength(255)]
    public string DepartmentName { get; set; } = string.Empty;

    public int? ParentDepartmentId { get; set; }

    public int Level { get; set; } = 1;

    [MaxLength(500)]
    public string Note { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

