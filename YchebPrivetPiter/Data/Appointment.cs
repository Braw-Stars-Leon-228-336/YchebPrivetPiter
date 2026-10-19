using System;
using System.Collections.Generic;
/*using YchebPrivetPiter.Models; */  // ← добавлено

namespace YchebPrivetPiter.Data;

public partial class Appointment
{
    public int AppointmentId { get; set; }

    public int ClientId { get; set; }

    public int CarId { get; set; }

    public int ServiceId { get; set; }

    public int? MechanicId { get; set; }

    public int? RepairPlaceId { get; set; }

    public DateTime ScheduledAt { get; set; }

    // ⚠️ ДОБАВЛЕНО — enum вместо string
    public AppointmentStatus Status { get; set; }

    public string? Comment { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Car Car { get; set; } = null!;

    public virtual Client Client { get; set; } = null!;

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Mechanic? Mechanic { get; set; }

    public virtual RepairPlace? RepairPlace { get; set; }

    public virtual Service Service { get; set; } = null!;

    public virtual WorkOrder? WorkOrder { get; set; }
}