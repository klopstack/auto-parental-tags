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
        // Register services without resolving ILoggerFactory during registration phase
        // Logging will occur in the Plugin constructor instead
        serviceCollection.AddSingleton<AiServiceFactory>();
        serviceCollection.AddSingleton<LibraryMonitor>();
        serviceCollection.AddSingleton<AutoParentalTagsScheduledTask>();
    }
}
