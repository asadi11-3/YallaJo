using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Accounts.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds Accounts.ProviderApplication aggregates for the four test "provider" users
/// (guide-pending, guide-approved, business, agency). Runs AFTER AccountsDbInitializer so
/// the corresponding Profiles already exist. Idempotent: re-running adds nothing.
/// </summary>
public sealed class AccountsProviderApplicationSeeder(AccountsDbContext dbContext) : IModuleDbInitializer
{
    public int Order => 51;

    private static readonly Guid AdminUserId = new("b0000000-0000-0000-0000-000000000001");

    /// <summary>End-state target for each seeded provider application.</summary>
    private enum SeedEndState
    {
        Pending,
        Approved
    }

    private sealed record ProviderSeedSpec(
        Guid UserId,
        ProviderType Type,
        string BusinessName,
        string ContactEmail,
        string ContactPhone,
        string Address,
        string Description,
        SeedEndState EndState
    );

    private static readonly IReadOnlyList<ProviderSeedSpec> Targets =
    [
        new(
            new Guid("b0000000-0000-0000-0000-000000000004"),
            ProviderType.IndependentGuide,
            "Pending Guide Co.",
            "guide-pending@yallajo.test",
            "+962790000004",
            "Amman, Jordan",
            "Test seed: independent guide, awaiting admin review.",
            SeedEndState.Pending
        ),
        new(
            new Guid("b0000000-0000-0000-0000-000000000005"),
            ProviderType.IndependentGuide,
            "Approved Guide Co.",
            "guide-approved@yallajo.test",
            "+962790000005",
            "Amman, Jordan",
            "Test seed: independent guide, approved provider.",
            SeedEndState.Approved
        ),
        new(
            new Guid("b0000000-0000-0000-0000-000000000006"),
            ProviderType.BusinessOwner,
            "Test Business LLC",
            "business@yallajo.test",
            "+962790000006",
            "Amman, Jordan",
            "Test seed: business owner, approved provider.",
            SeedEndState.Approved
        ),
        new(
            new Guid("b0000000-0000-0000-0000-000000000007"),
            ProviderType.Agency,
            "Test Travel Agency",
            "agency@yallajo.test",
            "+962790000007",
            "Amman, Jordan",
            "Test seed: travel agency, approved provider.",
            SeedEndState.Approved
        )
    ];

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Idempotent: skip seeds that already have an application row.
        var existingAppUserIds = await dbContext.ProviderApplications
            .Select(a => a.UserId)
            .ToListAsync(cancellationToken);
        var existing = new HashSet<Guid>(existingAppUserIds);

        var pending = Targets.Where(t => !existing.Contains(t.UserId)).ToList();
        if (pending.Count == 0)
        {
            return;
        }

        foreach (var spec in pending)
        {
            var application = BuildApplication(spec);
            dbContext.ProviderApplications.Add(application);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ProviderApplication BuildApplication(ProviderSeedSpec spec)
    {
        var registerResult = ProviderApplication.Register(
            spec.UserId,
            spec.Type,
            spec.BusinessName,
            spec.ContactEmail,
            spec.ContactPhone,
            spec.Address,
            spec.Description
        );
        EnsureSuccess(registerResult, "Register", spec);
        var application = registerResult.Value!;

        AddRequiredDocuments(application, spec);

        var submitResult = application.Submit();
        EnsureSuccess(submitResult, "Submit", spec);

        if (spec.EndState == SeedEndState.Approved)
        {
            var approveResult = application.Approve(AdminUserId);
            EnsureSuccess(approveResult, "Approve", spec);
        }

        return application;
    }

    private static void AddRequiredDocuments(ProviderApplication application, ProviderSeedSpec spec)
    {
        var missingDocs = application.GetMissingDocumentTypes();
        var expiresAt = DateTime.UtcNow.AddDays(365);

        foreach (var docType in missingDocs)
        {
            var fileName = $"{docType}.pdf";
            var fileUrl = $"https://cdn.yallajo.test/docs/{spec.UserId:N}/{docType}.pdf";
            const long fileSizeBytes = 1024L;

            var addResult = application.AddDocument(docType, fileUrl, fileName, fileSizeBytes, expiresAt);
            EnsureSuccess(addResult, $"AddDocument({docType})", spec);
        }
    }

    private static void EnsureSuccess(Result result, string operation, ProviderSeedSpec spec)
    {
        if (result.IsSuccess)
        {
            return;
        }

        var detail = FormatFailure(result.Errors, result.Messages);
        throw new InvalidOperationException(
            $"Seeding ProviderApplication for {spec.ContactEmail} failed at {operation}: {detail}");
    }

    private static void EnsureSuccess<T>(Result<T> result, string operation, ProviderSeedSpec spec)
    {
        if (result.IsSuccess)
        {
            return;
        }

        var detail = FormatFailure(result.Errors, result.Messages);
        throw new InvalidOperationException(
            $"Seeding ProviderApplication for {spec.ContactEmail} failed at {operation}: {detail}");
    }

    private static string FormatFailure(IReadOnlyList<Error> errors, IReadOnlyList<string> messages)
    {
        if (errors.Count > 0)
        {
            return string.Join("; ", errors.Select(e => $"{e.Code}: {e.Message}"));
        }

        if (messages.Count > 0)
        {
            return string.Join("; ", messages);
        }

        return "Unknown failure.";
    }
}
