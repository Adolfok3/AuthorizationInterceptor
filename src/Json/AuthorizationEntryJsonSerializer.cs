using AuthorizationInterceptor.Entries;
using System.Text.Json;

namespace AuthorizationInterceptor.Json
{
    public class AuthorizationEntryJsonSerializer
    {
        public static JsonSerializerOptions DefaultOptions { get; } = new JsonSerializerOptions
        {
            Converters = { new AuthorizationEntryJsonConverter() }
        };

        public static string Serialize(AuthorizationEntry entry)
            => JsonSerializer.Serialize(entry, DefaultOptions);

        public static AuthorizationEntry? Deserialize(string json)
            => JsonSerializer.Deserialize<AuthorizationEntry>(json, DefaultOptions);
    }
}
