using Microsoft.AspNetCore.Mvc;
using MiniDashboard.BL.Interfaces;
using MiniDashboard.DTOs.Classes;

namespace MiniDashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly IItemService _service;

    public ItemsController(IItemService service)
    {
        _service = service;
    }

    /// <summary>
    /// Retrieves all items
    /// </summary>
    /// <returns>List of all items</returns>
    /// <response code="200">Returns the list of items</response>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    /// <summary>
    /// Gets a specific item by ID
    /// </summary>
    /// <param name="id">The ID of the item to retrieve</param>
    /// <returns>The requested item</returns>
    /// <response code="200">Item found</response>
    /// <response code="404">Item not found</response>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null)
            return NotFound();

        return Ok(item);
    }

    /// <summary>
    /// Searches for items matching the query string
    /// </summary>
    /// <param name="query">Search term (e.g. name, description, keyword)</param>
    /// <returns>List of matching items</returns>
    /// <response code="200">Search results (empty list if no matches)</response>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        var items = await _service.SearchAsync(query);
        return Ok(items);
    }

    /// <summary>
    /// Creates a new item
    /// </summary>
    /// <param name="dto">Item creation data</param>
    /// <returns>The created item</returns>
    /// <response code="201">Item created successfully</response>
    /// <response code="409">Conflict (e.g. duplicate name/code)</response>
    /// <response code="400">Invalid data</response>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateItemDto dto)
    {
        try
        {
            var created = await _service.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing item
    /// </summary>
    /// <param name="id">ID of the item to update</param>
    /// <param name="dto">Updated item data</param>
    /// <returns>The updated item</returns>
    /// <response code="200">Item updated</response>
    /// <response code="404">Item not found</response>
    /// <response code="409">Conflict (business rule violation)</response>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateItemDto dto)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, dto);
            if (updated is null)
                return NotFound();

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes an item by ID
    /// </summary>
    /// <param name="id">ID of the item to delete</param>
    /// <response code="204">Item deleted successfully</response>
    /// <response code="404">Item not found</response>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
