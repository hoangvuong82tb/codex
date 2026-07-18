using ASMILE.BO.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ASMILE.BO.Core.Entities;

public sealed class VirtualFolder
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(16)]
    public string DriveCode { get; set; } = string.Empty;

    public Guid? ParentId { get; set; }

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(256)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(512)]
    public string VirtualPath { get; set; } = string.Empty;

    public VirtualNodeType FolderType { get; set; } = VirtualNodeType.Folder;
    public BusinessEntityType? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public int SortOrder { get; set; }
    public bool IsSystem { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
