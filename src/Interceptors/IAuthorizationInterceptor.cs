using AuthorizationInterceptor.Entries;
using System.Threading.Tasks;

namespace AuthorizationInterceptor.Interceptors
{
    /// <summary>
    /// Defines the interface for interceptor components responsible for managing authorization headers.
    /// </summary>
    public interface IAuthorizationInterceptor
    {
        /// <summary>
        /// Retrieves the current set of authorization headers.
        /// </summary>
        /// <returns>
        /// <see cref="AuthorizationEntry"/> containing the authorization headers.
        /// </returns>
        Task<AuthorizationEntry> GetHeadersAsync();

        /// <summary>
        /// Update the current set of authorization headers.
        /// </summary>
        /// <returns>
        /// <see cref="AuthorizationEntry"/> containing the new authorization headers.
        /// </returns>
        Task<AuthorizationEntry> UpdateHeadersAsync(AuthorizationEntry expiredEntries);
    }
}
