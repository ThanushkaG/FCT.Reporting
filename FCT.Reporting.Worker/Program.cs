using Azure.Identity;
using Azure.Storage.Blobs;
using FCT.Reporting.Application.Abstractions;
using FCT.Reporting.Infrastructure.Messaging;
using FCT.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, config) =>
    {
        // Load appsettings.json
        var built = config.Build();
        var kvName = built["KeyVault:Name"];
        if (!string.IsNullOrEmpty(kvName))
        {
            var kvUri = new Uri($"https://{kvName}.vault.azure.net/");
            // Use DefaultAzureCredential 
            config.AddAzureKeyVault(kvUri, new DefaultAzureCredential());
        }
    })
    .ConfigureServices((context, services) =>
    {
        // Db 
        var sqlCs = context.Configuration["Sql:ConnectionString"];
        if (string.IsNullOrWhiteSpace(sqlCs))
            throw new InvalidOperationException("Missing Sql:ConnectionString. Store secret 'Sql--ConnectionString' in Key Vault or provide Sql:ConnectionString in configuration.");

        services.AddDbContext<ReportingDbContext>(opt =>
            opt.UseSqlServer(sqlCs));

        // Blob 
        var blobCs = context.Configuration["Blob:ConnectionString"];
        if (string.IsNullOrWhiteSpace(blobCs))
            throw new InvalidOperationException("Missing Blob:ConnectionString. Store secret 'Blob--ConnectionString' in Key Vault or provide Blob:ConnectionString in configuration.");

        services.AddSingleton(_ => new BlobServiceClient(blobCs));
        services.AddSingleton<IFileStorage>(sp =>
            new AzureBlobFileStorage(sp.GetRequiredService<BlobServiceClient>(), context.Configuration["Blob:Container"]!));

        // Rabbit
        var rabbitUri = context.Configuration["Rabbit:Uri"];
        if (string.IsNullOrWhiteSpace(rabbitUri))
            throw new InvalidOperationException("Missing Rabbit:Uri configuration. Set secret 'Rabbit--Uri' in Key Vault or provide Rabbit:Uri in configuration.");

        services.AddSingleton<global::RabbitMQ.Client.IConnectionFactory>(_ =>
            new global::RabbitMQ.Client.ConnectionFactory { Uri = new Uri(rabbitUri) });

       services.AddSingleton<IRabbitPublisher, RabbitMqClientPublisher>();

        // Notification publisher
        services.AddHttpClient<INotificationPublisher, ApiNotificationPublisher>(client =>
        {
            client.BaseAddress = new Uri(context.Configuration["Api:BaseUrl"] ?? "http://localhost:5000");
        });

        // Hosted services
        services.AddHostedService<FCT.Reporting.Worker.ReportWorker>();
    })
    .Build();

await host.RunAsync();
