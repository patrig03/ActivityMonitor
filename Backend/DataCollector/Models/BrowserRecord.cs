namespace Backend.Models;

public class BrowserRecord
{
    public int Id { get; set; }
    public int BrowserId { get; set; }
    public string Url { get; set; }
    public string Domain { get; set; }
}