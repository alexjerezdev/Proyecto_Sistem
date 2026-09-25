using Microsoft.EntityFrameworkCore;

namespace CafeAroma.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }
    }
}