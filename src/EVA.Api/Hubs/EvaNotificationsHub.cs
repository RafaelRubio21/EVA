using EVA.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace EVA.Api.Hubs;

public interface IEvaNotificationClient
{
    Task ReceiveDueReminder(ReminderNotificationPayload notification);
    Task ReceiveCriticalEmailAlert(ReminderNotificationPayload notification);
}

public sealed class EvaNotificationsHub : Hub<IEvaNotificationClient>;
