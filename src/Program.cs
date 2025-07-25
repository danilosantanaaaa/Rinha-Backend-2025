using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;

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
});

builder.Services.AddHttpClient(nameof(PaymentGateway.Default), client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PaymentProcessorDefault"]!);
});

builder.Services.AddHttpClient(nameof(PaymentGateway.Fallback), client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PaymentProcessorFallback"]!);
});

builder.Services.AddKeyedSingleton<MessageQueue<PaymentRequest>>(Configuration.PaymentQueue);
builder.Services.AddKeyedSingleton<MessageQueue<PaymentRequest>>(Configuration.PaymentRetry);

builder.Services.AddSingleton<DatabaseConnection>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddSingleton<PaymentGatewayClient>();
builder.Services.AddScoped<PaymentRepository>();

builder.Services.AddSingleton<HealthSummary>();
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
    [FromKeyedServices(Configuration.PaymentQueue)] MessageQueue<PaymentRequest> queue) =>
{
    await queue.EnqueueAsync(request);

    return Results.Ok();
});

app.MapGet("payments-summary", async (
    [FromServices] PaymentService paymentService,
    [FromQuery] DateTime? from,
    [FromQuery] DateTime? to) =>
{
    var result = await paymentService.GetSummaryAsync(from, to);
    return Results.Ok(result);
});

await app.RunAsync();
