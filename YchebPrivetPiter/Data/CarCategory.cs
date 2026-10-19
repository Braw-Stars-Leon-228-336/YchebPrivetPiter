using System;
using System.Collections.Generic;

namespace YchebPrivetPiter.Data;

public partial class CarCategory
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Car> Cars { get; set; } = new List<Car>();
}
