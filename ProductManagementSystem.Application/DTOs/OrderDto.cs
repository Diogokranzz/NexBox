using System;
using System.Collections.Generic;

namespace ProductManagementSystem.Application.DTOs;

public record OrderItemDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice, string ImageUrl);

public record OrderDto(int Id, DateTime OrderDate, string CustomerName, string DigitalSignature, decimal TotalAmount, List<OrderItemDto> Items);

public record CreateOrderItemRequest(int ProductId, int Quantity, decimal UnitPrice);

public record CreateOrderRequest(string CustomerName, string DigitalSignature, List<CreateOrderItemRequest> Items);
