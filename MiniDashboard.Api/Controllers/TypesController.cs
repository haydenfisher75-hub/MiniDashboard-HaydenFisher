using Microsoft.AspNetCore.Mvc;
using MiniDashboard.DAL.Interfaces;

namespace MiniDashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TypesController : ControllerBase
{
    private readonly ITypeRepository _repository;

    public TypesController(ITypeRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Retrieves all available types
    /// </summary>
    /// <returns>List of all types</returns>
    /// <response code="200">List of types</response>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var types = await _repository.GetAllAsync();
        return Ok(types);
    }
}
