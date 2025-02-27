using AuthorizationInterceptor.Extensions.Abstractions.Headers;

namespace AuthorizationInterceptor.Tests.Utils;

public static class MockAuthorizationHeaders
{
    public static AuthorizationHeaders CreateHeaders()
        => new OAuthHeaders("test", "test", 100, "test", 300);
}
