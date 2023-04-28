using System.Data;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using ChatApplication.Configuration;
using ChatApplication.Serializers.Implementations;
using ChatApplication.ServiceBus;
using ChatApplication.ServiceBus.Interfaces;
using ChatApplication.Services;
using ChatApplication.Storage;
using ChatApplication.Storage.SQL;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



builder.Services.Configure<CosmosSettings>(builder.Configuration.GetSection("Cosmos"));
builder.Services.Configure<BlobSettings>(builder.Configuration.GetSection("BlobStorage"));
builder.Services.Configure<SQLSettings>(builder.Configuration.GetSection("SQL"));
builder.Services.Configure<ServiceBusSettings>(builder.Configuration.GetSection("ServiceBus"));

builder.Services.AddSingleton<IImageStore, BlobImageStore>();

builder.Services.AddSingleton(sp =>
{
    var blobOptions = sp.GetRequiredService<IOptions<BlobSettings>>();
    return new BlobContainerClient(blobOptions.Value.ConnectionString, blobOptions.Value.ContainerName);
});

// builder.Services.AddSingleton<IMessageStore, CosmosMessageStore>();
// builder.Services.AddSingleton<IConversationStore, CosmosConversationStore>();
// builder.Services.AddSingleton<IProfileStore, CosmosProfileStore>();
builder.Services.AddSingleton<IMessageStore,SQLMessageStore>();
builder.Services.AddSingleton<IConversationStore, SQLConversationStore>();
builder.Services.AddSingleton<IProfileStore, SQLProfileStore>();


builder.Services.AddSingleton(sp =>
{
    var cosmosOptions = sp.GetRequiredService<IOptions<CosmosSettings>>();
    return new CosmosClient(cosmosOptions.Value.ConnectionString);
});

builder.Services.AddSingleton<IImageService, ImageService>();
builder.Services.AddSingleton<IProfileService, ProfileService>();
builder.Services.AddSingleton<IConversationService, ConversationService>();


builder.Services.AddSingleton(sp =>
{
    var serviceBusOptions = sp.GetRequiredService<IOptions<ServiceBusSettings>>();
    return new ServiceBusClient(serviceBusOptions.Value.ConnectionString);
});


builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IOptions<SQLSettings>>();
    var connectionString = configuration.Value.ConnectionString;
    return new SqlConnection(connectionString);
});

builder.Services.AddSingleton<IAddMessageServiceBusPublisher, AddMessageServiceBusPublisher>();
builder.Services.AddSingleton<IStartConversationServiceBusPublisher, StartConversationServiceBusPublisher>();
builder.Services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();    
builder.Services.AddSingleton<IStartConversationParametersSerializer, JsonStartConversationParametersSerializer>();    
builder.Services.AddHostedService<AddMessageHostedService>();
builder.Services.AddHostedService<StartConversationHostedService>();


builder.Services.AddApplicationInsightsTelemetry();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }