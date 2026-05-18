using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentSeo.Infrastructure.Persistence.Seeding;

public sealed class ContentSeoDbInitializer(ContentSeoDbContext dbContext) : IModuleDbInitializer
{
    private static readonly Guid LanguageEnglish = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001");
    private static readonly Guid LanguageArabic = Guid.Parse("eeeeeeee-0000-0000-0000-000000000002");
    private static readonly Guid SeoTourId = Guid.Parse("e1e1e1e1-0000-0000-0000-000000000001");
    private static readonly Guid SeoBlogId = Guid.Parse("e1e1e1e1-0000-0000-0000-000000000002");
    private static readonly Guid WeatherId = Guid.Parse("e1e1e1e1-0000-0000-0000-000000000003");
    private static readonly Guid FaqId = Guid.Parse("e1e1e1e1-0000-0000-0000-000000000004");
    private static readonly Guid FaqTranslationEnId = Guid.Parse("e1e1e1e1-0000-0000-0000-000000000005");
    private static readonly Guid FaqTranslationArId = Guid.Parse("e1e1e1e1-0000-0000-0000-000000000006");
    private static readonly Guid SitemapHomeId = Guid.Parse("e1e1e1e1-0000-0000-0000-000000000007");
    private static readonly Guid SitemapTourId = Guid.Parse("e1e1e1e1-0000-0000-0000-000000000008");
    private static readonly Guid RedirectId = Guid.Parse("e1e1e1e1-0000-0000-0000-000000000009");

