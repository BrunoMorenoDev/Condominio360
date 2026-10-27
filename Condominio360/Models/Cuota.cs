using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Condominio360.Helpers;

namespace Condominio360.Models;

/// <summary>
/// Cuota mensual emitida a una unidad. Su monto es la alícuota de la unidad.
/// Contiene las reglas de cálculo del Core (sección 2.b del documento de especificación):
///   Fecha límite      = Fecha de vencimiento + G días
///   Días de atraso    = máx(0, F − Fecha límite)
///   Meses de atraso   = ⌈ Días de atraso / 30 ⌉
///   Capital pendiente = Monto − Capital pagado
///   Recargo           = Capital pendiente × t × Meses de atraso   (2 decimales)
///   Saldo             = Capital pendiente + máx(0, Recargo − Recargo pagado)
/// </summary>
public class Cuota
{
    public int Id { get; set; }

    public int UnidadId { get; set; }
    public Unidad? Unidad { get; set; }

    [Display(Name = "Año")]
    public int Anio { get; set; }

    public int Mes { get; set; }

    public decimal Monto { get; set; }

    [Display(Name = "Capital pagado")]
    public decimal CapitalPagado { get; set; }

    [Display(Name = "Recargo pagado")]
    public decimal RecargoPagado { get; set; }

    [Display(Name = "Fecha de emisión")]
    public DateTime FechaEmision { get; set; }

    [Display(Name = "Fecha de vencimiento")]
    public DateTime FechaVencimiento { get; set; }

    public EstadoCuota Estado { get; set; } = EstadoCuota.Pendiente;

    public int GeneradaPor { get; set; }
    public Usuario? GeneradaPorUsuario { get; set; }

    public ICollection<AplicacionPago> Aplicaciones { get; set; } = new List<AplicacionPago>();

    [NotMapped]
    public string Periodo => Formato.Periodo(Anio, Mes);

    // ------------------------------------------------------------------
    // Reglas de cálculo (no dependen de la base de datos)
    // ------------------------------------------------------------------

    public decimal CapitalPendiente() => Estado == EstadoCuota.Anulada ? 0m : Monto - CapitalPagado;

    public int DiasAtraso(DateTime corte, ConfiguracionCobro cfg)
    {
        if (Estado == EstadoCuota.Anulada) return 0;
        var dias = (corte.Date - cfg.FechaLimite(FechaVencimiento)).Days;
        return Math.Max(0, dias);
    }

    public int MesesAtraso(DateTime corte, ConfiguracionCobro cfg) =>
        (int)Math.Ceiling(DiasAtraso(corte, cfg) / 30m);

    /// <summary>Recargo total generado a la fecha de corte sobre el capital que aún no se paga.</summary>
    public decimal Recargo(DateTime corte, ConfiguracionCobro cfg) =>
        Math.Round(CapitalPendiente() * cfg.TasaRecargoMensual * MesesAtraso(corte, cfg), 2,
                   MidpointRounding.AwayFromZero);

    public decimal RecargoPendiente(DateTime corte, ConfiguracionCobro cfg) =>
        Math.Max(0m, Recargo(corte, cfg) - RecargoPagado);

    public decimal Saldo(DateTime corte, ConfiguracionCobro cfg) =>
        Estado == EstadoCuota.Anulada ? 0m : CapitalPendiente() + RecargoPendiente(corte, cfg);

    /// <summary>Vencida = tiene saldo y días de atraso a la fecha de corte.</summary>
    public bool EstaVencida(DateTime corte, ConfiguracionCobro cfg) =>
        Estado != EstadoCuota.Anulada && Saldo(corte, cfg) > 0 && DiasAtraso(corte, cfg) > 0;

    /// <summary>
    /// Estado a la fecha de corte: Pagada si el saldo es 0; Vencida si tiene días de atraso;
    /// Parcial si recibió abonos y sigue en plazo; Pendiente en otro caso.
    /// </summary>
    public EstadoCuota CalcularEstado(DateTime corte, ConfiguracionCobro cfg)
    {
        if (Estado == EstadoCuota.Anulada) return EstadoCuota.Anulada;
        if (Saldo(corte, cfg) == 0) return EstadoCuota.Pagada;
        if (DiasAtraso(corte, cfg) > 0) return EstadoCuota.Vencida;
        if (CapitalPagado > 0 || RecargoPagado > 0) return EstadoCuota.Parcial;
        return EstadoCuota.Pendiente;
    }

    public void ActualizarEstado(DateTime corte, ConfiguracionCobro cfg) => Estado = CalcularEstado(corte, cfg);

    /// <summary>Copia en memoria para simular un pago sin modificar la cuota real.</summary>
    public Cuota Clonar() => (Cuota)MemberwiseClone();
}
