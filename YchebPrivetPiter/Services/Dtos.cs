using System;

namespace YchebPrivetPiter.Services;

public class TopServiceDto
{
    public int ServiceId { get; set; }
    public string Name { get; set; } = null!;
    public int TimesUsed { get; set; }
    public decimal Revenue { get; set; }
}

public class MechanicWorkloadDto
{
    public int MechanicId { get; set; }
    public string MechanicName { get; set; } = null!;
    public int OrdersCount { get; set; }
    public int CompletedCount { get; set; }
}

public class RepeatClientDto
{
    public int ClientId { get; set; }
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public int Visits { get; set; }
}

public class RevenueRollupDto
{
    public int? Year { get; set; }
    public int? Month { get; set; }
    public decimal Revenue { get; set; }
}

public class OrdersCubeDto
{
    public int? MechanicId { get; set; }
    public string? MechanicName { get; set; }
    public string? Status { get; set; }
    public int Count { get; set; }
}
public class DailyRevenueDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int Count { get; set; }
}
public class ChartBarItem
{
    public string Label { get; set; } = "";
    public double Value { get; set; }
    public double HeightPercent { get; set; }
    public string Color { get; set; } = "#FF7C4DFF";
}