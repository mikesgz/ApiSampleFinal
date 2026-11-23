using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<Milk> Milks { get; set; }
        public DbSet<Cow> Cows { get; set; }
        public DbSet<ConfiguracionGranja> ConfiguracionesGranja => Set<ConfiguracionGranja>();
        public DbSet<ConfiguracionProducto> ConfiguracionesProducto => Set<ConfiguracionProducto>();
        public DbSet<ProduccionVaca> ProduccionesVaca => Set<ProduccionVaca>();
        public DbSet<TransformacionLeche> TransformacionesLeche => Set<TransformacionLeche>();
        public DbSet<LoteProducto> LotesProducto => Set<LoteProducto>();
        public DbSet<Venta> Ventas => Set<Venta>();
        public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConfiguracionGranja>().HasKey(x => x.Id);
            modelBuilder.Entity<ConfiguracionGranja>().Property(x => x.Id).ValueGeneratedNever();
            modelBuilder.Entity<ConfiguracionProducto>().HasKey(x => x.Tipo);
            modelBuilder.Entity<ConfiguracionProducto>().Property(x => x.Tipo).HasConversion<int>();
            modelBuilder.Entity<ProduccionVaca>().HasIndex(x => new { x.VacaId, x.Fecha }).IsUnique();
            modelBuilder.Entity<ProduccionVaca>().HasOne(x => x.Vaca).WithMany().HasForeignKey(x => x.VacaId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TransformacionLeche>().Property(x => x.Tipo).HasConversion<int>();
            modelBuilder.Entity<TransformacionLeche>().HasIndex(x => x.Fecha);
            modelBuilder.Entity<LoteProducto>().Property(x => x.Tipo).HasConversion<int>();
            modelBuilder.Entity<LoteProducto>().HasIndex(x => new { x.Tipo, x.FechaProduccion });
            modelBuilder.Entity<Venta>().Property(x => x.Tipo).HasConversion<int>();
            modelBuilder.Entity<Venta>().HasMany(x => x.Detalles).WithOne().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<DetalleVenta>().HasOne(x => x.Lote).WithMany().HasForeignKey(x => x.LoteId).OnDelete(DeleteBehavior.Restrict);
            base.OnModelCreating(modelBuilder);
        }

    }

}
