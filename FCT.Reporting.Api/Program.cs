using FCT.Reporting.Application.Abstractions;
using FCT.Reporting.Application.Reports.Commands;
using FCT.Reporting.Infrastructure.Messaging;
using FCT.Reporting.Infrastructure.Persistence;
using FCT.Reporting.Infrastructure.Security;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Entra ID
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Scope policies (scp is space-separated)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Reports.Generate", policy =>
        policy.RequireAssertion(ctx =>
        {
            var scp = ctx.User.FindFirst("scp")?.Value ?? "";
            return scp.Split(' ').Contains("Reports.Generate");
        }));

    options.AddPolicy("Reports.Read", policy =>
        policy.RequireAssertion(ctx =>
        {
            var scp = ctx.User.FindFirst("scp")?.Value ?? "";
            return scp.Split(' ').Contains("Reports.Read");
        }));
});

// CORS for Angular
builder.Services.AddCors(o =>
{
    o.AddPolicy("spa", p =>
        p.WithOrigins("http://localhost:4200")
         .AllowAnyHeader()
         .AllowAnyMethod());
});

// EF Core 10
builder.Services.AddDbContext<ReportingDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Sql")));

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateReportJobCommand>());

// Current user
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// Repos + ports
builder.Services.AddScoped<IReportJobRepository, ReportJobRepository>();

// Blob
builder.Services.AddSingleton(_ => new BlobServiceClient(builder.Configuration.GetConnectionString("Blob")));
builder.Services.AddSingleton<IFileStorage>(sp =>
    new AzureBlobFileStorage(sp.GetRequiredService<BlobServiceClient>(), builder.Configuration["Blob:Container"]!));

// RabbitMQ (dispatcher publish)
builder.Services.AddSingleton<IConnectionFactory>(_ =>
    new ConnectionFactory
    {
        HostName = builder.Configuration["Rabbit:Host"],
        UserName = builder.Configuration["Rabbit:User"],
        Password = builder.Configuration["Rabbit:Pass"]
    });

builder.Services.AddSingleton<IRabbitPublisher, RabbitMqClientPublisher>();

// Outbox writer + dispatcher
builder.Services.AddScoped<IMessagePublisher, OutboxMessagePublisher>();
builder.Services.AddHostedService<OutboxDispatcherHostedService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("spa");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();