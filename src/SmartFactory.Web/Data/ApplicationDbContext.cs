using Microsoft.EntityFrameworkCore;
using SmartFactory.Web.Models;

namespace SmartFactory.Web.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<Machine> Machines => Set<Machine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Machine>(entity =>
        {
            entity.HasIndex(machine => machine.Code).IsUnique();
            entity.Property(machine => machine.Code).HasMaxLength(30);
            entity.Property(machine => machine.Name).HasMaxLength(100);
            entity.Property(machine => machine.MachineCategory).HasMaxLength(50);
            entity.Property(machine => machine.Manufacturer).HasMaxLength(100);
            entity.Property(machine => machine.Model).HasMaxLength(100);
        });
    }
}
