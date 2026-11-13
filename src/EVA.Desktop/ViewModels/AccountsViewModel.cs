using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVA.Desktop.Models;
using EVA.Desktop.Services;

namespace EVA.Desktop.ViewModels;

public partial class AccountsViewModel : ViewModelBase
{
    private readonly EvaApiClient _apiClient;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _statusMessage;

    [ObservableProperty] private string _newEmail = string.Empty;
    [ObservableProperty] private string _newDisplayName = string.Empty;
    [ObservableProperty] private int _selectedProviderIndex;

    public ObservableCollection<EmailAccountDto> Accounts { get; } = [];
    public ObservableCollection<string> ProviderOptions { get; } = ["Gmail OAuth 2.0", "IMAP"];

    public AccountsViewModel(EvaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var list = await _apiClient.GetAccountsAsync();
            Accounts.Clear();
            foreach (var item in list)
            {
                Accounts.Add(item);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar contas: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(NewEmail))
        {
            ErrorMessage = "Informe o endereço de e-mail da conta.";
            return;
        }

        ErrorMessage = null;
        StatusMessage = null;
        try
        {
            var provider = SelectedProviderIndex == 1 ? EmailProviderType.Imap : EmailProviderType.GmailOAuth;
            await _apiClient.AddAccountAsync(NewEmail.Trim(), NewDisplayName.Trim(), provider);
            StatusMessage = $"Conta '{NewEmail.Trim()}' adicionada com sucesso.";
            NewEmail = string.Empty;
            NewDisplayName = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao adicionar conta: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ToggleAccountAsync(EmailAccountDto account)
    {
        ErrorMessage = null;
        try
        {
            await _apiClient.ToggleAccountAsync(account.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao alternar status da conta: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteAccountAsync(EmailAccountDto account)
    {
        ErrorMessage = null;
        try
        {
            await _apiClient.DeleteAccountAsync(account.Id);
            StatusMessage = $"Conta '{account.Email}' removida.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao remover conta: {ex.Message}";
        }
    }
}
