using System.Collections;
using System.Collections.Specialized;
using DatingClient.Models;
using DatingClient.ViewModels;

namespace DatingClient.Views;

public partial class MessagesPage : ContentPage
{
    private bool _isLoadingMore;
    private bool _lastMsgReached = true;
    private bool _firstMsgReached;
    
    public MessagesPage(MessagesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        (vm.Messages ??= []).CollectionChanged += Messages_CollectionChanged;
        // снимаем подавление через короткую задержку, когда UI отрисуется
        _ = Task.Delay(_initialSuppressDuration).ContinueWith(_ => _suppressInitialMark = false, TaskScheduler.FromCurrentSynchronizationContext());

    }
    
    protected override void OnAppearing()
    {
        base.OnAppearing();
        Shell.SetTabBarIsVisible(this, false);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is MessagesViewModel vm)
            vm.OnDisappearing();
        
        Shell.SetTabBarIsVisible(this, true);
    }
    
    private void Messages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isLoadingMore) return;
        if (e.Action == NotifyCollectionChangedAction.Add && 
            MessagesList.ItemsSource is IList list && 
            _lastMsgReached)
        {
            // scroll to the last message
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(50); // let UI render up
                var last = list[list.Count - 1];
                MessagesList.ScrollTo(last, position: ScrollToPosition.End, animate: true);
            });
        }
    }

    private async void OnMessagesScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        if (_isLoadingMore) return;
        
        // e.FirstVisibleItemIndex == 0 → user at the top
        if (!_firstMsgReached && e.FirstVisibleItemIndex <= 3 && e.VerticalDelta < 0)
        {
            _isLoadingMore = true;
            _lastMsgReached = false;
            GoToLastMsgBtn.IsVisible = true;
            
            try
            {
                // получим текущий первый видимый индекс и элемент
                int firstIndex = Math.Max(e.FirstVisibleItemIndex, 0);
                // вызываем загрузку старых сообщений в VM и ожидаем
                if (BindingContext is MessagesViewModel vm)
                {
                    // блокируем ввод на время вставки/скролла
                    MessagesList.InputTransparent = true;
                    
                    int inserted = vm.SyncOldestWithUi();
                    if (inserted > 0)
                    {
                        // прокручиваем к тому же элементу — индекс сместился на inserted
                        int targetIndex = firstIndex + inserted;

                        // даём MAUI применить вставки (обычно достаточно Yield; при проблемах — краткая задержка)
                        await Task.Yield();
                        MessagesList.ScrollTo(targetIndex+1, position: ScrollToPosition.Start, animate: false);
                    }
                    else
                    {
                        await Task.Yield();
                        _firstMsgReached = true;
                    }   
                    // небольшой короткий пауз, чтобы UI стабилизировался
                    await Task.Delay(30);
                    MessagesList.InputTransparent = false;

                    vm.LoadOlderMessagesCommand.Execute(null);
                } 
            }
            finally
            {
                _isLoadingMore = false;
            }
        }
        else if (!_lastMsgReached && e.VerticalDelta > 0 
                 && MessagesList.ItemsSource is IList list && list.Count-1 <= e.LastVisibleItemIndex)
        {
            if (BindingContext is MessagesViewModel vm)
            {
                if (vm.IsMessagesNewest)
                {
                    _lastMsgReached = true;
                    GoToLastMsgBtn.IsVisible = false;
                }
                else
                {
                    MessagesList.InputTransparent = true;
                    int lastIndex = Math.Max(e.LastVisibleItemIndex, 0);
                    int inserted = vm.SyncNewestWithUi();
                    if (inserted > 0)
                    {
                        // прокручиваем к тому же элементу — индекс сместился на inserted
                        int targetIndex = lastIndex + inserted;

                        // даём MAUI применить вставки (обычно достаточно Yield; при проблемах — краткая задержка)
                        await Task.Yield();
                        MessagesList.ScrollTo(targetIndex-1, position: ScrollToPosition.End, animate: false);
                    }
                    // небольшой короткий пауз, чтобы UI стабилизировался
                    await Task.Delay(30);
                    MessagesList.InputTransparent = false;

                    vm.LoadNewerMessagesCommand.Execute(null);
                    
                    ////////////////////////
                    if(!vm.IsMessagesOldest)
                    {
                        _firstMsgReached = false;//check
                    }
                    
                }
            }
        }
        
        // --- New: mark visible messages as read ---
        if (BindingContext is MessagesViewModel vmRead && MessagesList.ItemsSource is IList messagesList)
        {
            // если подавление включено и событие не инициировано пользователем (VerticalDelta == 0), то пропускаем
            if (_suppressInitialMark)
            {
                // но если весь список очень короткий — пометим последние (защита: пользователь явно видит последние)
                // можно раскомментировать следующий блок, если нужно помечать короткие чаты сразу:
                // if (messagesList.Count <= MaxMarkAtOnce) { ... пометка последних ... }
            }
            else
            {
                int firstVisible = Math.Max(e.FirstVisibleItemIndex, 0);
                int lastVisible = Math.Min(e.LastVisibleItemIndex, messagesList.Count - 1);

                // защита: если индексы выглядят нефокусно (диапазон слишком большой), ограничим
                int visibleCount = lastVisible - firstVisible + 1;
                if (visibleCount <= 0 || visibleCount > 100) // 100 — защита от мусора в событиях
                {
                    // вместо использования диапазона попробуем пометить последние N элементов,
                    // если событие пришло с нулевым VerticalDelta (открытие) — не делать.
                    if (e.VerticalDelta != 0)
                    {
                        // помечаем последние MaxMarkAtOnce элементов
                        int start = Math.Max(0, messagesList.Count - MaxMarkAtOnce);
                        for (int i = start; i < messagesList.Count; i++)
                        {
                            if (messagesList[i] is Message msg && !msg.IsMine && !msg.IsRead)
                            {
                                msg.IsRead = true;
                                _readBuffer.Add(msg.Id);
                            }
                        }
                        ScheduleFlush();
                    }
                    // иначе молча выходим
                }
                else
                {
                    // ограничим количество помечаемых, чтобы не добавить слишком много сразу
                    int toMark = Math.Min(visibleCount, MaxMarkAtOnce);
                    int markStart = firstVisible;

                    // если visibleCount > MaxMarkAtOnce, центрируем пометку в конце (обычно видим последние)
                    if (visibleCount > MaxMarkAtOnce)
                        markStart = lastVisible - toMark + 1;

                    for (int i = markStart; i <= lastVisible && i < messagesList.Count; i++)
                    {
                        if (messagesList[i] is Message msg && !msg.IsMine && !msg.IsRead)
                        {
                            msg.IsRead = true; // сразу визуально
                            _readBuffer.Add(msg.Id); // на сервер отложенно
                        }
                    }
                    ScheduleFlush();
                }
            }
        }
    }

    private void GoToLast(object? sender, EventArgs e)
    {
        try
        {
            if(MessagesList.ItemsSource is IList list)
                MessagesList.ScrollTo(list[list.Count-1], position: ScrollToPosition.End, animate: true);
        }
        catch (Exception exception)
        {
            return;
        }
    }
    
    // буфер ID прочитанных сообщений
    private readonly HashSet<long> _readBuffer = new();
    private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(1);
    private bool _flushScheduled = false;
    
    private bool _suppressInitialMark = true;
    private readonly TimeSpan _initialSuppressDuration = TimeSpan.FromMilliseconds(1500);
    private const int MaxMarkAtOnce = 12; // максимально помечаем за раз

    private async void FlushReadBufferAsync()
    {
        if (_readBuffer.Count == 0) return;

        if (BindingContext is MessagesViewModel vm)
        {
            var ids = _readBuffer.ToArray();
            _readBuffer.Clear();
            try
            {
                await vm.MarkMessagesAsReadAsync(ids);
            }
            catch (Exception ex)
            {
                // логируем, но не мешаем UI
                Console.WriteLine($"Failed to mark messages read: {ex}");
            }
        }
        _flushScheduled = false;
    }

    private void ScheduleFlush()
    {
        if (_flushScheduled) return;
        _flushScheduled = true;

        _ = Task.Delay(_flushInterval).ContinueWith(_ => FlushReadBufferAsync());
    }
}