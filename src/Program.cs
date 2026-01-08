using ApiGateway.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Autenticação JWT
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationPolicies();

builder.Services.AddGatewayReverseProxy(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin() //NOSONAR
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));

app.MapReverseProxy();

app.Run();