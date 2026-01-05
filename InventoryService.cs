// Ubicación: InvoiceManagementAPI/Services/InventoryService.cs
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using InvoiceManagementAPI.Model.DTOs;

namespace InvoiceManagementAPI.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly HttpClient _httpClient;

        public InventoryService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ProductDto> ObtenerProductoPorCodigo(string codigo)
        {
            try
            {
                // NOTA: Como la API externa usa paginación, idealmente deberías tener un endpoint de búsqueda directa.
                // Aquí simulamos buscar en la página 1.
                var response = await _httpClient.GetFromJsonAsync<InventoryResponse>("data?page=1"); // Ajusta el endpoint según la ruta real de la API externa

                var producto = response?.Data?.FirstOrDefault(p => p.Codigo == codigo);
                return producto;
            }
            catch (Exception ex)
            {
                // Manejar logs aquí
                return null;
            }
        }

        public async Task<bool> VerificarStock(string codigo, int cantidadRequerida)
        {
            var producto = await ObtenerProductoPorCodigo(codigo);
            if (producto == null) return false;
            return producto.Stock >= cantidadRequerida;
        }
    }
}