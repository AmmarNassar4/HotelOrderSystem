using HotelOrderSystem.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOrderSystem.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/v1/admin")]
public sealed class AdminController : ControllerBase
{
    // Admin endpoints are intentionally split into focused controllers:
    // - AdminDashboardController
    // - AdminOrdersController
    // - AdminTeamsController
    // - AdminRoomsController
    // - AdminUsersController
    // - ItemCategoriesController
    // - CatalogItemsController
}
