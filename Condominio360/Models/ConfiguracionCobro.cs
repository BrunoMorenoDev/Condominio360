using System.ComponentModel.DataAnnotations;

namespace Condominio360.Models;

/// <summary>
/// Parámetros del cálculo de cobranza (Core):
///   D = día de vencimiento, G = días de gracia, t = tasa de recargo mensual.
/// Se guarda un historial: la configuración vigente a una fecha es la de
/// mayor <see cref="VigenteDesde"/> que no supera esa fecha.
/// </summary>
public class ConfiguracionCobro
{
    public int Id { get; set; }

    /// <summary>D: día del mes en que vence la cuota (1 a 28 para que exista en todos los meses).</summary>
    [Range(1, 28, ErrorMessage = "El día de vencimiento debe estar entre 1 y 28.")]
    [Display(Name = "Día de vencimiento (D)")]
    public int DiaVencimiento { get; set; } = 10;

    /// <summary>G: días adicionales sin recargo después del vencimiento.</summary>
    [Range(0, 30, ErrorMessage = "Los días de gracia deben estar entre 0 y 30.")]
    [Display(Name = "Días de gracia (G)")]
    public int DiasGracia { get; set; } = 5;

    /// <summary>t: tasa mensual expresada como fracción (0.0150 = 1,5 %).</summary>
    [Range(typeof(decimal), "0", "0.1", ErrorMessage = "La tasa debe estar entre 0 % y 10 % mensual.")]
    [Display(Name = "Tasa de recargo mensual (t)")]
    public decimal TasaRecargoMensual { get; set; } = 0.015m;

    [DataType(DataType.Date)]
    [Display(Name = "Vigente desde")]
    public DateTime VigenteDesde { get; set; } = DateTime.Today;

    [Display(Name = "Actualizado por")]
    public int ActualizadoPor { get; set; }
    public Usuario? ActualizadoPorUsuario { get; set; }

    /// <summary>Fecha límite sin recargo = Fecha de vencimiento + G días.</summary>
    public DateTime FechaLimite(DateTime vencimiento) => vencimiento.Date.AddDays(DiasGracia);

    /// <summary>Fecha de vencimiento de la cuota del periodo (día D del mes).</summary>
    public DateTime FechaVencimiento(int anio, int mes) => new(anio, mes, DiaVencimiento);
}
