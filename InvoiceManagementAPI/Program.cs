using Microsoft.EntityFrameworkCore;
using InvoiceManagementAPI.Data;
using InvoiceManagementAPI.Services;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// 1. FORZAR PUERTO 8080 (CRUCIAL PARA AZURE LINUX)
// ============================================
// Azure manda el tráfico al puerto 8080. Si escuchamos en el 5000, da error.
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
// 3. SERVICIOS
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
        Title = "Invoice API",
        Version = "v1",
        Description = "API Azure + Hostinger"
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddHttpClient<IInventoryService, InventoryService>(client =>
{
    client.BaseAddress = new Uri("https://api-inventario.up.railway.app/api/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();

// ============================================
// 4. PIPELINE
// ============================================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Invoice API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// ============================================
// 5. DIAGNÓSTICO CORREGIDO
// ============================================
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();

        if (context.Database.CanConnect())
        {
            Console.WriteLine("✅ [AZURE DIAGNOSTIC] Conexión a Hostinger EXITOSA.");

            // Usamos ADO.NET puro para evitar el error de Entity Framework
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

app.Run();