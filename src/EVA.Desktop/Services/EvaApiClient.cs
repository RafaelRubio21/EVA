using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EVA.Desktop.Models;

namespace EVA.Desktop.Services;

public sealed class EvaApiClient(DesktopConfigService configService)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private HttpClient CreateClient()
    {
        var config = configService.Current;
        var baseUrl = string.IsNullOrWhiteSpace(config.ApiBaseUrl)
            ? "http://localhost:5199"
            : config.ApiBaseUrl.TrimEnd('/');

        var client = new HttpClient { BaseAddress = new Uri(baseUrl + "/") };
        if (!string.IsNullOrWhiteSpace(config.ApiKey))
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", config.ApiKey);
        }

        return client;
    }

    public async Task<EmailStatsSummary?> GetStatsAsync(string? accountEmail = null, CancellationToken ct = default)
    {
        using var client = CreateClient();
        var url = string.IsNullOrWhiteSpace(accountEmail)
            ? "api/v1/emails/stats"
            : $"api/v1/emails/stats?accountEmail={Uri.EscapeDataString(accountEmail)}";

        return await client.GetFromJsonAsync<EmailStatsSummary>(url, JsonOptions, ct);
    }

    public async Task<PagedResult<EmailSummaryDto>?> GetEmailsAsync(
        string? accountEmail = null,
        PriorityLevel? priorityLevel = null,
        bool? requiresResponse = null,
        int page = 1,
        int pageSize = 30,
        CancellationToken ct = default)
    {
        using var client = CreateClient();
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };

        if (!string.IsNullOrWhiteSpace(accountEmail))
            query.Add($"accountEmail={Uri.EscapeDataString(accountEmail)}");
        if (priorityLevel.HasValue)
            query.Add($"priorityLevel={(int)priorityLevel.Value}");
        if (requiresResponse.HasValue)
            query.Add($"requiresResponse={requiresResponse.Value.ToString().ToLowerInvariant()}");

        var url = "api/v1/emails?" + string.Join("&", query);
        return await client.GetFromJsonAsync<PagedResult<EmailSummaryDto>>(url, JsonOptions, ct);
    }

    public async Task<EmailDetailsDto?> GetEmailByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var client = CreateClient();
        return await client.GetFromJsonAsync<EmailDetailsDto>($"api/v1/emails/{id}", JsonOptions, ct);
    }

    public async Task CompleteEmailAsync(Guid id, CancellationToken ct = default)
    {
        using var client = CreateClient();
        using var response = await client.PatchAsync($"api/v1/emails/{id}/complete", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RescheduleReminderAsync(Guid id, DateTimeOffset nextReminderAt, CancellationToken ct = default)
    {
        using var client = CreateClient();
        using var response = await client.PatchAsJsonAsync(
            $"api/v1/emails/{id}/reminder",
            new { NextReminderAt = nextReminderAt },
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<EmailAccountDto>> GetAccountsAsync(CancellationToken ct = default)
    {
        using var client = CreateClient();
        return await client.GetFromJsonAsync<IReadOnlyList<EmailAccountDto>>("api/v1/accounts", JsonOptions, ct) ?? [];
    }

    public async Task<EmailAccountDto?> AddAccountAsync(
        string email,
        string displayName,
        EmailProviderType providerType,
        CancellationToken ct = default)
    {
        using var client = CreateClient();
        using var response = await client.PostAsJsonAsync(
            "api/v1/accounts",
            new { Email = email, DisplayName = displayName, ProviderType = providerType },
            ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EmailAccountDto>(JsonOptions, ct);
    }

    public async Task<EmailAccountDto?> ToggleAccountAsync(Guid id, CancellationToken ct = default)
    {
        using var client = CreateClient();
        using var response = await client.PatchAsync($"api/v1/accounts/{id}/toggle", null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EmailAccountDto>(JsonOptions, ct);
    }

    public async Task DeleteAccountAsync(Guid id, CancellationToken ct = default)
    {
        using var client = CreateClient();
        using var response = await client.DeleteAsync($"api/v1/accounts/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<EvaSettingsDto?> GetSettingsAsync(CancellationToken ct = default)
    {
        using var client = CreateClient();
        return await client.GetFromJsonAsync<EvaSettingsDto>("api/v1/settings", JsonOptions, ct);
    }

    public async Task<EvaSettingsDto?> UpdateSettingsAsync(
        int emailPollingSeconds,
        int reminderCheckSeconds,
        CancellationToken ct = default)
    {
        using var client = CreateClient();
        using var response = await client.PutAsJsonAsync(
            "api/v1/settings",
            new
            {
                EmailPollingIntervalSeconds = emailPollingSeconds,
                ReminderCheckIntervalSeconds = reminderCheckSeconds
            },
            ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EvaSettingsDto>(JsonOptions, ct);
    }
}
