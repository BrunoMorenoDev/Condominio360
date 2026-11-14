using System.ComponentModel.DataAnnotations;
using Condominio360.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Condominio360.ViewModels;

// =====================================================================
// Panel del residente
// =====================================================================

public class PanelResidenteViewModel
{
    public string NombreUsuario { get; set; } = string.Empty;
    public Residente Residente { get; set; } = null!;
    public EstadoCuentaViewModel EstadoCuenta { get; set; } = null!;
    public bool PuedeReservar { get; set; }
    public int PagosPorVerificar { get; set; }
    public List<Reserva> ProximasReservas { get; set; } = new();
    public List<Visita> ProximasVisitas { get; set; } = new();
}

// =====================================================================
// Reporte de pago (residente)
// =====================================================================

/// <summary>Pago reportado por un residente: queda "Por verificar" hasta que la administración lo revise.</summary>
public class ReportarPagoViewModel
{
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

    [Required(ErrorMessage = "Ingrese el número de la transferencia o del depósito.")]
    [StringLength(40, ErrorMessage = "Máximo 40 caracteres.")]
    [Display(Name = "Referencia bancaria")]
    public string? Referencia { get; set; }

    [Display(Name = "Comprobante (JPG, PNG o PDF, máx. 2 MB)")]
    public IFormFile? Archivo { get; set; }

    // Información de apoyo (no se envía en el POST)
    public Unidad? Unidad { get; set; }
    public decimal SaldoTotal { get; set; }
    public decimal PorVerificar { get; set; }
    public decimal Disponible => Math.Max(0, SaldoTotal - PorVerificar);
}

// =====================================================================
// Áreas comunales y reservas
// =====================================================================

/// <summary>Formulario de área comunal. Los horarios se manejan como TimeOnly (columna TIME).</summary>
public class AreaComunalFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(60, ErrorMessage = "Máximo 60 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "Máximo 250 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Range(1, 500, ErrorMessage = "La capacidad debe estar entre 1 y 500 personas.")]
    [Display(Name = "Capacidad (personas)")]
    public int Capacidad { get; set; } = 10;

    [Required(ErrorMessage = "Indique la hora de apertura.")]
    [DataType(DataType.Time)]
    [Display(Name = "Hora de apertura")]
    public TimeOnly HoraApertura { get; set; } = new(8, 0);

    [Required(ErrorMessage = "Indique la hora de cierre.")]
    [DataType(DataType.Time)]
    [Display(Name = "Hora de cierre")]
    public TimeOnly HoraCierre { get; set; } = new(22, 0);

    [Range(typeof(decimal), "0", "1000", ErrorMessage = "El costo debe estar entre 0 y 1000 USD.")]
    [Display(Name = "Costo de uso (USD)")]
    public decimal CostoUso { get; set; }

    [Display(Name = "Área activa (se puede reservar)")]
    public bool Activa { get; set; } = true;
}

public class SolicitarReservaViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el área comunal.")]
    [Display(Name = "Área comunal")]
    public int AreaId { get; set; }

    [Required(ErrorMessage = "Indique la fecha.")]
    [DataType(DataType.Date)]
    public DateTime Fecha { get; set; } = DateTime.Today.AddDays(1);

    [Required(ErrorMessage = "Indique la hora de inicio.")]
    [DataType(DataType.Time)]
    [Display(Name = "Hora de inicio")]
    public TimeOnly HoraInicio { get; set; } = new(10, 0);

    [Required(ErrorMessage = "Indique la hora de fin.")]
    [DataType(DataType.Time)]
    [Display(Name = "Hora de fin")]
    public TimeOnly HoraFin { get; set; } = new(12, 0);

    // Apoyo para la vista
    public IEnumerable<SelectListItem> Areas { get; set; } = Enumerable.Empty<SelectListItem>();
    public List<AreaComunal> DetalleAreas { get; set; } = new();
    public ClasificacionMorosidad Clasificacion { get; set; }
    public bool PuedeReservar { get; set; } = true;
}

public class ReservasIndexViewModel
{
    public EstadoReserva? Estado { get; set; }
    public int? AreaId { get; set; }
    public DateTime? Desde { get; set; }
    public List<Reserva> Reservas { get; set; } = new();
    public List<AreaComunal> Areas { get; set; } = new();
    public int PorAprobar { get; set; }
    /// <summary>Clasificación de morosidad actual de cada unidad con reservas (para decidir la aprobación).</summary>
    public Dictionary<int, ClasificacionMorosidad> ClasificacionPorUnidad { get; set; } = new();
}

// =====================================================================
// Visitas
// =====================================================================

public class AutorizarVisitaViewModel
{
    [Required(ErrorMessage = "Ingrese el nombre del visitante.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Entre 3 y 100 caracteres.")]
    [Display(Name = "Nombre del visitante")]
    public string NombreVisitante { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese la cédula del visitante.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "La cédula debe tener 10 dígitos.")]
    [Display(Name = "Cédula del visitante")]
    public string CedulaVisitante { get; set; } = string.Empty;

    [StringLength(10, ErrorMessage = "Máximo 10 caracteres.")]
    [Display(Name = "Placa del vehículo (opcional)")]
    public string? PlacaVehiculo { get; set; }

    [Required(ErrorMessage = "Indique la fecha esperada.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha esperada")]
    public DateTime FechaEsperada { get; set; } = DateTime.Today;
}

public class GaritaViewModel
{
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string? Buscar { get; set; }
    public List<Visita> Visitas { get; set; } = new();
    /// <summary>Visitas de días anteriores que ingresaron y aún no registran salida.</summary>
    public List<Visita> DentroDeDiasAnteriores { get; set; } = new();
    public bool EsHoy => Fecha.Date == DateTime.Today;
    public int Esperadas => Visitas.Count(v => v.Estado == EstadoVisita.Autorizada);
    public int Dentro => Visitas.Count(v => v.Estado == EstadoVisita.Ingresada) + DentroDeDiasAnteriores.Count;
    public int Finalizadas => Visitas.Count(v => v.Estado == EstadoVisita.Finalizada);
}

// =====================================================================
// Usuarios
// =====================================================================

public class UsuarioFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Entre 3 y 50 caracteres.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Use solo letras, números, punto, guion o guion bajo.")]
    [Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
    [StringLength(100)]
    public string Correo { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el rol.")]
    [Display(Name = "Rol")]
    public int RolId { get; set; }

    [Display(Name = "Usuario activo")]
    public bool Activo { get; set; } = true;

    // Vínculo con el residente (solo rol Residente): Torre → Unidad → Residente
    [Display(Name = "Torre / bloque")]
    public string? Torre { get; set; }

    [Display(Name = "Unidad")]
    public int? UnidadId { get; set; }

    [Display(Name = "Residente vinculado")]
    public int? ResidenteId { get; set; }

    [DataType(DataType.Password)]
    [StringLength(64, ErrorMessage = "Máximo 64 caracteres.")]
    [Display(Name = "Contraseña")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    public string? ConfirmarPassword { get; set; }

    public bool EsEdicion => Id > 0;

    public IEnumerable<SelectListItem> Roles { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Torres { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Unidades { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Residentes { get; set; } = Enumerable.Empty<SelectListItem>();
}

public class UsuariosIndexViewModel
{
    public string? Buscar { get; set; }
    public int? RolId { get; set; }
    public bool? Activo { get; set; }
    public List<Usuario> Usuarios { get; set; } = new();
    public List<Rol> Roles { get; set; } = new();
}
