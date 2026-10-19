using System;
using System.Collections.Generic;

namespace YchebPrivetPiter.Data;

public partial class Review
{
    public int ReviewId { get; set; }

    public int ClientId { get; set; }

    public int WorkOrderId { get; set; }

    public short Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual WorkOrder WorkOrder { get; set; } = null!;
}
