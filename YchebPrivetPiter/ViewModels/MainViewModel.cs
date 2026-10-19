using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using YchebPrivetPiter.Data;

namespace YchebPrivetPiter.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _sp;

    private ViewModelBase _currentView = null!;
    public ViewModelBase CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    private User? _currentUser;
    public User? CurrentUser
    {
        get => _currentUser;
        set => SetProperty(ref _currentUser, value);
    }

    public MainViewModel(IServiceProvider sp)
    {
        _sp = sp;
        ShowLogin();
    }

    private void ShowLogin()
    {
        var login = ActivatorUtilities.CreateInstance<LoginViewModel>(_sp);
        login.LoginSucceeded += OnLoginSuccess;   // <-- подписка на событие
        CurrentView = login;
    }

    private void OnLoginSuccess(User user)
    {
        CurrentUser = user;
        var shell = ActivatorUtilities.CreateInstance<ShellViewModel>(_sp, user, this);
        CurrentView = shell;
    }

    public void OnLogout()
    {
        CurrentUser = null;
        ShowLogin();
    }
}