using Condominio360.Data;
using Condominio360.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>CRUD de residentes del condominio. Sección protegida.</summary>
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ResidentesController : Controller
{
    private const string CamposPermitidos = "Id,Nombres,Apellidos,Cedula,Correo,Telefono,Tipo,FechaIngreso,UnidadId";
    private readonly AppDbContext _db;

    public ResidentesController(AppDbContext db) => _db = db;

    // GET: /Residentes?buscar=texto  (LEER - lista con búsqueda)
    public async Task<IActionResult> Index(string? buscar)
    {
        IQueryable<Residente> consulta = _db.Residentes.AsNoTracking().Include(r => r.Unidad);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var patron = $"%{buscar.Trim()}%";
            consulta = consulta.Where(r =>
                EF.Functions.Like(r.Nombres, patron) ||
                EF.Functions.Like(r.Apellidos, patron) ||
                EF.Functions.Like(r.Cedula, patron));
        }

        ViewData["Buscar"] = buscar;
        var residentes = await consulta.OrderBy(r => r.Apellidos).ThenBy(r => r.Nombres).ToListAsync();
        return View(residentes);
    }

    // GET: /Residentes/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();

        var residente = await _db.Residentes.AsNoTracking()
            .Include(r => r.Unidad)
            .FirstOrDefaultAsync(r => r.Id == id);

        return residente is null ? NotFound() : View(residente);
    }

    // GET: /Residentes/Create
    public async Task<IActionResult> Create()
    {
        await CargarUnidadesAsync();
        return View(new Residente());
    }

    // POST: /Residentes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(CamposPermitidos)] Residente residente)
    {
        Normalizar(residente);
        await ValidarAsync(residente);

        if (!ModelState.IsValid)
        {
            await CargarUnidadesAsync(residente.UnidadId);
            return View(residente);
        }

        _db.Residentes.Add(residente);
        await _db.SaveChangesAsync();

        TempData["Exito"] = $"Residente {residente.NombreCompleto} registrado.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Residentes/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();

        var residente = await _db.Residentes.FindAsync(id);
        if (residente is null) return NotFound();

        await CargarUnidadesAsync(residente.UnidadId);
        return View(residente);
    }

    // POST: /Residentes/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(CamposPermitidos)] Residente residente)
    {
        if (id != residente.Id) return NotFound();

        Normalizar(residente);
        await ValidarAsync(residente);

        if (!ModelState.IsValid)
        {
            await CargarUnidadesAsync(residente.UnidadId);
            return View(residente);
        }

        try
        {
            _db.Update(residente);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _db.Residentes.AnyAsync(r => r.Id == id)) return NotFound();
            throw;
        }

        TempData["Exito"] = $"Datos de {residente.NombreCompleto} actualizados.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Residentes/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();

        var residente = await _db.Residentes.AsNoTracking()
            .Include(r => r.Unidad)
            .FirstOrDefaultAsync(r => r.Id == id);

        return residente is null ? NotFound() : View(residente);
    }

    // POST: /Residentes/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var residente = await _db.Residentes.FindAsync(id);
        if (residente is not null)
        {
            _db.Residentes.Remove(residente);
            await _db.SaveChangesAsync();
            TempData["Exito"] = $"Residente {residente.NombreCompleto} eliminado.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task CargarUnidadesAsync(int? seleccionada = null)
    {
        var unidades = await _db.Unidades.AsNoTracking()
            .OrderBy(u => u.Torre).ThenBy(u => u.Numero)
            .Select(u => new { u.Id, Nombre = "Torre " + u.Torre + " · " + u.Numero })
            .ToListAsync();

        ViewData["Unidades"] = new SelectList(unidades, "Id", "Nombre", seleccionada);
    }

    private static void Normalizar(Residente r)
    {
        r.Nombres = (r.Nombres ?? string.Empty).Trim();
        r.Apellidos = (r.Apellidos ?? string.Empty).Trim();
        r.Cedula = (r.Cedula ?? string.Empty).Trim();
        r.Correo = (r.Correo ?? string.Empty).Trim().ToLowerInvariant();
        r.Telefono = string.IsNullOrWhiteSpace(r.Telefono) ? null : r.Telefono.Trim();
    }

    private async Task ValidarAsync(Residente r)
    {
        if (await _db.Residentes.AnyAsync(x => x.Id != r.Id && x.Cedula == r.Cedula))
            ModelState.AddModelError(nameof(Residente.Cedula), "Ya existe un residente con esta cédula.");

        if (r.UnidadId > 0 && !await _db.Unidades.AnyAsync(u => u.Id == r.UnidadId))
            ModelState.AddModelError(nameof(Residente.UnidadId), "La unidad seleccionada no existe.");

        if (r.FechaIngreso > DateTime.Today)
            ModelState.AddModelError(nameof(Residente.FechaIngreso), "La fecha de ingreso no puede ser futura.");
    }
}
