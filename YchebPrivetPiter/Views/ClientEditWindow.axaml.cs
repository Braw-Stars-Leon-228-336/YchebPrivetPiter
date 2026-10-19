using Avalonia.Controls;
using YchebPrivetPiter.ViewModels;

namespace YchebPrivetPiter.Views;

public partial class ClientEditWindow : Window
{
    public ClientEditWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is ClientEditViewModel vm)
                vm.CloseRequested += Close;
        };
    }
}