using System.Text.Json.Serialization;

namespace DatingClient.Models;

public record LoginResponse(
    [property: JsonPropertyName("user_id")]int UserId,
    [property: JsonPropertyName("access_token")]string AccessToken,
    [property: JsonPropertyName("refresh_token")]string RefreshToken,
    [property: JsonPropertyName("access_expires")]string AccessExpires
    );

public record TokenResponse(
    [property: JsonPropertyName("access_token")]string AccessToken,
    [property: JsonPropertyName("access_expires")]string AccessExpires
    );


public record LoginRequest(
    [property: JsonPropertyName("username")]string Username,
    [property: JsonPropertyName("password")]string Password,
    [property: JsonPropertyName("device_id")]string DeviceId
    );

public record WsSessionToken(
    [property: JsonPropertyName("session_token")]string SessionToken
    );