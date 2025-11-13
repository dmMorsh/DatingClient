using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DatingClient.Models;

public class Message : ObservableObject
{
    [JsonIgnore]
    public string Sender { get; set; }
    
    [JsonIgnore]
    public bool IsMine { get; set; }
    
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("sender_id")]
    public long SenderID { get; set; }
    
    [JsonPropertyName("receiver_id")]
    public long ReceiverID { get; set; }
    
    [JsonPropertyName("chat_id")]
    public long ChatID { get; set; }
    
    [JsonPropertyName("content")]
    public string Content { get; set; }
    
    private bool _isRead;

    [JsonPropertyName("is_read")]
    public bool IsRead
    {
        get => _isRead;
        set => SetProperty(ref _isRead, value);
    }
    
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }
}