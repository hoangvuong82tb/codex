namespace ASMILE.BO.Application.DTOs;

public sealed class EmailDispatchRequestDto
{
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsBodyHtml { get; set; }
    public string? SourceType { get; set; }
    public Guid? SourceId { get; set; }
}

public sealed class EmailDispatchResultDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public string? LastError { get; set; }
}
