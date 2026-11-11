using System.ComponentModel.DataAnnotations;

namespace IbdaaAtelier.Web.Models.Masters;

public sealed class ServiceCategoryForm
{
    public int ServiceCategoryId { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [Required, StringLength(100)]
    public string NameAr { get; set; } = "";
}

public sealed class AlterationServiceForm
{
    public int ServiceId { get; set; }

    [Range(1, int.MaxValue)]
    public int ServiceCategoryId { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = "";

    [Required, StringLength(120)]
    public string NameAr { get; set; } = "";

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(1000)]
    public string? DescriptionAr { get; set; }

    [Range(typeof(decimal), "0", "999999999999999.999")]
    public decimal Price { get; set; }

    [Range(1, 100000)]
    public int? EstimatedMinutes { get; set; }
}

public sealed class ServiceListPage
{
    public List<ServiceCategory> Categories { get; init; } = [];
    public List<AlterationService> Services { get; init; } = [];
    public string Search { get; init; } = "";
    public int Page { get; init; } = 1;
    public int TotalPages { get; init; } = 1;
}
