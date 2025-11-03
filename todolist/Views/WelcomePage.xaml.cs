using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.Maui.ApplicationModel; // MainThread
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

        TodosCollectionView.ItemsSource = _todos;

        _ = LoadTodosAsync();
    }

    private async System.Threading.Tasks.Task LoadTodosAsync()
    {
        try
        {
            var list = await TodoService.GetTodosForUserAsync(_user.Id);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _todos.Clear();
                foreach (var t in list) _todos.Add(t);
            });
            Debug.WriteLine($"Loaded {list.Count} todos for user {_user.Id}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadTodosAsync error: {ex}");
        }
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        var title = TitleEntry.Text?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            await DisplayAlert("Greška", "Unesi naslov zadatka.", "OK");
            return;
        }

        var desc = DescriptionEditor.Text?.Trim();

        try
        {
            var added = await TodoService.AddTodoAsync(_user.Id, title, desc);
            MainThread.BeginInvokeOnMainThread(() => _todos.Insert(0, added));
            TitleEntry.Text = string.Empty;
            DescriptionEditor.Text = string.Empty;

            Debug.WriteLine($"Added todo id={added.Id}");
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
            if (!(sender is Button b)) return;
            var itemCtx = b.BindingContext as TodoItem;
            if (itemCtx == null) return;

            int id = itemCtx.Id;
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
            if (!(sender is Button b)) return;

            // direktno iz BindingContext dohvatimo item — ovo je najpouzdanije
            var item = b.BindingContext as TodoItem;
            if (item == null)
            {
                Debug.WriteLine("OnToggleDoneClicked: BindingContext nije TodoItem");
                return;
            }

            var id = item.Id;
            Debug.WriteLine($"OnToggleDoneClicked: id={id} before Done={item.Done}");

            // pozovemo servis koji sam toggla i sprema
            var ok = await TodoService.ToggleDoneAsync(id, _user.Id);
            Debug.WriteLine($"TodoService.ToggleDoneAsync returned {ok} for id={id}");

            if (ok)
            {
                // dohvatimo ažurirani objekt iz servisa (sigurno stanje)
                var list = await TodoService.GetTodosForUserAsync(_user.Id);
                var updated = list.FirstOrDefault(t => t.Id == id);

                if (updated != null)
                {
                    // update UI na glavnom threadu — ovo aktivira INotifyPropertyChanged
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        item.Done = updated.Done;
                        Debug.WriteLine($"UI updated item id={id} Done={item.Done}");
                    });
                }
                else
                {
                    Debug.WriteLine($"Updated item not found in service after toggle: id={id}");
                }
            }
            else
            {
                Debug.WriteLine($"Toggle failed in service for id={id}");
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
            if (b.BindingContext is TodoItem ti) return ti.Id;
        }
        return -1;
    }
}
