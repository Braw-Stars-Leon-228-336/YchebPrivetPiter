using System;
using System.Collections.Generic;

namespace YchebPrivetPiter.Data;

public partial class Supplier
{
    public int SupplierId { get; set; }

    public string Name { get; set; } = null!;

    public string? Inn { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public virtual ICollection<Part> Parts { get; set; } = new List<Part>();
}
