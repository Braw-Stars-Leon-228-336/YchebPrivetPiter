using System;
using System.Collections.Generic;

namespace YchebPrivetPiter.Data;

public partial class Mechanic
{
    public int MechanicId { get; set; }

    public int UserId { get; set; }

    public int? SpecializationId { get; set; }

    public DateOnly? HireDate { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<InspectionResult> InspectionResults { get; set; } = new List<InspectionResult>();

    public virtual ICollection<MechanicSchedule> MechanicSchedules { get; set; } = new List<MechanicSchedule>();

    public virtual Specialization? Specialization { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual ICollection<WorkOrderService> WorkOrderServices { get; set; } = new List<WorkOrderService>();

    public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}
