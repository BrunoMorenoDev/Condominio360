using System.ComponentModel.DataAnnotations;

namespace Condominio360.Models;

public enum TipoUnidad
{
    Departamento,
    Casa,
    Local,
    Oficina
}

/// <summary>
/// Unidad habitacional o comercial del condominio (departamento, casa, local...).
/// </summary>
public class Unidad
{
    public int Id { get; set; }

    [Required(ErrorMessage = "La torre o bloque es obligatorio.")]
    [StringLength(20, ErrorMessage = "Máximo 20 caracteres.")]
    [Display(Name = "Torre / bloque")]
    public string Torre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de unidad es obligatorio.")]
    [StringLength(10, ErrorMessage = "Máximo 10 caracteres.")]
    [Display(Name = "Número")]
    public string Numero { get; set; } = string.Empty;

    [Range(0, 60, ErrorMessage = "El piso debe estar entre 0 y 60.")]
    public int Piso { get; set; }

    [Required(ErrorMessage = "Seleccione el tipo de unidad.")]
    [Display(Name = "Tipo")]
    public TipoUnidad Tipo { get; set; } = TipoUnidad.Departamento;

    [Range(typeof(decimal), "10", "2000", ErrorMessage = "El área debe estar entre 10 y 2000 m².")]
    [Display(Name = "Área (m²)")]
    public decimal AreaM2 { get; set; }

    [Range(typeof(decimal), "0", "5000", ErrorMessage = "La alícuota debe estar entre 0 y 5000 USD.")]
    [DataType(DataType.Currency)]
    [Display(Name = "Alícuota mensual (USD)")]
    public decimal Alicuota { get; set; }

    public ICollection<Residente> Residentes { get; set; } = new List<Residente>();

    public string Codigo => $"{Torre}-{Numero}";
}
