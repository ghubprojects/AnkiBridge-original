using AnkiBridge.Application.Behaviors;
using AnkiBridge.Application.Common.IntegrationEvents;
using AnkiBridge.Application.ExceptionHandlers;
using AnkiBridge.Application.Features.Flashcard.IntegrationEvents;
using AnkiBridge.Application.Features.Learning.IntegrationEvents;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace AnkiBridge.Application;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddApplicationServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR services and behaviors
        services.AddTransient(typeof(IRequestExceptionHandler<,,>), typeof(ValidationExceptionHandler<,,>));
        services.AddTransient(typeof(IRequestExceptionHandler<,,>), typeof(GlobalExceptionHandler<,,>));
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Register integration event handlers
        services.AddSubscription<NoteExportStartedIntegrationEvent, NoteExportStartedIntegrationEventHandler>();
        services.AddSubscription<NotesExportStartedIntegrationEvent, NotesExportStartedIntegrationEventHandler>();
        services.AddSubscription<LearningEntryMediaUploadRequestedIntegrationEvent,
            LearningEntryMediaUploadRequestedIntegrationEventHandler>();

        return builder;
    }
}
