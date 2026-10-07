using System.ComponentModel.DataAnnotations;

namespace Condominio360.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingrese su usuario.")]
    [Display(Name = "Usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese su contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Mantener la sesión iniciada")]
    public bool Recordarme { get; set; }

    /// <summary>URL protegida a la que el usuario intentó entrar antes de iniciar sesión.</summary>
    public string? ReturnUrl { get; set; }
}
