using ASMILE.BO.Core.Enums;

namespace ASMILE.BO.Application.DTOs;

public sealed class VirtualExplorerNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = "\uE8B7";
    public string VirtualPath { get; set; } = string.Empty;
    public VirtualNodeType NodeType { get; set; } = VirtualNodeType.Folder;
    public BusinessModule Module { get; set; } = BusinessModule.System;
    public BusinessEntityType? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public Guid? DocumentId { get; set; }
    public DateTime? DateModified { get; set; }
    public bool HasChildren { get; set; }
    public List<VirtualExplorerNodeDto> Children { get; set; } = new();
    public int SortOrder { get; set; }
}
