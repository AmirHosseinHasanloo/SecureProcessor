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

        // Add gRPC client - روش صحیح
        services.AddSingleton<GrpcChannel>(provider =>
        {
            return GrpcChannel.ForAddress("https://localhost:5001");
        });

        // Register gRPC client properly
        services.AddSingleton<MessageDispatcherService.MessageDispatcherServiceClient>(provider =>
        {
            var channel = provider.GetRequiredService<GrpcChannel>();
            return new MessageDispatcherService.MessageDispatcherServiceClient(channel);
        });

        services.AddSingleton<ProcessorClientService>();

        var serviceProvider = services.BuildServiceProvider();

        var processorClient = serviceProvider.GetService<ProcessorClientService>();

        try
        {
            await processorClient.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
            Console.ReadLine();
        }
    }
}