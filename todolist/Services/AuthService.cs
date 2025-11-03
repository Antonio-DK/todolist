using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using todolist.Models;

namespace todolist.Services;

public static class AuthService
{
    private static readonly List<User> _users = new();
    private static int _nextId = 1;

    public static string GenerateSalt()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public static string HashPassword(string password, string salt)
    {
        using var sha = SHA256.Create();
        var raw = Encoding.UTF8.GetBytes((password ?? "") + salt);
        var hashed = sha.ComputeHash(raw);
        return Convert.ToBase64String(hashed);
    }

    public static Task<bool> RegisterAsync(string displayName, string email, string password)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) return Task.FromResult(false);

        if (_users.Any(u => u.Email == email)) return Task.FromResult(false);

        var salt = GenerateSalt();
        var hash = HashPassword(password, salt);

        var user = new User
        {
            Id = _nextId++,
            DisplayName = displayName ?? "",
            Email = email,
            Salt = salt,
            PasswordHash = hash
        };

        _users.Add(user);
        return Task.FromResult(true);
    }

    public static Task<User?> LoginAsync(string email, string password)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        var user = _users.FirstOrDefault(u => u.Email == email);
        if (user == null) return Task.FromResult<User?>(null);

        var hash = HashPassword(password, user.Salt);
        if (hash == user.PasswordHash) return Task.FromResult<User?>(user);

        return Task.FromResult<User?>(null);
    }
}
