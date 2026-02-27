using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProductManagementSystem.DesktopClient;

public partial class DashboardWindow : Window
{
    private readonly HttpClient _client;
    private readonly List<ProductModel> _cart = new();
    private string _currentPage = "dashboard";

    public DashboardWindow(string? token, string? username)
    {
        InitializeComponent();
        txtUser.Text = username ?? "Admin";

        _client = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
        if (token != null)
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        txtSmtpUser.Text = "diogokranz11@gmail.com";
        txtSmtpHost.Text = "smtp.gmail.com";

        LoadProducts();
    }

    private void SetActivePage(string page)
    {
        _currentPage = page;
        pageDashboard.Visibility = page == "dashboard" ? Visibility.Visible : Visibility.Collapsed;
        pageOrders.Visibility = page == "orders" ? Visibility.Visible : Visibility.Collapsed;
        pageAnalytics.Visibility = page == "analytics" ? Visibility.Visible : Visibility.Collapsed;
        pageCustomers.Visibility = page == "customers" ? Visibility.Visible : Visibility.Collapsed;
        pageSettings.Visibility = page == "settings" ? Visibility.Visible : Visibility.Collapsed;

        var activeFg = Brushes.White;
        var inactiveFg = new SolidColorBrush(Color.FromRgb(200, 205, 210));
        var activeIconFg = Brushes.White;
        var inactiveIconFg = new SolidColorBrush(Color.FromRgb(140, 148, 156));
        var activeBg = new SolidColorBrush(Color.FromRgb(230, 87, 10));
        var transparentBg = Brushes.Transparent;

        SetNavStyle(navDashboard, txtNavDashboard, page == "dashboard", activeFg, inactiveFg, activeIconFg, inactiveIconFg, activeBg, transparentBg);
        SetNavStyle(navOrders, txtNavOrders, page == "orders", activeFg, inactiveFg, activeIconFg, inactiveIconFg, activeBg, transparentBg);
        SetNavStyle(navAnalytics, txtNavAnalytics, page == "analytics", activeFg, inactiveFg, activeIconFg, inactiveIconFg, activeBg, transparentBg);
        SetNavStyle(navCustomers, txtNavCustomers, page == "customers", activeFg, inactiveFg, activeIconFg, inactiveIconFg, activeBg, transparentBg);
        SetNavStyle(navSettings, txtNavSettings, page == "settings", activeFg, inactiveFg, activeIconFg, inactiveIconFg, activeBg, transparentBg);
    }

    private static void SetNavStyle(System.Windows.Controls.Border border, TextBlock text, bool isActive, Brush activeFg, Brush inactiveFg, Brush activeIcon, Brush inactiveIcon, Brush activeBg, Brush inactiveBg)
    {
        border.Background = isActive ? activeBg : inactiveBg;
        text.Foreground = isActive ? activeFg : inactiveFg;

        if (border.Child is StackPanel sp && sp.Children[0] is MaterialDesignThemes.Wpf.PackIcon icon)
            icon.Foreground = isActive ? activeIcon : inactiveIcon;
    }

    private void Nav_Dashboard(object sender, MouseButtonEventArgs e) { SetActivePage("dashboard"); }
    private void Nav_Orders(object sender, MouseButtonEventArgs e) { SetActivePage("orders"); LoadOrders(); }
    private void Nav_Analytics(object sender, MouseButtonEventArgs e) { SetActivePage("analytics"); LoadAnalytics(); }
    private void Nav_Customers(object sender, MouseButtonEventArgs e) { SetActivePage("customers"); LoadCustomers(); }
    private void Nav_Settings(object sender, MouseButtonEventArgs e) { SetActivePage("settings"); CheckApiStatus(); }

