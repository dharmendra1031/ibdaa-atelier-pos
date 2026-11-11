using System.ComponentModel.DataAnnotations;

namespace IbdaaAtelier.Web.Models.Security;

public sealed class LoginForm
{
    [Required, StringLength(60)]
    public string UserName { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}
