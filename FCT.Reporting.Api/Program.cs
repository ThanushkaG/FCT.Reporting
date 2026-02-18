using Azure.Identity;
using Azure.Storage.Blobs;
using FCT.Reporting.Api;
using FCT.Reporting.Api.Hubs;
using FCT.Reporting.Application.Abstractions;
using FCT.Reporting.Application.Reports.Commands;
using FCT.Reporting.Infrastructure.Messaging;
using FCT.Reporting.Infrastructure.Persistence;
using FCT.Reporting.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using RabbitMQ.Client;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using FCT.Reporting.Infrastructure.Repository;

var builder = WebApplication.CreateBuilder(args);

var kvName = builder.Configuration["KeyVault:Name"]; 
if (!string.IsNullOrEmpty(kvName))
{
    var kvUri = new Uri($"https://{kvName}.vault.azure.net/");
    builder.Configuration.AddAzureKeyVault(kvUri, new DefaultAzureCredential());
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Entra ID
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));


//Authorization

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Reports.Generate",
        policy => policy.RequireScope("Reports.Generate"));

    options.AddPolicy("Reports.Read",
        policy => policy.RequireScope("Reports.Read"));
});


// CORS

builder.Services.AddCors(o =>
{
    o.AddPolicy("spa", p =>
        p.WithOrigins("http://localhost:4200")
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());
});


//  Database (from Key Vault)

builder.Services.AddDbContext<ReportingDbContext>(opt =>
{
    var cs = builder.Configuration["Sql:ConnectionString"];
    opt.UseSqlServer(cs);
});


// MediatR

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<CreateReportJobCommand>());


// DI

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddTransient<IMessagePublisher, OutboxMessagePublisher>();

// Repositories

builder.Services.AddScoped<IReportJobRepository, ReportJobRepository>();


// Blob Storage (from Key Vault)

builder.Services.AddSingleton(_ =>
{
    var cs = builder.Configuration["Blob:ConnectionString"];
    return new BlobServiceClient(cs);
});

builder.Services.AddSingleton<IFileStorage>(sp =>
    new AzureBlobFileStorage(
        sp.GetRequiredService<BlobServiceClient>(),
        builder.Configuration["Blob:Container"]!
    ));


// RabbitMQ (from Key Vault)

builder.Services.AddSingleton<IConnectionFactory>(_ =>
{
    var uri = builder.Configuration["Rabbit:Uri"];

    return new ConnectionFactory
    {
        Uri = new Uri(uri)
    };
});

builder.Services.AddSingleton<IRabbitPublisher, RabbitMqClientPublisher>();


// Notification publisher

builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationPublisher, SignalRNotificationPublisher>();


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty; 
});

app.UseCors("spa");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ReportsHub>("/hubs/reports");

// Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
    db.Database.Migrate();
}

app.Run();
