using System;
using System.Collections.Generic;

namespace YchebPrivetPiter.Data;

public partial class CarBrand
{
    public int BrandId { get; set; }

    public string Name { get; set; } = null!;

    public string? Country { get; set; }

    public virtual ICollection<Car> Cars { get; set; } = new List<Car>();
}
