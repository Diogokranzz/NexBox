using System;
using ProductManagementSystem.Domain.Exceptions;

namespace ProductManagementSystem.Domain.Entities;

public class Product
{
    public int Id { get; init; }
    public string Nome { get; private set; }
    public decimal Preco { get; private set; }
    public int Estoque { get; private set; }
    public string ImageUrl { get; private set; }

    public Product(string nome, decimal preco, int estoque, string imageUrl = "")
    {
        Validate(nome, preco, estoque);
        Nome = nome;
        Preco = preco;
        Estoque = estoque;
        ImageUrl = imageUrl ?? "";
    }

    public void UpdateInfo(string nome, decimal preco, int estoque, string imageUrl = "")
    {
        Validate(nome, preco, estoque);
        Nome = nome;
        Preco = preco;
        Estoque = estoque;
        ImageUrl = imageUrl ?? "";
    }

    private static void Validate(string nome, decimal preco, int estoque)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do produto não pode ser vazio.");

        if (preco <= 0)
            throw new DomainException("O preço deve ser maior que zero.");

        if (estoque < 0)
            throw new DomainException("O estoque não pode ser negativo.");
    }
}
