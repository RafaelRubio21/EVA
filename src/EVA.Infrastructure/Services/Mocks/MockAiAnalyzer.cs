using EVA.Application.Common.Interfaces;
using EVA.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EVA.Infrastructure.Services.Mocks;

public sealed class MockAiAnalyzer(ILogger<MockAiAnalyzer> logger) : IAiAnalyzer
{
    public Task<AiEmailAnalysisResult> AnalyzeEmailAsync(
        string sender,
        string subject,
        string body,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[MockAiAnalyzer] Simulando análise de IA para o e-mail de '{Sender}' com assunto '{Subject}'.",
            sender,
            subject);

        var content = $"{subject} {body}".ToLowerInvariant();

        var isUrgent = content.Contains("urgente") ||
                       content.Contains("crítico") ||
                       content.Contains("prazo") ||
                       content.Contains("incidente");

        var requiresResponse = isUrgent ||
                               content.Contains("?") ||
                               content.Contains("aguardo") ||
                               content.Contains("confirmar") ||
                               content.Contains("aprovação");

        var priority = isUrgent
            ? PriorityLevel.Critical
            : requiresResponse
                ? PriorityLevel.High
                : PriorityLevel.Normal;

        DateTimeOffset? deadline = requiresResponse
            ? receivedAt.AddHours(isUrgent ? 4 : 24)
            : null;

        DateTimeOffset? reminder = requiresResponse
            ? receivedAt.AddHours(isUrgent ? 1 : 6)
            : null;

        var summary = $"[Triagem EVA] E-mail de {sender} sobre '{subject}'. " +
                      $"Prioridade classificada como {priority}. " +
                      (requiresResponse ? "Exige resposta do usuário." : "Apenas informativo.");

        var result = new AiEmailAnalysisResult(
            priority,
            requiresResponse,
            summary,
            deadline,
            reminder);

        return Task.FromResult(result);
    }
}
