using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using HeapSort.Controllers;
using HeapSort.Models;
using HeapSort.Services;
using BCrypt.Net;

namespace HeapSort.Tests;

[TestClass]
public class AuthControllerTests
{
    private AuthController _authController = null!;
    private AuthService _authService = null!;
    private DatabaseService _databaseService = null!;
    private IConfiguration _configuration = null!;
    private string _testDbPath = null!;

    [TestInitialize]
    public void Setup()
    {
        // Создание in-memory SQLite базы данных для тестов
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.db");
        
        var configDictionary = new Dictionary<string, string?>
        {
            { "ConnectionStrings:DefaultConnection", $"Data Source={_testDbPath}" },
            { "Jwt:Key", "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!" },
            { "Jwt:Issuer", "HeapSortAPI" },
            { "Jwt:Audience", "HeapSortClient" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDictionary)
            .Build();

        _databaseService = new DatabaseService(_configuration);
        _authService = new AuthService(_configuration, _databaseService);
        _authController = new AuthController(_authService);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Удаление тестовой базы данных после тестов
        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch { }
        }
    }

    [TestMethod]
    public async Task Register_WithValidCredentials_ReturnsOk()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Password = "TestPassword123!"
        };

        // Act
        var result = await _authController.Register(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var okResult = result as OkObjectResult;
        Assert.IsNotNull(okResult);
        var response = okResult.Value as AuthResponse;
        Assert.IsNotNull(response);
        Assert.AreEqual("testuser", response.Username);
        Assert.IsFalse(string.IsNullOrEmpty(response.Token));
    }

    [TestMethod]
    public async Task Register_WithEmptyUsername_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "",
            Password = "TestPassword123!"
        };

        // Act
        var result = await _authController.Register(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task Register_WithNullUsername_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = null!,
            Password = "TestPassword123!"
        };

        // Act
        var result = await _authController.Register(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task Register_WithWhitespaceUsername_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "   ",
            Password = "TestPassword123!"
        };

        // Act
        var result = await _authController.Register(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task Register_WithEmptyPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Password = ""
        };

        // Act
        var result = await _authController.Register(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task Register_WithNullPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Password = null!
        };

        // Act
        var result = await _authController.Register(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task Register_WithWhitespacePassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Password = "   "
        };

        // Act
        var result = await _authController.Register(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task Register_WithDuplicateUsername_ReturnsConflict()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "duplicateuser",
            Password = "TestPassword123!"
        };

        // Первая регистрация
        await _authController.Register(request);

        // Act - попытка регистрации с тем же именем
        var result = await _authController.Register(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(ConflictObjectResult));
    }

    [TestMethod]
    public async Task Register_PasswordIsHashed()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "hashuser",
            Password = "TestPassword123!"
        };

        // Act
        await _authController.Register(request);

        // Assert - проверяем, что пароль хранится в хешированном виде
        var user = await _databaseService.GetUserByUsernameAsync("hashuser");
        Assert.IsNotNull(user);
        Assert.AreNotEqual("TestPassword123!", user.PasswordHash);
        Assert.IsTrue(BCrypt.Net.BCrypt.Verify("TestPassword123!", user.PasswordHash));
    }

    [TestMethod]
    public async Task Register_UserStoredInDatabase()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "dbuser",
            Password = "TestPassword123!"
        };

        // Act
        await _authController.Register(request);

        // Assert
        var user = await _databaseService.GetUserByUsernameAsync("dbuser");
        Assert.IsNotNull(user);
        Assert.AreEqual("dbuser", user.Username);
        Assert.IsTrue(user.Id > 0);
    }

    [TestMethod]
    public async Task Register_ReturnsJwtToken()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "tokenuser",
            Password = "TestPassword123!"
        };

        // Act
        var result = await _authController.Register(request) as OkObjectResult;
        var response = result?.Value as AuthResponse;

        // Assert
        Assert.IsNotNull(response);
        Assert.IsFalse(string.IsNullOrEmpty(response.Token));
        Assert.IsTrue(response.Token.Length > 0);
    }

    [TestMethod]
    public async Task Register_WithShortUsername_ShouldAccept()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "ab",  // Короткое имя (текущие ограничения не проверяют длину)
            Password = "TestPassword123!"
        };

        // Act
        var result = await _authController.Register(request);

        // Assert - текущая реализация принимает короткое имя
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public async Task Register_WithLongUsername_ShouldAccept()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = new string('a', 100),  // Длинное имя (текущие ограничения не проверяют длину)
            Password = "TestPassword123!"
        };

        // Act
        var result = await _authController.Register(request);

        // Assert - текущая реализация принимает длинное имя
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public async Task Register_WithShortPassword_ShouldAccept()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "shortpassuser",
            Password = "123"  // Короткий пароль (текущие ограничения не проверяют длину)
        };

        // Act
        var result = await _authController.Register(request);

        // Assert - текущая реализация принимает короткий пароль
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public async Task Register_WithSpecialCharactersInUsername_ShouldAccept()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "user@123",
            Password = "TestPassword123!"
        };

        // Act
        var result = await _authController.Register(request);

        // Assert - текущая реализация принимает специальные символы
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }
}

