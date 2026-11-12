namespace EVA.Infrastructure.Services.Email;

public sealed class ImapEmailOptions
{
    public const string SectionName = "ImapEmail";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 993;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int MaxBatchSize { get; set; } = 20;
}
