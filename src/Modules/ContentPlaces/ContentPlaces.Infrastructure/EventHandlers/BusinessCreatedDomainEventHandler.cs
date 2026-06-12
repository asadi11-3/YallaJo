using ContentPlaces.Contracts.IntegrationEvents;
using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Events.BusinessEvents;
using ContentPlaces.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentPlaces.Infrastructure.EventHandlers;

/// <summary>
/// Triggers auto-translation for all active languages when a Business is created,
/// then writes a <see cref="BusinessCreatedIntegrationEvent"/> to the outbox.
/// Runs in the same UoW scope as the command — does NOT call SaveChangesAsync.
/// </summary>
/// <remarks>
/// Domain events are dispatched before SaveChanges, so the new Business is still tracked as
/// <c>Added</c> and not yet queryable from the database. We resolve it from the change tracker
/// (DbSet.Local) rather than via a repository query (which would return null pre-save and skip
/// both translation and the outbox notification).
/// </remarks>
public sealed class BusinessCreatedDomainEventHandler(
    IEntityTranslationOrchestrator orchestrator,
    ContentPlacesDbContext dbContext,
    ILogger<BusinessCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BusinessCreatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<BusinessCreatedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        // Resolve the just-created aggregate from the change tracker (state = Added).
        var business = dbContext.Set<Business>().Local.FirstOrDefault(b => b.Id == evt.Id);

        if (business is null)
        {
            logger.LogWarning(
                "BusinessCreatedDomainEvent: Business {BusinessId} not tracked; skipping translation and outbox.",
                evt.Id);
            return;
        }

        // ── Publish integration event via outbox first (mandatory; translation is best-effort) ──
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BusinessCreatedIntegrationEvent(
                business.Id, business.Name, business.Slug,
                business.OwnerId, business.PlaceId,
                business.IsHalal, business.HasVegetarianOptions, business.HasAlcoholFreeArea)));

        try
        {
            // ── Auto-translate Name + Description into all active languages ───────
            var fields = new Dictionary<string, string> { ["Name"] = business.Name };
            if (!string.IsNullOrWhiteSpace(business.Description))
                fields["Description"] = business.Description;

            var translationSets = await orchestrator.TranslateToAllActiveLanguagesAsync(
                fields, sourceLanguageCode: "en", ct).ConfigureAwait(false);

            var addedCount = 0;

            foreach (var set in translationSets)
            {
                if (business.BusinessTranslations.Any(t => t.LanguageId == set.LanguageId))
                    continue;

                if (!set.Fields.TryGetValue("Name", out var translatedName) ||
                    string.IsNullOrWhiteSpace(translatedName))
                    continue;

                set.Fields.TryGetValue("Description", out var translatedDescription);

                business.AddOrUpdateTranslation(
                    set.LanguageId,
                    translatedName,
                    string.IsNullOrWhiteSpace(translatedDescription) ? null : translatedDescription);

                addedCount++;
            }

            if (addedCount > 0)
            {
                logger.LogInformation(
                    "BusinessCreatedDomainEvent: Added {Count} translations for Business {BusinessId}.",
                    addedCount, business.Id);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "BusinessCreatedDomainEvent: translation failed for Business {BusinessId}; created with source language only.",
                business.Id);
        }
    }
}
