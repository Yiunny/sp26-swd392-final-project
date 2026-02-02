using Microsoft.AspNetCore.Mvc;
using ShopHangTet.Services;
using ShopHangTet.DTOs;
using ShopHangTet.Models;

namespace ShopHangTet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IEmailService _emailService;

    public OrdersController(IOrderService orderService, IEmailService emailService)
    {
        _orderService = orderService;
        _emailService = emailService;
    }

    /// <summary>
    /// Tạo đơn hàng mới (B2C hoặc B2B)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto request)
    {
        try
        {
            // Validate request
            var validation = await _orderService.ValidateOrderAsync(request);
            if (!validation.IsValid)
            {
                return BadRequest(new { errors = validation.Errors });
            }

            // Place order
            Models.OrderModel order;
            if (request.OrderType == OrderTypeDto.B2B)
            {
                order = await _orderService.PlaceB2BOrderAsync(request);
            }
            else
            {
                order = await _orderService.PlaceOrderAsync(request);
            }

            // Send confirmation email
            await _emailService.SendOrderConfirmationAsync(
                request.CustomerEmail, 
                order.OrderCode, 
                order.TotalAmount
            );

            return Ok(new { 
                success = true,
                orderId = order.Id.ToString(),
                orderCode = order.OrderCode,
                message = "Order placed successfully"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                success = false,
                message = ex.Message 
            });
        }
    }

    /// <summary>
    /// Track đơn hàng cho guest (không cần đăng nhập)
    /// </summary>
    [HttpGet("track")]
    public async Task<IActionResult> TrackOrder([FromQuery] string orderCode, [FromQuery] string email)
    {
        try
        {
            var tracking = await _orderService.TrackOrderAsync(orderCode, email);
            if (tracking == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            return Ok(tracking);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách đơn hàng (cho authenticated user)
    /// </summary>
    [HttpGet("my-orders")]
    // [Authorize] // Uncomment khi implement authentication
    public async Task<IActionResult> GetMyOrders([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        try
        {
            // TODO: Implement GetOrdersByUserAsync in OrderService
            // For now, return empty list
            var userId = "temp-user-id"; // Placeholder - get from JWT token
            return Ok(new { orders = new List<object>(), message = "Coming soon" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}