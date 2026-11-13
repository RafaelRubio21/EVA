using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVA.Desktop.Models;
using EVA.Desktop.Services;

namespace EVA.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly DesktopConfigService _configService;
    private readonly EvaApiClient _apiClient;
    private readonly EvaSignalRService _signalRService;

    [ObservableProperty] private ViewModelBase _currentView;
    [ObservableProperty] private string _activeSection = "Dashboard";
    [ObservableProperty] private string _signalRStatus = "Conectando...";
    [ObservableProperty] private string _signalRStatusColor = "#F59E0B";

    [ObservableProperty] private string? _realtimeAlertTitle;
    [ObservableProperty] private string? _realtimeAlertBody;
    [ObservableProperty] private bool _hasRealtimeAlert;

    public DashboardViewModel Dashboard { get; }
    public AccountsViewModel Accounts { get; }
    public SettingsViewModel Settings { get; }

    public MainViewModel()
    {
        _configService = new DesktopConfigService();
        _apiClient = new EvaApiClient(_configService);
        _signalRService = new EvaSignalRService(_configService);

        Dashboard = new DashboardViewModel(_apiClient);
        Accounts = new AccountsViewModel(_apiClient);
        Settings = new SettingsViewModel(_configService, _apiClient, ReconnectAsync);

        _currentView = Dashboard;

        _signalRService.ConnectionStateChanged += state =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                SignalRStatus = state;
                SignalRStatusColor = state switch
                {
                    "Conectado" => "#10B981",
                    "Reconectando..." => "#F59E0B",
                    _ => "#EF4444"
                };
            });
        };

        _signalRService.NotificationReceived += (payload, isCriticalArrival) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                RealtimeAlertTitle = isCriticalArrival
                    ? $"Novo e-mail {payload.PriorityLevel}: {payload.Subject}"
                    : $"Lembrete pendente ({payload.PriorityLevel}): {payload.Subject}";
                RealtimeAlertBody = $"{payload.Sender} — {payload.Summary}";
                HasRealtimeAlert = true;

                _ = Dashboard.RefreshAsync();
            });
        };

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await _configService.LoadAsync();
        await Dashboard.RefreshAsync();
        await _signalRService.ConnectAsync();
    }

    private async Task ReconnectAsync()
    {
        await _signalRService.ConnectAsync();
        await Dashboard.RefreshAsync();
    }

    [RelayCommand]
    private void NavigateTo(string section)
    {
        ActiveSection = section;
        CurrentView = section switch
        {
            "Accounts" => Accounts,
            "Settings" => Settings,
            _ => Dashboard
        };

        if (section == "Dashboard") _ = Dashboard.RefreshAsync();
        else if (section == "Accounts") _ = Accounts.LoadAsync();
        else if (section == "Settings") _ = Settings.LoadAsync();
    }

    [RelayCommand]
    private void DismissAlert()
    {
        HasRealtimeAlert = false;
    }
}
