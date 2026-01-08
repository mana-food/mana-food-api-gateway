using System.IdentityModel.Tokens.Jwt;
using System.Text;
using ApiGateway.Configurations;
using ApiGateway.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.Extensions;

public static class AuthExtension
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        // Bind e valida configurações JWT
        var jwtConfig = configuration.GetSection("Jwt").Get<JwtConfiguration>()
            ?? throw new InvalidOperationException("JWT configuration is missing");

        jwtConfig.Validate();

        services.AddAuthentication("Bearer")
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
                        context.TransformClaims();
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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtConfig.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtConfig.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = "role",
                    NameClaimType = "name"
                };
            });

        return services;
    }

    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.ADMIN_ONLY, policy =>
                policy.RequireRole(Roles.ADMIN));

            options.AddPolicy(Policies.ADMIN_OR_MANAGER, policy =>
                policy.RequireRole(Roles.ADMIN, Roles.MANAGER));

            options.AddPolicy(Policies.KITCHEN_STAFF, policy =>
                policy.RequireRole(Roles.KITCHEN, Roles.ADMIN, Roles.MANAGER));

            options.AddPolicy(Policies.OPERATORS, policy =>
                policy.RequireRole(Roles.OPERATOR, Roles.ADMIN, Roles.MANAGER));

            options.AddPolicy(Policies.MANAGEMENT, policy =>
                policy.RequireRole(Roles.ADMIN, Roles.MANAGER));

            options.AddPolicy(Policies.AUTHENTICATED_USER, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(Policies.ORDER_MANAGEMENT, policy =>
                policy.RequireRole(Roles.ADMIN, Roles.KITCHEN));

            options.AddPolicy(Policies.DATA_QUERY, policy =>
                policy.RequireRole(Roles.ADMIN, Roles.MANAGER, Roles.OPERATOR));
        });

        return services;
    }
}