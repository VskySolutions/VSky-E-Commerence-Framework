using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VSky.Application.Features.ProductReviews;

namespace VSky.API.Controllers;

/// <summary>
/// Public storefront reviews for a product (WO-14): anyone can read a product's approved reviews and
/// rating summary; a signed-in customer who purchased the product can submit one.
/// </summary>
[Route("api/storefront/products/{productId:guid}/reviews")]
public class StorefrontProductReviewsController : ApiControllerBase
{
    /// <summary>The product's approved reviews (newest first) plus its rating summary; empty + disabled when reviews are off.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ProductReviewListResultDto>> Get(Guid productId)
        => Ok(await Mediator.Send(new GetProductReviewsQuery(productId)));

    /// <summary>Submit a review for the product as the signed-in customer (route product id wins over the body).</summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ProductReviewDto>> Submit(Guid productId, [FromBody] SubmitProductReviewCommand command)
        => Ok(await Mediator.Send(command with { ProductId = productId }));
}
