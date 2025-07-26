using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecureProcessor.Core.Patterns.Options;
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

// Add gRPC services
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseRouting();

// Map gRPC services
// ✅ تغییر نام کلاس
app.MapGrpcService<MessageDispatcherServiceImpl>();

// Optional: Add a default endpoint for browser testing
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();