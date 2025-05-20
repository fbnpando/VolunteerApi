using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VolunteerApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configura la conexi�n a la base de datos
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configura opciones (ejemplo: GoogleAI)
builder.Services.Configure<GoogleAIOptions>(builder.Configuration.GetSection("GoogleAI"));

// Agrega servicios de controladores y configuraci�n JSON limpia (sin referencias circulares)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = null; // sin Preserve
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Registra Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Agrega HttpClientFactory
builder.Services.AddHttpClient();

// Logging
builder.Services.AddLogging();

// Configura CORS para permitir todo (�til en desarrollo)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configuraci�n para entorno de desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Deshabilitar la redirecci�n a HTTPS para desarrollo local
// Si no quieres usar HTTPS en desarrollo, puedes eliminar esta l�nea
// app.UseHttpsRedirection(); // Comenta o elimina esta l�nea

// Middleware
app.UseCors("AllowAllOrigins");

app.UseRouting();
app.UseAuthorization();

// Mapear los controladores
app.MapControllers();

// Ejecuta la aplicaci�n
app.Run();
