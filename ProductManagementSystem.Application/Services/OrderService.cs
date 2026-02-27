using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using ProductManagementSystem.Application.DTOs;
using ProductManagementSystem.Domain.Entities;
using ProductManagementSystem.Domain.Exceptions;
using ProductManagementSystem.Domain.Repositories;

namespace ProductManagementSystem.Application.Services;

public class OrderService(IOrderRepository repository)
{
    private readonly IOrderRepository _repository = repository;

    public async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var total = request.Items.Sum(x => x.Quantity * x.UnitPrice);
        if (total <= 0) throw new DomainException("O pedido deve ter um valor maior que zero.");

        var order = new Order(request.CustomerName, request.DigitalSignature, total);
        
        foreach (var item in request.Items)
        {
            order.AddItem(new OrderItem(item.ProductId, item.Quantity, item.UnitPrice));
        }

        var created = await _repository.CreateAsync(order, cancellationToken);
        

        var completeOrder = await _repository.GetByIdAsync(created.Id, cancellationToken);
        return MapearParaDto(completeOrder!);
    }

    public async Task<IReadOnlyCollection<OrderDto>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _repository.GetAllAsync(cancellationToken);
        return orders.Select(MapearParaDto).ToList();
    }

    private static OrderDto MapearParaDto(Order order)
    {
        var items = order.Items.Select(i => new OrderItemDto(
            i.ProductId ?? 0,
            i.Product?.Nome ?? "Produto Desconhecido",
            i.Quantity,
            i.UnitPrice,
            i.Product?.ImageUrl ?? ""
        )).ToList();

        return new OrderDto(order.Id, order.OrderDate, order.CustomerName, order.DigitalSignature, order.TotalAmount, items);
    }
}
