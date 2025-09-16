using System.Runtime.CompilerServices;
using AutoMapper;
using Contoso.Api.Data;
using Contoso.Api.Models;
using Microsoft.EntityFrameworkCore;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Identity;
using Azure.Storage.Sas;


namespace Contoso.Api.Services;

public class ProductsService : IProductsService
{
    private readonly ContosoDbContext _context;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;

    public ProductsService(ContosoDbContext context, IMapper mapper, IConfiguration configuration)
    {
        _context = context;
        _mapper = mapper;
        _configuration = configuration;
    }

    public async Task<PagedResult<ProductDto>> GetProductsAsync(QueryParameters queryParameters)
    {
        string connectionString = _configuration["StorageConnectionString"]; 
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("StorageConnectionString variable is not set.");
        }

        BlobServiceClient client = new BlobServiceClient(connectionString);

        // Get a reference to a container and create it if it doesn't exist.
        var containerName = "t03container";
        BlobContainerClient containerClient = client.GetBlobContainerClient(containerName);

        // Generate a SAS for the container (Read) and reuse the query for all blob URLs
        if (!containerClient.CanGenerateSasUri)
        {
            throw new InvalidOperationException("Unable to generate SAS URI for the container. Ensure the storage connection string includes an account key or use a credential capable of signing SAS.");
        }
        var containerSasUri = containerClient.GenerateSasUri(BlobContainerSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
        var containerSasQuery = containerSasUri.Query; // starts with '?'

        // Prepare thumbnails container and SAS (optional fallback)
        var thumbsContainerName = "t03thumbs";
        BlobContainerClient thumbsContainerClient = client.GetBlobContainerClient(thumbsContainerName);
        string? thumbsSasQuery = null;
        if (thumbsContainerClient.CanGenerateSasUri)
        {
            var thumbsSasUri = thumbsContainerClient.GenerateSasUri(BlobContainerSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
            thumbsSasQuery = thumbsSasUri.Query;
        }



        var products = await _context.Products
                            .Where(p =>  p.Category == queryParameters.filterText || string.IsNullOrEmpty(queryParameters.filterText))
                            .Skip(queryParameters.StartIndex) 
                            .Take(queryParameters.PageSize)
                            .ToListAsync();

        var totalCount = await _context.Products
                                        .Where(p =>  p.Category == queryParameters.filterText || string.IsNullOrEmpty(queryParameters.filterText))
                                        .CountAsync();
        var itemsZZ = _mapper.Map<List<ProductDto>>(products);

        foreach (var item in itemsZZ)
        {
            BlobClient blobClient = containerClient.GetBlobClient(item.ImageUrl);
            BlobProperties properties = blobClient.GetProperties();
            foreach (var metadataItem in properties.Metadata)
            {
                Console.WriteLine($"Metadata Key: {metadataItem.Key}, Value: {metadataItem.Value}");
            }

            var releaseDate = "0";
            try
            {
                releaseDate = properties.Metadata["ReleaseDate"];
            }
            catch { }


            DateTime currentTime = DateTime.UtcNow;
            long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();
            long releaseDateUnix = long.Parse(releaseDate);

            if (unixTime >= releaseDateUnix)
            {
                // Prefer thumbnail in t03thumbs if it exists; fallback to full-size image in main container
                var fullUrl = $"{containerClient.Uri}/{item.ImageUrl}{containerSasQuery}";

                bool usedThumb = false;
                if (!string.IsNullOrEmpty(thumbsSasQuery))
                {
                    var thumbBlobName = $"thumb_{item.ImageUrl}";
                    var thumbsBlobClient = thumbsContainerClient.GetBlobClient(thumbBlobName);
                    try
                    {
                        if (thumbsBlobClient.Exists())
                        {
                            item.ImageUrl = $"{thumbsContainerClient.Uri}/{thumbBlobName}{thumbsSasQuery}";
                            usedThumb = true;
                        }
                    }
                    catch
                    {
                        // Ignore errors when checking thumbnail; fallback to full-size
                    }
                }

                if (!usedThumb)
                {
                    item.ImageUrl = fullUrl;
                }
            }
            else
            {
                var comingSoonUrl = $"{containerClient.Uri}/comingsoon.png";
                item.ImageUrl = $"{comingSoonUrl}{containerSasQuery}";
            }

        }


        var pagedProducts = new PagedResult<ProductDto>
        {
            Items = itemsZZ,
            TotalCount = totalCount,
            PageSize = queryParameters.PageSize,
            PageNumber = queryParameters.PageNumber
        };


        return pagedProducts;
    }

    public async Task<ProductDto> GetProductAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        var item = _mapper.Map<ProductDto>(product);

        string connectionString = _configuration["StorageConnectionString"];
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("StorageConnectionString variable is not set.");
        }

        BlobServiceClient client = new BlobServiceClient(connectionString);

