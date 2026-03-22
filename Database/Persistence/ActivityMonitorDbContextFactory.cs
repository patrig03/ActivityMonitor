using Microsoft.EntityFrameworkCore;

namespace Database.Persistence;

public interface IActivityMonitorDbContextFactory
{
    ActivityMonitorDbContext CreateDbContext();
}

public sealed class ActivityMonitorDbContextFactory(string connectionString) : IActivityMonitorDbContextFactory
{
    private readonly Lazy<ServerVersion> _serverVersion = new(() => ServerVersion.AutoDetect(connectionString));

    public ActivityMonitorDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ActivityMonitorDbContext>();
        optionsBuilder.UseMySql(
            connectionString,
            _serverVersion.Value,
            mySqlOptions => mySqlOptions.EnableRetryOnFailure());

        return new ActivityMonitorDbContext(optionsBuilder.Options);
    }
}
