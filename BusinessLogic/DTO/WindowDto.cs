namespace BusinessLogic.DTO;

public class WindowDto
{
    public string WmClass { get; }
    public string Title { get; }
    public TimeSpan VisibleFor { get; set; }

    public WindowDto(string wmClass, string title, TimeSpan visibleFor)
    {
        WmClass = wmClass;
        Title = title;
        VisibleFor = visibleFor;
    }
}