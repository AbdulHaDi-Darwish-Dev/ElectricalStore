using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ElectricalStore.Application.Authorization;
using ElectricalStore.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ElectricalStore.Infrastructure.Persistence;
using Xunit;

namespace ElectricalStore.IntegrationTests;

public sealed class OrderingApiTests : IClassFixture<AppWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AppWebApplicationFactory _factory;

    public OrderingApiTests(AppWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CheckoutPreview_PlaceGuestOrder_Confirm_Lifecycle_AndAccess()
    {
        var owner = await CreateOwnerClientAsync();
        var (variantId, zoneId, unitPrice) = await SeedPurchasableAsync(owner, stock: 10m, price: 200m, shippingFee: 1500m);

        var anonymous = _factory.CreateClient();

        var preview = await anonymous.PostAsJsonAsync("/checkout/preview", new
        {
            items = new[] { new { variantId, quantity = 2m } },
            deliveryZoneId = zoneId
        });
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        var previewDto = (await preview.Content.ReadFromJsonAsync<CheckoutPreviewResponse>(Json))!;
        Assert.Equal(400m, previewDto.MerchandiseSubtotal);
        Assert.Equal(1500m, previewDto.ShippingFee);
        Assert.Equal(1900m, previewDto.Total);
        Assert.Equal(unitPrice, previewDto.Items[0].UnitPrice);

        var badIncrement = await anonymous.PostAsJsonAsync("/checkout/preview", new
        {
            items = new[] { new { variantId, quantity = 1.5m } },
            deliveryZoneId = zoneId
        });
        Assert.Equal(HttpStatusCode.BadRequest, badIncrement.StatusCode);

        var place = await PlaceOrderAsync(anonymous, new
        {
            items = new[] { new { variantId, quantity = 2m } },
            deliveryZoneId = zoneId,
            customerName = "Guest Buyer",
            phone = "0944444444",
            addressText = "Aleppo Street 1",
            customerNote = "call first"
        });
        Assert.Equal(HttpStatusCode.Created, place.StatusCode);
        var placed = (await place.Content.ReadFromJsonAsync<OrderResponse>(Json))!;
        Assert.Equal("PendingConfirmation", placed.Status);
        Assert.Equal("Unpaid", placed.PaymentStatus);
        Assert.False(string.IsNullOrWhiteSpace(placed.GuestAccessToken));
        Assert.Equal(1900m, placed.Total);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stock = await db.InventoryItems.AsNoTracking()
                .SingleAsync(x => x.ProductVariantId == variantId);
            Assert.Equal(0m, stock.Reserved);
            var order = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == placed.Id);
            Assert.DoesNotContain(placed.GuestAccessToken!, order.GuestAccessTokenHash!);
        }

        var track = _factory.CreateClient();
        track.DefaultRequestHeaders.Add("X-Order-Token", placed.GuestAccessToken);
        var tracked = await track.GetAsync($"/orders/{placed.Id}/track");
        tracked.EnsureSuccessStatusCode();

        var badToken = _factory.CreateClient();
        badToken.DefaultRequestHeaders.Add("X-Order-Token", "not-a-real-token");
        var leaked = await badToken.GetAsync($"/orders/{placed.Id}/track");
        Assert.Equal(HttpStatusCode.NotFound, leaked.StatusCode);

        var confirm = await owner.PostAsync($"/admin/orders/{placed.Id}/confirm", null);
        confirm.EnsureSuccessStatusCode();
        var confirmed = (await confirm.Content.ReadFromJsonAsync<OrderResponse>(Json))!;
        Assert.Equal("Confirmed", confirmed.Status);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stock = await db.InventoryItems.AsNoTracking()
                .SingleAsync(x => x.ProductVariantId == variantId);
            Assert.Equal(10m, stock.OnHand);
            Assert.Equal(2m, stock.Reserved);
        }

        var doubleConfirm = await owner.PostAsync($"/admin/orders/{placed.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.Conflict, doubleConfirm.StatusCode);

        var prepare = await owner.PostAsync($"/admin/orders/{placed.Id}/prepare", null);
        prepare.EnsureSuccessStatusCode();

        var dispatch = await owner.PostAsync($"/admin/orders/{placed.Id}/out-for-delivery", null);
        dispatch.EnsureSuccessStatusCode();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stock = await db.InventoryItems.AsNoTracking()
                .SingleAsync(x => x.ProductVariantId == variantId);
            Assert.Equal(8m, stock.OnHand);
            Assert.Equal(0m, stock.Reserved);
        }

        var deliver = await owner.PostAsync($"/admin/orders/{placed.Id}/deliver", null);
        deliver.EnsureSuccessStatusCode();

        var markPaid = await owner.PostAsync($"/admin/orders/{placed.Id}/mark-paid", null);
        markPaid.EnsureSuccessStatusCode();
        var paid = (await markPaid.Content.ReadFromJsonAsync<OrderResponse>(Json))!;
        Assert.Equal("Paid", paid.PaymentStatus);

        var markPaidAgain = await owner.PostAsync($"/admin/orders/{placed.Id}/mark-paid", null);
        Assert.Equal(HttpStatusCode.Conflict, markPaidAgain.StatusCode);

        var me = await owner.GetAsync("/me");
        me.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        var permissions = doc.RootElement.GetProperty("permissions")
            .EnumerateArray()
            .Select(p => p.GetString())
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains(AppPermissions.Orders.Read, permissions);
        Assert.Contains(AppPermissions.Orders.Manage, permissions);
        Assert.Contains(AppPermissions.Settings.Manage, permissions);
    }

    [Fact]
    public async Task MarkPaid_Forbidden_BeforeOutForDelivery()
    {
        var owner = await CreateOwnerClientAsync();
        var (variantId, zoneId, _) = await SeedPurchasableAsync(owner, stock: 5m, price: 100m, shippingFee: 0m);
        var place = await PlaceOrderAsync(_factory.CreateClient(), new
        {
            items = new[] { new { variantId, quantity = 1m } },
            deliveryZoneId = zoneId,
            customerName = "G",
            phone = "0977777777",
            addressText = "A"
        });
        place.EnsureSuccessStatusCode();
        var order = (await place.Content.ReadFromJsonAsync<OrderResponse>(Json))!;

        Assert.Equal(HttpStatusCode.Conflict,
            (await owner.PostAsync($"/admin/orders/{order.Id}/mark-paid", null)).StatusCode);

        (await owner.PostAsync($"/admin/orders/{order.Id}/confirm", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict,
            (await owner.PostAsync($"/admin/orders/{order.Id}/mark-paid", null)).StatusCode);

        (await owner.PostAsync($"/admin/orders/{order.Id}/prepare", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict,
            (await owner.PostAsync($"/admin/orders/{order.Id}/mark-paid", null)).StatusCode);

        (await owner.PostAsync($"/admin/orders/{order.Id}/out-for-delivery", null)).EnsureSuccessStatusCode();
        (await owner.PostAsync($"/admin/orders/{order.Id}/mark-paid", null)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task PlaceOrder_IdempotencyKey_ReplaysSameOrderAndGuestToken()
    {
        var owner = await CreateOwnerClientAsync();
        var (variantId, zoneId, _) = await SeedPurchasableAsync(owner, stock: 5m, price: 100m, shippingFee: 0m);
        var key = "idem-" + Guid.NewGuid().ToString("N");
        var body = new
        {
            items = new[] { new { variantId, quantity = 1m } },
            deliveryZoneId = zoneId,
            customerName = "Guest",
            phone = "0988888888",
            addressText = "Addr"
        };

        var first = await PlaceOrderAsync(_factory.CreateClient(), body, key);
        first.EnsureSuccessStatusCode();
        var a = (await first.Content.ReadFromJsonAsync<OrderResponse>(Json))!;

        var second = await PlaceOrderAsync(_factory.CreateClient(), body, key);
        second.EnsureSuccessStatusCode();
        var b = (await second.Content.ReadFromJsonAsync<OrderResponse>(Json))!;

        Assert.Equal(a.Id, b.Id);
        Assert.Equal(a.GuestAccessToken, b.GuestAccessToken);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.Orders.CountAsync(o => o.Id == a.Id));
    }

    [Fact]
    public async Task OrderingSettings_AdminGetPut_AffectsPreview_NotHistoricalPending()
    {
        var owner = await CreateOwnerClientAsync();
        var (variantId, zoneId, _) = await SeedPurchasableAsync(owner, stock: 20m, price: 100m, shippingFee: 0m);

        var put = await owner.PutAsJsonAsync("/admin/settings/ordering", new { minimumMerchandiseSubtotal = 500m });
        put.EnsureSuccessStatusCode();

        var previewLow = await _factory.CreateClient().PostAsJsonAsync("/checkout/preview", new
        {
            items = new[] { new { variantId, quantity = 1m } },
            deliveryZoneId = zoneId
        });
        previewLow.EnsureSuccessStatusCode();
        using (var doc = JsonDocument.Parse(await previewLow.Content.ReadAsStringAsync()))
        {
            Assert.False(doc.RootElement.GetProperty("meetsMinimumOrder").GetBoolean());
        }

        await owner.PutAsJsonAsync("/admin/settings/ordering", new { minimumMerchandiseSubtotal = 0m });

        var place = await PlaceOrderAsync(_factory.CreateClient(), new
        {
            items = new[] { new { variantId, quantity = 1m } },
            deliveryZoneId = zoneId,
            customerName = "G",
            phone = "0999999999",
            addressText = "A"
        });
        place.EnsureSuccessStatusCode();
        var order = (await place.Content.ReadFromJsonAsync<OrderResponse>(Json))!;

        await owner.PutAsJsonAsync("/admin/settings/ordering", new { minimumMerchandiseSubtotal = 10000m });

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Order-Token", order.GuestAccessToken);
        var modify = await client.PutAsJsonAsync($"/orders/{order.Id}/items", new
        {
            items = new[] { new { variantId, quantity = 1m } },
            deliveryZoneId = zoneId
        });
        modify.EnsureSuccessStatusCode();

        // Reset singleton for other tests sharing the factory DB.
        (await owner.PutAsJsonAsync("/admin/settings/ordering", new { minimumMerchandiseSubtotal = 0m }))
            .EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AuthenticatedOrder_ListOwnOnly_CustomerCancelPending_AdminCancelReleases()
    {
        var owner = await CreateOwnerClientAsync();
        var (variantId, zoneId, _) = await SeedPurchasableAsync(owner, stock: 5m, price: 100m, shippingFee: 0m);

        var customer = await RegisterCustomerAsync();
        var place = await PlaceOrderAsync(customer, new
        {
            items = new[] { new { variantId, quantity = 1m } },
            deliveryZoneId = zoneId,
            customerName = "Alice",
            phone = "0955555555",
            addressText = "Home"
        });
        place.EnsureSuccessStatusCode();
        var order = (await place.Content.ReadFromJsonAsync<OrderResponse>(Json))!;
        Assert.Null(order.GuestAccessToken);

        var list = await customer.GetAsync("/orders");
        list.EnsureSuccessStatusCode();
        var mine = (await list.Content.ReadFromJsonAsync<List<OrderListItem>>(Json))!;
        Assert.Contains(mine, o => o.Id == order.Id);

        var other = await RegisterCustomerAsync();
        var foreign = await other.GetAsync($"/orders/{order.Id}");
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);

        var cancel = await customer.PostAsJsonAsync($"/orders/{order.Id}/cancel", new { reason = "changed mind" });
        cancel.EnsureSuccessStatusCode();

        var (variantB, zoneB, _) = await SeedPurchasableAsync(owner, stock: 3m, price: 80m, shippingFee: 10m);
        var place2 = await PlaceOrderAsync(customer, new
        {
            items = new[] { new { variantId = variantB, quantity = 1m } },
            deliveryZoneId = zoneB,
            customerName = "Alice",
            phone = "0955555555",
            addressText = "Home"
        });
        place2.EnsureSuccessStatusCode();
        var order2 = (await place2.Content.ReadFromJsonAsync<OrderResponse>(Json))!;

        (await owner.PostAsync($"/admin/orders/{order2.Id}/confirm", null)).EnsureSuccessStatusCode();

        var customerCancelConfirmed = await customer.PostAsJsonAsync($"/orders/{order2.Id}/cancel", new { reason = "nope" });
        Assert.Equal(HttpStatusCode.Conflict, customerCancelConfirmed.StatusCode);

        var adminCancel = await owner.PostAsJsonAsync($"/admin/orders/{order2.Id}/cancel", new { reason = "customer called" });
        adminCancel.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stock = await db.InventoryItems.AsNoTracking().SingleAsync(x => x.ProductVariantId == variantB);
        Assert.Equal(3m, stock.OnHand);
        Assert.Equal(0m, stock.Reserved);
    }

    [Fact]
    public async Task ModifyPending_Reprices_AndConfirmedModifyRejected()
    {
        var owner = await CreateOwnerClientAsync();
        var (variantId, zoneId, _) = await SeedPurchasableAsync(owner, stock: 20m, price: 100m, shippingFee: 50m);

        var place = await PlaceOrderAsync(_factory.CreateClient(), new
        {
            items = new[] { new { variantId, quantity = 1m } },
            deliveryZoneId = zoneId,
            customerName = "Guest",
            phone = "0966666666",
            addressText = "Addr"
        });
        place.EnsureSuccessStatusCode();
        var order = (await place.Content.ReadFromJsonAsync<OrderResponse>(Json))!;

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Order-Token", order.GuestAccessToken);
        var modify = await client.PutAsJsonAsync($"/orders/{order.Id}/items", new
        {
            items = new[] { new { variantId, quantity = 3m } },
            deliveryZoneId = zoneId,
            reason = (string?)null
        });
        modify.EnsureSuccessStatusCode();
        var modified = (await modify.Content.ReadFromJsonAsync<OrderResponse>(Json))!;
        Assert.Equal(300m, modified.MerchandiseSubtotal);
        Assert.Equal(350m, modified.Total);

        (await owner.PostAsync($"/admin/orders/{order.Id}/confirm", null)).EnsureSuccessStatusCode();

        var modifyConfirmed = await client.PutAsJsonAsync($"/orders/{order.Id}/items", new
        {
            items = new[] { new { variantId, quantity = 1m } },
            deliveryZoneId = zoneId
        });
        Assert.Equal(HttpStatusCode.Conflict, modifyConfirmed.StatusCode);
    }

    [Fact]
    public async Task AdminOrders_RequirePermissions()
    {
        var user = await RegisterCustomerAsync();
        var response = await user.GetAsync("/admin/orders");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> PlaceOrderAsync(HttpClient client, object body, string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/orders")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.TryAddWithoutValidation(
            "Idempotency-Key",
            idempotencyKey ?? ("idem-" + Guid.NewGuid().ToString("N")));
        return await client.SendAsync(request);
    }

    private async Task<HttpClient> CreateOwnerClientAsync()
    {
        var client = _factory.CreateClient();
        var tokens = await client.LoginAsOwnerAsync();
        return _factory.CreateAuthenticatedClient(tokens.AccessToken);
    }

    private async Task<HttpClient> RegisterCustomerAsync()
    {
        var client = _factory.CreateClient();
        var email = $"cust_{Guid.NewGuid():N}@example.test";
        var userName = $"c{Guid.NewGuid():N}"[..12];
        (await client.PostAsJsonAsync("/auth/register", new
        {
            userName,
            email,
            password = TestKeys.UserPassword
        })).EnsureSuccessStatusCode();
        await _factory.MarkEmailConfirmedAsync(email);
        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password = TestKeys.UserPassword
        });
        login.EnsureSuccessStatusCode();
        var tokens = (await login.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;
        return _factory.CreateAuthenticatedClient(tokens.AccessToken);
    }

    private async Task<(Guid VariantId, Guid ZoneId, decimal Price)> SeedPurchasableAsync(
        HttpClient owner,
        decimal stock,
        decimal price,
        decimal shippingFee)
    {
        var cat = await owner.PostAsJsonAsync("/admin/categories", new
        {
            name = "Ord Cat " + Guid.NewGuid().ToString("N")[..6],
            isActive = true
        });
        cat.EnsureSuccessStatusCode();
        var category = (await cat.Content.ReadFromJsonAsync<IdResponse>(Json))!;
        await UploadAsync(owner, HttpMethod.Put, $"/admin/categories/{category.Id}/image");

        var sku = "ORD-" + Guid.NewGuid().ToString("N")[..8];
        var create = await owner.PostAsJsonAsync("/admin/products", new
        {
            name = "Ord Product " + sku,
            categoryId = category.Id,
            isActive = true,
            variants = new[]
            {
                new
                {
                    name = "Standard",
                    sku,
                    price,
                    sellingUnit = "Piece",
                    quantityIncrement = 1m,
                    isActive = true
                }
            }
        });
        create.EnsureSuccessStatusCode();
        var product = (await create.Content.ReadFromJsonAsync<ProductResponse>(Json))!;
        await UploadAsync(owner, HttpMethod.Post, $"/admin/products/{product.Id}/images");

        var variantId = product.Variants[0].Id;
        (await owner.PostAsJsonAsync($"/admin/inventory/{variantId}/adjust", new
        {
            quantityDelta = stock,
            reason = "seed"
        })).EnsureSuccessStatusCode();

        var zone = await owner.PostAsJsonAsync("/admin/shipping/zones", new
        {
            name = "Ord Zone " + Guid.NewGuid().ToString("N")[..6],
            fee = shippingFee,
            isActive = true
        });
        zone.EnsureSuccessStatusCode();
        var zoneDto = (await zone.Content.ReadFromJsonAsync<IdResponse>(Json))!;
        return (variantId, zoneDto.Id, price);
    }

    private static async Task UploadAsync(HttpClient owner, HttpMethod method, string url)
    {
        byte[] jpeg =
        [
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9
        ];
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(jpeg);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(file, "file", "img.jpg");
        using var request = new HttpRequestMessage(method, url) { Content = content };
        (await owner.SendAsync(request)).EnsureSuccessStatusCode();
    }

    private sealed class IdResponse
    {
        public Guid Id { get; set; }
    }

    private sealed class ProductResponse
    {
        public Guid Id { get; set; }
        public List<VariantResponse> Variants { get; set; } = new();
    }

    private sealed class VariantResponse
    {
        public Guid Id { get; set; }
    }

    private sealed class CheckoutPreviewResponse
    {
        public List<CheckoutLineResponse> Items { get; set; } = new();
        public decimal ShippingFee { get; set; }
        public decimal MerchandiseSubtotal { get; set; }
        public decimal Total { get; set; }
    }

    private sealed class CheckoutLineResponse
    {
        public decimal UnitPrice { get; set; }
    }

    private sealed class OrderResponse
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public decimal MerchandiseSubtotal { get; set; }
        public decimal Total { get; set; }
        public string? GuestAccessToken { get; set; }
    }

    private sealed class OrderListItem
    {
        public Guid Id { get; set; }
    }
}
