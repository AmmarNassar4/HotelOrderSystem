using HotelOrderSystem.Api.Common;
using HotelOrderSystem.Api.Dtos;
using HotelOrderSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOrderSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrderDto>>>> GetPending(CancellationToken cancellationToken)
    {
        var result = await _orders.GetPendingAsync(User.GetUserId(), User.GetTeamId(), User.GetRoleName(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("my-active")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrderDto>>>> GetMyActive(CancellationToken cancellationToken)
    {
        var result = await _orders.GetMyActiveAsync(User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _orders.GetByIdAsync(id, User.GetUserId(), User.GetTeamId(), User.GetRoleName(), cancellationToken);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateOrderResponse>>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await _orders.CreateAsync(request, User.GetUserId(), User.GetRoleName(), cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:int}/accept")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> Accept(int id, AcceptOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await _orders.AcceptAsync(id, User.GetUserId(), User.GetTeamId(), User.GetRoleName(), request, cancellationToken);
        return result.IsSuccess ? Ok(result) : Conflict(result);
    }

    [HttpPut("{id:int}/start")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> Start(int id, CancellationToken cancellationToken)
    {
        var result = await _orders.StartAsync(id, User.GetUserId(), User.GetRoleName(), cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:int}/complete")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> Complete(int id, CompleteOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await _orders.CompleteAsync(id, User.GetUserId(), User.GetRoleName(), request, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> Cancel(int id, CancelOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await _orders.CancelAsync(id, User.GetUserId(), User.GetRoleName(), request, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
