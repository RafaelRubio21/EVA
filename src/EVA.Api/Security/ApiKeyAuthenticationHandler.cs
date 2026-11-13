using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EVA.Api.Security;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IHostEnvironment environment)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var configuredKey = Options.ApiKey;

        // Em ambiente de desenvolvimento sem chave configurada, permite acesso com aviso para facilitar testes locais
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            if (environment.IsDevelopment())
            {
                Logger.LogWarning("[EVA Security] Security:ApiKey não configurada em Development; autenticação liberada.");
                return Task.FromResult(AuthenticateResult.Success(CreateTicket("eva-dev-client")));
            }

            return Task.FromResult(AuthenticateResult.Fail("Security:ApiKey não configurada no servidor."));
        }

        string? providedKey = null;

        if (Request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var headerValues))
        {
            providedKey = headerValues.FirstOrDefault();
        }
        else if (Request.Path.StartsWithSegments("/hubs") &&
                 Request.Query.TryGetValue(ApiKeyAuthenticationOptions.QueryParamName, out var queryValues))
        {
            providedKey = queryValues.FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var expectedBytes = Encoding.UTF8.GetBytes(configuredKey);
        var providedBytes = Encoding.UTF8.GetBytes(providedKey);

        if (expectedBytes.Length != providedBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes))
        {
            return Task.FromResult(AuthenticateResult.Fail("API Key inválida."));
        }

        return Task.FromResult(AuthenticateResult.Success(CreateTicket("eva-client")));
    }

    private AuthenticationTicket CreateTicket(string clientName)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, clientName),
            new Claim(ClaimTypes.Name, clientName)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationTicket(principal, Scheme.Name);
    }
}
