using Accounts.Application.Commands.Maintenance.BackfillProviderDocuments;
using Accounts.Application.Interfaces;
using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;
using ContentCore.Contracts.Storage;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Tests.Unit;

/// <summary>
/// Patch 2B handler-level tests covering: pre-flight migrations gate, idempotency
/// via AlreadyLinked, dry-run no-write contract, skip-by-reason taxonomy
/// (NoFileUrl / InvalidPrefix / InvalidExtension / FileNotFound), and the
/// "no PII at Information level" log contract.
/// </summary>
public sealed class BackfillProviderDocumentsCommandHandlerTests
{
    private const string BaseUrl = "/uploads";

    [Fact]
    public async Task Returns_Invalid_when_pre_flight_migrations_check_fails()
    {
        var (handler, _, store, _, _, _) = BuildHandler();
        store.CheckMigrationsAsync(Arg.Any<CancellationToken>())
             .Returns(new BackfillMigrationsStatus(AccountsApplied: false, ContentCoreApplied: false));

        var result = await handler.Handle(
            new BackfillProviderDocumentsCommand(MaxRows: 100, DryRun: true, AfterId: null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().NotBeEmpty();
        // Error.Validation prefixes the code; assert the suffix component.
        result.Errors[0].Code.Should().Contain("backfill");
    }

    [Fact]
    public async Task Empty_repository_returns_zero_totals_and_no_writes()
    {
        var (handler, repo, _, registrar, _, _) = BuildHandler();
        repo.GetDocumentsForBackfillAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderDocument>());

        var result = await handler.Handle(
            new BackfillProviderDocumentsCommand(MaxRows: 100, DryRun: true, AfterId: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var report = result.Value!;
        report.Total.Should().Be(0);
        report.Inserted.Should().Be(0);
        report.Reused.Should().Be(0);
        report.Errors.Should().Be(0);
        report.HasMore.Should().BeFalse();

        await registrar.DidNotReceiveWithAnyArgs()
            .GetOrAddByStorageKeyAsync(default!, default, default);
    }

    [Fact]
    public async Task Dry_run_does_not_call_store_InsertLinkAsync()
    {
        var (handler, repo, store, registrar, storage, _) = BuildHandler();

        var doc = BuildDoc("/uploads/provider-application-documents/abcdef.pdf");
        // First call returns the doc; subsequent calls (cursor advanced past it)
        // return empty so the keyset loop terminates.
        repo.GetDocumentsForBackfillAsync(Guid.Empty, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { doc });
        repo.GetDocumentsForBackfillAsync(doc.Id, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderDocument>());

        // File exists.
        storage.OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(Result<FileDownload>.Success(
                   new FileDownload(new MemoryStream(), "application/pdf", 1)));

        // Registrar reports a NEW insertion (in dry-run mode the registrar itself
        // returns Guid.Empty + WasReused=false per contract).
        registrar.GetOrAddByStorageKeyAsync(Arg.Any<FileAssetSeed>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                 .Returns(ci => Result<FileAssetRecord>.Success(new FileAssetRecord(Guid.Empty, WasReused: false)));

        // Not already linked.
        store.IsAlreadyLinkedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await handler.Handle(
            new BackfillProviderDocumentsCommand(MaxRows: 10, DryRun: true, AfterId: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var report = result.Value!;
        report.DryRun.Should().BeTrue();
        var diag = $"Total={report.Total} Inserted={report.Inserted} Reused={report.Reused} Errors={report.Errors} Skipped=[{string.Join(",", report.SkippedByReason.Select(kv => kv.Key + "=" + kv.Value))}]";
        report.Total.Should().Be(1, "single doc returned from repo; " + diag);
        // Either Inserted OR Reused must be 1 (registrar reports it as a new add,
        // but in dry-run mode the link is not actually persisted).
        (report.Inserted + report.Reused).Should().Be(1, diag);

        // Critical contract: nothing was persisted.
        await store.DidNotReceiveWithAnyArgs()
                   .InsertLinkAsync(default, default, default, default);
    }

    [Fact]
    public async Task Already_linked_rows_are_skipped_AlreadyLinked()
    {
        var (handler, repo, store, registrar, _, _) = BuildHandler();

        var doc = BuildDoc("/uploads/provider-application-documents/abc.pdf");
        repo.GetDocumentsForBackfillAsync(Guid.Empty, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { doc });
        repo.GetDocumentsForBackfillAsync(doc.Id, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderDocument>());
        store.IsAlreadyLinkedAsync(doc.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await handler.Handle(
            new BackfillProviderDocumentsCommand(MaxRows: 10, DryRun: false, AfterId: null),
            CancellationToken.None);

        var report = result.Value!;
        report.SkippedByReason.Should().ContainKey("AlreadyLinked");
        report.SkippedByReason["AlreadyLinked"].Should().Be(1);
        report.Inserted.Should().Be(0);
        report.Reused.Should().Be(0);

        // No registrar call when already linked.
        await registrar.DidNotReceiveWithAnyArgs()
                       .GetOrAddByStorageKeyAsync(default!, default, default);
    }

    [Theory]
    [InlineData(null, "NoFileUrl")]
    [InlineData("/uploads/tours/abc.pdf", "InvalidPrefix")]
    [InlineData("/uploads/provider-application-documents/abc.exe", "InvalidExtension")]
    public async Task Bad_FileUrl_is_skipped_with_correct_reason(string? fileUrl, string expectedReason)
    {
        var (handler, repo, _, registrar, _, _) = BuildHandler();
        var doc = BuildDoc(fileUrl ?? string.Empty);
        repo.GetDocumentsForBackfillAsync(Guid.Empty, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { doc });
        repo.GetDocumentsForBackfillAsync(doc.Id, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderDocument>());

        var result = await handler.Handle(
            new BackfillProviderDocumentsCommand(MaxRows: 10, DryRun: false, AfterId: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var report = result.Value!;
        report.SkippedByReason.Should().ContainKey(expectedReason);
        report.SkippedByReason[expectedReason].Should().Be(1);

        // Parser failure short-circuits before the registrar is touched.
        await registrar.DidNotReceiveWithAnyArgs()
                       .GetOrAddByStorageKeyAsync(default!, default, default);
    }

    [Fact]
    public async Task Missing_physical_file_is_skipped_FileNotFound()
    {
        var (handler, repo, _, registrar, storage, _) = BuildHandler();

        var doc = BuildDoc("/uploads/provider-application-documents/abc.pdf");
        repo.GetDocumentsForBackfillAsync(Guid.Empty, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { doc });
        repo.GetDocumentsForBackfillAsync(doc.Id, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderDocument>());
        storage.OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(Result<FileDownload>.Failure(Error.NotFound("File"), Outcome.NotFound));

        var result = await handler.Handle(
            new BackfillProviderDocumentsCommand(MaxRows: 10, DryRun: false, AfterId: null),
            CancellationToken.None);

        var report = result.Value!;
        report.SkippedByReason.Should().ContainKey("FileNotFound");
        report.SkippedByReason["FileNotFound"].Should().Be(1);

        await registrar.DidNotReceiveWithAnyArgs()
                       .GetOrAddByStorageKeyAsync(default!, default, default);
    }

    [Fact]
    public async Task Information_logs_never_contain_FileUrl_StorageKey_or_FileName()
    {
        // Capture every log record.
        var captured = new List<CapturedLog>();
        var (handler, repo, store, registrar, storage, _) = BuildHandler(captured);

        const string secretFileName = "my-secret-PII-document.pdf";
        const string secretFileUrl  = "/uploads/provider-application-documents/01910000-0000-7000-8000-000000000abc.pdf";
        const string secretStorageKey = "provider-application-documents/01910000-0000-7000-8000-000000000abc.pdf";

        var doc = BuildDoc(secretFileUrl, secretFileName);
        repo.GetDocumentsForBackfillAsync(Guid.Empty, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { doc });
        repo.GetDocumentsForBackfillAsync(doc.Id, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderDocument>());
        storage.OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(Result<FileDownload>.Success(
                   new FileDownload(new MemoryStream(), "application/pdf", 1)));

        var newAssetId = Guid.CreateVersion7();
        registrar.GetOrAddByStorageKeyAsync(Arg.Any<FileAssetSeed>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                 .Returns(Result<FileAssetRecord>.Success(new FileAssetRecord(newAssetId, WasReused: false)));

        store.IsAlreadyLinkedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        store.InsertLinkAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DocumentType>(), Arg.Any<CancellationToken>())
             .Returns(LinkInsertResult.Inserted);

        var result = await handler.Handle(
            new BackfillProviderDocumentsCommand(MaxRows: 10, DryRun: false, AfterId: null),
            CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        // PII contract: at Information level or above, none of the leakable
        // strings may appear in any rendered message.
        var leakable = new[] { secretFileName, secretFileUrl, secretStorageKey, "/uploads/provider-application-documents/" };

        foreach (var rec in captured.Where(c => c.Level >= LogLevel.Information))
        {
            foreach (var needle in leakable)
            {
                rec.Rendered.Should().NotContain(needle,
                    $"PII leak at level {rec.Level}: '{rec.Rendered}'");
            }
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static (BackfillProviderDocumentsCommandHandler handler,
                    IProviderApplicationRepository repo,
                    IProviderDocumentBackfillStore store,
                    IFileAssetRegistrar registrar,
                    IFileStorageService storage,
                    List<CapturedLog> logs)
        BuildHandler(List<CapturedLog>? sharedLogs = null)
    {
        var repo = Substitute.For<IProviderApplicationRepository>();
        var store = Substitute.For<IProviderDocumentBackfillStore>();
        var registrar = Substitute.For<IFileAssetRegistrar>();
        var storage = Substitute.For<IFileStorageService>();

        // Default migrations OK so most tests don't have to set it.
        store.CheckMigrationsAsync(Arg.Any<CancellationToken>())
             .Returns(new BackfillMigrationsStatus(true, true));
        store.GetFileStorageBaseUrl().Returns(BaseUrl);

        var captured = sharedLogs ?? new List<CapturedLog>();
        var logger = new ListLogger<BackfillProviderDocumentsCommandHandler>(captured);

        var handler = new BackfillProviderDocumentsCommandHandler(repo, store, registrar, storage, logger);
        return (handler, repo, store, registrar, storage, captured);
    }

    private static ProviderDocument BuildDoc(string? fileUrl, string fileName = "license.pdf")
    {
        var application = ProviderApplication.Register(
            userId:              Guid.CreateVersion7(),
            type:                ProviderType.TourOperator,
            businessName:        "Acme Tours",
            contactEmail:        "contact@acme.com",
            contactPhone:        "+1234567890",
            address:             "123 Main St, Cairo",
            description:         "Premium tour operator in Egypt",
            typeSpecificDataJson: null).Value!;

        var addResult = application.AddDocument(
            documentType: DocumentType.BusinessLicense,
            fileUrl:       fileUrl ?? string.Empty,
            fileName:      fileName,
            fileSizeBytes: 1024);
        var doc = addResult.Value!;

        // Wire the back-navigation that EF would normally populate so the
        // handler can read doc.Application.UserId during seed building.
        var navProperty = typeof(ProviderDocument).GetProperty(nameof(ProviderDocument.Application));
        navProperty!.SetValue(doc, application);
        return doc;
    }

    private sealed record CapturedLog(LogLevel Level, string Rendered);

    private sealed class ListLogger<T>(List<CapturedLog> sink) : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            sink.Add(new CapturedLog(logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
