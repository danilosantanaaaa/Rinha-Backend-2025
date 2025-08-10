using Microsoft.AspNetCore.Mvc;

using Rinha.Api.Repositories;
using Rinha.Api.Services;
using Rinha.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");

    options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions()
    {
        AbortOnConnectFail = true,
        EndPoints = { options.Configuration! }
    };
});

builder.Services.AddHttpClient(nameof(PaymentGateway.Default), client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PaymentProcessorDefault"]!);
});

builder.Services.AddHttpClient(nameof(PaymentGateway.Fallback), client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PaymentProcessorFallback"]!);
});

builder.Services.AddSingleton<HealthChecker>();
builder.Services.AddSingleton<MessageQueue<PaymentRequest>>();

var connectionStrings = builder.Configuration.GetConnectionString("PostgreSQL")
    ?? throw new InvalidOperationException("Database connection string is not configured.");

builder.Services.AddNpgsqlDataSource(connectionStrings);

builder.Services.AddScoped<PaymentService>();
builder.Services.AddSingleton<PaymentGatewayClient>();
builder.Services.AddScoped<PaymentRepository>();

builder.Services.AddSingleton<CacheService>();
builder.Services.AddHostedService<HealthBackgroundService>();
builder.Services.AddHostedService<PaymentBackgroundService>();
ThreadPool.SetMinThreads(64, 64);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseExceptionHandler();

app.MapPost("payments", async (
    [FromBody] PaymentRequest request,
    MessageQueue<PaymentRequest> queue) =>
{
    await queue.EnqueueAsync(request);

    return Results.Ok();
});

app.MapGet("payments-summary", async (
    [FromServices] PaymentService paymentService,
    [FromQuery] DateTimeOffset? from,
    [FromQuery] DateTimeOffset? to) =>
{
    var result = await paymentService.GetSummaryAsync(from, to);
    return Results.Ok(result);
});

await app.RunAsync();
