using EVA.Application.Common.Interfaces;
using EVA.Domain.Repositories;
using EVA.Infrastructure.BackgroundJobs;
using EVA.Infrastructure.Persistence;
using EVA.Infrastructure.Persistence.Repositories;
using EVA.Infrastructure.Services.Mocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EVA.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("EvaDatabase")
            ?? throw new InvalidOperationException("Connection string 'EvaDatabase' não configurada.");

        services.AddDbContext<EvaDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(EvaDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            }));

        services.AddScoped<IEmailRepository, EmailRepository>();

        // Mocks para os provedores externos nesta etapa inicial
        services.AddScoped<IAiAnalyzer, MockAiAnalyzer>();
        services.AddScoped<IEmailProvider, MockEmailProvider>();

        // Workers em segundo plano (Triagem e Monitoramento de Lembretes)
        services.AddHostedService<EmailIngestionBackgroundService>();
        services.AddHostedService<ReminderNotificationBackgroundService>();

        return services;
    }
}
