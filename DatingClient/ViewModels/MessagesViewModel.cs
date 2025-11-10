using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DatingClient.Models;
using DatingClient.Services;

namespace DatingClient.ViewModels;

[QueryProperty(nameof(ChatId), "chatId")]
public partial class MessagesViewModel : ObservableObject, IQueryAttributable
{
    const int MaxBufferSize = 125;
    const int OldestLimit = 30;
    const int NewestLimit = 30;
    
    private readonly ApiService _api;
    private readonly SocketService _socketService;
    
    [ObservableProperty] private int _chatId;
    [ObservableProperty] private int _receiverId;
    
    [ObservableProperty] private ObservableCollection<Message> _messages = [];
    [ObservableProperty] private string _newMessage = string.Empty;
    
    private bool _reachedOlder;
    private bool _reachedNewer;
    private bool _isLoadingOlder;
    private bool _isLoadingNewer;
    private long? _oldestMessageId;
    private long? _newestMessageId;
    
    public MessagesViewModel(ApiService api, SocketService socketService)
    {
        _api = api; 
        _socketService = socketService;
    }

    public bool IsMessagesNewest => Messages.LastOrDefault()?.Id == _newestMessageId; 
    public bool IsMessagesOldest => Messages.FirstOrDefault()?.Id == _oldestMessageId; 

    private async Task<Message?> SendMessageAsync(string text)
    {
        var msg = new Message
        {
            ChatID = ChatId,
            SenderID = _api.UserId,
            ReceiverID = ReceiverId,
            Content = text, 
        };
        return await _api.SendMessageAsync(msg);
    }
    
    private List<Message> _oldest = [];
    private List<Message> _newest = [];
    private async Task LoadInitialMessages()
    {
        Messages.Clear();
        try
        {
            const int limit = 70;
            var all = await _api.GetChatMessagesAsync(ChatId, limit);
            if (all is null) return;
            all.ForEach(x =>
            {
                x.IsMine = x.SenderID == _api.UserId;
            });
            
            all.Skip(30).Take(40).ToList().ForEach(x => Messages.Add(x));
            _oldest = all.Take(30).ToList();
            
            _oldestMessageId = all.FirstOrDefault()?.Id;
            _newestMessageId = all.LastOrDefault()?.Id;
            
            await _api.MarkChatMessagesReadAsync(ChatId);
        }
        catch (Exception e)
        {
            await Shell.Current.DisplayAlert("Error", e.Message, "Ok");
        }
    }
    
    public int SyncOldestWithUi()
    {
        int inserted = _oldest.Count;
        Messages.SyncMessagesWithUi(_oldest, m => m.Id, prepend: true);
        FreeBuffer(true);
        return inserted;
    }
    
    public int SyncNewestWithUi()
    {
        // if count more than max then we cut by OldestLimit
        int inserted = Messages.Count > MaxBufferSize ? OldestLimit-_newest.Count : _newest.Count;
        Messages.SyncMessagesWithUi(_newest, m => m.Id);
        FreeBuffer();
        return inserted;
    }

    private void FreeBuffer(bool prepend = false)
    {
        if (Messages.Count > MaxBufferSize)
        {
            if (prepend)
            {
                _newest = Messages.TakeLast(NewestLimit).ToList();
                _reachedNewer = false;
                var count = NewestLimit;
                foreach (var message in Messages.Reverse())
                {
                    Messages.Remove(message);
                    count--;
                    if (count == 0) break;
                }
            }
            else
            {
                _oldest = Messages.Take(OldestLimit).ToList();
                _reachedOlder = false;
                var count = OldestLimit;
                for (; count > 0; count--)
                {
                    Messages.Remove(Messages[0]);
                }
            }
        }
    }
    
    [RelayCommand]
    private async Task LoadOlderMessagesAsync()
    {
        if (_oldestMessageId is null) return;
        if (_isLoadingOlder || _reachedOlder) return;
        _isLoadingOlder = true;

        try
        {
            var older = await _api.GetChatMessagesBeforeAsync(ChatId, Messages.FirstOrDefault()?.Id, OldestLimit);
            if (older?.Any() == true)
            {
                foreach (var msg in older)
                    msg.IsMine = msg.SenderID == _api.UserId;

                _oldestMessageId = older.First().Id;
                _oldest = older;
            }
            else
            {
                _reachedOlder = true;
            }
        }
        finally
        {
            _isLoadingOlder = false;
        }
    }
    
    [RelayCommand]
    private async Task LoadNewerMessagesAsync()
    {
        if (_newestMessageId is null) return;
        if (_isLoadingNewer || _reachedNewer) return;
        _isLoadingNewer = true;

        try
        {
            var newer = await _api.GetChatMessagesAfterAsync(ChatId, Messages.LastOrDefault()?.Id, NewestLimit);
            if (newer?.Any() == true)
            {
                foreach (var msg in newer)
                    msg.IsMine = msg.SenderID == _api.UserId;

                _newest = newer;
                if(newer.Count < NewestLimit) _reachedNewer = true;
            }
            else
            {
                _reachedNewer = true;
            }
        }
        finally
        {
            _isLoadingNewer = false;
        }
    }
    
    [RelayCommand]
    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(NewMessage))
            return;
        var msg = await SendMessageAsync(NewMessage);
        if (msg is null) return;
        msg.IsMine = true;
        Messages.Add(msg);
        _newestMessageId = msg.Id;
        
        NewMessage = string.Empty;
    }
    
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            ChatId = Convert.ToInt32(query["chatId"]);
            ReceiverId = Convert.ToInt32(query["receiverId"]);
            await LoadInitialMessages();
        
            _socketService.OnMessageReceived += OnSocketMessage;
        }
        catch (Exception e)
        {
            throw; // TODO handle exception
        }
    }

    public void OnDisappearing()
    {
        _socketService.OnMessageReceived -= OnSocketMessage;
        
        var lastMessage = Messages.LastOrDefault();
        if (lastMessage is not null)
        {
            //TODO
            // WeakReferenceMessenger.Default.Send(lastMessage);//WeakReferenceMessenger.Default.Send(new SendItemMessage("Model Changed"));

            MessagingCenter.Send(this, "ChatClosed", new ChatSummary()
            {
                Id = ChatId,
                LastMessage = lastMessage.Content,
                LastMessageTime = lastMessage.CreatedAt,
                LastMessageUser = lastMessage.SenderID,
                IsRead = true
            });
        }

    }
    
    private void OnSocketMessage(WSMessage ws)
    {
        if (ws.ChatId != ChatId)
            return;
        var msg = new Message
        {
            Id = ws.Id??0 ,Content = ws.Content??"", IsMine = false, CreatedAt = DateTime.Now,
            SenderID = ws.UserId??0, ChatID = (int)ws.ChatId
        };
        _newestMessageId = msg.Id;
        Messages.Add(msg);
    }
}

public static class ObservableCollectionExtensions
{
    public static void SyncMessagesWithUi<T>(this ObservableCollection<T> target, List<T> source, Func<T, long> getId, bool prepend = false)
    {
        var existingIds = new HashSet<long>(target.Select(getId));

        var newItems = source
            .Where(x => !existingIds.Contains(getId(x)))
            .ToList();
        
        if (newItems.Count == 0)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (prepend)
            {
                for (int i = newItems.Count - 1; i >= 0; i--)
                    target.Insert(0, newItems[i]);
            }
            else
            {
                foreach (var item in newItems)
                    target.Add(item);
            }
            source.Clear();
        });
    }
}
