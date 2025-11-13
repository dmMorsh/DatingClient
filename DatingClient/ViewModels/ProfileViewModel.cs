using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DatingClient.Services;
using DatingClient.Models;

namespace DatingClient.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly CacheService _cacheService;
    private readonly LocationService _locationService;
    
    [ObservableProperty] private User _user;
    
    [ObservableProperty] private bool _isEditing;

    [ObservableProperty] private string _statusMessage;
    
    [ObservableProperty] private bool _isDetecting;
    
    [ObservableProperty] private string _locationQuery = "";
    
    public ObservableCollection<LocationSuggestion> Suggestions => _locationService.Suggestions;

    public ProfileViewModel(ApiService api, CacheService cacheService, LocationService locationService)
    {
        _api = api;
        _cacheService = cacheService;
        _locationService = locationService;
        _ = LoadProfileAsync();
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
            var location = await _locationService.DetectLocationAsync();

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
    
    partial void OnLocationQueryChanged(string value)
    { 
        _locationService.LocationQuery = value;
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
