namespace EVA.Infrastructure.Services.Email;

public sealed class GmailOAuthOptions
{
    public const string SectionName = "GmailOAuth";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string UserEmail { get; set; } = "me";
    public string TokenStoragePath { get; set; } = "token.json";
    public int MaxBatchSize { get; set; } = 20;
}
