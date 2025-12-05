using BE.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// CORS for FE dev host
var corsPolicy = "_allowFE";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("*"); // Expose tất cả headers để frontend có thể đọc được
    });
});

// Add services to the container.
builder.Services.AddDbContext<AppBookStoreContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Giữ nguyên tên property
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Bookly API",
        Version = "v1",
        Description = "API for Bookly Bookstore"
    });
    
    // Ignore circular references
    c.CustomSchemaIds(type => type.FullName);
    
    // Handle DateTime serialization
    c.MapType<DateTime>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "date-time"
    });
    
    // Ignore properties that might cause issues
    c.IgnoreObsoleteActions();
    c.IgnoreObsoleteProperties();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bookly API v1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
    });
}

// CORS phải được đặt trước UseHttpsRedirection để xử lý preflight requests
app.UseCors(corsPolicy);
app.UseHttpsRedirection();
app.UseAuthorization();

// Exception handling middleware - đặt sau các middleware khác nhưng trước MapControllers
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        // Đảm bảo response chưa được gửi
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
            {
                success = false,
                message = "Internal server error",
                error = app.Environment.IsDevelopment() ? ex.Message : "An error occurred",
                stackTrace = app.Environment.IsDevelopment() ? ex.StackTrace : null
            }));
        }
    }
});

app.MapControllers();
app.Run();
