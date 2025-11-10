using System.Collections.Concurrent;
using DatingClient.Models;
using SQLite;

namespace DatingClient.Services;

public class CacheService
{
    private readonly SQLiteAsyncConnection _db;
    private readonly ApiService _api;
    private readonly AvatarCacheService _avatarCache;
    private readonly ConcurrentDictionary<long, UserProfile> _userProfileCache = new();
    private readonly ConcurrentDictionary<long, User> _userCache = new();
    private readonly TimeSpan _maxCacheAge = TimeSpan.FromDays(2);

    public CacheService(ApiService api, AvatarCacheService avatarCache)
    {
        _api = api;
        _avatarCache = avatarCache;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "usercache.db3");
        _db = new SQLiteAsyncConnection(dbPath);
        _db.CreateTableAsync<UserProfile>().Wait();
        _db.CreateTableAsync<User>().Wait();
        _db.CreateTableAsync<ChatSummary>().Wait();
    }

    public async Task<UserProfile> GetOrFetchUserProfileAsync(long userId)
    {
        if (_userProfileCache.TryGetValue(userId, out var cached) && !IsExpired(cached))
            return cached;

        var local = await _db.FindAsync<UserProfile>(userId);
        if (local is not null && !IsExpired(local))
        {
            _userProfileCache[userId] = local;
            return local;
        }

        var fromServer = await FetchUserProfileFromServer(userId);
        if (fromServer is null)
        {
            return local ?? new UserProfile { Id = userId, DisplayName = $"User {userId}" };
        }

        if (!string.IsNullOrEmpty(fromServer.AvatarUrl))
        {
            var localPath = await _avatarCache.GetAvatarPathAsync(fromServer.AvatarUrl);
            fromServer.LocalAvatarPath = localPath;
        }
        
        fromServer.LastUpdated = DateTime.UtcNow;
        _userProfileCache[userId] = fromServer;
        await _db.InsertOrReplaceAsync(fromServer);

        return fromServer;
    }

    public async Task ClearCacheAsync()
    {
        _userProfileCache.Clear();
        _userCache.Clear();
        await _db.DeleteAllAsync<UserProfile>();
        await _db.DeleteAllAsync<User>();
        await _db.DeleteAllAsync<ChatSummary>();
    }

    private bool IsExpired(IUpdatable user) =>
        DateTime.UtcNow - user.LastUpdated > _maxCacheAge;

    private async Task<UserProfile?> FetchUserProfileFromServer(long userId)
    {
        try
        {
            var dto = await _api.GetUserProfileAsync(userId);
            if (dto is null)
                return null;
            
            return new UserProfile
            {
                Id = dto.Id,
                DisplayName = dto.Name ?? $"User {dto.Id}",
                AvatarUrl = dto.PhotoUrl
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<User> GetOrFetchUserAsync(int userId, bool itsMe = false)
    {
        if (_userCache.TryGetValue(userId, out var cached) && !IsExpired(cached))
            return cached;

        var local = await _db.FindAsync<User>(userId);
        if (local is not null && !IsExpired(local))
        {
            _userCache[userId] = local;
            return local;
        }

        User? fromServer;
        try
        {
            fromServer = itsMe
                ? await _api.GetMyProfileAsync()
                : await _api.GetUserProfileAsync(userId);
        }
        catch
        {
            fromServer = null;
        }
        
        if (fromServer is null)
        {
            return local ?? new User { Id = userId, Name = $"User {userId}" };
        }

        if (!string.IsNullOrEmpty(fromServer.PhotoUrl))
        {
            var localPath = await _avatarCache.GetAvatarPathAsync(fromServer.PhotoUrl);
            fromServer.LocalPhotoUrl = localPath;
        }
        
        fromServer.LastUpdated = DateTime.UtcNow;
        _userCache[userId] = fromServer;
        await _db.InsertOrReplaceAsync(fromServer);

        return fromServer;
    }

    public async Task UpdateUserAsync(User user)
    {
        user.LastUpdated = DateTime.UtcNow;
        _userCache[user.Id] = user;
        await _db.InsertOrReplaceAsync(user);
    }

    public async Task<List<ChatSummary>?> GetMyChatsAsync()
    {
        try
        {
            var remote = await _api.GetMyChatsAsync();
            if (remote is not null)
            {
                await _db.RunInTransactionAsync(tr =>
                {
                    tr.DeleteAll<ChatSummary>();
                    tr.InsertAll(remote);
                });

                return remote;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Offline, loading from cache: " + e.Message);
        }
        
        var local = await _db.Table<ChatSummary>()
            .ToListAsync();
        return local;
    }
}