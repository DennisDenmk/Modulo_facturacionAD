using Microsoft.EntityFrameworkCore;
using InvoiceManagementAPI.Data;
using InvoiceManagementAPI.Services;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// 1. CONFIGURACIÓN DE LA BASE DE DATOS Y ZONA HORARIA
// ============================================
// Usamos AddScoped con una fábrica para configurar la zona horaria en cada conexión
builder.Services.AddScoped<InvoiceDbContext>(sp =>
{
    var optionsBuilder = new DbContextOptionsBuilder<InvoiceDbContext>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    // Configuración estándar de MySQL
    optionsBuilder.UseMySql(connectionString,
        new MySqlServerVersion(new Version(8, 0, 21)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    );

    var context = new InvoiceDbContext(optionsBuilder.Options);

    // ✅ FORZAR ZONA HORARIA DE FORMA SEGURA (-05:00 Ecuador/Colombia)
    // Esto se ejecuta cada vez que se solicita el contexto
    try
    {
        context.Database.OpenConnection(); // Abrimos conexión explícitamente
        context.Database.ExecuteSqlRaw("SET time_zone = '-05:00'");
    }
    catch (Exception ex)
    {
        // Logueamos el error pero no detenemos la app, por si es un fallo transitorio
        Console.WriteLine($"⚠️ Advertencia: No se pudo establecer la zona horaria MySQL: {ex.Message}");
    }

    return context;
});

// ============================================
// 2. SERVICIOS Y SWAGGER
// ============================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Invoice Management API",
        Version = "v1",
        Description = "API desplegada en Azure con MySQL Hostinger"
    });
});

// ============================================
// 3. CORS Y CLIENTES HTTP
// ============================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddHttpClient<IInventoryService, InventoryService>(client =>
{
    client.BaseAddress = new Uri("https://api-inventario.up.railway.app/api/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();

// ============================================
// 4. PIPELINE DE LA APLICACIÓN
// ============================================

// Habilitar Swagger SIEMPRE (incluso en producción para Azure)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Invoice Management API v1");
    c.RoutePrefix = "swagger"; // Accesible en /swagger
});

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// ============================================
// 5. DIAGNÓSTICO DE INICIO (Crucial para Azure)
// ============================================
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();

        // Verificación simple de conexión
        if (context.Database.CanConnect())
        {
            Console.WriteLine("✅ [AZURE DIAGNOSTIC] Conexión a Hostinger EXITOSA.");

            // Verificar que la hora sea la correcta (-05:00)
            var fechaDb = await context.Database.SqlQueryRaw<DateTime>("SELECT NOW()").FirstOrDefaultAsync();
            Console.WriteLine($"✅ [AZURE DIAGNOSTIC] Hora en BD: {fechaDb}");
        }
        else
        {
            Console.WriteLine("❌ [AZURE DIAGNOSTIC] No se pudo conectar a la base de datos.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ [AZURE DIAGNOSTIC] CRITICAL ERROR: {ex.Message}");
        if (ex.InnerException != null)
            Console.WriteLine($"   --> Inner: {ex.InnerException.Message}");
    }
}

app.Run();