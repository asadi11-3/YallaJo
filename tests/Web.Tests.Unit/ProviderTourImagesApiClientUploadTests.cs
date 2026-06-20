using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// Regression for the Provider Tour Images upload failing with
/// "Required parameter 'string EntityType' was not provided from form".
///
/// The ContentCore upload endpoint (POST /api/v1/content-core/attachments) binds
/// EntityType/EntityId/AttachmentType from the multipart form body ([FromForm]),
/// not the query string. <see cref="ProviderTourImagesApiClient.UploadImageAsync"/>
/// must therefore send those values as multipart form fields. These tests read the
/// raw multipart payload to lock that contract in.
/// </summary>
public sealed class ProviderTourImagesApiClientUploadTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Calls { get; } = new();
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(request);
            if (request.Content is not null)
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            };
        }
    }

    private static (ProviderTourImagesApiClient Sut, CapturingHandler Handler) CreateSut()
    {
        var handler = new CapturingHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return (new ProviderTourImagesApiClient(api), handler);
    }

    private static MemoryStream Png() => new(new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4 });

    [Fact]
    public async Task UploadImageAsync_ShouldPostTo_AttachmentsRoot_WithoutQueryString()
    {
        var (sut, handler) = CreateSut();
        var tourId = Guid.NewGuid();

        await sut.UploadImageAsync(tourId, Png(), "photo.png", "image/png");

        handler.Calls.Should().HaveCount(1);
        handler.Calls[0].Method.Should().Be(HttpMethod.Post);
        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().Be("/api/v1/content-core/attachments");
        // The bug shipped EntityType/EntityId/AttachmentType as query params; assert none remain.
        handler.Calls[0].RequestUri!.Query.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadImageAsync_ShouldSend_EntityType_EntityId_AttachmentType_AsFormFields()
    {
        var (sut, handler) = CreateSut();
        var tourId = Guid.NewGuid();

        await sut.UploadImageAsync(tourId, Png(), "photo.png", "image/png");

        handler.LastBody.Should().NotBeNull();
        // Normalize quoting: different HttpClient versions emit name=Foo or name="Foo".
        var body = handler.LastBody!.Replace("\"", string.Empty);

        // Multipart field headers carry the form-field names; the values follow in the body.
        body.Should().Contain("name=EntityType");
        body.Should().Contain("Tour");
        body.Should().Contain("name=EntityId");
        body.Should().Contain(tourId.ToString());
        body.Should().Contain("name=AttachmentType");
        body.Should().Contain("Image");
        // SortOrder is a non-nullable [FromForm] int on the endpoint, so it must be present.
        body.Should().Contain("name=SortOrder");
        body.Should().Contain("name=file");
    }
}
