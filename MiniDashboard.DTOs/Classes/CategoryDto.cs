namespace MiniDashboard.DTOs.Classes;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int TypeId { get; set; }
}
