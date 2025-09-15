using AutoMapper;
using Contoso.Api.Data;
using Contoso.Api.Models;
using Contoso.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Contoso.Api.Services;

public class OrderService : IOrderService
{
    private readonly ContosoDbContext _context;
    private readonly IMapper _mapper;

    public OrderService(ContosoDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    public async Task<IEnumerable<OrderDto>> GetOrdersAsync(int userId)
    {
         var orders = await _context.Orders
                                    .Where(o => o.UserId == userId)
                                    .Include(o => o.Items!)
                                    .ThenInclude(o => o.Product)
                                    .ToListAsync();

        return _mapper.Map<IEnumerable<OrderDto>>(orders);
    }

    public async Task<OrderDto> GetOrderByIdAsync(int id, int userId)
    {
        var order = await _context.Orders
                            .Where(o => o.UserId == userId && o.Id == id)
                            .Include(o => o.Items!)
                            .ThenInclude(o => o.Product)
                            .FirstOrDefaultAsync();

        return _mapper.Map<OrderDto>(order);
    }

    public async Task<OrderDto> CreateOrderAsync(OrderDto orderDto)
    {
        var newOrder = new Order
        {
            Id = Random.Shared.Next(1, int.MaxValue),
            UserId = orderDto.UserId,
            Total = orderDto.Total,
            CreatedAt = DateTime.UtcNow,
            Items = orderDto.Items?.Select(i => new OrderItem
            {
                Id = Random.Shared.Next(1, int.MaxValue),
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.Price
            }).ToList() ?? new List<OrderItem>()
        };

        _context.Orders.Add(newOrder);

        await _context.SaveChangesAsync();

        // if (orderDto.Items != null)
        // {
        //     foreach (var orderItem in orderDto.Items)
        //     {
        //         var newOrderItem = new OrderItem
        //         {
        //             OrderId = newOrder.Id,
        //             ProductId = orderItem.ProductId,
        //             Quantity = orderItem.Quantity,
        //             UnitPrice = orderItem.Price
        //         };

        //         _context.OrderItems.Add(newOrderItem);
        //     }

        //     await _context.SaveChangesAsync();      
        // }

        return _mapper.Map<OrderDto>(newOrder);     
    }
}