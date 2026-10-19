using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using YchebPrivetPiter.ViewModels;

namespace YchebPrivetPiter.Views;

public partial class LoginView : UserControl
{
    public LoginView() => InitializeComponent();

    private void OnPasswordKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is LoginViewModel vm)
        {
            if (vm.SignInCommand.CanExecute(null))
                vm.SignInCommand.Execute(null);
        }
    }
}