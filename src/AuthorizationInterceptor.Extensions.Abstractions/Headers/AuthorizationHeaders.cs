using System;
using System.Collections.Generic;

namespace AuthorizationInterceptor.Extensions.Abstractions.Headers
{
    /// <summary>
    /// Represents the headers returned by an authorization interceptor process or authentication method and including expiration information.
    /// </summary>
    public class AuthorizationHeaders : Dictionary<string, string>
    {
        /// <summary>
        /// Get auth headers in an OAuth settings
        /// </summary>
        public OAuthHeaders? OAuthHeaders { get; private set; }

        /// <summary>
        /// Gets the optional expiration timespan for the authorization data.
        /// </summary>
        /// <value>
        /// The duration for which the authorization data is considered valid, or <c>null</c> if it does not expire.
        /// </value>
        public TimeSpan? ExpiresIn { get; private set; }

        /// <summary>
        /// Gets the time at which the authorization data was generated/authenticated.
        /// </summary>
        /// <value>
        /// The <see cref="DateTimeOffset"/> representing the point in time the authorization data was generated/authenticated.
        /// </value>
        public DateTimeOffset AuthenticatedAt { get; private set; }

        /// <summary>
        /// Calculate the real expiration time
        /// </summary>
        /// <returns>Returns the real expiration time relative to now</returns>
        public TimeSpan? GetRealExpiration()
            => ExpiresIn - (DateTimeOffset.UtcNow - AuthenticatedAt);

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationHeaders"/> class with an optional expiration timespan.
        /// </summary>
        /// <param name="expiresIn">The optional expiration timespan for the authorization data.</param>
        public AuthorizationHeaders(TimeSpan? expiresIn = null)
        {
            ExpiresIn = expiresIn;
            AuthenticatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Determines if the headers are still valid based on their expiration time.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the headers are valid or if there is no expiration time set; otherwise, <c>false</c>.
        /// </returns>
        public bool IsHeadersValid()
        {
            if (ExpiresIn is null || OAuthHeaders?.ExpiresIn is null)
                return true;

            var realExpiration = TimeSpan.FromSeconds(OAuthHeaders.ExpiresIn.Value);
            return (realExpiration - (DateTimeOffset.UtcNow - AuthenticatedAt)) > TimeSpan.Zero;
        }

        public static implicit operator AuthorizationHeaders(OAuthHeaders headers)
        {
            headers.Validate();
            return new AuthorizationHeaders(headers);
        }

        private AuthorizationHeaders(OAuthHeaders oAuthHeaders)
        {
            AuthenticatedAt = DateTimeOffset.UtcNow;
            OAuthHeaders = oAuthHeaders;
            ExpiresIn = oAuthHeaders.GetRealTimeExpiration();

            Add("Authorization", $"{oAuthHeaders.TokenType} {oAuthHeaders.AccessToken}");
        }

        private AuthorizationHeaders()
        {
        }
    }
}
