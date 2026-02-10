using Microsoft.AspNetCore.Mvc;
using MiniDashboard.DAL.Interfaces;

namespace MiniDashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _repository;

    public CategoriesController(ICategoryRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Gets all categories, optionally filtered by type
    /// </summary>
    /// <param name="typeId">Optional: Filter categories by type ID</param>
    /// <returns>List of categories</returns>
    /// <response code="200">List of categories (filtered or all)</response>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? typeId)
    {
        if (typeId.HasValue)
        {
            var filtered = await _repository.GetByTypeIdAsync(typeId.Value);
            return Ok(filtered);
        }

        var categories = await _repository.GetAllAsync();
        return Ok(categories);
    }
}
