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

        // простая анимация появления
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
        // if (sender is not Grid grid) return;
        // var heart = grid.FindByName<Label>("LikeEffect");
        // if (heart is null) return;
        //
        // heart.Opacity = 0;
        // heart.Scale = 0.5;
        //
        // await Task.WhenAll(
        //     heart.FadeTo(1, 150, Easing.CubicOut),
        //     heart.ScaleTo(1.2, 150, Easing.CubicOut)
        // );
        //
        // await Task.Delay(300);
        // await heart.FadeTo(0, 200);
    }
}