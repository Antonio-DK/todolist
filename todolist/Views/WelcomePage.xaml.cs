using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using todolist.Models;
using todolist.Services;

namespace todolist.Views;

public partial class WelcomePage : ContentPage
{
    private readonly User _user;
    private readonly ObservableCollection<TodoItem> _todos = new();

    public WelcomePage(User user)
    {
        InitializeComponent();
        _user = user;

        WelcomeLabel.Text = $"Bok, {user.DisplayName}!";
        EmailLabel.Text = user.Email;

        // Postavi ItemsSource jednom - nećemo zamjenjivati ovu instancu
        TodosCollectionView.ItemsSource = _todos;

        // Debug: ispiši path gdje se sprema json
        Debug.WriteLine($"AppDataDirectory: {FileSystem.AppDataDirectory}");

        // Učitaj postojeće zadatke
        _ = LoadTodosAsync();
    }

    private async System.Threading.Tasks.Task LoadTodosAsync()
    {
        try
        {
            var list = await TodoService.GetTodosForUserAsync(_user.Id);
            Debug.WriteLine($"LoadTodosAsync: found {list.Count} todos for user {_user.Id}");
            // update kolekcije na main thread
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                _todos.Clear();
                foreach (var t in list) _todos.Add(t);
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadTodosAsync error: {ex}");
        }
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        var title = TitleEntry.Text?.Trim();
        var desc = DescriptionEditor.Text?.Trim();

        if (string.IsNullOrEmpty(title))
        {
            await DisplayAlert("Greška", "Unesi naslov zadatka.", "OK");
            return;
        }

        try
        {
            var added = await TodoService.AddTodoAsync(_user.Id, title, desc);
            Debug.WriteLine($"Added todo id={added.Id} for user {_user.Id}");

            // update UI na main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _todos.Insert(0, added);
            });

            TitleEntry.Text = string.Empty;
            DescriptionEditor.Text = string.Empty;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OnAddClicked error: {ex}");
            await DisplayAlert("Greška", "Neuspjelo dodavanje zadatka.", "OK");
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        try
        {
            int id = ParseCommandParameter(sender);
            var ok = await TodoService.DeleteTodoAsync(id, _user.Id);
            Debug.WriteLine($"Delete todo id={id} ok={ok}");
            if (ok)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var toRemove = _todos.FirstOrDefault(t => t.Id == id);
                    if (toRemove != null) _todos.Remove(toRemove);
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OnDeleteClicked error: {ex}");
        }
    }

    private async void OnToggleDoneClicked(object sender, EventArgs e)
    {
        try
        {
            int id = ParseCommandParameter(sender);
            var ok = await TodoService.ToggleDoneAsync(id, _user.Id);
            Debug.WriteLine($"Toggle todo id={id} ok={ok}");
            if (ok)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var item = _todos.FirstOrDefault(t => t.Id == id);
                    if (item != null)
                    {
                        item.Done = !item.Done;
                        var idx = _todos.IndexOf(item);
                        _todos.RemoveAt(idx);
                        _todos.Insert(idx, item);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OnToggleDoneClicked error: {ex}");
        }
    }

    private async void OnClearAllClicked(object sender, EventArgs e)
    {
        try
        {
            await TodoService.ClearUserTodosAsync(_user.Id);
            MainThread.BeginInvokeOnMainThread(() => _todos.Clear());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OnClearAllClicked error: {ex}");
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await Navigation.PopToRootAsync();
    }

    private int ParseCommandParameter(object sender)
    {
        if (sender is Button b)
        {
            var param = b.CommandParameter;
            if (param is int i) return i;
            if (param is string s && int.TryParse(s, out var parsed)) return parsed;
            // možda binding nije postavljen - pokušaj dohvatiti Id iz BindingContext
            if (b.BindingContext is TodoItem ti) return ti.Id;
        }
        return -1;
    }
}
