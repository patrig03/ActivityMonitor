using Database.DTO;

namespace Backend.Models;

public class BrowserRecord
{
    public int Id { get; set; }
    public int BrowserId { get; set; }
    public string Url { get; set; }
    public string Domain { get; set; }

    public BrowserActivityDto ToDto()
    {
        return new BrowserActivityDto
        {
            UserId = 1,
            ActivityId = Id,
            AppId = BrowserId,
            Url = Url,
        };
    }
}