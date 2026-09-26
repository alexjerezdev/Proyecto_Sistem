using Microsoft.EntityFrameworkCore;
using CafeAroma.Api.Models;
<<<<<<< HEAD
=======

>>>>>>> origin/feature/backend-bd
namespace CafeAroma.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
            
        }

<<<<<<< HEAD
             public DbSet<Usuario> Usuarios { get; set; }
             public DbSet<HistoricoPrecio> HistoricosPrecio { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
{
            modelBuilder.Entity<Usuario>().ToTable("usuario");
            modelBuilder.Entity<Usuario>().Property(u => u.UsuarioId).HasColumnName("usuario_id");
            modelBuilder.Entity<Usuario>().Property(u => u.Nombre).HasColumnName("nombre");
            modelBuilder.Entity<Usuario>().Property(u => u.Rol).HasColumnName("rol");
            modelBuilder.Entity<Usuario>().Property(u => u.ContrasenaHash).HasColumnName("contrasena_hash");
            modelBuilder.Entity<Usuario>().Property(u => u.Estado).HasColumnName("estado");

            modelBuilder.Entity<HistoricoPrecio>().ToTable("historico_precio");
modelBuilder.Entity<HistoricoPrecio>().Property(h => h.HistoricoPrecioId).HasColumnName("historico_precio_id");
modelBuilder.Entity<HistoricoPrecio>().Property(h => h.ProductoId).HasColumnName("producto_id");
modelBuilder.Entity<HistoricoPrecio>().Property(h => h.PrecioAnterior).HasColumnName("precio_anterior");
modelBuilder.Entity<HistoricoPrecio>().Property(h => h.PrecioNuevo).HasColumnName("precio_nuevo");
modelBuilder.Entity<HistoricoPrecio>().Property(h => h.UsuarioId).HasColumnName("usuario_id");
modelBuilder.Entity<HistoricoPrecio>().Property(h => h.Fecha).HasColumnName("fecha");
}

   
=======
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
>>>>>>> origin/feature/backend-bd
    }
}