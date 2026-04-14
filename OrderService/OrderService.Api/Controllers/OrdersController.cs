using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Data;
using OrderService.Api.DTOs;
using OrderService.Api.Messaging;
using OrderService.Api.Models;
using OrderService.Api.Services;

namespace OrderService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _context;
    private readonly ICustomerClient _customerClient;
    private readonly IProductClient _productClient;
    private readonly IEventPublisher _publisher;

    public OrdersController(
        OrderDbContext context,
        ICustomerClient customerClient,
        IProductClient productClient,
        IEventPublisher publisher)
    {
        _context = context;
        _customerClient = customerClient;
        _productClient = productClient;
        _publisher = publisher;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var orders = await _context.Orders.ToListAsync(ct);
        var response = orders.Select(o => new OrderResponseDto(
            o.Id, o.CustomerId, o.ProductId,
            o.Quantity, o.TotalAmount, o.Status, o.CreatedAt));
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order == null) return NotFound();
        return Ok(new OrderResponseDto(
            order.Id, order.CustomerId, order.ProductId,
            order.Quantity, order.TotalAmount, order.Status, order.CreatedAt));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto, CancellationToken ct)
    {
        // 验证 Customer 存在
        var customerExists = await _customerClient.CustomerExistsAsync(dto.CustomerId);
        if (!customerExists) return BadRequest($"Customer {dto.CustomerId} not found.");

        // 验证 Product 存在并获取价格
        var productExists = await _productClient.ProductExistsAsync(dto.ProductId);
        if (!productExists) return BadRequest($"Product {dto.ProductId} not found.");

        var price = await _productClient.GetProductPriceAsync(dto.ProductId);

        var order = new Order
        {
            CustomerId  = dto.CustomerId,
            ProductId   = dto.ProductId,
            Quantity    = dto.Quantity,
            TotalAmount = price * dto.Quantity,   // ← 计算总价
            Status      = "Created",
            CreatedAt   = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(ct);

        _publisher.Publish(new OrderCreatedEvent
        {
            OrderId   = order.Id,
            ProductId = order.ProductId,
            Quantity  = order.Quantity
        });

        return CreatedAtAction(nameof(GetById), new { id = order.Id },
            new OrderResponseDto(
                order.Id, order.CustomerId, order.ProductId,
                order.Quantity, order.TotalAmount, order.Status, order.CreatedAt));
    }
}