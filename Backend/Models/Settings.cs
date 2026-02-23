namespace Backend.Models;

public class Settings
{
    public int Id { get; set; }
    public int UserId  { get; set; }
    public TimeSpan DeltaTime { get; set; }
    public string MutexName { get; set; }
    public string DbPath { get; set; }
}