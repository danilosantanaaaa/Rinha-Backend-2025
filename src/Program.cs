using System.Threading.Channels;

using Microsoft.AspNetCore.Mvc;

using Rinha.Api;
using Rinha.Api.Clients;
using Rinha.Api.Helpers;
using Rinha.Api.Models;
using Rinha.Api.Repositories;
using Rinha.Api.Services;
using Rinha.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "RinhaPaymentCache";
});

builder.Services.AddHttpClient(nameof(PaymentGateway.Default), client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PaymentProcessorDefault"]!);
});

builder.Services.AddHttpClient(nameof(PaymentGateway.Fallback), client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PaymentProcessorFallback"]!);
});

builder.Services.AddSingleton(
    Channel.CreateUnbounded<PaymentRequest>(new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = false
    }));

builder.Services.AddSingleton<DatabaseConnection>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<PaymentProcessorClient>();
builder.Services.AddScoped<PaymentRepository>();

builder.Services.AddSingleton<HealthSummary>();
builder.Services.AddHostedService<HealthBackgroundService>();
builder.Services.AddHostedService<PaymentBackgroundService>();
ThreadPool.SetMinThreads(Environment.ProcessorCount * 4, Environment.ProcessorCount * 4);

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
    [FromServices] Channel<PaymentRequest> channel) =>
{
    await channel.Writer.WriteAsync(request);

    return Results.Ok();
});

app.MapGet("payments-summary", async (
    [FromServices] PaymentService paymentService,
    [FromQuery] DateTime from,
    [FromQuery] DateTime to) =>
{
    var result = await paymentService.GetSummaryAsync(from, to);

    return Results.Ok(result);
});

await app.RunAsync();
