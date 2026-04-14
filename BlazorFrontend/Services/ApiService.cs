using System.Net.Http.Json;
using BlazorFrontend.Models;

namespace BlazorFrontend.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;

        public ApiService(HttpClient http)
        {
            _http = http;
        }

        // ── Products ──────────────────────────────────────
        public async Task<List<ProductDto>> GetProductsAsync()
            => await _http.GetFromJsonAsync<List<ProductDto>>("/products") ?? new();

        public async Task AddProductAsync(ProductDto p)
            => await _http.PostAsJsonAsync("/products", new
            {
                p.Name,
                p.Description,
                p.Price,
                p.Stock
            });

        // ── Customers ─────────────────────────────────────
        public async Task<List<CustomerDto>> GetCustomersAsync()
            => await _http.GetFromJsonAsync<List<CustomerDto>>("/customers") ?? new();

        public async Task AddCustomerAsync(CustomerDto c)
            => await _http.PostAsJsonAsync("/customers", new
            {
                c.Name,
                c.Email
            });

        // ── Orders ────────────────────────────────────────
        public async Task<List<OrderDto>> GetOrdersAsync()
            => await _http.GetFromJsonAsync<List<OrderDto>>("/orders") ?? new();

        public async Task AddOrderAsync(OrderDto o)
            => await _http.PostAsJsonAsync("/orders", new
            {
                o.CustomerId,
                o.ProductId,
                o.Quantity
            });
    }
}