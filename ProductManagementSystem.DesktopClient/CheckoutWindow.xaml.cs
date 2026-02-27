using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ProductManagementSystem.DesktopClient;

public partial class CheckoutWindow : Window
{
    private readonly HttpClient _client;
    private readonly ObservableCollection<CartItemModel> _cartItems;
    private readonly string _user;
    private decimal _total = 0;
    private decimal _discount = 0;
    private string _paymentMethod = "";
    
    public bool IsPurchased { get; private set; } = false;

    public CheckoutWindow(List<ProductModel> cart, string? token, bool isPt, string user)
    {
        InitializeComponent();
        _user = user;

        _client = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
        if (token != null)
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _cartItems = new ObservableCollection<CartItemModel>();
        var grouped = cart.GroupBy(p => p.Id);
        foreach (var group in grouped)
        {
            var first = group.First();
            _cartItems.Add(new CartItemModel
            {
                Id = first.Id,
                Nome = first.Nome,
                UnitPrice = first.Preco,
                Quantity = group.Count(),
                MaxStock = first.Estoque,
                ImageUrl = first.ImageUrl
            });
        }

        listCart.ItemsSource = _cartItems;
        txtCustomerName.Text = user;
        txtEmail.Text = ""; // limpo para inserir qualquer e-mail
        ApplyLocalization();
        RecalculateTotal();
    }

    private void ApplyLocalization()
    {
        txtTitle.Text = "Checkout";
        lblCustomer.Text = "Cliente";
        lblPayment.Text = "Pagamento";
        lblPixDiscount.Text = " (5% desc.)";
        lblCreditCard.Text = "Cartão de Crédito";
        lblDebitCard.Text = "Cartão de Débito";
        lblCash.Text = "Dinheiro";
        lblCashReceived.Text = "Valor Recebido";
        lblChange.Text = "Troco:";
        lblPixInfo.Text = "Escaneie para pagar";
        lblSubtotal.Text = "Subtotal:";
        lblItems.Text = "Itens:";
        lblTotal.Text = "TOTAL:";
        lblCardInfo.Text = "Dados do Cartão";
        btnCheckout.Content = "FINALIZAR COMPRA";
    }

    private void RecalculateTotal()
    {
        var subtotal = _cartItems.Sum(i => i.UnitPrice * i.Quantity);
        var totalItems = _cartItems.Sum(i => i.Quantity);

        _discount = 0;
        if (rbPix?.IsChecked == true)
            _discount = subtotal * 0.05m;

        _total = subtotal - _discount;

        txtSubtotal.Text = $"R$ {subtotal:F2}";
        txtItemCount.Text = totalItems.ToString();

        if (_discount > 0)
        {
            lblDiscount.Text = "Desconto PIX:";
            txtDiscount.Text = $"- R$ {_discount:F2}";
        }
        else
        {
            lblDiscount.Text = "";
            txtDiscount.Text = "";
        }

        txtTotal.Text = $"R$ {_total:F2}";

        if (rbPix?.IsChecked == true)
        {
            txtPixTotal.Text = $"R$ {_total:F2}";
            GeneratePixQrCode();
        }

        if (rbCash?.IsChecked == true)
            UpdateChange();
    }

    private void GeneratePixQrCode()
    {
        try
        {
            var pixKey = "productsync@pagamentos.com.br";
            var pixPayload = $"00020126580014BR.GOV.BCB.PIX0136{pixKey}5204000053039865406{_total:F2}5802BR5925NexBox Pagamentos6009SAO PAULO62070503***6304";

            txtPixKey.Text = $"Chave PIX: {pixKey}";

            var qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={Uri.EscapeDataString(pixPayload)}&color=343a40&bgcolor=FFFFFF&format=png";

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(qrUrl);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            imgQrCode.Source = bitmap;
        }
        catch
        {
            txtPixKey.Text = "QR Code indisponivel";
        }
    }

