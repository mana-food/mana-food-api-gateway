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
            },

            // PAGAMENTO - Create Payment (público - sem autenticação)
            new RouteConfig
            {
                RouteId = "payment-create",
                ClusterId = PaymentServiceCluster,
                Match = new RouteMatch { Path = "/api/payment/create", Methods = new[] { "POST" } }
            },

            // PAGAMENTO - Get QR Code Image (público - sem autenticação)
            new RouteConfig
            {
                RouteId = "payment-qr-image",
                ClusterId = PaymentServiceCluster,
                Match = new RouteMatch { Path = "/api/payment/qr-image/{**catch-all}", Methods = new[] { "GET" } }
            },

            // PAGAMENTO - Webhook MercadoPago (público - sem autenticação)
            new RouteConfig
            {
                RouteId = "payment-webhook-mercadopago",
                ClusterId = PaymentServiceCluster,
                Match = new RouteMatch { Path = "/api/webhooks/mercadopago/{**catch-all}", Methods = new[] { "POST" } }
            },

            // PRODUTOS - GetAll (público)
            new RouteConfig
            {
                RouteId = "products-list",
                ClusterId = ProductServiceCluster,
                Match = new RouteMatch { Path = "/api/products", Methods = new[] { "GET" } }
            },

            // PRODUTOS - GetById (público)
            new RouteConfig
            {
                RouteId = "products-get-by-id",
                ClusterId = ProductServiceCluster,
                Match = new RouteMatch { Path = "/api/products/{id}", Methods = new[] { "GET" } }
            },

            // PRODUTOS - GetByCategory (público)
            new RouteConfig
            {
                RouteId = "products-by-category",
                ClusterId = ProductServiceCluster,
                Match = new RouteMatch { Path = "/api/products/category/{category}", Methods = new[] { "GET" } }
            },

            // PRODUTOS - Create (requer ADMIN_OR_MANAGER)
            new RouteConfig
            {
                RouteId = "products-create",
                ClusterId = ProductServiceCluster,
                Match = new RouteMatch { Path = "/api/products", Methods = new[] { "POST" } },
                AuthorizationPolicy = Policies.ADMIN_OR_MANAGER
            },

            // PRODUTOS - Update (requer ADMIN_OR_MANAGER)
            new RouteConfig
            {
                RouteId = "products-update",
                ClusterId = ProductServiceCluster,
                Match = new RouteMatch { Path = "/api/products/{id}", Methods = new[] { "PUT" } },
                AuthorizationPolicy = Policies.ADMIN_OR_MANAGER
            },

            // PRODUTOS - Delete (requer ADMIN_OR_MANAGER)
            new RouteConfig
            {
                RouteId = "products-delete",
                ClusterId = ProductServiceCluster,
                Match = new RouteMatch { Path = "/api/products/{id}", Methods = new[] { "DELETE" } },
                AuthorizationPolicy = Policies.ADMIN_OR_MANAGER
            },

            // CATEGORIES - GetAll (público)
            new RouteConfig
            {
                RouteId = "categories-list",
                ClusterId = ProductServiceCluster,
                Match = new RouteMatch { Path = "/api/categories", Methods = new[] { "GET" } }
            },

            // CATEGORIES - GetById (público)
            new RouteConfig
            {
                RouteId = "categories-get-by-id",
                ClusterId = ProductServiceCluster,
                Match = new RouteMatch { Path = "/api/categories/{id}", Methods = new[] { "GET" } }
            },

            // CATEGORIES - Create (requer ADMIN_OR_MANAGER)
            new RouteConfig
            {
                RouteId = "categories-create",
                ClusterId = ProductServiceCluster,
                Match = new RouteMatch { Path = "/api/categories", Methods = new[] { "POST" } },
                AuthorizationPolicy = Policies.ADMIN_OR_MANAGER
            },

            // CATEGORIES - Update (requer ADMIN_OR_MANAGER)
            new RouteConfig
            {
                RouteId = "categories-update",
                ClusterId = ProductServiceCluster,
                Match = new RouteMatch { Path = "/api/categories/{id}", Methods = new[] { "PUT" } },
                AuthorizationPolicy = Policies.ADMIN_OR_MANAGER
            },

            // CATEGORIES - Delete (requer ADMIN_OR_MANAGER)
            new RouteConfig
            {
                RouteId = "categories-delete",
                ClusterId = ProductServiceCluster,
                Match = new RouteMatch { Path = "/api/categories/{id}", Methods = new[] { "DELETE" } },
                AuthorizationPolicy = Policies.ADMIN_OR_MANAGER
            },

            // ITEMS - GetAll (público)
            new RouteConfig
            {
                RouteId = "items-list",
                ClusterId = ProductServiceCluster,
                Match = new RouteMatch { Path = "/api/items", Methods = new[] { "GET" } }
            },

            // ITEMS - GetById (público)
            new RouteConfig
            {
                RouteId = "items-get-by-id",
                ClusterId = ProductServiceCluster,
                Match = new RouteMatch { Path = "/api/items/{id}", Methods = new[] { "GET" } }
            },

            // ITEMS - Create (requer ADMIN_OR_MANAGER)
            new RouteConfig
            {
                RouteId = "items-create",
                ClusterId = ProductServiceCluster,
                Match = new RouteMatch { Path = "/api/items", Methods = new[] { "POST" } },
                AuthorizationPolicy = Policies.ADMIN_OR_MANAGER
            },

            // ITEMS - Update (requer ADMIN_OR_MANAGER)
            new RouteConfig
            {
                RouteId = "items-update",
                ClusterId = ProductServiceCluster,
                Match = new RouteMatch { Path = "/api/items/{id}", Methods = new[] { "PUT" } },
                AuthorizationPolicy = Policies.ADMIN_OR_MANAGER
            },

            // ITEMS - Delete (requer ADMIN_OR_MANAGER)
            new RouteConfig
            {
                RouteId = "items-delete",
                ClusterId = ProductServiceCluster,
                Match = new RouteMatch { Path = "/api/items/{id}", Methods = new[] { "DELETE" } },
                AuthorizationPolicy = Policies.ADMIN_OR_MANAGER
            },

            // ORDERS - GetAll  
            new RouteConfig
            {
                RouteId = "orders-list",
                ClusterId = OrderServiceCluster,
                Match = new RouteMatch { Path = "/api/orders", Methods = new[] { "GET" } },
            },

            // ORDERS - GetById 
            new RouteConfig
            {
                RouteId = "orders-get-by-id",
                ClusterId = OrderServiceCluster,
                Match = new RouteMatch { Path = "/api/orders/{id}", Methods = new[] { "GET" } },
            },

            // ORDERS - Create público
            new RouteConfig
            {
                RouteId = "orders-create",
                ClusterId = OrderServiceCluster,
                Match = new RouteMatch { Path = "/api/orders", Methods = new[] { "POST" } }
            },

            // ORDERS - Update (requer ADMIN_OR_MANAGER)
            new RouteConfig
            {
                RouteId = "orders-update",
                ClusterId = OrderServiceCluster,
                Match = new RouteMatch { Path = "/api/orders/{id}", Methods = new[] { "PUT" } },
                AuthorizationPolicy = Policies.ADMIN_OR_MANAGER
            },

            // ORDERS - Delete (requer ADMIN_OR_MANAGER)
            new RouteConfig
            {
                RouteId = "orders-delete",
                ClusterId = OrderServiceCluster,
                Match = new RouteMatch { Path = "/api/orders/{id}", Methods = new[] { "DELETE" } },
                AuthorizationPolicy = Policies.ADMIN_OR_MANAGER
            },

            // ORDERS - Ready Order
            new RouteConfig
            {
                RouteId = "orders-ready",
                ClusterId = OrderServiceCluster,
                Match = new RouteMatch { Path = "/api/orders/{id}/ready", Methods = new[] { "GET" } },
            },

            // ORDERS - Confirm Payment 
            new RouteConfig
            {
                RouteId = "orders-confirm-payment",
                ClusterId = OrderServiceCluster,
                Match = new RouteMatch { Path = "/api/orders/{id}/confirm-payment", Methods = new[] { "GET" } },
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
            },
            new ClusterConfig
            {
                ClusterId = PaymentServiceCluster,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "paymentServiceDestination", new DestinationConfig { Address = config.PaymentService.Url } }
                }
            },

            new ClusterConfig
            {
                ClusterId = ProductServiceCluster,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "productServiceDestination", new DestinationConfig { Address = config.ProductService.Url } }
                }
            },
            new ClusterConfig
            {
                ClusterId = OrderServiceCluster,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "orderServiceDestination", new DestinationConfig { Address = config.OrderService.Url } }
                }
            }
        };
    }
}