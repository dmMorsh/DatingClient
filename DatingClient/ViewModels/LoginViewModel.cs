using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DatingClient.Helpers;
using DatingClient.Services;
using static System.String;

namespace DatingClient.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly SocketService _socketService;
    
    public Command LoginCommand { get; }
    
    [ObservableProperty] private string _statusLabel = Empty;
    [ObservableProperty] private string _usernameEntry = Empty;
    [ObservableProperty] private string _passwordEntry = Empty;
    [ObservableProperty] private bool _isLoading;

    public LoginViewModel(ApiService api, SocketService socketService)
    {
        _api = api;
        _socketService = socketService;
        LoginCommand = new Command(async void () => await Login());
    }
    
    private async Task Login()
    {
        IsLoading = true;
        try
        {
            var success = await _api.LoginAsync(UsernameEntry, PasswordEntry);
            if (success)
            {
                AuthHelper.LogIn(_api, _socketService);
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", "Неверный логин или пароль", "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Ошибка", ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        await Application.Current.MainPage.DisplayAlert("Register", "Not implemented yet", "OK");
    }
    
    
    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        IsLoading = true;
        try
        {
            await _api.PostAsync<object>("/register", new
            {
                username = UsernameEntry,
                password = PasswordEntry
            });
            StatusLabel = "Registered! Now login.";
        }
        catch (Exception ex)
        {
            StatusLabel = $"{Constants.ApiBaseUrl} - /register - {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}