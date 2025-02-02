using Microsoft.EntityFrameworkCore;
using PaypalApi.Context;
using PaypalApi.Services;
using Auth.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddRouting(options => options.LowercaseUrls = true);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("Connection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// En Program.cs
builder.Services.AddScoped<IPaymentProcessorService, PaymentProcessorService>();
// En Program.cs
builder.Services.AddScoped<IAuthService, AuthService>();



// Configuración del logging
builder.Logging
    .ClearProviders()
    .AddConsole()
    .SetMinimumLevel(LogLevel.Debug);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        options.JsonSerializerOptions.WriteIndented = true;
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware personalizado para webhooks de Stripe
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/webhook/payments"))
    {
        context.Request.EnableBuffering();
    }
    await next();
});

app.UseHttpsRedirection();
app.UseCors();
app.UseRouting();
app.MapControllers();

app.Run();

