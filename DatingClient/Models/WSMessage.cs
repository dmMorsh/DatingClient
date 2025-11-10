using System.Text.Json.Serialization;

namespace DatingClient.Models;

public class WSMessage
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }
    
    [JsonPropertyName("type")]
    public required string Type { get; set; }
    
    //TODO make it object and add Message separate. so we cud send txt+img 4 example
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    [JsonPropertyName("chat_id")]
    public int? ChatId { get; set; }
    
    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }
}