using EVA.Api.Hubs;
using EVA.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace EVA.Api.Services;

public sealed class SignalRReminderNotifier(
    IHubContext<EvaNotificationsHub, IEvaNotificationClient> hubContext) : IReminderNotifier
{
    public async Task NotifyDueReminderAsync(
        ReminderNotificationPayload payload,
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.All.ReceiveDueReminder(payload);
    }

    public async Task NotifyCriticalEmailArrivedAsync(
        ReminderNotificationPayload payload,
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.All.ReceiveCriticalEmailAlert(payload);
    }
}
