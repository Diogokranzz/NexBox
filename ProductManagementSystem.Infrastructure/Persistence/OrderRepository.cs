using Microsoft.EntityFrameworkCore;
using ProductManagementSystem.Domain.Entities;
using ProductManagementSystem.Domain.Repositories;

namespace ProductManagementSystem.Infrastructure.Persistence;

public class OrderRepository(AppDbContext context) : IOrderRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Add(order);
        foreach (var item in order.Items)
        {
            var product = await _context.Products.FindAsync(new object[] { item.ProductId }, cancellationToken);
            if (product != null)
            {

                var newStock = product.Estoque - item.Quantity;
                product.UpdateInfo(product.Nome, product.Preco, newStock >= 0 ? newStock : 0, product.ImageUrl);
            }
        }
        await _context.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task<IReadOnlyCollection<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }
}
