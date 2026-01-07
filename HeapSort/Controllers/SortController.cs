using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HeapSort.Models;
using HeapSort.Services;

namespace HeapSort.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SortController : ControllerBase
{
    private readonly SortService _sortService;

    public SortController(SortService sortService)
    {
        _sortService = sortService;
    }

    [HttpPost("heapsort")]
    public async Task<IActionResult> HeapSort([FromBody] SortRequest request)
    {
        try
        {
            if (request.ArraySize.HasValue && (request.ArraySize < 1 || request.ArraySize > 10000))
            {
                return BadRequest(new { message = "Array size must be between 1 and 10000" });
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _sortService.PerformHeapSortAndSaveAsync(userId, request);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("heapsort/without-save")]
    public IActionResult HeapSortWithoutSave([FromBody] SortRequest request)
    {
        try
        {
            if (request.ArraySize.HasValue)
            {
                if (request.ArraySize < 1 || request.ArraySize > 10000)
                {
                    return BadRequest(new { message = "Array size must be between 1 and 10000" });
                }

                var result = _sortService.PerformHeapSort(
                    request.ArraySize.Value,
                    request.MinValue,
                    request.MaxValue
                );

                return Ok(result);
            }
            else
            {
                return BadRequest(new { message = "ArraySize is required for sorting without save" });
            }
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

