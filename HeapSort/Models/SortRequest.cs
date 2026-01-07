namespace HeapSort.Models;

public class SortRequest
{
    public int? ArrayId { get; set; } // ID сохраненного массива
    public int? ArraySize { get; set; } // Размер для генерации случайного массива
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
}

public class SortResult
{
    public int[] OriginalArray { get; set; } = Array.Empty<int>();
    public int[] SortedArray { get; set; } = Array.Empty<int>();
    public long ExecutionTimeMs { get; set; }
    public DateTime SortDate { get; set; }
}
