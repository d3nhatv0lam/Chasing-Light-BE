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
            builder.Property(p => p.Id).HasColumnName("id");
            builder.Property(p => p.DisplayName).HasColumnName("display_name").HasMaxLength(150);
            builder.Property(p => p.AvatarUrl).HasColumnName("avatar_url");
            builder.Property(p => p.TotalStars).HasColumnName("total_stars");
            builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Stage>(builder =>
        {
            builder.ToTable("stages");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).HasColumnName("id");
            builder.Property(s => s.StageNumber).HasColumnName("stage_number");
            builder.Property(s => s.Difficulty).HasColumnName("difficulty").HasConversion<short>();
            builder.Property(s => s.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<UserRecord>(builder =>
        {
            builder.ToTable("user_records");
            builder.HasKey(r => new { r.UserId, r.StageId });
            builder.Property(r => r.UserId).HasColumnName("user_id");
            builder.Property(r => r.StageId).HasColumnName("stage_id");
            builder.Property(r => r.StarsEarned).HasColumnName("stars_earned");
            builder.Property(r => r.BestTimeSeconds).HasColumnName("best_time_seconds");
            builder.Property(r => r.BestStepCount).HasColumnName("best_step_count");
            builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

            builder.HasOne(r => r.Profile)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Stage)
                .WithMany()
                .HasForeignKey(r => r.StageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(r => new { r.StageId, r.StarsEarned, r.BestTimeSeconds, r.BestStepCount })
                .HasDatabaseName("idx_user_records_stage_rank");
        });
    }
}