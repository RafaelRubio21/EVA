using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EVA.Application.Common.Interfaces;
using EVA.Domain.Enums;
using EVA.Infrastructure.Services.Mocks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EVA.Infrastructure.Services.Ai;

public sealed class GeminiAiAnalyzer(
    HttpClient httpClient,
    IOptions<GeminiAiOptions> options,
    MockAiAnalyzer fallbackAnalyzer,
    ILogger<GeminiAiAnalyzer> logger) : IAiAnalyzer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
    };

    public async Task<AiEmailAnalysisResult> AnalyzeEmailAsync(
        string sender,
        string subject,
        string body,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken = default)
    {
        var config = options.Value;

        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            logger.LogDebug("[GeminiAiAnalyzer] ApiKey não configurada; utilizando MockAiAnalyzer como fallback.");
            return await fallbackAnalyzer.AnalyzeEmailAsync(sender, subject, body, receivedAt, cancellationToken);
        }

        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{config.Model}:generateContent?key={config.ApiKey}";

        var prompt = $$"""
            Você é a EVA (Entidade Virtual de Apoio), uma secretária executiva inteligente.
            Analise o e-mail abaixo recebido em {{receivedAt:O}} e classifique a prioridade, se exige resposta, um resumo conciso em português, o prazo final (deadline) se houver, o horário sugerido para o primeiro lembrete e, caso exija resposta, redija uma sugestão breve e cordial de rascunho de resposta em português (suggestedReplyDraft).

            Remetente: {{sender}}
            Assunto: {{subject}}
            Corpo:
            {{body}}
            """;

        var requestPayload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                responseSchema = new
                {
                    type = "OBJECT",
                    properties = new
                    {
                        priorityLevel = new
                        {
                            type = "STRING",
                            @enum = new[] { "Low", "Normal", "High", "Critical" }
                        },
                        requiresResponse = new { type = "BOOLEAN" },
                        summary = new { type = "STRING" },
                        suggestedReplyDraft = new { type = "STRING", nullable = true },
                        deadline = new { type = "STRING", nullable = true },
                        suggestedReminderAt = new { type = "STRING", nullable = true }
                    },
                    required = new[] { "priorityLevel", "requiresResponse", "summary" }
                }
            }
        };

        try
        {
            using var response = await httpClient.PostAsJsonAsync(endpoint, requestPayload, cancellationToken);
            response.EnsureSuccessStatusCode();

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(
                JsonOptions,
                cancellationToken);

            var rawJson = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                logger.LogWarning("[GeminiAiAnalyzer] Resposta vazia do Gemini; acionando fallback.");
                return await fallbackAnalyzer.AnalyzeEmailAsync(sender, subject, body, receivedAt, cancellationToken);
            }

            var parsed = JsonSerializer.Deserialize<GeminiStructuredAnalysis>(rawJson, JsonOptions);
            if (parsed is null)
            {
                return await fallbackAnalyzer.AnalyzeEmailAsync(sender, subject, body, receivedAt, cancellationToken);
            }

            var priority = Enum.TryParse<PriorityLevel>(parsed.PriorityLevel, ignoreCase: true, out var level)
                ? level
                : PriorityLevel.Normal;

            DateTimeOffset? deadline = DateTimeOffset.TryParse(parsed.Deadline, out var dl) ? dl : null;
            DateTimeOffset? reminder = DateTimeOffset.TryParse(parsed.SuggestedReminderAt, out var rm) ? rm : null;

            return new AiEmailAnalysisResult(
                priority,
                parsed.RequiresResponse,
                parsed.Summary,
                deadline,
                reminder,
                parsed.SuggestedReplyDraft);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[GeminiAiAnalyzer] Erro na chamada à API do Gemini; utilizando fallback seguro.");
            return await fallbackAnalyzer.AnalyzeEmailAsync(sender, subject, body, receivedAt, cancellationToken);
        }
    }

    private sealed record GeminiGenerateContentResponse(List<GeminiCandidate>? Candidates);
    private sealed record GeminiCandidate(GeminiContent? Content);
    private sealed record GeminiContent(List<GeminiPart>? Parts);
    private sealed record GeminiPart(string? Text);

    private sealed record GeminiStructuredAnalysis(
        string PriorityLevel,
        bool RequiresResponse,
        string Summary,
        string? SuggestedReplyDraft,
        string? Deadline,
        string? SuggestedReminderAt);
}
