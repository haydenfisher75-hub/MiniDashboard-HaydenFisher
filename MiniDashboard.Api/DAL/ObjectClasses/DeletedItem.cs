namespace MiniDashboard.DAL.Classes;

public class DeletedItem : Item
{
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}
