using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Harmony.Module.Objects;

namespace Harmony.Module.Converters
{
    internal class CharacterConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Character);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            if (token.Type == JTokenType.Boolean && token.ToObject<bool>() == false)
            {
                return null;
            }
            else if (token.Type == JTokenType.Object)
            {
                return token.ToObject<Character>();
            }

            throw new JsonSerializationException("Unexpected token type: " + token.Type);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}