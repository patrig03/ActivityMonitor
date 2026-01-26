namespace Database.DTO;


public sealed class UserDto
{
    public int UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? PinHash { get; set; }
    public bool SyncEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
}















