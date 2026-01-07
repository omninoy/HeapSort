using Microsoft.Data.Sqlite;
using HeapSort.Models;
using System.Text.Json;

namespace HeapSort.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=heapsort.db";
        InitializeDatabase();
        EnableForeignKeys();
    }

    private void EnableForeignKeys()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
        command.ExecuteNonQuery();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Создание таблицы пользователей
        var createUsersTable = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT UNIQUE NOT NULL,
                PasswordHash TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            )";

        // Создание таблицы массивов
        var createArraysTable = @"
            CREATE TABLE IF NOT EXISTS Arrays (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Elements TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (UserId) REFERENCES Users(Id)
            )";

        // Создание таблицы истории сортировок
        var createSortHistoryTable = @"
            CREATE TABLE IF NOT EXISTS SortHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                ArrayId INTEGER,
                OriginalArray TEXT NOT NULL,
                SortedArray TEXT NOT NULL,
                ExecutionTimeMs INTEGER NOT NULL,
                SortDate TEXT NOT NULL,
                FOREIGN KEY (UserId) REFERENCES Users(Id),
                FOREIGN KEY (ArrayId) REFERENCES Arrays(Id)
            )";

        using var command1 = new SqliteCommand(createUsersTable, connection);
        command1.ExecuteNonQuery();

        using var command2 = new SqliteCommand(createArraysTable, connection);
        command2.ExecuteNonQuery();

        using var command3 = new SqliteCommand(createSortHistoryTable, connection);
        command3.ExecuteNonQuery();
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = new SqliteCommand(
            "SELECT Id, Username, PasswordHash, CreatedAt FROM Users WHERE Username = @Username",
            connection);
        command.Parameters.AddWithValue("@Username", username);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                CreatedAt = DateTime.Parse(reader.GetString(3))
            };
        }

        return null;
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = new SqliteCommand(
            "SELECT Id, Username, PasswordHash, CreatedAt FROM Users WHERE Id = @Id",
            connection);
        command.Parameters.AddWithValue("@Id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                CreatedAt = DateTime.Parse(reader.GetString(3))
            };
        }

        return null;
    }

    public async Task<User> CreateUserAsync(string username, string passwordHash)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = new SqliteCommand(
            "INSERT INTO Users (Username, PasswordHash, CreatedAt) VALUES (@Username, @PasswordHash, @CreatedAt); SELECT last_insert_rowid();",
            connection);
        command.Parameters.AddWithValue("@Username", username);
        command.Parameters.AddWithValue("@PasswordHash", passwordHash);
        command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("O"));

        var userId = Convert.ToInt32(await command.ExecuteScalarAsync());

        return new User
        {
            Id = userId,
            Username = username,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task UpdateUserPasswordAsync(int userId, string newPasswordHash)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = new SqliteCommand(
            "UPDATE Users SET PasswordHash = @PasswordHash WHERE Id = @Id",
            connection);
        command.Parameters.AddWithValue("@PasswordHash", newPasswordHash);
        command.Parameters.AddWithValue("@Id", userId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task SaveSortHistoryAsync(int userId, SortResult sortResult, int? arrayId = null)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = new SqliteCommand(
            "INSERT INTO SortHistory (UserId, ArrayId, OriginalArray, SortedArray, ExecutionTimeMs, SortDate) VALUES (@UserId, @ArrayId, @OriginalArray, @SortedArray, @ExecutionTimeMs, @SortDate)",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@ArrayId", arrayId.HasValue ? (object)arrayId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@OriginalArray", string.Join(",", sortResult.OriginalArray));
        command.Parameters.AddWithValue("@SortedArray", string.Join(",", sortResult.SortedArray));
        command.Parameters.AddWithValue("@ExecutionTimeMs", sortResult.ExecutionTimeMs);
        command.Parameters.AddWithValue("@SortDate", sortResult.SortDate.ToString("O"));

        await command.ExecuteNonQueryAsync();
    }

    // Методы для работы с массивами
    public async Task<ArrayData> CreateArrayAsync(int userId, int[] elements)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var elementsJson = JsonSerializer.Serialize(elements);
        var now = DateTime.UtcNow.ToString("O");

        var command = new SqliteCommand(
            "INSERT INTO Arrays (UserId, Elements, CreatedAt, UpdatedAt) VALUES (@UserId, @Elements, @CreatedAt, @UpdatedAt); SELECT last_insert_rowid();",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Elements", elementsJson);
        command.Parameters.AddWithValue("@CreatedAt", now);
        command.Parameters.AddWithValue("@UpdatedAt", now);

        var arrayId = Convert.ToInt32(await command.ExecuteScalarAsync());

        return new ArrayData
        {
            Id = arrayId,
            UserId = userId,
            Elements = elementsJson,
            CreatedAt = DateTime.Parse(now),
            UpdatedAt = DateTime.Parse(now)
        };
    }

    public async Task<List<ArrayData>> GetUserArraysAsync(int userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = new SqliteCommand(
            "SELECT Id, UserId, Elements, CreatedAt, UpdatedAt FROM Arrays WHERE UserId = @UserId ORDER BY CreatedAt DESC",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);

        var arrays = new List<ArrayData>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            arrays.Add(new ArrayData
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                Elements = reader.GetString(2),
                CreatedAt = DateTime.Parse(reader.GetString(3)),
                UpdatedAt = DateTime.Parse(reader.GetString(4))
            });
        }

        return arrays;
    }

    public async Task<ArrayData?> GetArrayByIdAsync(int arrayId, int userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = new SqliteCommand(
            "SELECT Id, UserId, Elements, CreatedAt, UpdatedAt FROM Arrays WHERE Id = @Id AND UserId = @UserId",
            connection);
        command.Parameters.AddWithValue("@Id", arrayId);
        command.Parameters.AddWithValue("@UserId", userId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ArrayData
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                Elements = reader.GetString(2),
                CreatedAt = DateTime.Parse(reader.GetString(3)),
                UpdatedAt = DateTime.Parse(reader.GetString(4))
            };
        }

        return null;
    }

    public async Task<ArrayData?> UpdateArrayAsync(int arrayId, int userId, int[] elements)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var elementsJson = JsonSerializer.Serialize(elements);
        var now = DateTime.UtcNow.ToString("O");

        var command = new SqliteCommand(
            "UPDATE Arrays SET Elements = @Elements, UpdatedAt = @UpdatedAt WHERE Id = @Id AND UserId = @UserId",
            connection);
        command.Parameters.AddWithValue("@Elements", elementsJson);
        command.Parameters.AddWithValue("@UpdatedAt", now);
        command.Parameters.AddWithValue("@Id", arrayId);
        command.Parameters.AddWithValue("@UserId", userId);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        if (rowsAffected == 0)
        {
            return null;
        }

        return await GetArrayByIdAsync(arrayId, userId);
    }

    public async Task<bool> DeleteArrayAsync(int arrayId, int userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Включаем поддержку внешних ключей для этого подключения
        await new SqliteCommand("PRAGMA foreign_keys = ON;", connection).ExecuteNonQueryAsync();

        // Сначала удаляем связанные записи из SortHistory
        var deleteHistoryCommand = new SqliteCommand(
            "DELETE FROM SortHistory WHERE ArrayId = @ArrayId",
            connection);
        deleteHistoryCommand.Parameters.AddWithValue("@ArrayId", arrayId);
        await deleteHistoryCommand.ExecuteNonQueryAsync();

        // Затем удаляем сам массив
        var command = new SqliteCommand(
            "DELETE FROM Arrays WHERE Id = @Id AND UserId = @UserId",
            connection);
        command.Parameters.AddWithValue("@Id", arrayId);
        command.Parameters.AddWithValue("@UserId", userId);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }
}
