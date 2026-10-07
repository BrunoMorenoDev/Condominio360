using Condominio360.Models;
using Microsoft.AspNetCore.Identity;

namespace Condominio360.Data;

/// <summary>
/// Crea el usuario administrador y datos de ejemplo la primera vez que se ejecuta la app.
/// </summary>
public static class DbSeeder
{
    public static void Seed(AppDbContext db, IPasswordHasher<Usuario> hasher, IConfiguration config)
    {
        if (!db.Usuarios.Any())
        {
            var admin = new Usuario
            {
                NombreUsuario = (config["AdminSeed:Usuario"] ?? "admin").Trim().ToLowerInvariant(),
                NombreCompleto = config["AdminSeed:NombreCompleto"] ?? "Administrador General",
                Rol = "Administrador"
            };
            admin.PasswordHash = hasher.HashPassword(admin, config["AdminSeed:Password"] ?? "Admin123*");
            db.Usuarios.Add(admin);
        }

        if (!db.Unidades.Any())
        {
            var a101 = new Unidad { Torre = "A", Numero = "101", Piso = 1, Tipo = TipoUnidad.Departamento, AreaM2 = 85.50m, Alicuota = 72.00m };
            var a202 = new Unidad { Torre = "A", Numero = "202", Piso = 2, Tipo = TipoUnidad.Departamento, AreaM2 = 110.00m, Alicuota = 95.00m };
            var b301 = new Unidad { Torre = "B", Numero = "301", Piso = 3, Tipo = TipoUnidad.Departamento, AreaM2 = 64.30m, Alicuota = 58.50m };
            var l01  = new Unidad { Torre = "PB", Numero = "L-01", Piso = 0, Tipo = TipoUnidad.Local, AreaM2 = 40.00m, Alicuota = 45.00m };

            db.Unidades.AddRange(a101, a202, b301, l01);

            db.Residentes.AddRange(
                new Residente { Nombres = "María José", Apellidos = "Andrade Ruiz", Cedula = "1712345678", Correo = "mjandrade@correo.com", Telefono = "0991234567", Tipo = TipoResidente.Propietario, FechaIngreso = new DateTime(2022, 3, 15), Unidad = a101 },
                new Residente { Nombres = "Carlos", Apellidos = "Mena Vaca", Cedula = "1723456789", Correo = "cmena@correo.com", Telefono = "0987654321", Tipo = TipoResidente.Arrendatario, FechaIngreso = new DateTime(2024, 8, 1), Unidad = a202 },
                new Residente { Nombres = "Ana Lucía", Apellidos = "Torres Paz", Cedula = "1709876543", Correo = "altorres@correo.com", Tipo = TipoResidente.Propietario, FechaIngreso = new DateTime(2019, 11, 20), Unidad = b301 }
            );
        }

        db.SaveChanges();
    }
}
