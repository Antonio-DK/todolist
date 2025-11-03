using Microsoft.Maui.Controls;
using todolist.Models;

namespace todolist.Views;

public partial class WelcomePage : ContentPage
{
    private readonly User _user;
    public WelcomePage(User user)
    {
        InitializeComponent();
        _user = user;
        WelcomeLabel.Text = $"Bok, {user.DisplayName}!";
        EmailLabel.Text = user.Email;
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await Navigation.PopToRootAsync();
    }
}
