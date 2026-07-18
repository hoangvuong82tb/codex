namespace ASMILE.BO.Application.DTOs;

public sealed class LookupItemDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Display => string.IsNullOrWhiteSpace(Code) ? Name : $"{Code} - {Name}";
}

