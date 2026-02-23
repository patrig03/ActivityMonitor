namespace Backend.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string PinHash { get; set; }
    public DateTime CreatedAt { get; set; }
}