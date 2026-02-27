using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace ProductManagementSystem.DesktopClient;

public partial class AddProductWindow : Window
{
    private readonly HttpClient _client;
    
    public AddProductWindow(string? token, bool isPortuguese)
    {
        InitializeComponent();
        
        _client = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
        if (!string.IsNullOrEmpty(token))
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (isPortuguese)
        {
            txtTitle.Text = "Adicionar Novo Produto";
            btnSave.Content = "SALVAR PRODUTO";
            MaterialDesignThemes.Wpf.HintAssist.SetHint(txtNome, "Nome do Produto");
            MaterialDesignThemes.Wpf.HintAssist.SetHint(txtPreco, "Preço (R$)");
            MaterialDesignThemes.Wpf.HintAssist.SetHint(txtEstoque, "Unidades em Estoque");
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
        this.Close();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            this.DragMove();
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        txtError.Text = "";
        btnSave.IsEnabled = false;

        if (!decimal.TryParse(txtPreco.Text, out decimal preco))
            txtError.Text = btnSave.Content.ToString() == "SALVAR PRODUTO" ? "Preço inválido." : "Invalid price.";
        else if (!int.TryParse(txtEstoque.Text, out int estoque))
            txtError.Text = btnSave.Content.ToString() == "SALVAR PRODUTO" ? "Estoque inválido." : "Invalid stock.";
        else
        {
            try
            {
                var seed = txtNome.Text.Replace(" ", "").ToLowerInvariant();
                var payload = new 
                { 
                    nome = txtNome.Text, 
                    preco = preco,
                    estoque = estoque,
                    imageUrl = $"https://picsum.photos/seed/{Uri.EscapeDataString(seed)}/800/800"
                };
                
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _client.PostAsync("api/products", content);
                
                if (response.IsSuccessStatusCode)
                {
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    txtError.Text = btnSave.Content.ToString() == "SALVAR PRODUTO" ? "Erro ao salvar produto no servidor." : "Error saving product to server.";
                }
            }
            catch (Exception)
            {
                txtError.Text = btnSave.Content.ToString() == "SALVAR PRODUTO" ? "Erro de conexão API." : "API connection error.";
            }
        }
        btnSave.IsEnabled = true;
    }
}
