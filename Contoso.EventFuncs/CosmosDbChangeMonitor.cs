using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.CosmosDB;
using Microsoft.Extensions.Logging;

namespace Company.Function
{
    public class CosmosDbChangeMonitor
    {
        private readonly ILogger<CosmosDbChangeMonitor> _logger;
        private readonly EventGridPublisherClient? _eventGridClient;

        public CosmosDbChangeMonitor(ILogger<CosmosDbChangeMonitor> logger, EventGridPublisherClient? eventGridClient = null)
        {
            _logger = logger;
            _eventGridClient = eventGridClient;
        }

        [Function("CosmosDbChangeMonitor")]
        public async Task Run([
            CosmosDBTrigger(
                databaseName: "t03-db",
                containerName: "Products",
                Connection = "t03cosmos_DOCUMENTDB",
                LeaseContainerName = "leases",
                CreateLeaseContainerIfNotExists = true)
        ] IReadOnlyList<MyDocument> updatedDocuments)
        {
            _logger.LogInformation("Updating...");

            if (updatedDocuments == null || updatedDocuments.Count == 0)
            {
                return;
            }

            _logger.LogInformation("Documents modified: {Count}", updatedDocuments.Count);

            if (_eventGridClient == null)
            {
                _logger.LogWarning("EventGridPublisherClient is not configured. Skipping publish.");
                return;
            }

            var events = new List<EventGridEvent>(updatedDocuments.Count);
            foreach (var doc in updatedDocuments)
            {
                var ev = new EventGridEvent(
                    subject: $"documents/{doc.id}",
                    eventType: "DocumentUpdated",
                    data: doc,
                    dataVersion: "1.0")
                {
                    Id = Guid.NewGuid().ToString(),
                    EventTime = DateTime.UtcNow
                };
                events.Add(ev);
            }

            await _eventGridClient.SendEventsAsync(events);
        }
    }

    // Customize the model with your own desired properties
    public class MyDocument
    {
        public string id { get; set; } = string.Empty;
        //... other properties
    }
}
