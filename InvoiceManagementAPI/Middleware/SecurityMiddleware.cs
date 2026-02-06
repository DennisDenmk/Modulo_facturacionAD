using InvoiceManagementAPI.Services;

namespace InvoiceManagementAPI.Middleware
{
    public class SecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityMiddleware> _logger;

        public SecurityMiddleware(RequestDelegate next, ILogger<SecurityMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ISecurityApiService securityService, IConfiguration configuration)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            var method = context.Request.Method.ToUpper();

            // ============================================
            // 1. RUTAS SIEMPRE PÚBLICAS (todos los métodos)
            // ============================================
            var alwaysPublicPaths = new[]
            {
                "/api/auth/login",
                "/api/auth/validate-token",
                "/swagger"
            };

            if (alwaysPublicPaths.Any(p => path.StartsWith(p.ToLower())))
            {
                _logger.LogInformation($"✅ Ruta pública permitida: {method} {path}");
                await _next(context);
                return;
            }

            // ============================================
            // 2. ENDPOINTS PÚBLICOS SOLO PARA GET
            // ============================================
            if (method == "GET")
            {
                var publicGetEndpoints = new[]
                {
                    "/api/clientes",        // 👈 Incluye /api/clientes y /api/clientes/{id}
                    "/api/facturas",
                };

                if (publicGetEndpoints.Any(p => path.StartsWith(p.ToLower())))
                {
                    _logger.LogInformation($"✅ GET público permitido: {path}");
                    await _next(context);
                    return;
                }
            }

            // ============================================
            // 3. RESTO DE MÉTODOS REQUIEREN AUTENTICACIÓN
            // ============================================
            _logger.LogInformation($"🔒 Validando token para: {method} {path}");

            // Obtener el token del header Authorization
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning($"❌ Token no proporcionado para: {method} {path}");
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Token de autenticación requerido"
                });
                return;
            }

            // Validar el token con la API de seguridad
            var moduloId = configuration.GetValue<int>("Security:ModuloId", 5);
            var isValid = await securityService.ValidarTokenAsync(token, moduloId);

            if (!isValid)
            {
                _logger.LogWarning($"❌ Token inválido para: {method} {path}");
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Token inválido o expirado"
                });
                return;
            }

            // Token válido, continuar con la petición
            _logger.LogInformation($"✅ Token válido. Permitiendo acceso a: {method} {path}");
            await _next(context);
        }
    }

    // Extension method para agregar el middleware fácilmente
    public static class SecurityMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityMiddleware>();
        }
    }
}