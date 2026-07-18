using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASMILE.BO.Core.Entities;

public sealed class AuditLog : ICompanyScoped, IBranchScoped
{
    public int Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [MaxLength(100)]
    public string Username { get; set; } = "system";

    [MaxLength(200)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Detail { get; set; } = string.Empty;

    [MaxLength(100)]
    public string EntityName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string EntityId { get; set; } = string.Empty;

    public string? OldValuesJson { get; set; }

    public string? NewValuesJson { get; set; }

    [MaxLength(100)]
    public string? IpAddress { get; set; }

    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    [NotMapped]
    public string Module { get; set; } = string.Empty;

    [NotMapped]
    public string EntityType
    {
        get => EntityName;
        set => EntityName = value;
    }

    [NotMapped]
    public string OldValueJson
    {
        get => OldValuesJson ?? string.Empty;
        set => OldValuesJson = value;
    }

    [NotMapped]
    public string NewValueJson
    {
        get => NewValuesJson ?? string.Empty;
        set => NewValuesJson = value;
    }

    [NotMapped]
    public string UserName
    {
        get => Username;
        set => Username = value;
    }

    [NotMapped]
    public string MachineName { get; set; } = string.Empty;
}
