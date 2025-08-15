using System.Text.Json;

using Rinha.Api.Models.Payments;
using Rinha.Api.Repositories;
using Rinha.Api.Services;
using Rinha.Api.Workers;

[module: DapperAot]

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddProblemDetails();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var redisConnection = builder.Configuration.GetConnectionString("RedisCache")
    ?? throw new InvalidOperationException("Connection Strings for RedisCache invalid or nullable.");

var redis = ConnectionMultiplexer.Connect(redisConnection);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
builder.Services.AddSingleton<PaymentCacheRepository>();

builder.Services.AddSingleton<IDistributedLockFactory>(_ =>
{
    var multiplexer = new List<RedLockMultiplexer> { redis };
    return RedLockFactory.Create(multiplexer);
});

builder.Services.AddHttpClient(nameof(PaymentGateway.Default), client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PaymentProcessorDefault"]!);
}).ConfigurePrimaryHttpMessageHandler(Configuration.GetSocketHandler());

builder.Services.AddHttpClient(nameof(PaymentGateway.Fallback), client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PaymentProcessorFallback"]!);
}).ConfigurePrimaryHttpMessageHandler(Configuration.GetSocketHandler());

builder.Services.AddSingleton<HealthChecker>();
builder.Services.AddSingleton<MessageQueue<PaymentRequest>>();
builder.Services.AddSingleton<MessageQueue<Payment>>();

var connectionStrings = builder.Configuration.GetConnectionString("PostgreSQL")
    ?? throw new InvalidOperationException("Database connection string wasn't configured.");

builder.Services.AddNpgsqlDataSource(connectionStrings);

builder.Services.AddScoped<PaymentService>();
builder.Services.AddSingleton<PaymentClient>();
builder.Services.AddScoped<PaymentRepository>();

builder.Services.AddHostedService<HealthWorker>();
builder.Services.AddHostedService<PaymentProcessorWorker>();
builder.Services.AddHostedService<PaymentBatchedInsertWorker>();
ThreadPool.SetMinThreads(64, 64);

var app = builder.Build();

app.UseExceptionHandler();

app.MapPost("payments", async (
    MessageQueue<PaymentRequest> requestQueue,
    PaymentRequest request) =>
{
    await requestQueue.EnqueueAsync(request);

    return Results.Ok();
});

app.MapGet("payments-summary", async (
    PaymentService paymentService,
    DateTimeOffset? from,
    DateTimeOffset? to) =>
{
    try
    {
        var result = await paymentService.GetSummaryAsync(from, to);
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.BadRequest();
    }
});

await app.RunAsync();
