using Contoso.Api.Data;
using Contoso.Api.Models;
using Contoso.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Azure.Identity;

namespace Contoso.Api.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductsService _productService;
    private readonly IConfiguration _configuration;

    public ProductsController(IProductsService productService, IConfiguration configuration)
    {
        _productService = productService;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<PagedResult<ProductDto>> GetProductsAsync(QueryParameters queryParameters)
    {
        return await _productService.GetProductsAsync(queryParameters);
    }

    [HttpGet("categories")]
    public async Task<List<string>> GetProductCategories()
    {
        return await _productService.GetProductCategories();
    }
    
    [HttpGet("{id}")]
    public async Task<ProductDto> GetProductAsync(int id)
    {
        return await _productService.GetProductAsync(id);
    }

    [HttpPost("create")]
    [Authorize]
    public async Task<ProductDto> CreateProductAsync(ProductDto product)
    {
        return await _productService.CreateProductAsync(product);
    }


    [HttpPut]
    [Authorize]
    public async Task<IActionResult> UpdateProductAsync(ProductDto product)
    {
        var updatedProduct = await _productService.UpdateProductAsync(product);

        if (updatedProduct == null)
        {
            return BadRequest("Product not found");
        }

        return Ok(updatedProduct);
    }


    [HttpPost("upload/images")]
    [Authorize]
    public async Task<IActionResult> GetUploadBlobUrl([FromBody] List<ProductImageDto> productImages)
    {
        if (productImages == null || productImages.Count == 0)
        {
            return BadRequest("No images provided for upload.");
        }

        string containerName = "t03container";

        try
        {            
            string connectionString = _configuration["StorageConnectionString"]; 
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("StorageConnectionString variable is not set.");
            }

            BlobServiceClient client = new BlobServiceClient(connectionString);

            // Get a reference to a container and create it if it doesn't exist.
            BlobContainerClient containerClient = client.GetBlobContainerClient(containerName);

            foreach (var image in productImages)
            {
                using var stream = new MemoryStream(image.Image);

                // 1. Create a unique file name to avoid overwrites
                string uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(image.ImageUrl)}";
                BlobClient blobClient = containerClient.GetBlobClient(uniqueFileName);

                // 2. Upload the image data to Azure Blob Storage
                await blobClient.UploadAsync(stream, overwrite: true);

                // 3. Set the custom metadata
                var metadata = new Dictionary<string, string>
                {
                    { "ReleaseDate", DateTime.UtcNow.AddDays(5).ToString("o") } // "o" for round-trip ISO 8601 format
                };
                await blobClient.SetMetadataAsync(metadata);

                // 4. Add it to the db
            }
        }
        catch (Exception ex)
        {
            // Catch exceptions related to container creation or configuration
            return StatusCode(500, $"An error occurred while processing the request: {ex.Message}");
        }

        return StatusCode(200);
    }

    [HttpPost("create/bulk")]
    [Authorize]
    public async Task<IActionResult> CreateProductsAsync()
    {
         ///////////////////////
        //// YOUR CODE HERE ///
       ///////////////////////
       
       return  BadRequest();
    }


    [HttpDelete("{id}")]
    [Authorize]
    public async Task DeleteProductAsync(int id)
    {
        await _productService.DeleteProductAsync(id);
    }
}