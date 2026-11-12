using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EVA.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("[EVA Pipeline] Iniciando requisição {RequestName}", requestName);

        try
        {
            var response = await next(cancellationToken);
            stopwatch.Stop();

            logger.LogInformation(
                "[EVA Pipeline] Requisição {RequestName} concluída com sucesso em {ElapsedMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(
                ex,
                "[EVA Pipeline] Falha ao processar requisição {RequestName} após {ElapsedMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
