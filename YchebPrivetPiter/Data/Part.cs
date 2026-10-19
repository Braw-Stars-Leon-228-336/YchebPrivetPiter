using System;
using System.Collections.Generic;

namespace YchebPrivetPiter.Data;

public partial class Part
{
    public int PartId { get; set; }

    public int SupplierId { get; set; }

    public string Article { get; set; } = null!;

    public string Name { get; set; } = null!;

    public decimal PurchasePrice { get; set; }

    public decimal SalePrice { get; set; }

    public int StockQuantity { get; set; }

    public int MinStock { get; set; }

    public bool IsActive { get; set; }

    public virtual Supplier Supplier { get; set; } = null!;

    public virtual ICollection<WorkOrderPart> WorkOrderParts { get; set; } = new List<WorkOrderPart>();
}
