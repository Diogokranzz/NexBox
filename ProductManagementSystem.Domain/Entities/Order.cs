using System;
using System.Collections.Generic;

namespace ProductManagementSystem.Domain.Entities;

public class Order
{
    public int Id { get; init; }
    public DateTime OrderDate { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string DigitalSignature { get; private set; } = string.Empty;
    
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public Order(string customerName, string digitalSignature, decimal totalAmount)
    {
        CustomerName = customerName;
        DigitalSignature = digitalSignature;
        TotalAmount = totalAmount;
        OrderDate = DateTime.UtcNow;
    }
    
    public void AddItem(OrderItem item)
    {
        _items.Add(item);
    }
    

    protected Order() { }
}
