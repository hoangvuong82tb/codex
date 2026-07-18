namespace ASMILE.BO.Application.DTOs;

public sealed class DepartmentDto
{
    public int Id { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int? ParentDepartmentId { get; set; }
    public int Level { get; set; } = 1;
    public string Note { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int PeopleCount { get; set; }
}
