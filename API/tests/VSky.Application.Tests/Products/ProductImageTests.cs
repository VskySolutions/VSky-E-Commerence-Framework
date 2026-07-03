using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Features.Products;
using VSky.Application.Tests.Common;
using VSky.Domain.Enums;
using Xunit;

namespace VSky.Application.Tests.Products;

public class ProductImageTests : CatalogTestBase
{
    // ---- Add --------------------------------------------------------------------------------------

    [Fact]
    public async Task Add_creates_product_level_image()
    {
        var productId = SeedProduct();

        var dto = await new AddProductImageCommandHandler(NewContext()).Handle(
            new AddProductImageCommand(productId, null, ProductMediaType.Image, "https://cdn/a.png", DisplayOrder: 1),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Null(dto.ProductVariantId);

        await using var db = NewContext();
        Assert.Equal(1, await db.ProductImages.CountAsync(i => i.ProductId == productId));
    }

    [Fact]
    public async Task Add_creates_variant_level_image()
    {
        var productId = SeedProduct(p => p.ProductType = ProductType.WithVariants);
        var variantId = SeedVariant(productId);

        var dto = await new AddProductImageCommandHandler(NewContext()).Handle(
            new AddProductImageCommand(productId, variantId, ProductMediaType.Video, "https://youtu.be/x"),
            CancellationToken.None);

        Assert.Equal(variantId, dto.ProductVariantId);
        Assert.Equal(ProductMediaType.Video, dto.MediaType);
    }

    [Fact]
    public async Task Add_throws_NotFound_when_product_missing()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => new AddProductImageCommandHandler(NewContext()).Handle(
            new AddProductImageCommand(Guid.NewGuid(), null, ProductMediaType.Image, "https://cdn/a.png"),
            CancellationToken.None));
    }

    [Fact]
    public async Task Add_throws_NotFound_when_variant_belongs_to_other_product()
    {
        var productId = SeedProduct();
        var otherProductId = SeedProduct(p => p.ProductType = ProductType.WithVariants);
        var foreignVariantId = SeedVariant(otherProductId);

        await Assert.ThrowsAsync<NotFoundException>(() => new AddProductImageCommandHandler(NewContext()).Handle(
            new AddProductImageCommand(productId, foreignVariantId, ProductMediaType.Image, "https://cdn/a.png"),
            CancellationToken.None));
    }

    // ---- Update -----------------------------------------------------------------------------------

    [Fact]
    public async Task Update_changes_image_fields()
    {
        var productId = SeedProduct();
        var imageId = SeedImage(productId, url: "https://cdn/old.png");

        var dto = await new UpdateProductImageCommandHandler(NewContext()).Handle(
            new UpdateProductImageCommand(imageId, ProductMediaType.Image, "https://cdn/new.png", AltText: "alt", DisplayOrder: 3),
            CancellationToken.None);

        Assert.Equal("https://cdn/new.png", dto.Url);
        Assert.Equal("alt", dto.AltText);
        Assert.Equal(3, dto.DisplayOrder);
    }

    [Fact]
    public async Task Update_throws_NotFound_when_image_missing()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => new UpdateProductImageCommandHandler(NewContext()).Handle(
            new UpdateProductImageCommand(Guid.NewGuid(), ProductMediaType.Image, "https://cdn/x.png"),
            CancellationToken.None));
    }

    [Fact]
    public async Task Update_throws_NotFound_when_variant_belongs_to_other_product()
    {
        var productId = SeedProduct();
        var imageId = SeedImage(productId);
        var otherProductId = SeedProduct(p => p.ProductType = ProductType.WithVariants);
        var foreignVariantId = SeedVariant(otherProductId);

        await Assert.ThrowsAsync<NotFoundException>(() => new UpdateProductImageCommandHandler(NewContext()).Handle(
            new UpdateProductImageCommand(imageId, ProductMediaType.Image, "https://cdn/x.png", ProductVariantId: foreignVariantId),
            CancellationToken.None));
    }

    // ---- Delete -----------------------------------------------------------------------------------

    [Fact]
    public async Task Delete_removes_image()
    {
        var productId = SeedProduct();
        var imageId = SeedImage(productId);

        await new DeleteProductImageCommandHandler(NewContext()).Handle(new DeleteProductImageCommand(imageId), CancellationToken.None);

        await using var db = NewContext();
        Assert.False(await db.ProductImages.AnyAsync(i => i.Id == imageId));
    }

    [Fact]
    public async Task Delete_is_idempotent_for_missing_id()
    {
        await new DeleteProductImageCommandHandler(NewContext()).Handle(new DeleteProductImageCommand(Guid.NewGuid()), CancellationToken.None);
    }

    // ---- Replace ----------------------------------------------------------------------------------

    [Fact]
    public async Task Replace_swaps_entire_gallery()
    {
        var productId = SeedProduct();
        SeedImage(productId, url: "https://cdn/1.png", displayOrder: 0);
        SeedImage(productId, url: "https://cdn/2.png", displayOrder: 1);

        var result = await new ReplaceProductImagesCommandHandler(NewContext()).Handle(
            new ReplaceProductImagesCommand(productId, new()
            {
                new ProductImageInput(null, ProductMediaType.Image, "https://cdn/only.png", DisplayOrder: 0),
            }),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("https://cdn/only.png", result[0].Url);

        await using var db = NewContext();
        Assert.Equal(1, await db.ProductImages.CountAsync(i => i.ProductId == productId));
    }

    [Fact]
    public async Task Replace_throws_NotFound_for_unknown_variant()
    {
        var productId = SeedProduct();

        await Assert.ThrowsAsync<NotFoundException>(() => new ReplaceProductImagesCommandHandler(NewContext()).Handle(
            new ReplaceProductImagesCommand(productId, new()
            {
                new ProductImageInput(Guid.NewGuid(), ProductMediaType.Image, "https://cdn/x.png"),
            }),
            CancellationToken.None));
    }
}
