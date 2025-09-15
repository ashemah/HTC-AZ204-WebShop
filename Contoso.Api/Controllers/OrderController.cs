using System.Security.Claims;
using Contoso.Api.Data;
using Contoso.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Contoso.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;     
    }

    [HttpGet()]
    public async Task<IActionResult> GetOrdersAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var orders = await _orderService.GetOrdersAsync(userId);

        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderByIdAsync(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var order = await _orderService.GetOrderByIdAsync(id, userId);

        return Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrderAsync(OrderDto orderDto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        orderDto.UserId = userId;

        var newOrder = await _orderService.CreateOrderAsync(orderDto);

        return Ok(newOrder);
    }
}