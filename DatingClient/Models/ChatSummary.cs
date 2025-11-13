using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DatingClient.Models;

public partial class ChatSummary : ObservableObject
{
    [property: JsonPropertyName("id")]
    [ObservableProperty]
    private long _id;
    
    [property: JsonPropertyName("last_message")]
    [ObservableProperty]
    private string _lastMessage = string.Empty;
    
    [property: JsonPropertyName("last_message_time")]
    [ObservableProperty]
    private DateTime? _lastMessageTime;
    
    [property: JsonPropertyName("last_message_user")]
    [ObservableProperty]
    private long _lastMessageUser;
    
    [property: JsonPropertyName("user1_id")]
    [ObservableProperty]
    private long _user1;
    
    [property: JsonPropertyName("user2_id")]
    [ObservableProperty]
    private long _user2;
    
    [property: JsonIgnore]
    [ObservableProperty]
    private string _title = string.Empty;
    
    [property: JsonIgnore]
    [ObservableProperty]
    private string _avatarUrl = string.Empty;
    
    [property: JsonPropertyName("is_read")]
    [ObservableProperty]
    private bool? _isRead;
}