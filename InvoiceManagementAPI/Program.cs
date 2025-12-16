using Microsoft.EntityFrameworkCore;
using InvoiceManagementAPI.Data;

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

var app = builder.Build();

// ============================================
// MIDDLEWARE PIPELINE
// ============================================

// Habilitar Swagger en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Invoice Management API v1");
        c.RoutePrefix = "swagger"; 
    });
}

app.UseHttpsRedirection();

// Habilitar CORS
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// ============================================
// VERIFICAR CONEXIÓN A LA BASE DE DATOS
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