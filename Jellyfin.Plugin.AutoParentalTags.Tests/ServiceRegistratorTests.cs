using System.Linq;
using Jellyfin.Plugin.AutoParentalTags.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.AutoParentalTags.Tests;

/// <summary>
/// Tests for the ServiceRegistrator class.
/// </summary>
public class ServiceRegistratorTests
{
    /// <summary>
    /// Tests that ServiceRegistrator can be instantiated.
    /// </summary>
    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Act
        var registrator = new ServiceRegistrator();

        // Assert
        Assert.NotNull(registrator);
    }

    /// <summary>
    /// Tests that RegisterServices registers AiServiceFactory.
    /// </summary>
    [Fact]
    public void RegisterServices_ShouldRegisterAiServiceFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        var registrator = new ServiceRegistrator();
        var mockAppHost = new Mock<IServerApplicationHost>();

        // Act
        registrator.RegisterServices(services, mockAppHost.Object);

        // Assert
        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<AiServiceFactory>();
        Assert.NotNull(factory);
    }

    /// <summary>
    /// Tests that RegisterServices registers LibraryMonitor.
    /// </summary>
    [Fact]
    public void RegisterServices_ShouldRegisterLibraryMonitor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(Mock.Of<ILibraryManager>());
        var registrator = new ServiceRegistrator();
        var mockAppHost = new Mock<IServerApplicationHost>();

        // Act
        registrator.RegisterServices(services, mockAppHost.Object);

        // Assert
        var provider = services.BuildServiceProvider();
        var monitor = provider.GetService<LibraryMonitor>();
        Assert.NotNull(monitor);
    }

    /// <summary>
    /// Tests that RegisterServices registers both services as singletons.
    /// </summary>
    [Fact]
    public void RegisterServices_ShouldRegisterServicesAsSingletons()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(Mock.Of<ILibraryManager>());
        var registrator = new ServiceRegistrator();
        var mockAppHost = new Mock<IServerApplicationHost>();

        // Act
        registrator.RegisterServices(services, mockAppHost.Object);

        // Assert
        var factoryDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(AiServiceFactory));
        var monitorDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(LibraryMonitor));

        Assert.NotNull(factoryDescriptor);
        Assert.NotNull(monitorDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, factoryDescriptor.Lifetime);
        Assert.Equal(ServiceLifetime.Singleton, monitorDescriptor.Lifetime);
    }
}
