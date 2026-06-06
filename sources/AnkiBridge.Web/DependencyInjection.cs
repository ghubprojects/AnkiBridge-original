using AnkiBridge.Web.Common.Dispatching;

namespace AnkiBridge.Web;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddWebServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        // Register the request dispatcher
        services.AddScoped<IRequestDispatcher, RequestDispatcher>();

        services.AddHttpClient(); // cần cho IHttpClientFactory

        return builder;
    }
}
