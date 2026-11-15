using System.ComponentModel.DataAnnotations;

namespace Cursuri.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your username.")]
    [Display(Name = "Username")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}
