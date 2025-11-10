using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DatingClient.Models;
using DatingClient.Services;

namespace DatingClient.ViewModels;

public partial class LikesViewModel : ObservableObject
{
    private readonly ApiService _api;

    [ObservableProperty] private ObservableCollection<User> _likes = [];
    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private bool _isLoading;
    
    public LikesViewModel(ApiService api)
    {
        _api = api;
        _ = LoadLikesAsync();
    }

    private async Task LoadLikesAsync()
    {
        IsLoading = true;
        try
        {
            var likes = await _api.GetLikesAsync();
            if (likes is not null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Likes.Clear();
                    foreach (var user in likes)
                        Likes.Add(user);
                });
            }
            OnPropertyChanged(nameof(Likes));
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Ошибка", "Не удалось подключиться к серверу.", "Ок");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsRefreshing = true;
            await LoadLikesAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task LikeBackAsync(User user)
    {
        try
        {
            await _api.SendLikeAsync(user.Id);
            Likes.Remove(user);
            OnPropertyChanged(nameof(Likes));
        }
        catch (Exception e)
        {
            Shell.Current.DisplayAlert("Error", e.Message, "OK");
        }
    }
    
    [RelayCommand]
    private async Task DislikeAsync(User user)
    {
        try
        {
            await _api.SendDislikeAsync(user.Id);
            Likes.Remove(user);
            OnPropertyChanged(nameof(Likes));
        }
        catch (Exception e)
        {
            Shell.Current.DisplayAlert("Error", e.Message, "OK");
        }
    }
}