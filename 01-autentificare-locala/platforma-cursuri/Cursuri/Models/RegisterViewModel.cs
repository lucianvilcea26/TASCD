using System.ComponentModel.DataAnnotations;

namespace Cursuri.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Choose a username.")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "The username must have 3 to 30 characters.")]
    [RegularExpression("^[a-z0-9.-]+$", ErrorMessage = "Use only lowercase letters, digits, '.' and '-'.")]
    [Display(Name = "Username")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [StringLength(20)]
    [Display(Name = "Matricol (students)")]
    public string? Matricol { get; set; }

    [Required(ErrorMessage = "Choose a password.")]
    [StringLength(128, MinimumLength = 15, ErrorMessage = "The password must have at least 15 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";

    [Compare(nameof(Password), ErrorMessage = "The two passwords are not the same.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";
}
