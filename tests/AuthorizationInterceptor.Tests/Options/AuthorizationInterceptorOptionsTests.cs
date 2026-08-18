using AuthorizationInterceptor.Extensions.Abstractions.Options;
using AuthorizationInterceptor.Options;

namespace AuthorizationInterceptor.Tests.Options;

public class AuthorizationInterceptorOptionsTests
{
    [Fact]
    public void LockMode_ShouldDefaultToNone()
    {
        //Act
        var options = new AuthorizationInterceptorOptions();

        //Assert
        Assert.Equal(AuthenticationLockMode.None, options.LockMode);
    }

    [Fact]
    public void DistributedLockTimeout_ShouldDefaultToNull()
    {
        //Act
        var options = new AuthorizationInterceptorOptions();

        //Assert
        Assert.Null(options.DistributedLockTimeout);
    }

    [Fact]
    public void LockMode_ShouldAllowCombiningLocalAndDistributed()
    {
        //Act
        var options = new AuthorizationInterceptorOptions
        {
            LockMode = AuthenticationLockMode.Local | AuthenticationLockMode.Distributed
        };

        //Assert
        Assert.True(options.LockMode.HasFlag(AuthenticationLockMode.Local));
        Assert.True(options.LockMode.HasFlag(AuthenticationLockMode.Distributed));
    }

    [Fact]
    public void DistributedLockTimeout_ShouldBeAssignable()
    {
        //Arrange
        var timeout = TimeSpan.FromSeconds(30);

        //Act
        var options = new AuthorizationInterceptorOptions { DistributedLockTimeout = timeout };

        //Assert
        Assert.Equal(timeout, options.DistributedLockTimeout);
    }
}
