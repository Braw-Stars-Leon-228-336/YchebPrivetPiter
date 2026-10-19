using Avalonia.Controls;
using YchebPrivetPiter.ViewModels;

namespace YchebPrivetPiter.Views;

public partial class CarEditWindow : Window
{
    public CarEditWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is CarEditViewModel vm)
                vm.CloseRequested += Close;
        };
    }
}