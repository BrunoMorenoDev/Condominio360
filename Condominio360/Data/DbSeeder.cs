using Condominio360.Models;
using Microsoft.AspNetCore.Identity;

namespace Condominio360.Data;

/// <summary>
/// Garantiza los datos mínimos para poder usar el sistema en una base vacía:
/// los tres roles, el usuario administrador y una configuración de cobro.
/// Los datos de ejemplo completos están en database/03_datos_ejemplo.sql.
/// </summary>
public static class DbSeeder
{
    public static void Seed(AppDbContext db, IPasswordHasher<Usuario> hasher, IConfiguration config)
    {
        if (!db.Roles.Any())
        {
            db.Roles.AddRange(
                new Rol { Nombre = Rol.Administrador, Descripcion = "Gestiona unidades, residentes, cobranza, áreas comunales y usuarios" },
                new Rol { Nombre = Rol.Residente, Descripcion = "Consulta su estado de cuenta, reporta pagos, reserva áreas y autoriza visitas" },
                new Rol { Nombre = Rol.Guardia, Descripcion = "Controla el ingreso y salida de visitas en la garita" });
            db.SaveChanges();
        }

        if (!db.Usuarios.Any())
        {
            var rolAdmin = db.Roles.First(r => r.Nombre == Rol.Administrador);
            var admin = new Usuario
            {
                NombreUsuario = (config["AdminSeed:Usuario"] ?? "admin").Trim().ToLowerInvariant(),
                NombreCompleto = config["AdminSeed:NombreCompleto"] ?? "Administrador General",
                Correo = config["AdminSeed:Correo"] ?? "admin@condominio360.ec",
                RolId = rolAdmin.Id,
                Activo = true,
                CreadoEn = DateTime.Now
            };
            admin.PasswordHash = hasher.HashPassword(admin, config["AdminSeed:Password"] ?? "Admin123*");
            db.Usuarios.Add(admin);
            db.SaveChanges();
        }

        if (!db.ConfiguracionesCobro.Any())
        {
            var adminId = db.Usuarios.OrderBy(u => u.Id).Select(u => u.Id).First();
            db.ConfiguracionesCobro.Add(new ConfiguracionCobro
            {
                DiaVencimiento = 10,
                DiasGracia = 5,
                TasaRecargoMensual = 0.015m,
                VigenteDesde = new DateTime(DateTime.Today.Year, 1, 1),
                ActualizadoPor = adminId
            });
            db.SaveChanges();
        }
    }
}
