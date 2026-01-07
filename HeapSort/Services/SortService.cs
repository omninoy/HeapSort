using HeapSort.Models;
using System.Text.Json;

namespace HeapSort.Services;

public class SortService
{
    private readonly DatabaseService _databaseService;

    public SortService(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public SortResult PerformHeapSort(int[] array)
    {
        if (array == null || array.Length == 0)
        {
            throw new ArgumentException("Array cannot be null or empty");
        }

        // Копирование массива для сортировки
        var sortedArray = new int[array.Length];
        Array.Copy(array, sortedArray, array.Length);

        // Измерение времени выполнения
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        HeapSort(sortedArray);
        stopwatch.Stop();

        return new SortResult
        {
            OriginalArray = array,
            SortedArray = sortedArray,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
            SortDate = DateTime.UtcNow
        };
    }

    public SortResult PerformHeapSort(int arraySize, int? minValue = null, int? maxValue = null)
    {
        // Генерация случайного массива
        var random = new Random();
        minValue ??= 1;
        maxValue ??= 1000;

        var originalArray = new int[arraySize];
        for (int i = 0; i < arraySize; i++)
        {
            originalArray[i] = random.Next(minValue.Value, maxValue.Value + 1);
        }

        return PerformHeapSort(originalArray);
    }

    public async Task<SortResult> PerformHeapSortAndSaveAsync(int userId, SortRequest request)
    {
        int[] originalArray;
        int? arrayId = null;

        if (request.ArrayId.HasValue)
        {
            // Использование сохраненного массива
            var arrayData = await _databaseService.GetArrayByIdAsync(request.ArrayId.Value, userId);
            if (arrayData == null)
            {
                throw new ArgumentException("Array not found");
            }

            originalArray = JsonSerializer.Deserialize<int[]>(arrayData.Elements) ?? Array.Empty<int>();
            arrayId = request.ArrayId.Value;
        }
        else if (request.ArraySize.HasValue)
        {
            // Генерация случайного массива
            var random = new Random();
            var minValue = request.MinValue ?? 1;
            var maxValue = request.MaxValue ?? 1000;
            var size = request.ArraySize.Value;

            originalArray = new int[size];
            for (int i = 0; i < size; i++)
            {
                originalArray[i] = random.Next(minValue, maxValue + 1);
            }
        }
        else
        {
            throw new ArgumentException("Either ArrayId or ArraySize must be provided");
        }

        var result = PerformHeapSort(originalArray);
        await _databaseService.SaveSortHistoryAsync(userId, result, arrayId);
        return result;
    }

    public async Task<SortResult> PerformHeapSortAndSaveAsync(int userId, int arraySize, int? minValue = null, int? maxValue = null)
    {
        var result = PerformHeapSort(arraySize, minValue, maxValue);
        await _databaseService.SaveSortHistoryAsync(userId, result);
        return result;
    }

    // Пирамидальная сортировка (Heap Sort)
    private void HeapSort(int[] array)
    {
        int n = array.Length;

        // Построение кучи (перегруппировка массива)
        for (int i = n / 2 - 1; i >= 0; i--)
        {
            Heapify(array, n, i);
        }

        // Извлечение элементов из кучи по одному
        for (int i = n - 1; i > 0; i--)
        {
            // Переместить текущий корень в конец
            (array[0], array[i]) = (array[i], array[0]);

            // Вызвать heapify на уменьшенной куче
            Heapify(array, i, 0);
        }
    }

    // Преобразование поддерева в кучу с корнем в узле i
    private void Heapify(int[] array, int n, int i)
    {
        int largest = i; // Инициализировать наибольший элемент как корень
        int left = 2 * i + 1; // Левый дочерний элемент
        int right = 2 * i + 2; // Правый дочерний элемент

        // Если левый дочерний элемент больше корня
        if (left < n && array[left] > array[largest])
        {
            largest = left;
        }

        // Если правый дочерний элемент больше, чем самый большой элемент на данный момент
        if (right < n && array[right] > array[largest])
        {
            largest = right;
        }

        // Если самый большой элемент не является корнем
        if (largest != i)
        {
            (array[i], array[largest]) = (array[largest], array[i]);

            // Рекурсивно heapify затронутое поддерево
            Heapify(array, n, largest);
        }
    }
}
