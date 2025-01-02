using System;
using System.Net.Http;
using JetBrains.Annotations;

namespace ProxyCheckUtil
{
    /// <summary>
    /// Simple implementation of <see cref="IHttpClientFactory"/> that creates a single <see cref="System.Net.Http.HttpClient"/> instance.
    /// Not recommended for production use, since it doesn't respect DNS changes.
    ///
    /// Creating a new <see cref="System.Net.Http.HttpClient"/> instance for each request will lead to connection pool exhaustion.
    /// Use the pooling from the 'Microsoft.Extensions.Http' package instead.
    /// </summary>
    internal class DefaultHttpClientFactory : IHttpClientFactory
    {
        private static readonly HttpClient HttpClient = new NonDisposableHttpClient();

        public HttpClient CreateClient(string name)
        {
            return HttpClient;
        }

        private class NonDisposableHttpClient : HttpClient
        {
            protected override void Dispose(bool disposing)
            {
                // Do nothing, since we want to keep the HttpClient instance alive.
            }
        }
    }
}
