using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DatingClient.Models;

public class ChatSummary : INotifyPropertyChanged
{
    private long _id;
    private string _lastMessage = string.Empty;
    private DateTime? _lastMessageTime;
    private long _lastMessageUser;
    private long _user1;
    private long _user2;
    private string _title = string.Empty;
    private string _avatarUrl = string.Empty;
    // private bool _unreaded;
    private bool? _isRead;
    
    [JsonPropertyName("id")]
    public long Id {
        get => _id;
        set => SetField(ref _id, value);
    }
    
    [JsonPropertyName("last_message")]
    public string LastMessage {
        get => _lastMessage ;
        set => SetField(ref _lastMessage, value);
    }
    
    [JsonPropertyName("last_message_time")]
    public DateTime? LastMessageTime {
        get =>  _lastMessageTime;
        set => SetField(ref _lastMessageTime, value);
    }
    
    [JsonPropertyName("last_message_user")]
    public long LastMessageUser {
        get => _lastMessageUser;
        set => SetField(ref _lastMessageUser, value);
    }
    
    [JsonPropertyName("user1_id")]
    public long User1 {
        get => _user1;
        set => SetField(ref _user1, value);
    }
    
    [JsonPropertyName("user2_id")]
    public long User2 {
        get => _user2;
        set => SetField(ref _user2, value);
    }
    
    [JsonIgnore]
    public string Title {
        get => _title;
        set => SetField(ref _title, value);
    }
    
    [JsonIgnore]
    public string AvatarUrl {
        get => _avatarUrl;
        set => SetField(ref _avatarUrl, value);
    }
    
    [JsonPropertyName("is_read")]
    public bool? IsRead {
        get => _isRead ?? false;
        set => SetField(ref _isRead, value);
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