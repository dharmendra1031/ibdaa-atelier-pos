using System.ComponentModel.DataAnnotations;

namespace IbdaaAtelier.Web.Models.Masters;

public sealed class CustomerForm
{
    public long CustomerId { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = "";

    [Required, StringLength(120)]
    public string NameAr { get; set; } = "";

    [Required, StringLength(20)]
    public string Mobile { get; set; } = "";

    [StringLength(20)]
    public string? AlternateMobile { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public sealed class CustomerListPage
{
    public List<Customer> Customers { get; init; } = [];
    public int Page { get; init; } = 1;
    public int TotalPages { get; init; } = 1;
    public string Search { get; init; } = "";
}
