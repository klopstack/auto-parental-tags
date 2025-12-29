using Jellyfin.Plugin.AutoParentalTags.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.AutoParentalTags;

/// <summary>
/// Service registrator for dependency injection.
/// </summary>
public class ServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<AiServiceFactory>();
        serviceCollection.AddSingleton<LibraryMonitor>();
        serviceCollection.AddSingleton<AutoParentalTagsScheduledTask>();
    }
}
