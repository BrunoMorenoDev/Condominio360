using Condominio360.Data;
using Condominio360.Models;
using Condominio360.Services;
using Condominio360.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>Resumen del condominio con los indicadores del Core. Sección protegida.</summary>
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

        // Los módulos de Residente y Guardia se implementan en una entrega posterior
        if (!User.IsInRole(Rol.Administrador))
        {
            ViewData["Nombre"] = nombre;
            return View("SinModulo");
        }

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
            PagosPorVerificar = await _db.Pagos.CountAsync(p => p.Estado == EstadoPago.PorVerificar)
        };

        return View(vm);
    }
}
