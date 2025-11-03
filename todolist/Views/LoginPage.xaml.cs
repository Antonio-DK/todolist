using Microsoft.Maui.Controls;
using todolist.Services;

namespace todolist.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        MessageLabel.Text = string.Empty;
        var email = EmailEntry.Text?.Trim();
        var pwd = PasswordEntry.Text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pwd))
        {
            MessageLabel.Text = "Unesi email i lozinku.";
            return;
        }

        var user = await AuthService.LoginAsync(email, pwd);
        if (user == null)
        {
            MessageLabel.Text = "Pogrešan email ili lozinka.";
            return;
        }

        await Navigation.PushAsync(new WelcomePage(user));
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }
}
