using System.Text.Json;

using Dapper;

using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;

using Rinha.Api.Repositories;
using Rinha.Api.Services;
using Rinha.Api.Workers;

using StackExchange.Redis;

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
builder.Services.AddSingleton<CacheService>();

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

var connectionStrings = builder.Configuration.GetConnectionString("PostgreSQL")
    ?? throw new InvalidOperationException("Database connection string is not configured.");

builder.Services.AddNpgsqlDataSource(connectionStrings);

builder.Services.AddScoped<PaymentService>();
builder.Services.AddSingleton<PaymentClient>();
builder.Services.AddScoped<PaymentRepository>();

builder.Services.AddHostedService<HealthBackgroundService>();
builder.Services.AddHostedService<PaymentBackgroundService>();
ThreadPool.SetMinThreads(64, 64);

var app = builder.Build();

app.UseExceptionHandler();

app.MapPost("payments", async (
    MessageQueue<PaymentRequest> queue,
    PaymentRequest request) =>
{
    await queue.EnqueueAsync(request);

    return Results.Ok();
});

app.MapGet("payments-summary", async (
    PaymentService paymentService,
    DateTimeOffset? from,
    DateTimeOffset? to) =>
{
    var result = await paymentService.GetSummaryAsync(from, to);
    return Results.Ok(result);
});

await app.RunAsync();
