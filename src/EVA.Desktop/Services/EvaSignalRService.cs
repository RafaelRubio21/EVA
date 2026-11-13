using System;
using System.Threading.Tasks;
using EVA.Desktop.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace EVA.Desktop.Services;

public sealed class EvaSignalRService(DesktopConfigService configService) : IAsyncDisposable
{
    private HubConnection? _connection;

    public event Action<string>? ConnectionStateChanged;
    public event Action<ReminderNotificationPayload, bool>? NotificationReceived;

    public async Task ConnectAsync()
    {
        await DisconnectAsync();

        var config = configService.Current;
        var baseUrl = string.IsNullOrWhiteSpace(config.ApiBaseUrl)
            ? "http://localhost:5199"
            : config.ApiBaseUrl.TrimEnd('/');

        var hubUrl = string.IsNullOrWhiteSpace(config.ApiKey)
            ? $"{baseUrl}/hubs/eva-notifications"
            : $"{baseUrl}/hubs/eva-notifications?api_key={Uri.EscapeDataString(config.ApiKey)}";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<ReminderNotificationPayload>("ReceiveDueReminder", payload =>
            NotificationReceived?.Invoke(payload, false));

        _connection.On<ReminderNotificationPayload>("ReceiveCriticalEmailAlert", payload =>
            NotificationReceived?.Invoke(payload, true));

        _connection.Reconnecting += _ =>
        {
            ConnectionStateChanged?.Invoke("Reconectando...");
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
        {
            ConnectionStateChanged?.Invoke("Conectado");
            return Task.CompletedTask;
        };

        _connection.Closed += _ =>
        {
            ConnectionStateChanged?.Invoke("Desconectado");
            return Task.CompletedTask;
        };

        try
        {
            await _connection.StartAsync();
            ConnectionStateChanged?.Invoke("Conectado");
        }
        catch
        {
            ConnectionStateChanged?.Invoke("Offline");
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
