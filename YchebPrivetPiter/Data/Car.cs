using System;
using System.Collections.Generic;

namespace YchebPrivetPiter.Data;

public partial class Car
{
    public int CarId { get; set; }

    public int ClientId { get; set; }

    public int BrandId { get; set; }

    public int? CategoryId { get; set; }

    public string Model { get; set; } = null!;

    public short? ProductionYear { get; set; }

    public string Vin { get; set; } = null!;

    public string PlateNumber { get; set; } = null!;

    public int Mileage { get; set; }

    public string? Color { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual CarBrand Brand { get; set; } = null!;

    public virtual CarCategory? Category { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}
