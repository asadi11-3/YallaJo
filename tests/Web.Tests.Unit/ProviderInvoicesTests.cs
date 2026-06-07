using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// FE-2B-3 — provider invoices: the ApiClient targets the seller-scoped
/// /api/v1/invoices/provider/my-invoices endpoint and the existing
/// /{id}/download endpoint, and the Facade maps the page to rows, handles the
/// provider-mismatch 403 with an actionable message, and forces sign-out on 401.
/// </summary>
public sealed class ProviderInvoicesTests
{
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Calls { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(request);
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private static (ProviderInvoicesApiClient Sut, StubHandler Handler) CreateApiClient(
        HttpStatusCode status = HttpStatusCode.OK, string body = "{}")
    {
        var handler = new StubHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return (new ProviderInvoicesApiClient(api), handler);
    }

    private static ProviderInvoicesFacade CreateFacade(HttpStatusCode status, string body)
    {
        var handler = new StubHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return new ProviderInvoicesFacade(
            new ProviderInvoicesApiClient(api), NullLogger<ProviderInvoicesFacade>.Instance);
    }

    private const string OnePageJson = """
        {
          "items": [
            {
              "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
              "paymentId": "00000000-0000-0000-0000-000000000001",
              "bookingId": "00000000-0000-0000-0000-000000000002",
              "userId": "00000000-0000-0000-0000-000000000003",
              "providerId": "00000000-0000-0000-0000-000000000004",
              "invoiceNumber": "INV-2026-0001",
              "status": "Paid",
              "currency": "JOD",
              "amountSubtotal": 90, "amountTax": 10, "amountDiscount": 0, "amountTotal": 100,
              "issuedAt": "2026-02-01T10:00:00Z",
              "buyerName": "Sample Customer", "buyerEmail": "c@example.com",
              "sellerName": "My Tours", "sellerTaxId": null,
              "pdfAvailable": true,
              "items": [ { "description": "Tour", "quantity": 2, "unitPrice": 45, "subtotal": 90 } ]
            }
          ],
          "nextCursor": null
        }
        """;

    // ── ApiClient routes ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyInvoicesAsync_TargetsProviderInvoicesEndpoint()
    {
        var (sut, handler) = CreateApiClient(body: OnePageJson);

        await sut.GetMyInvoicesAsync();

        handler.Calls[0].Method.Should().Be(HttpMethod.Get);
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be("/api/v1/invoices/provider/my-invoices");
        handler.Calls[0].RequestUri!.Query.Should().Contain("pageSize=");
    }

    [Fact]
    public async Task DownloadAsync_TargetsInvoiceDownloadEndpoint()
    {
        var (sut, handler) = CreateApiClient(body: "{}");
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        await sut.DownloadAsync(id);

        handler.Calls[0].Method.Should().Be(HttpMethod.Get);
        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().Be("/api/v1/invoices/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/download");
    }

    // ── Facade mapping ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_MapsPage_ToRows_WithBuyerName()
    {
        var facade = CreateFacade(HttpStatusCode.OK, OnePageJson);

        var result = await facade.GetAsync();

        result.IsSuccess.Should().BeTrue();
        result.Data!.HasInvoices.Should().BeTrue();
        var row = result.Data.Invoices.Single();
        row.InvoiceNumber.Should().Be("INV-2026-0001");
        row.BuyerName.Should().Be("Sample Customer");
        row.PdfAvailable.Should().BeTrue();
        row.ItemCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAsync_OnForbidden_ReturnsActionableProviderMismatchMessage()
    {
        var facade = CreateFacade(HttpStatusCode.Forbidden,
            """{"title":"Invoice.OwnerMismatch","detail":"Caller is not a provider."}""");

        var result = await facade.GetAsync();

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("sign out and sign in again");
    }

    [Fact]
    public async Task GetAsync_OnUnauthorized_ForcesSignOut()
    {
        var facade = CreateFacade(HttpStatusCode.Unauthorized, "{}");

        var result = await facade.GetAsync();

        result.RequireSignOut.Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_EmptyPage_ReturnsNoInvoices()
    {
        var facade = CreateFacade(HttpStatusCode.OK, """{"items":[],"nextCursor":null}""");

        var result = await facade.GetAsync();

        result.IsSuccess.Should().BeTrue();
        result.Data!.HasInvoices.Should().BeFalse();
    }
}
