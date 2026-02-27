using FluentAssertions;
using ProductManagementSystem.Domain.Entities;
using ProductManagementSystem.Domain.Exceptions;
using Xunit;

namespace ProductManagementSystem.UnitTests.Domain;

public class ProductTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateProduct()
    {
        var name = "Mechanical Keyboard";
        var price = 150.50m;
        var stock = 10;

        var product = new Product(name, price, stock);

        product.Should().NotBeNull();
        product.Nome.Should().Be(name);
        product.Preco.Should().Be(price);
        product.Estoque.Should().Be(stock);
    }

    [Theory]
    [InlineData("", 10.0, 5, "O nome do produto não pode ser vazio.")]
    [InlineData("Mouse", -5.0, 5, "O preço deve ser maior que zero.")]
    [InlineData("Monitor", 100.0, -1, "O estoque não pode ser negativo.")]
    public void Constructor_WithInvalidParameters_ShouldThrowException(
        string name, double priceDouble, int stock, string expectedMessageWildcard)
    {
        var price = (decimal)priceDouble;

        Action act = () => new Product(name, price, stock);

        act.Should()
           .Throw<DomainException>()
           .WithMessage(expectedMessageWildcard);
    }
}
