using Azure.Data.Tables;
using Devlooped;
using Devlooped.CloudActors;
using Microsoft.AspNetCore.Mvc;
using TestDomain;

var builder = WebApplication.CreateBuilder(args);

builder.UseOrleans(silo =>
{
    if (builder.Environment.IsProduction())
    {
        silo.UseAzureStorageClustering(options => options.TableServiceClient = new TableServiceClient(
                builder.Configuration["App:Storage"] ??
                builder.Configuration["AzureWebJobsStorage"] ??
                throw new InvalidOperationException("Missing either App:Storage or AzureWebJobsStorage connection strings."))); ;

        silo.AddAzureTableGrainStorageAsDefault(options => options.TableServiceClient = new TableServiceClient(
            builder.Configuration["App:Storage"] ??
            builder.Configuration["AzureWebJobsStorage"] ??
            throw new InvalidOperationException("Missing either App:Storage or AzureWebJobsStorage connection strings.")));
    }
    else
    {
        silo.UseDashboard(options => options.HostSelf = false);
        silo.UseLocalhostClustering();
        silo.AddAzureTableGrainStorageAsDefault(options => options.TableServiceClient = 
            CloudStorageAccount.DevelopmentStorageAccount.CreateTableServiceClient());
    }

    //silo.AddStreamstoneActorStorage(opt => opt.AutoSnapshot = true);
});

builder.Services.AddSingleton(CloudStorageAccount.DevelopmentStorageAccount);
builder.Services.AddCloudActors();

var app = builder.Build();

app.MapPost("/account", async (IActorBus bus) =>
{
    var id = Guid.CreateVersion7().ToString("N");
    return Results.Created($"/account/{id}", new { id });
});

app.MapGet("/account/{id}", async (string id, IActorBus bus) =>
{
    var balance = await bus.QueryAsync($"account/{id.Trim('"')}", new GetBalance());
    return Results.Ok(balance);
});

app.MapPost("/account/{id}", async (string id, [FromBody] decimal amount, IActorBus bus) =>
{
    id = id.Trim('"');
    if (amount > 0)
        await bus.ExecuteAsync($"account/{id}", new Deposit(amount));
    else if (amount < 0)
        await bus.ExecuteAsync($"account/{id}", new Withdraw(-amount));

    var balance = await bus.QueryAsync($"account/{id}", new GetBalance());
    return Results.Ok(balance);
});

app.MapDelete("/account/{id}", async (string id, IActorBus bus) =>
{
    var balance = await bus.ExecuteAsync($"account/{id.Trim('"')}", new Close(CloseReason.Customer));
    return Results.Ok(balance);
});

app.Map("/orleans", x => x.UseOrleansDashboard());

app.Run();
