using System.Text.Json.Serialization;

namespace Topgg.ProxyCheck
{
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        Converters = new []
        {
            typeof(JsonStringEnumConverter<StatusResult>),
            typeof(JsonStringEnumConverter<RiskLevel>)
        }
    )]
    [JsonSerializable(typeof(ProxyCheckResult))]
    [JsonSerializable(typeof(ProxyCheckResult.IpResult))]
    internal partial class ProxyJsonContext : JsonSerializerContext
    {
    }
}
