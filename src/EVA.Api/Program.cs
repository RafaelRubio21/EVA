using EVA.Api.Hubs;
using EVA.Api.Services;
using EVA.Application;
using EVA.Application.Common.Interfaces;
using EVA.Domain.Exceptions;
using EVA.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Camadas da Clean Architecture
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// SignalR e Notificador em Tempo Real para Clientes Desktop e Mobile
builder.Services.AddSignalR();
builder.Services.AddScoped<IReminderNotifier, SignalRReminderNotifier>();

// Configuração da Web API
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Tratamento global de exceções (Domínio e Validação do Pipeline MediatR)
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        var problemDetails = exception switch
        {
            ValidationException validationEx => new ValidationProblemDetails(
                validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Erro de validação na requisição."
            },
            DomainException domainEx => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Violação de regra de domínio.",
                Detail = domainEx.Message
            },
            NotFoundException notFoundEx => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Recurso não encontrado.",
                Detail = notFoundEx.Message
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Erro interno no servidor."
            }
        };

        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHub<EvaNotificationsHub>("/hubs/eva-notifications");

app.Run();
