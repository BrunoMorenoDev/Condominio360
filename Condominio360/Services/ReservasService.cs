using Condominio360.Data;
using Condominio360.Helpers;
using Condominio360.Models;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Services;

/// <summary>
/// Reglas de las reservas de áreas comunales. Se validan en el servidor al solicitar y otra vez al aprobar:
/// horario dentro del horario del área, sin cruces con otras reservas y la regla d.2 del Core
/// (las unidades Moroso o Moroso crítico no pueden reservar hasta regularizar su deuda).
/// </summary>
public class ReservasService
{
    public const int DiasMaximoAnticipacion = 60;
    public const int HorasMaximasPorReserva = 8;

    private readonly AppDbContext _db;
    private readonly CobranzaService _cobranza;

    public ReservasService(AppDbContext db, CobranzaService cobranza)
    {
        _db = db;
        _cobranza = cobranza;
    }

    /// <summary>Reservas que ocupan el área en una fecha (solicitadas y aprobadas).</summary>
    public Task<List<Reserva>> OcupacionAsync(int areaId, DateTime fecha, int excluirReservaId = 0) =>
        _db.Reservas.AsNoTracking()
            .Where(r => r.AreaId == areaId && r.Fecha == fecha.Date && r.Id != excluirReservaId &&
                        (r.Estado == EstadoReserva.Solicitada || r.Estado == EstadoReserva.Aprobada))
            .OrderBy(r => r.HoraInicio)
            .ToListAsync();

    /// <summary>
    /// Valida una solicitud de reserva. Devuelve la lista de errores (campo, mensaje); vacía si es válida.
    /// </summary>
    public async Task<List<(string Campo, string Mensaje)>> ValidarSolicitudAsync(
        int areaId, DateTime fecha, TimeOnly inicio, TimeOnly fin, Residente residente)
    {
        var errores = new List<(string, string)>();

        // 1. Regla del Core (d.2): la morosidad de la unidad bloquea las reservas
        var clasificacion = await _cobranza.ClasificacionUnidadAsync(residente.UnidadId, DateTime.Today);
        if (!CobranzaService.PuedeReservar(clasificacion))
        {
            errores.Add((string.Empty,
                $"Su unidad está clasificada como {Formato.Texto(clasificacion)}. No puede reservar áreas comunales hasta regularizar su deuda."));
            return errores;
        }

        // 2. El área existe y está activa
        var area = await _db.AreasComunales.AsNoTracking().FirstOrDefaultAsync(a => a.Id == areaId);
        if (area is null || !area.Activa)
        {
            errores.Add(("AreaId", "El área comunal seleccionada no existe o no está disponible."));
            return errores;
        }

        // 3. Fecha: desde hoy y con máximo 60 días de anticipación
        var dia = fecha.Date;
        if (dia < DateTime.Today)
            errores.Add(("Fecha", "La fecha de la reserva no puede ser pasada."));
        else if (dia > DateTime.Today.AddDays(DiasMaximoAnticipacion))
            errores.Add(("Fecha", $"Solo se puede reservar con un máximo de {DiasMaximoAnticipacion} días de anticipación."));

        // 4. Horario: fin posterior al inicio, dentro del horario del área y duración razonable
        if (fin <= inicio)
            errores.Add(("HoraFin", "La hora de fin debe ser posterior a la hora de inicio."));
        else
        {
            if (inicio < area.HoraApertura || fin > area.HoraCierre)
                errores.Add(("HoraInicio", $"{area.Nombre} atiende de {area.HoraApertura:HH:mm} a {area.HoraCierre:HH:mm}."));
            if ((fin - inicio).TotalHours > HorasMaximasPorReserva)
                errores.Add(("HoraFin", $"Una reserva puede durar como máximo {HorasMaximasPorReserva} horas."));
        }
        if (dia == DateTime.Today && inicio <= TimeOnly.FromDateTime(DateTime.Now))
            errores.Add(("HoraInicio", "La hora de inicio ya pasó."));

        // 5. Sin cruces con otras reservas solicitadas o aprobadas del área
        if (errores.Count == 0)
        {
            var cruce = (await OcupacionAsync(areaId, dia)).FirstOrDefault(r => r.HoraInicio < fin && inicio < r.HoraFin);
            if (cruce is not null)
                errores.Add(("HoraInicio",
                    $"El horario se cruza con otra reserva ({cruce.HoraInicio:HH:mm}–{cruce.HoraFin:HH:mm}). Elija un horario disponible."));
        }

        return errores;
    }

    /// <summary>Aprueba una reserva solicitada volviendo a validar morosidad y cruces con reservas aprobadas.</summary>
    public async Task AprobarAsync(int reservaId, int adminId)
    {
        var reserva = await _db.Reservas.Include(r => r.Residente).FirstOrDefaultAsync(r => r.Id == reservaId)
                      ?? throw new ReglaNegocioException("La reserva no existe.");
        if (reserva.Estado != EstadoReserva.Solicitada)
            throw new ReglaNegocioException("Solo se pueden aprobar reservas en estado Solicitada.");
        if (reserva.Fecha.Date < DateTime.Today)
            throw new ReglaNegocioException("La fecha de la reserva ya pasó; recházela.");

        var clasificacion = await _cobranza.ClasificacionUnidadAsync(reserva.Residente!.UnidadId, DateTime.Today);
        if (!CobranzaService.PuedeReservar(clasificacion))
            throw new ReglaNegocioException(
                $"La unidad del residente está clasificada como {Formato.Texto(clasificacion)}: no puede reservar hasta regularizar su deuda.");

        var cruce = await _db.Reservas.AnyAsync(r =>
            r.Id != reserva.Id && r.AreaId == reserva.AreaId && r.Fecha == reserva.Fecha &&
            r.Estado == EstadoReserva.Aprobada && r.HoraInicio < reserva.HoraFin && reserva.HoraInicio < r.HoraFin);
        if (cruce)
            throw new ReglaNegocioException("El horario se cruza con otra reserva ya aprobada.");

        reserva.Estado = EstadoReserva.Aprobada;
        reserva.AprobadaPor = adminId;
        await _db.SaveChangesAsync();
    }

    public async Task RechazarAsync(int reservaId, int adminId)
    {
        var reserva = await _db.Reservas.FirstOrDefaultAsync(r => r.Id == reservaId)
                      ?? throw new ReglaNegocioException("La reserva no existe.");
        if (reserva.Estado != EstadoReserva.Solicitada)
            throw new ReglaNegocioException("Solo se pueden rechazar reservas en estado Solicitada.");

        reserva.Estado = EstadoReserva.Rechazada;
        reserva.AprobadaPor = adminId;   // usuario que resolvió la solicitud
        await _db.SaveChangesAsync();
    }
}
