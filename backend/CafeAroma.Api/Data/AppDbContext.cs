using Microsoft.EntityFrameworkCore;
using CafeAroma.Api.Models;

namespace CafeAroma.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<CierreCaja> CierresCaja { get; set; } 

        public DbSet<Insumo> Insumos { get; set; }
        public DbSet<ProductoInsumo> ProductoInsumos { get; set; }
        public DbSet<MovimientoInventario> MovimientosInventario { get; set; }
        public DbSet<AlertaStock> AlertasStock { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProductoInsumo>()
                .HasKey(pi => new { pi.ProductoId, pi.InsumoId });
        }
    }
}