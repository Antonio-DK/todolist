using Microsoft.Maui.Controls;
using todolist.Services;

namespace todolist.Views;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        MessageLabel.Text = string.Empty;
        var name = DisplayNameEntry.Text?.Trim();
        var email = EmailEntry.Text?.Trim();
        var pwd = PasswordEntry.Text;
        var conf = ConfirmEntry.Text;

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pwd))
        {
            MessageLabel.Text = "Popuni sva polja.";
            return;
        }

        if (pwd != conf)
        {
            MessageLabel.Text = "Lozinke se ne poklapaju.";
            return;
        }

        var ok = await AuthService.RegisterAsync(name, email, pwd);
        if (!ok)
        {
            MessageLabel.Text = "Korisnik s tim emailom već postoji ili loš unos.";
            return;
        }

        await DisplayAlert("Uspjeh", "Registracija uspješna. Prijavi se.", "OK");
        await Navigation.PopAsync();
    }
}
