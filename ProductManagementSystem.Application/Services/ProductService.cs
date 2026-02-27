using ProductManagementSystem.Application.DTOs;
using ProductManagementSystem.Domain.Entities;
using ProductManagementSystem.Domain.Exceptions;
using ProductManagementSystem.Domain.Repositories;

namespace ProductManagementSystem.Application.Services;

public class ProductService(IProductRepository repository)
{
    private readonly IProductRepository _repository = repository;

    public async Task<PagedResponse<ProductDto>> ObterPaginadoAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = pagination.GetPageNumber();
        var pageSize = pagination.GetPageSize();

        var (products, totalCount) = await _repository.ObterPaginadoAsync(
            pageNumber,
            pageSize,
            cancellationToken);

        var productDtos = products.Select(MapearParaDto);
        return PagedResponse<ProductDto>.Create(productDtos, pageNumber, pageSize, totalCount);
    }

    public async Task<ProductDto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _repository.ObterPorIdAsync(id, cancellationToken);
        return product is null ? null : MapearParaDto(product);
    }

    public async Task<ProductDto> CriarAsync(CriarProductoRequest request, CancellationToken cancellationToken = default)
    {
        var product = new Product(request.Nome, request.Preco, request.Estoque, request.ImageUrl);
        await _repository.CriarAsync(product, cancellationToken);
        return MapearParaDto(product);
    }

    public async Task<ProductDto> AtualizarAsync(int id, AtualizarProductoRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _repository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new DomainException("Produto não encontrado.");

        product.UpdateInfo(request.Nome, request.Preco, request.Estoque, request.ImageUrl);
        await _repository.AtualizarAsync(product, cancellationToken);
        return MapearParaDto(product);
    }

    public async Task DeletarAsync(int id, CancellationToken cancellationToken = default)
    {
        var success = await _repository.DeletarAsync(id, cancellationToken);
        if (!success)
            throw new DomainException("Produto não encontrado.");
    }

    private static ProductDto MapearParaDto(Product product) =>
        new(product.Id, product.Nome, product.Preco, product.Estoque, product.ImageUrl);
}