    private void IncreaseQty_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is CartItemModel item)
        {
            if (item.Quantity < item.MaxStock)
            {
                item.Quantity++;
                RecalculateTotal();
                listCart.Items.Refresh();
            }
        }
    }

    private void DecreaseQty_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is CartItemModel item)
        {
            if (item.Quantity > 1)
            {
                item.Quantity--;
                RecalculateTotal();
                listCart.Items.Refresh();
            }
            else
            {
                _cartItems.Remove(item);
                RecalculateTotal();
                if (_cartItems.Count == 0)
                {
                    btnCheckout.IsEnabled = false;
                    SetStatus("Carrinho vazio!", true);
                }
            }
        }
    }

    private void PaymentMethod_Changed(object sender, RoutedEventArgs e)
    {
        if (rbPix == null) return;

        panelCash.Visibility = rbCash.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        panelPix.Visibility = rbPix.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        panelCard.Visibility = (rbCredit.IsChecked == true || rbDebit.IsChecked == true) ? Visibility.Visible : Visibility.Collapsed;

        if (rbPix.IsChecked == true) _paymentMethod = "PIX";
        else if (rbCredit.IsChecked == true) _paymentMethod = "CREDIT_CARD";
        else if (rbDebit.IsChecked == true) _paymentMethod = "DEBIT_CARD";
        else if (rbCash.IsChecked == true) _paymentMethod = "CASH";

        RecalculateTotal();
        txtStatus.Text = "";
    }

    private string _cardBrand = "";
    private void CardBrand_Click(object sender, RoutedEventArgs e)
    {
        var selected = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 87, 10));
        var unselected = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85));

        rbVisa.BorderBrush = unselected;
        rbMaster.BorderBrush = unselected;
        rbElo.BorderBrush = unselected;

        if (sender is System.Windows.Controls.Button btn)
        {
            btn.BorderBrush = selected;
            if (btn == rbVisa) _cardBrand = "VISA";
            else if (btn == rbMaster) _cardBrand = "MASTERCARD";
            else if (btn == rbElo) _cardBrand = "ELO";
        }
    }

    private void CardNumber_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var tb = txtCardNumber;
        var raw = Regex.Replace(tb.Text, @"[^\d]", "");
        if (raw.Length > 16) raw = raw[..16];
        var formatted = string.Join(" ", Enumerable.Range(0, (raw.Length + 3) / 4).Select(i => raw.Substring(i * 4, Math.Min(4, raw.Length - i * 4))));
        if (tb.Text != formatted)
        {
            tb.Text = formatted;
            tb.CaretIndex = formatted.Length;
        }
    }

    private void CardExpiry_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var tb = txtCardExpiry;
        var raw = Regex.Replace(tb.Text, @"[^\d]", "");
        if (raw.Length > 4) raw = raw[..4];
        string formatted = raw.Length > 2 ? $"{raw[..2]}/{raw[2..]}" : raw;
        if (tb.Text != formatted)
        {
            tb.Text = formatted;
            tb.CaretIndex = formatted.Length;
        }
    }

    private void CashAmount_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateChange();
    }

    private void UpdateChange()
    {
        var text = txtCashAmount.Text.Replace("R$", "").Replace("$", "").Replace(",", ".").Trim();
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var cashAmount))
        {
            var change = cashAmount - _total;
            if (change >= 0)
            {
                txtChange.Text = $"R$ {change:F2}";
                txtChange.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11));
            }
            else
            {
                txtChange.Text = $"Falta R$ {Math.Abs(change):F2}";
                txtChange.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68));
            }
        }
        else
        {
            txtChange.Text = "R$ 0,00";
        }
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) this.DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => this.Close();

    private void SetStatus(string msg, bool isError)
    {
        txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            isError ? System.Windows.Media.Color.FromRgb(239, 68, 68) : System.Windows.Media.Color.FromRgb(230, 87, 10));
        txtStatus.Text = msg;
    }

    private string GerarAssinaturaDigital(decimal total, string consumer)
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signBase = $"RECEIPT-{consumer}-{ts}-TOTAL:{total}-METHOD:{_paymentMethod}-{string.Join(",", _cartItems.Select(x => $"{x.Id}x{x.Quantity}"))}";
        using SHA256 sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(signBase));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private bool ValidateCheckout()
    {
        if (string.IsNullOrWhiteSpace(txtCustomerName.Text))
        {
            SetStatus("Informe o nome do cliente.", true);
            return false;
        }
        if (string.IsNullOrEmpty(_paymentMethod))
        {
            SetStatus("Selecione uma forma de pagamento.", true);
            return false;
        }
        if (_paymentMethod == "CASH")
        {
            var text = txtCashAmount.Text.Replace("R$", "").Replace("$", "").Replace(",", ".").Trim();
            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var cash) || cash < _total)
            {
                SetStatus("Valor recebido insuficiente.", true);
                return false;
            }
        }
        if (_paymentMethod is "CREDIT_CARD" or "DEBIT_CARD")
        {
            var cardNum = Regex.Replace(txtCardNumber.Text, @"\s", "");
            if (cardNum.Length < 13 || cardNum.Length > 16)
            {
                SetStatus("Número do cartão inválido.", true);
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtCardHolder.Text))
            {
                SetStatus("Informe o nome no cartão.", true);
                return false;
            }
            if (!Regex.IsMatch(txtCardExpiry.Text, @"^\d{2}/\d{2}$"))
            {
                SetStatus("Validade invalida (MM/AA).", true);
                return false;
            }
            if (txtCardCVV.Text.Length < 3)
            {
                SetStatus("CVV inválido.", true);
                return false;
            }
        }
        if (_cartItems.Count == 0)
        {
            SetStatus("Carrinho vazio.", true);
            return false;
        }
        return true;
    }

    private async void Checkout_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateCheckout()) return;

        btnCheckout.IsEnabled = false;
        SetStatus("Processando pagamento...", false);

        try
        {
            await System.Threading.Tasks.Task.Delay(1500);

            var sig = GerarAssinaturaDigital(_total, txtCustomerName.Text);

            var payload = new
            {
                customerName = txtCustomerName.Text,
                digitalSignature = sig,
                items = _cartItems.Select(p => new { productId = p.Id, quantity = p.Quantity, unitPrice = p.UnitPrice }).ToList()
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("api/orders", content);

            if (response.IsSuccessStatusCode)
            {
                panelSignature.Visibility = Visibility.Visible;
                txtSignature.Text = sig;
                lblOrderId.Text = $"Processado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                lblOrderDate.Text = $"Pagamento: {GetPaymentLabel()}";
                btnCheckout.Visibility = Visibility.Collapsed;

                var emailSent = await TrySendEmailReceipt(sig);
                lblEmailSent.Text = emailSent
                    ? $"Recibo enviado para: {txtEmail.Text}"
                    : "Email nao configurado ou falhou";

                txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94));
                txtStatus.Text = "COMPRA FINALIZADA COM SUCESSO!";
                txtTitle.Text = "Recibo Digital";

                this.IsPurchased = true;
                // Deixa a aba aberta pro cliente poder ler o recibo na tela inteira. O Dashboard cuidara de limpar!
            }
            else
            {
                var err = await response.Content.ReadAsStringAsync();
                SetStatus($"Erro: {err}", true);
                btnCheckout.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Falha: {ex.Message}", true);
            btnCheckout.IsEnabled = true;
        }
    }

    private async System.Threading.Tasks.Task<bool> TrySendEmailReceipt(string signature)
    {
        if (string.IsNullOrWhiteSpace(txtEmail.Text) || !txtEmail.Text.Contains("@"))
            return false;

        try
        {
            var itemsHtml = new StringBuilder();
            foreach (var item in _cartItems)
            {
                itemsHtml.AppendLine($@"
                <tr>
                    <td style='padding:8px;border-bottom:1px solid #e2e8f0;'>{item.Nome}</td>
                    <td style='padding:8px;border-bottom:1px solid #e2e8f0;text-align:center;'>{item.Quantity}</td>
                    <td style='padding:8px;border-bottom:1px solid #e2e8f0;text-align:right;'>R$ {item.UnitPrice:F2}</td>
                    <td style='padding:8px;border-bottom:1px solid #e2e8f0;text-align:right;font-weight:bold;'>R$ {(item.UnitPrice * item.Quantity):F2}</td>
                </tr>");
            }

            var discountRow = _discount > 0
                ? $"<tr><td colspan='3' style='padding:8px;text-align:right;color:#16a34a;'>Desconto PIX:</td><td style='padding:8px;text-align:right;color:#16a34a;font-weight:bold;'>- R$ {_discount:F2}</td></tr>"
                : "";

            var html = $@"
            <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;background:#f8fafc;padding:30px;'>
                <div style='background:#343a40;color:white;padding:25px;border-radius:12px 12px 0 0;text-align:center;'>
                    <h1 style='margin:0;color:#e6570a;'>NexBox</h1>
                    <p style='margin:8px 0 0;color:#adb5bd;'>Recibo de Compra</p>
                </div>
                <div style='background:white;padding:25px;border:1px solid #e2e8f0;'>
                    <p><strong>Cliente:</strong> {txtCustomerName.Text}</p>
                    <p><strong>Data:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
                    <p><strong>Pagamento:</strong> {GetPaymentLabel()}</p>
                    <hr style='border:none;border-top:1px solid #e2e8f0;margin:15px 0;'/>
                    <table style='width:100%;border-collapse:collapse;'>
                        <thead>
                            <tr style='background:#f1f5f9;'>
                                <th style='padding:8px;text-align:left;'>Produto</th>
                                <th style='padding:8px;text-align:center;'>Qtd</th>
                                <th style='padding:8px;text-align:right;'>Preço</th>
                                <th style='padding:8px;text-align:right;'>Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            {itemsHtml}
                            {discountRow}
                        </tbody>
                        <tfoot>
                            <tr style='background:#343a40;color:white;'>
                                <td colspan='3' style='padding:12px;font-weight:bold;font-size:16px;'>TOTAL</td>
                                <td style='padding:12px;text-align:right;font-weight:bold;font-size:16px;color:#e6570a;'>R$ {_total:F2}</td>
                            </tr>
                        </tfoot>
                    </table>
                    <hr style='border:none;border-top:1px solid #e2e8f0;margin:15px 0;'/>
                    <p style='font-size:11px;color:#64748b;'>Assinatura Digital (SHA256):</p>
                    <p style='font-size:10px;color:#94a3b8;word-break:break-all;font-family:monospace;background:#f1f5f9;padding:8px;border-radius:6px;'>{signature}</p>
                </div>
                <div style='text-align:center;padding:15px;color:#94a3b8;font-size:11px;'>
                    NexBox - {DateTime.Now.Year} - Sistema de Gestão de Produtos
                </div>
            </div>";

            var emailPayload = new
            {
                to = txtEmail.Text,
                subject = $"Recibo de Compra - NexBox #{DateTime.Now:yyyyMMddHHmmss}",
                body = html
            };

            var emailContent = new StringContent(JsonSerializer.Serialize(emailPayload), Encoding.UTF8, "application/json");
            var emailResponse = await _client.PostAsync("api/email/send", emailContent);
            return emailResponse.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private string GetPaymentLabel() => _paymentMethod switch
    {
        "PIX" => "PIX",
        "CREDIT_CARD" => "Cartão de Crédito",
        "DEBIT_CARD" => "Cartão de Débito",
        "CASH" => "Dinheiro",
        _ => "-"
    };
}

public class CartItemModel : INotifyPropertyChanged
{
    private int _quantity;
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int MaxStock { get; set; }
    public string ImageUrl { get; set; } = string.Empty;

    public int Quantity
    {
        get => _quantity;
        set { _quantity = value; OnPropertyChanged(nameof(Quantity)); OnPropertyChanged(nameof(FormattedSubtotal)); }
    }

    public string ProxyImageUrl => $"http://localhost:5000/api/images/{Id}";
    public string FormattedUnitPrice => $"R$ {UnitPrice:F2} /un";
    public string FormattedSubtotal => $"R$ {UnitPrice * Quantity:F2}";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
