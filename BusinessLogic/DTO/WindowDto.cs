namespace BusinessLogic.DTO;

public class WindowDto(string wmClass, string title, TimeSpan visibleFor, TimeSpan activeFor, DateTime lastVisible, DateTime lastActive)
{
    public string WmClass { get; } = wmClass;
    public string Title { get; } = title;
    public TimeSpan VisibleFor { get; set; } = visibleFor;
    public TimeSpan ActiveFor { get; set; } = activeFor;
    public DateTime LastVisible { get; set; } = lastVisible;
    public DateTime LastActive { get; set; } = lastActive;
}