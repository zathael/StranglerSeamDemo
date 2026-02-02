using StranglerSeamDemo.Api.Data;
using StranglerSeamDemo.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace StranglerSeamDemo.Api.Controllers;

[ApiController]
[Route("cases")]
public class CasesController : ControllerBase
{
    private static readonly HashSet<string> AllowedStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "New", "InProgress", "OnHold", "Done", "Cancelled" };

    private readonly AppDbContext _db;

    public CasesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PagedResult<CaseDto>>> GetCases(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) return BadRequest("page must be >= 1");
        if (pageSize is < 1 or > 100) return BadRequest("pageSize must be 1..100");

        var q = _db.Cases.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(c =>
                c.PatientName.Contains(term) ||
                c.Procedure.Contains(term) ||
                c.Status.Contains(term));
        }

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(c => c.LastUpdatedUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CaseDto(c.Id, c.PatientName, c.Procedure, c.Status, c.LastUpdatedUtc))
            .ToListAsync();

        return Ok(new PagedResult<CaseDto>(items, total, page, pageSize));
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<CaseDto>> UpdateStatus(int id, [FromBody] UpdateStatusRequest body)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Status))
            return BadRequest("status is required");

        if (!AllowedStatuses.Contains(body.Status))
            return BadRequest($"status must be one of: {string.Join(", ", AllowedStatuses)}");

        var entity = await _db.Cases.FirstOrDefaultAsync(c => c.Id == id);
        if (entity is null) return NotFound();

        entity.Status = body.Status.Trim();
        entity.LastUpdatedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new CaseDto(entity.Id, entity.PatientName, entity.Procedure, entity.Status, entity.LastUpdatedUtc));
    }
}
