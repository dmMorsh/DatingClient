using SQLite;

namespace DatingClient.Models;

public class UserProfile : IUpdatable
{
    [PrimaryKey]
    public long Id { get; set; }

    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }
    
    public string? LocalAvatarPath { get; set; }

    public DateTime LastUpdated { get; set; }
}