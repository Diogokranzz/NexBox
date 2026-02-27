using ProductManagementSystem.Domain.Entities;

namespace ProductManagementSystem.Domain.Repositories;

public interface IProductRepository
{
    Task<Product?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> ObterTodosAsync(CancellationToken cancellationToken = default);
    
    Task<(List<Product> items, int totalCount)> ObterPaginadoAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    
    Task<Product> CriarAsync(Product product, CancellationToken cancellationToken = default);
    Task<Product> AtualizarAsync(Product product, CancellationToken cancellationToken = default);
    Task<bool> DeletarAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExisteAsync(int id, CancellationToken cancellationToken = default);
}
