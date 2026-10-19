using KycAggregation.Api.Upstream;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace KycAggregation.Api.Tests;

/// <summary>Spins up the API in-memory with a per-instance SQLite file and a programmable upstream client.</summary>
public sealed class KycApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"kyc-test-{Guid.NewGuid():N}.db");
    private readonly string _keysPath = Path.Combine(Path.GetTempPath(), $"kyc-keys-{Guid.NewGuid():N}");

    public ConcurrentQueue<string> Logs { get; } = new();

    /// <summary>Override the storage paths to share a database/keys across "restarts".</summary>
    public string? DbPath { get; init; }
    public string? KeysPath { get; init; }
    /// <summary>When true, the storage files survive disposal (used by the restart test).</summary>
    public bool KeepFiles { get; init; }

    public Func<string, DateOnly, CustomerDataBundle?> Responder { get; set; } = (_, _) => null;
    public bool ThrowOnFetch { get; set; }
    public int? PermitLimit { get; set; }

    public const string TestApiKey = "integration-test-api-key";

    /// <summary>A client that already presents a valid API key.</summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", TestApiKey);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Kyc", $"Data Source={DbPath ?? _dbPath}");
        builder.UseSetting("DataProtection:KeysDirectory", KeysPath ?? _keysPath);
        builder.UseSetting("Security:SsnHashKey", "integration-test-ssn-hash-key");
        builder.UseSetting("Security:ApiKeys:0", TestApiKey);
        if (PermitLimit is int limit)
        {
            builder.UseSetting("RateLimiting:PermitLimit", limit.ToString());
        }

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICustomerDataClient>();
            services.AddSingleton<ICustomerDataClient>(new FakeClient(this));
            services.AddSingleton<ILoggerProvider>(new CapturingLoggerProvider(Logs));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && !KeepFiles)
        {
            TryDelete(DbPath ?? _dbPath);
            var keys = KeysPath ?? _keysPath;
            if (Directory.Exists(keys)) Directory.Delete(keys, recursive: true);
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best-effort cleanup */ }
    }

    private sealed class FakeClient(KycApiFactory owner) : ICustomerDataClient
    {
        public Task<CustomerDataBundle?> GetAsync(string ssn, DateOnly asOf, CancellationToken ct)
        {
            if (owner.ThrowOnFetch) throw new InvalidOperationException("upstream failure");
            return Task.FromResult(owner.Responder(ssn, asOf));
        }
    }

    private sealed class CapturingLoggerProvider(ConcurrentQueue<string> sink) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(sink);
        public void Dispose() { }

        private sealed class CapturingLogger(ConcurrentQueue<string> sink) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
                => sink.Enqueue($"{logLevel}|{formatter(state, exception)}");
        }
    }
}
