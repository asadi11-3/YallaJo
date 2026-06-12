// <copyright file="FaqItemCreatedDomainEventHandler.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Infrastructure.EventHandlers;

using ContentSeo.Contracts.IntegrationEvents;
using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Events;
using ContentSeo.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Clock;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Infrastructure.Outbox;

/// <summary>
/// Handles FaqItemCreatedDomainEvent:
///   1. Stages a <see cref="FaqItemChangedIntegrationEvent"/> (Created) to the outbox.
///   2. Auto-translates Question/Answer into all active languages.
/// </summary>
/// <remarks>
/// Domain events are dispatched before SaveChanges, so the new FaqItem is still tracked as
/// <c>Added</c> and not yet queryable from the database. We resolve it from the change tracker
/// (DbSet.Local). Translation is best-effort and must never fail FAQ creation.
/// </remarks>
public sealed class FaqItemCreatedDomainEventHandler(
    ContentSeoDbContext dbContext,
    IEntityTranslationOrchestrator orchestrator,
    IDateTimeProvider clock,
    ILogger<FaqItemCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<FaqItemCreatedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<FaqItemCreatedDomainEvent> notification, CancellationToken ct)
    {
        var evt = notification.Event;

        // Mandatory: stage the cross-module change notification.
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new FaqItemChangedIntegrationEvent(evt.FaqItemId, evt.EntityType, evt.EntityId, "Created", clock.UtcNow)));
        logger.LogDebug("Staged FaqItemChangedIntegrationEvent (Created) for {Id}", evt.FaqItemId);

        // Best-effort: auto-translate Question/Answer into all active languages.
        var faq = dbContext.Set<FaqItem>().Local.FirstOrDefault(f => f.Id == evt.FaqItemId);
        if (faq is null)
        {
            logger.LogWarning(
                "FaqItemCreatedDomainEvent: FaqItem {FaqItemId} not tracked; skipping translation.",
                evt.FaqItemId);
            return;
        }

        try
        {
            var fields = new Dictionary<string, string>
            {
                ["Question"] = faq.Question,
                ["Answer"] = faq.Answer
            };

            var translationSets = await orchestrator.TranslateToAllActiveLanguagesAsync(
                fields, sourceLanguageCode: "en", ct).ConfigureAwait(false);

            var addedCount = 0;

            foreach (var set in translationSets)
            {
                if (faq.FaqItemTranslations.Any(t => t.LanguageId == set.LanguageId))
                    continue;

                if (!set.Fields.TryGetValue("Question", out var translatedQuestion) ||
                    string.IsNullOrWhiteSpace(translatedQuestion))
                    continue;

                if (!set.Fields.TryGetValue("Answer", out var translatedAnswer) ||
                    string.IsNullOrWhiteSpace(translatedAnswer))
                    continue;

                dbContext.Set<FaqItemTranslation>().Add(
                    FaqItemTranslation.Create(faq.Id, set.LanguageId, translatedQuestion, translatedAnswer));

                addedCount++;
            }

            if (addedCount > 0)
            {
                logger.LogInformation(
                    "FaqItemCreatedDomainEvent: Added {Count} translations for FaqItem {FaqItemId}.",
                    addedCount, evt.FaqItemId);
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
                "FaqItemCreatedDomainEvent: translation failed for FaqItem {FaqItemId}; created with source language only.",
                evt.FaqItemId);
        }
    }
}
