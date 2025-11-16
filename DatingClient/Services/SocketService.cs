using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using DatingClient.Models;

namespace DatingClient.Services;

public class SocketService
{
    private readonly ApiService _api;

    private ClientWebSocket _ws;
    private CancellationTokenSource _cts;

    private bool _manualClose = false;
    private bool _isReconnecting = false;

    private int _reconnectDelay = 1000;
    private const int MaxReconnectDelay = 30000;

    private Uri _uri;
    private string _sessionToken;

    private static bool _isConnected;
    public static bool IsConnected => _isConnected;

    public event Action<WSMessage>? OnMessageReceived;

    public SocketService(ApiService api)
    {
        _api = api;
    }

    // ------------------------ CONNECT ------------------------
    public async Task ConnectAsync()
    {
        if (_isConnected)
            await Disconnect(); // in case we changed user

        _manualClose = false;

        await CreateNewSessionUri();

        await TryConnectOnce();
    }

    private async Task CreateNewSessionUri()
    {
        var sessionResponse = await _api.PostAsync<WsSessionToken>("/ws/start", null);
        _sessionToken = sessionResponse!.SessionToken;

        var wsUrl = _api.BaseUrl.Replace("http", "ws") + $"/ws/chat?session={_sessionToken}";
        _uri = new Uri(wsUrl);
    }

    private async Task TryConnectOnce()
    {
        try
        {
            _cts = new CancellationTokenSource();
            _ws = new ClientWebSocket();

            await _ws.ConnectAsync(_uri, _cts.Token);

            _isConnected = true;
            _reconnectDelay = 1000;

            _ = ListenAsync();
        }
        catch
        {
            _isConnected = false;
            _ = TryReconnectLoop();
        }
    }

    // ------------------------ SEND ------------------------
    public async Task SendMessageAsync(object message)
    {
        if (_ws?.State != WebSocketState.Open)
            return;

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);

        await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    // ------------------------ LISTEN ------------------------
    private async Task ListenAsync()
    {
        try
        {
            while (_ws.State == WebSocketState.Open)
            {
                var buffer = new byte[4096];
                using var ms = new MemoryStream();

                WebSocketReceiveResult result;

                do
                {
                    result = await _ws.ReceiveAsync(buffer, CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                        throw new WebSocketException("WS closed");

                    ms.Write(buffer, 0, result.Count);

                } while (!result.EndOfMessage);

                var json = Encoding.UTF8.GetString(ms.ToArray());
                await HandleMessage(json);
            }
        }
        catch
        {
            _isConnected = false;

            if (!_manualClose)
                _ = TryReconnectLoop();
        }
    }

    private async Task HandleMessage(string json)
    {
        try
        {
            var msg = JsonSerializer.Deserialize<WSMessage>(json);

            if (msg == null)
                return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnMessageReceived?.Invoke(msg);
            });
        }
        catch
        {
            await Shell.Current.DisplayAlert("Error", $"Invalid WS message: {json}", "OK");
        }
    }

    // ------------------------ RECONNECT ------------------------
    private async Task TryReconnectLoop()
    {
        if (_isReconnecting || _manualClose)
            return;

        _isReconnecting = true;

        while (!_manualClose)
        {
            try
            {
                await Task.Delay(_reconnectDelay);

                await CreateNewSessionUri();
                await TryConnectOnce();

                _isReconnecting = false;
                return;
            }
            catch
            {
                _isConnected = false;
                _reconnectDelay = Math.Min(_reconnectDelay * 2, MaxReconnectDelay);
            }
        }
    }

    // ------------------------ DISCONNECT ------------------------
    public async Task Disconnect()
    {
        _manualClose = true;
        _isConnected = false;

        try
        {
            if (_ws?.State == WebSocketState.Open)
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
        }
        catch
        {
            // ignored
        }

        _cts?.Cancel();
        _ws?.Dispose();
    }
}
