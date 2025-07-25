using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecureProcessor.Processor.gRPC_Client;
using SecureProcessor.Processor.Services;
using SecureProcessor.Shared.Protos;

class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add services
        services.AddSingleton<IMessageProcessorService, MessageProcessorService>();

        services.AddLogging();

        // Create channel and client manually
        var channel = GrpcChannel.ForAddress("https://localhost:5001");
        services.AddSingleton(channel);

        var client = new MessageDispatcher.MessageDispatcherClient(channel);
        services.AddSingleton(client);

        services.AddSingleton<ProcessorClientService>();

        var serviceProvider = services.BuildServiceProvider();

        var processorClient = serviceProvider.GetService<ProcessorClientService>();

        try
        {
            await processorClient.StartAsync();
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetService<ILogger<Program>>();
            logger?.LogError(ex, "Error in processor client");
            Console.WriteLine($"Error: {ex.Message}");
            Console.ReadLine();
        }
    }
}