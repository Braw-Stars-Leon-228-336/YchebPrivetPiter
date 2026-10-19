using System;
using System.Collections.Generic;
//using YchebPrivetPiter.Models;   // ← добавлено

namespace YchebPrivetPiter.Data;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int InvoiceId { get; set; }

    public DateTime PaidAt { get; set; }

    public decimal Amount { get; set; }

    // ⚠️ ДОБАВЛЕНО
    public PaymentMethod Method { get; set; }

    // ⚠️ ДОБАВЛЕНО
    public PaymentStatus Status { get; set; }

    public string? TransactionNumber { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;
}