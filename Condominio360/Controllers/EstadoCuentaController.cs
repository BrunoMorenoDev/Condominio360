using Condominio360.Data;
using Condominio360.Models;
using Condominio360.Services;
using Condominio360.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>Estado de cuenta de una unidad a una fecha de corte (wireframe, Figura 2).</summary>
[Authorize(Roles = Rol.Administrador)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class EstadoCuentaController : Controller
{
    private readonly AppDbContext _db;
    private readonly CobranzaService _cobranza;

    public EstadoCuentaController(AppDbContext db, CobranzaService cobranza)
    {
        _db = db;
        _cobranza = cobranza;
    }

    // GET: /EstadoCuenta?torre=A&unidadId=3&corte=2026-10-27
    public async Task<IActionResult> Index(string? torre, int? unidadId, DateTime? corte)
    {
        var fecha = (corte ?? DateTime.Today).Date;
        if (fecha > DateTime.Today.AddYears(1)) fecha = DateTime.Today;

        EstadoCuentaViewModel vm;
        if (unidadId is > 0 && await _cobranza.EstadoCuentaAsync(unidadId.Value, fecha) is { } estado)
        {
            vm = estado;
        }
        else
        {
            vm = new EstadoCuentaViewModel { Corte = fecha, Torre = torre };
        }

        var torres = await _db.Unidades.AsNoTracking().Select(u => u.Torre).Distinct().OrderBy(t => t).ToListAsync();
        vm.Torres = torres.Select(t => new SelectListItem(t, t, t == vm.Torre));
        vm.Unidades = string.IsNullOrEmpty(vm.Torre)
            ? Enumerable.Empty<SelectListItem>()
            : await _db.Unidades.AsNoTracking()
                .Where(u => u.Torre == vm.Torre)
                .OrderBy(u => u.Numero)
                .Select(u => new SelectListItem(u.Torre + "-" + u.Numero, u.Id.ToString(), u.Id == vm.UnidadId))
                .ToListAsync();

        return View(vm);
    }
}
