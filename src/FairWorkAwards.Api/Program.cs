using Microsoft.EntityFrameworkCore;
using FairWorkAwards.Application.Interfaces;
using FairWorkAwards.Infrastructure.Services;
using FairWorkAwards.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Fair Work Awards API",
        Version = "v1",
        Description = "API for managing Fair Work Awards, pay rates, and rule calculations"
    });
});

// Configure Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=FairWorkAwards;Trusted_Connection=True;MultipleActiveResultSets=true";

builder.Services.AddDbContext<FairWorkAwardsDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register application services
builder.Services.AddScoped<IRuleBuilderService, RuleBuilderService>();
builder.Services.AddScoped<IRuleEngineService, RuleEngineService>();

// Add CORS for development
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowAll");
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
