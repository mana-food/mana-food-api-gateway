using ApiGateway.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.Tests.Extensions;

public class ReverseProxyExtensionTests
{
    [Fact]
    public void AddGatewayReverseProxy_WithValidConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        // Act
        services.AddGatewayReverseProxy(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var proxyConfigProvider = serviceProvider.GetService<IProxyConfigProvider>();
        proxyConfigProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddGatewayReverseProxy_WithMissingConfiguration_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var act = () => services.AddGatewayReverseProxy(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Services configuration is missing");
    }

    [Fact]
    public void AddGatewayReverseProxy_WithInvalidUserServiceUrl_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string>
        {
            { "Services:UserService:Url", "invalid-url" },
            { "Services:AuthLambda:Url", "https://lambda.aws.com" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        var act = () => services.AddGatewayReverseProxy(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("UserService URL é inválida: invalid-url");
    }

    [Fact]
    public void AddGatewayReverseProxy_WithInvalidAuthLambdaUrl_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string>
        {
            { "Services:UserService:Url", "http://localhost:8080" },
            { "Services:AuthLambda:Url", "not-a-url" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        var act = () => services.AddGatewayReverseProxy(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("AuthLambda URL é inválida: not-a-url");
    }

    [Fact]
    public void AddGatewayReverseProxy_WithEmptyUserServiceUrl_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string>
        {
            { "Services:UserService:Url", "" },
            { "Services:AuthLambda:Url", "https://lambda.aws.com" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        var act = () => services.AddGatewayReverseProxy(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("UserService URL é obrigatória");
    }

    [Fact]
    public void AddGatewayReverseProxy_WithEmptyAuthLambdaUrl_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string>
        {
            { "Services:UserService:Url", "http://localhost:8080" },
            { "Services:AuthLambda:Url", "" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        var act = () => services.AddGatewayReverseProxy(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("AuthLambda URL é obrigatória");
    }

    [Fact]
    public void AddGatewayReverseProxy_ShouldConfigureRoutes()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        // Act
        services.AddGatewayReverseProxy(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var proxyConfigProvider = serviceProvider.GetRequiredService<IProxyConfigProvider>();
        var proxyConfig = proxyConfigProvider.GetConfig();

        proxyConfig.Routes.Should().NotBeEmpty();
        proxyConfig.Routes.Should().HaveCountGreaterOrEqualTo(8);
    }

    [Fact]
    public void AddGatewayReverseProxy_ShouldConfigureClusters()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        // Act
        services.AddGatewayReverseProxy(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var proxyConfigProvider = serviceProvider.GetRequiredService<IProxyConfigProvider>();
        var proxyConfig = proxyConfigProvider.GetConfig();

        proxyConfig.Clusters.Should().NotBeEmpty();
        proxyConfig.Clusters.Should().HaveCount(3);
    }

    [Fact]
    public void AddGatewayReverseProxy_ShouldConfigureAuthCluster()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        // Act
        services.AddGatewayReverseProxy(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var proxyConfigProvider = serviceProvider.GetRequiredService<IProxyConfigProvider>();
        var proxyConfig = proxyConfigProvider.GetConfig();

        var authCluster = proxyConfig.Clusters.FirstOrDefault(c => c.ClusterId == "authCluster");
        authCluster.Should().NotBeNull();
        authCluster!.Destinations.Should().ContainKey("authDestination");
    }

    [Fact]
    public void AddGatewayReverseProxy_ShouldConfigureUserServiceCluster()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        // Act
        services.AddGatewayReverseProxy(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var proxyConfigProvider = serviceProvider.GetRequiredService<IProxyConfigProvider>();
        var proxyConfig = proxyConfigProvider.GetConfig();

        var userCluster = proxyConfig.Clusters.FirstOrDefault(c => c.ClusterId == "userServiceCluster");
        userCluster.Should().NotBeNull();
        userCluster!.Destinations.Should().ContainKey("userServiceDestination");
    }

    private static IConfiguration CreateConfiguration()
    {
        var configData = new Dictionary<string, string>
        {
            { "Services:UserService:Url", "http://localhost:8080" },
            { "Services:AuthLambda:Url", "https://lambda.aws.com" },
            { "Services:PaymentService:Url", "http://localhost:9090" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }
}