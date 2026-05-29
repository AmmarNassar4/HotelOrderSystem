using System.Reflection;
using System.Text;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using HotelOrderSystem.Api.Config;
using HotelOrderSystem.Api.Data;
using HotelOrderSystem.Api.Hubs;
using HotelOrderSystem.Api.Middleware;
using HotelOrderSystem.Api.Services;
using HotelOrderSystem.Api.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<SlaOptions>(builder.Configuration.GetSection(SlaOptions.SectionName));
builder.Services.Configure<PresenceOptions>(builder.Configuration.GetSection(PresenceOptions.SectionName));
builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection(NotificationOptions.SectionName));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (jwtOptions.SigningKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddMemoryCache();
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hotel Order & Task Management API",
        Version = "v1",
        Description = "API-first backend for hotel requests, task routing, staff presence, SignalR, and FCM outbox notifications."
    });

    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(_ => true)
            .AllowCredentials();
    });
});

builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPresenceService, PresenceService>();
builder.Services.AddScoped<INotificationOutboxService, NotificationOutboxService>();
builder.Services.AddScoped<IPushNotificationService, FirebasePushNotificationService>();
builder.Services.AddSingleton<IRealtimeNotificationService, RealtimeNotificationService>();

builder.Services.AddHostedService<NotificationOutboxWorker>();
builder.Services.AddHostedService<SlaEscalationWorker>();
builder.Services.AddHostedService<PresenceCleanupWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

    if (configuration.GetValue<bool>("Database:EnsureCreated"))
    {
        await db.Database.EnsureCreatedAsync();
    }

    if (configuration.GetValue<bool>("Database:SeedDemoData"))
    {
        await SeedData.EnsureSeededAsync(db, passwordService);
    }

    var notificationOptions = configuration.GetSection("Notifications").Get<NotificationOptions>();
    if (notificationOptions?.FcmMode != "Stub")
    {
        var firebaseLogger = loggerFactory.CreateLogger("FirebaseInit");
        const string serviceAccountFileName = "firebase-service-account.json";
        var candidateServiceAccountPaths = new[]
        {
            Path.Combine(app.Environment.ContentRootPath, serviceAccountFileName),
            Path.Combine(AppContext.BaseDirectory, serviceAccountFileName),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, serviceAccountFileName)
        }
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

        var serviceAccountPath = candidateServiceAccountPaths.FirstOrDefault(File.Exists);
        if (serviceAccountPath is null)
        {
            firebaseLogger.LogWarning(
                "Firebase service account JSON was not found. Checked: {CheckedPaths}",
                string.Join(" | ", candidateServiceAccountPaths));
        }
        else
        {
            try
            {
                var firebaseApp = FirebaseApp.DefaultInstance;
                if (firebaseApp is null)
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(serviceAccountPath)
                    });
                    firebaseLogger.LogInformation("Firebase initialized successfully using service account: {ServiceAccountPath}", serviceAccountPath);
                }
                else
                {
                    firebaseLogger.LogInformation("Firebase already initialized: {FirebaseAppName}", firebaseApp.Name);
                }
            }
            catch (Exception ex)
            {
                firebaseLogger.LogError(ex, "Failed to initialize Firebase using service account: {ServiceAccountPath}. Push notifications will not work.", serviceAccountPath);
            }
        }
    }
}

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel Order API v1");
        options.DocumentTitle = "Hotel Order API";
    });
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("DefaultCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AdminHub>("/hubs/admin");
app.MapHub<StaffHub>("/hubs/staff");
app.MapFallbackToFile("index.html");

app.Run();