    public int Order => 150;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.SeoMetadata.AnyAsync(cancellationToken))
        {
            return;
        }

        var seoMetadata = CreateSeoMetadata();
        var weather = CreateWeatherCaches();
        var faqItems = CreateFaqItems();
        var faqTranslations = CreateFaqTranslations();
        var sitemapEntries = CreateSitemapEntries();
        var redirects = CreateRedirects();

        dbContext.SeoMetadata.AddRange(seoMetadata);
        dbContext.WeatherCaches.AddRange(weather);
        dbContext.FaqItems.AddRange(faqItems);
        dbContext.FaqItemTranslations.AddRange(faqTranslations);
        dbContext.SitemapEntries.AddRange(sitemapEntries);
        dbContext.Redirects.AddRange(redirects);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<SeoMetadata> CreateSeoMetadata()
    {
        var tour = CreateEntity<SeoMetadata>();
        SetProperty(tour, nameof(SeoMetadata.Id), SeoTourId);
        SetProperty(tour, nameof(SeoMetadata.EntityType), SeoEntityType.Tour);
        SetProperty(tour, nameof(SeoMetadata.EntityId), SeedContentIds.TourPetraExplorer);
        SetProperty(tour, nameof(SeoMetadata.MetaTitle), "Petra Full Day Explorer Tour | YallaJo");
        SetProperty(tour, nameof(SeoMetadata.MetaDescription), "Book a guided Petra full-day experience with local experts.");
        SetProperty(tour, nameof(SeoMetadata.CanonicalUrl), "https://yallajo.local/tours/petra-full-day-explorer");
        SetProperty(tour, nameof(SeoMetadata.OgTitle), "Petra Full Day Explorer");
        SetProperty(tour, nameof(SeoMetadata.OgDescription), "Explore Petra with a licensed local guide.");
        SetProperty(tour, nameof(SeoMetadata.OgImageUrl), "https://yallajo.local/assets/og/petra-tour.jpg");
        SetProperty(tour, nameof(SeoMetadata.SchemaMarkup), "{\"@type\":\"TouristTrip\",\"name\":\"Petra Full Day Explorer\"}");
        SetProperty(tour, nameof(SeoMetadata.SitemapPriority), 0.9m);
        SetProperty(tour, nameof(SeoMetadata.SitemapChangeFrequency), "weekly");

        var blog = CreateEntity<SeoMetadata>();
        SetProperty(blog, nameof(SeoMetadata.Id), SeoBlogId);
        SetProperty(blog, nameof(SeoMetadata.EntityType), SeoEntityType.Blog);
        SetProperty(blog, nameof(SeoMetadata.EntityId), Guid.Parse("b1b1b1b1-0000-0000-0000-000000000001"));
        SetProperty(blog, nameof(SeoMetadata.MetaTitle), "Petra Sunrise Tips | YallaJo Blog");
        SetProperty(blog, nameof(SeoMetadata.MetaDescription), "Practical first-time visitor guidance for Petra sunrise tours.");
        SetProperty(blog, nameof(SeoMetadata.CanonicalUrl), "https://yallajo.local/blog/petra-sunrise-practical-tips");
        SetProperty(blog, nameof(SeoMetadata.OgTitle), "Petra Sunrise: Practical Tips");
        SetProperty(blog, nameof(SeoMetadata.OgDescription), "Route planning and essentials for your Petra visit.");
        SetProperty(blog, nameof(SeoMetadata.OgImageUrl), "https://yallajo.local/assets/og/petra-blog.jpg");
        SetProperty(blog, nameof(SeoMetadata.SchemaMarkup), "{\"@type\":\"BlogPosting\",\"headline\":\"Petra Sunrise Tips\"}");
        SetProperty(blog, nameof(SeoMetadata.SitemapPriority), 0.7m);
        SetProperty(blog, nameof(SeoMetadata.SitemapChangeFrequency), "monthly");

        return [tour, blog];
    }

    private static List<WeatherCache> CreateWeatherCaches()
    {
        var weather = CreateEntity<WeatherCache>();
        SetProperty(weather, nameof(WeatherCache.Id), WeatherId);
        SetProperty(weather, nameof(WeatherCache.PlaceId), SeedContentIds.PlacePetra);
        SetProperty(weather, nameof(WeatherCache.RoundedLatitude), WeatherCache.RoundCoordinate(30.3285m));
        SetProperty(weather, nameof(WeatherCache.RoundedLongitude), WeatherCache.RoundCoordinate(35.4444m));
        SetProperty(weather, nameof(WeatherCache.ForecastDate), DateOnly.FromDateTime(DateTime.UtcNow));
        SetProperty(weather, nameof(WeatherCache.Temperature), 24.5m);
        SetProperty(weather, nameof(WeatherCache.FeelsLike), 25.8m);
        SetProperty(weather, nameof(WeatherCache.Humidity), 37);
        SetProperty(weather, nameof(WeatherCache.WindSpeed), 14.2m);
        SetProperty(weather, nameof(WeatherCache.WindDirection), 265);
        SetProperty(weather, nameof(WeatherCache.Condition), "Clear");
        SetProperty(weather, nameof(WeatherCache.Icon), "clear-day");
        SetProperty(weather, nameof(WeatherCache.UvIndex), 7.4m);
        SetProperty(weather, nameof(WeatherCache.ForecastJson), "[]");
        SetProperty(weather, nameof(WeatherCache.FetchedAt), DateTime.UtcNow.AddMinutes(-20));
        SetProperty(weather, nameof(WeatherCache.ExpiresAt), DateTime.UtcNow.AddHours(2));
        return [weather];
    }

    private static List<FaqItem> CreateFaqItems()
    {
        var faq = CreateEntity<FaqItem>();
        SetProperty(faq, nameof(FaqItem.Id), FaqId);
        SetProperty(faq, nameof(FaqItem.EntityType), SeoEntityType.Tour);
        SetProperty(faq, nameof(FaqItem.EntityId), SeedContentIds.TourPetraExplorer);
        SetProperty(faq, nameof(FaqItem.Question), "What should I bring for the Petra Full Day Explorer tour?");
        SetProperty(faq, nameof(FaqItem.Answer), "Bring water, sunscreen, a hat, and comfortable walking shoes. A light jacket is recommended in winter.");
        SetProperty(faq, nameof(FaqItem.SortOrder), 1);
        SetProperty(faq, nameof(FaqItem.IsActive), true);
        return [faq];
    }

    private static List<FaqItemTranslation> CreateFaqTranslations()
    {
        var en = CreateEntity<FaqItemTranslation>();
        SetProperty(en, nameof(FaqItemTranslation.Id), FaqTranslationEnId);
        SetProperty(en, nameof(FaqItemTranslation.FaqItemId), FaqId);
        SetProperty(en, nameof(FaqItemTranslation.LanguageId), LanguageEnglish);
        SetProperty(en, nameof(FaqItemTranslation.Question), "What should I bring for the Petra Full Day Explorer tour?");
        SetProperty(en, nameof(FaqItemTranslation.Answer), "Bring water, sunscreen, a hat, and comfortable walking shoes.");

        var ar = CreateEntity<FaqItemTranslation>();
        SetProperty(ar, nameof(FaqItemTranslation.Id), FaqTranslationArId);
        SetProperty(ar, nameof(FaqItemTranslation.FaqItemId), FaqId);
        SetProperty(ar, nameof(FaqItemTranslation.LanguageId), LanguageArabic);
        SetProperty(ar, nameof(FaqItemTranslation.Question), "What should I bring for the Petra Full Day Explorer tour?");
        SetProperty(ar, nameof(FaqItemTranslation.Answer), "Bring water, sunscreen, and walking shoes suitable for long trails.");

        return [en, ar];
    }

    private static List<SitemapEntry> CreateSitemapEntries()
    {
        var home = CreateEntity<SitemapEntry>();
        SetProperty(home, nameof(SitemapEntry.Id), SitemapHomeId);
        SetProperty(home, nameof(SitemapEntry.Url), "https://yallajo.local/");
        SetProperty(home, nameof(SitemapEntry.ChangeFrequency), "daily");
        SetProperty(home, nameof(SitemapEntry.Priority), 1.0m);
        SetProperty(home, nameof(SitemapEntry.LastModified), DateTime.UtcNow.Date);
        SetProperty(home, nameof(SitemapEntry.EntityType), "Page");
        SetProperty<Guid?>(home, nameof(SitemapEntry.EntityId), null);
        SetProperty(home, nameof(SitemapEntry.IsActive), true);

        var tour = CreateEntity<SitemapEntry>();
        SetProperty(tour, nameof(SitemapEntry.Id), SitemapTourId);
        SetProperty(tour, nameof(SitemapEntry.Url), "https://yallajo.local/tours/petra-full-day-explorer");
        SetProperty(tour, nameof(SitemapEntry.ChangeFrequency), "weekly");
        SetProperty(tour, nameof(SitemapEntry.Priority), 0.9m);
        SetProperty(tour, nameof(SitemapEntry.LastModified), DateTime.UtcNow.Date.AddDays(-1));
        SetProperty(tour, nameof(SitemapEntry.EntityType), "Tour");
        SetProperty(tour, nameof(SitemapEntry.EntityId), SeedContentIds.TourPetraExplorer);
        SetProperty(tour, nameof(SitemapEntry.IsActive), true);

        return [home, tour];
    }

    private static List<Redirect> CreateRedirects()
    {
        var redirect = CreateEntity<Redirect>();
        SetProperty(redirect, nameof(Redirect.Id), RedirectId);
        SetProperty(redirect, nameof(Redirect.OldUrl), "https://yallajo.local/blog/petra-guide");
        SetProperty(redirect, nameof(Redirect.NewUrl), "https://yallajo.local/blog/petra-sunrise-practical-tips");
        SetProperty(redirect, nameof(Redirect.StatusCode), 301);
        SetProperty(redirect, nameof(Redirect.IsActive), true);
        SetProperty(redirect, nameof(Redirect.HitCount), 12);
        return [redirect];
    }

    private static TEntity CreateEntity<TEntity>() where TEntity : class
    {
        var entity = Activator.CreateInstance(typeof(TEntity), nonPublic: true) as TEntity;
        if (entity is null)
        {
            throw new InvalidOperationException($"Failed to create entity instance for {typeof(TEntity).FullName}.");
        }

        return entity;
    }

    private static void SetProperty<TValue>(object target, string propertyName, TValue value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (property is null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().FullName}.");
        }

        property.SetValue(target, value);
    }
}
