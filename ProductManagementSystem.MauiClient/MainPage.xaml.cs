using System.Text;
using System.Text.Json;

namespace ProductManagementSystem.MauiClient;

public partial class MainPage : ContentPage
{
    private readonly HttpClient _client = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };

    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        txtError.Text = "";
        AuthBtn.IsEnabled = false;

        try
        {
            var payload = new { username = txtUsername.Text, password = txtPassword.Text };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("api/auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                var resultStr = await response.Content.ReadAsStringAsync();
                var result = JsonDocument.Parse(resultStr);
                var token = result.RootElement.GetProperty("accessToken").GetString();

                await Navigation.PushAsync(new DashboardPage(token, txtUsername.Text));
            }
            else
            {
                txtError.Text = "Login falhou: Credenciais inválidas.";
            }
        }
        catch (Exception)
        {
            txtError.Text = "Erro de conexão API rodando?";
        }
        finally
        {
            AuthBtn.IsEnabled = true;
        }
    }
}
