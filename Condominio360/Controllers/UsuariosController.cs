using Condominio360.Data;
using Condominio360.Helpers;
using Condominio360.Models;
using Condominio360.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>
/// Gestión de usuarios del sistema: consultar, crear, modificar y desactivar.
/// Un usuario con rol Residente se vincula a su registro de residente. Los usuarios no se
/// eliminan (forman parte de la auditoría de pagos, cuotas y visitas): se desactivan.
/// </summary>
[Authorize(Roles = Rol.Administrador)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class UsuariosController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<Usuario> _hasher;

    public UsuariosController(AppDbContext db, IPasswordHasher<Usuario> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    // GET: /Usuarios?buscar=ana&rolId=2&activo=true
    public async Task<IActionResult> Index(string? buscar, int? rolId, bool? activo)
    {
        IQueryable<Usuario> consulta = _db.Usuarios.AsNoTracking()
            .Include(u => u.Rol)
            .Include(u => u.Residente).ThenInclude(r => r!.Unidad);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var patron = $"%{buscar.Trim()}%";
            consulta = consulta.Where(u =>
                EF.Functions.ILike(u.NombreUsuario, patron) ||
                EF.Functions.ILike(u.NombreCompleto, patron) ||
                EF.Functions.ILike(u.Correo, patron));
        }
        if (rolId is > 0) consulta = consulta.Where(u => u.RolId == rolId);
        if (activo is not null) consulta = consulta.Where(u => u.Activo == activo);

        return View(new UsuariosIndexViewModel
        {
            Buscar = buscar,
            RolId = rolId,
            Activo = activo,
            Usuarios = await consulta.OrderBy(u => u.Rol!.Nombre).ThenBy(u => u.NombreCompleto).ToListAsync(),
            Roles = await _db.Roles.AsNoTracking().OrderBy(r => r.Id).ToListAsync()
        });
    }

    // GET: /Usuarios/Create
    public async Task<IActionResult> Create()
    {
        var vm = new UsuarioFormViewModel();
        await CargarListasAsync(vm);
        return View(vm);
    }

    // POST: /Usuarios/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsuarioFormViewModel vm)
    {
        Normalizar(vm);
        await ValidarAsync(vm, usuarioActual: null);

        if (!ModelState.IsValid)
        {
            LimpiarPasswords(vm);
            await CargarListasAsync(vm);
            return View(vm);
        }

        var usuario = new Usuario
        {
            NombreUsuario = vm.NombreUsuario,
            NombreCompleto = vm.NombreCompleto,
            Correo = vm.Correo,
            RolId = vm.RolId,
            ResidenteId = vm.ResidenteId,
            Activo = vm.Activo,
            CreadoEn = DateTime.Now
        };
        usuario.PasswordHash = _hasher.HashPassword(usuario, vm.Password!);

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        TempData["Exito"] = $"Usuario «{usuario.NombreUsuario}» creado.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Usuarios/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var u = await _db.Usuarios.AsNoTracking().Include(x => x.Residente).ThenInclude(r => r!.Unidad)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return NotFound();

        var vm = new UsuarioFormViewModel
        {
            Id = u.Id,
            NombreUsuario = u.NombreUsuario,
            NombreCompleto = u.NombreCompleto,
            Correo = u.Correo,
            RolId = u.RolId,
            Activo = u.Activo,
            ResidenteId = u.ResidenteId,
            UnidadId = u.Residente?.UnidadId,
            Torre = u.Residente?.Unidad?.Torre
        };
        await CargarListasAsync(vm);
        return View(vm);
    }

    // POST: /Usuarios/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UsuarioFormViewModel vm)
    {
        if (id != vm.Id) return NotFound();
        var usuario = await _db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id);
        if (usuario is null) return NotFound();

        Normalizar(vm);
        await ValidarAsync(vm, usuario);

        if (!ModelState.IsValid)
        {
            LimpiarPasswords(vm);
            await CargarListasAsync(vm);
            return View(vm);
        }

        usuario.NombreUsuario = vm.NombreUsuario;
        usuario.NombreCompleto = vm.NombreCompleto;
        usuario.Correo = vm.Correo;
        usuario.RolId = vm.RolId;
        usuario.ResidenteId = vm.ResidenteId;
        usuario.Activo = vm.Activo;
        if (!string.IsNullOrEmpty(vm.Password))
            usuario.PasswordHash = _hasher.HashPassword(usuario, vm.Password);

        await _db.SaveChangesAsync();

        TempData["Exito"] = $"Usuario «{usuario.NombreUsuario}» actualizado." +
                            (string.IsNullOrEmpty(vm.Password) ? "" : " Se cambió la contraseña.");
        return RedirectToAction(nameof(Index));
    }

    // POST: /Usuarios/CambiarEstado/5  (activar / desactivar)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var usuario = await _db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id);
        if (usuario is null) return NotFound();

        if (usuario.Activo)
        {
            if (usuario.Id == User.IdUsuario())
            {
                TempData["Error"] = "No puede desactivar su propia cuenta.";
                return RedirectToAction(nameof(Index));
            }
            if (usuario.TieneRol(Rol.Administrador) && !await HayOtroAdministradorActivoAsync(usuario.Id))
            {
                TempData["Error"] = "Debe quedar al menos un administrador activo.";
                return RedirectToAction(nameof(Index));
            }
        }
        else if (usuario.TieneRol(Rol.Residente) && usuario.ResidenteId is null)
        {
            TempData["Error"] = "Antes de activar este usuario vincúlelo a un residente (Editar).";
            return RedirectToAction(nameof(Index));
        }

        usuario.Activo = !usuario.Activo;
        await _db.SaveChangesAsync();
        TempData["Exito"] = $"Usuario «{usuario.NombreUsuario}» {(usuario.Activo ? "activado" : "desactivado")}." +
                            (usuario.Activo ? "" : " Su sesión se cerrará en la próxima solicitud.");
        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------------

    private static void Normalizar(UsuarioFormViewModel vm)
    {
        vm.NombreUsuario = (vm.NombreUsuario ?? string.Empty).Trim().ToLowerInvariant();
        vm.NombreCompleto = (vm.NombreCompleto ?? string.Empty).Trim();
        vm.Correo = (vm.Correo ?? string.Empty).Trim().ToLowerInvariant();
        vm.Torre = string.IsNullOrWhiteSpace(vm.Torre) ? null : vm.Torre.Trim();
        if (vm.UnidadId is <= 0) vm.UnidadId = null;
        if (vm.ResidenteId is <= 0) vm.ResidenteId = null;
    }

    private static void LimpiarPasswords(UsuarioFormViewModel vm)
    {
        vm.Password = null;
        vm.ConfirmarPassword = null;
    }

    /// <summary>Validaciones de servidor: unicidad, rol, vínculo con residente, contraseña y auto-protección.</summary>
    private async Task ValidarAsync(UsuarioFormViewModel vm, Usuario? usuarioActual)
    {
        // 1. Nombre de usuario único (se guarda en minúsculas)
        if (!string.IsNullOrEmpty(vm.NombreUsuario) &&
            await _db.Usuarios.AnyAsync(u => u.Id != vm.Id && u.NombreUsuario == vm.NombreUsuario))
            ModelState.AddModelError(nameof(vm.NombreUsuario), "Ya existe un usuario con ese nombre.");

        // 2. Rol existente
        var rol = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == vm.RolId);
        if (rol is null)
        {
            ModelState.AddModelError(nameof(vm.RolId), "Seleccione un rol válido.");
        }
        else if (rol.Nombre == Rol.Residente)
        {
            // 3. Un Residente se vincula a un residente que no tenga otra cuenta
            if (vm.ResidenteId is null)
                ModelState.AddModelError(nameof(vm.ResidenteId), "Un usuario con rol Residente debe vincularse a un residente.");
            else
            {
                var residente = await _db.Residentes.AsNoTracking().Include(r => r.Unidad)
                    .FirstOrDefaultAsync(r => r.Id == vm.ResidenteId);
                if (residente is null)
                    ModelState.AddModelError(nameof(vm.ResidenteId), "El residente seleccionado no existe.");
                else if (vm.UnidadId is not null && residente.UnidadId != vm.UnidadId)
                    ModelState.AddModelError(nameof(vm.ResidenteId), "El residente no pertenece a la unidad seleccionada.");
                else if (await _db.Usuarios.AnyAsync(u => u.Id != vm.Id && u.ResidenteId == vm.ResidenteId))
                    ModelState.AddModelError(nameof(vm.ResidenteId), $"{residente.NombreCompleto} ya tiene otra cuenta de usuario.");
            }
        }
        else
        {
            // Administradores y guardias no se vinculan a un residente
            vm.ResidenteId = null;
        }

        // 4. Contraseña: obligatoria al crear; opcional al editar (vacía = no cambia)
        if (usuarioActual is null || !string.IsNullOrEmpty(vm.Password))
        {
            var error = ValidarPassword(vm.Password);
            if (error is not null) ModelState.AddModelError(nameof(vm.Password), error);
            else if (vm.Password != vm.ConfirmarPassword)
                ModelState.AddModelError(nameof(vm.ConfirmarPassword), "Las contraseñas no coinciden.");
        }

        // 5. Auto-protección: el administrador no puede quitarse el rol ni desactivarse a sí mismo
        if (usuarioActual is not null)
        {
            var esUnoMismo = usuarioActual.Id == User.IdUsuario();
            var dejaDeSerAdminActivo = usuarioActual.TieneRol(Rol.Administrador) && usuarioActual.Activo &&
                                       (rol?.Nombre != Rol.Administrador || !vm.Activo);
            if (esUnoMismo && dejaDeSerAdminActivo)
                ModelState.AddModelError(string.Empty, "No puede quitarse el rol de Administrador ni desactivar su propia cuenta.");
            else if (dejaDeSerAdminActivo && !await HayOtroAdministradorActivoAsync(usuarioActual.Id))
                ModelState.AddModelError(string.Empty, "Debe quedar al menos un administrador activo.");
        }
    }

    /// <summary>Política de contraseñas: 8 a 64 caracteres con mayúscula, minúscula, número y símbolo.</summary>
    private static string? ValidarPassword(string? password)
    {
        if (string.IsNullOrEmpty(password)) return "Ingrese una contraseña.";
        if (password.Length is < 8 or > 64) return "La contraseña debe tener entre 8 y 64 caracteres.";
        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) ||
            password.All(char.IsLetterOrDigit))
            return "La contraseña debe incluir mayúsculas, minúsculas, números y al menos un símbolo.";
        return null;
    }

    private Task<bool> HayOtroAdministradorActivoAsync(int excluirId) =>
        _db.Usuarios.AnyAsync(u => u.Id != excluirId && u.Activo && u.Rol!.Nombre == Rol.Administrador);

    /// <summary>Roles y dropdowns en cascada Torre → Unidad → Residente para el vínculo.</summary>
    private async Task CargarListasAsync(UsuarioFormViewModel vm)
    {
        vm.Roles = await _db.Roles.AsNoTracking().OrderBy(r => r.Id)
            .Select(r => new SelectListItem(r.Nombre, r.Id.ToString(), r.Id == vm.RolId))
            .ToListAsync();

        var torres = await _db.Unidades.AsNoTracking().Select(u => u.Torre).Distinct().OrderBy(t => t).ToListAsync();
        vm.Torres = torres.Select(t => new SelectListItem(t, t, t == vm.Torre));

        vm.Unidades = string.IsNullOrEmpty(vm.Torre)
            ? Enumerable.Empty<SelectListItem>()
            : await _db.Unidades.AsNoTracking()
                .Where(u => u.Torre == vm.Torre)
                .OrderBy(u => u.Numero)
                .Select(u => new SelectListItem(u.Torre + "-" + u.Numero, u.Id.ToString(), u.Id == vm.UnidadId))
                .ToListAsync();

        vm.Residentes = vm.UnidadId is null
            ? Enumerable.Empty<SelectListItem>()
            : await _db.Residentes.AsNoTracking()
                .Where(r => r.UnidadId == vm.UnidadId)
                .OrderBy(r => r.Apellidos)
                .Select(r => new SelectListItem(
                    r.Nombres + " " + r.Apellidos + (r.Usuario != null && r.Usuario.Id != vm.Id ? " · ya tiene cuenta" : ""),
                    r.Id.ToString(), r.Id == vm.ResidenteId))
                .ToListAsync();
    }
}
