using Condominio360.Data;
using Condominio360.Helpers;
using Condominio360.Models;
using Condominio360.Services;
using Condominio360.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>Administración de las reservas de áreas comunales: aprobar o rechazar solicitudes.</summary>
[Authorize(Roles = Rol.Administrador)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ReservasController : Controller
{
    private readonly AppDbContext _db;
    private readonly ReservasService _reservas;
    private readonly CobranzaService _cobranza;

    public ReservasController(AppDbContext db, ReservasService reservas, CobranzaService cobranza)
    {
        _db = db;
        _reservas = reservas;
        _cobranza = cobranza;
    }

    // GET: /Reservas?estado=Solicitada&areaId=1&desde=2026-11-01
    public async Task<IActionResult> Index(EstadoReserva? estado, int? areaId, DateTime? desde)
    {
        IQueryable<Reserva> consulta = _db.Reservas.AsNoTracking()
            .Include(r => r.Area)
            .Include(r => r.Residente).ThenInclude(r => r!.Unidad)
            .Include(r => r.AprobadaPorUsuario);

        if (estado is not null) consulta = consulta.Where(r => r.Estado == estado);
        if (areaId is > 0) consulta = consulta.Where(r => r.AreaId == areaId);
        if (desde is not null) consulta = consulta.Where(r => r.Fecha >= desde.Value.Date);

        var reservas = await consulta
            .OrderBy(r => r.Estado == EstadoReserva.Solicitada ? 0 : 1)
            .ThenByDescending(r => r.Fecha).ThenBy(r => r.HoraInicio)
            .ToListAsync();

        var vm = new ReservasIndexViewModel
        {
            Estado = estado,
            AreaId = areaId,
            Desde = desde,
            Reservas = reservas,
            Areas = await _db.AreasComunales.AsNoTracking().OrderBy(a => a.Nombre).ToListAsync(),
            PorAprobar = await _db.Reservas.CountAsync(r => r.Estado == EstadoReserva.Solicitada)
        };

        // Clasificación actual de las unidades con solicitudes pendientes (regla d.2 del Core)
        foreach (var unidadId in reservas.Where(r => r.Estado == EstadoReserva.Solicitada)
                                         .Select(r => r.Residente!.UnidadId).Distinct())
            vm.ClasificacionPorUnidad[unidadId] = await _cobranza.ClasificacionUnidadAsync(unidadId, DateTime.Today);

        return View(vm);
    }

    // POST: /Reservas/Aprobar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(int id)
    {
        try
        {
            await _reservas.AprobarAsync(id, User.IdUsuario());
            TempData["Exito"] = "Reserva aprobada.";
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { estado = EstadoReserva.Solicitada });
    }

    // POST: /Reservas/Rechazar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(int id)
    {
        try
        {
            await _reservas.RechazarAsync(id, User.IdUsuario());
            TempData["Exito"] = "Reserva rechazada.";
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { estado = EstadoReserva.Solicitada });
    }
}
