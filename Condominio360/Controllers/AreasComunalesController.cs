using Condominio360.Data;
using Condominio360.Models;
using Condominio360.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>CRUD de áreas comunales (nombre, capacidad, horario y costo de uso). Solo administradores.</summary>
[Authorize(Roles = Rol.Administrador)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AreasComunalesController : Controller
{
    private readonly AppDbContext _db;

    public AreasComunalesController(AppDbContext db) => _db = db;

    // GET: /AreasComunales
    public async Task<IActionResult> Index()
    {
        var areas = await _db.AreasComunales.AsNoTracking()
            .Include(a => a.Reservas)
            .OrderBy(a => a.Nombre)
            .ToListAsync();
        return View(areas);
    }

    // GET: /AreasComunales/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var area = await _db.AreasComunales.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        if (area is null) return NotFound();

        ViewData["Proximas"] = await _db.Reservas.AsNoTracking()
            .Include(r => r.Residente).ThenInclude(r => r!.Unidad)
            .Where(r => r.AreaId == id && r.Fecha >= DateTime.Today &&
                        (r.Estado == EstadoReserva.Solicitada || r.Estado == EstadoReserva.Aprobada))
            .OrderBy(r => r.Fecha).ThenBy(r => r.HoraInicio)
            .ToListAsync();
        return View(area);
    }

    // GET: /AreasComunales/Create
    public IActionResult Create() => View(new AreaComunalFormViewModel());

    // POST: /AreasComunales/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AreaComunalFormViewModel vm)
    {
        Normalizar(vm);
        await ValidarAsync(vm);
        if (!ModelState.IsValid) return View(vm);

        var area = new AreaComunal();
        Copiar(vm, area);
        _db.AreasComunales.Add(area);
        await _db.SaveChangesAsync();

        TempData["Exito"] = $"Área comunal «{area.Nombre}» registrada.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /AreasComunales/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var area = await _db.AreasComunales.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        if (area is null) return NotFound();

        return View(new AreaComunalFormViewModel
        {
            Id = area.Id,
            Nombre = area.Nombre,
            Descripcion = area.Descripcion,
            Capacidad = area.Capacidad,
            HoraApertura = area.HoraApertura,
            HoraCierre = area.HoraCierre,
            CostoUso = area.CostoUso,
            Activa = area.Activa
        });
    }

    // POST: /AreasComunales/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AreaComunalFormViewModel vm)
    {
        if (id != vm.Id) return NotFound();
        var area = await _db.AreasComunales.FirstOrDefaultAsync(a => a.Id == id);
        if (area is null) return NotFound();

        Normalizar(vm);
        await ValidarAsync(vm);
        if (!ModelState.IsValid) return View(vm);

        Copiar(vm, area);
        await _db.SaveChangesAsync();

        TempData["Exito"] = $"Área comunal «{area.Nombre}» actualizada.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /AreasComunales/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var area = await _db.AreasComunales.AsNoTracking().Include(a => a.Reservas).FirstOrDefaultAsync(a => a.Id == id);
        return area is null ? NotFound() : View(area);
    }

    // POST: /AreasComunales/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var area = await _db.AreasComunales.FirstOrDefaultAsync(a => a.Id == id);
        if (area is null) return RedirectToAction(nameof(Index));

        // Regla de integridad: un área con historial de reservas no se elimina, se desactiva
        if (await _db.Reservas.AnyAsync(r => r.AreaId == id))
        {
            TempData["Error"] = $"«{area.Nombre}» tiene reservas registradas y no se puede eliminar. Desactívela para que no se pueda reservar.";
            return RedirectToAction(nameof(Index));
        }

        _db.AreasComunales.Remove(area);
        await _db.SaveChangesAsync();
        TempData["Exito"] = $"Área comunal «{area.Nombre}» eliminada.";
        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------------

    private static void Normalizar(AreaComunalFormViewModel vm)
    {
        vm.Nombre = (vm.Nombre ?? string.Empty).Trim();
        vm.Descripcion = string.IsNullOrWhiteSpace(vm.Descripcion) ? null : vm.Descripcion.Trim();
    }

    private async Task ValidarAsync(AreaComunalFormViewModel vm)
    {
        if (vm.HoraCierre <= vm.HoraApertura)
            ModelState.AddModelError(nameof(vm.HoraCierre), "La hora de cierre debe ser posterior a la de apertura.");

        if (decimal.Round(vm.CostoUso, 2) != vm.CostoUso)
            ModelState.AddModelError(nameof(vm.CostoUso), "El costo admite como máximo dos decimales.");

        if (!string.IsNullOrEmpty(vm.Nombre) &&
            await _db.AreasComunales.AnyAsync(a => a.Id != vm.Id && a.Nombre.ToLower() == vm.Nombre.ToLower()))
            ModelState.AddModelError(nameof(vm.Nombre), "Ya existe un área comunal con ese nombre.");

        // Al editar, el nuevo horario no puede dejar fuera reservas futuras ya aprobadas o solicitadas
        if (vm.Id > 0 && vm.HoraCierre > vm.HoraApertura)
        {
            var fueraDeHorario = await _db.Reservas.CountAsync(r =>
                r.AreaId == vm.Id && r.Fecha >= DateTime.Today &&
                (r.Estado == EstadoReserva.Solicitada || r.Estado == EstadoReserva.Aprobada) &&
                (r.HoraInicio < vm.HoraApertura || r.HoraFin > vm.HoraCierre));
            if (fueraDeHorario > 0)
                ModelState.AddModelError(nameof(vm.HoraApertura),
                    $"Hay {fueraDeHorario} reserva(s) futura(s) fuera del nuevo horario. Resuélvalas antes de cambiarlo.");
        }
    }

    private static void Copiar(AreaComunalFormViewModel vm, AreaComunal area)
    {
        area.Nombre = vm.Nombre;
        area.Descripcion = vm.Descripcion;
        area.Capacidad = vm.Capacidad;
        area.HoraApertura = vm.HoraApertura;
        area.HoraCierre = vm.HoraCierre;
        area.CostoUso = vm.CostoUso;
        area.Activa = vm.Activa;
    }
}
