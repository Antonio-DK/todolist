using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using todolist.Models;
using Microsoft.Maui.Storage; // FileSystem

namespace todolist.Services;

public static class TodoService
{
    private static readonly string _filePath = Path.Combine(FileSystem.AppDataDirectory, "todos.json");
    private static List<TodoItem> _todos = new();
    private static int _nextId = 1;
    private static bool _loaded = false;

    private static async Task EnsureLoadedAsync()
    {
        if (_loaded) return;

        if (File.Exists(_filePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_filePath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var data = JsonSerializer.Deserialize<List<TodoItem>>(json);
                    if (data != null)
                    {
                        _todos = data;
                        _nextId = _todos.Count > 0 ? _todos.Max(t => t.Id) + 1 : 1;
                    }
                }
            }
            catch
            {
                // ako je korumpiran file => resetiraj u prazan
                _todos = new List<TodoItem>();
                _nextId = 1;
            }
        }

        _loaded = true;
    }

    private static async Task SaveAsync()
    {
        // osiguraj directory (obično postoji)
        try
        {
            var json = JsonSerializer.Serialize(_todos, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch
        {
            // ne baci; moguće da nema prava na zapis u nekim okruženjima
        }
    }

    public static async Task<List<TodoItem>> GetTodosForUserAsync(int userId)
    {
        await EnsureLoadedAsync();
        return _todos.Where(t => t.UserId == userId)
                     .OrderByDescending(t => t.CreatedAt)
                     .ToList();
    }

    public static async Task<TodoItem> AddTodoAsync(int userId, string title, string? description)
    {
        await EnsureLoadedAsync();
        var item = new TodoItem
        {
            Id = _nextId++,
            UserId = userId,
            Title = title ?? string.Empty,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            Done = false
        };
        _todos.Add(item);
        await SaveAsync();
        return item;
    }

    public static async Task<bool> DeleteTodoAsync(int id, int userId)
    {
        await EnsureLoadedAsync();
        var item = _todos.FirstOrDefault(t => t.Id == id && t.UserId == userId);
        if (item == null) return false;
        _todos.Remove(item);
        await SaveAsync();
        return true;
    }

    public static async Task<bool> ToggleDoneAsync(int id, int userId)
    {
        await EnsureLoadedAsync();
        var item = _todos.FirstOrDefault(t => t.Id == id && t.UserId == userId);
        if (item == null) return false;
        item.Done = !item.Done;
        await SaveAsync();
        return true;
    }

    public static async Task ClearUserTodosAsync(int userId)
    {
        await EnsureLoadedAsync();
        _todos.RemoveAll(t => t.UserId == userId);
        await SaveAsync();
    }
}
