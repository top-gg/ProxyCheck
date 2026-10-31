using System.Net;

namespace Topgg.ProxyCheck
{
    public interface IProxyCheckCacheProvider
    {
        /// <summary>
        /// Returns the cached result for the IP address, or null when there is none or it expired.
        /// </summary>
        ProxyCheckResult.IpResult? Get(IPAddress ipAddress, ProxyCheckRequestOptions options);

        void Set(IPAddress ipAddress, ProxyCheckRequestOptions options, ProxyCheckResult.IpResult result);
    }
}
