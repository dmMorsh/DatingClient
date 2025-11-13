using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using DatingClient.Converters;
using SQLite;

namespace DatingClient.Models;

public partial class User : ObservableObject, IUpdatable
{
    [property: PrimaryKey]
    [property: JsonPropertyName("id")]
    [ObservableProperty]
    private long _id;
    
    [property: JsonPropertyName("username")]
    [ObservableProperty]
    private string _username = string.Empty;
    
    [property: JsonPropertyName("name")]
    [ObservableProperty]
    private string _name = string.Empty;
    
    [property: JsonConverter(typeof(GenderJsonConverter))]
    [property: JsonPropertyName("gender")]
    [ObservableProperty]
    private Gender _gender;
    
    private int _age;
    
    [JsonPropertyName("age")]
    public int Age
    {
        get
        {
            if (Birthday is null || Birthday == DateTime.MinValue) return _age;
            var today = DateTime.Today;
            var age = today.Year - Birthday?.Year ?? 0;
            if (Birthday?.Date > today.AddYears(-age)) age--;
            return age;
        }
        set => SetProperty(ref _age, value);
    }
    
    private DateTime? _birthday;
    
    [JsonPropertyName("birthday")]
    public DateTime? Birthday
    {
        get => _birthday;
        set
        {
            SetProperty(ref _birthday, value); 
            
            if (Birthday is null) return;
            var today = DateTime.Today;
            var age = today.Year - Birthday?.Year ?? 0;
            if (Birthday?.Date > today.AddYears(-age)) age--;
            Age = age;
        }
    }
    
    [property: JsonConverter(typeof(GenderJsonConverter))]
    [property: JsonPropertyName("interested_in")]
    [ObservableProperty]
    private Gender _interestedIn;
    
    [property: JsonPropertyName("bio")]
    [ObservableProperty]
    private string _bio = string.Empty;
    
    [property: JsonPropertyName("photo_url")]
    [ObservableProperty]
    private string _photoUrl = string.Empty;
    
    [property: JsonPropertyName("location")]
    [ObservableProperty]
    private string _location = string.Empty;
    
    [property: JsonPropertyName("latitude")]
    [ObservableProperty]
    private double? _latitude;
    
    [property: JsonPropertyName("longitude")]
    [ObservableProperty]
    private double? _longitude;
    
    [property: JsonPropertyName("distance_km")]
    [ObservableProperty]
    private int? _distanceKm;
    
    [property: JsonPropertyName("created_at")]
    [ObservableProperty]
    private string _createdAt = string.Empty;
    
    [property: JsonPropertyName("last_active")]
    [ObservableProperty]
    private string _lastActive = string.Empty;
    
    [property: JsonIgnore]
    [ObservableProperty]
    private DateTime _lastUpdated;
    
    [property: JsonIgnore]
    [ObservableProperty]
    private string? _localPhotoUrl = string.Empty;
}

public enum Gender
{
    Unknown = 0,
    Male = 1,
    Female = 2,
}