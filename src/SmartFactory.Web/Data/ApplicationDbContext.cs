using Microsoft.EntityFrameworkCore;
using SmartFactory.Web.Models;

namespace SmartFactory.Web.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<Maintenance> Maintenances => Set<Maintenance>();
    public DbSet<PredictionHistory> PredictionHistories => Set<PredictionHistory>();

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

        modelBuilder.Entity<Maintenance>(entity =>
        {
            entity.Property(m => m.Title).HasMaxLength(120);
            entity.Property(m => m.Description).HasMaxLength(1000);
            entity.Property(m => m.PerformedBy).HasMaxLength(100);
            entity.Property(m => m.Cost).HasPrecision(18, 2);

            entity.HasOne(m => m.Machine)
                .WithMany(machine => machine.Maintenances)
                .HasForeignKey(m => m.MachineId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(m => m.MachineId);
            entity.HasIndex(m => m.ScheduledDate);
            entity.HasIndex(m => m.Status);
        });

        modelBuilder.Entity<PredictionHistory>(entity =>
        {
            entity.Property(p => p.RiskLevel).HasMaxLength(30);
            entity.Property(p => p.Recommendation).HasMaxLength(500);

            entity.HasOne(p => p.Machine)
                .WithMany(machine => machine.PredictionHistories)
                .HasForeignKey(p => p.MachineId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(p => p.MachineId);
            entity.HasIndex(p => p.PredictedAt);
            entity.HasIndex(p => p.RiskLevel);
        });
    }
}
