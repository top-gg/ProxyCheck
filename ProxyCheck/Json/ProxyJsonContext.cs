using System.Text.Json.Serialization;

namespace ProxyCheckUtil
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
