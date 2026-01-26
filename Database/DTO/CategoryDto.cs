namespace Database.DTO;


public sealed class CategoryDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public decimal Confidence { get; set; }
}