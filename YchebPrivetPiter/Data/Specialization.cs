using System;
using System.Collections.Generic;

namespace YchebPrivetPiter.Data;

public partial class Specialization
{
    public int SpecializationId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Mechanic> Mechanics { get; set; } = new List<Mechanic>();
}
