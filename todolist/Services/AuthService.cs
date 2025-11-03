using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using todolist.Models;

namespace todolist.Services;

public static class AuthService
{
    private static readonly string _filePath = Path.Combine(FileSystem.AppDataDirectory, "users.json");
    private static readonly List<User> _users = new();
    private static int _nextId = 1;
    private static bool _loaded = false;

    private static async Task EnsureLoadedAsync()
    {
        if (_loaded) return;

        try
        {
            if (File.Exists(_filePath))
            {
                var json = await File.ReadAllTextAsync(_filePath);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var list = JsonSerializer.Deserialize<List<User>>(json, opts);
                if (list != null && list.Count > 0)
                {
                    _users.AddRange(list);
                    _nextId = _users.Max(u => u.Id) + 1;
                }
            }
        }
        catch
        {
            // ignoriraj korumpiran file i nastavi prazno
            _users.Clear();
            _nextId = 1;
        }

        _loaded = true;
    }

    private static async Task SaveAsync()
    {
        try
        {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_users, opts);
            // osiguraj folder postoji (FileSystem.AppDataDirectory postoji po definiciji)
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch
        {
            // ako ne može zapisati, ignoriraj (možeš dodati logging)
        }
    }

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

    public static async Task<bool> RegisterAsync(string displayName, string email, string password)
    {
        await EnsureLoadedAsync();

        email = (email ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) return false;

        if (_users.Any(u => u.Email == email)) return false;

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
        await SaveAsync();
        return true;
    }

    public static async Task<User?> LoginAsync(string email, string password)
    {
        await EnsureLoadedAsync();

        email = (email ?? "").Trim().ToLowerInvariant();
        var user = _users.FirstOrDefault(u => u.Email == email);
        if (user == null) return null;

        var hash = HashPassword(password, user.Salt);
        return hash == user.PasswordHash ? user : null;
    }

    // Opcionalno: metoda za dobivanje svih usera (npr. za debug)
    public static async Task<List<User>> GetAllUsersAsync()
    {
        await EnsureLoadedAsync();
        return _users.ToList();
    }
}