        // Get a reference to a container and create it if it doesn't exist.
        var containerName = "t03container";
        BlobContainerClient containerClient = client.GetBlobContainerClient(containerName);

        if (!containerClient.CanGenerateSasUri)
        {
            throw new InvalidOperationException("Unable to generate SAS URI for the container. Ensure the storage connection string includes an account key or use a credential capable of signing SAS.");
        }
        var containerSasUri = containerClient.GenerateSasUri(BlobContainerSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
        var containerSasQuery = containerSasUri.Query;

        // Thumbnails container (optional)
        var thumbsContainerName = "t03thumbs";
        BlobContainerClient thumbsContainerClient = client.GetBlobContainerClient(thumbsContainerName);
        string? thumbsSasQuery = null;
        if (thumbsContainerClient.CanGenerateSasUri)
        {
            var thumbsSasUri = thumbsContainerClient.GenerateSasUri(BlobContainerSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
            thumbsSasQuery = thumbsSasUri.Query;
        }

            BlobClient blobClient = containerClient.GetBlobClient(item.ImageUrl);
            BlobProperties properties = blobClient.GetProperties();
            foreach (var metadataItem in properties.Metadata)
            {
                Console.WriteLine($"Metadata Key: {metadataItem.Key}, Value: {metadataItem.Value}");    
            }

            var releaseDate = "0";
            try
            {
                releaseDate = properties.Metadata["ReleaseDate"];
            } catch {}

        DateTime currentTime = DateTime.UtcNow;
            long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();
            long releaseDateUnix = long.Parse(releaseDate);

        // var thing = containerClient.GenerateSasUri(Azure.Storage.Sas.BlobContainerSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1)); 
        //Console.WriteLine($"THING: {thing.ToString()}");

        // Console.WriteLine($"HERE1");
        // var imageUri = "https://t03storage.blob.core.windows.net/t03container/comingsoon.png?sp=r&st=2025-09-10T01:27:43Z&se=2032-06-23T09:42:43Z&spr=https&sv=2024-11-04&sr=b&sig=fSE6dU5VPOb3fo%2FTd2bYvH5QClnAQeYrNeGrbZwgv7k%3D";
        // try
        // {
        //     imageUri = blobClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1)).ToString();
        // } catch {}
        // Console.WriteLine($"HERE2 {imageUri}");
        if (unixTime >= releaseDateUnix)
        {
            // Prefer thumbnail in t03thumbs if available
            var fullUrl = $"{containerClient.Uri}/{item.ImageUrl}{containerSasQuery}";
            bool usedThumb = false;
            if (!string.IsNullOrEmpty(thumbsSasQuery))
            {
                var thumbBlobName = $"thumb_{item.ImageUrl}";
                var thumbsBlobClient = thumbsContainerClient.GetBlobClient(thumbBlobName);
                try
                {
                    if (thumbsBlobClient.Exists())
                    {
                        item.ImageUrl = $"{thumbsContainerClient.Uri}/{thumbBlobName}{thumbsSasQuery}";
                        usedThumb = true;
                    }
                }
                catch
                {
                    // Ignore errors when checking thumbnail; fallback to full-size
                }
            }

            if (!usedThumb)
            {
                item.ImageUrl = fullUrl;
            }
        }
        else
        {
            var comingSoonUrl = $"{containerClient.Uri}/comingsoon.png";
            item.ImageUrl = $"{comingSoonUrl}{containerSasQuery}";
        }


        return item;
    }

    public async Task<ProductDto> CreateProductAsync(ProductDto product)
    {
        var productModel = _mapper.Map<Product>(product);

        _context.Products.Add(productModel);

        await _context.SaveChangesAsync();

        return _mapper.Map<ProductDto>(productModel);
    }

    public async Task<ProductDto> UpdateProductAsync(ProductDto product)
    {
        var existingProduct = await _context.Products.AsNoTracking().FirstAsync(x => x.Id == product.Id);

        if  (existingProduct == null)
        {
            return null;
        }

        existingProduct.Name = product.Name;
        existingProduct.Description = product.Description;
        existingProduct.Price = product.Price;
        
        if (existingProduct.ImageUrl != product.ImageUrl)
        {
            existingProduct.ImageUrl = product.ImageUrl;
        }


        _context.Entry(existingProduct).State = EntityState.Modified;

        await _context.SaveChangesAsync();

        return _mapper.Map<ProductDto>(existingProduct);
    }

    public async Task DeleteProductAsync(int id)
    {
        var product = await _context.Products.AsNoTracking().FirstAsync(x => x.Id == id);

        _context.Products.Remove(product);

        await _context.SaveChangesAsync();
    }

    public async Task<List<string>> GetProductCategories()
    {
        return await _context.Products.Select(x => x.Category).Distinct().ToListAsync();
    }
}