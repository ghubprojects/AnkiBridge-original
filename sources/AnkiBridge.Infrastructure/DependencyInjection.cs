using AnkiBridge.Application.Abstractions.Dictionary;
using AnkiBridge.Application.Abstractions.Images;
using AnkiBridge.Application.Abstractions.Speech;
using AnkiBridge.Application.Abstractions.Translation;
using AnkiBridge.Application.Common.Contracts.Outbox;
using AnkiBridge.Application.Common.Contracts.Storage;
using AnkiBridge.Application.Features.Dictionary.Contracts.QueryServices;
using AnkiBridge.Application.Features.Flashcard.Contracts.Anki;
using AnkiBridge.Application.Features.Flashcard.Contracts.QueryServices;
using AnkiBridge.Application.Features.Learning.Contracts.QueryServices;
using AnkiBridge.Domain.Aggregates.Dictionary;
using AnkiBridge.Domain.Aggregates.Flashcard.Decks;
using AnkiBridge.Domain.Aggregates.Flashcard.Notes;
using AnkiBridge.Domain.Aggregates.Flashcard.NoteTypes;
using AnkiBridge.Domain.Aggregates.Learning;
using AnkiBridge.Infrastructure.ExternalServices.AnkiConnect;
using AnkiBridge.Infrastructure.ExternalServices.Dictionary.Cambridge;
using AnkiBridge.Infrastructure.ExternalServices.Images;
using AnkiBridge.Infrastructure.ExternalServices.Images.Pexels;
using AnkiBridge.Infrastructure.ExternalServices.Images.Pixabay;
using AnkiBridge.Infrastructure.ExternalServices.Speech.Google;
using AnkiBridge.Infrastructure.ExternalServices.Storage;
using AnkiBridge.Infrastructure.ExternalServices.Storage.Azure;
using AnkiBridge.Infrastructure.ExternalServices.Storage.AzureBlob;
using AnkiBridge.Infrastructure.ExternalServices.Translation;
using AnkiBridge.Infrastructure.ExternalServices.Translation.Google;
using AnkiBridge.Infrastructure.Outbox;
using AnkiBridge.Infrastructure.Persistence.Abstractions;
using AnkiBridge.Infrastructure.Persistence.DatabaseContext;
using AnkiBridge.Infrastructure.Persistence.Interceptors;
using AnkiBridge.Infrastructure.Persistence.QueryServices;
using AnkiBridge.Infrastructure.Persistence.Repositories;
using AnkiBridge.Infrastructure.Persistence.Seeding.Seeders;
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
        builder.AddPersistenceServices();
        builder.AddExternalServices();

        return builder;
    }

    public static IHostApplicationBuilder AddPersistenceServices(this IHostApplicationBuilder builder, bool dispatchDomainEvents = true)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // Add database context
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(configuration.GetConnectionString("ankibridgedb"));
        });
        builder.EnrichNpgsqlDbContext<ApplicationDbContext>();

        // Add interceptors
        if (dispatchDomainEvents)
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

        return builder;
    }

    private static IHostApplicationBuilder AddExternalServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // Add storage
        services.Configure<AzureBlobStorageOptions>(configuration.GetSection(AzureBlobStorageOptions.SectionName));
        builder.AddAzureBlobServiceClient("blobs");
        services.AddScoped<IFileStorage, AzureBlobStorage>();
        services.AddScoped<IRemoteMediaSource, HttpRemoteMediaSource>();
        services.AddHttpClient("media-download", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AnkiBridge/1.0");
        });

        // Add anki service
        services.Configure<AnkiConnectOptions>(configuration.GetSection(AnkiConnectOptions.SectionName));
        services.AddScoped<IAnkiService, AnkiConnectService>();
        services.AddHttpClient<IAnkiConnectClient, AnkiConnectClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AnkiConnectOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        // Add dictionary services
        services.Configure<CambridgeDictionaryOptions>(configuration.GetSection(CambridgeDictionaryOptions.SectionName));
        services.AddScoped<IDictionaryEntryProvider, CambridgeDictionaryEntryProvider>();
        services.AddScoped<IIpaProvider, CambridgeDictionaryIpaProvider>();

        // Add translation services
        services.AddScoped<CambridgeDictionaryTranslationProvider>();
        services.Configure<GoogleTranslationOptions>(configuration.GetSection(GoogleTranslationOptions.SectionName));
        services.AddHttpClient<GoogleTranslationProvider>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<GoogleTranslationOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddScoped<ITranslationProvider, FallbackTranslationProvider>();

        // Add speech services
        services.Configure<GoogleSpeechOptions>(configuration.GetSection(GoogleSpeechOptions.SectionName));
        services.AddSingleton<IAudioProvider, GoogleSpeechProvider>();

        // Add image services
        services.Configure<PixabayOptions>(configuration.GetSection(PixabayOptions.SectionName));
        services.AddHttpClient<PixabayImageProvider>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PixabayOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.Configure<PexelsOptions>(configuration.GetSection(PexelsOptions.SectionName));
        services.AddHttpClient<PexelsImageProvider>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PexelsOptions>>().Value;
            client.DefaultRequestHeaders.Add("Authorization", options.ApiKey);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddScoped<IImageProvider, FallbackImageProvider>();

        return builder;
    }
}
