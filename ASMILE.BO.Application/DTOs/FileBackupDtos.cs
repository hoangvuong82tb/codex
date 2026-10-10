namespace ASMILE.BO.Application.DTOs;

public sealed class FileBackupRecordDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long Size { get; set; }
    public int FileCount { get; set; }
    public string Status { get; set; } = "Completed";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
}

public sealed class FileRestoreResultDto
{
    public int RestoredFiles { get; set; }
    public int SkippedFiles { get; set; }
}
