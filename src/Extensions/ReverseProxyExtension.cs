using ApiGateway.Configurations;
using ApiGateway.Constants;
using Yarp.ReverseProxy.Configuration;

namespace ApiGateway.Extensions;

public static class ReverseProxyExtension
{
    private const string UserServiceCluster = "userServiceCluster";
    private const string AuthCluster = "authCluster";

    public static IServiceCollection AddGatewayReverseProxy(this IServiceCollection services, IConfiguration configuration)
    {
        var servicesConfig = configuration.GetSection("Services").Get<ServicesConfiguration>()
            ?? throw new InvalidOperationException("Services configuration is missing");

        servicesConfig.UserService.Validate("UserService");
        servicesConfig.AuthLambda.Validate("AuthLambda");

        var routes = BuildRoutes();
        var clusters = BuildClusters(servicesConfig);

        services.AddReverseProxy()
            .LoadFromMemory(routes, clusters);

        return services;
    }

    private static RouteConfig[] BuildRoutes()
    {
        return new[]
        {
            // Lambda - Autenticação (público - sem autorização)
            new RouteConfig
            {
                RouteId = "auth-login",
                ClusterId = AuthCluster,
                Match = new RouteMatch { Path = "/api/auth/login", Methods = new[] { "POST", "OPTIONS" } }
            },

            // USUÁRIOS - Create (público - AllowAnonymous)
            new RouteConfig
            {
                RouteId = "users-create",
                ClusterId = UserServiceCluster,
                Match = new RouteMatch { Path = "/api/users", Methods = new[] { "POST" } }
            },

            // USUÁRIOS - GetAll (requer ADMIN_OR_MANAGER)
            new RouteConfig
            {
                RouteId = "users-list",
                ClusterId = UserServiceCluster,
                Match = new RouteMatch { Path = "/api/users", Methods = new[] { "GET" } },
                AuthorizationPolicy = Policies.ADMIN_OR_MANAGER
            },

            // USUÁRIOS - GetById (requer ADMIN_OR_MANAGER)
            new RouteConfig
            {
                RouteId = "users-get-by-id",
                ClusterId = UserServiceCluster,
                Match = new RouteMatch { Path = "/api/users/{id}", Methods = new[] { "GET" } },
                AuthorizationPolicy = Policies.ADMIN_OR_MANAGER
            },

            // USUÁRIOS - GetByEmail (requer DATA_QUERY)
            new RouteConfig
            {
                RouteId = "users-by-email",
                ClusterId = UserServiceCluster,
                Match = new RouteMatch { Path = "/api/users/email/{email}", Methods = new[] { "GET" } },
                AuthorizationPolicy = Policies.DATA_QUERY
            },

            // USUÁRIOS - GetByCpf (requer DATA_QUERY)
            new RouteConfig
            {
                RouteId = "users-by-cpf",
                ClusterId = UserServiceCluster,
                Match = new RouteMatch { Path = "/api/users/cpf/{cpf}", Methods = new[] { "GET" } },
                AuthorizationPolicy = Policies.DATA_QUERY
            },

            // USUÁRIOS - Update (requer ADMIN_OR_MANAGER)
            new RouteConfig
            {
                RouteId = "users-update",
                ClusterId = UserServiceCluster,
                Match = new RouteMatch { Path = "/api/users/{id}", Methods = new[] { "PUT" } },
                AuthorizationPolicy = Policies.ADMIN_OR_MANAGER
            },

            // USUÁRIOS - Delete (requer ADMIN_OR_MANAGER)
            new RouteConfig
            {
                RouteId = "users-delete",
                ClusterId = UserServiceCluster,
                Match = new RouteMatch { Path = "/api/users/{id}", Methods = new[] { "DELETE" } },
                AuthorizationPolicy = Policies.ADMIN_OR_MANAGER
            }
        };
    }

    private static ClusterConfig[] BuildClusters(ServicesConfiguration config)
    {
        return new[]
        {
            new ClusterConfig
            {
                ClusterId = AuthCluster,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "authDestination", new DestinationConfig { Address = config.AuthLambda.Url } }
                }
            },
            new ClusterConfig
            {
                ClusterId = UserServiceCluster,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "userServiceDestination", new DestinationConfig { Address = config.UserService.Url } }
                }
            }
        };
    }
}