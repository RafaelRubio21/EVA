namespace EVA.Infrastructure.Services.Ai;

public sealed class GeminiAiOptions
{
    public const string SectionName = "GeminiAi";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.5-flash";
}
