using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Application.DTOs;

public sealed class PostingRequest
{
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public string? RequestedBy { get; set; }
}

public sealed class PostingPreviewResult
{
    public bool Success { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal Difference { get; set; }
    public IReadOnlyList<PostingLinePreview> Lines { get; set; } = Array.Empty<PostingLinePreview>();
    public IReadOnlyList<PostingError> Errors { get; set; } = Array.Empty<PostingError>();
}

public sealed class PostingLinePreview
{
    public int LineNo { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class PostingResult
{
    public bool Success { get; set; }
    public Guid? JournalId { get; set; }
    public string? JournalNo { get; set; }
    public string Message { get; set; } = string.Empty;
    public IReadOnlyList<PostingError> Errors { get; set; } = Array.Empty<PostingError>();

    public static PostingResult Failed(params PostingError[] errors)
        => new()
        {
            Success = false,
            Message = errors.Length == 0 ? "Posting failed." : errors[0].Message,
            Errors = errors
        };

    public static PostingResult Succeeded(JournalHeader journal, string message)
        => new()
        {
            Success = true,
            JournalId = journal.Id,
            JournalNo = journal.JournalNo,
            Message = message
        };
}

public sealed class JournalValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<PostingError> Errors { get; } = new();

    public void Add(string code, string message, int? lineNo = null)
        => Errors.Add(new PostingError { Code = code, Message = message, LineNo = lineNo });
}

public sealed class PostingError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? LineNo { get; set; }
}
