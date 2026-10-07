using Condominio360.Data;
using Condominio360.Models;
using Condominio360.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>Resumen del condominio. Sección protegida.</summary>
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PanelController : Controller
{
    private readonly AppDbContext _db;

    public PanelController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        // SQLite no suma decimales en el servidor: se suman en memoria
        var alicuotas = await _db.Unidades.Select(u => u.Alicuota).ToListAsync();

        var vm = new PanelViewModel
        {
            NombreUsuario = User.FindFirst("NombreCompleto")?.Value ?? User.Identity?.Name ?? "",
            TotalUnidades = alicuotas.Count,
            UnidadesOcupadas = await _db.Unidades.CountAsync(u => u.Residentes.Any()),
            TotalResidentes = await _db.Residentes.CountAsync(),
            Propietarios = await _db.Residentes.CountAsync(r => r.Tipo == TipoResidente.Propietario),
            Arrendatarios = await _db.Residentes.CountAsync(r => r.Tipo == TipoResidente.Arrendatario),
            AlicuotaMensualTotal = alicuotas.Sum()
        };

        return View(vm);
    }
}
