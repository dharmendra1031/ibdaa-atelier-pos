namespace IbdaaAtelier.Web.Models.Reports;

public sealed class SalesReportPage
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public string Period { get; init; } = "today";
    public int Orders { get; init; }
    public decimal Sales { get; init; }
    public decimal Collected { get; init; }
    public decimal Refunded { get; init; }
    public decimal Outstanding { get; init; }
    public List<MethodReportRow> Methods { get; init; } = [];
    public List<ItemReportRow> Items { get; init; } = [];
}

public sealed record MethodReportRow(string Method, decimal Received, decimal Refunded)
{
    public decimal Net => Received - Refunded;
}

public sealed record ItemReportRow(string Type, string Name, string NameAr,
    int Quantity, decimal Total);
