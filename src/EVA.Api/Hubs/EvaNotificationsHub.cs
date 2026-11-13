using EVA.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EVA.Api.Hubs;

public interface IEvaNotificationClient
{
    Task ReceiveDueReminder(ReminderNotificationPayload notification);
    Task ReceiveCriticalEmailAlert(ReminderNotificationPayload notification);
}

[Authorize]
public sealed class EvaNotificationsHub : Hub<IEvaNotificationClient>;
