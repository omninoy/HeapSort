namespace HeapSort.Models;

public class ArrayData
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Elements { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ArrayRequest
{
    public int[]? Elements { get; set; }
    public int? ArraySize { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
}

public class ArrayResponse
{
    public int Id { get; set; }
    public int[] Elements { get; set; } = Array.Empty<int>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AddElementRequest
{
    public int Value { get; set; }
    public string Position { get; set; } = "end";
    public int? Index { get; set; }
}

public class SliceRequest
{
    public int? Start { get; set; }
    public int? End { get; set; }
}

