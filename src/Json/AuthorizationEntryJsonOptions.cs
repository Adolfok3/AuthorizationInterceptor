using System.Text.Json;

namespace AuthorizationInterceptor.Json
{
    public static class AuthorizationEntryJsonOptions
    {
        public static JsonSerializerOptions DefaultOptions { get; } = new JsonSerializerOptions
        {
            Converters = { new AuthorizationEntryJsonConverter() }
        };
    }
}
