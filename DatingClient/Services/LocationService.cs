using System.Collections.ObjectModel;
using System.Web;
using DatingClient.Models;
using Newtonsoft.Json.Linq;
using Timer = System.Timers.Timer;

namespace DatingClient.Services;

public class LocationService
{
    private readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("https://nominatim.openstreetmap.org/")
    };
    private readonly Timer _debounceTimer;

    public LocationService()
    {
        _debounceTimer = new Timer(400);
        _debounceTimer.AutoReset = false;
        _debounceTimer.Elapsed += async (_, _) => await SearchLocationAsync();
    }

    public ObservableCollection<LocationSuggestion> Suggestions { get; } = new();

    private string _locationQuery = "";
    public string LocationQuery
    {
        get => _locationQuery;
        set
        {
            _locationQuery = value;
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }
    }

    public async Task<Location?> DetectLocationAsync()
    {
        try
        {
            var location = await Geolocation.GetLastKnownLocationAsync()
                           ?? await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));
            return location is null ? null : new Location(location.Latitude, location.Longitude, DateTimeOffset.Now);
        }
        catch
        {
            return null;
        }
    }

    public async Task SearchLocationAsync()
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
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
