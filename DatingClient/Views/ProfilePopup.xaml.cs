using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using DatingClient.Models;
using DatingClient.ViewModels;

namespace DatingClient.Views;

public partial class ProfilePopup : Popup
{
    public ProfilePopup(User user, ICommand likeCommand, ICommand skipCommand)
    {
        InitializeComponent();
        BindingContext = new ProfilePopupViewModel(user, likeCommand, skipCommand,() => CloseAsync());
        
    }
}