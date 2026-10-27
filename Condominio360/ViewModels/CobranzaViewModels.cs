using System.ComponentModel.DataAnnotations;
using Condominio360.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Condominio360.ViewModels;

// =====================================================================
// Configuración de cobro
// =====================================================================

/// <summary>Formulario de parámetros de cobro. La tasa se ingresa en porcentaje (1.5 = 1,5 %).</summary>
public class ConfiguracionCobroFormViewModel
{
    [Range(1, 28, ErrorMessage = "El día de vencimiento debe estar entre 1 y 28.")]
    [Display(Name = "Día de vencimiento (D)")]
    public int DiaVencimiento { get; set; } = 10;

    [Range(0, 30, ErrorMessage = "Los días de gracia deben estar entre 0 y 30.")]
    [Display(Name = "Días de gracia (G)")]
    public int DiasGracia { get; set; } = 5;

    [Range(typeof(decimal), "0", "10", ErrorMessage = "La tasa debe estar entre 0 % y 10 % mensual.")]
    [Display(Name = "Tasa de recargo mensual (%) (t)")]
    public decimal TasaPorcentaje { get; set; } = 1.5m;

    [Required(ErrorMessage = "Indique desde cuándo rige la configuración.")]
    [DataType(DataType.Date)]
    [Display(Name = "Vigente desde")]
    public DateTime VigenteDesde { get; set; } = DateTime.Today;
}

public class ConfiguracionCobroIndexViewModel
{
    public ConfiguracionCobro? Vigente { get; set; }
    public List<ConfiguracionCobro> Historial { get; set; } = new();
}

// =====================================================================
// Cuotas
// =====================================================================

public class CuotaFila
{
    public Cuota Cuota { get; set; } = null!;
    public int DiasAtraso { get; set; }
    public int MesesAtraso { get; set; }
    public decimal Recargo { get; set; }
    public decimal Pagado => Cuota.CapitalPagado + Cuota.RecargoPagado;
    public decimal Saldo { get; set; }
    public EstadoCuota Estado { get; set; }
}

public class CuotasIndexViewModel
{
    public int? Anio { get; set; }
    public int? Mes { get; set; }
    public string? Torre { get; set; }
    public EstadoCuota? Estado { get; set; }
    public List<CuotaFila> Filas { get; set; } = new();
    public List<string> Torres { get; set; } = new();
    public List<int> Anios { get; set; } = new();
    public decimal TotalEmitido => Filas.Where(f => f.Estado != EstadoCuota.Anulada).Sum(f => f.Cuota.Monto);
    public decimal TotalSaldo => Filas.Sum(f => f.Saldo);
}

