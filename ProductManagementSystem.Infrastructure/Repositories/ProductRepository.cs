using Microsoft.EntityFrameworkCore;
using ProductManagementSystem.Domain.Entities;
using ProductManagementSystem.Domain.Repositories;
using ProductManagementSystem.Infrastructure.Persistence;

namespace ProductManagementSystem.Infrastructure.Repositories;

public class ProductRepository(AppDbContext context) : IProductRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Product?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Products.FindAsync([id], cancellationToken);

    public async Task<IEnumerable<Product>> ObterTodosAsync(CancellationToken cancellationToken = default) =>
        await _context.Products.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<(List<Product> items, int totalCount)> ObterPaginadoAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await _context.Products
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var items = await _context.Products
            .AsNoTracking()
            .OrderBy(p => p.Nome)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
    
    public async Task<Product> CriarAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task<Product> AtualizarAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task<bool> DeletarAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await ObterPorIdAsync(id, cancellationToken);
        if (product is null) return false;
        
        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ExisteAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Products.AnyAsync(p => p.Id == id, cancellationToken);
}
