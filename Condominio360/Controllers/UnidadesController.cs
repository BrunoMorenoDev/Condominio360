using Condominio360.Data;
using Condominio360.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>CRUD de unidades del condominio. Sección protegida.</summary>
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class UnidadesController : Controller
{
    private const string CamposPermitidos = "Id,Torre,Numero,Piso,Tipo,AreaM2,Alicuota";
    private readonly AppDbContext _db;

    public UnidadesController(AppDbContext db) => _db = db;

    // GET: /Unidades  (LEER - lista)
    public async Task<IActionResult> Index()
    {
        var unidades = await _db.Unidades.AsNoTracking()
            .Include(u => u.Residentes)
            .OrderBy(u => u.Torre).ThenBy(u => u.Numero)
            .ToListAsync();
        return View(unidades);
    }

    // GET: /Unidades/Details/5  (LEER - detalle)
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();

        var unidad = await _db.Unidades.AsNoTracking()
            .Include(u => u.Residentes)
            .FirstOrDefaultAsync(u => u.Id == id);

        return unidad is null ? NotFound() : View(unidad);
    }

    // GET: /Unidades/Create  (CREAR - formulario)
    public IActionResult Create() => View(new Unidad());

    // POST: /Unidades/Create  (CREAR - guardar)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(CamposPermitidos)] Unidad unidad)
    {
        Normalizar(unidad);
        await ValidarDuplicadoAsync(unidad);
        if (!ModelState.IsValid) return View(unidad);

        _db.Unidades.Add(unidad);
        await _db.SaveChangesAsync();

        TempData["Exito"] = $"Unidad {unidad.Codigo} creada.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Unidades/Edit/5  (ACTUALIZAR - formulario)
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var unidad = await _db.Unidades.FindAsync(id);
        return unidad is null ? NotFound() : View(unidad);
    }

    // POST: /Unidades/Edit/5  (ACTUALIZAR - guardar)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(CamposPermitidos)] Unidad unidad)
    {
        if (id != unidad.Id) return NotFound();

        Normalizar(unidad);
        await ValidarDuplicadoAsync(unidad);
        if (!ModelState.IsValid) return View(unidad);

        try
        {
            _db.Update(unidad);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _db.Unidades.AnyAsync(u => u.Id == id)) return NotFound();
            throw;
        }

        TempData["Exito"] = $"Unidad {unidad.Codigo} actualizada.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Unidades/Delete/5  (ELIMINAR - confirmación)
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();

        var unidad = await _db.Unidades.AsNoTracking()
            .Include(u => u.Residentes)
            .FirstOrDefaultAsync(u => u.Id == id);

        return unidad is null ? NotFound() : View(unidad);
    }

    // POST: /Unidades/Delete/5  (ELIMINAR - ejecutar)
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var unidad = await _db.Unidades
            .Include(u => u.Residentes)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (unidad is null) return RedirectToAction(nameof(Index));

        if (unidad.Residentes.Any())
        {
            TempData["Error"] = $"La unidad {unidad.Codigo} tiene residentes registrados. Reasígnelos o elimínelos primero.";
            return RedirectToAction(nameof(Index));
        }

        _db.Unidades.Remove(unidad);
        await _db.SaveChangesAsync();

        TempData["Exito"] = $"Unidad {unidad.Codigo} eliminada.";
        return RedirectToAction(nameof(Index));
    }

    private static void Normalizar(Unidad unidad)
    {
        unidad.Torre = (unidad.Torre ?? string.Empty).Trim().ToUpperInvariant();
        unidad.Numero = (unidad.Numero ?? string.Empty).Trim().ToUpperInvariant();
    }

    private async Task ValidarDuplicadoAsync(Unidad unidad)
    {
        var existe = await _db.Unidades.AnyAsync(u =>
            u.Id != unidad.Id && u.Torre == unidad.Torre && u.Numero == unidad.Numero);

        if (existe)
            ModelState.AddModelError(nameof(Unidad.Numero), $"Ya existe la unidad {unidad.Codigo}.");
    }
}
