namespace Condominio360.Models;

/// <summary>Parte de un pago aplicada a una cuota: primero al recargo y luego al capital.</summary>
public class AplicacionPago
{
    public int Id { get; set; }

    public int PagoId { get; set; }
    public Pago? Pago { get; set; }

    public int CuotaId { get; set; }
    public Cuota? Cuota { get; set; }

    public decimal MontoRecargo { get; set; }
    public decimal MontoCapital { get; set; }

    public decimal Total() => MontoRecargo + MontoCapital;
}
