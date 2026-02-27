namespace ProductManagementSystem.Application.DTOs;

public record ProductDto(int Id, string Nome, decimal Preco, int Estoque, string ImageUrl);

public record CriarProductoRequest(string Nome, decimal Preco, int Estoque, string ImageUrl = "");

public record AtualizarProductoRequest(string Nome, decimal Preco, int Estoque, string ImageUrl = "");
