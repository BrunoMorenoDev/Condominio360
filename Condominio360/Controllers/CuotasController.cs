using Condominio360.Data;
using Condominio360.Helpers;
using Condominio360.Models;
using Condominio360.Services;
using Condominio360.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>Cuotas mensuales: consulta, generación por periodo y anulación.</summary>
[Authorize(Roles = Rol.Administrador)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class CuotasController : Controller
{
    private readonly AppDbContext _db;
    private readonly CobranzaService _cobranza;

    public CuotasController(AppDbContext db, CobranzaService cobranza)
    {
        _db = db;
        _cobranza = cobranza;
    }

    // GET: /Cuotas?anio=2026&mes=10&torre=A&estado=Vencida
    public async Task<IActionResult> Index(int? anio, int? mes, string? torre, EstadoCuota? estado)
    {
        var hoy = DateTime.Today;
        var cfg = await _cobranza.ConfiguracionVigenteAsync(hoy);

        // Por defecto se muestra el último periodo generado
        if (anio is null && mes is null)
        {
            var ultimo = await _db.Cuotas.AsNoTracking()
                .OrderByDescending(c => c.Anio).ThenByDescending(c => c.Mes)
                .Select(c => new { c.Anio, c.Mes }).FirstOrDefaultAsync();
            anio = ultimo?.Anio ?? hoy.Year;
            mes = ultimo?.Mes ?? hoy.Month;
        }

        IQueryable<Cuota> consulta = _db.Cuotas.AsNoTracking().Include(c => c.Unidad);
        if (anio is not null) consulta = consulta.Where(c => c.Anio == anio);
        if (mes is not null) consulta = consulta.Where(c => c.Mes == mes);
        if (!string.IsNullOrWhiteSpace(torre)) consulta = consulta.Where(c => c.Unidad!.Torre == torre);

        var cuotas = await consulta
            .OrderBy(c => c.Unidad!.Torre).ThenBy(c => c.Unidad!.Numero).ThenBy(c => c.Anio).ThenBy(c => c.Mes)
            .ToListAsync();

        var filas = cfg is null
            ? new List<CuotaFila>()
            : cuotas.Select(c => CobranzaService.Calcular(c, hoy, cfg)).ToList();
        if (estado is not null) filas = filas.Where(f => f.Estado == estado).ToList();

        var anios = await _db.Cuotas.AsNoTracking().Select(c => c.Anio).Distinct().OrderByDescending(a => a).ToListAsync();
        if (!anios.Contains(hoy.Year)) anios.Insert(0, hoy.Year);

        var vm = new CuotasIndexViewModel
        {
            Anio = anio,
            Mes = mes,
            Torre = torre,
            Estado = estado,
            Filas = filas,
            Anios = anios,
            Torres = await _db.Unidades.AsNoTracking().Select(u => u.Torre).Distinct().OrderBy(t => t).ToListAsync()
        };
        return View(vm);
    }

    // GET: /Cuotas/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();

        var cuota = await _db.Cuotas.AsNoTracking()
            .Include(c => c.Unidad)
            .Include(c => c.GeneradaPorUsuario)
            .Include(c => c.Aplicaciones).ThenInclude(a => a.Pago)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (cuota is null) return NotFound();

        var cfg = await _cobranza.ConfiguracionVigenteAsync(DateTime.Today);
        ViewData["Calculo"] = cfg is null ? null : CobranzaService.Calcular(cuota, DateTime.Today, cfg);
        ViewData["Configuracion"] = cfg;
        return View(cuota);
    }

    // GET: /Cuotas/Generar?anio=2026&mes=11  (resumen previo)
    public async Task<IActionResult> Generar(int? anio, int? mes)
    {
        var siguiente = DateTime.Today.AddMonths(1);
        var vm = await _cobranza.PrevisualizarGeneracionAsync(anio ?? siguiente.Year, mes ?? siguiente.Month);
        return View(vm);
    }

    // POST: /Cuotas/Generar
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Generar")]
    public async Task<IActionResult> GenerarConfirmado(int anio, int mes)
    {
        try
        {
            var creadas = await _cobranza.GenerarCuotasAsync(anio, mes, User.IdUsuario());
            TempData[creadas > 0 ? "Exito" : "Error"] = creadas > 0
                ? $"Se generaron {creadas} cuotas de {Formato.Periodo(anio, mes)}."
                : $"Todas las unidades activas ya tenían cuota de {Formato.Periodo(anio, mes)}.";
            return RedirectToAction(nameof(Index), new { anio, mes });
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Generar), new { anio, mes });
        }
        catch (DbUpdateException)
        {
            // Otro usuario generó el mismo periodo al mismo tiempo (restricción única unidad–año–mes)
            TempData["Error"] = "Algunas cuotas ya existían. Revise el periodo e intente de nuevo.";
            return RedirectToAction(nameof(Generar), new { anio, mes });
        }
    }

    // POST: /Cuotas/Anular/5  (solo cuotas sin abonos)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Anular(int id)
    {
        var cuota = await _db.Cuotas.Include(c => c.Unidad).FirstOrDefaultAsync(c => c.Id == id);
        if (cuota is null) return NotFound();

        if (cuota.CapitalPagado > 0 || cuota.RecargoPagado > 0 || await _db.AplicacionesPago.AnyAsync(a => a.CuotaId == id))
        {
            TempData["Error"] = "No se puede anular una cuota que ya recibió pagos.";
        }
        else if (cuota.Estado == EstadoCuota.Anulada)
        {
            TempData["Error"] = "La cuota ya estaba anulada.";
        }
        else
        {
            cuota.Estado = EstadoCuota.Anulada;
            await _db.SaveChangesAsync();
            TempData["Exito"] = $"Cuota {cuota.Periodo} de {cuota.Unidad?.Codigo} anulada.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Cuotas/ActualizarEstados  (recalcula Pendiente/Parcial/Vencida a la fecha de hoy)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarEstados()
    {
        var cambios = await _cobranza.ActualizarEstadosAsync();
        TempData["Exito"] = $"Estados recalculados a la fecha de hoy ({cambios} cuotas actualizadas).";
        return RedirectToAction(nameof(Index));
    }
}
