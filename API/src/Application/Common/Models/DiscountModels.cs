using VSky.Domain.Enums;

namespace VSky.Application.Common.Models;

/// <summary>
/// A single discount rule that reduced the cart, together with the reduction it contributed
/// (REQ-PRP-001). Part of the itemized breakdown returned by discount evaluation.
/// </summary>
public record AppliedDiscount(Guid DiscountId, string Name, DiscountScope Scope, decimal Amount);

/// <summary>
/// The itemized outcome of evaluating all applicable discounts against a cart: the discounts that
/// applied and the aggregate reduction (<see cref="TotalDiscount"/> equals the sum of the amounts).
/// </summary>
public record DiscountEvaluationResult(List<AppliedDiscount> Applied, decimal TotalDiscount);

/// <summary>
/// One cart line projected for discount evaluation: the product, the categories it belongs to
/// (for category-scoped rules), the line's monetary total and its quantity.
/// </summary>
public record DiscountCartLine(Guid ProductId, IReadOnlyList<Guid> CategoryIds, decimal LineTotal, int Quantity);
