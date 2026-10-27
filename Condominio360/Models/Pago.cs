using System.ComponentModel.DataAnnotations;

namespace Condominio360.Models;

/// <summary>
/// Pago realizado por una unidad. Los pagos reportados por residentes quedan
/// "Por verificar"; al verificarse se distribuyen entre las cuotas con saldo.
/// </summary>
public class Pago
{
    public int Id { get; set; }

    [Display(Name = "Unidad")]
    public int UnidadId { get; set; }
    public Unidad? Unidad { get; set; }

    [Display(Name = "Pagado por")]
    public int? ResidenteId { get; set; }
    public Residente? Residente { get; set; }

    public int RegistradoPor { get; set; }
    public Usuario? RegistradoPorUsuario { get; set; }

    public int? VerificadoPor { get; set; }
    public Usuario? VerificadoPorUsuario { get; set; }

    [Display(Name = "Fecha de pago")]
    public DateTime FechaPago { get; set; }

    public decimal Monto { get; set; }

    [Display(Name = "Método")]
    public MetodoPago Metodo { get; set; }

    [Display(Name = "Referencia")]
    public string? Referencia { get; set; }

    [Display(Name = "Comprobante")]
    public string? Comprobante { get; set; }

    public EstadoPago Estado { get; set; } = EstadoPago.PorVerificar;

    [Display(Name = "Fecha de verificación")]
    public DateTime? FechaVerificacion { get; set; }

    [Display(Name = "Observación")]
    public string? Observacion { get; set; }

    public ICollection<AplicacionPago> Aplicaciones { get; set; } = new List<AplicacionPago>();
}
