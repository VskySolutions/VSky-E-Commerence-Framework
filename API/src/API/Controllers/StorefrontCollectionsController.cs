using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VSky.Application.Features.CmsProductCollections;
using VSky.Application.Features.StorefrontCatalog;

namespace VSky.API.Controllers;

/// <summary>
/// Public (anonymous) storefront read for admin-curated product collections (WO-97). Returns a collection's
/// published products in curated order; disabled/deleted collections and unpublished/soft-deleted products
/// are excluded.
/// </summary>
[Route("api/storefront/collections")]
[AllowAnonymous]
public class StorefrontCollectionsController : ApiControllerBase
{
    /// <summary>The collection's published products, in the admin-curated order (empty when the collection is
    /// disabled, deleted or unknown).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IReadOnlyList<StorefrontProductSummaryDto>>> Get(Guid id)
        => Ok(await Mediator.Send(new GetCollectionProductsQuery(id)));
}
