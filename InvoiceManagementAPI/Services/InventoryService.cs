using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
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
            Console.WriteLine($"🔍 Buscando: '{codigo}'");

            int paginaActual = 1;
            int ultimaPagina = int.MaxValue;

            while (paginaActual <= ultimaPagina)
            {
                try
                {
                    var url = $"products?page={paginaActual}";
                    Console.WriteLine($"📡 GET {_httpClient.BaseAddress}{url}");

                    // 🔍 VER RESPUESTA RAW PARA DEBUG
                    var httpResponse = await _httpClient.GetAsync(url);
                    var jsonString = await httpResponse.Content.ReadAsStringAsync();

                    Console.WriteLine("=== RESPUESTA RAW (primeros 500 caracteres) ===");
                    Console.WriteLine(jsonString.Substring(0, Math.Min(500, jsonString.Length)));
                    Console.WriteLine("==============================================");

                    var response = JsonSerializer.Deserialize<InventoryResponse>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (response == null)
                    {
                        Console.WriteLine("⚠️ Response NULL");
                        return null;
                    }

                    Console.WriteLine($"📊 Deserializado: CurrentPage={response.CurrentPage}, LastPage={response.LastPage}, Total={response.Total}");

                    // Primera vez: establecer límite
                    if (paginaActual == 1)
                    {
                        ultimaPagina = response.LastPage;
                        Console.WriteLine($"📚 Total: {response.Total} productos en {ultimaPagina} página(s)");
                    }

                    if (response.Data != null && response.Data.Any())
                    {
                        Console.WriteLine($"📄 Página {response.CurrentPage}/{ultimaPagina} - {response.Data.Count} productos");

                        // Mostrar IDs para verificar orden
                        var ids = string.Join(", ", response.Data.Take(5).Select(p => $"{p.Codigo}(ID:{p.Id})"));
                        Console.WriteLine($"   Primeros 5: {ids}");

                        // Buscar producto
                        var producto = response.Data
                            .FirstOrDefault(p => p.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase));

                        if (producto != null)
                        {
                            Console.WriteLine($"✅ ENCONTRADO: {producto.Codigo} | Stock: {producto.Stock} | PVP: {producto.PrecioVenta}");
                            return producto;
                        }

                        Console.WriteLine($"   ❌ '{codigo}' no está en esta página");
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Página {paginaActual} vacía");
                        break;
                    }

                    paginaActual++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                    if (ex.InnerException != null)
                        Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                    return null;
                }
            }

            Console.WriteLine($"❌ '{codigo}' NO ENCONTRADO después de revisar {paginaActual - 1} página(s)");
            return null;
        }

        public async Task<bool> VerificarStock(string codigo, int cantidadRequerida)
        {
            var producto = await ObtenerProductoPorCodigo(codigo);

            if (producto == null)
            {
                Console.WriteLine($"⚠️ '{codigo}' no existe en inventario");
                return false;
            }

            bool suficiente = producto.Stock >= cantidadRequerida;
            Console.WriteLine($"📊 Stock: {producto.Stock} | Requiere: {cantidadRequerida} → {(suficiente ? "✅ OK" : "❌ INSUFICIENTE")}");

            return suficiente;
        }
    }
}