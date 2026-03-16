using Backend.Models;
using Database.Manager;

namespace ActivityMonitor.ViewModels;

public class SettingsViewModel
{
    private IDatabaseManager db = new DatabaseManager(Settings.DbPath);
    public Settings Settings { get; set; } = new();
    public SettingsViewModel()
    {
        var sdto = db.GetSettings(1);
        if (sdto != null)
        {
            Settings = Settings.FromDto(sdto);
        }
    }

    public void SaveCommand()
    {
        
    }
}