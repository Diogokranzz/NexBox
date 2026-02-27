using System.Net.Http.Headers;
using System.Text.Json;

namespace ProductManagementSystem.MauiClient;

public partial class DashboardPage : ContentPage
{
    private readonly HttpClient _client;

    public DashboardPage(string token, string username)
    {
        InitializeComponent();
        txtUser.Text = $"Olá, {username}";

        _client = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        LoadProducts();
    }

    private async void LoadProducts()
    {
        try
        {
            var response = await _client.GetAsync("api/products?pageNumber=1&pageSize=50");
            if (response.IsSuccessStatusCode)
            {
                var resultStr = await response.Content.ReadAsStringAsync();
                var result = JsonDocument.Parse(resultStr);
                var data = result.RootElement.GetProperty("data");
                
                var products = JsonSerializer.Deserialize<List<ProductModel>>(data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                productsCollection.ItemsSource = products;
            }
            else
            {
                await DisplayAlert("Erro", "Erro ao carregar produtos. Token expirou?", "OK");
            }
        }
        catch (Exception)
        {
            await DisplayAlert("Erro", "Falha de conexão com a API.", "OK");
        }
    }

    private void OnLoadProductsClicked(object sender, EventArgs e)
    {
        LoadProducts();
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}

public class ProductModel
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public decimal Preco { get; set; }
    public int Estoque { get; set; }
}
