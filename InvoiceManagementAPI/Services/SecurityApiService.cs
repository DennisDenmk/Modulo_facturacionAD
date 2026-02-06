using InvoiceManagementAPI.Model.Security;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace InvoiceManagementAPI.Services
{
    public interface ISecurityApiService
    {
        Task<ApiResponse<LoginData>> LoginAsync(LoginRequest request);
        Task<ApiResponse<MisFuncionesData>> GetMisFuncionesAsync(int moduloId, string token);
        Task<ApiResponse<AuditoriaData>> RegistrarAuditoriaAsync(AuditoriaRequest request, string token);
        Task<bool> ValidarTokenAsync(string token, int moduloId);
    }

    public class SecurityApiService : ISecurityApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SecurityApiService> _logger;
        private const string BASE_URL = "https://98.84.67.160.nip.io";

        public SecurityApiService(HttpClient httpClient, ILogger<SecurityApiService> logger)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(BASE_URL);
            _logger = logger;
        }

        public async Task<ApiResponse<LoginData>> LoginAsync(LoginRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/login", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Login falló: {response.StatusCode} - {responseContent}");
                    return new ApiResponse<LoginData>
                    {
                        Success = false,
                        Message = $"Error en autenticación: {responseContent}"
                    };
                }

                var result = JsonSerializer.Deserialize<ApiResponse<LoginData>>(responseContent);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en LoginAsync");
                return new ApiResponse<LoginData>
                {
                    Success = false,
                    Message = $"Error de conexión: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<MisFuncionesData>> GetMisFuncionesAsync(int moduloId, string token)
        {
            try
            {
                var request = new MisFuncionesRequest { ModuloId = moduloId };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/mis-funciones")
                {
                    Content = content
                };
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"GetMisFunciones falló: {response.StatusCode}");
                    return new ApiResponse<MisFuncionesData>
                    {
                        Success = false,
                        Message = "Token inválido o expirado"
                    };
                }

                return JsonSerializer.Deserialize<ApiResponse<MisFuncionesData>>(responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetMisFuncionesAsync");
                return new ApiResponse<MisFuncionesData>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ApiResponse<AuditoriaData>> RegistrarAuditoriaAsync(AuditoriaRequest request, string token)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auditoria")
                {
                    Content = content
                };
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"RegistrarAuditoria falló: {response.StatusCode}");
                    return new ApiResponse<AuditoriaData>
                    {
                        Success = false,
                        Message = "Error al registrar auditoría"
                    };
                }

                return JsonSerializer.Deserialize<ApiResponse<AuditoriaData>>(responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en RegistrarAuditoriaAsync");
                return new ApiResponse<AuditoriaData>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<bool> ValidarTokenAsync(string token, int moduloId)
        {
            var result = await GetMisFuncionesAsync(moduloId, token);
            return result?.Success == true;
        }
    }
}