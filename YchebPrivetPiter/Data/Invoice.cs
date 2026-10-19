using System;
using System.Collections.Generic;
/*using YchebPrivetPiter.Models;  */ // ← добавлено

namespace YchebPrivetPiter.Data;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public int WorkOrderId { get; set; }

    public int ClientId { get; set; }

    public string Number { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateOnly? DueDate { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxRate { get; set; }

    public decimal? TotalAmount { get; set; }

    // ⚠️ ДОБАВЛЕНО
    public InvoiceStatus Status { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual WorkOrder WorkOrder { get; set; } = null!;
}