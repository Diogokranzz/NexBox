namespace ProductManagementSystem.Domain.Entities;

public class OrderItem
{
    public int Id { get; init; }
    public int OrderId { get; private set; }
    public Order? Order { get; private set; }
    
    public int? ProductId { get; private set; }
    public Product? Product { get; private set; }
    
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    
    public OrderItem(int productId, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
    
    protected OrderItem() { }
}
