using System;
using System.Collections.Generic;

namespace YchebPrivetPiter.Data;

public partial class InspectionResult
{
    public int InspectionId { get; set; }

    public int WorkOrderId { get; set; }

    public int? MechanicId { get; set; }

    public string? EngineCondition { get; set; }

    public string? BrakeCondition { get; set; }

    public string? SuspensionCondition { get; set; }

    public string? ElectricalCondition { get; set; }

    public string? Recommendations { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Mechanic? Mechanic { get; set; }

    public virtual WorkOrder WorkOrder { get; set; } = null!;
}
