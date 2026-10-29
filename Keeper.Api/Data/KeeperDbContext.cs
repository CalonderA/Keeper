using Keeper.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Keeper.Api.Data;

public class KeeperDbContext : DbContext
{
    public KeeperDbContext(DbContextOptions<KeeperDbContext> options) : base(options)
    {
    }

    public DbSet<Visitor> Visitors => Set<Visitor>();

    public DbSet<VisitorActionRecord> VisitorActions => Set<VisitorActionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Visitor>(entity =>
        {
            entity.ToTable("visitors", table =>
                table.HasCheckConstraint("ck_visitors_trust_level", "\"TrustLevel\" >= 0 AND \"TrustLevel\" <= 100"));

            entity.HasKey(v => v.Id);
            entity.Property(v => v.Name).HasMaxLength(100).IsRequired();
            entity.Property(v => v.TrustLevel).HasDefaultValue(0);
            entity.Property(v => v.CreatedAt).IsRequired();

            entity.HasMany(v => v.Actions)
                .WithOne(a => a.Visitor)
                .HasForeignKey(a => a.VisitorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VisitorActionRecord>(entity =>
        {
            entity.ToTable("visitor_actions");

            entity.HasKey(a => a.Id);
            entity.Property(a => a.Action).HasConversion<string>().HasMaxLength(20).IsRequired();

            entity.HasIndex(a => new { a.VisitorId, a.Action }).IsUnique();
        });
    }
}
