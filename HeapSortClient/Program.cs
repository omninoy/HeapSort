using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var apiBaseUrl = "http://localhost:5000/api";
var httpClient = new HttpClient();
string? token = null;
string? username = null;

Console.WriteLine("=== HeapSort Client Application ===");
Console.WriteLine();

// Первый этап: вход или регистрация
while (token == null)
{
    Console.WriteLine("\nВыберите действие:");
    Console.WriteLine("1. Регистрация");
    Console.WriteLine("2. Вход");
    Console.WriteLine("3. Смена пароля");
    Console.WriteLine("4. Выход");
    Console.Write("\nВаш выбор: ");

    var choice = Console.ReadLine();

    try
    {
        switch (choice)
        {
            case "1":
                await RegisterAsync();
                break;
            case "2":
                await LoginAsync();
                break;
            case "3":
                await ChangePasswordAsync();
                break;
            case "4":
                Console.WriteLine("До свидания!");
                return;
            default:
                Console.WriteLine("Неверный выбор. Попробуйте снова.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
}

// Второй этап: работа с массивами (только после входа)
Console.WriteLine($"\n✓ Добро пожаловать, {username}!");
Console.WriteLine("Теперь вы можете работать с массивами.\n");

while (true)
{
    Console.WriteLine("\nВыберите действие:");
    Console.WriteLine("1. Установить массив (вручную)");
    Console.WriteLine("2. Сгенерировать случайный массив");
    Console.WriteLine("3. Получить все массивы");
    Console.WriteLine("4. Получить массив по ID");
    Console.WriteLine("5. Отсортировать массив");
    Console.WriteLine("6. Получить срез массива");
    Console.WriteLine("7. Добавить элемент в начало массива");
    Console.WriteLine("8. Добавить элемент в конец массива");
    Console.WriteLine("9. Добавить элемент после указанного индекса");
    Console.WriteLine("10. Удалить массив");
    Console.WriteLine("11. Выход");
    Console.Write("\nВаш выбор: ");

    var choice = Console.ReadLine();

    try
    {
        switch (choice)
        {
            case "1":
                await CreateArrayManuallyAsync();
                break;
            case "2":
                await GenerateRandomArrayAsync();
                break;
            case "3":
                await GetAllArraysAsync();
                break;
            case "4":
                await GetArrayByIdAsync();
                break;
            case "5":
                await SortArrayAsync();
                break;
            case "6":
                await GetSliceAsync();
                break;
            case "7":
                await AddElementAsync("start");
                break;
            case "8":
                await AddElementAsync("end");
                break;
            case "9":
                await AddElementAsync("after");
                break;
            case "10":
                await DeleteArrayAsync();
                break;
            case "11":
                Console.WriteLine("До свидания!");
                return;
            default:
                Console.WriteLine("Неверный выбор. Попробуйте снова.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
}

async Task RegisterAsync()
{
    Console.Write("Введите имя пользователя: ");
    var inputUsername = Console.ReadLine();
    Console.Write("Введите пароль: ");
    var password = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(inputUsername) || string.IsNullOrWhiteSpace(password))
    {
        Console.WriteLine("Имя пользователя и пароль не могут быть пустыми!");
        return;
    }

    var request = new
    {
        Username = inputUsername,
        Password = password
    };

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await httpClient.PostAsync($"{apiBaseUrl}/auth/register", content);
    var responseContent = await response.Content.ReadAsStringAsync();

    if (response.IsSuccessStatusCode)
    {
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        token = authResponse?.Token;
        username = authResponse?.Username;
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Console.WriteLine($"\n✓ Регистрация успешна! Добро пожаловать, {username}!");
    }
    else
    {
        Console.WriteLine($"\n✗ Ошибка регистрации: {responseContent}");
    }
}

async Task LoginAsync()
{
    Console.Write("Введите имя пользователя: ");
    var inputUsername = Console.ReadLine();
    Console.Write("Введите пароль: ");
    var password = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(inputUsername) || string.IsNullOrWhiteSpace(password))
    {
        Console.WriteLine("Имя пользователя и пароль не могут быть пустыми!");
        return;
    }

    var request = new
    {
        Username = inputUsername,
        Password = password
    };

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await httpClient.PostAsync($"{apiBaseUrl}/auth/login", content);
    var responseContent = await response.Content.ReadAsStringAsync();

    if (response.IsSuccessStatusCode)
    {
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        token = authResponse?.Token;
        username = authResponse?.Username;
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Console.WriteLine($"\n✓ Вход выполнен успешно! Добро пожаловать, {username}!");
    }
    else
    {
        Console.WriteLine($"\n✗ Ошибка входа: {responseContent}");
    }
}

async Task ChangePasswordAsync()
{
    if (string.IsNullOrEmpty(token))
    {
        Console.WriteLine("\n✗ Для смены пароля необходимо сначала войти в систему!");
        return;
    }

    Console.Write("Введите текущий пароль: ");
    var oldPassword = Console.ReadLine();
    Console.Write("Введите новый пароль: ");
    var newPassword = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
    {
        Console.WriteLine("Текущий и новый пароль не могут быть пустыми!");
        return;
    }

    var request = new
    {
        OldPassword = oldPassword,
        NewPassword = newPassword
    };

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{apiBaseUrl}/auth/change-password");
    requestMessage.Content = content;
    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var response = await httpClient.SendAsync(requestMessage);
    var responseContent = await response.Content.ReadAsStringAsync();

    if (response.IsSuccessStatusCode)
    {
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        token = authResponse?.Token;
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Console.WriteLine($"\n✓ Пароль успешно изменен! Новый токен получен.");
    }
    else
    {
        Console.WriteLine($"\n✗ Ошибка смены пароля: {responseContent}");
    }
}

async Task CreateArrayManuallyAsync()
{
    Console.Write("Введите элементы массива через запятую (например: 5,3,8,1,9): ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        Console.WriteLine("Массив не может быть пустым!");
        return;
    }

    try
    {
        var elements = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.Parse(s.Trim()))
            .ToArray();

        var request = new
        {
            Elements = elements
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync($"{apiBaseUrl}/array", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var arrayResponse = JsonSerializer.Deserialize<ArrayResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Console.WriteLine($"\n✓ Массив успешно создан!");
            Console.WriteLine($"ID: {arrayResponse?.Id}");
            Console.WriteLine($"Элементы: [{string.Join(", ", arrayResponse?.Elements ?? Array.Empty<int>())}]");
            Console.WriteLine($"Создан: {arrayResponse?.CreatedAt}");
        }
        else
        {
            Console.WriteLine($"\n✗ Ошибка создания массива: {responseContent}");
        }
    }
    catch (FormatException)
    {
        Console.WriteLine("Ошибка: некорректный формат чисел!");
    }
}

async Task GenerateRandomArrayAsync()
{
    Console.Write("Введите размер массива (1-10000): ");
    if (!int.TryParse(Console.ReadLine(), out var arraySize) || arraySize < 1 || arraySize > 10000)
    {
        Console.WriteLine("Неверный размер массива!");
        return;
    }

    Console.Write("Введите минимальное значение (Enter для значения по умолчанию 1): ");
    var minValueInput = Console.ReadLine();
    int? minValue = null;
    if (!string.IsNullOrWhiteSpace(minValueInput) && int.TryParse(minValueInput, out var min))
    {
        minValue = min;
    }

    Console.Write("Введите максимальное значение (Enter для значения по умолчанию 1000): ");
    var maxValueInput = Console.ReadLine();
    int? maxValue = null;
    if (!string.IsNullOrWhiteSpace(maxValueInput) && int.TryParse(maxValueInput, out var max))
    {
        maxValue = max;
    }

    var request = new
    {
        ArraySize = arraySize,
        MinValue = minValue,
        MaxValue = maxValue
    };

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await httpClient.PostAsync($"{apiBaseUrl}/array", content);
    var responseContent = await response.Content.ReadAsStringAsync();

    if (response.IsSuccessStatusCode)
    {
        var arrayResponse = JsonSerializer.Deserialize<ArrayResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Console.WriteLine($"\n✓ Случайный массив успешно создан!");
        Console.WriteLine($"ID: {arrayResponse?.Id}");
        var elements = arrayResponse?.Elements ?? Array.Empty<int>();
        var displayElements = elements.Length > 50 
            ? string.Join(", ", elements.Take(50)) + $" ... (всего {elements.Length} элементов)"
            : string.Join(", ", elements);
        Console.WriteLine($"Элементы: [{displayElements}]");
        Console.WriteLine($"Создан: {arrayResponse?.CreatedAt}");
    }
    else
    {
        Console.WriteLine($"\n✗ Ошибка создания массива: {responseContent}");
    }
}

async Task GetAllArraysAsync()
{
    var response = await httpClient.GetAsync($"{apiBaseUrl}/array");
    var responseContent = await response.Content.ReadAsStringAsync();

    if (response.IsSuccessStatusCode)
    {
        var arrays = JsonSerializer.Deserialize<List<ArrayResponse>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        if (arrays == null || arrays.Count == 0)
        {
            Console.WriteLine("\nУ вас пока нет сохраненных массивов.");
            return;
        }

        Console.WriteLine($"\n✓ Найдено массивов: {arrays.Count}\n");
        foreach (var arr in arrays)
        {
            var elements = arr.Elements ?? Array.Empty<int>();
            var displayElements = elements.Length > 20 
                ? string.Join(", ", elements.Take(20)) + $" ... (всего {elements.Length} элементов)"
                : string.Join(", ", elements);
            Console.WriteLine($"ID: {arr.Id} | Элементы: [{displayElements}] | Создан: {arr.CreatedAt}");
        }
    }
    else
    {
        Console.WriteLine($"\n✗ Ошибка получения массивов: {responseContent}");
    }
}

async Task GetArrayByIdAsync()
{
    Console.Write("Введите ID массива: ");
    if (!int.TryParse(Console.ReadLine(), out var arrayId))
    {
        Console.WriteLine("Неверный ID!");
        return;
    }

    var response = await httpClient.GetAsync($"{apiBaseUrl}/array/{arrayId}");
    var responseContent = await response.Content.ReadAsStringAsync();

    if (response.IsSuccessStatusCode)
    {
        var arrayResponse = JsonSerializer.Deserialize<ArrayResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var elements = arrayResponse?.Elements ?? Array.Empty<int>();
        var displayElements = elements.Length > 50 
            ? string.Join(", ", elements.Take(50)) + $" ... (всего {elements.Length} элементов)"
            : string.Join(", ", elements);
        Console.WriteLine($"\n✓ Массив найден:");
        Console.WriteLine($"ID: {arrayResponse?.Id}");
        Console.WriteLine($"Элементы: [{displayElements}]");
        Console.WriteLine($"Создан: {arrayResponse?.CreatedAt}");
        Console.WriteLine($"Обновлен: {arrayResponse?.UpdatedAt}");
    }
    else
    {
        Console.WriteLine($"\n✗ Ошибка получения массива: {responseContent}");
    }
}

async Task SortArrayAsync()
{
    Console.WriteLine("Выберите способ сортировки:");
    Console.WriteLine("1. Сортировать существующий массив по ID");
    Console.WriteLine("2. Сгенерировать новый массив и отсортировать");
    Console.Write("Ваш выбор: ");
    
    var choice = Console.ReadLine();
    
    object request;
    
    if (choice == "1")
    {
        Console.Write("Введите ID массива: ");
        if (!int.TryParse(Console.ReadLine(), out var arrayId))
        {
            Console.WriteLine("Неверный ID!");
            return;
        }
        request = new { ArrayId = arrayId };
    }
    else if (choice == "2")
    {
        Console.Write("Введите размер массива (1-10000): ");
        if (!int.TryParse(Console.ReadLine(), out var arraySize) || arraySize < 1 || arraySize > 10000)
        {
            Console.WriteLine("Неверный размер массива!");
            return;
        }

        Console.Write("Введите минимальное значение (Enter для значения по умолчанию 1): ");
        var minValueInput = Console.ReadLine();
        int? minValue = null;
        if (!string.IsNullOrWhiteSpace(minValueInput) && int.TryParse(minValueInput, out var min))
        {
            minValue = min;
        }

        Console.Write("Введите максимальное значение (Enter для значения по умолчанию 1000): ");
        var maxValueInput = Console.ReadLine();
        int? maxValue = null;
        if (!string.IsNullOrWhiteSpace(maxValueInput) && int.TryParse(maxValueInput, out var max))
        {
            maxValue = max;
        }
        
        request = new { ArraySize = arraySize, MinValue = minValue, MaxValue = maxValue };
    }
    else
    {
        Console.WriteLine("Неверный выбор!");
        return;
    }

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await httpClient.PostAsync($"{apiBaseUrl}/sort/heapsort", content);
    var responseContent = await response.Content.ReadAsStringAsync();

    if (response.IsSuccessStatusCode)
    {
        var sortResult = JsonSerializer.Deserialize<SortResult>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        Console.WriteLine("\n=== Результаты сортировки ===");
        var original = sortResult?.OriginalArray ?? Array.Empty<int>();
        var sorted = sortResult?.SortedArray ?? Array.Empty<int>();
        var originalDisplay = original.Length > 30 
            ? string.Join(", ", original.Take(30)) + $" ... (всего {original.Length} элементов)"
            : string.Join(", ", original);
        var sortedDisplay = sorted.Length > 30 
            ? string.Join(", ", sorted.Take(30)) + $" ... (всего {sorted.Length} элементов)"
            : string.Join(", ", sorted);
        Console.WriteLine($"Исходный массив: [{originalDisplay}]");
        Console.WriteLine($"Отсортированный массив: [{sortedDisplay}]");
        Console.WriteLine($"Время выполнения: {sortResult?.ExecutionTimeMs} мс");
        Console.WriteLine($"Дата сортировки: {sortResult?.SortDate}");
        Console.WriteLine("✓ Результаты сохранены в базу данных");
    }
    else
    {
        Console.WriteLine($"\n✗ Ошибка сортировки: {responseContent}");
    }
}

async Task GetSliceAsync()
{
    Console.Write("Введите ID массива: ");
    if (!int.TryParse(Console.ReadLine(), out var arrayId))
    {
        Console.WriteLine("Неверный ID!");
        return;
    }

    Console.Write("Введите начальный индекс (Enter для 0): ");
    var startInput = Console.ReadLine();
    int? start = null;
    if (!string.IsNullOrWhiteSpace(startInput) && int.TryParse(startInput, out var s))
    {
        start = s;
    }

    Console.Write("Введите конечный индекс (Enter для конца массива): ");
    var endInput = Console.ReadLine();
    int? end = null;
    if (!string.IsNullOrWhiteSpace(endInput) && int.TryParse(endInput, out var e))
    {
        end = e;
    }

    var queryParams = new List<string>();
    if (start.HasValue) queryParams.Add($"start={start.Value}");
    if (end.HasValue) queryParams.Add($"end={end.Value}");
    
    var url = $"{apiBaseUrl}/array/{arrayId}/slice";
    if (queryParams.Count > 0)
    {
        url += "?" + string.Join("&", queryParams);
    }

    var response = await httpClient.GetAsync(url);
    var responseContent = await response.Content.ReadAsStringAsync();

    if (response.IsSuccessStatusCode)
    {
        var result = JsonSerializer.Deserialize<SliceResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var slice = result?.Slice ?? Array.Empty<int>();
        Console.WriteLine($"\n✓ Срез массива: [{string.Join(", ", slice)}]");
    }
    else
    {
        Console.WriteLine($"\n✗ Ошибка получения среза: {responseContent}");
    }
}

async Task AddElementAsync(string position)
{
    Console.Write("Введите ID массива: ");
    if (!int.TryParse(Console.ReadLine(), out var arrayId))
    {
        Console.WriteLine("Неверный ID!");
        return;
    }

    Console.Write("Введите значение элемента: ");
    if (!int.TryParse(Console.ReadLine(), out var value))
    {
        Console.WriteLine("Неверное значение!");
        return;
    }

    object request;
    if (position == "after")
    {
        Console.Write("Введите индекс (элемент будет добавлен после этого индекса): ");
        if (!int.TryParse(Console.ReadLine(), out var index))
        {
            Console.WriteLine("Неверный индекс!");
            return;
        }
        request = new { Value = value, Position = position, Index = index };
    }
    else
    {
        request = new { Value = value, Position = position };
    }

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await httpClient.PostAsync($"{apiBaseUrl}/array/{arrayId}/add-element", content);
    var responseContent = await response.Content.ReadAsStringAsync();

    if (response.IsSuccessStatusCode)
    {
        var arrayResponse = JsonSerializer.Deserialize<ArrayResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var elements = arrayResponse?.Elements ?? Array.Empty<int>();
        var displayElements = elements.Length > 50 
            ? string.Join(", ", elements.Take(50)) + $" ... (всего {elements.Length} элементов)"
            : string.Join(", ", elements);
        Console.WriteLine($"\n✓ Элемент успешно добавлен!");
        Console.WriteLine($"ID массива: {arrayResponse?.Id}");
        Console.WriteLine($"Элементы: [{displayElements}]");
        Console.WriteLine($"Обновлен: {arrayResponse?.UpdatedAt}");
    }
    else
    {
        Console.WriteLine($"\n✗ Ошибка добавления элемента: {responseContent}");
    }
}

async Task DeleteArrayAsync()
{
    Console.Write("Введите ID массива для удаления: ");
    if (!int.TryParse(Console.ReadLine(), out var arrayId))
    {
        Console.WriteLine("Неверный ID!");
        return;
    }

    var response = await httpClient.DeleteAsync($"{apiBaseUrl}/array/{arrayId}");

    if (response.IsSuccessStatusCode)
    {
        Console.WriteLine($"\n✓ Массив с ID {arrayId} успешно удален!");
    }
    else
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"\n✗ Ошибка удаления массива: {responseContent}");
    }
}

class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

class ArrayResponse
{
    public int Id { get; set; }
    public int[] Elements { get; set; } = Array.Empty<int>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

class SortResult
{
    public int[] OriginalArray { get; set; } = Array.Empty<int>();
    public int[] SortedArray { get; set; } = Array.Empty<int>();
    public long ExecutionTimeMs { get; set; }
    public DateTime SortDate { get; set; }
}

class SliceResponse
{
    public int[] Slice { get; set; } = Array.Empty<int>();
}