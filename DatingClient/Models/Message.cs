using System.Text.Json.Serialization;

namespace DatingClient.Models;

public class Message
{
    [JsonIgnore]
    public string Sender { get; set; }
    
    [JsonIgnore]
    public bool IsMine { get; set; } // для отображения справа/слева
    
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
    
    [JsonPropertyName("is_read")]
    public bool IsRead { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }
}