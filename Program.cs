using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Configuration;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// Variáveis de ambiente
var userServiceUrl = Environment.GetEnvironmentVariable("USER_SERVICE_URL") ?? "http://localhost:8080";
var authLambdaUrl = Environment.GetEnvironmentVariable("AUTH_LAMBDA_URL") ?? "https://your-lambda.amazonaws.com/prod";
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "rF8wzYp2L#eX1v9sKd3@qMTuN6JBgCmySecretKey123456";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "ManaFoodIssuer";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "ManaFoodAudience";

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"AUTH FAILED: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                if (context.Principal?.Identity is ClaimsIdentity identity)
                {
                    var claimsToAdd = new List<Claim>();
                    var claimsToRemove = new List<Claim>();

                    var roleClaim = identity.FindFirst(c => 
                        c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" ||
                        c.Type == ClaimTypes.Role);
                    if (roleClaim != null)
                    {
                        claimsToRemove.Add(roleClaim);
                        claimsToAdd.Add(new Claim("role", roleClaim.Value));
                    }

                    var emailClaim = identity.FindFirst(c => 
                        c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress" ||
                        c.Type == ClaimTypes.Email);
                    if (emailClaim != null)
                    {
                        claimsToRemove.Add(emailClaim);
                        claimsToAdd.Add(new Claim("email", emailClaim.Value));
                    }

                    var subClaim = identity.FindFirst(c => 
                        c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" ||
                        c.Type == ClaimTypes.NameIdentifier);
                    if (subClaim != null)
                    {
                        claimsToRemove.Add(subClaim);
                        claimsToAdd.Add(new Claim("sub", subClaim.Value));
                    }

                    // Remover claims antigos e adicionar novos
                    foreach (var claim in claimsToRemove)
                    {
                        identity.RemoveClaim(claim);
                    }
                    foreach (var claim in claimsToAdd)
                    {
                        identity.AddClaim(claim);
                    }
                }

                var claims = context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                return Task.CompletedTask;
            }
        };
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role",
            NameClaimType = "name"
        };
    });

builder.Services.AddAuthorization();

var routes = new[]
{
    // Lambda - Autenticação (público)
    new RouteConfig
    {
        RouteId = "auth-login",
        ClusterId = "authCluster",
        Match = new RouteMatch { Path = "/api/auth/login", Methods = new[] { "POST", "OPTIONS" } }
    },
    
    // USUÁRIOS 
    new RouteConfig
    {
        RouteId = "users-by-email",
        ClusterId = "userServiceCluster",
        Match = new RouteMatch { Path = "/api/users/email/{email}", Methods = new[] { "GET" } }
    },
    new RouteConfig
    {
        RouteId = "users-by-cpf",
        ClusterId = "userServiceCluster",
        Match = new RouteMatch { Path = "/api/users/cpf/{cpf}", Methods = new[] { "GET" } }
    },
    new RouteConfig
    {
        RouteId = "users-create",
        ClusterId = "userServiceCluster",
        Match = new RouteMatch { Path = "/api/users", Methods = new[] { "POST" } }
    },
    new RouteConfig
    {
        RouteId = "users-list",
        ClusterId = "userServiceCluster",
        Match = new RouteMatch { Path = "/api/users", Methods = new[] { "GET" } }
    },
    new RouteConfig
    {
        RouteId = "users-get-by-id",
        ClusterId = "userServiceCluster",
        Match = new RouteMatch { Path = "/api/users/{id}", Methods = new[] { "GET" } }
    },
    new RouteConfig
    {
        RouteId = "users-update",
        ClusterId = "userServiceCluster",
        Match = new RouteMatch { Path = "/api/users/{id}", Methods = new[] { "PUT" } }
    },
    new RouteConfig
    {
        RouteId = "users-delete",
        ClusterId = "userServiceCluster",
        Match = new RouteMatch { Path = "/api/users/{id}", Methods = new[] { "DELETE" } }
    }
};

// Clusters
var clusters = new[]
{
    new ClusterConfig
    {
        ClusterId = "authCluster",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            { "authDestination", new DestinationConfig { Address = authLambdaUrl } }
        }
    },
    new ClusterConfig
    {
        ClusterId = "userServiceCluster",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            { "userServiceDestination", new DestinationConfig { Address = userServiceUrl } }
        }
    }
};

builder.Services
    .AddReverseProxy()
    .LoadFromMemory(routes, clusters);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    await next();
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));

app.MapReverseProxy();

app.Run();