using Avalonia.Controls;
using YchebPrivetPiter.ViewModels;

namespace YchebPrivetPiter.Views;

public partial class PaymentEditWindow : Window
{
    public PaymentEditWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is PaymentEditViewModel vm)
                vm.CloseRequested += Close;
        };
    }
}