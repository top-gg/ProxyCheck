using System.Net;
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
                var ip = new IPAddress(new byte[] { 10, (byte)writer, (byte)(i >> 8), (byte)i });
                cache.SetCacheRecord(
                    new Dictionary<IPAddress, ProxyCheckResult.IpResult> { [ip] = new() },
                    options);
                cache.GetCacheRecord(ip, options);
            }
        }));
        await Task.WhenAll(tasks);

        // Assert
        var all = Enumerable.Range(0, writers)
            .SelectMany(writer => Enumerable.Range(0, writesPerWriter)
                .Select(i => new IPAddress(new byte[] { 10, (byte)writer, (byte)(i >> 8), (byte)i })))
            .ToArray();
        Assert.Equal(writers * writesPerWriter, cache.GetCacheRecords(all, options).Count);
    }
}
