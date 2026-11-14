using Condominio360.Data;
using Condominio360.Helpers;
using Condominio360.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>
/// Base de los módulos del rol Residente. Cada consulta se limita a la unidad del residente
/// vinculado a la cuenta: el identificador de la unidad NUNCA se toma de la URL ni del formulario,
/// así un residente no puede ver ni modificar datos de otra unidad.
/// </summary>
[Authorize(Roles = Rol.Residente)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public abstract class ResidenteBaseController : Controller
{
    protected readonly AppDbContext Db;

    protected ResidenteBaseController(AppDbContext db) => Db = db;

    /// <summary>Residente vinculado al usuario de la sesión (con su unidad), o null si la cuenta no tiene vínculo.</summary>
    protected async Task<Residente?> ResidenteActualAsync()
    {
        var idUsuario = User.IdUsuario();
        var residenteId = await Db.Usuarios.AsNoTracking()
            .Where(u => u.Id == idUsuario && u.Activo)
            .Select(u => u.ResidenteId)
            .FirstOrDefaultAsync();
        if (residenteId is null) return null;

        return await Db.Residentes.AsNoTracking()
            .Include(r => r.Unidad)
            .FirstOrDefaultAsync(r => r.Id == residenteId);
    }

    /// <summary>Página que se muestra cuando la cuenta de residente no está vinculada a un registro de residente.</summary>
    protected IActionResult SinVinculo() => View("~/Views/Shared/SinVinculo.cshtml");
}
