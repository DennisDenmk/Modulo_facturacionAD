using Microsoft.EntityFrameworkCore;
using InvoiceManagementAPI.Data;
using InvoiceManagementAPI.Services;
using InvoiceManagementAPI.Middleware;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// 1. FORZAR PUERTO 8080 (CRUCIAL PARA AZURE LINUX)
// ============================================
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ============================================
// 2. CONFIGURACIÓN DE BDD Y ZONA HORARIA
// ============================================
builder.Services.AddScoped<InvoiceDbContext>(sp =>
{
    var optionsBuilder = new DbContextOptionsBuilder<InvoiceDbContext>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    optionsBuilder.UseMySql(connectionString,
        new MySqlServerVersion(new Version(8, 0, 21)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    );

    var context = new InvoiceDbContext(optionsBuilder.Options);

    // Intentar forzar zona horaria de forma segura
    try
    {
        context.Database.OpenConnection();
        context.Database.ExecuteSqlRaw("SET time_zone = '-05:00'");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Advertencia Zona Horaria: {ex.Message}");
    }

    return context;
});

// ============================================
// 3. SERVICIOS DE SEGURIDAD (NUEVO)
// ============================================
builder.Services.AddHttpClient<ISecurityApiService, SecurityApiService>();

// ============================================
// 4. SERVICIOS EXISTENTES
// ============================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();

// ============================================
// 5. SWAGGER CON SEGURIDAD (ACTUALIZADO)
// ============================================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Invoice API",
        Version = "v1",
        Description = "API Azure + Hostinger con Seguridad UTN"
    });

    // 🔒 Configuración de seguridad Bearer Token en Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando Bearer scheme. Ejemplo: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================
// 6. CORS
// ============================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// ============================================
// 7. HTTP CLIENT PARA INVENTARIO
// ============================================
builder.Services.AddHttpClient<IInventoryService, InventoryService>(client =>
{
    client.BaseAddress = new Uri("https://api-inventario.up.railway.app/api/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();

// ============================================
// 8. PIPELINE MIDDLEWARE
// ============================================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Invoice API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");

// ============================================
// 🔒 MIDDLEWARE DE SEGURIDAD
// ============================================
app.UseSecurityValidation(); // ✅ Activado - Tu middleware personalizado maneja todo

app.MapControllers();

// ============================================
// 9. DIAGNÓSTICO DE CONEXIÓN
// ============================================
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();

        if (context.Database.CanConnect())
        {
            Console.WriteLine("✅ [AZURE DIAGNOSTIC] Conexión a Hostinger EXITOSA.");

            var connection = context.Database.GetDbConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT NOW()";

            if (connection.State != System.Data.ConnectionState.Open) connection.Open();

            var fecha = cmd.ExecuteScalar();
            Console.WriteLine($"✅ [AZURE DIAGNOSTIC] Hora Servidor BDD: {fecha}");
        }
        else
        {
            Console.WriteLine("❌ [AZURE DIAGNOSTIC] Falló la conexión.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ [AZURE DIAGNOSTIC] ERROR: {ex.Message}");
    }
}

// ============================================
// 10. DIAGNÓSTICO DE SEGURIDAD
// ============================================
var securityEnabled = app.Configuration.GetValue<bool>("Security:EnableSecurity", false);
Console.WriteLine($"🔒 [SECURITY] Estado: {(securityEnabled ? "ACTIVADA" : "DESACTIVADA")}");
Console.WriteLine($"🔒 [SECURITY] Módulo ID: {app.Configuration.GetValue<int>("Security:ModuloId", 0)}");

app.Run();