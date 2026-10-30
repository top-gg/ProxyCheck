using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

namespace Topgg.ProxyCheck
{
    public class SimpleInMemoryCache : IProxyCheckCacheProvider
    {
        // One ProxyCheck instance is usually shared across requests, so the cache must be safe to
        // read and write from many threads at once.
        private readonly ConcurrentDictionary<(IPAddress, ProxyCheckRequestOptions), CacheItem> _cacheItems = new();
        private TimeSpan _maxCacheAge = TimeSpan.FromHours(1);

        private class CacheItem
        {
            public ProxyCheckResult.IpResult Result { get; set; } = null!;

            public DateTimeOffset Time { get; set; }

        }

        public SimpleInMemoryCache()
        {

        }

        public SimpleInMemoryCache(TimeSpan maxCacheAge)
        {
            _maxCacheAge = maxCacheAge;
        }

        public ProxyCheckResult.IpResult? GetCacheRecord(IPAddress ip, ProxyCheckRequestOptions options)
        {
            var results = GetCacheRecords(new[] {ip}, options);

            if (results != null && results.TryGetValue(ip, out var record))
                return record;

            return null;
        }

        public IDictionary<IPAddress, ProxyCheckResult.IpResult> GetCacheRecords(IPAddress[] ipAddress, ProxyCheckRequestOptions options)
        {
            var results = new Dictionary<IPAddress, ProxyCheckResult.IpResult>(ipAddress.Length);

            foreach (var ip in ipAddress)
            {
                if (results.ContainsKey(ip))
                    continue;

                if (_cacheItems.TryGetValue((ip, options), out var item) && !IsExpired(item))
                    results.Add(ip, item.Result);
            }

            return results;
        }

        public void SetCacheRecord(IDictionary<IPAddress, ProxyCheckResult.IpResult> results, ProxyCheckRequestOptions options)
        {
            // Let's clean the cache first
            CleanCache();

            // The options object belongs to the caller and can change later, so the key keeps a copy.
            var optionsKey = options.Clone();
            foreach (var item in results)
            {
                _cacheItems[(item.Key, optionsKey)] = new CacheItem
                {
                    Result = item.Value,
                    Time = DateTimeOffset.UtcNow
                };
            }
        }

        private bool IsExpired(CacheItem item) => item.Time + _maxCacheAge <= DateTimeOffset.UtcNow;

        private void CleanCache()
        {
            foreach (var entry in _cacheItems)
            {
                // Removes the entry only if it still holds this item, so a refreshed record stays.
                if (IsExpired(entry.Value))
                    ((ICollection<KeyValuePair<(IPAddress, ProxyCheckRequestOptions), CacheItem>>)_cacheItems).Remove(entry);
            }
        }
    }
}
