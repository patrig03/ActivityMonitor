using Database.DTO;

namespace Backend.Models;

public class BrowserRecord
{
    public int Id { get; set; }
    public int BrowserId { get; set; }
    public int? CategoryId { get; set; }
    public string Url { get; set; } = string.Empty;

    public string Domain
    {
        get
        {
            return Uri.TryCreate(Url, UriKind.Absolute, out var uri)
                ? uri.Host
                : string.Empty;
        }
    }

    public BrowserActivityDto ToDto()
    {
        return new BrowserActivityDto
        {
            UserId = 1,
            ActivityId = Id,
            AppId = BrowserId,
            CategoryId = CategoryId,
            Url = Url,
        };
    }
    public static BrowserRecord FromDto(BrowserActivityDto dto)
    {
        return new BrowserRecord
        {
            Id = dto.ActivityId,
            BrowserId = dto.AppId,
            CategoryId = dto.CategoryId,
            Url = dto.Url ?? string.Empty,
        };
    }
}
