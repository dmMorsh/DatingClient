using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using DatingClient.ViewModels;

namespace DatingClient.Views;

public partial class FilterPopup : Popup
{
    public FilterPopup(SearchViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
    
    // Метод для закрытия popup
    public void Close(object? sender, EventArgs e)
    {
        Close();
    }
    
    public void Apply(object? sender, EventArgs e)
    {
        if (BindingContext is SearchViewModel vm)
            vm.ApplyFilterCommand.Execute(null);

        Close();    
    }
    
    private void TapGestureRecognizer_OnTapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is SearchViewModel vm && vm.IsLocationAutoFilled)
        {
            vm.IsLocationAutoFilled = false;
        }
    }

    private void VisualElement_OnUnfocused(object? sender, FocusEventArgs e)
    {
        if (BindingContext is SearchViewModel vm && !vm.IsLocationAutoFilled)
        {
            vm.IsLocationAutoFilled = true;
        }
    }

    private void FilterPopup_OnClosed(object? sender, PopupClosedEventArgs e)
    {
        if (BindingContext is SearchViewModel vm && !vm.IsLocationAutoFilled)
        {
            vm.IsLocationAutoFilled = true;
        }
    }
}