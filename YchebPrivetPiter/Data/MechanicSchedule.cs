using System;
using System.Collections.Generic;
//using YchebPrivetPiter.Models;   // ← добавлено

namespace YchebPrivetPiter.Data;

public partial class MechanicSchedule
{
    public int ScheduleId { get; set; }

    public int MechanicId { get; set; }

    public int? RepairPlaceId { get; set; }

    public DateOnly WorkDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    // ⚠️ ДОБАВЛЕНО
    public ScheduleStatus Status { get; set; }

    public virtual Mechanic Mechanic { get; set; } = null!;

    public virtual RepairPlace? RepairPlace { get; set; }
}