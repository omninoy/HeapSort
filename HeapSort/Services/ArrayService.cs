using HeapSort.Models;
using System.Text.Json;

namespace HeapSort.Services;

public class ArrayService
{
    private readonly DatabaseService _databaseService;

    public ArrayService(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<ArrayResponse> CreateArrayAsync(int userId, ArrayRequest request)
    {
        int[] elements;

        if (request.Elements != null && request.Elements.Length > 0)
        {
            // Массив задан вручную
            elements = request.Elements;
        }
        else if (request.ArraySize.HasValue && request.ArraySize.Value > 0)
        {
            // Генерация случайного массива
            var random = new Random();
            var minValue = request.MinValue ?? 1;
            var maxValue = request.MaxValue ?? 1000;
            var size = request.ArraySize.Value;

            elements = new int[size];
            for (int i = 0; i < size; i++)
            {
                elements[i] = random.Next(minValue, maxValue + 1);
            }
        }
        else
        {
            throw new ArgumentException("Either Elements or ArraySize must be provided");
        }

        var arrayData = await _databaseService.CreateArrayAsync(userId, elements);

        return new ArrayResponse
        {
            Id = arrayData.Id,
            Elements = JsonSerializer.Deserialize<int[]>(arrayData.Elements) ?? Array.Empty<int>(),
            CreatedAt = arrayData.CreatedAt,
            UpdatedAt = arrayData.UpdatedAt
        };
    }

    public async Task<List<ArrayResponse>> GetAllArraysAsync(int userId)
    {
        var arrays = await _databaseService.GetUserArraysAsync(userId);

        return arrays.Select(a => new ArrayResponse
        {
            Id = a.Id,
            Elements = JsonSerializer.Deserialize<int[]>(a.Elements) ?? Array.Empty<int>(),
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        }).ToList();
    }

    public async Task<ArrayResponse?> GetArrayByIdAsync(int userId, int arrayId)
    {
        var arrayData = await _databaseService.GetArrayByIdAsync(arrayId, userId);
        if (arrayData == null)
        {
            return null;
        }

        return new ArrayResponse
        {
            Id = arrayData.Id,
            Elements = JsonSerializer.Deserialize<int[]>(arrayData.Elements) ?? Array.Empty<int>(),
            CreatedAt = arrayData.CreatedAt,
            UpdatedAt = arrayData.UpdatedAt
        };
    }

    public async Task<ArrayResponse?> UpdateArrayAsync(int userId, int arrayId, int[] elements)
    {
        var arrayData = await _databaseService.UpdateArrayAsync(arrayId, userId, elements);
        if (arrayData == null)
        {
            return null;
        }

        return new ArrayResponse
        {
            Id = arrayData.Id,
            Elements = JsonSerializer.Deserialize<int[]>(arrayData.Elements) ?? Array.Empty<int>(),
            CreatedAt = arrayData.CreatedAt,
            UpdatedAt = arrayData.UpdatedAt
        };
    }

    public async Task<bool> DeleteArrayAsync(int userId, int arrayId)
    {
        return await _databaseService.DeleteArrayAsync(arrayId, userId);
    }

    public async Task<ArrayResponse?> AddElementAsync(int userId, int arrayId, AddElementRequest request)
    {
        var arrayData = await _databaseService.GetArrayByIdAsync(arrayId, userId);
        if (arrayData == null)
        {
            return null;
        }

        var elements = JsonSerializer.Deserialize<List<int>>(arrayData.Elements) ?? new List<int>();

        switch (request.Position.ToLower())
        {
            case "start":
                elements.Insert(0, request.Value);
                break;
            case "end":
                elements.Add(request.Value);
                break;
            case "after":
                if (!request.Index.HasValue)
                {
                    throw new ArgumentException("Index is required for 'after' position");
                }
                var index = request.Index.Value;
                if (index < 0 || index >= elements.Count)
                {
                    throw new ArgumentException("Index is out of range");
                }
                elements.Insert(index + 1, request.Value);
                break;
            default:
                throw new ArgumentException("Position must be 'start', 'end', or 'after'");
        }

        return await UpdateArrayAsync(userId, arrayId, elements.ToArray());
    }

    public async Task<int[]?> GetSliceAsync(int userId, int arrayId, SliceRequest request)
    {
        var arrayData = await _databaseService.GetArrayByIdAsync(arrayId, userId);
        if (arrayData == null)
        {
            return null;
        }

        var elements = JsonSerializer.Deserialize<int[]>(arrayData.Elements) ?? Array.Empty<int>();
        var start = request.Start ?? 0;
        var end = request.End ?? elements.Length;

        if (start < 0) start = 0;
        if (end > elements.Length) end = elements.Length;
        if (start > end) return Array.Empty<int>();

        var length = end - start;
        var slice = new int[length];
        Array.Copy(elements, start, slice, 0, length);

        return slice;
    }
}

