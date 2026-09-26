using Microsoft.EntityFrameworkCore;
using CafeAroma.Api.Models;
namespace CafeAroma.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
            
        }

             public DbSet<Usuario> Usuarios { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
{
            modelBuilder.Entity<Usuario>().ToTable("usuario");
            modelBuilder.Entity<Usuario>().Property(u => u.UsuarioId).HasColumnName("usuario_id");
            modelBuilder.Entity<Usuario>().Property(u => u.Nombre).HasColumnName("nombre");
            modelBuilder.Entity<Usuario>().Property(u => u.Rol).HasColumnName("rol");
            modelBuilder.Entity<Usuario>().Property(u => u.ContrasenaHash).HasColumnName("contrasena_hash");
            modelBuilder.Entity<Usuario>().Property(u => u.Estado).HasColumnName("estado");
}

   
    }
}