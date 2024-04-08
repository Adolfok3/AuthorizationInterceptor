using AuthorizationInterceptor.Entries;
using System.Text.Json;

namespace AuthorizationInterceptor.Json
{
    /// <summary>
    /// A Json Serializer for the <see cref="AuthorizationEntry"/> class. Used to print or save in a format where data loss does not occur.
    /// </summary>
    public class AuthorizationEntryJsonSerializer
    {
        /// <summary>
        /// Custom converter used to print or save in a format where data loss does not occur.
        /// </summary>
        public static JsonSerializerOptions DefaultOptions { get; } = new JsonSerializerOptions
        {
            Converters = { new AuthorizationEntryJsonConverter() }
        };

        /// <summary>
        /// Serialize an <see cref="AuthorizationEntry"/> object with a custom converter
        /// </summary>
        /// <param name="entry">The authorization entries</param>
        /// <returns>Returns the entry as json</returns>
        public static string Serialize(AuthorizationEntry entry)
            => JsonSerializer.Serialize(entry, DefaultOptions);

        /// <summary>
        /// Deserialize to a new instance of <see cref="AuthorizationEntry"/> with a custom converter
        /// </summary>
        /// <param name="json"></param>
        /// <returns>Returns an <see cref="AuthorizationEntry"/> instance from json</returns>
        public static AuthorizationEntry? Deserialize(string json)
            => JsonSerializer.Deserialize<AuthorizationEntry>(json, DefaultOptions);
    }
}
