using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DatingClient.Models;
using DatingClient.Services;
using DatingClient.Views;

namespace DatingClient.ViewModels;
 //TODO add chat when match
public partial class ChatsViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly CacheService _userCache;
    private readonly SocketService _socketService;
    private bool _isInitialized;
    private long OpenedChatId { get; set; }
    
    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private bool _isLoading;
    
    public ObservableCollection<ChatSummary> Chats { get; set; }
    public ICommand OpenChatCommand { get; }
    public ICommand LoadChatCommand { get; }

    public ChatsViewModel(ApiService api, SocketService socketService, CacheService userCache)
    {
        _api = api;
        _socketService = socketService;
        _userCache = userCache;
        _socketService.OnMessageReceived += OnSocketMessage;
        OpenChatCommand = new Command<ChatSummary>(async (chat) => await OpenChat(chat));
        LoadChatCommand = new Command(async void () => await LoadChat());
        
        Subsribes();
    }

    private void Subsribes()
    {
        MessagingCenter.Subscribe<MessagesViewModel, ChatSummary>(this, "ChatClosed", (sender, update) =>
        {
            var chat = Chats.FirstOrDefault(c => c.Id == update.Id);
            if (chat is not null)
            {
                chat.LastMessage = update.LastMessage;
                chat.LastMessageTime = update.LastMessageTime;
                chat.LastMessageUser = update.LastMessageUser;
                chat.IsRead = update.IsRead;
                
                OpenedChatId = 0;
            }
        });
    }

    private async Task LoadChat()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        IsLoading = true;
        
        (Chats ??= []).Clear();

        try
        {
            var myChatsAsync = await _userCache.GetMyChatsAsync();
            if (myChatsAsync is null || !myChatsAsync.Any()) return;

            var enriched = await Task.WhenAll(myChatsAsync.Select(async chat =>
            {
                var otherId = chat.User1 == _api.UserId ? chat.User2 : chat.User1;
                var profile = await _userCache.GetOrFetchUserProfileAsync(otherId);
                chat.Title = profile.DisplayName ?? "Unknown";
                if (chat.LastMessageTime == DateTime.MinValue) chat.LastMessageTime = null;
                chat.AvatarUrl = profile.LocalAvatarPath ?? profile.AvatarUrl ?? string.Empty;
                return chat;
            }));

            var sorted = enriched
                .OrderByDescending(c => c.LastMessageTime ?? DateTime.MinValue)
                .ToList();

            foreach (var chat in sorted)
                Chats.Add(chat);
        }
        catch (Exception e)
        {
            await Shell.Current.DisplayAlert("Error", e.Message, "Ok");
        }
        OnPropertyChanged(nameof(Chats));
        IsLoading = false;
    }

    private async Task OpenChat(ChatSummary chat)
    {
        chat.IsRead = true;
        OpenedChatId = chat.Id;
        
        var receiver = chat.User1 == _api.UserId ? chat.User2 : chat.User1;
        await Shell.Current.GoToAsync($"{nameof(MessagesPage)}?chatId={chat.Id}&receiverId={receiver}");
    }
    
    private void OnSocketMessage(WSMessage msg)
    {
        if (msg.ChatId is null) return;
        var chat = Chats?.FirstOrDefault(x => x.Id == msg.ChatId);
        if (chat is null) return;
        chat.LastMessageTime = DateTime.Now;
        chat.LastMessage = msg.Content!;
        chat.LastMessageUser = (int)msg.UserId!;
        if (OpenedChatId != chat.Id)
            chat.IsRead = false;
        Chats?.Move(Chats.IndexOf(chat), 0);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsRefreshing = true;
            _isInitialized = false;
            await LoadChat();
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}