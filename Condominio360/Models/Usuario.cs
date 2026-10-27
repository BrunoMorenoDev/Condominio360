using System.ComponentModel.DataAnnotations;

namespace Condominio360.Models;

/// <summary>
/// Usuario que puede iniciar sesión en el sistema.
/// La contraseña NUNCA se guarda en texto plano: solo su hash (PBKDF2).
/// Un usuario con rol Residente se vincula al registro del residente correspondiente.
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

    [Required, StringLength(100), EmailAddress]
    public string Correo { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public DateTime CreadoEn { get; set; } = DateTime.Now;

    public int RolId { get; set; }
    public Rol? Rol { get; set; }

    public int? ResidenteId { get; set; }
    public Residente? Residente { get; set; }

    public bool TieneRol(string nombre) => Rol?.Nombre == nombre;
}
