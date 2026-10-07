using System.Security.Claims;
using Condominio360.Data;
using Condominio360.Models;
using Condominio360.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>Inicio y cierre de sesión. Es el único controlador de la zona privada que acepta anónimos.</summary>
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly ILogger<AccountController> _logger;

    public AccountController(AppDbContext db, IPasswordHasher<Usuario> hasher, ILogger<AccountController> logger)
    {
        _db = db;
        _hasher = hasher;
        _logger = logger;
    }

    // GET: /Account/Login?ReturnUrl=/Unidades
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Panel");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var nombre = model.NombreUsuario.Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombre);

        var credencialesValidas = usuario is not null &&
            _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, model.Password)
                != PasswordVerificationResult.Failed;

        if (!credencialesValidas)
        {
            // Mensaje genérico: no revela si el usuario existe
            _logger.LogWarning("Intento de inicio de sesión fallido para '{Usuario}'", nombre);
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
            model.Password = string.Empty;
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario!.Id.ToString()),
            new(ClaimTypes.Name, usuario.NombreUsuario),
            new("NombreCompleto", usuario.NombreCompleto),
            new(ClaimTypes.Role, usuario.Rol)
        };

        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad),
            new AuthenticationProperties
            {
                IsPersistent = model.Recordarme,
                ExpiresUtc = model.Recordarme ? DateTimeOffset.UtcNow.AddDays(7) : null
            });

        _logger.LogInformation("Usuario '{Usuario}' inició sesión", usuario.NombreUsuario);

        // Solo se redirige a URLs internas (evita "open redirect")
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Panel");
    }

    // POST: /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Exito"] = "Sesión cerrada correctamente.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
