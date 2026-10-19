using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;

namespace YchebPrivetPiter.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly AuthService _auth;

    /// <summary>Событие: пользователь успешно вошёл.</summary>
    public event Action<User>? LoginSucceeded;

    private string _login = "1";
    public string Login { get => _login; set => SetProperty(ref _login, value); }

    private string _password = "1";
    public string Password { get => _password; set => SetProperty(ref _password, value); }

    private string? _errorMessage;
    public string? ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

    public ICommand SignInCommand { get; }

    public LoginViewModel(AuthService auth)
    {
        _auth = auth;
        SignInCommand = new AsyncRelayCommand(SignInAsync);
    }

    private async Task SignInAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Введите логин и пароль";
            return;
        }

        IsBusy = true;
        try
        {
            var user = await _auth.AuthenticateAsync(Login.Trim(), Password);
            if (user is null)
            {
                ErrorMessage = "Неверный логин или пароль";
                return;
            }

            Password = string.Empty;
            LoginSucceeded?.Invoke(user);   // <-- вместо прямого вызова _main
        }
        finally
        {
            IsBusy = false;
        }
    }
}