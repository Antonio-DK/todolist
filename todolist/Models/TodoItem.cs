using System;
using System.ComponentModel;

namespace todolist.Models;

public class TodoItem : INotifyPropertyChanged
{
    private bool _done;

    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool Done
    {
        get => _done;
        set
        {
            if (_done != value)
            {
                _done = value;
                OnPropertyChanged(nameof(Done));
                OnPropertyChanged(nameof(DoneText));
            }
        }
    }

    // Binding label: "Da" ili "Ne"
    public string DoneText => Done ? "Da" : "Ne";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
