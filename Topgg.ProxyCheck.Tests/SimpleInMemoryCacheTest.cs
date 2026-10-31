using System.Net;
using System.Reflection;
using NSubstitute;
using Topgg.ProxyCheck;

namespace ProxyCheckUtil.Tests;

public class SimpleInMemoryCacheTest
{
    [Fact]
    public async Task ConcurrentReadsAndWritesKeepTheCacheUsable()
    {
        // Arrange
        var cache = new SimpleInMemoryCache();
        var options = new ProxyCheckRequestOptions();
        const int writers = 4;
        const int writesPerWriter = 500;

        // Act
        var tasks = Enumerable.Range(0, writers).Select(writer => Task.Run(() =>
        {
            for (var i = 0; i < writesPerWriter; i++)
            {
                var ip = CreateIp(writer, i);
                cache.Set(ip, options, new ProxyCheckResult.IpResult());
                cache.Get(ip, options);
            }
        }));
        await Task.WhenAll(tasks);

        // Assert
        for (var writer = 0; writer < writers; writer++)
        {
            for (var i = 0; i < writesPerWriter; i++)
            {
                Assert.NotNull(cache.Get(CreateIp(writer, i), options));
            }
        }
    }

    [Fact]
    public void ExpiredRecordsAreNotReturned()
    {
        // Arrange
        var cache = new SimpleInMemoryCache(TimeSpan.Zero);
        var options = new ProxyCheckRequestOptions();
        var ip = IPAddress.Parse("10.0.0.1");

        // Act
        cache.Set(ip, options, new ProxyCheckResult.IpResult());

        // Assert
        Assert.Null(cache.Get(ip, options));
    }

    [Fact]
    public void RecordsStayKeyedByTheOptionsTheyWereStoredWith()
    {
        // Arrange
        var cache = new SimpleInMemoryCache();
        var options = new ProxyCheckRequestOptions();
        var ip = IPAddress.Parse("10.0.0.1");
        cache.Set(ip, options, new ProxyCheckResult.IpResult());

        // Act
        options.IncludeVPN = true;

        // Assert
        Assert.Null(cache.Get(ip, options));
        Assert.NotNull(cache.Get(ip, new ProxyCheckRequestOptions()));
    }

    [Fact]
    public async Task RepeatedQueryIsAnsweredFromTheCache()
    {
        // Arrange
        var handler = Substitute.For<HttpMessageHandler>();
        handler
            .GetType()
            .GetMethod("SendAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(handler, [Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()]);
        handler.ReturnsResponse(HttpStatusCode.OK, """{ "status": "ok", "104.16.255.200": { "proxy": "no" } }""");

        using var client = new HttpClient(handler);
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient().Returns(client);
        var proxyCheck = new ProxyCheck(httpClientFactory, "test-api-key", new SimpleInMemoryCache());
        var ip = IPAddress.Parse("104.16.255.200");

        // Act
        var first = await proxyCheck.QueryAsync(ip);
        var second = await proxyCheck.QueryAsync(ip);

        // Assert
        Assert.False(first.Results[ip].IsCacheHit);
        Assert.True(second.Results[ip].IsCacheHit);
        Assert.Single(handler.ReceivedCalls(), call => call.GetMethodInfo().Name == "SendAsync");
    }

    private static IPAddress CreateIp(int writer, int i)
        => new(new byte[] { 10, (byte)writer, (byte)(i >> 8), (byte)i });
}
