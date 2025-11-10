using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using DatingClient.Models;

namespace DatingClient.Services;

public class SocketService
{
    private readonly ApiService _api;
    private readonly ClientWebSocket _ws = new();
    private static bool _isConnected;
    public static bool IsConnected => _isConnected;
    
    public SocketService(ApiService api)
    {
        _api = api;
    }

    public async Task ConnectAsync()
    {
        if (_isConnected) return;
        var sessionResponse = await _api.PostAsync<WsSessionToken>("/ws/start", null);// TODO move to api
        var wsUrl = _api.BaseUrl.Replace("http", "ws") + $"/ws/chat?session={sessionResponse?.SessionToken}";
        var uri = new Uri(wsUrl);
        await _ws.ConnectAsync(uri, CancellationToken.None);
        _ = ListenAsync();
        
        _isConnected = true;
    }

    public async Task SendMessageAsync(object message)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task ListenAsync()
    {
        var buffer = new byte[4096];
        while (_ws.State == WebSocketState.Open)
        {
            var result = await _ws.ReceiveAsync(buffer, CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
                break;
            
            var json = String.Empty;
            try
            {
                json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var msg = JsonSerializer.Deserialize<WSMessage>(json);
                if (msg is { Type: Constants.Message })
                {
                    OnMessageReceived?.Invoke(msg);
                }
                else if (msg is { Type: Constants.Match })
                {
                    await Shell.Current.DisplayAlert("Match", msg.Content, "OK");
                }
            }
            catch (Exception e)
            {
                await Shell.Current.DisplayAlert("Error", $"Couldn't read ws message - {json}", "OK");
            }
        }
    }

    public event Action<WSMessage>? OnMessageReceived;
}
