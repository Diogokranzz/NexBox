using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProductManagementSystem.Domain.Entities;

namespace ProductManagementSystem.Domain.Repositories;

public interface IOrderRepository
{
    Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Order>> GetAllAsync(CancellationToken cancellationToken = default);
}
