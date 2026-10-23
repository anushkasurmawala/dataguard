using Microsoft.AspNetCore.Mvc;
using DataGuard.Api.Data;
using DataGuard.Api.Services;
namespace DataGuard.Api.Controllers;
[ApiController, Route("api/datasets")]
public class DatasetsController(DatasetService service, AppDbContext db) : ControllerBase
{
    [HttpPost("{type}"), RequestSizeLimit(21 * 1024 * 1024)]
    public async Task<IActionResult> Upload(string type, IFormFile file)
    {
        if (type != "baseline" && type != "current") return BadRequest(new
        {
            message = "Invalid dataset role."
        });
        return Ok(await service.Upload(file, type));
    }
    [HttpGet("{id:guid}/profile")]
    public async Task<IActionResult> Profile(Guid id)
    {
        var d = await db.Datasets.FindAsync(id);
        return d == null ? NotFound() : Ok(DatasetService.Response(d));
    }
}
