using DatingClient.Helpers;
using DatingClient.Services;
using DatingClient.ViewModels;
using DatingClient.Views;
using Microsoft.Maui.Platform;

namespace DatingClient;

public partial class App : Application
{
    public App(ApiService api, SocketService socketService)
    {
        InitializeComponent();

        ChooseMeinPageToSet(api, socketService);
    }
    
    protected override void OnStart()
    {
        SetThemeColours();
    }

    private void ChooseMeinPageToSet(ApiService api, SocketService socketService)
    {
        api.AccessToken = AuthHelper.GetAccessToken();

        if (!string.IsNullOrEmpty(api.AccessToken))
        {
            api.UserId = Convert.ToInt32(AuthHelper.GetUserId());
            api.RefreshToken = AuthHelper.GetRefreshToken();
            
            MainPage = new AppShell(); // user already logged in
            _ = socketService.ConnectAsync();
        }
        else
        {
            MainPage = new NavigationPage(new LoginPage(new LoginViewModel(api, socketService)));
        }
    }

    public static void SetMainPage(Page page)
    {
        if (Current is not null) Current.MainPage = page;
    }
    
    private static void SetThemeColours()
    {
    #if ANDROID
        var window = Platform.CurrentActivity?.Window;
        if (window is not null && Current is not null)
        {
            var lightColor = Current.Resources["PageColor"] as Color;
            var darkColor = Current.Resources["PageColorDark"] as Color;
            var lightTab = Current.Resources["TabBarBackgroundColor"] as Color;
            var darkTab = Current.Resources["TabBarBackgroundColorDark"] as Color;

            var appTheme = Current.RequestedTheme;
            var color = appTheme == AppTheme.Dark ? darkColor : lightColor;
            var colorTab = appTheme == AppTheme.Dark ? darkTab : lightTab;

            if (color is not null) window.SetStatusBarColor(color.ToPlatform());
            if (colorTab is not null) window.SetNavigationBarColor(colorTab.ToPlatform());

            Current.RequestedThemeChanged += (s, e) =>
            {
                var activityWindow = Platform.CurrentActivity?.Window;
                if (activityWindow is not null)
                {
                    var newLightColor = Current.Resources["PageColor"] as Color;
                    var newDarkColor = Current.Resources["PageColorDark"] as Color;
                    var newLightTab = Current.Resources["TabBarBackgroundColor"] as Color;
                    var newDarkTab = Current.Resources["TabBarBackgroundColorDark"] as Color;

                    var newColor = e.RequestedTheme == AppTheme.Dark ? newDarkColor : newLightColor;
                    var newColorTab = e.RequestedTheme == AppTheme.Dark ? newDarkTab : newLightTab;

                    if (newColor is not null) activityWindow.SetStatusBarColor(newColor.ToPlatform());
                    if (newColorTab is not null) activityWindow.SetNavigationBarColor(newColorTab.ToPlatform());
                }
            };
        }
    #endif
    }
}