using Condominio360.Data;
using Condominio360.Helpers;
using Condominio360.Models;
using Condominio360.Services;
using Condominio360.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>
/// Parámetros de cobro del Core (D, G y t). Cada cambio crea una nueva versión con
/// fecha "vigente desde", de modo que los cálculos de meses anteriores no se alteran.
/// </summary>
[Authorize(Roles = Rol.Administrador)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ConfiguracionCobroController : Controller
{
    private readonly AppDbContext _db;
    private readonly CobranzaService _cobranza;

    public ConfiguracionCobroController(AppDbContext db, CobranzaService cobranza)
    {
        _db = db;
        _cobranza = cobranza;
    }

    // GET: /ConfiguracionCobro
    public async Task<IActionResult> Index()
    {
        var vm = new ConfiguracionCobroIndexViewModel
        {
            Vigente = await _cobranza.ConfiguracionVigenteAsync(DateTime.Today),
            Historial = await _db.ConfiguracionesCobro.AsNoTracking()
                .Include(c => c.ActualizadoPorUsuario)
                .OrderByDescending(c => c.VigenteDesde)
                .ToListAsync()
        };
        return View(vm);
    }

    // GET: /ConfiguracionCobro/Create
    public async Task<IActionResult> Create()
    {
        var vigente = await _cobranza.ConfiguracionVigenteAsync(DateTime.Today);
        var vm = new ConfiguracionCobroFormViewModel { VigenteDesde = DateTime.Today };
        if (vigente is not null)
        {
            vm.DiaVencimiento = vigente.DiaVencimiento;
            vm.DiasGracia = vigente.DiasGracia;
            vm.TasaPorcentaje = vigente.TasaRecargoMensual * 100m;
        }
        return View(vm);
    }

    // POST: /ConfiguracionCobro/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ConfiguracionCobroFormViewModel vm)
    {
        // Validaciones de servidor adicionales a los atributos [Range]
        if (Math.Round(vm.TasaPorcentaje, 2) != vm.TasaPorcentaje)
            ModelState.AddModelError(nameof(vm.TasaPorcentaje), "Use como máximo dos decimales (ej.: 1.25).");

        var ultima = await _db.ConfiguracionesCobro.AsNoTracking()
            .OrderByDescending(c => c.VigenteDesde).FirstOrDefaultAsync();
        if (ultima is not null && vm.VigenteDesde.Date <= ultima.VigenteDesde)
            ModelState.AddModelError(nameof(vm.VigenteDesde),
                $"Debe ser posterior a la última configuración ({ultima.VigenteDesde:dd/MM/yyyy}).");

        if (vm.VigenteDesde.Date > DateTime.Today.AddYears(1))
            ModelState.AddModelError(nameof(vm.VigenteDesde), "No puede ser mayor a un año desde hoy.");

        if (!ModelState.IsValid) return View(vm);

        _db.ConfiguracionesCobro.Add(new ConfiguracionCobro
        {
            DiaVencimiento = vm.DiaVencimiento,
            DiasGracia = vm.DiasGracia,
            TasaRecargoMensual = Math.Round(vm.TasaPorcentaje / 100m, 4),
            VigenteDesde = vm.VigenteDesde.Date,
            ActualizadoPor = User.IdUsuario()
        });
        await _db.SaveChangesAsync();

        TempData["Exito"] = $"Nueva configuración de cobro vigente desde {vm.VigenteDesde:dd/MM/yyyy}.";
        return RedirectToAction(nameof(Index));
    }
}
