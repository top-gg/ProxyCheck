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
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Topgg.ProxyCheck
{
    [PublicAPI]
    public class ProxyCheckResult
    {
        private IpResultDictionary _resultsDictionary;

        public ProxyCheckResult()
        {
            Results = new Dictionary<IPAddress, IpResult>();
            _resultsDictionary = new IpResultDictionary(Results);
        }

        /// <summary>
        /// API status result
        /// </summary>
        [JsonPropertyName("status")]
        public StatusResult Status { get; set; }

        /// <summary>
        /// Answering node
        /// </summary>
        [JsonPropertyName("node")]
        public string? Node { get; set; }

        /// <summary>
        /// Dictionary of results for the IP address(es) provided
        /// </summary>
        [JsonIgnore]
        public Dictionary<IPAddress, IpResult> Results { get; internal set; }

        [JsonExtensionData]
        public IDictionary<string, JsonElement> ExtensionData
        {
            get => _resultsDictionary;
            set
            {
                _resultsDictionary.Clear();

                foreach (var kv in value)
                {
                    _resultsDictionary.Add(kv);
                }
            }
        }

        /// <summary>
        /// The amount of time the query took on the server
        /// </summary>
        [JsonPropertyName("query time")]
        public TimeSpan? QueryTime { get; set; }

        [PublicAPI]
        public class IpResult
        {
            /// <summary>
            /// The ASN the IP address belongs to
            /// </summary>
            [JsonPropertyName("asn")]
            public string? ASN { get; set; }

            /// <summary>
            /// The provider the IP address belongs to
            /// </summary>
            [JsonPropertyName("provider")]
            public string? Provider { get; set; }

            /// <summary>
            /// The country the IP address is in.
            /// </summary>
            [JsonPropertyName("country")]
            public string? Country { get; set; }

            /// <summary>
            /// The latitude of the IP address
            /// </summary>
            /// <remarks>
            /// This is not the exact location of the IP address
            /// </remarks>
            [JsonPropertyName("latitude")]
            public double? Latitude { get; set; }

            /// <summary>
            /// The longitude of the IP address
            /// </summary>
            /// <remarks>
            /// This is not the exact location of the IP address
            /// </remarks>
            [JsonPropertyName("longitude")]
            public double? Longitude { get; set; }

            /// <summary>
            /// The city the of the IP address
            /// </summary>
            /// <remarks>
            /// This may not be the exact city
            /// </remarks>
            [JsonPropertyName("city")]
            public string? City { get; set; }

            /// <summary>
            /// ISO Country code of the IP address country
            /// </summary>
            [JsonPropertyName("isocode")]
            public string? ISOCode { get; set; }

            /// <summary>
            /// True if the IP is detected as proxy
            /// False otherwise
            /// </summary>
            [JsonPropertyName("proxy")]
            [JsonConverter(typeof(YesNoJsonConverter))]
            public bool IsProxy { get; set; }

            /// <summary>
            /// The type of proxy detected
            /// </summary>
            [JsonPropertyName("type")]
            public string ProxyType { get; set; } = "";

            /// <summary>
            /// The port the proxy server is operating on
            /// </summary>
            [JsonPropertyName("port")]
            public int? Port { get; set; }

            /// <summary>
            /// Not null when risk is > 0, the risk score of the IP address
            /// </summary>
            [JsonPropertyName("risk")]
            public int? RiskScore { get; set; }
            
            /// <summary>
            /// The last time the proxy server was seen in human readable format.
            /// </summary>
            [JsonPropertyName("last seen human")]
            public string? LastSeenHuman { get; set; }

            /// <summary>
            /// The last time the proxy server was seen in Unix time stamp
            /// </summary>
            [JsonPropertyName("last seen unix")]
            public long? LastSeenUnix { get; set; }

            /// <summary>
            /// The last time the proxy server was seen
            /// </summary>
            public DateTimeOffset? LastSeen
            {
                get
                {
                    if (LastSeenUnix == null)
                        return null;

                    return DateTimeOffset.FromUnixTimeSeconds(LastSeenUnix.Value);
                }

            }

            /// <summary>
            /// If not `null` the description of the error that occured
            /// </summary>
            public string? ErrorMessage { get; set; }

            /// <summary>
            /// True if this item was retrieved from cache
            /// </summary>
            public bool IsCacheHit { get; set; }
        }
    }
}
