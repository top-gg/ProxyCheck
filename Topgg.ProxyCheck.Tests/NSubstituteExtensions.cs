using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using NSubstitute;
using NSubstitute.Core;

namespace ProxyCheckUtil.Tests;

public static class NSubstituteExtensions
{
    public static HttpMessageHandler SetupRequest(this HttpMessageHandler handler, HttpMethod method, string requestUri)
    {
        handler
            .GetType()
            .GetMethod("SendAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(handler, [
                Arg.Is<HttpRequestMessage>(x =>
                    x.Method == method &&
                    x.RequestUri != null &&
                    x.RequestUri.ToString() == requestUri),
                Arg.Any<CancellationToken>()
            ]);

        return handler;
    }

    public static ConfiguredCall ReturnsResponse(this HttpMessageHandler handler, HttpStatusCode statusCode, string? jsonContent = null)
    {
        return ((object)handler).Returns(
            Task.FromResult(new HttpResponseMessage()
            {
                StatusCode = statusCode,
                Content = jsonContent != null ? new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json") : null
            })
        );
    }

    public static void ShouldHaveReceived(this HttpMessageHandler handler, HttpMethod requestMethod, string requestUri, int timesCalled = 1)
    {
        var calls = handler.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == "SendAsync")
            .Select(call => call.GetOriginalArguments().First())
            .Cast<HttpRequestMessage>()
            .Where(request =>
                request.Method == requestMethod &&
                request.RequestUri != null &&
                request.RequestUri.ToString() == requestUri
            );

        Assert.Equal(timesCalled, calls.Count());
    }
}