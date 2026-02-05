using System.Text.Json.Serialization;

namespace InvoiceManagementAPI.Model.Security
{
    // ==========================================
    // MODELOS DE REQUEST (Lo que enviamos)
    // ==========================================

    public class LoginRequest
    {
        [JsonPropertyName("email_username")]
        public string EmailUsername { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("modulo_id")]
        public int ModuloId { get; set; }
    }

    public class MisFuncionesRequest
    {
        [JsonPropertyName("modulo_id")]
        public int ModuloId { get; set; }
    }

    public class AuditoriaRequest
    {
        [JsonPropertyName("id_funcion")]
        public int IdFuncion { get; set; }

        [JsonPropertyName("accion")]
        public string Accion { get; set; }

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; }

        [JsonPropertyName("observacion")]
        public string Observacion { get; set; }

        [JsonPropertyName("ip_usuario")]
        public string IpUsuario { get; set; }
    }

    // ==========================================
    // MODELOS DE RESPONSE (Lo que recibimos)
    // ==========================================

    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public T Data { get; set; }
    }

    public class LoginData
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("user")]
        public UserInfo User { get; set; }

        [JsonPropertyName("modulo")]
        public ModuloInfo Modulo { get; set; }
    }

    public class UserInfo
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("dni")]
        public string Dni { get; set; }
    }

    public class ModuloInfo
    {
        [JsonPropertyName("modulo_id")]
        public int ModuloId { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; }

        [JsonPropertyName("funciones")]
        public List<FuncionInfo> Funciones { get; set; }
    }

    public class FuncionInfo
    {
        [JsonPropertyName("funcion_id")]
        public int FuncionId { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; }
    }

    public class MisFuncionesData
    {
        [JsonPropertyName("usuario")]
        public UsuarioBasico Usuario { get; set; }

        [JsonPropertyName("modulo")]
        public ModuloInfo Modulo { get; set; }
    }

    public class UsuarioBasico
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }
    }

    public class AuditoriaData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("accion")]
        public string Accion { get; set; }

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}