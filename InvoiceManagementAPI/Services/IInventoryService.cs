// Ubicación: InvoiceManagementAPI/Services/IInventoryService.cs
using System.Threading.Tasks;
using InvoiceManagementAPI.Model.DTOs; // Referencia al otro proyecto

namespace InvoiceManagementAPI.Services
{
    public interface IInventoryService
    {
        Task<ProductDto> ObtenerProductoPorCodigo(string codigo);
        Task<bool> VerificarStock(string codigo, int cantidadRequerida);
    }
}