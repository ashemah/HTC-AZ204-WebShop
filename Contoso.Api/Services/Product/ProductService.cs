using System.Runtime.CompilerServices;
using AutoMapper;
using Contoso.Api.Data;
using Contoso.Api.Models;
using Microsoft.EntityFrameworkCore;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Identity;


namespace Contoso.Api.Services;

public class ProductsService : IProductsService
{
    private readonly ContosoDbContext _context;
    private readonly IMapper _mapper;

    public ProductsService(ContosoDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<ProductDto>> GetProductsAsync(QueryParameters queryParameters)
    {
        BlobServiceClient client = new(
            new Uri($"https://t03storage.blob.core.windows.net"),
            new DefaultAzureCredential()
        );

        // Get a reference to a container and create it if it doesn't exist.
        var containerName = "t03container";
        BlobContainerClient containerClient = client.GetBlobContainerClient(containerName);



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
            } catch {}
  

            DateTime currentTime = DateTime.UtcNow;
            long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();
            long releaseDateUnix = long.Parse(releaseDate);

            var imageUri = blobClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
            if (unixTime >= releaseDateUnix)
            {
                //item.ImageUrl = item.ImageUrl + "?sp=r&st=2025-09-10T00:11:53Z&se=2025-09-10T08:26:53Z&spr=https&sv=2024-11-04&sr=c&sig=DmFnQeB9yO%2FKaiHNrZzRXL1ATszt0t0opG3uI0UArZw%3D";
                item.ImageUrl = imageUri.ToString();
            }
            else
            {
                item.ImageUrl = "https://t03storage.blob.core.windows.net/t03container/comingsoon.png?sp=r&st=2025-09-10T01:27:43Z&se=2032-06-23T09:42:43Z&spr=https&sv=2024-11-04&sr=b&sig=fSE6dU5VPOb3fo%2FTd2bYvH5QClnAQeYrNeGrbZwgv7k%3D";
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


        BlobServiceClient client = new(
            new Uri($"https://t03storage.blob.core.windows.net"),
            new DefaultAzureCredential()
        );

        // Get a reference to a container and create it if it doesn't exist.
        var containerName = "t03container";
        BlobContainerClient containerClient = client.GetBlobContainerClient(containerName);

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

        Console.WriteLine($"HERE1");
        var imageUri = blobClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
        Console.WriteLine($"HERE2 {imageUri}");
        if (unixTime >= releaseDateUnix)
        {
            //item.ImageUrl = item.ImageUrl + "?sp=r&st=2025-09-10T00:11:53Z&se=2025-09-10T08:26:53Z&spr=https&sv=2024-11-04&sr=c&sig=DmFnQeB9yO%2FKaiHNrZzRXL1ATszt0t0opG3uI0UArZw%3D";
            item.ImageUrl = imageUri.ToString();
        }
        else
        {
            item.ImageUrl = "https://t03storage.blob.core.windows.net/t03container/comingsoon.png?sp=r&st=2025-09-10T01:27:43Z&se=2032-06-23T09:42:43Z&spr=https&sv=2024-11-04&sr=b&sig=fSE6dU5VPOb3fo%2FTd2bYvH5QClnAQeYrNeGrbZwgv7k%3D";
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