using Condominio360.Data;
using Condominio360.Models;
using Condominio360.Services;
using Condominio360.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>Visitas autorizadas por el residente para su unidad (las controla el guardia en la garita).</summary>
public class MisVisitasController : ResidenteBaseController
{
    public const int DiasMaximoAnticipacion = 30;

    public MisVisitasController(AppDbContext db) : base(db) { }

    // GET: /MisVisitas
    public async Task<IActionResult> Index()
    {
        var residente = await ResidenteActualAsync();
        if (residente is null) return SinVinculo();

        var visitas = await Db.Visitas.AsNoTracking()
            .Include(v => v.AutorizadaPorResidente)
            .Where(v => v.UnidadId == residente.UnidadId)
            .OrderByDescending(v => v.FechaEsperada).ThenByDescending(v => v.Id)
            .Take(100)
            .ToListAsync();

        ViewData["Unidad"] = residente.Unidad?.Codigo;
        return View(visitas);
    }

    // GET: /MisVisitas/Autorizar
    public async Task<IActionResult> Autorizar()
    {
        var residente = await ResidenteActualAsync();
        if (residente is null) return SinVinculo();
        return View(new AutorizarVisitaViewModel());
    }

    // POST: /MisVisitas/Autorizar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Autorizar(AutorizarVisitaViewModel vm)
    {
        var residente = await ResidenteActualAsync();
        if (residente is null) return SinVinculo();

        vm.NombreVisitante = (vm.NombreVisitante ?? string.Empty).Trim();
        vm.CedulaVisitante = (vm.CedulaVisitante ?? string.Empty).Trim();

        // Cédula del visitante: algoritmo módulo 10 del Registro Civil (validación de servidor)
        var errorCedula = ValidadorCedula.Validar(vm.CedulaVisitante);
        if (errorCedula is not null)
        {
            ModelState.Remove(nameof(vm.CedulaVisitante));
            ModelState.AddModelError(nameof(vm.CedulaVisitante), errorCedula);
        }

        // Placa opcional con formato ecuatoriano
        vm.PlacaVehiculo = ValidadorPlaca.Normalizar(vm.PlacaVehiculo, out var errorPlaca);
        if (errorPlaca is not null) ModelState.AddModelError(nameof(vm.PlacaVehiculo), errorPlaca);

        // Fecha esperada: desde hoy hasta 30 días
        if (vm.FechaEsperada.Date < DateTime.Today)
            ModelState.AddModelError(nameof(vm.FechaEsperada), "La fecha esperada no puede ser pasada.");
        else if (vm.FechaEsperada.Date > DateTime.Today.AddDays(DiasMaximoAnticipacion))
            ModelState.AddModelError(nameof(vm.FechaEsperada), $"Solo se pueden autorizar visitas con hasta {DiasMaximoAnticipacion} días de anticipación.");

        // Evita duplicar la misma autorización
        if (ModelState.IsValid && await Db.Visitas.AnyAsync(v =>
                v.UnidadId == residente.UnidadId && v.CedulaVisitante == vm.CedulaVisitante &&
                v.FechaEsperada == vm.FechaEsperada.Date &&
                (v.Estado == EstadoVisita.Autorizada || v.Estado == EstadoVisita.Ingresada)))
            ModelState.AddModelError(nameof(vm.CedulaVisitante), "Ese visitante ya está autorizado para esa fecha.");

        if (!ModelState.IsValid) return View(vm);

        var visita = new Visita
        {
            UnidadId = residente.UnidadId,      // la visita siempre es para la unidad del residente
            AutorizadaPor = residente.Id,
            NombreVisitante = vm.NombreVisitante,
            CedulaVisitante = vm.CedulaVisitante,
            PlacaVehiculo = vm.PlacaVehiculo,
            FechaEsperada = vm.FechaEsperada.Date,
            Estado = EstadoVisita.Autorizada
        };
        Db.Visitas.Add(visita);
        await Db.SaveChangesAsync();

        TempData["Exito"] = $"Visita de {visita.NombreVisitante} autorizada para el {visita.FechaEsperada:dd/MM/yyyy}. La garita ya puede registrar su ingreso.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /MisVisitas/Cancelar/5  (solo visitas de su unidad que todavía no ingresan)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        var residente = await ResidenteActualAsync();
        if (residente is null) return SinVinculo();

        var visita = await Db.Visitas.FirstOrDefaultAsync(v => v.Id == id && v.UnidadId == residente.UnidadId);
        if (visita is null) return NotFound();

        if (visita.Estado != EstadoVisita.Autorizada)
        {
            TempData["Error"] = "Solo se pueden cancelar visitas que todavía no han ingresado.";
            return RedirectToAction(nameof(Index));
        }

        visita.Estado = EstadoVisita.Cancelada;
        await Db.SaveChangesAsync();
        TempData["Exito"] = "Autorización de visita cancelada.";
        return RedirectToAction(nameof(Index));
    }
}
