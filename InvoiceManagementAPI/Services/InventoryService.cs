using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using InvoiceManagementAPI.Model.DTOs;

namespace InvoiceManagementAPI.Services
{
    // Clase auxiliar interna para leer la metadata de paginación de Laravel
    // Hereda de tu InventoryResponse original para no romper nada
    public class InventoryResponseWithMeta : InventoryResponse
    {
        [JsonPropertyName("last_page")]
        public int LastPage { get; set; }

        [JsonPropertyName("current_page")]
        public int CurrentPage { get; set; }
    }

    public class InventoryService : IInventoryService
    {
        private readonly HttpClient _httpClient;

        public InventoryService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ProductDto> ObtenerProductoPorCodigo(string codigo)
        {
            int paginaActual = 1;
            int ultimaPagina = 1;

            // Iniciamos un bucle para buscar página por página
            do
            {
                try
                {
                    // 1. Solicitamos la página dinámica
                    var url = $"products?page={paginaActual}";

                    // Usamos la clase extendida para capturar el 'last_page'
                    var response = await _httpClient.GetFromJsonAsync<InventoryResponseWithMeta>(url);

                    if (response?.Data != null)
                    {
                        // 2. Buscamos el producto en ESTA página
                        // Usamos StringComparison para ignorar mayúsculas/minúsculas
                        var producto = response.Data
                            .FirstOrDefault(p => p.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase));

                        // SI LO ENCONTRAMOS, ¡RETORNAMOS INMEDIATAMENTE!
                        if (producto != null)
                        {
                            return producto;
                        }

                        // 3. Si no está, actualizamos el límite para seguir buscando
                        // La API nos dice cuál es la última página (ej: 5)
                        if (response.LastPage > 0)
                        {
                            ultimaPagina = response.LastPage;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Loguear error pero no detener el flujo si es posible intentar otra cosa
                    Console.WriteLine($"Error consultando página {paginaActual}: {ex.Message}");
                    return null;
                }

                // Pasamos a la siguiente página
                paginaActual++;

            } while (paginaActual <= ultimaPagina);

            // Si llegamos aquí, recorrimos TODAS las páginas y no existe
            return null;
        }

        public async Task<bool> VerificarStock(string codigo, int cantidadRequerida)
        {
            var producto = await ObtenerProductoPorCodigo(codigo);
            if (producto == null) return false;
            return producto.Stock >= cantidadRequerida;
        }
    }
}