using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;

namespace YchebPrivetPiter.ViewModels;

public class ClientEditViewModel : ViewModelBase
{
    private readonly ClientService _clients;
    private readonly int _clientId;   // 0 = новый

    public string Title => _clientId == 0 ? "Новый клиент" : "Редактирование клиента";

    private string _fullName = "";
    public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }

    private string _phone = "";
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }

    private string? _email;
    public string? Email { get => _email; set => SetProperty(ref _email, value); }

    private string? _address;
    public string? Address { get => _address; set => SetProperty(ref _address, value); }

    private DateTimeOffset? _birthDate;
    public DateTimeOffset? BirthDate { get => _birthDate; set => SetProperty(ref _birthDate, value); }

    private string? _notes;
    public string? Notes { get => _notes; set => SetProperty(ref _notes, value); }

    private string? _errorMessage;
    public string? ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

    public bool IsSaved { get; private set; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    /// <summary>Сигнал окну: пора закрываться.</summary>
    public event Action? CloseRequested;

    public ClientEditViewModel(ClientService clients, Client? existing = null)
    {
        _clients = clients;
        _clientId = existing?.ClientId ?? 0;

        if (existing is not null)
        {
            _fullName = existing.FullName;
            _phone = existing.Phone;
            _email = existing.Email;
            _address = existing.Address;
            _birthDate = existing.BirthDate.HasValue
                ? new DateTimeOffset(existing.BirthDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
                : null;
            _notes = existing.Notes;
        }

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke());
    }

    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(FullName))
        {
            ErrorMessage = "Укажите ФИО";
            return;
        }
        if (string.IsNullOrWhiteSpace(Phone))
        {
            ErrorMessage = "Укажите телефон";
            return;
        }

        IsBusy = true;
        try
        {
            var phoneExists = await _clients.ExistsByPhoneAsync(
                Phone.Trim(), _clientId == 0 ? null : _clientId);
            if (phoneExists)
            {
                ErrorMessage = "Клиент с таким телефоном уже есть в базе";
                return;
            }

            var entity = new Client
            {
                ClientId = _clientId,
                FullName = FullName.Trim(),
                Phone = Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
                BirthDate = BirthDate.HasValue
                    ? DateOnly.FromDateTime(BirthDate.Value.Date)
                    : null,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
            };

            if (_clientId == 0)
                await _clients.CreateAsync(entity);
            else
                await _clients.UpdateAsync(entity);

            IsSaved = true;
            CloseRequested?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}