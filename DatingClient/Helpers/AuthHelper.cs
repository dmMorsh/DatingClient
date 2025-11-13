using DatingClient.Services;
using DatingClient.ViewModels;
using DatingClient.Views;

namespace DatingClient.Helpers;

public static class AuthHelper
{
    public static string? GetUserId()
        => SecureStorage.GetAsync(Constants.UserId).GetAwaiter().GetResult();

    public static string? GetAccessToken()
        => SecureStorage.GetAsync(Constants.AccessToken).GetAwaiter().GetResult();

    public static string? GetRefreshToken()
        => SecureStorage.GetAsync(Constants.RefreshToken).GetAwaiter().GetResult();

    public static string GetDeviceId()
    {
        var res = SecureStorage.GetAsync(Constants.DeviceId).GetAwaiter().GetResult();
        if (string.IsNullOrEmpty(res))
        {
            var deviceId = Guid.NewGuid().ToString();
            SecureStorage.SetAsync(Constants.DeviceId, deviceId).GetAwaiter().GetResult();
            return deviceId;
        }

        return res;
    }

    public static void Logout(ApiService api, SocketService socketService, CacheService userCache)
    {
        SecureStorage.Remove(Constants.UserId);
        SecureStorage.Remove(Constants.AccessToken);
        SecureStorage.Remove(Constants.RefreshToken);
        _ = userCache.ClearCacheAsync();
        App.SetMainPage(new NavigationPage(new LoginPage(new LoginViewModel(api, socketService))));
    }

    public static void LogIn(ApiService api, SocketService socketService)
    {
        SecureStorage.SetAsync(Constants.UserId, api.UserId.ToString());
        SecureStorage.SetAsync(Constants.AccessToken, api.AccessToken ?? string.Empty);
        SecureStorage.SetAsync(Constants.RefreshToken, api.RefreshToken ?? string.Empty);

        if (Application.Current != null) Application.Current.MainPage = new AppShell();
        _ = socketService.ConnectAsync();
    }
}