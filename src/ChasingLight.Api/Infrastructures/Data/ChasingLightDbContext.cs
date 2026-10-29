using ChasingLight.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChasingLight.Api.Infrastructures.Data;

public class ChasingLightDbContext : DbContext
{
    public ChasingLightDbContext(DbContextOptions<ChasingLightDbContext> options) : base(options)
    {
    }

    public DbSet<Profile> Profiles => base.Set<Profile>();
    public DbSet<Stage> Stages => base.Set<Stage>();
    public DbSet<UserRecord> UserRecords => base.Set<UserRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Profile>(builder =>
        {
            builder.ToTable("profiles");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.DisplayName).HasMaxLength(150);
        });

        modelBuilder.Entity<Stage>(builder =>
        {
            builder.ToTable("stages");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Difficulty).HasConversion<short>();
        });

        modelBuilder.Entity<UserRecord>(builder =>
        {
            builder.ToTable("user_records");
            builder.HasKey(r => new { r.UserId, r.StageId });

            builder.HasOne(r => r.Profile)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Stage)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(r => new { r.StageId, r.StarsEarned, r.BestTimeSeconds, r.BestStepCount })
                .HasDatabaseName("idx_user_records_stage_rank");
        });
    }
}