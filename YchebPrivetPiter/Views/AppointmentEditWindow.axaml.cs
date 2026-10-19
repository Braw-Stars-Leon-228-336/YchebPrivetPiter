using Avalonia.Controls;
using YchebPrivetPiter.ViewModels;

namespace YchebPrivetPiter.Views;

public partial class AppointmentEditWindow : Window
{
    public AppointmentEditWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is AppointmentEditViewModel vm)
                vm.CloseRequested += Close;
        };
    }
}