using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using DataGuard.Api.Data;
using DataGuard.Api.DTOs;
using DataGuard.Api.Services;
namespace DataGuard.Api.Controllers;
[ApiController, Route("api/comparisons")]
public class ComparisonsController(ComparisonService service, AppDbContext db) : ControllerBase
{
    [HttpPost] public async Task<IActionResult> Create(CompareRequest request) => Ok(await service.Compare(request));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var c = await db.Comparisons.FindAsync(id);
        return c == null ? NotFound() : Ok(JsonSerializer.Deserialize<ComparisonResult>(c.ResultJson));
    }
    [HttpGet("{id:guid}/features")]
    public async Task<IActionResult> Features(Guid id)
    {
        var c = await db.Comparisons.FindAsync(id);
        return c == null ? NotFound() : Ok(JsonSerializer.Deserialize<ComparisonResult>(c.ResultJson)!.Features);
    }
}
