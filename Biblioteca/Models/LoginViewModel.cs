using System.ComponentModel.DataAnnotations;

namespace Biblioteca.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Scrie numărul de permis.")]
    [Display(Name = "Număr de permis")]
    public string CardNumber { get; set; } = "";

    [Required(ErrorMessage = "Scrie parola.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parolă")]
    public string Password { get; set; } = "";
}
