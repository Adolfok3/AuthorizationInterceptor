using AuthorizationInterceptor.Extensions;
using AuthorizationInterceptor.Options;
using Microsoft.Extensions.DependencyInjection;

namespace AuthorizationInterceptor.IntegrationTests.Infrastructure;

/// <summary>
/// A single application "instance": an isolated <see cref="ServiceProvider"/> with its own
/// <c>AuthenticationSingleFlight</c> (local lock) and its own <see cref="System.Net.Http.IHttpClientFactory"/>.
/// Multiple instances model separate processes; when they share a distributed lock/cache they model a scaled-out
/// deployment. All instances route to the same in-memory target, hence to the same <see cref="LoginCounter"/>.
/// </summary>
public sealed class ConsumerInstance : IAsyncDisposable
{
    private readonly ServiceProvider _provider;

    public HttpClient Client { get; }

    private ConsumerInstance(ServiceProvider provider, HttpClient client)
    {
        _provider = provider;
        Client = client;
    }

    public static ConsumerInstance Create(
        Func<HttpMessageHandler> targetHandlerFactory,
        string clientName,
        Action<AuthorizationInterceptorOptions> configureOptions,
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        configureServices?.Invoke(services);

        services.AddHttpClient("auth", client => client.BaseAddress = new Uri("http://localhost"))
            .ConfigurePrimaryHttpMessageHandler(targetHandlerFactory);

        services.AddHttpClient(clientName, client => client.BaseAddress = new Uri("http://localhost"))
            .AddAuthorizationInterceptorHandler<CountingApiAuthenticationHandler>(configureOptions)
            .ConfigurePrimaryHttpMessageHandler(targetHandlerFactory);

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(clientName);

        return new ConsumerInstance(provider, client);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _provider.DisposeAsync();
    }
}
