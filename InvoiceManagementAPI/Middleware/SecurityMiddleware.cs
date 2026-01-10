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
            // Rutas públicas que NO requieren autenticación
            var publicPaths = new[]
            {
                "/api/auth/login",
                "/swagger",
                "/api/auth/validate-token"
            };

            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Si es una ruta pública, continuar sin validar
            if (publicPaths.Any(p => path.StartsWith(p.ToLower())))
            {
                await _next(context);
                return;
            }

            // Obtener el token del header Authorization
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (string.IsNullOrWhiteSpace(token))
            {
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
            var moduloId = configuration.GetValue<int>("Security:ModuloId", 2);
            var isValid = await securityService.ValidarTokenAsync(token, moduloId);

            if (!isValid)
            {
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