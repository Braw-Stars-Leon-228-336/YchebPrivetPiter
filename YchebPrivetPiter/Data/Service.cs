using System;
using System.Collections.Generic;

namespace YchebPrivetPiter.Data;

public partial class Service
{
    public int ServiceId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal BasePrice { get; set; }

    public int DurationMinutes { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<WorkOrderService> WorkOrderServices { get; set; } = new List<WorkOrderService>();

    public virtual ICollection<Mechanic> Mechanics { get; set; } = new List<Mechanic>();
}
