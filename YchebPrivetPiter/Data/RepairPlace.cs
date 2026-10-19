using System;
using System.Collections.Generic;

namespace YchebPrivetPiter.Data;

public partial class RepairPlace
{
    public int PlaceId { get; set; }

    public string Name { get; set; } = null!;

    // ⚠️ ДОБАВЛЕНО
    public PlaceKind Kind { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<MechanicSchedule> MechanicSchedules { get; set; } = new List<MechanicSchedule>();
}