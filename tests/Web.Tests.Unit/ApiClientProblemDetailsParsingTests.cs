using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

public sealed class ApiClientProblemDetailsParsingTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public StubHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body   = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var resp = new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/problem+json"),
            };
            return Task.FromResult(resp);
        }
    }

    private static ApiClient Create(HttpStatusCode status, string body) =>
        new(
            new HttpClient(new StubHandler(status, body)) { BaseAddress = new Uri("https://api.test/") },
            NullLogger<ApiClient>.Instance);

    private sealed class TestResp
    {
        public Guid Id { get; set; }
    }

    [Fact]
    public async Task SharedKernel_single_validation_problem_is_mapped_to_field_dictionary()
    {
        const string body = """
        {
          "type":"https://tools.ietf.org/html/rfc9110#section-15.5.1",
          "title":"Validation.Name",
          "status":400,
          "detail":"'Name' must not be empty."
        }
        """;
        var client = Create(HttpStatusCode.BadRequest, body);

        var result = await client.PostAsync<TestResp>("/api/v1/places", new { });

        result.IsSuccess.Should().BeFalse();
        result.IsValidationError.Should().BeTrue();
        result.ValidationErrors.Should().NotBeNull();
        result.ValidationErrors!.Should().ContainKey("Name");
        result.ValidationErrors["Name"].Should().ContainSingle()
            .Which.Should().Be("'Name' must not be empty.");
    }

    [Fact]
    public async Task ValidationProblemDetails_errors_map_is_preserved()
    {
        const string body = """
        {
          "title":"One or more validation errors occurred.",
          "status":400,
          "errors": {
            "Name": ["'Name' must not be empty.", "'Name' must be ≤ 300 chars."],
            "Slug": ["Slug must contain only lowercase letters, digits, and hyphens."]
          }
        }
        """;
        var client = Create(HttpStatusCode.BadRequest, body);

        var result = await client.PostAsync<TestResp>("/api/v1/places", new { });

        result.IsValidationError.Should().BeTrue();
        result.ValidationErrors!.Should().ContainKey("Name");
        result.ValidationErrors["Name"].Should().HaveCount(2);
        result.ValidationErrors["Slug"].Should().ContainSingle();
    }

    [Fact]
    public async Task Conflict_response_surfaces_Detail_not_Title()
    {
        const string body = """
        {
          "title":"Place.SlugConflict",
          "status":409,
          "detail":"A place with slug 'petra' already exists."
        }
        """;
        var client = Create(HttpStatusCode.Conflict, body);

        var result = await client.PostAsync<TestResp>("/api/v1/places", new { });

        result.IsConflict.Should().BeTrue();
        result.Error.Should().Be("A place with slug 'petra' already exists.");
    }

    [Fact]
    public async Task NotFound_response_surfaces_Detail_not_Title()
    {
        const string body = """
        { "title":"Place.NotFound", "status":404, "detail":"Place 'abc' was not found." }
        """;
        var client = Create(HttpStatusCode.NotFound, body);

        var result = await client.GetAsync<TestResp>("/api/v1/places/abc");

        result.IsNotFound.Should().BeTrue();
        result.Error.Should().Be("Place 'abc' was not found.");
    }

    [Fact]
    public async Task Server_500_appends_correlationId_so_logs_can_be_grepped()
    {
        const string body = """
        {
          "title":"Internal Server Error",
          "status":500,
          "detail":"Azure Translator API returned 401: …",
          "correlationId":"00-3d31de3eff917e137d3ca3b36a47be4e-2c7016735b95f2eb-01"
        }
        """;
        var client = Create(HttpStatusCode.InternalServerError, body);

        var result = await client.PutAsync("/api/v1/places/123", new { });

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(500);
        result.Error.Should().StartWith("Azure Translator API returned 401");
        result.Error.Should().Contain("correlationId=00-3d31de3eff917e137d3ca3b36a47be4e");
    }

    [Fact]
    public async Task Forbidden_returns_IsForbidden_with_human_message()
    {
        const string body = """
        { "title":"Forbidden", "status":403 }
        """;
        var client = Create(HttpStatusCode.Forbidden, body);

        var result = await client.DeleteAsync("/api/v1/places/abc");

        result.IsForbidden.Should().BeTrue();
        result.Error.Should().Be("Forbidden");
    }

    [Fact]
    public async Task NonJson_body_does_not_throw_and_surfaces_a_short_snippet()
    {
        const string body = "<html>Backend exploded</html>";
        var client = Create(HttpStatusCode.InternalServerError, body);

        var result = await client.GetAsync<TestResp>("/api/v1/places");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(500);
        result.Error.Should().Contain("HTTP 500");
        result.Error.Should().Contain("Backend exploded");
    }

    [Fact]
    public async Task Empty_body_falls_back_to_HTTP_status_message()
    {
        var client = Create(HttpStatusCode.BadGateway, string.Empty);

        var result = await client.GetAsync<TestResp>("/api/v1/places");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(502);
        result.Error.Should().Be("HTTP 502");
    }
}
