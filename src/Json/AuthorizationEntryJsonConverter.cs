using AuthorizationInterceptor.Entries;
using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AuthorizationInterceptor.Json
{
    internal class AuthorizationEntryJsonConverter : JsonConverter<AuthorizationEntry>
    {
        public override AuthorizationEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (Activator.CreateInstance(typeof(AuthorizationEntry), true) is not AuthorizationEntry authorizationEntry)
                throw new JsonException("Unable to create an AuthorizationEntry instance.");

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return authorizationEntry;

                var propertyName = reader.GetString();
                reader.Read();

                if (reader.TokenType == JsonTokenType.Null)
                    continue;

                switch (propertyName)
                {
                    case "Headers":
                        SetHeaders(ref reader, authorizationEntry);
                        continue;
                    case "ExpiresIn":
                        var expiresIn = reader.GetString();
                        if (string.IsNullOrEmpty(expiresIn))
                            continue;

                        SetProperty(authorizationEntry, "ExpiresIn", TimeSpan.Parse(expiresIn));
                        continue;
                    case "AuthenticatedAt":
                        var authenticatedAt = reader.GetString();
                        if (string.IsNullOrEmpty(authenticatedAt))
                            continue;

                        SetProperty(authorizationEntry, "AuthenticatedAt", DateTimeOffset.Parse(authenticatedAt));
                        continue;
                    case "OAuthEntry":
                        SetOAuthEntry(ref reader, options, authorizationEntry);
                        continue;
                }
            }

            throw new JsonException("Expected EndObject token.");
        }

        public override void Write(Utf8JsonWriter writer, AuthorizationEntry value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteStartObject("Headers");
            foreach (var item in value)
                writer.WriteString(item.Key, item.Value);

            writer.WriteEndObject();

            writer.WriteString("ExpiresIn", value.ExpiresIn?.ToString());
            writer.WriteString("AuthenticatedAt", value.AuthenticatedAt);

            if (value.OAuthEntry == null)
            {
                writer.WriteNull("OAuthEntry");
                writer.WriteEndObject();
                return;
            }

            writer.WriteStartObject("OAuthEntry");
            writer.WriteString("AccessToken", value.OAuthEntry?.AccessToken);
            writer.WriteString("TokenType", value.OAuthEntry?.TokenType);
            writer.WriteString("ExpiresIn", value.OAuthEntry?.ExpiresIn?.ToString());
            writer.WriteString("RefreshToken", value.OAuthEntry?.RefreshToken);
            writer.WriteString("Scope", value.OAuthEntry?.Scope);
            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        private void SetOAuthEntry(ref Utf8JsonReader reader, JsonSerializerOptions options, AuthorizationEntry authorizationEntry)
        {
            var oAuthEntryJson = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
            
            var accessToken = RequireStringProperty(oAuthEntryJson, "AccessToken");
            if (string.IsNullOrEmpty(accessToken))
                return;

            var tokenType = RequireStringProperty(oAuthEntryJson, "TokenType");
            if (string.IsNullOrEmpty(tokenType))
                return;

            var expiresInValue = RequireStringProperty(oAuthEntryJson, "ExpiresIn");
            double? expiresIn = string.IsNullOrEmpty(expiresInValue) ? null : double.Parse(expiresInValue);
            var refreshToken = RequireStringProperty(oAuthEntryJson, "RefreshToken");
            var scope = RequireStringProperty(oAuthEntryJson, "Scope");
            SetProperty(authorizationEntry, "OAuthEntry", new OAuthEntry(accessToken, tokenType, expiresIn, refreshToken, scope));
        }

        private void SetHeaders(ref Utf8JsonReader reader, AuthorizationEntry authorizationEntry)
        {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                var key = reader.GetString();
                reader.Read();
                var value = reader.GetString();
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                    continue;

                authorizationEntry.Add(key, value);
            }
        }

        private void SetProperty<TValue>(AuthorizationEntry obj, string propertyName, TValue value)
        {
            var propertyInfo = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            propertyInfo?.SetValue(obj, value, null);
        }

        public string? RequireStringProperty(JsonElement oAuthEntryJson, string propertyName)
        {
            if (!oAuthEntryJson.TryGetProperty(propertyName, out var property))
                return null;

            return property.GetString();
        }
    }
}
