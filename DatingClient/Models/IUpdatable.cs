namespace DatingClient.Models;

public interface IUpdatable
{
    public DateTime LastUpdated { get; set; }
}