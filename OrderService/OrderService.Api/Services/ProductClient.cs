using System.Net;
using System.Net.Http.Json;

namespace OrderService.Api.Services;

public class ProductClient : IProductClient
{
    private readonly HttpClient _httpClient;

    public ProductClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> ProductExistsAsync(int productId)
    {
        var response = await _httpClient.GetAsync($"api/products/{productId}");
        return response.StatusCode == HttpStatusCode.OK;
    }

    public async Task<decimal> GetProductPriceAsync(int productId)
    {
        var product = await _httpClient.GetFromJsonAsync<ProductResponse>($"api/products/{productId}");
        return product?.Price ?? 0;
    }

    private record ProductResponse(int Id, string Name, string Description, decimal Price, int Stock);
}