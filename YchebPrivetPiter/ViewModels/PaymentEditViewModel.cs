using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;

namespace YchebPrivetPiter.ViewModels;

public class PaymentEditViewModel : ViewModelBase
{
    private readonly PaymentService _payments;
    private readonly Invoice _invoice;

    public string Title => $"Платёж по счёту {_invoice.Number}";

    // ---------- Информация о счёте (только для чтения) ----------
    public string InvoiceNumber => _invoice.Number;
    public string ClientName => _invoice.Client?.FullName ?? "—";
    public decimal TotalAmount => _invoice.TotalAmount ?? 0m;
    public decimal PaidAmount => _paidAmount;
    public decimal Remaining => Math.Max(0m, TotalAmount - PaidAmount);

    private decimal _paidAmount;
    public decimal KaidAmount
    {
        get => _paidAmount;
        set
        {
            if (SetProperty(ref _paidAmount, value))
            {
                OnPropertyChanged(nameof(Remaining));
            }
        }
    }

    // ---------- Поля платежа ----------
    private decimal _amount;
    public decimal Amount
    {
        get => _amount;
        set => SetProperty(ref _amount, value);
    }

    private PaymentMethod _method = PaymentMethod.card;
    public PaymentMethod Method
    {
        get => _method;
        set => SetProperty(ref _method, value);
    }

    private string? _transactionNumber;
    public string? TransactionNumber
    {
        get => _transactionNumber;
        set => SetProperty(ref _transactionNumber, value);
    }

    public Array AllMethods { get; } = Enum.GetValues(typeof(PaymentMethod));

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public bool IsSaved { get; private set; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public event Action? CloseRequested;

    public PaymentEditViewModel(PaymentService payments, Invoice invoice)
    {
        _payments = payments;
        _invoice = invoice;

        // Считаем уже оплаченную сумму (если Client.Payments не подгружены — оставим 0)
        _paidAmount = 0m;

        // По умолчанию подставляем остаток к оплате
        Amount = Math.Max(0m, TotalAmount - PaidAmount);

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke());

        _ = LoadPaidAsync();
    }

    private async Task LoadPaidAsync()
    {
        IsBusy = true;
        try
        {
            var existing = await _payments.GetByInvoiceAsync(_invoice.InvoiceId);
            decimal paid = 0m;
            foreach (var p in existing)
            {
                if (p.Status == PaymentStatus.completed) paid += p.Amount;
            }
            _paidAmount = paid;
            OnPropertyChanged(nameof(PaidAmount));
            OnPropertyChanged(nameof(Remaining));

            // Пересчитаем сумму по умолчанию
            Amount = Math.Max(0m, TotalAmount - PaidAmount);
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

    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (Amount <= 0)
        {
            ErrorMessage = "Сумма должна быть больше нуля";
            return;
        }

        if (Amount > Remaining && Remaining > 0)
        {
            ErrorMessage = $"Сумма превышает остаток к оплате ({Remaining:N2} ₽)";
            return;
        }

        IsBusy = true;
        try
        {
            await _payments.RegisterAsync(
                _invoice.InvoiceId,
                Amount,
                Method,
                string.IsNullOrWhiteSpace(TransactionNumber) ? null : TransactionNumber.Trim(),
                PaymentStatus.completed);

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