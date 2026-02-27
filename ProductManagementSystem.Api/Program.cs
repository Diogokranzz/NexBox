using System.Text;
using ProductManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProductManagementSystem.Api.Extensions;
using ProductManagementSystem.Api.Services;
using ProductManagementSystem.Application.DTOs;
using ProductManagementSystem.Application.Services;
using ProductManagementSystem.Application.Validators;
using ProductManagementSystem.Domain.Exceptions;
using ProductManagementSystem.Domain.Repositories;
using ProductManagementSystem.Infrastructure.Persistence;
using ProductManagementSystem.Infrastructure.Repositories;
using Serilog;
using Serilog.Events;
using FluentValidation;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ProductManagementSystem.Api")
    .Enrich.WithEnvironmentUserName()
    .Enrich.WithMachineName()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/app-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Iniciando aplicacao ProductManagementSystem");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=app.db";

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString)
    );

    builder.Services.AddControllers();
    builder.Services.AddCustomHealthChecks(builder.Configuration);

    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IOrderRepository, OrderRepository>();

    builder.Services.AddScoped<ProductService>();
    builder.Services.AddScoped<AuthenticationService>();
    builder.Services.AddScoped<EmailSenderService>();
    builder.Services.AddScoped<JwtService>();
    builder.Services.AddScoped<ILoggingService, LoggingService>();
    builder.Services.AddScoped<OrderService>();

    builder.Services.AddScoped<IValidator<CriarProductoRequest>, CriarProductoValidator>();

    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddCustomSwagger();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try {
            await db.Database.MigrateAsync();
            Log.Information("Migrations aplicadas com sucesso");

            var authService = scope.ServiceProvider.GetRequiredService<AuthenticationService>();

            if (!db.Users.Any(u => u.Username == "admin"))
            {
                await authService.RegisterAsync(new RegisterRequest("admin", "admin123", "admin@admin.com"), CancellationToken.None);
                Log.Information("Usuario admin padrao criado");
            }
        } catch (Exception ex) {
            Log.Error(ex, "Erro ao aplicar migrations ou seed de BD");
        }
    }

    app.UseStaticFiles();
    app.UseCustomSwagger();
    app.MapCustomHealthChecks();
    app.MapControllers();

    app.UseSerilogRequestLogging();
    app.UseCors("AllowAll");
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();

    var authGroup = app.MapGroup("/api/auth")
        .WithName("Authentication")
        .WithOpenApi();

    authGroup.MapPost("/login", async (LoginRequest request, AuthenticationService authService, ILoggingService loggingService, CancellationToken cancellationToken) =>
    {
        try
        {
            var response = await authService.LoginAsync(request, cancellationToken);
            loggingService.LogInformation("Login bem-sucedido para {Username}", request.Username);
            return Results.Ok(response);
        }
        catch (DomainException ex)
        {
            loggingService.LogWarning("Falha no login: {Message}", ex.Message);
            return Results.BadRequest(new { mensagem = ex.Message });
        }
    }).WithName("Login").WithSummary("Autenticar usuario").AllowAnonymous();

    authGroup.MapPost("/register", async (RegisterRequest request, AuthenticationService authService, ILoggingService loggingService, CancellationToken cancellationToken) =>
    {
        try
        {
            var response = await authService.RegisterAsync(request, cancellationToken);
            loggingService.LogInformation("Novo usuario registrado: {Username}", request.Username);
            return Results.Created($"/api/auth/profile", response);
        }
        catch (DomainException ex)
        {
            loggingService.LogWarning("Falha no registro: {Message}", ex.Message);
            return Results.BadRequest(new { mensagem = ex.Message });
        }
    }).WithName("Register").WithSummary("Registrar novo usuario").AllowAnonymous();

    var productsGroup = app.MapGroup("/api/products")
        .WithName("Products")
        .WithOpenApi()
        .RequireAuthorization();

    productsGroup.MapGet("/", async ([FromQuery] int pageNumber, [FromQuery] int pageSize, ProductService service, ILoggingService loggingService, CancellationToken cancellationToken) =>
        Results.Ok(await service.ObterPaginadoAsync(new PaginationRequest(pageNumber, pageSize), cancellationToken)))
        .WithName("ListarTodosPaginado").WithSummary("Listar produtos com paginacao").Produces<PagedResponse<ProductDto>>();

    productsGroup.MapGet("/{id}", async (int id, ProductService service, CancellationToken cancellationToken) =>
    {
        var produto = await service.ObterPorIdAsync(id, cancellationToken);
        return produto is null ? Results.NotFound() : Results.Ok(produto);
    }).WithName("ObterPorId").WithSummary("Obter produto por ID").Produces<ProductDto>();

    productsGroup.MapPost("/", async (CriarProductoRequest request, ProductService service, IValidator<CriarProductoRequest> validator, ILoggingService loggingService, CancellationToken cancellationToken) =>
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            loggingService.LogWarning("Validacao falhou para produto: {Errors}", errors);
            return Results.BadRequest(new { errors });
        }

        try
        {
            var produto = await service.CriarAsync(request, cancellationToken);
            loggingService.LogInformation("Produto criado: {NomeProduto} (ID: {ProductId})", produto.Nome, produto.Id);
            return Results.Created($"/api/products/{produto.Id}", produto);
        }
        catch (Exception ex)
        {
            loggingService.LogError(ex, "Erro ao criar produto");
            return Results.BadRequest(new { mensagem = ex.Message });
        }
    }).WithName("Criar").WithSummary("Criar novo produto");

    productsGroup.MapPut("/{id}", async (int id, AtualizarProductoRequest request, ProductService service, ILoggingService loggingService, CancellationToken cancellationToken) =>
    {
        try
        {
            var produto = await service.AtualizarAsync(id, request, cancellationToken);
            loggingService.LogInformation("Produto atualizado (ID: {ProductId})", id);
            return Results.Ok(produto);
        }
        catch (Exception ex)
        {
            loggingService.LogWarning("Produto nao encontrado (ID: {ProductId})", id);
            return Results.NotFound(new { mensagem = ex.Message });
        }
    }).WithName("Atualizar").WithSummary("Atualizar produto");

    productsGroup.MapDelete("/{id}", async (int id, ProductService service, ILoggingService loggingService, CancellationToken cancellationToken) =>
    {
        try
        {
            await service.DeletarAsync(id, cancellationToken);
            loggingService.LogInformation("Produto deletado (ID: {ProductId})", id);
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            loggingService.LogWarning("Falha ao deletar produto (ID: {ProductId}): {Message}", id, ex.Message);
            return Results.NotFound(new { mensagem = ex.Message });
        }
    }).WithName("Deletar").WithSummary("Deletar produto");

    app.MapGet("/api/images/{id}", async (int id, ProductService service, CancellationToken cancellationToken) =>
    {
        var produto = await service.ObterPorIdAsync(id, cancellationToken);
        if (produto is null) return Results.NotFound();

        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "NexBox_App/1.0");

            // 1. Se ja houver uma ImageUrl direta na base de dados, prioriza:
            if (!string.IsNullOrEmpty(produto.ImageUrl))
            {
                if (produto.ImageUrl.StartsWith("http"))
                {
                    try {
                        var directBytes = await httpClient.GetByteArrayAsync(produto.ImageUrl, cancellationToken);
                        return Results.File(directBytes, "image/jpeg");
                    } catch { /* se falhar, tenta as apis publicas */ }
                }
            }

            // Uma logica basica para identificar se o "Nome" foi salvo como Codigo de Barras (EAN 13/8 digitos numericos)
            bool isBarcode = !string.IsNullOrEmpty(produto.Nome) && produto.Nome.Length >= 8 && produto.Nome.All(char.IsDigit);
            string barcodeToSearch = isBarcode ? produto.Nome : "";

            // 1. Tentar Open Food Facts (Se for codigo de barras EAN)
            if (!string.IsNullOrEmpty(barcodeToSearch))
            {
                try
                {
                    var offUrl = $"https://world.openfoodfacts.org/api/v0/product/{barcodeToSearch}.json";
                    var offJson = await httpClient.GetStringAsync(offUrl, cancellationToken);
                    var offDoc = System.Text.Json.JsonDocument.Parse(offJson);
                    
                    if (offDoc.RootElement.TryGetProperty("status", out var status) && status.GetInt32() == 1)
                    {
                        if (offDoc.RootElement.TryGetProperty("product", out var prodObj) && 
                            prodObj.TryGetProperty("image_front_url", out var imgUrl) && 
                            !string.IsNullOrEmpty(imgUrl.GetString()))
                        {
                            var imgBytes = await httpClient.GetByteArrayAsync(imgUrl.GetString(), cancellationToken);
                            return Results.File(imgBytes, "image/jpeg");
                        }
                    }
                }
                catch { } // Se falhar ou nao achar, segue para os proximos
            }
            
            // 2. Tentar Ean-Search / UPC Item DB (para buscar por UPC gratuito se for barcode)
            if (!string.IsNullOrEmpty(barcodeToSearch))
            {
                try
                {
                    var upcUrl = $"https://api.upcitemdb.com/prod/trial/lookup?upc={barcodeToSearch}";
                    var upcJson = await httpClient.GetStringAsync(upcUrl, cancellationToken);
                    var upcDoc = System.Text.Json.JsonDocument.Parse(upcJson);

                    if (upcDoc.RootElement.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
                    {
                        var firstItem = items[0];
                        if (firstItem.TryGetProperty("images", out var images) && images.GetArrayLength() > 0)
                        {
                            var imgUrl = images[0].GetString();
                            if (!string.IsNullOrEmpty(imgUrl))
                            {
                                var imgBytes = await httpClient.GetByteArrayAsync(imgUrl, cancellationToken);
                                return Results.File(imgBytes, "image/jpeg");
                            }
                        }
                    }
                }
                catch { }
            }

            // 3. Fallback (Caso as APIs OpenFoodFacts derem vazio ou nao for cod. barras, retorna logo corporativo com a inicial do Produto)
            var nQuery = Uri.EscapeDataString(produto.Nome);
            var avatarUrl = $"https://ui-avatars.com/api/?name={nQuery}&background=e6570a&color=fff&size=200&font-size=0.4";
            var fallbackBytes = await httpClient.GetByteArrayAsync(avatarUrl, cancellationToken);
            return Results.File(fallbackBytes, "image/png");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Falha ao buscar imagem do produto {Id}", id);
            return Results.NotFound();
        }
    }).WithName("GetProductImage").WithSummary("Obter imagem do produto via OpenFoodFacts/Walmart Scraping");

    app.MapPost("/api/orders", async (HttpContext context, AppDbContext db) =>
    {
        try
        {
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            var doc = System.Text.Json.JsonDocument.Parse(body);

            var customerName = doc.RootElement.TryGetProperty("customerName", out var cn) ? cn.GetString() : "Guest";
            var signature = doc.RootElement.TryGetProperty("digitalSignature", out var ds) ? ds.GetString() : "";
            var items = doc.RootElement.TryGetProperty("items", out var it) ? it.EnumerateArray().ToList() : new();

            decimal total = 0;
            int itemCount = 0;
            foreach (var item in items)
            {
                var qty = item.GetProperty("quantity").GetInt32();
                var price = item.GetProperty("unitPrice").GetDecimal();
                total += qty * price;
                itemCount += qty;
            }

            var order = new Order(customerName ?? "Guest", signature ?? "", total);

            foreach (var item in items)
            {
                var productId = item.GetProperty("productId").GetInt32();
                var qty = item.GetProperty("quantity").GetInt32();
                var price = item.GetProperty("unitPrice").GetDecimal();
                order.AddItem(new OrderItem(productId, qty, price));
            }

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            Log.Information("Pedido #{Id} criado: {Customer}, Total: {Total}, Itens: {Items}", order.Id, customerName, total, itemCount);
            return Results.Ok(new { id = order.Id, total, itemCount });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Erro ao criar pedido");
            return Results.BadRequest(new { error = ex.Message });
        }
    }).WithName("CreateOrder").WithSummary("Criar pedido");

    app.MapGet("/api/orders", async (AppDbContext db) =>
    {
        var orders = await db.Orders
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new { o.Id, o.CustomerName, Total = o.TotalAmount, ItemCount = o.Items.Count, CreatedAt = o.OrderDate, o.DigitalSignature })
            .ToListAsync();
        return Results.Ok(orders);
    }).WithName("ListOrders").WithSummary("Listar pedidos");

    app.MapPost("/api/email/send", async (HttpContext context, EmailSenderService emailService) =>
    {
        try
        {
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            var doc = System.Text.Json.JsonDocument.Parse(body);
            var to = doc.RootElement.GetProperty("to").GetString() ?? "";
            var subject = doc.RootElement.GetProperty("subject").GetString() ?? "";
            var htmlBody = doc.RootElement.GetProperty("body").GetString() ?? "";

            if (string.IsNullOrEmpty(to) || !to.Contains("@"))
                return Results.BadRequest(new { error = "Email invalido" });

            // Substituiu o salvamento local pelo disparo do email real via SMTP do Google.
            var success = await emailService.SendEmailAsync(to, subject, htmlBody);

            if (success)
            {
                return Results.Ok(new { message = "Email enviado com sucesso", to });
            }
            else
            {
                return Results.Ok(new { message = "Falha silenciosa ao enviar email (verifique as senhas/logs no console)" });
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Falha ao enviar email: {Message}", ex.Message);
            return Results.Ok(new { message = $"Email failed: {ex.Message}" });
        }
    }).WithName("SendEmail").WithSummary("Enviar email de recibo");

    app.Run();

    Log.Information("Aplicacao encerrada normalmente");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicacao encerrada com erro fatal");
}
finally
{
    Log.CloseAndFlush();
}
