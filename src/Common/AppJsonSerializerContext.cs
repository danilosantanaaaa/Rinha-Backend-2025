using System.Text.Json.Serialization;

using Rinha.Api.Models.Healths;
using Rinha.Api.Models.Payments;
using Rinha.Api.Models.Summaries;

namespace Rinha.Api.Common;

[JsonSerializable(typeof(PaymentRequest))]
[JsonSerializable(typeof(DateTimeOffset?))]
[JsonSerializable(typeof(SummaryResponse))]
[JsonSerializable(typeof(Payment))]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(PaymentGateway))]
public sealed partial class AppJsonSerializerContext : JsonSerializerContext;