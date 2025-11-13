using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using DatingClient.Converters;

namespace DatingClient.Models;

public partial class SearchFilter : ObservableObject
{
    [property: JsonConverter(typeof(GenderJsonConverter))]
    [property: JsonPropertyName("gender")]
    [ObservableProperty]
    private Gender? _gender;

    [property: JsonPropertyName("min_age")]
    [ObservableProperty]
    private int? _minAge;

    [property: JsonPropertyName("max_age")]
    [ObservableProperty]
    private int? _maxAge;

    [property: JsonPropertyName("max_distance_km")]
    [ObservableProperty]
    private double? _maxDistanceKm;
    
    [property: JsonPropertyName("latitude")]
    [ObservableProperty]
    private double? _latitude;
    
    [property: JsonPropertyName("longitude")]
    [ObservableProperty]
    private double? _longitude;

    [property: JsonPropertyName("has_photo")]
    [ObservableProperty]
    private bool? _hasPhoto;

    [property: JsonConverter(typeof(GenderJsonConverter))]
    [property: JsonPropertyName("interested_in")]
    [ObservableProperty]
    private Gender? _interestedIn;
    
    [property: JsonPropertyName("online_only")]
    [ObservableProperty]
    private bool? _onlineOnly;

    [property: JsonIgnore]
    [ObservableProperty] 
    private string? _location;
}
