using DatingClient.ViewModels;

namespace DatingClient.Views;

public partial class ChatsPage : ContentPage
{
    public ChatsPage(ChatsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
    
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ChatsViewModel vm)
            vm.LoadChatCommand.Execute(null);
    }
}