using System.Net;
using System.Net.Http.Json;
using DatingClient.Helpers;
using DatingClient.Models;

namespace DatingClient.Services;

public class ApiService
{
    private readonly HttpClient _http = new();
    public int UserId { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? AccessExpires { get; set; }

    public string BaseUrl
    {
        get
        {
#if DEBUG
            return Constants.ApiBaseUrl;
#endif
            return Preferences.Get("api_base_url", Constants.ApiBaseUrl);
        }
        set => Preferences.Set("api_base_url", value);
    }

    public void FillFromLoginResponse(LoginResponse lr)
    {
        UserId = lr.UserId;
        AccessToken = lr.AccessToken;
        RefreshToken = lr.RefreshToken;
        AccessExpires = lr.AccessExpires;
    }

    private void AddAuthHeader()
    {
        if (!string.IsNullOrEmpty(AccessToken))
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        var response = await SendWithRetry(()=> _http.GetAsync($"{BaseUrl}{endpoint}"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }
    
    public async Task<T?> PostAsync<T>(string endpoint, object? payload)
    {
        var response = await SendWithRetry(()=> _http.PostAsJsonAsync($"{BaseUrl}{endpoint}", payload));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }
    
    public async Task<T?> PutAsync<T>(string endpoint, object payload)
    {
        var response = await SendWithRetry(()=> _http.PutAsJsonAsync($"{BaseUrl}{endpoint}", payload));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<T?> DeleteAsync<T>(string endpoint)
    {
        var response = await SendWithRetry(()=> _http.DeleteAsync($"{BaseUrl}{endpoint}"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }
    
    private async Task<HttpResponseMessage> SendWithRetry(Func<Task<HttpResponseMessage>> action)
    {
        AddAuthHeader();
        try
        {
            var response = await action();
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshed = await Refresh();
                if (refreshed)
                {
                    AddAuthHeader();
                    response = await action(); // retry once
                }
            }

            return response;
        }
        catch
        {
            await Shell.Current.DisplayAlert("Error", "Failed to connect", "Ok");
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }

    }
    private async Task<bool> Refresh()
    {
        var response = await _http.PostAsJsonAsync($"{BaseUrl}/refresh",
            new { user_id = UserId, refresh_token = RefreshToken });

        //TODO fix out
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            await Shell.Current.DisplayAlert("Error", "Failed to refresh, login again please", "Ok");

        if (!response.IsSuccessStatusCode)
            return false;

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();

        if (result is null || string.IsNullOrEmpty(result.AccessToken))
            return false;

        AccessToken = result.AccessToken;
        AccessExpires = result.AccessExpires;
        
        await SecureStorage.SetAsync(Constants.AccessToken, AccessToken);

        // reapply header
        AddAuthHeader();

        return true;
    }
    
    //Separate coz we dont need refresh
    public async Task<bool> LoginAsync(string username, string password)
    {
        var deviceId = AuthHelper.GetDeviceId();
        var resp = await _http.PostAsJsonAsync($"{BaseUrl}{"/login"}", 
            new LoginRequest(username, password, deviceId));
        
        if (!resp.IsSuccessStatusCode)
        {
            await Application.Current?.MainPage?.DisplayAlert("Error", "Failed to login", "Ok")!;
            return false;
        }
        
        var res = await resp.Content.ReadFromJsonAsync<LoginResponse>();
        if (res is null) 
            return false;
        
        FillFromLoginResponse(res);
        return true;
    }
    
    public async Task LogoutAsync(long userId, string deviceId)
    {
        await PostAsync<Status>("/logout", new{ user_id = userId, device_id = deviceId });
    }
    
    
    public async Task<User?> GetMyProfileAsync()
    {
        return await GetAsync<User?>("/me");
    }    

    public async Task UpdateProfileAsync(User updated)
    {
        await PutAsync<User>("/me", updated); 
    }

    public async Task<List<ChatSummary>?> GetMyChatsAsync()
    {
        return await GetAsync<List<ChatSummary>?>("/chats");
    }

    public async Task<List<Message>?> GetChatMessagesAsync(long chatId, int limit = 50)
    {
        return await GetAsync<List<Message>?>($"/chat/messages/{chatId}?limit={limit}");
    }
    
    public async Task<List<Message>?> GetChatMessagesBeforeAsync(long chatId, long? beforeId, int limit = 50)
    {
        return await GetAsync<List<Message>?>($"/chat/messages/{chatId}?before_id={beforeId}&limit={limit}");
    }

    public async Task<List<Message>?> GetChatMessagesAfterAsync(long chatId, long? afterId, int limit)
    {
        return await GetAsync<List<Message>?>($"/chat/messages/{chatId}?after_id={afterId}&limit={limit}");
    }
    
    public async Task<User?> GetUserProfileAsync(long userId)
    {
        return await GetAsync<User?>($"/user/{userId}");
    }
    
    private record Status(string status, string? content);

    public async Task SendLikeAsync(long userId)
    {
        await PostAsync<Status>("/swipe", new{ target_id = userId, action = "like" });
    }
    
    public async Task SendDislikeAsync(long userId)
    {
        await PostAsync<Status>("/swipe", new{ target_id = userId, action = "dislike" });
    }

    public async Task<IEnumerable<User>?> GetLikesAsync()
    {
        return await GetAsync<List<User>?>("/followers");
    }

    public async Task MarkChatMessagesReadAsync(long chat_id, long receiver_id)
    {
        await PostAsync<bool>($"/chat/read", new { chat_id, receiver_id });
    }
    
    public async Task MarkMessagesReadAsync(long[] message_ids, long chat_id, long receiver_id)
    {
        await PostAsync<bool>("/messages/read", new { message_ids, chat_id, receiver_id });
    }

    public async Task<List<User>?> GetSwipeCandidatesAsync(SearchFilter filter, long? lastSeenId, int pageSize)
    {
        var query = QueryHelper.ToQueryDictionary(filter);
        
        query["last_seen_id"] = lastSeenId?.ToString();
        query["page_size"] = pageSize.ToString();

        var url = await QueryHelper.BuildQueryStringAsync(query);
        return await GetAsync<List<User>?>($"/profiles/search?{url}");
    }
    
    // Only for testing purposes
    public async Task ClearMySwipes()
    {
        await DeleteAsync<bool?>("/clear/my/swipes").ConfigureAwait(false);
    }

    public async Task<Message?> SendMessageAsync(Message msg)
    {
        return await PostAsync<Message>("/messages/send", msg);
    }
}