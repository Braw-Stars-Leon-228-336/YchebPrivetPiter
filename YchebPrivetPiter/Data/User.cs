using System;
using System.Collections.Generic;

namespace YchebPrivetPiter.Data;

public partial class User
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

    public int? DepartmentId { get; set; }

    public string Login { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual Department? Department { get; set; }

    public virtual Mechanic? Mechanic { get; set; }

    public virtual Role Role { get; set; } = null!;
}
