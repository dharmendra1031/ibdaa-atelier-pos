using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Masters;

[Table("ShopSettings", Schema = "dbo")]
public sealed class ShopSettings
{
    [Key]
    public int SettingsId { get; set; }

    [Required, MaxLength(150)]
    public string BusinessName { get; set; } = "";

    [Required, MaxLength(150)]
    public string BusinessNameAr { get; set; } = "";

    [MaxLength(25)]
    public string? Mobile { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(500)]
    public string? AddressAr { get; set; }

    [MaxLength(260)]
    public string? LogoPath { get; set; }
    public bool RequireRegisterOpen { get; set; }
}

public sealed class ShopSettingsForm
{
    [Required, StringLength(150)]
    public string BusinessName { get; set; } = "";

    [Required, StringLength(150)]
    public string BusinessNameAr { get; set; } = "";

    [StringLength(25)]
    public string? Mobile { get; set; }

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(500)]
    public string? AddressAr { get; set; }

    public string? LogoPath { get; set; }
}
