using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace ProductManagementSystem.DesktopClient;

public partial class MainWindow : Window
{
    private readonly HttpClient _client = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            this.DragMove();
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        txtError.Text = "";
        btnLogin.IsEnabled = false;

        try
        {
            var payload = new
            {
                username = txtUsername.Text,
                password = txtPassword.Password
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("api/auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                var resultStr = await response.Content.ReadAsStringAsync();
                var result = JsonDocument.Parse(resultStr);
                var token = result.RootElement.GetProperty("accessToken").GetString();

                var dashboard = new DashboardWindow(token, txtUsername.Text);
                dashboard.Show();
                this.Close();
            }
            else
            {
                txtError.Text = "Login falhou: Credenciais inválidas.";
            }
        }
        catch (Exception)
        {
            txtError.Text = "Erro de conexão com o servidor. A API está rodando?";
        }
        finally
        {
            btnLogin.IsEnabled = true;
        }
    }
}
