using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Security;

[Table("Users", Schema = "dbo")]
public sealed class ShopUser
{
    [Key]
    public int UserId { get; set; }

    [Required, MaxLength(60)]
    public string UserName { get; set; } = "";

    [Required, MaxLength(512)]
    public string PasswordHash { get; set; } = "";

    [Required, MaxLength(20)]
    public string Role { get; set; } = "Cashier";

    public bool IsActive { get; set; } = true;
    public int FailedLoginCount { get; set; }
    public int AuthVersion { get; set; } = 1;
    public DateTime? LockedUntilUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
