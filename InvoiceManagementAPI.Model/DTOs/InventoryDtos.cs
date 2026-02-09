using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Globalization;

namespace InvoiceManagementAPI.Model.DTOs
{
    public class InventoryResponse
    {
        [JsonPropertyName("data")]
        public List<ProductDto> Data { get; set; }

        [JsonPropertyName("current_page")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("last_page")]
        public int LastPage { get; set; }

        [JsonPropertyName("per_page")]
        public int PerPage { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("from")]
        public int? From { get; set; }

        [JsonPropertyName("to")]
        public int? To { get; set; }
    }

    public class ProductDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("codigo")]
        public string Codigo { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; }

        [JsonPropertyName("pvp")]
        public string PvpString { get; set; }

        [JsonPropertyName("stock")]
        public int Stock { get; set; }

        [JsonPropertyName("impuesto")]
        public ImpuestoDto Impuesto { get; set; }

        [JsonIgnore]
        public decimal PrecioVenta
        {
            get
            {
                if (decimal.TryParse(PvpString, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                    return result;
                return 0;
            }
        }
    }

    public class ImpuestoDto
    {
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