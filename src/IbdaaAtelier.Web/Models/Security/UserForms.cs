using System.ComponentModel.DataAnnotations;

namespace IbdaaAtelier.Web.Models.Security;

public sealed class UserCreateForm
{
    [Required, StringLength(60, MinimumLength = 3)]
    public string UserName { get; set; } = "";

    [Required, StringLength(128, MinimumLength = 12)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required, Compare(nameof(Password))]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";

    [Required] public string Role { get; set; } = "Cashier";
}

public sealed class UserEditForm
{
    [Range(1, int.MaxValue)] public int UserId { get; set; }
    [Required] public string Role { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class UserPasswordForm
{
    [Range(1, int.MaxValue)] public int UserId { get; set; }
    [Required, StringLength(128, MinimumLength = 12)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
    [Required, Compare(nameof(Password))]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}
