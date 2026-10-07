using System.ComponentModel.DataAnnotations;

namespace Condominio360.Models;

/// <summary>
/// Usuario que puede iniciar sesión en el sistema (administración del condominio).
/// La contraseña NUNCA se guarda en texto plano: solo su hash (PBKDF2).
/// </summary>
public class Usuario
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Rol { get; set; } = "Administrador";
}
