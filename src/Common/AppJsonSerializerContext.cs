using System.Text.Json.Serialization;

namespace Rinha.Api.Common;

[JsonSerializable(typeof(PaymentRequest))]
[JsonSerializable(typeof(DateTimeOffset?))]
[JsonSerializable(typeof(SummaryResponse))]
[JsonSerializable(typeof(Payment))]
[JsonSerializable(typeof(HealthResponse))]
public sealed partial class AppJsonSerializerContext : JsonSerializerContext;