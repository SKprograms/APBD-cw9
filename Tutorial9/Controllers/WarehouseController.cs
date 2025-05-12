using Microsoft.AspNetCore.Mvc;
using Tutorial9.Exceptions;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;
    public WarehouseController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }
    
    [HttpPost] 
    public async Task<IActionResult> AddProduct([FromBody] WarehouseDTO warehouseDto)
    {
        try
        {
            var result = await _warehouseService.AddProductAsync(warehouseDto);
            return Ok(result);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (ConflictException e)
        {
            return Conflict(new { message = e.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "server error"});
        }
        
        
        
        
        
        
        
        
    }
    
    
}