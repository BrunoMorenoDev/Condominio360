using System.Text;
using Condominio360.Models;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Data;

/// <summary>
/// Mapeo de las entidades a las tablas del Diagrama Entidad-Relación en PostgreSQL (Supabase).
/// Las tablas usan snake_case (unidad_id, fecha_vencimiento...) y las clases PascalCase;
/// la conversión se hace automáticamente al final de <see cref="OnModelCreating"/>.
/// El esquema lo crean los scripts de /database: la app NO crea ni modifica tablas.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Unidad> Unidades => Set<Unidad>();
    public DbSet<Residente> Residentes => Set<Residente>();
    public DbSet<ConfiguracionCobro> ConfiguracionesCobro => Set<ConfiguracionCobro>();
    public DbSet<Cuota> Cuotas => Set<Cuota>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<AplicacionPago> AplicacionesPago => Set<AplicacionPago>();
    public DbSet<AreaComunal> AreasComunales => Set<AreaComunal>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Visita> Visitas => Set<Visita>();

    private const string Fecha = "date";
    private const string FechaHora = "timestamp without time zone";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rol>(e =>
        {
            e.ToTable("roles");
            e.HasIndex(r => r.Nombre).IsUnique();
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.HasIndex(u => u.NombreUsuario).IsUnique();
            e.Property(u => u.CreadoEn).HasColumnType(FechaHora);
            e.HasOne(u => u.Rol).WithMany(r => r.Usuarios)
             .HasForeignKey(u => u.RolId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(u => u.Residente).WithOne(r => r.Usuario)
             .HasForeignKey<Usuario>(u => u.ResidenteId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Unidad>(e =>
        {
            e.ToTable("unidades");
            e.HasIndex(u => new { u.Torre, u.Numero }).IsUnique();
            e.Property(u => u.Tipo).HasConversion<string>().HasMaxLength(20);
            e.Property(u => u.AreaM2).HasPrecision(8, 2);
            e.Property(u => u.Alicuota).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Residente>(e =>
        {
            e.ToTable("residentes");
            e.HasIndex(r => r.Cedula).IsUnique();
            e.Property(r => r.Cedula).HasColumnType("char(10)");
            e.Property(r => r.Tipo).HasConversion<string>().HasMaxLength(20);
            e.Property(r => r.FechaIngreso).HasColumnType(Fecha);

            // No se puede eliminar una unidad que todavía tiene residentes
            e.HasOne(r => r.Unidad).WithMany(u => u.Residentes)
             .HasForeignKey(r => r.UnidadId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConfiguracionCobro>(e =>
        {
            e.ToTable("configuracion_cobro");
            e.HasIndex(c => c.VigenteDesde).IsUnique();
            e.Property(c => c.TasaRecargoMensual).HasPrecision(5, 4);
            e.Property(c => c.VigenteDesde).HasColumnType(Fecha);
            e.HasOne(c => c.ActualizadoPorUsuario).WithMany()
             .HasForeignKey(c => c.ActualizadoPor).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cuota>(e =>
        {
            e.ToTable("cuotas");
            e.HasIndex(c => new { c.UnidadId, c.Anio, c.Mes }).IsUnique();
            e.Property(c => c.Monto).HasPrecision(10, 2);
            e.Property(c => c.CapitalPagado).HasPrecision(10, 2);
            e.Property(c => c.RecargoPagado).HasPrecision(10, 2);
            e.Property(c => c.FechaEmision).HasColumnType(Fecha);
            e.Property(c => c.FechaVencimiento).HasColumnType(Fecha);
            e.Property(c => c.Estado).HasConversion<string>().HasMaxLength(15);
            e.HasOne(c => c.Unidad).WithMany(u => u.Cuotas)
             .HasForeignKey(c => c.UnidadId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.GeneradaPorUsuario).WithMany()
             .HasForeignKey(c => c.GeneradaPor).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pago>(e =>
        {
            e.ToTable("pagos");
            e.Property(p => p.Monto).HasPrecision(10, 2);
            e.Property(p => p.FechaPago).HasColumnType(Fecha);
            e.Property(p => p.FechaVerificacion).HasColumnType(FechaHora);
            e.Property(p => p.Metodo).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.Estado).HasConversion<string>().HasMaxLength(15);
            e.Property(p => p.Referencia).HasMaxLength(40);
            e.Property(p => p.Comprobante).HasMaxLength(255);
            e.Property(p => p.Observacion).HasMaxLength(250);
            e.HasOne(p => p.Unidad).WithMany(u => u.Pagos)
             .HasForeignKey(p => p.UnidadId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Residente).WithMany()
             .HasForeignKey(p => p.ResidenteId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.RegistradoPorUsuario).WithMany()
             .HasForeignKey(p => p.RegistradoPor).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.VerificadoPorUsuario).WithMany()
             .HasForeignKey(p => p.VerificadoPor).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AplicacionPago>(e =>
        {
            e.ToTable("aplicaciones_pago");
            e.Property(a => a.MontoRecargo).HasPrecision(10, 2);
            e.Property(a => a.MontoCapital).HasPrecision(10, 2);
            e.HasOne(a => a.Pago).WithMany(p => p.Aplicaciones)
             .HasForeignKey(a => a.PagoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Cuota).WithMany(c => c.Aplicaciones)
             .HasForeignKey(a => a.CuotaId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AreaComunal>(e =>
        {
            e.ToTable("areas_comunales");
            e.Property(a => a.CostoUso).HasPrecision(8, 2);
        });

        modelBuilder.Entity<Reserva>(e =>
        {
            e.ToTable("reservas");
            e.Property(r => r.Fecha).HasColumnType(Fecha);
            e.Property(r => r.Estado).HasConversion<string>().HasMaxLength(15);
            e.HasOne(r => r.Area).WithMany(a => a.Reservas)
             .HasForeignKey(r => r.AreaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Residente).WithMany()
             .HasForeignKey(r => r.ResidenteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.AprobadaPorUsuario).WithMany()
             .HasForeignKey(r => r.AprobadaPor).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Visita>(e =>
        {
            e.ToTable("visitas");
            e.Property(v => v.FechaEsperada).HasColumnType(Fecha);
            e.Property(v => v.HoraIngreso).HasColumnType(FechaHora);
            e.Property(v => v.HoraSalida).HasColumnType(FechaHora);
            e.Property(v => v.Estado).HasConversion<string>().HasMaxLength(15);
            e.HasOne(v => v.Unidad).WithMany()
             .HasForeignKey(v => v.UnidadId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(v => v.AutorizadaPorResidente).WithMany()
             .HasForeignKey(v => v.AutorizadaPor).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(v => v.RegistradaPorUsuario).WithMany()
             .HasForeignKey(v => v.RegistradaPor).OnDelete(DeleteBehavior.Restrict);
        });

        // PascalCase (C#) → snake_case (PostgreSQL) para todas las columnas
        foreach (var entidad in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var propiedad in entidad.GetProperties())
                propiedad.SetColumnName(ASnakeCase(propiedad.Name));
        }
    }

    private static string ASnakeCase(string nombre)
    {
        var sb = new StringBuilder(nombre.Length + 8);
        for (var i = 0; i < nombre.Length; i++)
        {
            var c = nombre[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && (char.IsLower(nombre[i - 1]) || char.IsDigit(nombre[i - 1])))
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
