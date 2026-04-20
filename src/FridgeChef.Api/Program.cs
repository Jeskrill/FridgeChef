using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.RateLimiting;
using System.Text;
using FridgeChef.Api.CrossBc;
using FridgeChef.Api.Extensions;
using FridgeChef.Api.Middleware;
using FridgeChef.Auth.Application.DependencyInjection;
using FridgeChef.Auth.Infrastructure;
using FridgeChef.Auth.Infrastructure.Security;
using FridgeChef.Catalog.Application;
using FridgeChef.Catalog.Infrastructure;
using FridgeChef.Pantry.Application.DependencyInjection;
using FridgeChef.Pantry.Infrastructure;
using FridgeChef.Favorites.Application.DependencyInjection;
using FridgeChef.Favorites.Infrastructure;
using FridgeChef.UserPreferences.Application.DependencyInjection;
using FridgeChef.UserPreferences.Infrastructure;
using FridgeChef.Ontology.Application.DependencyInjection;
using FridgeChef.Ontology.Infrastructure;
using FridgeChef.Admin.Application.DependencyInjection;
using FridgeChef.Pricing.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ──
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration));

// ── Bounded Context Infrastructure ──
builder.Services.AddAuthInfrastructure(builder.Configuration);
builder.Services.AddCatalogInfrastructure(builder.Configuration);
builder.Services.AddPantryInfrastructure(builder.Configuration);
builder.Services.AddFavoritesInfrastructure(builder.Configuration);
builder.Services.AddUserPreferencesInfrastructure(builder.Configuration);
builder.Services.AddOntologyInfrastructure(builder.Configuration);
builder.Services.AddPricingInfrastructure(builder.Configuration);

// ── Bounded Context Application ──
builder.Services.AddAuthApplication();
builder.Services.AddCatalogApplication();
builder.Services.AddPantryApplication();
builder.Services.AddFavoritesApplication();
builder.Services.AddUserPreferencesApplication();
builder.Services.AddOntologyApplication();
builder.Services.AddAdminApplication();
builder.Services.AddCrossBcAdapters(); // кросс-BC адаптеры: Catalog→Favorites, Auth/Catalog/Favorites→Admin

// ── Authentication ──
var jwtSecret = builder.Configuration.GetRequiredJwtSecret();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "FridgeChef",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "FridgeChefApp",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(context =>
            context.User.Claims.Any(claim =>
                claim.Type == ClaimTypes.Role &&
                string.Equals(claim.Value, "admin", StringComparison.OrdinalIgnoreCase))));
});

// ── CORS ──
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5500"];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Слишком много запросов",
            Detail = "Превышен лимит запросов. Повторите попытку позже."
        }, ct);
    };

    options.AddPolicy("AuthPerIp", httpContext =>
    {
        var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.AddPolicy("AdminPerIdentity", httpContext =>
    {
        var key = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

// ── API ──
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "FridgeChef API",
        Version = "v1",
        Description = """
            **FridgeChef** — сервис подбора рецептов из продуктов в вашем холодильнике.

            | Понятие | Что это |
            |---------|--------|
            | **FoodNode** | Продукт в базе знаний. Иерархичен: «Молоко» → «Цельное молоко 3.2%» |
            | **Pantry (Холодильник)** | Список продуктов пользователя с количеством |
            | **Теги / Категории** | Диеты, кухни, типы блюд для фильтрации |

            Защищённые эндпоинты требуют JWT. Авторизуйтесь через кнопку **Authorize** вверху страницы.
            """,
    });

    options.DocumentFilter<TagDescriptionsDocumentFilter>();
    options.OperationFilter<RequestExamplesOperationFilter>();

    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Вставьте access-токен из /auth/login"
    });

    options.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddResponseCompression();
var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not configured");
builder.Services.AddHealthChecks()
    .AddNpgSql(defaultConnectionString);

var app = builder.Build();

// ── Middleware Pipeline ──
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseResponseCompression();
app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

// ── Endpoints ──
app.MapAllEndpoints();
app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FridgeChef API v1");
        c.InjectStylesheet("/swagger-ui/custom.css");
        c.DocumentTitle = "FridgeChef API";
    });
}

Log.Information("FridgeChef API starting on {Urls}", string.Join(", ", app.Urls));
app.Run();
