using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using DatingClient.Converters;
using SQLite;

namespace DatingClient.Models;

public class User : IUpdatable, INotifyPropertyChanged
{
    private long _id;
    private string _username = string.Empty;
    private string _name = string.Empty;
    private Gender _gender;
    private int _age;
    private DateTime? _birthday;
    private Gender _interestedIn;
    private string _bio = string.Empty;
    private string _photoUrl = string.Empty;
    private string _location = string.Empty;
    private double? _latitude;
    private double? _longitude;
    private int? _distanceKm;
    private string _createdAt = string.Empty;
    private string _lastActive = string.Empty;
    private DateTime _lastUpdated;
    private string? _localPhotoUrl = string.Empty;

    [PrimaryKey]
    [JsonPropertyName("id")]
    public long Id {
        get => _id;
        set => SetField(ref _id, value);
    }
    
    [JsonPropertyName("username")]
    public string Username {
        get => _username;
        set => SetField(ref _username, value);
    }
    
    [JsonPropertyName("name")]
    public string Name {
        get => _name;
        set => SetField(ref _name, value);
    }
    
    [JsonConverter(typeof(GenderJsonConverter))]
    [JsonPropertyName("gender")]
    public Gender Gender {
        get => _gender;
        set => SetField(ref _gender, value);
    }
    
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
        set => SetField(ref _age, value);
    }

    [JsonPropertyName("birthday")]
    public DateTime? Birthday
    {
        get => _birthday;
        set
        {
            SetField(ref _birthday, value); 
            
            if (Birthday is null) return;
            var today = DateTime.Today;
            var age = today.Year - Birthday?.Year ?? 0;
            if (Birthday?.Date > today.AddYears(-age)) age--;
            Age = age;
        }
    }

    [JsonConverter(typeof(GenderJsonConverter))]
    [JsonPropertyName("interested_in")]
    public Gender InterestedIn {
        get => _interestedIn;
        set => SetField(ref _interestedIn, value);
    }
    
    [JsonPropertyName("bio")]
    public string Bio {
        get => _bio;
        set => SetField(ref _bio, value);
    }
    
    [JsonPropertyName("photo_url")]
    public string PhotoUrl {
        get => _photoUrl;
        set => SetField(ref _photoUrl, value);
    }
    
    [JsonPropertyName("location")]
    public string Location {
        get => _location;
        set => SetField(ref _location, value);
    }
    
    [JsonPropertyName("latitude")]
    public double? Latitude {
        get => _latitude;
        set => SetField(ref _latitude, value);
    }
    
    [JsonPropertyName("longitude")]
    public double? Longitude {
        get => _longitude;
        set => SetField(ref _longitude, value);
    }
    
    [JsonPropertyName("distance_km")]
    public int? DistanceKm {
        get => _distanceKm;
        set => SetField(ref _distanceKm, value);
    }
    
    [JsonPropertyName("created_at")]
    public string CreatedAt {
        get => _createdAt;
        set => SetField(ref _createdAt, value);
    }
    
    [JsonPropertyName("last_active")]
    public string LastActive {
        get => _lastActive;
        set => SetField(ref _lastActive, value);
    }
    
    [JsonIgnore]
    public DateTime LastUpdated {
        get => _lastUpdated;
        set => SetField(ref _lastUpdated, value);
    }
    
    [JsonIgnore]
    public string? LocalPhotoUrl {
        get => _localPhotoUrl;
        set => SetField(ref _localPhotoUrl, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public enum Gender
{
    Unknown = 0,
    Male = 1,
    Female = 2,
}