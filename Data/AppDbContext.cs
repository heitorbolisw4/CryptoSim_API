using Crypto.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Crypto.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            
        }
        public DbSet<Usuario> Usuarios {get; set;}
        public DbSet<Moeda> Moedas { get; set; }
        public DbSet<Carteira> Carteiras { get; set; }
        public DbSet<SaldoCripto> SaldoCriptos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>(entity =>
            {
               // definir id como pk
               entity.HasKey(u => u.Id);

               entity.HasIndex(u => u.Email).IsUnique();

               entity.Property(u => u.Email).IsRequired().HasMaxLength(150);

               entity.Property(u => u.SenhaHash).IsRequired();

               entity.Property(u => u.Cpf).IsRequired().HasMaxLength(11);

               entity.Property(u => u.Ativo).IsRequired();

            });



            modelBuilder.Entity<Carteira>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.HasIndex(c => c.UsuarioId).IsUnique();

                entity.Property(c => c.UsuarioId).IsRequired(); 

                entity.HasOne(c => c.Usuario).WithOne(c => c.Carteira).HasForeignKey<Carteira>(c => c.UsuarioId);

            });

            modelBuilder.Entity<Moeda>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.HasIndex(m => m.Simbolo).IsUnique();

                entity.Property(m => m.Nome).HasMaxLength(150);

                entity.Property(m => m.Ativo).IsRequired();
            });

            modelBuilder.Entity<SaldoCripto>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.HasOne(s => s.Carteira).WithMany(s => s.Saldos).HasForeignKey(s => s.CarteiraId);

                entity.HasOne(s => s.Moeda).WithOne().HasForeignKey<SaldoCripto>(s => s.MoedaId);
                
            });

            
        }
    }
}