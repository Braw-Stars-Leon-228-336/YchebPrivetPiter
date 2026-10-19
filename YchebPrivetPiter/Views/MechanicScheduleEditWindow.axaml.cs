using Avalonia.Controls;
using YchebPrivetPiter.ViewModels;

namespace YchebPrivetPiter.Views;

public partial class MechanicScheduleEditWindow : Window
{
    public MechanicScheduleEditWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is MechanicScheduleEditViewModel vm)
                vm.CloseRequested += Close;
        };
    }
}