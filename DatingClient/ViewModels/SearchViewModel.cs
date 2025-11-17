using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DatingClient.Helpers;
using DatingClient.Models;
using DatingClient.Services;
using DatingClient.Utils;

namespace DatingClient.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly LocationService _locationService;
    private readonly ApiService _api;
    private readonly LimitedDeque<User> _deque = new(maxSize: 50, undoBuffer: 5);
    private const int PageSize = 20;
    private long? _lastSeenId;
    private bool _isDetecting;
    private bool _settingLq;

    public string LocationPlaceholder => string.IsNullOrEmpty(LocationQuery)
        ? string.IsNullOrEmpty(Filter.Location) ? "Укажи город..." : Filter.Location
        : LocationQuery;

    public ObservableCollection<LocationSuggestion> Suggestions => _locationService.Suggestions;
    [ObservableProperty] private string _locationQuery = "";
    [ObservableProperty] private bool _isLocationAutoFilled = true;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private SearchFilter _filter;
    private bool _endReached;

    public User? CurrentUser => _deque.Current;

    public bool HasPrevious => _deque.HasPrevious;

    public SearchViewModel(ApiService api, CacheService cacheService, LocationService locationService)
    {
        _api = api;
        _locationService = locationService;
        Filter = StorageHelper.LoadSearchFilter() ?? FillFilterFromUser(cacheService).Result;
        OnPropertyChanged(nameof(Filter));
        LoadUsersAsync();
    }

    private async Task<SearchFilter> FillFilterFromUser(CacheService cacheService)
    {
        var filter = new SearchFilter();
        var me = await cacheService.GetOrFetchUserAsync(_api.UserId, true);
        if (string.IsNullOrEmpty(me?.Username)) return filter;
        // fill default
        filter.InterestedIn = me.Gender;
        filter.Gender = me.InterestedIn;
        filter.MinAge = me.Age - 1;
        filter.MaxAge = me.Age + 1;
        filter.Latitude = me.Latitude;
        filter.Longitude = me.Longitude;
        filter.Location = me.Location;
        filter.MaxDistanceKm = 10;
        StorageHelper.SaveSearchFilter(filter);
        return filter;
    }

    private async void LoadUsersAsync()
    {
        if (IsLoading || _endReached) return;
        IsLoading = true;

        try
        {
            var users = await _api.GetSwipeCandidatesAsync(Filter, _lastSeenId, PageSize);
            if (users is not null)
            {
                _deque.AddRange(users);
                _lastSeenId = users.Last().Id;
                OnPropertyChanged(nameof(CurrentUser));
            }
            else
            {
                _endReached = true;
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Ошибка", "Не удалось загрузить профили.", "Ок");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LikeAsync()
    {
        if (CurrentUser is null) return;
        await _api.SendLikeAsync(CurrentUser.Id);
        MoveNext();
    }

    [RelayCommand]
    private async Task SkipAsync()
    {
        if (CurrentUser is null) return;
        await _api.SendDislikeAsync(CurrentUser.Id);
        MoveNext();
    }

    [RelayCommand]
    private void Undo()
    {
        if (_deque.MovePrevious())
        {
            OnPropertyChanged(nameof(CurrentUser));
            OnPropertyChanged(nameof(HasPrevious));
        }
    }

    private void MoveNext()
    {
        if (!_deque.MoveNext())
            LoadUsersAsync(); // if we are at the end, try to load more

        if (_deque.RemainingAhead < 5 && !_isLoading)
            LoadUsersAsync();

        OnPropertyChanged(nameof(CurrentUser));
        OnPropertyChanged(nameof(HasPrevious));
    }

    [RelayCommand]
    public async Task ApplyFilterAsync()
    {
        StorageHelper.SaveSearchFilter(Filter);
        _lastSeenId = null;
        _endReached = false;

        _deque.Clear();
        OnPropertyChanged(nameof(CurrentUser));
        await Task.Delay(50);
        LoadUsersAsync();
        OnPropertyChanged(nameof(HasPrevious));
    }

    [RelayCommand]
    private async Task DetectLocationAsync()
    {
        if (_isDetecting)
            return;
        try
        {
            _isDetecting = true;
            var location = await _locationService.DetectLocationAsync();

            if (location is not null)
            {
                var placemarks = await Geocoding.GetPlacemarksAsync(location);
                var placemark = placemarks?.FirstOrDefault();

                Filter.Latitude = location.Latitude;
                Filter.Longitude = location.Longitude;
                Filter.Location = placemark?.Locality ?? $"{location.Latitude:F4}, {location.Longitude:F4}";
                _settingLq = true;
                LocationQuery = Filter.Location;
                _settingLq = false;
            }
            else
            {
                await Shell.Current.DisplayAlert("Ошибка", "Не удалось определить координаты", "Ок");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Ошибка", "Не удалось определить местоположение. Разрешите доступ к GPS.",
                "Ок");
        }
        finally
        {
            _isDetecting = false;
        }
    }

    partial void OnLocationQueryChanged(string value)
    {
        if (_settingLq)
        {
            IsLocationAutoFilled = true;
            OnPropertyChanged(nameof(LocationPlaceholder));
            return;
        }

        ;
        _locationService.LocationQuery = value;
    }

    [RelayCommand]
    private async Task SelectSuggestionAsync(LocationSuggestion? suggestion)
    {
        if (suggestion is null) return;

        Filter.Latitude = suggestion.Latitude;
        Filter.Longitude = suggestion.Longitude;
        LocationQuery = String.Empty;

        var loc = new Location(Filter.Latitude ?? 0, Filter.Longitude ?? 0, new DateTimeOffset(DateTime.Now));
        var placemarks = await Geocoding.GetPlacemarksAsync(loc);
        var placemark = placemarks?.FirstOrDefault();
        if (placemark?.Locality is not null) Filter.Location = placemark.Locality;
        _settingLq = true;
        LocationQuery = Filter.Location;
        _settingLq = false;
    }
}