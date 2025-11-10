using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using DatingClient.Converters;

namespace DatingClient.Models;

public partial class SearchFilter : ObservableObject
{
    [JsonConverter(typeof(GenderJsonConverter))]
    [JsonPropertyName("gender")]
    [ObservableProperty]
    private Gender? _gender;

    [JsonPropertyName("min_age")]
    [ObservableProperty]
    private int? _minAge;

    [JsonPropertyName("max_age")]
    [ObservableProperty]
    private int? _maxAge;

    [JsonPropertyName("max_distance_km")]
    [ObservableProperty]
    private double? _maxDistanceKm;
    
    [JsonPropertyName("latitude")]
    [ObservableProperty]
    private double? _latitude;
    
    [JsonPropertyName("longitude")]
    [ObservableProperty]
    private double? _longitude;

    [JsonPropertyName("has_photo")]
    [ObservableProperty]
    private bool? _hasPhoto;

    [JsonConverter(typeof(GenderJsonConverter))]
    [JsonPropertyName("interested_in")]
    [ObservableProperty]
    private Gender? _interestedIn;
    
    [JsonPropertyName("online_only")]
    [ObservableProperty]
    private bool? _onlineOnly;

    [JsonIgnore]
    [ObservableProperty] 
    private string? _location;
}
