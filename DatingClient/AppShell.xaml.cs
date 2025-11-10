using DatingClient.Views;

namespace DatingClient;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(MessagesPage), typeof(MessagesPage));
    }
}