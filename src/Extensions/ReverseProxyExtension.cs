using ApiGateway.Configurations;
using ApiGateway.Constants;
using Yarp.ReverseProxy.Configuration;

namespace ApiGateway.Extensions;

public static class ReverseProxyExtension
{
    private const string UserServiceCluster = "userServiceCluster";
    private const string AuthCluster = "authCluster";
    private const string PaymentServiceCluster = "paymentServiceCluster";
    private const string ProductServiceCluster = "productServiceCluster";
    private const string OrderServiceCluster = "orderServiceCluster";

    public static IServiceCollection AddGatewayReverseProxy(this IServiceCollection services, IConfiguration configuration)
    {
        var servicesConfig = configuration.GetSection("Services").Get<ServicesConfiguration>()
            ?? throw new InvalidOperationException("Services configuration is missing");

        servicesConfig.UserService.Validate("UserService");
        servicesConfig.AuthLambda.Validate("AuthLambda");
        servicesConfig.PaymentService.Validate("PaymentService");
        servicesConfig.ProductService.Validate("ProductService");
        servicesConfig.OrderService.Validate("OrderService");

        var routes = BuildRoutes();
        var clusters = BuildClusters(servicesConfig);

        services.AddReverseProxy()
            .LoadFromMemory(routes, clusters);

        return services;
    }

    private static RouteConfig[] BuildRoutes()
    {
        var routes = new List<RouteConfig>();

        // Lambda - Autenticação
        routes.Add(CreateRoute("auth-login", AuthCluster, "/api/auth/login", new[] { "POST", "OPTIONS" }));

        // USUÁRIOS
        routes.Add(CreateRoute("users-create", UserServiceCluster, "/api/users", new[] { "POST" }));
        routes.Add(CreateRoute("users-list", UserServiceCluster, "/api/users", new[] { "GET" }, Policies.ADMIN_OR_MANAGER));
        routes.Add(CreateRoute("users-get-by-id", UserServiceCluster, "/api/users/{id}", new[] { "GET" }, Policies.ADMIN_OR_MANAGER));
        routes.Add(CreateRoute("users-by-email", UserServiceCluster, "/api/users/email/{email}", new[] { "GET" }, Policies.DATA_QUERY));
        routes.Add(CreateRoute("users-by-cpf", UserServiceCluster, "/api/users/cpf/{cpf}", new[] { "GET" }, Policies.DATA_QUERY));
        routes.Add(CreateRoute("users-update", UserServiceCluster, "/api/users/{id}", new[] { "PUT" }, Policies.ADMIN_OR_MANAGER));
        routes.Add(CreateRoute("users-delete", UserServiceCluster, "/api/users/{id}", new[] { "DELETE" }, Policies.ADMIN_OR_MANAGER));

        // PAGAMENTO
        routes.Add(CreateRoute("payment-create", PaymentServiceCluster, "/api/payment/create", new[] { "POST" }));
        routes.Add(CreateRoute("payment-qr-image", PaymentServiceCluster, "/api/payment/qr-image/{**catch-all}", new[] { "GET" }));
        routes.Add(CreateRoute("payment-webhook-mercadopago", PaymentServiceCluster, "/api/webhooks/mercadopago/{**catch-all}", new[] { "POST" }));

        // PRODUTOS
        routes.AddRange(CreateCrudRoutes("products", ProductServiceCluster, "/api/products", publicRead: true));

        // CATEGORIAS
        routes.AddRange(CreateCrudRoutes("categories", ProductServiceCluster, "/api/categories", publicRead: true));

        // ITEMS
        routes.AddRange(CreateCrudRoutes("items", ProductServiceCluster, "/api/items", publicRead: true));

        // ORDERS
        routes.Add(CreateRoute("orders-list", OrderServiceCluster, "/api/orders", new[] { "GET" }));
        routes.Add(CreateRoute("orders-get-by-id", OrderServiceCluster, "/api/orders/{id}", new[] { "GET" }));
        routes.Add(CreateRoute("orders-create", OrderServiceCluster, "/api/orders", new[] { "POST" }));
        routes.Add(CreateRoute("orders-update", OrderServiceCluster, "/api/orders/{id}", new[] { "PUT" }, Policies.ADMIN_OR_MANAGER));
        routes.Add(CreateRoute("orders-delete", OrderServiceCluster, "/api/orders/{id}", new[] { "DELETE" }, Policies.ADMIN_OR_MANAGER));
        routes.Add(CreateRoute("orders-ready", OrderServiceCluster, "/api/orders/{id}/ready", new[] { "GET" }));
        routes.Add(CreateRoute("orders-confirm-payment", OrderServiceCluster, "/api/orders/{id}/confirm-payment", new[] { "GET" }));

        return routes.ToArray();
    }

    private static RouteConfig CreateRoute(
        string routeId, 
        string clusterId, 
        string path, 
        string[] methods, 
        string? authPolicy = null)
    {
        return new RouteConfig
        {
            RouteId = routeId,
            ClusterId = clusterId,
            Match = new RouteMatch { Path = path, Methods = methods },
            AuthorizationPolicy = authPolicy
        };
    }

    private static IEnumerable<RouteConfig> CreateCrudRoutes(
        string resourceName, 
        string clusterId, 
        string basePath, 
        bool publicRead = false)
    {
        var routes = new List<RouteConfig>
        {
            CreateRoute($"{resourceName}-list", clusterId, basePath, new[] { "GET" }, 
                publicRead ? null : Policies.DATA_QUERY),
            CreateRoute($"{resourceName}-get-by-id", clusterId, $"{basePath}/{{id}}", new[] { "GET" }, 
                publicRead ? null : Policies.DATA_QUERY),
            CreateRoute($"{resourceName}-by-category", clusterId, $"{basePath}/category/{{category}}", new[] { "GET" }),
            CreateRoute($"{resourceName}-create", clusterId, basePath, new[] { "POST" }, Policies.ADMIN_OR_MANAGER),
            CreateRoute($"{resourceName}-update", clusterId, $"{basePath}/{{id}}", new[] { "PUT" }, Policies.ADMIN_OR_MANAGER),
            CreateRoute($"{resourceName}-delete", clusterId, $"{basePath}/{{id}}", new[] { "DELETE" }, Policies.ADMIN_OR_MANAGER)
        };

        // Remove rota by-category se for categories ou items
        if (resourceName == "categories" || resourceName == "items")
        {
            routes.RemoveAll(r => r.RouteId.EndsWith("-by-category"));
        }

        return routes;
    }

    private static ClusterConfig[] BuildClusters(ServicesConfiguration config)
    {
        return new[]
        {
            CreateCluster(AuthCluster, "authDestination", config.AuthLambda.Url),
            CreateCluster(UserServiceCluster, "userServiceDestination", config.UserService.Url),
            CreateCluster(PaymentServiceCluster, "paymentServiceDestination", config.PaymentService.Url),
            CreateCluster(ProductServiceCluster, "productServiceDestination", config.ProductService.Url),
            CreateCluster(OrderServiceCluster, "orderServiceDestination", config.OrderService.Url)
        };
    }

    private static ClusterConfig CreateCluster(string clusterId, string destinationName, string address)
    {
        return new ClusterConfig
        {
            ClusterId = clusterId,
            Destinations = new Dictionary<string, DestinationConfig>
            {
                { destinationName, new DestinationConfig { Address = address } }
            }
        };
    }
}