using Analytics.Application.Scoring;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Analytics.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAnalyticsApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddScoped<IRecommendationScoringEngine, V1ContentSimilarityScorer>();
        services.AddScoped<ICollaborativeScoringEngine, NoOpCollaborativeScoringEngine>();
        services.AddScoped<IBlendedScoringEngine, NoOpBlendedScoringEngine>();

        return services;
    }
}
