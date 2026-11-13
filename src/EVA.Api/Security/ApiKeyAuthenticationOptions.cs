using Microsoft.AspNetCore.Authentication;

namespace EVA.Api.Security;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";
    public const string QueryParamName = "api_key";

    public string ApiKey { get; set; } = string.Empty;
}
