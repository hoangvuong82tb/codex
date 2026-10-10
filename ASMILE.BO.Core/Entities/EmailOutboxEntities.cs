namespace ASMILE.BO.Core.Entities;

public sealed class EmailOutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsBodyHtml { get; set; }
    public string Status { get; private set; } = "Pending";
    public int AttemptCount { get; private set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SentAtUtc { get; private set; }
    public DateTime? NextAttemptAtUtc { get; private set; }
    public string? LastError { get; private set; }
    public string? SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public byte[]? RowVersion { get; private set; }

    public void MarkSent(DateTime sentAtUtc)
    {
        Status = "Sent";
        SentAtUtc = sentAtUtc;
        NextAttemptAtUtc = null;
        LastError = null;
    }

    public void MarkFailed(string error, DateTime nextAttemptAtUtc)
    {
        Status = "Failed";
        AttemptCount++;
        NextAttemptAtUtc = nextAttemptAtUtc;
        LastError = error.Length > 2000 ? error[..2000] : error;
    }
}
