using Microsoft.EntityFrameworkCore;
using VSky.Domain.Entities;

namespace VSky.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the persistence context exposed to Application-layer handlers, keeping them
/// free of a direct Infrastructure/EF Core provider dependency.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<ApiKey> ApiKeys { get; }
    DbSet<Customer> Customers { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<SmtpAccount> SmtpAccounts { get; }
    DbSet<TenantBranding> TenantBrandings { get; }
    DbSet<SupportedCurrency> SupportedCurrencies { get; }
    DbSet<Store> Stores { get; }
    DbSet<DeliveryZone> DeliveryZones { get; }
    DbSet<AdminAlert> AdminAlerts { get; }
    DbSet<PlatformSetting> PlatformSettings { get; }
    DbSet<SettingsChangeHistory> SettingsChangeHistory { get; }
    DbSet<EmailTemplate> EmailTemplates { get; }
    DbSet<EmailQueue> EmailQueue { get; }
    DbSet<MarketingSuppression> MarketingSuppressions { get; }
    DbSet<ApplicationLog> ApplicationLogs { get; }
    DbSet<AuditTrail> AuditTrails { get; }
    DbSet<BackgroundTaskLog> BackgroundTaskLogs { get; }

    // Central media library (WO-122)
    DbSet<Media> Media { get; }

    // Per-integration credential tables (Credentials_*)
    DbSet<StripeCredential> StripeCredentials { get; }
    DbSet<PayPalCredential> PayPalCredentials { get; }
    DbSet<RazorpayCredential> RazorpayCredentials { get; }
    DbSet<SquareCredential> SquareCredentials { get; }
    DbSet<AuthorizeNetCredential> AuthorizeNetCredentials { get; }
    DbSet<TaxJarCredential> TaxJarCredentials { get; }
    DbSet<StripeTaxCredential> StripeTaxCredentials { get; }
    DbSet<FedExCredential> FedExCredentials { get; }
    DbSet<DhlExpressCredential> DhlExpressCredentials { get; }
    DbSet<UspsCredential> UspsCredentials { get; }
    DbSet<UpsCredential> UpsCredentials { get; }
    DbSet<TwilioCredential> TwilioCredentials { get; }
    DbSet<AzureBlobCredential> AzureBlobCredentials { get; }

    // Catalog (WO-10, WO-11)
    DbSet<TaxCategory> TaxCategories { get; }
    DbSet<Category> Categories { get; }
    DbSet<Manufacturer> Manufacturers { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<ProductAttribute> ProductAttributes { get; }
    DbSet<ProductAttributeValue> ProductAttributeValues { get; }
    DbSet<SpecificationAttribute> SpecificationAttributes { get; }
    DbSet<SpecificationAttributeOption> SpecificationAttributeOptions { get; }
    DbSet<ProductPicture> ProductPictures { get; }
    DbSet<CategoryPicture> CategoryPictures { get; }
    DbSet<TierPrice> TierPrices { get; }
    DbSet<ProductRelationship> ProductRelationships { get; }
    DbSet<ProductTag> ProductTags { get; }
    DbSet<ProductTagMapping> ProductTagMappings { get; }
    DbSet<ProductCategory> ProductCategories { get; }
    DbSet<ProductAttributeMapping> ProductAttributeMappings { get; }
    DbSet<ProductVariantAttributeValue> ProductVariantAttributeValues { get; }
    DbSet<ProductSpecificationValue> ProductSpecificationValues { get; }
    DbSet<GroupedProductMember> GroupedProductMembers { get; }
    DbSet<ProductDownload> ProductDownloads { get; }

    // Inventory (WO-12)
    DbSet<InventoryLevel> InventoryLevels { get; }

    // Customer accounts (WO-20, WO-21)
    DbSet<Address> Addresses { get; }
    DbSet<CustomerAddress> CustomerAddresses { get; }
    DbSet<UserToken> UserTokens { get; }

    // Customer roles + group pricing (WO-22)
    DbSet<CustomerRole> CustomerRoles { get; }
    DbSet<CustomerRoleAssignment> CustomerRoleAssignments { get; }
    DbSet<CustomerGroupPrice> CustomerGroupPrices { get; }
    DbSet<ProductRoleRestriction> ProductRoleRestrictions { get; }
    DbSet<CategoryRoleRestriction> CategoryRoleRestrictions { get; }

    // Orders & store fulfilment (WO-51, WO-52)
    DbSet<Order> Orders { get; }
    DbSet<OrderLineItem> OrderLineItems { get; }
    DbSet<StoreManagerAssignment> StoreManagerAssignments { get; }
    DbSet<Shipment> Shipments { get; }
    DbSet<ShipmentLineItem> ShipmentLineItems { get; }
    DbSet<ShipmentTracking> ShipmentTrackingEvents { get; }
    DbSet<Rma> Rmas { get; }
    DbSet<RmaLineItem> RmaLineItems { get; }
    DbSet<StoreCreditTransaction> StoreCreditTransactions { get; }

    // Commerce — pricing, cart, shipping, payments, tax (Phase 3)
    DbSet<Discount> Discounts { get; }
    DbSet<CouponCode> CouponCodes { get; }
    DbSet<Cart> Carts { get; }
    DbSet<CartItem> CartItems { get; }
    DbSet<Wishlist> Wishlists { get; }
    DbSet<WishlistItem> WishlistItems { get; }
    DbSet<ShippingMethod> ShippingMethods { get; }
    DbSet<ShippingZone> ShippingZones { get; }
    DbSet<ShippingMethodZoneRate> ShippingMethodZoneRates { get; }
    DbSet<PaymentRecord> PaymentRecords { get; }
    DbSet<TaxProviderConfiguration> TaxProviderConfigurations { get; }
    DbSet<StateNexusAccumulator> StateNexusAccumulators { get; }
    DbSet<OrderStatusHistory> OrderStatusHistory { get; }

    // Security config (WO-106)
    DbSet<RecaptchaConfig> RecaptchaConfigs { get; }

    // Webhooks (WO-5)
    DbSet<WebhookSubscription> WebhookSubscriptions { get; }
    DbSet<WebhookSubscriptionEvent> WebhookSubscriptionEvents { get; }
    DbSet<WebhookDelivery> WebhookDeliveries { get; }

    // Localization (WO-18)
    DbSet<Language> Languages { get; }
    DbSet<ContentTranslation> ContentTranslations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
