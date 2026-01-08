using Microsoft.EntityFrameworkCore;
using InvoiceManagementAPI.Data;
using InvoiceManagementAPI.Services;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// CONFIGURACIÓN DE LA BASE DE DATOS
// ============================================
builder.Services.AddDbContext<InvoiceDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseMySql(connectionString,
        new MySqlServerVersion(new Version(8, 0, 21)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    );
});

// ============================================
// ✅ CONFIGURAR ZONA HORARIA GLOBALMENTE
// ============================================
builder.Services.AddScoped<InvoiceDbContext>(sp =>
{
    var optionsBuilder = new DbContextOptionsBuilder<InvoiceDbContext>();
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    optionsBuilder.UseMySql(connectionString,
        new MySqlServerVersion(new Version(8, 0, 21)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    );

    var context = new InvoiceDbContext(optionsBuilder.Options);

    // Establecer zona horaria cuando se crea el contexto
    try
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            connection.Open();
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SET time_zone = '-05:00'";
        cmd.ExecuteNonQuery();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ No se pudo establecer zona horaria: {ex.Message}");
    }

    return context;
});

// ============================================
// CONFIGURACIÓN DE CONTROLADORES Y SWAGGER
// ============================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configurar para manejar referencias circulares
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
        Description = "API para gestión de facturación"
    });
});

// ============================================
// CONFIGURACIÓN DE CORS (si necesitas consumir desde frontend)
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
    // Pon aquí la URL base de la API externa (ej: localhost o servidor real)
    client.BaseAddress = new Uri("https://api-inventario.up.railway.app/api/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();

// ============================================
// MIDDLEWARE PIPELINE
// ============================================
// Habilitar Swagger en desarrollo
//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Invoice Management API v1");
        c.RoutePrefix = "swagger";
    });
}

// app.UseHttpsRedirection();

// Habilitar CORS
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// ============================================
// VERIFICAR CONEXIÓN A LA BASE DE DATOS Y ZONA HORARIA
// ============================================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
    try
    {
        // Intenta conectar a la base de datos
        if (context.Database.CanConnect())
        {
            Console.WriteLine("✅ Conexión exitosa a la base de datos MySQL");

            // Verificar zona horaria
            var connection = context.Database.GetDbConnection();

            // ✅ CORREGIDO: Verificar si ya está abierta antes de abrir
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT @@session.time_zone, NOW()";
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                Console.WriteLine($"✅ Zona horaria configurada: {reader.GetString(0)}");
                Console.WriteLine($"✅ Hora actual del servidor: {reader.GetDateTime(1)}");
            }
        }
        else
        {
            Console.WriteLine("❌ No se pudo conectar a la base de datos");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error al conectar a la base de datos: {ex.Message}");
    }
}

app.Run();