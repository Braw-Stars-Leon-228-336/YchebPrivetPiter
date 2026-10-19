using Avalonia.Controls;
using YchebPrivetPiter.ViewModels;

namespace YchebPrivetPiter.Views;

public partial class InspectionEditWindow : Window
{
    public InspectionEditWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is InspectionEditViewModel vm)
                vm.CloseRequested += Close;
        };
    }
}