public class GenerarCuotasTipo
{
    public TipoUnidad Tipo { get; set; }
    public int Unidades { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>Resumen previo a la generación de cuotas de un periodo (wireframe "Generar cuotas").</summary>
public class GenerarCuotasViewModel
{
    [Range(1, 12, ErrorMessage = "Seleccione un mes válido.")]
    public int Mes { get; set; }

    [Range(2020, 2100, ErrorMessage = "Seleccione un año válido.")]
    [Display(Name = "Año")]
    public int Anio { get; set; }

    public ConfiguracionCobro? Configuracion { get; set; }
    public int UnidadesActivas { get; set; }
    public int YaGeneradas { get; set; }
    public int PorGenerar { get; set; }
    public decimal TotalAEmitir { get; set; }
    public List<GenerarCuotasTipo> PorTipo { get; set; } = new();
    public DateTime FechaEmision { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public DateTime FechaLimite { get; set; }
    public string? Error { get; set; }
}

// =====================================================================
// Pagos
// =====================================================================

/// <summary>Formulario de registro / edición de un pago (dropdowns en cascada Torre → Unidad → Residente).</summary>
public class PagoFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Seleccione la torre o bloque.")]
    [Display(Name = "Torre / bloque")]
    public string? Torre { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la unidad.")]
    [Display(Name = "Unidad")]
    public int UnidadId { get; set; }

    [Display(Name = "Pagado por (residente)")]
    public int? ResidenteId { get; set; }

    [Required(ErrorMessage = "Ingrese la fecha del pago.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de pago")]
    public DateTime FechaPago { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Ingrese el monto.")]
    [Range(typeof(decimal), "0.01", "100000", ErrorMessage = "El monto debe ser mayor que 0.")]
    [Display(Name = "Monto (USD)")]
    public decimal Monto { get; set; }

    [Required(ErrorMessage = "Seleccione el método de pago.")]
    [Display(Name = "Método")]
    public MetodoPago Metodo { get; set; } = MetodoPago.Transferencia;

    [StringLength(40, ErrorMessage = "Máximo 40 caracteres.")]
    [Display(Name = "Referencia bancaria")]
    public string? Referencia { get; set; }

    [Display(Name = "Comprobante (JPG, PNG o PDF, máx. 2 MB)")]
    public IFormFile? Archivo { get; set; }

    public string? ComprobanteActual { get; set; }

    // Listas de los dropdowns (no se envían en el POST)
    public IEnumerable<SelectListItem> Torres { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Unidades { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Residentes { get; set; } = Enumerable.Empty<SelectListItem>();
}

public class DistribucionLinea
{
    public Cuota Cuota { get; set; } = null!;
    public int DiasAtraso { get; set; }
    public decimal RecargoAplicado { get; set; }
    public decimal CapitalAplicado { get; set; }
    public decimal SaldoFinal { get; set; }
    public EstadoCuota NuevoEstado { get; set; }
}

public class ResultadoDistribucion
{
    public List<DistribucionLinea> Lineas { get; set; } = new();
    public decimal SaldoTotalUnidad { get; set; }
    public decimal Sobrante { get; set; }
    public decimal TotalRecargo => Lineas.Sum(l => l.RecargoAplicado);
    public decimal TotalCapital => Lineas.Sum(l => l.CapitalAplicado);
    public bool Excede => Sobrante > 0;
    public ClasificacionMorosidad ClasificacionAntes { get; set; }
    public ClasificacionMorosidad ClasificacionDespues { get; set; }
}

public class VerificarPagoViewModel
{
    public Pago Pago { get; set; } = null!;
    public ResultadoDistribucion? Distribucion { get; set; }
    public string? Error { get; set; }

    [StringLength(250, ErrorMessage = "Máximo 250 caracteres.")]
    [Display(Name = "Observación (obligatoria si se rechaza)")]
    public string? Observacion { get; set; }
}

public class PagosIndexViewModel
{
    public EstadoPago? Estado { get; set; }
    public string? Torre { get; set; }
    public List<Pago> Pagos { get; set; } = new();
    public List<string> Torres { get; set; } = new();
    public int PorVerificar { get; set; }
}

// =====================================================================
// Estado de cuenta y reporte de morosidad
// =====================================================================

public class EstadoCuentaViewModel
{
    public DateTime Corte { get; set; } = DateTime.Today;
    public string? Torre { get; set; }
    public int? UnidadId { get; set; }
    public IEnumerable<SelectListItem> Torres { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Unidades { get; set; } = Enumerable.Empty<SelectListItem>();

    public Unidad? Unidad { get; set; }
    public Residente? Responsable { get; set; }
    public ConfiguracionCobro? Configuracion { get; set; }
    public List<CuotaFila> Cuotas { get; set; } = new();
    public List<Pago> Pagos { get; set; } = new();

    public decimal SaldoTotal { get; set; }
    public decimal SaldoVencido { get; set; }
    public decimal RecargosAcumulados { get; set; }
    public int CuotasVencidas { get; set; }
    public int DiasMoraMasAntigua { get; set; }
    public ClasificacionMorosidad Clasificacion { get; set; }
}

public class MorosidadFila
{
    public Unidad Unidad { get; set; } = null!;
    public Residente? Responsable { get; set; }
    public int CuotasVencidas { get; set; }
    public int DiasMora { get; set; }
    public decimal Recargo { get; set; }
    public decimal SaldoVencido { get; set; }
    public decimal SaldoTotal { get; set; }
    public ClasificacionMorosidad Clasificacion { get; set; }
}

public class ReporteMorosidadViewModel
{
    public DateTime Corte { get; set; } = DateTime.Today;
    public string? Torre { get; set; }
    public ClasificacionMorosidad? Clasificacion { get; set; }
    public string? Buscar { get; set; }
    public List<string> Torres { get; set; } = new();
    public ConfiguracionCobro? Configuracion { get; set; }

    public List<MorosidadFila> Filas { get; set; } = new();
    public int TotalUnidadesActivas { get; set; }

    // Indicadores del condominio (sección 2.e)
    public decimal IndiceMorosidad { get; set; }
    public decimal CarteraVencida { get; set; }
    public decimal TasaRecaudacion { get; set; }
    public int UnidadesAlDia { get; set; }
    public int UnidadesMorosas { get; set; }
    public string PeriodoRecaudacion { get; set; } = string.Empty;
    public decimal CobradoPeriodo { get; set; }
    public decimal EmitidoPeriodo { get; set; }
}
