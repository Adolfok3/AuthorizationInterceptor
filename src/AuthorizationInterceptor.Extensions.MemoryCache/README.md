![AuthorizationInterceptor Icon](../../resources/icon.png)

# AuthorizationInterceptor.Extensions.MemoryCache
[![GithubActions](https://github.com/Adolfok3/authorizationinterceptor/actions/workflows/main.yml/badge.svg)](https://github.com/Adolfok3/AuthorizationInterceptor/actions)
[![License](https://img.shields.io/badge/license-MIT-green)](./LICENSE)
[![Codecov](https://codecov.io/github/Adolfok3/AuthorizationInterceptor/graph/badge.svg?token=PHBV20RCQK)](https://codecov.io/github/Adolfok3/AuthorizationInterceptor)
[![NuGet Version](https://img.shields.io/nuget/vpre/AuthorizationInterceptor.Extensions.Abstractions)](https://nuget.org/packages/AuthorizationInterceptor.Extensions.MemoryCache)

An interceptor for [AuthorizationInterceptor](https://github.com/Adolfok3/AuthorizationInterceptor) that uses a IMemoryCache abstraction to handle authorization headers. For more information on how to configure and use Authorization Interceptor, please check the main page of [AuthorizationInterceptor](https://github.com/Adolfok3/AuthorizationInterceptor).

### Installation
Run the following command in package manager console:
```
PM> Install-Package AuthorizationInterceptor.Extensions.MemoryCache
```

Or from the .NET CLI as:
```
dotnet add package AuthorizationInterceptor.Extensions.MemoryCache
```

### Setup
When adding Authorization Interceptor Handler, call the extension method `UseMemoryCacheInterceptor` to options:
```csharp
services.AddHttpClient("TargetApi")
        .AddAuthorizationInterceptorHandler<TargetApiAuthClass>(options =>
		{
			options.UseMemoryCacheInterceptor();
		})
```
> Note: Memory cache is an abstraction that needs to be pre-configured. Please check the official documentation of [Cache in-memory in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory)