using CommunityToolkit.Maui.Views;
using DatingClient.Models;
using DatingClient.ViewModels;

namespace DatingClient.Views;

public partial class LikesPage : ContentPage
{
    public LikesPage(LikesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private async void OnCardLoaded(object sender, EventArgs e)
    {
        if (sender is not Frame frame)
            return;

        frame.Opacity = 0;
        frame.TranslationY = 40;

        // appear animation
        await Task.WhenAll(
            frame.FadeTo(1, 250, Easing.CubicOut),
            frame.TranslateTo(0, 0, 250, Easing.CubicOut)
        );
    }

    private async void OnCardTapped(object sender, EventArgs e)
    {
        if (sender is VisualElement element && element.BindingContext is User user)
        {
            var vm = (LikesViewModel)BindingContext; 
            var popup = new ProfilePopup(user, vm.LikeBackCommand, vm.DislikeCommand);
            await this.ShowPopupAsync(popup);
        }
    }
}