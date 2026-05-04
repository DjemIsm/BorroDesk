using System.Text;
using System.Text.Json;
using BorroDesk.Api.Configuration;
using BorroDesk.Api.Data;
using BorroDesk.Api.Entities;
using BorroDesk.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddDbContext<BorroDeskDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthorization();

builder.Services
    .AddIdentityApiEndpoints<User>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<BorroDeskDbContext>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = string.IsNullOrWhiteSpace(jwtOptions.SigningKey)
                ? null
                : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await WriteProblemDetailsAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "Authentication required.",
                    "A valid bearer token is required to access this resource.");
            },
            OnForbidden = async context =>
            {
                await WriteProblemDetailsAsync(
                    context.HttpContext,
                    StatusCodes.Status403Forbidden,
                    "Access denied.",
                    "You do not have permission to access this resource.");
            }
        };
    });

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Type = "https://httpstatuses.com/400",
                Title = "Validation failed.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "One or more validation errors occurred.",
                Instance = context.HttpContext.Request.Path
            };
            problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            return new BadRequestObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter the JWT access token returned by POST /api/auth/login."
        };

        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, _) =>
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var allowsAnonymous = metadata.OfType<IAllowAnonymous>().Any();
        var requiresAuthorization = metadata.OfType<IAuthorizeData>().Any();

        if (allowsAnonymous || !requiresAuthorization)
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, context.Document)] = []
        });

        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await ApplicationDataSeeder.SeedDevelopmentAsync(app.Services);
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("BorroDesk API")
            .AddPreferredSecuritySchemes([JwtBearerDefaults.AuthenticationScheme])
            .AddHttpAuthentication(JwtBearerDefaults.AuthenticationScheme, _ => { });
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityApi<User>();
app.MapControllers();

app.Run();

static async Task WriteProblemDetailsAsync(HttpContext httpContext, int statusCode, string title, string detail)
{
    if (httpContext.Response.HasStarted)
    {
        return;
    }

    var problemDetails = new ProblemDetails
    {
        Type = $"https://httpstatuses.com/{statusCode}",
        Title = title,
        Status = statusCode,
        Detail = detail,
        Instance = httpContext.Request.Path
    };
    problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

    httpContext.Response.StatusCode = statusCode;
    httpContext.Response.ContentType = "application/problem+json";
    await httpContext.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
}
