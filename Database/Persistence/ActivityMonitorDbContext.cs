using Microsoft.EntityFrameworkCore;

namespace Database.Persistence;

public sealed class ActivityMonitorDbContext(DbContextOptions<ActivityMonitorDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<SettingsEntity> Settings => Set<SettingsEntity>();
    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();
    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();
    public DbSet<SessionEntity> Sessions => Set<SessionEntity>();
    public DbSet<BrowserActivityEntity> BrowserActivities => Set<BrowserActivityEntity>();
    public DbSet<ThresholdEntity> Thresholds => Set<ThresholdEntity>();
    public DbSet<InterventionEntity> Interventions => Set<InterventionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ActivityMonitorDbContext).Assembly);
    }
}
