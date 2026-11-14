using System.ComponentModel.DataAnnotations;

namespace Biblioteca.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Scrie numărul matricol.")]
    [Display(Name = "Număr matricol")]
    public string Matricol { get; set; } = "";

    [Required(ErrorMessage = "Scrie adresa de email.")]
    [EmailAddress(ErrorMessage = "Adresa de email nu este validă.")]
    [Display(Name = "Adresa de email din dosarul de la secretariat")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Alege o parolă.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parolă")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Scrie parola încă o dată.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Cele două parole nu coincid.")]
    [Display(Name = "Parola, încă o dată")]
    public string ConfirmPassword { get; set; } = "";
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Scrie adresa de email.")]
    [EmailAddress(ErrorMessage = "Adresa de email nu este validă.")]
    [Display(Name = "Adresa de email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Scrie parola.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parolă")]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class EmailViewModel
{
    [Required(ErrorMessage = "Scrie adresa de email.")]
    [EmailAddress(ErrorMessage = "Adresa de email nu este validă.")]
    [Display(Name = "Adresa de email")]
    public string Email { get; set; } = "";
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Scrie parola curentă.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parola curentă")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Alege parola nouă.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parola nouă")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Scrie parola nouă încă o dată.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Cele două parole nu coincid.")]
    [Display(Name = "Parola nouă, încă o dată")]
    public string ConfirmPassword { get; set; } = "";
}

public class ResetPasswordViewModel
{
    [Required] public string UserId { get; set; } = "";
    [Required] public string Token { get; set; } = "";

    [Required(ErrorMessage = "Alege parola nouă.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parola nouă")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Scrie parola nouă încă o dată.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Cele două parole nu coincid.")]
    [Display(Name = "Parola nouă, încă o dată")]
    public string ConfirmPassword { get; set; } = "";
}

// O pagină simplă cu un titlu și un mesaj, după fiecare pas care trimite un email.
public record MessageViewModel(string Title, string Text, string? LinkText = null, string? LinkAction = null);
