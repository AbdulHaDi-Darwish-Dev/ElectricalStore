using ElectricalStore.Api.Http;
using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Authorization;
using ElectricalStore.Application.Catalog.Products;
using Permixa.AspNetCore.Authorization;

namespace ElectricalStore.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        MapAdminProductEndpoints(app);
        MapPublicProductEndpoints(app);
        return app;
    }

    private static void MapAdminProductEndpoints(IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/admin/products")
            .WithTags("Back Office - Products")
            .RequireAuthorization()
            .RequirePermission(AppPermissions.Products.Manage);

        admin.MapPost("/", async (
                CreateProductRequest request,
                CreateProductUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(request, cancellationToken);
                return result.ToHttpResult(created =>
                    Results.Created($"/admin/products/{created.Id}", created));
            })
            .WithName("CreateProduct")
            .WithSummary("Create product with variants");

        admin.MapGet("/", async (
                Guid? categoryId,
                bool? isActive,
                string? search,
                ListAdminProductsUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var list = await useCase.ExecuteAsync(
                    new ProductListFilter(categoryId, isActive, search),
                    cancellationToken);
                return Results.Ok(list);
            })
            .WithName("ListAdminProducts")
            .WithSummary("List products");

        admin.MapGet("/{id:guid}", async (
                Guid id,
                GetAdminProductByIdUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("GetAdminProductById")
            .WithSummary("Get product by ID");

        admin.MapPut("/{id:guid}", async (
                Guid id,
                UpdateProductRequest request,
                UpdateProductUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, request, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("UpdateProduct")
            .WithSummary("Update product");

        admin.MapPost("/{id:guid}/activate", async (
                Guid id,
                ActivateProductUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("ActivateProduct")
            .WithSummary("Activate product");

        admin.MapPost("/{id:guid}/deactivate", async (
                Guid id,
                DeactivateProductUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("DeactivateProduct")
            .WithSummary("Deactivate product");

        admin.MapPost("/{id:guid}/variants", async (
                Guid id,
                CreateProductVariantRequest request,
                AddProductVariantUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, request, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("AddProductVariant")
            .WithSummary("Add product variant");

        admin.MapPut("/{id:guid}/variants/{variantId:guid}", async (
                Guid id,
                Guid variantId,
                UpdateProductVariantRequest request,
                UpdateProductVariantUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, variantId, request, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("UpdateProductVariant")
            .WithSummary("Update product variant");

        admin.MapPost("/{id:guid}/variants/{variantId:guid}/activate", async (
                Guid id,
                Guid variantId,
                ActivateProductVariantUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, variantId, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("ActivateProductVariant")
            .WithSummary("Activate product variant");

        admin.MapPost("/{id:guid}/variants/{variantId:guid}/deactivate", async (
                Guid id,
                Guid variantId,
                DeactivateProductVariantUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, variantId, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("DeactivateProductVariant")
            .WithSummary("Deactivate product variant");

        admin.MapPost("/{id:guid}/images", async (
                Guid id,
                IFormFile file,
                AddProductImageUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                if (file is null)
                    return Results.BadRequest();

                await using var stream = file.OpenReadStream();
                var result = await useCase.ExecuteAsync(
                    id,
                    stream,
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    cancellationToken);
                return result.ToHttpResult();
            })
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .WithName("AddProductImage")
            .WithSummary("Upload product image");

        admin.MapDelete("/{id:guid}/images/{imageId:guid}", async (
                Guid id,
                Guid imageId,
                DeleteProductImageUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, imageId, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("DeleteProductImage")
            .WithSummary("Delete product image");

        admin.MapPost("/{id:guid}/images/{imageId:guid}/primary", async (
                Guid id,
                Guid imageId,
                SetPrimaryProductImageUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, imageId, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("SetPrimaryProductImage")
            .WithSummary("Set primary product image");

        admin.MapPut("/{id:guid}/images/order", async (
                Guid id,
                ReorderProductImagesRequest request,
                ReorderProductImagesUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, request.OrderedImageIds, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("ReorderProductImages")
            .WithSummary("Reorder product images");
    }

    private static void MapPublicProductEndpoints(IEndpointRouteBuilder app)
    {
        var catalog = app.MapGroup("/catalog/products")
            .WithTags("Catalog - Products");

        catalog.MapGet("/", async (
                Guid? categoryId,
                string? search,
                ListCatalogProductsUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var list = await useCase.ExecuteAsync(
                    new CatalogProductListFilter(categoryId, search),
                    cancellationToken);
                return Results.Ok(list);
            })
            .WithName("ListCatalogProducts")
            .WithSummary("List catalog products");

        catalog.MapGet("/{id:guid}", async (
                Guid id,
                GetCatalogProductByIdUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("GetCatalogProductById")
            .WithSummary("Get catalog product by ID");
    }
}
