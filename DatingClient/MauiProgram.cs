using CommunityToolkit.Maui;
using DatingClient.Views;
using DatingClient.Services;
using DatingClient.ViewModels;
using Microsoft.Extensions.Logging;

namespace DatingClient;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .UseMauiCommunityToolkit();
        
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<CacheService>();
        builder.Services.AddSingleton<SocketService>();
        builder.Services.AddSingleton<AvatarCacheService>();
        builder.Services.AddSingleton<LocationService>();
        
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<ProfilePage>();
        
        builder.Services.AddTransient<ChatsViewModel>();
        builder.Services.AddTransient<ChatsPage>();
        
        builder.Services.AddTransient<MessagesViewModel>();
        builder.Services.AddTransient<MessagesPage>();
        
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<SettingsPage>();
        
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<LoginPage>();
        
        builder.Services.AddTransient<LikesViewModel>();
        builder.Services.AddTransient<LikesPage>();
        
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<SettingsPage>();
        
        builder.Services.AddTransient<SearchViewModel>();
        builder.Services.AddTransient<SearchPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}