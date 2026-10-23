using System.Net;
using System.Reflection;
using NSubstitute;
using Topgg.ProxyCheck;

namespace ProxyCheckUtil.Tests;

public class RequestUriTest
{
    [Fact]
    public async Task ApiKeyIsSentInQueryString()
    {
        // Arrange
        var handler = Substitute.For<HttpMessageHandler>();
        handler
            .GetType()
            .GetMethod("SendAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(handler, [Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()]);
        handler.ReturnsResponse(HttpStatusCode.OK, """{ "status": "ok" }""");

        using var client = new HttpClient(handler);

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient().Returns(client);

        var proxyCheck = new ProxyCheck(httpClientFactory, "test-api-key")
        {
            UseTLS = true
        };

        // Act
        await proxyCheck.QueryAsync("104.16.255.200");

        // Assert
        var request = handler.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == "SendAsync")
            .Select(call => call.GetOriginalArguments().First())
            .Cast<HttpRequestMessage>()
            .Single();

        var uri = request.RequestUri!;
        Assert.Equal("/v2/", uri.AbsolutePath);
        Assert.DoesNotContain("key=", uri.AbsolutePath);
        Assert.Contains("key=test-api-key", uri.Query);
        Assert.Equal(
            "https://proxycheck.io/v2/?key=test-api-key&vpn=0&asn=0&node=0&time=0&inf=1&port=0&seen=0&days=7&risk=0",
            uri.ToString());
    }
}
