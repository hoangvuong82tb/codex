namespace ASMILE.BO.Infrastructure.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string FromDisplayName { get; set; } = "ASMILE-BO";
    public int PollSeconds { get; set; } = 30;
    public int TimeoutMilliseconds { get; set; } = 30_000;
}
