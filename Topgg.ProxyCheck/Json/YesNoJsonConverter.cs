using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Topgg.ProxyCheck
{
    internal class YesNoJsonConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString() == "yes";
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value ? "yes" : "no");
        }
    }
}
