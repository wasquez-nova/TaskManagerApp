using Microsoft.EntityFrameworkCore;
using TaskManagerApp.Modeller;

namespace TaskManagerApp.Veri
{
    public class UygulamaDbBaglami : DbContext
    {
        public UygulamaDbBaglami(DbContextOptions<UygulamaDbBaglami> secenekler) : base(secenekler)
        {
        }

        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<Proje> Projeler { get; set; }
        public DbSet<Gorev> Gorevler { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Proje>()
                .HasOne(p => p.Kullanici)
                .WithMany(k => k.Projeler)
                .HasForeignKey(p => p.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade); // Kullanıcı silinirse projeleri de silinsin

             modelBuilder.Entity<Gorev>()
                .HasOne(g => g.Proje)
                .WithMany(p => p.Gorevler)
                .HasForeignKey(g => g.ProjeId)
                .OnDelete(DeleteBehavior.Cascade); // Proje silinirse görevleri de silinsin

                base.OnModelCreating(modelBuilder);
        }
    }
}
