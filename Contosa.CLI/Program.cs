using Azure.Identity;
using Azure.Storage.Blobs;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide the name of your Azure Storage account.");
            return;
        }

        string accountName = args[0];
        string accountUrl = $"https://{accountName}.blob.core.windows.net";

        // Use DefaultAzureCredential which supports multiple authentication methods
        var credential = new DefaultAzureCredential();

        try
        {
            // Create a BlobServiceClient
            var blobServiceClient = new BlobServiceClient(new Uri(accountUrl), credential);

            Console.WriteLine($"Listing containers in storage account '{accountName}':");
            Console.WriteLine("-------------------------------------------------");

            // List all containers
            await foreach (var container in blobServiceClient.GetBlobContainersAsync())
            {
                Console.WriteLine($"Container: {container.Name}");

                // Get a client for the container
                var containerClient = blobServiceClient.GetBlobContainerClient(container.Name);

                // List all blobs in the container
                await foreach (var blob in containerClient.GetBlobsAsync())
                {
                    Console.WriteLine($"  - Blob: {blob.Name}");
                }
                Console.WriteLine("-------------------------------------------------");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}