using System.Collections.ObjectModel;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DatingClient.Services;
using DatingClient.Models;
using Newtonsoft.Json.Linq;
using Timer = System.Timers.Timer;

namespace DatingClient.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly CacheService _cacheService;
    private readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("https://nominatim.openstreetmap.org/")
    };
    
    private readonly Timer _debounceTimer;
    
    [ObservableProperty] private User _user;
    //TODO fix on server
    [ObservableProperty] private DateTime _birthday;

    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _statusMessage;
    
    [ObservableProperty] private bool _isDetecting;
    
    [ObservableProperty] private string _locationQuery = "";
    [ObservableProperty] private ObservableCollection<LocationSuggestion> _suggestions = new();
    // [ObservableProperty] private LocationSuggestion _selectedSuggestion;


    public ProfileViewModel(ApiService api, CacheService cacheService)
    {
        _api = api;
        _cacheService = cacheService;
        _ = LoadProfileAsync();
        
        _debounceTimer = new Timer(400);
        _debounceTimer.AutoReset = false;
        _debounceTimer.Elapsed += async (_, _) => await SearchLocationAsync();
    }
    
    private async Task LoadProfileAsync()
    {
        try
        {
            var user = await _cacheService.GetOrFetchUserAsync(_api.UserId, true);
            if (user is not null)
            {
                User = user;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
    }

    [RelayCommand]
    private void EditProfile()
    {
        IsEditing = !IsEditing;
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        try
        {
            await _api.UpdateProfileAsync(User);
            await _cacheService.UpdateUserAsync(User);
            StatusMessage = "Профиль сохранён";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsEditing = false;
        }
    }
    
    [RelayCommand]
    private async Task DetectLocationAsync()
    {
        if (IsDetecting)
            return;

        try
        {
            IsDetecting = true;

            var location = await Geolocation.GetLastKnownLocationAsync()
                           ?? await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));

            if (location is not null)
            {
                var placemarks = await Geocoding.GetPlacemarksAsync(location);
                var placemark = placemarks?.FirstOrDefault();

                User.Latitude = location.Latitude;
                User.Longitude = location.Longitude;
                User.Location = placemark?.Locality ?? $"{location.Latitude:F4}, {location.Longitude:F4}";
            }
            else
            {
                await Shell.Current.DisplayAlert("Ошибка", "Не удалось определить координаты", "Ок");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Ошибка", "Не удалось определить местоположение. Разрешите доступ к GPS.", "Ок");
        }
        finally
        {
            IsDetecting = false;
        }
    }
    
    // Вызывается под капотом ObservableProperty _locationQuery
    partial void OnLocationQueryChanged(string value)
    {
        // Запускаем таймер при каждом изменении текста
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }
    
    [RelayCommand]
    private async Task SearchLocationAsync()
    {
        if (string.IsNullOrWhiteSpace(LocationQuery))
        {
            MainThread.BeginInvokeOnMainThread(() => Suggestions.Clear());
            return;
        }

        try
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("DatingApp/1.0 (your@email.com)");
            _http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.9,en;q=0.8");
            
            var encoded = HttpUtility.UrlEncode(LocationQuery);
            var url = $"search?q={encoded}&format=json&limit=5&accept-language=ru";
            var json = await _http.GetStringAsync(url);
            var arr = JArray.Parse(json);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Suggestions.Clear();
                foreach (var item in arr)
                {
                    Suggestions.Add(new LocationSuggestion
                    {
                        DisplayName = item["display_name"]?.ToString(),
                        Name = item["name"]?.ToString(),
                        Latitude = double.Parse(item["lat"]?.ToString() ?? "0", System.Globalization.CultureInfo.InvariantCulture),
                        Longitude = double.Parse(item["lon"]?.ToString() ?? "0", System.Globalization.CultureInfo.InvariantCulture)
                    });
                }
            });
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    
    [RelayCommand]
    private async Task SelectSuggestionAsync(LocationSuggestion? suggestion)
    {
        if (suggestion is null) return;

        User.Latitude = suggestion.Latitude;
        User.Longitude = suggestion.Longitude;
        LocationQuery = String.Empty;
        
        var loc = new Location(User.Latitude ?? 0, User.Longitude ?? 0,new DateTimeOffset(DateTime.Now));
        var placemarks = await Geocoding.GetPlacemarksAsync(loc);
        var placemark = placemarks?.FirstOrDefault();
        if (placemark?.Locality is not null) User.Location = placemark.Locality;
    }
}
