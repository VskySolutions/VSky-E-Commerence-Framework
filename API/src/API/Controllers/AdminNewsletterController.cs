using Microsoft.AspNetCore.Mvc;
using VSky.API.Authorization;
using VSky.Application.Common.Authorization;
using VSky.Application.Common.Models;
using VSky.Application.Features.Newsletter;
using VSky.Domain.Enums;

namespace VSky.API.Controllers;

/// <summary>Admin management of newsletter subscribers (WO-56): paged/filterable list and CSV export.</summary>
[Route("api/admin/newsletter")]
[RequireModule(Modules.Cms)]
public class AdminNewsletterController : ApiControllerBase
{
    /// <summary>List newsletter subscribers (paged), filterable by status and email search.</summary>
    [HttpGet("subscribers")]
    public async Task<ActionResult<PaginatedList<NewsletterSubscriptionDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] NewsletterSubscriptionStatus? status = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false)
        => Ok(await Mediator.Send(new ListNewsletterSubscriptionsQuery(page, pageSize, status, search, sortBy, sortDescending)));

    /// <summary>Export all newsletter subscribers as a CSV download.</summary>
    [HttpGet("subscribers/export.csv")]
    public async Task<IActionResult> Export()
    {
        var bytes = await Mediator.Send(new ExportNewsletterSubscribersQuery());
        return File(bytes, "text/csv", "newsletter-subscribers.csv");
    }
}