    private void Close_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    private void Window_MouseDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) this.DragMove(); }

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
                if (products != null)
                {
                    dataGridProducts.ItemsSource = products;
                }
            }
        }
        catch (Exception)
        {
            MessageBox.Show("Falha de conexao com a API.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadOrders()
    {
        try
        {
            var response = await _client.GetAsync("api/orders");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var orders = JsonSerializer.Deserialize<List<OrderModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (orders != null)
                {
                    var viewModels = orders.Select(o => new OrderViewModel
                    {
                        IdLabel = $"#{o.Id}",
                        CustomerName = o.CustomerName ?? "—",
                        DateLabel = o.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                        ItemsLabel = $"{o.ItemCount} item(s)",
                        TotalLabel = $"R$ {o.Total:F2}",
                        StatusLabel = "Concluido"
                    }).OrderByDescending(o => o.DateLabel).ToList();
                    listOrders.ItemsSource = viewModels;
                }
            }
            else
            {
                listOrders.ItemsSource = new List<OrderViewModel>();
            }
        }
        catch
        {
            listOrders.ItemsSource = new List<OrderViewModel>
            {
                new() { IdLabel = "—", CustomerName = "Nenhum pedido ainda", DateLabel = "", ItemsLabel = "", TotalLabel = "", StatusLabel = "" }
            };
        }
    }

    private async void LoadAnalytics()
    {
        try
        {
            var prodResp = await _client.GetAsync("api/products?pageNumber=1&pageSize=50");
            int productCount = 0;
            List<ProductModel> allProducts = new();
            if (prodResp.IsSuccessStatusCode)
            {
                var json = await prodResp.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");
                allProducts = JsonSerializer.Deserialize<List<ProductModel>>(data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                productCount = allProducts.Count;
            }
            txtKpiProducts.Text = productCount.ToString();

            var ordResp = await _client.GetAsync("api/orders");
            decimal totalRevenue = 0;
            int orderCount = 0;
            if (ordResp.IsSuccessStatusCode)
            {
                var json = await ordResp.Content.ReadAsStringAsync();
                var orders = JsonSerializer.Deserialize<List<OrderModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                orderCount = orders.Count;
                totalRevenue = orders.Sum(o => o.Total);
            }

            txtKpiRevenue.Text = $"R$ {totalRevenue:F2}";
            txtKpiOrders.Text = orderCount.ToString();
            txtKpiAvg.Text = orderCount > 0 ? $"R$ {(totalRevenue / orderCount):F2}" : "R$ 0,00";

            var topProducts = allProducts
                .OrderByDescending(p => p.Preco)
                .Take(5)
                .Select((p, i) => new TopProductViewModel
                {
                    Rank = (i + 1).ToString(),
                    Name = p.Nome,
                    QtySold = $"R$ {p.Preco:F2}",
                    BarWidth = Math.Max(10, (int)(p.Preco / (allProducts.Max(x => x.Preco) > 0 ? allProducts.Max(x => x.Preco) : 1) * 100))
                }).ToList();
            listTopProducts.ItemsSource = topProducts;
        }
        catch
        {
            txtKpiRevenue.Text = "—";
            txtKpiOrders.Text = "—";
        }
    }

    private async void LoadCustomers()
    {
        try
        {
            var response = await _client.GetAsync("api/orders");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var orders = JsonSerializer.Deserialize<List<OrderModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                var customers = orders
                    .Where(o => !string.IsNullOrEmpty(o.CustomerName))
                    .GroupBy(o => o.CustomerName)
                    .Select(g => new CustomerViewModel
                    {
                        Name = g.Key ?? "—",
                        AvatarUrl = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(g.Key ?? "U")}&background=e6570a&color=fff&size=80",
                        OrderCountLabel = $"{g.Count()} pedido(s)",
                        TotalSpent = $"R$ {g.Sum(o => o.Total):F2}"
                    })
                    .OrderByDescending(c => c.TotalSpent)
                    .ToList();

                listCustomers.ItemsSource = customers;
            }
        }
        catch
        {
            listCustomers.ItemsSource = new List<CustomerViewModel>();
        }
    }

    private async void CheckApiStatus()
    {
        try
        {
            var resp = await _client.GetAsync("api/products?pageNumber=1&pageSize=1");
            txtApiStatus.Text = resp.IsSuccessStatusCode ? "Status: Online" : "Status: Offline";
            txtApiStatus.Foreground = resp.IsSuccessStatusCode
                ? new SolidColorBrush(Color.FromRgb(22, 163, 74))
                : new SolidColorBrush(Color.FromRgb(220, 38, 38));
        }
        catch
        {
            txtApiStatus.Text = "Status: Offline";
            txtApiStatus.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
        }
    }

    private void SaveSmtp_Click(object sender, RoutedEventArgs e)
    {
        Environment.SetEnvironmentVariable("SMTP_HOST", txtSmtpHost.Text);
        Environment.SetEnvironmentVariable("SMTP_PORT", txtSmtpPort.Text);
        Environment.SetEnvironmentVariable("SMTP_USER", txtSmtpUser.Text);
        Environment.SetEnvironmentVariable("SMTP_PASS", txtSmtpPass.Password);

        MessageBox.Show("Configurações SMTP salvas com sucesso!", "SMTP", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void LoadProducts_Click(object sender, RoutedEventArgs e) => LoadProducts();

    private void NewProduct_Click(object sender, RoutedEventArgs e)
    {
        var token = _client.DefaultRequestHeaders.Authorization?.Parameter;
        var addDialog = new AddProductWindow(token, true);
        if (addDialog.ShowDialog() == true) LoadProducts();
    }

    private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int id)
        {
            var result = MessageBox.Show("Voce deseja mesmo excluir este produto?", "Confirmar Exclusao", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var response = await _client.DeleteAsync($"api/products/{id}");
                    if (response.IsSuccessStatusCode) LoadProducts();
                    else MessageBox.Show("Erro ao deletar!", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { MessageBox.Show("Falha na API.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }
    }

    private void AddToCart_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ProductModel p)
        {
            _cart.Add(p);
            btnCart.Content = $"CARRINHO ({_cart.Count})";
        }
    }

    private void BtnCart_Click(object sender, RoutedEventArgs e)
    {
        if (_cart.Count == 0)
        {
            MessageBox.Show("Carrinho vazio.", "Carrinho", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var token = _client.DefaultRequestHeaders.Authorization?.Parameter;
        var checkout = new CheckoutWindow(_cart, token, true, txtUser.Text);
        checkout.ShowDialog();
        
        // Sempre garantimos que o carrinho seja verificado se houve sucesso ou se fechou.
        if (checkout.IsPurchased)
        {
            _cart.Clear();
            btnCart.Content = "CARRINHO (0)";
            LoadProducts();
        }
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        new MainWindow().Show();
        this.Close();
    }
}


public class ProductModel
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public int Estoque { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string ProxyImageUrl => $"http://localhost:5000/api/images/{Id}";
    public bool IsPortuguese { get; set; }
    public string FormattedPrice => $"R$ {Preco:F2}";
    public string FormattedStock => $"{Estoque} un";
    public string AddToCartTooltip => "Adicionar ao Carrinho";
    public string DeleteTooltip => "Excluir";
}

public class OrderModel
{
    public int Id { get; set; }
    public string? CustomerName { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? DigitalSignature { get; set; }
}

public class OrderViewModel
{
    public string IdLabel { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string DateLabel { get; set; } = "";
    public string ItemsLabel { get; set; } = "";
    public string TotalLabel { get; set; } = "";
    public string StatusLabel { get; set; } = "";
}

public class TopProductViewModel
{
    public string Rank { get; set; } = "";
    public string Name { get; set; } = "";
    public string QtySold { get; set; } = "";
    public double BarWidth { get; set; }
}

public class CustomerViewModel
{
    public string Name { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public string OrderCountLabel { get; set; } = "";
    public string TotalSpent { get; set; } = "";
}
