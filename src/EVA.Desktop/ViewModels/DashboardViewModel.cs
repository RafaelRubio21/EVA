using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVA.Desktop.Models;
using EVA.Desktop.Services;

namespace EVA.Desktop.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly EvaApiClient _apiClient;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _statusBanner;

    // KPIs
    [ObservableProperty] private int _totalEmails;
    [ObservableProperty] private int _pendingResponses;
    [ObservableProperty] private int _criticalPending;
    [ObservableProperty] private int _overdueDeadlines;

    // Filters
    [ObservableProperty] private string _selectedAccountFilter = "Todas as contas";
    [ObservableProperty] private string _selectedPriorityFilter = "Todas";
    [ObservableProperty] private bool _onlyPending = true;

    // Master-Detail
    [ObservableProperty] private EmailSummaryDto? _selectedEmail;
    [ObservableProperty] private EmailDetailsDto? _selectedEmailDetails;
    [ObservableProperty] private bool _isLoadingDetails;

    public ObservableCollection<string> AccountFilters { get; } = ["Todas as contas"];
    public ObservableCollection<string> PriorityFilters { get; } = ["Todas", "Critical", "High", "Normal", "Low"];
    public ObservableCollection<EmailSummaryDto> Emails { get; } = [];

    public DashboardViewModel(EvaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var accountFilter = SelectedAccountFilter == "Todas as contas" ? null : SelectedAccountFilter;
            PriorityLevel? priorityFilter = Enum.TryParse<PriorityLevel>(SelectedPriorityFilter, out var parsed)
                ? parsed
                : null;

            var statsTask = _apiClient.GetStatsAsync(accountFilter);
            var emailsTask = _apiClient.GetEmailsAsync(
                accountFilter,
                priorityFilter,
                OnlyPending ? true : null,
                page: 1,
                pageSize: 50);

            await Task.WhenAll(statsTask, emailsTask);

            var stats = await statsTask;
            if (stats is not null)
            {
                TotalEmails = stats.TotalEmails;
                PendingResponses = stats.PendingResponses;
                CriticalPending = stats.CriticalPending;
                OverdueDeadlines = stats.OverdueDeadlines;

                var currentSelection = SelectedAccountFilter;
                AccountFilters.Clear();
                AccountFilters.Add("Todas as contas");
                foreach (var acc in stats.ByAccount)
                {
                    AccountFilters.Add(acc.AccountEmail);
                }
                SelectedAccountFilter = AccountFilters.Contains(currentSelection) ? currentSelection : "Todas as contas";
            }

            var paged = await emailsTask;
            Emails.Clear();
            if (paged is not null)
            {
                foreach (var item in paged.Items)
                {
                    Emails.Add(item);
                }
            }

            if (SelectedEmail is not null)
            {
                var stillExists = false;
                foreach (var item in Emails)
                {
                    if (item.Id == SelectedEmail.Id)
                    {
                        stillExists = true;
                        break;
                    }
                }
                if (!stillExists)
                {
                    SelectedEmail = null;
                    SelectedEmailDetails = null;
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Não foi possível conectar à API EVA: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedEmailChanged(EmailSummaryDto? value)
    {
        if (value is null)
        {
            SelectedEmailDetails = null;
            return;
        }

        _ = LoadDetailsAsync(value.Id);
    }

    partial void OnSelectedAccountFilterChanged(string value)
    {
        if (!IsLoading) _ = RefreshAsync();
    }

    partial void OnSelectedPriorityFilterChanged(string value)
    {
        if (!IsLoading) _ = RefreshAsync();
    }

    partial void OnOnlyPendingChanged(bool value)
    {
        if (!IsLoading) _ = RefreshAsync();
    }

    private async Task LoadDetailsAsync(Guid id)
    {
        IsLoadingDetails = true;
        try
        {
            SelectedEmailDetails = await _apiClient.GetEmailByIdAsync(id);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar detalhes: {ex.Message}";
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }

    [RelayCommand]
    private async Task CompleteResponseAsync()
    {
        if (SelectedEmailDetails is null) return;

        try
        {
            await _apiClient.CompleteEmailAsync(SelectedEmailDetails.Id);
            StatusBanner = $"Resposta de '{SelectedEmailDetails.Subject}' marcada como concluída.";
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao concluir resposta: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SnoozeReminderAsync(string hoursText)
    {
        if (SelectedEmailDetails is null) return;
        if (!int.TryParse(hoursText, out var hours)) hours = 1;

        try
        {
            var nextTime = DateTimeOffset.UtcNow.AddHours(hours);
            await _apiClient.RescheduleReminderAsync(SelectedEmailDetails.Id, nextTime);
            StatusBanner = $"Lembrete reagendado para daqui a {hours}h.";
            await LoadDetailsAsync(SelectedEmailDetails.Id);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Não foi possível reagendar: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CopyDraftAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedEmailDetails?.SuggestedReplyDraft)) return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(SelectedEmailDetails.SuggestedReplyDraft);
            StatusBanner = "Rascunho de resposta copiado para a área de transferência!";
        }
    }
}
