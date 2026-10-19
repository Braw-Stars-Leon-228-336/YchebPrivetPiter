using System;
using System.Collections.Generic;
//using YchebPrivetPiter.Models;   // ← добавлено

namespace YchebPrivetPiter.Data;

public partial class WorkOrder
{
    public int WorkOrderId { get; set; }

    public int? AppointmentId { get; set; }

    public int ClientId { get; set; }

    public int CarId { get; set; }

    public int? MechanicId { get; set; }

    // ⚠️ ДОБАВЛЕНО
    public WorkOrderStatus Status { get; set; }

    public string? Description { get; set; }

    public DateTime OpenedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public virtual Appointment? Appointment { get; set; }

    public virtual Car Car { get; set; } = null!;

    public virtual Client Client { get; set; } = null!;

    public virtual ICollection<InspectionResult> InspectionResults { get; set; } = new List<InspectionResult>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual Mechanic? Mechanic { get; set; }

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<WorkOrderPart> WorkOrderParts { get; set; } = new List<WorkOrderPart>();

    public virtual ICollection<WorkOrderService> WorkOrderServices { get; set; } = new List<WorkOrderService>();
}