using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Condominio360.Models;

public enum TipoResidente
{
    Propietario,
    Arrendatario
}

/// <summary>
/// Persona que vive o es dueña de una unidad del condominio.
/// </summary>
public class Residente
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Los nombres son obligatorios.")]
    [StringLength(60, ErrorMessage = "Máximo 60 caracteres.")]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [StringLength(60, ErrorMessage = "Máximo 60 caracteres.")]
    public string Apellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "La cédula es obligatoria.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "La cédula debe tener 10 dígitos.")]
    [Display(Name = "Cédula")]
    public string Cedula { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
    [StringLength(100)]
    public string Correo { get; set; } = string.Empty;

    [RegularExpression(@"^\+?\d{7,15}$", ErrorMessage = "Ingrese solo dígitos (7 a 15), opcionalmente con +.")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "Seleccione el tipo de residente.")]
    [Display(Name = "Tipo")]
    public TipoResidente Tipo { get; set; } = TipoResidente.Propietario;

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de ingreso")]
    public DateTime FechaIngreso { get; set; } = DateTime.Today;

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una unidad.")]
    [Display(Name = "Unidad")]
    public int UnidadId { get; set; }

    public Unidad? Unidad { get; set; }

    [NotMapped]
    public string NombreCompleto => $"{Nombres} {Apellidos}";
}
