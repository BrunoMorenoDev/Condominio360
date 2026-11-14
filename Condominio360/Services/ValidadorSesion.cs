using System.Security.Claims;
using Condominio360.Data;
using Condominio360.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Services;

/// <summary>
/// Revalida la cookie de sesión en cada solicitud: si el administrador desactivó al usuario,
/// le cambió el rol o el residente vinculado, la sesión se cierra de inmediato
/// (no hay que esperar a que la cookie expire).
/// </summary>
public static class ValidadorSesion
{
    public static async Task ValidarAsync(CookieValidatePrincipalContext contexto)
    {
        var principal = contexto.Principal;
        var id = principal?.IdUsuario() ?? 0;

        var db = contexto.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var actual = id == 0 ? null : await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new { u.Activo, Rol = u.Rol!.Nombre, u.ResidenteId })
            .FirstOrDefaultAsync();

        var rolCookie = principal?.FindFirstValue(ClaimTypes.Role);
        var residenteCookie = principal?.FindFirstValue("ResidenteId");

        if (actual is null || !actual.Activo || actual.Rol != rolCookie || actual.ResidenteId?.ToString() != residenteCookie)
        {
            contexto.RejectPrincipal();
            await contexto.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
