using System;
using System.Collections.Generic;

namespace YchebPrivetPiter.Data;

public partial class Role
{
    public int RoleId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
