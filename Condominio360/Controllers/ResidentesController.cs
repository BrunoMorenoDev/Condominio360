using Condominio360.Data;
using Condominio360.Models;
using Condominio360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>CRUD de residentes del condominio. Solo administradores.</summary>
[Authorize(Roles = Rol.Administrador)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ResidentesController : Controller
{
    private const string CamposPermitidos = "Id,Nombres,Apellidos,Cedula,Correo,Telefono,Tipo,FechaIngreso,ResponsablePago,UnidadId";
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
                EF.Functions.ILike(r.Nombres, patron) ||
                EF.Functions.ILike(r.Apellidos, patron) ||
                EF.Functions.ILike(r.Cedula, patron));
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
        await CargarListasAsync(null);
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
            await CargarListasAsync(residente.UnidadId);
            return View(residente);
        }

        await QuitarResponsableAnteriorAsync(residente);
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

        await CargarListasAsync(residente.UnidadId);
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
            await CargarListasAsync(residente.UnidadId);
            return View(residente);
        }

        try
        {
            await QuitarResponsableAnteriorAsync(residente);
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
        if (residente is null) return RedirectToAction(nameof(Index));

        if (await _db.Reservas.AnyAsync(r => r.ResidenteId == id) ||
            await _db.Visitas.AnyAsync(v => v.AutorizadaPor == id))
        {
            TempData["Error"] = $"{residente.NombreCompleto} tiene reservas o visitas registradas y no se puede eliminar.";
            return RedirectToAction(nameof(Index));
        }

        _db.Residentes.Remove(residente);
        await _db.SaveChangesAsync();
        TempData["Exito"] = $"Residente {residente.NombreCompleto} eliminado.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Torres y unidades de la torre seleccionada (dropdowns en cascada).</summary>
    private async Task CargarListasAsync(int? unidadId)
    {
        string? torre = null;
        if (unidadId is > 0)
            torre = await _db.Unidades.Where(u => u.Id == unidadId).Select(u => u.Torre).FirstOrDefaultAsync();

        var torres = await _db.Unidades.AsNoTracking().Select(u => u.Torre).Distinct().OrderBy(t => t).ToListAsync();
        ViewData["Torres"] = new SelectList(torres, torre);
        ViewData["TorreSeleccionada"] = torre;

        var unidades = torre is null
            ? new List<SelectListItem>()
            : await _db.Unidades.AsNoTracking()
                .Where(u => u.Torre == torre)
                .OrderBy(u => u.Numero)
                .Select(u => new SelectListItem(u.Torre + "-" + u.Numero, u.Id.ToString(), u.Id == unidadId))
                .ToListAsync();
        ViewData["Unidades"] = unidades;
    }

    private static void Normalizar(Residente r)
    {
        r.Nombres = (r.Nombres ?? string.Empty).Trim();
        r.Apellidos = (r.Apellidos ?? string.Empty).Trim();
        r.Cedula = (r.Cedula ?? string.Empty).Trim();
        r.Correo = (r.Correo ?? string.Empty).Trim().ToLowerInvariant();
        r.Telefono = string.IsNullOrWhiteSpace(r.Telefono) ? null : r.Telefono.Trim();
    }

    /// <summary>Validaciones en el servidor, antes de grabar en la base de datos.</summary>
    private async Task ValidarAsync(Residente r)
    {
        // Dato sensible: la cédula se valida con el algoritmo módulo 10 (no solo con JavaScript)
        var errorCedula = ValidadorCedula.Validar(r.Cedula);
        if (errorCedula is not null)
        {
            ModelState.Remove(nameof(Residente.Cedula));
            ModelState.AddModelError(nameof(Residente.Cedula), errorCedula);
        }
        else if (await _db.Residentes.AnyAsync(x => x.Id != r.Id && x.Cedula == r.Cedula))
        {
            ModelState.AddModelError(nameof(Residente.Cedula), "Ya existe un residente con esta cédula.");
        }

        if (r.UnidadId > 0 && !await _db.Unidades.AnyAsync(u => u.Id == r.UnidadId))
            ModelState.AddModelError(nameof(Residente.UnidadId), "La unidad seleccionada no existe.");

        if (r.FechaIngreso > DateTime.Today)
            ModelState.AddModelError(nameof(Residente.FechaIngreso), "La fecha de ingreso no puede ser futura.");
    }

    /// <summary>Cada unidad tiene un solo responsable de pago: el nuevo reemplaza al anterior.</summary>
    private async Task QuitarResponsableAnteriorAsync(Residente r)
    {
        if (!r.ResponsablePago) return;

        var anteriores = await _db.Residentes
            .Where(x => x.UnidadId == r.UnidadId && x.Id != r.Id && x.ResponsablePago)
            .ToListAsync();
        if (anteriores.Count == 0) return;

        foreach (var a in anteriores) a.ResponsablePago = false;
        await _db.SaveChangesAsync();   // primero se libera el índice único parcial
    }
}
