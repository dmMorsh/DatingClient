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
    
    private async Task<HttpResponseMessage?> SendWithRetry(Func<Task<HttpResponseMessage>> action)
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
                    response = await action(); // повторяем запрос
                }
            }

            return response;
        }
        catch (Exception e)
        {
            await Shell.Current.DisplayAlert("Error", "Failed to connect", "Ok");
            return null;
        }

    }
    private async Task<bool> Refresh()
    {
        var response = await _http.PostAsJsonAsync($"{BaseUrl}/refresh",
            new { user_id = UserId, refresh_token = RefreshToken });

        //TODO fix out
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            Shell.Current.DisplayAlert("Error", "Failed to refresh, login again please", "Ok");

        if (!response.IsSuccessStatusCode)
            return false;

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();

        if (result is null || string.IsNullOrEmpty(result.AccessToken))
            return false;

        AccessToken = result.AccessToken;
        AccessExpires = result.AccessExpires;
        
        await SecureStorage.SetAsync(Constants.AccessToken, AccessToken);

        // Обновляем заголовки для будущих запросов
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

    public async Task MarkChatMessagesReadAsync(long chatId)
    {
        await PostAsync<bool>($"/chat/read/{chatId}", null);
    }
    
    // not using yet
    public async Task MarkMessagesReadAsync(long[] msgIds)
    {
        await PostAsync<bool>("/messages/read", msgIds);
    }

    public async Task<List<User>?> GetSwipeCandidatesAsync(SearchFilter filter, long? lastSeenId, int pageSize)
    {
        var query = QueryHelper.ToQueryDictionary(filter);
        
        query["last_seen_id"] = lastSeenId?.ToString();///ЧТО ТУТ БУДЕТ?
        query["page_size"] = pageSize.ToString();

        var url = await QueryHelper.BuildQueryStringAsync(query);
        return await GetAsync<List<User>?>($"/profiles/search?{url}");
    }
    
    
    // public async Task<List<User>?> __GetSwipeCandidatesAsync(SearchFilter filter, long? lastSeenId, int pageSize)
    // {
    //     var query = new Dictionary<string, string?>
    //     {
    //         ["gender"] = filter.Gender.ToString(),
    //         ["min_age"] = filter.MinAge.ToString(),
    //         ["max_age"] = filter.MaxAge.ToString(),
    //         ["max_distance_km"] = filter.MaxDistanceKm.ToString(),
    //         ["has_photo"] = filter.HasPhoto.ToString(),
    //         ["interested_in"] = filter.InterestedIn.ToString(),
    //         ["last_seen_id"] = lastSeenId?.ToString(),
    //         ["page_size"] = pageSize.ToString()
    //     };
    //
    //     var url = await BuildQueryStringAsync(query);
    //     return await GetAsync<List<User>?>($"/profiles/search?{url}");
    // }
    //
    // private async Task<string> BuildQueryStringAsync(Dictionary<string, string?> query)
    // {
    //     // Убираем null значения
    //     var filtered = query
    //         .Where(kv => !string.IsNullOrEmpty(kv.Value))
    //         .ToDictionary(kv => kv.Key, kv => kv.Value!);
    //
    //     if (!filtered.Any()) return string.Empty;
    //
    //     var content = new FormUrlEncodedContent(filtered);
    //     var qs = await content.ReadAsStringAsync(); // возвращает "a=1&b=2"
    //     return qs;
    // }

    
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