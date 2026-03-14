using Database.DTO;

namespace Backend.Models;

public class BrowserRecord
{
    public int Id { get; set; }
    public int BrowserId { get; set; }
    public string Url { get; set; }

    public string Domain
    {
        get
        {
            var uri = new Uri(Url);
            return uri.Host;
        }
    }

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
    public static BrowserRecord FromDto(BrowserActivityDto dto)
    {
        return new BrowserRecord
        {
            Id = dto.ActivityId,
            BrowserId = dto.AppId,
            Url = dto.Url,
        };
    }
}