using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HeapSort.Models;
using HeapSort.Services;

namespace HeapSort.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ArrayController : ControllerBase
{
    private readonly ArrayService _arrayService;

    public ArrayController(ArrayService arrayService)
    {
        _arrayService = arrayService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateArray([FromBody] ArrayRequest request)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _arrayService.CreateArrayAsync(userId, request);
            return CreatedAtAction(nameof(GetArrayById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllArrays()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var arrays = await _arrayService.GetAllArraysAsync(userId);
        return Ok(arrays);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetArrayById(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var array = await _arrayService.GetArrayByIdAsync(userId, id);
        
        if (array == null)
        {
            return NotFound(new { message = "Array not found" });
        }

        return Ok(array);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateArray(int id, [FromBody] int[] elements)
    {
        if (elements == null || elements.Length == 0)
        {
            return BadRequest(new { message = "Elements array cannot be empty" });
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _arrayService.UpdateArrayAsync(userId, id, elements);

        if (result == null)
        {
            return NotFound(new { message = "Array not found" });
        }

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteArray(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _arrayService.DeleteArrayAsync(userId, id);

        if (!result)
        {
            return NotFound(new { message = "Array not found" });
        }

        return NoContent();
    }

    [HttpPost("{id}/add-element")]
    public async Task<IActionResult> AddElement(int id, [FromBody] AddElementRequest request)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _arrayService.AddElementAsync(userId, id, request);

            if (result == null)
            {
                return NotFound(new { message = "Array not found" });
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/slice")]
    public async Task<IActionResult> GetSlice(int id, [FromQuery] int? start, [FromQuery] int? end)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var slice = await _arrayService.GetSliceAsync(userId, id, new SliceRequest 
        { 
            Start = start, 
            End = end 
        });

        if (slice == null)
        {
            return NotFound(new { message = "Array not found" });
        }

        return Ok(new { slice });
    }
}

