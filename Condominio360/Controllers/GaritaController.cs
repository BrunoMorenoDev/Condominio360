using Condominio360.Data;
using Condominio360.Helpers;
using Condominio360.Models;
using Condominio360.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>
/// Control de visitas en la garita: el guardia consulta las visitas autorizadas del día y registra
/// la hora de ingreso y de salida. El administrador puede consultar el mismo registro.
/// </summary>
[Authorize(Roles = Rol.Guardia + "," + Rol.Administrador)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class GaritaController : Controller
{
    private readonly AppDbContext _db;

    public GaritaController(AppDbContext db) => _db = db;

    // GET: /Garita?fecha=2026-11-14&buscar=PBX
    public async Task<IActionResult> Index(DateTime? fecha, string? buscar)
    {
        var dia = (fecha ?? DateTime.Today).Date;
        var vm = new GaritaViewModel { Fecha = dia, Buscar = buscar?.Trim() };

        IQueryable<Visita> consulta = _db.Visitas.AsNoTracking()
            .Include(v => v.Unidad)
            .Include(v => v.AutorizadaPorResidente)
            .Include(v => v.RegistradaPorUsuario)
            .Where(v => v.FechaEsperada == dia);

        if (!string.IsNullOrWhiteSpace(vm.Buscar))
        {
            var patron = $"%{vm.Buscar}%";
            consulta = consulta.Where(v =>
                EF.Functions.ILike(v.NombreVisitante, patron) ||
                EF.Functions.ILike(v.CedulaVisitante, patron) ||
                (v.PlacaVehiculo != null && EF.Functions.ILike(v.PlacaVehiculo, patron)) ||
                EF.Functions.ILike(v.Unidad!.Torre + "-" + v.Unidad.Numero, patron));
        }

        vm.Visitas = await consulta
            .OrderBy(v => v.Estado == EstadoVisita.Ingresada ? 0 : v.Estado == EstadoVisita.Autorizada ? 1 : 2)
            .ThenBy(v => v.NombreVisitante)
            .ToListAsync();

        // Visitas que entraron otro día y todavía no registran salida
        if (vm.EsHoy)
        {
            vm.DentroDeDiasAnteriores = await _db.Visitas.AsNoTracking()
                .Include(v => v.Unidad)
                .Include(v => v.AutorizadaPorResidente)
                .Where(v => v.Estado == EstadoVisita.Ingresada && v.FechaEsperada < dia)
                .OrderBy(v => v.HoraIngreso)
                .ToListAsync();
        }

        return View(vm);
    }

    // POST: /Garita/Ingreso/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ingreso(int id)
    {
        var visita = await _db.Visitas.FirstOrDefaultAsync(v => v.Id == id);
        if (visita is null) return NotFound();

        if (visita.Estado != EstadoVisita.Autorizada)
            TempData["Error"] = "Solo se puede registrar el ingreso de una visita autorizada.";
        else if (visita.FechaEsperada.Date != DateTime.Today)
            TempData["Error"] = $"La visita está autorizada para el {visita.FechaEsperada:dd/MM/yyyy}, no para hoy.";
        else
        {
            visita.HoraIngreso = DateTime.Now;
            visita.Estado = EstadoVisita.Ingresada;
            visita.RegistradaPor = User.IdUsuario();
            await _db.SaveChangesAsync();
            TempData["Exito"] = $"Ingreso de {visita.NombreVisitante} registrado a las {visita.HoraIngreso:HH:mm}.";
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: /Garita/Salida/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salida(int id)
    {
        var visita = await _db.Visitas.FirstOrDefaultAsync(v => v.Id == id);
        if (visita is null) return NotFound();

        if (visita.Estado != EstadoVisita.Ingresada)
            TempData["Error"] = "Solo se puede registrar la salida de una visita que está dentro del condominio.";
        else
        {
            var ahora = DateTime.Now;
            visita.HoraSalida = visita.HoraIngreso is { } ingreso && ingreso > ahora ? ingreso : ahora;
            visita.Estado = EstadoVisita.Finalizada;
            visita.RegistradaPor = User.IdUsuario();
            await _db.SaveChangesAsync();
            TempData["Exito"] = $"Salida de {visita.NombreVisitante} registrada a las {visita.HoraSalida:HH:mm}.";
        }
        return RedirectToAction(nameof(Index));
    }
}
