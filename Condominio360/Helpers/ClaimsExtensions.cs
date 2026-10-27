using System.Security.Claims;

namespace Condominio360.Helpers;

public static class ClaimsExtensions
{
    /// <summary>Id del usuario autenticado (claim NameIdentifier).</summary>
    public static int IdUsuario(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
