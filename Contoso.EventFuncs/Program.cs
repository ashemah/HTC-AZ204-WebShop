using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;

var host = new HostBuilder()
    .ConfigureAppConfiguration(config =>
    {
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        config.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Event Grid publisher client registration using settings
        var topicEndpoint = context.Configuration["EventGridTopicEndpoint"];
        var topicKey = context.Configuration["EventGridTopicKey"];
        if (!string.IsNullOrWhiteSpace(topicEndpoint) && !string.IsNullOrWhiteSpace(topicKey))
        {
            var endpoint = new Uri(topicEndpoint);
            services.AddSingleton(new EventGridPublisherClient(endpoint, new Azure.AzureKeyCredential(topicKey)));
        }
    })
    .ConfigureLogging(logging =>
    {
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

await host.RunAsync();
