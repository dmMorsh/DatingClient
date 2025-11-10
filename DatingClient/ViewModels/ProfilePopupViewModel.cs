using System.Windows.Input;
using DatingClient.Models;

namespace DatingClient.ViewModels;

public class ProfilePopupViewModel
{
    private readonly Func<Task> _closeAction;
    public User User { get; }
    public ICommand LikeCommand { get; }
    public ICommand SkipCommand { get; }

    public ProfilePopupViewModel(User user, ICommand likeCommand, ICommand skipCommand, Func<Task> closeAsync)
    {
        User = user;
        _closeAction = closeAsync;
        if (likeCommand is not null)
        {
            LikeCommand = new Command(async void () =>
            {
                likeCommand?.Execute(User);
                await _closeAction();
            });
        }

        if (skipCommand is not null)
        {
            SkipCommand = new Command(async void () =>
            {
                skipCommand?.Execute(User);
                await _closeAction();
            });
        }
    }
}