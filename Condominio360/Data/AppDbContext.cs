using Condominio360.Models;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Unidad> Unidades => Set<Unidad>();
    public DbSet<Residente> Residentes => Set<Residente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.HasIndex(u => u.NombreUsuario).IsUnique();
        });

        modelBuilder.Entity<Unidad>(e =>
        {
            e.HasIndex(u => new { u.Torre, u.Numero }).IsUnique();
            e.Property(u => u.Tipo).HasConversion<string>().HasMaxLength(20);
            e.Property(u => u.AreaM2).HasPrecision(8, 2);
            e.Property(u => u.Alicuota).HasPrecision(10, 2);
            e.Ignore(u => u.Codigo);
        });

        modelBuilder.Entity<Residente>(e =>
        {
            e.HasIndex(r => r.Cedula).IsUnique();
            e.Property(r => r.Tipo).HasConversion<string>().HasMaxLength(20);

            // No se puede eliminar una unidad que todavía tiene residentes
            e.HasOne(r => r.Unidad)
             .WithMany(u => u.Residentes)
             .HasForeignKey(r => r.UnidadId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
