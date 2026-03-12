using System;
using System.Collections.ObjectModel;
using System.IO;
using Backend.Classifier.Models;
using Backend.Interventions.Models;
using Backend.Models;
using Database.Manager;
namespace ActivityMonitor.ViewModels;

public class ViewData
{
    public required Category category { get; set; }
    public required Threshold threshold { get; set; }
}

public class InterventionsViewModel
{
    public ObservableCollection<ViewData> Data { get; set; }
    private DatabaseManager Manager { get; }

    
    public InterventionsViewModel()
    {
        Manager = new (GetDatabasePath());
        Data = new ();
        
        var thresholdDtos = Manager.GetAllThresholds();
        
        foreach (var thresholdDto in thresholdDtos)
        {
            if (thresholdDto == null) { continue; }
            
            var categoryDto = Manager.GetCategory(thresholdDto.CategoryId);
            if (categoryDto == null) { continue; }
            
            var data = new ViewData
            {
                category = Category.FromDto(categoryDto),
                threshold = Threshold.FromDto(thresholdDto),
            };
            
            Data.Add(data);
        }
    }
    
    
    public void AddCommand()
    {
        
    }
    
    public void SaveCommand()
    {
        
    }
    
    public void DeleteCommand()
    {
        
    }
    
    private static string GetDatabasePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        appDataPath = Path.Combine(appDataPath, "ActivityMonitor");
        Directory.CreateDirectory(appDataPath);
        return Path.Combine(appDataPath, "database.db");
    }
}