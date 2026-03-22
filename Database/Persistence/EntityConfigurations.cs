using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Persistence;

public sealed class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("users");
        builder.HasKey(entity => entity.UserId);
        builder.Property(entity => entity.UserId).HasColumnName("user_id").ValueGeneratedOnAdd();
        builder.Property(entity => entity.DisplayName).HasColumnName("display_name").HasMaxLength(255);
        builder.Property(entity => entity.PinHash).HasColumnName("pin_hash").HasMaxLength(255);
        builder.Property(entity => entity.SyncEnabled).HasColumnName("sync_enabled");
        builder.Property(entity => entity.CreatedAt).HasColumnName("created_at");
    }
}

public sealed class SettingsEntityConfiguration : IEntityTypeConfiguration<SettingsEntity>
{
    public void Configure(EntityTypeBuilder<SettingsEntity> builder)
    {
        builder.ToTable("settings");
        builder.HasKey(entity => entity.SettingsId);
        builder.Property(entity => entity.SettingsId).HasColumnName("settings_id").ValueGeneratedOnAdd();
        builder.Property(entity => entity.UserId).HasColumnName("user_id");
        builder.Property(entity => entity.RefreshTimeSeconds).HasColumnName("refresh_time_seconds");

        builder.HasOne(entity => entity.User)
            .WithMany(user => user.Settings)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => entity.UserId).IsUnique();
    }
}

public sealed class CategoryEntityConfiguration : IEntityTypeConfiguration<CategoryEntity>
{
    public void Configure(EntityTypeBuilder<CategoryEntity> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(entity => entity.CategoryId);
        builder.Property(entity => entity.CategoryId).HasColumnName("category_id").ValueGeneratedOnAdd();
        builder.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(entity => entity.Description).HasColumnName("description").HasColumnType("text");
    }
}

