using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Globalization; // Importante para convertir "15.00" a decimal

namespace InvoiceManagementAPI.Model.DTOs
{
    public class InventoryResponse
    {
        [JsonPropertyName("data")]
        public List<ProductDto> Data { get; set; }

        // Campos para paginación (si la API los envía en la raíz)
        [JsonPropertyName("last_page")]
        public int LastPage { get; set; }

        [JsonPropertyName("current_page")]
        public int CurrentPage { get; set; }
    }

    public class ProductDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("codigo")]
        public string Codigo { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; }

        // EL PROBLEMA REAL: La API envía "84.00" (String)
        [JsonPropertyName("pvp")]
        public string PvpString { get; set; }

        [JsonPropertyName("stock")]
        public int Stock { get; set; }

        [JsonPropertyName("impuesto")]
        public ImpuestoDto Impuesto { get; set; }

        // PROPIEDAD CALCULADA: Convierte el string a decimal
        [JsonIgnore]
        public decimal PrecioVenta
        {
            get
            {
                // Usamos CultureInfo.InvariantCulture para asegurar que el punto "." se lea como decimal
                if (decimal.TryParse(PvpString, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                    return result;
                return 0;
            }
        }
    }

    public class ImpuestoDto
    {
        // EL PROBLEMA REAL: La API envía "15.00" (String)
        [JsonPropertyName("porcentaje")]
        public string PorcentajeString { get; set; }

        [JsonIgnore]
        public decimal Porcentaje
        {
            get
            {
                if (decimal.TryParse(PorcentajeString, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                    return result;
                return 0;
            }
        }
    }
}