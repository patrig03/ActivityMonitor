namespace Backend.Sync;

public interface ISyncStrategy
{
    bool SyncBrowserData();
    bool SyncAppData();
}