namespace MiniDashboard.DAL.Classes;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int TypeId { get; set; }
}
