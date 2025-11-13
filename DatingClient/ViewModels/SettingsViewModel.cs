using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DatingClient.Helpers;
using DatingClient.Services;

namespace DatingClient.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public bool IsDebug =>
#if DEBUG
        true;
#else
    false;
#endif

    private readonly ApiService _api;
    private readonly SocketService _socketService;
    private readonly CacheService _userCache;
    [ObservableProperty] private string _apiUrlEntry;

    public SettingsViewModel(ApiService api, SocketService socketService, CacheService userCache)
    {
        _api = api;
        _socketService = socketService;
        _userCache = userCache;
        ApiUrlEntry = _api.BaseUrl;

        CmdOnSaveClicked = new AsyncRelayCommand(OnSaveClicked);
        CmdOnLogoutClicked = new AsyncRelayCommand(OnLogoutClicked);
    }

    public IAsyncRelayCommand CmdOnLogoutClicked { get; set; }

    public IAsyncRelayCommand CmdOnSaveClicked { get; set; }
    public bool Connected => SocketService.IsConnected;


    private async Task OnSaveClicked()
    {
        _api.BaseUrl = ApiUrlEntry;
        await Shell.Current.DisplayAlert("OK", "Адрес сервера сохранён", "OK");
    }

    private Task OnLogoutClicked()
    {
        AuthHelper.Logout(_api, _socketService, _userCache);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ClearMySwipesAsync()
    {
        try
        {
            await _api.ClearMySwipes();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}