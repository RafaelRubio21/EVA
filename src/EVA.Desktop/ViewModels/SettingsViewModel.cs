using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVA.Desktop.Models;
using EVA.Desktop.Services;

namespace EVA.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly DesktopConfigService _configService;
    private readonly EvaApiClient _apiClient;
    private readonly Func<Task> _onConnectionSettingsSaved;

    [ObservableProperty] private string _apiBaseUrl = "http://localhost:5199";
    [ObservableProperty] private string _apiKey = string.Empty;
    [ObservableProperty] private int _emailPollingIntervalSeconds = 120;
    [ObservableProperty] private int _reminderCheckIntervalSeconds = 60;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _errorMessage;

    public SettingsViewModel(
        DesktopConfigService configService,
        EvaApiClient apiClient,
        Func<Task> onConnectionSettingsSaved)
    {
        _configService = configService;
        _apiClient = apiClient;
        _onConnectionSettingsSaved = onConnectionSettingsSaved;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        ApiBaseUrl = _configService.Current.ApiBaseUrl;
        ApiKey = _configService.Current.ApiKey;

        try
        {
            var remote = await _apiClient.GetSettingsAsync();
            if (remote is not null)
            {
                EmailPollingIntervalSeconds = remote.EmailPollingIntervalSeconds;
                ReminderCheckIntervalSeconds = remote.ReminderCheckIntervalSeconds;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Conexão local carregada, mas falhou ao buscar intervalos da API: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAllAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = null;

        try
        {
            await _configService.SaveAsync(new DesktopAppConfig
            {
                ApiBaseUrl = ApiBaseUrl.Trim(),
                ApiKey = ApiKey.Trim()
            });

            await _apiClient.UpdateSettingsAsync(EmailPollingIntervalSeconds, ReminderCheckIntervalSeconds);
            await _onConnectionSettingsSaved();

            StatusMessage = "Configurações salvas e sincronizadas com a API EVA!";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao salvar configurações: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
