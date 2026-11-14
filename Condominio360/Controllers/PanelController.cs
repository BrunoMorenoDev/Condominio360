using Condominio360.Data;
using Condominio360.Helpers;
using Condominio360.Models;
using Condominio360.Services;
using Condominio360.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>
/// Página de inicio después del login. Cada rol ve su propio panel:
/// Administrador → indicadores del condominio; Residente → su unidad; Guardia → garita.
/// </summary>
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PanelController : Controller
{
    private readonly AppDbContext _db;
    private readonly CobranzaService _cobranza;

    public PanelController(AppDbContext db, CobranzaService cobranza)
    {
        _db = db;
        _cobranza = cobranza;
    }

    public async Task<IActionResult> Index()
    {
        var nombre = User.FindFirst("NombreCompleto")?.Value ?? User.Identity?.Name ?? "";

        if (User.IsInRole(Rol.Guardia)) return RedirectToAction("Index", "Garita");
        if (User.IsInRole(Rol.Residente)) return await PanelResidenteAsync(nombre);
        if (!User.IsInRole(Rol.Administrador)) return Forbid();

        var reporte = await _cobranza.ReporteMorosidadAsync(DateTime.Today, null, null, null);

        var vm = new PanelViewModel
        {
            NombreUsuario = nombre,
            TotalUnidades = await _db.Unidades.CountAsync(),
            UnidadesActivas = await _db.Unidades.CountAsync(u => u.Activa),
            TotalResidentes = await _db.Residentes.CountAsync(),
            AlicuotaMensualTotal = await _db.Unidades.Where(u => u.Activa).SumAsync(u => u.Alicuota),
            IndiceMorosidad = reporte.IndiceMorosidad,
            CarteraVencida = reporte.CarteraVencida,
            TasaRecaudacion = reporte.TasaRecaudacion,
            PeriodoRecaudacion = reporte.PeriodoRecaudacion,
            PagosPorVerificar = await _db.Pagos.CountAsync(p => p.Estado == EstadoPago.PorVerificar),
            ReservasPorAprobar = await _db.Reservas.CountAsync(r => r.Estado == EstadoReserva.Solicitada && r.Fecha >= DateTime.Today),
            VisitasHoy = await _db.Visitas.CountAsync(v => v.FechaEsperada == DateTime.Today && v.Estado != EstadoVisita.Cancelada),
            UsuariosActivos = await _db.Usuarios.CountAsync(u => u.Activo)
        };

        return View(vm);
    }

    private async Task<IActionResult> PanelResidenteAsync(string nombre)
    {
        var idUsuario = User.IdUsuario();
        var residenteId = await _db.Usuarios.AsNoTracking()
            .Where(u => u.Id == idUsuario).Select(u => u.ResidenteId).FirstOrDefaultAsync();
        var residente = residenteId is null ? null : await _db.Residentes.AsNoTracking()
            .Include(r => r.Unidad).FirstOrDefaultAsync(r => r.Id == residenteId);
        if (residente is null) return View("~/Views/Shared/SinVinculo.cshtml");

        var estado = await _cobranza.EstadoCuentaAsync(residente.UnidadId, DateTime.Today);
        if (estado is null) return View("~/Views/Shared/SinVinculo.cshtml");

        var vm = new PanelResidenteViewModel
        {
            NombreUsuario = nombre,
            Residente = residente,
            EstadoCuenta = estado,
            PuedeReservar = CobranzaService.PuedeReservar(estado.Clasificacion),
            PagosPorVerificar = estado.Pagos.Count(p => p.Estado == EstadoPago.PorVerificar),
            ProximasReservas = await _db.Reservas.AsNoTracking()
                .Include(r => r.Area)
                .Where(r => r.ResidenteId == residente.Id && r.Fecha >= DateTime.Today &&
                            (r.Estado == EstadoReserva.Solicitada || r.Estado == EstadoReserva.Aprobada))
                .OrderBy(r => r.Fecha).ThenBy(r => r.HoraInicio)
                .Take(5).ToListAsync(),
            ProximasVisitas = await _db.Visitas.AsNoTracking()
                .Where(v => v.UnidadId == residente.UnidadId && v.FechaEsperada >= DateTime.Today &&
                            (v.Estado == EstadoVisita.Autorizada || v.Estado == EstadoVisita.Ingresada))
                .OrderBy(v => v.FechaEsperada)
                .Take(5).ToListAsync()
        };
        return View("Residente", vm);
    }
}
