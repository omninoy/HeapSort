using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using HeapSort.Models;
using BCrypt.Net;

namespace HeapSort.Services;

public class AuthService
{
    private readonly IConfiguration _configuration;
    private readonly DatabaseService _databaseService;

    public AuthService(IConfiguration configuration, DatabaseService databaseService)
    {
        _configuration = configuration;
        _databaseService = databaseService;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        // Проверка существования пользователя
        var existingUser = await _databaseService.GetUserByUsernameAsync(request.Username);
        if (existingUser != null)
        {
            return null; // Пользователь уже существует
        }

        // Хеширование пароля
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Создание пользователя
        var user = await _databaseService.CreateUserAsync(request.Username, passwordHash);

        // Генерация токена
        var token = GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            Username = user.Username
        };
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _databaseService.GetUserByUsernameAsync(request.Username);
        if (user == null)
        {
            return null; // Пользователь не найден
        }

        // Проверка пароля
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null; // Неверный пароль
        }

        // Генерация токена
        var token = GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            Username = user.Username
        };
    }

    public async Task<AuthResponse?> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _databaseService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return null; // Пользователь не найден
        }

        // Проверка старого пароля
        if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
        {
            return null; // Неверный старый пароль
        }

        // Хеширование нового пароля
        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        // Обновление пароля
        await _databaseService.UpdateUserPasswordAsync(userId, newPasswordHash);

        // Обновляем объект пользователя для генерации нового токена
        user.PasswordHash = newPasswordHash;
        var token = GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            Username = user.Username
        };
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "HeapSortAPI",
            audience: _configuration["Jwt:Audience"] ?? "HeapSortClient",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
