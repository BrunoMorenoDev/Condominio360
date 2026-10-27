namespace Condominio360.Models;

/// <summary>Área comunal reservable (salón, BBQ, gimnasio...). Se administra en una entrega posterior.</summary>
public class AreaComunal
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Capacidad { get; set; }
    public TimeOnly HoraApertura { get; set; }
    public TimeOnly HoraCierre { get; set; }
    public decimal CostoUso { get; set; }
    public bool Activa { get; set; } = true;

    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}

/// <summary>Reserva de un área comunal hecha por un residente.</summary>
public class Reserva
{
    public int Id { get; set; }

    public int AreaId { get; set; }
    public AreaComunal? Area { get; set; }

    public int ResidenteId { get; set; }
    public Residente? Residente { get; set; }

    public DateTime Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public EstadoReserva Estado { get; set; } = EstadoReserva.Solicitada;

    public int? AprobadaPor { get; set; }
    public Usuario? AprobadaPorUsuario { get; set; }
}

/// <summary>Visita autorizada por un residente y controlada en garita.</summary>
public class Visita
{
    public int Id { get; set; }

    public int UnidadId { get; set; }
    public Unidad? Unidad { get; set; }

    public int AutorizadaPor { get; set; }
    public Residente? AutorizadaPorResidente { get; set; }

    public int? RegistradaPor { get; set; }
    public Usuario? RegistradaPorUsuario { get; set; }

    public string NombreVisitante { get; set; } = string.Empty;
    public string CedulaVisitante { get; set; } = string.Empty;
    public string? PlacaVehiculo { get; set; }
    public DateTime FechaEsperada { get; set; }
    public DateTime? HoraIngreso { get; set; }
    public DateTime? HoraSalida { get; set; }
    public EstadoVisita Estado { get; set; } = EstadoVisita.Autorizada;
}
