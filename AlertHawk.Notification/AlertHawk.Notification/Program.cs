using AlertHawk.Notification.Domain.Classes;
using AlertHawk.Notification.Domain.Interfaces.Repositories;
using AlertHawk.Notification.Domain.Interfaces.Services;
using AlertHawk.Notification.Infrastructure.Repositories.Class;
using EasyMemoryCache.Configuration;
using System.Diagnostics.CodeAnalysis;

[assembly: ExcludeFromCodeCoverage]
var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true, false)
    .AddEnvironmentVariables()
    .Build();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DI
builder.Services.AddEasyCache(configuration.GetSection("CacheSettings").Get<CacheSettings>());

builder.Services.AddTransient<INotificationTypeService, NotificationTypeService>();
builder.Services.AddTransient<INotificationTypeRepository, NotificationTypeRepository>();

builder.WebHost.UseSentry();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseSentryTracing();

app.MapControllers();

app.Run();