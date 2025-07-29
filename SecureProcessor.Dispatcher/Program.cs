using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecureProcessor.Core.Patterns.CircuitBreaker;
using SecureProcessor.Core.Patterns.Options;
using SecureProcessor.Core.Patterns.Proxy;
using SecureProcessor.Core.Services;
using SecureProcessor.Dispatcher;
using SecureProcessor.Dispatcher.gRPC_Services;
using SecureProcessor.Dispatcher.Services;
using SecureProcessor.Shared.Protos;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Add services
builder.Services.Configure<DispatcherOptions>(
    builder.Configuration.GetSection("Dispatcher"));

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IMessageQueueService, MessageQueueService>();
builder.Services.AddSingleton<IProcessorManagerService, ProcessorManagerService>();
builder.Services.AddSingleton<IHealthCheckService, HealthCheckService>();
builder.Services.AddSingleton<ICircuitBreaker>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<CircuitBreaker>>();
    return new CircuitBreaker(logger);
});
// ✅ ثبت ServiceProxy با Factory Method

builder.Services.AddHttpClient<IHealthCheckService, HealthCheckService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "Dispatcher-Service");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    // ✅ دور زدن اعتبارسنجی certificate برای تست (در محیط تولید حذف شود)
    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
    return handler;
});
builder.Services.AddSingleton<IServiceProxy<IHealthCheckService>>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<ServiceProxy<IHealthCheckService>>>();
    var circuitBreaker = provider.GetRequiredService<ICircuitBreaker>();

    // ✅ استفاده از Factory برای جلوگیری از Circular Dependency
    return new ServiceProxy<IHealthCheckService>(
        () => provider.GetRequiredService<IHealthCheckService>(), // Factory Method
        circuitBreaker,
        logger
    );
});

// Add gRPC services
builder.Services.AddGrpc();

builder.Services.AddHostedService<DispatcherWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseRouting();

// Map gRPC services
// ✅ تغییر نام کلاس
app.MapGrpcService<MessageDispatcherServiceImpl>();

// Optional: Add a default endpoint for browser testing
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();