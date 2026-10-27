using Condominio360.Data;
using Condominio360.Models;
using Condominio360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>
/// Endpoints JSON que alimentan los dropdowns en cascada (Torre → Unidad → Residente).
/// Así el usuario nunca escribe una clave foránea: siempre la elige de una lista.
/// </summary>
[Authorize(Roles = Rol.Administrador)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class CatalogosController : Controller
{
    private readonly AppDbContext _db;
    private readonly CobranzaService _cobranza;

    public CatalogosController(AppDbContext db, CobranzaService cobranza)
    {
        _db = db;
        _cobranza = cobranza;
    }

    // GET: /Catalogos/Unidades?torre=A&soloActivas=true
    [HttpGet]
    public async Task<IActionResult> Unidades(string? torre, bool soloActivas = false)
    {
        if (string.IsNullOrWhiteSpace(torre)) return Json(Array.Empty<object>());

        var unidades = await _db.Unidades.AsNoTracking()
            .Where(u => u.Torre == torre && (!soloActivas || u.Activa))
            .OrderBy(u => u.Numero)
            .Select(u => new { id = u.Id, texto = u.Torre + "-" + u.Numero + (u.Activa ? "" : " (inactiva)") })
            .ToListAsync();

        return Json(unidades);
    }

    // GET: /Catalogos/Residentes?unidadId=4
    [HttpGet]
    public async Task<IActionResult> Residentes(int unidadId)
    {
        var residentes = await _db.Residentes.AsNoTracking()
            .Where(r => r.UnidadId == unidadId)
            .OrderByDescending(r => r.ResponsablePago).ThenBy(r => r.Apellidos)
            .Select(r => new
            {
                id = r.Id,
                texto = r.Nombres + " " + r.Apellidos + (r.ResponsablePago ? " · responsable de pago" : ""),
                responsable = r.ResponsablePago
            })
            .ToListAsync();

        return Json(residentes);
    }

    // GET: /Catalogos/SaldoUnidad?unidadId=4&fecha=2026-10-27  (informativo; el servidor vuelve a validar al guardar)
    [HttpGet]
    public async Task<IActionResult> SaldoUnidad(int unidadId, DateTime? fecha)
    {
        var saldo = await _cobranza.SaldoUnidadAsync(unidadId, (fecha ?? DateTime.Today).Date);
        return Json(new { saldo, texto = saldo.ToString("C") });
    }
}
