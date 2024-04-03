using System;
using System.Collections.Generic;

namespace AuthorizationInterceptor.Entries
{
    /// <summary>
    /// Represents the headers returned by a authorization interceptor process or authentication method and including expiration information.
    /// </summary>
    public class AuthorizationEntry : Dictionary<string, string>
    {
        /// <summary>
        /// Obtém as propriedades do OAuth depois de definidas no método de autenticação.
        /// </summary>
        public OAuthEntry? OAuthEntry { get; }

        /// <summary>
        /// Gets the optional expiration timespan for the authorization data.
        /// </summary>
        /// <value>
        /// The duration for which the authorization data is considered valid, or <c>null</c> if it does not expire.
        /// </value>
        public TimeSpan? ExpiresIn { get; }

        /// <summary>
        /// Gets the time at which the authorization data was generated/authenticated.
        /// </summary>
        /// <value>
        /// The <see cref="DateTimeOffset"/> representing the point in time the authorization data was generated/authenticated.
        /// </value>
        public DateTimeOffset AuthenticatedAt { get; }

        /// <summary>
        /// Calculate the real expiration time
        /// </summary>
        /// <returns>Returns the real expiration time relative to now</returns>
        public TimeSpan? GetRealExpiration()
            => ExpiresIn - (DateTimeOffset.UtcNow - AuthenticatedAt);

        public AuthorizationEntry(TimeSpan? expiresIn = null)
        {
            ExpiresIn = expiresIn;
            AuthenticatedAt = DateTimeOffset.UtcNow;
        }

        private AuthorizationEntry(OAuthEntry oAuthEntry)
        {
            AuthenticatedAt = DateTimeOffset.UtcNow;
            OAuthEntry = oAuthEntry;
            ExpiresIn = oAuthEntry.ExpiresIn.HasValue ? TimeSpan.FromSeconds(oAuthEntry.ExpiresIn.Value) : null;
            Add("Authorization", $"{oAuthEntry.TokenType} {oAuthEntry.AccessToken}");
        }

        public static implicit operator AuthorizationEntry(OAuthEntry entry)
        {
            ValidateValue(nameof(entry.AccessToken), entry.AccessToken);
            ValidateValue(nameof(entry.TokenType), entry.TokenType);
            if (entry.ExpiresIn.HasValue && entry.ExpiresIn.Value <= 0)
                throw new ArgumentException("ExpiresIn must be greater tha 0.");

            return new AuthorizationEntry(entry);
        }

        private static void ValidateValue(string name, string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"Property '{name}' is required.");
        }
    }
}
