using System;
using System.Collections.Generic;

namespace YchebPrivetPiter.Data;

public partial class WorkOrderPart
{
    public int Id { get; set; }

    public int WorkOrderId { get; set; }

    public int PartId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Part Part { get; set; } = null!;

    public virtual WorkOrder WorkOrder { get; set; } = null!;
}
