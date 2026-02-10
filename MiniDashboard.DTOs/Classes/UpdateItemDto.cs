using System.ComponentModel.DataAnnotations;

namespace MiniDashboard.DTOs.Classes;

public class UpdateItemDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "TypeId must be a valid type.")]
    public int TypeId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be a valid category.")]
    public int CategoryId { get; set; }

    [Required]
    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
    public int Quantity { get; set; }

    [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100.")]
    public decimal Discount { get; set; }
}
