using System.Collections;
using System.Collections.Specialized;
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

                // object? firstItem = null;
                // if (MessagesList.ItemsSource is IList list && list.Count > firstIndex)
                //     firstItem = list[firstIndex];

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

                // // после вставки — пролистаем к тому же элементу чтобы избежать скачка
                // if (firstItem is not null)
                // {
                //     // небольшой delay — даём CollectionView применить вставки
                //     await Task.Delay(50);
                //     MessagesList.ScrollTo(firstItem, position: ScrollToPosition.Start, animate: false);
                // }
            }
            finally
            {
                _isLoadingMore = false;
            }
        
            // // Загрузить предыдущие сообщения
            // if (BindingContext is MessagesViewModel vm)
            //     vm.LoadOlderMessagesCommand.Execute(null);
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
}