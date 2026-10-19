using System;
using System.Collections.Generic;

namespace YchebPrivetPiter.Data;

public partial class WorkOrderService
{
    public int Id { get; set; }

    public int WorkOrderId { get; set; }

    public int ServiceId { get; set; }

    public int? MechanicId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Mechanic? Mechanic { get; set; }

    public virtual Service Service { get; set; } = null!;

    public virtual WorkOrder WorkOrder { get; set; } = null!;
}
