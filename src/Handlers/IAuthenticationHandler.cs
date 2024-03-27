using AuthorizationInterceptor.Entries;
using System.Threading.Tasks;

namespace AuthorizationInterceptor.Handlers
{
    /// <summary>
    /// Defines an abstraction interface to handle the origin of the authorization headers.
    /// </summary>
    public interface IAuthenticationHandler
    {
        /// <summary>
        /// Implementation of the authentication method.
        /// </summary>
        /// <returns><see cref="AuthorizationEntry"></returns>
        Task<AuthorizationEntry> AuthenticateAsync();

        /// <summary>
        /// Implementation of the non-authentication method called when authorization is expired. Most used for refresh token actions. If the integration does not have the refresh token option, the same value as the Authenticate method must be returned.
        /// </summary>
        /// <param name="entries">Authorization headers previously provided in the authentication method</param>
        /// <returns><see cref="AuthorizationEntry"></returns>
        Task<AuthorizationEntry> UnauthenticateAsync(AuthorizationEntry? entries);
    }
}
