using KycAggregation.Api.Api;
using KycAggregation.Api.Error;
using KycAggregation.Api.Persistence;
using KycAggregation.Api.Security;
using KycAggregation.Api.Services;
using KycAggregation.Api.Upstream;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Upstream Customer Data API.
var apiOptions = config.GetSection(CustomerDataApiOptions.SectionName).Get<CustomerDataApiOptions>()
    ?? throw new InvalidOperationException("CustomerDataApi configuration is missing.");
builder.Services.AddHttpClient<ICustomerDataClient, CustomerDataClient>(client =>
    client.BaseAddress = new Uri(apiOptions.BaseUrl));

// Persistence: file-based SQLite with Data Protection keys persisted to disk so encrypted
// records can still be decrypted after a restart.
var keysDirectory = config["DataProtection:KeysDirectory"] ?? "keys";
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
    .SetApplicationName("KycAggregation");

builder.Services.AddDbContext<KycDbContext>(options =>
    options.UseSqlite(config.GetConnectionString("Kyc")
        ?? throw new InvalidOperationException("Kyc connection string is missing.")));

var ssnHashKey = config["Security:SsnHashKey"]
    ?? throw new InvalidOperationException("Security:SsnHashKey is missing.");
builder.Services.AddSingleton(new SsnHasher(ssnHashKey));

builder.Services.AddScoped<IKycRepository, EfKycRepository>();
builder.Services.AddScoped<IKycAggregationService, KycAggregationService>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Rate limiting: a fixed window applied to every request.
var permitLimit = config.GetValue("RateLimiting:PermitLimit", 100);
var windowSeconds = config.GetValue("RateLimiting:WindowSeconds", 60);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
        RateLimitPartition.GetFixedWindowLimiter("global", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            QueueLimit = 0,
        }));
});

var apiKeys = config.GetSection("Security:ApiKeys").Get<string[]>() ?? [];

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<KycDbContext>().Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseExceptionHandler(_ => { });
app.UseRateLimiter();
app.UseApiKeyAuthentication(apiKeys);

app.MapKycEndpoints();

app.Run();

public partial class Program;
