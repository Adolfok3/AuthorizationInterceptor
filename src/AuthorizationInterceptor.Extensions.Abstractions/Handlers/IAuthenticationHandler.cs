using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace AuthorizationInterceptor.Extensions.Abstractions.Handlers
{
    /// <summary>
    /// Defines an abstraction interface to handle the origin of the authorization headers.
    /// </summary>
    public interface IAuthenticationHandler
    {
        /// <summary>
        /// Implementation of the authentication method.
        /// </summary>
        /// <param name="expiredHeaders">If a previous authentication was made, the expiredHeaders will be passed; otherwise, it will be null.
        /// The expiredHeaders are mostly used to refresh tokens or to re-authenticate with previous header information.
        /// </param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>Returns a set of <see cref="AuthorizationHeaders"/> with authorized headers.</returns>
        ValueTask<AuthorizationHeaders?> AuthenticateAsync(AuthorizationHeaders? expiredHeaders, CancellationToken cancellationToken);
    }
}
