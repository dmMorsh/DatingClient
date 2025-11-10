namespace DatingClient.Views;

public partial class LoadingOverlay : ContentView
{
    public LoadingOverlay()
    {
        InitializeComponent();
    }
    
    public static readonly BindableProperty IsLoadingProperty =
        BindableProperty.Create(
            nameof(IsLoading),
            typeof(bool),
            typeof(LoadingOverlay),
            false);

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }
}