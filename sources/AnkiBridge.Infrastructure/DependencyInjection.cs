using AnkiBridge.Application.Common.Contracts.Images;
using AnkiBridge.Application.Common.Contracts.Outbox;
using AnkiBridge.Application.Common.Contracts.Speech;
using AnkiBridge.Application.Common.Contracts.Storage;
using AnkiBridge.Application.Features.Dictionary.Contracts.QueryServices;
using AnkiBridge.Application.Features.Dictionary.Contracts.Scraping;
using AnkiBridge.Application.Features.Flashcard.Contracts.Anki;
using AnkiBridge.Application.Features.Flashcard.Contracts.QueryServices;
using AnkiBridge.Application.Features.Learning.Contracts.QueryServices;
using AnkiBridge.Domain.Aggregates.Dictionary;
using AnkiBridge.Domain.Aggregates.Flashcard.Decks;
using AnkiBridge.Domain.Aggregates.Flashcard.Notes;
using AnkiBridge.Domain.Aggregates.Flashcard.NoteTypes;
using AnkiBridge.Domain.Aggregates.Learning;
using AnkiBridge.Infrastructure.Outbox;
using AnkiBridge.Infrastructure.Persistence.Abstractions;
using AnkiBridge.Infrastructure.Persistence.DatabaseContext;
using AnkiBridge.Infrastructure.Persistence.Interceptors;
using AnkiBridge.Infrastructure.Persistence.QueryServices;
using AnkiBridge.Infrastructure.Persistence.Repositories;
using AnkiBridge.Infrastructure.Persistence.Seeding.Seeders;
using AnkiBridge.Infrastructure.Services.AnkiConnect;
using AnkiBridge.Infrastructure.Services.Images;
using AnkiBridge.Infrastructure.Services.Scraping;
using AnkiBridge.Infrastructure.Services.Speech;
using AnkiBridge.Infrastructure.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AnkiBridge.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // Add database
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(configuration.GetConnectionString("ankibridgedb"));
        });
        builder.EnrichNpgsqlDbContext<ApplicationDbContext>();

        // Add interceptors
        services.AddScoped<ISaveChangesInterceptor, DomainEventDispatchInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, AuditingInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, SoftDeletingInterceptor>();

        // Add seeders
        services.AddScoped<IDbSeeder, DictionarySeeder>();
        services.AddScoped<IDbSeeder, LearningSeeder>();
        services.AddScoped<IDbSeeder, NoteTypeSeeder>();
        services.AddScoped<IDbSeeder, DeckSeeder>();
        services.AddScoped<IDbSeeder, NoteSeeder>();

        // Add query services
        services.AddScoped<IDictionaryEntryQueryService, DictionaryEntryQueryService>();
        services.AddScoped<ILearningEntryQueryService, LearningEntryQueryService>();
        services.AddScoped<IDeckQueryService, DeckQueryService>();
        services.AddScoped<INoteTypeQueryService, NoteTypeQueryService>();
        services.AddScoped<INoteQueryService, NoteQueryService>();

        // Add repositories
        services.AddScoped<ILearningEntryRepository, LearningEntryRepository>();
        services.AddScoped<IDictionaryEntryRepository, DictionaryEntryRepository>();
        services.AddScoped<IDeckRepository, DeckRepository>();
        services.AddScoped<INoteTypeRepository, NoteTypeRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();

        // Add transactional outbox
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddHostedService<OutboxProcessor>();

        // Add storage
        builder.AddAzureBlobServiceClient("blobs");
        services.AddScoped<IFileStorage, AzureBlobStorage>();

        // Add anki service
        services.AddScoped<IAnkiService, AnkiConnectService>();
        services.AddHttpClient<IAnkiConnectClient, AnkiConnectClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:8765"); // AnkiConnect
        });

        services.AddMediaServices(configuration);

        services.AddScrapingServices(configuration);

        return builder;
    }

    public static IServiceCollection AddMediaServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSpeechServices(configuration);
        services.AddImageServices(configuration);
        return services;
    }

    // ── Speech ────────────────────────────────────────────────────────────────

    private static IServiceCollection AddSpeechServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GoogleSpeechOptions>(configuration.GetSection(GoogleSpeechOptions.SectionName));

        // No HttpClient — GoogleSpeechSynthesizer only builds URLs.
        services.AddSingleton<ISpeechSynthesizer, GoogleSpeechSynthesizer>();

        return services;
    }

    // ── Images ────────────────────────────────────────────────────────────────

    private static IServiceCollection AddImageServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PixabayOptions>(configuration.GetSection(PixabayOptions.SectionName));

        services.Configure<PexelsOptions>(configuration.GetSection(PexelsOptions.SectionName));

        // Typed HttpClients — concrete types injected directly into CompositeImageProvider.
        services
            .AddHttpClient<PixabayImageProvider>()
            .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(10));

        services
            .AddHttpClient<PexelsImageProvider>()
            .ConfigureHttpClient((sp, client) =>
            {
                var opts = sp.GetRequiredService<IOptions<PexelsOptions>>().Value;
                client.DefaultRequestHeaders.Add("Authorization", opts.ApiKey);
                client.Timeout = TimeSpan.FromSeconds(10);
            });

        services.AddScoped<CompositeImageProvider>();
        services.AddScoped<IImageProvider, CompositeImageProvider>();

        return services;
    }

    public static IServiceCollection AddScrapingServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CambridgeDictionaryOptions>(
            configuration.GetSection(CambridgeDictionaryOptions.SectionName));

        services.AddScoped<IDictionaryProvider, CambridgeDictionaryProvider>();
        services.AddScoped<IPhraseIpaResolver, CambridgePhraseIpaResolver>();

        services.AddHttpClient<GoogleTranslationProvider>();
        services.AddScoped<CambridgeTranslationProvider>();
        services.AddScoped<GoogleTranslationProvider>();
        services.AddScoped<ITranslationProvider, FallbackTranslationProvider>();

        return services;
    }
}