public sealed class ApplicationEntityConfiguration : IEntityTypeConfiguration<ApplicationEntity>
{
    public void Configure(EntityTypeBuilder<ApplicationEntity> builder)
    {
        builder.ToTable("applications");
        builder.HasKey(entity => entity.AppId);
        builder.Property(entity => entity.AppId).HasColumnName("app_id").ValueGeneratedOnAdd();
        builder.Property(entity => entity.CategoryId).HasColumnName("category_id");
        builder.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(512);
        builder.Property(entity => entity.Class).HasColumnName("class").HasMaxLength(255);
        builder.Property(entity => entity.ProcessName).HasColumnName("process_name").HasMaxLength(255);
        builder.Property(entity => entity.PositionX).HasColumnName("position_x");
        builder.Property(entity => entity.PositionY).HasColumnName("position_y");
        builder.Property(entity => entity.Width).HasColumnName("width");
        builder.Property(entity => entity.Height).HasColumnName("height");
        builder.Property(entity => entity.WindowId).HasColumnName("window_id");

        builder.HasOne(entity => entity.Category)
            .WithMany(category => category.Applications)
            .HasForeignKey(entity => entity.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.Property(e => e.Name).HasMaxLength(191);
        builder.Property(e => e.Class).HasMaxLength(191);
        builder.Property(e => e.ProcessName).HasMaxLength(191);
        
        builder.HasIndex(entity => new { entity.Name, entity.Class, entity.ProcessName })
            .HasDatabaseName("ix_applications_identity");
    }
}

public sealed class SessionEntityConfiguration : IEntityTypeConfiguration<SessionEntity>
{
    public void Configure(EntityTypeBuilder<SessionEntity> builder)
    {
        builder.ToTable("sessions");
        builder.HasKey(entity => entity.SessionId);
        builder.Property(entity => entity.SessionId).HasColumnName("session_id").ValueGeneratedOnAdd();
        builder.Property(entity => entity.AppId).HasColumnName("app_id");
        builder.Property(entity => entity.UserId).HasColumnName("user_id");
        builder.Property(entity => entity.StartTime).HasColumnName("start_time");
        builder.Property(entity => entity.EndTime).HasColumnName("end_time");

        builder.HasOne(entity => entity.Application)
            .WithMany(application => application.Sessions)
            .HasForeignKey(entity => entity.AppId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(entity => entity.User)
            .WithMany(user => user.Sessions)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(entity => new { entity.AppId, entity.UserId, entity.StartTime })
            .HasDatabaseName("ix_sessions_identity");
    }
}

public sealed class BrowserActivityEntityConfiguration : IEntityTypeConfiguration<BrowserActivityEntity>
{
    public void Configure(EntityTypeBuilder<BrowserActivityEntity> builder)
    {
        builder.ToTable("browser_activity");
        builder.HasKey(entity => entity.ActivityId);
        builder.Property(entity => entity.ActivityId).HasColumnName("activity_id").ValueGeneratedOnAdd();
        builder.Property(entity => entity.UserId).HasColumnName("user_id");
        builder.Property(entity => entity.AppId).HasColumnName("app_id");
        builder.Property(entity => entity.Url).HasColumnName("url").HasColumnType("text");

        builder.HasOne(entity => entity.User)
            .WithMany(user => user.BrowserActivities)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.Application)
            .WithMany(application => application.BrowserActivities)
            .HasForeignKey(entity => entity.AppId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Property(e => e.Url).HasMaxLength(512);

        builder.HasIndex(entity => new { entity.UserId, entity.AppId, entity.Url })
            .HasDatabaseName("ix_browser_activity_identity");
    }
}

public sealed class ThresholdEntityConfiguration : IEntityTypeConfiguration<ThresholdEntity>
{
    public void Configure(EntityTypeBuilder<ThresholdEntity> builder)
    {
        builder.ToTable("thresholds");
        builder.HasKey(entity => entity.ThresholdId);
        builder.Property(entity => entity.ThresholdId).HasColumnName("threshold_id").ValueGeneratedOnAdd();
        builder.Property(entity => entity.UserId).HasColumnName("user_id");
        builder.Property(entity => entity.CategoryId).HasColumnName("category_id");
        builder.Property(entity => entity.AppId).HasColumnName("app_id");
        builder.Property(entity => entity.IsActive).HasColumnName("is_active");
        builder.Property(entity => entity.TargetType).HasColumnName("target_type").HasMaxLength(32);
        builder.Property(entity => entity.InterventionType).HasColumnName("intervention_type").HasMaxLength(32);
        builder.Property(entity => entity.DurationType).HasColumnName("duration_type").HasMaxLength(32);
        builder.Property(entity => entity.DailyLimitSec).HasColumnName("daily_limit_sec");
        builder.Property(entity => entity.SessionLimitSec).HasColumnName("session_limit_sec");

        builder.HasOne(entity => entity.User)
            .WithMany(user => user.Thresholds)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.Category)
            .WithMany(category => category.Thresholds)
            .HasForeignKey(entity => entity.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(entity => entity.Application)
            .WithMany(application => application.Thresholds)
            .HasForeignKey(entity => entity.AppId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class InterventionEntityConfiguration : IEntityTypeConfiguration<InterventionEntity>
{
    public void Configure(EntityTypeBuilder<InterventionEntity> builder)
    {
        builder.ToTable("interventions");
        builder.HasKey(entity => entity.InterventionId);
        builder.Property(entity => entity.InterventionId).HasColumnName("intervention_id").ValueGeneratedOnAdd();
        builder.Property(entity => entity.ThresholdId).HasColumnName("threshold_id");
        builder.Property(entity => entity.TriggeredAt).HasColumnName("triggered_at");
        builder.Property(entity => entity.Snoozed).HasColumnName("snoozed");

        builder.HasOne(entity => entity.Threshold)
            .WithMany(threshold => threshold.Interventions)
            .HasForeignKey(entity => entity.ThresholdId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
