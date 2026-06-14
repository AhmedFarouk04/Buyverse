using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Talabat.APIs.Dtos;
using Talabat.APIs.Errors;
using Talabat.Core.Services;
using Talabat.Core.Entities.Order_Aggregation;
using Talabat.Core.Models;

namespace Talabat.APIs.Controllers
{
    [Authorize]
    public class OrdersController : ApiBaseController
    {
        private readonly IOrderService orderService;
        private readonly IMapper mapper;

        public OrdersController(IOrderService orderService, IMapper mapper)
        {
            this.orderService = orderService;
            this.mapper = mapper;
        }

        [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [HttpPost] // POST : /api/orders
        public async Task<ActionResult<Order>> CreateOrder(OrderDto orderDto)
        {
            var buyerEmail = User.FindFirstValue(ClaimTypes.Email);
            if (buyerEmail is null)
                return Unauthorized();

            var address = mapper.Map<AddressDto, Address>(orderDto.ShippingAddress);

            var order = await orderService.CreateOrderAsync(
                buyerEmail,
                orderDto.BasketId,
                address,
                orderDto.DeliveryMethodId
            );

            if (order is null)
                return BadRequest(new ApiErrorResponse(400));

            return Ok(order);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<OrderToReturnDto>>> GetOrdersForUser()
        {
            var buyerEmail = User.FindFirstValue(ClaimTypes.Email);
            if (buyerEmail is null)
                return Unauthorized();

            var orders = await orderService.GetOrdersForUserAsync(buyerEmail);

            var mappedOrders = mapper.Map<
                IReadOnlyList<Order>,
                IReadOnlyList<OrderToReturnDto>>(orders);

            return Ok(mappedOrders);
        }


        [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [HttpGet("{id}")] // GET: api/Orders/1
        public async Task<ActionResult<Order>> GetOrderForUser(int id)
        {
            var buyerEmail = User.FindFirstValue(ClaimTypes.Email);
            if (buyerEmail is null)
                return Unauthorized();

            var order = await orderService.GetOrderByIdForUserAsync(id, buyerEmail);

            if (order is null)
                return NotFound(new ApiErrorResponse(404));

            var mapperOrder = mapper.Map<Order, OrderToReturnDto>(order);
            return Ok(mapperOrder);
        }

        [HttpGet("deliverymethods")] // GET: /api/Orders/deliveryMethods
        public async Task<ActionResult<IReadOnlyList<DeliveryMethod>>> GetDeliveryMethids()
        {
            var deliveryMethods = await orderService.GetDeliveryMethodsAsync();
            return Ok(deliveryMethods);
        }

        [HttpGet("summary")] // GET: /api/Orders/summary
        [ProducesResponseType(typeof(OrderSummary), StatusCodes.Status200OK)]
        public async Task<ActionResult<OrderSummary>> GetOrderSummary()
        {
            var buyerEmail = User.FindFirstValue(ClaimTypes.Email);
            if (buyerEmail is null)
                return Unauthorized();

            var summary = await orderService.GetOrderSummaryForUserAsync(buyerEmail);
            return Ok(summary);
        }



    }
}
