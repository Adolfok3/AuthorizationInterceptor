using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using System.Text.Json;

namespace AuthorizationInterceptor.Extensions.Abstractions.Json
{
    /// <summary>
    /// A Json Serializer for the <see cref="AuthorizationHeaders"/> class. Used to print or save in a format where data loss does not occur.
    /// </summary>
    public class AuthorizationHeadersJsonSerializer
    {
        /// <summary>
        /// Custom converter used to print or save in a format where data loss does not occur.
        /// </summary>
        public static JsonSerializerOptions DefaultOptions { get; } = new()
        {
            Converters = { new AuthorizationHeadersJsonConverter() }
        };

        /// <summary>
        /// Serialize an <see cref="AuthorizationHeaders"/> object with a custom converter
        /// </summary>
        /// <param name="headers">The authorization headers</param>
        /// <returns>Returns the headers as json</returns>
        public static string Serialize(AuthorizationHeaders headers)
            => JsonSerializer.Serialize(headers, DefaultOptions);

        /// <summary>
        /// Deserialize to a new instance of <see cref="AuthorizationHeaders"/> with a custom converter
        /// </summary>
        /// <param name="json"></param>
        /// <returns>Returns an <see cref="AuthorizationHeaders"/> instance from json</returns>
        public static AuthorizationHeaders? Deserialize(string json)
            => JsonSerializer.Deserialize<AuthorizationHeaders>(json, DefaultOptions);
    }
}
