using Jellyfin.Plugin.AutoParentalTags.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoParentalTags;

/// <summary>
/// Service registrator for dependency injection.
/// </summary>
public class ServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Try to get logger factory, fallback to null logger if not available (e.g., in tests)
        var loggerFactory = applicationHost.Resolve<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger<ServiceRegistrator>();

        logger?.LogInformation("Registering Auto Parental Tags services");

        serviceCollection.AddSingleton<AiServiceFactory>();
        serviceCollection.AddSingleton<LibraryMonitor>();
        serviceCollection.AddSingleton<AutoParentalTagsScheduledTask>();

        logger?.LogDebug("Services registered: AiServiceFactory, LibraryMonitor, AutoParentalTagsScheduledTask");
    }
}
