/*
 * This is free and unencumbered software released into the public domain.
 * 
 * Anyone is free to copy, modify, publish, use, compile, sell, or
 * distribute this software, either in source code form or as a compiled
 * binary, for any purpose, commercial or non-commercial, and by any
 * means.
 * 
 * In jurisdictions that recognize copyright laws, the author or authors
 * of this software dedicate any and all copyright interest in the
 * software to the public domain. We make this dedication for the benefit
 * of the public at large and to the detriment of our heirs and
 * successors. We intend this dedication to be an overt act of
 * relinquishment in perpetuity of all present and future rights to this
 * software under copyright law.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
 * OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
 * ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 * 
 * For more information, please refer to <https://unlicense.org>
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Topgg.ProxyCheck
{
    [PublicAPI]
    public class ProxyCheck
    {
        /// <summary>
        /// Creates the ProxyCheck object with optional API key and cache provider
        /// </summary>
        /// <remarks>
        /// This constructor is obsolete and will be removed in the future.
        /// Without the HttpClientFactory, the library will use a single HttpClient instance for all requests.
        /// This is not recommended for production use, since it doesn't respect DNS changes.
        /// </remarks>
        /// <param name="apiKey">API key to use</param>
        /// <param name="cacheProvider">Cache provider to use</param>
        [Obsolete("Use the constructor with IHttpClientFactory")]
        public ProxyCheck(string apiKey = "", IProxyCheckCacheProvider? cacheProvider = null)
            : this(new DefaultHttpClientFactory(), apiKey, cacheProvider)
        {
        }

        /// <summary>
        /// Creates the ProxyCheck object with optional API key and cache provider
        /// </summary>
        /// <param name="clientFactory">HttpClient factory to use</param>
        /// <param name="apiKey">API key to use</param>
        /// <param name="cacheProvider">Cache provider to use</param>
        public ProxyCheck(IHttpClientFactory clientFactory, string? apiKey = "",
            IProxyCheckCacheProvider? cacheProvider = null)
        {
            if (apiKey == null)
                apiKey = string.Empty;

            ApiKey = apiKey;
            CacheProvider = cacheProvider;
            _clientFactory = clientFactory;
        }

        [Obsolete("Use the constructor with IHttpClientFactory")]
        public ProxyCheck(IProxyCheckCacheProvider cacheProvider)
            : this("", cacheProvider)
        {
        }

        private const string ProxyCheckUrl = "proxycheck.io/v2";

        /// <summary>
        /// Builds the request URL. Every option, including the API key, goes into the query
        /// string, escaped, so nothing ends up in the path (where HTTP tracing doesn't redact it).
        /// </summary>
        private string BuildRequestUrl()
        {
            var query = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrWhiteSpace(ApiKey))
            {
                query.Add(new KeyValuePair<string, string>("key", ApiKey));
            }

            query.Add(Flag("vpn", IncludeVPN));
            query.Add(Flag("asn", IncludeASN));
            query.Add(Flag("node", IncludeNode));
            query.Add(Flag("time", IncludeTime));
            query.Add(Flag("inf", UseInference));
            query.Add(Flag("port", IncludePort));
            query.Add(Flag("seen", IncludeLastSeen));
            query.Add(Number("days", DayLimit));
            query.Add(Number("risk", (int)RiskLevel));

            var queryString = string.Join(
                "&",
                query.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
            return $"{(UseTLS ? "https" : "http")}://{ProxyCheckUrl}/?{queryString}";
        }

        private static KeyValuePair<string, string> Flag(string name, bool value)
            => new KeyValuePair<string, string>(name, value ? "1" : "0");

        private static KeyValuePair<string, string> Number(string name, int value)
            => new KeyValuePair<string, string>(name, value.ToString(System.Globalization.CultureInfo.InvariantCulture));

        private ProxyCheckRequestOptions _options = new();
        private readonly IHttpClientFactory _clientFactory;

        public IProxyCheckCacheProvider? CacheProvider { get; set; }

        /// <summary>
        /// The API key to use with the query
        /// (Default: String.Empty)
        /// </summary>
        public string ApiKey { get; set; }

        #region Request options kept here for legacy reasons

        /// <summary>
        /// Including checking for VPN
        /// (Default: false)
        /// </summary>
        public bool IncludeVPN
        {
            get => _options.IncludeVPN;
            set => _options.IncludeVPN = value;
        }

        /// <summary>
        /// Use HTTPS when checking IP address (slower)
        /// (Default: false)
        /// </summary>
        public bool UseTLS
        {
            get => _options.UseTLS;
            set => _options.UseTLS = value;
        }

        /// <summary>
        /// Enables viewing the ASN of the network the IP address belongs to
        /// (Default: false)
        /// </summary>
        public bool IncludeASN
        {
            get => _options.IncludeASN;
            set => _options.IncludeASN = value;
        }


        /// <summary>
        /// Use the real-time inference engine
        /// (Default: true)
        /// </summary>
        public bool UseInference
        {
            get => _options.UseInference;
            set => _options.UseInference = value;
        }

        /// <summary>
        /// Includes port number the IP was last seen operating a proxy server on
        /// (Default: false)
        /// </summary>
        public bool IncludePort
        {
            get => _options.IncludePort;
            set => _options.IncludePort = value;
        }

        /// <summary>
        /// Includes the last time the IP address was seen acting as a proxy server
        /// (Default: false)
        /// </summary>
        public bool IncludeLastSeen
        {
            get => _options.IncludeLastSeen;
            set => _options.IncludeLastSeen = value;
        }


        #endregion

        /// <summary>
        /// Includes the answering node in the reply
        /// (Default: false)
        /// </summary>
        public bool IncludeNode { get; set; }

        /// <summary>
        /// Includes the time it took for query
        /// (Default: false)
        /// </summary>
        public bool IncludeTime { get; set; }

        /// <summary>
        /// Restircts the proxy results between now and amount specifed days ago.
        /// (Default: 7)
        /// </summary>
        public int DayLimit { get; set; } = 7;

        /// <summary>
        /// Determines whether you will receive a risk score with the result. If enabled, a risk score will be included
        /// with your response.<br/>
        /// (Default: <see cref="RiskLevel.Disabled"/>) 
        /// </summary>
        public RiskLevel RiskLevel
        {
            get => _options.RiskLevel ?? RiskLevel.Disabled;
            set => _options.RiskLevel = value;
        }


        /// <summary>
        /// Checks to see if the given IP address is a proxy
        /// </summary>
        /// <param name="ipAddress">The IP address to check</param>
        /// <param name="tag">Optional tag</param>
        /// <exception cref="ProxyCheckException">An error has occured</exception>
        /// <returns>Object describing the result.</returns>
        public async Task<ProxyCheckResult> QueryAsync(string ipAddress, string tag = "")
        {
            if (ipAddress == null)
                throw new ArgumentNullException(nameof(ipAddress));

            if (!IPAddress.TryParse(ipAddress, out var ip))
                throw new ArgumentException("Must be a valid IP", nameof(ipAddress));

            return await QueryAsync(ip, tag);
        }

        /// <summary>
        /// Checks to see if the given IP address is a proxy
        /// </summary>
        /// <param name="ipAddress">The IP address to check</param>
        /// <param name="tag">Optional tag</param>
        /// <exception cref="ProxyCheckException">An error has occured</exception>
        /// <returns>Object describing the result.</returns>
        public async Task<ProxyCheckResult> QueryAsync(IPAddress ipAddress, string tag = "")
        {
            return await QueryAsync(new[] { ipAddress }, tag);
        }

        /// <summary>
        /// Checks to see if the given IP address is a proxy
        /// </summary>
        /// <param name="ipAddresses">The IP addresses to check</param>
        /// <param name="tag">Optional tag</param>
        /// <exception cref="ProxyCheckException">An error has occured</exception>
        /// <returns>Object describing the result.</returns>
        public async Task<ProxyCheckResult> QueryAsync(string[] ipAddresses, string tag = "")
        {
            if (ipAddresses == null)
                throw new ArgumentNullException(nameof(ipAddresses));

            if (ipAddresses.Length == 0)
                throw new ArgumentException("Must have at least 1 IP address", nameof(ipAddresses));

            List<IPAddress> ips = new List<IPAddress>(ipAddresses.Length);
            foreach (var ipString in ipAddresses)
            {
                if (!IPAddress.TryParse(ipString, out var ip))
                    throw new ArgumentException(
                        $"Invalid IP address provided. `{ipString}` is not a valid IP");

                ips.Add(ip);
            }

            return await QueryAsync(ips.ToArray(), tag);
        }

        /// <summary>
        /// Checks to see if the given IP address is a proxy
        /// </summary>
        /// <param name="ipAddresses">The IP addresses to check</param>
        /// <param name="tag">Optional tag</param>
        /// <exception cref="ProxyCheckException">An error has occured</exception>
        /// <returns>Object describing the result.</returns>
        public async Task<ProxyCheckResult> QueryAsync(IPAddress[] ipAddresses, string tag = "")
        {
            // We use this for if the cache is used and query is 100% cache hits
            Stopwatch sw = new Stopwatch();

            if (!ipAddresses.Any())
                throw new ArgumentException("Must contain at least 1 IP Address",
                    nameof(ipAddresses));

            ProxyCheckResult? cached = null;
            if (CacheProvider != null)
            {
                sw.Start();

                List<IPAddress>? misses = null;
                foreach (var ip in ipAddresses)
                {
                    var cachedResult = CacheProvider.Get(ip, _options);
                    if (cachedResult == null)
                    {
                        misses ??= new List<IPAddress>(ipAddresses.Length);
                        misses.Add(ip);
                        continue;
                    }

                    cached ??= new ProxyCheckResult
                    {
                        Status = StatusResult.OK,
                        Node = IncludeNode ? "CACHE" : null
                    };
                    cached.Results[ip] = cachedResult;
                }

                sw.Stop();

                if (misses == null) // All cache hits
                {
                    cached!.QueryTime = sw.Elapsed;
                    return cached;
                }

                // Only the misses go to the api
                ipAddresses = misses.ToArray();
            }

            var url = BuildRequestUrl();

            using var client = _clientFactory.CreateClient();

            Dictionary<string, string> postData = new Dictionary<string, string>();

            var ipListString = string.Join(",", ipAddresses.Select(c => c.ToString()));
            postData.Add("ips", ipListString);

            if (!string.IsNullOrWhiteSpace(tag))
                postData.Add("tag", tag);

            FormUrlEncodedContent content = new FormUrlEncodedContent(postData);

            try
            {
                var response = await client.PostAsync(url, content);
                ProxyCheckResult? result;

                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    result = await JsonSerializer.DeserializeAsync(stream,
                        ProxyJsonContext.Default.ProxyCheckResult);
                }

                if (result == null)
                    throw new ProxyCheckException("No result from server");

                // We want to update the cache now
                if (CacheProvider != null)
                {
                    foreach (var item in result.Results)
                        CacheProvider.Set(item.Key, _options, item.Value);
                }

                if (cached == null) // Return current results as none were cache hits
                    return result;

                foreach (var item in cached.Results)
                {
                    if (result.Results.ContainsKey(item.Key))
                        continue; // We don't want to include a cache hit from an IP that was somehow gotten from API too.

                    result.Results.Add(item.Key, item.Value);
                }

                return result;

            }
            catch (ArgumentNullException e)
            {
                throw new ProxyCheckException("URL should not be NULL", e);
            }
            catch (JsonException e)
            {
                throw new ProxyCheckException("Bad JSON from server", e);
            }
            catch (Exception e)
            {
                throw new ProxyCheckException("Unknown state please check the inner exception.", e);
            }
        }

    }
}

