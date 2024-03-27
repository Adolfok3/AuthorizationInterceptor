using AuthorizationInterceptor.Entries;

namespace AuthorizationInterceptor.Tests.Utils;

public static class MockAuthorizationEntry
{
    public static AuthorizationEntry CreateEntry()
        => new OAuthEntry("test", "test", 100, "test", "test");
}
