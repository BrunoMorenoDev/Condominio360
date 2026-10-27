using System.ComponentModel.DataAnnotations;

namespace Condominio360.Models;

/// <summary>Rol de un usuario del sistema: Administrador, Residente o Guardia.</summary>
public class Rol
{
    public const string Administrador = "Administrador";
    public const string Residente = "Residente";
    public const string Guardia = "Guardia";

    public int Id { get; set; }

    [Required, StringLength(20)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Descripcion { get; set; }

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
