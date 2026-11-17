using AuthorizationInterceptor.Extensions.Abstractions.Options;
using AuthorizationInterceptor.Options;
using FluentAssertions;

namespace AuthorizationInterceptor.Tests.Options;

public class AuthorizationInterceptorOptionsTests
{
    [Fact]
    public void LockModeShouldDefaultToNone()
    {
        //Act
        var options = new AuthorizationInterceptorOptions();

        //Assert
        options.LockMode.Should().Be(AuthenticationLockMode.None);
    }

    [Fact]
    public void DistributedLockTimeoutShouldDefaultToNull()
    {
        //Act
        var options = new AuthorizationInterceptorOptions();

        //Assert
        options.DistributedLockTimeout.Should().BeNull();
    }

    [Fact]
    public void LockModeShouldAllowCombiningLocalAndDistributed()
    {
        //Act
        var options = new AuthorizationInterceptorOptions
        {
            LockMode = AuthenticationLockMode.Local | AuthenticationLockMode.Distributed
        };

        //Assert
        options.LockMode.HasFlag(AuthenticationLockMode.Local).Should().BeTrue();
        options.LockMode.HasFlag(AuthenticationLockMode.Distributed).Should().BeTrue();
    }

    [Fact]
    public void DistributedLockTimeoutShouldBeAssignable()
    {
        //Arrange
        var timeout = TimeSpan.FromSeconds(30);

        //Act
        var options = new AuthorizationInterceptorOptions { DistributedLockTimeout = timeout };

        //Assert
        options.DistributedLockTimeout.Should().Be(timeout);
    }
}
