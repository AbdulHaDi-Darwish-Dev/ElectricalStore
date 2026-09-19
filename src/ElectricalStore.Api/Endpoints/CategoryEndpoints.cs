using ElectricalStore.Api.Http;
using ElectricalStore.Application.Authorization;
using ElectricalStore.Application.Catalog.Categories;
using Permixa.AspNetCore.Authorization;

namespace ElectricalStore.Api.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        MapAdminCategoryEndpoints(app);
        MapPublicCategoryEndpoints(app);
        return app;
    }

    private static void MapAdminCategoryEndpoints(IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/admin/categories")
            .WithTags("Back Office - Categories")
            .RequireAuthorization()
            .RequirePermission(AppPermissions.Categories.Manage);

        admin.MapPost("/", async (
                CreateCategoryRequest request,
                CreateCategoryUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(request, cancellationToken);
                return result.ToHttpResult(created =>
                    Results.Created($"/admin/categories/{created.Id}", created));
            })
            .WithName("CreateCategory")
            .WithSummary("Create category");

        admin.MapGet("/", async (
                ListAdminCategoriesUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var list = await useCase.ExecuteAsync(cancellationToken);
                return Results.Ok(list);
            })
            .WithName("ListAdminCategories")
            .WithSummary("List categories");

        admin.MapGet("/{id:guid}", async (
                Guid id,
                GetAdminCategoryByIdUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("GetAdminCategoryById")
            .WithSummary("Get category by ID");

        admin.MapPut("/{id:guid}", async (
                Guid id,
                UpdateCategoryRequest request,
                UpdateCategoryUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, request, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("UpdateCategory")
            .WithSummary("Update category");

        admin.MapPost("/{id:guid}/activate", async (
                Guid id,
                ActivateCategoryUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("ActivateCategory")
            .WithSummary("Activate category");

        admin.MapPost("/{id:guid}/deactivate", async (
                Guid id,
                DeactivateCategoryUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("DeactivateCategory")
            .WithSummary("Deactivate category");

        admin.MapPut("/{id:guid}/image", async (
                Guid id,
                IFormFile file,
                UpsertCategoryImageUseCase useCase,
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
            .WithName("UpsertCategoryImage")
            .WithSummary("Upload or replace category image");

        admin.MapDelete("/{id:guid}/image", async (
                Guid id,
                DeleteCategoryImageUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("DeleteCategoryImage")
            .WithSummary("Delete category image");
    }

    private static void MapPublicCategoryEndpoints(IEndpointRouteBuilder app)
    {
        var catalog = app.MapGroup("/catalog/categories")
            .WithTags("Catalog - Categories");

        catalog.MapGet("/", async (
                ListActiveCategoriesUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var list = await useCase.ExecuteAsync(cancellationToken);
                return Results.Ok(list);
            })
            .WithName("ListActiveCategories")
            .WithSummary("List active categories");

        catalog.MapGet("/{id:guid}", async (
                Guid id,
                GetActiveCategoryByIdUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("GetActiveCategoryById")
            .WithSummary("Get active category by ID");
    }
}
