using VSky.Application.Features.Customers;
using VSky.Application.Tests.Common;
using VSky.Domain.Entities;
using Xunit;

namespace VSky.Application.Tests.Accounts;

/// <summary>
/// Verifies the admin customer list only returns genuine customers (a User with a Customer profile
/// and zero admin RBAC roles) and never staff/admin accounts — which also carry a Customer profile.
/// </summary>
public class CustomerListFilterTests : CatalogTestBase
{
    private void SeedAccount(string email, string firstName, bool asAdmin)
    {
        using var db = NewContext();
        var user = new User
        {
            Username = email,
            Email = email,
            PasswordHash = "hashed:whatever",
            EmailVerified = true,
            IsActive = true,
            Customer = new Customer { FirstName = firstName, LastName = "Test" },
        };
        if (asAdmin)
        {
            var role = new Role { Name = "TenantAdmin-" + Guid.NewGuid().ToString("n"), NormalizedName = "TA", IsSystemRole = true };
            user.UserRoles.Add(new UserRole { Role = role, AssignedOnUtc = DateTime.UtcNow });
        }
        db.Users.Add(user);
        db.SaveChanges();
    }

    [Fact]
    public async Task List_excludes_admin_accounts_and_includes_customers()
    {
        SeedAccount("shopper@test.com", "Shopper", asAdmin: false);
        SeedAccount("staff@test.com", "Staff", asAdmin: true);

        using var db = NewContext();
        var handler = new ListCustomersQueryHandler(db);
        var result = await handler.Handle(new ListCustomersQuery(Page: 1, PageSize: 50), CancellationToken.None);

        Assert.Contains(result.Items, c => c.Email == "shopper@test.com");
        Assert.DoesNotContain(result.Items, c => c.Email == "staff@test.com");
    }

    [Fact]
    public async Task List_search_still_never_returns_admins()
    {
        SeedAccount("admin.jane@test.com", "Jane", asAdmin: true);
        SeedAccount("jane.shopper@test.com", "Jane", asAdmin: false);

        using var db = NewContext();
        var handler = new ListCustomersQueryHandler(db);
        var result = await handler.Handle(new ListCustomersQuery(Page: 1, PageSize: 50, Search: "Jane"), CancellationToken.None);

        Assert.Contains(result.Items, c => c.Email == "jane.shopper@test.com");
        Assert.DoesNotContain(result.Items, c => c.Email == "admin.jane@test.com");
    }
}
