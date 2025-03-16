![AuthorizationInterceptor Icon](./resources/icon.png)

# Authorization Interceptor: A simple and lightweight .NET package designed to streamline HttpClient authenticated requests
[![GithubActions](https://github.com/Adolfok3/authorizationinterceptor/actions/workflows/main.yml/badge.svg)](https://github.com/Adolfok3/AuthorizationInterceptor/actions)
[![License](https://img.shields.io/badge/license-MIT-green)](./LICENSE)
[![Coverage Status](https://coveralls.io/repos/github/Adolfok3/authorizationinterceptor/badge.svg?branch=main)](https://coveralls.io/github/Adolfok3/authorizationinterceptor?branch=main)
[![NuGet Version](https://img.shields.io/nuget/vpre/AuthorizationInterceptor)](https://www.nuget.org/packages/AuthorizationInterceptor)

## What is Authorization Interceptor?
Authorization Interceptor is a custom handler added to your HttpClient builder. With it, there's no need to worry about the expiration time and management of the authorization headers of your requests. Offering the possibility to use OAuth2 with RefreshToken or custom headers, whenever a request is sent and its response is a status code 401 (Unauthorized), the Interceptor will update the authorization headers and resend the same request with the updated authorization headers.

Authorization Interceptor can use MemoryCache to store authorization headers according to their expiration time, thus, it's not necessary to login or generate authorization every time you need to send a request to the API.

Authorization Interceptor can also share the same authorization headers with other instances of the application (if your application runs in a dockerized environment with Kubernetes) using the idea of distributed cache, avoiding concurrency among instances to log in or generate authorization headers with the API by reusing the authorization generated by the primary instance.

## Getting started

### Installation
Authorization Interceptor is installed [from NuGet](https://nuget.org/packages/authorizationinterceptor). Just run the following command in package manager console:
```
PM> Install-Package AuthorizationInterceptor
```

Or from the .NET CLI as:
```
dotnet add package AuthorizationInterceptor
```
### Setup
When adding a new HttpClient, call the extension method `AddAuthorizationInterceptorHandler`, passing in the authentication class for the target API:
```csharp
services.AddHttpClient("TargetApi")
        .AddAuthorizationInterceptorHandler<TargetApiAuthClass>()
```

This will make the `TargetApi` HttpClient use the Authorization Interceptor handler to generate and manage authorization headers.

> By default, the package will not use any interceptor to store authorization headers so the recommendation is to use at least [AuthorizationInterceptor.Extensions.MemoryCache](https://nuget.org/packages/AuthorizationInterceptor.Extensions.MemoryCache) package to store and manage the authorization headers lifecycle. Checkout [Interceptors section](#interceptors).

The `TargetApiAuthClass` must implement the `IAuthenticationHandler` interface, so that the package can perform the necessary dependency and know where and when to generate the authorization headers. An example implementation of the class would look like this:

```csharp
public async ValueTask<AuthorizationHeaders?> AuthenticateAsync(AuthorizationHeaders? expiredHeaders, CancellationToken cancellation)
{
    HttpResponseMessage? response = null;

    if (expiredHeaders == null)
    {
        // Generate the login for the first time and return the authorization headers
        response = await _client.PostAsync("auth", null, cancellation);
    }
    else
    {
        // If a previous login was made, the expiredHeaders will be passed and you can reuse to reauthenticate. It is most commonly used for integrations with APIs that use RefreshToken.
        // If the target API does not have the refresh token functionality, you will not need to implement this if condition e you can ignore the parameter 'expiredHeaders' performing always a new login
        response = await _client.PostAsync($"refresh?refresh={expiredHeaders.OAuthHeaders!.RefreshToken}", null, cancellation);
    }

    var content = await response.Content.ReadAsStringAsync(cancellation);
    var newHeaders = JsonSerializer.Deserialize<User>(content)!;

    return new OAuthHeaders(newHeaders.AccessToken, newHeaders.TokenType, newHeaders.ExpiresIn, newHeaders.RefreshToken, newHeaders.RefreshTokenExpiresIn);
}
```

In the example above, we showed the `TargetApiAuthClass` class, which must implement the authentication methods with the target API. Initially, the authorization headers do not exist, so the package will call the `AuthenticateAsync` method just once and will store the authorization in memory cache (if the MemoryCache package was installed and configured), always consulting it from there. However, if there is a response with status code 401 (unauthorized), the package will call the `AuthenticateAsync` again, passing the old/expired authorization and will return the new authorization.

> Note that in the `AuthenticateAsync` method the return type is `AuthorizationHeaders` but in the example above an `OAuthHeaders` is returned, because in this example we are assuming that the target API uses the OAuth2 authentication mechanism. However, if your target API does not have this functionality you can return a new object of type `AuthorizationHeaders` that inherits from a class `Dictionary<string, string>`. In practice, it would look like this:

```csharp
public async ValueTask<AuthorizationHeaders?> AuthenticateAsync(AuthorizationHeaders? expiredHeaders, CancellationToken cancellation)
    {
        return new AuthorizationHeaders
        {
            { "MyCustomAuthorizationHeader", "MytokenValue" },
            { "SomeOtherAuthorizationHeader", "OtherValue" }
        };
    }
```

### Custom Options
Assuming that your target API is legacy and it returns not only the status code 401 (unauthorized) for requests without authorization or with expired authorization but also returns 403 (forbidden). For this situation, there is a property in the options class called `UnauthenticatedPredicate` that customizes the type of predicate the package should evaluate for unauthorized requests.

In the extension method `AddAuthorizationInterceptorHandler`, pass this custom configuration in the following way:
```csharp
services.AddHttpClient("TargetApi")
        .AddAuthorizationInterceptorHandler<TargetApiAuthClass>(options =>
        {
            opts.UnauthenticatedPredicate = response => response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                                                        response.StatusCode == System.Net.HttpStatusCode.Unauthorized;
        })
```

Now, whenever there is a response with status code 401 or 403, the package will consider the authorization to be expired and will perform a new authentication.

### Interceptors
By default, no interceptors is configured so the recommendation is to use at least [AuthorizationInterceptor.Extensions.MemoryCache](https://nuget.org/packages/AuthorizationInterceptor.Extensions.MemoryCache) package to store and manage the authorization headers lifecycle in a memory cache system.

After install it, its simple to use:

```csharp
services.AddHttpClient("TargetApi")
        .AddAuthorizationInterceptorHandler<TargetApiAuthClass>(options =>
        {
            options.UseMemoryCacheInterceptor();
        })
```

#### Available Interceptors

- [AuthorizationInterceptor.Extensions.MemoryCache](https://nuget.org/packages/AuthorizationInterceptor.Extensions.MemoryCache)
- [AuthorizationInterceptor.Extensions.DistributedCache](https://nuget.org/packages/AuthorizationInterceptor.Extensions.DistributedCache)
- [AuthorizationInterceptor.Extensions.HybridCache](https://nuget.org/packages/AuthorizationInterceptor.Extensions.HybridCache)

> If you need an specific interceptor integration and doesnt exists here, checkout the [AuthorizationInterceptor.Extensions.Abstractions](https://nuget.org/packages/AuthorizationInterceptor.Extensions.Abstractions) to create your own interceptor.

#### Concurrency
When we have a scalable application running in a dockerized environment with Kubernetes, there can be more than one instance of the same application. This can lead to authentication concurrency issues between instances and divergent authorization headers among them. To solve this, it is recommended to use a custom interceptor that implements the idea of distributed cache. Therefore, in addition to saving the authorization headers in memory, the application will also save them in a distributed cache so that other instances of the application can reuse the authorization already generated by a primary instance. This avoids multiple authentication calls and divergent authorizations.

In practice, you need to configure some library already available for .Net that uses the DistributedCache abstraction. Please check the official documentation of [Distributed caching in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed)

Let's assume you have configured some package that uses the DistributedCache abstraction (Redis, NCache, etc), then you should install the [AuthorizationInterceptor.Extensions.DistributedCache](https://nuget.org/packages/AuthorizationInterceptor.Extensions.DistributedCache) extension package. In the options of the Authorization Interceptor, call the respective extension method `UseDistributedCacheInterceptor`.

```csharp
services.AddHttpClient("TargetApi")
        .AddAuthorizationInterceptorHandler<TargetApiAuthClass>(options =>
        {
            options.UseMemoryCacheInterceptor();
            options.UseDistributedCacheInterceptor();
        })
        .ConfigureHttpClient(opt => opt.BaseAddress = new Uri("https://targetapi.com"));
```

Adding a MemoryCache with a DistributedCache is a perfect match and recommended way to perform the authorized requests nicely. With this configuration, the Authorization Interceptor will create a sequence with: `MemoryCache > DistributedCache > AuthenticationHandler > DistributedCache > MemoryCache`.

### Custom Interceptors

It's possible to add custom interceptors in the sequence of interceptors. Create your Interceptor class and have it inherit from the interface `IAuthorizationInterceptor`. After that, add it to the constructor of the Authorization Interceptor through the method `UseCustomInterceptor<T>`, e.g.:

```csharp
services.AddHttpClient("TargetApi")
        .AddAuthorizationInterceptorHandler<TargetApiAuthClass>(options =>
        {
            options.UseMemoryCacheInterceptor();
            options.UseDistributedCacheInterceptor();
            options.UseCustomInterceptor<MyCustomInterceptor>();
        })
        .ConfigureHttpClient(opt => opt.BaseAddress = new Uri("https://targetapi.com"));
```

`MyCustomInterceptor` class:

```csharp
public class MyCustomInterceptor : IAuthorizationInterceptor
{
    public ValueTask<AuthorizationHeaders?> GetHeadersAsync(string name, CancellationToken cancellationToken)
    {
        //Do something and return the headers if exists in this context
    }

    public ValueTask UpdateHeadersAsync(string name, AuthorizationHeaders? expiredHeaders, AuthorizationHeaders? newHeaders, CancellationToken cancellationToken)
    {
        //Do something with expired headers if necessary and update with newHeaders
    }
}
```

With this configuration, the Authorization Interceptor will create a sequence of: `MemoryCache > DistributedCache > MyCustomInterceptor > AuthenticationHandler > DistributedCache > MyCustomInterceptor > MemoryCache`.

> The 'name' parameter refers to the name of the configured HttpClient. With this parameter you can differentiate between multiple httpclients configured using the same interceptor.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.