namespace DatingClient;

public static class Constants
{
    // API конфигурация
    public const string ApiBaseUrl = "http://192.168.0.12:8088";
    // public const string ApiBaseUrl = "https://intellyjourney.ru"; //🛰️ 📡 
    
    // Ключи хранилища
    public const string UserId = "user_id";
    public const string AccessToken = "access_token";
    public const string RefreshToken = "refresh_token";
    public const string DeviceId = "device_id";
    
    // WebSocket события
    public const string Message = "message";
    public const string Match = "match";
}