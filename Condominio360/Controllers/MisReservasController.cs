using Condominio360.Data;
using Condominio360.Models;
using Condominio360.Services;
using Condominio360.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>
/// Reservas de áreas comunales desde el lado del residente: consultar disponibilidad,
/// solicitar y cancelar. Aplica la regla d.2 del Core (morosos no reservan).
/// </summary>
public class MisReservasController : ResidenteBaseController
{
    private readonly ReservasService _reservas;
    private readonly CobranzaService _cobranza;

    public MisReservasController(AppDbContext db, ReservasService reservas, CobranzaService cobranza) : base(db)
    {
        _reservas = reservas;
        _cobranza = cobranza;
    }

    // GET: /MisReservas
    public async Task<IActionResult> Index()
    {
        var residente = await ResidenteActualAsync();
        if (residente is null) return SinVinculo();

        var clasificacion = await _cobranza.ClasificacionUnidadAsync(residente.UnidadId, DateTime.Today);
        ViewData["Clasificacion"] = clasificacion;
        ViewData["PuedeReservar"] = CobranzaService.PuedeReservar(clasificacion);

        var reservas = await Db.Reservas.AsNoTracking()
            .Include(r => r.Area)
            .Where(r => r.ResidenteId == residente.Id)
            .OrderByDescending(r => r.Fecha).ThenByDescending(r => r.HoraInicio)
            .ToListAsync();
        return View(reservas);
    }

    // GET: /MisReservas/Solicitar?areaId=1
    public async Task<IActionResult> Solicitar(int? areaId)
    {
        var residente = await ResidenteActualAsync();
        if (residente is null) return SinVinculo();

        var vm = new SolicitarReservaViewModel { AreaId = areaId ?? 0 };
        await CargarAsync(vm, residente);
        return View(vm);
    }

    // POST: /MisReservas/Solicitar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Solicitar(SolicitarReservaViewModel vm)
    {
        var residente = await ResidenteActualAsync();
        if (residente is null) return SinVinculo();

        if (ModelState.IsValid)
        {
            foreach (var (campo, mensaje) in await _reservas.ValidarSolicitudAsync(vm.AreaId, vm.Fecha, vm.HoraInicio, vm.HoraFin, residente))
                ModelState.AddModelError(campo, mensaje);
        }

        if (!ModelState.IsValid)
        {
            await CargarAsync(vm, residente);
            return View(vm);
        }

        var reserva = new Reserva
        {
            AreaId = vm.AreaId,
            ResidenteId = residente.Id,       // siempre el residente de la sesión
            Fecha = vm.Fecha.Date,
            HoraInicio = vm.HoraInicio,
            HoraFin = vm.HoraFin,
            Estado = EstadoReserva.Solicitada
        };
        Db.Reservas.Add(reserva);
        await Db.SaveChangesAsync();

        TempData["Exito"] = $"Reserva solicitada para el {reserva.Fecha:dd/MM/yyyy} de {reserva.HoraInicio:HH:mm} a {reserva.HoraFin:HH:mm}. " +
                            "La administración la revisará.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /MisReservas/Cancelar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        var residente = await ResidenteActualAsync();
        if (residente is null) return SinVinculo();

        var reserva = await Db.Reservas.FirstOrDefaultAsync(r => r.Id == id && r.ResidenteId == residente.Id);
        if (reserva is null) return NotFound();

        if (reserva.Estado is not (EstadoReserva.Solicitada or EstadoReserva.Aprobada) || reserva.Fecha.Date < DateTime.Today)
        {
            TempData["Error"] = "Solo se pueden cancelar reservas solicitadas o aprobadas que aún no han pasado.";
            return RedirectToAction(nameof(Index));
        }

        reserva.Estado = EstadoReserva.Cancelada;
        await Db.SaveChangesAsync();
        TempData["Exito"] = "Reserva cancelada.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /MisReservas/Disponibilidad?areaId=1&fecha=2026-11-20  (JSON informativo; el servidor revalida al guardar)
    [HttpGet]
    public async Task<IActionResult> Disponibilidad(int areaId, DateTime fecha)
    {
        var area = await Db.AreasComunales.AsNoTracking().FirstOrDefaultAsync(a => a.Id == areaId && a.Activa);
        if (area is null) return Json(new { horario = "", ocupadas = Array.Empty<object>() });

        var ocupadas = (await _reservas.OcupacionAsync(areaId, fecha))
            .Select(r => new { inicio = r.HoraInicio.ToString("HH:mm"), fin = r.HoraFin.ToString("HH:mm"), estado = r.Estado.ToString() });

        return Json(new
        {
            horario = $"{area.HoraApertura:HH:mm} – {area.HoraCierre:HH:mm}",
            apertura = area.HoraApertura.ToString("HH:mm"),
            cierre = area.HoraCierre.ToString("HH:mm"),
            capacidad = area.Capacidad,
            costo = area.CostoUso.ToString("C"),
            ocupadas
        });
    }

    private async Task CargarAsync(SolicitarReservaViewModel vm, Residente residente)
    {
        vm.Clasificacion = await _cobranza.ClasificacionUnidadAsync(residente.UnidadId, DateTime.Today);
        vm.PuedeReservar = CobranzaService.PuedeReservar(vm.Clasificacion);
        vm.DetalleAreas = await Db.AreasComunales.AsNoTracking().Where(a => a.Activa).OrderBy(a => a.Nombre).ToListAsync();
        vm.Areas = vm.DetalleAreas.Select(a => new SelectListItem(a.Nombre, a.Id.ToString(), a.Id == vm.AreaId));
    }
}
