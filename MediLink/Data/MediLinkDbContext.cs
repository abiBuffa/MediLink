using MediLink.Models;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Data;

public class MediLinkDbContext : DbContext
{
    public MediLinkDbContext(DbContextOptions<MediLinkDbContext> options) : base(options) { }

    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Medico> Medicos => Set<Medico>();
    public DbSet<Consulta> Consultas => Set<Consulta>();
    public DbSet<Receta> Recetas => Set<Receta>();
    public DbSet<Insumo> Insumos => Set<Insumo>();
    public DbSet<PedidoReposicion> PedidosReposicion => Set<PedidoReposicion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Paciente>().HasIndex(p => p.Documento).IsUnique();
        modelBuilder.Entity<Medico>().HasIndex(m => m.Matricula).IsUnique();
        modelBuilder.Entity<Receta>().HasIndex(r => r.Codigo).IsUnique();
        modelBuilder.Entity<Receta>().Property(r => r.Estado).HasConversion<string>();
        modelBuilder.Entity<PedidoReposicion>().Property(p => p.Estado).HasConversion<string>();
    }
}