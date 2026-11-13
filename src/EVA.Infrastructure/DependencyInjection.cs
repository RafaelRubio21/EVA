using EVA.Application.Common.Interfaces;
using EVA.Domain.Repositories;
using EVA.Infrastructure.BackgroundJobs;
using EVA.Infrastructure.Persistence;
using EVA.Infrastructure.Persistence.Repositories;
using EVA.Infrastructure.Services.Ai;
using EVA.Infrastructure.Services.Email;
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
        services.AddScoped<IEvaSettingsRepository, EvaSettingsRepository>();
        services.AddScoped<IEmailAccountRepository, EmailAccountRepository>();

        // Configurações tipadas de IA (Gemini) e E-mail (IMAP MailKit / Gmail OAuth)
        services.Configure<GeminiAiOptions>(configuration.GetSection(GeminiAiOptions.SectionName));
        services.Configure<ImapEmailOptions>(configuration.GetSection(ImapEmailOptions.SectionName));
        services.Configure<GmailOAuthOptions>(configuration.GetSection(GmailOAuthOptions.SectionName));

        // Mocks disponíveis como fallback automático quando credenciais externas não estiverem configuradas
        services.AddScoped<MockAiAnalyzer>();
        services.AddScoped<MockEmailProvider>();

        // Provedores reais com fallback transparente
        services.AddHttpClient<IAiAnalyzer, GeminiAiAnalyzer>();
        services.AddScoped<ImapEmailProvider>();
        services.AddScoped<IEmailProvider, GmailOAuthEmailProvider>();

        // Workers em segundo plano (Triagem e Monitoramento de Lembretes)
        services.AddHostedService<EmailIngestionBackgroundService>();
        services.AddHostedService<ReminderNotificationBackgroundService>();

        return services;
    }
}
