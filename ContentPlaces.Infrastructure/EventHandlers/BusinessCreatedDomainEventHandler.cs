using ContentPlaces.Contracts.IntegrationEvents;
using ContentPlaces.Domain.Events.BusinessEvents;
using ContentPlaces.Domain.Repositories;
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
public sealed class BusinessCreatedDomainEventHandler(
    IBusinessRepository businessRepository,
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

        var business = await businessRepository.GetAsync(
            filter:      b => b.Id == evt.Id,
            include:     q => q.Include(b => b.BusinessTranslations),
            asNoTracking: false,
            ct:          ct);

        if (business is null)
        {
            logger.LogWarning(
                "BusinessCreatedDomainEvent: Business {BusinessId} not found; skipping translation.",
                evt.Id);
            return;
        }

        // ── Auto-translate Name + Description into all active languages ───────
        var fields = new Dictionary<string, string> { ["Name"] = business.Name };
        if (!string.IsNullOrWhiteSpace(business.Description))
            fields["Description"] = business.Description;

        var translationSets = await orchestrator.TranslateToAllActiveLanguagesAsync(
            fields, sourceLanguageCode: "en", ct);

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

        // ── Publish integration event via outbox ──────────────────────────────
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BusinessCreatedIntegrationEvent(
                business.Id, business.Name, business.Slug,
                business.OwnerId, business.PlaceId,
                business.IsHalal, business.HasVegetarianOptions, business.HasAlcoholFreeArea)));

        logger.LogInformation(
            "BusinessCreatedDomainEvent: queued outbox for Business {BusinessId}", business.Id);
    }
}
