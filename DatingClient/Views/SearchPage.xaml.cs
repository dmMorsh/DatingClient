using CommunityToolkit.Maui.Views;
using DatingClient.Models;
using DatingClient.ViewModels;

namespace DatingClient.Views;

public partial class SearchPage : ContentPage
{
    private double _x;
    private const double SwipeThreshold = 120;
    
    public SearchPage(SearchViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _x = CardFrame.TranslationX;
                break;

            case GestureStatus.Running:
                CardFrame.TranslationX = _x + e.TotalX;
                CardFrame.TranslationY = 0;
                CardFrame.Rotation = e.TotalX / 20;
                
                LikeLabel.Opacity = Math.Max(0, e.TotalX / 100);
                NopeLabel.Opacity = Math.Max(0, -e.TotalX / 100);
                break;

            case GestureStatus.Completed:
                HandleSwipe(CardFrame.TranslationX);
                break;
        }
    }

    private async void HandleSwipe(double totalX)
    {
        if (Math.Abs(totalX) > SwipeThreshold)
        {
            var vm = BindingContext as SearchViewModel;
            if (vm is null) return;

            var toRight = totalX > 0;
            var targetX = totalX > 0 ? 1000 : -1000;

            // card off-screen with fade-out
            await Task.WhenAll(
                CardFrame.TranslateTo(targetX, 0, 250, Easing.SinIn),
                CardFrame.FadeTo(0, 250)
            );

            // handle like or skip
            if (toRight)
                await vm.LikeCommand.ExecuteAsync(null);
            else
                await vm.SkipCommand.ExecuteAsync(null);

            Task.Delay(20).Wait();
            
            await HideLabels();
            // immediately reset position off-screen at top
            CardFrame.TranslationX = 0;
            CardFrame.TranslationY = -400;//300 ok too
            CardFrame.Rotation = 0;

            // bring card back on-screen with fade-in
            await Task.WhenAll(
                CardFrame.TranslateTo(0, 0, 250, Easing.SinOut),
                CardFrame.FadeTo(1, 200)
            );

        }
        else
        {
            // return to center
            await Task.WhenAll(
                CardFrame.TranslateTo(0, 0, 150, Easing.SpringOut),
                CardFrame.RotateTo(0, 150), HideLabels()
            );
        }
    }

    private Task HideLabels()
    {
        LikeLabel.FadeTo(0);
        NopeLabel.FadeTo(0);
        return Task.CompletedTask;
    }

    private async void Skip_OnClicked(object? sender, EventArgs e)
    {
        await NopeLabel.FadeTo(1);
        HandleSwipe(-SwipeThreshold-1);
    }

    private async void Like_OnClicked(object? sender, EventArgs e)
    {
        await LikeLabel.FadeTo(1);
        HandleSwipe(SwipeThreshold+1);
    }
    
    private async void OnCardTapped(object sender, EventArgs e)
    {
        if (sender is VisualElement element && element.BindingContext is User user)
        {
            var vm = (SearchViewModel)BindingContext; 
            var popup = new ProfilePopup(user, vm.LikeCommand, vm.SkipCommand);
            await this.ShowPopupAsync(popup);
        }
    }
    
    private async void FilterClicked(object? sender, EventArgs e)
    {
        if (BindingContext is SearchViewModel vm)
        {
            var popup = new FilterPopup(vm);
            await this.ShowPopupAsync(popup);
        }
    }

    private async void UndoClicked(object? sender, EventArgs e)
    {
        if (BindingContext is SearchViewModel vm && vm.HasPrevious)
        {
            vm.UndoCommand.Execute(null);
            
            // immediately move card off-screen at top
            CardFrame.TranslationX = 0;
            CardFrame.TranslationY = -800;
            CardFrame.Rotation = 0;
            Task.Delay(150).Wait();
            // bring card back on-screen with spring effect
            await Task.WhenAll(
                CardFrame.TranslateTo(0, 0, 1250, Easing.SpringOut),
                CardFrame.RotateTo(0, 1250), HideLabels()
            );
        }
    }
}