using System.ComponentModel.DataAnnotations;

namespace ASMILE.BO.Core.Entities;

public sealed class VirtualDrive
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(16)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Icon { get; set; } = string.Empty;

    public int SortOrder { get; set; }
    public bool IsSystem { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
}
